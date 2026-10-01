using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using CRMS_Peguit.domain.Common;
using CRMS_Peguit.domain.entities;
using CRMS_Peguit.winforms.Auth;
using CRMS_Peguit.winforms.Controllers;
using CRMS_Peguit.winforms.Models.Services;
using CRMS_Peguit.winforms.Services;

namespace CRMS_Peguit.winforms.Views.SuperAdmin
{
    /// <summary>
    /// Super Admin console dialog for managing dynamic module entitlements across subscription tiers.
    /// Toggling permissions here immediately enforces runtime button visibility (btnBranching, btnDashboard, btnApprovals),
    /// screens, white-label settings, and marketing inclusions across the entire platform.
    /// </summary>
    public class ManageTierEntitlementsDialog : Form
    {
        private readonly SuperAdminSubscriptionController _controller = new();
        private readonly DataGridView _grid;
        private readonly Button _btnSave;
        private readonly Button _btnReset;
        private readonly Button _btnCancel;
        private readonly Label _lblStatus;

        private Dictionary<string, bool> _currentMatrix = new(StringComparer.OrdinalIgnoreCase);

        public ManageTierEntitlementsDialog()
        {
            Text = "Super Admin — Platform Tier Entitlements & Module Gating";
            Size = new Size(960, 720);
            MinimumSize = new Size(820, 560);
            StartPosition = FormStartPosition.CenterParent;
            FormBorderStyle = FormBorderStyle.Sizable;
            MaximizeBox = true;
            MinimizeBox = false;
            BackColor = Color.White;
            UiRadiusHelper.StyleModal(this, 8);

            // =========================================================================
            // 1. TOP HEADER PANEL
            // =========================================================================
            var pnlHeader = new Panel
            {
                Dock = DockStyle.Top,
                Height = 84,
                BackColor = Color.FromArgb(248, 250, 252),
                Padding = new Padding(24, 16, 24, 12)
            };
            pnlHeader.Paint += (s, e) =>
            {
                using var pen = new Pen(Color.FromArgb(226, 232, 240), 1f);
                e.Graphics.DrawLine(pen, 0, pnlHeader.Height - 1, pnlHeader.Width, pnlHeader.Height - 1);
            };

            var lblTitle = new Label
            {
                Text = "Platform Tier Entitlements Matrix",
                Font = new Font("Segoe UI", 13f, FontStyle.Bold),
                ForeColor = Theme.TextPrimary,
                AutoSize = true,
                Location = new Point(24, 14)
            };

            var lblSubtitle = new Label
            {
                Text = "Configure which system modules and white-label capabilities are enabled for each subscription package.\r\nToggling permissions here immediately governs runtime UI buttons, screens, and subscription marketing inclusions.",
                Font = new Font("Segoe UI", 8.5f),
                ForeColor = Theme.TextSecondary,
                AutoSize = true,
                Location = new Point(24, 40)
            };

            pnlHeader.Controls.AddRange(new Control[] { lblTitle, lblSubtitle });

            // =========================================================================
            // 2. BOTTOM FOOTER PANEL
            // =========================================================================
            var pnlFooter = new Panel
            {
                Dock = DockStyle.Bottom,
                Height = 64,
                BackColor = Color.FromArgb(248, 250, 252),
                Padding = new Padding(24, 14, 24, 14)
            };
            pnlFooter.Paint += (s, e) =>
            {
                using var pen = new Pen(Color.FromArgb(226, 232, 240), 1f);
                e.Graphics.DrawLine(pen, 0, 0, pnlFooter.Width, 0);
            };

            _btnReset = new Button
            {
                Text = "🔄 Reset to Standard Factory Tiers",
                Size = new Size(240, 36),
                Dock = DockStyle.Left,
                FlatStyle = FlatStyle.Flat,
                BackColor = Theme.Surface,
                ForeColor = Theme.TextSecondary,
                Font = new Font("Segoe UI", 9f),
                Cursor = Cursors.Hand
            };
            UiRadiusHelper.StyleSecondaryButton(_btnReset, 6);
            _btnReset.Click += async (s, e) => await ResetDefaultsAsync();

            _lblStatus = new Label
            {
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleCenter,
                Font = new Font("Segoe UI", 8.5f, FontStyle.Italic),
                ForeColor = Theme.TextSecondary,
                Text = string.Empty
            };

            var pnlRightActions = new FlowLayoutPanel
            {
                Dock = DockStyle.Right,
                Width = 280,
                FlowDirection = FlowDirection.RightToLeft,
                BackColor = Color.Transparent
            };

            _btnSave = new Button
            {
                Text = "💾 Save & Apply System-Wide",
                Size = new Size(185, 36),
                BackColor = Theme.Primary,
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 9.5f, FontStyle.Bold),
                Cursor = Cursors.Hand,
                Margin = new Padding(10, 0, 0, 0)
            };
            UiRadiusHelper.StylePrimaryButton(_btnSave, 6);
            _btnSave.Click += async (s, e) => await SaveEntitlementsAsync();

