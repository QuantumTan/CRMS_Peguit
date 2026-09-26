using CRMS_Peguit.winforms.Auth;
using CRMS_Peguit.winforms.Views.Dashboard;
using CRMS_Peguit.winforms.Views.Customers;
using CRMS_Peguit.winforms.Views.Leads;
using CRMS_Peguit.winforms.Views.Properties;
using CRMS_Peguit.winforms.Views.Deals;
using CRMS_Peguit.winforms.Views.Marketing;
using CRMS_Peguit.winforms.Views.Shared;
using CRMS_Peguit.winforms.Views.Users;
using CRMS_Peguit.winforms.Views.FollowUps;
using CRMS_Peguit.Models;
using CRMS_Peguit.winforms.Models.Services;
using CRMS_Peguit.winforms.Services;
using CRMS_Peguit.winforms.Services.Offline;
using ReaLTaiizor.Forms;
using System.Linq;
using UserRole = CRMS_Peguit.winforms.Models.Roles.UserRole;
using TenantTier = CRMS_Peguit.domain.entities.TenantTier;

namespace CRMS_Peguit.winforms
{
    public partial class MainForm : Form
    {
        private Button? _activeNavButton;
        private readonly List<Button> _navButtons = new();
        private readonly Dictionary<Button, (string Icon, string Title)> _navButtonInfo = new();
        private bool _sidebarCollapsed = false;
        private System.Windows.Forms.Timer? _sidebarAnimationTimer;
        private int _targetSidebarWidth = 240;

        // Global search debounce + floating dropdown
        private System.Windows.Forms.Timer? _searchDebounce;
        private ToolStripDropDown? _searchDropDown;
        private Panel? _pnlSearchBox;

        // Data Synchronization Indicator & Banner
        private Button _btnSyncIndicator = null!;
        private Panel _pnlOfflineBanner = null!;
        private Label _lblOfflineBannerText = null!;
        private Button _btnBannerSyncQueue = null!;
        private Button _btnBannerRetry = null!;

        public MainForm()
        {
            InitializeComponent();
            ApplyBranding();
            ApplyTheme();
            InitNavButtons();
            BindEvents();
            ApplyRolePermissions();
            InitSyncUi();
            if (CurrentSession.CurrentUser?.Role == UserRole.SuperAdmin)
            {
                SetActiveNavButton(btnAdminPanel);
                BtnAdminPanelClick(btnAdminPanel, EventArgs.Empty);
            }
            else
            {
                SetActiveNavButton(btnDashboard);
                BtnDashboardClick(btnDashboard, EventArgs.Empty);
            }

            // Start automated market update background worker
            MarketUpdateBackgroundService.Instance.Start();
        }

        private void ApplyBranding()
        {
            AppBrand.ApplyAppIcon(this);
            if (AppBrand.Logo != null)
            {
                picLogo.Image = AppBrand.Logo;
            }

            pnlLogoHeader.Cursor = Cursors.Hand;
            picLogo.Cursor = Cursors.Hand;
            lblLogo.Cursor = Cursors.Hand;
            EventHandler navDashboard = (s, e) => { SetActiveNavButton(btnDashboard); BtnDashboardClick(s, e); };
            picLogo.Click += navDashboard;
            lblLogo.Click += navDashboard;
            pnlLogoHeader.Click += navDashboard;
            mainToolTip.SetToolTip(pnlLogoHeader, "NEXA CRM SYSTEM — Go to Dashboard");
        }

        private void InitNavButtons()
        {
            _navButtonInfo[btnDashboard] = ("⊞", "Dashboard");
            _navButtonInfo[btnAdminPanel] = ("👑", "Admin Panel");
            _navButtonInfo[btnLeads] = ("◎", "Leads");
            _navButtonInfo[btnCustomers] = ("👥", "Customers");
            _navButtonInfo[btnProperties] = ("🏢", "Properties");
            _navButtonInfo[btnBranching] = ("🏢", "Branches");
            _navButtonInfo[btnDeals] = ("💼", "Deals");
            _navButtonInfo[btnCampaigns] = ("📣", "Campaigns");
            _navButtonInfo[btnActivities] = ("📈", "Activities");
            _navButtonInfo[btnFollowUps] = ("⏱", "Follow-Ups");
            _navButtonInfo[btnSupportTickets] = ("🎟", "Support Tickets");
            _navButtonInfo[btnAnalytics] = ("📊", "Analytics");
            _navButtonInfo[btnReports] = ("📋", "Reports & Exports");
            _navButtonInfo[btnApprovals] = ("✓", "Approvals & Review");
            _navButtonInfo[btnManageManagers] = ("🛡", "Manage Managers");
            _navButtonInfo[btnManageAgents] = ("👥", "Manage Agents");
            _navButtonInfo[btnArchives] = ("🗑", "Recycle Bin");

            _navButtons.AddRange(_navButtonInfo.Keys);

            foreach (var btn in _navButtons)
            {
                UiRadiusHelper.StyleButton(btn, 6);
                var info = _navButtonInfo[btn];
                mainToolTip.SetToolTip(btn, info.Title);
                btn.Text = $"   {info.Icon,-2}   {info.Title}";
                btn.Padding = new Padding(16, 0, 0, 0);
                btn.TextAlign = ContentAlignment.MiddleLeft;
                btn.ForeColor = Theme.SidebarText;

                btn.Paint += (s, e) =>
                {
                    if (s is Button b && b == _activeNavButton)
                    {
                        e.Graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
                        // Sleek Fluent-style left indicator accent line
                        using var accentBrush = new SolidBrush(Theme.SidebarAccent);
                        using var accentPath = UiRadiusHelper.CreateRoundedPath(new Rectangle(4, 7, 3, b.Height - 14), 2);
                        e.Graphics.FillPath(accentBrush, accentPath);
                    }
                };

                btn.MouseEnter += (s, e) =>
                {
                    if (s is Button b && b != _activeNavButton)
                        b.BackColor = Theme.SidebarHover;
                };
                btn.MouseLeave += (s, e) =>
                {
                    if (s is Button b && b != _activeNavButton)
                        b.BackColor = Color.Transparent;
                };
            }
        }

        private void SetActiveNavButton(Button button)
        {
            _activeNavButton = button;
            foreach (var btn in _navButtons)
            {
                if (btn == button)
                {
                    btn.BackColor = Theme.SidebarSelected;
                    btn.ForeColor = Theme.SidebarTextActive;
                    btn.Font = new Font("Segoe UI", 9.5f, FontStyle.Bold);
                }
                else
                {
                    btn.BackColor = Color.Transparent;
                    btn.ForeColor = Theme.SidebarText;
                    btn.Font = new Font("Segoe UI", 9.5f, FontStyle.Regular);
                }
                btn.Invalidate();
            }
        }

