using CRMS_Peguit.domain.entities;
using CRMS_Peguit.winforms.Auth;
using CRMS_Peguit.winforms.Controllers;
using CRMS_Peguit.winforms.Controls;
using CRMS_Peguit.winforms.Models.Services;
using CRMS_Peguit.winforms.Views.Shared;

using Lead = CRMS_Peguit.domain.entities.Lead;

namespace CRMS_Peguit.winforms.Views.Leads
{
    public partial class LeadsView : UserControl
    {
        private readonly LeadController _controller;
        private string _filterStage = "All";
        private Button? _btnExport;
        private Panel _pnlEmptyState = null!;
        private int _hoverRowIndex = -1;
        private string _sortColumn = "Name";
        private SortOrder _sortDirection = SortOrder.Ascending;
        private bool _isSkeletonLoading = false;
        private System.Windows.Forms.Timer? _skeletonTimer;
        private PaginationControl _pagination = null!;
        private List<Lead> _currentPageLeads = new();
        private readonly System.Windows.Forms.Timer _searchDebounceTimer;
        private bool _isLoading = false;

        public LeadsView()
        {
            InitializeComponent();
            _controller = new LeadController();

            _searchDebounceTimer = new System.Windows.Forms.Timer { Interval = 300 };
            _searchDebounceTimer.Tick += async (_, _) =>
            {
                _searchDebounceTimer.Stop();
                await RefreshGridAsync(resetPage: true, animate: false);
            };

            InitPagination();
            InitEmptyState();
            ApplyStyling();
            BindEvents();
            _ = RefreshGridAsync(resetPage: true, animate: false);

            this.Load += (_, _) => LayoutToolbar();
            this.Resize += (_, _) => LayoutToolbar();
        }

        private void InitPagination()
        {
            _pagination = new PaginationControl();
            _pagination.SetItemLabel("leads");
            _pagination.PageChanged += async (_, _) => await RefreshGridAsync(resetPage: false, animate: true);
            _pagination.PageSizeChanged += async (_, _) => await RefreshGridAsync(resetPage: true, animate: false);
            pnlCard.Controls.Add(_pagination);
            _pagination.BringToFront();
        }

        private void InitEmptyState()
        {
            _pnlEmptyState = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.White,
                Visible = false
            };

            var innerPanel = new Panel
            {
                Size = new Size(420, 240),
                BackColor = Color.Transparent
            };

            var lblIcon = new Label
            {
                Text = "🔍",
                Font = new Font("Segoe UI Emoji", 34f),
                ForeColor = Color.FromArgb(148, 163, 184),
                TextAlign = ContentAlignment.MiddleCenter,
                Size = new Size(70, 70),
                Location = new Point((420 - 70) / 2, 8)
            };

            var lblTitle = new Label
            {
                Text = "No leads found",
                Font = new Font("Segoe UI", 13f, FontStyle.Bold),
                ForeColor = Color.FromArgb(15, 23, 42),
                TextAlign = ContentAlignment.MiddleCenter,
                Size = new Size(400, 28),
                Location = new Point(10, 85)
            };

            var lblDesc = new Label
            {
                Text = "We couldn't find any leads matching your criteria.\nTry searching with different terms or selecting another stage.",
                Font = new Font("Segoe UI", 9.5f),
                ForeColor = Color.FromArgb(100, 116, 139),
                TextAlign = ContentAlignment.MiddleCenter,
                Size = new Size(400, 42),
                Location = new Point(10, 116)
            };

            var btnReset = new Button
            {
                Text = "Clear Filters & Search",
                Font = new Font("Segoe UI", 9.5f, FontStyle.Bold),
                ForeColor = Color.FromArgb(15, 91, 158),
                BackColor = Color.FromArgb(239, 246, 255),
                Cursor = Cursors.Hand,
                FlatStyle = FlatStyle.Flat,
                Size = new Size(180, 36),
                Location = new Point((420 - 180) / 2, 172)
            };
            btnReset.FlatAppearance.BorderColor = Color.FromArgb(191, 219, 254);
            UiRadiusHelper.StyleButton(btnReset, 8);
            btnReset.Click += (_, _) =>
            {
                txtSearch.Clear();
                SetFilter("All");
            };

            innerPanel.Controls.Add(lblIcon);
            innerPanel.Controls.Add(lblTitle);
            innerPanel.Controls.Add(lblDesc);
            innerPanel.Controls.Add(btnReset);

