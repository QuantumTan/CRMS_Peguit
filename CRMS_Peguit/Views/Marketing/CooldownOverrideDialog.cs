using System;
using System.Drawing;
using System.Windows.Forms;
using CRMS_Peguit.winforms.Models.Services;
using CRMS_Peguit.winforms.Services;

namespace CRMS_Peguit.winforms.Views.Marketing
{
    public class CooldownOverrideDialog : Form
    {
        private TextBox _txtReason = null!;
        private Button _btnConfirm = null!;
        private Button _btnCancel = null!;

        public string OverrideReason { get; private set; } = string.Empty;

        public CooldownOverrideDialog(string customerName, DateTime? lastSentDate, int daysRemaining)
        {
            InitializeComponentLayout(customerName, lastSentDate, daysRemaining);
        }

        private void InitializeComponentLayout(string customerName, DateTime? lastSentDate, int daysRemaining)
        {
            this.Text = "Anti-Fatigue Cooldown Override Required";
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.ShowInTaskbar = false;
            this.StartPosition = FormStartPosition.CenterParent;
            this.ClientSize = new Size(540, 360);
            this.BackColor = Color.White;

            // Header panel with Amber warning
            var pnlHeader = new Panel
            {
                Dock = DockStyle.Top,
                Height = 70,
                BackColor = Color.FromArgb(255, 251, 235), // light amber
                Padding = new Padding(24, 14, 24, 14)
            };

            pnlHeader.Paint += (s, e) =>
            {
                using var b = new SolidBrush(Color.FromArgb(217, 119, 6)); // amber accent
                e.Graphics.FillRectangle(b, 0, 0, 5, pnlHeader.Height);
            };

            var lblTitle = new Label
            {
                Text = "⚠ 30-Day Anti-Fatigue Cooldown Active",
                Font = new Font("Segoe UI", 12f, FontStyle.Bold),
                ForeColor = Color.FromArgb(180, 83, 9),
                Location = new Point(20, 12),
                AutoSize = true
            };

            var lblSub = new Label
            {
                Text = "NEXA Real Estate policy restricts retention outreach to max 1 email every 30 days.",
                Font = new Font("Segoe UI", 9f),
                ForeColor = Color.FromArgb(146, 64, 14),
                Location = new Point(20, 38),
                AutoSize = true
            };

            pnlHeader.Controls.Add(lblTitle);
            pnlHeader.Controls.Add(lblSub);

            // Body
            var pnlBody = new Panel
            {
                Dock = DockStyle.Fill,
                AutoScroll = true,
                Padding = new Padding(24, 16, 24, 16)
            };

            var lblClientInfo = new Label
            {
                Text = $"Target Client: {customerName}\n" +
                       $"Last Retention Email Sent: {(lastSentDate.HasValue ? lastSentDate.Value.ToString("MMM dd, yyyy") : "Recently")}\n" +
                       $"Cooldown Remaining: {daysRemaining} day(s)",
                Font = new Font("Segoe UI", 9.5f, FontStyle.Bold),
                ForeColor = Color.FromArgb(15, 23, 42),
                Location = new Point(10, 10),
                Size = new Size(500, 60)
            };

            var lblPrompt = new Label
            {
                Text = "To bypass this cooldown, a documented business justification is mandatory and will be recorded in the tenant audit log:",
                Font = new Font("Segoe UI", 9f),
                ForeColor = Color.FromArgb(71, 85, 105),
                Location = new Point(10, 80),
                Size = new Size(500, 36)
            };

            _txtReason = new TextBox
            {
                Location = new Point(10, 120),
                Size = new Size(500, 70),
                Multiline = true,
                ScrollBars = ScrollBars.Vertical,
                Font = new Font("Segoe UI", 9.5f)
            };

            pnlBody.Controls.Add(lblClientInfo);
            pnlBody.Controls.Add(lblPrompt);
            pnlBody.Controls.Add(_txtReason);

            // Bottom action panel
            var pnlBottom = new Panel
            {
                Dock = DockStyle.Bottom,
                Height = 60,
                BackColor = Color.FromArgb(248, 250, 252),
                Padding = new Padding(24, 12, 24, 12)
            };

            _btnCancel = new Button
            {
                Text = "Cancel",
                Size = new Size(84, 38),
                BackColor = Color.White,
                ForeColor = Color.FromArgb(71, 85, 105),
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 9f),
                Location = new Point(220, 11),
                Anchor = AnchorStyles.Top | AnchorStyles.Right,
                Cursor = Cursors.Hand
            };
            _btnCancel.FlatAppearance.BorderColor = Color.FromArgb(226, 232, 240);
            UiRadiusHelper.StyleButton(_btnCancel, 8);
            _btnCancel.Click += (s, e) => { this.DialogResult = DialogResult.Cancel; this.Close(); };

            _btnConfirm = new Button
            {
                Text = "Confirm & Dispatch with Override",
                Size = new Size(204, 38),
                BackColor = Color.FromArgb(217, 119, 6),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 9f, FontStyle.Bold),
                Location = new Point(312, 11),
                Anchor = AnchorStyles.Top | AnchorStyles.Right,
                Cursor = Cursors.Hand
            };
            _btnConfirm.FlatAppearance.BorderSize = 0;
            UiRadiusHelper.StyleButton(_btnConfirm, 8);
            _btnConfirm.Click += (s, e) =>
            {
                if (string.IsNullOrWhiteSpace(_txtReason.Text))
                {
                    MessageBox.Show("Please enter a documented justification to override the anti-fatigue cooldown.", "Validation Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    _txtReason.Focus();
                    return;
                }

                OverrideReason = _txtReason.Text.Trim();
                this.DialogResult = DialogResult.OK;
                this.Close();
            };

            pnlBottom.Controls.Add(_btnCancel);
            pnlBottom.Controls.Add(_btnConfirm);

            this.Controls.Add(pnlBody);
            this.Controls.Add(pnlBottom);
            this.Controls.Add(pnlHeader);
        }
    }
}
