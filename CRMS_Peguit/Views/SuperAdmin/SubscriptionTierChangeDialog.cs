using System;
using System.Drawing;
using System.Windows.Forms;
using CRMS_Peguit.domain.entities;
using CRMS_Peguit.winforms.Models.Services;

namespace CRMS_Peguit.winforms.Views.SuperAdmin
{
    public class SubscriptionTierChangeDialog : Form
    {
        public string SelectedPlan { get; private set; } = Subscription.TierTenantA;
        public string SelectedStatus { get; private set; } = "Active";
        public decimal BillingAmount { get; private set; } = 2500m;

        private readonly ComboBox _cmbTier;
        private readonly ComboBox _cmbStatus;
        private readonly NumericUpDown _numAmount;
        private readonly Label _lblDescription;

        public SubscriptionTierChangeDialog(string currentCompany, string currentPlan, string currentStatus, decimal currentAmount)
        {
            Text = $"Change Subscription — {currentCompany}";
            Size = new Size(480, 420);
            StartPosition = FormStartPosition.CenterParent;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;
            BackColor = Theme.Surface;

            var pnlContainer = new Panel
            {
                Dock = DockStyle.Fill,
                Padding = new Padding(24)
            };
            Controls.Add(pnlContainer);

            var lblHeader = new Label
            {
                Text = "Manage Tenant Subscription Plan",
                Font = new Font("Segoe UI", 13f, FontStyle.Bold),
                ForeColor = Theme.TextPrimary,
                AutoSize = true,
                Location = new Point(24, 20)
            };
            pnlContainer.Controls.Add(lblHeader);

            var lblSub = new Label
            {
                Text = $"Company: {currentCompany}",
                Font = new Font("Segoe UI", 9.5f, FontStyle.Regular),
                ForeColor = Theme.TextSecondary,
                AutoSize = true,
                Location = new Point(24, 48)
            };
            pnlContainer.Controls.Add(lblSub);

            // Tier Selector
            var lblTier = new Label
            {
                Text = "Subscription Tier Plan:",
                Font = new Font("Segoe UI", 9.5f, FontStyle.Bold),
                ForeColor = Theme.TextPrimary,
                Location = new Point(24, 85),
                AutoSize = true
            };
            pnlContainer.Controls.Add(lblTier);

            _cmbTier = new ComboBox
            {
                DropDownStyle = ComboBoxStyle.DropDownList,
                Font = new Font("Segoe UI", 10f),
                Location = new Point(24, 110),
                Width = 415
            };
            _cmbTier.Items.Add(Subscription.TierTenantA + " (Main Transaction + Data Collection)");
            _cmbTier.Items.Add(Subscription.TierTenantB + " (BI Analytics + Automated Actions)");
            _cmbTier.Items.Add(Subscription.TierTenantC + " (Branching + BI + Actions)");

            if (currentPlan.Contains("Tenant C", StringComparison.OrdinalIgnoreCase)) _cmbTier.SelectedIndex = 2;
            else if (currentPlan.Contains("Tenant B", StringComparison.OrdinalIgnoreCase)) _cmbTier.SelectedIndex = 1;
            else _cmbTier.SelectedIndex = 0;

            pnlContainer.Controls.Add(_cmbTier);

            _lblDescription = new Label
            {
                Text = GetTierDescription(_cmbTier.SelectedIndex),
                Font = new Font("Segoe UI", 9f, FontStyle.Italic),
                ForeColor = Theme.Primary,
                Location = new Point(24, 145),
                Size = new Size(415, 38)
            };
            pnlContainer.Controls.Add(_lblDescription);

            // Status Selector
            var lblStatus = new Label
            {
                Text = "Subscription Status:",
                Font = new Font("Segoe UI", 9.5f, FontStyle.Bold),
                ForeColor = Theme.TextPrimary,
                Location = new Point(24, 190),
                AutoSize = true
            };
            pnlContainer.Controls.Add(lblStatus);

            _cmbStatus = new ComboBox
            {
                DropDownStyle = ComboBoxStyle.DropDownList,
                Font = new Font("Segoe UI", 10f),
                Location = new Point(24, 215),
                Width = 415
            };
            _cmbStatus.Items.AddRange(new object[] { "Active", "Expiring", "Expired", "Cancelled" });
            _cmbStatus.SelectedItem = currentStatus;
            if (_cmbStatus.SelectedIndex < 0) _cmbStatus.SelectedIndex = 0;
            pnlContainer.Controls.Add(_cmbStatus);

            // Billing Amount
            var lblAmount = new Label
            {
                Text = "Monthly Billing Amount (₱):",
                Font = new Font("Segoe UI", 9.5f, FontStyle.Bold),
                ForeColor = Theme.TextPrimary,
                Location = new Point(24, 255),
                AutoSize = true
            };
            pnlContainer.Controls.Add(lblAmount);

            _numAmount = new NumericUpDown
            {
                Font = new Font("Segoe UI", 10f),
                Location = new Point(24, 280),
                Width = 415,
                DecimalPlaces = 2,
                Maximum = 1000000m,
                Value = currentAmount > 0 ? currentAmount : 2500m
            };
            pnlContainer.Controls.Add(_numAmount);

            _cmbTier.SelectedIndexChanged += (_, _) =>
            {
                _lblDescription.Text = GetTierDescription(_cmbTier.SelectedIndex);
                if (_cmbTier.SelectedIndex == 2) _numAmount.Value = 9500m;
                else if (_cmbTier.SelectedIndex == 1) _numAmount.Value = 5500m;
                else _numAmount.Value = 2500m;
            };

            // Buttons
            var btnCancel = new Button
            {
                Text = "Cancel",
                DialogResult = DialogResult.Cancel,
                Location = new Point(230, 325),
                Size = new Size(100, 36),
                Font = new Font("Segoe UI", 9.5f),
                Cursor = Cursors.Hand
            };
            UiRadiusHelper.StyleButton(btnCancel, 6);
            pnlContainer.Controls.Add(btnCancel);

            var btnSave = new Button
            {
                Text = "Save Plan",
                DialogResult = DialogResult.OK,
                Location = new Point(339, 325),
                Size = new Size(100, 36),
                Font = new Font("Segoe UI", 9.5f, FontStyle.Bold),
                BackColor = Theme.Primary,
                ForeColor = Color.White,
                Cursor = Cursors.Hand
            };
            UiRadiusHelper.StyleButton(btnSave, 6);
            btnSave.Click += (_, _) =>
            {
                SelectedPlan = _cmbTier.SelectedIndex switch
                {
                    2 => Subscription.TierTenantC,
                    1 => Subscription.TierTenantB,
                    _ => Subscription.TierTenantA
                };
                SelectedStatus = _cmbStatus.SelectedItem?.ToString() ?? "Active";
                BillingAmount = _numAmount.Value;
            };
            pnlContainer.Controls.Add(btnSave);

            AcceptButton = btnSave;
            CancelButton = btnCancel;
        }

        private static string GetTierDescription(int index) => index switch
        {
            2 => "⭐ Tenant C: Full enterprise access with Multi-Branching, Business Intelligence Analytics, and Automated Actions.",
            1 => "💼 Tenant B: Professional tier with Business Intelligence dashboards, visual charts, and automated workflow Actions.",
            _ => "📁 Tenant A: Base transactional tier with core Deals management and Data Collection (Leads, Customers, Properties)."
        };
    }
}
