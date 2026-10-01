using System;
using System.Drawing;
using System.Text.Json;
using System.Windows.Forms;
using CRMS_Peguit.domain.entities;
using CRMS_Peguit.winforms.Models.Services;
using CRMS_Peguit.winforms.Services.Offline;

namespace CRMS_Peguit.winforms.Views.Sync
{
    public partial class SyncConflictResolutionForm : Form
    {
        private readonly int _queueId;
        private readonly PendingSyncQueue _item;

        public bool ConflictResolved { get; private set; }

        public SyncConflictResolutionForm(int queueId)
        {
            _queueId = queueId;
            var item = LocalDataCache.Instance.GetQueueItem(queueId);
            _item = item ?? throw new ArgumentException($"Queue item #{queueId} not found.");

            InitializeComponent();
            PopulateData();
        }

        private void InitializeComponent()
        {
            this.Text = "NEXA — Sync Conflict Resolution";
            this.Size = new Size(860, 620);
            this.StartPosition = FormStartPosition.CenterParent;
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.MinimumSize = new Size(800, 560);
            this.BackColor = Color.FromArgb(248, 250, 252);
            this.Font = new Font("Segoe UI", 9.5F, FontStyle.Regular);

            // Header Panel
            var pnlHeader = new Panel
            {
                Dock = DockStyle.Top,
                Height = 85,
                BackColor = Color.FromArgb(254, 243, 199), // Amber-100
                Padding = new Padding(20, 16, 20, 12)
            };

            var lblTitle = new Label
            {
                Text = "⚠️ Conflict Detected: Simultaneous Modification",
                Font = new Font("Segoe UI", 12F, FontStyle.Bold),
                ForeColor = Color.FromArgb(146, 64, 14), // Amber-900
                AutoSize = true,
                Location = new Point(20, 14)
            };

            var lblSubtitle = new Label
            {
                Text = $"This {_item.EntityType} record (ID: {_item.ServerEntityId?.ToString() ?? _item.EntityLocalId}) was modified on the server while you were working offline.\nReview both versions below and explicitly select which one to keep.",
                Font = new Font("Segoe UI", 9F, FontStyle.Regular),
                ForeColor = Color.FromArgb(180, 83, 9), // Amber-700
                Location = new Point(20, 42),
                Size = new Size(800, 36)
            };

            pnlHeader.Controls.AddRange(new Control[] { lblTitle, lblSubtitle });

            // Bottom Buttons Panel
            var pnlBottom = new Panel
            {
                Dock = DockStyle.Bottom,
                Height = 65,
                BackColor = Color.White,
                Padding = new Padding(20, 12, 20, 12)
            };

            var btnCancel = new Button
            {
                Text = "Decide Later",
                Size = new Size(110, 38),
                Location = new Point(20, 14),
                Anchor = AnchorStyles.Top | AnchorStyles.Left,
                BackColor = Color.FromArgb(241, 245, 249),
                ForeColor = Color.FromArgb(71, 85, 105),
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand
            };
            btnCancel.FlatAppearance.BorderSize = 0;
            btnCancel.Click += (_, _) => this.Close();
            this.CancelButton = btnCancel;

            var btnKeepServer = new Button
            {
                Text = "Discard Offline (Keep Server)",
                Size = new Size(220, 38),
                Location = new Point(370, 14),
                Anchor = AnchorStyles.Top | AnchorStyles.Right,
                BackColor = Color.FromArgb(15, 23, 42),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 9.5F, FontStyle.Bold),
                Cursor = Cursors.Hand
            };
            btnKeepServer.FlatAppearance.BorderSize = 0;
            btnKeepServer.Click += async (_, _) => await ResolveKeepServerAsync();

            var btnKeepLocal = new Button
            {
                Text = "Apply Offline (Overwrite Server)",
                Size = new Size(230, 38),
                Location = new Point(600, 14),
                Anchor = AnchorStyles.Top | AnchorStyles.Right,
                BackColor = Color.FromArgb(37, 99, 235), // Blue-600
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 9.5F, FontStyle.Bold),
                Cursor = Cursors.Hand
            };
            btnKeepLocal.FlatAppearance.BorderSize = 0;
            btnKeepLocal.Click += async (_, _) => await ResolveKeepLocalAsync();

