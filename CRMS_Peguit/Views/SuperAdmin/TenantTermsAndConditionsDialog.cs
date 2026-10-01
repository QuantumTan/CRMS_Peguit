using System;
using System.Drawing;
using System.Threading.Tasks;
using System.Windows.Forms;
using CRMS_Peguit.winforms.Controllers;
using CRMS_Peguit.winforms.Models.Services;
using CRMS_Peguit.winforms.Services;

namespace CRMS_Peguit.winforms.Views.SuperAdmin
{
    public class TenantTermsAndConditionsDialog : Form
    {
        private readonly SuperAdminSubscriptionController _controller = new();
        private readonly CheckBox? _chkAgree;
        private readonly Button _btnAccept;
        private readonly Button? _btnCancel;
        private readonly RichTextBox _txtTerms;
        private readonly string _tenantName;
        private readonly string _planName;
        private readonly bool _isReadOnly;

        public bool HasAgreed => _chkAgree?.Checked ?? _isReadOnly;

        public const string DefaultTermsText = @"NEXA CLOUD CRM — MASTER SERVICE AGREEMENT & TERMS

Welcome to NEXA CRM. This agreement outlines the service terms between NEXA CRM Systems and {TenantName} for the {PlanName} subscription tier.

1. SERVICE & ACCESS
NEXA grants your organization access to the NEXA CRM platform and the modules corresponding to your selected tier ({PlanName}).

2. ACCOUNT RESPONSIBILITY
Accounts are designated for authorized staff members (Administrators, Managers, and Agents). Login credentials must remain confidential and should not be shared across multiple individuals.

3. DATA OWNERSHIP & PRIVACY
Your organization owns 100% of all customer records, property data, and transaction details entered into your CRM catalog. NEXA will never sell, inspect, or share your business data.

4. BILLING & RENEWALS
Subscription fees are billed in advance per billing cycle (monthly/annually). Services and cloud synchronization remain active as long as your subscription is in good standing.

5. BACKUPS & DATA EXPORT
Your system maintains a local-first SQLite cache with bi-directional cloud synchronization. In the event of subscription termination, you have 30 days to export your databases and contact lists.";

        public TenantTermsAndConditionsDialog(string tenantName, string planName, bool isReadOnly = false)
        {
            _tenantName = tenantName;
            _planName = planName;
            _isReadOnly = isReadOnly;

            Text = isReadOnly ? $"Terms & Conditions — {tenantName}" : "Tenant Onboarding — Terms & Conditions Agreement";
            Size = new Size(740, 680);
            MinimumSize = new Size(620, 500);
            StartPosition = FormStartPosition.CenterParent;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = true;
            MinimizeBox = false;
            BackColor = Color.White;
            UiRadiusHelper.StyleModal(this, 8);

            // 1. Fixed Bottom Footer Panel (Dock = Bottom, Height = 64)
            var pnlFooter = new Panel
            {
                Dock = DockStyle.Bottom,
                Height = 64,
                BackColor = Color.FromArgb(248, 250, 252),
                Padding = new Padding(24, 14, 24, 14)
            };
            pnlFooter.Paint += (s, e) =>
            {
                using var p = new Pen(Theme.Border, 1f);
                e.Graphics.DrawLine(p, 0, 0, pnlFooter.Width, 0);
            };

            var pnlActionsRight = new FlowLayoutPanel
            {
                Dock = DockStyle.Right,
                Width = 320,
                FlowDirection = FlowDirection.RightToLeft,
                BackColor = Color.Transparent
            };

            _btnAccept = new Button
            {
                Text = _isReadOnly ? "Close" : "Accept & Proceed ➔",
                Size = new Size(_isReadOnly ? 110 : 170, 36),
                FlatStyle = FlatStyle.Flat,
                BackColor = Theme.Primary,
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 9.5f, FontStyle.Bold),
                Cursor = Cursors.Hand,
                Enabled = _isReadOnly,
                DialogResult = DialogResult.OK,
                Margin = new Padding(8, 0, 0, 0)
            };
            UiRadiusHelper.StyleButton(_btnAccept, 6);

            if (!_isReadOnly)
            {
                _btnCancel = new Button
                {
                    Text = "Cancel",
                    Size = new Size(95, 36),
                    FlatStyle = FlatStyle.Flat,
                    BackColor = Theme.Surface,
                    ForeColor = Theme.TextPrimary,
                    Font = new Font("Segoe UI", 9f),
                    Cursor = Cursors.Hand,
                    DialogResult = DialogResult.Cancel,
                    Margin = new Padding(0)
                };
                UiRadiusHelper.StyleButton(_btnCancel, 6);
                pnlActionsRight.Controls.Add(_btnAccept);
                pnlActionsRight.Controls.Add(_btnCancel);
                CancelButton = _btnCancel;
            }
            else
            {
                pnlActionsRight.Controls.Add(_btnAccept);
                CancelButton = _btnAccept;
            }