            _pnlEmptyState.Controls.Add(innerPanel);
            _pnlEmptyState.Resize += (_, _) =>
            {
                innerPanel.Location = new Point((_pnlEmptyState.Width - innerPanel.Width) / 2, Math.Max(20, (_pnlEmptyState.Height - innerPanel.Height) / 2));
            };

            pnlCard.Controls.Add(_pnlEmptyState);
            _pnlEmptyState.BringToFront();
        }

        private void ApplyStyling()
        {
            UiRadiusHelper.StyleCard(pnlCard, 12);
            UiRadiusHelper.StyleButton(btnAdd, 8);
            UiRadiusHelper.ApplyPillShape(btnFilterAll);
            UiRadiusHelper.ApplyPillShape(btnFilterNew);
            UiRadiusHelper.ApplyPillShape(btnFilterContacted);
            UiRadiusHelper.ApplyPillShape(btnFilterQualified);
            UiRadiusHelper.ApplyPillShape(btnFilterConverted);
        }

        private void BindEvents()
        {
            btnAdd.Visible = RbacService.CanCreateSalesRecord;
            btnAdd.Click += BtnAddClick;
            txtSearch.TextChanged += (_, _) =>
            {
                _searchDebounceTimer.Stop();
                _searchDebounceTimer.Start();
            };

            if (RbacService.CanExportData)
            {
                _btnExport = new Button
                {
                    Text = "📥 Export CSV",
                    BackColor = Color.White,
                    ForeColor = Color.FromArgb(15, 91, 158),
                    Cursor = Cursors.Hand,
                    FlatStyle = FlatStyle.Flat,
                    Font = new Font("Segoe UI", 9.5f, FontStyle.Bold),
                    Size = new Size(130, 36)
                };
                _btnExport.FlatAppearance.BorderColor = Color.FromArgb(15, 91, 158);
                _btnExport.Click += (_, _) => ExportToCsv();
                UiRadiusHelper.StyleButton(_btnExport, 8);
                Controls.Add(_btnExport);
                _btnExport.BringToFront();
            }

            btnFilterAll.Click += (_, _) => SetFilter("All");
            btnFilterNew.Click += (_, _) => SetFilter("New");
            btnFilterContacted.Click += (_, _) => SetFilter("Contacted");
            btnFilterQualified.Click += (_, _) => SetFilter("Qualified");
            btnFilterConverted.Click += (_, _) => SetFilter("Converted");

            // Modern Grid Styling (52px row height for uniform CRM table presentation)
            UiGridHelper.ApplyModernGridStyle(grid, 52);
            UiRadiusHelper.SetPadding(txtSearch, 12, 12);

            grid.CellPainting += Grid_CellPainting;
            grid.CellContentClick += GridCellContentClick;
            grid.ColumnHeaderMouseClick += Grid_ColumnHeaderMouseClick;

            grid.CellMouseEnter += (s, e) =>
            {
                if (e.RowIndex >= 0 && e.RowIndex != _hoverRowIndex)
                {
                    int old = _hoverRowIndex;
                    _hoverRowIndex = e.RowIndex;
                    if (old >= 0 && old < grid.RowCount) grid.InvalidateRow(old);
                    if (_hoverRowIndex < grid.RowCount) grid.InvalidateRow(_hoverRowIndex);
                }
            };

            grid.CellMouseLeave += (s, e) =>
            {
                if (e.RowIndex >= 0 && e.RowIndex == _hoverRowIndex)
                {
                    int old = _hoverRowIndex;
                    _hoverRowIndex = -1;
                    if (old >= 0 && old < grid.RowCount) grid.InvalidateRow(old);
                }
            };

            grid.CellDoubleClick += (_, e) =>
            {
                if (e.RowIndex < 0 || _isSkeletonLoading) return;
                var lead = GetLeadAtRow(e.RowIndex);
                if (lead is not null) ViewLead(lead);
            };
        }

        private async void SetFilter(string stage)
        {
            if (string.Equals(_filterStage, stage, StringComparison.OrdinalIgnoreCase) && !string.Equals(stage, "All", StringComparison.OrdinalIgnoreCase))
            {
                _filterStage = "All";
            }
            else
            {
                _filterStage = stage;
            }
            await RefreshGridAsync(resetPage: true, animate: true);
        }

