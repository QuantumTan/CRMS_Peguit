using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using CRMS_Peguit.domain.entities;
using CRMS_Peguit.winforms.Controllers;
using CRMS_Peguit.winforms.Models.Services;

namespace CRMS_Peguit.winforms.Views.SupportTickets
{
    public partial class SupportTicketInputForm : Form
    {
        public SupportTicket? Result { get; private set; }
        private readonly SupportTicketController _controller;
        private List<Customer> _customers = new();

        public SupportTicketInputForm(SupportTicketController controller)
        {
            _controller = controller;
            InitializeComponent();
            ApplyStyling();
            LoadDropdowns();
        }

        private void ApplyStyling()
        {
            UiRadiusHelper.StyleButton(btnSave, 8);
            UiRadiusHelper.StyleButton(btnCancel, 8);
            UiRadiusHelper.AttachHoverFeedback(btnCancel, Color.White, Color.FromArgb(241, 245, 249));
            UiRadiusHelper.AttachHoverFeedback(btnSave, Theme.Primary, Theme.PrimaryDark);

            UiRadiusHelper.SetPadding(txtDescription, 10, 10);
            btnSave.Click += BtnSaveClick;
            btnCancel.Click += (_, _) => { DialogResult = DialogResult.Cancel; Close(); };
        }

        private void LoadDropdowns()
        {
            _customers = _controller.GetCustomers();
            cmbCustomer.Items.Clear();
            foreach (var c in _customers)
            {
                cmbCustomer.Items.Add(new CustomerComboItem(c.CustomerId, c.FullName, c.Email));
            }
            if (cmbCustomer.Items.Count > 0)
                cmbCustomer.SelectedIndex = 0;

            cmbCategory.Items.Clear();
            cmbCategory.Items.AddRange(new object[]
            {
                "Maintenance",
                "Billing",
                "Document Request",
                "Complaint",
                "Other"
            });
            cmbCategory.SelectedIndex = 0;

            cmbPriority.Items.Clear();
            cmbPriority.Items.AddRange(new object[]
            {
                "High",
                "Medium",
                "Low"
            });
            cmbPriority.SelectedIndex = 1; // Medium default
        }

        private void BtnSaveClick(object? sender, EventArgs e)
        {
            if (cmbCustomer.SelectedItem is not CustomerComboItem selectedCust)
            {
                MessageBox.Show("Please select a customer for this support ticket.", "Validation Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                cmbCustomer.Focus();
                return;
            }

            string desc = txtDescription.Text.Trim();
            if (string.IsNullOrWhiteSpace(desc))
            {
                MessageBox.Show("Please provide a description of the issue.", "Validation Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                txtDescription.Focus();
                return;
            }

            string category = cmbCategory.SelectedItem?.ToString() ?? "Other";
            string priority = cmbPriority.SelectedItem?.ToString() ?? "Medium";

            Result = new SupportTicket
            {
                CustomerId = selectedCust.CustomerId,
                Category = category,
                Priority = priority,
                Description = desc
            };

            DialogResult = DialogResult.OK;
            Close();
        }

        private class CustomerComboItem
        {
            public int CustomerId { get; }
            public string FullName { get; }
            public string? Email { get; }

            public CustomerComboItem(int id, string name, string? email)
            {
                CustomerId = id;
                FullName = name;
                Email = email;
            }

            public override string ToString()
            {
                return string.IsNullOrWhiteSpace(Email)
                    ? FullName
                    : $"{FullName} ({Email})";
            }
        }
    }
}
