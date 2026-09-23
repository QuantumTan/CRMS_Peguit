using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using CRMS_Peguit.domain.entities;
using CRMS_Peguit.winforms.Auth;
using CRMS_Peguit.winforms.Controllers;
using CRMS_Peguit.winforms.Controls;
using CRMS_Peguit.winforms.Models.Services;
using CRMS_Peguit.winforms.Services;
using Lead = CRMS_Peguit.domain.entities.Lead;

namespace CRMS_Peguit.winforms.Views.Marketing
{
    public partial class CampaignsView : UserControl
    {
        private readonly CampaignController _campaignController;
        private readonly LeadController _leadController;
        private string _selectedSource = "All";

        // Tab Navigation
        private bool _isAutomatedTabActive = false;
        private Button _btnTabAttribution = null!;
        private Button _btnTabAutomated = null!;

        // Automated Market Updates UI Controls
        private Panel _pnlAutomatedEmailsCard = null!;
        private TableLayoutPanel _pnlKpiContainer = null!;
        private KpiCard _kpiEnrolled = null!;
        private KpiCard _kpiDelivered = null!;
        private KpiCard _kpiEquity = null!;
        private KpiCard _kpiStatus = null!;

        private Button _btnToggleAutomation = null!;
        private Label _lblAutomationStatus = null!;
        private ComboBox _cboFrequency = null!;
        private ComboBox _cboAudience = null!;
        private ComboBox _cboFormat = null!;
        private NumericUpDown _numAppreciation = null!;
        private TextBox _txtBrokerageName = null!;
        private TextBox _txtCtaText = null!;
        private TextBox _txtSubjectTemplate = null!;
        private TextBox _txtBodyTemplate = null!;

        private ComboBox _cboClientPicker = null!;
        private TextBox _txtPreview = null!;
        private Button _btnOpenHtmlBrowser = null!;
        private TextBox _txtTestEmail = null!;
        private Button _btnSendTestEmail = null!;
        private Button _btnRunBatchNow = null!;
        private Button _btnSaveSettings = null!;
        private Button _btnResetSettings = null!;
        private DataGridView _gridAuditHistory = null!;
        private Label _lblLastRunInfo = null!;
        private AutomatedEmailSettings _currentSettings = null!;
        private ValuationMetrics? _latestPreviewMetrics = null!;
        private bool _isLoadingSettings;

        public CampaignsView()
        {
            _campaignController = new CampaignController();
            _leadController = new LeadController();
            InitializeComponent();
            ApplyModernStyling();
            InitializeTabNavigation();
            InitializeAutomatedEmailPanel();
            BindEvents();
            LoadData();
            LoadAutomatedSettings();
        }

        private void ApplyModernStyling()
        {
            this.BackColor = Theme.Background;

            lblTitle.Text = "Marketing & Client Retention";
            lblTitle.Font = new Font("Segoe UI", 20f, FontStyle.Bold);
            lblTitle.ForeColor = Theme.TextPrimary;
            lblTitle.Location = new Point(30, 14);

            lblSubtitle.Text = "Lead acquisition channels & automated client equity retention reports";
            lblSubtitle.Font = new Font("Segoe UI", 9.5f);
            lblSubtitle.ForeColor = Theme.TextSecondary;
            lblSubtitle.Location = new Point(30, 56);

            UiRadiusHelper.StyleButton(btnAddCampaign, 8);
            UiRadiusHelper.StyleButton(btnRefresh, 8);
            btnAddCampaign.Height = 36;
            btnRefresh.Height = 36;

            UiRadiusHelper.StyleCard(pnlStats, 12);
            UiRadiusHelper.StyleCard(pnlGridCard, 12);

            UiGridHelper.ApplyModernGridStyle(gridLeads, 48);
        }

        private void InitializeTabNavigation()
        {
            pnlHeader.Height = 140;
            pnlHeader.BackColor = Color.White;

            var pnlTabs = new FlowLayoutPanel
            {
                Location = new Point(30, 92),
                Size = new Size(620, 36),
                BackColor = Color.Transparent,
                FlowDirection = FlowDirection.LeftToRight,
                WrapContents = false
            };

            _btnTabAttribution = new Button
            {
                Text = "📊 Lead Attribution & Sources",
                Size = new Size(230, 34),
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand,
                Font = new Font("Segoe UI", 9f, FontStyle.Bold),
                BackColor = Color.FromArgb(15, 91, 158),
                ForeColor = Color.White,
                Margin = new Padding(0, 0, 10, 0)
            };
            _btnTabAttribution.FlatAppearance.BorderSize = 0;
            UiRadiusHelper.ApplyPillShape(_btnTabAttribution);

            _btnTabAutomated = new Button
            {
                Text = "📧 Client Retention & Automated Valuations",
                Size = new Size(290, 34),
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand,
                Font = new Font("Segoe UI", 9f, FontStyle.Regular),
                BackColor = Color.FromArgb(241, 245, 249),
                ForeColor = Color.FromArgb(71, 85, 105),
                Margin = new Padding(0, 0, 10, 0)
            };
            _btnTabAutomated.FlatAppearance.BorderSize = 0;
            UiRadiusHelper.ApplyPillShape(_btnTabAutomated);

            _btnTabAttribution.Click += (_, _) => SwitchTab(isAutomated: false);
            _btnTabAutomated.Click += (_, _) => SwitchTab(isAutomated: true);

            pnlTabs.Controls.Add(_btnTabAttribution);
            pnlTabs.Controls.Add(_btnTabAutomated);
            pnlHeader.Controls.Add(pnlTabs);
        }

        private void SwitchTab(bool isAutomated)
        {
            _isAutomatedTabActive = isAutomated;

            if (isAutomated)
            {
                _btnTabAutomated.BackColor = Color.FromArgb(15, 91, 158);
                _btnTabAutomated.ForeColor = Color.White;
                _btnTabAutomated.Font = new Font("Segoe UI", 9f, FontStyle.Bold);

                _btnTabAttribution.BackColor = Color.FromArgb(241, 245, 249);
                _btnTabAttribution.ForeColor = Color.FromArgb(71, 85, 105);
                _btnTabAttribution.Font = new Font("Segoe UI", 9f, FontStyle.Regular);

                pnlStats.Visible = false;
                pnlSourcePills.Visible = false;
                pnlGridCard.Visible = false;
                btnAddCampaign.Visible = false;

                _pnlAutomatedEmailsCard.Visible = true;
                _pnlAutomatedEmailsCard.BringToFront();

                RefreshAutomatedKpis();
                PopulateClientPicker();
                LoadAuditHistory();
                UpdateLivePreview();
            }
            else
            {
                _btnTabAttribution.BackColor = Color.FromArgb(15, 91, 158);
                _btnTabAttribution.ForeColor = Color.White;
                _btnTabAttribution.Font = new Font("Segoe UI", 9f, FontStyle.Bold);

                _btnTabAutomated.BackColor = Color.FromArgb(241, 245, 249);
                _btnTabAutomated.ForeColor = Color.FromArgb(71, 85, 105);
                _btnTabAutomated.Font = new Font("Segoe UI", 9f, FontStyle.Regular);

                pnlStats.Visible = true;
                pnlSourcePills.Visible = true;
                pnlGridCard.Visible = true;
                btnAddCampaign.Visible = true;

                _pnlAutomatedEmailsCard.Visible = false;
            }
        }

        private void InitializeAutomatedEmailPanel()
        {
            _pnlAutomatedEmailsCard = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Theme.Background,
                Padding = new Padding(30, 16, 30, 24),
                Visible = false,
                AutoScroll = true
            };