        private void UpdateFilterPillStyles(LeadStageCounts counts)
        {
            lblSubtitle.Text = $"{counts.Total} total · {counts.Qualified} qualified";

            var pills = new[]
            {
                (btnFilterAll, "All", counts.Total),
                (btnFilterNew, "New", counts.New),
                (btnFilterContacted, "Contacted", counts.Contacted),
                (btnFilterQualified, "Qualified", counts.Qualified),
                (btnFilterConverted, "Converted", counts.Converted)
            };

            foreach (var (btn, name, count) in pills)
            {
                bool isSelected = string.Equals(_filterStage, name, StringComparison.OrdinalIgnoreCase);
                btn.Text = $"{name}  {count}";
                UiRadiusHelper.StyleFilterPill(btn, isSelected);
                btn.Width = TextRenderer.MeasureText(btn.Text, btn.Font).Width + 32;
            }

            LayoutToolbar();
        }

        private async void Grid_ColumnHeaderMouseClick(object? sender, DataGridViewCellMouseEventArgs e)
        {
            if (e.ColumnIndex < 0 || _isSkeletonLoading) return;
            string colName = grid.Columns[e.ColumnIndex].Name;
            if (colName == "Actions" || colName == "LeadId") return;

            if (string.Equals(_sortColumn, colName, StringComparison.OrdinalIgnoreCase))
            {
                _sortDirection = (_sortDirection == SortOrder.Ascending) ? SortOrder.Descending : SortOrder.Ascending;
            }
            else
            {
                _sortColumn = colName;
                _sortDirection = SortOrder.Ascending;
            }

            await RefreshGridAsync(resetPage: false, animate: false);
        }

        public void RefreshGrid(bool reloadFromDb = true, bool animate = false)
        {
            _ = RefreshGridAsync(resetPage: false, animate: animate);
        }

        public async Task RefreshGridAsync(bool resetPage = false, bool animate = false)
        {
            if (_isLoading) return;
            _isLoading = true;

            try
            {
                int page = resetPage ? 1 : _pagination.CurrentPage;
                int pageSize = _pagination.PageSize;

                var counts = await _controller.GetStageCountsAsync();
                UpdateFilterPillStyles(counts);

                var pagedResult = await _controller.GetPagedAsync(
                    page,
                    pageSize,
                    txtSearch.Text,
                    _filterStage,
                    _sortColumn,
                    _sortDirection == SortOrder.Ascending);

                _currentPageLeads = pagedResult.Items;
                _pagination.UpdatePagination(pagedResult.TotalCount, pagedResult.PageNumber, pagedResult.PageSize);

                if (animate)
                {
                    _isSkeletonLoading = true;
                    _skeletonTimer?.Stop();
                    _skeletonTimer?.Dispose();

                    BindCurrentPage();
                    grid.Invalidate();

                    _skeletonTimer = new System.Windows.Forms.Timer { Interval = 130 };
                    _skeletonTimer.Tick += (_, _) =>
                    {
                        _skeletonTimer?.Stop();
                        _skeletonTimer?.Dispose();
                        _skeletonTimer = null;
                        _isSkeletonLoading = false;
                        grid.Invalidate();
                    };
                    _skeletonTimer.Start();
                }
                else
                {
                    _isSkeletonLoading = false;
                    BindCurrentPage();
                }
            }
            finally
            {
                _isLoading = false;
            }
        }

