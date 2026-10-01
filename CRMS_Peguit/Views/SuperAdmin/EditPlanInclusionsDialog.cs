using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using CRMS_Peguit.domain.entities;
using CRMS_Peguit.winforms.Controllers;
using CRMS_Peguit.winforms.Models.Services;
using CRMS_Peguit.winforms.Services;

// =============================================================================
// EditPlanInclusionsDialog — Super Admin management dialog for customizing
// the included features / capabilities listed for each subscription tier.
// Persisted dynamically into MasterDb.GlobalSettings with full audit trail.
// =============================================================================

namespace CRMS_Peguit.winforms.Views.SuperAdmin
{
    public class EditPlanInclusionsDialog : Form
    {
        private readonly SuperAdminSubscriptionController _controller = new();
        private readonly ComboBox _cmbTier;
        private readonly RichTextBox _txtFeatures;
        private readonly Label _lblInfo;
        private readonly Button _btnSave;
        private readonly Button _btnReset;
        private readonly Button _btnClose;

        private TenantTier _currentTier = TenantTier.TenantA;
        private bool _isDirty = false;

        public EditPlanInclusionsDialog(TenantTier initialTier = TenantTier.TenantA)
        {
            _currentTier = initialTier;

            Text = "Manage Subscription Plan Inclusions";
            Size = new Size(680, 620);
            MinimumSize = new Size(580, 480);
            StartPosition = FormStartPosition.CenterParent;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;
            BackColor = Color.White;
            UiRadiusHelper.StyleModal(this, 8);

            // 1. Bottom Footer (Dock = Bottom, Height = 64)
            var pnlFooter = new Panel
            {
                Dock = DockStyle.Bottom,
                Height = 64,
                BackColor = Color.FromArgb(248, 250, 252),
                Padding = new Padding(24, 14, 24, 14)
            };
            pnlFooter.Paint += (s, e) =>
            {
                using var p = new Pen(Theme.Border, 1f);
                e.Graphics.DrawLine(p, 0, 0, pnlFooter.Width, 0);
            };

            _btnReset = new Button
            {
                Text = "🔄  Reset to Factory Default",
                Size = new Size(200, 36),
                Dock = DockStyle.Left,
                FlatStyle = FlatStyle.Flat,
                BackColor = Theme.Surface,
                ForeColor = Theme.TextSecondary,
                Font = new Font("Segoe UI", 9f),
                Cursor = Cursors.Hand
            };
            UiRadiusHelper.StyleSecondaryButton(_btnReset, 6);
            _btnReset.Click += BtnReset_Click;

            var pnlRightActions = new FlowLayoutPanel
            {
                Dock = DockStyle.Right,
                Width = 240,
                FlowDirection = FlowDirection.RightToLeft,
                BackColor = Color.Transparent
            };

            _btnSave = new Button
            {
                Text = "💾 Save Inclusions",
                Size = new Size(130, 36),
                BackColor = Theme.Primary,
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 9.5f, FontStyle.Bold),
                Cursor = Cursors.Hand,
                Margin = new Padding(10, 0, 0, 0)
            };
            UiRadiusHelper.StylePrimaryButton(_btnSave, 6);
            _btnSave.Click += BtnSave_Click;

            _btnClose = new Button
            {
                Text = "Close",
                DialogResult = DialogResult.OK,
                Size = new Size(80, 36),
                BackColor = Theme.Surface,
                ForeColor = Theme.TextPrimary,
                Font = new Font("Segoe UI", 9f),
                Cursor = Cursors.Hand,
                Margin = new Padding(0)
            };
            UiRadiusHelper.StyleSecondaryButton(_btnClose, 6);

            pnlRightActions.Controls.Add(_btnSave);
            pnlRightActions.Controls.Add(_btnClose);
            pnlFooter.Controls.Add(_btnReset);
            pnlFooter.Controls.Add(pnlRightActions);

            // 2. Central Content Panel (Dock = Fill)
            var pnlContent = new Panel
            {
                Dock = DockStyle.Fill,
                Padding = new Padding(28, 20, 28, 16),
                BackColor = Color.White
            };

            var lblHeader = new Label
            {
                Text = "Configure Tier Included Features",
                Font = new Font("Segoe UI", 13.5f, FontStyle.Bold),
                ForeColor = Theme.TextPrimary,
                AutoSize = true,
                Location = new Point(28, 16)
            };
            pnlContent.Controls.Add(lblHeader);

            var lblSub = new Label
            {
                Text = "Customize the features and capabilities advertised under each subscription tier. One feature per line.",
                Font = new Font("Segoe UI", 9.5f),
                ForeColor = Theme.TextSecondary,
                AutoSize = true,
                Location = new Point(28, 44)
            };
            pnlContent.Controls.Add(lblSub);

            // Tier Selector
            var lblSelectTier = new Label
            {
                Text = "Select Plan Tier:",
                Font = new Font("Segoe UI", 9.5f, FontStyle.Bold),
                ForeColor = Theme.TextPrimary,
                AutoSize = true,
                Location = new Point(28, 80)
            };
            pnlContent.Controls.Add(lblSelectTier);

