using CRMS_Peguit.winforms.Auth;
using CRMS_Peguit.winforms.Controllers;
using CRMS_Peguit.winforms.Models.Services;
using CRMS_Peguit.winforms.Models.Roles;
using ReaLTaiizor.Forms;
using System;
using System.Drawing;
using System.Windows.Forms;

namespace CRMS_Peguit.winforms
{
    public partial class LoginForm : Form
    {
        private readonly AuthController _authController;

        // Store main form or super admin form and their FormClosed handlers.
        private MainForm? _mainForm;
        private FormClosedEventHandler? _mainFormClosedHandler;
        private SuperAdminForm? _superAdminForm;
        private FormClosedEventHandler? _superAdminClosedHandler;

        // ==========================================================
        // DEFAULT CONSTRUCTOR
        // ==========================================================

        public LoginForm()
            : this(DbConfiguration.GetApiBaseUrl())
        {
        }

        // ==========================================================
        // API CONSTRUCTOR
        // ==========================================================

        public LoginForm(string apiBaseUrl)
        {
            _authController = new AuthController(apiBaseUrl);
            InitializeComponent();
            ApplyBranding();
            BindEvents();
        }

        private void ApplyBranding()
        {
            AppBrand.ApplyAppIcon(this);
            if (AppBrand.Logo != null)
            {
                picBrandLogo.Image = AppBrand.Logo;
            }
        }

        private void BindEvents()
        {
            chkShowPassword.CheckedChanged += (_, _) =>
            {
                txtPassword.UseSystemPasswordChar = !chkShowPassword.Checked;
            };

            lnkForgotPassword.Click += LnkForgotPasswordClick;
            btnLogin.Click += BtnLogin_Click;
            UiRadiusHelper.StyleButton(btnLogin, 8);
            UiRadiusHelper.SetPadding(txtEmail, 8, 8);
            UiRadiusHelper.SetPadding(txtPassword, 8, 8);
        }

        // ==========================================================
        // LOGIN BUTTON
        // ==========================================================

        private async void BtnLogin_Click(object? sender, EventArgs e)
        {
            lblError.Text = "";

            var validation = _authController.ValidateCredentials(txtEmail.Text, txtPassword.Text);
            if (!validation.IsValid)
            {
                lblError.Text = validation.ErrorMessage ?? "Invalid credentials.";
                if (lblError.Text.Contains("email", StringComparison.OrdinalIgnoreCase))
                    txtEmail.Focus();
                else if (lblError.Text.Contains("password", StringComparison.OrdinalIgnoreCase))
                    txtPassword.Focus();
                return;
            }

            btnLogin.Enabled = false;
            btnLogin.Text = "Signing in...";

            try
            {
                var result = await _authController.LoginAsync(
                    txtEmail.Text,
                    txtPassword.Text
                );

                if (!result.Success)
                {
                    lblError.Text = result.ErrorMessage ?? "Login failed.";
                    return;
                }

                if (result.WasOffline)
                {
                    MessageBox.Show(
                        "You're offline. Signed in using your last saved credentials.",
                        "Offline Mode",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Information
                    );
                }

                // Route to the appropriate shell based on role.
                // SuperAdmin gets the separate SuperAdminForm — never MainForm.
                // All other roles get MainForm.
                if (CurrentSession.CurrentUser?.Role == UserRole.SuperAdmin)
                {
                    _superAdminForm = new SuperAdminForm(this);
                    _superAdminClosedHandler = (s, args) => Close();
                    _superAdminForm.FormClosed += _superAdminClosedHandler;
                    _superAdminForm.Show();
                }
                else
                {
                    _mainForm = new MainForm();
                    _mainFormClosedHandler = (s, args) => Close();
                    _mainForm.FormClosed += _mainFormClosedHandler;
                    _mainForm.Show();
                }
                Hide();
            }
            catch (Exception ex)
            {
                lblError.Text = $"Unexpected error: {ex.Message}";
            }
            finally
            {
                btnLogin.Enabled = true;
                btnLogin.Text = "Sign In";
            }
        }

        private async void LnkForgotPasswordClick(object? sender, EventArgs e)
        {
            lblError.Text = "";

            if (string.IsNullOrWhiteSpace(txtEmail.Text))
            {
                lblError.Text = "Email is required before requesting a reset.";
                txtEmail.Focus();
                return;
            }

            if (!ContactEmailService.IsValidEmail(txtEmail.Text))
            {
                lblError.Text = "Enter a valid email before requesting a reset.";
                txtEmail.Focus();
                return;
            }

            lnkForgotPassword.Enabled = false;

            try
            {
                var result = await ContactEmailService.SendForgotPasswordAsync(
                    txtEmail.Text.Trim());

                MessageBox.Show(
                    result.Message,
                    result.Success ? "Password Reset Email" : "Email Error",
                    MessageBoxButtons.OK,
                    result.Success ? MessageBoxIcon.Information : MessageBoxIcon.Warning
                );
            }
            finally
            {
                lnkForgotPassword.Enabled = true;
            }
        }

        // ==========================================================
        // Called by Form1 during logout to detach and show login form.
        // ==========================================================

        public void PrepareForLogout()
        {
            if (_mainForm != null && _mainFormClosedHandler != null)
            {
                _mainForm.FormClosed -= _mainFormClosedHandler;
                _mainForm = null;
                _mainFormClosedHandler = null;
            }

            if (_superAdminForm != null && _superAdminClosedHandler != null)
            {
                _superAdminForm.FormClosed -= _superAdminClosedHandler;
                _superAdminForm = null;
                _superAdminClosedHandler = null;
            }

            Show();
            Activate();
        }
    }
}