            pnlBottom.Controls.AddRange(new Control[] { btnCancel, btnKeepServer, btnKeepLocal });

            // Main Content Area with Split
            var pnlMain = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 2,
                RowCount = 1,
                Padding = new Padding(20, 16, 20, 12),
                BackColor = Color.Transparent
            };
            pnlMain.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));
            pnlMain.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));

            // Left Card: Local Offline Changes
            var grpLocal = new GroupBox
            {
                Text = "  Your Offline Version (Queued)  ",
                Dock = DockStyle.Fill,
                Font = new Font("Segoe UI", 9.5F, FontStyle.Bold),
                ForeColor = Color.FromArgb(37, 99, 235),
                Padding = new Padding(12)
            };

            _txtLocalPayload = new TextBox
            {
                Multiline = true,
                ReadOnly = true,
                ScrollBars = ScrollBars.Vertical,
                Dock = DockStyle.Fill,
                BackColor = Color.White,
                Font = new Font("Consolas", 9F, FontStyle.Regular),
                ForeColor = Color.FromArgb(15, 23, 42)
            };
            grpLocal.Controls.Add(_txtLocalPayload);

            // Right Card: Current Server Version
            var grpServer = new GroupBox
            {
                Text = "  Current Server Version (Cloud)  ",
                Dock = DockStyle.Fill,
                Font = new Font("Segoe UI", 9.5F, FontStyle.Bold),
                ForeColor = Color.FromArgb(15, 23, 42),
                Padding = new Padding(12)
            };

            _txtServerPayload = new TextBox
            {
                Multiline = true,
                ReadOnly = true,
                ScrollBars = ScrollBars.Vertical,
                Dock = DockStyle.Fill,
                BackColor = Color.White,
                Font = new Font("Consolas", 9F, FontStyle.Regular),
                ForeColor = Color.FromArgb(15, 23, 42)
            };
            grpServer.Controls.Add(_txtServerPayload);

            pnlMain.Controls.Add(grpLocal, 0, 0);
            pnlMain.Controls.Add(grpServer, 1, 0);

            this.Controls.Add(pnlMain);
            this.Controls.Add(pnlBottom);
            this.Controls.Add(pnlHeader);
        }

        private TextBox _txtLocalPayload = null!;
        private TextBox _txtServerPayload = null!;

        private void PopulateData()
        {
            _txtLocalPayload.Text = FormatJson(_item.PayloadJson);
            _txtServerPayload.Text = FormatJson(_item.ServerConflictPayload ?? "{}");
        }

        private static string FormatJson(string json)
        {
            if (string.IsNullOrWhiteSpace(json)) return "(No data available)";
            try
            {
                using var doc = JsonDocument.Parse(json);
                return JsonSerializer.Serialize(doc.RootElement, new JsonSerializerOptions { WriteIndented = true });
            }
            catch
            {
                return json;
            }
        }

        private async Task ResolveKeepLocalAsync()
        {
            var confirm = MessageBox.Show(
                "Are you sure you want to overwrite the server with your offline changes?",
                "Confirm Conflict Resolution",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Question);

            if (confirm != DialogResult.Yes) return;

            bool ok = await SyncService.Instance.ResolveConflictUseLocalAsync(_queueId);
            if (ok)
            {
                ConflictResolved = true;
                MessageBox.Show("Your offline changes have been pushed to the server.", "Conflict Resolved", MessageBoxButtons.OK, MessageBoxIcon.Information);
                this.DialogResult = DialogResult.OK;
                this.Close();
            }
            else
            {
                MessageBox.Show("Failed to apply local changes to server. Please check your network connection and retry.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private async Task ResolveKeepServerAsync()
        {
            var confirm = MessageBox.Show(
                "Are you sure you want to discard your offline changes and accept the server version?",
                "Confirm Conflict Resolution",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Question);

            if (confirm != DialogResult.Yes) return;

            bool ok = await SyncService.Instance.ResolveConflictUseServerAsync(_queueId);
            if (ok)
            {
                ConflictResolved = true;
                MessageBox.Show("Your local offline changes were discarded and the latest server version was preserved.", "Conflict Resolved", MessageBoxButtons.OK, MessageBoxIcon.Information);
                this.DialogResult = DialogResult.OK;
                this.Close();
            }
            else
            {
                MessageBox.Show("Failed to complete resolution. Please retry.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
    }
}
