using CRMS_Peguit.domain.entities;
using CRMS_Peguit.winforms.Auth;
using CRMS_Peguit.winforms.Controllers;
using CRMS_Peguit.winforms.Models.Services;

namespace CRMS_Peguit.winforms.Views.Properties
{
    public partial class PropertyInputForm : Form
    {
        public Property? Result { get; private set; }

        private readonly Property? _existingProperty;
        private readonly List<CustomerPickerItem> _owners;
        private readonly List<AgentPickerItem> _agents;

        public PropertyInputForm() : this(new List<CustomerPickerItem>(), new List<AgentPickerItem>(), null)
        {
        }

        public PropertyInputForm(List<CustomerPickerItem> owners, List<AgentPickerItem> agents, Property? property = null)
        {
            _owners = owners ?? new List<CustomerPickerItem>();
            _agents = agents ?? new List<AgentPickerItem>();
            _existingProperty = property;

            InitializeComponent();
            UiRadiusHelper.StyleButton(btnSave, 8);
            UiRadiusHelper.StyleButton(btnCancel, 8);
            UiRadiusHelper.AttachHoverFeedback(btnCancel, Color.White, Color.FromArgb(241, 245, 249));
            UiRadiusHelper.AttachHoverFeedback(btnSave, Theme.Primary, Theme.PrimaryDark);
            btnSave.Click += BtnSaveClick;
            LoadPickers();
            LoadData();
        }

        private void LoadPickers()
        {
            cmbOwner.Items.Clear();
            cmbOwner.Items.Add("-- Select Owner --");
            foreach (var owner in _owners)
            {
                cmbOwner.Items.Add(owner);
            }
            cmbOwner.SelectedIndex = 0;

            cmbListedByAgent.Items.Clear();
            cmbListedByAgent.Items.Add("-- Unassigned Agent --");
            foreach (var agent in _agents)
            {
                cmbListedByAgent.Items.Add(agent);
            }
            cmbListedByAgent.SelectedIndex = 0;
        }

        private void LoadData()
        {
            Text = _existingProperty is null
                ? "Add New Property"
                : $"Edit Property #{_existingProperty.PropertyId}";

            if (_existingProperty is not null)
            {
                txtAddress.Text = _existingProperty.Address;
                txtPrice.Text = _existingProperty.Price.ToString("F2");

                SelectComboValue(cmbPropertyType, _existingProperty.PropertyType, "house");
                SelectComboValue(cmbStatus, _existingProperty.Status, "available");

                if (_existingProperty.OwnerCustomerId > 0)
                {
                    var match = _owners.FirstOrDefault(x => x.CustomerId == _existingProperty.OwnerCustomerId);
                    if (match != null) cmbOwner.SelectedItem = match;
                }

                if (_existingProperty.ListedByAgentId > 0)
                {
                    var match = _agents.FirstOrDefault(x => x.UserId == _existingProperty.ListedByAgentId);
                    if (match != null) cmbListedByAgent.SelectedItem = match;
                }
            }
            else
            {
                cmbPropertyType.SelectedItem = "house";
                cmbStatus.SelectedItem = "available";
                cmbStatus.Enabled = false; // New properties default to available
            }

            // R24. Only Manager or Admin may set or change ownership.
            if (!RbacService.CanAssignRecords)
            {
                cmbListedByAgent.Enabled = false;
            }
        }

        private void BtnSaveClick(object? sender, EventArgs e)
        {
            string address = txtAddress.Text.Trim();
            if (string.IsNullOrWhiteSpace(address))
            {
                ShowValidationError("Address is required.", txtAddress);
                return;
            }

            decimal price = 0;
            if (!string.IsNullOrWhiteSpace(txtPrice.Text))
            {
                if (decimal.TryParse(txtPrice.Text.Replace(",", "").Trim(), out decimal parsedPrice) && parsedPrice >= 0)
                {
                    price = parsedPrice;
                }
                else
                {
                    ShowValidationError("Enter a valid price amount.", txtPrice);
                    return;
                }
            }

            if (cmbOwner.SelectedItem is not CustomerPickerItem selectedOwner || selectedOwner.CustomerId <= 0)
            {
                ShowValidationError("Please select a valid property owner.", cmbOwner);
                return;
            }
            int ownerId = selectedOwner.CustomerId;

            int? agentId = null;
            if (RbacService.CanAssignRecords)
            {
                if (cmbListedByAgent.SelectedItem is AgentPickerItem selectedAgent && selectedAgent.UserId > 0)
                {
                    agentId = selectedAgent.UserId;
                }
            }
            else
            {
                // R24: Agent cannot set or reassign ownership. Keep existing assignment.
                agentId = _existingProperty?.ListedByAgentId;
            }

            string propertyType = cmbPropertyType.SelectedItem?.ToString() ?? "house";
            string status = cmbStatus.SelectedItem?.ToString() ?? "available";

            if (_existingProperty is not null)
            {
                _existingProperty.Address = address;
                _existingProperty.PropertyType = propertyType;
                _existingProperty.Status = status;
                _existingProperty.Price = price;
                _existingProperty.OwnerCustomerId = ownerId;
                if (RbacService.CanAssignRecords)
                {
                    _existingProperty.ListedByAgentId = agentId;
                }
                Result = _existingProperty;
            }
            else
            {
                Result = new Property
                {
                    Address = address,
                    PropertyType = propertyType,
                    Status = status,
                    Price = price,
                    OwnerCustomerId = ownerId,
                    ListedByAgentId = agentId,
                    CreatedAt = DateTime.UtcNow
                };
            }

            DialogResult = DialogResult.OK;
            Close();
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


