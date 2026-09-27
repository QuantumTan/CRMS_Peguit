using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using CRMS_Peguit.domain.entities;
using CRMS_Peguit.winforms.Controllers;
using CRMS_Peguit.winforms.Models.Services;

namespace CRMS_Peguit.winforms.Views.FollowUps
{
    public partial class FollowUpInputForm : Form
    {
        public TaskReminder? Result { get; private set; }

        private readonly FollowUpController _controller;
        private readonly TaskReminder? _existing;
        private List<Customer> _assignedCustomers = new();
        private List<Lead> _assignedLeads = new();

        public FollowUpInputForm(FollowUpController controller, TaskReminder? existing = null)
        {
            _controller = controller;
            _existing = existing;

            InitializeComponent();
            ApplyStyling();
            LoadData();
        }

        private void ApplyStyling()
        {
            UiRadiusHelper.StyleButton(btnSave, 8);
            UiRadiusHelper.StyleButton(btnCancel, 8);
            UiRadiusHelper.AttachHoverFeedback(btnCancel, Color.White, Color.FromArgb(241, 245, 249));
            UiRadiusHelper.AttachHoverFeedback(btnSave, Theme.Primary, Theme.PrimaryDark);

            btnCancel.Click += (_, _) => { DialogResult = DialogResult.Cancel; Close(); };
            btnSave.Click += BtnSaveClick;

            rbCustomer.CheckedChanged += (_, _) => PopulateClientDropdown();
            rbLead.CheckedChanged += (_, _) => PopulateClientDropdown();
        }

        private void LoadData()
        {
            // Load types
            cmbType.Items.Clear();
            cmbType.Items.AddRange(new object[] { "Call", "Email", "Meeting", "Text" });
            cmbType.SelectedIndex = 0;

            // Load priorities
            cmbPriority.Items.Clear();
            cmbPriority.Items.AddRange(new object[] { "Low", "Medium", "High" });
            cmbPriority.SelectedIndex = 1; // Medium

            // Load assigned clients
            _assignedCustomers = _controller.GetAssignedCustomers();
            _assignedLeads = _controller.GetAssignedLeads();

            bool isEdit = _existing != null && _existing.TaskReminderId > 0;
            if (isEdit)
            {
                lblHeaderTitle.Text = "Edit Follow-Up";
                Text = "Edit Follow-Up";
                btnSave.Text = "Save Changes";
            }
            else
            {
                lblHeaderTitle.Text = "Schedule Follow-Up";
                Text = "Schedule Follow-Up";
                btnSave.Text = "Save Follow-Up";
            }

            if (_existing != null)
            {
                txtTitle.Text = _existing.Title;

                if (!string.IsNullOrWhiteSpace(_existing.Type) && cmbType.Items.Contains(_existing.Type))
                    cmbType.SelectedItem = _existing.Type;

                if (!string.IsNullOrWhiteSpace(_existing.Priority) && cmbPriority.Items.Contains(_existing.Priority))
                    cmbPriority.SelectedItem = _existing.Priority;

                var localDue = _existing.DueDate.ToLocalTime();
                dtpDueDate.Value = localDue.Date;
                dtpDueTime.Value = localDue;

                txtNotes.Text = _existing.Notes ?? string.Empty;

                if (_existing.RelatedCustomerId.HasValue)
                {
                    rbCustomer.Checked = true;
                    PopulateClientDropdown();
                    SelectClientById(_existing.RelatedCustomerId.Value);
                }
                else if (_existing.RelatedLeadId.HasValue)
                {
                    rbLead.Checked = true;
                    PopulateClientDropdown();
                    SelectClientById(_existing.RelatedLeadId.Value);
                }
                else
                {
                    rbCustomer.Checked = true;
                    PopulateClientDropdown();
                }
            }
            else
            {
                lblHeaderTitle.Text = "Schedule Follow-Up";
                Text = "Schedule Follow-Up";
                btnSave.Text = "Save Follow-Up";

                // Default date to today, time rounded to next hour
                var now = DateTime.Now;
                var nextHour = new DateTime(now.Year, now.Month, now.Day, now.Hour, 0, 0).AddHours(1);
                dtpDueDate.Value = nextHour.Date;
                dtpDueTime.Value = nextHour;

                rbCustomer.Checked = true;
                PopulateClientDropdown();
            }
        }