        private void BindEvents()
        {
            btnToggleSidebar.Click += (_, _) => ToggleSidebar();
            mainToolTip.SetToolTip(btnToggleSidebar, "Toggle Sidebar (Ctrl+B)");
            mainToolTip.SetToolTip(notificationBell, "System Status & Notifications");
            notificationBell.NavigationRequested += m => NavigateTo(m);
            notificationBell.Initialize();
            mainToolTip.SetToolTip(txtGlobalSearch, "Global search (Ctrl+K or Ctrl+F)");

            btnLogout.Click += BtnLogoutClick;
            btnDashboard.Click += (s, e) => { SetActiveNavButton(btnDashboard); BtnDashboardClick(s, e); };
            btnManageManagers.Click += (s, e) => { SetActiveNavButton(btnManageManagers); BtnManageManagersClick(s, e); };
            btnManageAgents.Click += (s, e) => { SetActiveNavButton(btnManageAgents); BtnManageAgentsClick(s, e); };
            btnArchives.Click += (s, e) => { SetActiveNavButton(btnArchives); BtnArchivesClick(s, e); };
            btnCustomers.Click += (s, e) => { SetActiveNavButton(btnCustomers); BtnCustomersClick(s, e); };
            btnLeads.Click += (s, e) => { SetActiveNavButton(btnLeads); BtnLeadsClick(s, e); };
            btnProperties.Click += (s, e) => { SetActiveNavButton(btnProperties); BtnPropertiesClick(s, e); };
            btnDeals.Click += (s, e) => { SetActiveNavButton(btnDeals); BtnDealsClick(s, e); };
            btnCampaigns.Click += (s, e) => { SetActiveNavButton(btnCampaigns); BtnCampaignsClick(s, e); };
            btnActivities.Click += (s, e) => { SetActiveNavButton(btnActivities); BtnActivitiesClick(s, e); };
            btnFollowUps.Click += (s, e) => { SetActiveNavButton(btnFollowUps); BtnFollowUpsClick(s, e); };
            btnAnalytics.Click += (s, e) => { SetActiveNavButton(btnAnalytics); BtnAnalyticsClick(s, e); };
            btnReports.Click += (s, e) => { SetActiveNavButton(btnReports); BtnReportsClick(s, e); };
            btnApprovals.Click += (s, e) => { SetActiveNavButton(btnApprovals); BtnApprovalsClick(s, e); };
            btnSupportTickets.Click += (s, e) => { SetActiveNavButton(btnSupportTickets); BtnSupportTicketsClick(s, e); };
            btnAdminPanel.Click += (s, e) => { SetActiveNavButton(btnAdminPanel); BtnAdminPanelClick(s, e); };
            btnBranching.Click += (s, e) => { SetActiveNavButton(btnBranching); BtnBranchingClick(s, e); };

            // ── Global Search: debounced TextChanged → floating results dropdown ──
            _searchDebounce = new System.Windows.Forms.Timer { Interval = 300 };
            _searchDebounce.Tick += (_, _) =>
            {
                _searchDebounce.Stop();
                ShowGlobalSearchResults();
            };

            txtGlobalSearch.TextChanged += (_, _) =>
            {
                _searchDebounce.Stop();
                if (string.IsNullOrWhiteSpace(txtGlobalSearch.Text))
                {
                    DismissSearchDropDown();
                    return;
                }
                _searchDebounce.Start();
            };

            txtGlobalSearch.KeyDown += (_, e) =>
            {
                if (e.KeyCode == Keys.Escape)
                {
                    DismissSearchDropDown();
                    e.Handled = true;
                    e.SuppressKeyPress = true;
                }
                else if (e.KeyCode == Keys.Enter && _searchDropDown?.Items.Count > 0)
                {
                    // Activate top result
                    (_searchDropDown.Items[0] as ToolStripMenuItem)?.PerformClick();
                    e.Handled = true;
                    e.SuppressKeyPress = true;
                }
            };

            txtGlobalSearch.Leave += (_, _) =>
            {
                // Small delay so clicking a result registers before the dropdown closes
                Task.Delay(200).ContinueWith(_ => BeginInvoke(DismissSearchDropDown));
            };

        }

        private void ToggleSidebar()
        {
            _sidebarCollapsed = !_sidebarCollapsed;
            _targetSidebarWidth = _sidebarCollapsed ? 64 : 240;

            if (_sidebarCollapsed)
            {
                lblLogo.Visible = false;
                lblSalesSection.Visible = false;
                lblSupportSection.Visible = false;
                lblInsightsSection.Visible = false;
                lblAdminSection.Visible = false;
                foreach (var btn in _navButtons)
                {
                    if (_navButtonInfo.TryGetValue(btn, out var info))
                    {
                        btn.Text = info.Icon;
                        btn.Padding = new Padding(0);
                        btn.TextAlign = ContentAlignment.MiddleCenter;
                    }
                }
                btnLogout.Text = "↪";
                btnLogout.Padding = new Padding(0);
                btnLogout.TextAlign = ContentAlignment.MiddleCenter;
                mainToolTip.SetToolTip(btnLogout, "Sign out");
                mainToolTip.SetToolTip(picLogo, "NEXA CRM SYSTEM");
            }

            _sidebarAnimationTimer?.Stop();
            _sidebarAnimationTimer?.Dispose();
            _sidebarAnimationTimer = new System.Windows.Forms.Timer { Interval = 10 };
            _sidebarAnimationTimer.Tick += (_, _) =>
            {
                int diff = _targetSidebarWidth - sidebarPanel.Width;
                int step = Math.Sign(diff) * Math.Max(16, Math.Abs(diff) / 2);
                if (Math.Abs(diff) <= Math.Abs(step))
                {
                    sidebarPanel.Width = _targetSidebarWidth;
                    _sidebarAnimationTimer.Stop();
                    _sidebarAnimationTimer.Dispose();
                    _sidebarAnimationTimer = null;
                    FinalizeSidebarState();
                }
                else
                {
                    sidebarPanel.Width += step;
                }
            };
            _sidebarAnimationTimer.Start();
        }

