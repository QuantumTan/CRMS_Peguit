using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Linq;
using System.Windows.Forms;
using CRMS_Peguit.domain.entities;
using CRMS_Peguit.winforms.Auth;
using CRMS_Peguit.winforms.Controllers;
using CRMS_Peguit.winforms.Controls;
using CRMS_Peguit.winforms.Models.Services;
using CRMS_Peguit.winforms.Services;
using CRMS_Peguit.winforms.Views.Shared;

namespace CRMS_Peguit.winforms.Views.SupportTickets
{
    public partial class SupportTicketsView : UserControl
    {
        private readonly SupportTicketController _controller;
        private string _filterStatus = "All";
        private Button? _btnExport;
        private Panel _pnlEmptyState = null!;
        private PaginationControl _pagination = null!;
        private List<SupportTicket> _currentPageTickets = new();
        private Dictionary<int, string> _agentDict = new();
        private readonly System.Windows.Forms.Timer _searchDebounceTimer;
        private bool _isLoading = false;

        public SupportTicketsView()
        {
            InitializeComponent();
            _controller = new SupportTicketController();

            _searchDebounceTimer = new System.Windows.Forms.Timer { Interval = 300 };
            _searchDebounceTimer.Tick += async (_, _) =>
            {
                _searchDebounceTimer.Stop();
                await RefreshGridAsync(resetPage: true);
            };

            InitPagination();
            InitEmptyState();
            ApplyStyling();
            BindEvents();
            _ = RefreshGridAsync(resetPage: true);

            this.Load += (_, _) => LayoutToolbar();
            this.Resize += (_, _) => LayoutToolbar();
        }

        private void InitPagination()
        {
            _pagination = new PaginationControl();
            _pagination.SetItemLabel("tickets");
            _pagination.PageChanged += async (_, _) => await RefreshGridAsync(resetPage: false);
            _pagination.PageSizeChanged += async (_, _) => await RefreshGridAsync(resetPage: true);
            pnlCard.Controls.Add(_pagination);
            _pagination.BringToFront();
        }

        private void InitEmptyState()
        {
            _pnlEmptyState = UiGridHelper.CreateEmptyStatePanel(
                KpiIconType.Ticket,
                "No Support Tickets Found",
                "No tickets match your search or filter criteria.\nTry clearing your search query or selecting a different status filter.",
                () =>
                {
                    txtSearch.Clear();
                    SetFilter("All");
                },
                "Clear Filters & Search");

            pnlCard.Controls.Add(_pnlEmptyState);
            _pnlEmptyState.BringToFront();
        }

        private void ApplyStyling()
        {
            UiRadiusHelper.StyleCard(pnlCard, 12);
            UiRadiusHelper.StyleButton(btnAdd, 8);
            UiRadiusHelper.ApplyPillShape(btnFilterAll);
            UiRadiusHelper.ApplyPillShape(btnFilterOpen);
            UiRadiusHelper.ApplyPillShape(btnFilterInProgress);
            UiRadiusHelper.ApplyPillShape(btnFilterResolved);
            UiRadiusHelper.ApplyPillShape(btnFilterOverdue);
        }

