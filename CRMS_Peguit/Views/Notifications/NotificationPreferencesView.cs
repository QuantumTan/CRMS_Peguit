using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using CRMS_Peguit.domain.entities;
using CRMS_Peguit.winforms.Auth;
using CRMS_Peguit.winforms.Controllers;
using CRMS_Peguit.winforms.Models.Services;
using CRMS_Peguit.winforms.Services;

namespace CRMS_Peguit.winforms.Views.Notifications
{
    public class NotificationPreferencesView : Form
    {
        private readonly NotificationController _controller;
        private readonly int _userId;
        private readonly Dictionary<NotificationType, CheckBox> _checkboxes = new();

        private Button _btnSave = null!;
        private Button _btnCancel = null!;
        private Panel _pnlContent = null!;

        public NotificationPreferencesView() : this(CurrentSession.UserId)
        {
        }

        public NotificationPreferencesView(int userId)
        {
            _userId = userId > 0 ? userId : CurrentSession.UserId;
            _controller = new NotificationController();
            InitializeComponent();
            LoadCurrentPreferences();
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _controller.Dispose();
            }
            base.Dispose(disposing);
        }

        private void InitializeComponent()
        {
            Text = "Notification Preferences — NEXA CRM";
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;
            ShowInTaskbar = false;
            StartPosition = FormStartPosition.CenterParent;
            ClientSize = new Size(580, 640);
            BackColor = Theme.Surface;

            // 1. Header Panel
            var pnlHeader = new Panel
            {
                Dock = DockStyle.Top,
                Height = 80,
                BackColor = Theme.HeaderBackground,
                Padding = new Padding(24, 16, 24, 16)
            };

            var lblHeaderTitle = new Label
            {
                Text = "🔔 Notification Preferences",
                Font = new Font("Segoe UI", 13.5f, FontStyle.Bold),
                ForeColor = Theme.TextPrimary,
                Location = new Point(24, 14),
                AutoSize = true
            };

            var lblHeaderSub = new Label
            {
                Text = "Customize which events trigger notifications in your personal feed.",
                Font = new Font("Segoe UI", 9f),
                ForeColor = Theme.TextSecondary,
                Location = new Point(26, 44),
                AutoSize = true
            };

            pnlHeader.Controls.Add(lblHeaderTitle);
            pnlHeader.Controls.Add(lblHeaderSub);

            pnlHeader.Paint += (s, e) =>
            {
                using var p = new Pen(Theme.HeaderBorder, 1);
                e.Graphics.DrawLine(p, 0, pnlHeader.Height - 1, pnlHeader.Width, pnlHeader.Height - 1);
            };

            // 2. Footer Panel
            var pnlFooter = new Panel
            {
                Dock = DockStyle.Bottom,
                Height = 65,
                BackColor = Theme.HeaderBackground,
                Padding = new Padding(24, 14, 24, 14)
            };

            _btnCancel = new Button
            {
                Text = "Cancel",
                Font = new Font("Segoe UI", 9f, FontStyle.Regular),
                ForeColor = Theme.TextSecondary,
                BackColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Size = new Size(100, 36),
                Location = new Point(ClientSize.Width - 224, 14),
                Cursor = Cursors.Hand
            };
            _btnCancel.FlatAppearance.BorderColor = Theme.Border;
            _btnCancel.Click += (_, _) => Close();
            UiRadiusHelper.StyleButton(_btnCancel, 6);

            _btnSave = new Button
            {
                Text = "Save Preferences",
                Font = new Font("Segoe UI", 9f, FontStyle.Bold),
                ForeColor = Color.White,
                BackColor = Theme.Primary,
                FlatStyle = FlatStyle.Flat,
                Size = new Size(130, 36),
                Location = new Point(ClientSize.Width - 114 - 30, 14),
                Cursor = Cursors.Hand
            };
            _btnSave.FlatAppearance.BorderSize = 0;
            _btnSave.Click += BtnSaveClick;
            UiRadiusHelper.StyleButton(_btnSave, 6);

            pnlFooter.Controls.Add(_btnCancel);
            pnlFooter.Controls.Add(_btnSave);

            pnlFooter.Paint += (s, e) =>
            {
                using var p = new Pen(Theme.HeaderBorder, 1);
                e.Graphics.DrawLine(p, 0, 0, pnlFooter.Width, 0);
            };

            // 3. Content Panel (Scrollable)
            _pnlContent = new Panel
            {
                Dock = DockStyle.Fill,
                AutoScroll = true,
                Padding = new Padding(24, 16, 24, 16),
                BackColor = Theme.Background
            };

            int yOffset = 16;

            // Section: Tasks & Follow-Ups
            yOffset = AddSectionHeader(_pnlContent, "⏱ Tasks & Follow-Ups", yOffset);
            yOffset = AddPreferenceItem(_pnlContent, NotificationType.FollowUpDueSoon, "Follow-Up Due Soon", "Notify when a scheduled follow-up is due within 1 hour.", yOffset);
            yOffset = AddPreferenceItem(_pnlContent, NotificationType.FollowUpOverdue, "Follow-Up Overdue", "Alert when a follow-up task has passed its due date.", yOffset);

            // Section: Deals & Sales Pipeline
            yOffset = AddSectionHeader(_pnlContent, "💼 Deals & Sales Pipeline", yOffset);
            yOffset = AddPreferenceItem(_pnlContent, NotificationType.DealStageChanged, "Deal Stage Advances", "Notify when your assigned deal reaches Reservation, Contract Signed, or Closed.", yOffset);
            yOffset = AddPreferenceItem(_pnlContent, NotificationType.DealClosed, "Team Deals Closed", "Oversight alert whenever any team deal reaches Closed status (Manager).", yOffset);

            // Section: Leads & Customers
            yOffset = AddSectionHeader(_pnlContent, "👥 Leads & Customers", yOffset);
            yOffset = AddPreferenceItem(_pnlContent, NotificationType.LeadAssigned, "Lead Assigned to You", "Notify when a Lead is newly assigned or reassigned to you.", yOffset);
            yOffset = AddPreferenceItem(_pnlContent, NotificationType.LeadStageChanged, "Lead Stage Updated", "Notify when the pipeline stage of your assigned lead changes.", yOffset);
            yOffset = AddPreferenceItem(_pnlContent, NotificationType.LeadUnassigned, "New Lead Pending Review", "Notify when a new unassigned lead requires manager action.", yOffset);
            yOffset = AddPreferenceItem(_pnlContent, NotificationType.CustomerAssigned, "Customer Assigned to You", "Notify when a Customer record is assigned to you.", yOffset);
            yOffset = AddPreferenceItem(_pnlContent, NotificationType.CustomerUnassigned, "New Customer Pending Assignment", "Notify when a newly created customer needs ownership assignment.", yOffset);

            // Section: Properties & Support
            yOffset = AddSectionHeader(_pnlContent, "🏢 Properties & Support Tickets", yOffset);
            yOffset = AddPreferenceItem(_pnlContent, NotificationType.PropertyAssigned, "Property Listing Assigned", "Notify when you are assigned as the listing agent for a property.", yOffset);
            yOffset = AddPreferenceItem(_pnlContent, NotificationType.PropertyStatusChanged, "Property Status Changes", "Notify when a property status updates (e.g. Sold, Under Contract).", yOffset);
            yOffset = AddPreferenceItem(_pnlContent, NotificationType.TicketAssigned, "Support Ticket Assigned", "Notify when a customer support ticket is assigned to you.", yOffset);
            yOffset = AddPreferenceItem(_pnlContent, NotificationType.TicketStatusChanged, "Ticket Status Updates", "Notify when support tickets you opened or handle change status.", yOffset);
            yOffset = AddPreferenceItem(_pnlContent, NotificationType.TicketCreated, "New Ticket Logged", "Notify management when a new ticket is submitted by an agent.", yOffset);

            // Section: System & Administrative
            yOffset = AddSectionHeader(_pnlContent, "🛡 System, Backup & Subscriptions", yOffset);
            yOffset = AddPreferenceItem(_pnlContent, NotificationType.SubscriptionExpiring, "Subscription Expiry Warnings", "Warn when tenant license is expiring within 7 days.", yOffset);
            yOffset = AddPreferenceItem(_pnlContent, NotificationType.SubscriptionExpired, "Subscription Expired", "Alert when tenant license has lapsed.", yOffset);
            yOffset = AddPreferenceItem(_pnlContent, NotificationType.BackupFailed, "Backup Failure Alerts", "Alert immediately if a database backup fails (never on success).", yOffset);
            yOffset = AddPreferenceItem(_pnlContent, NotificationType.AdminAccountCreated, "Admin Account Creation", "Notify super administrators when an administrative account is added.", yOffset);

            // Add dummy spacer at bottom
            var spacer = new Panel { Location = new Point(0, yOffset), Size = new Size(10, 24) };
            _pnlContent.Controls.Add(spacer);

            Controls.Add(_pnlContent);
            Controls.Add(pnlFooter);
            Controls.Add(pnlHeader);
        }