            // ── TOP SECTION: 4 Standard KPI Cards ──
            _pnlKpiContainer = new TableLayoutPanel
            {
                Dock = DockStyle.Top,
                Height = 104,
                ColumnCount = 4,
                RowCount = 1,
                BackColor = Color.Transparent,
                Margin = new Padding(0, 0, 0, 16)
            };
            _pnlKpiContainer.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25F));
            _pnlKpiContainer.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25F));
            _pnlKpiContainer.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25F));
            _pnlKpiContainer.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25F));
            _pnlKpiContainer.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));

            _kpiEnrolled = new KpiCard("ENROLLED CLIENTS", "clients", BiDisplayConstants.PrimaryAccent, KpiIconType.Users, "Past closed deals")
            {
                Dock = DockStyle.Fill,
                Margin = new Padding(0, 0, 8, 0)
            };
            _kpiDelivered = new KpiCard("UPDATES DELIVERED", "delivered", BiDisplayConstants.HighlightAccent, KpiIconType.Briefcase, "Lifetime touchpoints")
            {
                Dock = DockStyle.Fill,
                Margin = new Padding(4, 0, 6, 0)
            };
            _kpiEquity = new KpiCard("AVG. EQUITY SHOWN", "equity", BiDisplayConstants.StatusWon, KpiIconType.Currency, "Asset appreciation tracked")
            {
                Dock = DockStyle.Fill,
                Margin = new Padding(6, 0, 4, 0)
            };
            _kpiStatus = new KpiCard("AUTOMATION STATUS", "status", BiDisplayConstants.SkyAccent, KpiIconType.Clock, "Hourly engine check")
            {
                Dock = DockStyle.Fill,
                Margin = new Padding(8, 0, 0, 0)
            };

            _pnlKpiContainer.Controls.Add(_kpiEnrolled, 0, 0);
            _pnlKpiContainer.Controls.Add(_kpiDelivered, 1, 0);
            _pnlKpiContainer.Controls.Add(_kpiEquity, 2, 0);
            _pnlKpiContainer.Controls.Add(_kpiStatus, 3, 0);

            // ── MIDDLE SECTION: 2 Balanced Cards (50% / 50%) ──
            var tableSplit = new TableLayoutPanel
            {
                Dock = DockStyle.Top,
                Height = 670,
                ColumnCount = 2,
                RowCount = 1,
                BackColor = Color.Transparent,
                Margin = new Padding(0, 16, 0, 0)
            };
            tableSplit.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));
            tableSplit.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));
            tableSplit.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));

            // ── LEFT CARD: Campaign & Template Architecture ──
            var leftCard = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.White,
                Padding = new Padding(24),
                Margin = new Padding(0, 0, 8, 0),
                AutoScroll = true
            };
            UiRadiusHelper.StyleCard(leftCard, 12);

            var lblLeftTitle = new Label
            {
                Text = "Campaign & Template Architecture",
                Font = new Font("Segoe UI", 13f, FontStyle.Bold),
                ForeColor = Color.FromArgb(15, 23, 42),
                Location = new Point(24, 18),
                AutoSize = true
            };

            var lblLeftSubtitle = new Label
            {
                Text = "Configure automatic valuation metrics and client communication templates.",
                Font = new Font("Segoe UI", 9f),
                ForeColor = Color.FromArgb(100, 116, 139),
                Location = new Point(24, 44),
                AutoSize = true
            };

            // Engine status banner
            var pnlStatusBanner = new Panel
            {
                Location = new Point(24, 70),
                Size = new Size(460, 46),
                BackColor = Color.FromArgb(248, 250, 252)
            };
            UiRadiusHelper.ApplyRoundedCorners(pnlStatusBanner, 8);

            _btnToggleAutomation = new Button
            {
                Location = new Point(8, 6),
                Size = new Size(210, 34),
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand,
                Font = new Font("Segoe UI", 9f, FontStyle.Bold),
                ForeColor = Color.White
            };
            UiRadiusHelper.StyleButton(_btnToggleAutomation, 6);
            _btnToggleAutomation.Click += BtnToggleAutomation_Click;

            _lblAutomationStatus = new Label
            {
                Location = new Point(226, 14),
                Size = new Size(225, 20),
                Font = new Font("Segoe UI", 8.5f),
                ForeColor = Color.FromArgb(100, 116, 139),
                AutoEllipsis = true
            };

            pnlStatusBanner.Controls.Add(_btnToggleAutomation);
            pnlStatusBanner.Controls.Add(_lblAutomationStatus);

            // Audience & Format
            var lblAudience = new Label
            {
                Text = "Target Audience:",
                Location = new Point(24, 126),
                AutoSize = true,
                Font = new Font("Segoe UI", 9f, FontStyle.Bold),
                ForeColor = Color.FromArgb(51, 65, 85)
            };

            _cboAudience = new ComboBox
            {
                Location = new Point(24, 148),
                Size = new Size(220, 28),
                DropDownStyle = ComboBoxStyle.DropDownList,
                Font = new Font("Segoe UI", 9f)
            };
            _cboAudience.Items.AddRange(new object[] { "All Past Clients", "Buyers Only", "Sellers Only" });
            _cboAudience.SelectedIndex = 0;
            _cboAudience.SelectedIndexChanged += (_, _) =>
            {
                PopulateClientPicker();
                RefreshAutomatedKpis();
                if (!_isLoadingSettings)
                {
                    string aud = _cboAudience.SelectedItem?.ToString() ?? "All Past Clients";
                    PromptLoadAudiencePreTemplate(aud);
                }
            };

            var lblFormat = new Label
            {
                Text = "Email Presentation Format:",
                Location = new Point(260, 126),
                AutoSize = true,
                Font = new Font("Segoe UI", 9f, FontStyle.Bold),
                ForeColor = Color.FromArgb(51, 65, 85)
            };

            _cboFormat = new ComboBox
            {
                Location = new Point(260, 148),
                Size = new Size(224, 28),
                DropDownStyle = ComboBoxStyle.DropDownList,
                Font = new Font("Segoe UI", 9f)
            };
            _cboFormat.Items.AddRange(new object[] { "🎨 Branded Client Report (Visual HTML)", "✉ 1-on-1 Personal Note (Plain Text)" });
            _cboFormat.SelectedIndex = 0;
            _cboFormat.SelectedIndexChanged += (_, _) => UpdateLivePreview();

            // Interval & Appreciation
            var lblFreq = new Label
            {
                Text = "Sending Interval per Client:",
                Location = new Point(24, 184),
                AutoSize = true,
                Font = new Font("Segoe UI", 9f, FontStyle.Bold),
                ForeColor = Color.FromArgb(51, 65, 85)
            };

            _cboFrequency = new ComboBox
            {
                Location = new Point(24, 206),
                Size = new Size(220, 28),
                DropDownStyle = ComboBoxStyle.DropDownList,
                Font = new Font("Segoe UI", 9f)
            };
            _cboFrequency.Items.AddRange(new object[]
            {
                "Every 30 Days (Monthly Check-in)",
                "Every 90 Days (Quarterly Valuation)",
                "Every 180 Days (Semi-Annual Update)",
                "Every 365 Days (Annual Anniversary)"
            });
            _cboFrequency.SelectedIndex = 2;

            var lblRate = new Label
            {
                Text = "Est. Annual Growth (%):",
                Location = new Point(260, 184),
                AutoSize = true,
                Font = new Font("Segoe UI", 9f, FontStyle.Bold),
                ForeColor = Color.FromArgb(51, 65, 85)
            };

            _numAppreciation = new NumericUpDown
            {
                Location = new Point(260, 206),
                Size = new Size(130, 28),
                DecimalPlaces = 1,
                Minimum = 0.5m,
                Maximum = 50.0m,
                Value = 5.0m,
                Font = new Font("Segoe UI", 9f)
            };
            _numAppreciation.ValueChanged += (_, _) => UpdateLivePreview();

            // Brokerage Title & CTA Button
            var lblBroker = new Label
            {
                Text = "Brokerage Banner / Title:",
                Location = new Point(24, 242),
                AutoSize = true,
                Font = new Font("Segoe UI", 9f, FontStyle.Bold),
                ForeColor = Color.FromArgb(51, 65, 85)
            };

            _txtBrokerageName = new TextBox
            {
                Location = new Point(24, 264),
                Size = new Size(220, 26),
                Font = new Font("Segoe UI", 9f)
            };
            _txtBrokerageName.TextChanged += (_, _) => UpdateLivePreview();

            var lblCta = new Label
            {
                Text = "Action Button Label:",
                Location = new Point(260, 242),
                AutoSize = true,
                Font = new Font("Segoe UI", 9f, FontStyle.Bold),
                ForeColor = Color.FromArgb(51, 65, 85)
            };

            _txtCtaText = new TextBox
            {
                Location = new Point(260, 264),
                Size = new Size(224, 26),
                Font = new Font("Segoe UI", 9f)
            };
            _txtCtaText.TextChanged += (_, _) => UpdateLivePreview();

            // Subject Line
            var lblSubj = new Label
            {
                Text = "Email Subject Line:",
                Location = new Point(24, 298),
                AutoSize = true,
                Font = new Font("Segoe UI", 9f, FontStyle.Bold),
                ForeColor = Color.FromArgb(51, 65, 85)
            };

            var btnLoadPreTemplate = new Button
            {
                Text = "📋 Load Audience Pre-Template",
                Location = new Point(260, 294),
                Size = new Size(224, 24),
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand,
                Font = new Font("Segoe UI", 8.25f, FontStyle.Bold),
                BackColor = Color.FromArgb(241, 245, 249),
                ForeColor = Color.FromArgb(15, 91, 158)
            };
            btnLoadPreTemplate.FlatAppearance.BorderColor = Color.FromArgb(203, 213, 225);
            UiRadiusHelper.ApplyRoundedCorners(btnLoadPreTemplate, 4);
            btnLoadPreTemplate.Click += (_, _) =>
            {
                string aud = _cboAudience.SelectedItem?.ToString() ?? "All Past Clients";
                LoadPreTemplateForAudience(aud, confirm: true);
            };

            _txtSubjectTemplate = new TextBox
            {
                Location = new Point(24, 320),
                Size = new Size(460, 26),
                Font = new Font("Segoe UI", 9f)
            };
            _txtSubjectTemplate.TextChanged += (_, _) => UpdateLivePreview();

            // Token chips
            var lblTokens = new Label
            {
                Text = "Dynamic Personalization Tokens (Click to insert):",
                Location = new Point(24, 354),
                AutoSize = true,
                Font = new Font("Segoe UI", 8.5f, FontStyle.Bold),
                ForeColor = Color.FromArgb(100, 116, 139)
            };

            var pnlTokens = new FlowLayoutPanel
            {
                Location = new Point(24, 374),
                Size = new Size(460, 78),
                BackColor = Color.FromArgb(248, 250, 252),
                Padding = new Padding(6, 4, 6, 6),
                AutoScroll = false
            };
            UiRadiusHelper.ApplyRoundedCorners(pnlTokens, 6);

            var tokens = new[]
            {
                "{CustomerName}", "{FirstName}", "{PropertyAddress}",
                "{PropertyType}", "{OriginalPrice}", "{EstimatedValue}",
                "{EquityGain}", "{EquityPercent}", "{AppreciationRate}", "{YearsOwned}", "{AgentName}"
            };

            foreach (var t in tokens)
            {
                var chip = new Button
                {
                    Text = t,
                    AutoSize = true,
                    Height = 24,
                    FlatStyle = FlatStyle.Flat,
                    Cursor = Cursors.Hand,
                    Font = new Font("Segoe UI", 8f),
                    BackColor = Color.White,
                    ForeColor = Color.FromArgb(15, 91, 158),
                    Padding = new Padding(4, 0, 4, 0),
                    Margin = new Padding(2, 2, 2, 2)
                };
                chip.FlatAppearance.BorderColor = Color.FromArgb(203, 213, 225);
                UiRadiusHelper.ApplyRoundedCorners(chip, 4);

                string tokText = t;
                chip.Click += (_, _) =>
                {
                    int sel = _txtBodyTemplate.SelectionStart;
                    _txtBodyTemplate.Text = _txtBodyTemplate.Text.Insert(sel, tokText);
                    _txtBodyTemplate.SelectionStart = sel + tokText.Length;
                    _txtBodyTemplate.Focus();
                };
                pnlTokens.Controls.Add(chip);
            }

            // Body Template
            var lblBody = new Label
            {
                Text = "Email Copy Template (Plain Text mode fallback):",
                Location = new Point(24, 460),
                AutoSize = true,
                Font = new Font("Segoe UI", 9f, FontStyle.Bold),
                ForeColor = Color.FromArgb(51, 65, 85)
            };

            _txtBodyTemplate = new TextBox
            {
                Location = new Point(24, 482),
                Size = new Size(460, 100),
                Multiline = true,
                ScrollBars = ScrollBars.Vertical,
                Font = new Font("Segoe UI", 9f)
            };
            _txtBodyTemplate.TextChanged += (_, _) => UpdateLivePreview();

            // Save & Reset Buttons
            _btnSaveSettings = new Button
            {
                Text = "💾 Save Configuration",
                Location = new Point(24, 594),
                Size = new Size(200, 36),
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand,
                Font = new Font("Segoe UI", 9f, FontStyle.Bold),
                BackColor = Color.FromArgb(15, 91, 158),
                ForeColor = Color.White
            };
            UiRadiusHelper.StyleButton(_btnSaveSettings, 8);
            _btnSaveSettings.Click += BtnSaveSettings_Click;

            _btnResetSettings = new Button
            {
                Text = "↺ Reset Default",
                Location = new Point(234, 594),
                Size = new Size(130, 36),
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand,
                Font = new Font("Segoe UI", 9f),
                BackColor = Color.White,
                ForeColor = Color.FromArgb(71, 85, 105)
            };
            _btnResetSettings.FlatAppearance.BorderColor = Color.FromArgb(203, 213, 225);
            UiRadiusHelper.StyleButton(_btnResetSettings, 8);
            _btnResetSettings.Click += (_, _) => ResetToDefaultTemplate();

            leftCard.Controls.Add(lblLeftTitle);
            leftCard.Controls.Add(lblLeftSubtitle);
            leftCard.Controls.Add(pnlStatusBanner);
            leftCard.Controls.Add(lblAudience);
            leftCard.Controls.Add(_cboAudience);
            leftCard.Controls.Add(lblFormat);
            leftCard.Controls.Add(_cboFormat);
            leftCard.Controls.Add(lblFreq);
            leftCard.Controls.Add(_cboFrequency);
            leftCard.Controls.Add(lblRate);
            leftCard.Controls.Add(_numAppreciation);
            leftCard.Controls.Add(lblBroker);
            leftCard.Controls.Add(_txtBrokerageName);
            leftCard.Controls.Add(lblCta);
            leftCard.Controls.Add(_txtCtaText);
            leftCard.Controls.Add(lblSubj);
            leftCard.Controls.Add(btnLoadPreTemplate);
            leftCard.Controls.Add(_txtSubjectTemplate);
            leftCard.Controls.Add(lblTokens);
            leftCard.Controls.Add(pnlTokens);
            leftCard.Controls.Add(lblBody);
            leftCard.Controls.Add(_txtBodyTemplate);
            leftCard.Controls.Add(_btnSaveSettings);
            leftCard.Controls.Add(_btnResetSettings);

            // ── RIGHT CARD: Interactive Client Preview & Dispatch ──
            var rightCard = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.White,
                Padding = new Padding(24),
                Margin = new Padding(8, 0, 0, 0),
                AutoScroll = true
            };
            UiRadiusHelper.StyleCard(rightCard, 12);

            var lblRightTitle = new Label
            {
                Text = "Interactive Client Valuation Preview",
                Font = new Font("Segoe UI", 13f, FontStyle.Bold),
                ForeColor = Color.FromArgb(15, 23, 42),
                Location = new Point(24, 18),
                AutoSize = true
            };

            var lblRightSubtitle = new Label
            {
                Text = "Select any past transaction to inspect custom appreciation and layout.",
                Font = new Font("Segoe UI", 9f),
                ForeColor = Color.FromArgb(100, 116, 139),
                Location = new Point(24, 44),
                AutoSize = true
            };

            // Client Picker Dropdown
            _cboClientPicker = new ComboBox
            {
                Location = new Point(24, 72),
                Size = new Size(480, 30),
                DropDownStyle = ComboBoxStyle.DropDownList,
                DropDownWidth = 750,
                DrawMode = DrawMode.OwnerDrawFixed,
                ItemHeight = 22,
                Font = new Font("Segoe UI", 9.5f)
            };
            _cboClientPicker.DrawItem += (sender, e) =>
            {
                if (e.Index < 0) return;
                e.DrawBackground();
                bool isSelected = (e.State & DrawItemState.Selected) == DrawItemState.Selected;
                using var textBrush = new SolidBrush(isSelected ? Color.White : Color.FromArgb(15, 23, 42));
                using var sf = new StringFormat
                {
                    Trimming = StringTrimming.EllipsisCharacter,
                    FormatFlags = StringFormatFlags.NoWrap,
                    LineAlignment = StringAlignment.Center
                };
                string itemText = _cboClientPicker.Items[e.Index]?.ToString() ?? "";
                var textRect = new Rectangle(e.Bounds.Left + 4, e.Bounds.Top, e.Bounds.Width - 8, e.Bounds.Height);
                e.Graphics.DrawString(itemText, e.Font ?? _cboClientPicker.Font, textBrush, textRect, sf);
                e.DrawFocusRectangle();
            };
            var pickerTip = new ToolTip { AutoPopDelay = 5000, InitialDelay = 350 };
            _cboClientPicker.SelectedIndexChanged += (_, _) =>
            {
                if (_cboClientPicker.SelectedItem != null)
                    pickerTip.SetToolTip(_cboClientPicker, _cboClientPicker.SelectedItem.ToString());
                UpdateLivePreview();
            };

            // Preview Box
            _txtPreview = new TextBox
            {
                Location = new Point(24, 110),
                Size = new Size(480, 290),
                Multiline = true,
                ReadOnly = true,
                ScrollBars = ScrollBars.Vertical,
                BackColor = Color.FromArgb(248, 250, 252),
                ForeColor = Color.FromArgb(15, 23, 42),
                Font = new Font("Consolas", 8.5f),
                BorderStyle = BorderStyle.FixedSingle
            };

            // Open in browser button
            _btnOpenHtmlBrowser = new Button
            {
                Text = "🌐 Open Rendered HTML in Browser",
                Location = new Point(24, 408),
                Size = new Size(310, 32),
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand,
                Font = new Font("Segoe UI", 8.5f, FontStyle.Bold),
                BackColor = Color.White,
                ForeColor = Color.FromArgb(15, 91, 158),
                Padding = new Padding(6, 0, 6, 0)
            };
            _btnOpenHtmlBrowser.FlatAppearance.BorderColor = Color.FromArgb(180, 198, 217);
            UiRadiusHelper.StyleButton(_btnOpenHtmlBrowser, 6);
            _btnOpenHtmlBrowser.Click += BtnOpenHtmlBrowser_Click;

            // Test Email Dispatch Section
            var lblTestSec = new Label
            {
                Text = "⚡ Instant Test Sample Dispatch",
                Font = new Font("Segoe UI", 10f, FontStyle.Bold),
                ForeColor = Color.FromArgb(15, 23, 42),
                Location = new Point(24, 452),
                AutoSize = true
            };

            _txtTestEmail = new TextBox
            {
                Location = new Point(24, 476),
                Size = new Size(300, 30),
                Font = new Font("Segoe UI", 9.5f),
                PlaceholderText = "your.email@example.com"
            };

            _btnSendTestEmail = new Button
            {
                Text = "Send Test Email",
                Location = new Point(334, 474),
                Size = new Size(150, 34),
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand,
                Font = new Font("Segoe UI", 9f, FontStyle.Bold),
                BackColor = Color.FromArgb(15, 91, 158),
                ForeColor = Color.White
            };
            UiRadiusHelper.StyleButton(_btnSendTestEmail, 6);
            _btnSendTestEmail.Click += BtnSendTestEmail_Click;

            // Trigger Batch Now Button
            _btnRunBatchNow = new Button
            {
                Text = "🚀 Run Automated Batch Now (Force Send All)",
                Location = new Point(24, 520),
                Size = new Size(460, 38),
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand,
                Font = new Font("Segoe UI", 9.5f, FontStyle.Bold),
                BackColor = Color.FromArgb(16, 185, 129),
                ForeColor = Color.White
            };
            UiRadiusHelper.StyleButton(_btnRunBatchNow, 8);
            _btnRunBatchNow.Click += BtnRunBatchNow_Click;

            _lblLastRunInfo = new Label
            {
                Location = new Point(24, 566),
                Size = new Size(460, 40),
                Font = new Font("Segoe UI", 8.5f),
                ForeColor = Color.FromArgb(100, 116, 139),
                Text = "Last batch run: Never (Engine ready)"
            };

            rightCard.Controls.Add(lblRightTitle);
            rightCard.Controls.Add(lblRightSubtitle);
            rightCard.Controls.Add(_cboClientPicker);
            rightCard.Controls.Add(_txtPreview);
            rightCard.Controls.Add(_btnOpenHtmlBrowser);
            rightCard.Controls.Add(lblTestSec);
            rightCard.Controls.Add(_txtTestEmail);
            rightCard.Controls.Add(_btnSendTestEmail);
            rightCard.Controls.Add(_btnRunBatchNow);
            rightCard.Controls.Add(_lblLastRunInfo);

            tableSplit.Controls.Add(leftCard, 0, 0);
            tableSplit.Controls.Add(rightCard, 1, 0);

            // ── BOTTOM SECTION: Delivery Audit & History Trail ──
            var pnlAuditCard = new Panel
            {
                Dock = DockStyle.Top,
                Height = 330,
                BackColor = Color.White,
                Padding = new Padding(24),
                Margin = new Padding(0, 18, 0, 24)
            };
            UiRadiusHelper.StyleCard(pnlAuditCard, 12);

            var lblAuditTitle = new Label
            {
                Text = "📋 Delivery Audit & History Trail",
                Font = new Font("Segoe UI", 12f, FontStyle.Bold),
                ForeColor = Color.FromArgb(15, 23, 42),
                Location = new Point(24, 18),
                AutoSize = true
            };

            var lblAuditSubtitle = new Label
            {
                Text = "Comprehensive audit log of all automated and manual valuation touchpoints.",
                Font = new Font("Segoe UI", 8.5f),
                ForeColor = Color.FromArgb(100, 116, 139),
                Location = new Point(24, 44),
                AutoSize = true
            };

            var btnRefreshAudit = new Button
            {
                Text = "↻ Refresh Trail",
                Location = new Point(840, 16),
                Size = new Size(120, 32),
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 8.5f, FontStyle.Bold),
                BackColor = Color.White,
                ForeColor = Color.FromArgb(71, 85, 105),
                Cursor = Cursors.Hand
            };
            btnRefreshAudit.FlatAppearance.BorderColor = Color.FromArgb(203, 213, 225);
            UiRadiusHelper.StyleButton(btnRefreshAudit, 6);
            btnRefreshAudit.Click += (_, _) => LoadAuditHistory();

            _gridAuditHistory = new DataGridView
            {
                Location = new Point(24, 72),
                Size = new Size(936, 230),
                AllowUserToAddRows = false,
                AllowUserToDeleteRows = false,
                ReadOnly = true,
                RowHeadersVisible = false,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                BackgroundColor = Color.White,
                BorderStyle = BorderStyle.None,
                CellBorderStyle = DataGridViewCellBorderStyle.SingleHorizontal,
                GridColor = Color.FromArgb(241, 245, 249),
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                ScrollBars = ScrollBars.Both
            };
            UiGridHelper.ApplyModernGridStyle(_gridAuditHistory, 44);
            _gridAuditHistory.CellPainting += GridAuditHistory_CellPainting;

            pnlAuditCard.Controls.Add(lblAuditTitle);
            pnlAuditCard.Controls.Add(lblAuditSubtitle);
            pnlAuditCard.Controls.Add(btnRefreshAudit);
            pnlAuditCard.Controls.Add(_gridAuditHistory);

            _pnlAutomatedEmailsCard.Controls.Add(pnlAuditCard);
            _pnlAutomatedEmailsCard.Controls.Add(tableSplit);
            _pnlAutomatedEmailsCard.Controls.Add(_pnlKpiContainer);

            this.Controls.Add(_pnlAutomatedEmailsCard);

            // Responsive layout adjustments
            this.Resize += (_, _) =>
            {
                int fullW = _pnlAutomatedEmailsCard.ClientSize.Width - 60;
                if (fullW > 400)
                {
                    pnlAuditCard.Width = fullW;
                    _gridAuditHistory.Width = pnlAuditCard.ClientSize.Width - 48;
                    btnRefreshAudit.Left = pnlAuditCard.ClientSize.Width - 144;
                }

                int leftW = leftCard.ClientSize.Width - 48;
                if (leftW > 200)
                {
                    pnlStatusBanner.Width = leftW;
                    _txtSubjectTemplate.Width = leftW;
                    pnlTokens.Width = leftW;
                    _txtBodyTemplate.Width = leftW;
                }

                int rightW = rightCard.ClientSize.Width - 48;
                if (rightW > 200)
                {
                    _cboClientPicker.Width = rightW;
                    _txtPreview.Width = rightW;
                    _btnRunBatchNow.Width = rightW;
                    _lblLastRunInfo.Width = rightW;
                    _txtTestEmail.Width = Math.Max(120, rightW - 165);
                    _btnSendTestEmail.Left = _txtTestEmail.Right + 10;
                }
            };
        }

        private void RefreshAutomatedKpis()
        {
            try
            {
                int tenantId = CurrentSession.TenantId > 0 ? CurrentSession.TenantId : 1;
                var kpis = MarketUpdateBackgroundService.Instance.GetAnalyticsKpis(tenantId);

                bool isAgent = RbacService.IsAgent;
                _kpiEnrolled.SetValue(kpis.EnrolledClientsCount.ToString("N0"));
                _kpiEnrolled.SetSubtitle(isAgent ? $"{kpis.EnrolledClientsCount} of your past clients" : $"{kpis.EnrolledClientsCount} past clients tracked");

                _kpiDelivered.SetValue(kpis.LifetimeDeliveredCount.ToString("N0"));
                _kpiDelivered.SetSubtitle(isAgent ? $"{kpis.LifetimeDeliveredCount} updates sent" : $"{kpis.LifetimeDeliveredCount} emails sent");

                _kpiEquity.SetValue($"₱{kpis.AvgClientEquityGain / 1_000_000m:F2}M");
                _kpiEquity.SetSubtitle(isAgent ? $"₱{kpis.AvgClientEquityGain:N0} client avg gain" : $"₱{kpis.AvgClientEquityGain:N0} avg gain");

                if (kpis.IsActive)
                {
                    _kpiStatus.SetValue("ACTIVE");
                    _kpiStatus.SetValueColor(Color.FromArgb(16, 185, 129));
                    _kpiStatus.SetSubtitle(isAgent ? "Brokerage schedule active" : "Hourly engine active");
                }
                else
                {
                    _kpiStatus.SetValue("PAUSED");
                    _kpiStatus.SetValueColor(Color.FromArgb(100, 116, 139));
                    _kpiStatus.SetSubtitle(isAgent ? "Brokerage schedule paused" : "Scheduled dispatch paused");
                }
            }
            catch { }
        }

        private void PopulateClientPicker()
        {
            int tenantId = CurrentSession.TenantId > 0 ? CurrentSession.TenantId : 1;
            var clients = MarketUpdateBackgroundService.Instance.GetEligibleClients(tenantId);

            _cboClientPicker.Items.Clear();

            if (clients.Count == 0)
            {
                bool isAgent = RbacService.IsAgent;
                _cboClientPicker.Items.Add(new ClientPickerItem
                {
                    CustomerId = 0,
                    FullName = isAgent ? "No Closed Deals Yet" : "Maria Santos (Sample Mock Data)",
                    PropertyAddress = isAgent ? "Close deals to automatically enroll your past clients" : "Unit 1204, One Serendra, BGC, Taguig"
                });
            }
            else
            {
                foreach (var c in clients)
                {
                    _cboClientPicker.Items.Add(c);
                }
            }

            if (_cboClientPicker.Items.Count > 0)
                _cboClientPicker.SelectedIndex = 0;
        }

        private void LoadAuditHistory()
        {
            try
            {
                int tenantId = CurrentSession.TenantId > 0 ? CurrentSession.TenantId : 1;
                var history = MarketUpdateBackgroundService.Instance.GetDeliveryHistory(tenantId);

                _gridAuditHistory.Columns.Clear();

                _gridAuditHistory.DataSource = history.Select(h => new
                {
                    h.LogId,
                    Status = h.Status,
                    Client = h.CustomerName,
                    RecipientEmail = h.RecipientEmail,
                    Property = h.PropertyAddress,
                    Original = $"₱{h.OriginalPrice:N0}",
                    Valuation = $"₱{h.EstimatedValue:N0}",
                    EquityGrowth = $"+₱{h.EquityGainAmount:N0} (+{h.EquityGainPercent:F1}%)",
                    Format = h.EmailFormat,
                    Trigger = h.TriggerType,
                    Date = h.SentAt.ToLocalTime().ToString("MMM dd, yyyy HH:mm")
                }).ToList();

                _gridAuditHistory.ColumnHeadersDefaultCellStyle.WrapMode = DataGridViewTriState.False;

                if (_gridAuditHistory.Columns["LogId"] is DataGridViewColumn idCol)
                    idCol.Visible = false;

                if (_gridAuditHistory.Columns["Status"] is DataGridViewColumn stCol)
                {
                    stCol.HeaderText = "STATUS";
                    stCol.MinimumWidth = 95;
                    stCol.FillWeight = 95;
                }
                if (_gridAuditHistory.Columns["Client"] is DataGridViewColumn clCol)
                {
                    clCol.HeaderText = "CLIENT NAME";
                    clCol.MinimumWidth = 140;
                    clCol.FillWeight = 140;
                }
                if (_gridAuditHistory.Columns["RecipientEmail"] is DataGridViewColumn emCol)
                {
                    emCol.HeaderText = "RECIPIENT EMAIL";
                    emCol.MinimumWidth = 160;
                    emCol.FillWeight = 160;
                }
                if (_gridAuditHistory.Columns["Property"] is DataGridViewColumn prCol)
                {
                    prCol.HeaderText = "ASSET REFERENCE";
                    prCol.MinimumWidth = 200;
                    prCol.FillWeight = 200;
                }
                if (_gridAuditHistory.Columns["Original"] is DataGridViewColumn orCol)
                {
                    orCol.HeaderText = "ACQUISITION";
                    orCol.MinimumWidth = 110;
                    orCol.FillWeight = 110;
                    orCol.HeaderCell.Style.Alignment = DataGridViewContentAlignment.MiddleRight;
                    orCol.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight;
                }
                if (_gridAuditHistory.Columns["Valuation"] is DataGridViewColumn valCol)
                {
                    valCol.HeaderText = "APPRAISED VAL";
                    valCol.MinimumWidth = 110;
                    valCol.FillWeight = 110;
                    valCol.HeaderCell.Style.Alignment = DataGridViewContentAlignment.MiddleRight;
                    valCol.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight;
                }
                if (_gridAuditHistory.Columns["EquityGrowth"] is DataGridViewColumn eqCol)
                {
                    eqCol.HeaderText = "EST. EQUITY GAIN";
                    eqCol.MinimumWidth = 150;
                    eqCol.FillWeight = 150;
                    eqCol.HeaderCell.Style.Alignment = DataGridViewContentAlignment.MiddleRight;
                    eqCol.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight;
                }
                if (_gridAuditHistory.Columns["Format"] is DataGridViewColumn fmCol)
                {
                    fmCol.HeaderText = "FORMAT";
                    fmCol.MinimumWidth = 85;
                    fmCol.FillWeight = 85;
                }
                if (_gridAuditHistory.Columns["Trigger"] is DataGridViewColumn trCol)
                {
                    trCol.HeaderText = "TRIGGER";
                    trCol.MinimumWidth = 90;
                    trCol.FillWeight = 90;
                }
                if (_gridAuditHistory.Columns["Date"] is DataGridViewColumn dtCol)
                {
                    dtCol.HeaderText = "DISPATCH DATE";
                    dtCol.MinimumWidth = 135;
                    dtCol.FillWeight = 135;
                    dtCol.HeaderCell.Style.Alignment = DataGridViewContentAlignment.MiddleRight;
                    dtCol.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight;
                }
            }
            catch { }
        }

        private void GridAuditHistory_CellPainting(object? sender, DataGridViewCellPaintingEventArgs e)
        {
            if (e.RowIndex < 0 || e.Graphics is null) return;
            string colName = _gridAuditHistory.Columns[e.ColumnIndex].Name;

            if (colName == "Status" && e.Value != null)
            {
                string status = e.Value.ToString() ?? "";
                UiGridHelper.PaintStatusIndicator(_gridAuditHistory, e, status, center: false);
            }
            else if (colName == "EquityGrowth" && e.Value != null)
            {
                string valStr = e.Value.ToString() ?? "-";
                using var font = new Font("Segoe UI", 9f, FontStyle.Bold);
                UiGridHelper.PaintTextCell(_gridAuditHistory, e, valStr, font, Color.FromArgb(16, 185, 129),
                    TextFormatFlags.Right | TextFormatFlags.VerticalCenter, leftPadding: 8, rightPadding: 12);
            }
        }

        private void LoadAutomatedSettings()
        {
            _isLoadingSettings = true;
            try
            {
                int tenantId = CurrentSession.TenantId > 0 ? CurrentSession.TenantId : 1;
                _currentSettings = MarketUpdateBackgroundService.Instance.GetSettings(tenantId);

                UpdateToggleStateDisplay();

                _numAppreciation.Value = Math.Clamp(_currentSettings.AnnualAppreciationRatePercent, 0.5m, 50m);
                _txtBrokerageName.Text = _currentSettings.BrokerageName;
                _txtCtaText.Text = _currentSettings.CallToActionText;
                _txtSubjectTemplate.Text = _currentSettings.SubjectTemplate;
                _txtBodyTemplate.Text = _currentSettings.BodyTemplate;

                _cboAudience.SelectedItem = _currentSettings.TargetAudience switch
                {
                    "Buyers" => "Buyers Only",
                    "Sellers" => "Sellers Only",
                    _ => "All Past Clients"
                };

                _cboFormat.SelectedIndex = _currentSettings.EmailFormat == "PlainText" ? 1 : 0;

                if (_currentSettings.FrequencyDays <= 30) _cboFrequency.SelectedIndex = 0;
                else if (_currentSettings.FrequencyDays <= 90) _cboFrequency.SelectedIndex = 1;
                else if (_currentSettings.FrequencyDays <= 180) _cboFrequency.SelectedIndex = 2;
                else _cboFrequency.SelectedIndex = 3;

                _lblLastRunInfo.Text = _currentSettings.LastBatchRunAt.HasValue
                    ? $"Last batch run: {_currentSettings.LastBatchRunAt.Value.ToLocalTime():g}\r\nSummary: {_currentSettings.LastBatchStatus ?? "Complete"}"
                    : "Last batch run: Never (Engine ready)";

                if (CurrentSession.CurrentUser != null && !string.IsNullOrWhiteSpace(CurrentSession.CurrentUser.Email))
                {
                    _txtTestEmail.Text = CurrentSession.CurrentUser.Email;
                }

                PopulateClientPicker();
                RefreshAutomatedKpis();
                UpdateLivePreview();
                ApplyRbacPermissions();
            }
            catch { }
            finally
            {
                _isLoadingSettings = false;
            }
        }

        private void ApplyRbacPermissions()
        {
            bool isAgent = RbacService.IsAgent;
            bool canManageGlobalSettings = RbacService.IsAdmin || RbacService.IsSuperAdmin;

            if (isAgent)
            {
                // Engine toggle: view-only status for Agent
                _btnToggleAutomation.Enabled = false;
                _btnToggleAutomation.Cursor = Cursors.Default;

                // Brokerage title and interval frequency are global administrative settings
                _txtBrokerageName.ReadOnly = true;
                _txtBrokerageName.BackColor = Color.FromArgb(248, 250, 252);
                _cboFrequency.Enabled = false;

                // Save button disabled for agents so company-wide defaults are not overwritten
                _btnSaveSettings.Enabled = false;
                _btnSaveSettings.Text = "🔒 Global Settings (Admin Only)";
                _btnSaveSettings.BackColor = Color.FromArgb(148, 163, 184);
                _btnSaveSettings.Cursor = Cursors.Default;

                _btnResetSettings.Enabled = false;
                _btnResetSettings.Visible = false;

                // Scoped batch trigger text
                _btnRunBatchNow.Text = "🚀 Run Batch Update for My Clients Only";
            }
            else
            {
                _btnToggleAutomation.Enabled = canManageGlobalSettings;
                _btnToggleAutomation.Cursor = canManageGlobalSettings ? Cursors.Hand : Cursors.Default;
                _btnSaveSettings.Enabled = canManageGlobalSettings;
                _btnSaveSettings.Text = "💾 Save Configuration";
                _btnSaveSettings.BackColor = Color.FromArgb(15, 91, 158);
                _btnSaveSettings.Cursor = canManageGlobalSettings ? Cursors.Hand : Cursors.Default;

                _btnResetSettings.Enabled = canManageGlobalSettings;
                _btnResetSettings.Visible = true;
                _txtBrokerageName.ReadOnly = !canManageGlobalSettings;
                _txtBrokerageName.BackColor = canManageGlobalSettings ? Color.White : Color.FromArgb(248, 250, 252);
                _cboFrequency.Enabled = canManageGlobalSettings;

                if (RbacService.IsManager)
                {
                    _btnRunBatchNow.Text = "🚀 Run Team Batch Update (Force Send)";
                }
                else
                {
                    _btnRunBatchNow.Text = "🚀 Run Automated Batch Now (Force Send All)";
                }
            }
        }

        private void UpdateToggleStateDisplay()
        {
            bool isAgent = RbacService.IsAgent;
            if (_currentSettings.IsEnabled)
            {
                _btnToggleAutomation.Text = "● Engine Status: ACTIVE";
                _btnToggleAutomation.BackColor = Color.FromArgb(16, 185, 129);
                _lblAutomationStatus.Text = isAgent
                    ? "Brokerage automated background schedule is ACTIVE (Admin managed)."
                    : "Hourly background check running for eligible clients.";
                _lblAutomationStatus.ForeColor = Color.FromArgb(16, 185, 129);
            }
            else
            {
                _btnToggleAutomation.Text = "○ Engine Status: PAUSED";
                _btnToggleAutomation.BackColor = Color.FromArgb(100, 116, 139);
                _lblAutomationStatus.Text = isAgent
                    ? "Brokerage automated background schedule is PAUSED (Admin managed)."
                    : "Automatic scheduled dispatch is currently paused.";
                _lblAutomationStatus.ForeColor = Color.FromArgb(100, 116, 139);
            }

            RefreshAutomatedKpis();
        }

        private void BtnToggleAutomation_Click(object? sender, EventArgs e)
        {
            if (!RbacService.IsAdmin && !RbacService.IsSuperAdmin)
            {
                MessageBox.Show(
                    "Only system administrators can toggle the brokerage-wide automated retention engine.",
                    "Access Denied",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);
                return;
            }

            _currentSettings.IsEnabled = !_currentSettings.IsEnabled;
            UpdateToggleStateDisplay();
            int tenantId = CurrentSession.TenantId > 0 ? CurrentSession.TenantId : 1;
            MarketUpdateBackgroundService.Instance.SaveSettings(_currentSettings, tenantId);
        }

        private void BtnSaveSettings_Click(object? sender, EventArgs e)
        {
            if (!RbacService.IsAdmin && !RbacService.IsSuperAdmin)
            {
                MessageBox.Show(
                    "Global company retention settings can only be saved by administrators.",
                    "Access Denied",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);
                return;
            }

            int freqDays = _cboFrequency.SelectedIndex switch
            {
                0 => 30,
                1 => 90,
                2 => 180,
                3 => 365,
                _ => 180
            };

            _currentSettings.FrequencyDays = freqDays;
            _currentSettings.AnnualAppreciationRatePercent = _numAppreciation.Value;
            _currentSettings.EmailFormat = _cboFormat.SelectedIndex == 1 ? "PlainText" : "Html";
            _currentSettings.TargetAudience = _cboAudience.SelectedIndex switch
            {
                1 => "Buyers",
                2 => "Sellers",
                _ => "All"
            };
            _currentSettings.BrokerageName = _txtBrokerageName.Text.Trim();
            _currentSettings.CallToActionText = _txtCtaText.Text.Trim();
            _currentSettings.SubjectTemplate = _txtSubjectTemplate.Text.Trim();
            _currentSettings.BodyTemplate = _txtBodyTemplate.Text.Trim();

            int tenantId = CurrentSession.TenantId > 0 ? CurrentSession.TenantId : 1;
            bool saved = MarketUpdateBackgroundService.Instance.SaveSettings(_currentSettings, tenantId);

            if (saved)
            {
                MessageBox.Show(
                    "Automated market update configuration has been successfully saved.",
                    "Configuration Saved",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information);
                RefreshAutomatedKpis();
            }
            else
            {
                MessageBox.Show(
                    "Unable to save settings. Please verify database connection.",
                    "Save Failed",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);
            }

            UpdateLivePreview();
        }

        private void PromptLoadAudiencePreTemplate(string audience)
        {
            var res = MessageBox.Show(
                $"Would you like to automatically load the pre-written template tailored for '{audience}'?\n\nThis will update the subject, email copy, and call-to-action button specifically for this audience segment.",
                "Load Tailored Pre-Template",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Question);

            if (res == DialogResult.Yes)
            {
                LoadPreTemplateForAudience(audience, confirm: false);
            }
        }

        private void LoadPreTemplateForAudience(string audience, bool confirm = false)
        {
            if (confirm)
            {
                var res = MessageBox.Show(
                    $"Apply the pre-written template tailored for '{audience}'?\n\nThis will replace the current subject, email copy, and call-to-action button.",
                    "Confirm Pre-Template",
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Question);

                if (res != DialogResult.Yes) return;
            }

            var (subj, body, cta, rate) = AutomatedEmailSettings.GetPreTemplate(audience);
            _txtSubjectTemplate.Text = subj;
            _txtBodyTemplate.Text = body;
            _txtCtaText.Text = cta;
            _numAppreciation.Value = rate;
            UpdateLivePreview();
        }

        private void ResetToDefaultTemplate()
        {
            string audience = _cboAudience.SelectedItem?.ToString() ?? "All Past Clients";
            LoadPreTemplateForAudience(audience, confirm: true);
        }

        private void UpdateLivePreview()
        {
            if (_txtPreview == null) return;

            try
            {
                int tenantId = CurrentSession.TenantId > 0 ? CurrentSession.TenantId : 1;
                int? selectedCustId = (_cboClientPicker?.SelectedItem as ClientPickerItem)?.CustomerId;

                // Sync UI fields
                _currentSettings.EmailFormat = _cboFormat?.SelectedIndex == 1 ? "PlainText" : "Html";
                _currentSettings.AnnualAppreciationRatePercent = _numAppreciation != null ? _numAppreciation.Value : 5.0m;
                _currentSettings.BrokerageName = string.IsNullOrWhiteSpace(_txtBrokerageName?.Text) ? "NEXA Real Estate Advisory" : _txtBrokerageName.Text;
                _currentSettings.CallToActionText = string.IsNullOrWhiteSpace(_txtCtaText?.Text) ? "Schedule Consultation" : _txtCtaText.Text;
                _currentSettings.SubjectTemplate = string.IsNullOrWhiteSpace(_txtSubjectTemplate?.Text) ? "Market Valuation Update" : _txtSubjectTemplate.Text;
                _currentSettings.BodyTemplate = string.IsNullOrWhiteSpace(_txtBodyTemplate?.Text) ? "Valuation update" : _txtBodyTemplate.Text;

                var (subj, _, recipient, metrics) = MarketUpdateBackgroundService.Instance.GeneratePreview(tenantId, selectedCustId);
                _latestPreviewMetrics = metrics;

                // Display the clean visual card mockup instead of raw HTML!
                _txtPreview.Text = MarketUpdateBackgroundService.FormatVisualCardMockup(_currentSettings, metrics, recipient);

                _btnOpenHtmlBrowser.Visible = _currentSettings.EmailFormat == "Html";
            }
            catch (Exception ex)
            {
                _txtPreview.Text = $"Preview error: {ex.Message}";
            }
        }

        private void BtnOpenHtmlBrowser_Click(object? sender, EventArgs e)
        {
            if (_latestPreviewMetrics == null) return;

            try
            {
                string path = MarketUpdateBackgroundService.SavePreviewHtmlToFile(_currentSettings, _latestPreviewMetrics);
                Process.Start(new ProcessStartInfo(path) { UseShellExecute = true });
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Could not open browser: {ex.Message}", "Browser Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        private async void BtnSendTestEmail_Click(object? sender, EventArgs e)
        {
            string email = _txtTestEmail.Text.Trim();
            if (!ContactEmailService.IsValidEmail(email))
            {
                MessageBox.Show(
                    "Please enter a valid recipient email address.",
                    "Invalid Email",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);
                _txtTestEmail.Focus();
                return;
            }

            _btnSendTestEmail.Enabled = false;
            _btnSendTestEmail.Text = "Sending...";

            try
            {
                int tenantId = CurrentSession.TenantId > 0 ? CurrentSession.TenantId : 1;
                int? selectedCustId = (_cboClientPicker?.SelectedItem as ClientPickerItem)?.CustomerId;

                if (RbacService.IsAdmin || RbacService.IsSuperAdmin)
                {
                    BtnSaveSettings_Click(this, EventArgs.Empty);
                }

                var result = await MarketUpdateBackgroundService.Instance.SendTestEmailAsync(email, tenantId, selectedCustId);

                LoadAuditHistory();
                RefreshAutomatedKpis();

                if (result.Success)
                {
                    MessageBox.Show(
                        $"Test email was successfully dispatched to:\n{email}\n\nPlease check your inbox.",
                        "Test Dispatch Succeeded",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Information);
                }
                else
                {
                    MessageBox.Show(
                        $"Test dispatch failed:\n\n{result.Message}",
                        "Email Delivery Error",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Warning);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                _btnSendTestEmail.Enabled = true;
                _btnSendTestEmail.Text = "Send Test Email";
            }
        }

        private async void BtnRunBatchNow_Click(object? sender, EventArgs e)
        {
            bool isAgent = RbacService.IsAgent;
            string promptText = isAgent
                ? "Are you sure you want to trigger market updates for your assigned past clients right now?\n\nThis will evaluate only your closed transactions and dispatch personalized updates."
                : "Are you sure you want to trigger the automated market update batch for all eligible clients right now?\n\nThis will evaluate your past client transactions and deliver market reports.";

            var confirm = MessageBox.Show(
                promptText,
                isAgent ? "Confirm Personal Client Batch" : "Confirm Batch Run",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Question);

            if (confirm != DialogResult.Yes) return;

            _btnRunBatchNow.Enabled = false;
            _btnRunBatchNow.Text = "Processing Batch...";

            try
            {
                int tenantId = CurrentSession.TenantId > 0 ? CurrentSession.TenantId : 1;
                if (RbacService.IsAdmin || RbacService.IsSuperAdmin)
                {
                    BtnSaveSettings_Click(this, EventArgs.Empty);
                }

                int? scopedAgentId = isAgent ? CurrentSession.UserId : null;
                var result = await MarketUpdateBackgroundService.Instance.RunBatchAsync(
                    tenantId: tenantId,
                    forceRunAll: true,
                    triggerType: isAgent ? "AgentManualBatch" : "ManualBatch",
                    scopedAgentId: scopedAgentId);

                _lblLastRunInfo.Text = $"Last batch run: {DateTime.Now:g}\r\nSummary: {result.Summary}";

                LoadAuditHistory();
                RefreshAutomatedKpis();

                MessageBox.Show(
                    $"Batch Execution Completed!\n\n" +
                    $"• Total Evaluated: {result.TotalEvaluated}\n" +
                    $"• Successfully Sent: {result.SentCount}\n" +
                    $"• Skipped: {result.SkippedCount}\n" +
                    $"• Failed: {result.FailedCount}\n\n" +
                    $"Detailed delivery records have been added to the Delivery Audit & History Trail below.",
                    "Batch Complete",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Batch execution error: {ex.Message}", "Batch Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                _btnRunBatchNow.Enabled = true;
                _btnRunBatchNow.Text = isAgent
                    ? "🚀 Run Batch Update for My Clients Only"
                    : (RbacService.IsManager ? "🚀 Run Team Batch Update (Force Send)" : "🚀 Run Automated Batch Now (Force Send All)");
            }
        }

        private void BindEvents()
        {
            btnAddCampaign.Click += BtnAddCampaign_Click;
            btnRefresh.Click += (_, _) =>
            {
                LoadData();
                if (_isAutomatedTabActive)
                {
                    RefreshAutomatedKpis();
                    PopulateClientPicker();
                    LoadAuditHistory();
                    UpdateLivePreview();
                }
            };
            gridLeads.CellPainting += GridLeads_CellPainting;
        }

        private void LoadData()
        {
            var summary = _campaignController.GetCampaignSummary(_selectedSource);

            lblStatChannels.Text = $"{summary.ActiveChannelCount} Active Lead Channels";
            lblStatTotalLeads.Text = $"{summary.TotalLeads} Total Leads Attributed";
            lblStatConversion.Text = summary.TopChannelText;

            BuildFilterPills(summary);
            DisplayLeads(summary.FilteredLeads);
        }

        private void BuildFilterPills(CRMS_Peguit.winforms.Models.ViewModels.CampaignSummaryViewModel summary)
        {
            pnlSourcePills.Controls.Clear();

            var btnAll = CreatePillButton($"All ({summary.TotalLeads})", "All");
            pnlSourcePills.Controls.Add(btnAll);

            foreach (var source in summary.Channels)
            {
                int count = summary.ChannelCounts.TryGetValue(source, out int c) ? c : 0;
                var btn = CreatePillButton($"{source} ({count})", source);
                pnlSourcePills.Controls.Add(btn);
            }
        }

        private Button CreatePillButton(string label, string sourceValue)
        {
            bool isSelected = string.Equals(_selectedSource, sourceValue, StringComparison.OrdinalIgnoreCase);

            var btn = new Button
            {
                Text = label,
                Tag = sourceValue,
                AutoSize = true,
                Height = 34,
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand,
                Font = new Font("Segoe UI", 9f, isSelected ? FontStyle.Bold : FontStyle.Regular),
                BackColor = isSelected ? Color.FromArgb(15, 91, 158) : Color.White,
                ForeColor = isSelected ? Color.White : Color.FromArgb(71, 85, 105),
                Margin = new Padding(0, 0, 8, 0)
            };
            btn.FlatAppearance.BorderSize = 0;

            UiRadiusHelper.ApplyPillShape(btn);

            btn.Click += (_, _) =>
            {
                _selectedSource = sourceValue;
                LoadData();
            };

            return btn;
        }

        private void DisplayLeads(List<Lead> filteredLeads)
        {
            var filtered = filteredLeads;

            gridLeads.Columns.Clear();

            gridLeads.DataSource = filtered.Select(l => new
            {
                l.LeadId,
                Name = l.FullName,
                CampaignSource = string.IsNullOrWhiteSpace(l.Source) ? "Untagged" : l.Source,
                Stage = (l.Stage ?? "New").ToUpper(),
                Value = l.ExpectedValue.HasValue ? $"₱{l.ExpectedValue.Value:N2}" : "-",
                Contact = string.IsNullOrWhiteSpace(l.Phone) ? l.Email ?? "-" : l.Phone,
                AssignedAgent = _leadController.GetAssignedAgentName(l.AssignedAgentId) ?? "Unassigned",
                CapturedDate = l.CreatedAt.ToString("MMM dd, yyyy")
            }).ToList();

            if (gridLeads.Columns["LeadId"] is DataGridViewColumn idCol)
                idCol.Visible = false;

            if (gridLeads.Columns["Name"] is DataGridViewColumn nameCol)
            {
                nameCol.HeaderText = "LEAD NAME";
                nameCol.FillWeight = 140;
            }
            if (gridLeads.Columns["CampaignSource"] is DataGridViewColumn srcCol)
            {
                srcCol.HeaderText = "SOURCE / CAMPAIGN";
                srcCol.FillWeight = 130;
            }
            if (gridLeads.Columns["Stage"] is DataGridViewColumn stageCol)
            {
                stageCol.HeaderText = "STAGE";
                stageCol.FillWeight = 90;
                stageCol.HeaderCell.Style.Alignment = DataGridViewContentAlignment.MiddleLeft;
                stageCol.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleLeft;
            }
            if (gridLeads.Columns["Value"] is DataGridViewColumn valCol)
            {
                valCol.HeaderText = "EXPECTED VALUE";
                valCol.FillWeight = 100;
                valCol.HeaderCell.Style.Alignment = DataGridViewContentAlignment.MiddleRight;
                valCol.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight;
            }
            if (gridLeads.Columns["Contact"] is DataGridViewColumn conCol)
            {
                conCol.HeaderText = "CONTACT";
                conCol.FillWeight = 120;
            }
            if (gridLeads.Columns["AssignedAgent"] is DataGridViewColumn agentCol)
            {
                agentCol.HeaderText = "ASSIGNED AGENT";
                agentCol.FillWeight = 110;
            }
            if (gridLeads.Columns["CapturedDate"] is DataGridViewColumn dateCol)
            {
                dateCol.HeaderText = "DATE CAPTURED";
                dateCol.FillWeight = 100;
            }
        }

        private void GridLeads_CellPainting(object? sender, DataGridViewCellPaintingEventArgs e)
        {
            if (e.RowIndex < 0 || e.Graphics is null) return;
            string colName = gridLeads.Columns[e.ColumnIndex].Name;

            if (colName == "Stage" && e.Value != null)
            {
                string stage = e.Value.ToString() ?? "";
                UiGridHelper.PaintStatusIndicator(gridLeads, e, stage, center: false);
            }
            else if (colName == "Value" && e.Value != null)
            {
                string valStr = e.Value.ToString() ?? "-";
                using var font = new Font("Segoe UI", 9.5f, FontStyle.Bold);
                UiGridHelper.PaintTextCell(gridLeads, e, valStr, font, Theme.TextPrimary,
                    TextFormatFlags.Right | TextFormatFlags.VerticalCenter, leftPadding: 8, rightPadding: 12);
            }
        }

        private void BtnAddCampaign_Click(object? sender, EventArgs e)
        {
            using var inputForm = new Form
            {
                Text = "Create New Campaign / Lead Source",
                Size = new Size(460, 300),
                StartPosition = FormStartPosition.CenterParent,
                FormBorderStyle = FormBorderStyle.FixedDialog,
                MaximizeBox = false,
                MinimizeBox = false,
                BackColor = Color.White
            };

            var lblPrompt = new Label
            {
                Text = "Campaign / Source Name * (e.g., Summer Promo 2026, Ayala Expo):",
                Location = new Point(24, 20),
                AutoSize = true,
                Font = new Font("Segoe UI", 9.5f)
            };

            var txtName = new TextBox
            {
                Location = new Point(24, 48),
                Size = new Size(390, 30),
                Font = new Font("Segoe UI", 10f)
            };

            var lblChannel = new Label
            {
                Text = "Channel / Category (e.g., Social Media, Portal, Event, Direct):",
                Location = new Point(24, 90),
                AutoSize = true,
                Font = new Font("Segoe UI", 9.5f)
            };

            var cmbChannel = new ComboBox
            {
                Location = new Point(24, 118),
                Size = new Size(390, 30),
                Font = new Font("Segoe UI", 10f),
                DropDownStyle = ComboBoxStyle.DropDown
            };
            cmbChannel.Items.AddRange(new object[]
            {
                "Social Media",
                "Property Portal",
                "Search Engine / Paid Ads",
                "Referral",
                "Event / Expo",
                "Outdoor / Billboard",
                "Direct / Walk-in",
                "Website"
            });
            cmbChannel.SelectedIndex = 0;

            var btnCancel = new Button
            {
                Text = "Cancel",
                DialogResult = DialogResult.Cancel,
                Location = new Point(174, 195),
                Size = new Size(100, 38),
                BackColor = Color.White,
                ForeColor = Color.FromArgb(8, 52, 87),
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 9.5f)
            };
            btnCancel.FlatAppearance.BorderColor = Color.FromArgb(180, 198, 217);
            UiRadiusHelper.StyleButton(btnCancel, 8);

            var btnSubmit = new Button
            {
                Text = "Save Campaign",
                DialogResult = DialogResult.OK,
                Location = new Point(284, 195),
                Size = new Size(130, 38),
                BackColor = Color.FromArgb(15, 91, 158),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 9.5f, FontStyle.Bold)
            };
            UiRadiusHelper.StyleButton(btnSubmit, 8);

            inputForm.Controls.Add(lblPrompt);
            inputForm.Controls.Add(txtName);
            inputForm.Controls.Add(lblChannel);
            inputForm.Controls.Add(cmbChannel);
            inputForm.Controls.Add(btnCancel);
            inputForm.Controls.Add(btnSubmit);
            inputForm.AcceptButton = btnSubmit;
            inputForm.CancelButton = btnCancel;

            if (inputForm.ShowDialog(this) == DialogResult.OK)
            {
                string newSource = txtName.Text.Trim();
                string channel = cmbChannel.Text.Trim();
                if (!string.IsNullOrWhiteSpace(newSource))
                {
                    bool saved = _campaignController.AddCampaign(newSource, string.IsNullOrWhiteSpace(channel) ? "Direct" : channel);
                    if (saved)
                    {
                        _selectedSource = newSource;
                        LoadData();

                        MessageBox.Show(
                            $"Campaign '{newSource}' has been successfully saved to the database.\n\nIt is now immediately available in the Agent's Lead Source dropdown for lead attribution.",
                            "Campaign Saved",
                            MessageBoxButtons.OK,
                            MessageBoxIcon.Information);
                    }
                    else
                    {
                        MessageBox.Show(
                            "Unable to save the campaign. Please check connection and try again.",
                            "Error Saving Campaign",
                            MessageBoxButtons.OK,
                            MessageBoxIcon.Warning);
                    }
                }
            }
        }
    }
}
