using System;
using System.Drawing;
using System.Windows.Forms;
using CRMS_Peguit.domain.Common;
using CRMS_Peguit.domain.entities;
using CRMS_Peguit.winforms.Models.Services;
using CRMS_Peguit.winforms.Services;

namespace CRMS_Peguit.winforms.Views.SuperAdmin
{
    public class CreateTenantDialog : Form
    {
        public CreateTenantRequest Request { get; private set; } = new();

        private readonly TextBox _txtCompanyName;
        private readonly TextBox _txtCompanyCode;
        private readonly ComboBox _cmbTier;
        private readonly TextBox _txtAdminFirst;
        private readonly TextBox _txtAdminLast;
        private readonly TextBox _txtAdminEmail;
        private readonly TextBox _txtAdminPassword;
        private readonly Label _lblError;

        public CreateTenantDialog()
        {
            Text = "Create New Tenant Organization";
            Size = new Size(540, 620);
            UiRadiusHelper.StyleModal(this, 8);

            var pnlContent = new Panel
            {
                Dock = DockStyle.Fill,
                Padding = new Padding(24),
                BackColor = Color.White,
                AutoScroll = true
            };

            int y = 20;

            var lblHeader = new Label
            {
                Text = "New Tenant Organization",
                Font = new Font("Segoe UI", 13.5f, FontStyle.Bold),
                ForeColor = Theme.TextPrimary,
                AutoSize = true,
                Location = new Point(24, y)
            };
            pnlContent.Controls.Add(lblHeader);
            y += 30;

            var lblSubtitle = new Label
            {
                Text = "Provisions a company record, dedicated database routing, initial subscription, and primary administrator.",
                Font = new Font("Segoe UI", 9f),
                ForeColor = Theme.TextSecondary,
                Size = new Size(470, 32),
                Location = new Point(24, y)
            };
            pnlContent.Controls.Add(lblSubtitle);
            y += 38;

            // Company Name
            pnlContent.Controls.Add(MakeLabel("Company Name:", y));
            y += 20;
            _txtCompanyName = MakeTextBox(y, 470);
            _txtCompanyName.PlaceholderText = "e.g. Acme Properties Corp.";
            pnlContent.Controls.Add(_txtCompanyName);
            y += 38;

            // Company Code & Tier
            pnlContent.Controls.Add(MakeLabel("Company Code (Slug):", y));
            var lblTier = MakeLabel("Subscription Tier:", y);
            lblTier.Location = new Point(264, y);
            pnlContent.Controls.Add(lblTier);
            y += 20;

            _txtCompanyCode = MakeTextBox(y, 226);
            _txtCompanyCode.PlaceholderText = "e.g. ACME";
            _txtCompanyCode.CharacterCasing = CharacterCasing.Upper;
            pnlContent.Controls.Add(_txtCompanyCode);

            _cmbTier = new ComboBox
            {
                DropDownStyle = ComboBoxStyle.DropDownList,
                Location = new Point(264, y),
                Width = 230
            };
            UiRadiusHelper.StyleStandardComboBox(_cmbTier);
            _cmbTier.Items.Add("Tenant A (Starter)");
            _cmbTier.Items.Add("Tenant B (Professional)");
            _cmbTier.Items.Add("Tenant C (Enterprise)");
            _cmbTier.SelectedIndex = 0;
            pnlContent.Controls.Add(_cmbTier);
            y += 44;

            // Separator: Primary Admin Account
            var divider = new Panel
            {
                Location = new Point(24, y),
                Size = new Size(470, 1),
                BackColor = Theme.Border
            };
            pnlContent.Controls.Add(divider);
            y += 12;

            var lblAdminSection = new Label
            {
                Text = "PRIMARY ADMINISTRATOR ACCOUNT",
                Font = new Font("Segoe UI", 8.5f, FontStyle.Bold),
                ForeColor = Theme.Primary,
                AutoSize = true,
                Location = new Point(24, y)
            };
            pnlContent.Controls.Add(lblAdminSection);
            y += 26;

            // Names row
            pnlContent.Controls.Add(MakeLabel("Admin First Name:", y));
            var lblLast = MakeLabel("Admin Last Name:", y);
            lblLast.Location = new Point(264, y);
            pnlContent.Controls.Add(lblLast);
            y += 20;

            _txtAdminFirst = MakeTextBox(y, 226);
            pnlContent.Controls.Add(_txtAdminFirst);

            _txtAdminLast = MakeTextBox(y, 230);
            _txtAdminLast.Location = new Point(264, y);
            pnlContent.Controls.Add(_txtAdminLast);
            y += 38;

            // Email
            pnlContent.Controls.Add(MakeLabel("Admin Email:", y));
            y += 20;
            _txtAdminEmail = MakeTextBox(y, 470);
            _txtAdminEmail.PlaceholderText = "admin@example.com";
            pnlContent.Controls.Add(_txtAdminEmail);
            y += 38;

            // Password
            pnlContent.Controls.Add(MakeLabel("Admin Temporary Password:", y));
            y += 20;
            _txtAdminPassword = MakeTextBox(y, 470);
            _txtAdminPassword.UseSystemPasswordChar = true;
            pnlContent.Controls.Add(_txtAdminPassword);
            y += 38;

            // Error label
            _lblError = new Label
            {
                Text = "",
                Font = new Font("Segoe UI", 9f),
                ForeColor = Theme.StatusAlert,
                AutoSize = true,
                Location = new Point(24, y)
            };
            pnlContent.Controls.Add(_lblError);

            // Footer panel
            var pnlFooter = new Panel
            {
                Dock = DockStyle.Bottom,
                Height = 60,
                BackColor = Color.FromArgb(248, 250, 252)
            };

            // Buttons
            var btnCancel = new Button
            {
                Text = "Cancel",
                DialogResult = DialogResult.Cancel,
                Location = new Point(270, 12),
                Size = new Size(100, 36),
                Anchor = AnchorStyles.Top | AnchorStyles.Right
            };
            UiRadiusHelper.StyleSecondaryButton(btnCancel, 6);
            pnlFooter.Controls.Add(btnCancel);

            var btnCreate = new Button
            {
                Text = "Create Tenant",
                Location = new Point(380, 12),
                Size = new Size(120, 36),
                Anchor = AnchorStyles.Top | AnchorStyles.Right
            };
            UiRadiusHelper.StylePrimaryButton(btnCreate, 6);
            btnCreate.Click += BtnCreate_Click;
            pnlFooter.Controls.Add(btnCreate);

            Controls.Add(pnlContent);
            Controls.Add(pnlFooter);

            AcceptButton = btnCreate;
            CancelButton = btnCancel;
        }

