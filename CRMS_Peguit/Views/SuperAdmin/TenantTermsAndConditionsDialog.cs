using System;
using System.Drawing;
using System.Windows.Forms;
using CRMS_Peguit.winforms.Models.Services;

namespace CRMS_Peguit.winforms.Views.SuperAdmin
{
    public class TenantTermsAndConditionsDialog : Form
    {
        private readonly CheckBox _chkAgree;
        private readonly Button _btnAccept;
        private readonly Button _btnClose;

        public bool HasAgreed => _chkAgree.Checked;

        public TenantTermsAndConditionsDialog(string tenantName, string planName, bool isReadOnly = false)
        {
            Text = $"Terms & Conditions — {tenantName}";
            Size = new Size(620, 560);
            StartPosition = FormStartPosition.CenterParent;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;
            BackColor = Theme.Surface;

            var pnlContainer = new Panel
            {
                Dock = DockStyle.Fill,
                Padding = new Padding(24)
            };
            Controls.Add(pnlContainer);

            // Header
            var lblTitle = new Label
            {
                Text = "Tenant Subscription & Service Agreement",
                Font = new Font("Segoe UI", 13f, FontStyle.Bold),
                ForeColor = Theme.TextPrimary,
                AutoSize = true,
                Location = new Point(24, 18)
            };
            pnlContainer.Controls.Add(lblTitle);

            var lblSub = new Label
            {
                Text = $"Organization: {tenantName}  •  Plan: {planName}  •  Version 2026.1",
                Font = new Font("Segoe UI", 9.5f),
                ForeColor = Theme.TextSecondary,
                AutoSize = true,
                Location = new Point(24, 46)
            };
            pnlContainer.Controls.Add(lblSub);

            // Terms Content (RichTextBox / Read-only Box)
            var txtTerms = new RichTextBox
            {
                Location = new Point(24, 80),
                Size = new Size(556, 330),
                ReadOnly = true,
                BackColor = Color.FromArgb(249, 250, 251),
                ForeColor = Color.FromArgb(51, 65, 85),
                Font = new Font("Segoe UI", 9f),
                BorderStyle = BorderStyle.FixedSingle,
                Text = GetTermsText(tenantName, planName)
            };
            pnlContainer.Controls.Add(txtTerms);

            _btnAccept = new Button
            {
                Text = isReadOnly ? "Close" : "Accept & Proceed",
                Size = new Size(140, 36),
                BackColor = Theme.Primary,
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 9f, FontStyle.Bold),
                Cursor = Cursors.Hand,
                Enabled = isReadOnly,
                DialogResult = DialogResult.OK
            };
            UiRadiusHelper.StyleButton(_btnAccept, 6);

            _chkAgree = new CheckBox
            {
                Text = "I confirm that the tenant has accepted the Master Subscription Agreement & Data Privacy Policy",
                Font = new Font("Segoe UI", 9f, FontStyle.Regular),
                ForeColor = Theme.TextPrimary,
                AutoSize = true,
                Location = new Point(24, 424),
                Checked = isReadOnly,
                Enabled = !isReadOnly
            };
            _chkAgree.CheckedChanged += (_, _) => _btnAccept.Enabled = _chkAgree.Checked || isReadOnly;
            pnlContainer.Controls.Add(_chkAgree);

            var pnlActions = new FlowLayoutPanel
            {
                FlowDirection = FlowDirection.RightToLeft,
                Location = new Point(24, 460),
                Size = new Size(556, 44),
                BackColor = Color.Transparent
            };

            _btnClose = new Button
            {
                Text = "Cancel",
                Size = new Size(100, 36),
                BackColor = Theme.Surface,
                ForeColor = Theme.TextSecondary,
                Font = new Font("Segoe UI", 9f, FontStyle.Regular),
                Cursor = Cursors.Hand,
                DialogResult = DialogResult.Cancel,
                Visible = !isReadOnly
            };
            UiRadiusHelper.StyleButton(_btnClose, 6);

            pnlActions.Controls.Add(_btnAccept);
            pnlActions.Controls.Add(_btnClose);
            pnlContainer.Controls.Add(pnlActions);
        }

        private static string GetTermsText(string tenantName, string planName)
        {
            return $@"NEXA CLOUD CRM — MASTER SERVICE AGREEMENT & TERMS OF SERVICE

This Master Service Agreement (""Agreement"") is entered into by and between NEXA CRM Systems and {tenantName} (""Tenant"") for provision of the {planName} subscription tier.

1. SCOPE OF SERVICE & LICENSING
NEXA grants Tenant a non-exclusive, non-transferable, multi-tenant license to access the CRM platform, modules, and APIs corresponding to the selected subscription tier ({planName}). All rights not expressly granted are reserved.

2. SEAT ALLOCATION & USAGE BOUNDARIES
Tenant agrees not to exceed the maximum allocated seats specified in their billing plan without an authorized plan upgrade. Credentials must not be shared among multiple concurrent users.

3. DATA PRIVACY & COMPLIANCE (RA 10173 / GDPR)
- Tenant retains full legal ownership of all customer, transaction, and operational data imported into their tenant workspace.
- NEXA shall implement strict logical separation and encryption at rest and in transit for Tenant database schemas.
- In accordance with the Philippine Data Privacy Act of 2012 (RA 10173) and applicable international privacy frameworks, NEXA shall not sell, disclose, or process Tenant operational records without explicit authorization.

4. BILLING, PAYMENTS & PRORATION
- Subscription fees are billed monthly or annually in advance.
- Payments must be settled by the designated billing date. Failure to remit payment within fifteen (15) days of invoice generation may result in temporary account suspension.
- Invoices are generated electronically with corresponding downloadable tax receipts.

5. SERVICE LEVEL AGREEMENT (SLA) & BACKUPS
- Target platform availability is 99.9% uptime, excluding scheduled off-peak maintenance windows.
- Automated system snapshots and backups are taken daily at 3:00 AM UTC+8 with a 30-day retention cycle.

6. TERMINATION & DATA EXPORT
Upon termination of subscription services, Tenant shall be granted thirty (30) calendar days to export all CRM databases, property inventories, and contact lists via the Data & Backup console before archival.";
        }
    }
}