        private void BindCurrentPage()
        {
            grid.Columns.Clear();
            grid.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.None;

            var pageItems = _currentPageLeads
                .Select(lead => new
                {
                    lead.LeadId,
                    Name = lead.FullName,
                    Email = string.IsNullOrWhiteSpace(lead.Email) ? "-" : lead.Email,
                    Phone = string.IsNullOrWhiteSpace(lead.Phone) ? "-" : lead.Phone,
                    Source = string.IsNullOrWhiteSpace(lead.Source) ? "Website" : lead.Source,
                    ExpectedValue = lead.ExpectedValue.HasValue ? $"₱{lead.ExpectedValue.Value:N2}" : "-",
                    Stage = lead.Stage.ToUpper(),
                    Assignment = lead.AssignmentStatus.ToUpper()
                })
                .ToList();

            grid.DataSource = pageItems;

            var idCol = grid.Columns["LeadId"];
            if (idCol is not null) idCol.Visible = false;

            grid.ShowCellToolTips = true;

            if (grid.Columns["Name"] is DataGridViewColumn nameCol)
            {
                nameCol.HeaderText = "NAME";
                nameCol.FillWeight = 160;
                nameCol.MinimumWidth = 140;
                nameCol.SortMode = DataGridViewColumnSortMode.Programmatic;
            }
            if (grid.Columns["Email"] is DataGridViewColumn emailCol)
            {
                emailCol.HeaderText = "EMAIL";
                emailCol.FillWeight = 140;
                emailCol.MinimumWidth = 120;
                emailCol.SortMode = DataGridViewColumnSortMode.Programmatic;
            }
            if (grid.Columns["Phone"] is DataGridViewColumn phoneCol)
            {
                phoneCol.HeaderText = "PHONE";
                phoneCol.FillWeight = 100;
                phoneCol.MinimumWidth = 90;
                phoneCol.SortMode = DataGridViewColumnSortMode.Programmatic;
            }
            if (grid.Columns["Source"] is DataGridViewColumn srcCol)
            {
                srcCol.HeaderText = "SOURCE";
                srcCol.FillWeight = 90;
                srcCol.MinimumWidth = 80;
                srcCol.SortMode = DataGridViewColumnSortMode.Programmatic;
            }
            if (grid.Columns["ExpectedValue"] is DataGridViewColumn valCol)
            {
                valCol.HeaderText = "EXPECTED VALUE";
                valCol.FillWeight = 110;
                valCol.MinimumWidth = 105;
                valCol.SortMode = DataGridViewColumnSortMode.Programmatic;
                valCol.HeaderCell.Style.Alignment = DataGridViewContentAlignment.MiddleRight;
                valCol.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight;
            }
            if (grid.Columns["Stage"] is DataGridViewColumn stageCol)
            {
                stageCol.HeaderText = "STAGE";
                stageCol.FillWeight = 90;
                stageCol.MinimumWidth = 80;
                stageCol.SortMode = DataGridViewColumnSortMode.Programmatic;
                stageCol.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleLeft;
                stageCol.HeaderCell.Style.Alignment = DataGridViewContentAlignment.MiddleLeft;
            }
            if (grid.Columns["Assignment"] is DataGridViewColumn assignCol)
            {
                assignCol.HeaderText = "ASSIGNMENT";
                assignCol.FillWeight = 100;
                assignCol.MinimumWidth = 90;
                assignCol.SortMode = DataGridViewColumnSortMode.Programmatic;
                assignCol.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleLeft;
                assignCol.HeaderCell.Style.Alignment = DataGridViewContentAlignment.MiddleLeft;
            }

            UiGridHelper.AddActionsColumn(grid, 64);
            UiGridHelper.EnforceTableStandards(grid);
            grid.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
            _pnlEmptyState.Visible = (_currentPageLeads.Count == 0);
        }

