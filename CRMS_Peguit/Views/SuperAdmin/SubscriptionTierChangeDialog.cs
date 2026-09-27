using System;
using System.Drawing;
using System.Windows.Forms;
using CRMS_Peguit.domain.entities;
using CRMS_Peguit.winforms.Models.Services;
using CRMS_Peguit.winforms.Services;

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
            Size = new Size(520, 500);
            UiRadiusHelper.StyleModal(this, 8);

            var pnlContainer = new Panel
            {
                Dock = DockStyle.Fill,
                Padding = new Padding(24),
                BackColor = Color.White,
                AutoScroll = true
            };

            var lblHeader = new Label
            {
                Text = "Manage Tenant Subscription Plan",
                Font = new Font("Segoe UI", 13.5f, FontStyle.Bold),
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
                Location = new Point(24, 110),
                Width = 432
            };
            UiRadiusHelper.StyleStandardComboBox(_cmbTier);
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
                Location = new Point(24, 146),
                Size = new Size(432, 40)
            };
            pnlContainer.Controls.Add(_lblDescription);

            // Status Selector
            var lblStatus = new Label
            {
                Text = "Subscription Status:",
                Font = new Font("Segoe UI", 9.5f, FontStyle.Bold),
                ForeColor = Theme.TextPrimary,
                Location = new Point(24, 194),
                AutoSize = true
            };
            pnlContainer.Controls.Add(lblStatus);

            _cmbStatus = new ComboBox
            {
                DropDownStyle = ComboBoxStyle.DropDownList,
                Location = new Point(24, 218),
                Width = 432
            };
            UiRadiusHelper.StyleStandardComboBox(_cmbStatus);
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
                Location = new Point(24, 258),
                AutoSize = true
            };
            pnlContainer.Controls.Add(lblAmount);

            _numAmount = new NumericUpDown
            {
                Location = new Point(24, 282),
                Width = 432,
                DecimalPlaces = 2,
                Maximum = 1000000m,
                Value = currentAmount > 0 ? currentAmount : 2500m
            };
            UiRadiusHelper.StyleStandardNumericUpDown(_numAmount);
            pnlContainer.Controls.Add(_numAmount);

            _cmbTier.SelectedIndexChanged += (_, _) =>
            {
                _lblDescription.Text = GetTierDescription(_cmbTier.SelectedIndex);
                if (_cmbTier.SelectedIndex == 2) _numAmount.Value = 9500m;
                else if (_cmbTier.SelectedIndex == 1) _numAmount.Value = 5500m;
                else _numAmount.Value = 2500m;
            };

            // Footer panel
            var pnlFooter = new Panel
            {
                Dock = DockStyle.Bottom,
                Height = 60,
                BackColor = Color.FromArgb(248, 250, 252)
            };

            // Buttons
            var btnCancel = new Button
            {
                Text = "Cancel",
                DialogResult = DialogResult.Cancel,
                Location = new Point(242, 12),
                Size = new Size(100, 36),
                Anchor = AnchorStyles.Top | AnchorStyles.Right
            };
            UiRadiusHelper.StyleSecondaryButton(btnCancel, 6);
            pnlFooter.Controls.Add(btnCancel);

            var btnSave = new Button
            {
                Text = "Save Plan",
                DialogResult = DialogResult.OK,
                Location = new Point(350, 12),
                Size = new Size(106, 36),
                Anchor = AnchorStyles.Top | AnchorStyles.Right
            };
            UiRadiusHelper.StylePrimaryButton(btnSave, 6);
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
            pnlFooter.Controls.Add(btnSave);

            Controls.Add(pnlContainer);
            Controls.Add(pnlFooter);

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
