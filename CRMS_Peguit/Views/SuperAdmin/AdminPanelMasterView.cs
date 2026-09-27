using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using CRMS_Peguit.domain.Common;
using CRMS_Peguit.domain.entities;
using CRMS_Peguit.winforms.Controllers;
using CRMS_Peguit.winforms.Controls;
using CRMS_Peguit.winforms.Models.Services;
using CRMS_Peguit.winforms.Services;

namespace CRMS_Peguit.winforms.Views.SuperAdmin
{
    public class AdminPanelMasterView : UserControl
    {
        private readonly SuperAdminSubscriptionController _controller;
        private List<TenantSubscriptionDto> _allSubscriptions = new();
        private string _activeTierFilter = "All";

        // Layout Containers
        private Panel _pnlHeader = null!;
        private Button _btnTabBi = null!;
        private Button _btnTabSubs = null!;
        private Panel _pnlBiContent = null!;
        private Panel _pnlSubsContent = null!;

        // BI Controls
        private KpiCard _kpiTenants = null!;
        private KpiCard _kpiActiveSubs = null!;
        private KpiCard _kpiMrr = null!;
        private KpiCard _kpiTransactions = null!;
        private Panel _pnlPlanDistribution = null!;
        private Panel _pnlPlatformActivity = null!;

        // Subscriptions Controls
        private TextBox _txtSearch = null!;
        private DataGridView _grid = null!;
        private GridSkeletonOverlay? _gridSkeleton;
        private Button _btnRecordPayment = null!;
        private Button _btnPaymentHistory = null!;
        private Button _btnChangeTier = null!;

        public AdminPanelMasterView()
        {
            _controller = new SuperAdminSubscriptionController();
            InitializeComponent();
            LoadDataAsync();
        }

        private void InitializeComponent()
        {
            Dock = DockStyle.Fill;
            BackColor = Theme.Background;
            AutoScroll = true;

            // 1. TOP HEADER
            _pnlHeader = new Panel
            {
                Dock = DockStyle.Top,
                Height = 126,
                BackColor = Theme.Surface,
                Padding = new Padding(28, 16, 28, 0)
            };

            var lblTitle = new Label
            {
                Text = "👑 Master Admin Panel",
                Font = UiStyleConstants.PageTitleFont,
                ForeColor = Theme.TextPrimary,
                AutoSize = true,
                Location = new Point(28, 16)
            };
            _pnlHeader.Controls.Add(lblTitle);

            var lblSubtitle = new Label
            {
                Text = "Platform Oversight — Cross-Tenant Business Intelligence & Subscription Tier Management",
                Font = UiStyleConstants.SubtitleFont,
                ForeColor = Theme.TextSecondary,
                AutoSize = true,
                Location = new Point(28, 52)
            };
            _pnlHeader.Controls.Add(lblSubtitle);

            // Tab Buttons
            var pnlTabs = new Panel
            {
                Location = new Point(28, 84),
                Size = new Size(500, 35)
            };

            _btnTabBi = new Button
            {
                Text = "📊  Business Intelligence",
                Size = new Size(200, 32),
                Location = new Point(0, 0),
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 9.5f, FontStyle.Bold),
                Cursor = Cursors.Hand
            };
            UiRadiusHelper.StyleButton(_btnTabBi, 4);

            _btnTabSubs = new Button
            {
                Text = "💳  Subscriptions & Plans",
                Size = new Size(200, 32),
                Location = new Point(210, 0),
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 9.5f, FontStyle.Regular),
                Cursor = Cursors.Hand
            };
            UiRadiusHelper.StyleButton(_btnTabSubs, 4);

            _btnTabBi.Click += (_, _) => SwitchTab(true);
            _btnTabSubs.Click += (_, _) => SwitchTab(false);

            pnlTabs.Controls.Add(_btnTabBi);
            pnlTabs.Controls.Add(_btnTabSubs);
            _pnlHeader.Controls.Add(pnlTabs);

