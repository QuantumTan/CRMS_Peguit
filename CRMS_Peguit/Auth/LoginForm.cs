using CRMS_Peguit.winforms.Auth;
using CRMS_Peguit.winforms.Models.Services;
using ReaLTaiizor.Forms;
using System;
using System.Drawing;
using System.Windows.Forms;

namespace CRMS_Peguit.winforms
{
    public partial class LoginForm : MaterialForm
    {
        private readonly AuthService _authService;
        private TextBox txtCompanyId;
        private TextBox txtEmail;
        private TextBox txtPassword;
        private Button btnLogin;
        private Label lblError;
        private Label lblLogo;

        // TODO: replace with your actual monsterASP URL once deployed
        public LoginForm() : this("https://your-app.runasp.net/") { }

        public LoginForm(string apiBaseUrl)
        {
            _authService = new AuthService(apiBaseUrl);
            BuildUi();
        }

        private void BuildUi()
        {
            ClientSize = new Size(420, 440);
            StartPosition = FormStartPosition.CenterScreen;
            Text = "NEXA - Sign In";
            BackColor = Theme.Background;

            lblLogo = new Label
            {
                Text = "NEXA",
                ForeColor = Theme.Primary,
                Font = new Font("Segoe UI", 26, FontStyle.Bold),
                AutoSize = true,
                Location = new Point(150, 30)
            };

            var lblCompanyId = new Label
            {
                Text = "Company ID",
                ForeColor = Theme.TextPrimary,
                Location = new Point(50, 100),
                AutoSize = true
            };
            txtCompanyId = new TextBox
            {
                Location = new Point(50, 120),
                Size = new Size(320, 28),
                Font = new Font("Segoe UI", 10.5f)
            };

            var lblEmail = new Label
            {
                Text = "Email",
                ForeColor = Theme.TextPrimary,
                Location = new Point(50, 160),
                AutoSize = true
            };
            txtEmail = new TextBox
            {
                Location = new Point(50, 180),
                Size = new Size(320, 28),
                Font = new Font("Segoe UI", 10.5f)
            };

            var lblPassword = new Label
            {
                Text = "Password",
                ForeColor = Theme.TextPrimary,
                Location = new Point(50, 220),
                AutoSize = true
            };
            txtPassword = new TextBox
            {
                Location = new Point(50, 240),
                Size = new Size(320, 28),
                Font = new Font("Segoe UI", 10.5f),
                UseSystemPasswordChar = true
            };

            lblError = new Label
            {
                ForeColor = Color.IndianRed,
                Location = new Point(50, 275),
                Size = new Size(320, 40),
                Font = new Font("Segoe UI", 9f)
            };

            btnLogin = new Button
            {
                Text = "Sign In",
                BackColor = Theme.Primary,
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Location = new Point(50, 325),
                Size = new Size(320, 40),
                Cursor = Cursors.Hand,
                FlatAppearance = { BorderSize = 0 }
            };
            btnLogin.Click += BtnLogin_Click;

            Controls.Add(lblLogo);
            Controls.Add(lblCompanyId);
            Controls.Add(txtCompanyId);
            Controls.Add(lblEmail);
            Controls.Add(txtEmail);
            Controls.Add(lblPassword);
            Controls.Add(txtPassword);
            Controls.Add(lblError);
            Controls.Add(btnLogin);
        }

        private async void BtnLogin_Click(object? sender, EventArgs e)
        {
            lblError.Text = "";

            if (string.IsNullOrWhiteSpace(txtCompanyId.Text))
            {
                lblError.Text = "Company ID is required.";
                return;
            }

            btnLogin.Enabled = false;
            btnLogin.Text = "Signing in...";

            var result = await _authService.LoginAsync(
                txtCompanyId.Text.Trim(), txtEmail.Text.Trim(), txtPassword.Text);

            btnLogin.Enabled = true;
            btnLogin.Text = "Sign In";

            if (!result.Success)
            {
                lblError.Text = result.ErrorMessage ?? "Login failed.";
                return;
            }

            if (result.WasOffline)
            {
                MessageBox.Show(
                    "You're offline. Signed in using your last saved credentials.",
                    "Offline Mode", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }

            var main = new Form1();
            main.Show();
            Hide();
        }
    }
}