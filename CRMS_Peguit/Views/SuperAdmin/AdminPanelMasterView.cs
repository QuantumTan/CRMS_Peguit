using System;
using System.Collections.Generic;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
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
        private readonly bool _reportsOnly;
        private readonly bool _termsOnly;
        private bool _loading;
        private bool _reportsReady;
        private DateTimePicker _reportFrom = null!;
        private DateTimePicker _reportTo = null!;
        private List<TenantSubscriptionDto> _allSubscriptions = new();
        private string _activeTierFilter = "All";

        // Layout Containers
        private Panel _pnlHeader = null!;
        private Button _btnTabBi = null!;
        private Button _btnTabSubs = null!;
        private Button _btnTabReports = null!;
        private Button _btnTabTerms = null!;
        private Panel _pnlBiContent = null!;
        private Panel _pnlSubsContent = null!;
        private Panel _pnlReportsContent = null!;
        private Panel _pnlTermsContent = null!;

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

        // Platform reports and legal terms
        private ComboBox _cmbReportType = null!;
        private TextBox _txtReportSearch = null!;
        private DataGridView _gridReports = null!;
        private Label _lblReportSummary = null!;
        private RichTextBox _txtMasterTerms = null!;
        private List<PaymentRecordDto> _allPayments = new();

        private enum AdminPanelTab
        {
            BusinessIntelligence,
            Subscriptions,
            Reports,
            Terms
        }

        public AdminPanelMasterView(bool openReports = false, bool openTerms = false, bool loadData = true)
        {
            _reportsOnly = openReports;
            _termsOnly = openTerms;
            _controller = new SuperAdminSubscriptionController();
            InitializeComponent();
            if (openReports)
            {
                SwitchTab(AdminPanelTab.Reports);
            }
            else if (openTerms) SwitchTab(AdminPanelTab.Terms);
            if (loadData) LoadDataAsync();
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
                Text = _reportsOnly ? "Platform Reports" : _termsOnly ? "Terms & Conditions" : "Master Admin Panel",
                Font = UiStyleConstants.PageTitleFont,
                ForeColor = Theme.TextPrimary,
                AutoSize = true,
                Location = new Point(28, 16)
            };
            _pnlHeader.Controls.Add(lblTitle);

            var lblSubtitle = new Label
            {
                Text = _reportsOnly ? "Subscription portfolio and payment collections — master database only"
                    : _termsOnly ? "Review and maintain the tenant onboarding agreement"
                    : "Platform Oversight — Business Intelligence & Subscription Management",
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
                Size = new Size(790, 35)
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

            _btnTabReports = new Button
            {
                Text = "📋  Platform Reports",
                Size = new Size(175, 32),
                Location = new Point(420, 0),
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 9.5f),
                Cursor = Cursors.Hand
            };
            UiRadiusHelper.StyleButton(_btnTabReports, 4);

            _btnTabTerms = new Button
            {
                Text = "📜  Terms & Conditions",
                Size = new Size(185, 32),
                Location = new Point(605, 0),
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 9.5f),
                Cursor = Cursors.Hand
            };
            UiRadiusHelper.StyleButton(_btnTabTerms, 4);

            _btnTabBi.Click += (_, _) => SwitchTab(AdminPanelTab.BusinessIntelligence);
            _btnTabSubs.Click += (_, _) => SwitchTab(AdminPanelTab.Subscriptions);
            _btnTabReports.Click += (_, _) => SwitchTab(AdminPanelTab.Reports);
            _btnTabTerms.Click += (_, _) => SwitchTab(AdminPanelTab.Terms);

            pnlTabs.Controls.Add(_btnTabBi);
            pnlTabs.Controls.Add(_btnTabSubs);
            pnlTabs.Controls.Add(_btnTabReports);
            pnlTabs.Controls.Add(_btnTabTerms);
            _pnlHeader.Controls.Add(pnlTabs);
            if (_reportsOnly || _termsOnly)
            {
                pnlTabs.Visible = false;
                _pnlHeader.Height = 80;
            }

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
            if (_reportsOnly || _termsOnly)
                ResponsiveLayout.BindHeader(_pnlHeader, lblTitle, lblSubtitle, btnRefresh);
            else
            {
                bool headerBusy = false;
                void LayoutMasterHeader()
                {
                    if (headerBusy || _pnlHeader.Width <= 0) return;
                    headerBusy = true;
                    try
                    {
                        int width = Math.Max(1, _pnlHeader.ClientSize.Width - 56);
                        int titleBottom = ResponsiveLayout.LabelBlock(lblTitle, 28, 16, Math.Max(1, width - 110));
                        int subtitleBottom = ResponsiveLayout.LabelBlock(lblSubtitle, 28, titleBottom + 4, width);
                        pnlTabs.Location = new Point(28, subtitleBottom + 12);
                        pnlTabs.Width = width;
                        _btnTabBi.Width = _btnTabSubs.Width = ResponsiveLayout.Scale(this, 200);
                        _btnTabReports.Width = ResponsiveLayout.Scale(this, 175);
                        _btnTabTerms.Width = ResponsiveLayout.Scale(this, 185);
                        int tabsBottom = ResponsiveLayout.Flow(pnlTabs, new Control[] { _btnTabBi, _btnTabSubs, _btnTabReports, _btnTabTerms }, 0);
                        pnlTabs.Height = tabsBottom;
                        _pnlHeader.Height = pnlTabs.Bottom + 12;
                        btnRefresh.Location = new Point(_pnlHeader.ClientSize.Width - 118, 20);
                    }
                    finally { headerBusy = false; }
                }
                _pnlHeader.SizeChanged += (_, _) => LayoutMasterHeader();
                _pnlHeader.VisibleChanged += (_, _) => LayoutMasterHeader();
            }

            Controls.Add(_pnlHeader);

            // 2. BI TAB CONTENT
            BuildBiContentPanel();

            // 3. SUBSCRIPTIONS TAB CONTENT
            BuildSubsContentPanel();

            // 4. REPORTS TAB CONTENT
            BuildReportsContentPanel();

            // 5. TERMS TAB CONTENT
            BuildTermsContentPanel();

            // Default: Show Business Intelligence tab
            SwitchTab(AdminPanelTab.BusinessIntelligence);
            _pnlHeader.SendToBack();
        }

        private void SwitchTab(AdminPanelTab activeTab)
        {
            _pnlBiContent.Visible = activeTab == AdminPanelTab.BusinessIntelligence;
            _pnlSubsContent.Visible = activeTab == AdminPanelTab.Subscriptions;
            _pnlReportsContent.Visible = activeTab == AdminPanelTab.Reports;
            _pnlTermsContent.Visible = activeTab == AdminPanelTab.Terms;

            var tabs = new[]
            {
                (_btnTabBi, AdminPanelTab.BusinessIntelligence),
                (_btnTabSubs, AdminPanelTab.Subscriptions),
                (_btnTabReports, AdminPanelTab.Reports),
                (_btnTabTerms, AdminPanelTab.Terms)
            };

            foreach (var (button, tab) in tabs)
            {
                bool selected = tab == activeTab;
                button.BackColor = selected ? Theme.Primary : Theme.Surface;
                button.ForeColor = selected ? Color.White : Theme.TextSecondary;
                button.Font = new Font("Segoe UI", 9.5f, selected ? FontStyle.Bold : FontStyle.Regular);
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
                Text = "Subscription Tiers Breakdown",
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
            pnlKpis.SendToBack();
            _pnlBiContent.SizeChanged += (_, _) => ResponsiveLayout.KpiGrid(pnlKpis, _pnlBiContent.ClientSize.Width - _pnlBiContent.Padding.Horizontal);
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
            // Actions share the same wrapping toolbar as search and filters, not an overlapping overlay.
            foreach (Control action in pnlSubsActions.Controls.Cast<Control>().ToArray()) pnlToolbar.Controls.Add(action);
            pnlToolbar.Controls.Remove(pnlSubsActions);
            pnlSubsActions.Dispose();
            ResponsiveLayout.BindToolbar(pnlToolbar, 4,
                new Control[] { _txtSearch }.Concat(pnlToolbar.Controls.OfType<Button>().Where(b => pills.Contains(b.Text)))
                    .Concat(new Control[] { _btnRecordPayment, _btnPaymentHistory, _btnChangeTier }).ToArray());

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

        private void BuildReportsContentPanel()
        {
            _pnlReportsContent = new Panel
            {
                Dock = DockStyle.Fill,
                Padding = new Padding(24, 16, 24, 24),
                BackColor = Theme.Background
            };

            var pnlToolbar = new Panel { Dock = DockStyle.Top, Height = 126 };
            var lblTitle = new Label
            {
                Text = "Platform Reports",
                Font = new Font("Segoe UI", 13f, FontStyle.Bold),
                ForeColor = Theme.TextPrimary,
                AutoSize = true,
                Location = new Point(0, 0)
            };
            pnlToolbar.Controls.Add(lblTitle);

            _cmbReportType = new ComboBox
            {
                DropDownStyle = ComboBoxStyle.DropDownList,
                Font = new Font("Segoe UI", 9.5f),
                Location = new Point(0, 38),
                Size = new Size(220, 30)
            };
            _cmbReportType.Items.AddRange(new object[] { "Subscription Portfolio", "Payment Collections" });
            _cmbReportType.SelectedIndex = 0;
            _cmbReportType.SelectedIndexChanged += (_, _) => RefreshReportsGrid();
            pnlToolbar.Controls.Add(_cmbReportType);

            _txtReportSearch = new TextBox
            {
                PlaceholderText = "Search company, code, plan, or reference...",
                Font = new Font("Segoe UI", 9.5f),
                Location = new Point(232, 38),
                Size = new Size(300, 30)
            };
            UiRadiusHelper.SetPadding(_txtReportSearch, 6, 6);
            _txtReportSearch.TextChanged += (_, _) => RefreshReportsGrid();
            pnlToolbar.Controls.Add(_txtReportSearch);

            var btnExport = new Button
            {
                Text = "Export CSV / PDF",
                Size = new Size(125, 32),
                Anchor = AnchorStyles.Top | AnchorStyles.Right,
                Location = new Point(pnlToolbar.Width - 125, 36),
                BackColor = Theme.Primary,
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 9f, FontStyle.Bold),
                Cursor = Cursors.Hand
            };
            UiRadiusHelper.StyleButton(btnExport, 6);
            pnlToolbar.SizeChanged += (_, _) => btnExport.Left = Math.Max(540, pnlToolbar.Width - btnExport.Width);
            btnExport.Click += (_, _) => ExportCurrentReport();
            pnlToolbar.Controls.Add(btnExport);
            var dates = new FlowLayoutPanel
            {
                Location = new Point(0, 78), Size = new Size(650, 36), WrapContents = false
            };
            dates.Controls.Add(new Label { Text = "Payment dates:", AutoSize = true, Margin = new Padding(0, 6, 10, 0) });
            _reportFrom = new DateTimePicker { Format = DateTimePickerFormat.Short, ShowCheckBox = true, Checked = false, Width = 140, Value = DateTime.Today.AddMonths(-1) };
            _reportTo = new DateTimePicker { Format = DateTimePickerFormat.Short, ShowCheckBox = true, Checked = false, Width = 140, Value = DateTime.Today };
            _reportFrom.ValueChanged += (_, _) => RefreshReportsGrid();
            _reportTo.ValueChanged += (_, _) => RefreshReportsGrid();
            dates.Controls.Add(_reportFrom);
            dates.Controls.Add(new Label { Text = "to", AutoSize = true, Margin = new Padding(8, 6, 8, 0) });
            dates.Controls.Add(_reportTo);
            pnlToolbar.Controls.Add(dates);
            // Let filters and export wrap independently; keep the date range on a separate row.
            dates.Dock = DockStyle.Bottom;
            dates.AutoSize = true;
            dates.WrapContents = true;
            bool reportLayoutBusy = false;
            void LayoutReportToolbar()
            {
                if (reportLayoutBusy) return;
                reportLayoutBusy = true;
                try
                {
                    _txtReportSearch.Width = Math.Min(300, Math.Max(1, pnlToolbar.ClientSize.Width));
                    _cmbReportType.Width = Math.Min(220, Math.Max(1, pnlToolbar.ClientSize.Width));
                    btnExport.Width = Math.Min(150, Math.Max(1, pnlToolbar.ClientSize.Width));
                    int bottom = ResponsiveLayout.Flow(pnlToolbar, new Control[] { _cmbReportType, _txtReportSearch, btnExport }, lblTitle.Bottom + 12);
                    pnlToolbar.Height = bottom + dates.PreferredSize.Height + 16;
                    ResponsiveLayout.Flow(pnlToolbar, new Control[] { _cmbReportType, _txtReportSearch, btnExport }, lblTitle.Bottom + 12);
                }
                finally { reportLayoutBusy = false; }
            }
            pnlToolbar.SizeChanged += (_, _) => LayoutReportToolbar();
            pnlToolbar.VisibleChanged += (_, _) => LayoutReportToolbar();

            _lblReportSummary = new Label
            {
                Dock = DockStyle.Bottom,
                Height = 24,
                Font = new Font("Segoe UI", 9f),
                ForeColor = Theme.TextSecondary,
                TextAlign = ContentAlignment.MiddleLeft
            };

            var pnlGridCard = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Theme.Surface,
                Padding = new Padding(1)
            };
            UiRadiusHelper.StyleCard(pnlGridCard, 8);

            _gridReports = new DataGridView
            {
                Dock = DockStyle.Fill,
                BackgroundColor = Theme.Surface,
                BorderStyle = BorderStyle.None,
                RowHeadersVisible = false,
                AllowUserToAddRows = false,
                AllowUserToDeleteRows = false,
                ReadOnly = true,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill
            };
            UiGridHelper.ApplyModernGridStyle(_gridReports);
            pnlGridCard.Controls.Add(_gridReports);

            _pnlReportsContent.Controls.Add(pnlGridCard);
            _pnlReportsContent.Controls.Add(_lblReportSummary);
            _pnlReportsContent.Controls.Add(pnlToolbar);
            Controls.Add(_pnlReportsContent);
        }

        private void BuildTermsContentPanel()
        {
            _pnlTermsContent = new Panel
            {
                Dock = DockStyle.Fill,
                Padding = new Padding(24, 16, 24, 24),
                BackColor = Theme.Background
            };

            var pnlHeader = new Panel { Dock = DockStyle.Top, Height = 64 };
            var lblTitle = new Label
            {
                Text = "Master Terms & Conditions",
                Font = new Font("Segoe UI", 13f, FontStyle.Bold),
                ForeColor = Theme.TextPrimary,
                AutoSize = true,
                Location = new Point(0, 0)
            };
            var lblSubtitle = new Label
            {
                Text = "This agreement is presented during tenant onboarding. Review it here and keep the legal copy current.",
                Font = new Font("Segoe UI", 9f),
                ForeColor = Theme.TextSecondary,
                AutoSize = true,
                Location = new Point(0, 30)
            };
            var btnEdit = new Button
            {
                Text = "✎ Edit Terms",
                Size = new Size(120, 32),
                Anchor = AnchorStyles.Top | AnchorStyles.Right,
                Location = new Point(pnlHeader.Width - 120, 4),
                BackColor = Theme.Primary,
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 9f, FontStyle.Bold),
                Cursor = Cursors.Hand
            };
            UiRadiusHelper.StyleButton(btnEdit, 6);
            pnlHeader.SizeChanged += (_, _) => btnEdit.Left = Math.Max(0, pnlHeader.Width - btnEdit.Width);
            btnEdit.Click += async (_, _) =>
            {
                using var dialog = new EditMasterTermsDialog();
                if (dialog.ShowDialog(this) == DialogResult.OK)
                {
                    _txtMasterTerms.Text = await _controller.GetMasterTermsAsync();
                }
            };
            pnlHeader.Controls.Add(lblTitle);
            pnlHeader.Controls.Add(lblSubtitle);
            pnlHeader.Controls.Add(btnEdit);
            ResponsiveLayout.BindHeader(pnlHeader, lblTitle, lblSubtitle, btnEdit);

            var pnlTermsCard = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Theme.Surface,
                Padding = new Padding(18)
            };
            UiRadiusHelper.StyleCard(pnlTermsCard, 8);

            _txtMasterTerms = new RichTextBox
            {
                Dock = DockStyle.Fill,
                ReadOnly = true,
                BorderStyle = BorderStyle.None,
                BackColor = Theme.Surface,
                ForeColor = Theme.TextPrimary,
                Font = new Font("Segoe UI", 10f),
                DetectUrls = true
            };
            pnlTermsCard.Controls.Add(_txtMasterTerms);

            _pnlTermsContent.Controls.Add(pnlTermsCard);
            _pnlTermsContent.Controls.Add(pnlHeader);
            Controls.Add(_pnlTermsContent);
        }

        private void RefreshReportsGrid()
        {
            if (_gridReports == null || _cmbReportType == null || _reportFrom == null || _reportTo == null) return;

            string search = _txtReportSearch?.Text.Trim() ?? string.Empty;
            {
                _reportFrom.Enabled = _reportTo.Enabled = _cmbReportType.SelectedIndex == 1;
                if (_cmbReportType.SelectedIndex == 1 && _reportFrom.Checked && _reportTo.Checked &&
                    _reportFrom.Value.Date > _reportTo.Value.Date)
                {
                    _gridReports.DataSource = null;
                    _lblReportSummary.Text = "The start date must be on or before the end date.";
                    return;
                }
            }
            bool matches(string value) => string.IsNullOrWhiteSpace(search) ||
                                          value.Contains(search, StringComparison.OrdinalIgnoreCase);

            if (_cmbReportType.SelectedIndex == 1)
            {
                var rows = _allPayments
                    .Where(p => !_reportFrom.Checked || p.PaymentDate.Date >= _reportFrom.Value.Date)
                    .Where(p => !_reportTo.Checked || p.PaymentDate.Date <= _reportTo.Value.Date)
                    .Where(p => matches($"{p.CompanyName} {p.CompanyCode} {p.PaymentReference} {p.PaymentMethodDisplay}"))
                    .OrderByDescending(p => p.PaymentDate)
                    .Select(p => new
                    {
                        Date = p.PaymentDate,
                        p.CompanyCode,
                        p.CompanyName,
                        Amount = p.AmountPaid,
                        Method = p.PaymentMethodDisplay,
                        Reference = p.PaymentReference,
                        RecordedBy = p.RecordedByName
                    })
                    .ToList();
                _gridReports.DataSource = rows;
                _lblReportSummary.Text = $"{rows.Count:N0} payment record(s) · Total collected: ₱{rows.Sum(r => r.Amount):N2}";
            }
            else
            {
                var subscriptions = _allSubscriptions
                    .Where(s => matches($"{s.CompanyName} {s.CompanyCode} {s.PlanName} {s.Status}"))
                    .ToList();
                var rows = subscriptions
                    .OrderBy(s => s.CompanyName)
                    .Select(s => new
                    {
                        s.CompanyCode,
                        s.CompanyName,
                        Plan = s.PlanName,
                        Status = PlatformReportRules.SubscriptionStatus(s),
                        MonthlyBilling = s.BillingAmount,
                        StartDate = s.StartDate.ToString("yyyy-MM-dd"),
                        EndDate = s.SubscriptionId == 0 ? "—" : s.EndDate?.ToString("yyyy-MM-dd") ?? "Lifetime"
                    })
                    .ToList();
                _gridReports.DataSource = rows;
                _lblReportSummary.Text = $"{rows.Count:N0} portfolio record(s) · Current MRR: ₱{PlatformReportRules.CurrentMrr(subscriptions):N2}";
            }

            UiGridHelper.EnforceTableStandards(_gridReports);
            foreach (DataGridViewColumn column in _gridReports.Columns)
            {
                if (column.Name == "Amount" || column.Name == "MonthlyBilling")
                    column.DefaultCellStyle.Format = "N2";
                if (column.Name == "Date") column.DefaultCellStyle.Format = "yyyy-MM-dd";
            }
        }

        private void ExportCurrentReport()
        {
            if (!_reportsReady || _loading) return;
            if (_gridReports.Rows.Count == 0)
            {
                MessageBox.Show("There are no report rows to export.", "Export Report", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            using var dialog = new SaveFileDialog
            {
                Filter = "CSV file (*.csv)|*.csv|PDF document (*.pdf)|*.pdf",
                AddExtension = true,
                FileName = $"platform-{(_cmbReportType.SelectedIndex == 1 ? "payments" : "subscriptions")}-{DateTime.Now:yyyyMMdd}.csv"
            };
            if (dialog.ShowDialog(this) != DialogResult.OK) return;

            static string EscapeCsv(object? value)
            {
                string text = value is DateTime date ? date.ToString("yyyy-MM-dd")
                    : Convert.ToString(value, CultureInfo.InvariantCulture) ?? string.Empty;
                if (value is string && text.TrimStart() is string trimmed && trimmed.Length > 0 &&
                    "=+-@".Contains(trimmed[0])) text = "'" + text;
                return $"\"{text.Replace("\"", "\"\"")}\"";
            }

            var csv = new StringBuilder();
            var visibleColumns = _gridReports.Columns.Cast<DataGridViewColumn>()
                .Where(c => c.Visible)
                .OrderBy(c => c.DisplayIndex)
                .ToList();
            csv.AppendLine(string.Join(",", visibleColumns.Select(c => EscapeCsv(c.HeaderText))));
            foreach (DataGridViewRow row in _gridReports.Rows)
            {
                if (row.IsNewRow) continue;
                csv.AppendLine(string.Join(",", visibleColumns.Select(c => EscapeCsv(row.Cells[c.Index].Value))));
            }

            try
            {
                if (dialog.FilterIndex == 2)
                {
                    var rows = _gridReports.Rows.Cast<DataGridViewRow>().Where(r => !r.IsNewRow)
                        .Select(r => visibleColumns.Select(c => Convert.ToString(r.Cells[c.Index].FormattedValue, CultureInfo.CurrentCulture) ?? "").ToArray()).ToList();
                    if (!PdfExportHelper.TryExportTable(_cmbReportType.Text, visibleColumns.Select(c => c.HeaderText).ToArray(), rows,
                        dialog.FileName, out var error, activeFilter: _lblReportSummary.Text))
                        throw new IOException(error);
                }
                else File.WriteAllText(dialog.FileName, csv.ToString(), new UTF8Encoding(encoderShouldEmitUTF8Identifier: true));
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Could not export the report: {ex.Message}", "Export Failed", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }
            MessageBox.Show("Platform report exported successfully.", "Export Complete", MessageBoxButtons.OK, MessageBoxIcon.Information);
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
            if (_loading || IsDisposed) return;
            _loading = true;
            _reportsReady = false;
            _pnlReportsContent.Enabled = false;
            _lblReportSummary.Text = "Loading platform records…";
            _kpiTenants.ShowLoadingSkeleton();
            _kpiActiveSubs.ShowLoadingSkeleton();
            _kpiMrr.ShowLoadingSkeleton();
            _kpiTransactions.ShowLoadingSkeleton();
            _gridSkeleton?.ShowSkeleton();

            try
            {
                if (_termsOnly)
                {
                    var terms = await _controller.GetMasterTermsAsync();
                    if (!IsDisposed) _txtMasterTerms.Text = terms;
                    return;
                }
                if (_reportsOnly)
                {
                    var subscriptions = await _controller.GetAllSubscriptionsAsync(throwOnError: true);
                    var payments = await _controller.GetAllPaymentRecordsAsync(throwOnError: true);
                    if (IsDisposed) return;
                    _allSubscriptions = subscriptions;
                    _allPayments = payments;
                    _reportsReady = true;
                    RefreshReportsGrid();
                    return;
                }
                // 1. Load Platform BI
                var bi = await _controller.GetPlatformBiSummaryAsync();

                ConfigureKpi(_kpiTenants, "TOTAL TENANTS", bi.TotalTenants, "Platform client databases", Theme.Primary, KpiIconType.Building);
                ConfigureKpi(_kpiActiveSubs, "ACTIVE SUBSCRIPTIONS", bi.ActiveSubscriptions, "Current paid tenants", Theme.StatusSuccess, KpiIconType.Target);
                ConfigureKpi(_kpiMrr, "TOTAL MRR", $"₱{bi.TotalMrr:N0}", "Monthly recurring revenue", Theme.PrimaryDark, KpiIconType.Currency);
                ConfigureKpi(_kpiTransactions, "EXPIRING SOON", bi.ExpiringSubscriptions, "Expiring within 7 days", Theme.StatusPending, KpiIconType.Clock);

                RenderPlanDistributionCard(bi);
                RenderPlatformActivityCard(bi);

                // 2. Load Subscriptions
                _allSubscriptions = await _controller.GetAllSubscriptionsAsync();
                ApplyFilter();

                // 3. Load report sources and the legal agreement displayed in this panel.
                _allPayments = await _controller.GetAllPaymentRecordsAsync();
                _txtMasterTerms.Text = await _controller.GetMasterTermsAsync();
                RefreshReportsGrid();
                _reportsReady = true;
            }
            catch (Exception ex)
            {
                if (IsDisposed) return;
                _gridReports.DataSource = null;
                _lblReportSummary.Text = "Unable to load records. Use Refresh to retry.";
                MessageBox.Show($"Failed to load Master Admin data: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                _loading = false;
                if (!IsDisposed)
                {
                    _pnlReportsContent.Enabled = _reportsReady;
                    _kpiTenants.HideLoadingSkeleton();
                    _kpiActiveSubs.HideLoadingSkeleton();
                    _kpiMrr.HideLoadingSkeleton();
                    _kpiTransactions.HideLoadingSkeleton();
                    _gridSkeleton?.HideSkeleton();
                }
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
                Status = PlatformReportRules.SubscriptionStatus(x),
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