        private void FinalizeSidebarState()
        {
            if (_sidebarCollapsed)
            {
                picLogo.Location = new Point((64 - picLogo.Width) / 2, 9);
            }
            else
            {
                picLogo.Location = new Point(14, 9);
                lblLogo.Visible = true;
                mainToolTip.SetToolTip(picLogo, null);
                lblSalesSection.Visible = true;
                lblSupportSection.Visible = true;
                lblInsightsSection.Visible = btnAnalytics.Visible || btnReports.Visible;
                lblAdminSection.Visible = btnManageManagers.Visible || btnManageAgents.Visible || btnApprovals.Visible || btnAdminPanel.Visible || btnArchives.Visible;
                foreach (var btn in _navButtons)
                {
                    if (_navButtonInfo.TryGetValue(btn, out var info))
                    {
                        btn.Text = $"   {info.Icon,-2}   {info.Title}";
                        btn.Padding = new Padding(16, 0, 0, 0);
                        btn.TextAlign = ContentAlignment.MiddleLeft;
                    }
                }
                btnLogout.Text = "   ↪   Sign out";
                btnLogout.Padding = new Padding(16, 0, 0, 0);
                btnLogout.TextAlign = ContentAlignment.MiddleLeft;
                mainToolTip.SetToolTip(btnLogout, null);
            }
        }

        private void ShowNotificationMenu()
        {
            notificationBell.ShowNotificationDropdown();
        }

        protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
        {
            if (keyData == (Keys.Control | Keys.B))
            {
                ToggleSidebar();
                return true;
            }
            if (keyData == (Keys.Control | Keys.K) || keyData == (Keys.Control | Keys.F))
            {
                txtGlobalSearch.Focus();
                txtGlobalSearch.SelectAll();
                return true;
            }
            return base.ProcessCmdKey(ref msg, keyData);
        }

        // =====================================================
        // GLOBAL SEARCH — floating results dropdown
        // =====================================================

        private void ShowGlobalSearchResults()
        {
            string query = txtGlobalSearch.Text.Trim();
            if (string.IsNullOrWhiteSpace(query)) return;

            DismissSearchDropDown();

            var results = GlobalSearchService.Search(query, maxPerModule: 5);

            _searchDropDown = new ToolStripDropDown
            {
                AutoClose = false,
                BackColor = Color.White,
                Padding = new Padding(0, 4, 0, 4)
            };
            _searchDropDown.LayoutStyle = ToolStripLayoutStyle.VerticalStackWithOverflow;

            if (results.Count == 0)
            {
                var emptyItem = new ToolStripMenuItem($"🔍  No results found for \"{query}\"")
                {
                    Enabled = false,
                    Font = new Font("Segoe UI", 9.5f),
                    ForeColor = Color.FromArgb(100, 116, 139),
                    BackColor = Color.White
                };
                _searchDropDown.Items.Add(emptyItem);
            }
            else
            {
                string? lastModule = null;
                foreach (var r in results)
                {
                    // Module header separator
                    if (r.Module != lastModule)
                    {
                        if (lastModule != null) _searchDropDown.Items.Add(new ToolStripSeparator());

                        var header = new ToolStripMenuItem(GetModuleLabel(r.Module))
                        {
                            Enabled = false,
                            Font = new Font("Segoe UI", 7.5f, FontStyle.Bold),
                            ForeColor = Color.FromArgb(100, 116, 139),
                            BackColor = Color.White,
                            Padding = new Padding(14, 2, 8, 2)
                        };
                        _searchDropDown.Items.Add(header);
                        lastModule = r.Module;
                    }

                    var result = r; // capture for lambda
                    var item = new ToolStripMenuItem
                    {
                        Text = $"  {result.Icon}  {result.Title}",
                        ToolTipText = result.Subtitle,
                        Font = new Font("Segoe UI", 9.5f),
                        ForeColor = Color.FromArgb(15, 23, 42),
                        BackColor = Color.White,
                        Padding = new Padding(12, 4, 12, 4),
                        AutoToolTip = false
                    };

                    // Show subtitle as secondary text by adding a label
                    var subtitleItem = new ToolStripMenuItem
                    {
                        Text = $"      {result.Subtitle}",
                        Enabled = false,
                        Font = new Font("Segoe UI", 8f),
                        ForeColor = Color.FromArgb(100, 116, 139),
                        BackColor = Color.White,
                        Padding = new Padding(12, 0, 12, 2)
                    };

                    item.Click += (_, _) =>
                    {
                        DismissSearchDropDown();
                        txtGlobalSearch.Clear();
                        NavigateTo(result.Module);
                    };

                    _searchDropDown.Items.Add(item);
                    _searchDropDown.Items.Add(subtitleItem);
                }
            }

            // Footer hint
            _searchDropDown.Items.Add(new ToolStripSeparator());
            var hint = new ToolStripMenuItem("↑↓ navigate · Enter to open · Esc to close")
            {
                Enabled = false,
                Font = new Font("Segoe UI", 7.5f),
                ForeColor = Color.FromArgb(148, 163, 184),
                BackColor = Color.White,
                Padding = new Padding(12, 2, 8, 2)
            };
            _searchDropDown.Items.Add(hint);

            // Style the dropdown container
            _searchDropDown.Width = Math.Max(txtGlobalSearch.Width, 340);

            var screenPt = txtGlobalSearch.PointToScreen(new Point(0, txtGlobalSearch.Height + 2));
            _searchDropDown.Show(screenPt);
        }

        private static string GetModuleLabel(string module) => module switch
        {
            "customers" => "CUSTOMERS",
            "leads"     => "LEADS",
            "properties"=> "PROPERTIES",
            "deals"     => "DEALS",
            _           => module.ToUpper()
        };

        private void DismissSearchDropDown()
        {
            _searchDropDown?.Close();
            _searchDropDown?.Dispose();
            _searchDropDown = null;
        }

