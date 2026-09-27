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
using CRMS_Peguit.winforms.Models.ViewModels;
using CRMS_Peguit.winforms.Views.Customers;
using CRMS_Peguit.winforms.Views.Leads;
using CRMS_Peguit.winforms.Views.Properties;
using CRMS_Peguit.winforms.Views.Shared;

namespace CRMS_Peguit.winforms.Views.Management
{
    public partial class ApprovalsView : UserControl
    {
        private readonly ApprovalController _approvalController;

        private string _filterType = "All";
        private List<PendingApprovalItem> _allItems = new();
        private Label _lblEmptyState = null!;
        private PaginationControl _pagination = null!;
        private GridSkeletonOverlay? _gridSkeleton;

        public ApprovalsView()
        {
            InitializeComponent();

            _approvalController = new ApprovalController();

            InitPagination();
            InitEmptyState();
            ApplyStyling();
            BindEvents();
            UpdateFilterPillStyles();
            _ = RefreshGridAsync();

            this.Load += (_, _) => LayoutToolbar();
            this.Resize += (_, _) => LayoutToolbar();
        }

        private void InitPagination()
        {
            _pagination = new PaginationControl();
            _pagination.SetItemLabel("approvals");
            _pagination.PageChanged += (_, _) => FilterAndDisplay(resetPage: false);
            _pagination.PageSizeChanged += (_, _) => FilterAndDisplay(resetPage: true);
            pnlCard.Controls.Add(_pagination);
            _pagination.BringToFront();
        }

        private void InitEmptyState()
        {
            _lblEmptyState = new Label
            {
                Text = "🔍 No pending approval items match your filter criteria.\nAll caught up!",
                Font = new Font("Segoe UI", 11f),
                ForeColor = Theme.TextSecondary,
                TextAlign = ContentAlignment.MiddleCenter,
                Dock = DockStyle.Fill,
                Visible = false
            };
            pnlCard.Controls.Add(_lblEmptyState);
            _lblEmptyState.BringToFront();
        }

        private void ApplyStyling()
        {
            UiRadiusHelper.StyleCard(pnlCard, 12);
            UiRadiusHelper.StyleButton(btnRefresh, 8);
            UiRadiusHelper.ApplyPillShape(btnFilterAll);
            UiRadiusHelper.ApplyPillShape(btnFilterLeads);
            UiRadiusHelper.ApplyPillShape(btnFilterCustomers);
            UiRadiusHelper.ApplyPillShape(btnFilterProperties);
            _gridSkeleton = GridSkeletonOverlay.CreateForGrid(grid);
        }

        private void BindEvents()
        {
            btnRefresh.Click += (_, _) => RefreshGrid();
            txtSearch.TextChanged += (_, _) => ApplyFilterAndDisplay();

            btnFilterAll.Click += (_, _) => SetFilter("All");
            btnFilterLeads.Click += (_, _) => SetFilter("Leads");
            btnFilterCustomers.Click += (_, _) => SetFilter("Customers");
            btnFilterProperties.Click += (_, _) => SetFilter("Properties");

            // Modern Grid Styling & Search Padding
            UiGridHelper.ApplyModernGridStyle(grid, 52);
            UiRadiusHelper.SetPadding(txtSearch, 10, 10);

            grid.CellPainting += Grid_CellPainting;
            grid.CellContentClick += Grid_CellContentClick;
            grid.CellDoubleClick += (_, e) =>
            {
                if (e.RowIndex < 0) return;
                var item = GetItemAtRow(e.RowIndex);
                if (item is not null) OpenAssignDialog(item);
            };
        }

        private void SetFilter(string type)
        {
            _filterType = type;
            UpdateFilterPillStyles();
            ApplyFilterAndDisplay();
        }

        private void UpdateFilterPillStyles()
        {
            var pills = new[]
            {
                (btnFilterAll, "All"),
                (btnFilterLeads, "Leads"),
                (btnFilterCustomers, "Customers"),
                (btnFilterProperties, "Properties")
            };

            foreach (var (btn, name) in pills)
            {
                bool isSelected = string.Equals(_filterType, name, StringComparison.OrdinalIgnoreCase);
                if (isSelected)
                {
                    btn.BackColor = Color.FromArgb(15, 91, 158);
                    btn.ForeColor = Color.White;
                    btn.Font = new Font("Segoe UI", 9f, FontStyle.Bold);
                }
                else
                {
                    btn.BackColor = Color.White;
                    btn.ForeColor = Color.FromArgb(71, 85, 105);
                    btn.Font = new Font("Segoe UI", 9f, FontStyle.Regular);
                }
            }
        }

