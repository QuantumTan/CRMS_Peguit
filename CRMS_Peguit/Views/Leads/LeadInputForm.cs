using CRMS_Peguit.domain.entities;
using CRMS_Peguit.winforms.Models.Services;

using Lead = CRMS_Peguit.domain.entities.Lead;

namespace CRMS_Peguit.winforms.Views.Leads
{
    /// <summary>
    /// Dialog form for creating or editing a lead.
    /// </summary>
    public partial class LeadInputForm : Form
    {
        public Lead? Result { get; private set; }

        private readonly Lead? _existingLead;

        private TextBox txtFirstName = null!;
        private TextBox txtMiddleName = null!;
        private TextBox txtLastName = null!;
        private TextBox txtSuffix = null!;

        private TextBox txtEmail = null!;
        private TextBox txtPhone = null!;

        private ComboBox cmbSource = null!;
        private ComboBox cmbStage = null!;

        private Button btnSave = null!;
        private Button btnCancel = null!;

        public LeadInputForm(Lead? lead = null)
        {
            _existingLead = lead;

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

            Text = _existingLead is null
                ? "Add New Lead"
                : $"Edit Lead - {_existingLead.FullName}";

            // First name
            var lblFirstName = CreateLabel("First Name *", 20, 20);

            txtFirstName = CreateTextBox(20, 45, 220);
            txtFirstName.MaxLength = 100;

            // Middle name
            var lblMiddleName = CreateLabel("Middle Name", 260, 20);

            txtMiddleName = CreateTextBox(260, 45, 220);
            txtMiddleName.MaxLength = 100;

            // Last name
            var lblLastName = CreateLabel("Last Name *", 20, 90);

            txtLastName = CreateTextBox(20, 115, 220);
            txtLastName.MaxLength = 100;

            // Suffix
            var lblSuffix = CreateLabel("Suffix", 260, 90);

            txtSuffix = CreateTextBox(260, 115, 220);
            txtSuffix.MaxLength = 20;
            txtSuffix.PlaceholderText = "Jr., Sr., III, etc.";

            // Email
            var lblEmail = CreateLabel("Email", 20, 160);

            txtEmail = CreateTextBox(20, 185, 460);
            txtEmail.MaxLength = 200;

            // Phone
            var lblPhone = CreateLabel("Phone", 20, 230);

            txtPhone = CreateTextBox(20, 255, 460);
            txtPhone.MaxLength = 50;

            // Source
            var lblSource = CreateLabel("Source", 20, 300);

            cmbSource = new ComboBox
            {
                Location = new Point(20, 325),
                Width = 220,
                BackColor = Theme.Surface,
                ForeColor = Theme.TextPrimary,
                DropDownStyle = ComboBoxStyle.DropDownList
            };

            cmbSource.Items.AddRange(
                new[]
                {
                    "referral",
                    "website",
                    "walk-in",
                    "social media",
                    "other"
                });

            // Stage
            var lblStage = CreateLabel("Stage", 260, 300);

            cmbStage = new ComboBox
            {
                Location = new Point(260, 325),
                Width = 220,
                BackColor = Theme.Surface,
                ForeColor = Theme.TextPrimary,
                DropDownStyle = ComboBoxStyle.DropDownList
            };

            cmbStage.Items.AddRange(
                new[]
                {
                    "new",
                    "contacted",
                    "qualified",
                    "converted",
                    "lost"
                });

            // Save
            btnSave = new Button
            {
                Text = "Save",
                Location = new Point(300, 390),
                Width = 85,
                Height = 38,
                BackColor = Theme.Primary,
                ForeColor = Theme.Surface,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 10, FontStyle.Bold),
                Cursor = Cursors.Hand
            };

            btnSave.FlatAppearance.BorderSize = 0;
            btnSave.Click += BtnSaveClick;

            // Cancel
            btnCancel = new Button
            {
                Text = "Cancel",
                Location = new Point(395, 390),
                Width = 85,
                Height = 38,
                BackColor = Theme.Surface,
                ForeColor = Theme.TextPrimary,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 10, FontStyle.Bold),
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
                    lblSource,
                    cmbSource,
                    lblStage,
                    cmbStage,
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

        private TextBox CreateTextBox(int x, int y, int width)
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
            if (_existingLead is not null)
            {
                txtFirstName.Text = _existingLead.FirstName;
                txtMiddleName.Text = _existingLead.MiddleName ?? string.Empty;
                txtLastName.Text = _existingLead.LastName;
                txtSuffix.Text = _existingLead.Suffix ?? string.Empty;

                txtEmail.Text = _existingLead.Email ?? string.Empty;
                txtPhone.Text = _existingLead.Phone ?? string.Empty;

                SelectComboValue(
                    cmbSource,
                    _existingLead.Source,
                    "referral");

                SelectComboValue(
                    cmbStage,
                    _existingLead.Stage,
                    "new");
            }
            else
            {
                cmbSource.SelectedItem = "referral";
                cmbStage.SelectedItem = "new";
            }
        }

        private void BtnSaveClick(object? sender, EventArgs e)
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

            if (_existingLead is not null)
            {
                _existingLead.FirstName = firstName;
                _existingLead.MiddleName = NullIfEmpty(txtMiddleName.Text);
                _existingLead.LastName = lastName;
                _existingLead.Suffix = NullIfEmpty(txtSuffix.Text);

                _existingLead.Email = NullIfEmpty(txtEmail.Text);
                _existingLead.Phone = NullIfEmpty(txtPhone.Text);

                _existingLead.Source =
                    cmbSource.SelectedItem?.ToString() ?? "referral";

                _existingLead.Stage =
                    cmbStage.SelectedItem?.ToString() ?? "new";

                Result = _existingLead;
            }
            else
            {
                Result = new Lead
                {
                    FirstName = firstName,
                    MiddleName = NullIfEmpty(txtMiddleName.Text),
                    LastName = lastName,
                    Suffix = NullIfEmpty(txtSuffix.Text),

                    Email = NullIfEmpty(txtEmail.Text),
                    Phone = NullIfEmpty(txtPhone.Text),

                    Source =
                        cmbSource.SelectedItem?.ToString() ?? "referral",

                    Stage =
                        cmbStage.SelectedItem?.ToString() ?? "new",

                    CreatedAt = DateTime.UtcNow,
                    IsDeleted = false,
                    DeletedAt = null

                    // TenantId should be assigned by the controller,
                    // not hard-coded in the form.
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

            if (comboBox.Items.Contains(selectedValue))
            {
                comboBox.SelectedItem = selectedValue;
            }
            else
            {
                comboBox.SelectedItem = fallback;
            }
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