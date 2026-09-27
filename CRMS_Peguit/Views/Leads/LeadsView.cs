using CRMS_Peguit.domain.entities;
using CRMS_Peguit.winforms.Auth;
using CRMS_Peguit.winforms.Controllers;
using CRMS_Peguit.winforms.Controls;
using CRMS_Peguit.winforms.Models.Services;
using CRMS_Peguit.winforms.Services;
using CRMS_Peguit.winforms.Views.Shared;

using Lead = CRMS_Peguit.domain.entities.Lead;

namespace CRMS_Peguit.winforms.Views.Leads
{
    public partial class LeadsView : UserControl
    {
        private readonly LeadController _controller;
        private string _filterStage = "All";
        private ScreenFilterCoordinator _filterCoord = null!;
        private Button? _btnExport;
        private Panel _pnlEmptyState = null!;
        private int _hoverRowIndex = -1;
        private string _sortColumn = "Name";
        private SortOrder _sortDirection = SortOrder.Ascending;
        private GridSkeletonOverlay _gridSkeleton = null!;
        private PaginationControl _pagination = null!;
        private List<Lead> _currentPageLeads = new();
        private readonly System.Windows.Forms.Timer _searchDebounceTimer;
        private bool _isLoading = false;

        private TableLayoutPanel _pnlKpiContainer = null!;
        private KpiCard _kpiTotal = null!;
        private KpiCard _kpiNew = null!;
        private KpiCard _kpiContacted = null!;
        private KpiCard _kpiQualified = null!;
        private KpiCard _kpiConverted = null!;

        public LeadsView()
        {
            InitializeComponent();
            _controller = new LeadController();

            _gridSkeleton = GridSkeletonOverlay.CreateForGrid(grid);

            _searchDebounceTimer = new System.Windows.Forms.Timer { Interval = 300 };
            _searchDebounceTimer.Tick += async (_, _) =>
            {
                _searchDebounceTimer.Stop();
                await RefreshGridAsync(resetPage: true);
            };

            InitKpis();
            InitPagination();
            InitEmptyState();
            ApplyStyling();
            BindEvents();

            _kpiTotal.ShowLoadingSkeleton();
            _kpiNew.ShowLoadingSkeleton();
            _kpiContacted.ShowLoadingSkeleton();
            _kpiQualified.ShowLoadingSkeleton();
            _kpiConverted.ShowLoadingSkeleton();
            _gridSkeleton.ShowSkeleton();

            _ = RefreshGridAsync(resetPage: true);

            this.Load += (_, _) => LayoutToolbar();
            this.Resize += (_, _) => LayoutToolbar();
        }

