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
        private ScreenFilterCoordinator _filterCoord = null!;
        private Button? _btnExport;
        private Panel _pnlEmptyState = null!;
        private PaginationControl _pagination = null!;
        private List<Customer> _currentPageCustomers = new();
        private Dictionary<int, string> _agentDict = new();
        private readonly System.Windows.Forms.Timer _searchDebounceTimer;
        private bool _isLoading = false;
        private GridSkeletonOverlay _gridSkeleton = null!;

        public CustomersView()
        {
            InitializeComponent();
            _controller = new CustomerController();

            _gridSkeleton = GridSkeletonOverlay.CreateForGrid(grid);

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

            kpiTotal.ShowLoadingSkeleton();
            kpiActive.ShowLoadingSkeleton();
            kpiFollowUp.ShowLoadingSkeleton();
            kpiInactive.ShowLoadingSkeleton();
            _gridSkeleton.ShowSkeleton();

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
            _pagination.SendToBack();
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
                    Text = "📄 Export PDF",
                    BackColor = Color.White,
                    ForeColor = Theme.Primary,
                    Cursor = Cursors.Hand,
                    FlatStyle = FlatStyle.Flat,
                    Font = new Font("Segoe UI", 9.5f, FontStyle.Bold),
                    Size = new Size(120, 36)
                };
                _btnExport.FlatAppearance.BorderColor = Theme.Primary;
                _btnExport.Click += (_, _) => ExportToPdf();
                UiRadiusHelper.StyleButton(_btnExport, 8);
                Controls.Add(_btnExport);
                _btnExport.BringToFront();
            }

            // Case 1: InPlaceFilter mode on all KPI cards via ScreenFilterCoordinator
            _filterCoord = new ScreenFilterCoordinator();
            _filterCoord.Register(kpiTotal, kpiActive, kpiFollowUp, kpiInactive);
            
            _filterCoord.FilterChanged += (src, key) =>
            {
                SetFilter(key ?? "All");
            };

            btnFilterAll.Click += (s, _) => { _filterCoord.SetActive(s!, "All"); };
            btnFilterActive.Click += (s, _) => { _filterCoord.SetActive(s!, "Active"); };
            btnFilterFollowUp.Click += (s, _) => { _filterCoord.SetActive(s!, "Follow Up"); };
            btnFilterInactive.Click += (s, _) => { _filterCoord.SetActive(s!, "Inactive"); };

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
        }

        public void RefreshGrid(bool reloadFromDb = true)
        {
            _ = RefreshGridAsync(resetPage: false);
        }

        public async Task RefreshGridAsync(bool resetPage = false)
        {
            if (_isLoading) return;
            _isLoading = true;

            bool isFullLoad = _agentDict.Count == 0 || kpiTotal.IsLoading;
            if (isFullLoad)
            {
                kpiTotal.ShowLoadingSkeleton();
                kpiActive.ShowLoadingSkeleton();
                kpiFollowUp.ShowLoadingSkeleton();
                kpiInactive.ShowLoadingSkeleton();
            }

            _pnlEmptyState.Visible = false;
            _gridSkeleton.ShowSkeleton();

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
                    _agentDict = await System.Threading.Tasks.Task.Run(() => _controller.GetAgentDictionary());
                }

                _pagination.UpdatePagination(pagedResult.TotalCount, pagedResult.PageNumber, pagedResult.PageSize);
                BindCurrentPage();

                _pnlEmptyState.Visible = pagedResult.TotalCount == 0;
                grid.Visible = pagedResult.TotalCount > 0;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[CustomersView] RefreshGridAsync error: {ex.Message}");
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
                        MessageBox.Show("Agent assigned successfully.", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
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
                MessageBox.Show($"Customer '{form.Result.FullName}' was added successfully.", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
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
                MessageBox.Show($"Customer '{form.Result.FullName}' was updated successfully.", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
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
                MessageBox.Show($"Customer '{customer.FullName}' was archived successfully.", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
                RefreshGrid();
            }
        }

        private void ExportToPdf()
        {
            using var sfd = new SaveFileDialog
            {
                Filter = "PDF Document (*.pdf)|*.pdf",
                FileName = $"Customers_Export_{DateTime.Now:yyyyMMdd_HHmm}.pdf"
            };
            if (sfd.ShowDialog() == DialogResult.OK)
            {
                var customers = _controller.GetAll().ToList();
                string[] headers = new[] { "ID", "Full Name", "Type", "Phone", "Email", "Status", "Assigned Agent", "Created At" };
                var rows = customers.Select(c => new string[]
                {
                    c.CustomerId.ToString(),
                    c.FullName,
                    c.Type,
                    c.Phone,
                    c.Email,
                    c.Status,
                    _controller.GetAssignedAgentName(c.AssignedAgentId),
                    c.CreatedAt.ToString("yyyy-MM-dd")
                }).ToList();

                var kpis = new List<(string Title, string Value, string ColorHex)>
                {
                    ("Total Customers", customers.Count.ToString(), "#25679C"),
                    ("Active", customers.Count(x => string.Equals(x.Status, "Active", StringComparison.OrdinalIgnoreCase)).ToString(), "#059669"),
                    ("Follow Up", customers.Count(x => string.Equals(x.Status, "Follow-Up", StringComparison.OrdinalIgnoreCase)).ToString(), "#D97706"),
                    ("Inactive", customers.Count(x => string.Equals(x.Status, "Inactive", StringComparison.OrdinalIgnoreCase)).ToString(), "#64748B")
                };

                if (CRMS_Peguit.winforms.Services.PdfExportHelper.TryExportTable("Customers Directory & Status Roster", headers, rows, sfd.FileName, out string? error, activeFilter: _filterStatus, kpis: kpis))
                {
                    MessageBox.Show("Customers exported to PDF successfully.", "Export Complete", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
                else
                {
                    MessageBox.Show(error ?? "Export failed.", "Export Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }

private void LayoutToolbar()
        {
            ResponsiveLayout.ListPage(this, lblTitle, lblSubtitle, pnlKpiContainer, txtSearch,
                new Control[] { btnFilterAll, btnFilterActive, btnFilterFollowUp, btnFilterInactive }, new Control?[] { _btnExport, btnAdd }, pnlCard);
        }

        private void lblTitle_Click(object sender, EventArgs e)
        {

        }
    }
}