        private void BindEvents()
        {
            // Business Rule: Admin has read-only oversight; Agent & Manager can log tickets
            btnAdd.Visible = RbacService.CanCreateSalesRecord;
            btnAdd.Click += BtnAddClick;
            txtSearch.TextChanged += (_, _) =>
            {
                if (!string.IsNullOrWhiteSpace(txtSearch.Text) && !string.Equals(_filterStatus, "All", StringComparison.OrdinalIgnoreCase))
                {
                    _filterStatus = "All";
                    UpdateFilterPillStyles();
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
                    Size = new Size(130, 36)
                };
                _btnExport.FlatAppearance.BorderColor = Theme.Primary;
                _btnExport.Click += (_, _) => ExportToCsv();
                UiRadiusHelper.StyleButton(_btnExport, 8);
                Controls.Add(_btnExport);
                _btnExport.BringToFront();
            }

            // Case 1: InPlaceFilter mode on all KPI cards
            kpiTotal.ClickMode = KpiClickMode.InPlaceFilter;
            kpiOpen.ClickMode = KpiClickMode.InPlaceFilter;
            kpiInProgress.ClickMode = KpiClickMode.InPlaceFilter;
            kpiOverdue.ClickMode = KpiClickMode.InPlaceFilter;

            // Wire KPI card click-to-filter interaction
            kpiTotal.Click += (_, _) => SetFilter("All");
            kpiOpen.Click += (_, _) => SetFilter("Open");
            kpiInProgress.Click += (_, _) => SetFilter("In Progress");
            kpiOverdue.Click += (_, _) => SetFilter("Overdue");

            // Filter pills
            btnFilterAll.Click += (_, _) => SetFilter("All");
            btnFilterOpen.Click += (_, _) => SetFilter("Open");
            btnFilterInProgress.Click += (_, _) => SetFilter("In Progress");
            btnFilterResolved.Click += (_, _) => SetFilter("Resolved");
            btnFilterOverdue.Click += (_, _) => SetFilter("Overdue");

            // Modern Grid Styling & Search Padding
            UiGridHelper.ApplyModernGridStyle(grid, 52);
            UiRadiusHelper.SetPadding(txtSearch, 10, 10);

            grid.CellPainting += Grid_CellPainting;
            grid.CellContentClick += GridCellContentClick;
            grid.CellDoubleClick += (_, e) =>
            {
                if (e.RowIndex < 0) return;
                var ticket = GetTicketAtRow(e.RowIndex);
                if (ticket is not null) ViewTicket(ticket);
            };
        }

        public async void SetFilter(string filter)
        {
            if (string.Equals(_filterStatus, filter, StringComparison.OrdinalIgnoreCase) && !string.Equals(filter, "All", StringComparison.OrdinalIgnoreCase))
            {
                _filterStatus = "All";
            }
            else
            {
                _filterStatus = filter;
            }

            if (!string.Equals(_filterStatus, "All", StringComparison.OrdinalIgnoreCase) && !string.IsNullOrEmpty(txtSearch.Text))
            {
                txtSearch.Clear();
            }

            await RefreshGridAsync(resetPage: true);
        }

        private void UpdateFilterPillStyles(SupportTicketKpiCounts? kpis = null)
        {
            kpis ??= _controller.GetKpiCounts();

            // Refresh KPI card values and context subtitles
            kpiTotal.SetValue(kpis.Total);
            kpiOpen.SetValue(kpis.Open);
            kpiInProgress.SetValue(kpis.InProgress);
            kpiOverdue.SetValue(kpis.Overdue);

            kpiTotal.SetSubtitle("All registered");
            kpiOpen.SetSubtitle("Awaiting triage");
            kpiInProgress.SetSubtitle("In active resolution");
            kpiOverdue.SetSubtitle(kpis.Overdue > 0 ? "Requires attention" : "All within SLA",
                kpis.Overdue > 0 ? Color.FromArgb(220, 38, 38) : Color.FromArgb(22, 163, 74));

            kpiTotal.SetSelected(string.Equals(_filterStatus, "All", StringComparison.OrdinalIgnoreCase));
            kpiOpen.SetSelected(string.Equals(_filterStatus, "Open", StringComparison.OrdinalIgnoreCase));
            kpiInProgress.SetSelected(string.Equals(_filterStatus, "In Progress", StringComparison.OrdinalIgnoreCase));
            kpiOverdue.SetSelected(string.Equals(_filterStatus, "Overdue", StringComparison.OrdinalIgnoreCase));

            lblSubtitle.Text = $"{kpis.Total} total · {kpis.Open} open · {kpis.InProgress} in progress · {kpis.Overdue} overdue";

            var pills = new[]
            {
                (btnFilterAll, "All"),
                (btnFilterOpen, "Open"),
                (btnFilterInProgress, "In Progress"),
                (btnFilterResolved, "Resolved"),
                (btnFilterOverdue, "Overdue")
            };

            foreach (var (btn, name) in pills)
            {
                bool isSelected = string.Equals(_filterStatus, name, StringComparison.OrdinalIgnoreCase);
                if (isSelected)
                {
                    btn.BackColor = string.Equals(name, "Overdue", StringComparison.OrdinalIgnoreCase)
                        ? Theme.Danger
                        : Theme.Primary;
                    btn.ForeColor = Color.White;
                    btn.Font = new Font("Segoe UI", 9f, FontStyle.Bold);
                }
                else
                {
                    btn.BackColor = Color.White;
                    btn.ForeColor = string.Equals(name, "Overdue", StringComparison.OrdinalIgnoreCase)
                        ? Theme.Danger
                        : Color.FromArgb(71, 85, 105);
                    btn.Font = new Font("Segoe UI", 9f, FontStyle.Regular);
                }
            }
        }

