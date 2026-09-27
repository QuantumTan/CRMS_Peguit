using System;
using System.Drawing;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using CRMS_Peguit.domain.Common;
using CRMS_Peguit.winforms.Controllers;
using CRMS_Peguit.winforms.Controls;
using CRMS_Peguit.winforms.Models.Services;
using CRMS_Peguit.winforms.Services;
using PlatformSnapshotDto = CRMS_Peguit.winforms.Controllers.PlatformSnapshotDto;

// =============================================================================
// SuperAdminDashboardView — Complete Platform Overview
// Architecture:
//   - KPI row: Total Tenants, Active Subscriptions, Expiring This Month, Last Backup Status
//   - Bar chart: Subscriptions by Tier
//   - Recent Platform Activity: Live audit log stream
//   - KPI Click-Navigation: Navigates directly to target modules
//
// DATA BOUNDARY ATTESTATION:
// ZERO access to tenant business/operational data (Customers, Leads, Deals,
// Properties, Activities, SupportTickets, TaskReminders, Notifications).
// =============================================================================

namespace CRMS_Peguit.winforms.Views.SuperAdmin
{
    public class SuperAdminDashboardView : UserControl
    {
        private readonly SuperAdminController _controller = new();
        private readonly SuperAdminSyncHealthController _syncController = new();

        // KPI cards
        private KpiCard _kpiTenants = null!;
        private KpiCard _kpiActiveSubs = null!;
        private KpiCard _kpiExpiring = null!;
        private KpiCard _kpiBackup = null!;

        // Content panels
        private Panel _pnlTierDist = null!;
        private Panel _pnlActivity = null!;
        private ListSkeletonOverlay? _activitySkeleton;
        private ChartSkeletonOverlay? _tierSkeleton;

        // Public navigation events
        public event Action? NavigateToTenants;
        public event Action? NavigateToSubscriptions;
        public event Action? NavigateToBackups;
        public event Action? NavigateToAuditLog;

        public SuperAdminDashboardView()
        {
            InitializeComponent();
            _ = LoadDataAsync();
        }

