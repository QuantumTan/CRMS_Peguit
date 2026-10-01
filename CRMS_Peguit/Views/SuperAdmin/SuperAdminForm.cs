using System;
using System.Drawing;
using System.Windows.Forms;
using CRMS_Peguit.winforms.Auth;
using CRMS_Peguit.winforms.Controls;
using CRMS_Peguit.winforms.Models.Services;
using CRMS_Peguit.winforms.Views.SuperAdmin;

// =============================================================================
// SuperAdminForm — NEXA Super Admin Console Shell
// Architecture: Platform Infrastructure & Multi-Tenant Management
//   1. Dashboard (Platform Overview & Aggregate Metrics)
//   2. SYSTEM: Admins & Roles, Data & Backup, Settings & Policies, Subscription
//   3. TENANTS & HEALTH: Tenants, Sync Health, Audit Log
//
// DATA BOUNDARY ATTESTATION:
// SuperAdmin has ZERO access to tenant business/operational data (Leads, Deals,
// Customers, Properties, Activities, SupportTickets, TaskReminders, Notifications).
// Every query is provably incapable of returning a row from those tables.
// =============================================================================

namespace CRMS_Peguit.winforms
{
    public class SuperAdminForm : Form
    {
        // ── Outer Layout Panels ──────────────────────────────────────────────
        private Panel _sidebar = null!;
        private Panel _contentWrapper = null!;
        private Panel _header = null!;
        private Panel _mainPanel = null!;

        // ── Sidebar Controls ─────────────────────────────────────────────────
        private Panel _pnlNav = null!;
        private Button _btnDashboard = null!;
        private Button _btnAdministrators = null!;
        private Button _btnBackups = null!;
        private Button _btnSystemSettings = null!;
        private Button _btnSubscriptions = null!;
        private Button _btnReports = null!;
        private Button _btnTerms = null!;
        private Button _btnAudit = null!;
        private Button _btnSyncHealth = null!;
        private Button _btnTenants = null!;
        private Button? _activeNavBtn;

        // ── Header Controls ──────────────────────────────────────────────────
        private AvatarLabel _avatarHeader = null!;
        private Label _lblHeaderBreadcrumb = null!;
        private TextBox _txtHeaderSearch = null!;

        // ── Child Views (lazy-loaded) ─────────────────────────────────────────
        private SuperAdminDashboardView? _dashboardView;
        private AdministratorsView? _administratorsView;
        private SubscriptionsView? _subscriptionsView;
        private AdminPanelMasterView? _reportsView;
        private AdminPanelMasterView? _termsView;
        private PlatformAuditLogView? _auditView;
        private SyncHealthView? _syncHealthView;
        private SystemSettingsView? _systemSettingsView;
        private BackupsView? _backupsView;
        private TenantsView? _tenantsView;

        // ── Logout Reference ─────────────────────────────────────────────────
        private readonly LoginForm? _loginForm;

        public SuperAdminForm(LoginForm? loginForm = null)
        {
            _loginForm = loginForm;
            InitializeComponent();
            NavigateTo(_btnDashboard);
        }

        private void InitializeComponent()
        {
            Text = "NEXA CRM — Super Admin Console";
            Size = new Size(1440, 900);
            MinimumSize = new Size(1180, 750);
            StartPosition = FormStartPosition.CenterScreen;
            BackColor = Theme.Background;
            AppBrand.ApplyAppIcon(this);

            SuspendLayout();

            // 1. Content Wrapper (Fills remaining area)
            _contentWrapper = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Theme.Background
            };

            // 2. Top Header (inside contentWrapper)
            BuildHeader();

            // 3. Main Content Panel (inside contentWrapper)
            _mainPanel = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Theme.Background
            };

            // Add mainPanel first (Dock=Fill), then _header (Dock=Top)
            _contentWrapper.Controls.Add(_mainPanel);
            _contentWrapper.Controls.Add(_header);

            // 4. Sidebar (Dock = Left)
            BuildSidebar();

            // Add contentWrapper first, then sidebar (MainForm architecture)
            Controls.Add(_contentWrapper);
            Controls.Add(_sidebar);