        private async System.Threading.Tasks.Task RefreshGridAsync(bool resetPage = false)
        {
            if (_isLoading) return;
            _isLoading = true;

            try
            {
                int pageNumber = resetPage ? 1 : _pagination.CurrentPage;
                int pageSize = _pagination.PageSize;
                string search = txtSearch.Text.Trim();

                var kpiTask = _controller.GetKpiCountsAsync();
                var pagedTask = _controller.GetPagedAsync(pageNumber, pageSize, search, _filterStatus);
                var agentDictTask = System.Threading.Tasks.Task.Run(() => _controller.GetAgentDictionary());

                await System.Threading.Tasks.Task.WhenAll(kpiTask, pagedTask, agentDictTask);

                var kpis = await kpiTask;
                var pagedResult = await pagedTask;
                _agentDict = await agentDictTask;

                UpdateFilterPillStyles(kpis);

                _currentPageTickets = pagedResult.Items;
                _pagination.UpdatePagination(pagedResult.TotalCount, pagedResult.PageNumber, pagedResult.PageSize);
                BindCurrentPage();

                _pnlEmptyState.Visible = pagedResult.TotalCount == 0;
            }
            finally
            {
                _isLoading = false;
            }
        }

        private void RefreshGrid(bool reloadFromDb = true)
        {
            _ = RefreshGridAsync(resetPage: false);
        }

        private void BindCurrentPage()
        {
            grid.Columns.Clear();
            grid.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.None;

            var now = DateTime.UtcNow;

            var pageItems = _currentPageTickets
                .Select(t => new
                {
                    t.TicketId,
                    TicketNumber = t.TicketNumber,
                    Customer = t.Customer?.FullName ?? "Unknown",
                    Category = t.Category,
                    Priority = t.Priority.ToUpper(),
                    Status = t.Status.ToUpper(),
                    DueDate = t.DueDate.HasValue
                        ? (!string.Equals(t.Status, "Resolved", StringComparison.OrdinalIgnoreCase) && t.DueDate.Value < now
                            ? $"[OVERDUE] {t.DueDate.Value.ToLocalTime():MMM dd, yyyy}"
                            : t.DueDate.Value.ToLocalTime().ToString("MMM dd, yyyy"))
                        : "-",
                    AssignedTo = (t.AssignedToUserId.HasValue && _agentDict.TryGetValue(t.AssignedToUserId.Value, out var aName))
                        ? aName
                        : (t.AssignedToUser?.FullName ?? "Unassigned"),
                    Logged = t.CreatedAt.ToLocalTime().ToString("MMM dd, yyyy")
                })
                .ToList();

            grid.DataSource = pageItems;

            var idCol = grid.Columns["TicketId"];
            if (idCol is not null) idCol.Visible = false;

            grid.ShowCellToolTips = true;

            if (grid.Columns["TicketNumber"] is DataGridViewColumn numCol)
            {
                numCol.HeaderText = "TICKET #";
                numCol.FillWeight = 95;
                numCol.MinimumWidth = 85;
            }
            if (grid.Columns["Customer"] is DataGridViewColumn custCol)
            {
                custCol.HeaderText = "CUSTOMER";
                custCol.FillWeight = 150;
                custCol.MinimumWidth = 130;
            }
            if (grid.Columns["Category"] is DataGridViewColumn catCol)
            {
                catCol.HeaderText = "CATEGORY";
                catCol.FillWeight = 110;
                catCol.MinimumWidth = 95;
            }
            if (grid.Columns["Priority"] is DataGridViewColumn priCol)
            {
                priCol.HeaderText = "PRIORITY";
                priCol.FillWeight = 90;
                priCol.MinimumWidth = 80;
                priCol.HeaderCell.Style.Alignment = DataGridViewContentAlignment.MiddleLeft;
                priCol.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleLeft;
            }
            if (grid.Columns["Status"] is DataGridViewColumn statCol)
            {
                statCol.HeaderText = "STATUS";
                statCol.FillWeight = 95;
                statCol.MinimumWidth = 85;
                statCol.HeaderCell.Style.Alignment = DataGridViewContentAlignment.MiddleLeft;
                statCol.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleLeft;
            }
            if (grid.Columns["DueDate"] is DataGridViewColumn dueCol)
            {
                dueCol.HeaderText = "DUE DATE (SLA)";
                dueCol.FillWeight = 115;
                dueCol.MinimumWidth = 100;
            }
            if (grid.Columns["AssignedTo"] is DataGridViewColumn assignCol)
            {
                assignCol.HeaderText = "ASSIGNED TO";
                assignCol.FillWeight = 120;
                assignCol.MinimumWidth = 100;
            }
            if (grid.Columns["Logged"] is DataGridViewColumn logCol)
            {
                logCol.HeaderText = "LOGGED";
                logCol.FillWeight = 95;
                logCol.MinimumWidth = 85;
            }

            UiGridHelper.AddActionsColumn(grid, 64);
            UiGridHelper.EnforceTableStandards(grid);
            grid.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
            _pnlEmptyState.Visible = (_currentPageTickets.Count == 0);
        }

