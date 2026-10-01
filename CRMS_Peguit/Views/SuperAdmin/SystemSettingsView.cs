using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using CRMS_Peguit.winforms.Auth;
using CRMS_Peguit.winforms.Controllers;
using CRMS_Peguit.winforms.Controls;
using CRMS_Peguit.winforms.Models.Services;
using CRMS_Peguit.winforms.Services;

// =============================================================================
// SystemSettingsView — Platform-wide key/value settings with full audit trail.
//
// DATA BOUNDARY: SuperAdminController.GetSystemSettingsAsync() accesses
// RealEstateDbContext(TenantId=1).SystemSettings + Users (for updatedBy) ONLY.
// NO Customer/Lead/Deal/Property/Activity/SupportTicket/TaskReminder/Notification.
// =============================================================================

namespace CRMS_Peguit.winforms.Views.SuperAdmin
{
    public class SystemSettingsView : UserControl
    {
        private readonly SuperAdminController _controller = new();
        private List<SystemSettingDto> _settings = new();

        private DataGridView _grid = null!;
        private GridSkeletonOverlay? _gridSkeleton;
        private TextBox _txtSearch = null!;
        private Button _btnMasterTerms = null!;
        private Button _btnPlanInclusions = null!;
        private Button _btnTierEntitlements = null!;
        private Label _lblCount = null!;

        public SystemSettingsView()
        {
            InitializeComponent();
            _ = LoadDataAsync();
        }

        private void InitializeComponent()
        {
            Dock = DockStyle.Fill;
            BackColor = Theme.Background;

            // 1. Page Header (Height = 96)
            var pnlPageHeader = new Panel
            {
                Dock = DockStyle.Top,
                Height = 96,
                BackColor = Theme.Surface,
                Padding = new Padding(28, 18, 28, 0)
            };
            pnlPageHeader.Paint += (s, e) =>
            {
                using var p = new Pen(Theme.Border, 1f);
                e.Graphics.DrawLine(p, 0, pnlPageHeader.Height - 1, pnlPageHeader.Width, pnlPageHeader.Height - 1);
            };

            var lblTitle = new Label
            {
                Text = "System Configuration & Policies",
                Font = UiStyleConstants.PageTitleFont,
                ForeColor = Theme.TextPrimary,
                UseMnemonic = false,
                AutoSize = true,
                Location = new Point(28, 14)
            };
            var lblSub = new Label
            {
                Text = "Global platform settings & security policies with immutable audit trail — Who changed what, when",
                Font = UiStyleConstants.SubtitleFont,
                ForeColor = Theme.TextSecondary,
                UseMnemonic = false,
                AutoSize = true,
                Location = new Point(28, 56)
            };
            pnlPageHeader.Controls.Add(lblSub);
            pnlPageHeader.Controls.Add(lblTitle);
            ResponsiveLayout.BindHeader(pnlPageHeader, lblTitle, lblSub);

            // 2. Toolbar (Height = 56)
            var pnlToolbar = new Panel
            {
                Dock = DockStyle.Top,
                Height = 56,
                Padding = new Padding(24, 10, 24, 0),
                BackColor = Theme.Background
            };

            _txtSearch = new TextBox
            {
                PlaceholderText = "🔍  Search configuration setting key...",
                Font = new Font("Segoe UI", 10f),
                Size = new Size(320, 32),
                Location = new Point(24, 12)
            };
            _txtSearch.TextChanged += (_, _) => ApplyFilter();
            pnlToolbar.Controls.Add(_txtSearch);

            _lblCount = new Label
            {
                Text = "",
                Font = new Font("Segoe UI", 9.5f, FontStyle.Bold),
                ForeColor = Theme.TextSecondary,
                AutoSize = true,
                AutoEllipsis = true,
                MaximumSize = new Size(Math.Max(100, pnlToolbar.Width - 560), 24),
                Location = new Point(356, 17)
            };
            pnlToolbar.Controls.Add(_lblCount);

            _btnMasterTerms = new Button
            {
                Text = "📜 Master Terms",
                Size = new Size(140, 34),
                Anchor = AnchorStyles.Top | AnchorStyles.Right,
                BackColor = Theme.Surface,
                ForeColor = Theme.Primary,
                Font = new Font("Segoe UI", 9f, FontStyle.Bold),
                Cursor = Cursors.Hand
            };
            UiRadiusHelper.StyleSecondaryButton(_btnMasterTerms, 6);
            _btnMasterTerms.Click += (_, _) =>
            {
                using var dlg = new EditMasterTermsDialog();
                dlg.ShowDialog(this);
            };
            _btnMasterTerms.Location = new Point(pnlToolbar.Width - 310, 10);
            pnlToolbar.Controls.Add(_btnMasterTerms);

            _btnPlanInclusions = new Button
            {
                Text = "📋 Plan Inclusions",
                Size = new Size(150, 34),
                Anchor = AnchorStyles.Top | AnchorStyles.Right,
                BackColor = Theme.Surface,
                ForeColor = Theme.Primary,
                Font = new Font("Segoe UI", 9f, FontStyle.Bold),
                Cursor = Cursors.Hand
            };
            UiRadiusHelper.StyleSecondaryButton(_btnPlanInclusions, 6);
            _btnPlanInclusions.Click += (_, _) =>
            {
                using var dlg = new EditPlanInclusionsDialog();
                dlg.ShowDialog(this);
            };
            _btnPlanInclusions.Location = new Point(pnlToolbar.Width - 160, 10);
            pnlToolbar.Controls.Add(_btnPlanInclusions);

            _btnTierEntitlements = new Button
            {
                Text = "⚙ Tier Entitlements",
                Size = new Size(160, 34),
                Anchor = AnchorStyles.Top | AnchorStyles.Right,
                BackColor = Theme.Surface,
                ForeColor = Theme.Primary,
                Font = new Font("Segoe UI", 9f, FontStyle.Bold),
                Cursor = Cursors.Hand
            };
            UiRadiusHelper.StyleSecondaryButton(_btnTierEntitlements, 6);
            _btnTierEntitlements.Click += (_, _) =>
            {
                using var dlg = new ManageTierEntitlementsDialog();
                if (dlg.ShowDialog(this) == DialogResult.OK)
                {
                    _ = LoadDataAsync();
                }
            };
            _btnTierEntitlements.Location = new Point(pnlToolbar.Width - 480, 10);
            pnlToolbar.Controls.Add(_btnTierEntitlements);

            pnlToolbar.SizeChanged += (_, _) =>
            {
                _btnPlanInclusions.Location = new Point(pnlToolbar.Width - 160, 10);
                _btnMasterTerms.Location = new Point(pnlToolbar.Width - 310, 10);
                _btnTierEntitlements.Location = new Point(pnlToolbar.Width - 480, 10);
                _lblCount.MaximumSize = new Size(Math.Max(100, pnlToolbar.Width - 830), 24);
            };
            _lblCount.MaximumSize = Size.Empty;
            ResponsiveLayout.BindToolbar(pnlToolbar, 12, _txtSearch, _lblCount, _btnMasterTerms, _btnPlanInclusions, _btnTierEntitlements);

            // 3. Audit trail notice callout (Height = 42)
            var pnlNotice = new Panel
            {
                Dock = DockStyle.Top,
                Height = 42,
                Padding = new Padding(24, 4, 24, 4),
                BackColor = Theme.Background
            };

            var pnlNoticeCard = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.FromArgb(239, 246, 255),
                Padding = new Padding(14, 8, 14, 8)
            };
            UiRadiusHelper.StyleCard(pnlNoticeCard, 6);

