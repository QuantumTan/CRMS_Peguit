using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using CRMS_Peguit.winforms.Models.Services;

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
            Size = new Size(480, 440);
            StartPosition = FormStartPosition.CenterParent;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;
            BackColor = Theme.Surface;

            var pnl = new Panel { Dock = DockStyle.Fill, Padding = new Padding(28) };
            Controls.Add(pnl);

            int y = 20;

            var lblHeader = new Label
            {
                Text = "New Administrator Account",
                Font = new Font("Segoe UI", 13f, FontStyle.Bold),
                ForeColor = Theme.TextPrimary,
                AutoSize = true,
                Location = new Point(0, y)
            };
            pnl.Controls.Add(lblHeader);
            y += 34;

            var lblSubtitle = new Label
            {
                Text = "Creates a User record in the selected tenant database.\nRole will be set to Admin.",
                Font = new Font("Segoe UI", 9f),
                ForeColor = Theme.TextSecondary,
                AutoSize = true,
                Location = new Point(0, y)
            };
            pnl.Controls.Add(lblSubtitle);
            y += 40;

            // Tenant
            pnl.Controls.Add(MakeLabel("Company / Tenant:", y));
            y += 22;
            _cmbTenant = new ComboBox
            {
                DropDownStyle = ComboBoxStyle.DropDownList,
                Font = new Font("Segoe UI", 10f),
                Location = new Point(0, y),
                Width = 410
            };
            foreach (var (tid, name) in _tenants)
                _cmbTenant.Items.Add($"[{tid}] {name}");
            if (_cmbTenant.Items.Count > 0) _cmbTenant.SelectedIndex = 0;
            pnl.Controls.Add(_cmbTenant);
            y += 34;

            // Names row
            pnl.Controls.Add(MakeLabel("First Name:", y));
            y += 22;
            _txtFirst = MakeTextBox(y, 195);
            pnl.Controls.Add(_txtFirst);

            var lblLast = MakeLabel("Last Name:", y - 22);
            lblLast.Location = new Point(215, y - 22);
            pnl.Controls.Add(lblLast);
            _txtLast = MakeTextBox(y, 195);
            _txtLast.Location = new Point(215, y);
            pnl.Controls.Add(_txtLast);
            y += 34;

            // Email
            pnl.Controls.Add(MakeLabel("Email Address:", y));
            y += 22;
            _txtEmail = MakeTextBox(y, 410);
            pnl.Controls.Add(_txtEmail);
            y += 34;

            // Password
            pnl.Controls.Add(MakeLabel("Password:", y));
            y += 22;
            _txtPassword = MakeTextBox(y, 410);
            _txtPassword.UseSystemPasswordChar = true;
            pnl.Controls.Add(_txtPassword);
            y += 34;

            // Error label
            _lblError = new Label
            {
                Text = "",
                Font = new Font("Segoe UI", 9f),
                ForeColor = Theme.StatusAlert,
                AutoSize = true,
                Location = new Point(0, y)
            };
            pnl.Controls.Add(_lblError);
            y += 24;

            // Buttons
            var btnCancel = new Button
            {
                Text = "Cancel",
                DialogResult = DialogResult.Cancel,
                Location = new Point(210, y),
                Size = new Size(90, 36),
                Font = new Font("Segoe UI", 9.5f),
                Cursor = Cursors.Hand
            };
            UiRadiusHelper.StyleButton(btnCancel, 6);
            pnl.Controls.Add(btnCancel);

            var btnCreate = new Button
            {
                Text = "Create Admin",
                Location = new Point(316, y),
                Size = new Size(120, 36),
                Font = new Font("Segoe UI", 9.5f, FontStyle.Bold),
                BackColor = Theme.Primary,
                ForeColor = Color.White,
                Cursor = Cursors.Hand
            };
            UiRadiusHelper.StyleButton(btnCreate, 6);
            btnCreate.Click += BtnCreate_Click;
            pnl.Controls.Add(btnCreate);

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
            Location = new Point(0, y)
        };

        private static TextBox MakeTextBox(int y, int width) => new TextBox
        {
            Font = new Font("Segoe UI", 10f),
            Location = new Point(0, y),
            Width = width
        };
    }
}
