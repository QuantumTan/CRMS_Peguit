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
        private readonly TextBox _txtDisplayName;
        private readonly TextBox _txtCompanyCode;
        private readonly ComboBox _cmbTier;
        private readonly TextBox _txtAdminFirst;
        private readonly TextBox _txtAdminLast;
        private readonly TextBox _txtAdminEmail;
        private readonly TextBox _txtAdminPassword;
        private readonly Label _lblError;
        private readonly Button _btnCreate;
        private readonly Button _btnCancel;

        public CreateTenantDialog()
        {
            Text = "Onboard New Tenant Organization";
            Size = new Size(580, 720);
            MinimumSize = new Size(540, 620);
            StartPosition = FormStartPosition.CenterParent;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
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

            var pnlButtons = new FlowLayoutPanel
            {
                Dock = DockStyle.Right,
                Width = 320,
                FlowDirection = FlowDirection.RightToLeft,
                BackColor = Color.Transparent
            };

            _btnCreate = new Button
            {
                Text = "Complete Onboarding",
                Size = new Size(180, 36),
                FlatStyle = FlatStyle.Flat,
                BackColor = Theme.Primary,
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 9.5f, FontStyle.Bold),
                Cursor = Cursors.Hand,
                Margin = new Padding(8, 0, 0, 0)
            };
            UiRadiusHelper.StyleButton(_btnCreate, 6);
            _btnCreate.Click += BtnCreate_Click;

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

            pnlButtons.Controls.Add(_btnCreate);
            pnlButtons.Controls.Add(_btnCancel);
            pnlFooter.Controls.Add(pnlButtons);

            // 2. Central Scrollable Content Panel (Dock = Fill)
            var pnlContent = new Panel
            {
                Dock = DockStyle.Fill,
                Padding = new Padding(28, 20, 28, 20),
                BackColor = Color.White,
                AutoScroll = true
            };

            int y = 16;

            var lblHeader = new Label
            {
                Text = "Tenant Organization Provisioning",
                Font = new Font("Segoe UI", 13.5f, FontStyle.Bold),
                ForeColor = Theme.TextPrimary,
                AutoSize = true,
                Location = new Point(24, y)
            };
            pnlContent.Controls.Add(lblHeader);
            y += 28;

            var pnlTermsBadge = new Panel
            {
                Location = new Point(24, y),
                Size = new Size(490, 34),
                BackColor = Color.FromArgb(240, 253, 244)
            };
            UiRadiusHelper.StyleCard(pnlTermsBadge, 6);

            var lblTermsBadge = new Label
            {
                Text = "✓ Master Subscription Agreement & Terms Accepted",
                Font = new Font("Segoe UI", 8.5f, FontStyle.Bold),
                ForeColor = Color.FromArgb(22, 101, 52),
                AutoSize = true,
                Location = new Point(12, 8)
            };
            pnlTermsBadge.Controls.Add(lblTermsBadge);
            pnlContent.Controls.Add(pnlTermsBadge);
            y += 42;

            // Company Name
            pnlContent.Controls.Add(MakeLabel("Company Name (Legal Registered Name) *", y));
            y += 20;
            _txtCompanyName = MakeTextBox(y, 490);
            _txtCompanyName.PlaceholderText = "e.g. Apex Realty & Development Corp.";
            pnlContent.Controls.Add(_txtCompanyName);
            y += 38;

            // Brand / Display Name
            pnlContent.Controls.Add(MakeLabel("Brand / Display Name (White-Label)", y));
            y += 20;
            _txtDisplayName = MakeTextBox(y, 490);
            _txtDisplayName.PlaceholderText = "e.g. Apex Luxury Real Estate (defaults to company name if blank)";
            pnlContent.Controls.Add(_txtDisplayName);
            y += 38;

            // Company Code & Tier
            pnlContent.Controls.Add(MakeLabel("Company Code (Slug / Identifier) *", y));
            var lblTier = MakeLabel("Subscription Tier *", y);
            lblTier.Location = new Point(274, y);
            pnlContent.Controls.Add(lblTier);
            y += 20;

            _txtCompanyCode = MakeTextBox(y, 236);
            _txtCompanyCode.PlaceholderText = "e.g. APEX";
            _txtCompanyCode.CharacterCasing = CharacterCasing.Upper;
            pnlContent.Controls.Add(_txtCompanyCode);

            _cmbTier = new ComboBox
            {
                DropDownStyle = ComboBoxStyle.DropDownList,
                Location = new Point(274, y),
                Width = 240,
                Font = new Font("Segoe UI", 9.5f)
            };
            UiRadiusHelper.StyleStandardComboBox(_cmbTier);
            _cmbTier.Items.Add("Tenant A (Starter — ₱2,500/mo)");
            _cmbTier.Items.Add("Tenant B (Professional — ₱5,500/mo)");
            _cmbTier.Items.Add("Tenant C (Enterprise — ₱9,500/mo)");
            _cmbTier.SelectedIndex = 0;
            pnlContent.Controls.Add(_cmbTier);
            y += 48;

            // Separator: Primary Administrator Account
            var divider = new Panel
            {
                Location = new Point(24, y),
                Size = new Size(490, 1),
                BackColor = Theme.Border
            };
            pnlContent.Controls.Add(divider);
            y += 14;

            var lblAdminSection = new Label
            {
                Text = "PRIMARY ADMINISTRATOR CREDENTIALS",
                Font = new Font("Segoe UI", 8.5f, FontStyle.Bold),
                ForeColor = Theme.Primary,
                AutoSize = true,
                Location = new Point(24, y)
            };
            pnlContent.Controls.Add(lblAdminSection);
            y += 26;

            // First & Last Name
            pnlContent.Controls.Add(MakeLabel("Admin First Name *", y));
            var lblLast = MakeLabel("Admin Last Name *", y);
            lblLast.Location = new Point(274, y);
            pnlContent.Controls.Add(lblLast);
            y += 20;

            _txtAdminFirst = MakeTextBox(y, 236);
            pnlContent.Controls.Add(_txtAdminFirst);

            _txtAdminLast = MakeTextBox(y, 240);
            _txtAdminLast.Location = new Point(274, y);
            pnlContent.Controls.Add(_txtAdminLast);
            y += 38;

            // Email
            pnlContent.Controls.Add(MakeLabel("Admin Email Address *", y));
            y += 20;
            _txtAdminEmail = MakeTextBox(y, 490);
            _txtAdminEmail.PlaceholderText = "admin@apexrealty.com";
            pnlContent.Controls.Add(_txtAdminEmail);
            y += 38;

            // Password
            pnlContent.Controls.Add(MakeLabel("Initial Temporary Password *", y));
            y += 20;
            _txtAdminPassword = MakeTextBox(y, 490);
            _txtAdminPassword.UseSystemPasswordChar = true;
            pnlContent.Controls.Add(_txtAdminPassword);
            y += 40;

            // Error label
            _lblError = new Label
            {
                Text = "",
                Font = new Font("Segoe UI", 9f, FontStyle.Bold),
                ForeColor = Theme.StatusAlert,
                AutoSize = true,
                Location = new Point(24, y)
            };
            pnlContent.Controls.Add(_lblError);
            y += 30;

            // Dummy spacing panel to ensure bottom margin inside scroll
            var pnlBottomPad = new Panel
            {
                Location = new Point(24, y),
                Size = new Size(490, 20),
                BackColor = Color.Transparent
            };
            pnlContent.Controls.Add(pnlBottomPad);

            // WinForms docking order: Add Fill AFTER Bottom so Footer is never obscured!
            Controls.Add(pnlContent);
            Controls.Add(pnlFooter);
            ResponsiveLayout.BindInputPanel(pnlContent);

            AcceptButton = _btnCreate;
            CancelButton = _btnCancel;
        }

        private void BtnCreate_Click(object? sender, EventArgs e)
        {
            _lblError.Text = "";

            if (string.IsNullOrWhiteSpace(_txtCompanyName.Text))
            {
                _lblError.Text = "Company name is required.";
                _txtCompanyName.Focus();
                return;
            }
            if (string.IsNullOrWhiteSpace(_txtCompanyCode.Text))
            {
                _lblError.Text = "Company code is required.";
                _txtCompanyCode.Focus();
                return;
            }
            if (_txtCompanyCode.Text.Trim().Length < 2)
            {
                _lblError.Text = "Company code must be at least 2 characters.";
                _txtCompanyCode.Focus();
                return;
            }
            if (string.IsNullOrWhiteSpace(_txtAdminFirst.Text))
            {
                _lblError.Text = "Admin first name is required.";
                _txtAdminFirst.Focus();
                return;
            }
            if (string.IsNullOrWhiteSpace(_txtAdminLast.Text))
            {
                _lblError.Text = "Admin last name is required.";
                _txtAdminLast.Focus();
                return;
            }
            if (string.IsNullOrWhiteSpace(_txtAdminEmail.Text) || !_txtAdminEmail.Text.Contains('@'))
            {
                _lblError.Text = "A valid administrator email is required.";
                _txtAdminEmail.Focus();
                return;
            }
            if (_txtAdminPassword.Text.Length < 6)
            {
                _lblError.Text = "Password must be at least 6 characters.";
                _txtAdminPassword.Focus();
                return;
            }

            string tier = _cmbTier.SelectedIndex switch
            {
                2 => Subscription.TierTenantC,
                1 => Subscription.TierTenantB,
                _ => Subscription.TierTenantA
            };

            string brandName = string.IsNullOrWhiteSpace(_txtDisplayName.Text)
                ? _txtCompanyName.Text.Trim()
                : _txtDisplayName.Text.Trim();

            Request = new CreateTenantRequest
            {
                CompanyName = _txtCompanyName.Text.Trim(),
                DisplayName = brandName,
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
                Width = width,
                Height = 32,
                Font = new Font("Segoe UI", 9.5f)
            };
            UiRadiusHelper.StyleStandardInput(txt);
            return txt;
        }
    }
}