        private void ApplyTheme()
        {
            sidebarPanel.BackColor = Theme.SidebarBackground;
            topHeaderPanel.BackColor = Theme.HeaderBackground;
            mainPanel.BackColor = Theme.Background;
            BackColor = Theme.Background;
            lblHeaderUserName.ForeColor = Theme.TextPrimary;
            lblHeaderAvatar.BackColor = AzureTints.SkylineBlue;
            lblHeaderAvatar.ForeColor = AzureTints.PureWhite;
            notificationBell.Invalidate();

            // WCAG AA Compliant Section Headings (≥ 4.5:1 on dark sidebar)
            lblSalesSection.ForeColor = Theme.SidebarTextMuted;
            lblSupportSection.ForeColor = Theme.SidebarTextMuted;
            lblInsightsSection.ForeColor = Theme.SidebarTextMuted;
            lblAdminSection.ForeColor = Theme.SidebarTextMuted;

            UiRadiusHelper.MakeCircularAvatar(lblHeaderAvatar);
            UiRadiusHelper.StyleButton(btnLogout, 6);
            UiRadiusHelper.StyleButton(btnToggleSidebar, 6);

            // Modernize Global Search container with integrated search icon and Ctrl+K shortcut badge
            _pnlSearchBox = new Panel
            {
                Size = new Size(340, 34),
                Location = new Point(btnToggleSidebar.Right + 12, (topHeaderPanel.Height - 34) / 2),
                BackColor = Color.FromArgb(241, 245, 249),
                Cursor = Cursors.IBeam
            };
            UiRadiusHelper.ApplyRoundedCorners(_pnlSearchBox, 8);

            var lblSearchIcon = new Label
            {
                Text = "🔍",
                Font = new Font("Segoe UI Emoji", 9.5f),
                ForeColor = Color.FromArgb(100, 116, 139),
                Size = new Size(22, 22),
                Location = new Point(10, 6),
                TextAlign = ContentAlignment.MiddleCenter,
                Cursor = Cursors.IBeam
            };

            var lblSearchBadge = new Label
            {
                Text = "Ctrl+K",
                Font = new Font("Segoe UI", 7.5f, FontStyle.Bold),
                ForeColor = Color.FromArgb(100, 116, 139),
                BackColor = Color.FromArgb(226, 232, 240),
                Size = new Size(48, 20),
                Location = new Point(340 - 48 - 10, 7),
                TextAlign = ContentAlignment.MiddleCenter,
                Cursor = Cursors.Hand
            };
            UiRadiusHelper.ApplyRoundedCorners(lblSearchBadge, 4);

            topHeaderPanel.Controls.Remove(txtGlobalSearch);

            txtGlobalSearch.BorderStyle = BorderStyle.None;
            txtGlobalSearch.BackColor = Color.FromArgb(241, 245, 249);
            txtGlobalSearch.ForeColor = Theme.TextPrimary;
            txtGlobalSearch.Location = new Point(36, 7);
            txtGlobalSearch.Size = new Size(340 - 36 - 62, 20);
            txtGlobalSearch.PlaceholderText = "Search records, contacts...";

            bool searchFocused = false;
            _pnlSearchBox.Paint += (s, e) =>
            {
                e.Graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
                Color borderClr = searchFocused ? Color.FromArgb(14, 165, 233) : Color.FromArgb(226, 232, 240);
                using var pen = new Pen(borderClr, searchFocused ? 1.5f : 1f);
                using var path = UiRadiusHelper.CreateRoundedPath(new Rectangle(0, 0, _pnlSearchBox.Width - 1, _pnlSearchBox.Height - 1), 8);
                e.Graphics.DrawPath(pen, path);
            };

            txtGlobalSearch.GotFocus += (_, _) => { searchFocused = true; _pnlSearchBox.Invalidate(); };
            txtGlobalSearch.LostFocus += (_, _) => { searchFocused = false; _pnlSearchBox.Invalidate(); };
            _pnlSearchBox.Click += (_, _) => txtGlobalSearch.Focus();
            lblSearchIcon.Click += (_, _) => txtGlobalSearch.Focus();
            lblSearchBadge.Click += (_, _) => txtGlobalSearch.Focus();

            _pnlSearchBox.Controls.Add(lblSearchIcon);
            _pnlSearchBox.Controls.Add(txtGlobalSearch);
            _pnlSearchBox.Controls.Add(lblSearchBadge);
            topHeaderPanel.Controls.Add(_pnlSearchBox);

            topHeaderPanel.Paint += (s, e) =>
            {
                using var pen = new Pen(Color.FromArgb(226, 232, 240), 1f);
                e.Graphics.DrawLine(pen, 0, topHeaderPanel.Height - 1, topHeaderPanel.Width, topHeaderPanel.Height - 1);

                // Soft multi-layered elevation shadow separating header and content
                using var shadowBrush1 = new SolidBrush(Color.FromArgb(12, 0, 0, 0));
                e.Graphics.FillRectangle(shadowBrush1, 0, topHeaderPanel.Height - 3, topHeaderPanel.Width, 1);
                using var shadowBrush2 = new SolidBrush(Color.FromArgb(6, 0, 0, 0));
                e.Graphics.FillRectangle(shadowBrush2, 0, topHeaderPanel.Height - 2, topHeaderPanel.Width, 1);
            };
        }

        // =====================================================
        // ROLE PERMISSIONS
        // =====================================================