        private int AddSectionHeader(Panel parent, string title, int y)
        {
            var lbl = new Label
            {
                Text = title,
                Font = new Font("Segoe UI", 10.5f, FontStyle.Bold),
                ForeColor = Theme.TextPrimary,
                Location = new Point(24, y),
                Size = new Size(500, 26),
                TextAlign = ContentAlignment.BottomLeft
            };
            parent.Controls.Add(lbl);
            return y + 32;
        }

        private int AddPreferenceItem(Panel parent, NotificationType type, string title, string description, int y)
        {
            var card = new Panel
            {
                Location = new Point(24, y),
                Size = new Size(512, 54),
                BackColor = Color.White,
                Cursor = Cursors.Hand
            };
            UiRadiusHelper.ApplyRoundedCorners(card, 8);

            card.Paint += (s, e) =>
            {
                using var p = new Pen(Theme.BorderAccessible, 1);
                using var path = UiRadiusHelper.CreateRoundedPath(new Rectangle(0, 0, card.Width - 1, card.Height - 1), 8);
                e.Graphics.DrawPath(p, path);
            };

            var chk = new CheckBox
            {
                Text = title,
                Font = new Font("Segoe UI", 9.5f, FontStyle.Bold),
                ForeColor = Theme.TextPrimary,
                Location = new Point(14, 8),
                Size = new Size(480, 20),
                Checked = true,
                Cursor = Cursors.Hand
            };

            var lblDesc = new Label
            {
                Text = description,
                Font = new Font("Segoe UI", 8.5f),
                ForeColor = Theme.TextSecondary,
                Location = new Point(34, 30),
                Size = new Size(460, 18),
                Cursor = Cursors.Hand
            };

            card.Click += (_, _) => chk.Checked = !chk.Checked;
            lblDesc.Click += (_, _) => chk.Checked = !chk.Checked;

            card.Controls.Add(chk);
            card.Controls.Add(lblDesc);

            _checkboxes[type] = chk;
            parent.Controls.Add(card);

            return y + 62;
        }

        private void LoadCurrentPreferences()
        {
            var prefs = _controller.GetPreferences(_userId);
            foreach (var kvp in _checkboxes)
            {
                if (prefs.TryGetValue(kvp.Key, out bool isEnabled))
                {
                    kvp.Value.Checked = isEnabled;
                }
            }
        }

        private void BtnSaveClick(object? sender, EventArgs e)
        {
            var dict = new Dictionary<NotificationType, bool>();
            foreach (var kvp in _checkboxes)
            {
                dict[kvp.Key] = kvp.Value.Checked;
            }

            _controller.UpdatePreferences(_userId, dict);

            MessageBox.Show(
                "Your notification preferences have been saved.",
                "Preferences Saved",
                MessageBoxButtons.OK,
                MessageBoxIcon.Information);

            DialogResult = DialogResult.OK;
            Close();
        }
    }
}
