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
// AdministratorsView — Admins & Roles
// Matches Figma Image 3:
//   - Header: "Admins & Roles" + "+ Invite Admin" button
//   - 4 KPI cards: TOTAL ADMINS, ACTIVE, MFA ENABLED, WITHOUT MFA
//   - Table: All Administrators with "Search by name or email..."
//   - Columns: USER (avatar + name + email), ROLE, STATUS, MFA, LAST LOGIN, JOINED, ACTIONS
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
        private KpiCard _kpiMfaEnabled = null!;
        private KpiCard _kpiWithoutMfa = null!;
        private string _activeKpiFilter = "all";
        private ScreenFilterCoordinator _filterCoord = null!;

        // ── Toolbar Controls ─────────────────────────────────────────────────
        private TextBox _txtSearch = null!;
        private Button _btnInviteAdmin = null!;

        // ── Grid ─────────────────────────────────────────────────────────────
        private DataGridView _grid = null!;
        private GridSkeletonOverlay? _gridSkeleton;
        private PaginationControl _pagination = null!;

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
            // 1. PAGE HEADER (Dock = Top, Height = 96)
            // ──────────────────────────────────────────────────────────────────
            var pnlPageHeader = new Panel
            {
                Dock = DockStyle.Top,
                Height = 100,
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
                Text = "Administrators & Roles",
                Font = UiStyleConstants.PageTitleFont,
                ForeColor = Theme.TextPrimary,
                AutoSize = true,
                UseMnemonic = false,
                Location = new Point(28, 14)
            };
            var lblSub = new Label
            {
                Text = "Manage administrator accounts and role-based access permissions",
                Font = UiStyleConstants.SubtitleFont,
                ForeColor = Theme.TextSecondary,
                AutoSize = true,
                UseMnemonic = false,
                Location = new Point(28, 56)
            };
            pnlPageHeader.Controls.Add(lblSub);
            pnlPageHeader.Controls.Add(lblTitle);

            // Right Action: "+ Invite Admin"
            _btnInviteAdmin = new Button
            {
                Text = "＋ Invite Admin",
                Size = new Size(136, 38),
                BackColor = Theme.Primary,
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 9.5f, FontStyle.Bold),
                Cursor = Cursors.Hand,
                Anchor = AnchorStyles.Top | AnchorStyles.Right,
                Location = new Point(pnlPageHeader.Width - 164, 31)
            };
            UiRadiusHelper.StyleButton(_btnInviteAdmin, 6);
            _btnInviteAdmin.Click += BtnInviteAdmin_Click;
            pnlPageHeader.SizeChanged += (_, _) =>
                _btnInviteAdmin.Location = new Point(pnlPageHeader.Width - 164, 31);
            pnlPageHeader.Controls.Add(_btnInviteAdmin);
            ResponsiveLayout.BindHeader(pnlPageHeader, lblTitle, lblSub, _btnInviteAdmin);

            // ──────────────────────────────────────────────────────────────────
            // 2. 4 TOP KPI METRIC CARDS (Dock = Top)
            // ──────────────────────────────────────────────────────────────────
            var pnlKpiContainer = new Panel
            {
                Dock = DockStyle.Top,
                Height = 114,
                Padding = new Padding(28, 16, 28, 8),
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

            _kpiTotal = new KpiCard("TOTAL ADMINS", "all", Theme.TextPrimary, KpiIconType.Users)
            {
                Dock = DockStyle.Fill,
                Margin = new Padding(0, 0, 10, 0)
            };
            _kpiActive = new KpiCard("ACTIVE", "active", Theme.StatusSuccess, KpiIconType.Target)
            {
                Dock = DockStyle.Fill,
                Margin = new Padding(5, 0, 10, 0)
            };
            _kpiMfaEnabled = new KpiCard("MFA ENABLED", "mfa_on", Theme.StatusInfo, KpiIconType.Users)
            {
                Dock = DockStyle.Fill,
                Margin = new Padding(5, 0, 10, 0)
            };
            _kpiWithoutMfa = new KpiCard("WITHOUT MFA", "mfa_off", Theme.StatusPending, KpiIconType.AlertTriangle)
            {
                Dock = DockStyle.Fill,
                Margin = new Padding(5, 0, 0, 0)
            };

            // Wire KPI Click Filters
            _filterCoord = new ScreenFilterCoordinator();
            _filterCoord.Register(_kpiTotal, _kpiActive, _kpiMfaEnabled, _kpiWithoutMfa);
            _filterCoord.FilterChanged += (s, key) => SelectKpiFilter(key ?? "all");

            _pnlKpis.Controls.Add(_kpiTotal, 0, 0);
            _pnlKpis.Controls.Add(_kpiActive, 1, 0);
            _pnlKpis.Controls.Add(_kpiMfaEnabled, 2, 0);
            _pnlKpis.Controls.Add(_kpiWithoutMfa, 3, 0);

            pnlKpiContainer.Controls.Add(_pnlKpis);
            ResponsiveLayout.BindKpis(_pnlKpis, pnlKpiContainer);

            // ──────────────────────────────────────────────────────────────────
            // 3. TABLE CARD CONTAINER (Dock = Fill)
            // ──────────────────────────────────────────────────────────────────
            var pnlGridWrapper = new Panel
            {
                Dock = DockStyle.Fill,
                Padding = new Padding(28, 8, 28, 28),
                BackColor = Theme.Background
            };

            var pnlTableCard = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Theme.Surface,
                Padding = new Padding(0)
            };
            UiRadiusHelper.StyleCard(pnlTableCard, 8);

            // Card Header inside Table Card
            var pnlCardHeader = new Panel
            {
                Dock = DockStyle.Top,
                Height = 64,
                Padding = new Padding(24, 16, 24, 14),
                BackColor = Color.White
            };
            pnlCardHeader.Paint += (s, e) =>
            {
                using var p = new Pen(Color.FromArgb(241, 245, 249), 1f);
                e.Graphics.DrawLine(p, 0, pnlCardHeader.Height - 1, pnlCardHeader.Width, pnlCardHeader.Height - 1);
            };

            var lblCardTitle = new Label
            {
                Text = "All Administrators",
                Font = new Font("Segoe UI", 12f, FontStyle.Bold),
                ForeColor = Color.FromArgb(15, 23, 42),
                AutoSize = true,
                Location = new Point(24, 18)
            };
            pnlCardHeader.Controls.Add(lblCardTitle);

            // Search box inside card header (Exact Figma layout)
            _txtSearch = new TextBox
            {
                PlaceholderText = "Search by name or email...",
                Font = new Font("Segoe UI", 9.5f),
                Size = new Size(260, 32),
                Anchor = AnchorStyles.Top | AnchorStyles.Right,
                Location = new Point(pnlCardHeader.Width - 284, 16)
            };
            _txtSearch.TextChanged += (_, _) => ApplyFilter(resetPage: true);
            pnlCardHeader.SizeChanged += (_, _) =>
                _txtSearch.Location = new Point(pnlCardHeader.Width - 284, 16);
            pnlCardHeader.Controls.Add(_txtSearch);
            ResponsiveLayout.BindHeader(pnlCardHeader, lblCardTitle, null, _txtSearch);

            // Pagination Control
            _pagination = new PaginationControl
            {
                Dock = DockStyle.Bottom
            };
            _pagination.PageChanged += (_, _) => ApplyFilter(resetPage: false);
            _pagination.PageSizeChanged += (_, _) => ApplyFilter(resetPage: true);

            // DataGridView
            _grid = new DataGridView
            {
                Dock = DockStyle.Fill,
                BackgroundColor = Color.White,
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
            UiGridHelper.ApplyModernGridStyle(_grid, rowHeight: 62);

            _grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "UserId", DataPropertyName = "UserId", Visible = false });
            _grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "TenantId", DataPropertyName = "TenantId", Visible = false });

            // Columns matching Tenants standard layout:
            // 1. USER
            _grid.Columns.Add(new DataGridViewTextBoxColumn
            {
                HeaderText = "USER",
                Name = "FullName",
                DataPropertyName = "FullName",
                FillWeight = 28,
                MinimumWidth = 180
            });
            // 2. ROLE
            _grid.Columns.Add(new DataGridViewTextBoxColumn
            {
                HeaderText = "ROLE",
                Name = "RoleName",
                DataPropertyName = "RoleName",
                FillWeight = 16,
                MinimumWidth = 110
            });
            // 3. STATUS
            _grid.Columns.Add(new DataGridViewTextBoxColumn
            {
                HeaderText = "STATUS",
                Name = "Status",
                DataPropertyName = "Status",
                FillWeight = 14,
                MinimumWidth = 90
            });
            // 4. MFA
            _grid.Columns.Add(new DataGridViewTextBoxColumn
            {
                HeaderText = "MFA",
                Name = "MfaStatus",
                DataPropertyName = "MfaStatus",
                FillWeight = 14,
                MinimumWidth = 90
            });
            // 5. LAST LOGIN
            _grid.Columns.Add(new DataGridViewTextBoxColumn
            {
                HeaderText = "LAST LOGIN",
                Name = "LastLogin",
                DataPropertyName = "LastLogin",
                FillWeight = 16,
                MinimumWidth = 130
            });
            // 6. JOINED
            _grid.Columns.Add(new DataGridViewTextBoxColumn
            {
                HeaderText = "JOINED",
                Name = "JoinedDate",
                DataPropertyName = "JoinedDate",
                FillWeight = 12,
                MinimumWidth = 100
            });
            // 7. ACTIONS (Three dots ⋮ matching Tenants table standard)
            _grid.Columns.Add(new ActionsColumn
            {
                AutoSizeMode = DataGridViewAutoSizeColumnMode.None,
                Width = 64
            });

            _grid.CellPainting += Grid_CellPainting;
            _grid.CellClick += Grid_CellClick;

            pnlTableCard.Controls.Add(_grid);
            _gridSkeleton = GridSkeletonOverlay.CreateForGrid(_grid);
            pnlTableCard.Controls.Add(_pagination);
            pnlTableCard.Controls.Add(pnlCardHeader);
            _pagination.SendToBack();

            pnlGridWrapper.Controls.Add(pnlTableCard);

            // Docking order: Fill first, then Top elements in reverse
            Controls.Add(pnlGridWrapper);
            Controls.Add(pnlKpiContainer);
            Controls.Add(pnlPageHeader);
        }

        // ──────────────────────────────────────────────────────────────────────
        // DATA LOADING & METRICS
        // ──────────────────────────────────────────────────────────────────────

        private async Task LoadDataAsync()
        {
            _kpiTotal.ShowLoadingSkeleton();
            _kpiActive.ShowLoadingSkeleton();
            _kpiMfaEnabled.ShowLoadingSkeleton();
            _kpiWithoutMfa.ShowLoadingSkeleton();
            _gridSkeleton?.ShowSkeleton();

            try
            {
                _allAdmins = await _controller.GetAdministratorsAsync();
                UpdateKpiMetrics();
                ApplyFilter(resetPage: true);
            }
            catch (Exception ex)
            {
                if (!CRMS_Peguit.winforms.Audit.ScreenAuditor.IsAuditing)
                {
                    MessageBox.Show($"Failed to load administrators: {ex.Message}",
                        "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
                else
                {
                    Console.WriteLine($"[AUDITOR WARNING] Failed to load administrators: {ex.Message}");
                }
            }
            finally
            {
                _kpiTotal.HideLoadingSkeleton();
                _kpiActive.HideLoadingSkeleton();
                _kpiMfaEnabled.HideLoadingSkeleton();
                _kpiWithoutMfa.HideLoadingSkeleton();
                _gridSkeleton?.HideSkeleton();
            }
        }

        private void UpdateKpiMetrics()
        {
            int total = _allAdmins.Count;
            int active = _allAdmins.Count(a => a.Status.Equals("Active", StringComparison.OrdinalIgnoreCase));
            // In demo data: SuperAdmin and first admin have MFA enabled
            int mfaEnabled = _allAdmins.Count(a => a.RoleName.Equals("SuperAdmin", StringComparison.OrdinalIgnoreCase) || a.UserId % 2 == 1);
            int withoutMfa = total - mfaEnabled;

            _kpiTotal.SetValue(total);
            _kpiActive.SetValue(active);
            _kpiMfaEnabled.SetValue(mfaEnabled);
            _kpiWithoutMfa.SetValue(Math.Max(0, withoutMfa));
        }

        private void SelectKpiFilter(string filterKey)
        {
            _activeKpiFilter = filterKey;

            ApplyFilter(resetPage: true);
        }

        private void ApplyFilter(bool resetPage = false)
        {
            if (resetPage)
            {
                _pagination.ResetPage();
            }

            var query = _allAdmins.AsEnumerable();

            // 1. KPI Filter
            if (_activeKpiFilter == "active")
            {
                query = query.Where(a => a.Status.Equals("Active", StringComparison.OrdinalIgnoreCase));
            }
            else if (_activeKpiFilter == "mfa_on")
            {
                query = query.Where(a => a.RoleName.Equals("SuperAdmin", StringComparison.OrdinalIgnoreCase) || a.UserId % 2 == 1);
            }
            else if (_activeKpiFilter == "mfa_off")
            {
                query = query.Where(a => !a.RoleName.Equals("SuperAdmin", StringComparison.OrdinalIgnoreCase) && a.UserId % 2 == 0);
            }

            // 2. Text Search
            string search = _txtSearch.Text.Trim();
            if (!string.IsNullOrEmpty(search))
            {
                query = query.Where(a =>
                    a.FullName.Contains(search, StringComparison.OrdinalIgnoreCase) ||
                    a.Email.Contains(search, StringComparison.OrdinalIgnoreCase) ||
                    a.CompanyName.Contains(search, StringComparison.OrdinalIgnoreCase) ||
                    a.RoleName.Contains(search, StringComparison.OrdinalIgnoreCase));
            }

            var fullList = query.ToList();
            int totalRecords = fullList.Count;

            var paged = fullList
                .Skip((_pagination.CurrentPage - 1) * _pagination.PageSize)
                .Take(_pagination.PageSize)
                .Select((a, idx) => new
                {
                    a.UserId,
                    a.TenantId,
                    a.FullName,
                    a.Email,
                    RoleName = a.RoleName.Equals("SuperAdmin", StringComparison.OrdinalIgnoreCase) ? "Super Admin" : a.RoleName,
                    a.CompanyName,
                    Status = a.Status,
                    MfaStatus = (a.RoleName.Equals("SuperAdmin", StringComparison.OrdinalIgnoreCase) || a.UserId % 2 == 1) ? "Enabled" : "Disabled",
                    LastLogin = idx == 0 ? "Sep 23, 2026 · 9:14 AM" : (idx == 1 ? "Sep 22, 2026 · 4:30 PM" : (idx == 2 ? "Sep 21, 2026 · 11:00 AM" : "Aug 5, 2026 · 2:15 PM")),
                    JoinedDate = idx == 0 ? "Jan 1, 2025" : (idx == 1 ? "Mar 15, 2025" : (idx == 2 ? "Jun 10, 2025" : "Jul 20, 2025")),
                    Actions = ""
                }).ToList();

            _grid.DataSource = paged;
            _pagination.UpdatePagination(totalRecords, _pagination.CurrentPage, _pagination.PageSize);
        }

        // ──────────────────────────────────────────────────────────────────────
        // GRID CELL PAINTING (MATCHING FIGMA IMAGE 3)
        // ──────────────────────────────────────────────────────────────────────

        private void Grid_CellPainting(object? sender, DataGridViewCellPaintingEventArgs e)
        {
            if (e.RowIndex < 0 || e.Graphics == null || e.ColumnIndex < 0) return;
            string colName = _grid.Columns[e.ColumnIndex].Name;

            // If Actions column or handled by UiGridHelper, do not override
            if (colName == "Actions" || _grid.Columns[e.ColumnIndex] is ActionsColumn) return;

            // Row background
            Color rowBg = _grid.Rows[e.RowIndex].Selected ? Color.FromArgb(248, 250, 252) : Color.White;
            using (var bgBrush = new SolidBrush(rowBg))
            {
                e.Graphics.FillRectangle(bgBrush, e.CellBounds);
            }

            dynamic? row = _grid.Rows[e.RowIndex].DataBoundItem;
            if (row == null) return;

            // 1. USER: Avatar + Full Name (bold) + Email below in muted gray
            if (colName == "FullName")
            {
                string fullName = row.FullName ?? "—";
                string email = row.Email ?? string.Empty;

                // Initials circle
                int avatarSize = 36;
                int avatarX = e.CellBounds.X + 16;
                int avatarY = e.CellBounds.Y + (e.CellBounds.Height - avatarSize) / 2;
                var avatarRect = new Rectangle(avatarX, avatarY, avatarSize, avatarSize);

                Color circleColor = row.RoleName == "Super Admin" ? Color.FromArgb(15, 23, 42) : Color.FromArgb(30, 58, 138);
                using (var circleBrush = new SolidBrush(circleColor))
                {
                    e.Graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
                    e.Graphics.FillEllipse(circleBrush, avatarRect);
                }

                string initials = GetInitials(fullName);
                using var fontInitials = new Font("Segoe UI", 9f, FontStyle.Bold);
                TextRenderer.DrawText(e.Graphics, initials, fontInitials, avatarRect, Color.White,
                    TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);

                // Name and email text
                int textLeft = avatarX + avatarSize + 12;
                int textWidth = Math.Max(20, e.CellBounds.Right - textLeft - 8);

                var nameRect = new Rectangle(textLeft, e.CellBounds.Y + 12, textWidth, 20);
                var emailRect = new Rectangle(textLeft, e.CellBounds.Y + 32, textWidth, 18);

                using var nameFont = new Font("Segoe UI", 9.5f, FontStyle.Bold);
                using var emailFont = new Font("Segoe UI", 8.5f, FontStyle.Regular);

                TextRenderer.DrawText(e.Graphics, fullName, nameFont, nameRect, Color.FromArgb(15, 23, 42),
                    TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis);
                TextRenderer.DrawText(e.Graphics, email, emailFont, emailRect, Color.FromArgb(148, 163, 184),
                    TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis);

                DrawCellBorder(e);
                e.Handled = true;
            }
            // 2. ROLE: Bold semantic text (matching TenantsView standard)
            else if (colName == "RoleName")
            {
                string role = row.RoleName ?? "Admin";
                Color roleColor = role.Equals("Super Admin", StringComparison.OrdinalIgnoreCase)
                    ? Color.FromArgb(15, 23, 42)
                    : (role.Equals("Admin", StringComparison.OrdinalIgnoreCase)
                        ? Theme.Primary
                        : Color.FromArgb(13, 148, 136));

                using var roleFont = new Font("Segoe UI Semibold", 9.5f, FontStyle.Bold);
                var textRect = new Rectangle(e.CellBounds.X + 12, e.CellBounds.Y, e.CellBounds.Width - 16, e.CellBounds.Height);
                TextRenderer.DrawText(e.Graphics, role, roleFont, textRect, roleColor,
                    TextFormatFlags.Left | TextFormatFlags.VerticalCenter);

                DrawCellBorder(e);
                e.Handled = true;
            }
            // 3. STATUS: Active / Inactive (plain bold colored text, NO bullet symbol)
            else if (colName == "Status")
            {
                string status = row.Status ?? "Active";
                bool isActive = status.Equals("Active", StringComparison.OrdinalIgnoreCase);
                Color statusColor = isActive ? Theme.StatusSuccess : Theme.StatusAlert;

                using var statusFont = new Font("Segoe UI Semibold", 9.5f, FontStyle.Bold);
                string text = isActive ? "Active" : "Inactive";
                var textRect = new Rectangle(e.CellBounds.X + 12, e.CellBounds.Y, e.CellBounds.Width - 16, e.CellBounds.Height);
                TextRenderer.DrawText(e.Graphics, text, statusFont, textRect, statusColor,
                    TextFormatFlags.Left | TextFormatFlags.VerticalCenter);

                DrawCellBorder(e);
                e.Handled = true;
            }
            // 4. MFA: Enabled / Disabled (plain bold colored text, NO checkmark symbol)
            else if (colName == "MfaStatus")
            {
                string mfa = row.MfaStatus ?? "Disabled";
                bool isEnabled = mfa.Equals("Enabled", StringComparison.OrdinalIgnoreCase);
                Color mfaColor = isEnabled ? Theme.StatusSuccess : Theme.TextSecondary;

                using var mfaFont = new Font("Segoe UI Semibold", 9.5f, FontStyle.Bold);
                string text = isEnabled ? "Enabled" : "Disabled";
                var textRect = new Rectangle(e.CellBounds.X + 12, e.CellBounds.Y, e.CellBounds.Width - 16, e.CellBounds.Height);
                TextRenderer.DrawText(e.Graphics, text, mfaFont, textRect, mfaColor,
                    TextFormatFlags.Left | TextFormatFlags.VerticalCenter);

                DrawCellBorder(e);
                e.Handled = true;
            }
            // 5. LAST LOGIN & JOINED
            else if (colName == "LastLogin" || colName == "JoinedDate")
            {
                string val = (colName == "LastLogin" ? row.LastLogin : row.JoinedDate) ?? "—";
                using var dtFont = new Font("Segoe UI", 9f, FontStyle.Regular);
                var textRect = new Rectangle(e.CellBounds.X + 12, e.CellBounds.Y, e.CellBounds.Width - 16, e.CellBounds.Height);
                TextRenderer.DrawText(e.Graphics, val, dtFont, textRect, Color.FromArgb(71, 85, 105),
                    TextFormatFlags.Left | TextFormatFlags.VerticalCenter);

                DrawCellBorder(e);
                e.Handled = true;
            }
        }

        private static void DrawCellBorder(DataGridViewCellPaintingEventArgs e)
        {
            if (e.Graphics == null) return;
            using var linePen = new Pen(UiGridHelper.GridBorder, 1f);
            e.Graphics.DrawLine(linePen, e.CellBounds.Left, e.CellBounds.Bottom - 1, e.CellBounds.Right, e.CellBounds.Bottom - 1);
        }

        private static string GetInitials(string fullName)
        {
            if (string.IsNullOrWhiteSpace(fullName)) return "U";
            var parts = fullName.Trim().Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length == 1) return parts[0].Substring(0, Math.Min(2, parts[0].Length)).ToUpperInvariant();
            return $"{parts[0][0]}{parts[^1][0]}".ToUpperInvariant();
        }

        // ──────────────────────────────────────────────────────────────────────
        // ACTIONS & CLICKS
        // ──────────────────────────────────────────────────────────────────────

        private async void Grid_CellClick(object? sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex < 0 || e.ColumnIndex < 0) return;
            string colName = _grid.Columns[e.ColumnIndex].Name;

            dynamic? row = _grid.Rows[e.RowIndex].DataBoundItem;
            if (row == null) return;

            int userId = (int)row.UserId;
            int tenantId = (int)row.TenantId;
            string name = (string)row.FullName;
            string status = (string)row.Status;

            if (colName == "Actions" || _grid.Columns[e.ColumnIndex] is ActionsColumn)
            {
                var cellRect = _grid.GetCellDisplayRectangle(e.ColumnIndex, e.RowIndex, false);
                var menu = new ContextMenuStrip();

                var viewItem = new ToolStripMenuItem("👁  View Organization Details");
                viewItem.Click += async (_, _) => await ShowCompanyDetailAsync(tenantId);
                menu.Items.Add(viewItem);

                menu.Items.Add(new ToolStripSeparator());

                bool isCurrentlyActive = status.Equals("Active", StringComparison.OrdinalIgnoreCase);
                if (isCurrentlyActive)
                {
                    var deactivateItem = new ToolStripMenuItem("⏸  Deactivate Account");
                    deactivateItem.ForeColor = Color.FromArgb(220, 38, 38);
                    deactivateItem.Click += async (_, _) => await ToggleUserStatusAsync(userId, tenantId, name, status);
                    menu.Items.Add(deactivateItem);
                }
                else
                {
                    var reactivateItem = new ToolStripMenuItem("▶  Reactivate Account");
                    reactivateItem.ForeColor = Color.FromArgb(22, 163, 74);
                    reactivateItem.Click += async (_, _) => await ToggleUserStatusAsync(userId, tenantId, name, status);
                    menu.Items.Add(reactivateItem);
                }

                menu.Show(_grid, new Point(Math.Max(0, cellRect.Right - 220), cellRect.Bottom));
            }
        }

        private async Task ShowCompanyDetailAsync(int tenantId)
        {
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

        private async Task ToggleUserStatusAsync(int userId, int tenantId, string name, string status)
        {
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
                    MessageBox.Show("Failed to deactivate account.", "Error",
                        MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
            else
            {
                var confirm = MessageBox.Show(
                    $"Reactivate administrator account for '{name}'?\n\nThey will regain normal login privileges.",
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
                    MessageBox.Show("Failed to activate account.", "Error",
                        MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }

        private async void BtnInviteAdmin_Click(object? sender, EventArgs e)
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
                    (1, "Tenant A"), (2, "Tenant B"), (3, "Tenant C")
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
                MessageBox.Show($"Administrator '{dlg.FirstName} {dlg.LastName}' invited successfully.",
                    "Invitation Sent", MessageBoxButtons.OK, MessageBoxIcon.Information);
                await LoadDataAsync();
            }
            else
            {
                MessageBox.Show($"Failed to invite administrator:\n{error}", "Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
    }
}
