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
//   - Dashboard (Platform Overview & Aggregate Metrics)
//   - SYSTEM: Admins & Roles, Data & Backup, Settings & Policies, Subscription
//
// DATA BOUNDARY ATTESTATION:
// SuperAdmin has ZERO access to tenant business/operational data (Leads, Deals,
// Customers, Properties, Commissions, or Tenant CRM Reports).
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
        private Button? _activeNavBtn;

        // ── Header Controls ──────────────────────────────────────────────────
        private AvatarLabel _avatarHeader = null!;
        private Label _lblHeaderBreadcrumb = null!;
        private TextBox _txtHeaderSearch = null!;

        // ── Child Views (lazy-loaded) ─────────────────────────────────────────
        private SuperAdminDashboardView? _dashboardView;
        private AdministratorsView? _administratorsView;
        private SubscriptionsView? _subscriptionsView;
        private SystemSettingsView? _systemSettingsView;
        private BackupsView? _backupsView;

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
                Height = 72,
                BackColor = Theme.SidebarBackground,
                Padding = new Padding(20, 0, 20, 0)
            };

            // NEXA triangle logo
            var lblLogoIcon = new Label
            {
                Text = "△",
                Font = new Font("Segoe UI", 16f, FontStyle.Bold),
                ForeColor = Color.White,
                AutoSize = true,
                Location = new Point(20, 22)
            };
            var lblBrand = new Label
            {
                Text = "NEXA",
                Font = new Font("Segoe UI", 13.5f, FontStyle.Bold),
                ForeColor = Color.White,
                AutoSize = true,
                Location = new Point(48, 18)
            };
            var lblCrmSub = new Label
            {
                Text = "CRM SYSTEM",
                Font = new Font("Segoe UI", 7.5f, FontStyle.Bold),
                ForeColor = Theme.SidebarTextMuted,
                AutoSize = true,
                Location = new Point(50, 38)
            };
            pnlBrand.Controls.Add(lblLogoIcon);
            pnlBrand.Controls.Add(lblBrand);
            pnlBrand.Controls.Add(lblCrmSub);

            // ── User Profile Pill (Dock = Top, after brand) ───────────────────
            var pnlProfileWrap = new Panel
            {
                Dock = DockStyle.Top,
                Height = 72,
                BackColor = Theme.SidebarBackground,
                Padding = new Padding(14, 10, 14, 10)
            };

            var pnlUserPill = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Theme.SidebarProfileCard  // Slate 800 #1E293B
            };
            UiRadiusHelper.StyleCard(pnlUserPill, 8);

            string adminName = CurrentSession.CurrentUser?.FullName ?? "Super Admin";
            var avatarUser = new AvatarLabel(adminName, "Super Admin")
            {
                Location = new Point(8, 7),
                Size = new Size(170, 38),
                AvatarSize = 30
            };
            pnlUserPill.Controls.Add(avatarUser);

            // Green online dot
            var pnlDot = new Panel
            {
                Size = new Size(8, 8),
                Location = new Point(190, 22),
                BackColor = Color.FromArgb(16, 185, 129)  // Emerald 500
            };
            UiRadiusHelper.StyleCard(pnlDot, 4);
            pnlUserPill.Controls.Add(pnlDot);
            pnlProfileWrap.Controls.Add(pnlUserPill);

            // ── Sign Out (Dock = Bottom) ──────────────────────────────────────
            var pnlSignOut = new Panel
            {
                Dock = DockStyle.Bottom,
                Height = 56,
                BackColor = Theme.SidebarBackground,
                Padding = new Padding(16, 14, 16, 14)
            };

            var lnkSignOut = new Label
            {
                Text = "⎋   Sign out",
                Font = new Font("Segoe UI", 9.5f),
                ForeColor = Theme.SidebarText,
                AutoSize = true,
                Cursor = Cursors.Hand,
                Location = new Point(18, 14)
            };
            lnkSignOut.MouseEnter += (_, _) => lnkSignOut.ForeColor = Color.White;
            lnkSignOut.MouseLeave += (_, _) => lnkSignOut.ForeColor = Theme.SidebarText;
            lnkSignOut.Click += LnkSignOut_Click;
            pnlSignOut.Controls.Add(lnkSignOut);

            // ── Nav Items (Dock = Fill) ───────────────────────────────────────
            _pnlNav = new Panel
            {
                Dock = DockStyle.Fill,
                AutoScroll = true,
                BackColor = Color.Transparent,
                Padding = new Padding(10, 8, 10, 8)
            };

            int y = 6;

            // Dashboard
            _btnDashboard = CreateNavButton("⊞", "Dashboard", y);
            _btnDashboard.Click += (_, _) => NavigateTo(_btnDashboard);
            _pnlNav.Controls.Add(_btnDashboard);
            y += 46;

            // Section: SYSTEM
            y += 4;
            _pnlNav.Controls.Add(CreateSectionHeader("SYSTEM", y));
            y += 22;

            _btnAdministrators = CreateNavButton("🛡", "Admins & Roles", y);
            _btnAdministrators.Click += (_, _) => NavigateTo(_btnAdministrators);
            _pnlNav.Controls.Add(_btnAdministrators);
            y += 46;

            _btnBackups = CreateNavButton("🗄", "Data & Backup", y);
            _btnBackups.Click += (_, _) => NavigateTo(_btnBackups);
            _pnlNav.Controls.Add(_btnBackups);
            y += 46;

            _btnSystemSettings = CreateNavButton("⚙", "Settings & Policies", y);
            _btnSystemSettings.Click += (_, _) => NavigateTo(_btnSystemSettings);
            _pnlNav.Controls.Add(_btnSystemSettings);
            y += 46;

            _btnSubscriptions = CreateNavButton("💳", "Subscription", y);
            _btnSubscriptions.Click += (_, _) => NavigateTo(_btnSubscriptions);
            _pnlNav.Controls.Add(_btnSubscriptions);

            // Correct docking order: Fill first, then Bottom, then Top panels
            _sidebar.Controls.Add(_pnlNav);
            _sidebar.Controls.Add(pnlSignOut);
            _sidebar.Controls.Add(pnlProfileWrap);
            _sidebar.Controls.Add(pnlBrand);
        }

        private static Label CreateSectionHeader(string title, int y)
        {
            return new Label
            {
                Text = title,
                Font = new Font("Segoe UI", 7.5f, FontStyle.Bold),
                ForeColor = Theme.SidebarTextMuted,   // Slate 500 — matches tenant
                Location = new Point(16, y),
                Size = new Size(200, 18)
            };
        }

        private Button CreateNavButton(string icon, string label, int y)
        {
            var btn = new Button
            {
                Text = $"   {icon,-2}   {label}",
                Location = new Point(8, y),
                Size = new Size(220, 42),
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 9.5f, FontStyle.Regular),
                ForeColor = Theme.SidebarText,
                BackColor = Color.Transparent,
                TextAlign = ContentAlignment.MiddleLeft,
                Cursor = Cursors.Hand,
                Padding = new Padding(16, 0, 0, 0)
            };
            btn.FlatAppearance.BorderSize = 0;
            btn.FlatAppearance.MouseOverBackColor = Theme.SidebarHover;
            btn.FlatAppearance.MouseDownBackColor = Theme.SidebarSelected;
            UiRadiusHelper.StyleButton(btn, 6);

            // Fluent-style left accent bar — identical to MainForm tenant nav
            btn.Paint += (s, e) =>
            {
                if (s is Button b && b == _activeNavBtn)
                {
                    e.Graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
                    using var accentBrush = new SolidBrush(Theme.SidebarAccent);  // Sky 400 #38BDF8
                    using var accentPath = UiRadiusHelper.CreateRoundedPath(
                        new Rectangle(4, 7, 3, b.Height - 14), 2);
                    e.Graphics.FillPath(accentBrush, accentPath);
                }
            };

            btn.MouseEnter += (s, e) =>
            {
                if (s is Button b && b != _activeNavBtn)
                    b.BackColor = Theme.SidebarHover;
            };
            btn.MouseLeave += (s, e) =>
            {
                if (s is Button b && b != _activeNavBtn)
                    b.BackColor = Color.Transparent;
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

            // Left Pill: Super Admin indicator
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

            // Middle Search Bar
            _txtHeaderSearch = new TextBox
            {
                PlaceholderText = "🔍  Search tenants, administrators, policies...",
                Font = new Font("Segoe UI", 9.5f),
                Size = new Size(340, 32),
                Location = new Point(170, 16)
            };
            _header.Controls.Add(_txtHeaderSearch);

            // Breadcrumb (Internal Tracking)
            _lblHeaderBreadcrumb = new Label
            {
                Visible = false
            };
            _header.Controls.Add(_lblHeaderBreadcrumb);

            // Right: User Avatar + Notification bell
            string adminName = CurrentSession.CurrentUser?.FullName ?? "Alex Kim";
            _avatarHeader = new AvatarLabel(adminName, "Super Admin")
            {
                Size = new Size(160, 42),
                Anchor = AnchorStyles.Top | AnchorStyles.Right,
                AvatarSize = 34
            };
            _avatarHeader.Location = new Point(_header.Width - 180, 11);
            _header.SizeChanged += (_, _) =>
                _avatarHeader.Location = new Point(_header.Width - 180, 11);
            _header.Controls.Add(_avatarHeader);

            // Notification Bell
            var btnBell = new Button
            {
                Text = "🔔",
                Font = new Font("Segoe UI", 11f),
                Size = new Size(34, 34),
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.Transparent,
                ForeColor = Theme.TextSecondary,
                Cursor = Cursors.Hand,
                Anchor = AnchorStyles.Top | AnchorStyles.Right,
                Location = new Point(_header.Width - 224, 15)
            };
            btnBell.FlatAppearance.BorderSize = 0;
            _header.SizeChanged += (_, _) =>
                btnBell.Location = new Point(_header.Width - 224, 15);
            _header.Controls.Add(btnBell);
        }

        // ──────────────────────────────────────────────────────────────────────
        // NAVIGATION
        // ──────────────────────────────────────────────────────────────────────

        private void NavigateTo(Button navBtn)
        {
            // Deactivate previous
            if (_activeNavBtn != null)
            {
                _activeNavBtn.BackColor = Color.Transparent;
                _activeNavBtn.ForeColor = Theme.SidebarText;
                _activeNavBtn.Font = new Font("Segoe UI", 9.5f, FontStyle.Regular);
                _activeNavBtn.Invalidate();
            }

            // Activate new — matches tenant SetActiveNavButton
            navBtn.BackColor = Theme.SidebarSelected;   // Slate 800 #1E293B
            navBtn.ForeColor = Theme.SidebarTextActive; // White
            navBtn.Font = new Font("Segoe UI", 9.5f, FontStyle.Bold);
            _activeNavBtn = navBtn;
            navBtn.Invalidate(); // triggers Paint for the accent bar


            _mainPanel.Controls.Clear();

            UserControl view;
            if (navBtn == _btnDashboard)
            {
                _dashboardView ??= new SuperAdminDashboardView();
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
            else if (navBtn == _btnSystemSettings)
            {
                _systemSettingsView ??= new SystemSettingsView();
                view = _systemSettingsView;
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
