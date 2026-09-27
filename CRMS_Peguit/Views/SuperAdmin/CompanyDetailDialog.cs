using System;
using System.Drawing;
using System.Windows.Forms;
using CRMS_Peguit.winforms.Controllers;
using CRMS_Peguit.winforms.Controls;
using CRMS_Peguit.winforms.Models.Services;
using CRMS_Peguit.winforms.Services;

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
            Size = new Size(520, 520);
            UiRadiusHelper.StyleModal(this, 8);

            var pnlContent = new Panel
            {
                Dock = DockStyle.Fill,
                Padding = new Padding(24),
                BackColor = Color.White,
                AutoScroll = true
            };

            // Company header with AvatarLabel
            var avatarLabel = new AvatarLabel(detail.CompanyName, detail.CompanyCode)
            {
                Location = new Point(24, 20),
                Size = new Size(456, 44),
                AvatarSize = 36
            };
            pnlContent.Controls.Add(avatarLabel);

            int y = 76;

            // Company section
            AddSectionHeader(pnlContent, "Company Information", ref y);
            AddField(pnlContent, "Company Code", detail.CompanyCode, ref y);
            AddField(pnlContent, "Company Name", detail.CompanyName, ref y);
            AddField(pnlContent, "Status", detail.IsActive ? "Active" : "Inactive", ref y, isStatus: true);
            AddField(pnlContent, "Registered On", detail.CreatedAt.ToLocalTime().ToString("MMMM dd, yyyy"), ref y);

            y += 10;

            // Subscription section
            AddSectionHeader(pnlContent, "Current Subscription", ref y);
            AddField(pnlContent, "Plan Tier", detail.PlanName, ref y);
            AddField(pnlContent, "Status", detail.SubscriptionStatus, ref y, isStatus: true);
            AddField(pnlContent, "Monthly Billing", $"₱{detail.BillingAmount:N2}", ref y);
            AddField(pnlContent, "Subscription End",
                detail.SubscriptionEndDate.HasValue
                    ? detail.SubscriptionEndDate.Value.ToString("MMMM dd, yyyy")
                    : "Lifetime / No expiry", ref y);

            y += 10;

            // Disclaimer
            var lblNote = new Label
            {
                Text = "ℹ  No tenant CRM records (Customers, Leads, Deals, Properties) are accessible from this view.",
                Font = new Font("Segoe UI", 8.5f, FontStyle.Italic),
                ForeColor = Theme.TextSecondary,
                Location = new Point(24, y),
                Size = new Size(456, 32),
                AutoSize = false
            };
            pnlContent.Controls.Add(lblNote);

            // Footer Panel
            var pnlFooter = new Panel
            {
                Dock = DockStyle.Bottom,
                Height = 60,
                BackColor = Color.FromArgb(248, 250, 252)
            };

            var btnClose = new Button
            {
                Text = "Close",
                DialogResult = DialogResult.Cancel,
                Location = new Point(370, 12),
                Size = new Size(110, 36),
                Anchor = AnchorStyles.Top | AnchorStyles.Right
            };
            UiRadiusHelper.StyleSecondaryButton(btnClose, 6);
            pnlFooter.Controls.Add(btnClose);

            Controls.Add(pnlContent);
            Controls.Add(pnlFooter);

            AcceptButton = btnClose;
            CancelButton = btnClose;
        }

        private static void AddSectionHeader(Panel pnl, string title, ref int y)
        {
            var divider = new Panel
            {
                Location = new Point(24, y),
                Size = new Size(456, 1),
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
                Location = new Point(24, y)
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
                Location = new Point(24, y)
            };
            pnl.Controls.Add(lblKey);

            if (isStatus)
            {
                var stText = new StatusText
                {
                    Location = new Point(170, y),
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
                    AutoEllipsis = true,
                    MaximumSize = new Size(310, 24),
                    Location = new Point(170, y)
                };
                pnl.Controls.Add(lblVal);
            }

            y += 24;
        }
    }
}