        private void Grid_CellPainting(object? sender, DataGridViewCellPaintingEventArgs e)
        {
            if (e.RowIndex < 0 || e.Graphics is null) return;
            string colName = grid.Columns[e.ColumnIndex].Name;

            // Custom render Ticket Number with uniform 12px inset
            if (colName == "TicketNumber" && e.Value != null)
            {
                string num = e.Value.ToString() ?? "";
                using var font = new Font("Segoe UI", 9.5f, FontStyle.Bold);
                UiGridHelper.PaintTextCell(grid, e, num, font, Theme.PrimaryDark,
                    TextFormatFlags.Left | TextFormatFlags.VerticalCenter, leftPadding: 12);
            }
            // Custom render Status (minimalist 6px dot + text, Left-aligned at 12px)
            else if (colName == "Status" && e.Value != null)
            {
                string status = e.Value.ToString() ?? "";
                UiGridHelper.PaintStatusIndicator(grid, e, status, center: false);
            }
            // Custom render Priority (minimalist 6px dot + text, Left-aligned at 12px)
            else if (colName == "Priority" && e.Value != null)
            {
                string priority = e.Value.ToString() ?? "";
                UiGridHelper.PaintStatusIndicator(grid, e, priority, center: false);
            }
            // Custom render DueDate with red warning highlighting if overdue (uniform 12px inset)
            else if (colName == "DueDate" && e.Value != null)
            {
                string text = e.Value.ToString() ?? "";
                bool isOverdue = text.StartsWith("[OVERDUE]") || text.Contains("OVERDUE");

                Color textColor = isOverdue ? Theme.Danger : Theme.TextPrimary;
                using var font = new Font("Segoe UI", 9f, isOverdue ? FontStyle.Bold : FontStyle.Regular);
                UiGridHelper.PaintTextCell(grid, e, text, font, textColor,
                    TextFormatFlags.Left | TextFormatFlags.VerticalCenter, leftPadding: 12);
            }
            // Custom render Customer with initial badge at exact 12px inset
            else if (colName == "Customer" && e.Value != null)
            {
                string name = e.Value.ToString() ?? "";
                string initials = UiDetailCardHelper.GetInitials(name);
                UiGridHelper.PaintAvatarCell(grid, e, name, initials, Theme.PrimaryLight, Theme.PrimaryDark);
            }
        }

        private static GraphicsPath GetRoundedRectangle(Rectangle bounds, int radius)
        {
            var path = new GraphicsPath();
            int d = radius * 2;
            path.AddArc(bounds.X, bounds.Y, d, d, 180, 90);
            path.AddArc(bounds.Right - d, bounds.Y, d, d, 270, 90);
            path.AddArc(bounds.Right - d, bounds.Bottom - d, d, d, 0, 90);
            path.AddArc(bounds.X, bounds.Bottom - d, d, d, 90, 90);
            path.CloseFigure();
            return path;
        }

        private static bool ContainsText(string? value, string search) =>
            !string.IsNullOrWhiteSpace(value) &&
            value.Contains(search, StringComparison.OrdinalIgnoreCase);

        private void GridCellContentClick(object? sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex < 0 || e.ColumnIndex < 0) return;
            if (grid.Columns[e.ColumnIndex] is not ActionsColumn) return;

            grid.Rows[e.RowIndex].Selected = true;
            grid.CurrentCell = grid.Rows[e.RowIndex].Cells[e.ColumnIndex];

