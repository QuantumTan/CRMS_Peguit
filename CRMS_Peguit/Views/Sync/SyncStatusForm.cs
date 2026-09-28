using System;
using System.Drawing;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using CRMS_Peguit.domain.entities;
using CRMS_Peguit.winforms.Auth;
using CRMS_Peguit.winforms.Models.Services;
using CRMS_Peguit.winforms.Services.Offline;

namespace CRMS_Peguit.winforms.Views.Sync
{
    public partial class SyncStatusForm : Form
    {
        private DataGridView _grid = null!;
        private Label _lblStatusBadge = null!;
        private Label _lblPendingCount = null!;
        private Label _lblConflictCount = null!;
        private Label _lblFailedCount = null!;
        private Label _lblSyncedCount = null!;
        private Button _btnSyncNow = null!;
        private Button _btnRetryAll = null!;
        private Button _btnClearSynced = null!;
        private string _activeFilter = "All";

        public SyncStatusForm()
        {
            InitializeComponent();
            BindEvents();
            LoadData();
        }

        private void InitializeComponent()
        {
            this.Text = "NEXA — Local-to-Cloud Data Synchronization Status";
            this.Size = new Size(1040, 680);
            this.MinimumSize = new Size(960, 560);
            this.StartPosition = FormStartPosition.CenterParent;
            this.BackColor = Color.FromArgb(248, 250, 252);
            this.Font = new Font("Segoe UI", 9.5F, FontStyle.Regular);

            // ==========================================================
            // TOP HEADER
            // ==========================================================
            var pnlTop = new Panel
            {
                Dock = DockStyle.Top,
                Height = 120,
                BackColor = Color.White,
                Padding = new Padding(24, 16, 24, 16)
            };

            var lblTitle = new Label
            {
                Text = "Data Synchronization Queue",
                Font = new Font("Segoe UI", 14F, FontStyle.Bold),
                ForeColor = Color.FromArgb(15, 23, 42),
                AutoSize = true,
                Location = new Point(24, 16)
            };

            _lblStatusBadge = new Label
            {
                Text = SyncService.Instance.IsOnline ? "● Cloud Connected (Online)" : "● Disconnected (Offline)",
                Font = new Font("Segoe UI", 9F, FontStyle.Bold),
                ForeColor = SyncService.Instance.IsOnline ? Color.FromArgb(22, 163, 74) : Color.FromArgb(220, 38, 38),
                BackColor = SyncService.Instance.IsOnline ? Color.FromArgb(240, 253, 244) : Color.FromArgb(254, 242, 242),
                Padding = new Padding(8, 4, 8, 4),
                AutoSize = true,
                Location = new Point(310, 18),
                Cursor = Cursors.Hand
            };
            _lblStatusBadge.Click += async (s, e) =>
            {
                _lblStatusBadge.Text = "● Checking...";
                bool isOnline = await SyncService.Instance.CheckConnectivityAsync();
                _lblStatusBadge.Text = isOnline ? "● Cloud Connected (Online)" : "● Disconnected (Offline)";
                _lblStatusBadge.ForeColor = isOnline ? Color.FromArgb(22, 163, 74) : Color.FromArgb(220, 38, 38);
                _lblStatusBadge.BackColor = isOnline ? Color.FromArgb(240, 253, 244) : Color.FromArgb(254, 242, 242);
            };

            // 4 Mini Metric Pills
            var pnlMetrics = new FlowLayoutPanel
            {
                Location = new Point(24, 56),
                Size = new Size(620, 48),
                BackColor = Color.Transparent,
                FlowDirection = FlowDirection.LeftToRight,
                WrapContents = false
            };

            _lblPendingCount = CreateMetricBadge("Pending", "0", Color.FromArgb(37, 99, 235), Color.FromArgb(239, 246, 255));
            _lblConflictCount = CreateMetricBadge("Conflicts", "0", Color.FromArgb(217, 119, 6), Color.FromArgb(254, 243, 199));
            _lblFailedCount = CreateMetricBadge("Failed", "0", Color.FromArgb(220, 38, 38), Color.FromArgb(254, 242, 242));
            _lblSyncedCount = CreateMetricBadge("Synced", "0", Color.FromArgb(22, 163, 74), Color.FromArgb(240, 253, 244));

            pnlMetrics.Controls.AddRange(new Control[] { _lblPendingCount, _lblConflictCount, _lblFailedCount, _lblSyncedCount });

            // Action Buttons in Header
            _btnSyncNow = new Button
            {
                Text = "⚡ Sync Now",
                Size = new Size(110, 36),
                Location = new Point(640, 56),
                BackColor = Color.FromArgb(37, 99, 235),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 9F, FontStyle.Bold),
                Cursor = Cursors.Hand,
                Anchor = AnchorStyles.Top | AnchorStyles.Right
            };
            _btnSyncNow.FlatAppearance.BorderSize = 0;