        private void Grid_CellPainting(object? sender, DataGridViewCellPaintingEventArgs e)
        {
            if (e.Graphics is null) return;

            // Column Header Sort Indicator (Uniform header #F8FAFC with bottom border #E2E8F0, no blue cell glitch)
            if (e.RowIndex == -1 && e.ColumnIndex >= 0)
            {
                using (var hBrush = new SolidBrush(UiGridHelper.HeaderBg))
                {
                    e.Graphics.FillRectangle(hBrush, e.CellBounds);
                }

                var col = grid.Columns[e.ColumnIndex];
                var formatFlags = TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis;
                if (col.HeaderCell.Style.Alignment == DataGridViewContentAlignment.MiddleRight)
                    formatFlags |= TextFormatFlags.Right;
                else if (col.HeaderCell.Style.Alignment == DataGridViewContentAlignment.MiddleCenter)
                    formatFlags |= TextFormatFlags.HorizontalCenter;
                else
                    formatFlags |= TextFormatFlags.Left;

                var headerTextRect = new Rectangle(e.CellBounds.X + 12, e.CellBounds.Y, e.CellBounds.Width - 24, e.CellBounds.Height);
                TextRenderer.DrawText(e.Graphics, col.HeaderText, grid.ColumnHeadersDefaultCellStyle.Font, headerTextRect, UiGridHelper.HeaderText, formatFlags);

                using (var bPen = new Pen(Color.FromArgb(226, 232, 240), 1f))
                {
                    e.Graphics.DrawLine(bPen, e.CellBounds.Left, e.CellBounds.Bottom - 1, e.CellBounds.Right, e.CellBounds.Bottom - 1);
                }

                string colName = col.Name;
                if (string.Equals(colName, _sortColumn, StringComparison.OrdinalIgnoreCase))
                {
                    string arrow = _sortDirection == SortOrder.Ascending ? " ▲" : " ▼";
                    using var sortFont = new Font("Segoe UI", 7f, FontStyle.Bold);
                    
                    var textSize = TextRenderer.MeasureText(e.Graphics, col.HeaderText, grid.ColumnHeadersDefaultCellStyle.Font);
                    int arrowX = (col.HeaderCell.Style.Alignment == DataGridViewContentAlignment.MiddleRight)
                        ? e.CellBounds.Right - 16
                        : e.CellBounds.Left + textSize.Width + 16;
                    int arrowY = e.CellBounds.Y + (e.CellBounds.Height - 12) / 2;

                    TextRenderer.DrawText(e.Graphics, arrow, sortFont, new Point(arrowX, arrowY), Color.FromArgb(15, 91, 158));
                }
                e.Handled = true;
                return;
            }

            if (e.RowIndex < 0 || e.ColumnIndex < 0) return;

            // Determine row background with seamless hover and selection
            Color rowBg = grid.Rows[e.RowIndex].Selected
                ? UiGridHelper.SelectionBg
                : (e.RowIndex == _hoverRowIndex
                    ? UiGridHelper.RowHover
                    : (e.RowIndex % 2 == 1 ? UiGridHelper.RowAlternate : UiGridHelper.RowNormal));

            // Skeleton Loading State
            if (_isSkeletonLoading)
            {
                using (var bgBrush = new SolidBrush(rowBg))
                {
                    e.Graphics.FillRectangle(bgBrush, e.CellBounds);
                }

                // Shimmering placeholder bar
                e.Graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
                int barHeight = 14;
                int barY = e.CellBounds.Y + (e.CellBounds.Height - barHeight) / 2;
                int barWidth = Math.Max(30, e.CellBounds.Width - 36);
                int barX = e.CellBounds.X + 16;

                using (var shimmerBrush = new SolidBrush(Color.FromArgb(226, 232, 240)))
                using (var path = GetRoundedRectangle(new Rectangle(barX, barY, barWidth, barHeight), 4))
                {
                    e.Graphics.FillPath(shimmerBrush, path);
                }

                using (var dividerPen = new Pen(UiGridHelper.GridBorder, 1f))
                {
                    e.Graphics.DrawLine(dividerPen, e.CellBounds.Left, e.CellBounds.Bottom - 1, e.CellBounds.Right, e.CellBounds.Bottom - 1);
                }

                e.Handled = true;
                return;
            }

            string columnName = grid.Columns[e.ColumnIndex].Name;

            // ── STAGE INDICATOR (Minimalist 6px dot + text, NO pills, Left-aligned at 12px) ──
            if (columnName == "Stage" && e.Value != null)
            {
                string stage = e.Value.ToString() ?? "";
                UiGridHelper.PaintStatusIndicator(grid, e, stage, center: false);
            }
            // ── ASSIGNMENT STATUS INDICATOR (Minimalist 6px dot + text, NO pills, Left-aligned at 12px) ──
            else if (columnName == "Assignment" && e.Value != null)
            {
                string rawAssign = e.Value.ToString() ?? "";
                string displayLabel = rawAssign.Contains("PENDING", StringComparison.OrdinalIgnoreCase)
                    ? "PENDING REVIEW"
                    : (rawAssign.Contains("APPROVED", StringComparison.OrdinalIgnoreCase)
                        ? "APPROVED"
                        : (string.IsNullOrWhiteSpace(rawAssign) ? "UNASSIGNED" : rawAssign));

                UiGridHelper.PaintStatusIndicator(grid, e, displayLabel, center: false);
            }
            // ── NAME COLUMN (Initial avatar + text at uniform 12px inset) ──
            else if (columnName == "Name" && e.Value != null)
            {
                string name = e.Value.ToString() ?? "";
                string initials = GetInitials(name);
                UiGridHelper.PaintAvatarCell(grid, e, name, initials, Color.FromArgb(27, 80, 136), Color.White);
            }
            // ── EMAIL COLUMN (Uniform 12px inset) ──
            else if (columnName == "Email" && e.Value != null)
            {
                string email = e.Value.ToString() ?? "";
                using var font = new Font("Segoe UI", 9.5f);
                UiGridHelper.PaintTextCell(grid, e, email, font, Color.FromArgb(15, 91, 158),
                    TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis, leftPadding: 12);
            }
            // ── EXPECTED VALUE COLUMN (Right-aligned currency at uniform right margin) ──
            else if (columnName == "ExpectedValue" && e.Value != null)
            {
                string valStr = e.Value.ToString() ?? "-";
                using var font = new Font("Segoe UI", 9.5f, FontStyle.Bold);
                UiGridHelper.PaintTextCell(grid, e, valStr, font, Theme.TextPrimary,
                    TextFormatFlags.Right | TextFormatFlags.VerticalCenter, leftPadding: 8, rightPadding: 12);
            }
            // ── STANDARD COLUMNS (Phone, Source) ──
            else if (columnName != "Actions")
            {
                string text = e.Value?.ToString() ?? "";
                using var font = new Font("Segoe UI", 9.5f);
                UiGridHelper.PaintTextCell(grid, e, text, font, Theme.TextPrimary,
                    TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis, leftPadding: 12);
            }
        }

