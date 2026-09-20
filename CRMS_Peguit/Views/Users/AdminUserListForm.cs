using System;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Drawing;
using CRMS_Peguit.domain.entities;
using CRMS_Peguit.winforms.Controllers;
using CRMS_Peguit.winforms.Models.Services;

namespace CRMS_Peguit.winforms.Views.Users
{
    public partial class AdminUserListForm : UserControl
    {
        private readonly UserController _controller;
        private Label _lblEmptyState = null!;

        public AdminUserListForm(string initialRoleFilter = "All Roles")
        {
            InitializeComponent();
            _controller = new UserController();

            InitEmptyState();
            UiRadiusHelper.StyleCard(pnlCard, 12);
            BindEvents(initialRoleFilter);
            LayoutControls();
            
            this.Load += async (s, e) => await RefreshGridAsync();
            this.Resize += (s, e) => LayoutControls();
        }

        private void InitEmptyState()
        {
            _lblEmptyState = new Label
            {
                Text = "🔍 No users match your search or filter criteria.\nTry adjusting your search terms or role filter.",
                Font = new Font("Segoe UI", 11f),
                ForeColor = Theme.TextSecondary,
                TextAlign = ContentAlignment.MiddleCenter,
                Dock = DockStyle.Fill,
                Visible = false
            };
            pnlCard.Controls.Add(_lblEmptyState);
            _lblEmptyState.BringToFront();
        }
        
        private void BindEvents(string initialRoleFilter)
        {
            txtSearch.TextChanged += async (s, e) => await RefreshGridAsync();

            cmbRoleFilter.Items.Clear();
            cmbRoleFilter.Items.AddRange(new[] { "All Roles", "Manager", "Agent" });
            cmbRoleFilter.SelectedItem = initialRoleFilter;
            cmbRoleFilter.SelectedIndexChanged += async (s, e) => await RefreshGridAsync();

            chkIncludeInactive.CheckedChanged += async (s, e) => await RefreshGridAsync();
            btnAdd.Click += BtnAddClick;
            UiRadiusHelper.StyleButton(btnAdd, 8);

            // Modern Grid Styling & Search Padding
            UiGridHelper.ApplyModernGridStyle(grid, 52);
            UiRadiusHelper.SetPadding(txtSearch, 10, 10);

            grid.CellPainting += Grid_CellPainting;
            grid.CellContentClick += GridCellContentClick;
        }

        private void LayoutControls()
        {
            if (this.IsDisposed) return;

            int rightPadding = 30;
            int leftMargin = 30;
            int totalWidth = ClientSize.Width;
            int y = 88;

            // Position header action button
            btnAdd.Left = totalWidth - rightPadding - btnAdd.Width;
            btnAdd.Top = 24;

            // Check if search + role + checkbox fit in single row
            int filtersWidth = 150 + 12 + chkIncludeInactive.Width;
            int availableForSearch = totalWidth - leftMargin - rightPadding - filtersWidth - 20;

            if (availableForSearch >= 200)
            {
                // Single row
                txtSearch.Top = y;
                txtSearch.Left = leftMargin;
                txtSearch.Width = Math.Min(320, availableForSearch);

                cmbRoleFilter.Top = y;
                cmbRoleFilter.Left = txtSearch.Right + 12;
                cmbRoleFilter.Width = 140;

                chkIncludeInactive.Top = y + 3;
                chkIncludeInactive.Left = cmbRoleFilter.Right + 16;

                pnlCard.Top = 126;
                pnlCard.Height = Math.Max(100, ClientSize.Height - 126 - 24);
            }
            else
            {
                // Two rows: search on row 1, role & checkbox on row 2
                txtSearch.Top = y;
                txtSearch.Left = leftMargin;
                txtSearch.Width = Math.Max(180, totalWidth - leftMargin - rightPadding);

                int row2Y = y + 34;
                cmbRoleFilter.Top = row2Y;
                cmbRoleFilter.Left = leftMargin;
                cmbRoleFilter.Width = 140;

                chkIncludeInactive.Top = row2Y + 3;
                chkIncludeInactive.Left = cmbRoleFilter.Right + 16;

                pnlCard.Top = row2Y + 38;
                pnlCard.Height = Math.Max(100, ClientSize.Height - pnlCard.Top - 20);
            }

            pnlCard.Left = leftMargin;
            pnlCard.Width = Math.Max(100, totalWidth - leftMargin - rightPadding);
        }

