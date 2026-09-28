using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using Microsoft.EntityFrameworkCore;
using CRMS_Peguit.domain.entities;
using CRMS_Peguit.infrastructure.data;
using CRMS_Peguit.winforms.Auth;
using CRMS_Peguit.winforms.Controllers;
using CRMS_Peguit.winforms.Controls;
using CRMS_Peguit.winforms.Models.Roles;
using CRMS_Peguit.winforms.Models.Services;
using CRMS_Peguit.winforms.Services;

namespace CRMS_Peguit.winforms.Views.Marketing
{
    public class ClientRetentionView : UserControl
    {
        private readonly RetentionController _retentionController;

        // Top Header & Tab Navigation
        private Panel _pnlHeader = null!;
        private Label _lblTitle = null!;
        private Label _lblSubtitle = null!;
        private Button _btnRefresh = null!;
        private Button _btnExportCsv = null!;
        private Button _btnNewRequest = null!;

        private Panel _pnlTabStrip = null!;
        private readonly List<Button> _tabButtons = new();
        private Button _activeTabButton = null!;
        private Panel _pnlTabContainer = null!;

        // Tab 0: Overview & Segments
        private Panel _tabSegments = null!;
        private TableLayoutPanel _pnlKpis = null!;
        private KpiCard _kpiTrackedClients = null!;
        private KpiCard _kpiActiveQueue = null!;
        private KpiCard _kpiPendingApprovals = null!;
        private KpiCard _kpiDispatchedMonth = null!;
        private Panel _pnlSegmentFilters = null!;
        private FlowLayoutPanel _flpSegmentPills = null!;
        private TextBox _txtSearchClients = null!;
        private Button _btnRecalculateAll = null!;
        private Button _btnTab0GoToAutomation = null!;
        private DataGridView _gridClients = null!;
        private GridSkeletonOverlay? _gridClientsSkeleton;
        private PaginationControl _paginationClients = null!;
        private List<RetentionCustomerRow> _allClients = new();
        private string _selectedSegmentFilter = "All";

        // Tab 1: Valuation Automation
        private Panel _tabValuation = null!;
        private CheckBox _chkEnableAutomation = null!;
        private NumericUpDown _numAppreciation = null!;
        private ComboBox _cboFrequency = null!;
        private ComboBox _cboAudience = null!;
        private ComboBox _cboEmailFormat = null!;
        private TextBox _txtBrokerageName = null!;
        private TextBox _txtCtaText = null!;
        private TextBox _txtSubjectTemplate = null!;
        private Button _btnSaveValuationSettings = null!;
        private Button _btnRunValuationScanNow = null!;
        private Button _btnSendAutomationTest = null!;
        private Panel _pnlLiveSamplePreview = null!;
        private Label _lblSamplePreviewSubject = null!;
        private Label _lblSamplePreviewBody = null!;
        private Button _btnSamplePreviewCta = null!;
        private Label _lblSamplePreviewSignature = null!;
        private AutomatedEmailSettings _currentValuationSettings = null!;

        // Tab 2: Email Templates Library
        private Panel _tabTemplates = null!;
        private Button _btnCreateNewTemplate = null!;
        private FlowLayoutPanel _flpTemplatePills = null!;
        private Button _btnTplFilterActive = null!;
        private Button _btnTplFilterArchived = null!;
        private Button _btnTplFilterAll = null!;
        private string _selectedTplFilter = "Active"; // "Active", "Archived", "All"
        private TextBox _txtSearchTemplates = null!;
        private FlowLayoutPanel _flpTemplatesNav = null!;
        private Label _lblTplEditorHeader = null!;
        private Label _lblTplAudienceBadge = null!;
        private Label _lblTplNameLabel = null!;
        private TextBox _txtTplName = null!;
        private Label _lblTplAudienceLabel = null!;
        private ComboBox _cboTplAudience = null!;
        private TextBox _txtTplSubject = null!;
        private TextBox _txtTplCta = null!;
        private TextBox _txtTplBody = null!;
        private Button _btnSaveTemplateChanges = null!;
        private Button _btnSendTemplateTest = null!;
        private Button _btnArchiveTemplate = null!;
        private Button _btnDeleteTemplate = null!;
        private Button _btnCreateTemplateSubmit = null!;
        private Button _btnCancelCreateTemplate = null!;
        private bool _isCreatingNewTemplate = false;
        private Label _lblTplPreviewSubject = null!;
        private Label _lblTplPreviewBody = null!;
        private Button _btnTplPreviewCta = null!;
        private Label _lblTplPreviewSignature = null!;
        private List<TemplateItemModel> _allTemplateModels = new();
        private TemplateItemModel? _selectedTemplateModel;

        // Tab 3: Campaign Queue & Dispatch
        private Panel _tabQueue = null!;
        private ComboBox _cboQueueStatusFilter = null!;
        private DateTimePicker _dtpQueueFrom = null!;
        private DateTimePicker _dtpQueueTo = null!;
        private Button _btnFilterQueue = null!;
        private Button _btnGenerateQueue = null!;
        private Button _btnDispatchAll = null!;
        private DataGridView _gridQueue = null!;
        private GridSkeletonOverlay? _gridQueueSkeleton;
        private PaginationControl _paginationQueue = null!;
        private Panel _pnlQueueEmptyState = null!;
        private List<RetentionQueueRow> _allQueueItems = new();
        private Panel _pnlQueuePreviewCard = null!;
        private Label _lblQueuePreviewSubject = null!;
        private Label _lblQueuePreviewDetails = null!;
        private TextBox _txtQueuePreviewBody = null!;
        private Button _btnDispatchSelected = null!;
        private RetentionQueueRow? _selectedQueueRow;

        // Tab 4: 1-to-1 Manual Outreach
        private Panel _tabOutreach = null!;
        private TextBox _txtSearchOutreach = null!;
        private ListBox _lstOutreachResults = null!;
        private Panel _pnlOutreachClientCard = null!;
        private Label _lblOutreachClientName = null!;
        private Label _lblOutreachClientDetails = null!;
        private Label _lblOutreachSegmentStatus = null!;
        private Label _lblOutreachCooldownAlert = null!;
        private ComboBox _cboOutreachTemplate = null!;
        private TextBox _txtOutreachIncentive = null!;
        private TextBox _txtOutreachSubject = null!;
        private TextBox _txtOutreachBody = null!;
        private Panel _pnlOutreachPreview = null!;
        private Label _lblPreviewTo = null!;
        private Label _lblPreviewSubject = null!;
        private Label _lblPreviewBody = null!;
        private Panel _pnlPreviewIncentiveBox = null!;
        private Label _lblPreviewIncentiveText = null!;
        private Label _lblPreviewSignature = null!;
        private Button _btnSendManualEmail = null!;
        private Button _btnSendTestEmail = null!;
        private RetentionCustomerRow? _selectedOutreachCustomer;
        private List<EmailTemplate> _loadedRetentionTemplates = new();

        // Tab 5: Advisory Service Requests (Incentive Approvals)
        private Panel _tabRequests = null!;
        private ComboBox _cboRequestStatusFilter = null!;
        private DateTimePicker _dtpRequestFrom = null!;
        private DateTimePicker _dtpRequestTo = null!;
        private Button _btnFilterRequests = null!;
        private Button _btnTab5NewRequest = null!;
        private DataGridView _gridRequests = null!;
        private GridSkeletonOverlay? _gridRequestsSkeleton;
        private PaginationControl _paginationRequests = null!;
        private Panel _pnlRequestsEmptyState = null!;
        private List<RetentionRequestRow> _allRequests = new();

        public ClientRetentionView()
        {
            _retentionController = new RetentionController();

            InitializeComponentLayout();
            _ = LoadInitialDataAsync();
        }

        private void InitializeComponentLayout()
        {
            this.SuspendLayout();
            this.BackColor = Theme.Background;
            this.Dock = DockStyle.Fill;

            // Security check: SuperAdmin is strictly blocked from tenant customer data
            if (RbacService.IsSuperAdmin)
            {
                BuildSuperAdminBlockedLayout();
                this.ResumeLayout(true);
                return;
            }

            BuildHeader();
            BuildTabStrip();

            // Fixed Top Container: guarantees Header sits at Y=0 and TabStrip sits at Y=84
            var pnlTop = new Panel
            {
                Dock = DockStyle.Top,
                Height = 130, // 84 Header + 46 Tab Strip
                BackColor = Color.White
            };

            _pnlTabStrip.Dock = DockStyle.Bottom;
            _pnlHeader.Dock = DockStyle.Top;
            pnlTop.Controls.Add(_pnlTabStrip);
            pnlTop.Controls.Add(_pnlHeader);

            _pnlTabContainer = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Theme.Background
            };

            // Order of controls: Container (Fill) added first, Top added second (docks at Y=0)
            this.Controls.Add(_pnlTabContainer);
            this.Controls.Add(pnlTop);

            // Build all 6 organized tabs
            BuildTab0Segments();
            BuildTab1Valuation();
            BuildTab2Templates();
            BuildTab3Queue();
            BuildTab4Outreach();
            BuildTab5Requests();

            // Set initial tab
            SwitchTab(0);

            this.ResumeLayout(true);
            this.Resize += (_, _) => LayoutHeaderButtons();
        }

        #region SuperAdmin Blocked Layout

        private void BuildSuperAdminBlockedLayout()
        {
            var pnlLock = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.White,
                Padding = new Padding(40)
            };

            var lblLock = new Label
            {
                Text = "🔒 Access Restricted — Tenant Business Data\n\n" +
                       "Super Admin accounts manage platform infrastructure only and are restricted from accessing " +
                       "tenant operational client data, email retention campaigns, and discretionary pricing concessions.",
                Font = new Font("Segoe UI", 12f, FontStyle.Regular),
                ForeColor = Color.FromArgb(100, 116, 139),
                TextAlign = ContentAlignment.MiddleCenter,
                Dock = DockStyle.Fill
            };

