using System;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Drawing;
using CRMS_Peguit.domain.entities;
using CRMS_Peguit.winforms.Auth;
using CRMS_Peguit.winforms.Controllers;
using CRMS_Peguit.winforms.Models.Services;
using CRMS_Peguit.winforms.Services;

namespace CRMS_Peguit.winforms.Views.Users
{
    public partial class AdminUserEditForm : Form
    {
        private readonly UserController _controller;
        private readonly User? _user;
        private Label? _lblBranch;
        private ComboBox? _cmbBranch;
        
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
                this.ClientSize = new Size(420, CurrentSession.CanAccessBranching ? 500 : 450);

                txtFirstName.Text = _user!.FirstName;
                txtMiddleName.Text = _user.MiddleName;
                txtLastName.Text = _user.LastName;
                txtSuffix.Text = _user.Suffix;
                txtEmail.Text = _user.Email;
            }
            else
            {
                this.ClientSize = new Size(420, CurrentSession.CanAccessBranching ? 580 : 540);
            }

            if (CurrentSession.CanAccessBranching)
            {
                _lblBranch = new Label
                {
                    Text = "Assigned Branch *",
                    Location = new Point(cmbRole.Left, cmbRole.Bottom + 12),
                    AutoSize = true,
                    Font = new Font("Segoe UI", 9f, FontStyle.Bold),
                    ForeColor = Color.FromArgb(107, 33, 168)
                };

                _cmbBranch = new ComboBox
                {
                    DropDownStyle = ComboBoxStyle.DropDownList,
                    Location = new Point(cmbRole.Left, _lblBranch.Bottom + 4),
                    Size = new Size(cmbRole.Width, 26),
                    Font = new Font("Segoe UI", 9f)
                };

                pnlContent.Controls.Add(_lblBranch);
                pnlContent.Controls.Add(_cmbBranch);

                int offset = 54;
                if (!isEdit)
                {
                    lblPassword.Top += offset;
                    txtPassword.Top += offset;
                    lblConfirm.Top += offset;
                    txtConfirmPassword.Top += offset;
                }
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

            if (CurrentSession.CanAccessBranching && _cmbBranch != null)
            {
                var branchController = new BranchController();
                var branches = await branchController.GetAllBranchesAsync();
                var branchOptions = new List<object>();
                foreach (var b in branches.Where(b => b.IsActive))
                {
                    branchOptions.Add(new { BranchId = b.BranchId, BranchDisplay = $"🏢 {b.BranchName} ({b.BranchCode})" });
                }

                _cmbBranch.DisplayMember = "BranchDisplay";
                _cmbBranch.ValueMember = "BranchId";
                _cmbBranch.DataSource = branchOptions;

                if (_user?.BranchId.HasValue == true && _user.BranchId.Value > 0)
                {
                    _cmbBranch.SelectedValue = _user.BranchId.Value;
                }
                else if (CurrentSession.ActiveBranchId.HasValue)
                {
                    _cmbBranch.SelectedValue = CurrentSession.ActiveBranchId.Value;
                }
                else
                {
                    _cmbBranch.SelectedIndex = branchOptions.Count > 0 ? 0 : -1;
                }

                if (branchOptions.Count == 0)
                {
                    _cmbBranch.Enabled = false;
                    _lblBranch!.Text = "Assigned Branch * (create an active branch first)";
                }
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
                int? branchId = null;
                if (CurrentSession.CanAccessBranching && _cmbBranch?.SelectedValue is int bId && bId > 0)
                {
                    branchId = bId;
                }

                if (CurrentSession.CanAccessBranching && !branchId.HasValue)
                {
                    MessageBox.Show(
                        "Every Manager and Agent account must be assigned to an active branch. Create or activate a branch, then select it here.",
                        "Branch Assignment Required",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Warning);
                    _cmbBranch?.Focus();
                    return;
                }

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
                        RoleId = roleId,
                        BranchId = branchId
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
                    _user.BranchId = branchId;

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