        private void ApplyRolePermissions()
        {
            if (CurrentSession.CurrentUser is null)
            {
                MessageBox.Show(
                    "No active session. Please sign in again.",
                    "Session Error",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error
                );
                Close();
                return;
            }

            var user = CurrentSession.CurrentUser;
            string initials = !string.IsNullOrWhiteSpace(user.FullName)
                ? string.Join("", user.FullName.Split(' ', StringSplitOptions.RemoveEmptyEntries).Select(s => s[0])).ToUpper()
                : "U";
            if (initials.Length > 2) initials = initials.Substring(0, 2);

            string roleDisplay = user.Role switch
            {
                CRMS_Peguit.winforms.Models.Roles.UserRole.SuperAdmin => "Super Admin",
                CRMS_Peguit.winforms.Models.Roles.UserRole.Admin => "Admin",
                CRMS_Peguit.winforms.Models.Roles.UserRole.Manager => "Manager",
                CRMS_Peguit.winforms.Models.Roles.UserRole.SalesStaff => "Sales Staff",
                _ => user.Role.ToString()
            };

            lblHeaderAvatar.Text = initials;
            var (avatarBg, avatarFg) = CRMS_Peguit.winforms.Controls.AvatarLabel.GetDeterministicAvatarColors(user.FullName);
            lblHeaderAvatar.BackColor = avatarBg;
            lblHeaderAvatar.ForeColor = avatarFg;
            lblHeaderUserName.Text = $"{user.FullName}\r\n{roleDisplay}";

            if (_pnlSearchBox != null)
            {
                _pnlSearchBox.Left = btnToggleSidebar.Right + 12;
                _pnlSearchBox.Top = (topHeaderPanel.Height - _pnlSearchBox.Height) / 2;
            }

            // ── Tenant Tier Badge (Exam Requirements) ──
            if (CurrentSession.CurrentUser?.Role == UserRole.SuperAdmin)
            {
                lblTenantTierBadge.Text = "👑 Master Admin • Platform Oversight";
                lblTenantTierBadge.BackColor = Color.FromArgb(238, 242, 255);
                lblTenantTierBadge.ForeColor = Color.FromArgb(67, 56, 202);
            }
            else if (CurrentSession.TenantTier == TenantTier.TenantC)
            {
                string branchStr = string.IsNullOrWhiteSpace(CurrentSession.ActiveBranchName) ? "All Branches" : CurrentSession.ActiveBranchName;
                lblTenantTierBadge.Text = $"🏢 Tenant C (Enterprise) • {branchStr}";
                lblTenantTierBadge.BackColor = Color.FromArgb(236, 253, 245);
                lblTenantTierBadge.ForeColor = Color.FromArgb(4, 120, 87);
            }
            else if (CurrentSession.TenantTier == TenantTier.TenantB)
            {
                lblTenantTierBadge.Text = "💼 Tenant B (Professional) • BI & Actions";
                lblTenantTierBadge.BackColor = Color.FromArgb(239, 246, 255);
                lblTenantTierBadge.ForeColor = Color.FromArgb(29, 78, 216);
            }
            else
            {
                lblTenantTierBadge.Text = "📁 Tenant A (Standard) • Transactions & Data";
                lblTenantTierBadge.BackColor = Color.FromArgb(241, 245, 249);
                lblTenantTierBadge.ForeColor = Color.FromArgb(71, 85, 105);
            }
            lblTenantTierBadge.Visible = true;

            // ── Master & Multi-Tenant Navigation Gating ──
            btnAdminPanel.Visible = CurrentSession.CurrentUser?.Role == UserRole.SuperAdmin;
            btnBranching.Visible = CurrentSession.CanAccessBranching;

            btnApprovals.Visible = CurrentSession.CanAccess("Approvals") && RbacService.CanApproveAssignments && CurrentSession.CanAccessActions;
            btnManageManagers.Visible = CurrentSession.CanAccess("Managers");
            btnManageAgents.Visible = CurrentSession.CanAccess("SalesStaff");
            btnArchives.Visible = RbacService.IsAdmin || RbacService.IsManager || RbacService.IsSuperAdmin;
            lblAdminSection.Text = RbacService.IsAdmin ? "ADMINISTRATION" : "MANAGEMENT";
            lblAdminSection.Visible = btnManageManagers.Visible || btnManageAgents.Visible || btnApprovals.Visible || btnAdminPanel.Visible || btnArchives.Visible;

            // Base Tier (Tenant A, B, C): Data Collection & Main Transaction
            btnCustomers.Visible = CurrentSession.CanAccess("Customers");
            btnLeads.Visible = CurrentSession.CanAccess("Leads");
            btnProperties.Visible = CurrentSession.CanAccess("Properties");
            btnDeals.Visible = CurrentSession.CanAccess("Deals");

            // Actions: Gated to Tenant B and Tenant C
            btnCampaigns.Visible = CurrentSession.CanAccess("Campaigns") && CurrentSession.CanAccessActions;
            btnActivities.Visible = CurrentSession.CanAccess("Activities");
            btnFollowUps.Visible = CurrentSession.CanAccess("TasksReminders") && RbacService.IsAgent && CurrentSession.CanAccessActions;

            // Business Intelligence: Gated to Tenant B and Tenant C
            btnAnalytics.Visible = (CurrentSession.CanAccess("Analytics") || CurrentSession.CanAccess("Reports")) && CurrentSession.CanAccessBusinessIntelligence;
            if (RbacService.IsAgent)
            {
                _navButtonInfo[btnAnalytics] = ("📊", "My Performance");
                btnAnalytics.Text = "  📊  My Performance";
            }
            else
            {
                _navButtonInfo[btnAnalytics] = ("📊", "Analytics");
                btnAnalytics.Text = "  📊  Analytics";
            }

            // Reports & Exports: restricted to Admin and Manager only + Tier B/C
            btnReports.Visible = CurrentSession.CanAccess("Reports") && !RbacService.IsAgent && CurrentSession.CanAccessBusinessIntelligence;
            _navButtonInfo[btnReports] = ("📋", "Reports & Exports");
            btnReports.Text = "  📋  Reports & Exports";

            lblInsightsSection.Visible = btnAnalytics.Visible || btnReports.Visible;
            btnSupportTickets.Visible = CurrentSession.CanAccess("SupportTickets");

            Text = $"NEXA CRM SYSTEM — {user.FullName} ({roleDisplay})";
        }

        // =====================================================
        // VIEW MANAGEMENT (Smart View Caching for 0ms transitions)
        // =====================================================

        private readonly Dictionary<string, UserControl> _viewCache = new(StringComparer.OrdinalIgnoreCase);

        private void ShowViewCached(string key, Func<UserControl> factory)
        {
            if (!_viewCache.TryGetValue(key, out var view) || view.IsDisposed)
            {
                view = factory();
                view.Dock = DockStyle.Fill;
                mainPanel.Controls.Add(view);
                _viewCache[key] = view;
            }

            foreach (Control c in mainPanel.Controls)
            {
                if (c != view)
                    c.Visible = false;
            }

            view.Visible = true;
            view.BringToFront();
            view.Focus();
        }

        private void ShowView(UserControl view)
        {
            string key = view.GetType().Name;
            if (_viewCache.TryGetValue(key, out var oldView) && !oldView.IsDisposed)
            {
                mainPanel.Controls.Remove(oldView);
                oldView.Dispose();
            }

            view.Dock = DockStyle.Fill;
            mainPanel.Controls.Add(view);
            _viewCache[key] = view;

            foreach (Control c in mainPanel.Controls)
            {
                if (c != view)
                    c.Visible = false;
            }

            view.Visible = true;
            view.BringToFront();
            view.Focus();
        }

        // =====================================================
        // NAVIGATION
        // =====================================================

        public void NavigateTo(string module)
        {
            NavigateTo(module, null);
        }

