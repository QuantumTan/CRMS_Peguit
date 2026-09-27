using System;
using System.Drawing;
using System.Windows.Forms;
using CRMS_Peguit.winforms.Controllers;
using CRMS_Peguit.winforms.Models.Services;

// =============================================================================
// RestoreConfirmDialog — Mandatory two-step confirmation for the Restore action.
//
// SAFETY DESIGN:
//   Step 1: Shows the backup metadata (what will be restored, from when, by whom).
//           Asks "Are you sure you want to restore?" with a prominent warning.
//   Step 2: User must type the word RESTORE (case-insensitive) exactly into the
//           confirmation TextBox. The "Confirm Restore" button is DISABLED until
//           the text matches.
//
// This dialog makes one-click restore STRUCTURALLY IMPOSSIBLE — the confirm
// button cannot be clicked in a single interaction.
// =============================================================================

namespace CRMS_Peguit.winforms.Views.SuperAdmin
{
    public class RestoreConfirmDialog : Form
    {
        private readonly BackupLogDto _backup;
        private readonly TextBox _txtConfirm;
        private readonly Button _btnConfirm;

        private const string RequiredWord = "RESTORE";

        public RestoreConfirmDialog(BackupLogDto backup)
        {
            _backup = backup;

            Text = "Confirm Restore — Destructive Action";
            Size = new Size(520, 480);
            StartPosition = FormStartPosition.CenterParent;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;
            BackColor = Theme.Surface;

            var pnl = new Panel { Dock = DockStyle.Fill, Padding = new Padding(28) };
            Controls.Add(pnl);

            int y = 0;

            // Danger header
            var pnlDanger = new Panel
            {
                Location = new Point(0, y),
                Size = new Size(460, 56),
                BackColor = Color.FromArgb(254, 226, 226),
                Padding = new Padding(12, 8, 12, 8)
            };
            UiRadiusHelper.StyleCard(pnlDanger, 6);

            var lblDanger = new Label
            {
                Text = "⚠  DESTRUCTIVE ACTION — This will restore the database from backup",
                Font = new Font("Segoe UI", 10f, FontStyle.Bold),
                ForeColor = Color.FromArgb(153, 27, 27),
                Dock = DockStyle.Top,
                Height = 24
            };
            var lblDangerSub = new Label
            {
                Text = "All data written after this backup was taken may be permanently lost.",
                Font = new Font("Segoe UI", 9f),
                ForeColor = Color.FromArgb(153, 27, 27),
                Dock = DockStyle.Top,
                Height = 20
            };
            pnlDanger.Controls.Add(lblDangerSub);
            pnlDanger.Controls.Add(lblDanger);
            pnl.Controls.Add(pnlDanger);
            y += 68;

            // Step 1: Backup metadata
            var lblStep1 = new Label
            {
                Text = "STEP 1 — Review what will be restored:",
                Font = new Font("Segoe UI", 9.5f, FontStyle.Bold),
                ForeColor = Theme.TextPrimary,
                AutoSize = true,
                Location = new Point(0, y)
            };
            pnl.Controls.Add(lblStep1);
            y += 26;

            var pnlMeta = new Panel
            {
                Location = new Point(0, y),
                Size = new Size(460, 120),
                BackColor = Color.FromArgb(248, 250, 252),
                Padding = new Padding(16, 12, 16, 12)
            };
            UiRadiusHelper.StyleCard(pnlMeta, 6);

            AddMetaRow(pnlMeta, "Backup ID:", _backup.BackupId.ToString(), 0);
            AddMetaRow(pnlMeta, "Backup Date:", _backup.BackupDate.ToLocalTime().ToString("dddd, MMMM dd, yyyy  h:mm:ss tt"), 24);
            AddMetaRow(pnlMeta, "File / Location:", _backup.FileLocation, 48);
            AddMetaRow(pnlMeta, "Performed By:", _backup.PerformedByName, 72);
            AddMetaRow(pnlMeta, "Backup Status:", _backup.Status, 96);

            pnl.Controls.Add(pnlMeta);
            y += 132;

            // Step 2: Type RESTORE
            var lblStep2 = new Label
            {
                Text = $"STEP 2 — Type \"{RequiredWord}\" to enable the confirm button:",
                Font = new Font("Segoe UI", 9.5f, FontStyle.Bold),
                ForeColor = Theme.TextPrimary,
                AutoSize = true,
                Location = new Point(0, y)
            };
            pnl.Controls.Add(lblStep2);
            y += 26;

            _txtConfirm = new TextBox
            {
                Font = new Font("Segoe UI", 11f),
                Location = new Point(0, y),
                Width = 460,
                PlaceholderText = $"Type {RequiredWord} here...",
                CharacterCasing = CharacterCasing.Upper
            };
            _txtConfirm.TextChanged += (_, _) =>
            {
                bool matches = _txtConfirm.Text.Trim()
                    .Equals(RequiredWord, StringComparison.OrdinalIgnoreCase);
                if (_btnConfirm is null) return;
                _btnConfirm.Enabled = matches;
                _btnConfirm.BackColor = matches ? Color.FromArgb(153, 27, 27) : Color.FromArgb(203, 213, 225);
                _btnConfirm.ForeColor = matches ? Color.White : Color.FromArgb(156, 163, 175);
            };
            pnl.Controls.Add(_txtConfirm);
            y += 38;

            var lblHint = new Label
            {
                Text = $"The confirm button will remain disabled until you type \"{RequiredWord}\" exactly.",
                Font = new Font("Segoe UI", 8.5f, FontStyle.Italic),
                ForeColor = Theme.TextSecondary,
                AutoSize = true,
                Location = new Point(0, y)
            };
            pnl.Controls.Add(lblHint);
            y += 30;

            // Buttons
            var btnCancel = new Button
            {
                Text = "Cancel — Do Not Restore",
                DialogResult = DialogResult.Cancel,
                Location = new Point(0, y),
                Size = new Size(200, 38),
                Font = new Font("Segoe UI", 9.5f),
                Cursor = Cursors.Hand,
                BackColor = Theme.Surface,
                ForeColor = Theme.TextPrimary
            };
            UiRadiusHelper.StyleButton(btnCancel, 6);
            pnl.Controls.Add(btnCancel);

            // Confirm button starts DISABLED — only enables after typing RESTORE
            _btnConfirm = new Button
            {
                Text = "Confirm Restore",
                DialogResult = DialogResult.OK,
                Location = new Point(250, y),
                Size = new Size(210, 38),
                Font = new Font("Segoe UI", 9.5f, FontStyle.Bold),
                BackColor = Color.FromArgb(203, 213, 225), // disabled gray initially
                ForeColor = Color.FromArgb(156, 163, 175),
                Cursor = Cursors.Hand,
                Enabled = false // CANNOT be clicked until user types RESTORE
            };
            UiRadiusHelper.StyleButton(_btnConfirm, 6);
            pnl.Controls.Add(_btnConfirm);

            CancelButton = btnCancel;
            // AcceptButton is intentionally NOT set — pressing Enter cannot bypass Step 2
        }

        private static void AddMetaRow(Panel parent, string label, string value, int y)
        {
            parent.Controls.Add(new Label
            {
                Text = label,
                Font = new Font("Segoe UI", 8.5f, FontStyle.Bold),
                ForeColor = Theme.TextSecondary,
                AutoSize = true,
                Location = new Point(0, y)
            });
            parent.Controls.Add(new Label
            {
                Text = value,
                Font = new Font("Segoe UI", 8.5f),
                ForeColor = Theme.TextPrimary,
                AutoSize = true,
                Location = new Point(120, y)
            });
        }
    }
}
