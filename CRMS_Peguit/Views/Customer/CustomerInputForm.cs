using CRMS_Peguit.domain.entities;
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

        private TextBox txtFirstName = null!;
        private TextBox txtMiddleName = null!;
        private TextBox txtLastName = null!;
        private TextBox txtSuffix = null!;

        private TextBox txtEmail = null!;
        private TextBox txtPhone = null!;

        private ComboBox cmbType = null!;
        private ComboBox cmbStatus = null!;

        private Button btnSave = null!;
        private Button btnCancel = null!;

        public CustomerInputForm(Customer? customer = null)
        {
            _existingCustomer = customer;

            InitializeComponent();
            LoadData();
        }

        private void InitializeComponent()
        {
            Width = 520;
            Height = 500;
            StartPosition = FormStartPosition.CenterParent;
            BackColor = Theme.Background;
            ForeColor = Theme.TextPrimary;
            Font = new Font("Segoe UI", 10);
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;
            ShowInTaskbar = false;

            Text = _existingCustomer is null
                ? "Add New Customer"
                : $"Edit Customer - {_existingCustomer.FullName}";

            var lblFirstName =
                CreateLabel("First Name *", 20, 20);

            txtFirstName = CreateTextBox(20, 45, 220);
            txtFirstName.MaxLength = 100;

            var lblMiddleName =
                CreateLabel("Middle Name", 260, 20);

            txtMiddleName = CreateTextBox(260, 45, 220);
            txtMiddleName.MaxLength = 100;

            var lblLastName =
                CreateLabel("Last Name *", 20, 90);

            txtLastName = CreateTextBox(20, 115, 220);
            txtLastName.MaxLength = 100;

            var lblSuffix =
                CreateLabel("Suffix", 260, 90);

            txtSuffix = CreateTextBox(260, 115, 220);
            txtSuffix.MaxLength = 20;
            txtSuffix.PlaceholderText = "Jr., Sr., III, etc.";

            var lblEmail =
                CreateLabel("Email", 20, 160);

            txtEmail = CreateTextBox(20, 185, 460);
            txtEmail.MaxLength = 255;

            var lblPhone =
                CreateLabel("Phone", 20, 230);

            txtPhone = CreateTextBox(20, 255, 460);
            txtPhone.MaxLength = 50;

            var lblType =
                CreateLabel("Type", 20, 300);

            cmbType = new ComboBox
            {
                Location = new Point(20, 325),
                Width = 220,
                BackColor = Theme.Surface,
                ForeColor = Theme.TextPrimary,
                DropDownStyle = ComboBoxStyle.DropDownList
            };

            cmbType.Items.AddRange(
                new[]
                {
                    "buyer",
                    "seller",
                    "both"
                });

            var lblStatus =
                CreateLabel("Status", 260, 300);

            cmbStatus = new ComboBox
            {
                Location = new Point(260, 325),
                Width = 220,
                BackColor = Theme.Surface,
                ForeColor = Theme.TextPrimary,
                DropDownStyle = ComboBoxStyle.DropDownList
            };

            cmbStatus.Items.AddRange(
                new[]
                {
                    "active",
                    "inactive",
                    "prospect"
                });

            btnSave = new Button
            {
                Text = "Save",
                Location = new Point(300, 390),
                Width = 85,
                Height = 38,
                BackColor = Theme.Primary,
                ForeColor = Theme.Surface,
                FlatStyle = FlatStyle.Flat,
                Font = new Font(
                    "Segoe UI",
                    10,
                    FontStyle.Bold),

                Cursor = Cursors.Hand
            };

            btnSave.FlatAppearance.BorderSize = 0;
            btnSave.Click += BtnSaveClick;

            btnCancel = new Button
            {
                Text = "Cancel",
                Location = new Point(395, 390),
                Width = 85,
                Height = 38,
                BackColor = Theme.Surface,
                ForeColor = Theme.TextPrimary,
                FlatStyle = FlatStyle.Flat,
                Font = new Font(
                    "Segoe UI",
                    10,
                    FontStyle.Bold),

                Cursor = Cursors.Hand,
                DialogResult = DialogResult.Cancel
            };

            Controls.AddRange(
                new Control[]
                {
                    lblFirstName,
                    txtFirstName,
                    lblMiddleName,
                    txtMiddleName,
                    lblLastName,
                    txtLastName,
                    lblSuffix,
                    txtSuffix,
                    lblEmail,
                    txtEmail,
                    lblPhone,
                    txtPhone,
                    lblType,
                    cmbType,
                    lblStatus,
                    cmbStatus,
                    btnSave,
                    btnCancel
                });

            CancelButton = btnCancel;
            AcceptButton = btnSave;
        }

        private Label CreateLabel(string text, int x, int y)
        {
            return new Label
            {
                Text = text,
                Location = new Point(x, y),
                AutoSize = true,
                ForeColor = Theme.TextPrimary
            };
        }

        private TextBox CreateTextBox(
            int x,
            int y,
            int width)
        {
            return new TextBox
            {
                Location = new Point(x, y),
                Width = width,
                BackColor = Theme.Surface,
                ForeColor = Theme.TextPrimary,
                BorderStyle = BorderStyle.FixedSingle
            };
        }

        private void LoadData()
        {
            if (_existingCustomer is not null)
            {
                txtFirstName.Text =
                    _existingCustomer.FirstName;

                txtMiddleName.Text =
                    _existingCustomer.MiddleName ?? string.Empty;

                txtLastName.Text =
                    _existingCustomer.LastName;

                txtSuffix.Text =
                    _existingCustomer.Suffix ?? string.Empty;

                txtEmail.Text =
                    _existingCustomer.Email ?? string.Empty;

                txtPhone.Text =
                    _existingCustomer.Phone ?? string.Empty;

                SelectComboValue(
                    cmbType,
                    _existingCustomer.Type,
                    "buyer");

                SelectComboValue(
                    cmbStatus,
                    _existingCustomer.Status,
                    "active");
            }
            else
            {
                cmbType.SelectedItem = "buyer";
                cmbStatus.SelectedItem = "active";
            }
        }

        private void BtnSaveClick(
            object? sender,
            EventArgs e)
        {
            string firstName = txtFirstName.Text.Trim();
            string lastName = txtLastName.Text.Trim();

            if (string.IsNullOrWhiteSpace(firstName))
            {
                ShowValidationError(
                    "First name is required.",
                    txtFirstName);

                return;
            }

            if (string.IsNullOrWhiteSpace(lastName))
            {
                ShowValidationError(
                    "Last name is required.",
                    txtLastName);

                return;
            }

            if (_existingCustomer is not null)
            {
                _existingCustomer.FirstName = firstName;

                _existingCustomer.MiddleName =
                    NullIfEmpty(txtMiddleName.Text);

                _existingCustomer.LastName = lastName;

                _existingCustomer.Suffix =
                    NullIfEmpty(txtSuffix.Text);

                _existingCustomer.Email =
                    NullIfEmpty(txtEmail.Text);

                _existingCustomer.Phone =
                    NullIfEmpty(txtPhone.Text);

                _existingCustomer.Type =
                    cmbType.SelectedItem?.ToString() ?? "buyer";

                _existingCustomer.Status =
                    cmbStatus.SelectedItem?.ToString() ?? "active";

                Result = _existingCustomer;
            }
            else
            {
                Result = new Customer
                {
                    FirstName = firstName,

                    MiddleName =
                        NullIfEmpty(txtMiddleName.Text),

                    LastName = lastName,

                    Suffix =
                        NullIfEmpty(txtSuffix.Text),

                    Email =
                        NullIfEmpty(txtEmail.Text),

                    Phone =
                        NullIfEmpty(txtPhone.Text),

                    Type =
                        cmbType.SelectedItem?.ToString() ?? "buyer",

                    Status =
                        cmbStatus.SelectedItem?.ToString() ?? "active",

                    CreatedAt = DateTime.UtcNow,
                    IsDeleted = false,
                    DeletedAt = null

                    // Controller assigns TenantId.
                };
            }

            DialogResult = DialogResult.OK;
            Close();
        }

        private static string? NullIfEmpty(string? value)
        {
            return string.IsNullOrWhiteSpace(value)
                ? null
                : value.Trim();
        }

        private static void SelectComboValue(
            ComboBox comboBox,
            string? value,
            string fallback)
        {
            string selectedValue = string.IsNullOrWhiteSpace(value)
                ? fallback
                : value;

            comboBox.SelectedItem =
                comboBox.Items.Contains(selectedValue)
                    ? selectedValue
                    : fallback;
        }

        private static void ShowValidationError(
            string message,
            Control control)
        {
            MessageBox.Show(
                message,
                "Validation Error",
                MessageBoxButtons.OK,
                MessageBoxIcon.Warning);

            control.Focus();
        }
    }
}