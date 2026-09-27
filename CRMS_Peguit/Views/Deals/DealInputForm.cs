using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Linq;
using System.Windows.Forms;
using CRMS_Peguit.domain.entities;
using CRMS_Peguit.winforms.Auth;
using CRMS_Peguit.winforms.Controllers;
using CRMS_Peguit.winforms.Models.Services;

namespace CRMS_Peguit.winforms.Views.Deals
{
    public partial class DealInputForm : Form
    {
        private readonly DealController _controller;
        private readonly Deal? _existingDeal;

        public Deal? Result { get; private set; }

        // Controls
        private ComboBox _cboCustomer = null!;
        private ComboBox _cboProperty = null!;
        private ComboBox _cboAgent = null!;
        private TextBox _txtValue = null!;
        private NumericUpDown _numCommission = null!;
        private ComboBox _cboStage = null!;
        private DateTimePicker _dtpCloseDate = null!;

        // Terms controls
        private ComboBox _cboPaymentScheme = null!;
        private TextBox _txtReservationFee = null!;
        private NumericUpDown _numDownPercent = null!;
        private Label _lblDownAmount = null!;
        private Label _lblBalanceAmount = null!;
        private ComboBox _cboCgt = null!;
        private ComboBox _cboDst = null!;
        private CheckedListBox _chkClauses = null!;
        private TextBox _txtSpecialStipulations = null!;

        public DealInputForm(DealController controller, Deal? deal = null)
        {
            _controller = controller;
            _existingDeal = deal;
            InitializeComponent();
            SetupForm();
            BuildUi();
            LoadData();
        }

        private void SetupForm()
        {
            Text = _existingDeal is null ? "Create Deal & Terms" : $"Edit Deal #{_existingDeal.DealId} & Terms";
            Size = new Size(760, 720);
            MinimumSize = new Size(640, 560);
            StartPosition = FormStartPosition.CenterParent;
            BackColor = Color.FromArgb(244, 247, 251);
            DoubleBuffered = true;
        }

        private void BuildUi()
        {
            Controls.Clear();

            // 1. Bottom Footer Bar
            var pnlFooter = new Panel
            {
                Dock = DockStyle.Bottom,
                Height = 62,
                BackColor = Color.White,
                Padding = new Padding(24, 12, 24, 12)
            };
            pnlFooter.Paint += (s, e) =>
            {
                using var pen = new Pen(UiDetailCardHelper.BorderColor, 1f);
                e.Graphics.DrawLine(pen, 0, 0, pnlFooter.Width, 0);
            };

            var btnCancel = new Button
            {
                Text = "Cancel",
                Size = new Size(90, 36),
                BackColor = Color.FromArgb(241, 245, 249),
                ForeColor = Color.FromArgb(30, 41, 59),
                Font = new Font("Segoe UI", 9.5f, FontStyle.Bold),
                DialogResult = DialogResult.Cancel
            };
            UiRadiusHelper.StyleButton(btnCancel, 8);
            UiRadiusHelper.AttachHoverFeedback(btnCancel, Color.FromArgb(241, 245, 249), Color.FromArgb(226, 232, 240));

            var btnSave = new Button
            {
                Text = "💾 Save Deal & Terms",
                Size = new Size(160, 36),
                BackColor = Theme.Primary,
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 9.5f, FontStyle.Bold)
            };
            UiRadiusHelper.StyleButton(btnSave, 8);
            UiRadiusHelper.AttachHoverFeedback(btnSave, Theme.Primary, Theme.PrimaryDark);
            btnSave.Click += BtnSaveClick;

            pnlFooter.Controls.Add(btnCancel);
            pnlFooter.Controls.Add(btnSave);

            pnlFooter.Resize += (_, _) =>
            {
                btnCancel.Location = new Point(pnlFooter.Width - btnCancel.Width - 24, 13);
                btnSave.Location = new Point(btnCancel.Left - btnSave.Width - 10, 13);
            };

            Controls.Add(pnlFooter);
            AcceptButton = btnSave;
            CancelButton = btnCancel;

            // 2. Scrollable Content Panel
            var pnlContent = new Panel
            {
                Dock = DockStyle.Fill,
                AutoScroll = true,
                BackColor = Color.FromArgb(244, 247, 251),
                Padding = new Padding(24, 16, 24, 16)
            };

            int y = 14;

