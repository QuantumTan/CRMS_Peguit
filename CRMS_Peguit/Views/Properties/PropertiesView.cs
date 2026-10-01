using System.Drawing.Drawing2D;
using CRMS_Peguit.domain.entities;
using CRMS_Peguit.winforms.Auth;
using CRMS_Peguit.winforms.Controllers;
using CRMS_Peguit.winforms.Controls;
using CRMS_Peguit.winforms.Models.Services;
using CRMS_Peguit.winforms.Services;
using CRMS_Peguit.winforms.Views.Shared;

namespace CRMS_Peguit.winforms.Views.Properties
{
    public partial class PropertiesView : UserControl
    {
        private readonly PropertyController _controller;
        private string _filterStatus = "All";
        private Button? _btnExport;
        private Panel _pnlEmptyState = null!;
        private PaginationControl _pagination = null!;
        private List<Property> _currentPageProperties = new();
        private Dictionary<int, string> _owners = new();
        private Dictionary<int, string> _agents = new();
        private readonly System.Windows.Forms.Timer _searchDebounceTimer;
        private bool _isLoading = false;
        private GridSkeletonOverlay _gridSkeleton = null!;

        public PropertiesView()
        {
            InitializeComponent();
            _controller = new PropertyController();

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

            _gridSkeleton.ShowSkeleton();
            _ = RefreshGridAsync(resetPage: true);

            this.Load += (_, _) => LayoutToolbar();
            this.Resize += (_, _) => LayoutToolbar();
        }

        private void InitPagination()
        {
            _pagination = new PaginationControl();
            _pagination.SetItemLabel("properties");
            _pagination.PageChanged += async (_, _) => await RefreshGridAsync(resetPage: false);
            _pagination.PageSizeChanged += async (_, _) => await RefreshGridAsync(resetPage: true);
            pnlCard.Controls.Add(_pagination);
            _pagination.SendToBack();
        }