        public void NavigateTo(string module, string? initialFilter)
        {
            if (string.IsNullOrWhiteSpace(initialFilter) && module.Contains(':'))
            {
                var parts = module.Split(':', 2);
                module = parts[0].Trim();
                initialFilter = parts[1].Trim();
            }

            switch (module.ToLowerInvariant())
            {
                case "customers":
                    if (!CurrentSession.CanAccess("Customers")) return;
                    SetActiveNavButton(btnCustomers);
                    BtnCustomersClick(btnCustomers, EventArgs.Empty);
                    if (!string.IsNullOrWhiteSpace(initialFilter) && _viewCache.TryGetValue("Customers", out var cv) && cv is CustomersView custView)
                    {
                        custView.SetFilter(initialFilter);
                    }
                    break;
                case "leads":
                    if (!CurrentSession.CanAccess("Leads")) return;
                    SetActiveNavButton(btnLeads);
                    BtnLeadsClick(btnLeads, EventArgs.Empty);
                    if (!string.IsNullOrWhiteSpace(initialFilter) && _viewCache.TryGetValue("Leads", out var lv) && lv is LeadsView leadsView)
                    {
                        leadsView.SetFilter(initialFilter);
                    }
                    break;
                case "properties":
                    if (!CurrentSession.CanAccess("Properties")) return;
                    SetActiveNavButton(btnProperties);
                    BtnPropertiesClick(btnProperties, EventArgs.Empty);
                    break;
                case "deals":
                    if (!CurrentSession.CanAccess("Deals")) return;
                    SetActiveNavButton(btnDeals);
                    BtnDealsClick(btnDeals, EventArgs.Empty);
                    if (!string.IsNullOrWhiteSpace(initialFilter) && _viewCache.TryGetValue("Deals", out var dv) && dv is DealsView dealsView)
                    {
                        dealsView.SetFilter(initialFilter);
                    }
                    break;
                case "campaigns":
                    if (!CurrentSession.CanAccess("Campaigns")) return;
                    SetActiveNavButton(btnCampaigns);
                    BtnCampaignsClick(btnCampaigns, EventArgs.Empty);
                    break;
                case "approvals":
                    if (!CurrentSession.CanAccess("Approvals") && !RbacService.CanApproveAssignments) return;
                    SetActiveNavButton(btnApprovals);
                    BtnApprovalsClick(btnApprovals, EventArgs.Empty);
                    break;
                case "supporttickets":
                case "tickets":
                    if (!CurrentSession.CanAccess("SupportTickets")) return;
                    SetActiveNavButton(btnSupportTickets);
                    BtnSupportTicketsClick(btnSupportTickets, EventArgs.Empty);
                    if (!string.IsNullOrWhiteSpace(initialFilter) && _viewCache.TryGetValue("SupportTickets", out var stv) && stv is CRMS_Peguit.winforms.Views.SupportTickets.SupportTicketsView ticketsView)
                    {
                        ticketsView.SetFilter(initialFilter);
                    }
                    break;
                case "followups":
                case "tasksreminders":
                case "reminders":
                    if (!CurrentSession.CanAccess("TasksReminders") || !RbacService.IsAgent) return;
                    SetActiveNavButton(btnFollowUps);
                    BtnFollowUpsClick(btnFollowUps, EventArgs.Empty);
                    if (!string.IsNullOrWhiteSpace(initialFilter) && _viewCache.TryGetValue("FollowUps", out var fv) && fv is FollowUpsView fuView)
                    {
                        fuView.SetFilter(initialFilter);
                    }
                    break;
                case "archives":
                case "recyclebin":
                case "archive":
                    if (!RbacService.IsAdmin && !RbacService.IsManager && !RbacService.IsSuperAdmin) return;
                    SetActiveNavButton(btnArchives);
                    BtnArchivesClick(btnArchives, EventArgs.Empty);
                    break;
                case "reports":
                case "reports:commission":
                case "commission":
                case "commissionreport":
                case "commission-report":
                    if (!CurrentSession.CanAccess("Reports") || RbacService.IsAgent) return;
                    SetActiveNavButton(btnReports);
                    ShowViewCached("Reports", () =>
                    {
                        var rpt = new CRMS_Peguit.winforms.Views.Reports.ReportsView();
                        rpt.NavigationRequested += m => NavigateTo(m);
                        return rpt;
                    });

                    if (_viewCache.TryGetValue("Reports", out var cachedRpt) && cachedRpt is CRMS_Peguit.winforms.Views.Reports.ReportsView rptView)
                    {
                        string modLower = module.ToLowerInvariant();
                        if (modLower.Contains("commission"))
                        {
                            rptView.SelectReport("Commission");
                        }
                        else if (!string.IsNullOrWhiteSpace(initialFilter))
                        {
                            rptView.SelectReport(initialFilter);
                        }
                    }
                    break;
                case "analytics":
                case "teamperformance":
                case "performance":
                case "insights":
                    if (!CurrentSession.CanAccess("Analytics") && !CurrentSession.CanAccess("Reports")) return;
                    SetActiveNavButton(btnAnalytics);
                    ShowViewCached("Analytics", () =>
                    {
                        var ana = new CRMS_Peguit.winforms.Views.Analytics.AnalyticsView();
                        ana.NavigationRequested += m => NavigateTo(m);
                        return ana;
                    });
                    if (!string.IsNullOrWhiteSpace(initialFilter) && _viewCache.TryGetValue("Analytics", out var cachedAna) && cachedAna is CRMS_Peguit.winforms.Views.Analytics.AnalyticsView anaView)
                    {
                        anaView.SetFilter(initialFilter);
                    }
                    break;
                case "salesstaff":
                case "manageagents":
                case "agents":
                    if (!CurrentSession.CanAccess("SalesStaff")) return;
                    SetActiveNavButton(btnManageAgents);
                    BtnManageAgentsClick(btnManageAgents, EventArgs.Empty);
                    break;
                case "managemanagers":
                case "managers":
                    if (!CurrentSession.CanAccess("Managers")) return;
                    SetActiveNavButton(btnManageManagers);
                    BtnManageManagersClick(btnManageManagers, EventArgs.Empty);
                    break;
                case "manageusers":
                case "users":
                    if (CurrentSession.CanAccess("SalesStaff"))
                    {
                        SetActiveNavButton(btnManageAgents);
                        BtnManageAgentsClick(btnManageAgents, EventArgs.Empty);
                    }
                    else if (CurrentSession.CanAccess("Managers"))
                    {
                        SetActiveNavButton(btnManageManagers);
                        BtnManageManagersClick(btnManageManagers, EventArgs.Empty);
                    }
                    break;
                case "adminpanel":
                case "subscriptions":
                case "master":
                    if (CurrentSession.CurrentUser?.Role != UserRole.SuperAdmin) return;
                    SetActiveNavButton(btnAdminPanel);
                    BtnAdminPanelClick(btnAdminPanel, EventArgs.Empty);
                    break;
                case "branching":
                case "branches":
                case "branch":
                    if (!CurrentSession.CanAccessBranching) return;
                    SetActiveNavButton(btnBranching);
                    BtnBranchingClick(btnBranching, EventArgs.Empty);
                    break;
            }
        }

        private void BtnCampaignsClick(object? sender, EventArgs e)
        {
            if (!CurrentSession.CanAccess("Campaigns")) return;
            ShowViewCached("Campaigns", () => new CampaignsView());
        }

        private void BtnDashboardClick(object? sender, EventArgs e)
        {
            ShowViewCached("Dashboard", () =>
            {
                var dashboard = new DashboardView();
                dashboard.NavigationRequested += module => NavigateTo(module);
                return dashboard;
            });
        }

