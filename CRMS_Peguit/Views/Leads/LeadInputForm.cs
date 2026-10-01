using CRMS_Peguit.domain.entities;
using CRMS_Peguit.winforms.Auth;
using CRMS_Peguit.winforms.Controllers;
using CRMS_Peguit.winforms.Models.Services;

namespace CRMS_Peguit.winforms.Views.Leads
{
    /// <summary>
    /// Dialog form for creating or editing a lead.
    /// </summary>
    public partial class LeadInputForm : Form
    {
        public Lead? Result { get; private set; }

        private readonly Lead? _existingLead;

        public LeadInputForm(Lead? lead = null)
        {
            _existingLead = lead;

            InitializeComponent();
            CRMS_Peguit.winforms.Services.ResponsiveLayout.BindInputPanel(pnlContent);
            CRMS_Peguit.winforms.Services.ResponsiveLayout.BindToolbar(pnlFooter, 12, btnCancel, btnSave);
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
            Text = _existingLead is null
                ? "Add New Lead"
                : $"Edit Lead - {_existingLead.FullName}";

            PopulateSources();

            bool isPendingReview = _existingLead is not null &&
                                  string.Equals(_existingLead.AssignmentStatus, "pending_review", StringComparison.OrdinalIgnoreCase) &&
                                  !CRMS_Peguit.winforms.Auth.RbacService.HasFullOversight;

            if (isPendingReview)
            {
                btnSave.Enabled = false;
                btnSave.Text = "Pending Review";
                btnSave.BackColor = Color.FromArgb(148, 163, 184);
                Text += " (Under Managerial Review - Read Only)";
            }

            if (_existingLead is not null)
            {
                txtFirstName.Text = _existingLead.FirstName;
                txtMiddleName.Text = _existingLead.MiddleName ?? string.Empty;
                txtLastName.Text = _existingLead.LastName;
                
                string sfx = _existingLead.Suffix ?? string.Empty;
                if (!string.IsNullOrWhiteSpace(sfx) && !cmbSuffix.Items.Contains(sfx))
                {
                    cmbSuffix.Items.Add(sfx);
                }
                cmbSuffix.Text = sfx;

                txtEmail.Text = _existingLead.Email ?? string.Empty;
                txtPhone.Text = _existingLead.Phone ?? string.Empty;

                if (!string.IsNullOrWhiteSpace(_existingLead.Source))
                {
                    if (!cmbSource.Items.Contains(_existingLead.Source))
                    {
                        cmbSource.Items.Add(_existingLead.Source);
                    }
                    cmbSource.Text = _existingLead.Source;
                }
                else
                {
                    cmbSource.SelectedIndex = -1;
                    cmbSource.Text = string.Empty;
                }

                txtNotes.Text = _existingLead.Notes ?? string.Empty;
                txtExpectedValue.Text = _existingLead.ExpectedValue.HasValue
                    ? _existingLead.ExpectedValue.Value.ToString("F2")
                    : string.Empty;

                // Configure available stages: prevent regressing from contacted/qualified back to 'new'
                cmbStage.Items.Clear();
                string currentStage = (_existingLead.Stage ?? "new").ToLowerInvariant();

                if (currentStage == "converted")
                {
                    cmbStage.Items.Add("converted");
                    cmbStage.SelectedItem = "converted";
                    cmbStage.Enabled = false;
                }
                else if (currentStage == "new")
                {
                    cmbStage.Items.AddRange(new object[] { "new", "contacted", "qualified", "proposal", "negotiation", "lost" });
                    SelectComboValue(cmbStage, _existingLead.Stage, "new");
                }
                else
                {
                    // Lead has been contacted or progressed: 'new' is permanently locked out
                    cmbStage.Items.AddRange(new object[] { "contacted", "qualified", "proposal", "negotiation", "lost" });
                    SelectComboValue(cmbStage, _existingLead.Stage, "contacted");
                }

                SelectComboValue(cmbPriority, _existingLead.Priority, "medium");

                if (isPendingReview)
                {
                    txtFirstName.ReadOnly = true;
                    txtMiddleName.ReadOnly = true;
                    txtLastName.ReadOnly = true;
                    cmbSuffix.Enabled = false;
                    txtEmail.ReadOnly = true;
                    txtPhone.ReadOnly = true;
                    cmbSource.Enabled = false;
                    cmbStage.Enabled = false;
                    cmbPriority.Enabled = false;
                    txtExpectedValue.ReadOnly = true;
                    txtNotes.ReadOnly = true;
                }
            }
            else
            {
                cmbSuffix.SelectedIndex = -1;
                cmbSuffix.Text = string.Empty;
                cmbSource.SelectedIndex = -1;
                cmbSource.Text = string.Empty;

                cmbStage.Items.Clear();
                cmbStage.Items.Add("new");
                cmbStage.SelectedItem = "new";
                cmbStage.Enabled = false; // Initial stage is locked to "new" on creation
                cmbPriority.SelectedItem = "medium";
            }
        }