        private void InitEmptyState()
        {
            _pnlEmptyState = UiGridHelper.CreateEmptyStatePanel(
                KpiIconType.Building,
                "No Properties Found",
                "No properties match your search or filter criteria.\nTry clearing your search query or selecting a different status filter.",
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
            UiRadiusHelper.ApplyPillShape(btnFilterAvailable);
            UiRadiusHelper.ApplyPillShape(btnFilterPending);
            UiRadiusHelper.ApplyPillShape(btnFilterSold);
        }

        private void BindEvents()
        {
            btnAdd.Visible = RbacService.CanCreateSalesRecord;
            btnAdd.Click += BtnAddClick;
            txtSearch.TextChanged += (_, _) => RefreshGrid(reloadFromDb: false);

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

            _filterCoord = new ScreenFilterCoordinator();
            _filterCoord.FilterChanged += async (_, key) => 
            {
                if (key != null && cmbFilter != null) cmbFilter.SelectedIndex = -1;
                await ApplyFilter(key ?? "All");
            };

            btnFilterAll.Click += (_, _) => SetFilter("All");
            btnFilterAvailable.Click += (_, _) => SetFilter("Available");
            btnFilterPending.Click += (_, _) => SetFilter("Pending");
            btnFilterSold.Click += (_, _) => SetFilter("Sold");

            // Modern Grid Styling & Search Padding
            UiGridHelper.ApplyModernGridStyle(grid, 52);
            grid.ShowCellErrors = false;
            grid.ShowRowErrors = false;
            UiRadiusHelper.SetPadding(txtSearch, 10, 10);
            txtSearch.TextChanged += (_, _) =>
            {
                _searchDebounceTimer.Stop();
                _searchDebounceTimer.Start();
            };

            grid.CellPainting += Grid_CellPainting;
            grid.CellContentClick += GridCellContentClick;
            grid.CellDoubleClick += (_, e) =>
            {
                if (e.RowIndex < 0) return;
                var property = GetPropertyAtRow(e.RowIndex);
                if (property is not null) ViewProperty(property);
            };
        }

        private ScreenFilterCoordinator _filterCoord = null!;

        private async void SetFilter(string filter)
        {
            if (string.Equals(_filterStatus, filter, StringComparison.OrdinalIgnoreCase) && !string.Equals(filter, "All", StringComparison.OrdinalIgnoreCase))
            {
                _filterCoord.SetActive(this, null);
            }
            else
            {
                _filterCoord.SetActive(this, filter);
            }
        }

        private async Task ApplyFilter(string filter)
        {
            _filterStatus = filter;
            UpdateFilterPillStyles();
            await RefreshGridAsync(resetPage: true);
        }

        private void UpdateFilterPillStyles()
        {
            var pills = new[]
            {
                (btnFilterAll, "All"),
                (btnFilterAvailable, "Available"),
                (btnFilterPending, "Pending"),
                (btnFilterSold, "Sold")
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

            _pnlEmptyState.Visible = false;
            _gridSkeleton.ShowSkeleton();

            try
            {
                int page = resetPage ? 1 : _pagination.CurrentPage;
                int pageSize = _pagination.PageSize;

                var counts = await _controller.GetPropertyCountsAsync();
                lblSubtitle.Text = $"{counts.Total:N0} total · {counts.Available:N0} available";

                var pagedResult = await _controller.GetPagedAsync(page, pageSize, txtSearch.Text, _filterStatus);
                _currentPageProperties = pagedResult.Items;

                if (_owners.Count == 0)
                {
                    _owners = await System.Threading.Tasks.Task.Run(() => _controller.GetOwnerCustomers().ToDictionary(x => x.CustomerId, x => x.FullName));
                }
                if (_agents.Count == 0)
                {
                    _agents = await System.Threading.Tasks.Task.Run(() => _controller.GetAgents().ToDictionary(x => x.UserId, x => x.FullName));
                }

                _pagination.UpdatePagination(pagedResult.TotalCount, pagedResult.PageNumber, pagedResult.PageSize);
                BindCurrentPage();

                _pnlEmptyState.Visible = pagedResult.TotalCount == 0;
                grid.Visible = pagedResult.TotalCount > 0;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[PropertiesView] RefreshGridAsync error: {ex.Message}");
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

            var pageItems = _currentPageProperties
                .Select(property => new
                {
                    property.PropertyId,
                    Address = property.Address,
                    Type = string.IsNullOrWhiteSpace(property.PropertyType) ? "-" : property.PropertyType,
                    Price = $"₱{property.Price:N2}",
                    Status = property.Status.ToUpper(),
                    Assignment = string.IsNullOrWhiteSpace(property.AssignmentStatus) ? "-" : property.AssignmentStatus,
                    Owner = property.OwnerCustomer != null ? property.OwnerCustomer.FullName : GetName(_owners, property.OwnerCustomerId),
                    ListedBy = property.ListedByAgent != null ? property.ListedByAgent.FullName : GetName(_agents, property.ListedByAgentId)
                })
                .ToList();

            grid.DataSource = pageItems;

            var propertyIdColumn = grid.Columns["PropertyId"];
            if (propertyIdColumn is not null)
            {
                propertyIdColumn.Visible = false;
            }

            grid.ShowCellToolTips = true;

            if (grid.Columns["Address"] is DataGridViewColumn addressCol)
            {
                addressCol.HeaderText = "ADDRESS";
                addressCol.FillWeight = 180;
                addressCol.MinimumWidth = 150;
            }

            if (grid.Columns["Type"] is DataGridViewColumn typeCol)
            {
                typeCol.HeaderText = "TYPE";
                typeCol.FillWeight = 90;
                typeCol.MinimumWidth = 80;
            }

            if (grid.Columns["Price"] is DataGridViewColumn priceCol)
            {
                priceCol.HeaderText = "PRICE";
                priceCol.FillWeight = 100;
                priceCol.MinimumWidth = 90;
                priceCol.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight;
                priceCol.HeaderCell.Style.Alignment = DataGridViewContentAlignment.MiddleRight;
            }

            if (grid.Columns["Status"] is DataGridViewColumn statusCol)
            {
                statusCol.HeaderText = "STATUS";
                statusCol.FillWeight = 90;
                statusCol.MinimumWidth = 80;
                statusCol.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleLeft;
                statusCol.HeaderCell.Style.Alignment = DataGridViewContentAlignment.MiddleLeft;
            }

            if (grid.Columns["Assignment"] is DataGridViewColumn assignCol)
            {
                assignCol.HeaderText = "ASSIGNMENT";
                assignCol.FillWeight = 100;
                assignCol.MinimumWidth = 90;
            }

            if (grid.Columns["Owner"] is DataGridViewColumn ownerCol)
            {
                ownerCol.HeaderText = "OWNER";
                ownerCol.FillWeight = 120;
                ownerCol.MinimumWidth = 100;
            }

            if (grid.Columns["ListedBy"] is DataGridViewColumn listedByCol)
            {
                listedByCol.HeaderText = "LISTED BY";
                listedByCol.FillWeight = 120;
                listedByCol.MinimumWidth = 100;
            }

            UiGridHelper.AddActionsColumn(grid, 64);
            UiGridHelper.EnforceTableStandards(grid);
            grid.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
            _pnlEmptyState.Visible = (_currentPageProperties.Count == 0);
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
            // Style address with primary bold text at uniform 12px inset
            else if (grid.Columns[e.ColumnIndex].Name == "Address" && e.Value != null)
            {
                string address = e.Value.ToString() ?? "";
                using var font = new Font("Segoe UI", 9.5f, FontStyle.Bold);
                UiGridHelper.PaintTextCell(grid, e, address, font, Theme.TextPrimary,
                    TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis, leftPadding: 12);
            }
            // Clean right-aligned Price column painting at uniform right margin
            else if (grid.Columns[e.ColumnIndex].Name == "Price" && e.Value != null)
            {
                string priceText = e.Value.ToString() ?? "";
                priceText = priceText.TrimStart('!', '|', ' ');
                using var font = new Font("Segoe UI", 9.5f, FontStyle.Bold);
                UiGridHelper.PaintTextCell(grid, e, priceText, font, Theme.TextPrimary,
                    TextFormatFlags.Right | TextFormatFlags.VerticalCenter, leftPadding: 8, rightPadding: 12);
            }
        }

        private static GraphicsPath GetRoundedRectangle(Rectangle bounds, int radius)
        {
            var path = new GraphicsPath();
            int diameter = radius * 2;
            var arc = new Rectangle(bounds.Location, new Size(diameter, diameter));

            path.AddArc(arc, 180, 90);
            arc.X = bounds.Right - diameter;
            path.AddArc(arc, 270, 90);
            arc.Y = bounds.Bottom - diameter;
            path.AddArc(arc, 0, 90);
            arc.X = bounds.Left;
            path.AddArc(arc, 90, 90);
            path.CloseFigure();
            return path;
        }

        private Property? GetPropertyAtRow(int rowIndex)
        {
            if (rowIndex < 0 || rowIndex >= grid.Rows.Count) return null;
            object? idValue = grid.Rows[rowIndex].Cells["PropertyId"].Value;
            if (idValue is null || !int.TryParse(idValue.ToString(), out int propertyId))
            {
                return null;
            }

            return _currentPageProperties.FirstOrDefault(property => property.PropertyId == propertyId) ?? _controller.GetById(propertyId);
        }

        private Property? GetSelectedProperty()
        {
            if (grid.CurrentRow is null) return null;
            return GetPropertyAtRow(grid.CurrentRow.Index);
        }

        private void GridCellContentClick(object? sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex < 0 || e.ColumnIndex < 0) return;
            if (grid.Columns[e.ColumnIndex] is not ActionsColumn) return;

            grid.Rows[e.RowIndex].Selected = true;
            grid.CurrentCell = grid.Rows[e.RowIndex].Cells[e.ColumnIndex];

            Property? property = GetSelectedProperty();
            if (property is null) return;

            var menu = new ContextMenuStrip
            {
                BackColor = Theme.Surface,
                ForeColor = Theme.TextPrimary,
                Font = new Font("Segoe UI", 10)
            };

            var viewItem = new ToolStripMenuItem("View");
            viewItem.Click += (_, _) => ViewProperty(property);
            menu.Items.Add(viewItem);

            if (RbacService.CanEditRecord(property.ListedByAgentId, property.CreatedByUserId, property.AssignmentStatus))
            {
                var editItem = new ToolStripMenuItem("Edit");
                editItem.Click += (_, _) => EditProperty(property);
                menu.Items.Add(editItem);

                var deleteItem = new ToolStripMenuItem("Remove");
                deleteItem.Click += (_, _) => DeleteProperty(property);
                menu.Items.Add(deleteItem);
            }

            if (RbacService.CanAssignRecords)
            {
                var assignItem = new ToolStripMenuItem("Assign Agent");
                assignItem.Click += (_, _) =>
                {
                    using var dlg = new AssignAgentDialog(property.Address, _controller.GetAgents(), property.ListedByAgentId);
                    if (dlg.ShowDialog() == DialogResult.OK)
                    {
                        _controller.AssignAgent(property, dlg.SelectedAgentId, dlg.ApproveNow, dlg.ReviewNotes);
                        MessageBox.Show("Agent assigned successfully.", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
                        RefreshGrid();
                    }
                };
                menu.Items.Add(assignItem);
            }

            if (RbacService.CanApproveAssignments &&
                string.Equals(property.AssignmentStatus, "pending_review", StringComparison.OrdinalIgnoreCase))
            {
                var approveItem = new ToolStripMenuItem("Approve Assignment");
                approveItem.Click += (_, _) => ApprovePropertyAssignment(property);
                menu.Items.Add(approveItem);
            }

            menu.Show(grid, grid.PointToClient(Cursor.Position));
        }

        private void ViewProperty(Property property)
        {
            using var form = new PropertyDetailForm(property, _controller);
            form.ShowDialog();
        }

        private void EditProperty(Property property)
        {
            var owners = _controller.GetOwnerCustomers();
            var agents = _controller.GetAgents();

            EnsureExistingOwnerAndAgent(property, owners, agents);

            using var form = new PropertyInputForm(owners, agents, property);
            if (form.ShowDialog() == DialogResult.OK && form.Result is not null)
            {
                _controller.Update(form.Result);
                MessageBox.Show("Property listing updated successfully.", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
                RefreshGrid();
            }
        }

        private void DeleteProperty(Property property)
        {
            var confirmation = MessageBox.Show(
                $"Remove property at '{property.Address}'?",
                "Remove Property",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Question);

            if (confirmation != DialogResult.Yes) return;

            try
            {
                _controller.Delete(property);
                RefreshGrid();
            }
            catch (InvalidOperationException ex)
            {
                MessageBox.Show(ex.Message, "Cannot Remove Property", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to remove property: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void ApprovePropertyAssignment(Property property)
        {
            _controller.ApproveAssignment(property, "Reviewed from Properties module.");
            MessageBox.Show(
                $"Assignment for property #{property.PropertyId} has been approved.",
                "Assignment Approved",
                MessageBoxButtons.OK,
                MessageBoxIcon.Information);
            RefreshGrid();
        }

        private void BtnAddClick(object? sender, EventArgs e)
        {
            var owners = _controller.GetOwnerCustomers();
            if (owners.Count == 0)
            {
                MessageBox.Show(
                    "Add a seller or both-type customer before creating a property listing.",
                    "Owner Required",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information);

                return;
            }

            var agents = _controller.GetAgents();
            if (agents.Count == 0)
            {
                MessageBox.Show(
                    "Add an active agent before creating a property listing.",
                    "Agent Required",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information);

                return;
            }

            using var form = new PropertyInputForm(owners, agents);
            if (form.ShowDialog() == DialogResult.OK && form.Result is not null)
            {
                _controller.Add(form.Result);
                MessageBox.Show("Property listing created successfully.", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
                RefreshGrid();
            }
        }

        private void EnsureExistingOwnerAndAgent(
            Property property,
            List<CustomerPickerItem> owners,
            List<AgentPickerItem> agents)
        {
            if (!owners.Any(x => x.CustomerId == property.OwnerCustomerId))
            {
                owners.Add(new CustomerPickerItem(
                    property.OwnerCustomerId,
                    _controller.GetOwnerName(property.OwnerCustomerId) ?? $"Customer #{property.OwnerCustomerId}",
                    null));
            }

            if (property.ListedByAgentId.HasValue && !agents.Any(x => x.UserId == property.ListedByAgentId.Value))
            {
                agents.Add(new AgentPickerItem(
                    property.ListedByAgentId.Value,
                    _controller.GetListedAgentName(property.ListedByAgentId.Value) ?? $"User #{property.ListedByAgentId.Value}",
                    string.Empty));
            }
        }

        private static string GetName(Dictionary<int, string> names, int? id)
        {
            if (id is null) return "Unassigned";
            return names.TryGetValue(id.Value, out string? name) && !string.IsNullOrWhiteSpace(name)
                ? name
                : "-";
        }

        private void ExportToPdf()
        {
            using var sfd = new SaveFileDialog
            {
                Filter = "PDF Document (*.pdf)|*.pdf",
                FileName = $"Properties_Export_{DateTime.Now:yyyyMMdd_HHmm}.pdf"
            };
            if (sfd.ShowDialog() == DialogResult.OK)
            {
                var properties = _controller.GetAll().ToList();

                string[] headers = new[] { "ID", "Address", "Type", "Price", "Status", "Owner Customer", "Listing Agent" };
                var rows = properties.Select(p => new string[]
                {
                    p.PropertyId.ToString(),
                    p.Address,
                    p.PropertyType,
                    $"₱ {p.Price:N2}",
                    p.Status,
                    _controller.GetOwnerName(p.OwnerCustomerId) ?? $"Customer #{p.OwnerCustomerId}",
                    _controller.GetListedAgentName(p.ListedByAgentId) ?? "Unassigned"
                }).ToList();

                var kpis = new List<(string Title, string Value, string ColorHex)>
                {
                    ("Total Listings", properties.Count.ToString(), "#25679C"),
                    ("Available", properties.Count(x => string.Equals(x.Status, "Available", StringComparison.OrdinalIgnoreCase)).ToString(), "#059669"),
                    ("Pending", properties.Count(x => string.Equals(x.Status, "Pending", StringComparison.OrdinalIgnoreCase)).ToString(), "#D97706"),
                    ("Inventory Value", $"₱ {properties.Sum(x => x.Price):N2}", "#8B5CF6")
                };

                if (CRMS_Peguit.winforms.Services.PdfExportHelper.TryExportTable("Property Inventory & Listing Roster", headers, rows, sfd.FileName, out string? error, activeFilter: _filterStatus, kpis: kpis))
                {
                    MessageBox.Show("Properties exported to PDF successfully.", "Export Complete", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
                else
                {
                    MessageBox.Show(error ?? "Export failed.", "Export Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }

        private static bool ContainsText(string? value, string search)
        {
            return !string.IsNullOrWhiteSpace(value) &&
                   value.Contains(search, StringComparison.OrdinalIgnoreCase);
        }

private void LayoutToolbar()
        {
            ResponsiveLayout.ListPage(this, lblTitle, lblSubtitle, null, txtSearch,
                new Control[] { btnFilterAll, btnFilterAvailable, btnFilterPending, btnFilterSold }, new Control?[] { _btnExport, btnAdd }, pnlCard);
        }
    }
}