            _btnRetryAll = new Button
            {
                Text = "🔁 Retry Failed",
                Size = new Size(110, 36),
                Location = new Point(760, 56),
                BackColor = Color.FromArgb(241, 245, 249),
                ForeColor = Color.FromArgb(15, 23, 42),
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 9F, FontStyle.Regular),
                Cursor = Cursors.Hand,
                Anchor = AnchorStyles.Top | AnchorStyles.Right
            };
            _btnRetryAll.FlatAppearance.BorderSize = 0;

            _btnClearSynced = new Button
            {
                Text = "🧹 Clear Synced",
                Size = new Size(110, 36),
                Location = new Point(880, 56),
                BackColor = Color.FromArgb(241, 245, 249),
                ForeColor = Color.FromArgb(100, 116, 139),
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 9F, FontStyle.Regular),
                Cursor = Cursors.Hand,
                Anchor = AnchorStyles.Top | AnchorStyles.Right
            };
            _btnClearSynced.FlatAppearance.BorderSize = 0;

            pnlTop.Controls.AddRange(new Control[] { lblTitle, _lblStatusBadge, pnlMetrics, _btnSyncNow, _btnRetryAll, _btnClearSynced });

            pnlTop.Resize += (_, _) =>
            {
                int rightEdge = pnlTop.Width - 24;
                _btnClearSynced.Location = new Point(rightEdge - _btnClearSynced.Width, 56);
                _btnRetryAll.Location = new Point(_btnClearSynced.Left - _btnRetryAll.Width - 10, 56);
                _btnSyncNow.Location = new Point(_btnRetryAll.Left - _btnSyncNow.Width - 10, 56);
            };

            // ==========================================================
            // FILTER BAR
            // ==========================================================
            var pnlFilter = new Panel
            {
                Dock = DockStyle.Top,
                Height = 44,
                BackColor = Color.FromArgb(241, 245, 249),
                Padding = new Padding(24, 6, 24, 6)
            };

            var btnFilterAll = CreateFilterTab("All");
            var btnFilterPending = CreateFilterTab("Pending");
            var btnFilterConflict = CreateFilterTab("Conflict");
            var btnFilterFailed = CreateFilterTab("Failed");
            var btnFilterSynced = CreateFilterTab("Synced");

            pnlFilter.Controls.AddRange(new Control[] { btnFilterAll, btnFilterPending, btnFilterConflict, btnFilterFailed, btnFilterSynced });

            // ==========================================================
            // DATA GRID
            // ==========================================================
            var pnlGrid = new Panel
            {
                Dock = DockStyle.Fill,
                Padding = new Padding(24, 16, 24, 16),
                BackColor = Color.FromArgb(248, 250, 252)
            };

            _grid = new DataGridView
            {
                Dock = DockStyle.Fill,
                BackgroundColor = Color.White,
                BorderStyle = BorderStyle.None,
                CellBorderStyle = DataGridViewCellBorderStyle.SingleHorizontal,
                GridColor = Color.FromArgb(226, 232, 240),
                AllowUserToAddRows = false,
                AllowUserToDeleteRows = false,
                ReadOnly = true,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                RowHeadersVisible = false,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                RowTemplate = { Height = 40 }
            };

            ConfigureGridColumns();

            var pnlEmptyState = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.White,
                Visible = false,
                Name = "pnlEmptyState"
            };

            var lblEmptyIcon = new Label
            {
                Text = "✅",
                Font = new Font("Segoe UI", 42f),
                TextAlign = ContentAlignment.MiddleCenter,
                ForeColor = Color.FromArgb(22, 163, 74),
                Dock = DockStyle.None,
                AutoSize = true
            };
            lblEmptyIcon.Location = new Point((pnlEmptyState.Width - lblEmptyIcon.Width) / 2, 80);
            lblEmptyIcon.Anchor = AnchorStyles.None;

            var lblEmptyTitle = new Label
            {
                Text = "All data is synced",
                Font = new Font("Segoe UI", 14f, FontStyle.Bold),
                ForeColor = Color.FromArgb(15, 23, 42),
                TextAlign = ContentAlignment.MiddleCenter,
                Dock = DockStyle.None,
                AutoSize = true
            };

            var lblEmptySubtitle = new Label
            {
                Text = "No pending, failed, or conflicted records.\r\nYour local database is fully in sync with the cloud.",
                Font = new Font("Segoe UI", 10f),
                ForeColor = Color.FromArgb(100, 116, 139),
                TextAlign = ContentAlignment.MiddleCenter,
                Dock = DockStyle.None,
                AutoSize = true
            };

            pnlEmptyState.Controls.Add(lblEmptyIcon);
            pnlEmptyState.Controls.Add(lblEmptyTitle);
            pnlEmptyState.Controls.Add(lblEmptySubtitle);

            pnlEmptyState.SizeChanged += (s, e) =>
            {
                int cx = pnlEmptyState.ClientSize.Width / 2;
                int startY = pnlEmptyState.ClientSize.Height / 2 - 90;
                lblEmptyIcon.Location = new Point(cx - lblEmptyIcon.Width / 2, startY);
                lblEmptyTitle.Location = new Point(cx - lblEmptyTitle.Width / 2, lblEmptyIcon.Bottom + 10);
                lblEmptySubtitle.Location = new Point(cx - lblEmptySubtitle.Width / 2, lblEmptyTitle.Bottom + 8);
            };

            pnlGrid.Controls.Add(pnlEmptyState);
            pnlGrid.Controls.Add(_grid);

            // ==========================================================
            // BOTTOM TOOLBAR
            // ==========================================================
            var pnlBottom = new Panel
            {
                Dock = DockStyle.Bottom,
                Height = 52,
                BackColor = Color.White,
                Padding = new Padding(24, 8, 24, 8)
            };

            var lblTip = new Label
            {
                Text = "💡 Tip: Double-click an item with Conflict to open the side-by-side comparison modal and choose which version to keep.",
                Font = new Font("Segoe UI", 8.5F, FontStyle.Italic),
                ForeColor = Color.FromArgb(100, 116, 139),
                AutoSize = true,
                Location = new Point(24, 16)
            };

            var btnClose = new Button
            {
                Text = "Close",
                Size = new Size(90, 34),
                Anchor = AnchorStyles.Top | AnchorStyles.Right,
                Location = new Point(886, 9),
                BackColor = Color.FromArgb(241, 245, 249),
                ForeColor = Color.FromArgb(15, 23, 42),
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand
            };
            btnClose.FlatAppearance.BorderSize = 0;
            btnClose.Click += (_, _) => this.Close();

            pnlBottom.Controls.AddRange(new Control[] { lblTip, btnClose });

            this.CancelButton = btnClose;
            this.AcceptButton = _btnSyncNow;
            this.MinimumSize = new Size(860, 560);

            this.Controls.Add(pnlGrid);
            this.Controls.Add(pnlFilter);
            this.Controls.Add(pnlTop);
            this.Controls.Add(pnlBottom);
        }

        private static Label CreateMetricBadge(string label, string count, Color fore, Color back)
        {
            return new Label
            {
                Text = $"{label}: {count}",
                Font = new Font("Segoe UI", 9F, FontStyle.Bold),
                ForeColor = fore,
                BackColor = back,
                Padding = new Padding(10, 6, 10, 6),
                Margin = new Padding(0, 0, 10, 0),
                AutoSize = true
            };
        }

        private Button CreateFilterTab(string text)
        {
            var btn = new Button
            {
                Text = text,
                Dock = DockStyle.Left,
                Width = 90,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 9F, FontStyle.Regular),
                ForeColor = text == "All" ? Color.FromArgb(37, 99, 235) : Color.FromArgb(100, 116, 139),
                BackColor = text == "All" ? Color.White : Color.Transparent,
                Cursor = Cursors.Hand
            };
            btn.FlatAppearance.BorderSize = 0;
            btn.Click += (s, e) =>
            {
                _activeFilter = text;
                if (btn.Parent != null)
                {
                    foreach (Control c in btn.Parent.Controls)
                    {
                        if (c is Button b)
                        {
                            b.BackColor = b == btn ? Color.White : Color.Transparent;
                            b.ForeColor = b == btn ? Color.FromArgb(37, 99, 235) : Color.FromArgb(100, 116, 139);
                            b.Font = new Font("Segoe UI", 9F, b == btn ? FontStyle.Bold : FontStyle.Regular);
                        }
                    }
                }
                LoadData();
            };
            return btn;
        }

        private void ConfigureGridColumns()
        {
            _grid.Columns.Clear();

            _grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "QueueId", HeaderText = "ID", FillWeight = 35 });
            _grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "EntityType", HeaderText = "Entity", FillWeight = 65 });
            _grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "Operation", HeaderText = "Operation", FillWeight = 50 });
            _grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "LocalId", HeaderText = "Local/Server ID", FillWeight = 85 });
            _grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "Status", HeaderText = "Status", FillWeight = 60 });
            _grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "CreatedAt", HeaderText = "Queued At", FillWeight = 75 });
            _grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "Attempts", HeaderText = "Attempts", FillWeight = 45 });
            _grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "Details", HeaderText = "Failure / Conflict Details", FillWeight = 160 });

            var colAction = new DataGridViewButtonColumn
            {
                Name = "Action",
                HeaderText = "Action",
                Text = "Resolve / Retry",
                UseColumnTextForButtonValue = false,
                FillWeight = 85
            };
            _grid.Columns.Add(colAction);
        }

        private void BindEvents()
        {
            _btnSyncNow.Click += async (_, _) =>
            {
                _btnSyncNow.Enabled = false;
                _btnSyncNow.Text = "Syncing...";
                try
                {
                    await SyncService.Instance.SyncAsync(waitIfBusy: true, isFullSync: true);
                }
                finally
                {
                    _btnSyncNow.Enabled = true;
                    _btnSyncNow.Text = "⚡ Sync Now";
                    LoadData();
                }
            };

            _btnRetryAll.Click += async (_, _) =>
            {
                await SyncService.Instance.RetryAllFailedAsync();
                LoadData();
            };

            _btnClearSynced.Click += (_, _) =>
            {
                int tenantId = CurrentSession.TenantId > 0 ? CurrentSession.TenantId : 1;
                LocalDataCache.Instance.ClearSyncedQueue(tenantId);
                LoadData();
            };

            _grid.CellContentClick += async (s, e) =>
            {
                var actionCol = _grid.Columns["Action"];
                if (actionCol == null || e.RowIndex < 0 || e.ColumnIndex != actionCol.Index) return;

                int queueId = Convert.ToInt32(_grid.Rows[e.RowIndex].Cells["QueueId"].Value);
                string status = _grid.Rows[e.RowIndex].Cells["Status"].Value?.ToString() ?? "";

                if (status == "Conflict")
                {
                    using var dlg = new SyncConflictResolutionForm(queueId);
                    if (dlg.ShowDialog(this) == DialogResult.OK)
                    {
                        LoadData();
                    }
                }
                else if (status == "Failed")
                {
                    await SyncService.Instance.RetryItemAsync(queueId);
                    LoadData();
                }
            };

            _grid.CellDoubleClick += (s, e) =>
            {
                if (e.RowIndex < 0) return;
                int queueId = Convert.ToInt32(_grid.Rows[e.RowIndex].Cells["QueueId"].Value);
                string status = _grid.Rows[e.RowIndex].Cells["Status"].Value?.ToString() ?? "";

                if (status == "Conflict")
                {
                    using var dlg = new SyncConflictResolutionForm(queueId);
                    if (dlg.ShowDialog(this) == DialogResult.OK)
                    {
                        LoadData();
                    }
                }
            };

            SyncService.Instance.ConnectivityChanged += (s, online) =>
            {
                if (this.IsDisposed) return;
                this.BeginInvoke(new Action(() =>
                {
                    _lblStatusBadge.Text = online ? "● Cloud Connected (Online)" : "● Disconnected (Offline)";
                    _lblStatusBadge.ForeColor = online ? Color.FromArgb(22, 163, 74) : Color.FromArgb(220, 38, 38);
                    _lblStatusBadge.BackColor = online ? Color.FromArgb(240, 253, 244) : Color.FromArgb(254, 242, 242);
                    LoadData();
                }));
            };

            SyncService.Instance.SyncProgressChanged += (s, e) =>
            {
                if (this.IsDisposed) return;
                this.BeginInvoke(new Action(LoadData));
            };
        }

        public void LoadData()
        {
            int tenantId = CurrentSession.TenantId > 0 ? CurrentSession.TenantId : 1;
            var counts = LocalDataCache.Instance.GetQueueCounts(tenantId);

            _lblPendingCount.Text = $"Pending: {counts.Pending}";
            _lblConflictCount.Text = $"Conflicts: {counts.Conflict}";
            _lblFailedCount.Text = $"Failed: {counts.Failed}";
            _lblSyncedCount.Text = counts.Synced > 0 ? $"Synced: {counts.Synced}" : "✓ Up to date";

            var items = LocalDataCache.Instance.GetAllQueueItems(tenantId);
            if (_activeFilter != "All")
            {
                items = items.Where(i => i.Status.Equals(_activeFilter, StringComparison.OrdinalIgnoreCase)).ToList();
            }

            _grid.Rows.Clear();
            foreach (var item in items)
            {
                string idDisplay = item.ServerEntityId.HasValue
                    ? $"Server #{item.ServerEntityId}"
                    : item.EntityLocalId;

                string actionText = item.Status switch
                {
                    "Conflict" => "Resolve ⚠️",
                    "Failed" => "Retry 🔁",
                    _ => "-"
                };

                int rowIdx = _grid.Rows.Add(
                    item.QueueId,
                    item.EntityType,
                    item.Operation,
                    idDisplay,
                    item.Status,
                    item.CreatedAt.ToLocalTime().ToString("g"),
                    item.AttemptCount,
                    item.FailureReason ?? (item.Status == "Conflict" ? "Simultaneous change detected" : "-"),
                    actionText
                );

                var row = _grid.Rows[rowIdx];
                if (item.Status == "Conflict")
                {
                    row.DefaultCellStyle.BackColor = Color.FromArgb(254, 243, 199);
                    row.DefaultCellStyle.ForeColor = Color.FromArgb(146, 64, 14);
                }
                else if (item.Status == "Failed")
                {
                    row.DefaultCellStyle.BackColor = Color.FromArgb(254, 242, 242);
                    row.DefaultCellStyle.ForeColor = Color.FromArgb(185, 28, 28);
                }
                else if (item.Status == "Synced")
                {
                    row.DefaultCellStyle.ForeColor = Color.FromArgb(100, 116, 139);
                }
            }

            // Show empty-state panel when queue has no items to display
            bool hasItems = _grid.Rows.Count > 0;
            _grid.Visible = hasItems;

            var emptyPanel = this.Controls.Find("pnlEmptyState", true).FirstOrDefault()
                          ?? _grid.Parent?.Controls.Find("pnlEmptyState", true).FirstOrDefault();
            if (emptyPanel != null)
            {
                emptyPanel.Visible = !hasItems;
                if (!hasItems) emptyPanel.BringToFront();
            }
        }
    }
}