        private void BtnApprovalsClick(object? sender, EventArgs e)
        {
            if (!CurrentSession.CanAccess("Approvals") && !RbacService.CanApproveAssignments) return;
            ShowViewCached("Approvals", () => new CRMS_Peguit.winforms.Views.Management.ApprovalsView());
        }

        private void BtnManageManagersClick(object? sender, EventArgs e)
        {
            if (!CurrentSession.CanAccess("Managers")) return;
            ShowViewCached("ManageManagers", () => new AdminUserListForm("Manager"));
        }

        private void BtnManageAgentsClick(object? sender, EventArgs e)
        {
            if (!CurrentSession.CanAccess("SalesStaff")) return;
            ShowViewCached("ManageAgents", () => new AdminUserListForm("Agent"));
        }

        private void BtnArchivesClick(object? sender, EventArgs e)
        {
            if (!RbacService.IsAdmin && !RbacService.IsManager && !RbacService.IsSuperAdmin) return;
            ShowViewCached("Archives", () => new CRMS_Peguit.winforms.Views.Archives.ArchivesView());
        }

        private void BtnCustomersClick(object? sender, EventArgs e)
        {
            if (!CurrentSession.CanAccess("Customers")) return;
            ShowViewCached("Customers", () => new CustomersView());
        }

        private void BtnLeadsClick(object? sender, EventArgs e)
        {
            if (!CurrentSession.CanAccess("Leads")) return;
            ShowViewCached("Leads", () => new LeadsView());
        }

        private void BtnPropertiesClick(object? sender, EventArgs e)
        {
            if (!CurrentSession.CanAccess("Properties")) return;
            ShowViewCached("Properties", () => new PropertiesView());
        }

        private void BtnDealsClick(object? sender, EventArgs e)
        {
            if (!CurrentSession.CanAccess("Deals")) return;
            ShowViewCached("Deals", () => new DealsView());
        }

        private void BtnActivitiesClick(object? sender, EventArgs e)
        {
            if (!CurrentSession.CanAccess("Activities")) return;
            ShowViewCached("Activities", () => new CRMS_Peguit.winforms.Views.Activities.ActivitiesView());
        }

        private void BtnFollowUpsClick(object? sender, EventArgs e)
        {
            if (!CurrentSession.CanAccess("TasksReminders") || !RbacService.IsAgent) return;
            ShowViewCached("FollowUps", () => new FollowUpsView());
        }

        private void BtnAnalyticsClick(object? sender, EventArgs e)
        {
            if (!CurrentSession.CanAccess("Analytics") && !CurrentSession.CanAccess("Reports")) return;

            ShowViewCached("Analytics", () =>
            {
                var ana = new CRMS_Peguit.winforms.Views.Analytics.AnalyticsView();
                ana.NavigationRequested += m => NavigateTo(m);
                return ana;
            });
        }

        private void BtnReportsClick(object? sender, EventArgs e)
        {
            if (!CurrentSession.CanAccess("Reports") || RbacService.IsAgent) return;

            ShowViewCached("Reports", () =>
            {
                var rpt = new CRMS_Peguit.winforms.Views.Reports.ReportsView();
                rpt.NavigationRequested += m => NavigateTo(m);
                return rpt;
            });
        }

        private void BtnSupportTicketsClick(object? sender, EventArgs e)
        {
            if (!CurrentSession.CanAccess("SupportTickets")) return;
            ShowViewCached("SupportTickets", () => new CRMS_Peguit.winforms.Views.SupportTickets.SupportTicketsView());
        }

        private void BtnAdminPanelClick(object? sender, EventArgs e)
        {
            if (CurrentSession.CurrentUser?.Role != UserRole.SuperAdmin) return;
            ShowViewCached("AdminPanelMasterView", () => new CRMS_Peguit.winforms.Views.SuperAdmin.AdminPanelMasterView());
        }

        private void BtnBranchingClick(object? sender, EventArgs e)
        {
            if (!CurrentSession.CanAccessBranching) return;
            ShowViewCached("BranchesView", () =>
            {
                var branchView = new CRMS_Peguit.winforms.Views.Branching.BranchesView();
                branchView.ActiveBranchChanged += (bId, bName) =>
                {
                    ApplyRolePermissions();
                };
                return branchView;
            });
        }

        // =====================================================
        // LOGOUT
        // =====================================================

        private void BtnLogoutClick(object? sender, EventArgs e)
        {
            var result = MessageBox.Show(
                "Are you sure you want to logout?",
                "Confirm Logout",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Question
            );

            if (result != DialogResult.Yes)
                return;

            CurrentSession.SignOut();

            foreach (var v in _viewCache.Values)
            {
                if (!v.IsDisposed)
                    v.Dispose();
            }
            _viewCache.Clear();
            mainPanel.Controls.Clear();

            var loginForm = Application.OpenForms.OfType<LoginForm>().FirstOrDefault();

            if (loginForm != null)
            {
                loginForm.PrepareForLogout();
            }
            else
            {
                Application.Exit();   // fallback
                return;
            }

            Close();
        }

        // =========================================================================
        // OFFLINE SYNC UI & CONNECTIVITY BANNER
        // =========================================================================

        private void InitSyncUi()
        {
            // Start background connectivity monitor (pings every 30s)
            SyncService.Instance.Start(30);

            // Scoped read-cache refresh when starting online
            if (SyncService.Instance.IsOnline && !CurrentSession.IsOffline)
            {
                _ = Task.Run(async () =>
                {
                    await SyncService.Instance.RefreshUserCacheAsync(
                        CurrentSession.TenantId,
                        CurrentSession.UserId,
                        CurrentSession.CurrentUser?.Role.ToString() ?? "Agent");
                });
            }

            // Top Header Sync Indicator Button
            _btnSyncIndicator = new Button
            {
                Text = SyncService.Instance.IsOnline ? "🟢 Synced" : "🔴 Offline",
                Size = new Size(125, 30),
                Anchor = AnchorStyles.Top | AnchorStyles.Right,
                Location = new Point(lblTenantTierBadge.Left - 135, 14),
                BackColor = Color.FromArgb(241, 245, 249),
                ForeColor = Color.FromArgb(15, 23, 42),
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 8.5F, FontStyle.Bold),
                Cursor = Cursors.Hand
            };
            _btnSyncIndicator.FlatAppearance.BorderSize = 0;
            UiRadiusHelper.StyleButton(_btnSyncIndicator, 6);
            mainToolTip.SetToolTip(_btnSyncIndicator, "Click to view Data Synchronization Status & Queue");
            _btnSyncIndicator.Click += (_, _) =>
            {
                using var dlg = new Views.Sync.SyncStatusForm();
                dlg.ShowDialog(this);
            };
            topHeaderPanel.Controls.Add(_btnSyncIndicator);