            var lblNotice = new Label
            {
                Text = "🛡  Audit Policy: Every policy or configuration modification is cryptographically associated with your Admin Session and logged with UTC timestamp.",
                Font = new Font("Segoe UI", 9f, FontStyle.Bold),
                ForeColor = Color.FromArgb(29, 78, 216),
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleLeft
            };
            pnlNoticeCard.Controls.Add(lblNotice);
            pnlNotice.Controls.Add(pnlNoticeCard);

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
            UiGridHelper.ApplyModernGridStyle(_grid, rowHeight: 54);

            _grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "SettingId", DataPropertyName = "SettingId", Visible = false });

            _grid.Columns.Add(new DataGridViewTextBoxColumn
            {
                HeaderText = "SETTING KEY / POLICY",
                Name = "SettingKey",
                DataPropertyName = "SettingKey",
                FillWeight = 32,
                MinimumWidth = 200
            });
            _grid.Columns.Add(new DataGridViewTextBoxColumn
            {
                HeaderText = "CURRENT VALUE",
                Name = "SettingValue",
                DataPropertyName = "SettingValue",
                FillWeight = 24,
                MinimumWidth = 140
            });
            _grid.Columns.Add(new DataGridViewTextBoxColumn
            {
                HeaderText = "LAST UPDATED",
                Name = "UpdatedAt",
                DataPropertyName = "UpdatedAtFormatted",
                FillWeight = 22,
                MinimumWidth = 140
            });
            _grid.Columns.Add(new DataGridViewTextBoxColumn
            {
                HeaderText = "AUDITED MODIFIER",
                Name = "UpdatedByName",
                DataPropertyName = "UpdatedByName",
                FillWeight = 22,
                MinimumWidth = 140
            });

            _grid.Columns.Add(new ActionsColumn
            {
                AutoSizeMode = DataGridViewAutoSizeColumnMode.None,
                Width = 64
            });

            _grid.CellFormatting += (s, e) =>
            {
                if (e.RowIndex < 0 || e.Value == null) return;
                string colName = _grid.Columns[e.ColumnIndex].Name;

                if (colName == "SettingKey")
                {
                    e.CellStyle.Font = new Font("Segoe UI", 9.5f, FontStyle.Bold);
                    e.CellStyle.ForeColor = Theme.TextPrimary;
                }
                else if (colName == "SettingValue")
                {
                    e.CellStyle.Font = new Font("Segoe UI Semibold", 9.5f, FontStyle.Bold);
                    e.CellStyle.ForeColor = Color.FromArgb(14, 116, 144); // Cyan 700
                }
            };

            _grid.CellDoubleClick += (_, e) => { if (e.RowIndex >= 0) BtnEdit_Click(null, EventArgs.Empty); };

            _grid.CellClick += (s, e) =>
            {
                if (e.RowIndex < 0 || e.ColumnIndex < 0) return;
                string colName = _grid.Columns[e.ColumnIndex].Name;
                if (colName == "Actions" || _grid.Columns[e.ColumnIndex] is ActionsColumn)
                {
                    if (_grid.Rows[e.RowIndex].DataBoundItem is not { } boundItem) return;
                    dynamic row = boundItem;
                    string key = (string)row.SettingKey;
                    string val = (string)row.SettingValue;

                    var cellRect = _grid.GetCellDisplayRectangle(e.ColumnIndex, e.RowIndex, false);
                    var menu = new ContextMenuStrip();

                    var editItem = new ToolStripMenuItem("✏  Edit Value...");
                    editItem.Click += (_, _) => BtnEdit_Click(null, EventArgs.Empty);
                    menu.Items.Add(editItem);

                    menu.Items.Add(new ToolStripSeparator());

                    var copyKeyItem = new ToolStripMenuItem("📋  Copy Setting Key");
                    copyKeyItem.Click += (_, _) =>
                    {
                        if (!string.IsNullOrEmpty(key))
                        {
                            Clipboard.SetText(key);
                            MessageBox.Show($"Copied key '{key}' to clipboard.", "Copied", MessageBoxButtons.OK, MessageBoxIcon.Information);
                        }
                    };
                    menu.Items.Add(copyKeyItem);

                    var copyValItem = new ToolStripMenuItem("📋  Copy Value");
                    copyValItem.Click += (_, _) =>
                    {
                        if (!string.IsNullOrEmpty(val))
                        {
                            Clipboard.SetText(val);
                            MessageBox.Show($"Copied value to clipboard.", "Copied", MessageBoxButtons.OK, MessageBoxIcon.Information);
                        }
                    };
                    menu.Items.Add(copyValItem);

                    menu.Show(_grid, new Point(Math.Max(0, cellRect.Right - 200), cellRect.Bottom));
                }
            };

            pnlCard.Controls.Add(_grid);
            _gridSkeleton = GridSkeletonOverlay.CreateForGrid(_grid);
            pnlWrapper.Controls.Add(pnlCard);

            // Add in reverse docking order: Fill -> Notice -> Toolbar -> Header
            Controls.Add(pnlWrapper);    // Dock = Fill
            Controls.Add(pnlNotice);     // Dock = Top
            Controls.Add(pnlToolbar);    // Dock = Top
            Controls.Add(pnlPageHeader); // Dock = Top (at the very top)
        }

        private async Task LoadDataAsync()
        {
            _gridSkeleton?.ShowSkeleton();
            try
            {
                _settings = await _controller.GetSystemSettingsAsync();
                ApplyFilter();
            }
            catch (Exception ex)
            {
                if (!CRMS_Peguit.winforms.Audit.ScreenAuditor.IsAuditing)
                {
                    MessageBox.Show($"Failed to load settings: {ex.Message}",
                        "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
                else
                {
                    Console.WriteLine($"[AUDITOR WARNING] Failed to load settings: {ex.Message}");
                }
            }
            finally
            {
                _gridSkeleton?.HideSkeleton();
            }
        }

        private void ApplyFilter()
        {
            var query = _settings.AsEnumerable();
            var s = _txtSearch.Text.Trim();
            if (!string.IsNullOrEmpty(s))
                query = query.Where(x => x.SettingKey.Contains(s, StringComparison.OrdinalIgnoreCase));

            var list = query.Select(x => new
            {
                x.SettingId,
                x.SettingKey,
                x.SettingValue,
                UpdatedAtFormatted = x.UpdatedAt.ToLocalTime().ToString("MMM dd, yyyy  h:mm tt"),
                x.UpdatedByName
            }).ToList();

            _grid.DataSource = list;
            _lblCount.Text = $"{list.Count} setting{(list.Count != 1 ? "s" : "")}";
        }

        private async void BtnEdit_Click(object? sender, EventArgs e)
        {
            if (_grid.CurrentRow?.DataBoundItem == null)
            {
                MessageBox.Show("Please select a configuration setting to edit.", "Notice",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            dynamic row = _grid.CurrentRow.DataBoundItem;
            int settingId = row.SettingId;
            string key = row.SettingKey;
            string currentValue = row.SettingValue;

            using var dlg = new EditSettingDialog(key, currentValue);
            if (dlg.ShowDialog(this) != DialogResult.OK) return;

            int editorUserId = CurrentSession.UserId > 0 ? CurrentSession.UserId : 1;
            bool ok = await _controller.UpdateSystemSettingAsync(settingId, dlg.NewValue, editorUserId);

            if (ok)
            {
                MessageBox.Show($"Setting '{key}' successfully updated to '{dlg.NewValue}'.",
                    "Setting Updated", MessageBoxButtons.OK, MessageBoxIcon.Information);
                await LoadDataAsync();
            }
            else
            {
                MessageBox.Show("Failed to save setting. Please try again.", "Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
    }
}