            // Card 1: Core Transaction Info
            var cardCore = CreateSectionCard("💼  Transaction & Subject Property", ref y, 220);
            AddLabel(cardCore, "Buyer / Customer", 16, 44);
            _cboCustomer = AddComboBox(cardCore, 16, 66, 320);

            AddLabel(cardCore, "Subject Property", 350, 44);
            _cboProperty = AddComboBox(cardCore, 350, 66, 320);

            AddLabel(cardCore, "Assigned Sales Agent", 16, 100);
            _cboAgent = AddComboBox(cardCore, 16, 122, 320);

            AddLabel(cardCore, "Deal Stage", 350, 100);
            _cboStage = AddComboBox(cardCore, 350, 122, 320);
            _cboStage.Items.AddRange(new object[] { "Offer", "Reservation", "Contract", "Closed", "Lost" });
            _cboStage.SelectedIndex = 1; // Default to Reservation

            AddLabel(cardCore, "Total Agreed Price (₱)", 16, 156);
            _txtValue = AddTextBox(cardCore, 16, 178, 200, "15,000,000");
            _txtValue.TextChanged += (_, _) => RecalculateFinancing();

            AddLabel(cardCore, "Commission Rate (%)", 230, 156);
            _numCommission = new NumericUpDown
            {
                Location = new Point(230, 178),
                Width = 106,
                Height = 28,
                Font = new Font("Segoe UI", 10f),
                DecimalPlaces = 1,
                Minimum = 0,
                Maximum = 20,
                Value = 3.0m
            };
            cardCore.Controls.Add(_numCommission);

            AddLabel(cardCore, "Expected Closing Date", 350, 156);
            _dtpCloseDate = new DateTimePicker
            {
                Location = new Point(350, 178),
                Width = 320,
                Font = new Font("Segoe UI", 10f),
                Format = DateTimePickerFormat.Short,
                Value = DateTime.Now.AddDays(45)
            };
            cardCore.Controls.Add(_dtpCloseDate);

            pnlContent.Controls.Add(cardCore);

            // Card 2: Commercial Terms & Payment Scheme
            var cardTerms = CreateSectionCard("💳  Commercial Terms & Financing Scheme", ref y, 170);
            AddLabel(cardTerms, "Payment Scheme", 16, 44);
            _cboPaymentScheme = AddComboBox(cardTerms, 16, 66, 200);
            _cboPaymentScheme.Items.AddRange(new object[] { "Bank Financing", "Spot Cash", "Deferred In-House" });
            _cboPaymentScheme.SelectedIndex = 0;
            _cboPaymentScheme.SelectedIndexChanged += (_, _) => RecalculateFinancing();

            AddLabel(cardTerms, "Reservation Deposit (₱)", 230, 44);
            _txtReservationFee = AddTextBox(cardTerms, 230, 66, 180, "50,000");

            AddLabel(cardTerms, "Downpayment %", 430, 44);
            _numDownPercent = new NumericUpDown
            {
                Location = new Point(430, 66),
                Width = 100,
                Height = 28,
                Font = new Font("Segoe UI", 10f),
                DecimalPlaces = 1,
                Minimum = 0,
                Maximum = 100,
                Value = 20m
            };
            _numDownPercent.ValueChanged += (_, _) => RecalculateFinancing();
            cardTerms.Controls.Add(_numDownPercent);

            // Calculation preview panel
            var pnlCalc = new Panel
            {
                Location = new Point(16, 106),
                Size = new Size(654, 46),
                BackColor = Color.FromArgb(240, 253, 244)
            };
            pnlCalc.Paint += (s, e) =>
            {
                using var p = new Pen(Color.FromArgb(187, 247, 208), 1f);
                using var path = UiRadiusHelper.CreateRoundedPath(new Rectangle(0, 0, pnlCalc.Width - 1, pnlCalc.Height - 1), 6);
                e.Graphics.DrawPath(p, path);
            };
            UiRadiusHelper.ApplyRoundedCorners(pnlCalc, 6);

            _lblDownAmount = new Label
            {
                Text = "Downpayment: ₱3,000,000.00",
                Font = new Font("Segoe UI", 9.5f, FontStyle.Bold),
                ForeColor = Color.FromArgb(21, 128, 61),
                Location = new Point(14, 14),
                AutoSize = true
            };
            _lblBalanceAmount = new Label
            {
                Text = "Balance to Finance: ₱12,000,000.00",
                Font = new Font("Segoe UI", 9.5f, FontStyle.Bold),
                ForeColor = Color.FromArgb(21, 128, 61),
                Location = new Point(330, 14),
                AutoSize = true
            };
            pnlCalc.Controls.Add(_lblDownAmount);
            pnlCalc.Controls.Add(_lblBalanceAmount);
            cardTerms.Controls.Add(pnlCalc);

