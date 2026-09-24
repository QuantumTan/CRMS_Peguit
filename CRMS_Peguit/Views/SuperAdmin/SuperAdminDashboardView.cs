using System;
using System.Drawing;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using CRMS_Peguit.winforms.Controllers;
using CRMS_Peguit.winforms.Controls;
using CRMS_Peguit.winforms.Models.Services;

// =============================================================================
// SuperAdminDashboardView — KPI row + "Subscriptions by Status" bar chart.
//
// DATA BOUNDARY: All data via SuperAdminController.GetPlatformSnapshotAsync()
// which sources exclusively from MasterCrmsDbContext (Companies, Subscriptions)
// and BackupLogs from the management tenant (TenantId=1).
// NO Customer/Lead/Deal/Property/Activity/SupportTicket/TaskReminder/Notification.
// =============================================================================

namespace CRMS_Peguit.winforms.Views.SuperAdmin
{
    public class SuperAdminDashboardView : UserControl
    {
        private readonly SuperAdminController _controller = new();

        // KPI cards
        private KpiCard _kpiTenants = null!;
        private KpiCard _kpiActiveSubs = null!;
        private KpiCard _kpiExpiring = null!;
        private KpiCard _kpiMrr = null!;

        // Chart panels
        private Panel _pnlChart = null!;
        private Panel _pnlTierDist = null!;

        // Last backup controls
        private StatusText _stBackupStatus = null!;
        private Label _lblLastBackupDate = null!;

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

            // 1. Page Header (Height = 76)
            var pnlPageHeader = new Panel
            {
                Dock = DockStyle.Top,
                Height = 76,
                BackColor = Theme.Surface,
                Padding = new Padding(28, 14, 28, 0)
            };
            pnlPageHeader.Paint += (s, e) =>
            {
                using var p = new Pen(Theme.Border, 1f);
                e.Graphics.DrawLine(p, 0, pnlPageHeader.Height - 1, pnlPageHeader.Width, pnlPageHeader.Height - 1);
            };