        private async Task RefreshGridAsync()
        {
            if (this.IsDisposed) return;

            try
            {
                var allUsers = await _controller.GetAllAsync(chkIncludeInactive.Checked);
                int total = allUsers.Count;
                int active = allUsers.Count(u => string.Equals(u.Status, "active", StringComparison.OrdinalIgnoreCase));
                lblSubtitle.Text = $"{total} total users · {active} active";

                var users = allUsers;
                string search = txtSearch.Text.Trim();
                if (!string.IsNullOrWhiteSpace(search))
                {
                    users = users.Where(u => 
                        (u.FullName?.Contains(search, StringComparison.OrdinalIgnoreCase) == true) ||
                        (u.Email?.Contains(search, StringComparison.OrdinalIgnoreCase) == true)
                    ).ToList();
                }

                var rolesDict = (await _controller.GetManagedRolesAsync()).ToDictionary(r => r.RoleId, r => r.RoleName);

                string roleFilter = cmbRoleFilter.SelectedItem?.ToString() ?? "All Roles";
                if (roleFilter != "All Roles")
                {
                    var targetRole = rolesDict.Values.FirstOrDefault(r => r.Equals(roleFilter, StringComparison.OrdinalIgnoreCase));
                    if (targetRole != null)
                    {
                        users = users.Where(u => rolesDict.ContainsKey(u.RoleId) && rolesDict[u.RoleId] == targetRole).ToList();
                    }
                }

                grid.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.None;
                grid.DataSource = users.Select(u => new
                {
                    u.UserId,
                    Name = u.FullName,
                    Email = u.Email,
                    Role = rolesDict.ContainsKey(u.RoleId) ? rolesDict[u.RoleId] : "Unknown",
                    Status = u.Status
                }).ToList();

                grid.ShowCellToolTips = true;

                if (grid.Columns["UserId"] is DataGridViewColumn uIdCol) uIdCol.Visible = false;

                if (grid.Columns["Name"] is DataGridViewColumn nameCol)
                {
                    nameCol.HeaderText = "NAME";
                    nameCol.FillWeight = 140;
                    nameCol.MinimumWidth = 140;
                }
                if (grid.Columns["Email"] is DataGridViewColumn emailCol)
                {
                    emailCol.HeaderText = "EMAIL";
                    emailCol.FillWeight = 150;
                    emailCol.MinimumWidth = 150;
                }
                if (grid.Columns["Role"] is DataGridViewColumn roleCol)
                {
                    roleCol.HeaderText = "ROLE";
                    roleCol.FillWeight = 100;
                    roleCol.MinimumWidth = 100;
                }
                if (grid.Columns["Status"] is DataGridViewColumn statusCol)
                {
                    statusCol.HeaderText = "STATUS";
                    statusCol.FillWeight = 90;
                    statusCol.MinimumWidth = 85;
                    statusCol.HeaderCell.Style.Alignment = DataGridViewContentAlignment.MiddleLeft;
                    statusCol.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleLeft;
                }
                
                if (grid.Columns["Actions"] == null)
                {
                    grid.Columns.Add(new CRMS_Peguit.winforms.Controls.ActionsColumn());
                }

                grid.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
                UiGridHelper.EnforceTableStandards(grid);
                _lblEmptyState.Visible = (grid.Rows.Count == 0);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Error Loading Users", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void Grid_CellPainting(object? sender, DataGridViewCellPaintingEventArgs e)
        {
            if (e.RowIndex < 0 || e.Graphics is null) return;
            string colName = grid.Columns[e.ColumnIndex].Name;

            if (colName == "Name" && e.Value != null)
            {
                string name = e.Value.ToString() ?? "";
                UiGridHelper.PaintAvatarCell(grid, e, name);
            }
            else if (colName == "Status" && e.Value != null)
            {
                string status = e.Value.ToString() ?? "";
                UiGridHelper.PaintStatusIndicator(grid, e, status, center: false);
            }
        }

        private async void BtnAddClick(object? sender, EventArgs e)
        {
            using var form = new AdminUserEditForm(_controller, null);
            if (form.ShowDialog() == DialogResult.OK)
            {
                await RefreshGridAsync();
            }
        }

        private async void GridCellContentClick(object? sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex < 0 || e.ColumnIndex < 0) return;
            if (grid.Columns[e.ColumnIndex].Name != "Actions") return;

            var idValue = grid.Rows[e.RowIndex].Cells["UserId"].Value;
            if (idValue == null || !int.TryParse(idValue.ToString(), out int userId)) return;

            var menu = new ContextMenuStrip
            {
                BackColor = Theme.Surface,
                ForeColor = Theme.TextPrimary,
                Font = new Font("Segoe UI", 10)
            };

            var viewItem = new ToolStripMenuItem("View Details");
            viewItem.Click += async (_, _) => await ViewUserAsync(userId);
            menu.Items.Add(viewItem);

            var editItem = new ToolStripMenuItem("Edit");
            editItem.Click += async (_, _) => await EditUserAsync(userId);
            menu.Items.Add(editItem);

            menu.Show(grid, grid.PointToClient(Cursor.Position));
        }

        private async Task ViewUserAsync(int userId)
        {
            var user = await _controller.GetByIdAsync(userId);
            if (user == null) return;
            
            var roles = (await _controller.GetManagedRolesAsync()).ToDictionary(r => r.RoleId, r => r.RoleName);

            using var form = new AdminUserDetailsForm(_controller, user, roles);
            if (form.ShowDialog() == DialogResult.OK)
            {
                await RefreshGridAsync();
            }
        }

        private async Task EditUserAsync(int userId)
        {
            var user = await _controller.GetByIdAsync(userId);
            if (user == null) return;

            using var form = new AdminUserEditForm(_controller, user);
            if (form.ShowDialog() == DialogResult.OK)
            {
                await RefreshGridAsync();
            }
        }

    }
}