            pnlContent.Controls.Add(cardTerms);

            // Card 3: Statutory Tax & Closing Fee Split
            var cardTaxes = CreateSectionCard("⚖️  Statutory Tax & Closing Fee Apportionment", ref y, 100);
            AddLabel(cardTaxes, "Capital Gains Tax (6%)", 16, 44);
            _cboCgt = AddComboBox(cardTaxes, 16, 66, 180);
            _cboCgt.Items.AddRange(new object[] { "Seller", "Buyer", "50/50 Shared" });
            _cboCgt.SelectedIndex = 0; // Seller

            AddLabel(cardTaxes, "Doc Stamps & Transfer Tax", 230, 44);
            _cboDst = AddComboBox(cardTaxes, 230, 66, 180);
            _cboDst.Items.AddRange(new object[] { "Buyer", "Seller", "50/50 Shared" });
            _cboDst.SelectedIndex = 0; // Buyer

            AddLabel(cardTaxes, "Registration & Notarial", 430, 44);
            var cboReg = AddComboBox(cardTaxes, 430, 66, 180);
            cboReg.Items.AddRange(new object[] { "Buyer", "Seller" });
            cboReg.SelectedIndex = 0; // Buyer
            cboReg.Enabled = false;

            pnlContent.Controls.Add(cardTaxes);

            // Card 4: Brokerage Approved Clauses & Special Stipulations
            var cardClauses = CreateSectionCard("📜  Approved Brokerage Clauses & Riders", ref y, 260);

            _chkClauses = new CheckedListBox
            {
                Location = new Point(16, 44),
                Size = new Size(654, 110),
                Font = new Font("Segoe UI", 9f),
                CheckOnClick = true,
                BorderStyle = BorderStyle.FixedSingle
            };
            var standardClauses = DealClauseLibrary.GetStandardClauses();
            foreach (var clause in standardClauses)
            {
                _chkClauses.Items.Add($"[{clause.Id}] {clause.Title}", clause.IsDefaultSelected);
            }
            cardClauses.Controls.Add(_chkClauses);

            AddLabel(cardClauses, "Special Stipulations / Addenda Riders", 16, 162);
            _txtSpecialStipulations = new TextBox
            {
                Location = new Point(16, 184),
                Size = new Size(654, 58),
                Multiline = true,
                ScrollBars = ScrollBars.Vertical,
                Font = new Font("Segoe UI", 9.5f),
                Text = "Standard reservation agreement subject to clean title verification and bank financing release."
            };
            cardClauses.Controls.Add(_txtSpecialStipulations);

            pnlContent.Controls.Add(cardClauses);

            Controls.Add(pnlContent);
            pnlContent.BringToFront();

            Resize += (_, _) =>
            {
                int cardWidth = Math.Max(500, pnlContent.ClientSize.Width - 48);
                cardCore.Width = cardWidth;
                cardTerms.Width = cardWidth;
                cardTaxes.Width = cardWidth;
                cardClauses.Width = cardWidth;
            };
        }

        private Panel CreateSectionCard(string title, ref int y, int height)
        {
            var card = new Panel
            {
                Location = new Point(24, y),
                Size = new Size(686, height),
                BackColor = Color.White,
                Padding = new Padding(16, 12, 16, 12)
            };

            card.Paint += (s, e) =>
            {
                e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                var rect = new Rectangle(0, 0, card.Width - 1, card.Height - 1);
                using var path = UiRadiusHelper.CreateRoundedPath(rect, 8);
                using var pen = new Pen(UiDetailCardHelper.BorderColor, 1f);
                e.Graphics.DrawPath(pen, path);
            };
            UiRadiusHelper.ApplyRoundedCorners(card, 8);

            var lblTitle = new Label
            {
                Text = title,
                Font = new Font("Segoe UI", 10.5f, FontStyle.Bold),
                ForeColor = UiDetailCardHelper.SectionTitleColor,
                Location = new Point(14, 12),
                AutoSize = true
            };
            card.Controls.Add(lblTitle);

            y += height + 14;
            return card;
        }

