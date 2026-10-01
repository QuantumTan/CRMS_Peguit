using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using CRMS_Peguit.winforms.Models.Services;
using CRMS_Peguit.winforms.Services;

// =============================================================================
// CreateAdminDialog — Create a new Admin-role user in a specified tenant.
// Collects: First Name, Last Name, Email, Password, Tenant selection.
// NO CRM data — only User/Person metadata for administrator onboarding.
// =============================================================================

namespace CRMS_Peguit.winforms.Views.SuperAdmin
{
    public class CreateAdminDialog : Form
    {
        public string FirstName { get; private set; } = string.Empty;
        public string LastName { get; private set; } = string.Empty;
        public string Email { get; private set; } = string.Empty;
        public string Password { get; private set; } = string.Empty;
        public int SelectedTenantId { get; private set; }

        private readonly TextBox _txtFirst;
        private readonly TextBox _txtLast;
        private readonly TextBox _txtEmail;
        private readonly TextBox _txtPassword;
        private readonly ComboBox _cmbTenant;
        private readonly Label _lblError;

        private readonly List<(int TenantId, string CompanyName)> _tenants;

        public CreateAdminDialog(List<(int TenantId, string CompanyName)> tenants)
        {
            _tenants = tenants;

            Text = "Create Administrator";
            Size = new Size(540, 600);
            MinimumSize = new Size(500, 540);
            StartPosition = FormStartPosition.CenterParent;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;
            BackColor = Color.White;
            UiRadiusHelper.StyleModal(this, 8);

            var pnlContent = new Panel
            {
                Dock = DockStyle.Fill,
                Padding = new Padding(24),
                BackColor = Color.White,
                AutoScroll = true
            };

            int y = 24;

            var lblHeader = new Label
            {
                Text = "New Administrator Account",
                Font = new Font("Segoe UI", 13.5f, FontStyle.Bold),
                ForeColor = Theme.TextPrimary,
                AutoSize = true,
                Location = new Point(24, y)
            };
            pnlContent.Controls.Add(lblHeader);
            y += 32;

            var lblSubtitle = new Label
            {
                Text = "Creates a User record in the selected tenant database. Role will be set to Admin.",
                Font = new Font("Segoe UI", 9f),
                ForeColor = Theme.TextSecondary,
                AutoSize = true,
                Location = new Point(24, y)
            };
            pnlContent.Controls.Add(lblSubtitle);
            y += 34;

            // Tenant
            pnlContent.Controls.Add(MakeLabel("Company / Tenant:", y));
            y += 20;
            _cmbTenant = new ComboBox
            {
                DropDownStyle = ComboBoxStyle.DropDownList,
                Location = new Point(24, y),
                Width = 436
            };
            UiRadiusHelper.StyleStandardComboBox(_cmbTenant);
            foreach (var (tid, name) in _tenants)
                _cmbTenant.Items.Add($"[{tid}] {name}");
            if (_cmbTenant.Items.Count > 0) _cmbTenant.SelectedIndex = 0;
            pnlContent.Controls.Add(_cmbTenant);
            y += 38;

            // Names row
            pnlContent.Controls.Add(MakeLabel("First Name:", y));
            var lblLast = MakeLabel("Last Name:", y);
            lblLast.Location = new Point(248, y);
            pnlContent.Controls.Add(lblLast);
            y += 20;

            _txtFirst = MakeTextBox(y, 212);
            pnlContent.Controls.Add(_txtFirst);

            _txtLast = MakeTextBox(y, 212);
            _txtLast.Location = new Point(248, y);
            pnlContent.Controls.Add(_txtLast);
            y += 38;

            // Email
            pnlContent.Controls.Add(MakeLabel("Email Address:", y));
            y += 20;
            _txtEmail = MakeTextBox(y, 436);
            pnlContent.Controls.Add(_txtEmail);
            y += 38;

            // Password
            pnlContent.Controls.Add(MakeLabel("Password:", y));
            y += 20;
            _txtPassword = MakeTextBox(y, 436);
            _txtPassword.UseSystemPasswordChar = true;
            pnlContent.Controls.Add(_txtPassword);
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
            y += 24;

            // Spacing panel at the bottom of content to ensure comfortable scrolling
            var pnlBottomSpacer = new Panel
            {
                Location = new Point(24, y),
                Size = new Size(436, 20),
                BackColor = Color.Transparent
            };
            pnlContent.Controls.Add(pnlBottomSpacer);

            // Fixed Footer Panel (Dock = Bottom, Height = 64)
            var pnlFooter = new Panel
            {
                Dock = DockStyle.Bottom,
                Height = 64,
                BackColor = Color.FromArgb(248, 250, 252),
                Padding = new Padding(20, 14, 20, 14)
            };
            pnlFooter.Paint += (s, e) =>
            {
                using var p = new Pen(Theme.Border, 1f);
                e.Graphics.DrawLine(p, 0, 0, pnlFooter.Width, 0);
            };

            var pnlButtons = new FlowLayoutPanel
            {
                Dock = DockStyle.Right,
                Width = 280,
                FlowDirection = FlowDirection.RightToLeft,
                BackColor = Color.Transparent
            };

            var btnCreate = new Button
            {
                Text = "Create Admin",
                Size = new Size(130, 36),
                BackColor = Theme.Primary,
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 9.5f, FontStyle.Bold),
                Cursor = Cursors.Hand,
                Margin = new Padding(10, 0, 0, 0)
            };
            UiRadiusHelper.StylePrimaryButton(btnCreate, 6);
            btnCreate.Click += BtnCreate_Click;

            var btnCancel = new Button
            {
                Text = "Cancel",
                DialogResult = DialogResult.Cancel,
                Size = new Size(100, 36),
                BackColor = Theme.Surface,
                ForeColor = Theme.TextPrimary,
                Font = new Font("Segoe UI", 9f),
                Cursor = Cursors.Hand,
                Margin = new Padding(0)
            };
            UiRadiusHelper.StyleSecondaryButton(btnCancel, 6);

            pnlButtons.Controls.Add(btnCreate);
            pnlButtons.Controls.Add(btnCancel);
            pnlFooter.Controls.Add(pnlButtons);

            // Correct docking order: Fill added first, then Bottom
            Controls.Add(pnlContent);
            Controls.Add(pnlFooter);
            ResponsiveLayout.BindInputPanel(pnlContent);

            AcceptButton = btnCreate;
            CancelButton = btnCancel;
        }

        private void BtnCreate_Click(object? sender, EventArgs e)
        {
            _lblError.Text = "";

            if (string.IsNullOrWhiteSpace(_txtFirst.Text))
            { _lblError.Text = "First name is required."; return; }
            if (string.IsNullOrWhiteSpace(_txtLast.Text))
            { _lblError.Text = "Last name is required."; return; }
            if (string.IsNullOrWhiteSpace(_txtEmail.Text) || !_txtEmail.Text.Contains('@'))
            { _lblError.Text = "Valid email address is required."; return; }
            if (_txtPassword.Text.Length < 6)
            { _lblError.Text = "Password must be at least 6 characters."; return; }
            if (_cmbTenant.SelectedIndex < 0)
            { _lblError.Text = "Please select a tenant."; return; }

            FirstName = _txtFirst.Text.Trim();
            LastName = _txtLast.Text.Trim();
            Email = _txtEmail.Text.Trim();
            Password = _txtPassword.Text;
            SelectedTenantId = _tenants[_cmbTenant.SelectedIndex].TenantId;

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
