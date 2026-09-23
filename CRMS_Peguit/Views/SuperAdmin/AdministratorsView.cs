using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using Microsoft.EntityFrameworkCore;
using CRMS_Peguit.winforms.Auth;
using CRMS_Peguit.winforms.Controllers;
using CRMS_Peguit.winforms.Controls;
using CRMS_Peguit.winforms.Models.Services;
using CRMS_Peguit.winforms.Services;

// =============================================================================
// AdministratorsView — Platform System Users & Administrators Management
//
// DATA BOUNDARY: SuperAdminController.GetAdministratorsAsync() queries
// Users + Persons + Roles (per tenant) + Company names (MasterDb).
// Strict multi-tenant isolation — NO customer/deal/ticket CRM tables accessed.
// =============================================================================

namespace CRMS_Peguit.winforms.Views.SuperAdmin
{
    public class AdministratorsView : UserControl
    {
        private readonly SuperAdminController _controller = new();
        private List<AdminDto> _allAdmins = new();

        // ── KPI Controls ─────────────────────────────────────────────────────
        private TableLayoutPanel _pnlKpis = null!;
        private KpiCard _kpiTotal = null!;
        private KpiCard _kpiActive = null!;
        private KpiCard _kpiSuperAdmins = null!;
        private KpiCard _kpiInactive = null!;
        private string _activeKpiFilter = "all";

        // ── Toolbar & Filter Controls ─────────────────────────────────────────
        private TextBox _txtSearch = null!;
        private ComboBox _cmbRoleFilter = null!;
        private ComboBox _cmbStatusFilter = null!;
        private Button _btnResetFilters = null!;
        private Label _lblCount = null!;
        private Button _btnCreate = null!;
        private Button _btnToggleStatus = null!;
        private Button _btnRefresh = null!;

        // ── Grid ─────────────────────────────────────────────────────────────
        private DataGridView _grid = null!;

        public AdministratorsView()
        {
            InitializeComponent();
            _ = LoadDataAsync();
        }

        private void InitializeComponent()
        {
            Dock = DockStyle.Fill;
            BackColor = Theme.Background;
            AutoScroll = true;

            // ──────────────────────────────────────────────────────────────────
            // 1. PAGE HEADER (Dock = Top)
            // ──────────────────────────────────────────────────────────────────
            var pnlPageHeader = new Panel
            {
                Dock = DockStyle.Top,
                Height = 84,
                BackColor = Theme.Surface,
                Padding = new Padding(28, 16, 28, 16)
            };
            pnlPageHeader.Paint += (s, e) =>
            {
                using var p = new Pen(Theme.Border, 1f);
                e.Graphics.DrawLine(p, 0, pnlPageHeader.Height - 1, pnlPageHeader.Width, pnlPageHeader.Height - 1);
            };

            var lblTitle = new Label
            {
                Text = "👥  System Users & Administrators",
                Font = new Font("Segoe UI", 16f, FontStyle.Bold),
                ForeColor = Theme.TextPrimary,
                AutoSize = true,
                Location = new Point(28, 14)
            };
            var lblSub = new Label
            {
                Text = "Platform Governance — Manage Administrator & Manager accounts across all tenant databases",
                Font = new Font("Segoe UI", 9.5f),
                ForeColor = Theme.TextSecondary,
                AutoSize = true,
                Location = new Point(28, 44)
            };
            pnlPageHeader.Controls.Add(lblSub);
            pnlPageHeader.Controls.Add(lblTitle);

            // Top Header Action Buttons (Right-aligned)
            var pnlHeaderActions = new FlowLayoutPanel
            {
                FlowDirection = FlowDirection.RightToLeft,
                Anchor = AnchorStyles.Top | AnchorStyles.Right,
                Height = 42,
                Width = 340,
                Location = new Point(pnlPageHeader.Width - 364, 18),
                BackColor = Color.Transparent
            };
            pnlPageHeader.SizeChanged += (_, _) =>
                pnlHeaderActions.Location = new Point(pnlPageHeader.Width - 364, 18);

            _btnCreate = new Button
            {
                Text = "＋  Create User",
                Size = new Size(130, 36),
                BackColor = Theme.Primary,
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 9f, FontStyle.Bold),
                Cursor = Cursors.Hand,
                Margin = new Padding(6, 0, 0, 0)
            };
            UiRadiusHelper.StyleButton(_btnCreate, 6);
            _btnCreate.Click += BtnCreate_Click;

