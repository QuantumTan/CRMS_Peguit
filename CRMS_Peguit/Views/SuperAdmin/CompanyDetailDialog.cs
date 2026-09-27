using System;
using System.Drawing;
using System.Windows.Forms;
using CRMS_Peguit.winforms.Controllers;
using CRMS_Peguit.winforms.Controls;
using CRMS_Peguit.winforms.Models.Services;

// =============================================================================
// CompanyDetailDialog — Read-only view of Company + Subscription metadata.
// Opened when a Super Admin clicks a company name in AdministratorsView.
//
// DATA BOUNDARY: Only Company and Subscription fields are shown.
// NO CRM records (Customers, Leads, Deals, Properties, etc.) are ever
// loaded, displayed, or accessible from this dialog.
// =============================================================================

namespace CRMS_Peguit.winforms.Views.SuperAdmin
{
    public class CompanyDetailDialog : Form
    {
        public CompanyDetailDialog(CompanyDetailDto detail)
        {
            Text = $"Company Detail — {detail.CompanyName}";
            Size = new Size(500, 420);
            StartPosition = FormStartPosition.CenterParent;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;
            BackColor = Theme.Surface;

            var pnl = new Panel { Dock = DockStyle.Fill, Padding = new Padding(28) };
            Controls.Add(pnl);

            // Company header with AvatarLabel
            var avatarLabel = new AvatarLabel(detail.CompanyName, detail.CompanyCode)
            {
                Location = new Point(0, 0),
                Size = new Size(430, 44),
                AvatarSize = 36
            };
            pnl.Controls.Add(avatarLabel);

            int y = 58;

            // Company section
            AddSectionHeader(pnl, "Company Information", ref y);
            AddField(pnl, "Company Code", detail.CompanyCode, ref y);
            AddField(pnl, "Company Name", detail.CompanyName, ref y);
            AddField(pnl, "Status", detail.IsActive ? "Active" : "Inactive", ref y, isStatus: true);
            AddField(pnl, "Registered On", detail.CreatedAt.ToLocalTime().ToString("MMMM dd, yyyy"), ref y);

            y += 10;

            // Subscription section
            AddSectionHeader(pnl, "Current Subscription", ref y);
            AddField(pnl, "Plan Tier", detail.PlanName, ref y);
            AddField(pnl, "Status", detail.SubscriptionStatus, ref y, isStatus: true);
            AddField(pnl, "Monthly Billing", $"₱{detail.BillingAmount:N2}", ref y);
            AddField(pnl, "Subscription End",
                detail.SubscriptionEndDate.HasValue
                    ? detail.SubscriptionEndDate.Value.ToString("MMMM dd, yyyy")
                    : "Lifetime / No expiry", ref y);

            y += 10;

            // Disclaimer
            var lblNote = new Label
            {
                Text = "ℹ  No tenant CRM records (Customers, Leads, Deals, Properties) are accessible from this view.",
                Font = new Font("Segoe UI", 8f, FontStyle.Italic),
                ForeColor = Theme.TextSecondary,
                Location = new Point(0, y),
                Size = new Size(430, 32),
                AutoSize = false
            };
            pnl.Controls.Add(lblNote);
            y += 40;

            var btnClose = new Button
            {
                Text = "Close",
                DialogResult = DialogResult.Cancel,
                Location = new Point(340, y),
                Size = new Size(90, 36),
                Font = new Font("Segoe UI", 9.5f, FontStyle.Bold),
                BackColor = Theme.Primary,
                ForeColor = Color.White,
                Cursor = Cursors.Hand
            };
            UiRadiusHelper.StyleButton(btnClose, 6);
            pnl.Controls.Add(btnClose);

            AcceptButton = btnClose;
            CancelButton = btnClose;
        }

        private static void AddSectionHeader(Panel pnl, string title, ref int y)
        {
            var divider = new Panel
            {
                Location = new Point(0, y),
                Size = new Size(430, 1),
                BackColor = Theme.Border
            };
            pnl.Controls.Add(divider);
            y += 6;

            var lbl = new Label
            {
                Text = title.ToUpperInvariant(),
                Font = new Font("Segoe UI", 7.5f, FontStyle.Bold),
                ForeColor = Theme.TextSecondary,
                AutoSize = true,
                Location = new Point(0, y)
            };
            pnl.Controls.Add(lbl);
            y += 22;
        }

        private static void AddField(Panel pnl, string label, string value, ref int y, bool isStatus = false)
        {
            var lblKey = new Label
            {
                Text = label + ":",
                Font = new Font("Segoe UI", 9f, FontStyle.Bold),
                ForeColor = Theme.TextSecondary,
                AutoSize = true,
                Location = new Point(0, y)
            };
            pnl.Controls.Add(lblKey);

            if (isStatus)
            {
                var stText = new StatusText
                {
                    Location = new Point(150, y),
                    AutoSize = true
                };
                stText.SetStatus(value);
                pnl.Controls.Add(stText);
            }
            else
            {
                var lblVal = new Label
                {
                    Text = value,
                    Font = new Font("Segoe UI", 9.5f, FontStyle.Bold),
                    ForeColor = Theme.TextPrimary,
                    AutoSize = true,
                    Location = new Point(150, y)
                };
                pnl.Controls.Add(lblVal);
            }

            y += 24;
        }
    }
}