        private void PopulateClientDropdown()
        {
            cmbClient.Items.Clear();

            if (rbCustomer.Checked)
            {
                if (_assignedCustomers.Count == 0)
                {
                    cmbClient.Items.Add(new ClientComboItem(0, "No customers currently assigned to you", false));
                }
                else
                {
                    foreach (var c in _assignedCustomers)
                    {
                        string emailPart = !string.IsNullOrWhiteSpace(c.Email) ? $" ({c.Email})" : string.Empty;
                        cmbClient.Items.Add(new ClientComboItem(c.CustomerId, $"{c.FullName}{emailPart}", true));
                    }
                }
            }
            else
            {
                if (_assignedLeads.Count == 0)
                {
                    cmbClient.Items.Add(new ClientComboItem(0, "No leads currently assigned to you", false));
                }
                else
                {
                    foreach (var l in _assignedLeads)
                    {
                        string emailPart = !string.IsNullOrWhiteSpace(l.Email) ? $" ({l.Email})" : string.Empty;
                        string stagePart = !string.IsNullOrWhiteSpace(l.Stage) ? $" [{l.Stage}]" : string.Empty;
                        cmbClient.Items.Add(new ClientComboItem(l.LeadId, $"{l.FullName}{emailPart}{stagePart}", true));
                    }
                }
            }

            if (cmbClient.Items.Count > 0)
                cmbClient.SelectedIndex = 0;
        }

        private void SelectClientById(int id)
        {
            for (int i = 0; i < cmbClient.Items.Count; i++)
            {
                if (cmbClient.Items[i] is ClientComboItem item && item.Id == id)
                {
                    cmbClient.SelectedIndex = i;
                    break;
                }
            }
        }

        private void BtnSaveClick(object? sender, EventArgs e)
        {
            string title = txtTitle.Text.Trim();
            if (string.IsNullOrWhiteSpace(title))
            {
                MessageBox.Show("Please enter a follow-up title.", "Validation Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                txtTitle.Focus();
                return;
            }

            if (cmbClient.SelectedItem is not ClientComboItem selectedClient || !selectedClient.IsValid)
            {
                string targetType = rbCustomer.Checked ? "customer" : "lead";
                MessageBox.Show($"Please select a valid assigned {targetType}.", "Validation Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                cmbClient.Focus();
                return;
            }

            DateTime selectedDate = dtpDueDate.Value.Date;
            TimeSpan selectedTime = dtpDueTime.Value.TimeOfDay;
            DateTime localDue = selectedDate.Add(selectedTime);
            DateTime utcDue = localDue.ToUniversalTime();

            string channelType = cmbType.SelectedItem?.ToString() ?? "Call";
            string priority = cmbPriority.SelectedItem?.ToString() ?? "Medium";
            string? notes = string.IsNullOrWhiteSpace(txtNotes.Text) ? null : txtNotes.Text.Trim();

            int? customerId = rbCustomer.Checked ? selectedClient.Id : null;
            int? leadId = rbLead.Checked ? selectedClient.Id : null;

            if (_existing != null)
            {
                _existing.Title = title;
                _existing.Type = channelType;
                _existing.Priority = priority;
                _existing.DueDate = utcDue;
                _existing.Notes = notes;
                _existing.RelatedCustomerId = customerId;
                _existing.RelatedLeadId = leadId;
                Result = _existing;
            }
            else
            {
                Result = new TaskReminder
                {
                    Title = title,
                    Type = channelType,
                    Priority = priority,
                    DueDate = utcDue,
                    Notes = notes,
                    RelatedCustomerId = customerId,
                    RelatedLeadId = leadId
                };
            }

            DialogResult = DialogResult.OK;
            Close();
        }

        private sealed record ClientComboItem(int Id, string DisplayText, bool IsValid)
        {
            public override string ToString() => DisplayText;
        }
    }
}
