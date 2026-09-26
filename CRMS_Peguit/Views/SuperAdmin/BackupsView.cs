using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using CRMS_Peguit.domain.Common;
using CRMS_Peguit.winforms.Auth;
using CRMS_Peguit.winforms.Controls;
using CRMS_Peguit.winforms.Models.Services;
using CRMS_Peguit.winforms.Services;

// =============================================================================
// BackupsView — Backup history and management.
//
// DATA BOUNDARY: SuperAdminController accesses
// RealEstateDbContext(TenantId=1).BackupLogs + Users (performer name) ONLY.
// NO Customer/Lead/Deal/Property/Activity/SupportTicket/TaskReminder/Notification.
//
// RESTORE SAFETY: Restore is a two-step destructive action.
//   Step 1: RestoreConfirmDialog shows backup metadata, asks "Are you sure?"
//   Step 2: User must type "RESTORE" explicitly before the confirm button enables.
//   One-click restore is STRUCTURALLY IMPOSSIBLE in this implementation.
// =============================================================================

namespace CRMS_Peguit.winforms.Views.SuperAdmin
{
    public class BackupsView : UserControl
    {
        private readonly SuperAdminApiService _controller = new();
        private List<BackupLogDto> _backups = new();

        private DataGridView _grid = null!;
        private PaginationControl _pagination = null!;
        private Button _btnRunBackup = null!;
        private Button _btnRestore = null!;
        private Button _btnSeedAllTenants = null!;
        private Label _lblLastStatus = null!;
        private Panel _pnlEmptyState = null!;

        public BackupsView()
        {
            InitializeComponent();
            _ = LoadDataAsync();
        }

        private void InitializeComponent()
        {
            Dock = DockStyle.Fill;
            BackColor = Theme.Background;

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
                Text = "System Backups & Disaster Recovery",
                Font = UiStyleConstants.PageTitleFont,
                ForeColor = Theme.TextPrimary,
                AutoSize = true,
                Location = new Point(28, 14)
            };
            var lblSub = new Label
            {
                Text = "Platform database backups, snapshot logs, and two-step disaster recovery — Infrastructure metadata only",
                Font = UiStyleConstants.SubtitleFont,
                ForeColor = Theme.TextSecondary,
                AutoSize = true,
                Location = new Point(28, 42)
            };
            pnlPageHeader.Controls.Add(lblSub);
            pnlPageHeader.Controls.Add(lblTitle);

            // 2. Toolbar (Height = 56)
            var pnlToolbar = new Panel
            {
                Dock = DockStyle.Top,
                Height = 56,
                Padding = new Padding(24, 10, 24, 0),
                BackColor = Theme.Background
            };

            _lblLastStatus = new Label
            {
                Text = "Loading backup log history...",
                Font = new Font("Segoe UI", 9.5f, FontStyle.Bold),
                ForeColor = Theme.TextSecondary,
                AutoSize = true,
                Location = new Point(24, 17)
            };
            pnlToolbar.Controls.Add(_lblLastStatus);

            // Right-aligned actions container
            var pnlActions = new FlowLayoutPanel
            {
                FlowDirection = FlowDirection.RightToLeft,
                Anchor = AnchorStyles.Top | AnchorStyles.Right,
                Height = 40,
                Width = 540,
                Location = new Point(pnlToolbar.Width - 564, 8),
                BackColor = Color.Transparent
            };
            pnlToolbar.SizeChanged += (_, _) =>
                pnlActions.Location = new Point(pnlToolbar.Width - 564, 8);

            _btnRunBackup = new Button
            {
                Text = "🗄  Run Backup Now",
                Size = new Size(160, 34),
                BackColor = Theme.Primary,
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 9f, FontStyle.Bold),
                Cursor = Cursors.Hand,
                Margin = new Padding(4, 0, 0, 0)
            };
            UiRadiusHelper.StyleButton(_btnRunBackup, 6);
            _btnRunBackup.Click += BtnRunBackup_Click;

            _btnRestore = new Button
            {
                Text = "⏮  Restore...",
                Size = new Size(130, 34),
                BackColor = Color.FromArgb(254, 226, 226),
                ForeColor = Color.FromArgb(153, 27, 27),
                Font = new Font("Segoe UI", 9f, FontStyle.Bold),
                Cursor = Cursors.Hand,
                Margin = new Padding(4, 0, 0, 0)
            };
            UiRadiusHelper.StyleButton(_btnRestore, 6);
            _btnRestore.Click += BtnRestore_Click;