        private void InitKpis()
        {
            _pnlKpiContainer = new TableLayoutPanel
            {
                ColumnCount = 5,
                RowCount = 1,
                Height = 88,
                BackColor = Color.Transparent
            };
            _pnlKpiContainer.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 20f));
            _pnlKpiContainer.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 20f));
            _pnlKpiContainer.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 20f));
            _pnlKpiContainer.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 20f));
            _pnlKpiContainer.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 20f));
            _pnlKpiContainer.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));

            _kpiTotal = new KpiCard("TOTAL LEADS", "all", Color.FromArgb(100, 116, 139), KpiIconType.Target, "All prospects") { Dock = DockStyle.Fill, Margin = new Padding(0, 0, 6, 0), ClickMode = KpiClickMode.InPlaceFilter };
            _kpiNew = new KpiCard("NEW", "new", Color.FromArgb(14, 165, 233), KpiIconType.Briefcase, "Uncontacted") { Dock = DockStyle.Fill, Margin = new Padding(3, 0, 4, 0), ClickMode = KpiClickMode.InPlaceFilter };
            _kpiContacted = new KpiCard("CONTACTED", "contacted", Color.FromArgb(245, 158, 11), KpiIconType.Clock, "In discussion") { Dock = DockStyle.Fill, Margin = new Padding(4, 0, 4, 0), ClickMode = KpiClickMode.InPlaceFilter };
            _kpiQualified = new KpiCard("QUALIFIED", "qualified", Color.FromArgb(16, 185, 129), KpiIconType.Target, "High interest") { Dock = DockStyle.Fill, Margin = new Padding(4, 0, 4, 0), ClickMode = KpiClickMode.InPlaceFilter };
            _kpiConverted = new KpiCard("CONVERTED", "converted", Color.FromArgb(168, 85, 247), KpiIconType.Users, "Became clients") { Dock = DockStyle.Fill, Margin = new Padding(6, 0, 0, 0), ClickMode = KpiClickMode.InPlaceFilter };

            _filterCoord = new ScreenFilterCoordinator();
            _filterCoord.Register(_kpiTotal, _kpiNew, _kpiContacted, _kpiQualified, _kpiConverted);
            _filterCoord.FilterChanged += (src, key) =>
            {
                SetFilter(key ?? "All");
            };

            _pnlKpiContainer.Controls.Add(_kpiTotal, 0, 0);
            _pnlKpiContainer.Controls.Add(_kpiNew, 1, 0);
            _pnlKpiContainer.Controls.Add(_kpiContacted, 2, 0);
            _pnlKpiContainer.Controls.Add(_kpiQualified, 3, 0);
            _pnlKpiContainer.Controls.Add(_kpiConverted, 4, 0);

            Controls.Add(_pnlKpiContainer);
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
            _pnlEmptyState = UiGridHelper.CreateEmptyStatePanel(
                KpiIconType.Target,
                "No Leads Found",
                "No leads match your search or filter criteria.\nTry clearing your search query or selecting a different stage filter.",
                () =>
                {
                    txtSearch.Clear();
                    SetFilter("All");
                });
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
                if (!string.IsNullOrWhiteSpace(txtSearch.Text) && !string.Equals(_filterStage, "All", StringComparison.OrdinalIgnoreCase))
                {
                    _filterStage = "All";
                }
                _searchDebounceTimer.Stop();
                _searchDebounceTimer.Start();
            };

            if (RbacService.CanExportData)
            {
                _btnExport = new Button
                {
                    Text = "Export CSV",
                    BackColor = Color.White,
                    ForeColor = Theme.Primary,
                    Cursor = Cursors.Hand,
                    FlatStyle = FlatStyle.Flat,
                    Font = new Font("Segoe UI", 9.5f, FontStyle.Bold),
                    Size = new Size(120, 36)
                };
                _btnExport.FlatAppearance.BorderColor = Theme.Primary;
                _btnExport.Click += (_, _) => ExportToCsv();
                UiRadiusHelper.StyleButton(_btnExport, 8);
                Controls.Add(_btnExport);
                _btnExport.BringToFront();
            }

            btnFilterAll.Click += (s, _) => _filterCoord.SetActive(s!, "All");
            btnFilterNew.Click += (s, _) => _filterCoord.SetActive(s!, "New");
            btnFilterContacted.Click += (s, _) => _filterCoord.SetActive(s!, "Contacted");
            btnFilterQualified.Click += (s, _) => _filterCoord.SetActive(s!, "Qualified");
            btnFilterConverted.Click += (s, _) => _filterCoord.SetActive(s!, "Converted");

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
                if (e.RowIndex < 0) return;
                var lead = GetLeadAtRow(e.RowIndex);
                if (lead is not null) ViewLead(lead);
            };
        }

        public async void SetFilter(string stage)
        {
            if (string.Equals(_filterStage, stage, StringComparison.OrdinalIgnoreCase) && !string.Equals(stage, "All", StringComparison.OrdinalIgnoreCase))
            {
                _filterStage = "All";
            }
            else
            {
                _filterStage = stage;
            }

            if (!string.Equals(_filterStage, "All", StringComparison.OrdinalIgnoreCase) && !string.IsNullOrEmpty(txtSearch.Text))
            {
                txtSearch.Clear();
            }

            await RefreshGridAsync(resetPage: true);
        }

        private void UpdateFilterPillStyles(LeadStageCounts counts)
        {
            lblSubtitle.Text = $"{counts.Total} total · {counts.Qualified} qualified";

            _kpiTotal.SetValue(counts.Total);
            _kpiNew.SetValue(counts.New);
            _kpiContacted.SetValue(counts.Contacted);
            _kpiQualified.SetValue(counts.Qualified);
            _kpiConverted.SetValue(counts.Converted);

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
            if (e.ColumnIndex < 0) return;
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

            await RefreshGridAsync(resetPage: false);
        }

        public void RefreshGrid(bool reloadFromDb = true, bool animate = false)
        {
            _ = RefreshGridAsync(resetPage: false);
        }

        public async Task RefreshGridAsync(bool resetPage = false, bool animate = false)
        {
            if (_isLoading) return;
            _isLoading = true;

            bool isFullLoad = _kpiTotal.IsLoading;
            if (isFullLoad)
            {
                _kpiTotal.ShowLoadingSkeleton();
                _kpiNew.ShowLoadingSkeleton();
                _kpiContacted.ShowLoadingSkeleton();
                _kpiQualified.ShowLoadingSkeleton();
                _kpiConverted.ShowLoadingSkeleton();
            }

            _pnlEmptyState.Visible = false;
            _gridSkeleton.ShowSkeleton();

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

                BindCurrentPage();

                _pnlEmptyState.Visible = pagedResult.TotalCount == 0;
                grid.Visible = pagedResult.TotalCount > 0;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[LeadsView] RefreshGridAsync error: {ex.Message}");
                _pnlEmptyState.Visible = true;
                grid.Visible = false;
            }
            finally
            {
                _gridSkeleton.HideSkeleton();
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
                        MessageBox.Show("Agent assigned successfully.", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
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
                MessageBox.Show($"Lead '{form.Result.FullName}' was updated successfully.", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
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
            MessageBox.Show($"Lead '{lead.FullName}' was archived successfully.", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
            RefreshGrid();
        }

        private void BtnAddClick(object? sender, EventArgs e)
        {
            using var form = new LeadInputForm();
            if (form.ShowDialog() == DialogResult.OK && form.Result is not null)
            {
                _controller.Add(form.Result);
                MessageBox.Show($"Lead '{form.Result.FullName}' was created successfully.", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
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

            int rightPadding = UiStyleConstants.PageMarginRight;
            int leftMargin = UiStyleConstants.PageMarginLeft;
            int totalWidth = ClientSize.Width;

            // 1. Page Title & Subtitle
            lblTitle.Location = new Point(leftMargin, UiStyleConstants.PageMarginTop);
            lblSubtitle.Location = new Point(leftMargin + 2, lblTitle.Bottom + 4);

            // 2. KPI Row
            _pnlKpiContainer.Location = new Point(leftMargin, lblSubtitle.Bottom + 12);
            _pnlKpiContainer.Size = new Size(Math.Max(100, totalWidth - leftMargin - rightPadding), UiStyleConstants.KpiRowHeight);

            // 3. Toolbar Row (All controls aligned at y)
            int y = _pnlKpiContainer.Bottom + 14;

            int rightEdge = totalWidth - rightPadding;
            if (btnAdd.Visible)
            {
                btnAdd.Top = y;
                btnAdd.Height = UiStyleConstants.ToolbarRowHeight;
                btnAdd.Left = rightEdge - btnAdd.Width;
                rightEdge = btnAdd.Left - 10;
            }
            if (_btnExport != null && _btnExport.Visible)
            {
                _btnExport.Top = y;
                _btnExport.Height = UiStyleConstants.ToolbarRowHeight;
                _btnExport.Left = rightEdge - _btnExport.Width;
                rightEdge = _btnExport.Left - 10;
            }

            var pills = new[] { btnFilterConverted, btnFilterQualified, btnFilterContacted, btnFilterNew, btnFilterAll };
            int filterRight = rightEdge;
            int totalFilterWidth = 0;
            foreach (var p in pills) totalFilterWidth += p.Width + 6;

            int availableForSearch = filterRight - leftMargin - totalFilterWidth - 16;

            if (availableForSearch >= 180)
            {
                // Single row
                foreach (var p in pills)
                {
                    p.Top = y;
                    p.Height = UiStyleConstants.ToolbarRowHeight;
                    p.Left = filterRight - p.Width;
                    filterRight = p.Left - 6;
                }

                txtSearch.Top = y;
                txtSearch.Left = leftMargin;
                txtSearch.Height = UiStyleConstants.ToolbarRowHeight;
                txtSearch.Width = Math.Min(UiStyleConstants.SearchBoxWidth, availableForSearch);

                int cardTop = y + UiStyleConstants.ToolbarRowHeight + 14;
                pnlCard.Top = cardTop;
                pnlCard.Height = Math.Max(100, ClientSize.Height - cardTop - UiStyleConstants.PageMarginBottom);
            }
            else
            {
                // Two rows: search on row 1, filter pills wrapped to row 2
                txtSearch.Top = y;
                txtSearch.Left = leftMargin;
                txtSearch.Height = UiStyleConstants.ToolbarRowHeight;
                txtSearch.Width = Math.Max(180, rightEdge - leftMargin);

                int pillY = y + UiStyleConstants.ToolbarRowHeight + 10;
                int filterX = leftMargin;
                var forwardPills = new[] { btnFilterAll, btnFilterNew, btnFilterContacted, btnFilterQualified, btnFilterConverted };
                foreach (var p in forwardPills)
                {
                    p.Top = pillY;
                    p.Height = UiStyleConstants.ToolbarRowHeight;
                    p.Left = filterX;
                    filterX += p.Width + 6;
                }

                int cardTop = pillY + UiStyleConstants.ToolbarRowHeight + 14;
                pnlCard.Top = cardTop;
                pnlCard.Height = Math.Max(100, ClientSize.Height - cardTop - 20);
            }

            pnlCard.Left = leftMargin;
            pnlCard.Width = Math.Max(100, totalWidth - leftMargin - rightPadding);
        }
    }
}