            ResumeLayout(true);
        }

        // ──────────────────────────────────────────────────────────────────────
        // SIDEBAR
        // ──────────────────────────────────────────────────────────────────────

        private void BuildSidebar()
        {
            _sidebar = new Panel
            {
                Dock = DockStyle.Left,
                Width = 240,
                BackColor = Theme.SidebarBackground  // Slate 900 #0F172A — matches tenant exactly
            };

            // ── Brand / Logo header (Dock = Top) ──────────────────────────────
            var pnlBrand = new Panel
            {
                Dock = DockStyle.Top,
                Height = 64,
                BackColor = Theme.SidebarBackground,
                Padding = new Padding(14, 0, 14, 0),
                Cursor = Cursors.Hand
            };

            var picLogo = new PictureBox
            {
                Size = new Size(32, 32),
                Location = new Point(14, 16),
                SizeMode = PictureBoxSizeMode.Zoom,
                BackColor = Color.Transparent,
                Cursor = Cursors.Hand
            };
            if (AppBrand.Logo != null)
            {
                picLogo.Image = AppBrand.Logo;
            }
            picLogo.Paint += (s, e) =>
            {
                if (picLogo.Image == null)
                {
                    e.Graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
                    using var brush = new SolidBrush(Color.FromArgb(37, 99, 235));
                    var points = new PointF[]
                    {
                        new PointF(16, 4),
                        new PointF(28, 28),
                        new PointF(4, 28)
                    };
                    e.Graphics.FillPolygon(brush, points);
                }
            };

            var lblBrand = new Label
            {
                Text = "NEXA CRM SYSTEM",
                Font = new Font("Segoe UI", 12f, FontStyle.Bold),
                ForeColor = Color.White,
                AutoSize = true,
                Location = new Point(52, 21),
                Cursor = Cursors.Hand
            };

            EventHandler navDashboard = (s, e) => NavigateTo(_btnDashboard);
            pnlBrand.Click += navDashboard;
            picLogo.Click += navDashboard;
            lblBrand.Click += navDashboard;

            pnlBrand.Controls.Add(picLogo);
            pnlBrand.Controls.Add(lblBrand);

            pnlBrand.Paint += (s, e) =>
            {
                using var pen = new Pen(Color.FromArgb(30, 41, 59), 1f); // Slate 800 hairline separator
                e.Graphics.DrawLine(pen, 14, pnlBrand.Height - 1, pnlBrand.Width - 14, pnlBrand.Height - 1);
            };

            // ── Sign Out (Dock = Bottom) ──────────────────────────────────────
            var pnlSignOut = new Panel
            {
                Dock = DockStyle.Bottom,
                Height = 56,
                BackColor = Theme.SidebarBackground,
                Padding = new Padding(10, 7, 10, 7)
            };
            pnlSignOut.Paint += (s, e) =>
            {
                using var pen = new Pen(Color.FromArgb(30, 41, 59), 1f); // Slate 800 hairline border
                e.Graphics.DrawLine(pen, 10, 0, pnlSignOut.Width - 10, 0);
            };

            var btnLogout = new Button
            {
                Dock = DockStyle.Fill,
                Text = "   ↪   Sign out",
                Font = new Font("Segoe UI", 9f),
                ForeColor = Theme.SidebarText,
                BackColor = Color.Transparent,
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand,
                TextAlign = ContentAlignment.MiddleLeft,
                Padding = new Padding(14, 0, 0, 0)
            };
            btnLogout.FlatAppearance.BorderSize = 0;
            btnLogout.FlatAppearance.MouseOverBackColor = Color.FromArgb(30, 41, 59); // Slate 800
            btnLogout.FlatAppearance.MouseDownBackColor = Color.FromArgb(15, 23, 42);
            UiRadiusHelper.StyleButton(btnLogout, 8, drawFocusRing: false);

            btnLogout.MouseEnter += (_, _) => btnLogout.ForeColor = Color.White;
            btnLogout.MouseLeave += (_, _) => btnLogout.ForeColor = Theme.SidebarText;
            btnLogout.Click += LnkSignOut_Click;
            pnlSignOut.Controls.Add(btnLogout);

            // ── Nav Items (Dock = Fill) ───────────────────────────────────────
            _pnlNav = new Panel
            {
                Dock = DockStyle.Fill,
                AutoScroll = true,
                BackColor = Color.Transparent,
                Padding = new Padding(10, 10, 10, 10)
            };

            int y = 10;

            // 1. Dashboard
            _btnDashboard = CreateNavButton(KpiIconType.Dashboard, "Dashboard", y);
            _btnDashboard.Click += (_, _) => NavigateTo(_btnDashboard);
            _pnlNav.Controls.Add(_btnDashboard);
            y += 48;

            // Section: TENANTS ACCOUNTS
            y += 10;
            _pnlNav.Controls.Add(CreateSectionHeader("TENANTS ACCOUNTS", y));
            y += 24;

            // 2. Tenants (Company Management)
            _btnTenants = CreateNavButton(KpiIconType.Building, "Tenants", y);
            _btnTenants.Click += (_, _) => NavigateTo(_btnTenants);
            _pnlNav.Controls.Add(_btnTenants);
            y += 48;

            // 3. Administrators
            _btnAdministrators = CreateNavButton(KpiIconType.Shield, "Administrators", y);
            _btnAdministrators.Click += (_, _) => NavigateTo(_btnAdministrators);
            _pnlNav.Controls.Add(_btnAdministrators);
            y += 48;

            // 4. Subscriptions
            _btnSubscriptions = CreateNavButton(KpiIconType.CreditCard, "Subscriptions", y);
            _btnSubscriptions.Click += (_, _) => NavigateTo(_btnSubscriptions);
            _pnlNav.Controls.Add(_btnSubscriptions);
            y += 48;

            // 5. Platform Reports
            _btnReports = CreateNavButton(KpiIconType.FileText, "Reports", y);
            _btnReports.Click += (_, _) => NavigateTo(_btnReports);
            _pnlNav.Controls.Add(_btnReports);
            y += 48;

            // Section: SYSTEM GOVERNANCE
            y += 10;
            _pnlNav.Controls.Add(CreateSectionHeader("SYSTEM GOVERNANCE", y));
            y += 24;

            // 5. System Settings
            _btnSystemSettings = CreateNavButton(KpiIconType.Settings, "System Settings", y);
            _btnSystemSettings.Click += (_, _) => NavigateTo(_btnSystemSettings);
            _pnlNav.Controls.Add(_btnSystemSettings);
            y += 48;

            // 6. Backups
            _btnBackups = CreateNavButton(KpiIconType.Database, "Backups", y);
            _btnBackups.Click += (_, _) => NavigateTo(_btnBackups);
            _pnlNav.Controls.Add(_btnBackups);
            y += 48;
            _btnTerms = CreateNavButton(KpiIconType.FileText, "Terms & Conditions", y);
            _btnTerms.Click += (_, _) => NavigateTo(_btnTerms);
            _pnlNav.Controls.Add(_btnTerms);
            y += 48;
            _btnAudit = CreateNavButton(KpiIconType.FileText, "Audit Log", y);
            _btnAudit.Click += (_, _) => NavigateTo(_btnAudit);
            _pnlNav.Controls.Add(_btnAudit);
            y += 48;
            _btnSyncHealth = CreateNavButton(KpiIconType.Database, "Sync Health", y);
            _btnSyncHealth.Click += (_, _) => NavigateTo(_btnSyncHealth);
            _pnlNav.Controls.Add(_btnSyncHealth);

            // Correct docking order: Fill first, then Bottom, then Top panels
            _sidebar.Controls.Add(_pnlNav);
            _sidebar.Controls.Add(pnlSignOut);
            _sidebar.Controls.Add(pnlBrand);
        }

        private static Label CreateSectionHeader(string title, int y)
        {
            return new Label
            {
                Text = title,
                Font = new Font("Segoe UI", 7f, FontStyle.Bold),
                ForeColor = Color.FromArgb(100, 116, 139), // Slate 500
                Location = new Point(18, y),
                Size = new Size(204, 18)
            };
        }

        private Button CreateNavButton(KpiIconType icon, string label, int y)
        {
            var btn = new Button
            {
                Text = string.Empty,
                AccessibleName = label,
                Tag = (icon, label),
                Location = new Point(10, y),
                Size = new Size(220, 42),
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 9f, FontStyle.Regular),
                ForeColor = Theme.SidebarText,
                BackColor = Color.Transparent,
                Cursor = Cursors.Hand
            };
            btn.FlatAppearance.BorderSize = 0;
            btn.FlatAppearance.MouseOverBackColor = Color.Transparent;
            btn.FlatAppearance.MouseDownBackColor = Color.Transparent;

            bool isHovered = false;

            btn.Paint += (s, e) =>
            {
                if (s is not Button b) return;
                e.Graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
                e.Graphics.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;

                bool isActive = b == _activeNavBtn;
                var rect = new Rectangle(0, 0, b.Width, b.Height);
                int pillRadius = 10;

                // 1. Draw Pill Background
                if (isActive)
                {
                    // Vibrant highlighted pill for active tab
                    using var activeBrush = new SolidBrush(Color.FromArgb(37, 99, 235)); // Royal Blue #2563EB
                    using var path = UiRadiusHelper.CreateRoundedPath(rect, pillRadius);
                    e.Graphics.FillPath(activeBrush, path);
                }
                else if (isHovered)
                {
                    // Subtle hover pill
                    using var hoverBrush = new SolidBrush(Color.FromArgb(30, 41, 59)); // Slate 800 #1E293B
                    using var path = UiRadiusHelper.CreateRoundedPath(rect, pillRadius);
                    e.Graphics.FillPath(hoverBrush, path);
                }

                // 2. Draw Clean Outline Icon
                Color iconColor = isActive ? Color.White : (isHovered ? Color.FromArgb(241, 245, 249) : Theme.SidebarText);
                var iconRect = new Rectangle(14, (b.Height - 20) / 2, 20, 20);
                UiIconHelper.DrawIcon(e.Graphics, icon, iconRect, iconColor);

                // 3. Draw Typography
                Color textColor = isActive ? Color.White : (isHovered ? Color.FromArgb(241, 245, 249) : Theme.SidebarText);
                using var textFont = new Font("Segoe UI", 9f, isActive ? FontStyle.Bold : FontStyle.Regular);
                var textRect = new Rectangle(44, 0, b.Width - 48, b.Height);
                TextRenderer.DrawText(e.Graphics, label, textFont, textRect, textColor,
                    TextFormatFlags.Left | TextFormatFlags.VerticalCenter);
            };

            btn.MouseEnter += (s, e) =>
            {
                isHovered = true;
                btn.Invalidate();
            };
            btn.MouseLeave += (s, e) =>
            {
                isHovered = false;
                btn.Invalidate();
            };

            return btn;
        }

        // ──────────────────────────────────────────────────────────────────────
        // HEADER
        // ──────────────────────────────────────────────────────────────────────

        private void BuildHeader()
        {
            _header = new Panel
            {
                Dock = DockStyle.Top,
                Height = 64,
                BackColor = Theme.Surface,
                Padding = new Padding(24, 0, 24, 0)
            };

            _header.Paint += (s, e) =>
            {
                using var p = new Pen(Theme.Border, 1f);
                e.Graphics.DrawLine(p, 0, _header.Height - 1, _header.Width, _header.Height - 1);
            };

            // Left Pill: Super Admin indicator (Height = 32, centered at Y = 16)
            var pnlPill = new Panel
            {
                Location = new Point(24, 16),
                Size = new Size(130, 32),
                BackColor = Color.FromArgb(241, 245, 249)
            };
            UiRadiusHelper.StyleCard(pnlPill, 16);

            var lblPillDot = new Label
            {
                Text = "●",
                ForeColor = Color.FromArgb(15, 23, 42),
                Font = new Font("Segoe UI", 7f),
                AutoSize = true,
                Location = new Point(14, 8)
            };
            var lblPillText = new Label
            {
                Text = "Super Admin",
                Font = new Font("Segoe UI", 9f, FontStyle.Bold),
                ForeColor = Color.FromArgb(15, 23, 42),
                AutoSize = true,
                Location = new Point(28, 6)
            };
            pnlPill.Controls.Add(lblPillDot);
            pnlPill.Controls.Add(lblPillText);
            _header.Controls.Add(pnlPill);

            // Middle Search Bar Container (Height = 36, centered at Y = 14)
            var pnlSearchContainer = new Panel
            {
                Location = new Point(170, 14),
                Size = new Size(360, 36),
                BackColor = Color.FromArgb(248, 250, 252)
            };
            UiRadiusHelper.StyleCard(pnlSearchContainer, 8, Color.FromArgb(226, 232, 240));

            var lblSearchIcon = new Label
            {
                Text = "🔍",
                Font = new Font("Segoe UI Emoji", 9.5f),
                ForeColor = Color.FromArgb(100, 116, 139),
                Size = new Size(24, 24),
                Location = new Point(10, 6),
                TextAlign = ContentAlignment.MiddleCenter
            };
            pnlSearchContainer.Controls.Add(lblSearchIcon);

            _txtHeaderSearch = new TextBox
            {
                PlaceholderText = "Find a module (press Enter)...",
                Font = new Font("Segoe UI", 9.5f),
                Size = new Size(310, 22),
                Location = new Point(38, 7),
                BorderStyle = BorderStyle.None,
                BackColor = Color.FromArgb(248, 250, 252),
                ForeColor = Color.FromArgb(15, 23, 42)
            };
            pnlSearchContainer.Controls.Add(_txtHeaderSearch);
            _txtHeaderSearch.KeyDown += (_, e) =>
            {
                if (e.KeyCode != Keys.Enter) return;
                e.SuppressKeyPress = true;
                string query = _txtHeaderSearch.Text.Trim();
                if (query.Length == 0) return;
                var match = _pnlNav.Controls.OfType<Button>().FirstOrDefault(b =>
                    b.AccessibleName?.Contains(query, StringComparison.OrdinalIgnoreCase) == true);
                if (match != null)
                {
                    NavigateTo(match);
                    _txtHeaderSearch.Clear();
                }
                else MessageBox.Show("No matching module. Try Reports, Terms, Tenants, or Audit.",
                    "Find Module", MessageBoxButtons.OK, MessageBoxIcon.Information);
            };
            _header.Controls.Add(pnlSearchContainer);

            // Breadcrumb (Internal Tracking)
            _lblHeaderBreadcrumb = new Label
            {
                Visible = false
            };
            _header.Controls.Add(_lblHeaderBreadcrumb);

            // Right: User Avatar + Notification bell (Vertically centered at Y = 12 and Y = 14)
            string adminName = CurrentSession.CurrentUser?.FullName ?? "Super Admin";
            _avatarHeader = new AvatarLabel(adminName, "Super Admin")
            {
                Size = new Size(180, 40),
                Anchor = AnchorStyles.Top | AnchorStyles.Left,
                AvatarSize = 34
            };
            _avatarHeader.Location = new Point(_header.Width - 200, 12);
            _header.Controls.Add(_avatarHeader);

            // Notification Bell (Height = 36, centered at Y = 14)
            var btnBell = new Button
            {
                Text = "🔔",
                Font = new Font("Segoe UI", 11f),
                Size = new Size(36, 36),
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.Transparent,
                ForeColor = Theme.TextSecondary,
                Cursor = Cursors.Hand,
                Anchor = AnchorStyles.Top | AnchorStyles.Left,
                Location = new Point(_header.Width - 246, 14)
            };
            btnBell.FlatAppearance.BorderSize = 0;
            btnBell.AccessibleName = "Open platform audit events";
            btnBell.Click += (_, _) => NavigateTo(_btnAudit);
            UiRadiusHelper.StyleButton(btnBell, 8);
            _header.Controls.Add(btnBell);

            void LayoutHeader()
            {
                _avatarHeader.Location = new Point(_header.Width - 200, 12);
                btnBell.Location = new Point(_header.Width - 246, 14);
                int availableSearch = btnBell.Left - 20 - 170;
                pnlSearchContainer.Width = Math.Clamp(availableSearch, 180, 360);
                _txtHeaderSearch.Width = pnlSearchContainer.Width - 50;
            }

            _header.SizeChanged += (_, _) => LayoutHeader();
            LayoutHeader();
        }

        // ──────────────────────────────────────────────────────────────────────
        // NAVIGATION
        // ──────────────────────────────────────────────────────────────────────

        private void NavigateTo(Button navBtn)
        {
            var prevBtn = _activeNavBtn;
            _activeNavBtn = navBtn;

            if (prevBtn != null)
            {
                prevBtn.Invalidate();
            }
            navBtn.Invalidate();


            _mainPanel.Controls.Clear();

            UserControl view;
            if (navBtn == _btnDashboard)
            {
                if (_dashboardView == null)
                {
                    _dashboardView = new SuperAdminDashboardView();
                    _dashboardView.NavigateToTenants += () => NavigateTo(_btnTenants);
                    _dashboardView.NavigateToSubscriptions += () => NavigateTo(_btnSubscriptions);
                    _dashboardView.NavigateToBackups += () => NavigateTo(_btnBackups);
                }
                view = _dashboardView;
            }
            else if (navBtn == _btnAdministrators)
            {
                _administratorsView ??= new AdministratorsView();
                view = _administratorsView;
            }
            else if (navBtn == _btnSubscriptions)
            {
                _subscriptionsView ??= new SubscriptionsView();
                view = _subscriptionsView;
            }
            else if (navBtn == _btnReports)
            {
                _reportsView ??= new AdminPanelMasterView(openReports: true);
                view = _reportsView;
            }
            else if (navBtn == _btnTerms)
            {
                _termsView ??= new AdminPanelMasterView(openTerms: true);
                view = _termsView;
            }
            else if (navBtn == _btnAudit)
            {
                _auditView ??= new PlatformAuditLogView();
                view = _auditView;
            }
            else if (navBtn == _btnSyncHealth)
            {
                _syncHealthView ??= new SyncHealthView();
                view = _syncHealthView;
            }
            else if (navBtn == _btnSystemSettings)
            {
                _systemSettingsView ??= new SystemSettingsView();
                view = _systemSettingsView;
            }
            else if (navBtn == _btnTenants)
            {
                if (_tenantsView == null)
                {
                    _tenantsView = new TenantsView();
                    _tenantsView.NavigateToSubscription += (companyId) =>
                    {
                        NavigateTo(_btnSubscriptions);
                    };
                }
                view = _tenantsView;
            }
            else
            {
                _backupsView ??= new BackupsView();
                view = _backupsView;
            }

            view.Dock = DockStyle.Fill;
            _mainPanel.Controls.Add(view);
        }

        // ──────────────────────────────────────────────────────────────────────
        // SIGN OUT
        // ──────────────────────────────────────────────────────────────────────

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                foreach (var view in new UserControl?[] { _dashboardView, _administratorsView, _subscriptionsView,
                    _reportsView, _termsView, _auditView, _syncHealthView, _systemSettingsView, _backupsView, _tenantsView })
                    view?.Dispose();
            }
            base.Dispose(disposing);
        }

        private void LnkSignOut_Click(object? sender, EventArgs e)
        {
            var confirm = MessageBox.Show(
                "Sign out of the Super Admin Console?",
                "Sign Out",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Question);

            if (confirm != DialogResult.Yes) return;

            CurrentSession.SignOut();

            var loginForm = _loginForm ?? Application.OpenForms.OfType<LoginForm>().FirstOrDefault();
            if (loginForm != null)
            {
                loginForm.PrepareForLogout();
            }
            else
            {
                Application.Exit();
                return;
            }

            Close();
        }
    }
}