            var ticket = GetTicketAtRow(e.RowIndex);
            if (ticket is null) return;

            var menu = new ContextMenuStrip
            {
                BackColor = Theme.Surface,
                ForeColor = Theme.TextPrimary,
                Font = new Font("Segoe UI", 10)
            };

            var viewItem = new ToolStripMenuItem("View Ticket");
            viewItem.Click += (_, _) => ViewTicket(ticket);
            menu.Items.Add(viewItem);

            // Update Status (Manager or owning Agent)
            bool canEdit = _controller.CanUserEditTicket(ticket);
            bool isResolved = string.Equals(ticket.Status, "Resolved", StringComparison.OrdinalIgnoreCase);

            if (canEdit && !isResolved)
            {
                var statusMenu = new ToolStripMenuItem("Update Status");

                if (string.Equals(ticket.Status, "Open", StringComparison.OrdinalIgnoreCase))
                {
                    var inProgressItem = new ToolStripMenuItem("Mark In Progress");
                    inProgressItem.Click += (_, _) =>
                    {
                        _controller.UpdateStatus(ticket.TicketId, "In Progress");
                        UpdateFilterPillStyles();
                        RefreshGrid();
                    };
                    statusMenu.DropDownItems.Add(inProgressItem);
                }

                var resolveItem = new ToolStripMenuItem("Resolve Ticket");
                resolveItem.Click += (_, _) =>
                {
                    _controller.UpdateStatus(ticket.TicketId, "Resolved");
                    UpdateFilterPillStyles();
                    RefreshGrid();
                };
                statusMenu.DropDownItems.Add(resolveItem);

                menu.Items.Add(statusMenu);
            }

            // Reopen (Explicit action if Resolved)
            if (canEdit && isResolved)
            {
                var reopenItem = new ToolStripMenuItem("Reopen Ticket");
                reopenItem.Click += (_, _) =>
                {
                    _controller.Reopen(ticket.TicketId, "Reopened from ticket list.");
                    UpdateFilterPillStyles();
                    RefreshGrid();
                };
                menu.Items.Add(reopenItem);
            }

            // Assign / Reassign (Manager / Admin ONLY)
            if (RbacService.CanAssignRecords)
            {
                var assignItem = new ToolStripMenuItem("Assign / Reassign Agent");
                assignItem.Click += (_, _) =>
                {
                    using var dlg = new AssignAgentDialog(ticket.TicketNumber, _controller.GetAgents(), ticket.AssignedToUserId);
                    if (dlg.ShowDialog() == DialogResult.OK)
                    {
                        _controller.AssignTo(ticket.TicketId, dlg.SelectedAgentId, dlg.ReviewNotes);
                        UpdateFilterPillStyles();
                        RefreshGrid();
                    }
                };
                menu.Items.Add(assignItem);
            }

            // Add Note / Comment
            var commentItem = new ToolStripMenuItem("Add Internal Note");
            commentItem.Click += (_, _) =>
            {
                PromptAddComment(ticket);
            };
            menu.Items.Add(commentItem);

            // Archive (Manager only)
            if (RbacService.IsManager || RbacService.IsSuperAdmin)
            {
                var archiveItem = new ToolStripMenuItem("Archive");
                archiveItem.Click += (_, _) =>
                {
                    var confirm = MessageBox.Show(
                        $"Are you sure you want to archive ticket {ticket.TicketNumber}?",
                        "Confirm Archive",
                        MessageBoxButtons.YesNo,
                        MessageBoxIcon.Warning);

                    if (confirm == DialogResult.Yes)
                    {
                        _controller.SoftDelete(ticket.TicketId);
                        UpdateFilterPillStyles();
                        RefreshGrid();
                    }
                };
                menu.Items.Add(archiveItem);
            }

            menu.Show(grid, grid.PointToClient(Cursor.Position));
        }

