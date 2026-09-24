using System;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Drawing;
using CRMS_Peguit.domain.entities;
using CRMS_Peguit.winforms.Controllers;
using CRMS_Peguit.winforms.Models.Services;

namespace CRMS_Peguit.winforms.Views.Users
{
    public partial class AdminUserEditForm : Form
    {
        private readonly UserController _controller;
        private readonly User? _user;
        
        public AdminUserEditForm() : this(new UserController(), null)
        {
        }

        public AdminUserEditForm(UserController controller, User? user)
        {
            _controller = controller;
            _user = user;
            InitializeComponent();

            bool isEdit = _user != null;
            this.Text = isEdit ? "Edit User" : "Add New User";
            if (isEdit)
            {
                lblPassword.Visible = false;
                txtPassword.Visible = false;
                lblConfirm.Visible = false;
                txtConfirmPassword.Visible = false;
                this.ClientSize = new Size(400, 450);

                txtFirstName.Text = _user!.FirstName;
                txtMiddleName.Text = _user.MiddleName;
                txtLastName.Text = _user.LastName;
                txtSuffix.Text = _user.Suffix;
                txtEmail.Text = _user.Email;
            }

            UiRadiusHelper.StyleButton(btnSave, 8);

            UiRadiusHelper.StyleButton(btnCancel, 8);

            txtFirstName.MaxLength = 50;
            txtMiddleName.MaxLength = 50;
            txtLastName.MaxLength = 50;
            txtSuffix.MaxLength = 20;
            txtEmail.MaxLength = 100;

            btnSave.Click += async (s, e) => await SaveAsync();
            btnCancel.Click += (s, e) => { this.DialogResult = DialogResult.Cancel; this.Close(); };
            this.Load += async (s, e) => await LoadDataAsync();
        }

        private async Task LoadDataAsync()
        {
            var roles = await _controller.GetManagedRolesAsync();
            cmbRole.DataSource = roles;
            cmbRole.DisplayMember = "RoleName";
            cmbRole.ValueMember = "RoleId";

            if (_user != null)
            {
                cmbRole.SelectedValue = _user.RoleId;
            }
        }

        private async Task SaveAsync()
        {
            try
            {
                if (!ValidationHelper.IsValidPersonName(txtFirstName.Text, "First name", out string? fnErr))
                {
                    MessageBox.Show(fnErr, "Validation Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    txtFirstName.Focus();
                    return;
                }

                if (!ValidationHelper.IsValidPersonName(txtLastName.Text, "Last name", out string? lnErr))
                {
                    MessageBox.Show(lnErr, "Validation Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    txtLastName.Focus();
                    return;
                }

                if (string.IsNullOrWhiteSpace(txtEmail.Text) || !ContactEmailService.IsValidEmail(txtEmail.Text.Trim()))
                {
                    MessageBox.Show("Please enter a valid email address.", "Validation Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    txtEmail.Focus();
                    return;
                }

                if (cmbRole.SelectedValue == null)
                {
                    MessageBox.Show("Please select a role.", "Validation Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                var roleId = (int)cmbRole.SelectedValue;

                if (_user == null)
                {
                    if (string.IsNullOrWhiteSpace(txtPassword.Text) || txtPassword.Text != txtConfirmPassword.Text)
                    {
                        MessageBox.Show("Passwords do not match or are empty.", "Validation Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        return;
                    }

                    var newUser = new User
                    {
                        FirstName = txtFirstName.Text.Trim(),
                        MiddleName = txtMiddleName.Text.Trim(),
                        LastName = txtLastName.Text.Trim(),
                        Suffix = txtSuffix.Text.Trim(),
                        Email = txtEmail.Text.Trim(),
                        RoleId = roleId
                    };
                    await _controller.CreateAsync(newUser, txtPassword.Text);
                }
                else
                {
                    _user.FirstName = txtFirstName.Text.Trim();
                    _user.MiddleName = txtMiddleName.Text.Trim();
                    _user.LastName = txtLastName.Text.Trim();
                    _user.Suffix = txtSuffix.Text.Trim();
                    _user.Email = txtEmail.Text.Trim();
                    _user.RoleId = roleId;

                    await _controller.UpdateAsync(_user);
                }

                MessageBox.Show("User account saved successfully.", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
                this.DialogResult = DialogResult.OK;
                this.Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Error Saving User", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
    }
}