        private static void AddLabel(Panel parent, string text, int x, int y)
        {
            var lbl = new Label
            {
                Text = text,
                Font = new Font("Segoe UI", 8.5f, FontStyle.Bold),
                ForeColor = UiDetailCardHelper.LabelMutedColor,
                Location = new Point(x, y),
                AutoSize = true
            };
            parent.Controls.Add(lbl);
        }

        private static TextBox AddTextBox(Panel parent, int x, int y, int width, string defaultText = "")
        {
            var tb = new TextBox
            {
                Location = new Point(x, y),
                Width = width,
                Height = 28,
                Font = new Font("Segoe UI", 10f),
                Text = defaultText
            };
            UiRadiusHelper.SetPadding(tb, 8, 8);
            parent.Controls.Add(tb);
            return tb;
        }

        private static ComboBox AddComboBox(Panel parent, int x, int y, int width)
        {
            var cbo = new ComboBox
            {
                Location = new Point(x, y),
                Width = width,
                Height = 28,
                DropDownStyle = ComboBoxStyle.DropDownList,
                Font = new Font("Segoe UI", 9.5f)
            };
            parent.Controls.Add(cbo);
            return cbo;
        }

        private void RecalculateFinancing()
        {
            if (!decimal.TryParse(_txtValue.Text.Replace(",", "").Trim(), out decimal val))
                val = 0;

            var financing = DealController.CalculateFinancing(val, _numDownPercent.Value, _cboPaymentScheme.SelectedItem?.ToString());
            _lblDownAmount.Text = financing.DownPaymentDisplay;
            _lblBalanceAmount.Text = financing.BalanceDisplay;
        }

        private void LoadData()
        {
            // Populate Buyer Picker
            var customers = _controller.GetCustomerPickerList(_existingDeal?.CustomerId);
            _cboCustomer.DisplayMember = "Value";
            _cboCustomer.ValueMember = "Key";
            _cboCustomer.DataSource = customers;

            // Populate Property Picker
            var properties = _controller.GetPropertyPickerList();
            _cboProperty.DisplayMember = "Value";
            _cboProperty.ValueMember = "Key";
            _cboProperty.DataSource = properties;

            // Populate Agent Picker
            var agents = _controller.GetAgentPickerList();
            _cboAgent.DisplayMember = "Value";
            _cboAgent.ValueMember = "Key";
            _cboAgent.DataSource = agents;

            if (_existingDeal != null)
            {
                if (customers.Any(c => c.Key == _existingDeal.CustomerId))
                    _cboCustomer.SelectedValue = _existingDeal.CustomerId;

                if (properties.Any(p => p.Key == _existingDeal.PropertyId))
                    _cboProperty.SelectedValue = _existingDeal.PropertyId;

                if (_existingDeal.AgentId.HasValue && agents.Any(a => a.Key == _existingDeal.AgentId.Value))
                    _cboAgent.SelectedValue = _existingDeal.AgentId.Value;

                _txtValue.Text = _existingDeal.Value.ToString("N2");
                _numCommission.Value = Math.Min(20, Math.Max(0, _existingDeal.CommissionRate * 100));

                int stgIdx = _cboStage.FindStringExact(_existingDeal.Stage);
                if (stgIdx >= 0) _cboStage.SelectedIndex = stgIdx;

                if (_existingDeal.ExpectedCloseDate.HasValue)
                    _dtpCloseDate.Value = _existingDeal.ExpectedCloseDate.Value;

                if (!string.IsNullOrWhiteSpace(_existingDeal.PaymentScheme))
                {
                    int schIdx = _cboPaymentScheme.FindStringExact(_existingDeal.PaymentScheme);
                    if (schIdx >= 0) _cboPaymentScheme.SelectedIndex = schIdx;
                }

                if (_existingDeal.ReservationFee.HasValue)
                    _txtReservationFee.Text = _existingDeal.ReservationFee.Value.ToString("N2");

                if (_existingDeal.DownPaymentPercent.HasValue)
                    _numDownPercent.Value = Math.Min(100, Math.Max(0, _existingDeal.DownPaymentPercent.Value));

                if (!string.IsNullOrWhiteSpace(_existingDeal.CgtPayer))
                    _cboCgt.SelectedItem = _existingDeal.CgtPayer;

                if (!string.IsNullOrWhiteSpace(_existingDeal.DstPayer))
                    _cboDst.SelectedItem = _existingDeal.DstPayer;

                if (!string.IsNullOrWhiteSpace(_existingDeal.SpecialStipulations))
                    _txtSpecialStipulations.Text = _existingDeal.SpecialStipulations;

                // Select active clauses
                var activeIds = (_existingDeal.ApprovedClauseIds ?? "").Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
                if (activeIds.Length > 0)
                {
                    for (int i = 0; i < _chkClauses.Items.Count; i++)
                    {
                        string itemText = _chkClauses.Items[i].ToString() ?? "";
                        bool isChecked = activeIds.Any(id => itemText.StartsWith($"[{id}]"));
                        _chkClauses.SetItemChecked(i, isChecked);
                    }
                }
            }
            else
            {
                // On new deal creation, default assigned agent to the current logged-in agent
                int currentUserId = CurrentSession.UserId;
                if (agents.Any(a => a.Key == currentUserId))
                {
                    _cboAgent.SelectedValue = currentUserId;
                }

                _cboStage.SelectedItem = "Reservation";
                _cboStage.Enabled = false; // Initial stage is locked to Reservation on create

                _dtpCloseDate.MinDate = DateTime.Today;
                _dtpCloseDate.Value = DateTime.Today.AddDays(30);
            }

            // R24: Only Manager or Admin may set or change ownership. Regular agents are locked.
            if (!RbacService.CanAssignRecords)
            {
                _cboAgent.Enabled = false;
            }

            RecalculateFinancing();
        }