            pnlLock.Controls.Add(lblLock);
            this.Controls.Add(pnlLock);
        }

        #endregion

        #region Header & Tabs Navigation

        private void BuildHeader()
        {
            _pnlHeader = new Panel
            {
                Height = 84,
                BackColor = Color.White,
                Padding = new Padding(28, 14, 28, 14)
            };

            _lblTitle = new Label
            {
                Text = "Customer Retention & Email Automation",
                Font = new Font("Segoe UI", 15f, FontStyle.Bold),
                ForeColor = Color.FromArgb(15, 23, 42),
                Location = new Point(28, 14),
                AutoSize = true
            };

            _lblSubtitle = new Label
            {
                Text = "Lifecycle retention segments, automated equity valuation campaigns & client care service governance",
                Font = new Font("Segoe UI", 9.5f),
                ForeColor = Color.FromArgb(100, 116, 139),
                Location = new Point(28, 46),
                AutoSize = true
            };

            _btnNewRequest = new Button
            {
                Text = "➕ Request Client Care",
                Size = new Size(160, 36),
                BackColor = Theme.SidebarAccent,
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 9f, FontStyle.Bold),
                Cursor = Cursors.Hand
            };
            _btnNewRequest.FlatAppearance.BorderSize = 0;
            UiRadiusHelper.StyleButton(_btnNewRequest, 8);
            _btnNewRequest.Click += (s, e) => ShowNewRequestModal();

            _btnExportCsv = new Button
            {
                Text = "📥 Export CSV",
                Size = new Size(110, 36),
                BackColor = Color.White,
                ForeColor = Color.FromArgb(71, 85, 105),
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 9f),
                Cursor = Cursors.Hand
            };
            _btnExportCsv.FlatAppearance.BorderColor = Color.FromArgb(226, 232, 240);
            UiRadiusHelper.StyleButton(_btnExportCsv, 8);
            _btnExportCsv.Click += (s, e) => ExportCurrentViewToCsv();

            _btnRefresh = new Button
            {
                Text = "↻ Refresh",
                Size = new Size(95, 36),
                BackColor = Color.White,
                ForeColor = Color.FromArgb(71, 85, 105),
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 9f),
                Cursor = Cursors.Hand
            };
            _btnRefresh.FlatAppearance.BorderColor = Color.FromArgb(226, 232, 240);
            UiRadiusHelper.StyleButton(_btnRefresh, 8);
            _btnRefresh.Click += async (s, e) => await RefreshCurrentTabAsync();

            _pnlHeader.Controls.Add(_lblTitle);
            _pnlHeader.Controls.Add(_lblSubtitle);
            _pnlHeader.Controls.Add(_btnNewRequest);
            _pnlHeader.Controls.Add(_btnExportCsv);
            _pnlHeader.Controls.Add(_btnRefresh);

            _pnlHeader.Resize += (_, _) => LayoutHeaderButtons();
            LayoutHeaderButtons();
        }

        private void LayoutHeaderButtons()
        {
            if (_pnlHeader == null || _btnRefresh == null || _btnExportCsv == null || _btnNewRequest == null) return;
            int right = _pnlHeader.ClientSize.Width - 28;
            _btnRefresh.Location = new Point(right - _btnRefresh.Width, 24);
            _btnExportCsv.Location = new Point(_btnRefresh.Left - 10 - _btnExportCsv.Width, 24);
            _btnNewRequest.Location = new Point(_btnExportCsv.Left - 10 - _btnNewRequest.Width, 24);
        }

        private void BuildTabStrip()
        {
            _pnlTabStrip = new Panel
            {
                Height = 46,
                BackColor = Color.FromArgb(248, 250, 252),
                Padding = new Padding(20, 4, 20, 4)
            };

            var tabTitles = new[]
            {
                "📊 Overview & Segments",
                "⚙ Valuation Automation",
                "📝 Email Templates",
                "🚀 Campaign Queue",
                "✉ 1-to-1 Outreach",
                "🛡 Client Care Requests"
            };

            int x = 20;
            for (int i = 0; i < tabTitles.Length; i++)
            {
                int tabIndex = i;
                var btn = new Button
                {
                    Text = tabTitles[i],
                    Location = new Point(x, 5),
                    Size = new Size(158, 35),
                    FlatStyle = FlatStyle.Flat,
                    BackColor = Color.Transparent,
                    ForeColor = Color.FromArgb(100, 116, 139),
                    Font = new Font("Segoe UI", 8.5f, FontStyle.Regular),
                    Cursor = Cursors.Hand,
                    TextAlign = ContentAlignment.MiddleCenter
                };
                btn.FlatAppearance.BorderSize = 0;
                UiRadiusHelper.StyleButton(btn, 6);
                btn.Click += (s, e) => SwitchTab(tabIndex);

                _tabButtons.Add(btn);
                _pnlTabStrip.Controls.Add(btn);
                x += 162;
            }

            _pnlTabStrip.Paint += (s, e) =>
            {
                using var pen = new Pen(Color.FromArgb(226, 232, 240), 1f);
                e.Graphics.DrawLine(pen, 0, _pnlTabStrip.Height - 1, _pnlTabStrip.Width, _pnlTabStrip.Height - 1);
            };
        }

        private void SwitchTab(int index)
        {
            if (index < 0 || index >= _tabButtons.Count) return;

            for (int i = 0; i < _tabButtons.Count; i++)
            {
                var b = _tabButtons[i];
                if (i == index)
                {
                    _activeTabButton = b;
                    b.BackColor = Color.White;
                    b.ForeColor = Theme.SidebarAccent;
                    b.Font = new Font("Segoe UI", 8.5f, FontStyle.Bold);
                }
                else
                {
                    b.BackColor = Color.Transparent;
                    b.ForeColor = Color.FromArgb(100, 116, 139);
                    b.Font = new Font("Segoe UI", 8.5f, FontStyle.Regular);
                }
            }

            _pnlTabContainer.Controls.Clear();
            Control activeView = index switch
            {
                0 => _tabSegments,
                1 => _tabValuation,
                2 => _tabTemplates,
                3 => _tabQueue,
                4 => _tabOutreach,
                5 => _tabRequests,
                _ => _tabSegments
            };
            activeView.Dock = DockStyle.Fill;
            _pnlTabContainer.Controls.Add(activeView);
            activeView.BringToFront();

            _ = RefreshCurrentTabAsync();
        }

        private async Task RefreshCurrentTabAsync()
        {
            if (_activeTabButton == null) return;
            int idx = _tabButtons.IndexOf(_activeTabButton);

            try
            {
                if (idx == 0)
                {
                    await LoadSegmentsDataAsync();
                }
                else if (idx == 1)
                {
                    await LoadValuationSettingsDataAsync();
                }
                else if (idx == 2)
                {
                    await LoadTemplatesLibraryDataAsync();
                }
                else if (idx == 3)
                {
                    await LoadQueueDataAsync();
                }
                else if (idx == 4)
                {
                    await LoadManualEmailDataAsync();
                }
                else if (idx == 5)
                {
                    await LoadRequestsDataAsync();
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[ClientRetentionView] Refresh error: {ex.Message}");
            }
        }

        #endregion

        #region Guidance Banner Helper

        private Panel CreateGuidanceBanner(string title, string description, string? actionText = null, Action? actionCallback = null)
        {
            var pnl = new Panel
            {
                Dock = DockStyle.Top,
                Height = 64,
                BackColor = Color.FromArgb(240, 249, 255),
                Padding = new Padding(16, 8, 16, 8),
                Margin = new Padding(0, 0, 0, 10)
            };
            UiRadiusHelper.StyleCard(pnl, 8);

            var lblTitle = new Label
            {
                Text = title,
                Font = new Font("Segoe UI", 9.5f, FontStyle.Bold),
                ForeColor = Color.FromArgb(12, 74, 110),
                Location = new Point(14, 8),
                AutoSize = true,
                UseMnemonic = false
            };

            var lblDesc = new Label
            {
                Text = description,
                Font = new Font("Segoe UI", 8.5f),
                ForeColor = Color.FromArgb(3, 105, 161),
                Location = new Point(14, 30),
                Height = 22,
                AutoEllipsis = true,
                UseMnemonic = false
            };

            Button? btn = null;
            if (!string.IsNullOrEmpty(actionText) && actionCallback != null)
            {
                btn = new Button
                {
                    Text = actionText,
                    Dock = DockStyle.Right,
                    Width = 175,
                    Height = 34,
                    BackColor = Color.White,
                    ForeColor = Color.FromArgb(2, 132, 199),
                    FlatStyle = FlatStyle.Flat,
                    Font = new Font("Segoe UI", 8.5f, FontStyle.Bold),
                    Cursor = Cursors.Hand,
                    Padding = new Padding(10, 0, 10, 0)
                };
                btn.FlatAppearance.BorderColor = Color.FromArgb(186, 230, 253);
                UiRadiusHelper.StyleButton(btn, 6);
                btn.Click += (s, e) => actionCallback();
                pnl.Controls.Add(btn);
            }

            pnl.Controls.Add(lblTitle);
            pnl.Controls.Add(lblDesc);

            void UpdateDescWidth()
            {
                int btnReserve = btn != null ? btn.Width + 24 : 10;
                lblDesc.Width = Math.Max(200, pnl.ClientSize.Width - lblDesc.Left - btnReserve);
            }

            pnl.Resize += (_, _) => UpdateDescWidth();
            UpdateDescWidth();

            pnl.Paint += (s, e) =>
            {
                // Left accent bar
                using var brush = new SolidBrush(Color.FromArgb(2, 132, 199));
                e.Graphics.FillRectangle(brush, 0, 0, 4, pnl.Height);
            };

            return pnl;
        }

        #endregion

        #region Tab 0: Overview & Segments

        private void BuildTab0Segments()
        {
            _tabSegments = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Theme.Background,
                Padding = new Padding(28, 14, 28, 14)
            };

            var banner = CreateGuidanceBanner(
                "💡 Real Estate Retention Lifecycle Engine",
                "NEXA tracks past buyers by recency and interaction: New Client (<6m), Recent Client (<6m, 2+ deals), Repeat Client (6-24m, 2+ deals), At Risk (6-18m since contact), and Inactive (>18m). Anti-fatigue cooldown protects clients from email fatigue. (Strict Policy: Real estate advisory only — no retail coupons, vouchers, or sales discounts).",
                "⚙ Valuation Engine",
                () => SwitchTab(1));

            // 4 KPI Cards Container
            _pnlKpis = new TableLayoutPanel
            {
                Dock = DockStyle.Top,
                Height = 100,
                ColumnCount = 4,
                RowCount = 1,
                BackColor = Color.Transparent,
                Margin = new Padding(0, 8, 0, 10)
            };
            _pnlKpis.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25F));
            _pnlKpis.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25F));
            _pnlKpis.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25F));
            _pnlKpis.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25F));
            _pnlKpis.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));

            _kpiTrackedClients = new KpiCard("TRACKED CLIENTS", "0", Color.FromArgb(15, 23, 42), KpiIconType.Users, "Assigned past clients")
            {
                Dock = DockStyle.Fill,
                Margin = new Padding(0, 0, 6, 0),
                ClickMode = KpiClickMode.InPlaceFilter
            };
            _kpiActiveQueue = new KpiCard("ACTIVE QUEUE", "0", Color.FromArgb(217, 119, 6), KpiIconType.Clock, "Awaiting dispatch")
            {
                Dock = DockStyle.Fill,
                Margin = new Padding(4, 0, 6, 0),
                ClickMode = KpiClickMode.InPlaceFilter
            };
            _kpiPendingApprovals = new KpiCard("PENDING REQUESTS", "0", Color.FromArgb(220, 38, 38), KpiIconType.Briefcase, "Advisory requests pending")
            {
                Dock = DockStyle.Fill,
                Margin = new Padding(6, 0, 4, 0),
                ClickMode = KpiClickMode.InPlaceFilter
            };
            _kpiDispatchedMonth = new KpiCard("DISPATCHED (MONTH)", "0", Color.FromArgb(22, 163, 74), KpiIconType.Currency, "Delivered retention emails")
            {
                Dock = DockStyle.Fill,
                Margin = new Padding(6, 0, 0, 0),
                ClickMode = KpiClickMode.InPlaceFilter
            };

            _kpiTrackedClients.Click += (_, _) => FilterSegmentsByPill("All");
            _kpiActiveQueue.Click += (_, _) => SwitchTab(3);
            _kpiPendingApprovals.Click += (_, _) => SwitchTab(5);
            _kpiDispatchedMonth.Click += (_, _) => SwitchTab(3);

            _pnlKpis.Controls.Add(_kpiTrackedClients, 0, 0);
            _pnlKpis.Controls.Add(_kpiActiveQueue, 1, 0);
            _pnlKpis.Controls.Add(_kpiPendingApprovals, 2, 0);
            _pnlKpis.Controls.Add(_kpiDispatchedMonth, 3, 0);

            // Filter Bar
            _pnlSegmentFilters = new Panel
            {
                Dock = DockStyle.Top,
                Height = 52,
                BackColor = Color.White,
                Padding = new Padding(12, 8, 12, 8),
                Margin = new Padding(0, 10, 0, 10)
            };
            UiRadiusHelper.StyleCard(_pnlSegmentFilters, 8);

            _flpSegmentPills = new FlowLayoutPanel
            {
                Dock = DockStyle.Left,
                AutoSize = true,
                Height = 36,
                FlowDirection = FlowDirection.LeftToRight,
                WrapContents = false,
                AutoScroll = false
            };

            var pillNames = new[] { "All", "Repeat Client", "New Client", "Recent Client", "At Risk", "Inactive", "Prospective Client" };
            foreach (var name in pillNames)
            {
                var pill = new Button
                {
                    Text = name,
                    Height = 32,
                    AutoSize = true,
                    BackColor = name == "All" ? Theme.SidebarAccent : Color.FromArgb(241, 245, 249),
                    ForeColor = name == "All" ? Color.White : Color.FromArgb(71, 85, 105),
                    FlatStyle = FlatStyle.Flat,
                    Font = new Font("Segoe UI", 8.5f, name == "All" ? FontStyle.Bold : FontStyle.Regular),
                    Cursor = Cursors.Hand,
                    Margin = new Padding(0, 2, 5, 0)
                };
                pill.FlatAppearance.BorderSize = 0;
                UiRadiusHelper.StyleButton(pill, 16);
                pill.Click += (s, e) => FilterSegmentsByPill(name);
                _flpSegmentPills.Controls.Add(pill);
            }

            var pnlRightActions = new FlowLayoutPanel
            {
                Dock = DockStyle.Right,
                AutoSize = true,
                Height = 36,
                FlowDirection = FlowDirection.LeftToRight,
                WrapContents = false
            };

            _txtSearchClients = new TextBox
            {
                Width = 170,
                Font = new Font("Segoe UI", 9.5f),
                PlaceholderText = "Search clients...",
                Margin = new Padding(0, 4, 8, 0)
            };
            _txtSearchClients.TextChanged += (s, e) => ApplyClientFilters();

            _btnRecalculateAll = new Button
            {
                Text = "⚡ Recalculate",
                Width = 110,
                Height = 32,
                BackColor = Color.White,
                ForeColor = Theme.SidebarAccent,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 8.5f, FontStyle.Bold),
                Cursor = Cursors.Hand,
                Margin = new Padding(0, 2, 8, 0)
            };
            _btnRecalculateAll.FlatAppearance.BorderColor = Theme.SidebarAccent;
            UiRadiusHelper.StyleButton(_btnRecalculateAll, 6);
            _btnRecalculateAll.Click += async (s, e) => await OnRecalculateAllSegmentsAsync();

            _btnTab0GoToAutomation = new Button
            {
                Text = "⚙ Valuation",
                Width = 105,
                Height = 32,
                BackColor = Color.White,
                ForeColor = Color.FromArgb(71, 85, 105),
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 8.5f, FontStyle.Bold),
                Cursor = Cursors.Hand,
                Margin = new Padding(0, 2, 0, 0)
            };
            _btnTab0GoToAutomation.FlatAppearance.BorderColor = Color.FromArgb(226, 232, 240);
            UiRadiusHelper.StyleButton(_btnTab0GoToAutomation, 6);
            _btnTab0GoToAutomation.Click += (s, e) => SwitchTab(1);

            pnlRightActions.Controls.Add(_txtSearchClients);
            pnlRightActions.Controls.Add(_btnRecalculateAll);
            pnlRightActions.Controls.Add(_btnTab0GoToAutomation);

            _pnlSegmentFilters.Controls.Add(_flpSegmentPills);
            _pnlSegmentFilters.Controls.Add(pnlRightActions);

            // Clients DataGridView Card
            var pnlGridCard = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.White,
                Padding = new Padding(16),
                Margin = new Padding(0, 10, 0, 0)
            };
            UiRadiusHelper.StyleCard(pnlGridCard, 10);

            _gridClients = new DataGridView
            {
                Dock = DockStyle.Fill,
                BackgroundColor = Color.White,
                BorderStyle = BorderStyle.None,
                AllowUserToAddRows = false,
                AllowUserToDeleteRows = false,
                ReadOnly = true,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                MultiSelect = false,
                RowHeadersVisible = false,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill
            };
            UiGridHelper.ApplyModernGridStyle(_gridClients, 48);
            ConfigureClientsGridColumns();

            _paginationClients = new PaginationControl
            {
                Dock = DockStyle.Bottom,
                Height = 44
            };
            _paginationClients.SetItemLabel("clients");
            _paginationClients.PageChanged += (_, _) => RenderPagedClients(resetPage: false);
            _paginationClients.PageSizeChanged += (_, _) => RenderPagedClients(resetPage: true);

            pnlGridCard.Controls.Add(_gridClients);
            _gridClientsSkeleton = GridSkeletonOverlay.CreateForGrid(_gridClients);
            pnlGridCard.Controls.Add(_paginationClients);
            _gridClients.BringToFront();

            _tabSegments.Controls.Add(pnlGridCard);
            _tabSegments.Controls.Add(_pnlSegmentFilters);
            _tabSegments.Controls.Add(_pnlKpis);
            _tabSegments.Controls.Add(banner);

            banner.SendToBack();
            _pnlKpis.SendToBack();
            _pnlSegmentFilters.SendToBack();
            pnlGridCard.BringToFront();
        }

        private void ConfigureClientsGridColumns()
        {
            _gridClients.Columns.Clear();

            _gridClients.Columns.Add(new DataGridViewTextBoxColumn { Name = "CustomerId", HeaderText = "ID", FillWeight = 30 });
            _gridClients.Columns.Add(new DataGridViewTextBoxColumn { Name = "CustomerName", HeaderText = "Client Name", FillWeight = 110 });
            _gridClients.Columns.Add(new DataGridViewTextBoxColumn { Name = "Contact", HeaderText = "Email / Phone", FillWeight = 120 });
            _gridClients.Columns.Add(new DataGridViewTextBoxColumn { Name = "Segment", HeaderText = "Retention Segment", FillWeight = 95 });
            _gridClients.Columns.Add(new DataGridViewTextBoxColumn { Name = "LastClosed", HeaderText = "Last Closed Deal", FillWeight = 75 });
            _gridClients.Columns.Add(new DataGridViewTextBoxColumn { Name = "LastActivity", HeaderText = "Last Activity", FillWeight = 75 });
            _gridClients.Columns.Add(new DataGridViewTextBoxColumn { Name = "AssignedAgent", HeaderText = "Assigned Advisor", FillWeight = 95 });
            _gridClients.Columns.Add(new DataGridViewTextBoxColumn { Name = "Cooldown", HeaderText = "Cooldown Policy", FillWeight = 85 });

            UiGridHelper.AddActionsColumn(_gridClients, 64);

            _gridClients.CellPainting += GridClients_CellPainting;
            _gridClients.CellContentClick += GridClients_CellContentClick;
        }

        private void GridClients_CellPainting(object? sender, DataGridViewCellPaintingEventArgs e)
        {
            if (e.RowIndex < 0) return;

            int nameIdx = _gridClients.Columns["CustomerName"]?.Index ?? -1;
            int agentIdx = _gridClients.Columns["AssignedAgent"]?.Index ?? -1;
            int segIdx = _gridClients.Columns["Segment"]?.Index ?? -1;
            int coolIdx = _gridClients.Columns["Cooldown"]?.Index ?? -1;

            if (e.ColumnIndex == nameIdx || e.ColumnIndex == agentIdx)
            {
                string text = e.Value?.ToString() ?? string.Empty;
                UiGridHelper.PaintAvatarCell(_gridClients, e, text);
            }
            else if (e.ColumnIndex == segIdx || e.ColumnIndex == coolIdx)
            {
                string text = e.Value?.ToString() ?? string.Empty;
                UiGridHelper.PaintStatusText(_gridClients, e, text);
            }
        }

        private void GridClients_CellContentClick(object? sender, DataGridViewCellEventArgs e)
        {
            int actionIdx = _gridClients.Columns["Actions"]?.Index ?? -1;
            if (e.RowIndex < 0 || e.ColumnIndex != actionIdx) return;

            var row = _gridClients.Rows[e.RowIndex];
            int customerId = Convert.ToInt32(row.Cells["CustomerId"].Value);
            var customer = _allClients.FirstOrDefault(c => c.CustomerId == customerId);
            if (customer == null) return;

            var menu = new ContextMenuStrip();
            menu.Items.Add("✉ 1-to-1 Outreach", null, (s, ev) =>
            {
                _selectedOutreachCustomer = customer;
                SwitchTab(4);
            });

            menu.Items.Add("➕ Request Client Care Service", null, async (s, ev) =>
            {
                var custObj = await _retentionController.GetCustomerByIdAsync(customer.CustomerId);
                var dlg = new RetentionRequestDialog(_retentionController, custObj);
                dlg.ShowDialog(this);
                if (dlg.WasActionTaken) await RefreshCurrentTabAsync();
            });

            menu.Items.Add("⚡ Recalculate Segment", null, async (s, ev) =>
            {
                using var db = LocalDb.CreateContext(CurrentSession.TenantId);
                await RetentionCalculationService.RecalculateCustomerAsync(db, customer.CustomerId);
                await RefreshCurrentTabAsync();
            });

            var rect = _gridClients.GetCellDisplayRectangle(e.ColumnIndex, e.RowIndex, true);
            menu.Show(_gridClients, new Point(rect.Right - 150, rect.Bottom));
        }

        private void FilterSegmentsByPill(string pillName)
        {
            _selectedSegmentFilter = pillName;
            foreach (Control c in _flpSegmentPills.Controls)
            {
                if (c is Button b)
                {
                    bool isSelected = b.Text == pillName;
                    b.BackColor = isSelected ? Theme.SidebarAccent : Color.FromArgb(241, 245, 249);
                    b.ForeColor = isSelected ? Color.White : Color.FromArgb(71, 85, 105);
                    b.Font = new Font("Segoe UI", 8.5f, isSelected ? FontStyle.Bold : FontStyle.Regular);
                }
            }
            ApplyClientFilters();
        }

        private void ApplyClientFilters()
        {
            string search = _txtSearchClients.Text.Trim().ToLowerInvariant();
            var filtered = _allClients.AsEnumerable();

            if (_selectedSegmentFilter != "All")
            {
                filtered = filtered.Where(c => c.CurrentSegment.Equals(_selectedSegmentFilter, StringComparison.OrdinalIgnoreCase));
            }

            if (!string.IsNullOrWhiteSpace(search))
            {
                filtered = filtered.Where(c =>
                    c.FullName.ToLower().Contains(search) ||
                    c.Email.ToLower().Contains(search) ||
                    c.Phone.Contains(search) ||
                    c.AssignedAgentName.ToLower().Contains(search));
            }

            int total = filtered.Count();
            _paginationClients.UpdatePagination(total, _paginationClients.CurrentPage, _paginationClients.PageSize);
            RenderPagedClients(resetPage: true);
        }

        private void RenderPagedClients(bool resetPage = false)
        {
            if (resetPage) _paginationClients.ResetPage();

            string search = _txtSearchClients.Text.Trim().ToLowerInvariant();
            var filtered = _allClients.AsEnumerable();

            if (_selectedSegmentFilter != "All")
            {
                filtered = filtered.Where(c => c.CurrentSegment.Equals(_selectedSegmentFilter, StringComparison.OrdinalIgnoreCase));
            }

            if (!string.IsNullOrWhiteSpace(search))
            {
                filtered = filtered.Where(c =>
                    c.FullName.ToLower().Contains(search) ||
                    c.Email.ToLower().Contains(search) ||
                    c.Phone.Contains(search) ||
                    c.AssignedAgentName.ToLower().Contains(search));
            }

            int pSize = _paginationClients.PageSize;
            int page = _paginationClients.CurrentPage;

            var paged = filtered.Skip((page - 1) * pSize).Take(pSize).ToList();

            _gridClients.Rows.Clear();
            foreach (var c in paged)
            {
                string cooldownText = c.IsOnCooldown ? $"Cooldown ({c.DaysUntilCooldownExpires}d)" : "Ready";

                _gridClients.Rows.Add(
                    c.CustomerId,
                    c.FullName,
                    $"{c.Email} • {c.Phone}",
                    c.CurrentSegment,
                    c.LastClosedDate?.ToString("MMM dd, yyyy") ?? "None",
                    c.LastActivityDate?.ToString("MMM dd, yyyy") ?? "None",
                    c.AssignedAgentName,
                    cooldownText
                );
            }
        }

        private async Task LoadSegmentsDataAsync()
        {
            _kpiTrackedClients.ShowLoadingSkeleton();
            _kpiActiveQueue.ShowLoadingSkeleton();
            _kpiPendingApprovals.ShowLoadingSkeleton();
            _kpiDispatchedMonth.ShowLoadingSkeleton();
            _gridClientsSkeleton?.ShowSkeleton();

            try
            {
                using (var db = LocalDb.CreateContext(CurrentSession.TenantId))
                {
                    bool hasUncalculatedClosedClients = await db.Deals
                        .AnyAsync(d => (d.Stage == "Closed" || d.Stage == "Won") &&
                                       db.Customers.Any(c => c.CustomerId == d.CustomerId && c.CurrentRetentionSegment == "Prospective Client"));

                    if (hasUncalculatedClosedClients)
                    {
                        await RetentionCalculationService.RecalculateAllCustomersAsync(db, CurrentSession.TenantId);
                    }
                }

                var summary = await _retentionController.GetSummaryAsync();
                _kpiTrackedClients.SetValue(summary.TotalTrackedCustomers);
                _kpiActiveQueue.SetValue(summary.ActiveQueueCount);
                _kpiPendingApprovals.SetValue(summary.PendingApprovalsCount);
                _kpiDispatchedMonth.SetValue(summary.DispatchedThisMonthCount);

                _allClients = await _retentionController.GetCustomersAsync();
                _paginationClients.UpdatePagination(_allClients.Count, _paginationClients.CurrentPage, _paginationClients.PageSize);
                RenderPagedClients(resetPage: true);
            }
            finally
            {
                _kpiTrackedClients.HideLoadingSkeleton();
                _kpiActiveQueue.HideLoadingSkeleton();
                _kpiPendingApprovals.HideLoadingSkeleton();
                _kpiDispatchedMonth.HideLoadingSkeleton();
                _gridClientsSkeleton?.HideSkeleton();
            }
        }

        private async Task OnRecalculateAllSegmentsAsync()
        {
            _btnRecalculateAll.Enabled = false;
            _btnRecalculateAll.Text = "Calculating...";

            try
            {
                using var db = LocalDb.CreateContext(CurrentSession.TenantId);
                int changed = await RetentionCalculationService.RecalculateAllCustomersAsync(db, CurrentSession.TenantId);
                MessageBox.Show($"Recalculation completed. {changed} customer retention segments updated.", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
                await LoadSegmentsDataAsync();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Recalculation failed: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                _btnRecalculateAll.Enabled = true;
                _btnRecalculateAll.Text = "⚡ Recalculate";
            }
        }

        #endregion

        #region Tab 1: Valuation Automation

        private void BuildTab1Valuation()
        {
            _tabValuation = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Theme.Background,
                Padding = new Padding(28, 14, 28, 14),
                AutoScroll = true
            };

            var banner = CreateGuidanceBanner(
                "⚙ Automated Property Equity Valuation Engine",
                "Configure how NEXA automatically monitors homeowner equity gains over time. When homeowner purchase anniversaries arrive, NEXA calculates estimated value growth using your benchmark and prepares annual equity check-ups.",
                "📝 View Templates",
                () => SwitchTab(2));

            var splitTable = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 2,
                RowCount = 1,
                BackColor = Color.Transparent,
                Margin = new Padding(0, 10, 0, 0)
            };
            splitTable.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));
            splitTable.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));
            splitTable.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));

            // Left Panel: Valuation Settings Form
            var leftCard = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.White,
                Padding = new Padding(22),
                Margin = new Padding(0, 0, 10, 0),
                AutoScroll = true
            };
            UiRadiusHelper.StyleCard(leftCard, 10);

            var lblT = new Label { Text = "⚡ Valuation Benchmark & Campaign Cadence", Font = new Font("Segoe UI", 12.5f, FontStyle.Bold), Location = new Point(14, 12), AutoSize = true };
            var lblSub = new Label { Text = "Configure equity appreciation growth benchmarks, delivery cadence, and brokerage branding", Font = new Font("Segoe UI", 9f), ForeColor = Color.FromArgb(100, 116, 139), Location = new Point(14, 38), AutoSize = true };

            _chkEnableAutomation = new CheckBox
            {
                Text = "Enable Automated Retention Campaign Generation",
                Font = new Font("Segoe UI", 9.5f, FontStyle.Bold),
                ForeColor = Color.FromArgb(15, 23, 42),
                Location = new Point(16, 68),
                AutoSize = true,
                Checked = true
            };

            int y = 104;

            var lblRate = new Label { Text = "Annual Appreciation Benchmark (%):", Font = new Font("Segoe UI", 9f, FontStyle.Bold), Location = new Point(16, y), AutoSize = true };
            _numAppreciation = new NumericUpDown
            {
                Location = new Point(16, y + 22),
                Width = 160,
                DecimalPlaces = 2,
                Minimum = 0.1m,
                Maximum = 30.0m,
                Increment = 0.25m,
                Value = 5.0m,
                Font = new Font("Segoe UI", 9.5f)
            };
            _numAppreciation.ValueChanged += (s, e) => UpdateLiveSamplePreview();

            var lblFreq = new Label { Text = "Outreach Cadence (Days):", Font = new Font("Segoe UI", 9f, FontStyle.Bold), Location = new Point(220, y), AutoSize = true };
            _cboFrequency = new ComboBox
            {
                Location = new Point(220, y + 22),
                Width = 200,
                DropDownStyle = ComboBoxStyle.DropDownList,
                Font = new Font("Segoe UI", 9.5f)
            };
            _cboFrequency.Items.AddRange(new object[] { "90 Days (Quarterly)", "180 Days (Semi-Annual)", "365 Days (Annual)" });
            _cboFrequency.SelectedIndex = 1;

            y += 62;

            var lblAudience = new Label { Text = "Target Audience:", Font = new Font("Segoe UI", 9f, FontStyle.Bold), Location = new Point(16, y), AutoSize = true };
            _cboAudience = new ComboBox
            {
                Location = new Point(16, y + 22),
                Width = 200,
                DropDownStyle = ComboBoxStyle.DropDownList,
                Font = new Font("Segoe UI", 9.5f)
            };
            _cboAudience.Items.AddRange(new object[] { "All Past Clients", "Buyers Only", "Sellers Only" });
            _cboAudience.SelectedIndex = 0;

            var lblFormat = new Label { Text = "Email Delivery Format:", Font = new Font("Segoe UI", 9f, FontStyle.Bold), Location = new Point(230, y), AutoSize = true };
            _cboEmailFormat = new ComboBox
            {
                Location = new Point(230, y + 22),
                Width = 190,
                DropDownStyle = ComboBoxStyle.DropDownList,
                Font = new Font("Segoe UI", 9.5f)
            };
            _cboEmailFormat.Items.AddRange(new object[] { "Branded HTML Report", "Plain Text Personal Note" });
            _cboEmailFormat.SelectedIndex = 0;
            _cboEmailFormat.SelectedIndexChanged += (s, e) => UpdateLiveSamplePreview();

            y += 62;

            var lblBroker = new Label { Text = "Brokerage Display Name:", Font = new Font("Segoe UI", 9f, FontStyle.Bold), Location = new Point(16, y), AutoSize = true };
            _txtBrokerageName = new TextBox
            {
                Location = new Point(16, y + 22),
                Width = 420,
                Font = new Font("Segoe UI", 9.5f),
                Text = "NEXA Real Estate Advisory"
            };
            _txtBrokerageName.TextChanged += (s, e) => UpdateLiveSamplePreview();

            y += 62;

            var lblCta = new Label { Text = "Valuation Call to Action:", Font = new Font("Segoe UI", 9f, FontStyle.Bold), Location = new Point(16, y), AutoSize = true };
            _txtCtaText = new TextBox
            {
                Location = new Point(16, y + 22),
                Width = 420,
                Font = new Font("Segoe UI", 9.5f),
                Text = "Schedule Annual Home Check-up"
            };
            _txtCtaText.TextChanged += (s, e) => UpdateLiveSamplePreview();

            y += 62;

            var lblSubjTpl = new Label { Text = "Subject Template:", Font = new Font("Segoe UI", 9f, FontStyle.Bold), Location = new Point(16, y), AutoSize = true };
            _txtSubjectTemplate = new TextBox
            {
                Location = new Point(16, y + 22),
                Width = 420,
                Font = new Font("Segoe UI", 9.5f),
                Text = "Market Valuation & Equity Report for {PropertyAddress}"
            };
            _txtSubjectTemplate.TextChanged += (s, e) => UpdateLiveSamplePreview();

            y += 68;

            _btnSaveValuationSettings = new Button
            {
                Text = "💾 Save Automation Settings",
                Location = new Point(16, y),
                Size = new Size(210, 38),
                BackColor = Theme.SidebarAccent,
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 9f, FontStyle.Bold),
                Cursor = Cursors.Hand
            };
            _btnSaveValuationSettings.FlatAppearance.BorderSize = 0;
            UiRadiusHelper.StyleButton(_btnSaveValuationSettings, 8);
            _btnSaveValuationSettings.Click += async (s, e) => await OnSaveValuationSettingsAsync();

            _btnRunValuationScanNow = new Button
            {
                Text = "⚡ Run Campaign Scan Now",
                Location = new Point(235, y),
                Size = new Size(200, 38),
                BackColor = Color.FromArgb(22, 163, 74),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 9f, FontStyle.Bold),
                Cursor = Cursors.Hand
            };
            _btnRunValuationScanNow.FlatAppearance.BorderSize = 0;
            UiRadiusHelper.StyleButton(_btnRunValuationScanNow, 8);
            _btnRunValuationScanNow.Click += async (s, e) => await OnRunScanAndGoToQueueAsync();

            y += 48;

            _btnSendAutomationTest = new Button
            {
                Text = "✉ Send Sample Test Email to My Inbox",
                Location = new Point(16, y),
                Size = new Size(420, 34),
                BackColor = Color.White,
                ForeColor = Color.FromArgb(71, 85, 105),
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 9f),
                Cursor = Cursors.Hand
            };
            _btnSendAutomationTest.FlatAppearance.BorderColor = Color.FromArgb(226, 232, 240);
            UiRadiusHelper.StyleButton(_btnSendAutomationTest, 8);
            _btnSendAutomationTest.Click += async (s, e) => await OnSendAutomationTestAsync();

            leftCard.Controls.Add(lblT);
            leftCard.Controls.Add(lblSub);
            leftCard.Controls.Add(_chkEnableAutomation);
            leftCard.Controls.Add(lblRate);
            leftCard.Controls.Add(_numAppreciation);
            leftCard.Controls.Add(lblFreq);
            leftCard.Controls.Add(_cboFrequency);
            leftCard.Controls.Add(lblAudience);
            leftCard.Controls.Add(_cboAudience);
            leftCard.Controls.Add(lblFormat);
            leftCard.Controls.Add(_cboEmailFormat);
            leftCard.Controls.Add(lblBroker);
            leftCard.Controls.Add(_txtBrokerageName);
            leftCard.Controls.Add(lblCta);
            leftCard.Controls.Add(_txtCtaText);
            leftCard.Controls.Add(lblSubjTpl);
            leftCard.Controls.Add(_txtSubjectTemplate);
            leftCard.Controls.Add(_btnSaveValuationSettings);
            leftCard.Controls.Add(_btnRunValuationScanNow);
            leftCard.Controls.Add(_btnSendAutomationTest);
            splitTable.Controls.Add(leftCard, 0, 0);

            // Right Panel: Live Sample Preview Card
            _pnlLiveSamplePreview = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.White,
                Padding = new Padding(22),
                Margin = new Padding(10, 0, 0, 0),
                AutoScroll = true
            };
            UiRadiusHelper.StyleCard(_pnlLiveSamplePreview, 10);

            var previewCard = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.FromArgb(248, 250, 252),
                Padding = new Padding(20),
                AutoScroll = true
            };
            UiRadiusHelper.StyleCard(previewCard, 8);

            var lblPrevTitle = new Label
            {
                Text = "Live Automated Email Sample Preview",
                Font = new Font("Segoe UI", 12f, FontStyle.Bold),
                ForeColor = Color.FromArgb(15, 23, 42),
                Location = new Point(16, 12),
                AutoSize = true
            };

            var lblPrevSub = new Label
            {
                Text = "Real-time rendering of the equity update delivered to past homeowners",
                Font = new Font("Segoe UI", 8.5f),
                ForeColor = Color.FromArgb(100, 116, 139),
                Location = new Point(16, 36),
                AutoSize = true
            };

            _lblSamplePreviewSubject = new Label
            {
                Text = "Subject: Market Valuation & Equity Report for Unit 1204, One Serendra, BGC",
                Font = new Font("Segoe UI", 10.5f, FontStyle.Bold),
                ForeColor = Theme.SidebarAccent,
                Location = new Point(16, 68),
                Size = new Size(440, 24)
            };

            var div = new Panel
            {
                Location = new Point(16, 98),
                Size = new Size(440, 1),
                BackColor = Color.FromArgb(226, 232, 240)
            };

            _lblSamplePreviewBody = new Label
            {
                Font = new Font("Segoe UI", 9.5f),
                ForeColor = Color.FromArgb(30, 41, 59),
                Location = new Point(16, 110),
                Size = new Size(440, 270)
            };

            _btnSamplePreviewCta = new Button
            {
                Text = "Schedule Annual Home Check-up",
                Location = new Point(16, 390),
                Size = new Size(320, 38),
                BackColor = Theme.SidebarAccent,
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 9.5f, FontStyle.Bold),
                Cursor = Cursors.Hand
            };
            _btnSamplePreviewCta.FlatAppearance.BorderSize = 0;
            UiRadiusHelper.StyleButton(_btnSamplePreviewCta, 6);

            _lblSamplePreviewSignature = new Label
            {
                Font = new Font("Segoe UI", 9f),
                ForeColor = Color.FromArgb(100, 116, 139),
                Location = new Point(16, 440),
                Size = new Size(440, 60)
            };

            previewCard.Controls.Add(lblPrevTitle);
            previewCard.Controls.Add(lblPrevSub);
            previewCard.Controls.Add(_lblSamplePreviewSubject);
            previewCard.Controls.Add(div);
            previewCard.Controls.Add(_lblSamplePreviewBody);
            previewCard.Controls.Add(_btnSamplePreviewCta);
            previewCard.Controls.Add(_lblSamplePreviewSignature);

            _pnlLiveSamplePreview.Controls.Add(previewCard);
            splitTable.Controls.Add(_pnlLiveSamplePreview, 1, 0);

            _tabValuation.Controls.Add(splitTable);
            _tabValuation.Controls.Add(banner);

            banner.SendToBack();
            splitTable.BringToFront();

            UpdateLiveSamplePreview();
        }

        private void UpdateLiveSamplePreview()
        {
            if (_lblSamplePreviewSubject == null || _lblSamplePreviewBody == null) return;

            decimal rate = _numAppreciation != null ? _numAppreciation.Value : 5.0m;
            string brokerage = !string.IsNullOrWhiteSpace(_txtBrokerageName?.Text) ? _txtBrokerageName.Text.Trim() : "NEXA Real Estate Advisory";
            string cta = !string.IsNullOrWhiteSpace(_txtCtaText?.Text) ? _txtCtaText.Text.Trim() : "Schedule Annual Home Check-up";
            string subjTemplate = !string.IsNullOrWhiteSpace(_txtSubjectTemplate?.Text) ? _txtSubjectTemplate.Text.Trim() : "Market Valuation & Equity Report for {PropertyAddress}";

            decimal origPrice = 18500000m;
            double years = 2.0;
            decimal multiplier = (decimal)Math.Pow((double)(1m + (rate / 100m)), years);
            decimal estValue = Math.Round(origPrice * multiplier, 0);
            decimal equityGain = estValue - origPrice;
            decimal gainPct = Math.Round((equityGain / origPrice) * 100m, 1);

            string sampleSubject = subjTemplate
                .Replace("{PropertyAddress}", "Unit 1204, One Serendra, BGC")
                .Replace("{YearsOwned}", years.ToString("F0"))
                .Replace("{CustomerName}", "Maria Santos");

            _lblSamplePreviewSubject.Text = $"Subject: {sampleSubject}";

            _lblSamplePreviewBody.Text =
                $"Dear Maria Santos,\n\n" +
                $"As part of our continuous client care service at {brokerage}, we track local market sales and property appreciation.\n\n" +
                $"• Property: Unit 1204, One Serendra, BGC (Condominium)\n" +
                $"• Acquisition Price: ₱{origPrice:N0}\n" +
                $"• Estimated Current Market Value: ₱{estValue:N0} (+{rate:F1}% annual benchmark)\n" +
                $"• Net Accumulated Equity: +₱{equityGain:N0} (+{gainPct:F1}% total growth)\n\n" +
                $"If you have questions regarding this valuation or wish to explore a confidential market consultation, feel free to reply or click below:";

            _btnSamplePreviewCta.Text = cta;
            _lblSamplePreviewSignature.Text = $"Warm regards,\nTenant C Agent\nLicensed Real Estate Advisor\n{brokerage}";
        }

        private async Task LoadValuationSettingsDataAsync()
        {
            using var db = LocalDb.CreateContext(CurrentSession.TenantId);
            _currentValuationSettings = await db.AutomatedEmailSettings.FirstOrDefaultAsync(s => s.TenantId == CurrentSession.TenantId)
                ?? new AutomatedEmailSettings { TenantId = CurrentSession.TenantId };

            _chkEnableAutomation.Checked = _currentValuationSettings.IsEnabled;
            _numAppreciation.Value = Math.Max(0.1m, Math.Min(30m, _currentValuationSettings.AnnualAppreciationRatePercent));
            _txtBrokerageName.Text = _currentValuationSettings.BrokerageName;
            _txtCtaText.Text = _currentValuationSettings.CallToActionText;
            _txtSubjectTemplate.Text = _currentValuationSettings.SubjectTemplate;

            int freqIdx = _currentValuationSettings.FrequencyDays switch
            {
                90 => 0,
                180 => 1,
                365 => 2,
                _ => 1
            };
            if (freqIdx >= 0 && freqIdx < _cboFrequency.Items.Count) _cboFrequency.SelectedIndex = freqIdx;

            int audIdx = _currentValuationSettings.TargetAudience.ToLowerInvariant() switch
            {
                "buyers" => 1,
                "sellers" => 2,
                _ => 0
            };
            if (audIdx >= 0 && audIdx < _cboAudience.Items.Count) _cboAudience.SelectedIndex = audIdx;

            UpdateLiveSamplePreview();
        }

        private async Task OnSaveValuationSettingsAsync()
        {
            if (!RbacService.HasFullOversight && !RbacService.IsAdmin)
            {
                MessageBox.Show("Only Administrators and Managers are authorized to edit valuation benchmark settings.", "Permission Denied", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            try
            {
                using var db = LocalDb.CreateContext(CurrentSession.TenantId);
                var settings = await db.AutomatedEmailSettings.FirstOrDefaultAsync(s => s.TenantId == CurrentSession.TenantId);
                if (settings == null)
                {
                    settings = new AutomatedEmailSettings { TenantId = CurrentSession.TenantId };
                    db.AutomatedEmailSettings.Add(settings);
                }

                settings.IsEnabled = _chkEnableAutomation.Checked;
                settings.AnnualAppreciationRatePercent = _numAppreciation.Value;
                settings.FrequencyDays = _cboFrequency.SelectedIndex switch
                {
                    0 => 90,
                    1 => 180,
                    2 => 365,
                    _ => 180
                };
                settings.TargetAudience = _cboAudience.SelectedIndex switch
                {
                    1 => "Buyers",
                    2 => "Sellers",
                    _ => "All"
                };
                settings.EmailFormat = _cboEmailFormat.SelectedIndex == 1 ? "PlainText" : "Html";
                settings.BrokerageName = _txtBrokerageName.Text.Trim();
                settings.CallToActionText = _txtCtaText.Text.Trim();
                settings.SubjectTemplate = _txtSubjectTemplate.Text.Trim();
                settings.UpdatedAt = DateTime.UtcNow;

                await db.SaveChangesAsync();
                MessageBox.Show("Valuation benchmark settings updated successfully.", "Saved", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to save settings: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private async Task OnRunScanAndGoToQueueAsync()
        {
            await OnSaveValuationSettingsAsync();

            _btnRunValuationScanNow.Enabled = false;
            _btnRunValuationScanNow.Text = "Scanning...";

            try
            {
                int generated = await _retentionController.GenerateAutomatedValuationCampaignsAsync(forceAll: true);
                MessageBox.Show(
                    $"Scan Complete: {generated} automated retention campaigns generated.\nNavigating to Campaign Queue for review and dispatch.",
                    "Success",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information);

                SwitchTab(3); // Navigate to Campaign Queue!
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to run valuation scan: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                _btnRunValuationScanNow.Enabled = true;
                _btnRunValuationScanNow.Text = "⚡ Run Campaign Scan Now";
            }
        }

        private async Task OnSendAutomationTestAsync()
        {
            string userEmail = CurrentSession.CurrentUser?.Email ?? string.Empty;
            if (string.IsNullOrWhiteSpace(userEmail))
            {
                MessageBox.Show("Your user profile does not have an email address configured for test delivery.", "Validation Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            var confirm = MessageBox.Show(
                $"Send a sample automated equity valuation email to {userEmail}?",
                "Send Sample Test",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Question);

            if (confirm != DialogResult.Yes) return;

            try
            {
                _btnSendAutomationTest.Enabled = false;
                var res = await ContactEmailService.SendAsync(
                    userEmail,
                    "[SAMPLE VALUATION] " + _lblSamplePreviewSubject.Text.Replace("Subject: ", ""),
                    _lblSamplePreviewBody.Text + "\n\n" + _lblSamplePreviewSignature.Text,
                    isBodyHtml: true);

                if (res.Success)
                {
                    MessageBox.Show($"Sample valuation email delivered successfully to {userEmail}.", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
                else
                {
                    MessageBox.Show($"Sample test email failed: {res.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Test delivery failed: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                _btnSendAutomationTest.Enabled = true;
            }
        }

        #endregion

        #region Tab 2: Email Templates Library

        private void BuildTab2Templates()
        {
            _tabTemplates = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Theme.Background,
                Padding = new Padding(28, 14, 28, 14),
                AutoScroll = false
            };

            var banner = CreateGuidanceBanner(
                "📝 Retention & Automation Email Templates Library",
                "View, customize, create, and archive retention messaging templates across your client lifecycle. Fiduciary real estate advisory only — retail coupons and sales discounts are strictly excluded.",
                "⚡ Valuation Engine",
                () => SwitchTab(1));

            var splitTemplates = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 2,
                RowCount = 1,
                BackColor = Color.Transparent,
                Margin = new Padding(0, 10, 0, 0)
            };
            splitTemplates.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 320F));
            splitTemplates.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            splitTemplates.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));

            // Left Navigation List of Templates
            var leftListCard = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.White,
                Padding = new Padding(14),
                Margin = new Padding(0, 0, 10, 0)
            };
            UiRadiusHelper.StyleCard(leftListCard, 10);

            var pnlLeftTop = new Panel
            {
                Dock = DockStyle.Top,
                Height = 120,
                BackColor = Color.Transparent
            };

            var lblCatalogTitle = new Label
            {
                Text = "Email Templates",
                Font = new Font("Segoe UI", 11.5f, FontStyle.Bold),
                ForeColor = Color.FromArgb(15, 23, 42),
                Location = new Point(2, 4),
                AutoSize = true,
                UseMnemonic = false
            };

            _btnCreateNewTemplate = new Button
            {
                Text = "➕ New Template",
                Location = new Point(148, 2),
                Size = new Size(138, 32),
                BackColor = Theme.SidebarAccent,
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 8.5f, FontStyle.Bold),
                Cursor = Cursors.Hand,
                Visible = !RbacService.IsAgent && (RbacService.HasFullOversight || RbacService.IsAdmin)
            };
            _btnCreateNewTemplate.FlatAppearance.BorderSize = 0;
            UiRadiusHelper.StyleButton(_btnCreateNewTemplate, 6);
            _btnCreateNewTemplate.Click += (s, e) => StartCreateNewTemplate();

            var lblCatalogSub = new Label
            {
                Text = "Manage lifecycle & valuation templates",
                Font = new Font("Segoe UI", 8.5f),
                ForeColor = Color.FromArgb(100, 116, 139),
                Location = new Point(2, 32),
                AutoSize = true,
                UseMnemonic = false
            };

            // Filter Pills: Active | Archived | All
            _flpTemplatePills = new FlowLayoutPanel
            {
                Location = new Point(0, 56),
                Size = new Size(290, 30),
                FlowDirection = FlowDirection.LeftToRight,
                WrapContents = false
            };

            _btnTplFilterActive = CreateTplPillButton("Active", true);
            _btnTplFilterArchived = CreateTplPillButton("Archived", false);
            _btnTplFilterAll = CreateTplPillButton("All", false);

            _btnTplFilterActive.Click += (s, e) => SetTemplateFilter("Active");
            _btnTplFilterArchived.Click += (s, e) => SetTemplateFilter("Archived");
            _btnTplFilterAll.Click += (s, e) => SetTemplateFilter("All");

            _flpTemplatePills.Controls.Add(_btnTplFilterActive);
            _flpTemplatePills.Controls.Add(_btnTplFilterArchived);
            _flpTemplatePills.Controls.Add(_btnTplFilterAll);

            _txtSearchTemplates = new TextBox
            {
                Location = new Point(2, 90),
                Width = 286,
                Font = new Font("Segoe UI", 9f),
                PlaceholderText = "Search templates..."
            };
            _txtSearchTemplates.TextChanged += (s, e) => ApplyTemplateFilterList();

            pnlLeftTop.Controls.Add(lblCatalogTitle);
            pnlLeftTop.Controls.Add(_btnCreateNewTemplate);
            pnlLeftTop.Controls.Add(lblCatalogSub);
            pnlLeftTop.Controls.Add(_flpTemplatePills);
            pnlLeftTop.Controls.Add(_txtSearchTemplates);

            _flpTemplatesNav = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                FlowDirection = FlowDirection.TopDown,
                WrapContents = false,
                AutoScroll = true,
                Padding = new Padding(0, 6, 0, 0)
            };

            leftListCard.Controls.Add(_flpTemplatesNav);
            leftListCard.Controls.Add(pnlLeftTop);
            pnlLeftTop.BringToFront();
            _flpTemplatesNav.BringToFront();

            splitTemplates.Controls.Add(leftListCard, 0, 0);

            // Right Panel: Editor & Live Preview (50/50 split)
            var rightEditorCard = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.White,
                Padding = new Padding(18),
                Margin = new Padding(0)
            };
            UiRadiusHelper.StyleCard(rightEditorCard, 10);

            var innerSplit = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 2,
                RowCount = 1,
                BackColor = Color.Transparent
            };
            innerSplit.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 52F));
            innerSplit.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 48F));
            innerSplit.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));

            // Editor Controls on Left of inner split
            var pnlEditForm = new Panel
            {
                Dock = DockStyle.Fill,
                Padding = new Padding(6),
                AutoScroll = true
            };

            _lblTplEditorHeader = new Label
            {
                Text = "Template Editor",
                Font = new Font("Segoe UI", 12f, FontStyle.Bold),
                ForeColor = Color.FromArgb(15, 23, 42),
                Location = new Point(6, 6),
                AutoSize = true,
                UseMnemonic = false
            };

            _lblTplAudienceBadge = new Label
            {
                Text = "Audience: All Clients  •  Category: Retention",
                Font = new Font("Segoe UI", 8.5f),
                ForeColor = Theme.SidebarAccent,
                Location = new Point(6, 30),
                AutoSize = true,
                UseMnemonic = false
            };

            int curY = 56;

            _lblTplNameLabel = new Label { Text = "Template Name *", Font = new Font("Segoe UI", 9f, FontStyle.Bold), Location = new Point(6, curY), AutoSize = true, UseMnemonic = false };
            _txtTplName = new TextBox
            {
                Location = new Point(6, curY + 20),
                Width = 420,
                Font = new Font("Segoe UI", 9.5f)
            };
            _txtTplName.TextChanged += (s, e) => {
                if (_isCreatingNewTemplate) UpdateTemplateLivePreview();
            };

            curY += 50;

            _lblTplAudienceLabel = new Label { Text = "Target Audience *", Font = new Font("Segoe UI", 9f, FontStyle.Bold), Location = new Point(6, curY), AutoSize = true, UseMnemonic = false };
            _cboTplAudience = new ComboBox
            {
                Location = new Point(6, curY + 20),
                Width = 420,
                DropDownStyle = ComboBoxStyle.DropDownList,
                Font = new Font("Segoe UI", 9.5f)
            };
            _cboTplAudience.Items.AddRange(new object[] { "All Past Clients", "Buyers Only", "Sellers Only", "Investors", "VIP Homeowners" });
            _cboTplAudience.SelectedIndex = 0;

            curY += 50;

            var lblTplSubjLabel = new Label { Text = "Email Subject Template *", Font = new Font("Segoe UI", 9f, FontStyle.Bold), Location = new Point(6, curY), AutoSize = true, UseMnemonic = false };
            _txtTplSubject = new TextBox
            {
                Location = new Point(6, curY + 20),
                Width = 420,
                Font = new Font("Segoe UI", 9.5f)
            };
            _txtTplSubject.TextChanged += (s, e) => UpdateTemplateLivePreview();

            curY += 50;

            var lblTplCtaLabel = new Label { Text = "Call to Action Button Text *", Font = new Font("Segoe UI", 9f, FontStyle.Bold), Location = new Point(6, curY), AutoSize = true, UseMnemonic = false };
            _txtTplCta = new TextBox
            {
                Location = new Point(6, curY + 20),
                Width = 420,
                Font = new Font("Segoe UI", 9.5f)
            };
            _txtTplCta.TextChanged += (s, e) => UpdateTemplateLivePreview();

            curY += 50;

            var lblTplBodyLabel = new Label { Text = "Email Body Message * (Supports {CustomerName}, {PropertyAddress}, {AgentName})", Font = new Font("Segoe UI", 9f, FontStyle.Bold), Location = new Point(6, curY), AutoSize = true, UseMnemonic = false };
            _txtTplBody = new TextBox
            {
                Location = new Point(6, curY + 20),
                Width = 420,
                Height = 180,
                Multiline = true,
                ScrollBars = ScrollBars.Vertical,
                Font = new Font("Segoe UI", 9.5f)
            };
            _txtTplBody.TextChanged += (s, e) => UpdateTemplateLivePreview();

            curY += 210;

            // Action Buttons Panel
            var pnlTplButtons = new FlowLayoutPanel
            {
                Location = new Point(6, curY),
                Size = new Size(430, 44),
                FlowDirection = FlowDirection.LeftToRight,
                WrapContents = false
            };

            // Edit Mode Buttons
            _btnSaveTemplateChanges = new Button
            {
                Text = "💾 Save Changes",
                Size = new Size(130, 36),
                BackColor = Theme.SidebarAccent,
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 9f, FontStyle.Bold),
                Cursor = Cursors.Hand,
                Margin = new Padding(0, 0, 8, 0)
            };
            _btnSaveTemplateChanges.FlatAppearance.BorderSize = 0;
            UiRadiusHelper.StyleButton(_btnSaveTemplateChanges, 6);
            _btnSaveTemplateChanges.Click += async (s, e) => await OnSaveTemplateChangesAsync();

            _btnSendTemplateTest = new Button
            {
                Text = "✉ Test Email",
                Size = new Size(105, 36),
                BackColor = Color.White,
                ForeColor = Color.FromArgb(71, 85, 105),
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 9f),
                Cursor = Cursors.Hand,
                Margin = new Padding(0, 0, 8, 0)
            };
            _btnSendTemplateTest.FlatAppearance.BorderColor = Color.FromArgb(226, 232, 240);
            UiRadiusHelper.StyleButton(_btnSendTemplateTest, 6);
            _btnSendTemplateTest.Click += async (s, e) => await OnSendTemplateTestEmailAsync();

            _btnArchiveTemplate = new Button
            {
                Text = "📦 Archive",
                Size = new Size(95, 36),
                BackColor = Color.White,
                ForeColor = Color.FromArgb(217, 119, 6),
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 8.5f, FontStyle.Bold),
                Cursor = Cursors.Hand,
                Margin = new Padding(0, 0, 8, 0)
            };
            _btnArchiveTemplate.FlatAppearance.BorderColor = Color.FromArgb(253, 230, 138);
            UiRadiusHelper.StyleButton(_btnArchiveTemplate, 6);
            _btnArchiveTemplate.Click += async (s, e) => await OnToggleArchiveSelectedTemplateAsync();

            _btnDeleteTemplate = new Button
            {
                Text = "🗑 Delete",
                Size = new Size(75, 36),
                BackColor = Color.White,
                ForeColor = Color.FromArgb(220, 38, 38),
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 8.5f),
                Cursor = Cursors.Hand,
                Margin = new Padding(0)
            };
            _btnDeleteTemplate.FlatAppearance.BorderColor = Color.FromArgb(254, 202, 202);
            UiRadiusHelper.StyleButton(_btnDeleteTemplate, 6);
            _btnDeleteTemplate.Click += async (s, e) => await OnDeleteSelectedTemplateAsync();

            // Create Mode Buttons
            _btnCreateTemplateSubmit = new Button
            {
                Text = "✔ Create Template",
                Size = new Size(145, 36),
                BackColor = Theme.SidebarAccent,
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 9f, FontStyle.Bold),
                Cursor = Cursors.Hand,
                Visible = false,
                Margin = new Padding(0, 0, 8, 0)
            };
            _btnCreateTemplateSubmit.FlatAppearance.BorderSize = 0;
            UiRadiusHelper.StyleButton(_btnCreateTemplateSubmit, 6);
            _btnCreateTemplateSubmit.Click += async (s, e) => await OnCreateTemplateSubmitAsync();

            _btnCancelCreateTemplate = new Button
            {
                Text = "✖ Cancel",
                Size = new Size(90, 36),
                BackColor = Color.White,
                ForeColor = Color.FromArgb(100, 116, 139),
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 9f),
                Cursor = Cursors.Hand,
                Visible = false,
                Margin = new Padding(0)
            };
            _btnCancelCreateTemplate.FlatAppearance.BorderColor = Color.FromArgb(226, 232, 240);
            UiRadiusHelper.StyleButton(_btnCancelCreateTemplate, 6);
            _btnCancelCreateTemplate.Click += (s, e) => CancelCreateNewTemplate();

            pnlTplButtons.Controls.Add(_btnSaveTemplateChanges);
            pnlTplButtons.Controls.Add(_btnSendTemplateTest);
            pnlTplButtons.Controls.Add(_btnArchiveTemplate);
            pnlTplButtons.Controls.Add(_btnDeleteTemplate);
            pnlTplButtons.Controls.Add(_btnCreateTemplateSubmit);
            pnlTplButtons.Controls.Add(_btnCancelCreateTemplate);

            pnlEditForm.Controls.Add(_lblTplEditorHeader);
            pnlEditForm.Controls.Add(_lblTplAudienceBadge);
            pnlEditForm.Controls.Add(_lblTplNameLabel);
            pnlEditForm.Controls.Add(_txtTplName);
            pnlEditForm.Controls.Add(_lblTplAudienceLabel);
            pnlEditForm.Controls.Add(_cboTplAudience);
            pnlEditForm.Controls.Add(lblTplSubjLabel);
            pnlEditForm.Controls.Add(_txtTplSubject);
            pnlEditForm.Controls.Add(lblTplCtaLabel);
            pnlEditForm.Controls.Add(_txtTplCta);
            pnlEditForm.Controls.Add(lblTplBodyLabel);
            pnlEditForm.Controls.Add(_txtTplBody);
            pnlEditForm.Controls.Add(pnlTplButtons);
            innerSplit.Controls.Add(pnlEditForm, 0, 0);

            // Right side of inner split: Live Rendered Preview
            var pnlTplPreviewCard = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.FromArgb(248, 250, 252),
                Padding = new Padding(18),
                Margin = new Padding(10, 6, 6, 6),
                AutoScroll = true
            };
            UiRadiusHelper.StyleCard(pnlTplPreviewCard, 8);

            var lblPrevHeader = new Label
            {
                Text = "Live Template Preview",
                Font = new Font("Segoe UI", 11f, FontStyle.Bold),
                ForeColor = Color.FromArgb(15, 23, 42),
                Location = new Point(12, 10),
                AutoSize = true,
                UseMnemonic = false
            };

            _lblTplPreviewSubject = new Label
            {
                Text = "Subject: ...",
                Font = new Font("Segoe UI", 10f, FontStyle.Bold),
                ForeColor = Theme.SidebarAccent,
                Location = new Point(12, 38),
                Size = new Size(380, 24),
                UseMnemonic = false
            };

            var divLine = new Panel
            {
                Location = new Point(12, 68),
                Size = new Size(380, 1),
                BackColor = Color.FromArgb(226, 232, 240)
            };

            _lblTplPreviewBody = new Label
            {
                Font = new Font("Segoe UI", 9.5f),
                ForeColor = Color.FromArgb(30, 41, 59),
                Location = new Point(12, 80),
                Size = new Size(380, 260),
                UseMnemonic = false
            };

            _btnTplPreviewCta = new Button
            {
                Text = "Action Button",
                Location = new Point(12, 350),
                Size = new Size(260, 36),
                BackColor = Theme.SidebarAccent,
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 9f, FontStyle.Bold),
                Cursor = Cursors.Hand,
                UseMnemonic = false
            };
            _btnTplPreviewCta.FlatAppearance.BorderSize = 0;
            UiRadiusHelper.StyleButton(_btnTplPreviewCta, 6);

            _lblTplPreviewSignature = new Label
            {
                Text = "Warm regards,\nAlthea Garcia | Licensed Advisor\nNEXA Real Estate Advisory",
                Font = new Font("Segoe UI", 9f),
                ForeColor = Color.FromArgb(100, 116, 139),
                Location = new Point(12, 400),
                Size = new Size(380, 60),
                UseMnemonic = false
            };

            pnlTplPreviewCard.Controls.Add(lblPrevHeader);
            pnlTplPreviewCard.Controls.Add(_lblTplPreviewSubject);
            pnlTplPreviewCard.Controls.Add(divLine);
            pnlTplPreviewCard.Controls.Add(_lblTplPreviewBody);
            pnlTplPreviewCard.Controls.Add(_btnTplPreviewCta);
            pnlTplPreviewCard.Controls.Add(_lblTplPreviewSignature);
            innerSplit.Controls.Add(pnlTplPreviewCard, 1, 0);

            rightEditorCard.Controls.Add(innerSplit);
            splitTemplates.Controls.Add(rightEditorCard, 1, 0);

            _tabTemplates.Controls.Add(splitTemplates);
            _tabTemplates.Controls.Add(banner);
            banner.SendToBack();
            splitTemplates.BringToFront();
        }

        private Button CreateTplPillButton(string text, bool isActive)
        {
            var btn = new Button
            {
                Text = text,
                Height = 28,
                AutoSize = true,
                BackColor = isActive ? Theme.SidebarAccent : Color.FromArgb(241, 245, 249),
                ForeColor = isActive ? Color.White : Color.FromArgb(71, 85, 105),
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 8f, isActive ? FontStyle.Bold : FontStyle.Regular),
                Cursor = Cursors.Hand,
                Margin = new Padding(0, 0, 4, 0),
                UseMnemonic = false
            };
            btn.FlatAppearance.BorderSize = 0;
            UiRadiusHelper.StyleButton(btn, 14);
            return btn;
        }

        private void SetTemplateFilter(string filter)
        {
            _selectedTplFilter = filter;

            void UpdatePill(Button btn, bool active)
            {
                btn.BackColor = active ? Theme.SidebarAccent : Color.FromArgb(241, 245, 249);
                btn.ForeColor = active ? Color.White : Color.FromArgb(71, 85, 105);
                btn.Font = new Font("Segoe UI", 8f, active ? FontStyle.Bold : FontStyle.Regular);
            }

            UpdatePill(_btnTplFilterActive, filter == "Active");
            UpdatePill(_btnTplFilterArchived, filter == "Archived");
            UpdatePill(_btnTplFilterAll, filter == "All");

            ApplyTemplateFilterList();
        }

        private async Task LoadTemplatesLibraryDataAsync()
        {
            _allTemplateModels.Clear();

            using var db = LocalDb.CreateContext(CurrentSession.TenantId);

            // 1. Automated Home Equity Valuation Template (from settings)
            var settings = await db.AutomatedEmailSettings.FirstOrDefaultAsync(s => s.TenantId == CurrentSession.TenantId)
                ?? new AutomatedEmailSettings { TenantId = CurrentSession.TenantId };

            _allTemplateModels.Add(new TemplateItemModel
            {
                TemplateId = -1,
                Name = "⚡ Automated Home Equity & Valuation",
                Category = "Automated Valuation",
                TargetAudience = settings.TargetAudience,
                Subject = settings.SubjectTemplate,
                Body = settings.BodyTemplate,
                CallToActionText = settings.CallToActionText,
                IsAutomatedValuation = true,
                IsActive = settings.IsEnabled,
                IsSystem = true
            });

            // 2. All stored templates (active & archived, non-deleted)
            var dbTemplates = await db.EmailTemplates
                .AsNoTracking()
                .Where(t => !t.IsDeleted)
                .OrderBy(t => t.IsActive ? 0 : 1)
                .ThenBy(t => t.Name)
                .ToListAsync();

            foreach (var t in dbTemplates)
            {
                _allTemplateModels.Add(new TemplateItemModel
                {
                    TemplateId = t.TemplateId,
                    Name = t.Name,
                    Category = t.Category ?? "Retention",
                    TargetAudience = t.TargetAudience ?? "All",
                    Subject = t.Subject,
                    Body = t.Body,
                    CallToActionText = t.CallToActionText ?? "Schedule Consultation",
                    IsAutomatedValuation = false,
                    IsActive = t.IsActive,
                    IsSystem = t.IsSystem
                });
            }

            // Update count indicators on filter pills
            int activeCount = _allTemplateModels.Count(m => m.IsActive);
            int archivedCount = _allTemplateModels.Count(m => !m.IsActive);
            int totalCount = _allTemplateModels.Count;

            _btnTplFilterActive.Text = $"Active ({activeCount})";
            _btnTplFilterArchived.Text = $"Archived ({archivedCount})";
            _btnTplFilterAll.Text = $"All ({totalCount})";

            ApplyTemplateFilterList();
        }

        private void ApplyTemplateFilterList()
        {
            _flpTemplatesNav.Controls.Clear();

            string search = _txtSearchTemplates?.Text.Trim().ToLowerInvariant() ?? string.Empty;

            var filtered = _allTemplateModels.AsEnumerable();

            if (_selectedTplFilter == "Active")
            {
                filtered = filtered.Where(m => m.IsActive);
            }
            else if (_selectedTplFilter == "Archived")
            {
                filtered = filtered.Where(m => !m.IsActive);
            }

            if (!string.IsNullOrWhiteSpace(search))
            {
                filtered = filtered.Where(m =>
                    m.Name.ToLowerInvariant().Contains(search) ||
                    m.TargetAudience.ToLowerInvariant().Contains(search) ||
                    m.Subject.ToLowerInvariant().Contains(search));
            }

            var visibleList = filtered.ToList();

            if (visibleList.Count == 0)
            {
                var lblEmpty = new Label
                {
                    Text = _selectedTplFilter == "Archived"
                        ? "No archived templates.\nTemplates you archive will appear here."
                        : "No templates match your search.",
                    Font = new Font("Segoe UI", 9f),
                    ForeColor = Color.FromArgb(148, 163, 184),
                    TextAlign = ContentAlignment.MiddleCenter,
                    Size = new Size(270, 80)
                };
                _flpTemplatesNav.Controls.Add(lblEmpty);
                return;
            }

            for (int i = 0; i < visibleList.Count; i++)
            {
                var model = visibleList[i];
                var card = new Panel
                {
                    Size = new Size(286, 54),
                    BackColor = _selectedTemplateModel?.TemplateId == model.TemplateId ? Color.FromArgb(240, 249, 255) : Color.White,
                    Cursor = Cursors.Hand,
                    Margin = new Padding(0, 0, 0, 6)
                };
                UiRadiusHelper.StyleCard(card, 6);

                var lblTitle = new Label
                {
                    Text = model.Name,
                    Font = new Font("Segoe UI", 8.5f, _selectedTemplateModel?.TemplateId == model.TemplateId ? FontStyle.Bold : FontStyle.Regular),
                    ForeColor = _selectedTemplateModel?.TemplateId == model.TemplateId ? Theme.SidebarAccent : Color.FromArgb(15, 23, 42),
                    Location = new Point(8, 6),
                    Size = new Size(270, 18),
                    AutoEllipsis = true,
                    UseMnemonic = false,
                    Cursor = Cursors.Hand
                };

                string tagText = !model.IsActive ? "📦 Archived" : (model.IsAutomatedValuation ? "⚡ Valuation" : (model.IsSystem ? "Standard" : "Custom"));
                Color tagColor = !model.IsActive ? Color.FromArgb(217, 119, 6) : (model.IsAutomatedValuation ? Color.FromArgb(2, 132, 199) : Color.FromArgb(100, 116, 139));

                var lblSub = new Label
                {
                    Text = $"{tagText} • Audience: {model.TargetAudience}",
                    Font = new Font("Segoe UI", 7.5f),
                    ForeColor = tagColor,
                    Location = new Point(8, 28),
                    Size = new Size(270, 16),
                    AutoEllipsis = true,
                    UseMnemonic = false,
                    Cursor = Cursors.Hand
                };

                void OnCardClicked()
                {
                    _selectedTemplateModel = model;
                    SelectTemplateModel(model);
                }

                card.Click += (s, e) => OnCardClicked();
                lblTitle.Click += (s, e) => OnCardClicked();
                lblSub.Click += (s, e) => OnCardClicked();

                card.Controls.Add(lblTitle);
                card.Controls.Add(lblSub);
                _flpTemplatesNav.Controls.Add(card);
            }

            if (_selectedTemplateModel == null && visibleList.Count > 0)
            {
                SelectTemplateModel(visibleList[0]);
            }
            else if (_selectedTemplateModel != null)
            {
                var matching = visibleList.FirstOrDefault(m => m.TemplateId == _selectedTemplateModel.TemplateId);
                if (matching != null)
                {
                    SelectTemplateModel(matching);
                }
                else if (visibleList.Count > 0)
                {
                    SelectTemplateModel(visibleList[0]);
                }
            }
        }

        private void SelectTemplateModel(TemplateItemModel model)
        {
            _selectedTemplateModel = model;
            _isCreatingNewTemplate = false;

            // Highlight in list
            foreach (Control c in _flpTemplatesNav.Controls)
            {
                if (c is Panel p && p.Controls.Count >= 2 && p.Controls[0] is Label l)
                {
                    bool isSel = l.Text == model.Name;
                    p.BackColor = isSel ? Color.FromArgb(240, 249, 255) : Color.White;
                    l.ForeColor = isSel ? Theme.SidebarAccent : Color.FromArgb(15, 23, 42);
                    l.Font = new Font("Segoe UI", 8.5f, isSel ? FontStyle.Bold : FontStyle.Regular);
                }
            }

            _lblTplEditorHeader.Text = model.Name;
            string statusDesc = model.IsActive ? "Active" : "Archived";
            string typeDesc = model.IsSystem ? "System Standard" : "Custom Template";
            _lblTplAudienceBadge.Text = $"Audience: {model.TargetAudience}  •  Status: {statusDesc}  •  Type: {typeDesc}";
            _lblTplAudienceBadge.ForeColor = model.IsActive ? Theme.SidebarAccent : Color.FromArgb(217, 119, 6);

            _txtTplName.Text = model.Name;
            _txtTplName.ReadOnly = model.IsAutomatedValuation;
            _txtTplName.BackColor = model.IsAutomatedValuation ? Color.FromArgb(248, 250, 252) : Color.White;

            // Select audience in dropdown
            string audLower = model.TargetAudience.ToLowerInvariant();
            if (audLower.Contains("buyer")) _cboTplAudience.SelectedIndex = 1;
            else if (audLower.Contains("seller")) _cboTplAudience.SelectedIndex = 2;
            else if (audLower.Contains("investor")) _cboTplAudience.SelectedIndex = 3;
            else if (audLower.Contains("vip") || audLower.Contains("homeowner")) _cboTplAudience.SelectedIndex = 4;
            else _cboTplAudience.SelectedIndex = 0;

            _txtTplSubject.Text = model.Subject;
            _txtTplCta.Text = model.CallToActionText;

            string normalizedBody = (model.Body ?? string.Empty)
                .Replace("\r\n", "\n")
                .Replace("\n", "\r\n");

            _txtTplBody.Text = normalizedBody;

            // Configure button visibility & state
            _btnSaveTemplateChanges.Visible = true;
            _btnSendTemplateTest.Visible = true;

            // Archive button:
            if (model.IsAutomatedValuation || RbacService.IsAgent || (!RbacService.HasFullOversight && !RbacService.IsAdmin))
            {
                _btnArchiveTemplate.Visible = false;
                _btnDeleteTemplate.Visible = false;
            }
            else
            {
                _btnArchiveTemplate.Visible = true;
                _btnArchiveTemplate.Text = model.IsActive ? "📦 Archive" : "↩ Unarchive";
                _btnArchiveTemplate.ForeColor = model.IsActive ? Color.FromArgb(217, 119, 6) : Color.FromArgb(22, 163, 74);
                _btnArchiveTemplate.FlatAppearance.BorderColor = model.IsActive ? Color.FromArgb(253, 230, 138) : Color.FromArgb(187, 247, 208);

                _btnDeleteTemplate.Visible = !model.IsSystem;
            }

            _btnCreateTemplateSubmit.Visible = false;
            _btnCancelCreateTemplate.Visible = false;

            UpdateTemplateLivePreview();
        }

        private void StartCreateNewTemplate()
        {
            if (!RbacService.HasFullOversight && !RbacService.IsAdmin)
            {
                MessageBox.Show("Only Administrators and Managers are authorized to create new email templates.", "Permission Denied", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            _isCreatingNewTemplate = true;
            _selectedTemplateModel = null;

            _lblTplEditorHeader.Text = "➕ Create New Retention Template";
            _lblTplAudienceBadge.Text = "Drafting New Custom Template • Fiduciary Real Estate Advisory";
            _lblTplAudienceBadge.ForeColor = Theme.SidebarAccent;

            _txtTplName.ReadOnly = false;
            _txtTplName.BackColor = Color.White;
            _txtTplName.Text = "Annual Asset Appreciation & Advisory Review";

            _cboTplAudience.SelectedIndex = 0;
            _txtTplSubject.Text = "CONFIDENTIAL: Annual Property Equity & Valuation Update for {PropertyAddress}";
            _txtTplCta.Text = "Schedule Confidential Review";
            _txtTplBody.Text =
                "Dear {CustomerName},\r\n\r\n" +
                "As part of our continuous fiduciary client care, we monitor neighborhood transactions and equity appreciation for your property at {PropertyAddress}.\r\n\r\n" +
                "Over the past year, your property has benefited from steady capital appreciation. We have prepared an updated comparative valuation summary for your review.\r\n\r\n" +
                "Should you wish to review your property equity report in detail or discuss broader portfolio options, please connect with us at your earliest convenience.\r\n\r\n" +
                "Warm regards,\r\n{AgentName}\r\nLicensed Real Estate Advisor\r\nNEXA Real Estate Advisory";

            // Switch buttons
            _btnSaveTemplateChanges.Visible = false;
            _btnSendTemplateTest.Visible = false;
            _btnArchiveTemplate.Visible = false;
            _btnDeleteTemplate.Visible = false;

            _btnCreateTemplateSubmit.Visible = true;
            _btnCancelCreateTemplate.Visible = true;

            // Deselect list cards
            foreach (Control c in _flpTemplatesNav.Controls)
            {
                if (c is Panel p && p.Controls.Count >= 2 && p.Controls[0] is Label l)
                {
                    p.BackColor = Color.White;
                    l.ForeColor = Color.FromArgb(15, 23, 42);
                    l.Font = new Font("Segoe UI", 8.5f, FontStyle.Regular);
                }
            }

            UpdateTemplateLivePreview();
        }

        private void CancelCreateNewTemplate()
        {
            _isCreatingNewTemplate = false;
            if (_allTemplateModels.Count > 0)
            {
                SelectTemplateModel(_allTemplateModels[0]);
            }
        }

        private async Task OnCreateTemplateSubmitAsync()
        {
            string name = _txtTplName.Text.Trim();
            string subject = _txtTplSubject.Text.Trim();
            string cta = _txtTplCta.Text.Trim();
            string body = _txtTplBody.Text.Trim();
            string audience = _cboTplAudience.SelectedItem?.ToString() ?? "All Past Clients";

            if (string.IsNullOrWhiteSpace(name))
            {
                MessageBox.Show("Please enter a template name.", "Validation Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                _txtTplName.Focus();
                return;
            }

            if (string.IsNullOrWhiteSpace(subject))
            {
                MessageBox.Show("Please enter an email subject line.", "Validation Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                _txtTplSubject.Focus();
                return;
            }

            if (string.IsNullOrWhiteSpace(body))
            {
                MessageBox.Show("Please enter the email body message.", "Validation Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                _txtTplBody.Focus();
                return;
            }

            try
            {
                using var db = LocalDb.CreateContext(CurrentSession.TenantId);
                var newTpl = new EmailTemplate
                {
                    TenantId = CurrentSession.TenantId,
                    Name = name,
                    Category = "Retention",
                    TargetAudience = audience,
                    EmailFormat = "Html",
                    Subject = subject,
                    Body = body,
                    CallToActionText = !string.IsNullOrWhiteSpace(cta) ? cta : "Schedule Consultation",
                    CallToActionUrl = "https://nexacrm.local/advisory",
                    IsSystem = false,
                    IsActive = true,
                    IsDeleted = false,
                    CreatedByRole = RbacService.IsAdmin ? "Admin" : (RbacService.IsManager ? "Manager" : "Agent"),
                    CreatedByUserId = CurrentSession.UserId > 0 ? CurrentSession.UserId : null,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };

                db.EmailTemplates.Add(newTpl);
                await db.SaveChangesAsync();

                MessageBox.Show($"New template '{name}' created successfully.", "Template Created", MessageBoxButtons.OK, MessageBoxIcon.Information);

                _isCreatingNewTemplate = false;
                await LoadTemplatesLibraryDataAsync();

                var created = _allTemplateModels.FirstOrDefault(m => m.TemplateId == newTpl.TemplateId);
                if (created != null)
                {
                    SelectTemplateModel(created);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to create template: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private async Task OnToggleArchiveSelectedTemplateAsync()
        {
            if (_selectedTemplateModel == null) return;

            if (!RbacService.HasFullOversight && !RbacService.IsAdmin)
            {
                MessageBox.Show("Only Administrators and Managers are authorized to archive or unarchive email templates.", "Permission Denied", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            if (_selectedTemplateModel.IsAutomatedValuation)
            {
                MessageBox.Show("The core Automated Home Equity & Valuation template is managed via the Valuation tab and cannot be archived.", "Action Not Allowed", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            bool willArchive = _selectedTemplateModel.IsActive;
            string actionWord = willArchive ? "archive" : "unarchive (restore)";

            var confirm = MessageBox.Show(
                $"Are you sure you want to {actionWord} template '{_selectedTemplateModel.Name}'?\n\n" +
                (willArchive 
                    ? "Archived templates will be removed from active 1-to-1 outreach and campaign generation. You can restore them anytime under the 'Archived' filter." 
                    : "The template will be restored to Active status and will be immediately available in campaign and outreach workflows."),
                $"{char.ToUpper(actionWord[0]) + actionWord.Substring(1)} Template",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Question);

            if (confirm != DialogResult.Yes) return;

            try
            {
                using var db = LocalDb.CreateContext(CurrentSession.TenantId);
                var tpl = await db.EmailTemplates.FirstOrDefaultAsync(t => t.TemplateId == _selectedTemplateModel.TemplateId);
                if (tpl != null)
                {
                    tpl.IsActive = !willArchive;
                    tpl.UpdatedAt = DateTime.UtcNow;
                    await db.SaveChangesAsync();

                    MessageBox.Show($"Template '{tpl.Name}' is now {(willArchive ? "archived" : "restored to active status")}.", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    await LoadTemplatesLibraryDataAsync();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to {actionWord} template: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private async Task OnDeleteSelectedTemplateAsync()
        {
            if (_selectedTemplateModel == null) return;

            if (_selectedTemplateModel.IsSystem || _selectedTemplateModel.IsAutomatedValuation)
            {
                MessageBox.Show("Standard system templates cannot be deleted. You can archive them instead to remove them from active use.", "Cannot Delete", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            if (!RbacService.HasFullOversight && !RbacService.IsAdmin)
            {
                MessageBox.Show("Only Administrators and Managers are authorized to delete email templates.", "Permission Denied", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            var confirm = MessageBox.Show(
                $"Are you sure you want to delete custom template '{_selectedTemplateModel.Name}'?",
                "Confirm Deletion",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Warning);

            if (confirm != DialogResult.Yes) return;

            try
            {
                using var db = LocalDb.CreateContext(CurrentSession.TenantId);
                var tpl = await db.EmailTemplates.FirstOrDefaultAsync(t => t.TemplateId == _selectedTemplateModel.TemplateId);
                if (tpl != null)
                {
                    tpl.IsDeleted = true;
                    tpl.DeletedAt = DateTime.UtcNow;
                    tpl.DeletedByUserId = CurrentSession.UserId > 0 ? CurrentSession.UserId : null;
                    await db.SaveChangesAsync();

                    MessageBox.Show($"Template '{tpl.Name}' has been deleted.", "Deleted", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    await LoadTemplatesLibraryDataAsync();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to delete template: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void UpdateTemplateLivePreview()
        {
            if (_lblTplPreviewSubject == null || _lblTplPreviewBody == null) return;

            string sampleClient = "Maria Santos";
            string sampleProperty = "Unit 1204, One Serendra, BGC";
            string sampleAgent = CurrentSession.CurrentUser?.FullName ?? "Althea Garcia";
            string sampleIncentive = "Complimentary Home Equity & Neighborhood CMA Report";

            string rawSubject = _txtTplSubject != null ? _txtTplSubject.Text : string.Empty;
            string rawBody = _txtTplBody != null ? _txtTplBody.Text : string.Empty;
            string rawCta = _txtTplCta != null ? _txtTplCta.Text : string.Empty;

            string subject = rawSubject
                .Replace("{CustomerName}", sampleClient)
                .Replace("{{customer_name}}", sampleClient)
                .Replace("{PropertyAddress}", sampleProperty)
                .Replace("{{property_address}}", sampleProperty)
                .Replace("{YearsOwned}", "2");

            string body = rawBody
                .Replace("{CustomerName}", sampleClient)
                .Replace("{{customer_name}}", sampleClient)
                .Replace("{FirstName}", "Maria")
                .Replace("{PropertyAddress}", sampleProperty)
                .Replace("{{property_address}}", sampleProperty)
                .Replace("{PropertyType}", "Condominium")
                .Replace("{OriginalPrice}", "₱18,500,000")
                .Replace("{EstimatedValue}", "₱20,396,250")
                .Replace("{EquityGain}", "₱1,896,250")
                .Replace("{EquityPercent}", "10.3")
                .Replace("{AppreciationRate}", "5.0")
                .Replace("{YearsOwned}", "2")
                .Replace("{AgentName}", sampleAgent)
                .Replace("{{agent_name}}", sampleAgent)
                .Replace("{{proposed_incentive}}", sampleIncentive);

            _lblTplPreviewSubject.Text = $"Subject: {subject}";
            _lblTplPreviewBody.Text = body;
            _btnTplPreviewCta.Text = !string.IsNullOrWhiteSpace(rawCta) ? rawCta : "Schedule Advisory Review";
            _lblTplPreviewSignature.Text = $"Warm regards,\n{sampleAgent}\nLicensed Real Estate Advisory Team\nNEXA Real Estate Advisory";
        }

        private async Task OnSaveTemplateChangesAsync()
        {
            if (_selectedTemplateModel == null) return;

            if (!RbacService.HasFullOversight && !RbacService.IsAdmin)
            {
                MessageBox.Show("Only Administrators and Managers are authorized to modify standardized retention email templates.", "Permission Denied", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            try
            {
                using var db = LocalDb.CreateContext(CurrentSession.TenantId);

                if (_selectedTemplateModel.IsAutomatedValuation)
                {
                    var settings = await db.AutomatedEmailSettings.FirstOrDefaultAsync(s => s.TenantId == CurrentSession.TenantId);
                    if (settings != null)
                    {
                        settings.SubjectTemplate = _txtTplSubject.Text.Trim();
                        settings.CallToActionText = _txtTplCta.Text.Trim();
                        settings.BodyTemplate = _txtTplBody.Text.Trim();
                        settings.UpdatedAt = DateTime.UtcNow;
                        await db.SaveChangesAsync();
                    }
                }
                else
                {
                    var tpl = await db.EmailTemplates.FirstOrDefaultAsync(t => t.TemplateId == _selectedTemplateModel.TemplateId);
                    if (tpl != null)
                    {
                        if (!_selectedTemplateModel.IsSystem)
                        {
                            tpl.Name = _txtTplName.Text.Trim();
                            tpl.TargetAudience = _cboTplAudience.SelectedItem?.ToString() ?? tpl.TargetAudience;
                        }
                        tpl.Subject = _txtTplSubject.Text.Trim();
                        tpl.CallToActionText = _txtTplCta.Text.Trim();
                        tpl.Body = _txtTplBody.Text.Trim();
                        tpl.UpdatedAt = DateTime.UtcNow;
                        await db.SaveChangesAsync();

                        _selectedTemplateModel.Name = tpl.Name;
                        _selectedTemplateModel.TargetAudience = tpl.TargetAudience;
                    }
                }

                _selectedTemplateModel.Subject = _txtTplSubject.Text.Trim();
                _selectedTemplateModel.CallToActionText = _txtTplCta.Text.Trim();
                _selectedTemplateModel.Body = _txtTplBody.Text.Trim();

                MessageBox.Show($"Template '{_selectedTemplateModel.Name}' saved successfully.", "Saved", MessageBoxButtons.OK, MessageBoxIcon.Information);
                await LoadTemplatesLibraryDataAsync();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to save template: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private async Task OnSendTemplateTestEmailAsync()
        {
            string userEmail = CurrentSession.CurrentUser?.Email ?? string.Empty;
            if (string.IsNullOrWhiteSpace(userEmail))
            {
                MessageBox.Show("Your user profile does not have an email address configured for test delivery.", "Validation Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            var confirm = MessageBox.Show(
                $"Send a sample preview of '{_selectedTemplateModel?.Name}' to {userEmail}?",
                "Send Test Email",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Question);

            if (confirm != DialogResult.Yes) return;

            try
            {
                _btnSendTemplateTest.Enabled = false;
                var res = await ContactEmailService.SendAsync(
                    userEmail,
                    "[TEST TEMPLATE] " + _lblTplPreviewSubject.Text.Replace("Subject: ", ""),
                    _lblTplPreviewBody.Text + "\n\n" + _lblTplPreviewSignature.Text,
                    isBodyHtml: true);

                if (res.Success)
                {
                    MessageBox.Show($"Template test dispatched successfully to {userEmail}.", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
                else
                {
                    MessageBox.Show($"Test delivery failed: {res.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Test email failed: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                _btnSendTemplateTest.Enabled = true;
            }
        }

        #endregion

        #region Tab 3: Campaign Queue & Dispatch

        private void BuildTab3Queue()
        {
            _tabQueue = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Theme.Background,
                Padding = new Padding(28, 14, 28, 14),
                AutoScroll = true
            };

            var banner = CreateGuidanceBanner(
                "🚀 Retention Campaign Queue & Human-in-the-Loop Dispatch",
                "Automated home equity valuation check-ups and approved client care emails wait here for human review before dispatch. You can inspect rendered values, test individual dispatches, or send in batch.",
                "⚡ Generate Queue",
                async () => await OnGenerateAutomatedQueueClickedAsync());

            // Top Filter Strip
            var pnlTop = new Panel
            {
                Dock = DockStyle.Top,
                Height = 52,
                BackColor = Color.White,
                Padding = new Padding(14, 8, 14, 8),
                Margin = new Padding(0, 8, 0, 10)
            };
            UiRadiusHelper.StyleCard(pnlTop, 8);

            var lblStatus = new Label { Text = "Status:", Font = new Font("Segoe UI", 9f, FontStyle.Bold), Location = new Point(14, 14), AutoSize = true };
            _cboQueueStatusFilter = new ComboBox
            {
                Location = new Point(68, 10),
                Width = 115,
                DropDownStyle = ComboBoxStyle.DropDownList,
                Font = new Font("Segoe UI", 9f)
            };
            _cboQueueStatusFilter.Items.AddRange(new object[] { "All", "Queued", "Dispatched", "Failed" });
            _cboQueueStatusFilter.SelectedIndex = 0;

            var lblDate = new Label { Text = "From:", Font = new Font("Segoe UI", 9f, FontStyle.Bold), Location = new Point(195, 14), AutoSize = true };
            _dtpQueueFrom = new DateTimePicker
            {
                Location = new Point(240, 10),
                Width = 110,
                Format = DateTimePickerFormat.Short,
                Value = DateTime.Today.AddDays(-60)
            };

            var lblTo = new Label { Text = "To:", Font = new Font("Segoe UI", 9f, FontStyle.Bold), Location = new Point(360, 14), AutoSize = true };
            _dtpQueueTo = new DateTimePicker
            {
                Location = new Point(390, 10),
                Width = 110,
                Format = DateTimePickerFormat.Short,
                Value = DateTime.Today
            };

            _btnFilterQueue = new Button
            {
                Text = "Filter",
                Location = new Point(510, 9),
                Size = new Size(75, 32),
                BackColor = Theme.SidebarAccent,
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 8.5f, FontStyle.Bold),
                Cursor = Cursors.Hand
            };
            _btnFilterQueue.FlatAppearance.BorderSize = 0;
            UiRadiusHelper.StyleButton(_btnFilterQueue, 6);
            _btnFilterQueue.Click += async (s, e) => await LoadQueueDataAsync();

            var pnlQueueActions = new FlowLayoutPanel
            {
                Dock = DockStyle.Right,
                AutoSize = true,
                Height = 36,
                FlowDirection = FlowDirection.LeftToRight,
                WrapContents = false
            };

            _btnGenerateQueue = new Button
            {
                Text = "⚡ Generate Queue",
                Width = 145,
                Height = 32,
                BackColor = Color.White,
                ForeColor = Theme.SidebarAccent,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 8.5f, FontStyle.Bold),
                Cursor = Cursors.Hand,
                Margin = new Padding(0, 2, 8, 0)
            };
            _btnGenerateQueue.FlatAppearance.BorderColor = Theme.SidebarAccent;
            UiRadiusHelper.StyleButton(_btnGenerateQueue, 6);
            _btnGenerateQueue.Click += async (s, e) => await OnGenerateAutomatedQueueClickedAsync();

            _btnDispatchAll = new Button
            {
                Text = "🚀 Dispatch All Queued",
                Width = 160,
                Height = 32,
                BackColor = Color.FromArgb(22, 163, 74),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 8.5f, FontStyle.Bold),
                Cursor = Cursors.Hand,
                Margin = new Padding(0, 2, 0, 0)
            };
            _btnDispatchAll.FlatAppearance.BorderSize = 0;
            UiRadiusHelper.StyleButton(_btnDispatchAll, 6);
            _btnDispatchAll.Click += async (s, e) => await OnDispatchAllQueuedEmailsAsync();

            pnlQueueActions.Controls.Add(_btnGenerateQueue);
            pnlQueueActions.Controls.Add(_btnDispatchAll);

            pnlTop.Controls.Add(lblStatus);
            pnlTop.Controls.Add(_cboQueueStatusFilter);
            pnlTop.Controls.Add(lblDate);
            pnlTop.Controls.Add(_dtpQueueFrom);
            pnlTop.Controls.Add(lblTo);
            pnlTop.Controls.Add(_dtpQueueTo);
            pnlTop.Controls.Add(_btnFilterQueue);
            pnlTop.Controls.Add(pnlQueueActions);

            var splitQueue = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 2,
                RowCount = 1,
                BackColor = Color.Transparent,
                Margin = new Padding(0, 10, 0, 0)
            };
            splitQueue.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 58F));
            splitQueue.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 42F));
            splitQueue.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));

            // Left Grid Card
            var pnlGridCard = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.White,
                Padding = new Padding(16),
                Margin = new Padding(0, 0, 10, 0)
            };
            UiRadiusHelper.StyleCard(pnlGridCard, 10);

            _gridQueue = new DataGridView
            {
                Dock = DockStyle.Fill,
                BackgroundColor = Color.White,
                BorderStyle = BorderStyle.None,
                AllowUserToAddRows = false,
                AllowUserToDeleteRows = false,
                ReadOnly = true,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                MultiSelect = false,
                RowHeadersVisible = false,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill
            };
            UiGridHelper.ApplyModernGridStyle(_gridQueue, 48);
            ConfigureQueueGridColumns();

            _paginationQueue = new PaginationControl
            {
                Dock = DockStyle.Bottom,
                Height = 44
            };
            _paginationQueue.SetItemLabel("campaign emails");
            _paginationQueue.PageChanged += (_, _) => RenderPagedQueue(resetPage: false);
            _paginationQueue.PageSizeChanged += (_, _) => RenderPagedQueue(resetPage: true);

            _pnlQueueEmptyState = BuildQueueEmptyStatePanel();

            pnlGridCard.Controls.Add(_pnlQueueEmptyState);
            pnlGridCard.Controls.Add(_gridQueue);
            _gridQueueSkeleton = GridSkeletonOverlay.CreateForGrid(_gridQueue);
            pnlGridCard.Controls.Add(_paginationQueue);
            _gridQueue.BringToFront();
            splitQueue.Controls.Add(pnlGridCard, 0, 0);

            // Right Preview & Dispatch Card
            _pnlQueuePreviewCard = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.White,
                Padding = new Padding(20),
                Margin = new Padding(10, 0, 0, 0),
                AutoScroll = true
            };
            UiRadiusHelper.StyleCard(_pnlQueuePreviewCard, 10);

            var lblCardTitle = new Label { Text = "Campaign Dispatch Preview", Font = new Font("Segoe UI", 12f, FontStyle.Bold), ForeColor = Color.FromArgb(15, 23, 42), Location = new Point(14, 14), AutoSize = true };
            _lblQueuePreviewSubject = new Label { Text = "Select an entry from the queue to review and dispatch", Font = new Font("Segoe UI", 10f, FontStyle.Bold), ForeColor = Theme.SidebarAccent, Location = new Point(14, 46), Size = new Size(380, 40) };
            _lblQueuePreviewDetails = new Label { Text = "Recipient and segment details will appear here", Font = new Font("Segoe UI", 9f), ForeColor = Color.FromArgb(100, 116, 139), Location = new Point(14, 88), Size = new Size(380, 22) };

            _txtQueuePreviewBody = new TextBox
            {
                Location = new Point(14, 115),
                Size = new Size(380, 310),
                Multiline = true,
                ReadOnly = true,
                BackColor = Color.FromArgb(248, 250, 252),
                ScrollBars = ScrollBars.Vertical,
                Font = new Font("Segoe UI", 9.5f)
            };

            _btnDispatchSelected = new Button
            {
                Text = "🚀 Dispatch Retention Email Now",
                Location = new Point(14, 440),
                Size = new Size(380, 42),
                BackColor = Theme.SidebarAccent,
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 9.5f, FontStyle.Bold),
                Cursor = Cursors.Hand,
                Enabled = false
            };
            _btnDispatchSelected.FlatAppearance.BorderSize = 0;
            UiRadiusHelper.StyleButton(_btnDispatchSelected, 8);
            _btnDispatchSelected.Click += async (s, e) => await OnDispatchSelectedQueueEmailAsync();

            _pnlQueuePreviewCard.Controls.Add(lblCardTitle);
            _pnlQueuePreviewCard.Controls.Add(_lblQueuePreviewSubject);
            _pnlQueuePreviewCard.Controls.Add(_lblQueuePreviewDetails);
            _pnlQueuePreviewCard.Controls.Add(_txtQueuePreviewBody);
            _pnlQueuePreviewCard.Controls.Add(_btnDispatchSelected);
            splitQueue.Controls.Add(_pnlQueuePreviewCard, 1, 0);

            _tabQueue.Controls.Add(splitQueue);
            _tabQueue.Controls.Add(pnlTop);
            _tabQueue.Controls.Add(banner);

            banner.SendToBack();
            pnlTop.SendToBack();
            splitQueue.BringToFront();
        }

        private Panel BuildQueueEmptyStatePanel()
        {
            var pnl = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.White,
                Visible = false
            };

            var lblIcon = new Label
            {
                Text = "📬",
                Font = new Font("Segoe UI", 32f),
                TextAlign = ContentAlignment.MiddleCenter,
                Size = new Size(100, 60),
                Location = new Point(250, 100)
            };

            var lblTitle = new Label
            {
                Text = "Campaign Queue is Currently Empty",
                Font = new Font("Segoe UI", 13f, FontStyle.Bold),
                ForeColor = Color.FromArgb(15, 23, 42),
                TextAlign = ContentAlignment.MiddleCenter,
                Size = new Size(460, 30),
                Location = new Point(70, 165)
            };

            var lblDesc = new Label
            {
                Text = "There are no retention campaign emails waiting in the queue.\nGenerate automated equity check-ups for past property buyers or configure automation rules.",
                Font = new Font("Segoe UI", 9.5f),
                ForeColor = Color.FromArgb(100, 116, 139),
                TextAlign = ContentAlignment.MiddleCenter,
                Size = new Size(500, 45),
                Location = new Point(50, 200)
            };

            var btnGen = new Button
            {
                Text = "⚡ Generate Valuation Queue Now",
                Size = new Size(260, 40),
                Location = new Point(170, 260),
                BackColor = Theme.SidebarAccent,
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 9f, FontStyle.Bold),
                Cursor = Cursors.Hand
            };
            btnGen.FlatAppearance.BorderSize = 0;
            UiRadiusHelper.StyleButton(btnGen, 8);
            btnGen.Click += async (s, e) => await OnGenerateAutomatedQueueClickedAsync();

            var btnSettings = new Button
            {
                Text = "⚙ Configure Valuation Settings",
                Size = new Size(260, 36),
                Location = new Point(170, 310),
                BackColor = Color.White,
                ForeColor = Color.FromArgb(71, 85, 105),
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 9f),
                Cursor = Cursors.Hand
            };
            btnSettings.FlatAppearance.BorderColor = Color.FromArgb(226, 232, 240);
            UiRadiusHelper.StyleButton(btnSettings, 8);
            btnSettings.Click += (s, e) => SwitchTab(1);

            pnl.Controls.Add(lblIcon);
            pnl.Controls.Add(lblTitle);
            pnl.Controls.Add(lblDesc);
            pnl.Controls.Add(btnGen);
            pnl.Controls.Add(btnSettings);

            pnl.Resize += (_, _) =>
            {
                int cx = pnl.Width / 2;
                lblIcon.Left = cx - (lblIcon.Width / 2);
                lblTitle.Left = cx - (lblTitle.Width / 2);
                lblDesc.Left = cx - (lblDesc.Width / 2);
                btnGen.Left = cx - (btnGen.Width / 2);
                btnSettings.Left = cx - (btnSettings.Width / 2);
            };

            return pnl;
        }

        private void ConfigureQueueGridColumns()
        {
            _gridQueue.Columns.Clear();

            _gridQueue.Columns.Add(new DataGridViewTextBoxColumn { Name = "EmailLogId", HeaderText = "ID", FillWeight = 30 });
            _gridQueue.Columns.Add(new DataGridViewTextBoxColumn { Name = "CustomerName", HeaderText = "Client Name", FillWeight = 100 });
            _gridQueue.Columns.Add(new DataGridViewTextBoxColumn { Name = "Segment", HeaderText = "Segment", FillWeight = 85 });
            _gridQueue.Columns.Add(new DataGridViewTextBoxColumn { Name = "Subject", HeaderText = "Subject", FillWeight = 130 });
            _gridQueue.Columns.Add(new DataGridViewTextBoxColumn { Name = "Status", HeaderText = "Status", FillWeight = 75 });
            _gridQueue.Columns.Add(new DataGridViewTextBoxColumn { Name = "CreatedAt", HeaderText = "Queued On", FillWeight = 80 });

            UiGridHelper.AddActionsColumn(_gridQueue, 64);

            _gridQueue.CellPainting += GridQueue_CellPainting;
            _gridQueue.CellContentClick += GridQueue_CellContentClick;
            _gridQueue.SelectionChanged += GridQueue_SelectionChanged;
        }

        private void GridQueue_CellPainting(object? sender, DataGridViewCellPaintingEventArgs e)
        {
            if (e.RowIndex < 0) return;

            int nameIdx = _gridQueue.Columns["CustomerName"]?.Index ?? -1;
            int statusIdx = _gridQueue.Columns["Status"]?.Index ?? -1;

            if (e.ColumnIndex == nameIdx)
            {
                string text = e.Value?.ToString() ?? string.Empty;
                UiGridHelper.PaintAvatarCell(_gridQueue, e, text);
            }
            else if (e.ColumnIndex == statusIdx)
            {
                string text = e.Value?.ToString() ?? string.Empty;
                UiGridHelper.PaintStatusText(_gridQueue, e, text);
            }
        }

        private void GridQueue_SelectionChanged(object? sender, EventArgs e)
        {
            if (_gridQueue.CurrentRow == null || _gridQueue.CurrentRow.Index < 0)
            {
                _selectedQueueRow = null;
                _btnDispatchSelected.Enabled = false;
                _lblQueuePreviewSubject.Text = "Select an entry from the queue to review and dispatch";
                _lblQueuePreviewDetails.Text = string.Empty;
                _txtQueuePreviewBody.Text = string.Empty;
                return;
            }

            int logId = Convert.ToInt32(_gridQueue.CurrentRow.Cells["EmailLogId"].Value);
            _selectedQueueRow = _allQueueItems.FirstOrDefault(q => q.EmailLogId == logId);
            if (_selectedQueueRow != null)
            {
                _lblQueuePreviewSubject.Text = _selectedQueueRow.Subject;
                _lblQueuePreviewDetails.Text = $"To: {_selectedQueueRow.CustomerName} <{_selectedQueueRow.RecipientEmail}>  •  Segment: {_selectedQueueRow.Segment}";

                string bodyFormatted = (_selectedQueueRow.Body ?? string.Empty)
                    .Replace("\r\n", "\n")
                    .Replace("\n", "\r\n");

                _txtQueuePreviewBody.Text = bodyFormatted;
                _btnDispatchSelected.Enabled = _selectedQueueRow.Status == "Queued" || _selectedQueueRow.Status == "Failed";
            }
        }

        private void GridQueue_CellContentClick(object? sender, DataGridViewCellEventArgs e)
        {
            int actionIdx = _gridQueue.Columns["Actions"]?.Index ?? -1;
            if (e.RowIndex < 0 || e.ColumnIndex != actionIdx) return;

            var row = _gridQueue.Rows[e.RowIndex];
            int logId = Convert.ToInt32(row.Cells["EmailLogId"].Value);
            var item = _allQueueItems.FirstOrDefault(q => q.EmailLogId == logId);
            if (item == null) return;

            var menu = new ContextMenuStrip();
            if (item.Status == "Queued" || item.Status == "Failed")
            {
                menu.Items.Add("🚀 Dispatch Email Now", null, async (s, ev) =>
                {
                    _selectedQueueRow = item;
                    await OnDispatchSelectedQueueEmailAsync();
                });
            }

            menu.Items.Add("📋 View Full Details", null, (s, ev) =>
            {
                MessageBox.Show(
                    $"Campaign Item #{item.EmailLogId}\n" +
                    $"Client: {item.CustomerName} ({item.RecipientEmail})\n" +
                    $"Status: {item.Status}\n" +
                    $"Source: {item.GenerationSource}\n" +
                    $"Queued: {item.CreatedAt:g}\n" +
                    $"Dispatched: {item.DispatchedAt?.ToString("g") ?? "Pending"}\n" +
                    $"Error: {item.ErrorMessage ?? "None"}\n\n" +
                    $"Subject: {item.Subject}\n\n" +
                    $"Body:\n{item.Body}",
                    "Campaign Entry Details",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information);
            });

            var rect = _gridQueue.GetCellDisplayRectangle(e.ColumnIndex, e.RowIndex, true);
            menu.Show(_gridQueue, new Point(rect.Right - 150, rect.Bottom));
        }

        private async Task LoadQueueDataAsync()
        {
            _gridQueueSkeleton?.ShowSkeleton();
            try
            {
                string status = _cboQueueStatusFilter.SelectedItem?.ToString() ?? "All";
                DateTime from = _dtpQueueFrom.Value.Date;
                DateTime to = _dtpQueueTo.Value.Date;

                _allQueueItems = await _retentionController.GetCampaignQueueAsync(status, from, to);
                _paginationQueue.UpdatePagination(_allQueueItems.Count, _paginationQueue.CurrentPage, _paginationQueue.PageSize);
                RenderPagedQueue(resetPage: true);

                bool isEmpty = _allQueueItems.Count == 0;
                _pnlQueueEmptyState.Visible = isEmpty;
                if (isEmpty) _pnlQueueEmptyState.BringToFront();
            }
            finally
            {
                _gridQueueSkeleton?.HideSkeleton();
            }
        }

        private void RenderPagedQueue(bool resetPage = false)
        {
            if (resetPage) _paginationQueue.ResetPage();

            int pSize = _paginationQueue.PageSize;
            int page = _paginationQueue.CurrentPage;

            var paged = _allQueueItems.Skip((page - 1) * pSize).Take(pSize).ToList();

            _gridQueue.Rows.Clear();
            foreach (var q in paged)
            {
                _gridQueue.Rows.Add(
                    q.EmailLogId,
                    q.CustomerName,
                    q.Segment,
                    q.Subject,
                    q.Status,
                    q.CreatedAt.ToString("MMM dd, yyyy")
                );
            }
        }

        private async Task OnGenerateAutomatedQueueClickedAsync()
        {
            _btnGenerateQueue.Enabled = false;
            _btnGenerateQueue.Text = "Scanning...";

            try
            {
                int generated = await _retentionController.GenerateAutomatedValuationCampaignsAsync(forceAll: true);
                if (generated > 0)
                {
                    MessageBox.Show(
                        $"Automation Scan Complete: {generated} personalized equity valuation campaign emails have been generated and queued for past clients.",
                        "Campaigns Queued",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Information);
                }
                else
                {
                    MessageBox.Show(
                        "All eligible past property buyers already have active items in the campaign queue.",
                        "Queue Up to Date",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Information);
                }

                await LoadQueueDataAsync();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to generate automated retention campaigns: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                _btnGenerateQueue.Enabled = true;
                _btnGenerateQueue.Text = "⚡ Generate Queue";
            }
        }

        private async Task OnDispatchAllQueuedEmailsAsync()
        {
            int queuedCount = _allQueueItems.Count(q => q.Status == "Queued");
            if (queuedCount == 0)
            {
                MessageBox.Show("There are no emails with status 'Queued' waiting in the list.", "Notice", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            var confirm = MessageBox.Show(
                $"Dispatch all {queuedCount} queued retention campaign emails now?\n\n(Emails will respect the 30-day anti-fatigue cooldown unless overridden.)",
                "Confirm Batch Dispatch",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Question);

            if (confirm != DialogResult.Yes) return;

            _btnDispatchAll.Enabled = false;
            _btnDispatchAll.Text = "Dispatching...";

            try
            {
                var result = await _retentionController.DispatchAllQueuedCampaignEmailsAsync(overrideCooldown: false);
                string summary = $"Batch Dispatch Completed:\n• {result.Dispatched} emails dispatched successfully.\n";
                if (result.SkippedCooldown > 0) summary += $"• {result.SkippedCooldown} skipped (in 30-day cooldown).\n";
                if (result.Failed > 0) summary += $"• {result.Failed} failed delivery.";

                MessageBox.Show(summary, "Batch Results", MessageBoxButtons.OK, MessageBoxIcon.Information);
                await LoadQueueDataAsync();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Batch dispatch encountered an error: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                _btnDispatchAll.Enabled = true;
                _btnDispatchAll.Text = "🚀 Dispatch All Queued";
            }
        }

        private async Task OnDispatchSelectedQueueEmailAsync()
        {
            if (_selectedQueueRow == null) return;

            var confirm = MessageBox.Show(
                $"Dispatch retention email #{_selectedQueueRow.EmailLogId} to {_selectedQueueRow.CustomerName} ({_selectedQueueRow.RecipientEmail})?",
                "Confirm Dispatch",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Question);

            if (confirm != DialogResult.Yes) return;

            try
            {
                _btnDispatchSelected.Enabled = false;
                await _retentionController.DispatchQueuedEmailAsync(
                    _selectedQueueRow.EmailLogId,
                    _selectedQueueRow.Subject,
                    _selectedQueueRow.Body);

                MessageBox.Show("Retention campaign email dispatched successfully.", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
                await LoadQueueDataAsync();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Dispatch failed: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                _btnDispatchSelected.Enabled = true;
            }
        }

        #endregion

        #region Tab 4: 1-to-1 Manual Outreach

        private void BuildTab4Outreach()
        {
            _tabOutreach = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Theme.Background,
                Padding = new Padding(28, 14, 28, 14),
                AutoScroll = true
            };

            var banner = CreateGuidanceBanner(
                "✉️ Direct 1-to-1 Client Retention Outreach",
                "Craft personalized retention notes for high-value clients using curated retention templates and complimentary advisory services. NEXA enforces a 30-day anti-fatigue cooldown policy between outreach touches.",
                "View Queue",
                () => SwitchTab(3));

            var tableSplit = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 2,
                RowCount = 1,
                BackColor = Color.Transparent,
                Margin = new Padding(0, 10, 0, 0)
            };
            tableSplit.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 46F));
            tableSplit.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 54F));
            tableSplit.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));

            // Left Card: Controls & Customer Search
            var leftCard = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.White,
                Padding = new Padding(20),
                Margin = new Padding(0, 0, 10, 0),
                AutoScroll = true
            };
            UiRadiusHelper.StyleCard(leftCard, 10);

            var lblSearchTitle = new Label { Text = "Select Target Client *", Font = new Font("Segoe UI", 9.5f, FontStyle.Bold), Location = new Point(14, 12), AutoSize = true };
            _txtSearchOutreach = new TextBox
            {
                Location = new Point(14, 34),
                Width = 380,
                Font = new Font("Segoe UI", 9.5f),
                PlaceholderText = "Search by client name, email, or phone..."
            };
            _txtSearchOutreach.TextChanged += (s, e) => FilterOutreachSearchResults();

            _lstOutreachResults = new ListBox
            {
                Location = new Point(14, 66),
                Width = 380,
                Height = 90,
                Font = new Font("Segoe UI", 9f),
                Visible = false
            };
            _lstOutreachResults.SelectedIndexChanged += (s, e) => OnOutreachClientSelected();

            // Client Info Card
            _pnlOutreachClientCard = new Panel
            {
                Location = new Point(14, 70),
                Width = 380,
                Height = 110,
                BackColor = Color.FromArgb(248, 250, 252),
                Padding = new Padding(12)
            };
            UiRadiusHelper.StyleCard(_pnlOutreachClientCard, 6);

            _lblOutreachClientName = new Label { Text = "No Client Selected", Font = new Font("Segoe UI", 10.5f, FontStyle.Bold), Location = new Point(12, 10), AutoSize = true };
            _lblOutreachClientDetails = new Label { Text = "Search above to select a client for 1-to-1 retention outreach", Font = new Font("Segoe UI", 9f), ForeColor = Color.FromArgb(100, 116, 139), Location = new Point(12, 34), AutoSize = true };
            _lblOutreachSegmentStatus = new Label { Text = "Segment: None", Font = new Font("Segoe UI", 9f, FontStyle.Bold), ForeColor = Theme.SidebarAccent, Location = new Point(12, 58), AutoSize = true };
            _lblOutreachCooldownAlert = new Label { Text = "Policy: Ready", Font = new Font("Segoe UI", 9f, FontStyle.Bold), ForeColor = Color.FromArgb(22, 163, 74), Location = new Point(12, 80), AutoSize = true };

            _pnlOutreachClientCard.Controls.Add(_lblOutreachClientName);
            _pnlOutreachClientCard.Controls.Add(_lblOutreachClientDetails);
            _pnlOutreachClientCard.Controls.Add(_lblOutreachSegmentStatus);
            _pnlOutreachClientCard.Controls.Add(_lblOutreachCooldownAlert);

            // Template Selector
            var lblTpl = new Label { Text = "Retention Email Template *", Font = new Font("Segoe UI", 9f, FontStyle.Bold), Location = new Point(14, 190), AutoSize = true };
            _cboOutreachTemplate = new ComboBox
            {
                Location = new Point(14, 212),
                Width = 380,
                DropDownStyle = ComboBoxStyle.DropDownList,
                Font = new Font("Segoe UI", 9.5f)
            };
            _cboOutreachTemplate.SelectedIndexChanged += (s, e) => ApplySelectedOutreachTemplate();

            // Complimentary Client Care Service Offering
            var lblInc = new Label { Text = "Complimentary Client Care Service Offering:", Font = new Font("Segoe UI", 9f, FontStyle.Bold), Location = new Point(14, 250), AutoSize = true };
            _txtOutreachIncentive = new TextBox
            {
                Location = new Point(14, 272),
                Width = 380,
                Font = new Font("Segoe UI", 9.5f)
            };
            _txtOutreachIncentive.TextChanged += (s, e) => UpdateOutreachPreview();

            // Subject
            var lblSubj = new Label { Text = "Subject *", Font = new Font("Segoe UI", 9f, FontStyle.Bold), Location = new Point(14, 308), AutoSize = true };
            _txtOutreachSubject = new TextBox
            {
                Location = new Point(14, 330),
                Width = 380,
                Font = new Font("Segoe UI", 9.5f)
            };
            _txtOutreachSubject.TextChanged += (s, e) => UpdateOutreachPreview();

            // Body
            var lblBody = new Label { Text = "Email Message Body *", Font = new Font("Segoe UI", 9f, FontStyle.Bold), Location = new Point(14, 366), AutoSize = true };
            _txtOutreachBody = new TextBox
            {
                Location = new Point(14, 388),
                Width = 380,
                Height = 150,
                Multiline = true,
                ScrollBars = ScrollBars.Vertical,
                Font = new Font("Segoe UI", 9.5f)
            };
            _txtOutreachBody.TextChanged += (s, e) => UpdateOutreachPreview();

            // Dispatch Buttons
            _btnSendManualEmail = new Button
            {
                Text = "✉ Dispatch Retention Email Now",
                Location = new Point(14, 552),
                Size = new Size(250, 40),
                BackColor = Theme.SidebarAccent,
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 9.5f, FontStyle.Bold),
                Cursor = Cursors.Hand
            };
            _btnSendManualEmail.FlatAppearance.BorderSize = 0;
            UiRadiusHelper.StyleButton(_btnSendManualEmail, 8);
            _btnSendManualEmail.Click += async (s, e) => await OnSendManualRetentionEmailAsync();

            _btnSendTestEmail = new Button
            {
                Text = "Send Test",
                Location = new Point(275, 552),
                Size = new Size(119, 40),
                BackColor = Color.White,
                ForeColor = Color.FromArgb(71, 85, 105),
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 9f),
                Cursor = Cursors.Hand
            };
            _btnSendTestEmail.FlatAppearance.BorderColor = Color.FromArgb(226, 232, 240);
            UiRadiusHelper.StyleButton(_btnSendTestEmail, 8);
            _btnSendTestEmail.Click += async (s, e) => await OnSendTestEmailAsync();

            leftCard.Controls.Add(lblSearchTitle);
            leftCard.Controls.Add(_txtSearchOutreach);
            leftCard.Controls.Add(_lstOutreachResults);
            leftCard.Controls.Add(_pnlOutreachClientCard);
            leftCard.Controls.Add(lblTpl);
            leftCard.Controls.Add(_cboOutreachTemplate);
            leftCard.Controls.Add(lblInc);
            leftCard.Controls.Add(_txtOutreachIncentive);
            leftCard.Controls.Add(lblSubj);
            leftCard.Controls.Add(_txtOutreachSubject);
            leftCard.Controls.Add(lblBody);
            leftCard.Controls.Add(_txtOutreachBody);
            leftCard.Controls.Add(_btnSendManualEmail);
            leftCard.Controls.Add(_btnSendTestEmail);
            tableSplit.Controls.Add(leftCard, 0, 0);

            // Right Card: Branded Real-Estate Live Preview Card
            _pnlOutreachPreview = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.White,
                Padding = new Padding(24),
                Margin = new Padding(10, 0, 0, 0),
                AutoScroll = true
            };
            UiRadiusHelper.StyleCard(_pnlOutreachPreview, 10);

            var previewContainer = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.FromArgb(248, 250, 252),
                Padding = new Padding(20),
                AutoScroll = true
            };
            UiRadiusHelper.StyleCard(previewContainer, 8);

            var lblHeaderBrand = new Label
            {
                Text = "NEXA Real Estate Advisory · Client Retention Outreach",
                Font = new Font("Segoe UI", 8.5f, FontStyle.Bold),
                ForeColor = Theme.SidebarAccent,
                Location = new Point(16, 14),
                AutoSize = true
            };

            _lblPreviewTo = new Label
            {
                Text = "To: (Recipient will appear here)",
                Font = new Font("Segoe UI", 9.5f),
                ForeColor = Color.FromArgb(71, 85, 105),
                Location = new Point(16, 38),
                Size = new Size(520, 22)
            };

            _lblPreviewSubject = new Label
            {
                Text = "Subject: (Subject will appear here)",
                Font = new Font("Segoe UI", 11f, FontStyle.Bold),
                ForeColor = Color.FromArgb(15, 23, 42),
                Location = new Point(16, 62),
                Size = new Size(520, 26)
            };

            var pnlDivider = new Panel
            {
                Location = new Point(16, 94),
                Size = new Size(520, 1),
                BackColor = Color.FromArgb(226, 232, 240)
            };

            _lblPreviewBody = new Label
            {
                Text = "Select a client and template to preview the personalized outreach message.",
                Font = new Font("Segoe UI", 9.5f),
                ForeColor = Color.FromArgb(30, 41, 59),
                Location = new Point(16, 106),
                Size = new Size(520, 220)
            };

            // Incentive Highlight Callout Box
            _pnlPreviewIncentiveBox = new Panel
            {
                Location = new Point(16, 335),
                Size = new Size(520, 60),
                BackColor = Color.FromArgb(240, 253, 244),
                Padding = new Padding(12)
            };
            UiRadiusHelper.StyleCard(_pnlPreviewIncentiveBox, 6);

            _lblPreviewIncentiveText = new Label
            {
                Text = "📋 Complimentary Advisory Service: Comprehensive Property Equity & CMA Consultation",
                Font = new Font("Segoe UI", 9f, FontStyle.Bold),
                ForeColor = Color.FromArgb(22, 101, 52),
                Location = new Point(12, 18),
                Size = new Size(495, 24)
            };
            _pnlPreviewIncentiveBox.Controls.Add(_lblPreviewIncentiveText);

            _lblPreviewSignature = new Label
            {
                Text = "Best regards,\nYour Dedicated Real Estate Advisor\nNEXA Real Estate Advisory Team",
                Font = new Font("Segoe UI", 9f),
                ForeColor = Color.FromArgb(100, 116, 139),
                Location = new Point(16, 405),
                Size = new Size(520, 60)
            };

            previewContainer.Controls.Add(lblHeaderBrand);
            previewContainer.Controls.Add(_lblPreviewTo);
            previewContainer.Controls.Add(_lblPreviewSubject);
            previewContainer.Controls.Add(pnlDivider);
            previewContainer.Controls.Add(_lblPreviewBody);
            previewContainer.Controls.Add(_pnlPreviewIncentiveBox);
            previewContainer.Controls.Add(_lblPreviewSignature);

            _pnlOutreachPreview.Controls.Add(previewContainer);
            tableSplit.Controls.Add(_pnlOutreachPreview, 1, 0);

            _tabOutreach.Controls.Add(tableSplit);
            _tabOutreach.Controls.Add(banner);
            banner.SendToBack();
            tableSplit.BringToFront();
        }

        private async Task LoadManualEmailDataAsync()
        {
            using var db = LocalDb.CreateContext(CurrentSession.TenantId);
            _loadedRetentionTemplates = db.EmailTemplates
                .AsNoTracking()
                .Where(t => t.Category == "Retention" && t.IsActive)
                .OrderBy(t => t.Name)
                .ToList();

            _cboOutreachTemplate.Items.Clear();
            foreach (var t in _loadedRetentionTemplates)
            {
                _cboOutreachTemplate.Items.Add(t.Name);
            }

            if (_selectedOutreachCustomer != null)
            {
                PopulateOutreachCustomerView(_selectedOutreachCustomer);
            }
            else if (_allClients.Count > 0)
            {
                PopulateOutreachCustomerView(_allClients[0]);
            }
        }

        private void FilterOutreachSearchResults()
        {
            string s = _txtSearchOutreach.Text.Trim().ToLowerInvariant();
            if (string.IsNullOrWhiteSpace(s))
            {
                _lstOutreachResults.Visible = false;
                return;
            }

            var matches = _allClients.Where(c =>
                c.FullName.ToLower().Contains(s) ||
                c.Email.ToLower().Contains(s) ||
                c.Phone.Contains(s)).Take(5).ToList();

            _lstOutreachResults.Items.Clear();
            foreach (var m in matches)
            {
                _lstOutreachResults.Items.Add(new OutreachComboItem(m));
            }

            if (_lstOutreachResults.Items.Count > 0)
            {
                _lstOutreachResults.BringToFront();
                _lstOutreachResults.Visible = true;
            }
            else
            {
                _lstOutreachResults.Visible = false;
            }
        }

        private void OnOutreachClientSelected()
        {
            if (_lstOutreachResults.SelectedItem is OutreachComboItem item)
            {
                _selectedOutreachCustomer = item.Customer;
                _lstOutreachResults.Visible = false;
                _txtSearchOutreach.Text = item.Customer.FullName;
                PopulateOutreachCustomerView(item.Customer);
            }
        }

        private void PopulateOutreachCustomerView(RetentionCustomerRow cust)
        {
            _selectedOutreachCustomer = cust;
            _lblOutreachClientName.Text = cust.FullName;
            _lblOutreachClientDetails.Text = $"{cust.Email}  •  {cust.Phone}  •  Advisor: {cust.AssignedAgentName}";
            _lblOutreachSegmentStatus.Text = $"Segment: {cust.CurrentSegment.ToUpper()}";

            if (cust.IsOnCooldown)
            {
                _lblOutreachCooldownAlert.Text = $"⚠ 30-Day Cooldown Active ({cust.DaysUntilCooldownExpires} days remaining)";
                _lblOutreachCooldownAlert.ForeColor = Color.FromArgb(217, 119, 6);
            }
            else
            {
                _lblOutreachCooldownAlert.Text = "✓ Ready for Outreach (No active cooldown)";
                _lblOutreachCooldownAlert.ForeColor = Color.FromArgb(22, 163, 74);
            }

            _txtOutreachIncentive.Text = RetentionCalculationService.GetRecommendedIncentive(cust.CurrentSegment);

            int tplIdx = -1;
            for (int i = 0; i < _loadedRetentionTemplates.Count; i++)
            {
                if (_loadedRetentionTemplates[i].Name.Contains(cust.CurrentSegment, StringComparison.OrdinalIgnoreCase))
                {
                    tplIdx = i;
                    break;
                }
            }

            if (tplIdx >= 0)
            {
                _cboOutreachTemplate.SelectedIndex = tplIdx;
            }
            else if (_cboOutreachTemplate.Items.Count > 0)
            {
                _cboOutreachTemplate.SelectedIndex = 0;
            }

            UpdateOutreachPreview();
        }

        private void ApplySelectedOutreachTemplate()
        {
            if (_cboOutreachTemplate.SelectedIndex < 0 || _cboOutreachTemplate.SelectedIndex >= _loadedRetentionTemplates.Count)
                return;

            var tpl = _loadedRetentionTemplates[_cboOutreachTemplate.SelectedIndex];
            _txtOutreachSubject.Text = tpl.Subject;

            string normalizedBody = (tpl.Body ?? string.Empty)
                .Replace("\r\n", "\n")
                .Replace("\n", "\r\n");

            _txtOutreachBody.Text = normalizedBody;
            UpdateOutreachPreview();
        }

        private void UpdateOutreachPreview()
        {
            if (_selectedOutreachCustomer == null) return;

            string clientFirstName = _selectedOutreachCustomer.FirstName;
            string clientFullName = _selectedOutreachCustomer.FullName;
            string incentive = _txtOutreachIncentive.Text.Trim();
            string advisor = _selectedOutreachCustomer.AssignedAgentName;

            string subject = _txtOutreachSubject.Text
                .Replace("{{customer_name}}", clientFirstName)
                .Replace("{{first_name}}", clientFirstName)
                .Replace("{{proposed_incentive}}", incentive);

            string body = _txtOutreachBody.Text
                .Replace("{{customer_name}}", clientFirstName)
                .Replace("{{first_name}}", clientFirstName)
                .Replace("{{customer_full_name}}", clientFullName)
                .Replace("{{proposed_incentive}}", incentive)
                .Replace("{{agent_name}}", advisor);

            _lblPreviewTo.Text = $"To: {clientFullName} <{_selectedOutreachCustomer.Email}>";
            _lblPreviewSubject.Text = $"Subject: {subject}";
            _lblPreviewBody.Text = body;

            if (!string.IsNullOrWhiteSpace(incentive))
            {
                _lblPreviewIncentiveText.Text = $"📋 Complimentary Advisory Service: {incentive}";
                _pnlPreviewIncentiveBox.Visible = true;
            }
            else
            {
                _pnlPreviewIncentiveBox.Visible = false;
            }

            _lblPreviewSignature.Text = $"Best regards,\n{advisor}\nLicensed Real Estate Advisory Team\nNEXA Real Estate Advisory";
        }

        private async Task OnSendManualRetentionEmailAsync()
        {
            if (_selectedOutreachCustomer == null)
            {
                MessageBox.Show("Please select a target client first.", "Validation Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            if (string.IsNullOrWhiteSpace(_txtOutreachSubject.Text) || string.IsNullOrWhiteSpace(_txtOutreachBody.Text))
            {
                MessageBox.Show("Subject and email body are required.", "Validation Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            bool overrideCooldown = false;
            string? overrideReason = null;

            if (_selectedOutreachCustomer.IsOnCooldown)
            {
                var overrideDlg = new CooldownOverrideDialog(
                    _selectedOutreachCustomer.FullName,
                    _selectedOutreachCustomer.LastRetentionEmailSentAt,
                    _selectedOutreachCustomer.DaysUntilCooldownExpires);

                if (overrideDlg.ShowDialog(this) != DialogResult.OK)
                {
                    return;
                }

                overrideCooldown = true;
                overrideReason = overrideDlg.OverrideReason;
            }

            try
            {
                _btnSendManualEmail.Enabled = false;
                _btnSendManualEmail.Text = "Sending...";

                string subject = _txtOutreachSubject.Text.Trim();
                string body = _txtOutreachBody.Text.Trim();
                string? incentive = string.IsNullOrWhiteSpace(_txtOutreachIncentive.Text) ? null : _txtOutreachIncentive.Text.Trim();

                await _retentionController.SendManualRetentionEmailAsync(
                    _selectedOutreachCustomer.CustomerId,
                    subject,
                    body,
                    incentive,
                    overrideCooldown,
                    overrideReason);

                MessageBox.Show("Retention email sent successfully.", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
                await LoadSegmentsDataAsync();
                SwitchTab(0);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to send retention email: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                _btnSendManualEmail.Enabled = true;
                _btnSendManualEmail.Text = "✉ Dispatch Retention Email Now";
            }
        }

        private async Task OnSendTestEmailAsync()
        {
            string userEmail = CurrentSession.CurrentUser?.Email ?? string.Empty;
            if (string.IsNullOrWhiteSpace(userEmail))
            {
                MessageBox.Show("Your user profile does not have an email address configured for test delivery.", "Validation Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            var confirm = MessageBox.Show(
                $"Send a formatted test preview to your email ({userEmail})?",
                "Send Test Dispatch",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Question);

            if (confirm != DialogResult.Yes) return;

            try
            {
                _btnSendTestEmail.Enabled = false;
                var res = await ContactEmailService.SendAsync(
                    userEmail,
                    "[TEST DISPATCH] " + _txtOutreachSubject.Text.Trim(),
                    _lblPreviewBody.Text,
                    isBodyHtml: true);

                if (res.Success)
                {
                    MessageBox.Show($"Test email dispatched successfully to {userEmail}.", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
                else
                {
                    MessageBox.Show($"Test email failed: {res.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Test email failed: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                _btnSendTestEmail.Enabled = true;
            }
        }

        #endregion

        #region Tab 5: Advisory Service Requests (Incentive Approvals)

        private void BuildTab5Requests()
        {
            _tabRequests = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Theme.Background,
                Padding = new Padding(28, 14, 28, 14),
                AutoScroll = true
            };

            var banner = CreateGuidanceBanner(
                "🛡️ Client Care Service Governance",
                "What is this for? Agents propose complimentary brokerage-sponsored professional services (licensed appraisals, title deed reviews, neighborhood CMA studies) to re-engage valued homeowners. Managers/Admins review and approve before dispatch. (Strict Policy: Real estate client care services only — no retail coupons, vouchers, or sales discounts).",
                "➕ Request Service",
                () => ShowNewRequestModal());

            // Top Filter Strip
            var pnlTop = new Panel
            {
                Dock = DockStyle.Top,
                Height = 52,
                BackColor = Color.White,
                Padding = new Padding(14, 8, 14, 8),
                Margin = new Padding(0, 8, 0, 10)
            };
            UiRadiusHelper.StyleCard(pnlTop, 8);

            var lblStatus = new Label { Text = "Status:", Font = new Font("Segoe UI", 9f, FontStyle.Bold), Location = new Point(14, 14), AutoSize = true };
            _cboRequestStatusFilter = new ComboBox
            {
                Location = new Point(68, 10),
                Width = 115,
                DropDownStyle = ComboBoxStyle.DropDownList,
                Font = new Font("Segoe UI", 9f)
            };
            _cboRequestStatusFilter.Items.AddRange(new object[] { "All", "Pending", "Approved", "Rejected" });
            _cboRequestStatusFilter.SelectedIndex = 0;

            var lblDate = new Label { Text = "From:", Font = new Font("Segoe UI", 9f, FontStyle.Bold), Location = new Point(200, 14), AutoSize = true };
            _dtpRequestFrom = new DateTimePicker
            {
                Location = new Point(245, 10),
                Width = 110,
                Format = DateTimePickerFormat.Short,
                Value = DateTime.Today.AddDays(-60)
            };

            var lblTo = new Label { Text = "To:", Font = new Font("Segoe UI", 9f, FontStyle.Bold), Location = new Point(365, 14), AutoSize = true };
            _dtpRequestTo = new DateTimePicker
            {
                Location = new Point(395, 10),
                Width = 110,
                Format = DateTimePickerFormat.Short,
                Value = DateTime.Today
            };

            _btnFilterRequests = new Button
            {
                Text = "Filter",
                Location = new Point(515, 9),
                Size = new Size(80, 32),
                BackColor = Theme.SidebarAccent,
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 8.5f, FontStyle.Bold),
                Cursor = Cursors.Hand
            };
            _btnFilterRequests.FlatAppearance.BorderSize = 0;
            UiRadiusHelper.StyleButton(_btnFilterRequests, 6);
            _btnFilterRequests.Click += async (s, e) => await LoadRequestsDataAsync();

            _btnTab5NewRequest = new Button
            {
                Text = "➕ Request Client Care Service",
                Dock = DockStyle.Right,
                Width = 210,
                Height = 32,
                BackColor = Color.White,
                ForeColor = Theme.SidebarAccent,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 8.5f, FontStyle.Bold),
                Cursor = Cursors.Hand
            };
            _btnTab5NewRequest.FlatAppearance.BorderColor = Theme.SidebarAccent;
            UiRadiusHelper.StyleButton(_btnTab5NewRequest, 6);
            _btnTab5NewRequest.Click += (s, e) => ShowNewRequestModal();

            pnlTop.Controls.Add(lblStatus);
            pnlTop.Controls.Add(_cboRequestStatusFilter);
            pnlTop.Controls.Add(lblDate);
            pnlTop.Controls.Add(_dtpRequestFrom);
            pnlTop.Controls.Add(lblTo);
            pnlTop.Controls.Add(_dtpRequestTo);
            pnlTop.Controls.Add(_btnFilterRequests);
            pnlTop.Controls.Add(_btnTab5NewRequest);

            // Requests Grid Card
            var pnlGridCard = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.White,
                Padding = new Padding(16),
                Margin = new Padding(0, 10, 0, 0)
            };
            UiRadiusHelper.StyleCard(pnlGridCard, 10);

            _gridRequests = new DataGridView
            {
                Dock = DockStyle.Fill,
                BackgroundColor = Color.White,
                BorderStyle = BorderStyle.None,
                AllowUserToAddRows = false,
                AllowUserToDeleteRows = false,
                ReadOnly = true,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                MultiSelect = false,
                RowHeadersVisible = false,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill
            };
            UiGridHelper.ApplyModernGridStyle(_gridRequests, 48);
            ConfigureRequestsGridColumns();

            _paginationRequests = new PaginationControl
            {
                Dock = DockStyle.Bottom,
                Height = 44
            };
            _paginationRequests.SetItemLabel("requests");
            _paginationRequests.PageChanged += (_, _) => RenderPagedRequests(resetPage: false);
            _paginationRequests.PageSizeChanged += (_, _) => RenderPagedRequests(resetPage: true);

            _pnlRequestsEmptyState = BuildRequestsEmptyStatePanel();

            pnlGridCard.Controls.Add(_pnlRequestsEmptyState);
            pnlGridCard.Controls.Add(_gridRequests);
            _gridRequestsSkeleton = GridSkeletonOverlay.CreateForGrid(_gridRequests);
            pnlGridCard.Controls.Add(_paginationRequests);
            _gridRequests.BringToFront();

            _tabRequests.Controls.Add(pnlGridCard);
            _tabRequests.Controls.Add(pnlTop);
            _tabRequests.Controls.Add(banner);

            banner.SendToBack();
            pnlTop.SendToBack();
            pnlGridCard.BringToFront();
        }

        private Panel BuildRequestsEmptyStatePanel()
        {
            var pnl = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.White,
                Visible = false
            };

            var lblIcon = new Label
            {
                Text = "🛡",
                Font = new Font("Segoe UI", 32f),
                TextAlign = ContentAlignment.MiddleCenter,
                Size = new Size(100, 60),
                Location = new Point(350, 110)
            };

            var lblTitle = new Label
            {
                Text = "No Client Care Service Requests Pending",
                Font = new Font("Segoe UI", 13f, FontStyle.Bold),
                ForeColor = Color.FromArgb(15, 23, 42),
                TextAlign = ContentAlignment.MiddleCenter,
                Size = new Size(500, 30),
                Location = new Point(150, 175)
            };

            var lblDesc = new Label
            {
                Text = "No complimentary client care service requests have been submitted for this period.\nAgents can submit proposals for manager approval to offer licensed appraisals or deed reviews to valued clients.",
                Font = new Font("Segoe UI", 9.5f),
                ForeColor = Color.FromArgb(100, 116, 139),
                TextAlign = ContentAlignment.MiddleCenter,
                Size = new Size(620, 45),
                Location = new Point(90, 210)
            };

            var btnCreate = new Button
            {
                Text = "➕ Request Client Care Service",
                Size = new Size(240, 40),
                Location = new Point(280, 275),
                BackColor = Theme.SidebarAccent,
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 9f, FontStyle.Bold),
                Cursor = Cursors.Hand
            };
            btnCreate.FlatAppearance.BorderSize = 0;
            UiRadiusHelper.StyleButton(btnCreate, 8);
            btnCreate.Click += (s, e) => ShowNewRequestModal();

            pnl.Controls.Add(lblIcon);
            pnl.Controls.Add(lblTitle);
            pnl.Controls.Add(lblDesc);
            pnl.Controls.Add(btnCreate);

            pnl.Resize += (_, _) =>
            {
                int cx = pnl.Width / 2;
                lblIcon.Left = cx - (lblIcon.Width / 2);
                lblTitle.Left = cx - (lblTitle.Width / 2);
                lblDesc.Left = cx - (lblDesc.Width / 2);
                btnCreate.Left = cx - (btnCreate.Width / 2);
            };

            return pnl;
        }

        private void ConfigureRequestsGridColumns()
        {
            _gridRequests.Columns.Clear();

            _gridRequests.Columns.Add(new DataGridViewTextBoxColumn { Name = "RequestId", HeaderText = "REQ #", FillWeight = 35 });
            _gridRequests.Columns.Add(new DataGridViewTextBoxColumn { Name = "CustomerName", HeaderText = "Client Name", FillWeight = 110 });
            _gridRequests.Columns.Add(new DataGridViewTextBoxColumn { Name = "SubmitterName", HeaderText = "Submitted By", FillWeight = 100 });
            _gridRequests.Columns.Add(new DataGridViewTextBoxColumn { Name = "TargetSegment", HeaderText = "Segment", FillWeight = 90 });
            _gridRequests.Columns.Add(new DataGridViewTextBoxColumn { Name = "ProposedIncentive", HeaderText = "Complimentary Service", FillWeight = 130 });
            _gridRequests.Columns.Add(new DataGridViewTextBoxColumn { Name = "ReasonCategory", HeaderText = "Reason", FillWeight = 110 });
            _gridRequests.Columns.Add(new DataGridViewTextBoxColumn { Name = "Status", HeaderText = "Status", FillWeight = 75 });
            _gridRequests.Columns.Add(new DataGridViewTextBoxColumn { Name = "CreatedAt", HeaderText = "Submitted On", FillWeight = 85 });

            UiGridHelper.AddActionsColumn(_gridRequests, 64);

            _gridRequests.CellPainting += GridRequests_CellPainting;
            _gridRequests.CellContentClick += GridRequests_CellContentClick;
        }

        private void GridRequests_CellPainting(object? sender, DataGridViewCellPaintingEventArgs e)
        {
            if (e.RowIndex < 0) return;

            int custIdx = _gridRequests.Columns["CustomerName"]?.Index ?? -1;
            int subIdx = _gridRequests.Columns["SubmitterName"]?.Index ?? -1;
            int statusIdx = _gridRequests.Columns["Status"]?.Index ?? -1;

            if (e.ColumnIndex == custIdx || e.ColumnIndex == subIdx)
            {
                string text = e.Value?.ToString() ?? string.Empty;
                UiGridHelper.PaintAvatarCell(_gridRequests, e, text);
            }
            else if (e.ColumnIndex == statusIdx)
            {
                string text = e.Value?.ToString() ?? string.Empty;
                UiGridHelper.PaintStatusText(_gridRequests, e, text);
            }
        }

        private void GridRequests_CellContentClick(object? sender, DataGridViewCellEventArgs e)
        {
            int actionIdx = _gridRequests.Columns["Actions"]?.Index ?? -1;
            if (e.RowIndex < 0 || e.ColumnIndex != actionIdx) return;

            var row = _gridRequests.Rows[e.RowIndex];
            int reqId = Convert.ToInt32(row.Cells["RequestId"].Value);
            var req = _allRequests.FirstOrDefault(r => r.RequestId == reqId);
            if (req == null) return;

            var menu = new ContextMenuStrip();
            menu.Items.Add("🔍 Review / View Details", null, async (s, ev) =>
            {
                var dlg = new RetentionRequestDialog(_retentionController, req);
                dlg.ShowDialog(this);
                if (dlg.WasActionTaken) await LoadRequestsDataAsync();
            });

            var rect = _gridRequests.GetCellDisplayRectangle(e.ColumnIndex, e.RowIndex, true);
            menu.Show(_gridRequests, new Point(rect.Right - 150, rect.Bottom));
        }

        private async Task LoadRequestsDataAsync()
        {
            _gridRequestsSkeleton?.ShowSkeleton();
            try
            {
                string status = _cboRequestStatusFilter.SelectedItem?.ToString() ?? "All";
                DateTime from = _dtpRequestFrom.Value.Date;
                DateTime to = _dtpRequestTo.Value.Date;

                _allRequests = await _retentionController.GetRetentionRequestsAsync(status, from, to);
                _paginationRequests.UpdatePagination(_allRequests.Count, _paginationRequests.CurrentPage, _paginationRequests.PageSize);
                RenderPagedRequests(resetPage: true);

                bool isEmpty = _allRequests.Count == 0;
                _pnlRequestsEmptyState.Visible = isEmpty;
                if (isEmpty) _pnlRequestsEmptyState.BringToFront();
            }
            finally
            {
                _gridRequestsSkeleton?.HideSkeleton();
            }
        }

        private void RenderPagedRequests(bool resetPage = false)
        {
            if (resetPage) _paginationRequests.ResetPage();

            int pSize = _paginationRequests.PageSize;
            int page = _paginationRequests.CurrentPage;

            var paged = _allRequests.Skip((page - 1) * pSize).Take(pSize).ToList();

            _gridRequests.Rows.Clear();
            foreach (var r in paged)
            {
                _gridRequests.Rows.Add(
                    r.RequestId,
                    r.CustomerName,
                    $"{r.SubmitterName} ({r.SubmitterRole})",
                    r.TargetSegment,
                    r.ProposedIncentive,
                    r.ReasonCategory,
                    r.Status,
                    r.CreatedAt.ToString("MMM dd, yyyy")
                );
            }
        }

        #endregion

        #region Common Actions

        private void ShowNewRequestModal()
        {
            var dlg = new RetentionRequestDialog(_retentionController);
            dlg.ShowDialog(this);
            if (dlg.WasActionTaken)
            {
                _ = RefreshCurrentTabAsync();
            }
        }

        private void ExportCurrentViewToCsv()
        {
            try
            {
                using var sfd = new SaveFileDialog
                {
                    Filter = "CSV Files (*.csv)|*.csv",
                    FileName = $"NEXA_Retention_Roster_{DateTime.Now:yyyyMMdd_HHmm}.csv"
                };

                if (sfd.ShowDialog() == DialogResult.OK)
                {
                    _retentionController.ExportRetentionToCsv(_allClients, sfd.FileName);
                    MessageBox.Show($"Retention roster exported successfully to {sfd.FileName}", "Export Completed", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Export failed: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private async Task LoadInitialDataAsync()
        {
            await RefreshCurrentTabAsync();
        }

        #endregion

        public class TemplateItemModel
        {
            public int TemplateId { get; set; }
            public string Name { get; set; } = string.Empty;
            public string Category { get; set; } = string.Empty;
            public string TargetAudience { get; set; } = "All";
            public string Subject { get; set; } = string.Empty;
            public string Body { get; set; } = string.Empty;
            public string CallToActionText { get; set; } = string.Empty;
            public bool IsAutomatedValuation { get; set; }
            public bool IsActive { get; set; } = true;
            public bool IsSystem { get; set; } = false;
        }

        private class OutreachComboItem
        {
            public RetentionCustomerRow Customer { get; }
            public OutreachComboItem(RetentionCustomerRow cust) => Customer = cust;
            public override string ToString() => $"{Customer.FullName} ({Customer.Email}) - {Customer.CurrentSegment}";
        }
    }
}
