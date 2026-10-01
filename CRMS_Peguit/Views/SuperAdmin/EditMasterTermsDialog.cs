using System;
using System.Drawing;
using System.Threading.Tasks;
using System.Windows.Forms;
using CRMS_Peguit.winforms.Controllers;
using CRMS_Peguit.winforms.Models.Services;
using CRMS_Peguit.winforms.Services;

namespace CRMS_Peguit.winforms.Views.SuperAdmin
{
    public class EditMasterTermsDialog : Form
    {
        private readonly SuperAdminSubscriptionController _controller = new();
        private RichTextBox _txtTerms = null!;
        private Button _btnSave = null!;
        private Button _btnReset = null!;
        private Button _btnCancel = null!;

        public EditMasterTermsDialog()
        {
            Text = "Platform Master Terms & Conditions — Management";
            Size = new Size(760, 720);
            MinimumSize = new Size(640, 520);
            StartPosition = FormStartPosition.CenterParent;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = true;
            MinimizeBox = false;
            BackColor = Color.White;
            UiRadiusHelper.StyleModal(this, 8);

            InitializeComponent();
            _ = LoadTermsAsync();
        }

        private void InitializeComponent()
        {
            // 1. Fixed Bottom Footer (Dock = Bottom)
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
                Text = "🔄  Reset to Platform Default",
                Size = new Size(200, 36),
                Dock = DockStyle.Left,
                FlatStyle = FlatStyle.Flat,
                BackColor = Theme.Surface,
                ForeColor = Theme.TextSecondary,
                Font = new Font("Segoe UI", 9f),
                Cursor = Cursors.Hand
            };
            UiRadiusHelper.StyleButton(_btnReset, 6);
            _btnReset.Click += BtnReset_Click;
            pnlFooter.Controls.Add(_btnReset);

            var pnlActionsRight = new FlowLayoutPanel
            {
                Dock = DockStyle.Right,
                Width = 260,
                FlowDirection = FlowDirection.RightToLeft,
                BackColor = Color.Transparent
            };

            _btnSave = new Button
            {
                Text = "💾  Save Master Terms",
                Size = new Size(150, 36),
                FlatStyle = FlatStyle.Flat,
                BackColor = Theme.Primary,
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 9.5f, FontStyle.Bold),
                Cursor = Cursors.Hand,
                Margin = new Padding(8, 0, 0, 0)
            };
            UiRadiusHelper.StyleButton(_btnSave, 6);
            _btnSave.Click += BtnSave_Click;

            _btnCancel = new Button
            {
                Text = "Cancel",
                Size = new Size(90, 36),
                FlatStyle = FlatStyle.Flat,
                BackColor = Theme.Surface,
                ForeColor = Theme.TextPrimary,
                Font = new Font("Segoe UI", 9f),
                Cursor = Cursors.Hand,
                DialogResult = DialogResult.Cancel,
                Margin = new Padding(0)
            };
            UiRadiusHelper.StyleButton(_btnCancel, 6);
            _btnCancel.Click += (_, _) => Close();

            pnlActionsRight.Controls.Add(_btnSave);
            pnlActionsRight.Controls.Add(_btnCancel);
            pnlFooter.Controls.Add(pnlActionsRight);

            // 2. Header Panel (Dock = Top)
            var pnlHeader = new Panel
            {
                Dock = DockStyle.Top,
                Height = 84,
                BackColor = Theme.Surface,
                Padding = new Padding(24, 16, 24, 12)
            };
            pnlHeader.Paint += (s, e) =>
            {
                using var p = new Pen(Theme.Border, 1f);
                e.Graphics.DrawLine(p, 0, pnlHeader.Height - 1, pnlHeader.Width, pnlHeader.Height - 1);
            };

            var lblTitle = new Label
            {
                Text = "Master Service Agreement & Terms of Service",
                Font = new Font("Segoe UI", 13f, FontStyle.Bold),
                ForeColor = Theme.TextPrimary,
                AutoSize = true,
                Location = new Point(24, 16)
            };

            var lblSub = new Label
            {
                Text = "These master terms are presented to and legally accepted by prospective tenants before onboarding and database provisioning.",
                Font = new Font("Segoe UI", 8.5f),
                ForeColor = Theme.TextSecondary,
                AutoSize = true,
                Location = new Point(24, 46)
            };

            pnlHeader.Controls.Add(lblTitle);
            pnlHeader.Controls.Add(lblSub);

            // 3. Central Edit Body (Dock = Fill)
            var pnlBody = new Panel
            {
                Dock = DockStyle.Fill,
                Padding = new Padding(24, 16, 24, 16),
                BackColor = Color.White
            };

            _txtTerms = new RichTextBox
            {
                Dock = DockStyle.Fill,
                Font = new Font("Segoe UI", 9.5f),
                ForeColor = Color.FromArgb(15, 23, 42),
                BackColor = Color.FromArgb(248, 250, 252),
                BorderStyle = BorderStyle.FixedSingle,
                ScrollBars = RichTextBoxScrollBars.Vertical,
                AcceptsTab = true
            };
            pnlBody.Controls.Add(_txtTerms);

            // WinForms docking order: Add Fill last so Bottom and Top take precedence!
            Controls.Add(pnlBody);
            Controls.Add(pnlHeader);
            Controls.Add(pnlFooter);

            AcceptButton = _btnSave;
            CancelButton = _btnCancel;
        }

        private async Task LoadTermsAsync()
        {
            _txtTerms.Enabled = false;
            _txtTerms.Text = "Loading master terms...";

            string terms = await _controller.GetMasterTermsAsync();
            _txtTerms.Text = terms;
            _txtTerms.Enabled = true;
        }

        private void BtnReset_Click(object? sender, EventArgs e)
        {
            var confirm = MessageBox.Show(
                "Reset master terms to platform factory defaults?",
                "Confirm Reset",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Question);

            if (confirm == DialogResult.Yes)
            {
                _txtTerms.Text = TenantTermsAndConditionsDialog.DefaultTermsText;
            }
        }

        private async void BtnSave_Click(object? sender, EventArgs e)
        {
            if (string.IsNullOrWhiteSpace(_txtTerms.Text))
            {
                MessageBox.Show("Master terms content cannot be blank.", "Validation Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            _btnSave.Enabled = false;
            _btnSave.Text = "Saving...";

            try
            {
                var (success, error) = await _controller.SaveMasterTermsAsync(_txtTerms.Text.Trim());
                if (success)
                {
                    MessageBox.Show(
                        "Master Terms & Conditions updated successfully.\nAll subsequent tenant onboardings will present this agreement.",
                        "Terms Saved",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Information);
                    DialogResult = DialogResult.OK;
                    Close();
                }
                else
                {
                    MessageBox.Show($"Failed to save master terms: {error}", "Save Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error saving terms: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                _btnSave.Enabled = true;
                _btnSave.Text = "💾  Save Master Terms";
            }
        }
    }
}
