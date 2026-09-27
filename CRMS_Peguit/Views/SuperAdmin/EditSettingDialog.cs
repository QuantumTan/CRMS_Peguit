using System;
using System.Drawing;
using System.Windows.Forms;
using CRMS_Peguit.winforms.Models.Services;
using CRMS_Peguit.winforms.Services;

// =============================================================================
// EditSettingDialog — Edit a single SystemSetting value.
// Shows the key (read-only) and a TextBox for the new value.
// =============================================================================

namespace CRMS_Peguit.winforms.Views.SuperAdmin
{
    public class EditSettingDialog : Form
    {
        public string NewValue { get; private set; } = string.Empty;
        private readonly TextBox _txtValue;

        public EditSettingDialog(string settingKey, string currentValue)
        {
            Text = $"Edit Setting — {settingKey}";
            Size = new Size(500, 330);
            UiRadiusHelper.StyleModal(this, 8);

            var pnlContent = new Panel
            {
                Dock = DockStyle.Fill,
                Padding = new Padding(24),
                BackColor = Color.White,
                AutoScroll = true
            };

            var lblHeader = new Label
            {
                Text = "Edit System Setting",
                Font = new Font("Segoe UI", 13.5f, FontStyle.Bold),
                ForeColor = Theme.TextPrimary,
                AutoSize = true,
                Location = new Point(24, 20)
            };
            pnlContent.Controls.Add(lblHeader);

            var lblKey = new Label
            {
                Text = $"Setting: {settingKey}",
                Font = new Font("Segoe UI", 9.5f),
                ForeColor = Theme.TextSecondary,
                AutoSize = true,
                Location = new Point(24, 52)
            };
            pnlContent.Controls.Add(lblKey);

            var lblVal = new Label
            {
                Text = "New Value:",
                Font = new Font("Segoe UI", 9.5f, FontStyle.Bold),
                ForeColor = Theme.TextPrimary,
                AutoSize = true,
                Location = new Point(24, 88)
            };
            pnlContent.Controls.Add(lblVal);

            _txtValue = new TextBox
            {
                Location = new Point(24, 112),
                Width = 412,
                Text = currentValue
            };
            UiRadiusHelper.StyleStandardInput(_txtValue);
            pnlContent.Controls.Add(_txtValue);

            var lblNote = new Label
            {
                Text = "This change will be recorded with your user ID and current timestamp.",
                Font = new Font("Segoe UI", 8.5f, FontStyle.Italic),
                ForeColor = Theme.TextSecondary,
                AutoSize = true,
                Location = new Point(24, 150)
            };
            pnlContent.Controls.Add(lblNote);

            // Footer Panel
            var pnlFooter = new Panel
            {
                Dock = DockStyle.Bottom,
                Height = 60,
                BackColor = Color.FromArgb(248, 250, 252)
            };

            var btnCancel = new Button
            {
                Text = "Cancel",
                DialogResult = DialogResult.Cancel,
                Location = new Point(250, 12),
                Size = new Size(100, 36),
                Anchor = AnchorStyles.Top | AnchorStyles.Right
            };
            UiRadiusHelper.StyleSecondaryButton(btnCancel, 6);
            pnlFooter.Controls.Add(btnCancel);

            var btnSave = new Button
            {
                Text = "Save",
                Location = new Point(360, 12),
                Size = new Size(100, 36),
                Anchor = AnchorStyles.Top | AnchorStyles.Right
            };
            UiRadiusHelper.StylePrimaryButton(btnSave, 6);
            btnSave.Click += (_, _) =>
            {
                if (string.IsNullOrWhiteSpace(_txtValue.Text))
                {
                    MessageBox.Show("Value cannot be empty.", "Validation",
                        MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }
                NewValue = _txtValue.Text.Trim();
                DialogResult = DialogResult.OK;
                Close();
            };
            pnlFooter.Controls.Add(btnSave);

            Controls.Add(pnlContent);
            Controls.Add(pnlFooter);

            AcceptButton = btnSave;
            CancelButton = btnCancel;
        }
    }
}