            // Refresh Button
            var btnRefresh = new Button
            {
                Text = "↻ Refresh",
                Size = new Size(90, 32),
                Location = new Point(_pnlHeader.Width - 114, 20),
                Anchor = AnchorStyles.Top | AnchorStyles.Right,
                Font = new Font("Segoe UI", 9f, FontStyle.Bold),
                Cursor = Cursors.Hand,
                BackColor = Theme.Surface,
                ForeColor = Theme.TextPrimary
            };
            UiRadiusHelper.StyleButton(btnRefresh, 6);
            btnRefresh.Click += (_, _) => LoadDataAsync();
            _pnlHeader.Controls.Add(btnRefresh);

            Controls.Add(_pnlHeader);

            // 2. BI TAB CONTENT
            BuildBiContentPanel();

            // 3. SUBSCRIPTIONS TAB CONTENT
            BuildSubsContentPanel();

            // Default: Show Business Intelligence tab
            SwitchTab(true);
        }

        private void SwitchTab(bool showBi)
        {
            _pnlBiContent.Visible = showBi;
            _pnlSubsContent.Visible = !showBi;

            if (showBi)
            {
                _btnTabBi.BackColor = Theme.Primary;
                _btnTabBi.ForeColor = Color.White;
                _btnTabBi.Font = new Font("Segoe UI", 9.5f, FontStyle.Bold);

                _btnTabSubs.BackColor = Theme.Surface;
                _btnTabSubs.ForeColor = Theme.TextSecondary;
                _btnTabSubs.Font = new Font("Segoe UI", 9.5f, FontStyle.Regular);
            }
            else
            {
                _btnTabSubs.BackColor = Theme.Primary;
                _btnTabSubs.ForeColor = Color.White;
                _btnTabSubs.Font = new Font("Segoe UI", 9.5f, FontStyle.Bold);

                _btnTabBi.BackColor = Theme.Surface;
                _btnTabBi.ForeColor = Theme.TextSecondary;
                _btnTabBi.Font = new Font("Segoe UI", 9.5f, FontStyle.Regular);
            }
        }

        private void BuildBiContentPanel()
        {
            _pnlBiContent = new Panel
            {
                Dock = DockStyle.Fill,
                AutoScroll = true,
                Padding = new Padding(24, 20, 24, 24)
            };

            // KPI Grid Panel
            var pnlKpis = new TableLayoutPanel
            {
                Dock = DockStyle.Top,
                Height = 110,
                ColumnCount = 4,
                RowCount = 1,
                Padding = new Padding(0, 0, 0, 16)
            };
            pnlKpis.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25f));
            pnlKpis.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25f));
            pnlKpis.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25f));
            pnlKpis.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25f));

            _kpiTenants = new KpiCard { Dock = DockStyle.Fill };
            _kpiActiveSubs = new KpiCard { Dock = DockStyle.Fill };
            _kpiMrr = new KpiCard { Dock = DockStyle.Fill };
            _kpiTransactions = new KpiCard { Dock = DockStyle.Fill };

            pnlKpis.Controls.Add(_kpiTenants, 0, 0);
            pnlKpis.Controls.Add(_kpiActiveSubs, 1, 0);
            pnlKpis.Controls.Add(_kpiMrr, 2, 0);
            pnlKpis.Controls.Add(_kpiTransactions, 3, 0);

            _pnlBiContent.Controls.Add(pnlKpis);

            // Cards row
            var pnlCardsRow = new TableLayoutPanel
            {
                Dock = DockStyle.Top,
                Height = 360,
                ColumnCount = 2,
                RowCount = 1,
                Padding = new Padding(0, 12, 0, 16)
            };
            pnlCardsRow.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50f));
            pnlCardsRow.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50f));

            // Plan Distribution Card
            _pnlPlanDistribution = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Theme.Surface,
                Padding = new Padding(20),
                Margin = new Padding(0, 0, 8, 0)
            };
            UiRadiusHelper.StyleCard(_pnlPlanDistribution, 8);

            var lblDistTitle = new Label
            {
                Text = "Subscription Tiers Breakdown (Exam Model)",
                Font = new Font("Segoe UI", 12f, FontStyle.Bold),
                ForeColor = Theme.TextPrimary,
                Dock = DockStyle.Top,
                Height = 28
            };
            var lblDistSub = new Label
            {
                Text = "Distribution of clients across Tenant A, Tenant B, and Tenant C tiers",
                Font = new Font("Segoe UI", 9f),
                ForeColor = Theme.TextSecondary,
                Dock = DockStyle.Top,
                Height = 22
            };
            _pnlPlanDistribution.Controls.Add(lblDistSub);
            _pnlPlanDistribution.Controls.Add(lblDistTitle);

            // Platform Activity Card
            _pnlPlatformActivity = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Theme.Surface,
                Padding = new Padding(20),
                Margin = new Padding(8, 0, 0, 0)
            };
            UiRadiusHelper.StyleCard(_pnlPlatformActivity, 8);

            var lblActTitle = new Label
            {
                Text = "Subscription Health Overview",
                Font = new Font("Segoe UI", 12f, FontStyle.Bold),
                ForeColor = Theme.TextPrimary,
                Dock = DockStyle.Top,
                Height = 28
            };
            var lblActSub = new Label
            {
                Text = "Active · Expiring · Expired subscription counts — aggregate from master database only",
                Font = new Font("Segoe UI", 9f),
                ForeColor = Theme.TextSecondary,
                Dock = DockStyle.Top,
                Height = 22
            };
            _pnlPlatformActivity.Controls.Add(lblActSub);
            _pnlPlatformActivity.Controls.Add(lblActTitle);

            pnlCardsRow.Controls.Add(_pnlPlanDistribution, 0, 0);
            pnlCardsRow.Controls.Add(_pnlPlatformActivity, 1, 0);

            _pnlBiContent.Controls.Add(pnlCardsRow);
            Controls.Add(_pnlBiContent);
        }

        private void BuildSubsContentPanel()
        {
            _pnlSubsContent = new Panel
            {
                Dock = DockStyle.Fill,
                AutoScroll = true,
                Padding = new Padding(24, 16, 24, 24)
            };

            // Toolbar
            var pnlToolbar = new Panel
            {
                Dock = DockStyle.Top,
                Height = 50,
                Padding = new Padding(0, 0, 0, 10)
            };

            _txtSearch = new TextBox
            {
                PlaceholderText = "Search tenant company name or code...",
                Font = new Font("Segoe UI", 10f),
                Size = new Size(280, 32),
                Location = new Point(0, 6)
            };
            UiRadiusHelper.SetPadding(_txtSearch, 6, 6);
            _txtSearch.TextChanged += (_, _) => ApplyFilter();
            pnlToolbar.Controls.Add(_txtSearch);

            // Filter Pills
            int pillX = 295;
            string[] pills = { "All", "Tenant A", "Tenant B", "Tenant C" };
            foreach (var pill in pills)
            {
                var btnPill = new Button
                {
                    Text = pill,
                    Location = new Point(pillX, 6),
                    Size = new Size(pill == "All" ? 50 : 85, 30),
                    FlatStyle = FlatStyle.Flat,
                    Font = new Font("Segoe UI", 9f),
                    BackColor = pill == _activeTierFilter ? Theme.Primary : Theme.Surface,
                    ForeColor = pill == _activeTierFilter ? Color.White : Theme.TextSecondary,
                    Cursor = Cursors.Hand
                };
                UiRadiusHelper.StyleButton(btnPill, 4);
                btnPill.Click += (s, _) =>
                {
                    _activeTierFilter = pill;
                    foreach (Control c in pnlToolbar.Controls)
                    {
                        if (c is Button b && pills.Contains(b.Text))
                        {
                            b.BackColor = b.Text == _activeTierFilter ? Theme.Primary : Theme.Surface;
                            b.ForeColor = b.Text == _activeTierFilter ? Color.White : Theme.TextSecondary;
                        }
                    }
                    ApplyFilter();
                };
                pnlToolbar.Controls.Add(btnPill);
                pillX += btnPill.Width + 6;
            }

            // Action Buttons Container
            var pnlSubsActions = new FlowLayoutPanel
            {
                FlowDirection = FlowDirection.RightToLeft,
                Anchor = AnchorStyles.Top | AnchorStyles.Right,
                Height = 36,
                Width = 480,
                Location = new Point(pnlToolbar.Width - 480, 4),
                BackColor = Color.Transparent
            };
            pnlToolbar.SizeChanged += (_, _) =>
            {
                pnlSubsActions.Location = new Point(pnlToolbar.Width - 480, 4);
            };

            _btnChangeTier = new Button
            {
                Text = "⚡ Change Tier / Plan",
                Size = new Size(150, 32),
                BackColor = Theme.Surface,
                ForeColor = Theme.TextPrimary,
                Font = new Font("Segoe UI", 9f, FontStyle.Bold),
                Cursor = Cursors.Hand,
                Margin = new Padding(4, 0, 0, 0)
            };
            UiRadiusHelper.StyleButton(_btnChangeTier, 6);
            _btnChangeTier.Click += BtnChangeTier_Click;

            _btnPaymentHistory = new Button
            {
                Text = "📋 Payment History",
                Size = new Size(150, 32),
                BackColor = Theme.Surface,
                ForeColor = Theme.TextPrimary,
                Font = new Font("Segoe UI", 9f, FontStyle.Bold),
                Cursor = Cursors.Hand,
                Margin = new Padding(4, 0, 0, 0)
            };
            UiRadiusHelper.StyleButton(_btnPaymentHistory, 6);
            _btnPaymentHistory.Click += BtnPaymentHistory_Click;

            _btnRecordPayment = new Button
            {
                Text = "💳 Record Payment",
                Size = new Size(150, 32),
                BackColor = Theme.Primary,
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 9f, FontStyle.Bold),
                Cursor = Cursors.Hand,
                Margin = new Padding(4, 0, 0, 0)
            };
            UiRadiusHelper.StyleButton(_btnRecordPayment, 6);
            _btnRecordPayment.Click += BtnRecordPayment_Click;

            pnlSubsActions.Controls.Add(_btnChangeTier);
            pnlSubsActions.Controls.Add(_btnPaymentHistory);
            pnlSubsActions.Controls.Add(_btnRecordPayment);
            pnlToolbar.Controls.Add(pnlSubsActions);

            // DataGridView Card Container
            var pnlGridCard = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Theme.Surface,
                Padding = new Padding(1)
            };
            UiRadiusHelper.StyleCard(pnlGridCard, 8);

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
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill
            };
            UiGridHelper.ApplyModernGridStyle(_grid);

            _grid.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Company Code", DataPropertyName = "CompanyCode", FillWeight = 15 });
            _grid.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Company Name", DataPropertyName = "CompanyName", FillWeight = 30 });
            _grid.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Plan Tier", DataPropertyName = "PlanName", FillWeight = 18 });
            _grid.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Monthly Billing", DataPropertyName = "BillingAmountFormatted", FillWeight = 16 });
            _grid.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Status", DataPropertyName = "Status", FillWeight = 12 });
            _grid.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "End Date", DataPropertyName = "EndDateFormatted", FillWeight = 16 });
            UiGridHelper.AddActionsColumn(_grid, 60);

            _grid.CellContentClick += Grid_CellContentClick;

            _grid.CellFormatting += (s, e) =>
            {
                if (e.RowIndex >= 0)
                {
                    // Style Plan Tier
                    if (_grid.Columns[e.ColumnIndex].HeaderText == "Plan Tier" && e.Value != null)
                    {
                        var val = e.Value.ToString() ?? "";
                        if (val.Contains("Tenant C")) e.CellStyle.ForeColor = Color.FromArgb(109, 40, 217); // Purple
                        else if (val.Contains("Tenant B")) e.CellStyle.ForeColor = Color.FromArgb(3, 105, 161); // Blue
                        else e.CellStyle.ForeColor = Color.FromArgb(71, 85, 105); // Slate
                        e.CellStyle.Font = new Font("Segoe UI", 9.5f, FontStyle.Bold);
                    }
                    // Style Status
                    if (_grid.Columns[e.ColumnIndex].HeaderText == "Status" && e.Value != null)
                    {
                        var val = e.Value.ToString() ?? "";
                        e.CellStyle.ForeColor = StatusColorHelper.GetTextColor(val);
                        e.CellStyle.Font = new Font("Segoe UI", 9.5f, FontStyle.Bold);
                    }
                }
            };

            pnlGridCard.Controls.Add(_grid);
            _gridSkeleton = GridSkeletonOverlay.CreateForGrid(_grid);

            // Correct docking order: Fill added first, Top added second
            _pnlSubsContent.Controls.Add(pnlGridCard);
            _pnlSubsContent.Controls.Add(pnlToolbar);

            Controls.Add(_pnlSubsContent);
        }

        private static void ConfigureKpi(KpiCard card, string title, object value, string subtitle, Color accentColor, KpiIconType icon)
        {
            card.SetTitle(title);
            if (value is int intVal) card.SetValue(intVal);
            else card.SetValue(value?.ToString() ?? "0");
            card.SetSubtitle(subtitle, Color.FromArgb(100, 116, 139));
            card.SetIcon(icon, accentColor);
        }

        private async void LoadDataAsync()
        {
            _kpiTenants.ShowLoadingSkeleton();
            _kpiActiveSubs.ShowLoadingSkeleton();
            _kpiMrr.ShowLoadingSkeleton();
            _kpiTransactions.ShowLoadingSkeleton();
            _gridSkeleton?.ShowSkeleton();

            try
            {
                // 1. Load Platform BI
                var bi = await _controller.GetPlatformBiSummaryAsync();

                ConfigureKpi(_kpiTenants, "TOTAL TENANTS", bi.TotalTenants, "Platform client databases", Theme.Primary, KpiIconType.Building);
                ConfigureKpi(_kpiActiveSubs, "ACTIVE SUBSCRIPTIONS", bi.ActiveSubscriptions, "Current paid tenants", Theme.StatusSuccess, KpiIconType.Target);
                ConfigureKpi(_kpiMrr, "TOTAL MRR", $"₱{bi.TotalMrr:N0}", "Monthly recurring revenue", Theme.PrimaryDark, KpiIconType.Currency);
                ConfigureKpi(_kpiTransactions, "EXPIRING SOON", bi.ExpiringSubscriptions, "Expiring within 30 days", Theme.StatusPending, KpiIconType.Clock);

                RenderPlanDistributionCard(bi);
                RenderPlatformActivityCard(bi);

                // 2. Load Subscriptions
                _allSubscriptions = await _controller.GetAllSubscriptionsAsync();
                ApplyFilter();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to load Master Admin data: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                _kpiTenants.HideLoadingSkeleton();
                _kpiActiveSubs.HideLoadingSkeleton();
                _kpiMrr.HideLoadingSkeleton();
                _kpiTransactions.HideLoadingSkeleton();
                _gridSkeleton?.HideSkeleton();
            }
        }

        private void RenderPlanDistributionCard(PlatformBiSummaryDto bi)
        {
            // Clear dynamic rows
            var toRemove = _pnlPlanDistribution.Controls.Cast<Control>().Where(c => c.Top > 60).ToList();
            foreach (var c in toRemove) _pnlPlanDistribution.Controls.Remove(c);

            int total = Math.Max(1, bi.TenantACount + bi.TenantBCount + bi.TenantCCount);

            var items = new (string Tier, string Capabilities, int Count, Color BarColor)[]
            {
                ("Tenant C (Enterprise)", "Branching · Business Intelligence · Actions", bi.TenantCCount, Color.FromArgb(124, 58, 237)),
                ("Tenant B (Professional)", "Business Intelligence · Actions", bi.TenantBCount, Color.FromArgb(14, 165, 233)),
                ("Tenant A (Starter)", "Main Transaction · Data Collection", bi.TenantACount, Color.FromArgb(100, 116, 139))
            };

            int y = 65;
            foreach (var item in items)
            {
                var lblName = new Label
                {
                    Text = $"{item.Tier}  —  {item.Count} Tenants",
                    Font = new Font("Segoe UI", 9.5f, FontStyle.Bold),
                    ForeColor = Theme.TextPrimary,
                    Location = new Point(20, y),
                    AutoSize = true
                };
                _pnlPlanDistribution.Controls.Add(lblName);

                var lblDesc = new Label
                {
                    Text = $"Features: {item.Capabilities}",
                    Font = new Font("Segoe UI", 8.5f),
                    ForeColor = Theme.TextSecondary,
                    Location = new Point(20, y + 20),
                    AutoSize = true
                };
                _pnlPlanDistribution.Controls.Add(lblDesc);

                // Progress Bar Container
                var pnlBarBg = new Panel
                {
                    Location = new Point(20, y + 42),
                    Size = new Size(_pnlPlanDistribution.Width - 44, 10),
                    Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right,
                    BackColor = Color.FromArgb(241, 245, 249)
                };
                UiRadiusHelper.StyleCard(pnlBarBg, 5);

                int fillWidth = Math.Max(10, (int)((float)item.Count / total * pnlBarBg.Width));
                var pnlFill = new Panel
                {
                    Location = new Point(0, 0),
                    Size = new Size(fillWidth, 10),
                    BackColor = item.BarColor
                };
                pnlBarBg.Controls.Add(pnlFill);
                _pnlPlanDistribution.Controls.Add(pnlBarBg);

                y += 65;
            }
        }

        private void RenderPlatformActivityCard(PlatformBiSummaryDto bi)
        {
            var toRemove = _pnlPlatformActivity.Controls.Cast<Control>().Where(c => c.Top > 60).ToList();
            foreach (var c in toRemove) _pnlPlatformActivity.Controls.Remove(c);

            // Legal aggregate-only data — subscription counts from MasterCrmsDbContext only
            var items = new (string Metric, string Value, string Subtitle, string Icon)[]
            {
                ("Active Subscriptions", $"{bi.ActiveSubscriptions} tenants", "Currently paying, plan is live", "✅"),
                ("Expiring This Month", $"{bi.ExpiringSubscriptions} subscriptions", "Need renewal attention within 30 days", "⏰"),
                ("Expired Subscriptions", $"{bi.ExpiredSubscriptions} plans", "Lapsed — require re-activation", "⚠")
            };

            int y = 65;
            foreach (var item in items)
            {
                var pnlRow = new Panel
                {
                    Location = new Point(20, y),
                    Size = new Size(_pnlPlatformActivity.Width - 44, 55),
                    Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right,
                    BackColor = Color.FromArgb(248, 250, 252)
                };
                UiRadiusHelper.StyleCard(pnlRow, 6);

                var lblIcon = new Label
                {
                    Text = item.Icon,
                    Font = new Font("Segoe UI", 16f),
                    Location = new Point(12, 10),
                    Size = new Size(36, 36)
                };
                pnlRow.Controls.Add(lblIcon);

                var lblMetric = new Label
                {
                    Text = item.Metric,
                    Font = new Font("Segoe UI", 9.5f, FontStyle.Bold),
                    ForeColor = Theme.TextPrimary,
                    Location = new Point(55, 8),
                    AutoSize = true
                };
                pnlRow.Controls.Add(lblMetric);

                var lblSub = new Label
                {
                    Text = item.Subtitle,
                    Font = new Font("Segoe UI", 8.5f),
                    ForeColor = Theme.TextSecondary,
                    Location = new Point(55, 28),
                    AutoSize = true
                };
                pnlRow.Controls.Add(lblSub);

                var lblVal = new Label
                {
                    Text = item.Value,
                    Font = new Font("Segoe UI", 11f, FontStyle.Bold),
                    ForeColor = Theme.Primary,
                    Anchor = AnchorStyles.Top | AnchorStyles.Right,
                    Location = new Point(pnlRow.Width - 190, 14),
                    Size = new Size(180, 26),
                    TextAlign = ContentAlignment.MiddleRight
                };
                pnlRow.Controls.Add(lblVal);

                _pnlPlatformActivity.Controls.Add(pnlRow);
                y += 65;
            }
        }

        private void ApplyFilter()
        {
            var query = _allSubscriptions.AsEnumerable();

            if (!string.IsNullOrWhiteSpace(_txtSearch.Text))
            {
                var s = _txtSearch.Text.Trim();
                query = query.Where(x => x.CompanyName.Contains(s, StringComparison.OrdinalIgnoreCase) ||
                                         x.CompanyCode.Contains(s, StringComparison.OrdinalIgnoreCase));
            }

            if (_activeTierFilter != "All")
            {
                query = query.Where(x => x.PlanName.Contains(_activeTierFilter, StringComparison.OrdinalIgnoreCase));
            }

            var displayList = query.Select(x => new
            {
                x.SubscriptionId,
                x.CompanyId,
                x.CompanyCode,
                x.CompanyName,
                x.PlanName,
                BillingAmountFormatted = $"₱{x.BillingAmount:N2}",
                Status = Subscription.CalculateStatus(x.EndDate),
                EndDateFormatted = x.EndDate?.ToString("MMM dd, yyyy") ?? "Lifetime"
            }).ToList();

            _grid.DataSource = displayList;
            if (_grid.Columns["SubscriptionId"] is { } subCol) subCol.Visible = false;
            if (_grid.Columns["CompanyId"] is { } compCol) compCol.Visible = false;
        }

        private void Grid_CellContentClick(object? sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex < 0 || e.ColumnIndex < 0) return;
            if (_grid.Columns[e.ColumnIndex] is not ActionsColumn && _grid.Columns[e.ColumnIndex].Name != "Actions") return;

            if (_grid.Rows[e.RowIndex].DataBoundItem is not { } boundItem) return;
            dynamic row = boundItem;
            int subId = (int)row.SubscriptionId;
            int compId = (int)row.CompanyId;
            var sub = _allSubscriptions.FirstOrDefault(s => s.SubscriptionId == subId || s.CompanyId == compId);
            if (sub == null) return;

            var menu = new ContextMenuStrip();

            var recordPaymentItem = new ToolStripMenuItem("💳  Record Payment");
            recordPaymentItem.Click += (_, _) =>
            {
                using var dlg = new RecordPaymentDialog(sub);
                if (dlg.ShowDialog(this) == DialogResult.OK)
                {
                    LoadDataAsync();
                }
            };
            menu.Items.Add(recordPaymentItem);

            var historyItem = new ToolStripMenuItem("📋  Payment History");
            historyItem.Click += (_, _) =>
            {
                using var dlg = new PaymentHistoryDialog(sub);
                dlg.ShowDialog(this);
                LoadDataAsync();
            };
            menu.Items.Add(historyItem);

            menu.Items.Add(new ToolStripSeparator());

            var changeTierItem = new ToolStripMenuItem("⚡  Change Tier / Plan");
            changeTierItem.Click += (_, _) =>
            {
                using var dlg = new SubscriptionTierChangeDialog(sub.CompanyName, sub.PlanName, sub.Status, sub.BillingAmount);
                if (dlg.ShowDialog(this) == DialogResult.OK)
                {
                    _ = UpdateTierAndReload(sub.SubscriptionId, sub.CompanyId, sub.CompanyName, dlg);
                }
            };
            menu.Items.Add(changeTierItem);

            var cellRect = _grid.GetCellDisplayRectangle(e.ColumnIndex, e.RowIndex, false);
            menu.Show(_grid, new Point(cellRect.Left, cellRect.Bottom));
        }

        private async Task UpdateTierAndReload(int subId, int compId, string compName, SubscriptionTierChangeDialog dlg)
        {
            bool ok = await _controller.UpdateSubscriptionAsync(subId, dlg.SelectedPlan, dlg.SelectedStatus, dlg.BillingAmount, DateTime.UtcNow.AddYears(1));
            if (ok)
            {
                MessageBox.Show($"Tenant '{compName}' successfully updated to {dlg.SelectedPlan} ({dlg.SelectedStatus})!", "Plan Updated", MessageBoxButtons.OK, MessageBoxIcon.Information);
                LoadDataAsync();
            }
            else
            {
                await _controller.ChangeTenantTierAsync(compId, dlg.SelectedPlan);
                LoadDataAsync();
            }
        }

        private void BtnRecordPayment_Click(object? sender, EventArgs e)
        {
            if (_grid.CurrentRow?.DataBoundItem == null)
            {
                MessageBox.Show("Please select a tenant company from the grid to record payment.", "Notice", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            dynamic row = _grid.CurrentRow.DataBoundItem;
            int subId = row.SubscriptionId;
            int compId = row.CompanyId;
            var sub = _allSubscriptions.FirstOrDefault(s => s.SubscriptionId == subId || s.CompanyId == compId);
            if (sub == null) return;

            using var dlg = new RecordPaymentDialog(sub);
            if (dlg.ShowDialog(this) == DialogResult.OK)
            {
                LoadDataAsync();
            }
        }

        private void BtnPaymentHistory_Click(object? sender, EventArgs e)
        {
            if (_grid.CurrentRow?.DataBoundItem == null)
            {
                MessageBox.Show("Please select a tenant company from the grid to view payment history.", "Notice", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            dynamic row = _grid.CurrentRow.DataBoundItem;
            int subId = row.SubscriptionId;
            int compId = row.CompanyId;
            var sub = _allSubscriptions.FirstOrDefault(s => s.SubscriptionId == subId || s.CompanyId == compId);
            if (sub == null) return;

            using var dlg = new PaymentHistoryDialog(sub);
            dlg.ShowDialog(this);
            LoadDataAsync();
        }

        private async void BtnChangeTier_Click(object? sender, EventArgs e)
        {
            if (_grid.CurrentRow?.DataBoundItem == null)
            {
                MessageBox.Show("Please select a tenant company from the grid to modify its plan.", "Notice", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            dynamic row = _grid.CurrentRow.DataBoundItem;
            int subId = row.SubscriptionId;
            int compId = row.CompanyId;
            string compName = row.CompanyName;
            string currentPlan = row.PlanName;
            string currentStatus = row.Status;

            var currentSub = _allSubscriptions.FirstOrDefault(s => s.CompanyId == compId);
            decimal currentAmount = currentSub?.BillingAmount ?? 2500m;

            using var dlg = new SubscriptionTierChangeDialog(compName, currentPlan, currentStatus, currentAmount);
            if (dlg.ShowDialog(this) == DialogResult.OK)
            {
                bool ok = await _controller.UpdateSubscriptionAsync(subId, dlg.SelectedPlan, dlg.SelectedStatus, dlg.BillingAmount, DateTime.UtcNow.AddYears(1));
                if (ok)
                {
                    MessageBox.Show($"Tenant '{compName}' successfully updated to {dlg.SelectedPlan} ({dlg.SelectedStatus})!", "Plan Updated", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    LoadDataAsync();
                }
                else
                {
                    // Fallback create / change tier
                    await _controller.ChangeTenantTierAsync(compId, dlg.SelectedPlan);
                    LoadDataAsync();
                }
            }
        }

        private async void BtnRenew_Click(object? sender, EventArgs e)
        {
            if (_grid.CurrentRow?.DataBoundItem == null)
            {
                MessageBox.Show("Please select a tenant company to renew.", "Notice", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            dynamic row = _grid.CurrentRow.DataBoundItem;
            int subId = row.SubscriptionId;
            string compName = row.CompanyName;
            string currentPlan = row.PlanName;

            var currentSub = _allSubscriptions.FirstOrDefault(s => s.SubscriptionId == subId);
            decimal amount = currentSub?.BillingAmount ?? 2500m;
            var newEndDate = (currentSub?.EndDate ?? DateTime.UtcNow).AddYears(1);

            bool ok = await _controller.UpdateSubscriptionAsync(subId, currentPlan, "Active", amount, newEndDate);
            if (ok)
            {
                MessageBox.Show($"Subscription for '{compName}' renewed for 1 year through {newEndDate:MMM dd, yyyy}!", "Renewed", MessageBoxButtons.OK, MessageBoxIcon.Information);
                LoadDataAsync();
            }
        }
    }
}