            _btnRefresh = new Button
            {
                Text = "↻  Refresh",
                Size = new Size(100, 36),
                BackColor = Theme.Surface,
                ForeColor = Theme.TextPrimary,
                Font = new Font("Segoe UI", 9f, FontStyle.Regular),
                Cursor = Cursors.Hand,
                Margin = new Padding(6, 0, 0, 0)
            };
            UiRadiusHelper.StyleButton(_btnRefresh, 6);
            _btnRefresh.Click += async (_, _) => await LoadDataAsync();

            pnlHeaderActions.Controls.Add(_btnCreate);
            pnlHeaderActions.Controls.Add(_btnRefresh);
            pnlPageHeader.Controls.Add(pnlHeaderActions);

            // ──────────────────────────────────────────────────────────────────
            // 2. KPI METRIC CARDS ROW (Dock = Top)
            // ──────────────────────────────────────────────────────────────────
            var pnlKpiContainer = new Panel
            {
                Dock = DockStyle.Top,
                Height = 120,
                Padding = new Padding(24, 16, 24, 4),
                BackColor = Theme.Background
            };

            _pnlKpis = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 4,
                RowCount = 1
            };
            _pnlKpis.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25f));
            _pnlKpis.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25f));
            _pnlKpis.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25f));
            _pnlKpis.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25f));

            _kpiTotal = new KpiCard("Total Administrators", "all", Theme.Primary, KpiIconType.Users, "Across all tenants")
            {
                Dock = DockStyle.Fill,
                Margin = new Padding(0, 0, 8, 0)
            };
            _kpiActive = new KpiCard("Active Accounts", "active", Color.FromArgb(16, 185, 129), KpiIconType.Target, "Normal access state")
            {
                Dock = DockStyle.Fill,
                Margin = new Padding(4, 0, 8, 0)
            };
            _kpiSuperAdmins = new KpiCard("Super Admins", "superadmin", Color.FromArgb(139, 92, 246), KpiIconType.Users, "Root platform authority")
            {
                Dock = DockStyle.Fill,
                Margin = new Padding(4, 0, 8, 0)
            };
            _kpiInactive = new KpiCard("Suspended / Inactive", "inactive", Color.FromArgb(239, 68, 68), KpiIconType.AlertTriangle, "Blocked login state")
            {
                Dock = DockStyle.Fill,
                Margin = new Padding(4, 0, 0, 0)
            };

            // Wire KPI Click Filters
            _kpiTotal.SetAction(() => SelectKpiFilter("all"));
            _kpiActive.SetAction(() => SelectKpiFilter("active"));
            _kpiSuperAdmins.SetAction(() => SelectKpiFilter("superadmin"));
            _kpiInactive.SetAction(() => SelectKpiFilter("inactive"));

            _pnlKpis.Controls.Add(_kpiTotal, 0, 0);
            _pnlKpis.Controls.Add(_kpiActive, 1, 0);
            _pnlKpis.Controls.Add(_kpiSuperAdmins, 2, 0);
            _pnlKpis.Controls.Add(_kpiInactive, 3, 0);

            pnlKpiContainer.Controls.Add(_pnlKpis);

            // ──────────────────────────────────────────────────────────────────
            // 3. FACETED TOOLBAR & ACTIONS (Dock = Top)
            // ──────────────────────────────────────────────────────────────────
            var pnlToolbar = new Panel
            {
                Dock = DockStyle.Top,
                Height = 60,
                Padding = new Padding(24, 12, 24, 8),
                BackColor = Theme.Background
            };

            // Search input
            _txtSearch = new TextBox
            {
                PlaceholderText = "🔍  Search administrator name, email, or company...",
                Font = new Font("Segoe UI", 9.5f),
                Size = new Size(320, 32),
                Location = new Point(24, 14)
            };
            _txtSearch.TextChanged += (_, _) => ApplyFilter();
            pnlToolbar.Controls.Add(_txtSearch);

            // Role Filter
            _cmbRoleFilter = new ComboBox
            {
                DropDownStyle = ComboBoxStyle.DropDownList,
                Font = new Font("Segoe UI", 9.5f),
                Size = new Size(130, 32),
                Location = new Point(354, 14),
                BackColor = Color.White
            };
            _cmbRoleFilter.Items.AddRange(new object[] { "All Roles", "SuperAdmin", "Admin", "Manager" });
            _cmbRoleFilter.SelectedIndex = 0;
            _cmbRoleFilter.SelectedIndexChanged += (_, _) => ApplyFilter();
            pnlToolbar.Controls.Add(_cmbRoleFilter);

            // Status Filter
            _cmbStatusFilter = new ComboBox
            {
                DropDownStyle = ComboBoxStyle.DropDownList,
                Font = new Font("Segoe UI", 9.5f),
                Size = new Size(130, 32),
                Location = new Point(492, 14),
                BackColor = Color.White
            };
            _cmbStatusFilter.Items.AddRange(new object[] { "All Statuses", "Active", "Inactive" });
            _cmbStatusFilter.SelectedIndex = 0;
            _cmbStatusFilter.SelectedIndexChanged += (_, _) => ApplyFilter();
            pnlToolbar.Controls.Add(_cmbStatusFilter);

            // Reset Filters Button
            _btnResetFilters = new Button
            {
                Text = "✕ Clear",
                Size = new Size(72, 30),
                Location = new Point(630, 14),
                BackColor = Theme.Surface,
                ForeColor = Theme.TextSecondary,
                Font = new Font("Segoe UI", 8.5f),
                Cursor = Cursors.Hand
            };
            UiRadiusHelper.StyleButton(_btnResetFilters, 4);
            _btnResetFilters.Click += (_, _) =>
            {
                _txtSearch.Text = string.Empty;
                _cmbRoleFilter.SelectedIndex = 0;
                _cmbStatusFilter.SelectedIndex = 0;
                SelectKpiFilter("all");
            };
            pnlToolbar.Controls.Add(_btnResetFilters);

            // Count label
            _lblCount = new Label
            {
                Text = "",
                Font = new Font("Segoe UI", 9f, FontStyle.Bold),
                ForeColor = Theme.TextSecondary,
                AutoSize = true,
                Location = new Point(712, 18)
            };
            pnlToolbar.Controls.Add(_lblCount);

            // Action button container on right
            var pnlRowActions = new FlowLayoutPanel
            {
                FlowDirection = FlowDirection.RightToLeft,
                Anchor = AnchorStyles.Top | AnchorStyles.Right,
                Height = 38,
                Width = 200,
                Location = new Point(pnlToolbar.Width - 224, 10),
                BackColor = Color.Transparent
            };
            pnlToolbar.SizeChanged += (_, _) =>
                pnlRowActions.Location = new Point(pnlToolbar.Width - 224, 10);

            _btnToggleStatus = new Button
            {
                Text = "⊘  Deactivate",
                Size = new Size(140, 32),
                BackColor = Color.FromArgb(254, 226, 226),
                ForeColor = Color.FromArgb(153, 27, 27),
                Font = new Font("Segoe UI", 9f, FontStyle.Bold),
                Cursor = Cursors.Hand,
                Enabled = false
            };
            UiRadiusHelper.StyleButton(_btnToggleStatus, 6);
            _btnToggleStatus.Click += BtnToggleStatus_Click;
            pnlRowActions.Controls.Add(_btnToggleStatus);

            pnlToolbar.Controls.Add(pnlRowActions);

            // ──────────────────────────────────────────────────────────────────
            // 4. GRID WRAPPER (Dock = Fill)
            // ──────────────────────────────────────────────────────────────────
            var pnlGridWrapper = new Panel
            {
                Dock = DockStyle.Fill,
                Padding = new Padding(24, 6, 24, 24),
                BackColor = Theme.Background
            };

            var pnlCard = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Theme.Surface,
                Padding = new Padding(1)
            };
            UiRadiusHelper.StyleCard(pnlCard, 8);

            _grid = new DataGridView
            {
                Dock = DockStyle.Fill,
                BackgroundColor = Theme.Surface,
                BorderStyle = BorderStyle.None,
                RowHeadersVisible = false,
                AllowUserToAddRows = false,
                AllowUserToDeleteRows = false,
                ReadOnly = true,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                MultiSelect = false,
                AutoGenerateColumns = false,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill
            };
            UiGridHelper.ApplyModernGridStyle(_grid, rowHeight: 52);

            _grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "UserId", DataPropertyName = "UserId", Visible = false });
            _grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "TenantId", DataPropertyName = "TenantId", Visible = false });

            _grid.Columns.Add(new DataGridViewTextBoxColumn
            {
                HeaderText = "ADMINISTRATOR",
                Name = "FullName",
                DataPropertyName = "FullName",
                FillWeight = 26,
                MinimumWidth = 180
            });
            _grid.Columns.Add(new DataGridViewTextBoxColumn
            {
                HeaderText = "EMAIL ADDRESS",
                Name = "Email",
                DataPropertyName = "Email",
                FillWeight = 24,
                MinimumWidth = 160
            });
            _grid.Columns.Add(new DataGridViewTextBoxColumn
            {
                HeaderText = "SYSTEM ROLE",
                Name = "RoleName",
                DataPropertyName = "RoleName",
                FillWeight = 16,
                MinimumWidth = 110
            });
            _grid.Columns.Add(new DataGridViewTextBoxColumn
            {
                HeaderText = "ORGANIZATION / TENANT",
                Name = "CompanyName",
                DataPropertyName = "CompanyName",
                FillWeight = 22,
                MinimumWidth = 150
            });
            _grid.Columns.Add(new DataGridViewTextBoxColumn
            {
                HeaderText = "STATUS",
                Name = "Status",
                DataPropertyName = "Status",
                FillWeight = 12,
                MinimumWidth = 90
            });

            _grid.CellPainting += Grid_CellPainting;
            _grid.CellClick += Grid_CompanyClick;
            _grid.SelectionChanged += Grid_SelectionChanged;

            pnlCard.Controls.Add(_grid);
            pnlGridWrapper.Controls.Add(pnlCard);

            // Add in proper docking order: Fill first, then Top elements in reverse order
            Controls.Add(pnlGridWrapper);   // Dock = Fill
            Controls.Add(pnlToolbar);       // Dock = Top (below KPIs)
            Controls.Add(pnlKpiContainer);  // Dock = Top (below Header)
            Controls.Add(pnlPageHeader);    // Dock = Top (at the very top)
        }

        // ──────────────────────────────────────────────────────────────────────
        // DATA LOADING & METRICS
        // ──────────────────────────────────────────────────────────────────────

        private async Task LoadDataAsync()
        {
            try
            {
                _allAdmins = await _controller.GetAdministratorsAsync();
                UpdateKpiMetrics();
                ApplyFilter();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to load administrators: {ex.Message}",
                    "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void UpdateKpiMetrics()
        {
            int total = _allAdmins.Count;
            int active = _allAdmins.Count(a => a.Status.Equals("Active", StringComparison.OrdinalIgnoreCase));
            int superAdmins = _allAdmins.Count(a => a.RoleName.Equals("SuperAdmin", StringComparison.OrdinalIgnoreCase));
            int inactive = _allAdmins.Count(a => a.Status.Equals("Inactive", StringComparison.OrdinalIgnoreCase));

            _kpiTotal.SetValue(total);
            _kpiActive.SetValue(active);
            _kpiSuperAdmins.SetValue(superAdmins);
            _kpiInactive.SetValue(inactive);
        }

        private void SelectKpiFilter(string filterKey)
        {
            _activeKpiFilter = filterKey;
            _kpiTotal.SetSelected(filterKey == "all");
            _kpiActive.SetSelected(filterKey == "active");
            _kpiSuperAdmins.SetSelected(filterKey == "superadmin");
            _kpiInactive.SetSelected(filterKey == "inactive");

            ApplyFilter();
        }

        private void ApplyFilter()
        {
            var query = _allAdmins.AsEnumerable();

            // 1. KPI Filter
            if (_activeKpiFilter == "active")
            {
                query = query.Where(a => a.Status.Equals("Active", StringComparison.OrdinalIgnoreCase));
            }
            else if (_activeKpiFilter == "superadmin")
            {
                query = query.Where(a => a.RoleName.Equals("SuperAdmin", StringComparison.OrdinalIgnoreCase));
            }
            else if (_activeKpiFilter == "inactive")
            {
                query = query.Where(a => a.Status.Equals("Inactive", StringComparison.OrdinalIgnoreCase));
            }

            // 2. Dropdown Role Filter
            string selectedRole = _cmbRoleFilter.SelectedItem?.ToString() ?? "All Roles";
            if (selectedRole != "All Roles")
            {
                query = query.Where(a => a.RoleName.Equals(selectedRole, StringComparison.OrdinalIgnoreCase));
            }

            // 3. Dropdown Status Filter
            string selectedStatus = _cmbStatusFilter.SelectedItem?.ToString() ?? "All Statuses";
            if (selectedStatus != "All Statuses")
            {
                query = query.Where(a => a.Status.Equals(selectedStatus, StringComparison.OrdinalIgnoreCase));
            }

            // 4. Text Search
            string search = _txtSearch.Text.Trim();
            if (!string.IsNullOrEmpty(search))
            {
                query = query.Where(a =>
                    a.FullName.Contains(search, StringComparison.OrdinalIgnoreCase) ||
                    a.Email.Contains(search, StringComparison.OrdinalIgnoreCase) ||
                    a.CompanyName.Contains(search, StringComparison.OrdinalIgnoreCase) ||
                    a.RoleName.Contains(search, StringComparison.OrdinalIgnoreCase));
            }

            var list = query.Select(a => new
            {
                a.UserId,
                a.TenantId,
                a.FullName,
                a.Email,
                a.RoleName,
                a.CompanyName,
                a.Status
            }).ToList();

            _grid.DataSource = list;
            _lblCount.Text = $"Showing {list.Count} of {_allAdmins.Count} user{(list.Count != 1 ? "s" : "")}";
            UpdateActionButtonState();
        }

        // ──────────────────────────────────────────────────────────────────────
        // GRID EVENTS & STYLING
        // ──────────────────────────────────────────────────────────────────────

        private void Grid_SelectionChanged(object? sender, EventArgs e)
        {
            UpdateActionButtonState();
        }

        private void UpdateActionButtonState()
        {
            if (_grid.CurrentRow?.DataBoundItem == null)
            {
                _btnToggleStatus.Enabled = false;
                _btnToggleStatus.Text = "⊘  Deactivate";
                _btnToggleStatus.BackColor = Color.FromArgb(243, 244, 246);
                _btnToggleStatus.ForeColor = Color.FromArgb(156, 163, 175);
                return;
            }

            _btnToggleStatus.Enabled = true;
            dynamic row = _grid.CurrentRow.DataBoundItem;
            string status = row.Status ?? "Active";

            if (status.Equals("Active", StringComparison.OrdinalIgnoreCase))
            {
                _btnToggleStatus.Text = "⊘  Deactivate";
                _btnToggleStatus.BackColor = Color.FromArgb(254, 226, 226);
                _btnToggleStatus.ForeColor = Color.FromArgb(153, 27, 27);
            }
            else
            {
                _btnToggleStatus.Text = "✓  Activate";
                _btnToggleStatus.BackColor = Color.FromArgb(209, 250, 229);
                _btnToggleStatus.ForeColor = Color.FromArgb(6, 95, 70);
            }
        }

        private void Grid_CellPainting(object? sender, DataGridViewCellPaintingEventArgs e)
        {
            if (e.RowIndex < 0 || e.Graphics == null) return;
            string colName = _grid.Columns[e.ColumnIndex].Name;

            if (colName == "FullName" && e.Value != null)
            {
                string name = e.Value.ToString() ?? "—";
                UiGridHelper.PaintAvatarCell(_grid, e, name);
            }
            else if (colName == "Status" && e.Value != null)
            {
                string status = e.Value.ToString() ?? "Active";
                UiGridHelper.PaintStatusText(_grid, e, status, center: false);
            }
            else if (colName == "RoleName" && e.Value != null)
            {
                string role = e.Value.ToString() ?? "Admin";
                Color roleColor = role.Equals("SuperAdmin", StringComparison.OrdinalIgnoreCase)
                    ? Color.FromArgb(109, 40, 217) // Purple
                    : (role.Equals("Manager", StringComparison.OrdinalIgnoreCase)
                        ? Color.FromArgb(217, 119, 6) // Amber
                        : Color.FromArgb(29, 78, 216)); // Blue

                using var roleFont = new Font("Segoe UI Semibold", 9.5f, FontStyle.Bold);
                UiGridHelper.PaintTextCell(_grid, e, role, roleFont, roleColor);
            }
            else if (colName == "CompanyName" && e.Value != null)
            {
                e.Handled = false;
            }
        }

        private async void Grid_CompanyClick(object? sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex < 0 || e.ColumnIndex < 0) return;
            if (_grid.Columns[e.ColumnIndex].Name != "CompanyName") return;

            dynamic row = _grid.Rows[e.RowIndex].DataBoundItem!;
            int tenantId = row.TenantId;

            var detail = await _controller.GetCompanyDetailAsync(tenantId);
            if (detail == null)
            {
                MessageBox.Show("Company details not found.", "Information",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            using var dlg = new CompanyDetailDialog(detail);
            dlg.ShowDialog(this);
        }

        // ──────────────────────────────────────────────────────────────────────
        // BUSINESS ACTIONS (CRM INDUSTRY STANDARD FLOW)
        // ──────────────────────────────────────────────────────────────────────

        private async void BtnToggleStatus_Click(object? sender, EventArgs e)
        {
            if (_grid.CurrentRow?.DataBoundItem == null) return;

            dynamic row = _grid.CurrentRow.DataBoundItem;
            int userId = row.UserId;
            int tenantId = row.TenantId;
            string name = row.FullName;
            string status = row.Status;

            // Self-protection guardrail: SuperAdmin cannot deactivate their own active account
            if (userId == CurrentSession.UserId)
            {
                MessageBox.Show(
                    "You cannot deactivate your own active session account.\n\nTo manage this account, another Super Administrator must perform the action.",
                    "Action Forbidden", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            bool isCurrentlyActive = status.Equals("Active", StringComparison.OrdinalIgnoreCase);

            if (isCurrentlyActive)
            {
                var confirm = MessageBox.Show(
                    $"Deactivate administrator account for '{name}'?\n\nThey will be immediately blocked from logging into the tenant system until reactivated.",
                    "Confirm Account Deactivation", MessageBoxButtons.YesNo, MessageBoxIcon.Warning);

                if (confirm != DialogResult.Yes) return;

                bool ok = await _controller.DeactivateAdminAsync(userId, tenantId);
                if (ok)
                {
                    MessageBox.Show($"'{name}' has been deactivated.", "Account Updated",
                        MessageBoxButtons.OK, MessageBoxIcon.Information);
                    await LoadDataAsync();
                }
                else
                {
                    MessageBox.Show("Failed to deactivate account. Please try again.", "Error",
                        MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
            else
            {
                var confirm = MessageBox.Show(
                    $"Reactivate administrator account for '{name}'?\n\nThey will regain normal login and administration privileges.",
                    "Confirm Account Activation", MessageBoxButtons.YesNo, MessageBoxIcon.Question);

                if (confirm != DialogResult.Yes) return;

                bool ok = await _controller.ActivateAdminAsync(userId, tenantId);
                if (ok)
                {
                    MessageBox.Show($"'{name}' has been reactivated.", "Account Updated",
                        MessageBoxButtons.OK, MessageBoxIcon.Information);
                    await LoadDataAsync();
                }
                else
                {
                    MessageBox.Show("Failed to activate account. Please try again.", "Error",
                        MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }

        private async void BtnCreate_Click(object? sender, EventArgs e)
        {
            List<(int TenantId, string CompanyName)> companies = new();
            try
            {
                using var masterDb = LocalDb.CreateMasterContext();
                var comps = await masterDb.Companies
                    .AsNoTracking()
                    .OrderBy(c => c.CompanyName)
                    .ToListAsync();
                companies = comps.Select(c => (c.CompanyId, c.CompanyName)).ToList();
            }
            catch
            {
                companies = new List<(int, string)>
                {
                    (1, "Tenant 1"), (2, "Tenant 2"), (3, "Tenant 3")
                };
            }

            using var dlg = new CreateAdminDialog(companies);
            if (dlg.ShowDialog(this) != DialogResult.OK) return;

            var (success, error) = await _controller.CreateAdminAsync(
                dlg.SelectedTenantId,
                dlg.FirstName, dlg.LastName,
                dlg.Email, dlg.Password,
                "Admin");

            if (success)
            {
                MessageBox.Show($"Administrator '{dlg.FirstName} {dlg.LastName}' created successfully.",
                    "Created", MessageBoxButtons.OK, MessageBoxIcon.Information);
                await LoadDataAsync();
            }
            else
            {
                MessageBox.Show($"Failed to create administrator:\n{error}", "Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
    }
}
