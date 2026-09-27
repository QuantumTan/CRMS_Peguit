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

        // Top Header
        private Panel _pnlHeader = null!;
        private Label _lblTitle = null!;
        private Label _lblSubtitle = null!;
        private Button _btnRefresh = null!;
        private Button _btnExportCsv = null!;
        private Button _btnNewRequest = null!;

        // Tab Navigation Strip
        private Panel _pnlTabStrip = null!;
        private readonly List<Button> _tabButtons = new();
        private Button _activeTabButton = null!;
        private Panel _pnlTabContainer = null!;

        // Tab 1: Dashboard & Segments
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
        private DataGridView _gridClients = null!;
        private PaginationControl _paginationClients = null!;
        private List<RetentionCustomerRow> _allClients = new();
        private string _selectedSegmentFilter = "All";

        // Tab 2: 1-to-1 Manual Outreach
        private Panel _tabManualEmail = null!;
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
        private Button _btnSendManualEmail = null!;
        private Button _btnSendTestEmail = null!;
        private RetentionCustomerRow? _selectedOutreachCustomer;
        private List<EmailTemplate> _loadedRetentionTemplates = new();

        // Tab 3: Requests & Approvals
        private Panel _tabRequests = null!;
        private ComboBox _cboRequestStatusFilter = null!;
        private DateTimePicker _dtpRequestFrom = null!;
        private DateTimePicker _dtpRequestTo = null!;
        private Button _btnFilterRequests = null!;
        private DataGridView _gridRequests = null!;
        private PaginationControl _paginationRequests = null!;
        private List<RetentionRequestRow> _allRequests = new();

        // Tab 4: Campaign Queue & Dispatch
        private Panel _tabQueue = null!;
        private ComboBox _cboQueueStatusFilter = null!;
        private DateTimePicker _dtpQueueFrom = null!;
        private DateTimePicker _dtpQueueTo = null!;
        private Button _btnFilterQueue = null!;
        private DataGridView _gridQueue = null!;
        private PaginationControl _paginationQueue = null!;
        private List<RetentionQueueRow> _allQueueItems = new();
        private Panel _pnlQueuePreviewCard = null!;
        private Label _lblQueuePreviewSubject = null!;
        private Label _lblQueuePreviewBody = null!;
        private Button _btnDispatchSelected = null!;
        private RetentionQueueRow? _selectedQueueRow;

        // Tab 5: Automated Valuation & Settings
        private Panel _tabSettings = null!;
        private NumericUpDown _numAppreciation = null!;
        private ComboBox _cboFrequency = null!;
        private TextBox _txtBrokerageName = null!;
        private TextBox _txtCtaText = null!;
        private TextBox _txtSubjectTemplate = null!;
        private Button _btnSaveValuationSettings = null!;
        private AutomatedEmailSettings _currentValuationSettings = null!;

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

            _pnlTabContainer = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Theme.Background
            };
            this.Controls.Add(_pnlTabContainer);

            // Build all 5 tabs
            BuildTab1Segments();
            BuildTab2ManualEmail();
            BuildTab3Requests();
            BuildTab4Queue();
            BuildTab5Settings();

            // Set initial tab
            SwitchTab(0);

            this.ResumeLayout(true);
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
                Dock = DockStyle.Top,
                Height = 80,
                BackColor = Color.White,
                Padding = new Padding(28, 14, 28, 14)
            };

            _lblTitle = new Label
            {
                Text = "Customer Retention & Email Campaigns",
                Font = new Font("Segoe UI", 15f, FontStyle.Bold),
                ForeColor = Color.FromArgb(15, 23, 42),
                Location = new Point(28, 12),
                AutoSize = true
            };

            _lblSubtitle = new Label
            {
                Text = "Lifecycle retention segments, service incentive governance & human-in-the-loop email campaigns",
                Font = new Font("Segoe UI", 9.5f),
                ForeColor = Color.FromArgb(100, 116, 139),
                Location = new Point(28, 44),
                AutoSize = true
            };

            _btnNewRequest = new Button
            {
                Text = "➕ New Request",
                Size = new Size(130, 38),
                Anchor = AnchorStyles.Top | AnchorStyles.Right,
                Location = new Point(this.Width - 410, 20),
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
                Size = new Size(110, 38),
                Anchor = AnchorStyles.Top | AnchorStyles.Right,
                Location = new Point(this.Width - 270, 20),
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
                Size = new Size(95, 38),
                Anchor = AnchorStyles.Top | AnchorStyles.Right,
                Location = new Point(this.Width - 150, 20),
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
            this.Controls.Add(_pnlHeader);
        }

        private void BuildTabStrip()
        {
            _pnlTabStrip = new Panel
            {
                Dock = DockStyle.Top,
                Height = 44,
                BackColor = Color.FromArgb(248, 250, 252),
                Padding = new Padding(28, 0, 28, 0)
            };

            var tabTitles = new[]
            {
                "📊 Dashboard & Segments",
                "✉️ 1-to-1 Manual Outreach",
                "✓ Requests & Approvals",
                "🚀 Campaign Queue & Dispatch",
                "⚙️ Valuation & Templates"
            };

            int x = 28;
            for (int i = 0; i < tabTitles.Length; i++)
            {
                int tabIndex = i;
                var btn = new Button
                {
                    Text = tabTitles[i],
                    Location = new Point(x, 4),
                    Size = new Size(185, 36),
                    FlatStyle = FlatStyle.Flat,
                    BackColor = Color.Transparent,
                    ForeColor = Color.FromArgb(100, 116, 139),
                    Font = new Font("Segoe UI", 9f, FontStyle.Regular),
                    Cursor = Cursors.Hand,
                    TextAlign = ContentAlignment.MiddleCenter
                };
                btn.FlatAppearance.BorderSize = 0;
                btn.Click += (s, e) => SwitchTab(tabIndex);

                _tabButtons.Add(btn);
                _pnlTabStrip.Controls.Add(btn);
                x += 190;
            }

            this.Controls.Add(_pnlTabStrip);
        }

        private void SwitchTab(int index)
        {
            for (int i = 0; i < _tabButtons.Count; i++)
            {
                var b = _tabButtons[i];
                if (i == index)
                {
                    _activeTabButton = b;
                    b.BackColor = Color.White;
                    b.ForeColor = Theme.SidebarAccent;
                    b.Font = new Font("Segoe UI", 9f, FontStyle.Bold);
                }
                else
                {
                    b.BackColor = Color.Transparent;
                    b.ForeColor = Color.FromArgb(100, 116, 139);
                    b.Font = new Font("Segoe UI", 9f, FontStyle.Regular);
                }
            }

            _pnlTabContainer.Controls.Clear();
            Control activeView = index switch
            {
                0 => _tabSegments,
                1 => _tabManualEmail,
                2 => _tabRequests,
                3 => _tabQueue,
                4 => _tabSettings,
                _ => _tabSegments
            };
            activeView.Dock = DockStyle.Fill;
            _pnlTabContainer.Controls.Add(activeView);

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
                    await LoadManualEmailDataAsync();
                }
                else if (idx == 2)
                {
                    await LoadRequestsDataAsync();
                }
                else if (idx == 3)
                {
                    await LoadQueueDataAsync();
                }
                else if (idx == 4)
                {
                    await LoadValuationSettingsDataAsync();
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[ClientRetentionView] Refresh error: {ex.Message}");
            }
        }

        #endregion

        #region Tab 1: Dashboard & Segments

        private void BuildTab1Segments()
        {
            _tabSegments = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Theme.Background,
                Padding = new Padding(28, 16, 28, 16),
                AutoScroll = true
            };

            // 4 KPI Cards Container
            _pnlKpis = new TableLayoutPanel
            {
                Dock = DockStyle.Top,
                Height = 104,
                ColumnCount = 4,
                RowCount = 1,
                BackColor = Color.Transparent,
                Margin = new Padding(0, 0, 0, 14)
            };
            _pnlKpis.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25F));
            _pnlKpis.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25F));
            _pnlKpis.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25F));
            _pnlKpis.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25F));
            _pnlKpis.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));

            _kpiTrackedClients = new KpiCard("TRACKED CLIENTS", "0", Color.FromArgb(15, 23, 42), KpiIconType.Users, "Assigned past clients")
            {
                Dock = DockStyle.Fill,
                Margin = new Padding(0, 0, 8, 0),
                ClickMode = KpiClickMode.InPlaceFilter
            };
            _kpiActiveQueue = new KpiCard("ACTIVE QUEUE", "0", Color.FromArgb(217, 119, 6), KpiIconType.Clock, "Awaiting dispatch")
            {
                Dock = DockStyle.Fill,
                Margin = new Padding(4, 0, 6, 0),
                ClickMode = KpiClickMode.InPlaceFilter
            };
            _kpiPendingApprovals = new KpiCard("PENDING APPROVALS", "0", Color.FromArgb(220, 38, 38), KpiIconType.Briefcase, "Incentive requests pending")
            {
                Dock = DockStyle.Fill,
                Margin = new Padding(6, 0, 4, 0),
                ClickMode = KpiClickMode.InPlaceFilter
            };
            _kpiDispatchedMonth = new KpiCard("DISPATCHED (MONTH)", "0", Color.FromArgb(22, 163, 74), KpiIconType.Currency, "Delivered retention emails")
            {
                Dock = DockStyle.Fill,
                Margin = new Padding(8, 0, 0, 0),
                ClickMode = KpiClickMode.InPlaceFilter
            };

            _kpiTrackedClients.Click += (_, _) => FilterSegmentsByPill("All");
            _kpiActiveQueue.Click += (_, _) => SwitchTab(3);
            _kpiPendingApprovals.Click += (_, _) => SwitchTab(2);

            _pnlKpis.Controls.Add(_kpiTrackedClients, 0, 0);
            _pnlKpis.Controls.Add(_kpiActiveQueue, 1, 0);
            _pnlKpis.Controls.Add(_kpiPendingApprovals, 2, 0);
            _pnlKpis.Controls.Add(_kpiDispatchedMonth, 3, 0);
            _tabSegments.Controls.Add(_pnlKpis);

            // Filter Bar
            _pnlSegmentFilters = new Panel
            {
                Dock = DockStyle.Top,
                Height = 52,
                BackColor = Color.White,
                Padding = new Padding(16, 8, 16, 8),
                Margin = new Padding(0, 14, 0, 14)
            };
            UiRadiusHelper.StyleCard(_pnlSegmentFilters, 8);

            _flpSegmentPills = new FlowLayoutPanel
            {
                Dock = DockStyle.Left,
                Width = 720,
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
                    Margin = new Padding(0, 2, 6, 0)
                };
                pill.FlatAppearance.BorderSize = 0;
                UiRadiusHelper.StyleButton(pill, 16);
                pill.Click += (s, e) => FilterSegmentsByPill(name);
                _flpSegmentPills.Controls.Add(pill);
            }

            _txtSearchClients = new TextBox
            {
                Dock = DockStyle.Right,
                Width = 220,
                Font = new Font("Segoe UI", 9.5f),
                PlaceholderText = "Search by name, email, phone..."
            };
            _txtSearchClients.TextChanged += (s, e) => ApplyClientFilters();

            _btnRecalculateAll = new Button
            {
                Text = "⚡ Recalculate All",
                Dock = DockStyle.Right,
                Width = 130,
                BackColor = Color.White,
                ForeColor = Theme.SidebarAccent,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 8.5f, FontStyle.Bold),
                Cursor = Cursors.Hand,
                Margin = new Padding(0, 0, 10, 0)
            };
            _btnRecalculateAll.FlatAppearance.BorderColor = Theme.SidebarAccent;
            UiRadiusHelper.StyleButton(_btnRecalculateAll, 6);
            _btnRecalculateAll.Click += async (s, e) => await OnRecalculateAllSegmentsAsync();

            _pnlSegmentFilters.Controls.Add(_flpSegmentPills);
            _pnlSegmentFilters.Controls.Add(_btnRecalculateAll);
            _pnlSegmentFilters.Controls.Add(_txtSearchClients);
            _tabSegments.Controls.Add(_pnlSegmentFilters);

            // Clients DataGridView Card
            var pnlGridCard = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.White,
                Padding = new Padding(16),
                Margin = new Padding(0, 14, 0, 0)
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
            pnlGridCard.Controls.Add(_paginationClients);
            _tabSegments.Controls.Add(pnlGridCard);
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

            // Avatar painting for Client Name & Assigned Agent
            int nameIdx = _gridClients.Columns["CustomerName"]?.Index ?? -1;
            int agentIdx = _gridClients.Columns["AssignedAgent"]?.Index ?? -1;
            int segIdx = _gridClients.Columns["Segment"]?.Index ?? -1;
            int coolIdx = _gridClients.Columns["Cooldown"]?.Index ?? -1;

            if (e.ColumnIndex == nameIdx || e.ColumnIndex == agentIdx)
            {
                string text = e.Value?.ToString() ?? string.Empty;
                UiGridHelper.PaintAvatarCell(_gridClients, e, text);
            }
            // StatusText for Retention Segment & Cooldown
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
            menu.Items.Add("✉ Quick Retention Email", null, (s, ev) =>
            {
                _selectedOutreachCustomer = customer;
                SwitchTab(1);
            });

            menu.Items.Add("➕ Create Retention Request", null, async (s, ev) =>
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
            var summary = await _retentionController.GetSummaryAsync();
            _kpiTrackedClients.SetValue(summary.TotalTrackedCustomers);
            _kpiActiveQueue.SetValue(summary.ActiveQueueCount);
            _kpiPendingApprovals.SetValue(summary.PendingApprovalsCount);
            _kpiDispatchedMonth.SetValue(summary.DispatchedThisMonthCount);

            _allClients = await _retentionController.GetCustomersAsync();
            _paginationClients.UpdatePagination(_allClients.Count, _paginationClients.CurrentPage, _paginationClients.PageSize);
            RenderPagedClients(resetPage: true);
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
                _btnRecalculateAll.Text = "⚡ Recalculate All";
            }
        }

        #endregion

        #region Tab 2: 1-to-1 Manual Outreach

        private void BuildTab2ManualEmail()
        {
            _tabManualEmail = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Theme.Background,
                Padding = new Padding(28, 16, 28, 16),
                AutoScroll = true
            };

            var tableSplit = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 2,
                RowCount = 1,
                BackColor = Color.Transparent
            };
            tableSplit.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 45F));
            tableSplit.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 55F));
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
                Width = 360,
                Font = new Font("Segoe UI", 9.5f),
                PlaceholderText = "Search by client name, email, or phone..."
            };
            _txtSearchOutreach.TextChanged += (s, e) => FilterOutreachSearchResults();

            _lstOutreachResults = new ListBox
            {
                Location = new Point(14, 66),
                Width = 360,
                Height = 80,
                Font = new Font("Segoe UI", 9f),
                Visible = false
            };
            _lstOutreachResults.SelectedIndexChanged += (s, e) => OnOutreachClientSelected();

            // Client Info Card
            _pnlOutreachClientCard = new Panel
            {
                Location = new Point(14, 70),
                Width = 360,
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
                Width = 360,
                DropDownStyle = ComboBoxStyle.DropDownList,
                Font = new Font("Segoe UI", 9.5f)
            };
            _cboOutreachTemplate.SelectedIndexChanged += (s, e) => ApplySelectedOutreachTemplate();

            // Service Incentive
            var lblInc = new Label { Text = "Proposed Incentive (Service-Based):", Font = new Font("Segoe UI", 9f, FontStyle.Bold), Location = new Point(14, 250), AutoSize = true };
            _txtOutreachIncentive = new TextBox
            {
                Location = new Point(14, 272),
                Width = 360,
                Font = new Font("Segoe UI", 9.5f)
            };
            _txtOutreachIncentive.TextChanged += (s, e) => UpdateOutreachPreview();

            // Subject
            var lblSubj = new Label { Text = "Subject *", Font = new Font("Segoe UI", 9f, FontStyle.Bold), Location = new Point(14, 308), AutoSize = true };
            _txtOutreachSubject = new TextBox
            {
                Location = new Point(14, 330),
                Width = 360,
                Font = new Font("Segoe UI", 9.5f)
            };
            _txtOutreachSubject.TextChanged += (s, e) => UpdateOutreachPreview();

            // Body
            var lblBody = new Label { Text = "Email Message Body *", Font = new Font("Segoe UI", 9f, FontStyle.Bold), Location = new Point(14, 366), AutoSize = true };
            _txtOutreachBody = new TextBox
            {
                Location = new Point(14, 388),
                Width = 360,
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
                Location = new Point(14, 550),
                Size = new Size(240, 40),
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
                Location = new Point(265, 550),
                Size = new Size(110, 40),
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

            // Right Card: Live Preview
            _pnlOutreachPreview = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.White,
                Padding = new Padding(24),
                Margin = new Padding(10, 0, 0, 0),
                AutoScroll = true
            };
            UiRadiusHelper.StyleCard(_pnlOutreachPreview, 10);

            var lblPrevHeader = new Label
            {
                Text = "Live Formatted Email Preview",
                Font = new Font("Segoe UI", 12f, FontStyle.Bold),
                ForeColor = Color.FromArgb(15, 23, 42),
                Location = new Point(20, 16),
                AutoSize = true
            };

            _lblPreviewTo = new Label
            {
                Text = "To: (Recipient will appear here)",
                Font = new Font("Segoe UI", 9f),
                ForeColor = Color.FromArgb(71, 85, 105),
                Location = new Point(20, 48),
                Size = new Size(500, 20)
            };

            _lblPreviewSubject = new Label
            {
                Text = "Subject: (Subject will appear here)",
                Font = new Font("Segoe UI", 10.5f, FontStyle.Bold),
                ForeColor = Color.FromArgb(15, 23, 42),
                Location = new Point(20, 74),
                Size = new Size(500, 24)
            };

            var pnlDivider = new Panel
            {
                Location = new Point(20, 104),
                Size = new Size(500, 1),
                BackColor = Color.FromArgb(226, 232, 240)
            };

            _lblPreviewBody = new Label
            {
                Text = "Select a client and template to preview the personalized outreach message.",
                Font = new Font("Segoe UI", 10f),
                ForeColor = Color.FromArgb(30, 41, 59),
                Location = new Point(20, 116),
                Size = new Size(500, 420)
            };

            _pnlOutreachPreview.Controls.Add(lblPrevHeader);
            _pnlOutreachPreview.Controls.Add(_lblPreviewTo);
            _pnlOutreachPreview.Controls.Add(_lblPreviewSubject);
            _pnlOutreachPreview.Controls.Add(pnlDivider);
            _pnlOutreachPreview.Controls.Add(_lblPreviewBody);
            tableSplit.Controls.Add(_pnlOutreachPreview, 1, 0);

            _tabManualEmail.Controls.Add(tableSplit);
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

            // Auto-select template matching segment
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
            _txtOutreachBody.Text = tpl.Body;

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

            // Anti-fatigue check
            if (_selectedOutreachCustomer.IsOnCooldown)
            {
                var overrideDlg = new CooldownOverrideDialog(
                    _selectedOutreachCustomer.FullName,
                    _selectedOutreachCustomer.LastRetentionEmailSentAt,
                    _selectedOutreachCustomer.DaysUntilCooldownExpires);

                if (overrideDlg.ShowDialog(this) != DialogResult.OK)
                {
                    return; // Cancelled
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
                await LoadManualEmailDataAsync();
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

        #region Tab 3: Requests & Approvals

        private void BuildTab3Requests()
        {
            _tabRequests = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Theme.Background,
                Padding = new Padding(28, 16, 28, 16),
                AutoScroll = true
            };

            // Top Filter Strip
            var pnlTop = new Panel
            {
                Dock = DockStyle.Top,
                Height = 52,
                BackColor = Color.White,
                Padding = new Padding(16, 8, 16, 8),
                Margin = new Padding(0, 0, 0, 14)
            };
            UiRadiusHelper.StyleCard(pnlTop, 8);

            var lblStatus = new Label { Text = "Status:", Font = new Font("Segoe UI", 9f, FontStyle.Bold), Location = new Point(14, 14), AutoSize = true };
            _cboRequestStatusFilter = new ComboBox
            {
                Location = new Point(70, 10),
                Width = 120,
                DropDownStyle = ComboBoxStyle.DropDownList,
                Font = new Font("Segoe UI", 9f)
            };
            _cboRequestStatusFilter.Items.AddRange(new object[] { "All", "Pending", "Approved", "Rejected" });
            _cboRequestStatusFilter.SelectedIndex = 0;

            var lblDate = new Label { Text = "From:", Font = new Font("Segoe UI", 9f, FontStyle.Bold), Location = new Point(210, 14), AutoSize = true };
            _dtpRequestFrom = new DateTimePicker
            {
                Location = new Point(255, 10),
                Width = 110,
                Format = DateTimePickerFormat.Short,
                Value = DateTime.Today.AddDays(-60)
            };

            var lblTo = new Label { Text = "To:", Font = new Font("Segoe UI", 9f, FontStyle.Bold), Location = new Point(375, 14), AutoSize = true };
            _dtpRequestTo = new DateTimePicker
            {
                Location = new Point(405, 10),
                Width = 110,
                Format = DateTimePickerFormat.Short,
                Value = DateTime.Today
            };

            _btnFilterRequests = new Button
            {
                Text = "Filter",
                Location = new Point(530, 9),
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

            pnlTop.Controls.Add(lblStatus);
            pnlTop.Controls.Add(_cboRequestStatusFilter);
            pnlTop.Controls.Add(lblDate);
            pnlTop.Controls.Add(_dtpRequestFrom);
            pnlTop.Controls.Add(lblTo);
            pnlTop.Controls.Add(_dtpRequestTo);
            pnlTop.Controls.Add(_btnFilterRequests);
            _tabRequests.Controls.Add(pnlTop);

            // Requests Grid Card
            var pnlGridCard = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.White,
                Padding = new Padding(16),
                Margin = new Padding(0, 14, 0, 0)
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

            pnlGridCard.Controls.Add(_gridRequests);
            pnlGridCard.Controls.Add(_paginationRequests);
            _tabRequests.Controls.Add(pnlGridCard);
        }

        private void ConfigureRequestsGridColumns()
        {
            _gridRequests.Columns.Clear();

            _gridRequests.Columns.Add(new DataGridViewTextBoxColumn { Name = "RequestId", HeaderText = "REQ #", FillWeight = 35 });
            _gridRequests.Columns.Add(new DataGridViewTextBoxColumn { Name = "CustomerName", HeaderText = "Client Name", FillWeight = 110 });
            _gridRequests.Columns.Add(new DataGridViewTextBoxColumn { Name = "SubmitterName", HeaderText = "Submitted By", FillWeight = 100 });
            _gridRequests.Columns.Add(new DataGridViewTextBoxColumn { Name = "TargetSegment", HeaderText = "Segment", FillWeight = 90 });
            _gridRequests.Columns.Add(new DataGridViewTextBoxColumn { Name = "ProposedIncentive", HeaderText = "Proposed Incentive", FillWeight = 130 });
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
            string status = _cboRequestStatusFilter.SelectedItem?.ToString() ?? "All";
            DateTime from = _dtpRequestFrom.Value.Date;
            DateTime to = _dtpRequestTo.Value.Date;

            _allRequests = await _retentionController.GetRetentionRequestsAsync(status, from, to);
            _paginationRequests.UpdatePagination(_allRequests.Count, _paginationRequests.CurrentPage, _paginationRequests.PageSize);
            RenderPagedRequests(resetPage: true);
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

        #region Tab 4: Campaign Queue & Dispatch

        private void BuildTab4Queue()
        {
            _tabQueue = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Theme.Background,
                Padding = new Padding(28, 16, 28, 16),
                AutoScroll = true
            };

            // Top Filter Strip
            var pnlTop = new Panel
            {
                Dock = DockStyle.Top,
                Height = 52,
                BackColor = Color.White,
                Padding = new Padding(16, 8, 16, 8),
                Margin = new Padding(0, 0, 0, 14)
            };
            UiRadiusHelper.StyleCard(pnlTop, 8);

            var lblStatus = new Label { Text = "Status:", Font = new Font("Segoe UI", 9f, FontStyle.Bold), Location = new Point(14, 14), AutoSize = true };
            _cboQueueStatusFilter = new ComboBox
            {
                Location = new Point(70, 10),
                Width = 120,
                DropDownStyle = ComboBoxStyle.DropDownList,
                Font = new Font("Segoe UI", 9f)
            };
            _cboQueueStatusFilter.Items.AddRange(new object[] { "All", "Queued", "Dispatched", "Failed" });
            _cboQueueStatusFilter.SelectedIndex = 0;

            var lblDate = new Label { Text = "From:", Font = new Font("Segoe UI", 9f, FontStyle.Bold), Location = new Point(210, 14), AutoSize = true };
            _dtpQueueFrom = new DateTimePicker
            {
                Location = new Point(255, 10),
                Width = 110,
                Format = DateTimePickerFormat.Short,
                Value = DateTime.Today.AddDays(-60)
            };

            var lblTo = new Label { Text = "To:", Font = new Font("Segoe UI", 9f, FontStyle.Bold), Location = new Point(375, 14), AutoSize = true };
            _dtpQueueTo = new DateTimePicker
            {
                Location = new Point(405, 10),
                Width = 110,
                Format = DateTimePickerFormat.Short,
                Value = DateTime.Today
            };

            _btnFilterQueue = new Button
            {
                Text = "Filter",
                Location = new Point(530, 9),
                Size = new Size(80, 32),
                BackColor = Theme.SidebarAccent,
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 8.5f, FontStyle.Bold),
                Cursor = Cursors.Hand
            };
            _btnFilterQueue.FlatAppearance.BorderSize = 0;
            UiRadiusHelper.StyleButton(_btnFilterQueue, 6);
            _btnFilterQueue.Click += async (s, e) => await LoadQueueDataAsync();

            pnlTop.Controls.Add(lblStatus);
            pnlTop.Controls.Add(_cboQueueStatusFilter);
            pnlTop.Controls.Add(lblDate);
            pnlTop.Controls.Add(_dtpQueueFrom);
            pnlTop.Controls.Add(lblTo);
            pnlTop.Controls.Add(_dtpQueueTo);
            pnlTop.Controls.Add(_btnFilterQueue);
            _tabQueue.Controls.Add(pnlTop);

            var splitQueue = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 2,
                RowCount = 1,
                BackColor = Color.Transparent,
                Margin = new Padding(0, 14, 0, 0)
            };
            splitQueue.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 60F));
            splitQueue.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 40F));
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

            pnlGridCard.Controls.Add(_gridQueue);
            pnlGridCard.Controls.Add(_paginationQueue);
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
            _lblQueuePreviewSubject = new Label { Text = "Select a queue entry to review and dispatch", Font = new Font("Segoe UI", 10f, FontStyle.Bold), Location = new Point(14, 46), Size = new Size(340, 24) };
            _lblQueuePreviewBody = new Label { Text = "", Font = new Font("Segoe UI", 9.5f), Location = new Point(14, 76), Size = new Size(340, 360) };

            _btnDispatchSelected = new Button
            {
                Text = "🚀 Dispatch Retention Email Now",
                Location = new Point(14, 450),
                Size = new Size(260, 42),
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
            _pnlQueuePreviewCard.Controls.Add(_lblQueuePreviewBody);
            _pnlQueuePreviewCard.Controls.Add(_btnDispatchSelected);
            splitQueue.Controls.Add(_pnlQueuePreviewCard, 1, 0);

            _tabQueue.Controls.Add(splitQueue);
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
                _lblQueuePreviewSubject.Text = "Select a queue entry to review and dispatch";
                _lblQueuePreviewBody.Text = string.Empty;
                return;
            }

            int logId = Convert.ToInt32(_gridQueue.CurrentRow.Cells["EmailLogId"].Value);
            _selectedQueueRow = _allQueueItems.FirstOrDefault(q => q.EmailLogId == logId);
            if (_selectedQueueRow != null)
            {
                _lblQueuePreviewSubject.Text = _selectedQueueRow.Subject;
                _lblQueuePreviewBody.Text = $"To: {_selectedQueueRow.CustomerName} <{_selectedQueueRow.RecipientEmail}>\n" +
                                            $"Segment: {_selectedQueueRow.Segment}\n" +
                                            $"Incentive: {_selectedQueueRow.IncentiveOffered ?? "None"}\n\n" +
                                            $"{_selectedQueueRow.Body}";

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
            string status = _cboQueueStatusFilter.SelectedItem?.ToString() ?? "All";
            DateTime from = _dtpQueueFrom.Value.Date;
            DateTime to = _dtpQueueTo.Value.Date;

            _allQueueItems = await _retentionController.GetCampaignQueueAsync(status, from, to);
            _paginationQueue.UpdatePagination(_allQueueItems.Count, _paginationQueue.CurrentPage, _paginationQueue.PageSize);
            RenderPagedQueue(resetPage: true);
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

        #region Tab 5: Automated Valuation & Templates

        private void BuildTab5Settings()
        {
            _tabSettings = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Theme.Background,
                Padding = new Padding(28, 16, 28, 16),
                AutoScroll = true
            };

            var card = new Panel
            {
                Location = new Point(20, 16),
                Size = new Size(740, 520),
                BackColor = Color.White,
                Padding = new Padding(24)
            };
            UiRadiusHelper.StyleCard(card, 10);

            var lblT = new Label { Text = "Property Appreciation & Client Care Settings", Font = new Font("Segoe UI", 12f, FontStyle.Bold), Location = new Point(20, 16), AutoSize = true };
            var lblSub = new Label { Text = "Preserves automated real-estate appreciation math and benchmark market valuation reporting", Font = new Font("Segoe UI", 9f), ForeColor = Color.FromArgb(100, 116, 139), Location = new Point(20, 42), AutoSize = true };

            int y = 80;

            var lblRate = new Label { Text = "Annual Appreciation Benchmark (%):", Font = new Font("Segoe UI", 9f, FontStyle.Bold), Location = new Point(20, y), AutoSize = true };
            _numAppreciation = new NumericUpDown
            {
                Location = new Point(20, y + 24),
                Width = 140,
                DecimalPlaces = 2,
                Minimum = 0.1m,
                Maximum = 30.0m,
                Value = 5.0m,
                Font = new Font("Segoe UI", 9.5f)
            };

            var lblFreq = new Label { Text = "Outreach Cadence (Days):", Font = new Font("Segoe UI", 9f, FontStyle.Bold), Location = new Point(200, y), AutoSize = true };
            _cboFrequency = new ComboBox
            {
                Location = new Point(200, y + 24),
                Width = 160,
                DropDownStyle = ComboBoxStyle.DropDownList,
                Font = new Font("Segoe UI", 9.5f)
            };
            _cboFrequency.Items.AddRange(new object[] { "90 Days (Quarterly)", "180 Days (Semi-Annual)", "365 Days (Annual)" });
            _cboFrequency.SelectedIndex = 1;

            y += 70;

            var lblBroker = new Label { Text = "Brokerage Display Name:", Font = new Font("Segoe UI", 9f, FontStyle.Bold), Location = new Point(20, y), AutoSize = true };
            _txtBrokerageName = new TextBox
            {
                Location = new Point(20, y + 24),
                Width = 660,
                Font = new Font("Segoe UI", 9.5f),
                Text = "NEXA Real Estate Advisory"
            };

            y += 66;

            var lblCta = new Label { Text = "Valuation Call to Action:", Font = new Font("Segoe UI", 9f, FontStyle.Bold), Location = new Point(20, y), AutoSize = true };
            _txtCtaText = new TextBox
            {
                Location = new Point(20, y + 24),
                Width = 660,
                Font = new Font("Segoe UI", 9.5f),
                Text = "Schedule a Complimentary Equity Consultation"
            };

            y += 66;

            var lblSubjTpl = new Label { Text = "Subject Template:", Font = new Font("Segoe UI", 9f, FontStyle.Bold), Location = new Point(20, y), AutoSize = true };
            _txtSubjectTemplate = new TextBox
            {
                Location = new Point(20, y + 24),
                Width = 660,
                Font = new Font("Segoe UI", 9.5f),
                Text = "Market Valuation & Equity Report for {PropertyAddress}"
            };

            y += 66;

            _btnSaveValuationSettings = new Button
            {
                Text = "Save Benchmark Settings",
                Location = new Point(20, y + 24),
                Size = new Size(200, 38),
                BackColor = Theme.SidebarAccent,
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 9f, FontStyle.Bold),
                Cursor = Cursors.Hand
            };
            _btnSaveValuationSettings.FlatAppearance.BorderSize = 0;
            UiRadiusHelper.StyleButton(_btnSaveValuationSettings, 8);
            _btnSaveValuationSettings.Click += async (s, e) => await OnSaveValuationSettingsAsync();

            card.Controls.Add(lblT);
            card.Controls.Add(lblSub);
            card.Controls.Add(lblRate);
            card.Controls.Add(_numAppreciation);
            card.Controls.Add(lblFreq);
            card.Controls.Add(_cboFrequency);
            card.Controls.Add(lblBroker);
            card.Controls.Add(_txtBrokerageName);
            card.Controls.Add(lblCta);
            card.Controls.Add(_txtCtaText);
            card.Controls.Add(lblSubjTpl);
            card.Controls.Add(_txtSubjectTemplate);
            card.Controls.Add(_btnSaveValuationSettings);

            _tabSettings.Controls.Add(card);
        }

        private async Task LoadValuationSettingsDataAsync()
        {
            using var db = LocalDb.CreateContext(CurrentSession.TenantId);
            _currentValuationSettings = await db.AutomatedEmailSettings.FirstOrDefaultAsync(s => s.TenantId == CurrentSession.TenantId)
                ?? new AutomatedEmailSettings { TenantId = CurrentSession.TenantId };

            _numAppreciation.Value = Math.Max(0.1m, Math.Min(30m, _currentValuationSettings.AnnualAppreciationRatePercent));
            _txtBrokerageName.Text = _currentValuationSettings.BrokerageName;
            _txtCtaText.Text = _currentValuationSettings.CallToActionText;
            _txtSubjectTemplate.Text = _currentValuationSettings.SubjectTemplate;
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

                settings.AnnualAppreciationRatePercent = _numAppreciation.Value;
                settings.BrokerageName = _txtBrokerageName.Text.Trim();
                settings.CallToActionText = _txtCtaText.Text.Trim();
                settings.SubjectTemplate = _txtSubjectTemplate.Text.Trim();
                settings.UpdatedAt = DateTime.UtcNow;

                await db.SaveChangesAsync();
                MessageBox.Show("Benchmark settings updated successfully.", "Saved", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to save settings: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
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

        private class OutreachComboItem
        {
            public RetentionCustomerRow Customer { get; }
            public OutreachComboItem(RetentionCustomerRow cust) => Customer = cust;
            public override string ToString() => $"{Customer.FullName} ({Customer.Email}) - {Customer.CurrentSegment}";
        }
    }
}