        private void PopulateSources()
        {
            try
            {
                using var campaignController = new CampaignController();
                var sources = campaignController.GetActiveCampaignSources();
                cmbSource.Items.Clear();
                foreach (var s in sources)
                {
                    cmbSource.Items.Add(s);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[LeadInputForm.PopulateSources] Error: {ex.Message}");
                cmbSource.Items.Clear();
                cmbSource.Items.AddRange(new object[]
                {
                    "Facebook Ad", "Referral", "Walk-in", "Website", "Property Portal", "Google Ads", "Billboard / Outdoor", "Open House / Event"
                });
            }
        }

        private void BtnSaveClick(object? sender, EventArgs e)
        {
            string firstName = txtFirstName.Text.Trim();
            string lastName = txtLastName.Text.Trim();

            if (!LeadController.ValidateLeadInput(
                firstName,
                lastName,
                txtEmail.Text,
                txtPhone.Text,
                txtExpectedValue.Text,
                out decimal? expectedValue,
                out string? errorMessage,
                out string? errorField))
            {
                Control target = errorField switch
                {
                    "FirstName" => txtFirstName,
                    "LastName" => txtLastName,
                    "Email" => txtEmail,
                    "Phone" => txtPhone,
                    "ExpectedValue" => txtExpectedValue,
                    _ => txtFirstName
                };
                ShowValidationError(errorMessage ?? "Validation error.", target);
                return;
            }

            if (_existingLead is not null)
            {
                _existingLead.FirstName = firstName;
                _existingLead.MiddleName = NullIfEmpty(txtMiddleName.Text);
                _existingLead.LastName = lastName;
                _existingLead.Suffix = NullIfEmpty(cmbSuffix.Text);
                _existingLead.Email = txtEmail.Text.Trim();
                _existingLead.Phone = NullIfEmpty(txtPhone.Text);
                _existingLead.Source = NullIfEmpty(cmbSource.Text);
                _existingLead.Stage = cmbStage.SelectedItem?.ToString() ?? "new";
                _existingLead.Priority = cmbPriority.SelectedItem?.ToString() ?? "medium";
                _existingLead.ExpectedValue = expectedValue;
                _existingLead.Notes = NullIfEmpty(txtNotes.Text);

                Result = _existingLead;
            }
            else
            {
                Result = new Lead
                {
                    FirstName = firstName,
                    MiddleName = NullIfEmpty(txtMiddleName.Text),
                    LastName = lastName,
                    Suffix = NullIfEmpty(cmbSuffix.Text),
                    Email = txtEmail.Text.Trim(),
                    Phone = NullIfEmpty(txtPhone.Text),
                    Source = NullIfEmpty(cmbSource.Text),
                    Stage = cmbStage.SelectedItem?.ToString() ?? "new",
                    Priority = cmbPriority.SelectedItem?.ToString() ?? "medium",
                    ExpectedValue = expectedValue,
                    Notes = NullIfEmpty(txtNotes.Text),
                    BranchId = CurrentSession.CanAccessBranching && CurrentSession.ActiveBranchId.HasValue ? CurrentSession.ActiveBranchId.Value : null,
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
