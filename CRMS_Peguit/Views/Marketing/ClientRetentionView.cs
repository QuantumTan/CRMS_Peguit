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

namespace CRMS_Peguit.winforms.Views.Marketing
{
    public class ClientRetentionView : UserControl
    {
        // Header Controls
        private Panel _pnlHeader = null!;
        private Label _lblTitle = null!;
        private Label _lblSubtitle = null!;
        private Button _btnRefresh = null!;

        // Scrollable Body
        private Panel _pnlContent = null!;

        // 4 KPI Cards
        private TableLayoutPanel _pnlKpiContainer = null!;
        private KpiCard _kpiEnrolled = null!;
        private KpiCard _kpiDelivered = null!;
        private KpiCard _kpiEquity = null!;
        private KpiCard _kpiStatus = null!;

        // Left Card: Template Architecture & Settings
        private Button _btnSegmentActive = null!;
        private Button _btnSegmentPaused = null!;
        private Button _btnSegmentStopped = null!;
        private ComboBox _cboFrequency = null!;
        private ComboBox _cboAudience = null!;
        private ComboBox _cboFormat = null!;
        private NumericUpDown _numAppreciation = null!;
        private TextBox _txtBrokerageName = null!;
        private TextBox _txtCtaText = null!;
        private TextBox _txtSubjectTemplate = null!;
        private TextBox _txtBodyTemplate = null!;

        // Template System & RBAC Governance
        private ComboBox _cboTemplates = null!;
        private Label _lblTemplateBadge = null!;
        private Button _btnNewTemplate = null!;
        private Button _btnCloneTemplate = null!;
        private Button _btnDeleteTemplate = null!;
        private List<EmailTemplate> _loadedTemplates = new();
        private EmailTemplate? _selectedTemplate;

        // Right Column: Interactive Visual Preview & Test Dispatch
        private ComboBox _cboClientPicker = null!;
        private Panel _pnlPreviewContainer = null!;
        private Label _lblPreviewHeader = null!;
        private Label _lblPreviewBody = null!;
        private Panel _pnlPreviewMetricsBox = null!;
        private Label _lblMetricAcq = null!;
        private Label _lblMetricEst = null!;
        private Label _lblMetricGain = null!;
        private Button _btnPreviewCtaMockup = null!;
        private Label _lblPreviewFooter = null!;

        private Button _btnOpenHtmlBrowser = null!;
        private TextBox _txtTestEmail = null!;
        private Button _btnSendTestEmail = null!;
        private Button _btnRunBatchNow = null!;
        private Button _btnSaveSettings = null!;
        private Button _btnResetSettings = null!;
        private Label _lblLastRunInfo = null!;

        // Bottom Card: Audit Trail
        private DataGridView _gridAuditHistory = null!;
        private PaginationControl _paginationAudit = null!;
        private List<MarketUpdateLog> _allAuditLogs = new();
        private string? _auditFilterStatus = null;

        // Model State
        private AutomatedEmailSettings _currentSettings = null!;
        private ValuationMetrics? _latestPreviewMetrics = null!;
        private bool _isLoadingSettings;

        public ClientRetentionView()
        {
            InitializeComponentLayout();
            _ = LoadAutomatedSettingsAsync();
        }

        private void InitializeComponentLayout()
        {
            this.SuspendLayout();
            this.BackColor = Theme.Background;
            this.Dock = DockStyle.Fill;

            // ── TOP HEADER ──
            _pnlHeader = new Panel
            {
                Dock = DockStyle.Top,
                Height = 84,
                BackColor = Color.White,
                Padding = new Padding(28, 16, 28, 16)
            };

            _lblTitle = new Label
            {
                Text = "Client Retention",
                Font = new Font("Segoe UI", 16f, FontStyle.Bold),
                ForeColor = Color.FromArgb(15, 23, 42),
                Location = new Point(30, 14),
                AutoSize = true
            };

            _lblSubtitle = new Label
            {
                Text = "Automated property equity updates, marketing templates & client retention campaigns",
                Font = new Font("Segoe UI", 9.5f),
                ForeColor = Color.FromArgb(100, 116, 139),
                Location = new Point(30, 48),
                AutoSize = true
            };

            _btnRefresh = new Button
            {
                Text = "↻ Refresh",
                Anchor = AnchorStyles.Top | AnchorStyles.Right,
                BackColor = Color.White,
                Cursor = Cursors.Hand,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 9.5f),
                ForeColor = Color.FromArgb(71, 85, 105),
                Location = new Point(Math.Max(500, this.Width - 140), 20),
                Size = new Size(110, 40)
            };
            _btnRefresh.FlatAppearance.BorderColor = Color.FromArgb(226, 232, 240);
            UiRadiusHelper.StyleButton(_btnRefresh, 8);
            _btnRefresh.Click += (_, _) =>
            {
                RefreshAutomatedKpis();
                PopulateClientPicker();
                LoadAuditHistory();
                UpdateLivePreview();
            };

            _pnlHeader.Controls.Add(_lblTitle);
            _pnlHeader.Controls.Add(_lblSubtitle);
            _pnlHeader.Controls.Add(_btnRefresh);
            this.Controls.Add(_pnlHeader);

            // ── SCROLLABLE BODY ──
            _pnlContent = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Theme.Background,
                Padding = new Padding(30, 16, 30, 24),
                AutoScroll = true
            };