        private void InitializeComponent()
        {
            Dock = DockStyle.Fill;
            BackColor = Theme.Background;

            var scrollHost = new Panel
            {
                Dock = DockStyle.Fill,
                AutoScroll = true,
                BackColor = Theme.Background
            };
            Controls.Add(scrollHost);

            // 1. Page Header (Height = 96)
            var pnlPageHeader = new Panel
            {
                Dock = DockStyle.Top,
                Height = 96,
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
                Text = "Platform Overview",
                Font = UiStyleConstants.PageTitleFont,
                ForeColor = Theme.TextPrimary,
                AutoSize = true,
                Location = new Point(28, 18)
            };
            var lblSub = new Label
            {
                Text = "High-level platform metrics, subscription distribution, and recent platform activity stream.",
                Font = UiStyleConstants.SubtitleFont,
                ForeColor = Theme.TextSecondary,
                AutoSize = true,
                Location = new Point(28, 56)
            };
            var btnRefresh = new Button
            {
                Text = "↻  Refresh",
                Size = new Size(110, 36),
                Anchor = AnchorStyles.Top | AnchorStyles.Right,
                BackColor = Theme.Surface,
                ForeColor = Theme.TextPrimary,
                Font = new Font("Segoe UI", 9.5f, FontStyle.Bold),
                Cursor = Cursors.Hand
            };
            UiRadiusHelper.StyleButton(btnRefresh, 6);
            btnRefresh.Click += (_, _) => _ = LoadDataAsync();
            btnRefresh.Location = new Point(pnlPageHeader.Width - 138, 30);
            pnlPageHeader.SizeChanged += (_, _) =>
                btnRefresh.Location = new Point(pnlPageHeader.Width - 138, 30);

            pnlPageHeader.Controls.Add(lblSub);
            pnlPageHeader.Controls.Add(lblTitle);
            pnlPageHeader.Controls.Add(btnRefresh);

            // 2. KPI row (Height = 124)
            // Exactly 4 KPIs: Total Tenants, Active Subscriptions, Expiring This Month, Last Backup Status
            var pnlKpis = new TableLayoutPanel
            {
                Dock = DockStyle.Top,
                Height = 124,
                ColumnCount = 4,
                RowCount = 1,
                Padding = new Padding(24, 16, 24, 8),
                BackColor = Theme.Background
            };
            pnlKpis.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25f));
            pnlKpis.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25f));
            pnlKpis.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25f));
            pnlKpis.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25f));

            _kpiTenants = new KpiCard("TOTAL TENANTS", "tenants", Theme.Primary, KpiIconType.Building, null, KpiClickMode.Navigate)
            {
                Dock = DockStyle.Fill,
                Margin = new Padding(0, 0, 8, 0)
            };
            _kpiTenants.SetAction(() => NavigateToTenants?.Invoke());

            _kpiActiveSubs = new KpiCard("ACTIVE SUBSCRIPTIONS", "subscriptions", Theme.StatusSuccess, KpiIconType.Target, null, KpiClickMode.Navigate)
            {
                Dock = DockStyle.Fill,
                Margin = new Padding(4, 0, 4, 0)
            };
            _kpiActiveSubs.SetAction(() => NavigateToSubscriptions?.Invoke());

            _kpiExpiring = new KpiCard("EXPIRING THIS MONTH", "expiring", Theme.StatusPending, KpiIconType.Clock, null, KpiClickMode.Navigate)
            {
                Dock = DockStyle.Fill,
                Margin = new Padding(4, 0, 4, 0)
            };
            _kpiExpiring.SetAction(() => NavigateToSubscriptions?.Invoke());

            _kpiBackup = new KpiCard("LAST BACKUP STATUS", "backups", Theme.PrimaryDark, KpiIconType.Refresh, null, KpiClickMode.Navigate)
            {
                Dock = DockStyle.Fill,
                Margin = new Padding(8, 0, 0, 0)
            };
            _kpiBackup.SetAction(() => NavigateToBackups?.Invoke());

            pnlKpis.Controls.Add(_kpiTenants, 0, 0);
            pnlKpis.Controls.Add(_kpiActiveSubs, 1, 0);
            pnlKpis.Controls.Add(_kpiExpiring, 2, 0);
            pnlKpis.Controls.Add(_kpiBackup, 3, 0);

            // 3. Main Dashboard Content (Height = 440)
            var pnlContent = new TableLayoutPanel
            {
                Dock = DockStyle.Top,
                Height = 460,
                ColumnCount = 2,
                RowCount = 1,
                Padding = new Padding(24, 16, 24, 20),
                BackColor = Theme.Background
            };
            pnlContent.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50f));
            pnlContent.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50f));

            // Left Card: Subscriptions by Tier
            _pnlTierDist = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Theme.Surface,
                Padding = new Padding(22),
                Margin = new Padding(0, 0, 10, 0)
            };
            UiRadiusHelper.StyleCard(_pnlTierDist, 8);

            var pnlTierHeader = new Panel
            {
                Dock = DockStyle.Top,
                Height = 52,
                BackColor = Color.Transparent
            };
            var lblTierHeader = new Label
            {
                Text = "Subscriptions by Tier",
                Font = new Font("Segoe UI", 12f, FontStyle.Bold),
                ForeColor = Theme.TextPrimary,
                Location = new Point(0, 0),
                AutoSize = true
            };
            var lblTierSub = new Label
            {
                Text = "Active client organizations distributed across plan tiers (Tenant A, B, C)",
                Font = new Font("Segoe UI", 8.5f),
                ForeColor = Theme.TextSecondary,
                Location = new Point(0, 24),
                AutoSize = true
            };
            pnlTierHeader.Controls.Add(lblTierHeader);
            pnlTierHeader.Controls.Add(lblTierSub);
            _pnlTierDist.Controls.Add(pnlTierHeader);

            _tierSkeleton = new ChartSkeletonOverlay
            {
                Location = new Point(22, 64),
                Size = new Size(380, 280),
                Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Bottom
            };
            _pnlTierDist.Controls.Add(_tierSkeleton);

            _pnlTierDist.SizeChanged += (_, _) =>
            {
                if (_lastSnapshot != null)
                    RenderTierDistribution(_lastSnapshot);
            };

            // Right Card: Recent Platform Activity
            _pnlActivity = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Theme.Surface,
                Padding = new Padding(22),
                Margin = new Padding(10, 0, 0, 0)
            };
            UiRadiusHelper.StyleCard(_pnlActivity, 8);

            var pnlActivityHeader = new Panel
            {
                Dock = DockStyle.Top,
                Height = 48,
                BackColor = Color.Transparent
            };

            var lblActivityTitle = new Label
            {
                Text = "Recent Platform Activity",
                Font = new Font("Segoe UI", 12f, FontStyle.Bold),
                ForeColor = Theme.TextPrimary,
                Location = new Point(0, 0),
                AutoSize = true
            };
            var lblActivitySub = new Label
            {
                Text = "Live audit stream of administrative events across tenants",
                Font = new Font("Segoe UI", 8.5f),
                ForeColor = Theme.TextSecondary,
                Location = new Point(0, 24),
                AutoSize = true
            };
            var lnkViewAllAudit = new Label
            {
                Text = "View full log →",
                Font = new Font("Segoe UI", 9f, FontStyle.Bold),
                ForeColor = Theme.Primary,
                Anchor = AnchorStyles.Top | AnchorStyles.Right,
                AutoSize = true,
                Cursor = Cursors.Hand
            };
            lnkViewAllAudit.Click += (_, _) => NavigateToAuditLog?.Invoke();
            lnkViewAllAudit.Location = new Point(_pnlActivity.Width - 140, 6);
            _pnlActivity.SizeChanged += (_, _) => lnkViewAllAudit.Location = new Point(_pnlActivity.Width - 140, 6);

            pnlActivityHeader.Controls.Add(lblActivityTitle);
            pnlActivityHeader.Controls.Add(lblActivitySub);
            pnlActivityHeader.Controls.Add(lnkViewAllAudit);
            _pnlActivity.Controls.Add(pnlActivityHeader);

            _activitySkeleton = ListSkeletonOverlay.CreateForContainer(_pnlActivity);

            pnlContent.Controls.Add(_pnlTierDist, 0, 0);
            pnlContent.Controls.Add(_pnlActivity, 1, 0);

            // WinForms docking reverse order
            scrollHost.Controls.Add(pnlContent);
            scrollHost.Controls.Add(pnlKpis);
            scrollHost.Controls.Add(pnlPageHeader);
        }

        public async Task LoadDataAsync()
        {
            _kpiTenants.ShowLoadingSkeleton();
            _kpiActiveSubs.ShowLoadingSkeleton();
            _kpiExpiring.ShowLoadingSkeleton();
            _kpiBackup.ShowLoadingSkeleton();
            _tierSkeleton?.ShowSkeleton(ChartSkeletonType.Bars);
            _activitySkeleton?.ShowSkeleton(4);

            try
            {
                var snap = await _controller.GetPlatformSnapshotAsync();

                ConfigureKpi(_kpiTenants, "TOTAL TENANTS", snap.TotalTenants,
                    $"{snap.ActiveTenants} Active Organizations", Theme.Primary, KpiIconType.Building);

                ConfigureKpi(_kpiActiveSubs, "ACTIVE SUBSCRIPTIONS", snap.ActiveSubscriptions,
                    "Current active paid plans", Theme.StatusSuccess, KpiIconType.Target);

                ConfigureKpi(_kpiExpiring, "EXPIRING THIS MONTH", snap.ExpiringThisMonth,
                    snap.ExpiringThisMonth > 0 ? "Requires renewal outreach" : "All plans in good standing",
                    snap.ExpiringThisMonth > 0 ? Theme.StatusPending : Theme.StatusSuccess,
                    KpiIconType.Clock);

                string backupDisplay = string.IsNullOrWhiteSpace(snap.LastBackupStatus) || snap.LastBackupStatus == "None"
                    ? "Normal" : snap.LastBackupStatus;
                string backupDate = snap.LastBackupDate.HasValue
                    ? snap.LastBackupDate.Value.ToLocalTime().ToString("MMM dd  h:mm tt")
                    : "No backups";
                ConfigureKpi(_kpiBackup, "LAST BACKUP STATUS", backupDisplay,
                    $"Last: {backupDate}", Theme.PrimaryDark, KpiIconType.Refresh);

                RenderTierDistribution(snap);
                await RenderRecentActivityAsync();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to load dashboard: {ex.Message}",
                    "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                _kpiTenants.HideLoadingSkeleton();
                _kpiActiveSubs.HideLoadingSkeleton();
                _kpiExpiring.HideLoadingSkeleton();
                _kpiBackup.HideLoadingSkeleton();
                _tierSkeleton?.HideSkeleton();
                _activitySkeleton?.HideSkeleton();
            }
        }

        private static void ConfigureKpi(KpiCard card, string title, object value,
            string subtitle, Color accent, KpiIconType icon)
        {
            card.SetTitle(title);
            if (value is int i) card.SetValue(i);
            else card.SetValue(value?.ToString() ?? "0");
            card.SetSubtitle(subtitle, Color.FromArgb(100, 116, 139));
            card.SetIcon(icon, accent);
        }

        private PlatformSnapshotDto? _lastSnapshot;

        private void RenderTierDistribution(PlatformSnapshotDto snap)
        {
            _lastSnapshot = snap;
            var old = _pnlTierDist.Controls.Cast<Control>()
                .Where(c => c.Tag?.ToString() == "tier").ToList();
            foreach (var c in old) _pnlTierDist.Controls.Remove(c);

            int total = Math.Max(1, snap.TenantACount + snap.TenantBCount + snap.TenantCCount);

            var tiers = new (string Name, int Count, Color Color, string Subtitle)[]
            {
                ("Tenant C (Enterprise)", snap.TenantCCount, Color.FromArgb(124, 58, 237), "Multi-Branching · Full BI · Workflows"),
                ("Tenant B (Professional)", snap.TenantBCount, Color.FromArgb(14, 165, 233), "Business Intelligence · Actions"),
                ("Tenant A (Starter)", snap.TenantACount, Color.FromArgb(100, 116, 139), "Base CRM & Data Collection")
            };

            int y = 64;
            int availableWidth = Math.Max(200, _pnlTierDist.ClientSize.Width - 44);

            foreach (var (name, count, color, subtitle) in tiers)
            {
                int pct = (int)((float)count / total * 100);

                var lblName = new Label
                {
                    Text = name,
                    Font = new Font("Segoe UI", 9.5f, FontStyle.Bold),
                    ForeColor = Theme.TextPrimary,
                    Location = new Point(22, y),
                    AutoSize = true,
                    Tag = "tier"
                };

                var lblCountAndPct = new Label
                {
                    Text = $"{count} ({pct}%)",
                    Font = new Font("Segoe UI", 9.5f, FontStyle.Bold),
                    ForeColor = color,
                    Anchor = AnchorStyles.Top | AnchorStyles.Right,
                    Location = new Point(_pnlTierDist.ClientSize.Width - 22 - 100, y),
                    Size = new Size(100, 20),
                    TextAlign = ContentAlignment.MiddleRight,
                    Tag = "tier"
                };

                _pnlTierDist.Controls.Add(lblName);
                _pnlTierDist.Controls.Add(lblCountAndPct);

                var pnlBarBg = new Panel
                {
                    Location = new Point(22, y + 24),
                    Size = new Size(availableWidth, 12),
                    Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right,
                    BackColor = Color.FromArgb(241, 245, 249),
                    Tag = "tier",
                    Cursor = Cursors.Hand
                };
                UiRadiusHelper.StyleCard(pnlBarBg, 6);

                int fillPx = count == 0 ? 0 : Math.Max(16, (int)((float)count / total * availableWidth));
                var pnlFill = new Panel
                {
                    Location = new Point(0, 0),
                    Size = new Size(0, 12), // starts at 0 for entrance growth
                    BackColor = color
                };
                UiRadiusHelper.StyleCard(pnlFill, 6);
                pnlBarBg.Controls.Add(pnlFill);
                _pnlTierDist.Controls.Add(pnlBarBg);

                // Smooth bar fill entrance growth (~320ms EaseOutCubic)
                if (fillPx > 0)
                {
                    var animTimer = new System.Windows.Forms.Timer { Interval = 16 };
                    var startTime = DateTime.UtcNow;
                    animTimer.Tick += (s, e) =>
                    {
                        if (pnlFill.IsDisposed)
                        {
                            animTimer.Stop();
                            animTimer.Dispose();
                            return;
                        }
                        double elapsed = (DateTime.UtcNow - startTime).TotalMilliseconds;
                        double progress = Math.Min(1.0, elapsed / 320.0);
                        double ease = 1.0 - Math.Pow(1.0 - progress, 3);
                        pnlFill.Width = Math.Max(4, (int)(fillPx * ease));
                        if (progress >= 1.0)
                        {
                            pnlFill.Width = fillPx;
                            animTimer.Stop();
                            animTimer.Dispose();
                        }
                    };
                    animTimer.Start();
                }

                var lblDesc = new Label
                {
                    Text = subtitle,
                    Font = new Font("Segoe UI", 8f),
                    ForeColor = Color.FromArgb(148, 163, 184),
                    Location = new Point(22, y + 42),
                    Size = new Size(availableWidth, 18),
                    Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right,
                    AutoEllipsis = true,
                    Tag = "tier"
                };
                _pnlTierDist.Controls.Add(lblDesc);

                pnlBarBg.Click += (_, _) => NavigateToSubscriptions?.Invoke();
                lblName.Click += (_, _) => NavigateToSubscriptions?.Invoke();

                y += 74;
            }
        }

        private async Task RenderRecentActivityAsync()
        {
            var old = _pnlActivity.Controls.Cast<Control>()
                .Where(c => c.Tag?.ToString() == "act").ToList();
            foreach (var c in old) _pnlActivity.Controls.Remove(c);

            try
            {
                var logs = await _syncController.GetRecentAuditLogsAsync(6);

                if (logs.Count == 0)
                {
                    var lblEmpty = new Label
                    {
                        Text = "No platform activity recorded yet.",
                        Font = new Font("Segoe UI", 9.5f, FontStyle.Italic),
                        ForeColor = Theme.TextSecondary,
                        Location = new Point(20, 70),
                        AutoSize = true,
                        Tag = "act"
                    };
                    _pnlActivity.Controls.Add(lblEmpty);
                    return;
                }

                int y = 56;
                foreach (var log in logs)
                {
                    var rowPanel = new Panel
                    {
                        Location = new Point(14, y),
                        Size = new Size(_pnlActivity.ClientSize.Width - 28, 52),
                        Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right,
                        BackColor = Color.FromArgb(249, 250, 251),
                        Tag = "act"
                    };
                    UiRadiusHelper.StyleCard(rowPanel, 6);

                    // Circular Avatar with deterministic initials
                    var avatarPanel = new Panel
                    {
                        Location = new Point(10, 10),
                        Size = new Size(32, 32),
                        BackColor = Color.Transparent
                    };
                    var (bg, fg) = AvatarLabel.GetDeterministicAvatarColors(log.PerformedByName);
                    string initials = AvatarLabel.GetInitials(log.PerformedByName);
                    avatarPanel.Paint += (s, e) =>
                    {
                        e.Graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
                        using var b = new SolidBrush(bg);
                        e.Graphics.FillEllipse(b, 0, 0, 32, 32);
                        using var f = new Font("Segoe UI", 8.5f, FontStyle.Bold);
                        TextRenderer.DrawText(e.Graphics, initials, f, new Rectangle(0, 0, 32, 32), fg,
                            TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
                    };
                    rowPanel.Controls.Add(avatarPanel);

                    // Action description line 1: Name and Action Type
                    var lblUserAction = new Label
                    {
                        Text = $"{log.PerformedByName} · {log.ActionType}",
                        Font = new Font("Segoe UI", 9f, FontStyle.Bold),
                        ForeColor = Theme.TextPrimary,
                        Location = new Point(50, 7),
                        Size = new Size(Math.Max(50, rowPanel.Width - 190), 18),
                        Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right,
                        AutoEllipsis = true
                    };
                    rowPanel.Controls.Add(lblUserAction);

                    // Action description line 2: Detail
                    var lblDetail = new Label
                    {
                        Text = log.Detail,
                        Font = new Font("Segoe UI", 8.5f),
                        ForeColor = Theme.TextSecondary,
                        Location = new Point(50, 27),
                        Size = new Size(Math.Max(50, rowPanel.Width - 190), 18),
                        Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right,
                        AutoEllipsis = true
                    };
                    rowPanel.Controls.Add(lblDetail);

                    // Timestamp
                    var lblTime = new Label
                    {
                        Text = log.CreatedAt.ToLocalTime().ToString("MMM dd  h:mm tt"),
                        Font = new Font("Segoe UI", 8f),
                        ForeColor = Color.FromArgb(100, 116, 139),
                        Location = new Point(rowPanel.Width - 134, 16),
                        Size = new Size(124, 20),
                        TextAlign = ContentAlignment.MiddleRight,
                        Anchor = AnchorStyles.Top | AnchorStyles.Right
                    };
                    rowPanel.Controls.Add(lblTime);

                    _pnlActivity.Controls.Add(rowPanel);
                    y += 58;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Recent activity render error: {ex.Message}");
            }
        }
    }
}
