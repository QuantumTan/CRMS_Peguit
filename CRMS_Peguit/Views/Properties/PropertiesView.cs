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

        public PropertiesView()
        {
            InitializeComponent();
            _controller = new PropertyController();

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
            _pagination.SetItemLabel("properties");
            _pagination.PageChanged += async (_, _) => await RefreshGridAsync(resetPage: false);
            _pagination.PageSizeChanged += async (_, _) => await RefreshGridAsync(resetPage: true);
            pnlCard.Controls.Add(_pagination);
            _pagination.BringToFront();
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

        private async void SetFilter(string filter)
        {
            if (string.Equals(_filterStatus, filter, StringComparison.OrdinalIgnoreCase) && !string.Equals(filter, "All", StringComparison.OrdinalIgnoreCase))
            {
                _filterStatus = "All";
            }
            else
            {
                _filterStatus = filter;
            }
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
                    _owners = _controller.GetOwnerCustomers().ToDictionary(x => x.CustomerId, x => x.FullName);
                }
                if (_agents.Count == 0)
                {
                    _agents = _controller.GetAgents().ToDictionary(x => x.UserId, x => x.FullName);
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

            _controller.Delete(property);
            RefreshGrid();
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

        private void ExportToCsv()
        {
            using var sfd = new SaveFileDialog
            {
                Filter = "CSV File (*.csv)|*.csv",
                FileName = $"Properties_Export_{DateTime.Now:yyyyMMdd_HHmm}.csv"
            };
            if (sfd.ShowDialog() == DialogResult.OK)
            {
                var properties = _controller.GetAll().ToList();
                var sb = new System.Text.StringBuilder();
                sb.AppendLine("PropertyId,Address,PropertyType,Price,Status,OwnerCustomerId,ListedByAgentId");
                foreach (var p in properties)
                {
                    sb.AppendLine($"\"{p.PropertyId}\",\"{p.Address}\",\"{p.PropertyType}\",\"{p.Price}\",\"{p.Status}\",\"{p.OwnerCustomerId}\",\"{p.ListedByAgentId}\"");
                }
                System.IO.File.WriteAllText(sfd.FileName, sb.ToString());
                MessageBox.Show("Properties exported successfully.", "Export Complete", MessageBoxButtons.OK, MessageBoxIcon.Information);
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

            // 2. Toolbar Row (No KPI row on Properties screen; toolbar follows directly below header)
            int y = lblSubtitle.Bottom + 16;

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

            var pills = new[] { btnFilterSold, btnFilterPending, btnFilterAvailable, btnFilterAll };
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
                var forwardPills = new[] { btnFilterAll, btnFilterAvailable, btnFilterPending, btnFilterSold };
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