            var lblTitle = new Label
            {
                Text = "Platform Overview",
                Font = new Font("Segoe UI", 16f, FontStyle.Bold),
                ForeColor = Theme.TextPrimary,
                AutoSize = true,
                Location = new Point(28, 14)
            };
            var lblSub = new Label
            {
                Text = "High-level platform metrics & subscription health — aggregate counts only, no tenant business data",
                Font = new Font("Segoe UI", 9.5f),
                ForeColor = Theme.TextSecondary,
                AutoSize = true,
                Location = new Point(28, 42)
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
            btnRefresh.Location = new Point(pnlPageHeader.Width - 138, 18);
            pnlPageHeader.SizeChanged += (_, _) =>
                btnRefresh.Location = new Point(pnlPageHeader.Width - 138, 18);

            pnlPageHeader.Controls.Add(lblSub);
            pnlPageHeader.Controls.Add(lblTitle);
            pnlPageHeader.Controls.Add(btnRefresh);

            // 2. KPI row (Height = 124)
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

            _kpiTenants = new KpiCard { Dock = DockStyle.Fill, Margin = new Padding(0, 0, 8, 0) };
            _kpiActiveSubs = new KpiCard { Dock = DockStyle.Fill, Margin = new Padding(4, 0, 4, 0) };
            _kpiExpiring = new KpiCard { Dock = DockStyle.Fill, Margin = new Padding(4, 0, 4, 0) };
            _kpiMrr = new KpiCard { Dock = DockStyle.Fill, Margin = new Padding(8, 0, 0, 0) };

            pnlKpis.Controls.Add(_kpiTenants, 0, 0);
            pnlKpis.Controls.Add(_kpiActiveSubs, 1, 0);
            pnlKpis.Controls.Add(_kpiExpiring, 2, 0);
            pnlKpis.Controls.Add(_kpiMrr, 3, 0);

            // 3. Backup Status Bar (Height = 48)
            var pnlBackupBar = new Panel
            {
                Dock = DockStyle.Top,
                Height = 48,
                BackColor = Theme.Surface,
                Padding = new Padding(28, 0, 28, 0)
            };
            pnlBackupBar.Paint += (s, e) =>
            {
                using var p = new Pen(Theme.Border, 1f);
                e.Graphics.DrawLine(p, 0, 0, pnlBackupBar.Width, 0);
                e.Graphics.DrawLine(p, 0, pnlBackupBar.Height - 1, pnlBackupBar.Width, pnlBackupBar.Height - 1);
            };

            var lblBackupIcon = new Label
            {
                Text = "🗄",
                Font = new Font("Segoe UI", 12f),
                Location = new Point(28, 12),
                AutoSize = true
            };
            var lblLastBackup = new Label
            {
                Text = "System Health & Last Backup:",
                Font = new Font("Segoe UI", 9.5f, FontStyle.Bold),
                ForeColor = Theme.TextPrimary,
                AutoSize = true,
                Location = new Point(54, 14)
            };
            _stBackupStatus = new StatusText
            {
                Location = new Point(245, 14),
                AutoSize = true
            };
            _lblLastBackupDate = new Label
            {
                Text = "—",
                Font = new Font("Segoe UI", 9f),
                ForeColor = Theme.TextSecondary,
                AutoSize = true,
                Location = new Point(310, 14)
            };
            pnlBackupBar.Controls.Add(lblBackupIcon);
            pnlBackupBar.Controls.Add(lblLastBackup);
            pnlBackupBar.Controls.Add(_stBackupStatus);
            pnlBackupBar.Controls.Add(_lblLastBackupDate);

            // 4. Charts row (Height = 380)
            var pnlCharts = new TableLayoutPanel
            {
                Dock = DockStyle.Top,
                Height = 380,
                ColumnCount = 2,
                RowCount = 1,
                Padding = new Padding(24, 16, 24, 20),
                BackColor = Theme.Background
            };
            pnlCharts.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 54f));
            pnlCharts.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 46f));

            // Left Card: Subscriptions by Status
            _pnlChart = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Theme.Surface,
                Padding = new Padding(22),
                Margin = new Padding(0, 0, 10, 0)
            };
            UiRadiusHelper.StyleCard(_pnlChart, 8);

            var lblChartTitle = new Label
            {
                Text = "Subscriptions by Status",
                Font = new Font("Segoe UI", 11.5f, FontStyle.Bold),
                ForeColor = Theme.TextPrimary,
                Dock = DockStyle.Top,
                Height = 26
            };
            var lblChartSub = new Label
            {
                Text = "Active · Expiring This Month · Expired (Platform Aggregate)",
                Font = new Font("Segoe UI", 8.5f),
                ForeColor = Theme.TextSecondary,
                Dock = DockStyle.Top,
                Height = 20
            };
            _pnlChart.Controls.Add(lblChartSub);
            _pnlChart.Controls.Add(lblChartTitle);

            // Right Card: Tenant Tier Distribution
            _pnlTierDist = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Theme.Surface,
                Padding = new Padding(22),
                Margin = new Padding(10, 0, 0, 0)
            };
            UiRadiusHelper.StyleCard(_pnlTierDist, 8);

            var lblTierTitle = new Label
            {
                Text = "Tenant Tier Distribution",
                Font = new Font("Segoe UI", 11.5f, FontStyle.Bold),
                ForeColor = Theme.TextPrimary,
                Dock = DockStyle.Top,
                Height = 26
            };
            var lblTierSub = new Label
            {
                Text = "Current client count by tier plan (Tenant A / B / C)",
                Font = new Font("Segoe UI", 8.5f),
                ForeColor = Theme.TextSecondary,
                Dock = DockStyle.Top,
                Height = 20
            };
            _pnlTierDist.Controls.Add(lblTierSub);
            _pnlTierDist.Controls.Add(lblTierTitle);

            pnlCharts.Controls.Add(_pnlChart, 0, 0);
            pnlCharts.Controls.Add(_pnlTierDist, 1, 0);

            // IMPORTANT WINFORMS DOCKING ORDER:
            // To dock top-to-bottom (Header -> KPIs -> BackupBar -> Charts),
            // add in REVERSE order so pnlPageHeader is added LAST (index 0):
            scrollHost.Controls.Add(pnlCharts);
            scrollHost.Controls.Add(pnlBackupBar);
            scrollHost.Controls.Add(pnlKpis);
            scrollHost.Controls.Add(pnlPageHeader);
        }

        private async Task LoadDataAsync()
        {
            try
            {
                var snap = await _controller.GetPlatformSnapshotAsync();

                ConfigureKpi(_kpiTenants, "TOTAL TENANTS", snap.TotalTenants,
                    $"{snap.ActiveTenants} Active Organizations", Theme.Primary, KpiIconType.Building);
                _kpiTenants.SetAction(() => ShowDrillDown("Total Tenants", $"{snap.TotalTenants} organizations registered across the platform (Active: {snap.ActiveTenants})."));

                ConfigureKpi(_kpiActiveSubs, "ACTIVE SUBSCRIPTIONS", snap.ActiveSubscriptions,
                    "Current active paid plans", Theme.StatusSuccess, KpiIconType.Target);
                _kpiActiveSubs.SetAction(() => ShowDrillDown("Active Subscriptions", $"{snap.ActiveSubscriptions} active paid subscription plans in good standing."));

                ConfigureKpi(_kpiExpiring, "EXPIRING THIS MONTH", snap.ExpiringThisMonth,
                    snap.ExpiringThisMonth > 0 ? "Requires renewal outreach" : "All plans in good standing",
                    snap.ExpiringThisMonth > 0 ? Theme.StatusPending : Theme.StatusSuccess,
                    KpiIconType.Clock);
                _kpiExpiring.SetAction(() => ShowDrillDown("Expiring Subscriptions", snap.ExpiringThisMonth > 0 ? $"{snap.ExpiringThisMonth} subscriptions requiring renewal outreach within 30 days." : "All tenant plans are in good standing with zero expiring accounts."));

                ConfigureKpi(_kpiMrr, "TOTAL MRR", $"₱{snap.TotalMrr:N0}",
                    "Monthly recurring revenue", Theme.PrimaryDark, KpiIconType.Currency);
                _kpiMrr.SetAction(() => ShowDrillDown("Platform MRR", $"Total platform monthly recurring revenue is ₱{snap.TotalMrr:N0} across all active subscriptions."));

                _stBackupStatus.SetStatus(snap.LastBackupStatus);
                _lblLastBackupDate.Text = snap.LastBackupDate.HasValue
                    ? $"  ·  Completed on {snap.LastBackupDate.Value.ToLocalTime():MMM dd, yyyy  h:mm tt}"
                    : "  ·  No recent backups recorded";

                RenderStatusChart(snap);
                RenderTierDistribution(snap);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to load dashboard: {ex.Message}",
                    "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
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

        private void RenderStatusChart(PlatformSnapshotDto snap)
        {
            var old = _pnlChart.Controls.Cast<Control>()
                .Where(c => c.Tag?.ToString() == "bar").ToList();
            foreach (var c in old) _pnlChart.Controls.Remove(c);

            int total = Math.Max(1, snap.ActiveSubscriptions + snap.ExpiringThisMonth + snap.ExpiredSubscriptions);

            var bars = new (string Label, int Count, Color Color)[]
            {
                ("Active", snap.ActiveSubscriptions, Theme.StatusSuccess),
                ("Expiring", snap.ExpiringThisMonth, Theme.StatusPending),
                ("Expired", snap.ExpiredSubscriptions, Theme.StatusAlert)
            };

            int y = 62;
            int barAreaWidth = Math.Max(180, _pnlChart.Width - 60);

            foreach (var (label, count, color) in bars)
            {
                int pct = (int)((float)count / total * 100);

                var lblName = new Label
                {
                    Text = $"{label} Subscriptions",
                    Font = new Font("Segoe UI", 9.5f, FontStyle.Bold),
                    ForeColor = Theme.TextPrimary,
                    Location = new Point(20, y),
                    AutoSize = true,
                    Tag = "bar"
                };

                var lblCountAndPct = new Label
                {
                    Text = $"{count}  ({pct}%)",
                    Font = new Font("Segoe UI", 9.5f, FontStyle.Bold),
                    ForeColor = color,
                    Anchor = AnchorStyles.Top | AnchorStyles.Right,
                    Location = new Point(barAreaWidth - 80, y),
                    Size = new Size(100, 20),
                    TextAlign = ContentAlignment.MiddleRight,
                    Tag = "bar"
                };

                _pnlChart.Controls.Add(lblName);
                _pnlChart.Controls.Add(lblCountAndPct);

                var pnlBarBg = new Panel
                {
                    Location = new Point(20, y + 24),
                    Size = new Size(barAreaWidth, 14),
                    Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right,
                    BackColor = Color.FromArgb(241, 245, 249),
                    Tag = "bar"
                };
                UiRadiusHelper.StyleCard(pnlBarBg, 7);

                int fillPx = count == 0 ? 0 : Math.Max(16, (int)((float)count / total * barAreaWidth));
                var pnlFill = new Panel
                {
                    Location = new Point(0, 0),
                    Size = new Size(fillPx, 14),
                    BackColor = color
                };
                UiRadiusHelper.StyleCard(pnlFill, 7);
                pnlBarBg.Controls.Add(pnlFill);
                _pnlChart.Controls.Add(pnlBarBg);

                // Make progress bar clickable
                pnlBarBg.Cursor = Cursors.Hand;
                lblName.Cursor = Cursors.Hand;
                Action barClick = () => ShowDrillDown($"{label} Subscriptions", $"{count} out of {total} subscriptions are in '{label}' status ({pct}%).");
                pnlBarBg.Click += (_, _) => barClick();
                pnlFill.Click += (_, _) => barClick();
                lblName.Click += (_, _) => barClick();

                y += 68;
            }

            var lblNote = new Label
            {
                Text = "⚡ Source: MasterCrmsDbContext.Subscriptions — aggregate counts only",
                Font = new Font("Segoe UI", 8f, FontStyle.Italic),
                ForeColor = Color.FromArgb(148, 163, 184),
                Location = new Point(20, y + 10),
                AutoSize = true,
                Tag = "bar"
            };
            _pnlChart.Controls.Add(lblNote);
        }

        private void RenderTierDistribution(PlatformSnapshotDto snap)
        {
            var old = _pnlTierDist.Controls.Cast<Control>()
                .Where(c => c.Tag?.ToString() == "tier").ToList();
            foreach (var c in old) _pnlTierDist.Controls.Remove(c);

            int total = Math.Max(1, snap.TenantACount + snap.TenantBCount + snap.TenantCCount);

            var tiers = new (string Name, int Count, Color Color, string Description)[]
            {
                ("Tenant C — Enterprise", snap.TenantCCount, Color.FromArgb(124, 58, 237), "Multi-Branching + BI + Automations"),
                ("Tenant B — Professional", snap.TenantBCount, Color.FromArgb(14, 165, 233), "Business Intelligence + Actions"),
                ("Tenant A — Starter", snap.TenantACount, Color.FromArgb(100, 116, 139), "Base Transactions & Data Collection")
            };

            int y = 62;
            foreach (var (name, count, color, desc) in tiers)
            {
                int pct = (int)((float)count / total * 100);

                var pnlRow = new Panel
                {
                    Location = new Point(20, y),
                    Size = new Size(_pnlTierDist.Width - 44, 56),
                    Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right,
                    BackColor = Color.FromArgb(248, 250, 252),
                    Cursor = Cursors.Hand,
                    Tag = "tier"
                };
                UiRadiusHelper.StyleCard(pnlRow, 6);

                var dot = new Panel
                {
                    Location = new Point(14, 18),
                    Size = new Size(16, 16),
                    BackColor = color
                };
                UiRadiusHelper.StyleCard(dot, 8);

                var lblName = new Label
                {
                    Text = name,
                    Font = new Font("Segoe UI", 9.5f, FontStyle.Bold),
                    ForeColor = Theme.TextPrimary,
                    Location = new Point(38, 8),
                    AutoSize = true,
                    Cursor = Cursors.Hand
                };
                var lblDesc = new Label
                {
                    Text = desc,
                    Font = new Font("Segoe UI", 8.5f),
                    ForeColor = Theme.TextSecondary,
                    Location = new Point(38, 28),
                    AutoSize = true,
                    Cursor = Cursors.Hand
                };

                var lblCountAndPct = new Label
                {
                    Text = $"{count} tenant{(count != 1 ? "s" : "")}  ·  {pct}%",
                    Font = new Font("Segoe UI", 10.5f, FontStyle.Bold),
                    ForeColor = color,
                    Anchor = AnchorStyles.Top | AnchorStyles.Right,
                    Location = new Point(pnlRow.Width - 170, 16),
                    Size = new Size(155, 24),
                    TextAlign = ContentAlignment.MiddleRight,
                    Cursor = Cursors.Hand
                };

                pnlRow.Controls.Add(dot);
                pnlRow.Controls.Add(lblName);
                pnlRow.Controls.Add(lblDesc);
                pnlRow.Controls.Add(lblCountAndPct);

                Action rowClick = () => ShowDrillDown(name, $"{count} tenant organization(s) on this plan tier ({pct}% platform share).\n\nFeatures: {desc}");
                pnlRow.Click += (_, _) => rowClick();
                lblName.Click += (_, _) => rowClick();
                lblDesc.Click += (_, _) => rowClick();
                lblCountAndPct.Click += (_, _) => rowClick();

                _pnlTierDist.Controls.Add(pnlRow);

                y += 68;
            }
        }

        private static void ShowDrillDown(string title, string details)
        {
            MessageBox.Show(details, $"Platform Analytics — {title}", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
    }
}
