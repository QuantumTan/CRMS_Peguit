using System;
using System.Drawing;
using System.Windows.Forms;
using CRMS_Peguit.domain.entities;
using CRMS_Peguit.winforms.Models.Services;

namespace CRMS_Peguit.winforms.Views.Branching
{
    public class BranchEditDialog : Form
    {
        public Branch BranchResult { get; }

        private readonly TextBox _txtCode;
        private readonly TextBox _txtName;
        private readonly TextBox _txtAddress;
        private readonly TextBox _txtPhone;
        private readonly CheckBox _chkActive;
        private readonly Label _lblError;

        public BranchEditDialog(Branch? existing = null)
        {
            BranchResult = existing ?? new Branch();

            Text = existing == null || existing.BranchId == 0 ? "Add New Branch (Tenant C)" : $"Edit Branch — {existing.BranchCode}";
            Size = new Size(480, 430);
            StartPosition = FormStartPosition.CenterParent;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;
            BackColor = Theme.Surface;

            var pnl = new Panel { Dock = DockStyle.Fill, Padding = new Padding(24) };
            Controls.Add(pnl);

            var lblHeader = new Label
            {
                Text = existing == null || existing.BranchId == 0 ? "🏢  Create Multi-Branch Office" : "🏢  Update Branch Office Details",
                Font = new Font("Segoe UI", 12f, FontStyle.Bold),
                ForeColor = Theme.TextPrimary,
                Location = new Point(24, 18),
                AutoSize = true
            };
            pnl.Controls.Add(lblHeader);

            var lblSub = new Label
            {
                Text = "Branch offices isolate team members, listings, leads, and transaction records.",
                Font = new Font("Segoe UI", 9f),
                ForeColor = Theme.TextSecondary,
                Location = new Point(24, 44),
                AutoSize = true
            };
            pnl.Controls.Add(lblSub);

            // Branch Code
            var lblCode = new Label { Text = "Branch Code (e.g. HQ-MNL):", Location = new Point(24, 80), AutoSize = true, Font = new Font("Segoe UI", 9f, FontStyle.Bold), ForeColor = Theme.TextPrimary };
            _txtCode = new TextBox { Text = BranchResult.BranchCode, Location = new Point(24, 102), Width = 415, Font = new Font("Segoe UI", 10f) };
            UiRadiusHelper.SetPadding(_txtCode, 6, 6);
            pnl.Controls.Add(lblCode);
            pnl.Controls.Add(_txtCode);

            // Branch Name
            var lblName = new Label { Text = "Branch Name / Location:", Location = new Point(24, 140), AutoSize = true, Font = new Font("Segoe UI", 9f, FontStyle.Bold), ForeColor = Theme.TextPrimary };
            _txtName = new TextBox { Text = BranchResult.BranchName, Location = new Point(24, 162), Width = 415, Font = new Font("Segoe UI", 10f) };
            UiRadiusHelper.SetPadding(_txtName, 6, 6);
            pnl.Controls.Add(lblName);
            pnl.Controls.Add(_txtName);

            // Address
            var lblAddr = new Label { Text = "Physical Office Address:", Location = new Point(24, 200), AutoSize = true, Font = new Font("Segoe UI", 9f, FontStyle.Bold), ForeColor = Theme.TextPrimary };
            _txtAddress = new TextBox { Text = BranchResult.Address, Location = new Point(24, 222), Width = 415, Font = new Font("Segoe UI", 10f) };
            UiRadiusHelper.SetPadding(_txtAddress, 6, 6);
            pnl.Controls.Add(lblAddr);
            pnl.Controls.Add(_txtAddress);

            // Phone
            var lblPhone = new Label { Text = "Contact Phone / Trunkline:", Location = new Point(24, 260), AutoSize = true, Font = new Font("Segoe UI", 9f, FontStyle.Bold), ForeColor = Theme.TextPrimary };
            _txtPhone = new TextBox { Text = BranchResult.Phone, Location = new Point(24, 282), Width = 260, Font = new Font("Segoe UI", 10f) };
            UiRadiusHelper.SetPadding(_txtPhone, 6, 6);
            pnl.Controls.Add(lblPhone);
            pnl.Controls.Add(_txtPhone);

            _chkActive = new CheckBox
            {
                Text = "Branch is Active",
                Checked = BranchResult.IsActive,
                Location = new Point(300, 284),
                AutoSize = true,
                Font = new Font("Segoe UI", 9.5f),
                ForeColor = Theme.TextPrimary
            };
            pnl.Controls.Add(_chkActive);

            _lblError = new Label
            {
                Text = "",
                ForeColor = Theme.Danger,
                Font = new Font("Segoe UI", 9f, FontStyle.Bold),
                Location = new Point(24, 320),
                AutoSize = true
            };
            pnl.Controls.Add(_lblError);

            // Buttons
            var btnCancel = new Button
            {
                Text = "Cancel",
                DialogResult = DialogResult.Cancel,
                Location = new Point(230, 345),
                Size = new Size(100, 35),
                Font = new Font("Segoe UI", 9.5f),
                Cursor = Cursors.Hand
            };
            UiRadiusHelper.StyleButton(btnCancel, 6);
            pnl.Controls.Add(btnCancel);

            var btnSave = new Button
            {
                Text = "Save Branch",
                Location = new Point(339, 345),
                Size = new Size(100, 35),
                Font = new Font("Segoe UI", 9.5f, FontStyle.Bold),
                BackColor = Theme.Primary,
                ForeColor = Color.White,
                Cursor = Cursors.Hand
            };
            UiRadiusHelper.StyleButton(btnSave, 6);
            btnSave.Click += (_, _) =>
            {
                if (string.IsNullOrWhiteSpace(_txtCode.Text))
                {
                    _lblError.Text = "Branch Code is required.";
                    _txtCode.Focus();
                    return;
                }
                if (string.IsNullOrWhiteSpace(_txtName.Text))
                {
                    _lblError.Text = "Branch Name is required.";
                    _txtName.Focus();
                    return;
                }

                BranchResult.BranchCode = _txtCode.Text.Trim();
                BranchResult.BranchName = _txtName.Text.Trim();
                BranchResult.Address = _txtAddress.Text.Trim();
                BranchResult.Phone = _txtPhone.Text.Trim();
                BranchResult.IsActive = _chkActive.Checked;

                DialogResult = DialogResult.OK;
                Close();
            };
            pnl.Controls.Add(btnSave);

            AcceptButton = btnSave;
            CancelButton = btnCancel;
        }
    }
}