            // ── TOP SECTION: 4 KPI CARDS ──
            _pnlKpiContainer = new TableLayoutPanel
            {
                Dock = DockStyle.Top,
                Height = 106,
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

            _kpiEnrolled = new KpiCard("ENROLLED CLIENTS", "clients", Color.FromArgb(15, 23, 42), KpiIconType.Users, "Past clients tracked")
            {
                Dock = DockStyle.Fill,
                Margin = new Padding(0, 0, 8, 0),
                ClickMode = KpiClickMode.InPlaceFilter
            };
            _kpiDelivered = new KpiCard("UPDATES DELIVERED", "delivered", Color.FromArgb(15, 23, 42), KpiIconType.Briefcase, "0 emails sent this cycle")
            {
                Dock = DockStyle.Fill,
                Margin = new Padding(4, 0, 6, 0),
                ClickMode = KpiClickMode.InPlaceFilter
            };
            _kpiEquity = new KpiCard("AVG. EQUITY SHOWN", "equity", Color.FromArgb(22, 163, 74), KpiIconType.Currency, "₱0 avg gain")
            {
                Dock = DockStyle.Fill,
                Margin = new Padding(6, 0, 4, 0),
                ClickMode = KpiClickMode.InPlaceFilter
            };
            _kpiStatus = new KpiCard("AUTOMATION STATUS", "status", Color.FromArgb(217, 119, 6), KpiIconType.Clock, "Scheduled dispatch paused")
            {
                Dock = DockStyle.Fill,
                Margin = new Padding(8, 0, 0, 0),
                ClickMode = KpiClickMode.InPlaceFilter
            };

            _kpiEnrolled.Click += (_, _) => FilterAuditTrail(null);
            _kpiDelivered.Click += (_, _) => FilterAuditTrail("Sent");
            _kpiEquity.Click += (_, _) =>
            {
                FilterAuditTrail(null);
                _numAppreciation.Focus();
                _numAppreciation.Select(0, _numAppreciation.Text.Length);
            };
            _kpiStatus.Click += (_, _) => FilterAuditTrail("Success");

            _pnlKpiContainer.Controls.Add(_kpiEnrolled, 0, 0);
            _pnlKpiContainer.Controls.Add(_kpiDelivered, 1, 0);
            _pnlKpiContainer.Controls.Add(_kpiEquity, 2, 0);
            _pnlKpiContainer.Controls.Add(_kpiStatus, 3, 0);

            // ── MIDDLE SECTION: 2 Balanced Columns (50% / 50%) ──
            var tableSplit = new TableLayoutPanel
            {
                Dock = DockStyle.Top,
                Height = 790,
                ColumnCount = 2,
                RowCount = 1,
                BackColor = Color.Transparent,
                Margin = new Padding(0, 16, 0, 0)
            };
            tableSplit.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));
            tableSplit.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));
            tableSplit.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));

            // ── LEFT CARD: Campaign Template Architecture ──
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
                Text = "Campaign Template Architecture",
                Font = new Font("Segoe UI", 11.5f, FontStyle.Bold),
                ForeColor = Color.FromArgb(15, 23, 42),
                Location = new Point(24, 18),
                AutoSize = true
            };

            var lblLeftSubtitle = new Label
            {
                Text = "Configure automated equity updates, marketing templates, and client retention campaigns.",
                Font = new Font("Segoe UI", 8.5f),
                ForeColor = Color.FromArgb(100, 116, 139),
                Location = new Point(24, 42),
                AutoSize = true
            };

            // 3-Segment Pill Control in Card Header (Fully sized so no text is cut off)
            var pnlSegments = new Panel
            {
                Location = new Point(leftCard.Width - 245, 18),
                Size = new Size(226, 28),
                BackColor = Color.FromArgb(241, 245, 249)
            };
            UiRadiusHelper.ApplyRoundedCorners(pnlSegments, 6);

            _btnSegmentActive = new Button
            {
                Text = "ACTIVE",
                Location = new Point(2, 2),
                Size = new Size(72, 24),
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand,
                Font = new Font("Segoe UI", 7.5f, FontStyle.Regular),
                BackColor = Color.Transparent,
                ForeColor = Color.FromArgb(148, 163, 184)
            };
            _btnSegmentActive.FlatAppearance.BorderSize = 0;
            UiRadiusHelper.ApplyRoundedCorners(_btnSegmentActive, 4);
            _btnSegmentActive.Click += (_, _) => SetAutomationEngineState("ACTIVE");

            _btnSegmentPaused = new Button
            {
                Text = "PAUSED",
                Location = new Point(76, 2),
                Size = new Size(72, 24),
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand,
                Font = new Font("Segoe UI", 7.5f, FontStyle.Bold),
                BackColor = Color.FromArgb(217, 119, 6),
                ForeColor = Color.White
            };
            _btnSegmentPaused.FlatAppearance.BorderSize = 0;
            UiRadiusHelper.ApplyRoundedCorners(_btnSegmentPaused, 4);
            _btnSegmentPaused.Click += (_, _) => SetAutomationEngineState("PAUSED");

            _btnSegmentStopped = new Button
            {
                Text = "STOPPED",
                Location = new Point(150, 2),
                Size = new Size(74, 24),
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand,
                Font = new Font("Segoe UI", 7.5f, FontStyle.Regular),
                BackColor = Color.Transparent,
                ForeColor = Color.FromArgb(148, 163, 184)
            };
            _btnSegmentStopped.FlatAppearance.BorderSize = 0;
            UiRadiusHelper.ApplyRoundedCorners(_btnSegmentStopped, 4);
            _btnSegmentStopped.Click += (_, _) => SetAutomationEngineState("STOPPED");

            pnlSegments.Controls.Add(_btnSegmentActive);
            pnlSegments.Controls.Add(_btnSegmentPaused);
            pnlSegments.Controls.Add(_btnSegmentStopped);

            var pnlDivider = new Panel
            {
                Location = new Point(24, 68),
                Size = new Size(leftCard.Width - 48, 1),
                BackColor = Color.FromArgb(241, 245, 249)
            };

            // CAMPAIGN EMAIL TEMPLATE
            var lblTemplate = new Label
            {
                Text = "CAMPAIGN EMAIL TEMPLATE",
                Location = new Point(24, 80),
                AutoSize = true,
                Font = new Font("Segoe UI", 8f, FontStyle.Bold),
                ForeColor = Color.FromArgb(100, 116, 139)
            };

            _cboTemplates = new ComboBox
            {
                Location = new Point(24, 102),
                Size = new Size(leftCard.Width - 175, 32),
                DropDownStyle = ComboBoxStyle.DropDownList,
                Font = new Font("Segoe UI", 9.5f)
            };
            _cboTemplates.SelectedIndexChanged += CboTemplates_SelectedIndexChanged;

            _btnNewTemplate = new Button
            {
                Text = "+",
                Location = new Point(_cboTemplates.Right + 6, 101),
                Size = new Size(34, 32),
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand,
                Font = new Font("Segoe UI", 12f, FontStyle.Bold),
                BackColor = Color.White,
                ForeColor = Color.FromArgb(71, 85, 105)
            };
            _btnNewTemplate.FlatAppearance.BorderColor = Color.FromArgb(226, 232, 240);
            UiRadiusHelper.ApplyRoundedCorners(_btnNewTemplate, 6);
            _btnNewTemplate.Click += BtnNewTemplate_Click;

            _btnCloneTemplate = new Button
            {
                Text = "✏",
                Location = new Point(_btnNewTemplate.Right + 6, 101),
                Size = new Size(34, 32),
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand,
                Font = new Font("Segoe UI", 9.5f),
                BackColor = Color.White,
                ForeColor = Color.FromArgb(71, 85, 105)
            };
            _btnCloneTemplate.FlatAppearance.BorderColor = Color.FromArgb(226, 232, 240);
            UiRadiusHelper.ApplyRoundedCorners(_btnCloneTemplate, 6);
            _btnCloneTemplate.Click += BtnCloneTemplate_Click;

            _btnDeleteTemplate = new Button
            {
                Text = "🗑",
                Location = new Point(_btnCloneTemplate.Right + 6, 101),
                Size = new Size(34, 32),
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand,
                Font = new Font("Segoe UI", 9.5f),
                BackColor = Color.White,
                ForeColor = Color.FromArgb(220, 38, 38)
            };
            _btnDeleteTemplate.FlatAppearance.BorderColor = Color.FromArgb(254, 202, 202);
            UiRadiusHelper.ApplyRoundedCorners(_btnDeleteTemplate, 6);
            _btnDeleteTemplate.Click += BtnDeleteTemplate_Click;

            _lblTemplateBadge = new Label
            {
                Text = "Protected system template — Clone it to customize.",
                Location = new Point(24, 138),
                AutoSize = true,
                Font = new Font("Segoe UI", 8f),
                ForeColor = Color.FromArgb(217, 119, 6)
            };

            // TARGET AUDIENCE & EMAIL PRESENTATION FORMAT
            var lblAudience = new Label
            {
                Text = "TARGET AUDIENCE",
                Location = new Point(24, 168),
                AutoSize = true,
                Font = new Font("Segoe UI", 8f, FontStyle.Bold),
                ForeColor = Color.FromArgb(100, 116, 139)
            };

            _cboAudience = new ComboBox
            {
                Location = new Point(24, 188),
                Size = new Size(220, 32),
                DropDownStyle = ComboBoxStyle.DropDownList,
                Font = new Font("Segoe UI", 9.5f)
            };
            _cboAudience.Items.AddRange(new object[] { "All Past Clients", "Buyers Only", "Sellers Only" });
            _cboAudience.SelectedIndex = 0;
            _cboAudience.SelectedIndexChanged += (_, _) =>
            {
                PopulateClientPicker();
                RefreshAutomatedKpis();
                if (!_isLoadingSettings && _selectedTemplate == null)
                {
                    string aud = _cboAudience.SelectedItem?.ToString() ?? "All Past Clients";
                    PromptLoadAudiencePreTemplate(aud);
                }
            };

            var lblFormat = new Label
            {
                Text = "EMAIL PRESENTATION FORMAT",
                Location = new Point(260, 168),
                AutoSize = true,
                Font = new Font("Segoe UI", 8f, FontStyle.Bold),
                ForeColor = Color.FromArgb(100, 116, 139)
            };

            _cboFormat = new ComboBox
            {
                Location = new Point(260, 188),
                Size = new Size(220, 32),
                DropDownStyle = ComboBoxStyle.DropDownList,
                Font = new Font("Segoe UI", 9.5f)
            };
            _cboFormat.Items.AddRange(new object[] { "Branded Client Report (Visual)", "1-on-1 Personal Note (Plain Text)" });
            _cboFormat.SelectedIndex = 0;
            _cboFormat.SelectedIndexChanged += (_, _) => UpdateLivePreview();

            // SENDING INTERVAL PER CLIENT & EST. ANNUAL GROWTH (%)
            var lblFreq = new Label
            {
                Text = "SENDING INTERVAL PER CLIENT",
                Location = new Point(24, 230),
                AutoSize = true,
                Font = new Font("Segoe UI", 8f, FontStyle.Bold),
                ForeColor = Color.FromArgb(100, 116, 139)
            };

            _cboFrequency = new ComboBox
            {
                Location = new Point(24, 250),
                Size = new Size(220, 32),
                DropDownStyle = ComboBoxStyle.DropDownList,
                Font = new Font("Segoe UI", 9.5f)
            };
            _cboFrequency.Items.AddRange(new object[]
            {
                "Every 30 Days (Monthly Check-in)",
                "Every 90 Days (Quarterly Valuation)",
                "Every 180 Days (Semi-Annual)",
                "Every 365 Days (Annual Anniversary)"
            });
            _cboFrequency.SelectedIndex = 2;

            var lblRate = new Label
            {
                Text = "EST. ANNUAL GROWTH (%)",
                Location = new Point(260, 230),
                AutoSize = true,
                Font = new Font("Segoe UI", 8f, FontStyle.Bold),
                ForeColor = Color.FromArgb(100, 116, 139)
            };

            _numAppreciation = new NumericUpDown
            {
                Location = new Point(260, 250),
                Size = new Size(220, 32),
                DecimalPlaces = 1,
                Minimum = 0.5m,
                Maximum = 50.0m,
                Value = 5.0m,
                Font = new Font("Segoe UI", 9.5f)
            };
            _numAppreciation.ValueChanged += (_, _) =>
            {
                UpdateLivePreview();
                RefreshAutomatedKpis();
            };

            // BROKERAGE BANNER / TITLE & ACTION BUTTON LABEL
            var lblBroker = new Label
            {
                Text = "BROKERAGE BANNER / TITLE",
                Location = new Point(24, 292),
                AutoSize = true,
                Font = new Font("Segoe UI", 8f, FontStyle.Bold),
                ForeColor = Color.FromArgb(100, 116, 139)
            };

            _txtBrokerageName = new TextBox
            {
                Location = new Point(24, 312),
                Size = new Size(220, 28),
                Font = new Font("Segoe UI", 9.5f)
            };
            _txtBrokerageName.TextChanged += (_, _) => UpdateLivePreview();

            var lblCta = new Label
            {
                Text = "ACTION BUTTON LABEL",
                Location = new Point(260, 292),
                AutoSize = true,
                Font = new Font("Segoe UI", 8f, FontStyle.Bold),
                ForeColor = Color.FromArgb(100, 116, 139)
            };

            _txtCtaText = new TextBox
            {
                Location = new Point(260, 312),
                Size = new Size(220, 28),
                Font = new Font("Segoe UI", 9.5f)
            };
            _txtCtaText.TextChanged += (_, _) => UpdateLivePreview();

            // EMAIL SUBJECT LINE
            var lblSubj = new Label
            {
                Text = "EMAIL SUBJECT LINE",
                Location = new Point(24, 354),
                AutoSize = true,
                Font = new Font("Segoe UI", 8f, FontStyle.Bold),
                ForeColor = Color.FromArgb(100, 116, 139)
            };

            _txtSubjectTemplate = new TextBox
            {
                Location = new Point(24, 374),
                Size = new Size(460, 28),
                Font = new Font("Segoe UI", 9.5f)
            };
            _txtSubjectTemplate.TextChanged += (_, _) => UpdateLivePreview();

            // DYNAMIC PERSONALIZATION TOKENS (WrapContents = true so none cut off)
            var lblTokens = new Label
            {
                Text = "DYNAMIC PERSONALIZATION TOKENS — click to insert at cursor",
                Location = new Point(24, 414),
                AutoSize = true,
                Font = new Font("Segoe UI", 8f, FontStyle.Bold),
                ForeColor = Color.FromArgb(100, 116, 139)
            };

            var pnlTokens = new FlowLayoutPanel
            {
                Location = new Point(24, 434),
                Size = new Size(460, 56),
                BackColor = Color.Transparent,
                WrapContents = true,
                AutoScroll = false
            };

            var tokens = new[]
            {
                "{CustomerName}", "{FirstName}", "{PropertyAddress}",
                "{PropertyType}", "{OriginalPrice}", "{EstimatedValue}",
                "{EquityGain}", "{YearsOwned}"
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
                    Font = new Font("Segoe UI", 8f, FontStyle.Bold),
                    BackColor = Color.FromArgb(239, 246, 255),
                    ForeColor = Color.FromArgb(29, 78, 216),
                    Padding = new Padding(6, 0, 6, 0),
                    Margin = new Padding(0, 0, 6, 4)
                };
                chip.FlatAppearance.BorderSize = 0;
                UiRadiusHelper.ApplyPillShape(chip);

                string tokText = t;
                chip.Click += (_, _) =>
                {
                    if (_txtBodyTemplate.ReadOnly) return;
                    int sel = _txtBodyTemplate.SelectionStart;
                    _txtBodyTemplate.Text = _txtBodyTemplate.Text.Insert(sel, tokText);
                    _txtBodyTemplate.SelectionStart = sel + tokText.Length;
                    _txtBodyTemplate.Focus();
                };
                pnlTokens.Controls.Add(chip);
            }

            // EMAIL COPY TEMPLATE
            var lblBody = new Label
            {
                Text = "EMAIL COPY TEMPLATE (Plain Text Body)",
                Location = new Point(24, 498),
                AutoSize = true,
                Font = new Font("Segoe UI", 8f, FontStyle.Bold),
                ForeColor = Color.FromArgb(100, 116, 139)
            };

            _txtBodyTemplate = new TextBox
            {
                Location = new Point(24, 520),
                Size = new Size(460, 140),
                Multiline = true,
                ScrollBars = ScrollBars.Vertical,
                Font = new Font("Segoe UI", 9f),
                BackColor = Color.FromArgb(248, 250, 252),
                BorderStyle = BorderStyle.FixedSingle
            };
            _txtBodyTemplate.TextChanged += (_, _) => UpdateLivePreview();

            // Action Buttons
            _btnSaveSettings = new Button
            {
                Text = "Save Configuration",
                Location = new Point(24, 672),
                Size = new Size(160, 38),
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand,
                Font = new Font("Segoe UI", 9f, FontStyle.Bold),
                BackColor = Color.FromArgb(11, 48, 86),
                ForeColor = Color.White
            };
            UiRadiusHelper.StyleButton(_btnSaveSettings, 8);
            _btnSaveSettings.Click += BtnSaveSettings_Click;

            _btnResetSettings = new Button
            {
                Text = "Reset Default",
                Location = new Point(194, 672),
                Size = new Size(120, 38),
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
            leftCard.Controls.Add(pnlSegments);
            leftCard.Controls.Add(pnlDivider);
            leftCard.Controls.Add(lblTemplate);
            leftCard.Controls.Add(_cboTemplates);
            leftCard.Controls.Add(_btnNewTemplate);
            leftCard.Controls.Add(_btnCloneTemplate);
            leftCard.Controls.Add(_btnDeleteTemplate);
            leftCard.Controls.Add(_lblTemplateBadge);
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
            leftCard.Controls.Add(_txtSubjectTemplate);
            leftCard.Controls.Add(lblTokens);
            leftCard.Controls.Add(pnlTokens);
            leftCard.Controls.Add(lblBody);
            leftCard.Controls.Add(_txtBodyTemplate);
            leftCard.Controls.Add(_btnSaveSettings);
            leftCard.Controls.Add(_btnResetSettings);

            // ── RIGHT COLUMN: Two Stacked Cards ──
            var pnlRightColumn = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.Transparent,
                Margin = new Padding(8, 0, 0, 0),
                AutoScroll = true
            };

            // CARD 1: Interactive Client Valuation Preview (Non-Editable / Non-Clickable Preview Container)
            var cardPreview = new Panel
            {
                Location = new Point(0, 0),
                Size = new Size(490, 520),
                BackColor = Color.White,
                Padding = new Padding(20)
            };
            UiRadiusHelper.StyleCard(cardPreview, 12);

            var lblRightTitle = new Label
            {
                Text = "Interactive Client Valuation Preview",
                Font = new Font("Segoe UI", 11.5f, FontStyle.Bold),
                ForeColor = Color.FromArgb(15, 23, 42),
                Location = new Point(20, 16),
                AutoSize = true
            };

            var lblRightSubtitle = new Label
            {
                Text = "Select any past transaction to inspect custom appreciation and layout.",
                Font = new Font("Segoe UI", 8.5f),
                ForeColor = Color.FromArgb(100, 116, 139),
                Location = new Point(20, 38),
                AutoSize = true
            };

            _cboClientPicker = new ComboBox
            {
                Location = new Point(20, 62),
                Size = new Size(450, 32),
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

            // Non-clickable, beautifully formatted Preview Mockup Panel
            _pnlPreviewContainer = new Panel
            {
                Location = new Point(20, 100),
                Size = new Size(450, 365),
                AutoScroll = true,
                BackColor = Color.FromArgb(248, 250, 252),
                Padding = new Padding(16),
                Cursor = Cursors.Default
            };
            UiRadiusHelper.ApplyRoundedCorners(_pnlPreviewContainer, 8);
            _pnlPreviewContainer.Paint += (s, e) =>
            {
                using var borderPen = new Pen(Color.FromArgb(226, 232, 240), 1);
                e.Graphics.DrawRectangle(borderPen, 0, 0, _pnlPreviewContainer.Width - 1, _pnlPreviewContainer.Height - 1);
            };

            _lblPreviewHeader = new Label
            {
                Location = new Point(12, 12),
                Font = new Font("Segoe UI", 8.5f),
                ForeColor = Color.FromArgb(71, 85, 105),
                Cursor = Cursors.Default,
                AutoSize = true
            };

            _lblPreviewBody = new Label
            {
                Location = new Point(12, 84),
                Font = new Font("Segoe UI", 9f),
                ForeColor = Color.FromArgb(15, 23, 42),
                Cursor = Cursors.Default,
                AutoSize = true
            };

            _pnlPreviewMetricsBox = new Panel
            {
                Location = new Point(12, 168),
                Size = new Size(410, 60),
                BackColor = Color.White,
                Cursor = Cursors.Default
            };
            UiRadiusHelper.ApplyRoundedCorners(_pnlPreviewMetricsBox, 6);
            _pnlPreviewMetricsBox.Paint += (s, e) =>
            {
                using var p = new Pen(Color.FromArgb(203, 213, 225), 1);
                e.Graphics.DrawRectangle(p, 0, 0, _pnlPreviewMetricsBox.Width - 1, _pnlPreviewMetricsBox.Height - 1);
            };

            _lblMetricAcq = new Label { Location = new Point(6, 6), Size = new Size(125, 48), Font = new Font("Segoe UI", 8f), Cursor = Cursors.Default };
            _lblMetricEst = new Label { Location = new Point(135, 6), Size = new Size(125, 48), Font = new Font("Segoe UI", 8f), Cursor = Cursors.Default };
            _lblMetricGain = new Label { Location = new Point(265, 6), Size = new Size(135, 48), Font = new Font("Segoe UI", 8f), Cursor = Cursors.Default };
            _pnlPreviewMetricsBox.Controls.AddRange(new Control[] { _lblMetricAcq, _lblMetricEst, _lblMetricGain });

            _btnPreviewCtaMockup = new Button
            {
                Location = new Point(12, 236),
                Size = new Size(410, 34),
                FlatStyle = FlatStyle.Flat,
                Enabled = false,
                Font = new Font("Segoe UI", 9f, FontStyle.Bold),
                BackColor = Color.FromArgb(11, 48, 86),
                ForeColor = Color.White,
                Cursor = Cursors.Default
            };
            _btnPreviewCtaMockup.FlatAppearance.BorderSize = 0;
            UiRadiusHelper.ApplyRoundedCorners(_btnPreviewCtaMockup, 6);

            _lblPreviewFooter = new Label
            {
                Location = new Point(12, 278),
                Font = new Font("Segoe UI", 8.5f),
                ForeColor = Color.FromArgb(100, 116, 139),
                Cursor = Cursors.Default,
                AutoSize = true
            };

            // Seamless mouse-wheel scrolling on hover without requiring explicit focus click
            void FocusPreviewContainer(object? s, EventArgs e)
            {
                if (!_pnlPreviewContainer.Focused && _pnlPreviewContainer.CanFocus)
                    _pnlPreviewContainer.Focus();
            }

            _pnlPreviewContainer.MouseEnter += FocusPreviewContainer;
            _lblPreviewHeader.MouseEnter += FocusPreviewContainer;
            _lblPreviewBody.MouseEnter += FocusPreviewContainer;
            _pnlPreviewMetricsBox.MouseEnter += FocusPreviewContainer;
            _lblMetricAcq.MouseEnter += FocusPreviewContainer;
            _lblMetricEst.MouseEnter += FocusPreviewContainer;
            _lblMetricGain.MouseEnter += FocusPreviewContainer;
            _btnPreviewCtaMockup.MouseEnter += FocusPreviewContainer;
            _lblPreviewFooter.MouseEnter += FocusPreviewContainer;

            _pnlPreviewContainer.Controls.AddRange(new Control[]
            {
                _lblPreviewHeader, _lblPreviewBody, _pnlPreviewMetricsBox, _btnPreviewCtaMockup, _lblPreviewFooter
            });

            _btnOpenHtmlBrowser = new Button
            {
                Text = "🌐 Open Rendered HTML in Browser",
                Location = new Point(20, 474),
                Size = new Size(290, 32),
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand,
                Font = new Font("Segoe UI", 8.5f, FontStyle.Bold),
                BackColor = Color.White,
                ForeColor = Color.FromArgb(2, 132, 199)
            };
            _btnOpenHtmlBrowser.FlatAppearance.BorderSize = 0;
            _btnOpenHtmlBrowser.Click += BtnOpenHtmlBrowser_Click;

            cardPreview.Controls.Add(lblRightTitle);
            cardPreview.Controls.Add(lblRightSubtitle);
            cardPreview.Controls.Add(_cboClientPicker);
            cardPreview.Controls.Add(_pnlPreviewContainer);
            cardPreview.Controls.Add(_btnOpenHtmlBrowser);

            // CARD 2: Instant Test Sample Dispatch
            var cardTestDispatch = new Panel
            {
                Location = new Point(0, 536),
                Size = new Size(490, 190),
                BackColor = Color.White,
                Padding = new Padding(20)
            };
            UiRadiusHelper.StyleCard(cardTestDispatch, 12);

            var lblTestSec = new Label
            {
                Text = "⚡ Instant Test Sample Dispatch",
                Font = new Font("Segoe UI", 10f, FontStyle.Bold),
                ForeColor = Color.FromArgb(15, 23, 42),
                Location = new Point(20, 16),
                AutoSize = true
            };

            _txtTestEmail = new TextBox
            {
                Location = new Point(20, 44),
                Size = new Size(300, 32),
                Font = new Font("Segoe UI", 9.5f),
                PlaceholderText = "admin@nexacrm.com"
            };

            _btnSendTestEmail = new Button
            {
                Text = "Send Test Email",
                Location = new Point(_txtTestEmail.Right + 10, 43),
                Size = new Size(130, 34),
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand,
                Font = new Font("Segoe UI", 9f, FontStyle.Bold),
                BackColor = Color.FromArgb(11, 48, 86),
                ForeColor = Color.White
            };
            UiRadiusHelper.StyleButton(_btnSendTestEmail, 6);
            _btnSendTestEmail.Click += BtnSendTestEmail_Click;

            _btnRunBatchNow = new Button
            {
                Text = "🚀 Run Automated Batch Now (Force Send All)",
                Location = new Point(20, 88),
                Size = new Size(440, 38),
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand,
                Font = new Font("Segoe UI", 9.5f, FontStyle.Bold),
                BackColor = Color.FromArgb(22, 163, 74),
                ForeColor = Color.White
            };
            UiRadiusHelper.StyleButton(_btnRunBatchNow, 8);
            _btnRunBatchNow.Click += BtnRunBatchNow_Click;

            _lblLastRunInfo = new Label
            {
                Location = new Point(20, 134),
                Size = new Size(440, 36),
                Font = new Font("Segoe UI", 8.5f),
                ForeColor = Color.FromArgb(100, 116, 139),
                Text = "Last batch run: Never (Engine ready)",
                TextAlign = ContentAlignment.MiddleCenter
            };

            cardTestDispatch.Controls.Add(lblTestSec);
            cardTestDispatch.Controls.Add(_txtTestEmail);
            cardTestDispatch.Controls.Add(_btnSendTestEmail);
            cardTestDispatch.Controls.Add(_btnRunBatchNow);
            cardTestDispatch.Controls.Add(_lblLastRunInfo);

            pnlRightColumn.Controls.Add(cardPreview);
            pnlRightColumn.Controls.Add(cardTestDispatch);

            tableSplit.Controls.Add(leftCard, 0, 0);
            tableSplit.Controls.Add(pnlRightColumn, 1, 0);

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

            _paginationAudit = new PaginationControl();
            _paginationAudit.SetItemLabel("audit logs");
            _paginationAudit.PageChanged += (_, _) => RenderPagedAuditLogs(resetPage: false);
            _paginationAudit.PageSizeChanged += (_, _) => RenderPagedAuditLogs(resetPage: true);
            pnlAuditCard.Controls.Add(_paginationAudit);
            _paginationAudit.BringToFront();

            _pnlContent.Controls.Add(pnlAuditCard);
            _pnlContent.Controls.Add(tableSplit);
            _pnlContent.Controls.Add(_pnlKpiContainer);

            this.Controls.Add(_pnlContent);

            // ── IMPORTANT: Enforce proper Dock Order so Header does NOT overlap Content ──
            _pnlHeader.SendToBack();
            _pnlContent.BringToFront();

            // Responsive layout adjustments
            this.Resize += (_, _) =>
            {
                int fullW = _pnlContent.ClientSize.Width - 60;
                if (fullW > 400)
                {
                    pnlAuditCard.Width = fullW;
                    _gridAuditHistory.Width = pnlAuditCard.ClientSize.Width - 48;
                    btnRefreshAudit.Left = pnlAuditCard.ClientSize.Width - 144;
                }

                int leftW = leftCard.ClientSize.Width - 48;
                if (leftW > 200)
                {
                    pnlSegments.Left = Math.Max(20, leftCard.ClientSize.Width - 24 - pnlSegments.Width);
                    pnlDivider.Width = leftW;
                    _cboTemplates.Width = Math.Max(120, leftW - 120);
                    _btnNewTemplate.Left = _cboTemplates.Right + 6;
                    _btnCloneTemplate.Left = _btnNewTemplate.Right + 6;
                    _btnDeleteTemplate.Left = _btnCloneTemplate.Right + 6;

                    int colW = Math.Max(100, (leftW - 16) / 2);
                    _cboAudience.Width = colW;
                    lblFormat.Left = 24 + colW + 16;
                    _cboFormat.Left = 24 + colW + 16;
                    _cboFormat.Width = colW;

                    _cboFrequency.Width = colW;
                    lblRate.Left = 24 + colW + 16;
                    _numAppreciation.Left = 24 + colW + 16;
                    _numAppreciation.Width = colW;

                    _txtBrokerageName.Width = colW;
                    lblCta.Left = 24 + colW + 16;
                    _txtCtaText.Left = 24 + colW + 16;
                    _txtCtaText.Width = colW;

                    _txtSubjectTemplate.Width = leftW;
                    pnlTokens.Width = leftW;
                    _txtBodyTemplate.Width = leftW;
                }

                int rightW = pnlRightColumn.ClientSize.Width;
                if (rightW > 200)
                {
                    cardPreview.Width = rightW;
                    cardTestDispatch.Width = rightW;

                    int cardW = cardPreview.ClientSize.Width - 40;
                    _cboClientPicker.Width = cardW;
                    _pnlPreviewContainer.Width = cardW;

                    _txtTestEmail.Width = Math.Max(100, cardW - 140);
                    _btnSendTestEmail.Left = _txtTestEmail.Right + 10;
                    _btnRunBatchNow.Width = cardW;
                    _lblLastRunInfo.Width = cardW;

                    LayoutPreviewControls();
                }
            };

            this.ResumeLayout(false);
        }

        private async Task LoadAutomatedSettingsAsync()
        {
            await Task.Yield();
            LoadAutomatedSettings();
        }

        private void RefreshAutomatedKpis()
        {
            try
            {
                int tenantId = CurrentSession.TenantId > 0 ? CurrentSession.TenantId : 1;
                var kpis = MarketUpdateBackgroundService.Instance.GetAnalyticsKpis(tenantId);

                bool isAgent = RbacService.IsAgent;
                _kpiEnrolled.SetValue(kpis.EnrolledClientsCount.ToString("N0"));
                _kpiEnrolled.SetValueColor(Color.FromArgb(15, 23, 42));
                _kpiEnrolled.SetSubtitle(isAgent ? $"{kpis.EnrolledClientsCount} of your past clients" : $"{kpis.EnrolledClientsCount} past clients tracked");

                _kpiDelivered.SetValue(kpis.LifetimeDeliveredCount.ToString("N0"));
                _kpiDelivered.SetValueColor(Color.FromArgb(15, 23, 42));
                _kpiDelivered.SetSubtitle(isAgent ? $"{kpis.LifetimeDeliveredCount} updates sent" : $"{kpis.LifetimeDeliveredCount} emails sent this cycle");

                _kpiEquity.SetValue($"₱{kpis.AvgClientEquityGain / 1_000_000m:F2}M");
                _kpiEquity.SetValueColor(Color.FromArgb(22, 163, 74));
                _kpiEquity.SetSubtitle($"₱{kpis.AvgClientEquityGain:N0} avg gain");

                if (kpis.IsActive)
                {
                    _kpiStatus.SetValue("ACTIVE");
                    _kpiStatus.SetValueColor(Color.FromArgb(22, 163, 74));
                    _kpiStatus.SetSubtitle("Scheduled dispatch active");
                }
                else
                {
                    _kpiStatus.SetValue("PAUSED");
                    _kpiStatus.SetValueColor(Color.FromArgb(217, 119, 6));
                    _kpiStatus.SetSubtitle("Scheduled dispatch paused");
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

        private void FilterAuditTrail(string? status)
        {
            if (string.Equals(_auditFilterStatus, status, StringComparison.OrdinalIgnoreCase))
            {
                _auditFilterStatus = null;
            }
            else
            {
                _auditFilterStatus = status;
            }

            _kpiEnrolled.SetSelected(_auditFilterStatus == null);
            _kpiDelivered.SetSelected(string.Equals(_auditFilterStatus, "Sent", StringComparison.OrdinalIgnoreCase));
            _kpiEquity.SetSelected(false);
            _kpiStatus.SetSelected(string.Equals(_auditFilterStatus, "Success", StringComparison.OrdinalIgnoreCase));

            LoadAuditHistory();
        }

        private void LoadAuditHistory(bool resetPage = false)
        {
            try
            {
                int tenantId = CurrentSession.TenantId > 0 ? CurrentSession.TenantId : 1;
                var history = MarketUpdateBackgroundService.Instance.GetDeliveryHistory(tenantId);

                if (!string.IsNullOrEmpty(_auditFilterStatus))
                {
                    history = history.Where(h => h.Status.Equals(_auditFilterStatus, StringComparison.OrdinalIgnoreCase)
                                              || (string.Equals(_auditFilterStatus, "Sent", StringComparison.OrdinalIgnoreCase) && (h.Status == "Sent" || h.Status == "Success"))).ToList();
                }

                _allAuditLogs = history;
                RenderPagedAuditLogs(resetPage);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[LoadAuditHistory] Error: {ex.Message}");
            }
        }

        private void RenderPagedAuditLogs(bool resetPage = false)
        {
            int total = _allAuditLogs.Count;
            int page = resetPage ? 1 : (_paginationAudit?.CurrentPage ?? 1);
            int pageSize = _paginationAudit?.PageSize ?? 25;

            _paginationAudit?.UpdatePagination(total, page, pageSize);

            _gridAuditHistory.Columns.Clear();

            if (total == 0)
            {
                _gridAuditHistory.DataSource = null;
                return;
            }

            int effectivePage = _paginationAudit?.CurrentPage ?? 1;
            var pageItems = _allAuditLogs.Skip((effectivePage - 1) * pageSize).Take(pageSize).ToList();

            _gridAuditHistory.DataSource = pageItems.Select(h => new
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

                LoadTemplatesList(_currentSettings.ActiveTemplateId);
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

        private void LoadTemplatesList(int? selectTemplateId = null)
        {
            try
            {
                int tenantId = CurrentSession.TenantId > 0 ? CurrentSession.TenantId : 1;
                _loadedTemplates = EmailTemplateService.Instance.GetActiveTemplates(tenantId);

                _cboTemplates.Items.Clear();
                int selectIndex = 0;

                for (int i = 0; i < _loadedTemplates.Count; i++)
                {
                    var t = _loadedTemplates[i];
                    string prefix = t.IsSystem ? "[SYSTEM]" : $"[{t.CreatedByRole.ToUpperInvariant()}]";
                    string display = $"{prefix} {t.Name} ({t.Category})";
                    _cboTemplates.Items.Add(display);

                    if (selectTemplateId.HasValue && t.TemplateId == selectTemplateId.Value)
                    {
                        selectIndex = i;
                    }
                    else if (!selectTemplateId.HasValue && _currentSettings?.ActiveTemplateId.HasValue == true && t.TemplateId == _currentSettings.ActiveTemplateId.Value)
                    {
                        selectIndex = i;
                    }
                }

                if (_cboTemplates.Items.Count > 0)
                {
                    _cboTemplates.SelectedIndex = selectIndex;
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[ClientRetentionView] Error loading templates: {ex.Message}");
            }
        }

        private void CboTemplates_SelectedIndexChanged(object? sender, EventArgs e)
        {
            if (_cboTemplates.SelectedIndex < 0 || _cboTemplates.SelectedIndex >= _loadedTemplates.Count)
                return;

            _selectedTemplate = _loadedTemplates[_cboTemplates.SelectedIndex];
            if (_selectedTemplate == null) return;

            bool isAgent = RbacService.IsAgent;

            // Load template content into editors
            _txtSubjectTemplate.Text = _selectedTemplate.Subject;
            _txtBodyTemplate.Text = _selectedTemplate.Body;
            if (!string.IsNullOrWhiteSpace(_selectedTemplate.CallToActionText))
            {
                _txtCtaText.Text = _selectedTemplate.CallToActionText;
            }

            // Sync presentation format
            _cboFormat.SelectedIndex = _selectedTemplate.EmailFormat.Equals("PlainText", StringComparison.OrdinalIgnoreCase) ? 1 : 0;

            // Sync target audience
            string aud = _selectedTemplate.TargetAudience;
            if (aud.Equals("Buyers", StringComparison.OrdinalIgnoreCase)) _cboAudience.SelectedIndex = 1;
            else if (aud.Equals("Sellers", StringComparison.OrdinalIgnoreCase)) _cboAudience.SelectedIndex = 2;
            else _cboAudience.SelectedIndex = 0;

            // RBAC Access Control & Visual State
            if (isAgent)
            {
                // Sales Agent: Read-Only for ALL master and system templates
                _txtSubjectTemplate.ReadOnly = true;
                _txtSubjectTemplate.BackColor = Color.FromArgb(248, 250, 252);
                _txtBodyTemplate.ReadOnly = true;
                _txtBodyTemplate.BackColor = Color.FromArgb(248, 250, 252);
                _txtCtaText.ReadOnly = true;
                _txtCtaText.BackColor = Color.FromArgb(248, 250, 252);
                _cboAudience.Enabled = false;
                _cboFormat.Enabled = false;

                _lblTemplateBadge.Text = "Protected organizational template (Read-only for agents)";
                _lblTemplateBadge.ForeColor = Color.FromArgb(100, 116, 139);

                _btnNewTemplate.Visible = false;
                _btnCloneTemplate.Visible = false;
                _btnDeleteTemplate.Visible = false;
            }
            else if (_selectedTemplate.IsSystem)
            {
                // Admin or Manager viewing System-generated template: protected against direct overwrite
                _txtSubjectTemplate.ReadOnly = true;
                _txtSubjectTemplate.BackColor = Color.FromArgb(248, 250, 252);
                _txtBodyTemplate.ReadOnly = true;
                _txtBodyTemplate.BackColor = Color.FromArgb(248, 250, 252);
                _txtCtaText.ReadOnly = true;
                _txtCtaText.BackColor = Color.FromArgb(248, 250, 252);
                _cboAudience.Enabled = false;
                _cboFormat.Enabled = false;

                _lblTemplateBadge.Text = "Protected system template — Clone it to customize.";
                _lblTemplateBadge.ForeColor = Color.FromArgb(217, 119, 6);

                _btnNewTemplate.Visible = true;
                _btnCloneTemplate.Visible = true;
                _btnCloneTemplate.Enabled = true;
                _btnDeleteTemplate.Visible = true;
                _btnDeleteTemplate.Enabled = false; // System templates cannot be deleted
            }
            else
            {
                // Admin or Manager viewing Custom Template: Full Edit Access
                _txtSubjectTemplate.ReadOnly = false;
                _txtSubjectTemplate.BackColor = Color.White;
                _txtBodyTemplate.ReadOnly = false;
                _txtBodyTemplate.BackColor = Color.White;
                _txtCtaText.ReadOnly = false;
                _txtCtaText.BackColor = Color.White;
                _cboAudience.Enabled = true;
                _cboFormat.Enabled = true;

                _lblTemplateBadge.Text = $"Custom template ({_selectedTemplate.CreatedByRole} created) — Fully editable.";
                _lblTemplateBadge.ForeColor = Color.FromArgb(22, 163, 74);

                _btnNewTemplate.Visible = true;
                _btnCloneTemplate.Visible = true;
                _btnCloneTemplate.Enabled = true;
                _btnDeleteTemplate.Visible = true;
                _btnDeleteTemplate.Enabled = true;
            }

            if (_currentSettings != null)
            {
                _currentSettings.ActiveTemplateId = _selectedTemplate.TemplateId;
            }

            UpdateLivePreview();
        }

        private void BtnNewTemplate_Click(object? sender, EventArgs e)
        {
            if (RbacService.IsAgent)
            {
                MessageBox.Show("Sales agents are not authorized to create organizational email templates.", "Access Denied", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            using var dlg = new Form
            {
                Text = "Create New Real Estate Marketing / Retention Template",
                Size = new Size(540, 580),
                StartPosition = FormStartPosition.CenterParent,
                FormBorderStyle = FormBorderStyle.FixedDialog,
                MaximizeBox = false,
                MinimizeBox = false,
                BackColor = Color.White
            };

            var lblName = new Label { Text = "Template Name *:", Location = new Point(24, 20), AutoSize = true, Font = new Font("Segoe UI", 9f, FontStyle.Bold) };
            var txtName = new TextBox { Location = new Point(24, 42), Size = new Size(475, 26), Font = new Font("Segoe UI", 9.5f) };

            var lblCat = new Label { Text = "Marketing Category *:", Location = new Point(24, 76), AutoSize = true, Font = new Font("Segoe UI", 9f, FontStyle.Bold) };
            var cboCat = new ComboBox { Location = new Point(24, 98), Size = new Size(230, 26), DropDownStyle = ComboBoxStyle.DropDownList, Font = new Font("Segoe UI", 9.5f) };
            cboCat.Items.AddRange(new object[] { "Equity Retention", "Listing CMA", "Client Milestone", "New Listing", "Showing VIP" });
            cboCat.SelectedIndex = 0;

            var lblAud = new Label { Text = "Target Audience *:", Location = new Point(269, 76), AutoSize = true, Font = new Font("Segoe UI", 9f, FontStyle.Bold) };
            var cboAud = new ComboBox { Location = new Point(269, 98), Size = new Size(230, 26), DropDownStyle = ComboBoxStyle.DropDownList, Font = new Font("Segoe UI", 9.5f) };
            cboAud.Items.AddRange(new object[] { "All Past Clients", "Buyers Only", "Sellers Only" });
            cboAud.SelectedIndex = 0;

            var lblFmt = new Label { Text = "Format *:", Location = new Point(24, 134), AutoSize = true, Font = new Font("Segoe UI", 9f, FontStyle.Bold) };
            var cboFmt = new ComboBox { Location = new Point(24, 156), Size = new Size(230, 26), DropDownStyle = ComboBoxStyle.DropDownList, Font = new Font("Segoe UI", 9.5f) };
            cboFmt.Items.AddRange(new object[] { "Branded HTML", "Plain Text" });
            cboFmt.SelectedIndex = 0;

            var lblCta = new Label { Text = "Call To Action Text:", Location = new Point(269, 134), AutoSize = true, Font = new Font("Segoe UI", 9f, FontStyle.Bold) };
            var txtCta = new TextBox { Location = new Point(269, 156), Size = new Size(230, 26), Text = "Schedule Valuation Consultation", Font = new Font("Segoe UI", 9.5f) };

            var lblSubj = new Label { Text = "Email Subject Line *:", Location = new Point(24, 192), AutoSize = true, Font = new Font("Segoe UI", 9f, FontStyle.Bold) };
            var txtSubj = new TextBox { Location = new Point(24, 214), Size = new Size(475, 26), Font = new Font("Segoe UI", 9.5f) };

            var lblBody = new Label { Text = "Email Copy Body *:", Location = new Point(24, 250), AutoSize = true, Font = new Font("Segoe UI", 9f, FontStyle.Bold) };
            var txtBody = new TextBox { Location = new Point(24, 272), Size = new Size(475, 180), Multiline = true, ScrollBars = ScrollBars.Vertical, Font = new Font("Segoe UI", 9.5f) };

            var btnCancel = new Button { Text = "Cancel", DialogResult = DialogResult.Cancel, Location = new Point(275, 476), Size = new Size(100, 36), BackColor = Color.White, ForeColor = Color.FromArgb(71, 85, 105), FlatStyle = FlatStyle.Flat, Font = new Font("Segoe UI", 9f) };
            btnCancel.FlatAppearance.BorderColor = Color.FromArgb(203, 213, 225);
            UiRadiusHelper.StyleButton(btnCancel, 6);

            var btnSave = new Button { Text = "Save Template", DialogResult = DialogResult.OK, Location = new Point(385, 476), Size = new Size(114, 36), BackColor = Color.FromArgb(15, 91, 158), ForeColor = Color.White, FlatStyle = FlatStyle.Flat, Font = new Font("Segoe UI", 9f, FontStyle.Bold) };
            UiRadiusHelper.StyleButton(btnSave, 6);

            dlg.Controls.AddRange(new Control[] { lblName, txtName, lblCat, cboCat, lblAud, cboAud, lblFmt, cboFmt, lblCta, txtCta, lblSubj, txtSubj, lblBody, txtBody, btnCancel, btnSave });
            dlg.AcceptButton = btnSave;
            dlg.CancelButton = btnCancel;

            if (dlg.ShowDialog(this) == DialogResult.OK)
            {
                string name = txtName.Text.Trim();
                string subj = txtSubj.Text.Trim();
                string body = txtBody.Text.Trim();

                if (string.IsNullOrWhiteSpace(name) || string.IsNullOrWhiteSpace(subj) || string.IsNullOrWhiteSpace(body))
                {
                    MessageBox.Show("Please provide a Template Name, Subject, and Body content.", "Validation Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                int tenantId = CurrentSession.TenantId > 0 ? CurrentSession.TenantId : 1;
                var newTpl = new EmailTemplate
                {
                    TenantId = tenantId,
                    Name = name,
                    Category = cboCat.SelectedItem?.ToString() ?? "Equity Retention",
                    TargetAudience = cboAud.SelectedIndex switch { 1 => "Buyers", 2 => "Sellers", _ => "All" },
                    EmailFormat = cboFmt.SelectedIndex == 1 ? "PlainText" : "Html",
                    Subject = subj,
                    Body = body,
                    CallToActionText = txtCta.Text.Trim(),
                    CallToActionUrl = "https://nexacrm.local/cma-request",
                    IsSystem = false,
                    IsActive = true,
                    CreatedByRole = RbacService.IsAdmin ? "Admin" : "Manager"
                };

                bool saved = EmailTemplateService.Instance.SaveTemplate(newTpl, tenantId, out string errMsg);
                if (saved)
                {
                    MessageBox.Show($"Email template '{name}' was created successfully.", "Template Created", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    LoadTemplatesList(newTpl.TemplateId);
                }
                else
                {
                    MessageBox.Show(errMsg, "Save Failed", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                }
            }
        }

        private void BtnCloneTemplate_Click(object? sender, EventArgs e)
        {
            if (_selectedTemplate == null) return;

            if (RbacService.IsAgent)
            {
                MessageBox.Show("Sales agents are not authorized to create or clone templates.", "Access Denied", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            using var promptForm = new Form
            {
                Text = "Clone Email Template",
                Size = new Size(420, 180),
                StartPosition = FormStartPosition.CenterParent,
                FormBorderStyle = FormBorderStyle.FixedDialog,
                MaximizeBox = false,
                MinimizeBox = false,
                BackColor = Color.White
            };

            var lblPrompt = new Label { Text = "Enter name for cloned template:", Location = new Point(20, 16), AutoSize = true, Font = new Font("Segoe UI", 9f, FontStyle.Bold) };
            var txtPrompt = new TextBox { Text = $"{_selectedTemplate.Name} (Copy)", Location = new Point(20, 42), Size = new Size(360, 26), Font = new Font("Segoe UI", 9.5f) };
            var btnOk = new Button { Text = "Clone", DialogResult = DialogResult.OK, Location = new Point(280, 88), Size = new Size(100, 32), BackColor = Color.FromArgb(15, 91, 158), ForeColor = Color.White, FlatStyle = FlatStyle.Flat, Font = new Font("Segoe UI", 9f, FontStyle.Bold) };
            var btnCancel = new Button { Text = "Cancel", DialogResult = DialogResult.Cancel, Location = new Point(190, 88), Size = new Size(80, 32), BackColor = Color.White, ForeColor = Color.FromArgb(71, 85, 105), FlatStyle = FlatStyle.Flat, Font = new Font("Segoe UI", 9f) };
            btnCancel.FlatAppearance.BorderColor = Color.FromArgb(203, 213, 225);
            UiRadiusHelper.StyleButton(btnOk, 6);
            UiRadiusHelper.StyleButton(btnCancel, 6);

            promptForm.Controls.AddRange(new Control[] { lblPrompt, txtPrompt, btnCancel, btnOk });
            promptForm.AcceptButton = btnOk;
            promptForm.CancelButton = btnCancel;

            if (promptForm.ShowDialog(this) == DialogResult.OK && !string.IsNullOrWhiteSpace(txtPrompt.Text))
            {
                int tenantId = CurrentSession.TenantId > 0 ? CurrentSession.TenantId : 1;
                var clone = EmailTemplateService.Instance.CloneAsCustom(_selectedTemplate.TemplateId, txtPrompt.Text.Trim(), tenantId, out string err);

                if (clone != null)
                {
                    MessageBox.Show($"Template cloned successfully as '{clone.Name}'. You can now customize and edit this template.", "Clone Created", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    LoadTemplatesList(clone.TemplateId);
                }
                else
                {
                    MessageBox.Show(err, "Cloning Failed", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                }
            }
        }

        private void BtnDeleteTemplate_Click(object? sender, EventArgs e)
        {
            if (_selectedTemplate == null) return;

            if (RbacService.IsAgent)
            {
                MessageBox.Show("Sales agents are not authorized to archive email templates.", "Access Denied", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            if (_selectedTemplate.IsSystem)
            {
                MessageBox.Show("Standard system factory templates cannot be deleted.", "Protected Template", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            var confirm = MessageBox.Show(
                $"Are you sure you want to archive template '{_selectedTemplate.Name}'?\n\nThis template will be soft-deleted. Past delivery audit logs will remain intact.",
                "Confirm Archive Template",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Question);

            if (confirm != DialogResult.Yes) return;

            int tenantId = CurrentSession.TenantId > 0 ? CurrentSession.TenantId : 1;
            bool ok = EmailTemplateService.Instance.SoftDeleteTemplate(_selectedTemplate.TemplateId, tenantId, out string err);

            if (ok)
            {
                MessageBox.Show("Template was archived successfully.", "Template Archived", MessageBoxButtons.OK, MessageBoxIcon.Information);
                LoadTemplatesList();
            }
            else
            {
                MessageBox.Show(err, "Archive Failed", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        private void ApplyRbacPermissions()
        {
            bool isAgent = RbacService.IsAgent;
            bool canManageGlobalSettings = RbacService.IsAdmin || RbacService.IsSuperAdmin;
            bool canManageTemplates = RbacService.IsAdmin || RbacService.IsSuperAdmin || RbacService.IsManager;

            if (_btnSegmentActive != null)
            {
                _btnSegmentActive.Enabled = !isAgent && canManageGlobalSettings;
                _btnSegmentPaused.Enabled = !isAgent && canManageGlobalSettings;
                _btnSegmentStopped.Enabled = !isAgent && canManageGlobalSettings;
            }

            if (isAgent)
            {
                // Brokerage title and interval frequency are global administrative settings
                _txtBrokerageName.ReadOnly = true;
                _txtBrokerageName.BackColor = Color.FromArgb(248, 250, 252);
                _cboFrequency.Enabled = false;

                // Template actions are disabled/hidden for agents
                _btnNewTemplate.Visible = false;
                _btnCloneTemplate.Visible = false;
                _btnDeleteTemplate.Visible = false;

                _txtSubjectTemplate.ReadOnly = true;
                _txtSubjectTemplate.BackColor = Color.FromArgb(248, 250, 252);
                _txtBodyTemplate.ReadOnly = true;
                _txtBodyTemplate.BackColor = Color.FromArgb(248, 250, 252);
                _txtCtaText.ReadOnly = true;
                _txtCtaText.BackColor = Color.FromArgb(248, 250, 252);
                _cboAudience.Enabled = false;
                _cboFormat.Enabled = false;

                // Save button disabled for agents so company-wide defaults are not overwritten
                _btnSaveSettings.Enabled = false;
                _btnSaveSettings.Text = "Master Settings (Admin/Manager Only)";
                _btnSaveSettings.BackColor = Color.FromArgb(148, 163, 184);
                _btnSaveSettings.Cursor = Cursors.Default;

                _btnResetSettings.Enabled = false;
                _btnResetSettings.Visible = false;

                // Scoped batch trigger text
                _btnRunBatchNow.Text = "🚀 Run Batch Update for My Clients Only";
            }
            else
            {
                _btnSaveSettings.Enabled = canManageTemplates;
                _btnSaveSettings.Text = "Save Configuration";
                _btnSaveSettings.BackColor = Color.FromArgb(11, 48, 86);
                _btnSaveSettings.Cursor = canManageTemplates ? Cursors.Hand : Cursors.Default;

                _btnResetSettings.Enabled = canManageTemplates;
                _btnResetSettings.Visible = true;
                _txtBrokerageName.ReadOnly = !canManageGlobalSettings;
                _txtBrokerageName.BackColor = canManageGlobalSettings ? Color.White : Color.FromArgb(248, 250, 252);
                _cboFrequency.Enabled = canManageGlobalSettings;

                _btnNewTemplate.Visible = canManageTemplates;
                _btnCloneTemplate.Visible = canManageTemplates;
                _btnDeleteTemplate.Visible = canManageTemplates;

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

        private void SetAutomationEngineState(string state)
        {
            if (RbacService.IsAgent)
            {
                MessageBox.Show("Sales agents are not authorized to toggle the retention engine state.", "Access Denied", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            if (!RbacService.IsAdmin && !RbacService.IsSuperAdmin)
            {
                MessageBox.Show(
                    "Only system administrators can toggle the brokerage-wide automated retention engine.",
                    "Access Denied",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);
                return;
            }

            if (state == "ACTIVE")
            {
                _currentSettings.IsEnabled = true;
            }
            else
            {
                _currentSettings.IsEnabled = false;
            }

            UpdateSegmentButtonsDisplay(state);
            int tenantId = CurrentSession.TenantId > 0 ? CurrentSession.TenantId : 1;
            MarketUpdateBackgroundService.Instance.SaveSettings(_currentSettings, tenantId);
            RefreshAutomatedKpis();
        }

        private void UpdateSegmentButtonsDisplay(string? state = null)
        {
            if (_btnSegmentActive == null || _btnSegmentPaused == null || _btnSegmentStopped == null) return;

            string current = state ?? (_currentSettings != null && _currentSettings.IsEnabled ? "ACTIVE" : "PAUSED");

            // ACTIVE button
            if (current == "ACTIVE")
            {
                _btnSegmentActive.BackColor = Color.FromArgb(22, 163, 74);
                _btnSegmentActive.ForeColor = Color.White;
                _btnSegmentActive.Font = new Font("Segoe UI", 7.5f, FontStyle.Bold);
            }
            else
            {
                _btnSegmentActive.BackColor = Color.Transparent;
                _btnSegmentActive.ForeColor = Color.FromArgb(148, 163, 184);
                _btnSegmentActive.Font = new Font("Segoe UI", 7.5f, FontStyle.Regular);
            }

            // PAUSED button
            if (current == "PAUSED")
            {
                _btnSegmentPaused.BackColor = Color.FromArgb(217, 119, 6);
                _btnSegmentPaused.ForeColor = Color.White;
                _btnSegmentPaused.Font = new Font("Segoe UI", 7.5f, FontStyle.Bold);
            }
            else
            {
                _btnSegmentPaused.BackColor = Color.Transparent;
                _btnSegmentPaused.ForeColor = Color.FromArgb(148, 163, 184);
                _btnSegmentPaused.Font = new Font("Segoe UI", 7.5f, FontStyle.Regular);
            }

            // STOPPED button
            if (current == "STOPPED")
            {
                _btnSegmentStopped.BackColor = Color.FromArgb(100, 116, 139);
                _btnSegmentStopped.ForeColor = Color.White;
                _btnSegmentStopped.Font = new Font("Segoe UI", 7.5f, FontStyle.Bold);
            }
            else
            {
                _btnSegmentStopped.BackColor = Color.Transparent;
                _btnSegmentStopped.ForeColor = Color.FromArgb(148, 163, 184);
                _btnSegmentStopped.Font = new Font("Segoe UI", 7.5f, FontStyle.Regular);
            }
        }

        private void UpdateToggleStateDisplay()
        {
            UpdateSegmentButtonsDisplay();
            RefreshAutomatedKpis();
        }

        private void BtnSaveSettings_Click(object? sender, EventArgs e)
        {
            if (!RbacService.IsAdmin && !RbacService.IsSuperAdmin && !RbacService.IsManager)
            {
                MessageBox.Show(
                    "Global company retention and template settings can only be saved by administrators or managers.",
                    "Access Denied",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);
                return;
            }

            int tenantId = CurrentSession.TenantId > 0 ? CurrentSession.TenantId : 1;

            // If a custom template is selected and editable, save template modifications as well
            if (_selectedTemplate != null && !_selectedTemplate.IsSystem)
            {
                _selectedTemplate.Subject = _txtSubjectTemplate.Text.Trim();
                _selectedTemplate.Body = _txtBodyTemplate.Text.Trim();
                _selectedTemplate.CallToActionText = _txtCtaText.Text.Trim();
                _selectedTemplate.TargetAudience = _cboAudience.SelectedIndex switch
                {
                    1 => "Buyers",
                    2 => "Sellers",
                    _ => "All"
                };
                _selectedTemplate.EmailFormat = _cboFormat.SelectedIndex == 1 ? "PlainText" : "Html";

                EmailTemplateService.Instance.SaveTemplate(_selectedTemplate, tenantId, out _);
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
            _currentSettings.ActiveTemplateId = _selectedTemplate?.TemplateId;

            bool saved = MarketUpdateBackgroundService.Instance.SaveSettings(_currentSettings, tenantId);

            if (saved)
            {
                MessageBox.Show(
                    "Automated market update configuration and template settings have been successfully saved.",
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
            if (_pnlPreviewContainer == null) return;

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

                string origStr = $"₱{metrics.OriginalPrice:N0}";
                string estStr = $"₱{metrics.EstimatedValue:N0}";
                string gainStr = $"+₱{metrics.EquityGain:N0} (+{metrics.EquityGainPercent:F1}%)";
                string rateStr = metrics.AnnualRatePercent.ToString("F1");
                string yearsStr = metrics.YearsOwned.ToString("F1");
                string gainPercentStr = metrics.EquityGainPercent.ToString("F1");

                string renderedSubject = MarketUpdateBackgroundService.ReplaceTokens(_currentSettings.SubjectTemplate, metrics.CustomerName, metrics.FirstName,
                    metrics.PropertyAddress, metrics.PropertyType, origStr, estStr, gainStr, gainPercentStr, rateStr, yearsStr, metrics.AgentName);

                string renderedBody = MarketUpdateBackgroundService.ReplaceTokens(_currentSettings.BodyTemplate, metrics.CustomerName, metrics.FirstName,
                    metrics.PropertyAddress, metrics.PropertyType, origStr, estStr, gainStr, gainPercentStr, rateStr, yearsStr, metrics.AgentName);

                string formatStr = _currentSettings.EmailFormat == "Html" ? "Branded Visual Report" : "Plain Text Note";

                // Update non-editable visual card labels
                _lblPreviewHeader.Text = $"TO: {recipient}\r\nSUBJECT: {renderedSubject}\r\nFORMAT: {formatStr}";

                string bodyText = renderedBody?.Trim() ?? "";
                if (!bodyText.StartsWith("Dear", StringComparison.OrdinalIgnoreCase) &&
                    !bodyText.StartsWith("Hi", StringComparison.OrdinalIgnoreCase) &&
                    !bodyText.StartsWith("Hello", StringComparison.OrdinalIgnoreCase))
                {
                    bodyText = $"Dear {metrics.FirstName},\r\n\r\n{bodyText}";
                }
                _lblPreviewBody.Text = bodyText;

                _lblMetricAcq.Text = $"ACQUISITION\r\n{origStr}\r\n({metrics.PropertyType})";
                _lblMetricAcq.ForeColor = Color.FromArgb(71, 85, 105);

                _lblMetricEst.Text = $"CURRENT VALUATION\r\n{estStr}\r\n(+{metrics.AnnualRatePercent:F1}% p.a.)";
                _lblMetricEst.ForeColor = Color.FromArgb(15, 23, 42);

                _lblMetricGain.Text = $"EST. EQUITY GAIN\r\n{gainStr}\r\n({yearsStr} yrs owned)";
                _lblMetricGain.ForeColor = Color.FromArgb(22, 163, 74);

                _btnPreviewCtaMockup.Text = $"👉 {_currentSettings.CallToActionText}";
                _lblPreviewFooter.Text = $"ADVISOR: {metrics.AgentName}\r\n{_currentSettings.BrokerageName} · Advisory Team";

                _btnOpenHtmlBrowser.Visible = _currentSettings.EmailFormat == "Html";

                LayoutPreviewControls();
            }
            catch (Exception ex)
            {
                _lblPreviewBody.Text = $"Preview generation note: {ex.Message}";
                LayoutPreviewControls();
            }
        }

        private void LayoutPreviewControls()
        {
            if (_pnlPreviewContainer == null || _lblPreviewHeader == null || _lblPreviewBody == null) return;

            _pnlPreviewContainer.SuspendLayout();
            try
            {
                // Reset scroll position before computing layout to prevent scroll offset distortion
                _pnlPreviewContainer.AutoScrollPosition = new Point(0, 0);

                int pad = 12;
                int innerW = Math.Max(200, _pnlPreviewContainer.ClientSize.Width - (pad * 2));

                _lblPreviewHeader.MaximumSize = new Size(innerW, 0);
                _lblPreviewHeader.AutoSize = true;
                _lblPreviewHeader.Width = innerW;
                _lblPreviewHeader.Location = new Point(pad, pad);

                _lblPreviewBody.MaximumSize = new Size(innerW, 0);
                _lblPreviewBody.AutoSize = true;
                _lblPreviewBody.Width = innerW;
                _lblPreviewBody.Location = new Point(pad, _lblPreviewHeader.Bottom + 12);

                _pnlPreviewMetricsBox.Width = innerW;
                _pnlPreviewMetricsBox.Height = 60;
                _pnlPreviewMetricsBox.Location = new Point(pad, _lblPreviewBody.Bottom + 12);

                int mColW = Math.Max(50, (_pnlPreviewMetricsBox.ClientSize.Width - 24) / 3);
                _lblMetricAcq.SetBounds(6, 6, mColW, 48);
                _lblMetricEst.SetBounds(6 + mColW + 6, 6, mColW, 48);
                _lblMetricGain.SetBounds(6 + (mColW + 6) * 2, 6, Math.Max(50, _pnlPreviewMetricsBox.ClientSize.Width - 12 - (mColW + 6) * 2), 48);

                _btnPreviewCtaMockup.Width = innerW;
                _btnPreviewCtaMockup.Height = 34;
                _btnPreviewCtaMockup.Location = new Point(pad, _pnlPreviewMetricsBox.Bottom + 12);

                _lblPreviewFooter.MaximumSize = new Size(innerW, 0);
                _lblPreviewFooter.AutoSize = true;
                _lblPreviewFooter.Width = innerW;
                _lblPreviewFooter.Location = new Point(pad, _btnPreviewCtaMockup.Bottom + 12);
            }
            finally
            {
                _pnlPreviewContainer.ResumeLayout(true);
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
    }
}