        private void BtnSaveClick(object? sender, EventArgs e)
        {
            if (!DealController.ValidateDealInput(_cboCustomer.SelectedValue, _cboProperty.SelectedValue, _txtValue.Text, _dtpCloseDate.Value, out decimal dealVal, out string? error))
            {
                MessageBox.Show(error ?? "Validation error.", "Validation Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            decimal.TryParse(_txtReservationFee.Text.Replace(",", "").Trim(), out decimal resFee);
            decimal downPercent = _numDownPercent.Value;
            string paymentScheme = _cboPaymentScheme.SelectedItem?.ToString() ?? "Bank Financing";
            var financing = DealController.CalculateFinancing(dealVal, downPercent, paymentScheme);
            decimal downAmt = financing.DownPaymentAmount;
            decimal balAmt = financing.BalanceAmount;

            // Collect active clause IDs
            var selectedClauseIds = new List<string>();
            for (int i = 0; i < _chkClauses.Items.Count; i++)
            {
                if (_chkClauses.GetItemChecked(i))
                {
                    string text = _chkClauses.Items[i].ToString() ?? "";
                    int start = text.IndexOf('[');
                    int end = text.IndexOf(']');
                    if (start >= 0 && end > start)
                    {
                        selectedClauseIds.Add(text.Substring(start + 1, end - start - 1));
                    }
                }
            }

            string stage = _cboStage.SelectedItem?.ToString() ?? "Reservation";

            Result = new Deal
            {
                DealId = _existingDeal?.DealId ?? 0,
                CustomerId = Convert.ToInt32(_cboCustomer.SelectedValue),
                PropertyId = Convert.ToInt32(_cboProperty.SelectedValue),
                AgentId = RbacService.CanAssignRecords ? (_cboAgent.SelectedValue as int?) : (_existingDeal?.AgentId ?? CurrentSession.UserId),
                Value = dealVal,
                CommissionRate = _numCommission.Value / 100m,
                Stage = stage,
                ExpectedCloseDate = _dtpCloseDate.Value,
                PaymentScheme = paymentScheme,
                ReservationFee = resFee,
                DownPaymentPercent = downPercent,
                DownPaymentAmount = downAmt,
                BalanceAmount = balAmt,
                CgtPayer = _cboCgt.SelectedItem?.ToString() ?? "Seller",
                DstPayer = _cboDst.SelectedItem?.ToString() ?? "Buyer",
                TransferTaxPayer = "Buyer",
                RegistrationFeePayer = "Buyer",
                ApprovedClauseIds = string.Join(",", selectedClauseIds),
                SpecialStipulations = _txtSpecialStipulations.Text.Trim(),
                ContingenciesJson = _existingDeal?.ContingenciesJson ?? DealContingency.SerializeList(DealClauseLibrary.GetDefaultContingencies(paymentScheme))
            };

            DialogResult = DialogResult.OK;
            Close();
        }
    }
}
