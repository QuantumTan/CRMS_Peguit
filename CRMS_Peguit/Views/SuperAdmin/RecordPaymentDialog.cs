using System;
using System.Drawing;
using System.Windows.Forms;
using CRMS_Peguit.domain.Common;
using CRMS_Peguit.domain.entities;
using CRMS_Peguit.winforms.Controllers;
using CRMS_Peguit.winforms.Controls;
using CRMS_Peguit.winforms.Models.Services;
using CRMS_Peguit.winforms.Services;

namespace CRMS_Peguit.winforms.Views.SuperAdmin
{
    public class RecordPaymentDialog : Form
    {
        private readonly SuperAdminSubscriptionController _controller = new();
        private readonly TenantSubscriptionDto _subscription;

        private readonly NumericUpDown _numAmount;
        private readonly ComboBox _cmbMethod;
        private readonly TextBox _txtReference;
        private readonly DateTimePicker _dtpDate;
        private readonly TextBox _txtNotes;
        private readonly Label _lblError;

        // Preview controls
        private readonly Label _lblPreviewBaseDate;
        private readonly Label _lblPreviewPeriod;
        private readonly Label _lblPreviewNewEndDate;
        private readonly Label _lblPreviewNewStatus;
        private readonly Button _btnSubmit;

        public PaymentRecordDto? RecordedPayment { get; private set; }