        private void BtnCreate_Click(object? sender, EventArgs e)
        {
            _lblError.Text = "";

            if (string.IsNullOrWhiteSpace(_txtCompanyName.Text))
            { _lblError.Text = "Company name is required."; return; }
            if (string.IsNullOrWhiteSpace(_txtCompanyCode.Text))
            { _lblError.Text = "Company code is required."; return; }
            if (string.IsNullOrWhiteSpace(_txtAdminFirst.Text))
            { _lblError.Text = "Admin first name is required."; return; }
            if (string.IsNullOrWhiteSpace(_txtAdminLast.Text))
            { _lblError.Text = "Admin last name is required."; return; }
            if (string.IsNullOrWhiteSpace(_txtAdminEmail.Text) || !_txtAdminEmail.Text.Contains('@'))
            { _lblError.Text = "Valid admin email is required."; return; }
            if (_txtAdminPassword.Text.Length < 6)
            { _lblError.Text = "Password must be at least 6 characters."; return; }

            string tier = _cmbTier.SelectedIndex switch
            {
                2 => Subscription.TierTenantC,
                1 => Subscription.TierTenantB,
                _ => Subscription.TierTenantA
            };

            Request = new CreateTenantRequest
            {
                CompanyName = _txtCompanyName.Text.Trim(),
                CompanyCode = _txtCompanyCode.Text.Trim().ToUpperInvariant(),
                TierLevel = tier,
                AdminFirstName = _txtAdminFirst.Text.Trim(),
                AdminLastName = _txtAdminLast.Text.Trim(),
                AdminEmail = _txtAdminEmail.Text.Trim(),
                AdminPassword = _txtAdminPassword.Text
            };

            DialogResult = DialogResult.OK;
            Close();
        }

        private static Label MakeLabel(string text, int y) => new Label
        {
            Text = text,
            Font = new Font("Segoe UI", 9f, FontStyle.Bold),
            ForeColor = Theme.TextPrimary,
            AutoSize = true,
            Location = new Point(24, y)
        };

        private static TextBox MakeTextBox(int y, int width)
        {
            var txt = new TextBox
            {
                Location = new Point(24, y),
                Width = width
            };
            UiRadiusHelper.StyleStandardInput(txt);
            return txt;
        }
    }
}