            _cmbTier = new ComboBox
            {
                DropDownStyle = ComboBoxStyle.DropDownList,
                Location = new Point(28, 104),
                Width = 400,
                Font = new Font("Segoe UI", 10f)
            };
            UiRadiusHelper.StyleStandardComboBox(_cmbTier);
            _cmbTier.Items.Add("Tenant A (Starter Plan)");
            _cmbTier.Items.Add("Tenant B (Professional Plan)");
            _cmbTier.Items.Add("Tenant C (Enterprise Plan)");
            _cmbTier.SelectedIndex = _currentTier switch
            {
                TenantTier.TenantC => 2,
                TenantTier.TenantB => 1,
                _ => 0
            };
            _cmbTier.SelectedIndexChanged += async (_, _) => await SwitchTierAsync();
            pnlContent.Controls.Add(_cmbTier);

            _lblInfo = new Label
            {
                Text = "Edit the feature lines below. Empty lines are automatically omitted upon saving.",
                Font = new Font("Segoe UI", 9f, FontStyle.Italic),
                ForeColor = Theme.TextSecondary,
                AutoSize = true,
                Location = new Point(28, 144)
            };
            pnlContent.Controls.Add(_lblInfo);

            // Text editor container
            var pnlTextBorder = new Panel
            {
                Location = new Point(28, 168),
                Size = new Size(pnlContent.Width - 56, pnlContent.Height - 180),
                Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right,
                BackColor = Color.FromArgb(226, 232, 240),
                Padding = new Padding(1)
            };
            UiRadiusHelper.StyleCard(pnlTextBorder, 6);

            _txtFeatures = new RichTextBox
            {
                Dock = DockStyle.Fill,
                BorderStyle = BorderStyle.None,
                Font = new Font("Segoe UI", 10f),
                ForeColor = Color.FromArgb(15, 23, 42),
                BackColor = Color.White,
                Padding = new Padding(12)
            };
            _txtFeatures.TextChanged += (_, _) => _isDirty = true;
            pnlTextBorder.Controls.Add(_txtFeatures);
            pnlContent.Controls.Add(pnlTextBorder);

            Controls.Add(pnlContent);
            Controls.Add(pnlFooter);

            _ = LoadCurrentTierFeaturesAsync();
        }

        private async Task SwitchTierAsync()
        {
            if (_isDirty)
            {
                var choice = MessageBox.Show(
                    "You have unsaved changes in the current tier. Do you want to save before switching?",
                    "Unsaved Changes",
                    MessageBoxButtons.YesNoCancel,
                    MessageBoxIcon.Question);

                if (choice == DialogResult.Yes)
                {
                    await SaveFeaturesAsync();
                }
                else if (choice == DialogResult.Cancel)
                {
                    // Revert selection
                    _cmbTier.SelectedIndex = _currentTier switch
                    {
                        TenantTier.TenantC => 2,
                        TenantTier.TenantB => 1,
                        _ => 0
                    };
                    return;
                }
            }

            _currentTier = _cmbTier.SelectedIndex switch
            {
                2 => TenantTier.TenantC,
                1 => TenantTier.TenantB,
                _ => TenantTier.TenantA
            };

            await LoadCurrentTierFeaturesAsync();
        }

        private async Task LoadCurrentTierFeaturesAsync()
        {
            _txtFeatures.Enabled = false;
            var list = await _controller.GetPlanInclusionsAsync(_currentTier);
            _txtFeatures.Text = string.Join("\r\n", list);
            _txtFeatures.Enabled = true;
            _isDirty = false;
        }

        private async void BtnSave_Click(object? sender, EventArgs e)
        {
            await SaveFeaturesAsync();
        }

        private async Task SaveFeaturesAsync()
        {
            var lines = _txtFeatures.Text
                .Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.RemoveEmptyEntries)
                .Select(l => l.Trim())
                .Where(l => !string.IsNullOrWhiteSpace(l))
                .ToList();

            if (lines.Count == 0)
            {
                MessageBox.Show("Please enter at least one feature line for this tier.", "Validation", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            _btnSave.Enabled = false;
            _btnSave.Text = "Saving...";

            var (success, error) = await _controller.SavePlanInclusionsAsync(_currentTier, lines);

            _btnSave.Enabled = true;
            _btnSave.Text = "💾 Save Inclusions";

            if (success)
            {
                _isDirty = false;
                MessageBox.Show($"Plan inclusions for {_currentTier} saved successfully.", "Saved", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            else
            {
                MessageBox.Show($"Failed to save inclusions: {error}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private async void BtnReset_Click(object? sender, EventArgs e)
        {
            var choice = MessageBox.Show(
                $"Reset {_currentTier} inclusions back to the built-in system defaults?",
                "Confirm Reset",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Question);

            if (choice != DialogResult.Yes) return;

            var (success, error) = await _controller.ResetPlanInclusionsAsync(_currentTier);
            if (success)
            {
                await LoadCurrentTierFeaturesAsync();
                MessageBox.Show($"{_currentTier} inclusions have been reset to platform defaults.", "Reset Done", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            else
            {
                MessageBox.Show($"Failed to reset: {error}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
    }
}
