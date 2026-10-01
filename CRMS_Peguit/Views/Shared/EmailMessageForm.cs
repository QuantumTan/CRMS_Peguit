using System;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using CRMS_Peguit.winforms.Auth;
using CRMS_Peguit.winforms.Models.Services;
using CRMS_Peguit.winforms.Services;

namespace CRMS_Peguit.winforms.Views.Shared
{
    public partial class EmailMessageForm : Form
    {
        private readonly string _recipientName;
        private readonly bool _isAgent;

        public string SentSubject { get; private set; } = string.Empty;

        private sealed class TemplateDropdownItem
        {
            public int Id { get; set; }
            public string Name { get; set; } = string.Empty;
            public string Subject { get; set; } = string.Empty;
            public string Body { get; set; } = string.Empty;

            public override string ToString() => Name;
        }

        public EmailMessageForm() : this("Recipient", string.Empty)
        {
        }

        public EmailMessageForm(string recipientName, string? recipientEmail, string? defaultSubject = null)
        {
            _recipientName = recipientName;
            _isAgent = RbacService.IsAgent;

            InitializeComponent();
            CRMS_Peguit.winforms.Services.ResponsiveLayout.BindInputPanel(pnlContent);
            CRMS_Peguit.winforms.Services.ResponsiveLayout.BindToolbar(pnlFooter, 12, btnCancel, btnSend);
            UiRadiusHelper.StyleButton(btnSend, 8);
            UiRadiusHelper.StyleButton(btnCancel, 8);
            UiRadiusHelper.AttachHoverFeedback(btnCancel, Color.White, Color.FromArgb(241, 245, 249));
            UiRadiusHelper.AttachHoverFeedback(btnSend, Theme.Primary, Theme.PrimaryDark);

            Text = $"Send Email to {_recipientName}";
            txtRecipient.Text = recipientEmail ?? string.Empty;

            ConfigureRoleAccess();
            LoadTemplates(defaultSubject);

            cboTemplate.SelectedIndexChanged += OnTemplateSelectedIndexChanged;
            btnSend.Click += BtnSendClick;
        }

        private void ConfigureRoleAccess()
        {
            if (_isAgent)
            {
                pnlNotice.Visible = true;
                lblNotice.Text = "🔒 Corporate Policy Enforced: Sales Staff must use approved organizational templates and cannot modify the subject or message.";
                pnlNotice.BackColor = Color.FromArgb(239, 246, 255);
                lblNotice.ForeColor = Color.FromArgb(30, 64, 175);

                txtSubject.ReadOnly = true;
                txtBody.ReadOnly = true;
                txtSubject.BackColor = Color.FromArgb(248, 250, 252);
                txtBody.BackColor = Color.FromArgb(248, 250, 252);

                txtSubject.KeyPress += (_, e) => e.Handled = true;
                txtBody.KeyPress += (_, e) => e.Handled = true;

                btnSend.Enabled = false;
            }
            else
            {
                pnlNotice.Visible = true;
                lblNotice.Text = "ℹ️ Management Access: You may select an approved template or freely customize the subject and message.";
                pnlNotice.BackColor = Color.FromArgb(241, 245, 249);
                lblNotice.ForeColor = Color.FromArgb(71, 85, 105);

                txtSubject.ReadOnly = false;
                txtBody.ReadOnly = false;
                txtSubject.BackColor = Color.White;
                txtBody.BackColor = Color.White;
                btnSend.Enabled = true;
            }
        }

        private void LoadTemplates(string? defaultSubject)
        {
            cboTemplate.Items.Clear();
            cboTemplate.Items.Add("-- Select Approved Email Template --");

            int tenantId = CurrentSession.TenantId > 0 ? CurrentSession.TenantId : 1;
            var templates = EmailTemplateService.Instance.GetActiveTemplates(tenantId);

            int matchIndex = -1;
            int idx = 1;

            foreach (var t in templates)
            {
                var item = new TemplateDropdownItem
                {
                    Id = t.TemplateId,
                    Name = !string.IsNullOrWhiteSpace(t.Category) ? $"[{t.Category}] {t.Name}" : t.Name,
                    Subject = t.Subject ?? string.Empty,
                    Body = t.Body ?? string.Empty
                };

                cboTemplate.Items.Add(item);

                if (!string.IsNullOrWhiteSpace(defaultSubject))
                {
                    if (string.Equals(t.Subject, defaultSubject, StringComparison.OrdinalIgnoreCase) ||
                        t.Name.Contains(defaultSubject, StringComparison.OrdinalIgnoreCase) ||
                        (!string.IsNullOrWhiteSpace(t.Subject) && defaultSubject.Contains(t.Subject, StringComparison.OrdinalIgnoreCase)))
                    {
                        matchIndex = idx;
                    }
                }

                idx++;
            }

            if (matchIndex > 0)
            {
                cboTemplate.SelectedIndex = matchIndex;
            }
            else if (_isAgent && cboTemplate.Items.Count > 1)
            {
                // Auto-select first approved template for agent so the form is immediately populated
                cboTemplate.SelectedIndex = 1;
            }
            else
            {
                cboTemplate.SelectedIndex = 0;
            }
        }