        private static string GetInitials(string fullName)
        {
            if (string.IsNullOrWhiteSpace(fullName)) return "U";
            var parts = fullName.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length == 1) return parts[0].Substring(0, Math.Min(2, parts[0].Length)).ToUpper();
            return $"{char.ToUpper(parts[0][0])}{char.ToUpper(parts[^1][0])}";
        }

        private static System.Drawing.Drawing2D.GraphicsPath GetRoundedRectangle(Rectangle bounds, int radius)
        {
            var path = new System.Drawing.Drawing2D.GraphicsPath();
            int d = radius * 2;
            path.AddArc(bounds.X, bounds.Y, d, d, 180, 90);
            path.AddArc(bounds.Right - d, bounds.Y, d, d, 270, 90);
            path.AddArc(bounds.Right - d, bounds.Bottom - d, d, d, 0, 90);
            path.AddArc(bounds.X, bounds.Bottom - d, d, d, 90, 90);
            path.CloseFigure();
            return path;
        }

        private Lead? GetLeadAtRow(int rowIndex)
        {
            if (rowIndex < 0 || rowIndex >= grid.Rows.Count) return null;
            object? idValue = grid.Rows[rowIndex].Cells["LeadId"].Value;
            if (idValue is null || !int.TryParse(idValue.ToString(), out int leadId))
                return null;

            return _currentPageLeads.FirstOrDefault(lead => lead.LeadId == leadId) ?? _controller.GetById(leadId);
        }

        private Lead? GetSelectedLead()
        {
            if (grid.CurrentRow is null) return null;
            return GetLeadAtRow(grid.CurrentRow.Index);
        }

        private void GridCellContentClick(object? sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex < 0 || e.ColumnIndex < 0) return;
            if (grid.Columns[e.ColumnIndex] is not ActionsColumn) return;

            grid.Rows[e.RowIndex].Selected = true;
            grid.CurrentCell = grid.Rows[e.RowIndex].Cells[e.ColumnIndex];

            Lead? lead = GetSelectedLead();
            if (lead is null) return;

            var menu = new ContextMenuStrip
            {
                BackColor = Theme.Surface,
                ForeColor = Theme.TextPrimary,
                Font = new Font("Segoe UI", 10)
            };

            var viewItem = new ToolStripMenuItem("View");
            viewItem.Click += (_, _) => ViewLead(lead);
            menu.Items.Add(viewItem);

            var messageItem = new ToolStripMenuItem("Message");
            messageItem.Click += (_, _) => MessageLead(lead);
            messageItem.Enabled = CRMS_Peguit.winforms.Models.Services.ContactEmailService.IsValidEmail(lead.Email);
            menu.Items.Add(messageItem);

            if (CRMS_Peguit.winforms.Auth.RbacService.CanEditRecord(lead.AssignedAgentId, lead.CreatedByUserId, lead.AssignmentStatus))
            {
                var editItem = new ToolStripMenuItem("Edit");
                editItem.Click += (_, _) => EditLead(lead);
                menu.Items.Add(editItem);
            }

            if (CRMS_Peguit.winforms.Auth.RbacService.CanEditRecord(lead.AssignedAgentId, lead.CreatedByUserId) &&
                !string.Equals(lead.Stage, "converted", StringComparison.OrdinalIgnoreCase))
            {
                var convertItem = new ToolStripMenuItem("Convert to Customer");
                convertItem.Click += (_, _) => ConvertLead(lead);
                menu.Items.Add(convertItem);
            }

            if (CRMS_Peguit.winforms.Auth.RbacService.CanEditRecord(lead.AssignedAgentId, lead.CreatedByUserId) &&
                !string.Equals(lead.Stage, "lost", StringComparison.OrdinalIgnoreCase) &&
                !string.Equals(lead.Stage, "converted", StringComparison.OrdinalIgnoreCase))
            {
                var lostItem = new ToolStripMenuItem("Mark as Lost");
                lostItem.Click += (_, _) => MarkLeadLost(lead);
                menu.Items.Add(lostItem);
            }

            if (CRMS_Peguit.winforms.Auth.RbacService.CanEditRecord(lead.AssignedAgentId, lead.CreatedByUserId) &&
                string.Equals(lead.Stage, "lost", StringComparison.OrdinalIgnoreCase))
            {
                var restoreItem = new ToolStripMenuItem("Restore Lead");
                restoreItem.Click += (_, _) => RestoreLostLead(lead);
                menu.Items.Add(restoreItem);
            }

            if (CRMS_Peguit.winforms.Auth.RbacService.CanAssignRecords)
            {
                var assignItem = new ToolStripMenuItem("Assign Agent");
                assignItem.Click += (_, _) =>
                {
                    using var dlg = new AssignAgentDialog(lead.FullName, _controller.GetAgents(), lead.AssignedAgentId);
                    if (dlg.ShowDialog() == DialogResult.OK)
                    {
                        _controller.AssignAgent(lead, dlg.SelectedAgentId, dlg.ApproveNow, dlg.ReviewNotes);
                        RefreshGrid();
                    }
                };
                menu.Items.Add(assignItem);
            }

            if (CRMS_Peguit.winforms.Auth.RbacService.CanApproveAssignments &&
                string.Equals(lead.AssignmentStatus, "pending_review", StringComparison.OrdinalIgnoreCase))
            {
                var approveItem = new ToolStripMenuItem("Approve Assignment");
                approveItem.Click += (_, _) => ApproveLeadAssignment(lead);
                menu.Items.Add(approveItem);
            }

            if (CRMS_Peguit.winforms.Auth.RbacService.CanArchiveRecord(lead.AssignedAgentId, lead.CreatedByUserId))
            {
                var archiveItem = new ToolStripMenuItem("Archive");
                archiveItem.Click += (_, _) => ArchiveLead(lead);
                menu.Items.Add(archiveItem);
            }

            menu.Show(grid, grid.PointToClient(Cursor.Position));
        }

        private void ViewLead(Lead lead)
        {
            using var form = new LeadDetailForm(lead, _controller);
            form.ShowDialog();
        }

        private void EditLead(Lead lead)
        {
            using var form = new LeadInputForm(lead);
            if (form.ShowDialog() == DialogResult.OK && form.Result is not null)
            {
                _controller.Update(form.Result);
                RefreshGrid();
            }
        }

        private void ArchiveLead(Lead lead)
        {
            var confirmation = MessageBox.Show(
                $"Archive '{lead.FullName}'?", "Archive Lead",
                MessageBoxButtons.YesNo, MessageBoxIcon.Question);

            if (confirmation != DialogResult.Yes) return;

            _controller.SoftDelete(lead);
            RefreshGrid();
        }

        private void BtnAddClick(object? sender, EventArgs e)
        {
            using var form = new LeadInputForm();
            if (form.ShowDialog() == DialogResult.OK && form.Result is not null)
            {
                _controller.Add(form.Result);
                RefreshGrid();
            }
        }

        private void MessageLead(Lead lead)
        {
            using var form = new CRMS_Peguit.winforms.Views.Shared.EmailMessageForm(lead.FullName, lead.Email);
            if (form.ShowDialog() == DialogResult.OK)
            {
                _controller.LogEmail(lead, form.SentSubject);
                RefreshGrid();
            }
        }

        private void MarkLeadLost(Lead lead)
        {
            var confirmation = MessageBox.Show(
                $"Mark '{lead.FullName}' as lost?",
                "Lead Lifecycle",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Question);

            if (confirmation != DialogResult.Yes) return;

            _controller.MarkLost(lead);
            RefreshGrid();
        }

        private void RestoreLostLead(Lead lead)
        {
            var confirmation = MessageBox.Show(
                $"Restore '{lead.FullName}' back to active follow-up?",
                "Restore Lead",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Question);

            if (confirmation != DialogResult.Yes) return;

            _controller.RestoreFromLost(lead);
            RefreshGrid();
        }

        private void ApproveLeadAssignment(Lead lead)
        {
            _controller.ApproveAssignment(lead, "Reviewed from Leads module.");
            MessageBox.Show(
                $"Assignment for '{lead.FullName}' has been approved.",
                "Assignment Approved",
                MessageBoxButtons.OK,
                MessageBoxIcon.Information);
            RefreshGrid();
        }

        private void ConvertLead(Lead lead)
        {
            var confirmation = MessageBox.Show(
                $"Convert '{lead.FullName}' into a customer?\n\n" +
                "A new customer record will be created and this lead will be marked as converted.",
                "Convert Lead", MessageBoxButtons.YesNo, MessageBoxIcon.Question);

            if (confirmation != DialogResult.Yes) return;

            Customer customer = _controller.ConvertToCustomer(lead);

            MessageBox.Show(
                $"'{lead.FullName}' is now customer #{customer.CustomerId}.",
                "Conversion Complete", MessageBoxButtons.OK, MessageBoxIcon.Information);

            RefreshGrid();
        }

        private void ExportToCsv()
        {
            using var sfd = new SaveFileDialog
            {
                Filter = "CSV File (*.csv)|*.csv",
                FileName = $"Leads_Export_{DateTime.Now:yyyyMMdd_HHmm}.csv"
            };
            if (sfd.ShowDialog() == DialogResult.OK)
            {
                var leads = _controller.GetAll().ToList();
                var sb = new System.Text.StringBuilder();
                sb.AppendLine("LeadId,FullName,Source,Stage,Priority,ExpectedValue,Phone,Email,AssignedAgent,CreatedAt");
                foreach (var l in leads)
                {
                    sb.AppendLine($"\"{l.LeadId}\",\"{l.FullName}\",\"{l.Source}\",\"{l.Stage}\",\"{l.Priority}\",\"{l.ExpectedValue}\",\"{l.Phone}\",\"{l.Email}\",\"{_controller.GetAssignedAgentName(l.AssignedAgentId)}\",\"{l.CreatedAt:yyyy-MM-dd}\"");
                }
                System.IO.File.WriteAllText(sfd.FileName, sb.ToString());
                MessageBox.Show("Leads exported successfully.", "Export Complete", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }

        private static bool ContainsText(string? value, string search)
        {
            return !string.IsNullOrWhiteSpace(value) &&
                   value.Contains(search, StringComparison.OrdinalIgnoreCase);
        }

        private void LayoutToolbar()
        {
            if (this.IsDisposed) return;

            int rightPadding = 32;
            int leftMargin = 32;
            int totalWidth = ClientSize.Width;

            // Reposition title and subtitle inline next to each other
            lblTitle.Location = new Point(leftMargin, 20);
            lblSubtitle.Location = new Point(lblTitle.Right + 14, lblTitle.Top + 12);
            lblSubtitle.BringToFront();

            // Position header action buttons
            int rightEdge = totalWidth - rightPadding;
            if (btnAdd.Visible)
            {
                btnAdd.Left = rightEdge - btnAdd.Width;
                btnAdd.Top = 20;
                rightEdge = btnAdd.Left - 10;
            }
            if (_btnExport != null && _btnExport.Visible)
            {
                _btnExport.Left = rightEdge - _btnExport.Width;
                _btnExport.Top = 20;
            }

            int y = 78;

            // Layout filter pills
            var pills = new[] { btnFilterConverted, btnFilterQualified, btnFilterContacted, btnFilterNew, btnFilterAll };
            int filterRight = totalWidth - rightPadding;
            int totalFilterWidth = 0;
            foreach (var p in pills) totalFilterWidth += p.Width + 8;

            int availableForSearch = totalWidth - leftMargin - rightPadding - totalFilterWidth - 20;

            if (availableForSearch >= 200)
            {
                // Single row: search on left, filters aligned to right
                foreach (var p in pills)
                {
                    p.Top = y;
                    p.Height = 32;
                    p.Left = filterRight - p.Width;
                    filterRight = p.Left - 8;
                }

                txtSearch.Top = y;
                txtSearch.Left = leftMargin;
                txtSearch.Height = 32;
                txtSearch.Width = Math.Min(360, availableForSearch);

                pnlCard.Top = 126;
                pnlCard.Height = Math.Max(100, ClientSize.Height - 126 - 24);
            }
            else
            {
                // Two rows: search on row 1, filter pills wrapped to row 2
                txtSearch.Top = y;
                txtSearch.Left = leftMargin;
                txtSearch.Height = 32;
                txtSearch.Width = Math.Max(180, totalWidth - leftMargin - rightPadding);

                int filterX = leftMargin;
                int pillY = y + 40;
                var forwardPills = new[] { btnFilterAll, btnFilterNew, btnFilterContacted, btnFilterQualified, btnFilterConverted };
                foreach (var p in forwardPills)
                {
                    p.Top = pillY;
                    p.Height = 32;
                    p.Left = filterX;
                    filterX += p.Width + 8;
                }

                pnlCard.Top = pillY + 42;
                pnlCard.Height = Math.Max(100, ClientSize.Height - pnlCard.Top - 24);
            }

            pnlCard.Left = leftMargin;
            pnlCard.Width = Math.Max(100, totalWidth - leftMargin - rightPadding);
        }
    }
}