        public void RefreshGrid()
        {
            _ = RefreshGridAsync();
        }

        public async System.Threading.Tasks.Task RefreshGridAsync()
        {
            _gridSkeleton?.ShowSkeleton();
            try
            {
                var items = await System.Threading.Tasks.Task.Run(() => _approvalController.GetPendingApprovals());
                if (IsDisposed) return;

                if (InvokeRequired)
                {
                    BeginInvoke(new Action(() =>
                    {
                        if (IsDisposed) return;
                        ApplyLoadedItems(items);
                    }));
                }
                else
                {
                    ApplyLoadedItems(items);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[ApprovalsView] RefreshGridAsync error: {ex.Message}");
            }
            finally
            {
                if (InvokeRequired)
                {
                    BeginInvoke(new Action(() => _gridSkeleton?.HideSkeleton()));
                }
                else
                {
                    _gridSkeleton?.HideSkeleton();
                }
            }
        }

        private void ApplyLoadedItems(List<PendingApprovalItem> items)
        {
            _allItems = items;

            // Update subtitle badge
            int total = _allItems.Count;
            int leadCount = _allItems.Count(x => x.Type == "Lead");
            int custCount = _allItems.Count(x => x.Type == "Customer");
            int propCount = _allItems.Count(x => x.Type == "Property");
            lblSubtitle.Text = $"{total} items pending review · {leadCount} leads, {custCount} customers, {propCount} properties";

            ApplyFilterAndDisplay();
        }

        private void ApplyFilterAndDisplay(bool resetPage = false)
        {
            grid.Columns.Clear();
            grid.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.None;

            IEnumerable<PendingApprovalItem> query = _allItems;

            if (string.Equals(_filterType, "Leads", StringComparison.OrdinalIgnoreCase))
            {
                query = query.Where(x => x.Type == "Lead");
            }
            else if (string.Equals(_filterType, "Customers", StringComparison.OrdinalIgnoreCase))
            {
                query = query.Where(x => x.Type == "Customer");
            }
            else if (string.Equals(_filterType, "Properties", StringComparison.OrdinalIgnoreCase))
            {
                query = query.Where(x => x.Type == "Property");
            }

            string search = txtSearch.Text.Trim();
            if (!string.IsNullOrWhiteSpace(search))
            {
                query = query.Where(x =>
                    x.Title.Contains(search, StringComparison.OrdinalIgnoreCase) ||
                    x.SubmitterName.Contains(search, StringComparison.OrdinalIgnoreCase) ||
                    x.AssignedTo.Contains(search, StringComparison.OrdinalIgnoreCase) ||
                    x.Type.Contains(search, StringComparison.OrdinalIgnoreCase));
            }

            var allFiltered = query.ToList();
            int total = allFiltered.Count;
            int page = resetPage ? 1 : (_pagination?.CurrentPage ?? 1);
            int pageSize = _pagination?.PageSize ?? 25;

            _pagination?.UpdatePagination(total, page, pageSize);

            if (total == 0)
            {
                grid.DataSource = null;
                _lblEmptyState.Visible = true;
                _lblEmptyState.BringToFront();
                return;
            }

            _lblEmptyState.Visible = false;

            int effectivePage = _pagination?.CurrentPage ?? 1;
            var pageItems = allFiltered.Skip((effectivePage - 1) * pageSize).Take(pageSize).ToList();

            var displayList = pageItems.Select(x => new
            {
                x.Id,
                x.Type,
                Title = x.Title,
                SubmittedBy = x.SubmitterName,
                DateSubmitted = x.CreatedAt.ToString("MMM dd, yyyy"),
                x.AssignedTo,
                x.Status
            }).ToList();

            grid.DataSource = displayList;

            grid.ShowCellToolTips = true;

            var idCol = grid.Columns["Id"];
            if (idCol is not null) idCol.Visible = false;

            if (grid.Columns["Type"] is DataGridViewColumn typeCol)
            {
                typeCol.HeaderText = "TYPE";
                typeCol.FillWeight = 85;
                typeCol.MinimumWidth = 80;
            }
            if (grid.Columns["Title"] is DataGridViewColumn titleCol)
            {
                titleCol.HeaderText = "TITLE / RECORD";
                titleCol.FillWeight = 190;
                titleCol.MinimumWidth = 160;
            }
            if (grid.Columns["SubmittedBy"] is DataGridViewColumn subCol)
            {
                subCol.HeaderText = "SUBMITTED BY";
                subCol.FillWeight = 115;
                subCol.MinimumWidth = 100;
            }
            if (grid.Columns["DateSubmitted"] is DataGridViewColumn dateCol)
            {
                dateCol.HeaderText = "DATE SUBMITTED";
                dateCol.FillWeight = 95;
                dateCol.MinimumWidth = 95;
            }
            if (grid.Columns["AssignedTo"] is DataGridViewColumn assignCol)
            {
                assignCol.HeaderText = "ASSIGNED AGENT";
                assignCol.FillWeight = 120;
                assignCol.MinimumWidth = 110;
            }
            if (grid.Columns["Status"] is DataGridViewColumn statusCol)
            {
                statusCol.HeaderText = "STATUS";
                statusCol.FillWeight = 100;
                statusCol.MinimumWidth = 90;
            }

            grid.Columns.Add(new ActionsColumn());
            grid.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
        }

        private void FilterAndDisplay(bool resetPage = false) => ApplyFilterAndDisplay(resetPage);

        private void Grid_CellPainting(object? sender, DataGridViewCellPaintingEventArgs e)
        {
            if (e.RowIndex < 0 || e.Graphics is null) return;

            // Type Column with clean glyph and uniform 12px inset
            if (grid.Columns[e.ColumnIndex].Name == "Type" && e.Value != null)
            {
                string type = e.Value.ToString() ?? "";
                string icon = type switch
                {
                    "Lead" => "👤",
                    "Customer" => "🏢",
                    "Property" => "🏠",
                    _ => "📋"
                };

                using var font = new Font("Segoe UI", 9f, FontStyle.Bold);
                UiGridHelper.PaintTextCell(grid, e, $"{icon} {type}", font, Theme.TextPrimary,
                    TextFormatFlags.Left | TextFormatFlags.VerticalCenter, leftPadding: 12);
            }
            // Status Column (minimalist 6px dot + text, Left-aligned at 12px, Strictly NO badges/pills)
            else if (grid.Columns[e.ColumnIndex].Name == "Status" && e.Value != null)
            {
                string status = e.Value.ToString() ?? "";
                UiGridHelper.PaintStatusIndicator(grid, e, status, center: false);
            }
            // AssignedTo styling: italic gray if Unassigned (uniform 12px inset)
            else if (grid.Columns[e.ColumnIndex].Name == "AssignedTo" && e.Value != null)
            {
                string assigned = e.Value.ToString() ?? "";
                if (string.Equals(assigned, "Unassigned", StringComparison.OrdinalIgnoreCase))
                {
                    using var italicFont = new Font("Segoe UI", 9.5f, FontStyle.Italic);
                    UiGridHelper.PaintTextCell(grid, e, "Unassigned", italicFont, Color.FromArgb(148, 163, 184),
                        TextFormatFlags.Left | TextFormatFlags.VerticalCenter, leftPadding: 12);
                }
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

        private void Grid_CellContentClick(object? sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex < 0 || e.ColumnIndex < 0) return;
            if (grid.Columns[e.ColumnIndex] is not ActionsColumn) return;

            grid.Rows[e.RowIndex].Selected = true;
            grid.CurrentCell = grid.Rows[e.RowIndex].Cells[e.ColumnIndex];

            var item = GetItemAtRow(e.RowIndex);
            if (item is null) return;

            var menu = new ContextMenuStrip
            {
                BackColor = Theme.Surface,
                ForeColor = Theme.TextPrimary,
                Font = new Font("Segoe UI", 10)
            };

            var assignItem = new ToolStripMenuItem("Assign Agent...");
            assignItem.Click += (_, _) => OpenAssignDialog(item);
            menu.Items.Add(assignItem);

            var approveItem = new ToolStripMenuItem("Approve Immediately");
            approveItem.Click += (_, _) => ApproveItemImmediately(item);
            menu.Items.Add(approveItem);

            menu.Items.Add(new ToolStripSeparator());

            var detailsItem = new ToolStripMenuItem("View Details");
            detailsItem.Click += (_, _) => OpenDetailsForm(item);
            menu.Items.Add(detailsItem);

            menu.Show(grid, grid.PointToClient(Cursor.Position));
        }

        private void OpenAssignDialog(PendingApprovalItem item)
        {
            var agents = _approvalController.GetAgents();
            using var dlg = new AssignAgentDialog(item.Title, agents, item.AssignedAgentId);
            if (dlg.ShowDialog() == DialogResult.OK)
            {
                _approvalController.AssignAgent(item, dlg.SelectedAgentId, dlg.ApproveNow, dlg.ReviewNotes);

                MessageBox.Show(
                    $"Record '{item.Title}' has been assigned successfully.",
                    "Assignment Updated",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information);

                RefreshGrid();
            }
        }

        private void ApproveItemImmediately(PendingApprovalItem item)
        {
            _approvalController.ApproveAssignment(item, "Approved directly from Approvals Center.");

            MessageBox.Show(
                $"Submission for '{item.Title}' has been approved.",
                "Approved",
                MessageBoxButtons.OK,
                MessageBoxIcon.Information);

            RefreshGrid();
        }

        private void OpenDetailsForm(PendingApprovalItem item)
        {
            if (item.Type == "Lead" && item.OriginalEntity is Lead lead)
            {
                using var form = new LeadDetailForm(lead, _approvalController.LeadController);
                form.ShowDialog();
            }
            else if (item.Type == "Customer" && item.OriginalEntity is Customer cust)
            {
                using var form = new CustomerDetailForm(cust, _approvalController.CustomerController);
                form.ShowDialog();
            }
            else if (item.Type == "Property" && item.OriginalEntity is Property prop)
            {
                using var form = new PropertyDetailForm(prop, _approvalController.PropertyController);
                form.ShowDialog();
            }

            RefreshGrid();
        }

        private PendingApprovalItem? GetItemAtRow(int rowIndex)
        {
            if (rowIndex < 0 || rowIndex >= grid.Rows.Count) return null;
            if (grid.Rows[rowIndex].Cells["Id"].Value is int id &&
                grid.Rows[rowIndex].Cells["Type"].Value is string type)
            {
                return _allItems.FirstOrDefault(x => x.Id == id && x.Type == type);
            }
            return null;
        }

        private void LayoutToolbar()
        {
            if (this.IsDisposed) return;

            int rightPadding = 30;
            int leftMargin = 30;
            int totalWidth = ClientSize.Width;
            int y = 88;

            // Position header action buttons
            int rightEdge = totalWidth - rightPadding;
            btnRefresh.Left = rightEdge - btnRefresh.Width;
            btnRefresh.Top = 24;

            // Layout filter pills
            var pills = new[] { btnFilterProperties, btnFilterCustomers, btnFilterLeads, btnFilterAll };
            int filterRight = totalWidth - rightPadding;
            int totalFilterWidth = 0;
            foreach (var p in pills) totalFilterWidth += p.Width + 6;

            int availableForSearch = totalWidth - leftMargin - rightPadding - totalFilterWidth - 20;

            if (availableForSearch >= 180)
            {
                // Single row: search on left, filters aligned to right
                foreach (var p in pills)
                {
                    p.Top = y;
                    p.Left = filterRight - p.Width;
                    filterRight = p.Left - 6;
                }

                txtSearch.Top = y;
                txtSearch.Left = leftMargin;
                txtSearch.Width = Math.Min(360, availableForSearch);

                pnlCard.Top = 126;
                pnlCard.Height = Math.Max(100, ClientSize.Height - 126 - 30);
            }
            else
            {
                // Two rows: search on row 1, filter pills wrapped to row 2
                txtSearch.Top = y;
                txtSearch.Left = leftMargin;
                txtSearch.Width = Math.Max(180, totalWidth - leftMargin - rightPadding);

                int filterX = leftMargin;
                int pillY = y + 36;
                var forwardPills = new[] { btnFilterAll, btnFilterLeads, btnFilterCustomers, btnFilterProperties };
                foreach (var p in forwardPills)
                {
                    p.Top = pillY;
                    p.Left = filterX;
                    filterX += p.Width + 6;
                }

                pnlCard.Top = pillY + 38;
                pnlCard.Height = Math.Max(100, ClientSize.Height - pnlCard.Top - 20);
            }

            pnlCard.Left = leftMargin;
            pnlCard.Width = Math.Max(100, totalWidth - leftMargin - rightPadding);
        }
    }
}
