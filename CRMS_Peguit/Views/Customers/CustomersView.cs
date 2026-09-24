using CRMS_Peguit.domain.entities;
using CRMS_Peguit.winforms.Auth;
using CRMS_Peguit.winforms.Controllers;
using CRMS_Peguit.winforms.Controls;
using CRMS_Peguit.winforms.Models.Services;
using CRMS_Peguit.winforms.Services;
using CRMS_Peguit.winforms.Views.Shared;

namespace CRMS_Peguit.winforms.Views.Customers
{
    public partial class CustomersView : UserControl
    {
        private readonly CustomerController _controller;
        private string _filterStatus = "All";
        private Button? _btnExport;
        private Panel _pnlEmptyState = null!;
        private PaginationControl _pagination = null!;
        private List<Customer> _currentPageCustomers = new();
        private Dictionary<int, string> _agentDict = new();
        private readonly System.Windows.Forms.Timer _searchDebounceTimer;
        private bool _isLoading = false;

        public CustomersView()
        {
            InitializeComponent();
            _controller = new CustomerController();

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
            UpdateFilterPillStyles();
            _ = RefreshGridAsync(resetPage: true);

            this.Load += (_, _) => LayoutToolbar();
            this.Resize += (_, _) => LayoutToolbar();
        }

        private void InitPagination()
        {
            _pagination = new PaginationControl();
            _pagination.SetItemLabel("customers");
            _pagination.PageChanged += async (_, _) => await RefreshGridAsync(resetPage: false);
            _pagination.PageSizeChanged += async (_, _) => await RefreshGridAsync(resetPage: true);
            pnlCard.Controls.Add(_pagination);
            _pagination.BringToFront();
        }

        private void InitEmptyState()
        {
            _pnlEmptyState = UiGridHelper.CreateEmptyStatePanel(
                KpiIconType.Users,
                "No Customers Found",
                "No customers match your search or filter criteria.\nTry clearing your search query or selecting a different status filter.",
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

            btnFilterAll.Width = 100;
            btnFilterActive.Width = 120;
            btnFilterFollowUp.Width = 120;
            btnFilterInactive.Width = 120;

            UiRadiusHelper.ApplyPillShape(btnFilterAll);
            UiRadiusHelper.ApplyPillShape(btnFilterActive);
            UiRadiusHelper.ApplyPillShape(btnFilterFollowUp);
            UiRadiusHelper.ApplyPillShape(btnFilterInactive);
        }

        private void BindEvents()
        {
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
                    Size = new Size(120, 36)
                };
                _btnExport.FlatAppearance.BorderColor = Theme.Primary;
                _btnExport.Click += (_, _) => ExportToCsv();
                UiRadiusHelper.StyleButton(_btnExport, 8);
                Controls.Add(_btnExport);
                _btnExport.BringToFront();
            }

            // Case 1: InPlaceFilter mode on all KPI cards
            kpiTotal.ClickMode = KpiClickMode.InPlaceFilter;
            kpiActive.ClickMode = KpiClickMode.InPlaceFilter;
            kpiFollowUp.ClickMode = KpiClickMode.InPlaceFilter;
            kpiInactive.ClickMode = KpiClickMode.InPlaceFilter;

            kpiTotal.Click += (_, _) => SetFilter("All");
            kpiActive.Click += (_, _) => SetFilter("Active");
            kpiFollowUp.Click += (_, _) => SetFilter("Follow Up");
            kpiInactive.Click += (_, _) => SetFilter("Inactive");

            btnFilterAll.Click += (_, _) => SetFilter("All");
            btnFilterActive.Click += (_, _) => SetFilter("Active");
            btnFilterFollowUp.Click += (_, _) => SetFilter("Follow Up");
            btnFilterInactive.Click += (_, _) => SetFilter("Inactive");

            // Modern Grid Styling & Search Padding
            UiGridHelper.ApplyModernGridStyle(grid, 52);
            UiRadiusHelper.SetPadding(txtSearch, 10, 10);

            grid.CellPainting += Grid_CellPainting;
            grid.CellContentClick += GridCellContentClick;
            grid.CellDoubleClick += (_, e) =>
            {
                if (e.RowIndex < 0) return;
                var customer = GetCustomerAtRow(e.RowIndex);
                if (customer is not null) ViewCustomer(customer);
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

            UpdateFilterPillStyles();
            await RefreshGridAsync(resetPage: true);
        }