            _btnCancel = new Button
            {
                Text = "Close",
                DialogResult = DialogResult.Cancel,
                Size = new Size(75, 36),
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.Transparent,
                ForeColor = Theme.TextSecondary,
                Font = new Font("Segoe UI", 9f),
                Cursor = Cursors.Hand
            };
            UiRadiusHelper.StyleSecondaryButton(_btnCancel, 6);
            _btnCancel.Click += (s, e) => Close();

            pnlRightActions.Controls.AddRange(new Control[] { _btnSave, _btnCancel });
            pnlFooter.Controls.AddRange(new Control[] { _btnReset, pnlRightActions, _lblStatus });

            // =========================================================================
            // 3. MAIN GRID CONTAINER
            // =========================================================================
            var pnlBody = new Panel
            {
                Dock = DockStyle.Fill,
                Padding = new Padding(24, 16, 24, 12),
                BackColor = Color.White
            };

            _grid = new DataGridView
            {
                Dock = DockStyle.Fill,
                BackgroundColor = Color.White,
                BorderStyle = BorderStyle.None,
                CellBorderStyle = DataGridViewCellBorderStyle.SingleHorizontal,
                GridColor = Color.FromArgb(235, 238, 242),
                EnableHeadersVisualStyles = false,
                RowHeadersVisible = false,
                AllowUserToAddRows = false,
                AllowUserToDeleteRows = false,
                AllowUserToResizeRows = false,
                MultiSelect = false,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                AutoGenerateColumns = false
            };

            // Grid header styling
            _grid.ColumnHeadersHeight = 44;
            _grid.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.DisableResizing;
            _grid.ColumnHeadersDefaultCellStyle.BackColor = Color.FromArgb(243, 246, 249);
            _grid.ColumnHeadersDefaultCellStyle.ForeColor = Color.FromArgb(51, 65, 85);
            _grid.ColumnHeadersDefaultCellStyle.Font = new Font("Segoe UI", 9.5f, FontStyle.Bold);
            _grid.ColumnHeadersDefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleLeft;
            _grid.ColumnHeadersBorderStyle = DataGridViewHeaderBorderStyle.Single;

            // Default cell style
            _grid.DefaultCellStyle.Font = new Font("Segoe UI", 9f);
            _grid.DefaultCellStyle.ForeColor = Theme.TextPrimary;
            _grid.DefaultCellStyle.SelectionBackColor = Color.FromArgb(238, 242, 255);
            _grid.DefaultCellStyle.SelectionForeColor = Theme.TextPrimary;
            _grid.RowTemplate.Height = 46;

            SetupColumns();

            pnlBody.Controls.Add(_grid);

            Controls.Add(pnlBody);
            Controls.Add(pnlFooter);
            Controls.Add(pnlHeader);

            Load += async (s, e) => await LoadDataAsync();
        }

