using System;
using System.Drawing;
using System.Windows.Forms;
using CRMS_Peguit.domain.entities;
using CRMS_Peguit.winforms.Models.Services;

namespace CRMS_Peguit.winforms.Views.FollowUps
{
    public class CompleteFollowUpDialog : Form
    {
        public bool LogActivity => _chkLogActivity.Checked;
        public string ActivityType => _cmbActivityType.SelectedItem?.ToString() ?? "Call";
        public string ActivityNotes => _txtNotes.Text.Trim();

        private readonly TaskReminder _reminder;
        private CheckBox _chkLogActivity = null!;
        private Panel _pnlActivityOptions = null!;
        private ComboBox _cmbActivityType = null!;
        private TextBox _txtNotes = null!;
        private Button _btnComplete = null!;
        private Button _btnCancel = null!;

        public CompleteFollowUpDialog(TaskReminder reminder)
        {
            _reminder = reminder;
            InitializeComponent();
            ApplyStyling();
        }

        private void InitializeComponent()
        {
            Text = "Complete Follow-Up";
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;
            ShowInTaskbar = false;
            StartPosition = FormStartPosition.CenterParent;
            ClientSize = new Size(520, 480);
            BackColor = Color.White;

            // Header panel
            var pnlHeader = new Panel
            {
                Dock = DockStyle.Top,
                Height = 70,
                BackColor = Color.FromArgb(240, 253, 244), // light emerald tint
                Padding = new Padding(24, 14, 24, 14)
            };

            var lblHeaderTitle = new Label
            {
                Text = "✓ Mark Follow-Up Complete",
                Font = new Font("Segoe UI", 13.5f, FontStyle.Bold),
                ForeColor = Color.FromArgb(22, 101, 52),
                Location = new Point(20, 12),
                AutoSize = true
            };

            var lblHeaderSub = new Label
            {
                Text = "Mark this task as completed and optionally record a timeline activity.",
                Font = new Font("Segoe UI", 9f),
                ForeColor = Color.FromArgb(71, 85, 105),
                Location = new Point(22, 40),
                AutoSize = true
            };

            pnlHeader.Controls.Add(lblHeaderTitle);
            pnlHeader.Controls.Add(lblHeaderSub);

            // Summary info card
            var pnlSummary = new Panel
            {
                Location = new Point(24, 16),
                Size = new Size(472, 68),
                BackColor = Color.FromArgb(248, 250, 252)
            };
            UiRadiusHelper.ApplyRoundedCorners(pnlSummary, 8);

            string clientName = _reminder.RelatedCustomer?.FullName
                                ?? _reminder.RelatedLead?.FullName
                                ?? "Assigned Client";
            string clientTag = _reminder.RelatedCustomerId.HasValue ? "Customer" : "Lead";

            var lblTaskTitle = new Label
            {
                Text = _reminder.Title,
                Font = new Font("Segoe UI", 10.5f, FontStyle.Bold),
                ForeColor = Color.FromArgb(15, 23, 42),
                Location = new Point(14, 10),
                Size = new Size(440, 22),
                AutoEllipsis = true
            };

            var lblTaskClient = new Label
            {
                Text = $"Target: [{clientTag}] {clientName}  ·  Due: {_reminder.DueDate.ToLocalTime():MMM dd, yyyy · hh:mm tt}",
                Font = new Font("Segoe UI", 8.5f),
                ForeColor = Color.FromArgb(100, 116, 139),
                Location = new Point(14, 36),
                Size = new Size(440, 20),
                AutoEllipsis = true
            };

            pnlSummary.Controls.Add(lblTaskTitle);
            pnlSummary.Controls.Add(lblTaskClient);

            // Checkbox for optional Activity logging
            _chkLogActivity = new CheckBox
            {
                Text = "Log this completion as an Activity on client timeline",
                Font = new Font("Segoe UI", 9.5f, FontStyle.Bold),
                ForeColor = Color.FromArgb(15, 23, 42),
                Checked = true,
                Location = new Point(24, 98),
                Size = new Size(472, 24),
                Cursor = Cursors.Hand
            };

            // Activity options container
            _pnlActivityOptions = new Panel
            {
                Location = new Point(24, 130),
                Size = new Size(472, 200),
                BackColor = Color.White
            };

            var lblActType = new Label
            {
                Text = "ACTIVITY CHANNEL",
                Font = new Font("Segoe UI", 8f, FontStyle.Bold),
                ForeColor = Color.FromArgb(71, 85, 105),
                Location = new Point(0, 0),
                AutoSize = true
            };

            _cmbActivityType = new ComboBox
            {
                Location = new Point(0, 20),
                Size = new Size(220, 25),
                DropDownStyle = ComboBoxStyle.DropDownList,
                Font = new Font("Segoe UI", 9.5f)
            };
            _cmbActivityType.Items.AddRange(new object[] { "Call", "Email", "Meeting", "Text" });
            if (_cmbActivityType.Items.Contains(_reminder.Type))
                _cmbActivityType.SelectedItem = _reminder.Type;
            else
                _cmbActivityType.SelectedIndex = 0;

            var lblNotes = new Label
            {
                Text = "OUTCOME / ACTIVITY NOTES",
                Font = new Font("Segoe UI", 8f, FontStyle.Bold),
                ForeColor = Color.FromArgb(71, 85, 105),
                Location = new Point(0, 58),
                AutoSize = true
            };

            _txtNotes = new TextBox
            {
                Location = new Point(0, 78),
                Size = new Size(472, 110),
                Multiline = true,
                ScrollBars = ScrollBars.Vertical,
                Font = new Font("Segoe UI", 9.5f),
                PlaceholderText = "e.g. Called client as scheduled, reviewed latest property listings, client requested follow-up next Tuesday..."
            };

            _pnlActivityOptions.Controls.Add(lblActType);
            _pnlActivityOptions.Controls.Add(_cmbActivityType);
            _pnlActivityOptions.Controls.Add(lblNotes);
            _pnlActivityOptions.Controls.Add(_txtNotes);

            _chkLogActivity.CheckedChanged += (_, _) =>
            {
                _pnlActivityOptions.Visible = _chkLogActivity.Checked;
            };

            var pnlContent = new Panel
            {
                Dock = DockStyle.Fill,
                AutoScroll = true,
                BackColor = Color.White
            };
            pnlContent.Controls.Add(pnlSummary);
            pnlContent.Controls.Add(_chkLogActivity);
            pnlContent.Controls.Add(_pnlActivityOptions);

            // Footer panel
            var pnlFooter = new Panel
            {
                Dock = DockStyle.Bottom,
                Height = 60,
                BackColor = Color.FromArgb(248, 250, 252)
            };

            _btnCancel = new Button
            {
                Text = "Cancel",
                Size = new Size(100, 36),
                Location = new Point(230, 12),
                Anchor = AnchorStyles.Top | AnchorStyles.Right,
                BackColor = Color.White,
                ForeColor = Color.FromArgb(71, 85, 105),
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand,
                Font = new Font("Segoe UI", 9.5f, FontStyle.Bold)
            };
            _btnCancel.FlatAppearance.BorderColor = Color.FromArgb(203, 213, 225);
            _btnCancel.Click += (_, _) => { DialogResult = DialogResult.Cancel; Close(); };

            _btnComplete = new Button
            {
                Text = "Complete Follow-Up",
                Size = new Size(160, 36),
                Location = new Point(340, 12),
                Anchor = AnchorStyles.Top | AnchorStyles.Right,
                BackColor = Color.FromArgb(22, 101, 52), // Success green
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand,
                Font = new Font("Segoe UI", 9.5f, FontStyle.Bold)
            };
            _btnComplete.FlatAppearance.BorderSize = 0;
            _btnComplete.Click += BtnCompleteClick;

            pnlFooter.Controls.Add(_btnCancel);
            pnlFooter.Controls.Add(_btnComplete);

            Controls.Add(pnlContent);
            Controls.Add(pnlFooter);
            Controls.Add(pnlHeader);
        }

        private void ApplyStyling()
        {
            UiRadiusHelper.StyleButton(_btnComplete, 8);
            UiRadiusHelper.StyleButton(_btnCancel, 8);
            UiRadiusHelper.AttachHoverFeedback(_btnCancel, Color.White, Color.FromArgb(241, 245, 249));
            UiRadiusHelper.AttachHoverFeedback(_btnComplete, Color.FromArgb(22, 101, 52), Color.FromArgb(20, 83, 45));
        }

        private void BtnCompleteClick(object? sender, EventArgs e)
        {
            DialogResult = DialogResult.OK;
            Close();
        }
    }
}