        private void OnTemplateSelectedIndexChanged(object? sender, EventArgs e)
        {
            if (cboTemplate.SelectedItem is TemplateDropdownItem item)
            {
                txtSubject.Text = ResolvePlaceholders(item.Subject);
                txtBody.Text = ResolvePlaceholders(item.Body);

                if (_isAgent)
                {
                    btnSend.Enabled = true;
                }
            }
            else
            {
                if (_isAgent)
                {
                    txtSubject.Text = string.Empty;
                    txtBody.Text = string.Empty;
                    btnSend.Enabled = false;
                }
            }
        }

        private string ResolvePlaceholders(string templateText)
        {
            if (string.IsNullOrEmpty(templateText)) return string.Empty;

            string clientName = string.IsNullOrWhiteSpace(_recipientName) ? "Valued Client" : _recipientName.Trim();
            string firstName = clientName.Split(' ', StringSplitOptions.RemoveEmptyEntries).FirstOrDefault() ?? clientName;
            string agentName = CurrentSession.CurrentUser?.FullName ?? "Sales Advisory Specialist";
            string companyName = CurrentSession.TenantName ?? "Apex Realty";
            string agentEmail = CurrentSession.CurrentUser?.Email ?? string.Empty;
            string agentPhone = string.Empty;
            string today = DateTime.Now.ToString("MMMM dd, yyyy");

            return templateText
                .Replace("{CustomerName}", clientName, StringComparison.OrdinalIgnoreCase)
                .Replace("{{customer_name}}", clientName, StringComparison.OrdinalIgnoreCase)
                .Replace("{FirstName}", firstName, StringComparison.OrdinalIgnoreCase)
                .Replace("{{first_name}}", firstName, StringComparison.OrdinalIgnoreCase)
                .Replace("{AgentName}", agentName, StringComparison.OrdinalIgnoreCase)
                .Replace("{{agent_name}}", agentName, StringComparison.OrdinalIgnoreCase)
                .Replace("{CompanyName}", companyName, StringComparison.OrdinalIgnoreCase)
                .Replace("{{company_name}}", companyName, StringComparison.OrdinalIgnoreCase)
                .Replace("{AgentEmail}", agentEmail, StringComparison.OrdinalIgnoreCase)
                .Replace("{{agent_email}}", agentEmail, StringComparison.OrdinalIgnoreCase)
                .Replace("{AgentPhone}", agentPhone, StringComparison.OrdinalIgnoreCase)
                .Replace("{{agent_phone}}", agentPhone, StringComparison.OrdinalIgnoreCase)
                .Replace("{Date}", today, StringComparison.OrdinalIgnoreCase)
                .Replace("{{date}}", today, StringComparison.OrdinalIgnoreCase)
                .Replace("{PropertyAddress}", "the property", StringComparison.OrdinalIgnoreCase)
                .Replace("{{property_address}}", "the property", StringComparison.OrdinalIgnoreCase)
                .Replace("{YearsOwned}", "1", StringComparison.OrdinalIgnoreCase)
                .Replace("{OriginalPrice}", "₱0.00", StringComparison.OrdinalIgnoreCase)
                .Replace("{EstimatedValue}", "₱0.00", StringComparison.OrdinalIgnoreCase)
                .Replace("{EquityGain}", "₱0.00", StringComparison.OrdinalIgnoreCase)
                .Replace("{EquityPercent}", "0%", StringComparison.OrdinalIgnoreCase)
                .Replace("{AppreciationRate}", "5%", StringComparison.OrdinalIgnoreCase)
                .Replace("{PropertyType}", "Property", StringComparison.OrdinalIgnoreCase);
        }

        private async void BtnSendClick(object? sender, EventArgs e)
        {
            if (string.IsNullOrWhiteSpace(txtRecipient.Text))
            {
                MessageBox.Show("Please enter a valid recipient email address.", "Validation Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                txtRecipient.Focus();
                return;
            }

            if (_isAgent)
            {
                if (cboTemplate.SelectedItem is not TemplateDropdownItem)
                {
                    MessageBox.Show(
                        "You must select an approved email template. Sales Staff are not authorized to compose or modify email messages.",
                        "Approved Template Required",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Warning);
                    cboTemplate.Focus();
                    return;
                }
            }
            else
            {
                if (string.IsNullOrWhiteSpace(txtSubject.Text))
                {
                    MessageBox.Show("Please enter an email subject.", "Validation Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    txtSubject.Focus();
                    return;
                }

                if (string.IsNullOrWhiteSpace(txtBody.Text))
                {
                    MessageBox.Show("Please enter an email message.", "Validation Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    txtBody.Focus();
                    return;
                }
            }

            btnSend.Enabled = false;
            btnSend.Text = "Sending...";

            try
            {
                var result = await ContactEmailService.SendAsync(
                    txtRecipient.Text.Trim(),
                    txtSubject.Text.Trim(),
                    txtBody.Text.Trim());

                MessageBox.Show(
                    result.Message,
                    result.Success ? "Email Sent" : "Email Error",
                    MessageBoxButtons.OK,
                    result.Success ? MessageBoxIcon.Information : MessageBoxIcon.Warning);

                if (result.Success)
                {
                    SentSubject = txtSubject.Text.Trim();
                    DialogResult = DialogResult.OK;
                    Close();
                }
            }
            finally
            {
                btnSend.Enabled = true;
                btnSend.Text = "Send";
            }
        }
    }
}