            _btnSeedAllTenants = new Button
            {
                Text = "🌱  Seed All Tenants",
                Size = new Size(160, 34),
                BackColor = Color.FromArgb(240, 253, 244),
                ForeColor = Color.FromArgb(22, 101, 52),
                Font = new Font("Segoe UI", 9f, FontStyle.Bold),
                Cursor = Cursors.Hand,
                Margin = new Padding(4, 0, 0, 0)
            };
            UiRadiusHelper.StyleButton(_btnSeedAllTenants, 6);
            _btnSeedAllTenants.Click += BtnSeedAllTenants_Click;

            pnlActions.Controls.Add(_btnRunBackup);
            pnlActions.Controls.Add(_btnRestore);
            pnlActions.Controls.Add(_btnSeedAllTenants);
            pnlToolbar.Controls.Add(pnlActions);

            // 3. Danger / Warning Alert Banner (Height = 44)
            var pnlWarn = new Panel
            {
                Dock = DockStyle.Top,
                Height = 44,
                Padding = new Padding(24, 4, 24, 4),
                BackColor = Theme.Background
            };

            var pnlWarnCard = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.FromArgb(254, 243, 199), // Amber 100
                Padding = new Padding(14, 8, 14, 8)
            };
            UiRadiusHelper.StyleCard(pnlWarnCard, 6);

            var lblWarn = new Label
            {
                Text = "⚠  Disaster Recovery Warning: Database restoration is irreversible and replaces active data. A strict two-step confirmation requiring typing \"RESTORE\" is enforced.",
                Font = new Font("Segoe UI", 8.5f, FontStyle.Bold),
                ForeColor = Color.FromArgb(146, 64, 14),
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleLeft
            };
            pnlWarnCard.Controls.Add(lblWarn);
            pnlWarn.Controls.Add(pnlWarnCard);

            // 4. Grid wrapper (Dock = Fill)
            var pnlWrapper = new Panel
            {
                Dock = DockStyle.Fill,
                Padding = new Padding(24, 8, 24, 24),
                BackColor = Theme.Background
            };

            var pnlCard = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Theme.Surface,
                Padding = new Padding(1)
            };
            UiRadiusHelper.StyleCard(pnlCard, 8);

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
                AutoGenerateColumns = false,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill
            };
            UiGridHelper.ApplyModernGridStyle(_grid, rowHeight: 52);

            _grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "BackupId", DataPropertyName = "BackupId", Visible = false });

            _grid.Columns.Add(new DataGridViewTextBoxColumn
            {
                HeaderText = "BACKUP TIMESTAMP",
                Name = "BackupDateFormatted",
                DataPropertyName = "BackupDateFormatted",
                FillWeight = 24,
                MinimumWidth = 160
            });
            _grid.Columns.Add(new DataGridViewTextBoxColumn
            {
                HeaderText = "HEALTH STATUS",
                Name = "Status",
                DataPropertyName = "Status",
                FillWeight = 16,
                MinimumWidth = 110
            });
            _grid.Columns.Add(new DataGridViewTextBoxColumn
            {
                HeaderText = "SNAPSHOT ARCHIVE / LOCATION",
                Name = "FileLocation",
                DataPropertyName = "FileLocation",
                FillWeight = 38,
                MinimumWidth = 220
            });
            _grid.Columns.Add(new DataGridViewTextBoxColumn
            {
                HeaderText = "INITIATED BY",
                Name = "PerformedByName",
                DataPropertyName = "PerformedByName",
                FillWeight = 22,
                MinimumWidth = 140
            });

            _grid.CellPainting += (s, e) =>
            {
                if (e.RowIndex < 0 || e.Graphics == null) return;
                string colName = _grid.Columns[e.ColumnIndex].Name;

                if (colName == "Status" && e.Value != null)
                {
                    string status = e.Value.ToString() ?? "";
                    UiGridHelper.PaintStatusText(_grid, e, status, center: false);
                }
                else if (colName == "BackupDateFormatted" && e.CellStyle != null)
                {
                    e.CellStyle.Font = new Font("Segoe UI", 9.5f, FontStyle.Bold);
                    e.CellStyle.ForeColor = Theme.TextPrimary;
                }
                else if (colName == "FileLocation" && e.CellStyle != null)
                {
                    e.CellStyle.Font = new Font("Consolas", 9f);
                    e.CellStyle.ForeColor = Color.FromArgb(71, 85, 105);
                }
            };

            // Empty state container
            _pnlEmptyState = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Theme.Surface,
                Visible = false
            };
            var lblEmptyIcon = new Label
            {
                Text = "🗄",
                Font = new Font("Segoe UI", 36f),
                ForeColor = Color.FromArgb(148, 163, 184),
                AutoSize = true,
                Location = new Point(0, 0)
            };
            var lblEmptyTitle = new Label
            {
                Text = "No Backup History Recorded",
                Font = new Font("Segoe UI", 13f, FontStyle.Bold),
                ForeColor = Theme.TextPrimary,
                AutoSize = true
            };
            var lblEmptyDesc = new Label
            {
                Text = "Click 'Run Backup Now' above to generate the first complete platform snapshot.",
                Font = new Font("Segoe UI", 9.5f),
                ForeColor = Theme.TextSecondary,
                AutoSize = true
            };
            _pnlEmptyState.Controls.Add(lblEmptyIcon);
            _pnlEmptyState.Controls.Add(lblEmptyTitle);
            _pnlEmptyState.Controls.Add(lblEmptyDesc);
            _pnlEmptyState.SizeChanged += (_, _) =>
            {
                int cx = _pnlEmptyState.Width / 2;
                int cy = _pnlEmptyState.Height / 2;
                lblEmptyIcon.Location = new Point(cx - 30, cy - 80);
                lblEmptyTitle.Location = new Point(cx - (lblEmptyTitle.Width / 2), cy - 10);
                lblEmptyDesc.Location = new Point(cx - (lblEmptyDesc.Width / 2), cy + 20);
            };

            // Pagination Control
            _pagination = new PaginationControl
            {
                Dock = DockStyle.Bottom
            };
            _pagination.PageChanged += (_, _) => RenderGrid(resetPage: false);
            _pagination.PageSizeChanged += (_, _) => RenderGrid(resetPage: true);

            pnlCard.Controls.Add(_grid);
            pnlCard.Controls.Add(_pnlEmptyState);
            pnlCard.Controls.Add(_pagination);
            _pagination.BringToFront();

            pnlWrapper.Controls.Add(pnlCard);

            // Add in reverse docking order: Fill -> Warn -> Toolbar -> Header
            Controls.Add(pnlWrapper);    // Dock = Fill
            Controls.Add(pnlWarn);       // Dock = Top
            Controls.Add(pnlToolbar);    // Dock = Top
            Controls.Add(pnlPageHeader); // Dock = Top (at the very top)
        }

        private async Task LoadDataAsync()
        {
            try
            {
                _backups = await _controller.GetBackupHistoryAsync();
                RenderGrid(resetPage: true);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to load backup history: {ex.Message}",
                    "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void RenderGrid(bool resetPage = false)
        {
            if (resetPage)
            {
                _pagination.ResetPage();
            }

            int totalRecords = _backups.Count;
            var list = _backups
                .Skip((_pagination.CurrentPage - 1) * _pagination.PageSize)
                .Take(_pagination.PageSize)
                .Select(b => new
                {
                    b.BackupId,
                    BackupDateFormatted = b.BackupDate.ToLocalTime().ToString("MMM dd, yyyy  h:mm tt"),
                    b.Status,
                    b.FileLocation,
                    b.PerformedByName
                }).ToList();

            _grid.DataSource = list;

            bool hasData = totalRecords > 0;
            _grid.Visible = hasData;
            _pagination.Visible = hasData;
            _pnlEmptyState.Visible = !hasData;

            _pagination.UpdatePagination(totalRecords, _pagination.CurrentPage, _pagination.PageSize);

            _lblLastStatus.Text = hasData
                ? $"{totalRecords} backup snapshot{(totalRecords != 1 ? "s" : "")} recorded  ·  Latest: {_backups.First().BackupDate.ToLocalTime():MMM dd, yyyy  h:mm tt}"
                : "No platform snapshots recorded yet";
        }

        private async void BtnRunBackup_Click(object? sender, EventArgs e)
        {
            var confirm = MessageBox.Show(
                "Initiate immediate platform database snapshot?\n\nA complete database archive file will be created and logged.",
                "Confirm Platform Backup", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
            if (confirm != DialogResult.Yes) return;

            _btnRunBackup.Enabled = false;
            _btnRunBackup.Text = "Generating Snapshot...";

            try
            {
                int userId = CurrentSession.UserId > 0 ? CurrentSession.UserId : 1;
                var (success, log) = await _controller.RunBackupAsync(userId);
                if (success && log != null)
                {
                    MessageBox.Show(
                        $"Platform backup completed successfully!\n\nArchive: {log.FileLocation}\nTimestamp: {log.BackupDate.ToLocalTime():MMM dd, yyyy  h:mm:ss tt}\nStatus: {log.Status}",
                        "Backup Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    await LoadDataAsync();
                }
                else
                {
                    MessageBox.Show("Backup failed. Please check system event logs.", "Backup Error",
                        MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
            finally
            {
                _btnRunBackup.Enabled = true;
                _btnRunBackup.Text = "🗄  Run Backup Now";
            }
        }

        private async void BtnRestore_Click(object? sender, EventArgs e)
        {
            if (_grid.CurrentRow?.DataBoundItem == null)
            {
                MessageBox.Show("Please select a backup snapshot from the table to restore.",
                    "Notice", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            dynamic row = _grid.CurrentRow.DataBoundItem;
            int backupId = row.BackupId;

            var backup = await _controller.GetBackupForRestoreAsync(backupId);
            if (backup == null)
            {
                MessageBox.Show("Could not retrieve backup snapshot details.", "Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            using var dlg = new RestoreConfirmDialog(backup);
            if (dlg.ShowDialog(this) != DialogResult.OK) return;

            _btnRestore.Enabled = false;
            try
            {
                int userId = CurrentSession.UserId > 0 ? CurrentSession.UserId : 1;
                bool ok = await _controller.RestoreBackupAsync(backupId, userId);
                if (ok)
                {
                    MessageBox.Show(
                        $"Database restoration from '{backup.FileLocation}' has been recorded.\nRecovery operation logged.",
                        "Restoration Complete", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    await LoadDataAsync();
                }
                else
                {
                    MessageBox.Show("Restore operation could not be executed.", "Error",
                        MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
            finally
            {
                _btnRestore.Enabled = true;
            }
        }

        private async void BtnSeedAllTenants_Click(object? sender, EventArgs e)
        {
            var confirm = MessageBox.Show(
                "This will verify and seed full realistic datasets across ALL tenants:\n\n" +
                "  • Tenant 1: Apex Realty (Standard Tier)\n" +
                "  • Tenant 2: BlueHorizon Properties (Professional Tier)\n" +
                "  • Tenant 3: Crestview Holdings (Enterprise Multi-Branch: Manila, Cebu, Davao)\n\n" +
                "Datasets include users, roles, team members, customers, buyer profiles, properties, deals, contingencies, clauses, leads, support tickets, comments, activities, follow-ups, campaigns, and notifications.\n\n" +
                "Proceed with seeding all tenants?",
                "Confirm Seed All Tenants",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Question);

            if (confirm != DialogResult.Yes) return;

            _btnSeedAllTenants.Enabled = false;
            _btnRunBackup.Enabled = false;
            _btnRestore.Enabled = false;
            _lblLastStatus.Text = "🌱 Seeding all tenants in progress...";
            _lblLastStatus.ForeColor = Color.FromArgb(22, 101, 52);

            try
            {
                var progress = new Progress<string>(msg =>
                {
                    _lblLastStatus.Text = msg;
                });

                await Task.Run(() => LocalDb.SeedAllTenantsAsync(progress));

                _lblLastStatus.Text = "✓ All tenants successfully seeded.";
                _lblLastStatus.ForeColor = Color.FromArgb(22, 101, 52);

                MessageBox.Show(
                    "All tenant databases have been successfully seeded!\n\n" +
                    "  • Tenant 1 (Apex Realty): CRMS_Tenant_1\n" +
                    "  • Tenant 2 (BlueHorizon Properties): CRMS_Tenant_2\n" +
                    "  • Tenant 3 (Crestview Holdings): CRMS_Tenant_3 (3 Branches)\n\n" +
                    "Default credentials:\n" +
                    "  • Tenant 1: admin@test.com / Admin123! | manager@test.com / Manager123! | agent@test.com / Agent123!\n" +
                    "  • Tenant 2: tenantb_admin@test.com / Admin123! | manager.b@test.com / Manager123! | agent.b@test.com / Agent123!\n" +
                    "  • Tenant 3: tenantc_admin@test.com / Admin123! | manager.c@test.com / Manager123! | agent.c@test.com / Agent123!",
                    "Seeding Complete",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information);

                await LoadDataAsync();
            }
            catch (Exception ex)
            {
                _lblLastStatus.Text = $"Seeding error: {ex.Message}";
                _lblLastStatus.ForeColor = Color.FromArgb(153, 27, 27);
                MessageBox.Show($"Failed to seed all tenants: {ex.Message}", "Seeding Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                _btnSeedAllTenants.Enabled = true;
                _btnRunBackup.Enabled = true;
                _btnRestore.Enabled = true;
            }
        }
    }
}