        public RecordPaymentDialog(TenantSubscriptionDto subscription)
        {
            _subscription = subscription ?? throw new ArgumentNullException(nameof(subscription));

            Text = $"Record Payment — {_subscription.CompanyName}";
            Size = new Size(540, 690);
            UiRadiusHelper.StyleModal(this, 8);

            var pnlContainer = new Panel
            {
                Dock = DockStyle.Fill,
                Padding = new Padding(24),
                BackColor = Color.White,
                AutoScroll = true
            };

            int y = 14;

            // 1. Header
            var lblTitle = new Label
            {
                Text = "Record Manual / Offline Payment",
                Font = new Font("Segoe UI", 13.5f, FontStyle.Bold),
                ForeColor = Theme.TextPrimary,
                AutoSize = true,
                Location = new Point(24, y)
            };
            pnlContainer.Controls.Add(lblTitle);
            y += 28;

            var lblSub = new Label
            {
                Text = $"Tenant: {_subscription.CompanyName} ({_subscription.CompanyCode}) · Plan: {_subscription.PlanName}",
                Font = new Font("Segoe UI", 9.5f),
                ForeColor = Theme.TextSecondary,
                AutoSize = true,
                Location = new Point(24, y)
            };
            pnlContainer.Controls.Add(lblSub);
            y += 34;

            // 2. Amount Paid
            var lblAmount = new Label
            {
                Text = "Amount Paid (₱): *",
                Font = new Font("Segoe UI", 9f, FontStyle.Bold),
                ForeColor = Theme.TextPrimary,
                Location = new Point(24, y),
                AutoSize = true
            };
            pnlContainer.Controls.Add(lblAmount);
            y += 22;

            _numAmount = new NumericUpDown
            {
                Location = new Point(24, y),
                Size = new Size(470, 32),
                DecimalPlaces = 2,
                ThousandsSeparator = true,
                Minimum = 1m,
                Maximum = 10000000m,
                Value = _subscription.BillingAmount > 0 ? _subscription.BillingAmount : 2500m
            };
            UiRadiusHelper.StyleStandardNumericUpDown(_numAmount);
            _numAmount.ValueChanged += (_, _) => UpdatePreview();
            pnlContainer.Controls.Add(_numAmount);
            y += 42;

            // 3. Payment Method
            var lblMethod = new Label
            {
                Text = "Payment Method: *",
                Font = new Font("Segoe UI", 9f, FontStyle.Bold),
                ForeColor = Theme.TextPrimary,
                Location = new Point(24, y),
                AutoSize = true
            };
            pnlContainer.Controls.Add(lblMethod);
            y += 22;

            _cmbMethod = new ComboBox
            {
                Location = new Point(24, y),
                Size = new Size(470, 32),
                DropDownStyle = ComboBoxStyle.DropDownList
            };
            UiRadiusHelper.StyleStandardComboBox(_cmbMethod);
            _cmbMethod.Items.Add("Bank Transfer");
            _cmbMethod.Items.Add("GCash");
            _cmbMethod.Items.Add("Check");
            _cmbMethod.Items.Add("Cash");
            _cmbMethod.Items.Add("Other");
            _cmbMethod.SelectedIndex = 0;
            pnlContainer.Controls.Add(_cmbMethod);
            y += 42;

            // 4. Payment Reference (Required proof of payment)
            var lblReference = new Label
            {
                Text = "Payment Reference Number: *",
                Font = new Font("Segoe UI", 9f, FontStyle.Bold),
                ForeColor = Theme.TextPrimary,
                Location = new Point(24, y),
                AutoSize = true
            };
            pnlContainer.Controls.Add(lblReference);
            y += 22;

            _txtReference = new TextBox
            {
                Location = new Point(24, y),
                Size = new Size(470, 32),
                PlaceholderText = "e.g. Bank transaction ID, GCash ref no., or check number"
            };
            UiRadiusHelper.StyleStandardInput(_txtReference);
            _txtReference.TextChanged += (_, _) =>
            {
                if (_lblError != null) _lblError.Visible = false;
            };
            pnlContainer.Controls.Add(_txtReference);
            y += 42;

            // 5. Payment Date
            var lblDate = new Label
            {
                Text = "Payment Date: *",
                Font = new Font("Segoe UI", 9f, FontStyle.Bold),
                ForeColor = Theme.TextPrimary,
                Location = new Point(24, y),
                AutoSize = true
            };
            pnlContainer.Controls.Add(lblDate);
            y += 22;

            _dtpDate = new DateTimePicker
            {
                Location = new Point(24, y),
                Size = new Size(470, 32),
                Font = new Font("Segoe UI", 10f),
                Format = DateTimePickerFormat.Short,
                Value = DateTime.Today
            };
            _dtpDate.ValueChanged += (_, _) => UpdatePreview();
            pnlContainer.Controls.Add(_dtpDate);
            y += 42;

            // 6. Notes (Optional)
            var lblNotes = new Label
            {
                Text = "Notes / Reconciliation Details (Optional):",
                Font = new Font("Segoe UI", 9f),
                ForeColor = Theme.TextSecondary,
                Location = new Point(24, y),
                AutoSize = true
            };
            pnlContainer.Controls.Add(lblNotes);
            y += 20;

            _txtNotes = new TextBox
            {
                Location = new Point(24, y),
                Size = new Size(470, 52),
                Multiline = true,
                PlaceholderText = "Notes regarding bank confirmation, check clearance, agency contact, etc."
            };
            UiRadiusHelper.StyleStandardInput(_txtNotes);
            pnlContainer.Controls.Add(_txtNotes);
            y += 62;

            // 7. Live Extension Preview Card
            var pnlPreview = new Panel
            {
                Location = new Point(24, y),
                Size = new Size(470, 116),
                BackColor = Color.FromArgb(248, 250, 252),
                Padding = new Padding(14)
            };
            pnlPreview.Paint += (s, e) =>
            {
                using var p = new Pen(Theme.Border, 1f);
                e.Graphics.DrawRectangle(p, 0, 0, pnlPreview.Width - 1, pnlPreview.Height - 1);
            };
            UiRadiusHelper.StyleCard(pnlPreview, 6);

            var lblPreviewTitle = new Label
            {
                Text = "⚡ Automatic Subscription Extension Preview",
                Font = new Font("Segoe UI", 9.5f, FontStyle.Bold),
                ForeColor = Color.FromArgb(15, 23, 42),
                Location = new Point(14, 10),
                AutoSize = true
            };
            pnlPreview.Controls.Add(lblPreviewTitle);

            _lblPreviewBaseDate = new Label
            {
                Text = "Current End Date: —",
                Font = new Font("Segoe UI", 8.5f),
                ForeColor = Color.FromArgb(71, 85, 105),
                Location = new Point(14, 36),
                AutoSize = true
            };
            pnlPreview.Controls.Add(_lblPreviewBaseDate);

            _lblPreviewPeriod = new Label
            {
                Text = "Extension: +1 Month",
                Font = new Font("Segoe UI", 8.5f),
                ForeColor = Color.FromArgb(71, 85, 105),
                Location = new Point(240, 36),
                AutoSize = true
            };
            pnlPreview.Controls.Add(_lblPreviewPeriod);

            _lblPreviewNewEndDate = new Label
            {
                Text = "New Paid-Through Date: —",
                Font = new Font("Segoe UI", 9.5f, FontStyle.Bold),
                ForeColor = Color.FromArgb(15, 23, 42),
                Location = new Point(14, 62),
                AutoSize = true
            };
            pnlPreview.Controls.Add(_lblPreviewNewEndDate);

            _lblPreviewNewStatus = new Label
            {
                Text = "New Status: Active",
                Font = new Font("Segoe UI", 9.5f, FontStyle.Bold),
                ForeColor = Theme.StatusSuccess,
                Location = new Point(14, 88),
                AutoSize = true
            };
            pnlPreview.Controls.Add(_lblPreviewNewStatus);

            pnlContainer.Controls.Add(pnlPreview);
            y += 126;

            // Validation Error Message
            _lblError = new Label
            {
                Text = "Please enter a payment reference number.",
                Font = new Font("Segoe UI", 9f, FontStyle.Bold),
                ForeColor = Theme.StatusAlert,
                Location = new Point(24, y),
                AutoSize = true,
                Visible = false
            };
            pnlContainer.Controls.Add(_lblError);
            y += 24;

            // 8. Action Buttons in Footer Panel
            var pnlFooter = new Panel
            {
                Dock = DockStyle.Bottom,
                Height = 60,
                BackColor = Color.FromArgb(248, 250, 252)
            };

            var btnCancel = new Button
            {
                Text = "Cancel",
                Size = new Size(110, 38),
                Location = new Point(244, 11),
                Anchor = AnchorStyles.Top | AnchorStyles.Right
            };
            UiRadiusHelper.StyleSecondaryButton(btnCancel, 6);
            btnCancel.Click += (_, _) => DialogResult = DialogResult.Cancel;
            pnlFooter.Controls.Add(btnCancel);

            _btnSubmit = new Button
            {
                Text = "✔ Record Payment",
                Size = new Size(160, 38),
                Location = new Point(364, 11),
                Anchor = AnchorStyles.Top | AnchorStyles.Right
            };
            UiRadiusHelper.StylePrimaryButton(_btnSubmit, 6);
            _btnSubmit.Click += BtnSubmit_Click;
            pnlFooter.Controls.Add(_btnSubmit);

            Controls.Add(pnlContainer);
            Controls.Add(pnlFooter);

            AcceptButton = _btnSubmit;
            CancelButton = btnCancel;

            UpdatePreview();
        }