            // Persistent Offline Warning Banner docked under topHeaderPanel
            _pnlOfflineBanner = new Panel
            {
                Dock = DockStyle.Top,
                Height = 42,
                BackColor = Color.FromArgb(254, 243, 199), // Amber-100
                Visible = !SyncService.Instance.IsOnline || CurrentSession.IsOffline,
                Padding = new Padding(16, 6, 16, 6)
            };
            _pnlOfflineBanner.Paint += (s, e) =>
            {
                using var pen = new Pen(Color.FromArgb(245, 158, 11), 1);
                e.Graphics.DrawLine(pen, 0, _pnlOfflineBanner.Height - 1, _pnlOfflineBanner.Width, _pnlOfflineBanner.Height - 1);
            };

            _lblOfflineBannerText = new Label
            {
                Text = "⚡ Offline Mode — Working locally. Changes are queued and will sync when reconnected.",
                Font = new Font("Segoe UI", 9F, FontStyle.Bold),
                ForeColor = Color.FromArgb(146, 64, 14), // Amber-800
                AutoSize = true,
                Location = new Point(16, 12)
            };

            _btnBannerRetry = new Button
            {
                Text = "🔄 Check Connection",
                Size = new Size(145, 28),
                Anchor = AnchorStyles.Top | AnchorStyles.Right,
                Location = new Point(_pnlOfflineBanner.Width - 280, 7),
                BackColor = Color.FromArgb(251, 191, 36),
                ForeColor = Color.FromArgb(120, 53, 15),
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 8.5F, FontStyle.Bold),
                Cursor = Cursors.Hand
            };
            _btnBannerRetry.FlatAppearance.BorderSize = 0;
            UiRadiusHelper.StyleButton(_btnBannerRetry, 4);
            _btnBannerRetry.Click += async (_, _) =>
            {
                _btnBannerRetry.Enabled = false;
                _btnBannerRetry.Text = "Checking...";
                try
                {
                    bool isOnline = await SyncService.Instance.CheckConnectivityAsync();
                    if (isOnline)
                    {
                        _pnlOfflineBanner.Visible = false;
                        UpdateSyncIndicator();
                    }
                    else
                    {
                        _btnBannerRetry.Text = "Still Offline ⚠️";
                        await Task.Delay(1500);
                    }
                }
                catch
                {
                    // Ignore transient retry errors
                }
                finally
                {
                    _btnBannerRetry.Text = "🔄 Check Connection";
                    _btnBannerRetry.Enabled = true;
                }
            };

            _btnBannerSyncQueue = new Button
            {
                Text = "📋 View Queue",
                Size = new Size(115, 28),
                Anchor = AnchorStyles.Top | AnchorStyles.Right,
                Location = new Point(_pnlOfflineBanner.Width - 125, 7),
                BackColor = Color.FromArgb(217, 119, 6),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 8.5F, FontStyle.Bold),
                Cursor = Cursors.Hand
            };
            _btnBannerSyncQueue.FlatAppearance.BorderSize = 0;
            UiRadiusHelper.StyleButton(_btnBannerSyncQueue, 4);
            _btnBannerSyncQueue.Click += (_, _) =>
            {
                using var dlg = new Views.Sync.SyncStatusForm();
                dlg.ShowDialog(this);
            };

            _pnlOfflineBanner.Controls.Add(_lblOfflineBannerText);
            _pnlOfflineBanner.Controls.Add(_btnBannerRetry);
            _pnlOfflineBanner.Controls.Add(_btnBannerSyncQueue);

            contentWrapperPanel.Controls.Add(_pnlOfflineBanner);
            topHeaderPanel.BringToFront(); // Ensures header remains on top and banner docks directly beneath it

            // Subscribe to sync engine events
            SyncService.Instance.ConnectivityChanged += OnSyncConnectivityChanged;
            SyncService.Instance.SyncProgressChanged += OnSyncProgressChanged;
            UpdateSyncIndicator();
        }

        private void OnSyncConnectivityChanged(object? sender, bool isOnline)
        {
            if (this.IsDisposed) return;
            this.BeginInvoke(new Action(() =>
            {
                _pnlOfflineBanner.Visible = !isOnline || CurrentSession.IsOffline;
                UpdateSyncIndicator();
            }));
        }

        private void OnSyncProgressChanged(object? sender, SyncProgressEventArgs e)
        {
            if (this.IsDisposed) return;
            this.BeginInvoke(new Action(() =>
            {
                UpdateSyncIndicator();
            }));
        }

        private void UpdateSyncIndicator()
        {
            int tenantId = CurrentSession.TenantId > 0 ? CurrentSession.TenantId : 1;
            var counts = LocalDataCache.Instance.GetQueueCounts(tenantId);

            if (!SyncService.Instance.IsOnline || CurrentSession.IsOffline)
            {
                _btnSyncIndicator.Text = counts.Pending > 0 ? $"🔴 Offline ({counts.Pending})" : "🔴 Offline";
                _btnSyncIndicator.BackColor = Color.FromArgb(254, 242, 242);
                _btnSyncIndicator.ForeColor = Color.FromArgb(185, 28, 28);
            }
            else if (counts.Conflict > 0)
            {
                _btnSyncIndicator.Text = $"⚠️ Conflict ({counts.Conflict})";
                _btnSyncIndicator.BackColor = Color.FromArgb(254, 243, 199);
                _btnSyncIndicator.ForeColor = Color.FromArgb(180, 83, 9);
            }
            else if (counts.Failed > 0)
            {
                _btnSyncIndicator.Text = $"❌ Failed ({counts.Failed})";
                _btnSyncIndicator.BackColor = Color.FromArgb(254, 242, 242);
                _btnSyncIndicator.ForeColor = Color.FromArgb(185, 28, 28);
            }
            else if (counts.Pending > 0 || counts.Syncing > 0)
            {
                _btnSyncIndicator.Text = $"⏳ Syncing ({counts.Pending})";
                _btnSyncIndicator.BackColor = Color.FromArgb(239, 246, 255);
                _btnSyncIndicator.ForeColor = Color.FromArgb(37, 99, 235);
            }
            else
            {
                _btnSyncIndicator.Text = "🟢 Synced";
                _btnSyncIndicator.BackColor = Color.FromArgb(240, 253, 244);
                _btnSyncIndicator.ForeColor = Color.FromArgb(22, 163, 74);
            }
        }
    }
}