        private void PromptAddComment(SupportTicket ticket)
        {
            using var prompt = new Form
            {
                Text = $"Add Note — {ticket.TicketNumber}",
                Size = new Size(420, 200),
                StartPosition = FormStartPosition.CenterParent,
                FormBorderStyle = FormBorderStyle.FixedDialog,
                MaximizeBox = false,
                MinimizeBox = false,
                BackColor = Theme.Surface
            };

            var lbl = new Label { Text = "Internal note / reply text:", Location = new Point(20, 16), AutoSize = true, Font = new Font("Segoe UI", 9f, FontStyle.Bold) };
            var txt = new TextBox { Location = new Point(20, 38), Width = 360, Height = 60, Multiline = true };
            var btnOk = new Button { Text = "Post Note", Location = new Point(210, 110), Width = 84, Height = 32, DialogResult = DialogResult.OK, BackColor = Theme.Primary, ForeColor = Color.White };
            var btnCan = new Button { Text = "Cancel", Location = new Point(300, 110), Width = 80, Height = 32, DialogResult = DialogResult.Cancel };
            UiRadiusHelper.StyleButton(btnOk, 6);
            UiRadiusHelper.StyleButton(btnCan, 6);

            prompt.Controls.AddRange(new Control[] { lbl, txt, btnOk, btnCan });
            prompt.AcceptButton = btnOk;
            prompt.CancelButton = btnCan;

            if (prompt.ShowDialog(this) == DialogResult.OK && !string.IsNullOrWhiteSpace(txt.Text))
            {
                _controller.AddComment(ticket.TicketId, txt.Text.Trim());
                RefreshGrid();
            }
        }

        private SupportTicket? GetTicketAtRow(int rowIndex)
        {
            if (rowIndex < 0 || rowIndex >= grid.Rows.Count) return null;
            if (grid.Rows[rowIndex].Cells["TicketId"].Value is int id)
            {
                return _currentPageTickets.FirstOrDefault(t => t.TicketId == id) ?? _controller.GetById(id);
            }
            return null;
        }

        private async void BtnAddClick(object? sender, EventArgs e)
        {
            using var form = new SupportTicketInputForm(_controller);
            if (form.ShowDialog() == DialogResult.OK && form.Result is not null)
            {
                _controller.Add(form.Result);
                await RefreshGridAsync(resetPage: true);
            }
        }

        private async void ViewTicket(SupportTicket ticket)
        {
            using var form = new SupportTicketDetailForm(ticket, _controller);
            form.ShowDialog();
            await RefreshGridAsync(resetPage: false);
        }

        private void ExportToCsv()
        {
            using var sfd = new SaveFileDialog
            {
                Filter = "CSV File (*.csv)|*.csv",
                FileName = $"SupportTickets_Export_{DateTime.Now:yyyyMMdd_HHmm}.csv"
            };
            if (sfd.ShowDialog() == DialogResult.OK)
            {
                var tickets = _controller.GetAll();
                var sb = new System.Text.StringBuilder();
                sb.AppendLine("TicketId,TicketNumber,Category,Customer,Priority,Status,DueDate,AssignedTo,CreatedAt");
                foreach (var t in tickets)
                {
                    sb.AppendLine($"\"{t.TicketId}\",\"{t.TicketNumber}\",\"{t.Category}\",\"{t.Customer?.FullName}\",\"{t.Priority}\",\"{t.Status}\",\"{t.DueDate:yyyy-MM-dd}\",\"{_controller.GetAssignedAgentName(t.AssignedToUserId)}\",\"{t.CreatedAt:yyyy-MM-dd}\"");
                }
                System.IO.File.WriteAllText(sfd.FileName, sb.ToString());
                MessageBox.Show("Support tickets exported successfully.", "Export Complete", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }

        private void LayoutToolbar()
        {
            if (this.IsDisposed) return;

            int rightPadding = UiStyleConstants.PageMarginRight;
            int leftMargin = UiStyleConstants.PageMarginLeft;
            int totalWidth = ClientSize.Width;

            // 1. Position and size KPI container explicitly with generous clearance from subtitle
            pnlKpiContainer.Left = leftMargin;
            pnlKpiContainer.Top = Math.Max(90, lblSubtitle.Bottom + 10);
            pnlKpiContainer.Width = Math.Max(100, totalWidth - leftMargin - rightPadding);
            pnlKpiContainer.Height = UiStyleConstants.KpiRowHeight;

            // 2. Position toolbar row (Search box on left, filter pills in center, action buttons rightmost at identical y)
            int y = pnlKpiContainer.Bottom + 16;
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

            var pills = new[] { btnFilterOverdue, btnFilterResolved, btnFilterInProgress, btnFilterOpen, btnFilterAll };
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
                var forwardPills = new[] { btnFilterAll, btnFilterOpen, btnFilterInProgress, btnFilterResolved, btnFilterOverdue };
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
