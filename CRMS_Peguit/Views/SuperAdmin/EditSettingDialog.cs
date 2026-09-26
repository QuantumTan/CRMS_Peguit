using System;
using System.Drawing;
using System.Windows.Forms;
using CRMS_Peguit.winforms.Models.Services;

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
            Size = new Size(460, 260);
            StartPosition = FormStartPosition.CenterParent;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;
            BackColor = Theme.Surface;

            var pnl = new Panel { Dock = DockStyle.Fill, Padding = new Padding(28) };
            Controls.Add(pnl);

            var lblHeader = new Label
            {
                Text = "Edit System Setting",
                Font = new Font("Segoe UI", 13f, FontStyle.Bold),
                ForeColor = Theme.TextPrimary,
                AutoSize = true,
                Location = new Point(0, 0)
            };
            pnl.Controls.Add(lblHeader);

            var lblKey = new Label
            {
                Text = $"Setting: {settingKey}",
                Font = new Font("Segoe UI", 9.5f),
                ForeColor = Theme.TextSecondary,
                AutoSize = true,
                Location = new Point(0, 30)
            };
            pnl.Controls.Add(lblKey);

            var lblVal = new Label
            {
                Text = "New Value:",
                Font = new Font("Segoe UI", 9.5f, FontStyle.Bold),
                ForeColor = Theme.TextPrimary,
                AutoSize = true,
                Location = new Point(0, 68)
            };
            pnl.Controls.Add(lblVal);

            _txtValue = new TextBox
            {
                Font = new Font("Segoe UI", 10.5f),
                Location = new Point(0, 90),
                Width = 400,
                Text = currentValue
            };
            pnl.Controls.Add(_txtValue);

            var lblNote = new Label
            {
                Text = "This change will be recorded with your user ID and current timestamp.",
                Font = new Font("Segoe UI", 8.5f, FontStyle.Italic),
                ForeColor = Theme.TextSecondary,
                AutoSize = true,
                Location = new Point(0, 120)
            };
            pnl.Controls.Add(lblNote);

            var btnCancel = new Button
            {
                Text = "Cancel",
                DialogResult = DialogResult.Cancel,
                Location = new Point(196, 148),
                Size = new Size(90, 36),
                Font = new Font("Segoe UI", 9.5f),
                Cursor = Cursors.Hand
            };
            UiRadiusHelper.StyleButton(btnCancel, 6);
            pnl.Controls.Add(btnCancel);

            var btnSave = new Button
            {
                Text = "Save",
                Location = new Point(308, 148),
                Size = new Size(92, 36),
                Font = new Font("Segoe UI", 9.5f, FontStyle.Bold),
                BackColor = Theme.Primary,
                ForeColor = Color.White,
                Cursor = Cursors.Hand
            };
            UiRadiusHelper.StyleButton(btnSave, 6);
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
            pnl.Controls.Add(btnSave);

            AcceptButton = btnSave;
            CancelButton = btnCancel;
        }
    }
}
