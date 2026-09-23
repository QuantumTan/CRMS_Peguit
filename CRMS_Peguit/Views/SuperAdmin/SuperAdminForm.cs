using System;
using System.Drawing;
using System.Windows.Forms;
using CRMS_Peguit.winforms.Auth;
using CRMS_Peguit.winforms.Controls;
using CRMS_Peguit.winforms.Models.Services;
using CRMS_Peguit.winforms.Views.SuperAdmin;

// =============================================================================
// SuperAdminForm — Completely separate shell from MainForm.
// Structure:
//   Form
//     ├── sidebarPanel (Dock = Left, Width = 240)
//     │     ├── pnlBrand (Dock = Top, Height = 68)
//     │     ├── pnlNav (Dock = Fill, AutoScroll = true)
//     │     └── pnlProfile (Dock = Bottom, Height = 76)
//     └── contentWrapperPanel (Dock = Fill)
//           ├── topHeaderPanel (Dock = Top, Height = 60)
//           └── mainPanel (Dock = Fill) -> hosts child UserControls
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
        private Button _btnSubscriptions = null!;
        private Button _btnSystemSettings = null!;
        private Button _btnBackups = null!;
        private Button? _activeNavBtn;

        // ── Header Controls ──────────────────────────────────────────────────
        private AvatarLabel _avatarHeader = null!;
        private Label _lblHeaderBreadcrumb = null!;

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
            Text = "NEXA — Super Admin Console";
            Size = new Size(1360, 840);
            MinimumSize = new Size(1100, 720);
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
                BackColor = Theme.SidebarBackground
            };

            // Brand header (Dock = Top)
            var pnlBrand = new Panel
            {
                Dock = DockStyle.Top,
                Height = 68,
                BackColor = Color.FromArgb(10, 17, 34),
                Padding = new Padding(20, 14, 20, 14)
            };

            var lblBrand = new Label
            {
                Text = "⚡ NEXA",
                Font = new Font("Segoe UI", 15f, FontStyle.Bold),
                ForeColor = Color.White,
                AutoSize = true,
                Location = new Point(18, 12)
            };

            var lblRole = new Label
            {
                Text = "Super Admin Console",
                Font = new Font("Segoe UI", 8.5f, FontStyle.Bold),
                ForeColor = Theme.SidebarAccent,
                AutoSize = true,
                Location = new Point(20, 38)
            };

            pnlBrand.Controls.Add(lblRole);
            pnlBrand.Controls.Add(lblBrand);

            // Profile card at bottom (Dock = Bottom)
            var pnlProfile = new Panel
            {
                Dock = DockStyle.Bottom,
                Height = 76,
                BackColor = Theme.SidebarProfileCard,
                Padding = new Padding(16, 10, 16, 8)
            };

            string adminName = CurrentSession.CurrentUser?.FullName ?? "Super Admin";
            string adminEmail = CurrentSession.CurrentUser?.Email ?? string.Empty;

            var avatarProfile = new AvatarLabel(adminName, adminEmail)
            {
                Location = new Point(16, 8),
                Size = new Size(208, 38),
                AvatarSize = 32
            };
            pnlProfile.Controls.Add(avatarProfile);

            var lnkSignOut = new Label
            {
                Text = "⎋  Sign Out",
                Font = new Font("Segoe UI", 8.5f, FontStyle.Bold),
                ForeColor = Color.FromArgb(203, 213, 225),
                AutoSize = true,
                Cursor = Cursors.Hand,
                Location = new Point(18, 50)
            };
            lnkSignOut.MouseEnter += (_, _) => lnkSignOut.ForeColor = Color.White;
            lnkSignOut.MouseLeave += (_, _) => lnkSignOut.ForeColor = Color.FromArgb(203, 213, 225);
            lnkSignOut.Click += LnkSignOut_Click;
            pnlProfile.Controls.Add(lnkSignOut);

            // Nav container (Dock = Fill)
            _pnlNav = new Panel
            {
                Dock = DockStyle.Fill,
                AutoScroll = true,
                BackColor = Color.Transparent,
                Padding = new Padding(12, 16, 12, 16)
            };

            int y = 14;

            var lblNavSection = new Label
            {
                Text = "PLATFORM MANAGEMENT",
                Font = new Font("Segoe UI", 7.5f, FontStyle.Bold),
                ForeColor = Theme.SidebarTextMuted,
                Location = new Point(12, y),
                Size = new Size(200, 20)
            };
            _pnlNav.Controls.Add(lblNavSection);
            y += 24;

            _btnDashboard = CreateNavButton("⊞", "Dashboard", y);
            y += 50;
            _btnAdministrators = CreateNavButton("👥", "System Users", y);
            y += 50;
            _btnSubscriptions = CreateNavButton("💳", "Subscriptions", y);
            y += 50;
            _btnSystemSettings = CreateNavButton("⚙", "System Settings", y);
            y += 50;
            _btnBackups = CreateNavButton("🗄", "Backups", y);

            _btnDashboard.Click += (_, _) => NavigateTo(_btnDashboard);
            _btnAdministrators.Click += (_, _) => NavigateTo(_btnAdministrators);
            _btnSubscriptions.Click += (_, _) => NavigateTo(_btnSubscriptions);
            _btnSystemSettings.Click += (_, _) => NavigateTo(_btnSystemSettings);
            _btnBackups.Click += (_, _) => NavigateTo(_btnBackups);

            _pnlNav.Controls.Add(_btnDashboard);
            _pnlNav.Controls.Add(_btnAdministrators);
            _pnlNav.Controls.Add(_btnSubscriptions);
            _pnlNav.Controls.Add(_btnSystemSettings);
            _pnlNav.Controls.Add(_btnBackups);

            // Add in proper docking order
            _sidebar.Controls.Add(_pnlNav);     // Dock = Fill
            _sidebar.Controls.Add(pnlProfile); // Dock = Bottom
            _sidebar.Controls.Add(pnlBrand);   // Dock = Top
        }

        private Button CreateNavButton(string icon, string label, int y)
        {
            var btn = new Button
            {
                Text = $"   {icon}    {label}",
                Location = new Point(10, y),
                Size = new Size(216, 42),
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 9.5f, FontStyle.Regular),
                ForeColor = Theme.SidebarText,
                BackColor = Color.Transparent,
                TextAlign = ContentAlignment.MiddleLeft,
                Cursor = Cursors.Hand,
                Padding = new Padding(12, 0, 0, 0),
                Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right
            };
            btn.FlatAppearance.BorderSize = 0;
            btn.FlatAppearance.MouseOverBackColor = Theme.SidebarHover;
            btn.FlatAppearance.MouseDownBackColor = Theme.SidebarSelected;
            UiRadiusHelper.StyleButton(btn, 6);
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
                Height = 60,
                BackColor = Theme.Surface,
                Padding = new Padding(28, 0, 28, 0)
            };

            _header.Paint += (s, e) =>
            {
                using var p = new Pen(Theme.Border, 1f);
                e.Graphics.DrawLine(p, 0, _header.Height - 1, _header.Width, _header.Height - 1);
            };

            _lblHeaderBreadcrumb = new Label
            {
                Text = "Platform Overview  /  Dashboard",
                Font = new Font("Segoe UI", 12f, FontStyle.Bold),
                ForeColor = Theme.TextPrimary,
                AutoSize = true,
                Location = new Point(28, 18)
            };
            _header.Controls.Add(_lblHeaderBreadcrumb);

            string adminName = CurrentSession.CurrentUser?.FullName ?? "Super Admin";
            _avatarHeader = new AvatarLabel(adminName, "Platform Administrator")
            {
                Size = new Size(220, 42),
                Anchor = AnchorStyles.Top | AnchorStyles.Right,
                AvatarSize = 32
            };
            _avatarHeader.Location = new Point(_header.Width - 248, 9);
            _header.SizeChanged += (_, _) =>
                _avatarHeader.Location = new Point(_header.Width - 248, 9);
            _header.Controls.Add(_avatarHeader);
        }

        // ──────────────────────────────────────────────────────────────────────
        // NAVIGATION
        // ──────────────────────────────────────────────────────────────────────

        private void NavigateTo(Button navBtn)
        {
            // Reset previous button
            if (_activeNavBtn != null)
            {
                _activeNavBtn.BackColor = Color.Transparent;
                _activeNavBtn.ForeColor = Theme.SidebarText;
                _activeNavBtn.Font = new Font("Segoe UI", 9.5f, FontStyle.Regular);
            }

            // Highlight new active button
            navBtn.BackColor = Theme.SidebarSelected;
            navBtn.ForeColor = Theme.SidebarTextActive;
            navBtn.Font = new Font("Segoe UI", 9.5f, FontStyle.Bold);
            _activeNavBtn = navBtn;

            // Swap view
            _mainPanel.Controls.Clear();

            UserControl view;
            if (navBtn == _btnDashboard)
            {
                _lblHeaderBreadcrumb.Text = "Platform Overview  /  Dashboard";
                _dashboardView ??= new SuperAdminDashboardView();
                view = _dashboardView;
            }
            else if (navBtn == _btnAdministrators)
            {
                _lblHeaderBreadcrumb.Text = "Platform Administration  /  System Users";
                _administratorsView ??= new AdministratorsView();
                view = _administratorsView;
            }
            else if (navBtn == _btnSubscriptions)
            {
                _lblHeaderBreadcrumb.Text = "Tenant Governance  /  Subscriptions";
                _subscriptionsView ??= new SubscriptionsView();
                view = _subscriptionsView;
            }
            else if (navBtn == _btnSystemSettings)
            {
                _lblHeaderBreadcrumb.Text = "Configuration  /  System Settings";
                _systemSettingsView ??= new SystemSettingsView();
                view = _systemSettingsView;
            }
            else
            {
                _lblHeaderBreadcrumb.Text = "Infrastructure  /  Backups & Recovery";
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

            if (_loginForm != null)
            {
                _loginForm.PrepareForLogout();
            }

            Close();
        }
    }
}
