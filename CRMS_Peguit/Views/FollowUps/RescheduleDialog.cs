using System;
using System.Drawing;
using System.Windows.Forms;
using CRMS_Peguit.domain.entities;
using CRMS_Peguit.winforms.Models.Services;

namespace CRMS_Peguit.winforms.Views.FollowUps
{
    public class RescheduleDialog : Form
    {
        public DateTime NewDueDate { get; private set; }

        private readonly TaskReminder _reminder;
        private DateTimePicker _dtpDate = null!;
        private DateTimePicker _dtpTime = null!;
        private Button _btnSave = null!;
        private Button _btnCancel = null!;

        public RescheduleDialog(TaskReminder reminder)
        {
            _reminder = reminder;
            InitializeComponent();
            ApplyStyling();
        }

        private void InitializeComponent()
        {
            Text = "Reschedule Follow-Up";
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;
            ShowInTaskbar = false;
            StartPosition = FormStartPosition.CenterParent;
            ClientSize = new Size(500, 310);
            BackColor = Color.White;

            var pnlHeader = new Panel
            {
                Dock = DockStyle.Top,
                Height = 66,
                BackColor = Color.FromArgb(239, 246, 255), // Light blue
                Padding = new Padding(20, 12, 20, 12)
            };

            var lblTitle = new Label
            {
                Text = "📅 Reschedule Follow-Up",
                Font = new Font("Segoe UI", 12.5f, FontStyle.Bold),
                ForeColor = Theme.PrimaryDark,
                Location = new Point(18, 12),
                AutoSize = true
            };

            var lblSub = new Label
            {
                Text = $"Current: {_reminder.DueDate.ToLocalTime():MMM dd, yyyy · hh:mm tt}",
                Font = new Font("Segoe UI", 9f),
                ForeColor = Color.FromArgb(100, 116, 139),
                Location = new Point(20, 38),
                AutoSize = true
            };

            pnlHeader.Controls.Add(lblTitle);
            pnlHeader.Controls.Add(lblSub);

            var lblDate = new Label
            {
                Text = "NEW DUE DATE",
                Font = new Font("Segoe UI", 8.5f, FontStyle.Bold),
                ForeColor = Color.FromArgb(71, 85, 105),
                Location = new Point(24, 16),
                AutoSize = true
            };

            _dtpDate = new DateTimePicker
            {
                Location = new Point(24, 38),
                Size = new Size(190, 25),
                Font = new Font("Segoe UI", 9.5f),
                Format = DateTimePickerFormat.Short,
                MinDate = DateTime.Today,
                Value = DateTime.Today.AddDays(1)
            };

            var lblTime = new Label
            {
                Text = "NEW DUE TIME",
                Font = new Font("Segoe UI", 8.5f, FontStyle.Bold),
                ForeColor = Color.FromArgb(71, 85, 105),
                Location = new Point(234, 16),
                AutoSize = true
            };

            _dtpTime = new DateTimePicker
            {
                Location = new Point(234, 38),
                Size = new Size(200, 25),
                Font = new Font("Segoe UI", 9.5f),
                Format = DateTimePickerFormat.Custom,
                CustomFormat = "hh:mm tt",
                ShowUpDown = true,
                Value = _reminder.DueDate.ToLocalTime()
            };

            var lblHint = new Label
            {
                Text = "💡 Rescheduling sets the follow-up status back to Pending and records the update.",
                Font = new Font("Segoe UI", 8.5f),
                ForeColor = Color.FromArgb(100, 116, 139),
                Location = new Point(24, 86),
                Size = new Size(410, 40)
            };

            var pnlContent = new Panel
            {
                Dock = DockStyle.Fill,
                AutoScroll = true,
                BackColor = Color.White
            };
            pnlContent.Controls.Add(lblDate);
            pnlContent.Controls.Add(_dtpDate);
            pnlContent.Controls.Add(lblTime);
            pnlContent.Controls.Add(_dtpTime);
            pnlContent.Controls.Add(lblHint);

            var pnlFooter = new Panel
            {
                Dock = DockStyle.Bottom,
                Height = 56,
                BackColor = Color.FromArgb(248, 250, 252)
            };

            _btnCancel = new Button
            {
                Text = "Cancel",
                Size = new Size(90, 34),
                Location = new Point(234, 11),
                BackColor = Color.White,
                ForeColor = Color.FromArgb(71, 85, 105),
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand,
                Font = new Font("Segoe UI", 9f, FontStyle.Bold),
                Anchor = AnchorStyles.Top | AnchorStyles.Right
            };
            _btnCancel.FlatAppearance.BorderColor = Color.FromArgb(203, 213, 225);
            _btnCancel.Click += (_, _) => { DialogResult = DialogResult.Cancel; Close(); };

            _btnSave = new Button
            {
                Text = "Confirm Reschedule",
                Size = new Size(150, 34),
                Location = new Point(330, 11),
                BackColor = Theme.Primary,
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand,
                Font = new Font("Segoe UI", 9f, FontStyle.Bold),
                Anchor = AnchorStyles.Top | AnchorStyles.Right
            };
            _btnSave.FlatAppearance.BorderSize = 0;
            _btnSave.Click += BtnSaveClick;

            pnlFooter.Controls.Add(_btnCancel);
            pnlFooter.Controls.Add(_btnSave);

            pnlFooter.Resize += (_, _) =>
            {
                _btnSave.Location = new Point(pnlFooter.Width - _btnSave.Width - 20, 11);
                _btnCancel.Location = new Point(_btnSave.Left - _btnCancel.Width - 10, 11);
            };

            Controls.Add(pnlContent);
            Controls.Add(pnlFooter);
            Controls.Add(pnlHeader);
        }

        private void ApplyStyling()
        {
            UiRadiusHelper.StyleButton(_btnSave, 8);
            UiRadiusHelper.StyleButton(_btnCancel, 8);
            UiRadiusHelper.AttachHoverFeedback(_btnCancel, Color.White, Color.FromArgb(241, 245, 249));
            UiRadiusHelper.AttachHoverFeedback(_btnSave, Theme.Primary, Theme.PrimaryDark);
        }

        private void BtnSaveClick(object? sender, EventArgs e)
        {
            DateTime selectedDate = _dtpDate.Value.Date;
            TimeSpan selectedTime = _dtpTime.Value.TimeOfDay;
            DateTime localCombined = selectedDate.Add(selectedTime);

            if (localCombined < DateTime.Now)
            {
                MessageBox.Show("Rescheduled follow-up date and time cannot be in the past. Please select a current or future date and time.", "Invalid Date", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                _dtpDate.Focus();
                return;
            }

            NewDueDate = localCombined.ToUniversalTime();
            DialogResult = DialogResult.OK;
            Close();
        }
    }
}
