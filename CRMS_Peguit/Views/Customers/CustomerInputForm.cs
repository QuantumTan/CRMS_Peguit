using CRMS_Peguit.domain.entities;
using CRMS_Peguit.winforms.Services;
using CRMS_Peguit.winforms.Models.Services;

namespace CRMS_Peguit.winforms.Views.Customers
{
    /// <summary>
    /// Dialog form for creating or editing a customer.
    /// </summary>
    public partial class CustomerInputForm : Form
    {
        public Customer? Result { get; private set; }

        private readonly Customer? _existingCustomer;

        public CustomerInputForm(Customer? customer = null)
        {
            _existingCustomer = customer;

            InitializeComponent();
            UiRadiusHelper.StyleButton(btnSave, 8);
            UiRadiusHelper.StyleButton(btnCancel, 8);
            UiRadiusHelper.AttachHoverFeedback(btnCancel, Color.White, Color.FromArgb(241, 245, 249));
            UiRadiusHelper.AttachHoverFeedback(btnSave, Theme.Primary, Theme.PrimaryDark);
            btnSave.Click += BtnSaveClick;

            txtFirstName.MaxLength = 50;
            txtMiddleName.MaxLength = 50;
            txtLastName.MaxLength = 50;
            txtPhone.MaxLength = 20;

            LoadData();
        }

        private void LoadData()
        {
            Text = _existingCustomer is null
                ? "Add New Customer"
                : $"Edit Customer - {_existingCustomer.FullName}";

            bool isPendingReview = _existingCustomer is not null &&
                                  string.Equals(_existingCustomer.AssignmentStatus, "pending_review", StringComparison.OrdinalIgnoreCase) &&
                                  !CRMS_Peguit.winforms.Auth.RbacService.HasFullOversight;

            if (isPendingReview)
            {
                btnSave.Enabled = false;
                btnSave.Text = "Pending Review";
                btnSave.BackColor = Color.FromArgb(148, 163, 184);
                Text += " (Under Managerial Review - Read Only)";
            }

            if (_existingCustomer is not null)
            {
                txtFirstName.Text = _existingCustomer.FirstName;
                txtMiddleName.Text = _existingCustomer.MiddleName ?? string.Empty;
                txtLastName.Text = _existingCustomer.LastName;
                
                string sfx = _existingCustomer.Suffix ?? string.Empty;
                if (!string.IsNullOrWhiteSpace(sfx) && !cmbSuffix.Items.Contains(sfx))
                {
                    cmbSuffix.Items.Add(sfx);
                }
                cmbSuffix.Text = sfx;

                txtEmail.Text = _existingCustomer.Email ?? string.Empty;
                txtPhone.Text = _existingCustomer.Phone ?? string.Empty;
                SelectComboValue(cmbType, _existingCustomer.Type, "buyer");
                SelectComboValue(cmbStatus, _existingCustomer.Status, "active");
                cmbStatus.Enabled = !isPendingReview;

                if (isPendingReview)
                {
                    txtFirstName.ReadOnly = true;
                    txtMiddleName.ReadOnly = true;
                    txtLastName.ReadOnly = true;
                    cmbSuffix.Enabled = false;
                    txtEmail.ReadOnly = true;
                    txtPhone.ReadOnly = true;
                    cmbType.Enabled = false;
                    cmbStatus.Enabled = false;
                }
            }
            else
            {
                cmbSuffix.SelectedIndex = -1;
                cmbSuffix.Text = string.Empty;
                cmbType.SelectedItem = "buyer";
                cmbStatus.SelectedItem = "active";
                cmbStatus.Enabled = false; // New customers default to active
            }
        }

        private void BtnSaveClick(object? sender, EventArgs e)
        {
            string firstName = txtFirstName.Text.Trim();
            string lastName = txtLastName.Text.Trim();

            if (!CustomerApiService.ValidateCustomerInput(firstName, lastName, txtEmail.Text, txtPhone.Text, out string? error))
            {
                Control target = error?.Contains("email", StringComparison.OrdinalIgnoreCase) == true
                    ? txtEmail
                    : (error?.Contains("phone", StringComparison.OrdinalIgnoreCase) == true
                        ? txtPhone
                        : (error?.Contains("Last", StringComparison.OrdinalIgnoreCase) == true ? txtLastName : txtFirstName));
                ShowValidationError(error ?? "Validation error.", target);
                return;
            }

            if (_existingCustomer is not null)
            {
                _existingCustomer.FirstName = firstName;
                _existingCustomer.MiddleName = NullIfEmpty(txtMiddleName.Text);
                _existingCustomer.LastName = lastName;
                _existingCustomer.Suffix = NullIfEmpty(cmbSuffix.Text);
                _existingCustomer.Email = NullIfEmpty(txtEmail.Text);
                _existingCustomer.Phone = NullIfEmpty(txtPhone.Text);
                _existingCustomer.Type = cmbType.SelectedItem?.ToString() ?? "buyer";
                _existingCustomer.Status = cmbStatus.SelectedItem?.ToString() ?? "active";
                Result = _existingCustomer;
            }
            else
            {
                Result = new Customer
                {
                    FirstName = firstName,
                    MiddleName = NullIfEmpty(txtMiddleName.Text),
                    LastName = lastName,
                    Suffix = NullIfEmpty(cmbSuffix.Text),
                    Email = NullIfEmpty(txtEmail.Text),
                    Phone = NullIfEmpty(txtPhone.Text),
                    Type = cmbType.SelectedItem?.ToString() ?? "buyer",
                    Status = cmbStatus.SelectedItem?.ToString() ?? "active",
                    CreatedAt = DateTime.UtcNow,
                    IsDeleted = false,
                    DeletedAt = null
                };
            }

            DialogResult = DialogResult.OK;
            Close();
        }

        private static string? NullIfEmpty(string? value)
        {
            return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
        }

        private static void SelectComboValue(ComboBox comboBox, string? value, string fallback)
        {
            string selectedValue = string.IsNullOrWhiteSpace(value) ? fallback : value;
            comboBox.SelectedItem = comboBox.Items.Contains(selectedValue) ? selectedValue : fallback;
        }

        private static void ShowValidationError(string message, Control control)
        {
            MessageBox.Show(message, "Validation Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            control.Focus();
        }
    }
}