        private void SetupColumns()
        {
            _grid.Columns.Clear();

            // Col 0: Key (Hidden)
            _grid.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = "FeatureKey",
                HeaderText = "Key",
                Visible = false
            });

            // Col 1: Category
            _grid.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = "Category",
                HeaderText = "Category",
                Width = 130,
                ReadOnly = true,
                DefaultCellStyle = new DataGridViewCellStyle
                {
                    ForeColor = Color.FromArgb(100, 116, 139),
                    Font = new Font("Segoe UI", 8.5f, FontStyle.Bold)
                }
            });

            // Col 2: Module Name & Description
            _grid.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = "ModuleName",
                HeaderText = "System Module / Capability",
                AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill,
                ReadOnly = true
            });

            // Col 3: Starter (Tenant A) Checkbox
            var colTenantA = new DataGridViewCheckBoxColumn
            {
                Name = "TenantA",
                HeaderText = "Starter\n(Tenant A)",
                Width = 110,
                FlatStyle = FlatStyle.Flat,
                HeaderCell = { Style = { Alignment = DataGridViewContentAlignment.MiddleCenter } },
                DefaultCellStyle = { Alignment = DataGridViewContentAlignment.MiddleCenter }
            };
            _grid.Columns.Add(colTenantA);

            // Col 4: Professional (Tenant B) Checkbox
            var colTenantB = new DataGridViewCheckBoxColumn
            {
                Name = "TenantB",
                HeaderText = "Professional\n(Tenant B)",
                Width = 120,
                FlatStyle = FlatStyle.Flat,
                HeaderCell = { Style = { Alignment = DataGridViewContentAlignment.MiddleCenter } },
                DefaultCellStyle = { Alignment = DataGridViewContentAlignment.MiddleCenter }
            };
            _grid.Columns.Add(colTenantB);

            // Col 5: Enterprise (Tenant C) Checkbox
            var colTenantC = new DataGridViewCheckBoxColumn
            {
                Name = "TenantC",
                HeaderText = "Enterprise\n(Tenant C)",
                Width = 120,
                FlatStyle = FlatStyle.Flat,
                HeaderCell = { Style = { Alignment = DataGridViewContentAlignment.MiddleCenter } },
                DefaultCellStyle = { Alignment = DataGridViewContentAlignment.MiddleCenter }
            };
            _grid.Columns.Add(colTenantC);
        }

        private async Task LoadDataAsync()
        {
            try
            {
                _lblStatus.Text = "Loading entitlements matrix...";
                _currentMatrix = await _controller.GetTierEntitlementsMatrixAsync();

                _grid.Rows.Clear();

                foreach (var feature in FeatureGate.RegisteredFeatures)
                {
                    bool aVal = _currentMatrix.TryGetValue(FeatureGate.GetMatrixKey(TenantTier.TenantA, feature.Key), out bool a)
                        ? a : feature.DefaultTenantA;

                    bool bVal = _currentMatrix.TryGetValue(FeatureGate.GetMatrixKey(TenantTier.TenantB, feature.Key), out bool b)
                        ? b : feature.DefaultTenantB;

                    bool cVal = _currentMatrix.TryGetValue(FeatureGate.GetMatrixKey(TenantTier.TenantC, feature.Key), out bool c)
                        ? c : feature.DefaultTenantC;

                    int rowIdx = _grid.Rows.Add(
                        feature.Key,
                        feature.Category,
                        $"{feature.DisplayName}  —  {feature.Description}",
                        aVal,
                        bVal,
                        cVal
                    );

                    // Alternate row coloring
                    if (rowIdx % 2 != 0)
                    {
                        _grid.Rows[rowIdx].DefaultCellStyle.BackColor = Color.FromArgb(249, 250, 251);
                    }
                }

                _lblStatus.Text = $"{FeatureGate.RegisteredFeatures.Count} modules loaded.";
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to load tier entitlements: {ex.Message}", "Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
                _lblStatus.Text = "Failed to load.";
            }
        }

        private async Task SaveEntitlementsAsync()
        {
            _btnSave.Enabled = false;
            _btnReset.Enabled = false;
            _lblStatus.Text = "Applying changes system-wide...";

            try
            {
                var newMatrix = new Dictionary<string, bool>(StringComparer.OrdinalIgnoreCase);

                foreach (DataGridViewRow row in _grid.Rows)
                {
                    if (row.IsNewRow) continue;

                    string key = row.Cells["FeatureKey"].Value?.ToString() ?? string.Empty;
                    if (string.IsNullOrWhiteSpace(key)) continue;

                    bool a = row.Cells["TenantA"].Value as bool? ?? false;
                    bool b = row.Cells["TenantB"].Value as bool? ?? false;
                    bool c = row.Cells["TenantC"].Value as bool? ?? false;

                    newMatrix[FeatureGate.GetMatrixKey(TenantTier.TenantA, key)] = a;
                    newMatrix[FeatureGate.GetMatrixKey(TenantTier.TenantB, key)] = b;
                    newMatrix[FeatureGate.GetMatrixKey(TenantTier.TenantC, key)] = c;
                }

                int superAdminId = CurrentSession.UserId > 0 ? CurrentSession.UserId : 1;
                var result = await _controller.SaveTierEntitlementsMatrixAsync(newMatrix, superAdminId);

                if (result.Success)
                {
                    _lblStatus.Text = "Matrix saved and enforced.";
                    MessageBox.Show(
                        "Platform Tier Entitlements matrix successfully saved and applied!\r\n\r\n" +
                        "All tenant UI runtime module buttons, access gates, and subscription marketing inclusions are now strictly enforced with these updated settings.",
                        "Entitlements Updated",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Information);

                    DialogResult = DialogResult.OK;
                    Close();
                }
                else
                {
                    _lblStatus.Text = "Save error.";
                    MessageBox.Show($"Failed to save matrix: {result.Error}", "Save Error",
                        MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"An unexpected error occurred: {ex.Message}", "Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
                _lblStatus.Text = "Error during save.";
            }
            finally
            {
                _btnSave.Enabled = true;
                _btnReset.Enabled = true;
            }
        }

        private async Task ResetDefaultsAsync()
        {
            var confirm = MessageBox.Show(
                "Are you sure you want to reset all tier entitlements to standard factory defaults?\r\n\r\n" +
                "This will revert module permissions and package inclusion lists for Starter, Professional, and Enterprise back to platform standards.",
                "Confirm Reset to Factory Defaults",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Question);

            if (confirm != DialogResult.Yes) return;

            _btnSave.Enabled = false;
            _btnReset.Enabled = false;
            _lblStatus.Text = "Resetting matrix to defaults...";

            try
            {
                int superAdminId = CurrentSession.UserId > 0 ? CurrentSession.UserId : 1;
                var result = await _controller.ResetTierEntitlementsMatrixAsync(superAdminId);

                if (result.Success)
                {
                    await LoadDataAsync();
                    MessageBox.Show("Tier entitlements matrix has been restored to factory defaults.",
                        "Defaults Restored", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
                else
                {
                    MessageBox.Show($"Failed to reset entitlements: {result.Error}", "Reset Error",
                        MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error resetting entitlements: {ex.Message}", "Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                _btnSave.Enabled = true;
                _btnReset.Enabled = true;
            }
        }
    }
}