        private void UpdateFilterPillStyles()
        {
            var pills = new[]
            {
                (btnFilterAll, "All"),
                (btnFilterActive, "Active"),
                (btnFilterFollowUp, "Follow Up"),
                (btnFilterInactive, "Inactive")
            };

            foreach (var (btn, name) in pills)
            {
                bool isSelected = string.Equals(_filterStatus, name, StringComparison.OrdinalIgnoreCase);
                UiRadiusHelper.StyleFilterPill(btn, isSelected);
            }

            kpiTotal.SetSelected(string.Equals(_filterStatus, "All", StringComparison.OrdinalIgnoreCase));
            kpiActive.SetSelected(string.Equals(_filterStatus, "Active", StringComparison.OrdinalIgnoreCase));
            kpiFollowUp.SetSelected(string.Equals(_filterStatus, "Follow Up", StringComparison.OrdinalIgnoreCase));
            kpiInactive.SetSelected(string.Equals(_filterStatus, "Inactive", StringComparison.OrdinalIgnoreCase));
        }

        public void RefreshGrid(bool reloadFromDb = true)
        {
            _ = RefreshGridAsync(resetPage: false);
        }

        public async Task RefreshGridAsync(bool resetPage = false)
        {
            if (_isLoading) return;
            _isLoading = true;

            try
            {
                int page = resetPage ? 1 : _pagination.CurrentPage;
                int pageSize = _pagination.PageSize;

                var pagedResult = await _controller.GetPagedAsync(page, pageSize, txtSearch.Text, _filterStatus);
                _currentPageCustomers = pagedResult.Items;

                var kpis = await _controller.GetKpiCountsAsync();
                lblSubtitle.Text = $"{kpis.Total:N0} total · {kpis.Active:N0} active";

                kpiTotal.SetValue(kpis.Total);
                kpiTotal.SetSubtitle("All registered");
                kpiActive.SetValue(kpis.Active);
                kpiActive.SetSubtitle("Engaged clients");
                kpiFollowUp.SetValue(kpis.FollowUp);
                kpiFollowUp.SetSubtitle("Pending action");
                kpiInactive.SetValue(kpis.Inactive);
                kpiInactive.SetSubtitle("Archived / cold");

                if (_agentDict.Count == 0)
                {
                    _agentDict = _controller.GetAgentDictionary();
                }

                _pagination.UpdatePagination(pagedResult.TotalCount, pagedResult.PageNumber, pagedResult.PageSize);
                BindCurrentPage();
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

            var pageItems = _currentPageCustomers
                .Select(c => new
                {
                    c.CustomerId,
                    Name = c.FullName,
                    Company = string.IsNullOrWhiteSpace(c.Type) ? "Client" : char.ToUpper(c.Type[0]) + c.Type.Substring(1).ToLower(),
                    Phone = string.IsNullOrWhiteSpace(c.Phone) ? "-" : c.Phone,
                    Email = string.IsNullOrWhiteSpace(c.Email) ? "-" : c.Email,
                    AssignedTo = (c.AssignedAgentId.HasValue && _agentDict.TryGetValue(c.AssignedAgentId.Value, out var aName)) ? aName : "Unassigned",
                    Status = c.Status.ToUpper(),
                    LastContacted = c.CreatedAt.ToString("MMM dd, yyyy")
                })
                .ToList();

            grid.DataSource = pageItems;

            var idCol = grid.Columns["CustomerId"];
            if (idCol is not null) idCol.Visible = false;

            grid.ShowCellToolTips = true;

            if (grid.Columns["Name"] is DataGridViewColumn nameCol)
            {
                nameCol.HeaderText = "NAME";
                nameCol.FillWeight = 160;
                nameCol.MinimumWidth = 140;
            }
            if (grid.Columns["Company"] is DataGridViewColumn compCol)
            {
                compCol.HeaderText = "COMPANY";
                compCol.FillWeight = 90;
                compCol.MinimumWidth = 80;
            }
            if (grid.Columns["Phone"] is DataGridViewColumn phoneCol)
            {
                phoneCol.HeaderText = "PHONE";
                phoneCol.FillWeight = 100;
                phoneCol.MinimumWidth = 90;
            }
            if (grid.Columns["Email"] is DataGridViewColumn emailCol)
            {
                emailCol.HeaderText = "EMAIL";
                emailCol.FillWeight = 140;
                emailCol.MinimumWidth = 120;
            }
            if (grid.Columns["AssignedTo"] is DataGridViewColumn assignCol)
            {
                assignCol.HeaderText = "ASSIGNED TO";
                assignCol.FillWeight = 110;
                assignCol.MinimumWidth = 100;
            }
            if (grid.Columns["Status"] is DataGridViewColumn statusCol)
            {
                statusCol.HeaderText = "STATUS";
                statusCol.FillWeight = 90;
                statusCol.MinimumWidth = 80;
                statusCol.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleLeft;
                statusCol.HeaderCell.Style.Alignment = DataGridViewContentAlignment.MiddleLeft;
            }
            if (grid.Columns["LastContacted"] is DataGridViewColumn lastCol)
            {
                lastCol.HeaderText = "LAST CONTACT";
                lastCol.FillWeight = 130;
                lastCol.MinimumWidth = 120;
            }

            UiGridHelper.AddActionsColumn(grid, 64);
            UiGridHelper.EnforceTableStandards(grid);
            grid.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
            _pnlEmptyState.Visible = (_currentPageCustomers.Count == 0);
        }

        private void Grid_CellPainting(object? sender, DataGridViewCellPaintingEventArgs e)
        {
            if (e.RowIndex < 0 || e.Graphics is null) return;

            // Minimalist Status Indicator (Left-aligned at 12px, Strictly No Badges/Pills)
            if (grid.Columns[e.ColumnIndex].Name == "Status" && e.Value != null)
            {
                string status = e.Value.ToString() ?? "";
                UiGridHelper.PaintStatusIndicator(grid, e, status, center: false);
            }
            // Custom render Name with circular initials badge at exact 12px inset
            else if (grid.Columns[e.ColumnIndex].Name == "Name" && e.Value != null)
            {
                string name = e.Value.ToString() ?? "";
                string initials = GetInitials(name);
                UiGridHelper.PaintAvatarCell(grid, e, name, initials, Color.FromArgb(71, 118, 153), Color.White);
            }
            // Custom render Email with uniform 12px inset
            else if (grid.Columns[e.ColumnIndex].Name == "Email" && e.Value != null)
            {
                string email = e.Value.ToString() ?? "";
                using var font = new Font("Segoe UI", 9.5f);
                UiGridHelper.PaintTextCell(grid, e, email, font, Theme.Primary,
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

        private static bool ContainsText(string? value, string search) =>
            !string.IsNullOrWhiteSpace(value) &&
            value.Contains(search, StringComparison.OrdinalIgnoreCase);

        private void GridCellContentClick(object? sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex < 0 || e.ColumnIndex < 0) return;
            if (grid.Columns[e.ColumnIndex] is not ActionsColumn) return;

            grid.Rows[e.RowIndex].Selected = true;
            grid.CurrentCell = grid.Rows[e.RowIndex].Cells[e.ColumnIndex];

            var customer = GetCustomerAtRow(e.RowIndex);
            if (customer is null) return;

            var menu = new ContextMenuStrip
            {
                BackColor = Theme.Surface,
                ForeColor = Theme.TextPrimary,
                Font = new Font("Segoe UI", 10)
            };

            var viewItem = new ToolStripMenuItem("View");
            viewItem.Click += (_, _) => ViewCustomer(customer);
            menu.Items.Add(viewItem);

            var messageItem = new ToolStripMenuItem("Message");
            messageItem.Click += (_, _) => MessageCustomer(customer);
            messageItem.Enabled = ContactEmailService.IsValidEmail(customer.Email);
            menu.Items.Add(messageItem);

            if (RbacService.CanEditRecord(customer.AssignedAgentId, customer.CreatedByUserId, customer.AssignmentStatus))
            {
                var editItem = new ToolStripMenuItem("Edit");
                editItem.Click += (_, _) => EditCustomer(customer);
                menu.Items.Add(editItem);
            }

            if (RbacService.CanAssignRecords)
            {
                var assignItem = new ToolStripMenuItem("Assign Agent");
                assignItem.Click += (_, _) =>
                {
                    using var dlg = new AssignAgentDialog(customer.FullName, _controller.GetAgents(), customer.AssignedAgentId);
                    if (dlg.ShowDialog() == DialogResult.OK)
                    {
                        _controller.AssignAgent(customer, dlg.SelectedAgentId, dlg.ApproveNow, dlg.ReviewNotes);
                        RefreshGrid();
                    }
                };
                menu.Items.Add(assignItem);
            }

            if (RbacService.CanApproveAssignments &&
                string.Equals(customer.AssignmentStatus, "pending_review", StringComparison.OrdinalIgnoreCase))
            {
                var approveItem = new ToolStripMenuItem("Approve Assignment");
                approveItem.Click += (_, _) => ApproveCustomerAssignment(customer);
                menu.Items.Add(approveItem);
            }

            if (RbacService.CanArchiveRecord(customer.AssignedAgentId, customer.CreatedByUserId))
            {
                var archiveItem = new ToolStripMenuItem("Archive");
                archiveItem.Click += (_, _) => ArchiveCustomer(customer);
                menu.Items.Add(archiveItem);
            }

            menu.Show(grid, grid.PointToClient(Cursor.Position));
        }

        private void MessageCustomer(Customer customer)
        {
            using var form = new CRMS_Peguit.winforms.Views.Shared.EmailMessageForm(customer.FullName, customer.Email);
            if (form.ShowDialog() == DialogResult.OK)
            {
                _controller.LogEmail(customer, form.SentSubject);
                RefreshGrid();
            }
        }

        private void ApproveCustomerAssignment(Customer customer)
        {
            _controller.ApproveAssignment(customer, "Reviewed from Customers module.");
            MessageBox.Show(
                $"Assignment for '{customer.FullName}' has been approved.",
                "Assignment Approved",
                MessageBoxButtons.OK,
                MessageBoxIcon.Information);
            RefreshGrid();
        }

        private Customer? GetCustomerAtRow(int rowIndex)
        {
            if (rowIndex < 0 || rowIndex >= grid.Rows.Count) return null;
            if (grid.Rows[rowIndex].Cells["CustomerId"].Value is int id)
            {
                return _currentPageCustomers.FirstOrDefault(c => c.CustomerId == id) ?? _controller.GetById(id);
            }
            return null;
        }

        private void BtnAddClick(object? sender, EventArgs e)
        {
            using var form = new CustomerInputForm();
            if (form.ShowDialog() == DialogResult.OK && form.Result is not null)
            {
                _controller.Add(form.Result);
                RefreshKpis();
                RefreshGrid();
            }
        }

        private void ViewCustomer(Customer customer)
        {
            using var form = new CustomerDetailForm(customer, _controller);
            form.ShowDialog();
            RefreshKpis();
            RefreshGrid();
        }

        private void EditCustomer(Customer customer)
        {
            using var form = new CustomerInputForm(customer);
            if (form.ShowDialog() == DialogResult.OK && form.Result is not null)
            {
                _controller.Update(form.Result);
                RefreshKpis();
                RefreshGrid();
            }
        }

        private void RefreshKpis()
        {
            // Dynamic totals are now displayed in lblSubtitle
        }

        private void ArchiveCustomer(Customer customer)
        {
            var confirm = MessageBox.Show(
                $"Are you sure you want to archive '{customer.FullName}'?",
                "Confirm Archive",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Warning);

            if (confirm == DialogResult.Yes)
            {
                _controller.SoftDelete(customer);
                RefreshGrid();
            }
        }

        private void ExportToCsv()
        {
            using var sfd = new SaveFileDialog
            {
                Filter = "CSV File (*.csv)|*.csv",
                FileName = $"Customers_Export_{DateTime.Now:yyyyMMdd_HHmm}.csv"
            };
            if (sfd.ShowDialog() == DialogResult.OK)
            {
                var customers = _controller.GetAll().ToList();
                var sb = new System.Text.StringBuilder();
                sb.AppendLine("CustomerId,FullName,Type,Phone,Email,Status,AssignedAgent,CreatedAt");
                foreach (var c in customers)
                {
                    sb.AppendLine($"\"{c.CustomerId}\",\"{c.FullName}\",\"{c.Type}\",\"{c.Phone}\",\"{c.Email}\",\"{c.Status}\",\"{_controller.GetAssignedAgentName(c.AssignedAgentId)}\",\"{c.CreatedAt:yyyy-MM-dd}\"");
                }
                System.IO.File.WriteAllText(sfd.FileName, sb.ToString());
                MessageBox.Show("Customers exported successfully.", "Export Complete", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
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
            pnlKpiContainer.Location = new Point(leftMargin, lblSubtitle.Bottom + 12);
            pnlKpiContainer.Size = new Size(Math.Max(100, totalWidth - leftMargin - rightPadding), UiStyleConstants.KpiRowHeight);

            // 3. Toolbar Row (All controls aligned at y)
            int y = pnlKpiContainer.Bottom + 14;

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

            var pills = new[] { btnFilterInactive, btnFilterFollowUp, btnFilterActive, btnFilterAll };
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
                var forwardPills = new[] { btnFilterAll, btnFilterActive, btnFilterFollowUp, btnFilterInactive };
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

        private void lblTitle_Click(object sender, EventArgs e)
        {

        }
    }
}