        private void UpdatePreview()
        {
            var paymentDate = _dtpDate.Value.Date;
            var currentEnd = _subscription.EndDate;

            bool renewingBeforeExpiry = currentEnd.HasValue && currentEnd.Value.Date > paymentDate;
            _lblPreviewBaseDate.Text = currentEnd.HasValue
                ? $"Current Expiry: {currentEnd.Value:MMM dd, yyyy} ({(renewingBeforeExpiry ? "Renewing in advance" : "Renewing after lapse")})"
                : "Current Expiry: None (New Subscription)";

            bool isAnnual = !string.IsNullOrWhiteSpace(_subscription.PlanName) &&
                (_subscription.PlanName.Contains("Annual", StringComparison.OrdinalIgnoreCase) ||
                 _subscription.PlanName.Contains("Year", StringComparison.OrdinalIgnoreCase));

            _lblPreviewPeriod.Text = $"Extension Period: {(isAnnual ? "+1 Year" : "+1 Month")}";

            var (newEnd, newStatus) = Subscription.CalculateExtension(currentEnd, paymentDate, _subscription.PlanName);
            _lblPreviewNewEndDate.Text = $"New Paid-Through Date: {newEnd:MMM dd, yyyy}";

            _lblPreviewNewStatus.Text = $"Recalculated Status: {newStatus}";
            _lblPreviewNewStatus.ForeColor = StatusColorHelper.GetTextColor(newStatus);
        }

        private async void BtnSubmit_Click(object? sender, EventArgs e)
        {
            string reference = _txtReference.Text.Trim();
            if (string.IsNullOrWhiteSpace(reference))
            {
                _lblError.Text = "Payment Reference is required (transaction ID, GCash ref, or check number).";
                _lblError.Visible = true;
                _txtReference.Focus();
                return;
            }

            if (_numAmount.Value <= 0)
            {
                _lblError.Text = "Payment amount must be greater than zero.";
                _lblError.Visible = true;
                _numAmount.Focus();
                return;
            }

            _btnSubmit.Enabled = false;
            _btnSubmit.Text = "Recording...";

            PaymentMethod method = _cmbMethod.SelectedIndex switch
            {
                0 => PaymentMethod.BankTransfer,
                1 => PaymentMethod.GCash,
                2 => PaymentMethod.Check,
                3 => PaymentMethod.Cash,
                _ => PaymentMethod.Other
            };

            var req = new RecordPaymentRequest
            {
                SubscriptionId = _subscription.SubscriptionId,
                AmountPaid = _numAmount.Value,
                PaymentMethod = method,
                PaymentReference = reference,
                PaymentDate = _dtpDate.Value,
                Notes = string.IsNullOrWhiteSpace(_txtNotes.Text) ? null : _txtNotes.Text.Trim()
            };

            var (success, message, record) = await _controller.RecordPaymentAsync(req);

            _btnSubmit.Enabled = true;
            _btnSubmit.Text = "✔ Record Payment";

            if (success)
            {
                RecordedPayment = record;
                MessageBox.Show(
                    $"Payment of ₱{req.AmountPaid:N2} was successfully recorded for {_subscription.CompanyName}.\n\n" +
                    $"Reference: {reference}\n" +
                    $"Extended Through: {_subscription.EndDate:MMM dd, yyyy}\n\n" +
                    $"An entry was logged to the Platform Audit Log.",
                    "Payment Recorded",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information);

                DialogResult = DialogResult.OK;
                Close();
            }
            else
            {
                _lblError.Text = message;
                _lblError.Visible = true;
            }
        }
    }
}