            pnlFooter.Controls.Add(pnlActionsRight);

            // 2. Agreement Checkbox Panel (Dock = Bottom, Height = 48) - Only in onboarding mode
            Panel? pnlAgree = null;
            if (!_isReadOnly)
            {
                pnlAgree = new Panel
                {
                    Dock = DockStyle.Bottom,
                    Height = 48,
                    BackColor = Color.FromArgb(241, 245, 249),
                    Padding = new Padding(24, 12, 24, 12)
                };

                _chkAgree = new CheckBox
                {
                    Dock = DockStyle.Fill,
                    Text = "  I confirm that the tenant has reviewed and agreed to the Master Subscription Agreement & Data Privacy Policy.",
                    Font = new Font("Segoe UI", 9f, FontStyle.Bold),
                    ForeColor = Theme.TextPrimary,
                    Cursor = Cursors.Hand,
                    Checked = false
                };
                _chkAgree.CheckedChanged += (_, _) =>
                {
                    _btnAccept.Enabled = _chkAgree.Checked;
                };
                pnlAgree.Controls.Add(_chkAgree);
            }

            // 3. Header Panel (Dock = Top, Height = 76)
            var pnlHeader = new Panel
            {
                Dock = DockStyle.Top,
                Height = 76,
                BackColor = Theme.Surface,
                Padding = new Padding(24, 16, 24, 12)
            };
            pnlHeader.Paint += (s, e) =>
            {
                using var p = new Pen(Theme.Border, 1f);
                e.Graphics.DrawLine(p, 0, pnlHeader.Height - 1, pnlHeader.Width, pnlHeader.Height - 1);
            };

            var lblTitle = new Label
            {
                Text = "Tenant Subscription & Service Agreement",
                Font = new Font("Segoe UI", 13f, FontStyle.Bold),
                ForeColor = Theme.TextPrimary,
                AutoSize = true,
                Location = new Point(24, 14)
            };

            var lblSub = new Label
            {
                Text = $"Organization: {_tenantName}  •  Plan: {_planName}  •  Multi-Tenant SaaS Agreement",
                Font = new Font("Segoe UI", 8.5f),
                ForeColor = Theme.TextSecondary,
                AutoSize = true,
                Location = new Point(24, 42)
            };

            pnlHeader.Controls.Add(lblTitle);
            pnlHeader.Controls.Add(lblSub);

            if (_isReadOnly)
            {
                var btnEditMaster = new Button
                {
                    Text = "⚙  Edit Master Terms",
                    Size = new Size(160, 32),
                    Anchor = AnchorStyles.Top | AnchorStyles.Right,
                    FlatStyle = FlatStyle.Flat,
                    BackColor = Theme.Surface,
                    ForeColor = Theme.Primary,
                    Font = new Font("Segoe UI", 8.5f, FontStyle.Bold),
                    Cursor = Cursors.Hand,
                    Location = new Point(pnlHeader.Width - 184, 20)
                };
                UiRadiusHelper.StyleButton(btnEditMaster, 6);
                pnlHeader.SizeChanged += (_, _) => btnEditMaster.Location = new Point(pnlHeader.Width - 184, 20);
                btnEditMaster.Click += (_, _) =>
                {
                    using var editDlg = new EditMasterTermsDialog();
                    if (editDlg.ShowDialog(this) == DialogResult.OK)
                    {
                        _ = LoadTermsContentAsync();
                    }
                };
                pnlHeader.Controls.Add(btnEditMaster);
            }

            // 4. Center Content (Dock = Fill)
            var pnlBody = new Panel
            {
                Dock = DockStyle.Fill,
                Padding = new Padding(24, 16, 24, 16),
                BackColor = Color.White
            };

            _txtTerms = new RichTextBox
            {
                Dock = DockStyle.Fill,
                ReadOnly = true,
                BackColor = Color.FromArgb(248, 250, 252),
                ForeColor = Color.FromArgb(51, 65, 85),
                Font = new Font("Segoe UI", 9f),
                BorderStyle = BorderStyle.FixedSingle,
                ScrollBars = RichTextBoxScrollBars.Vertical
            };
            pnlBody.Controls.Add(_txtTerms);

            // Correct docking order: Fill added last
            Controls.Add(pnlBody);
            Controls.Add(pnlHeader);
            if (pnlAgree != null) Controls.Add(pnlAgree);
            Controls.Add(pnlFooter);

            AcceptButton = _btnAccept;

            _ = LoadTermsContentAsync();
        }

        private async Task LoadTermsContentAsync()
        {
            _txtTerms.Text = "Loading agreement text...";
            string masterTerms = await _controller.GetMasterTermsAsync();
            string populated = masterTerms
                .Replace("{TenantName}", _tenantName, StringComparison.OrdinalIgnoreCase)
                .Replace("{PlanName}", _planName, StringComparison.OrdinalIgnoreCase);

            _txtTerms.Text = populated;
        }
    }
}
