using System;
using System.Drawing;
using System.Threading.Tasks;
using System.Windows.Forms;
using CRMS_Peguit.domain.entities;
using CRMS_Peguit.winforms.Controllers;
using CRMS_Peguit.winforms.Controls;
using CRMS_Peguit.winforms.Models.Services;
using CRMS_Peguit.winforms.Services;

namespace CRMS_Peguit.winforms.Views.Marketing
{
    public class RetentionRequestDialog : Form
    {
        private readonly bool _isReviewMode;
        private readonly RetentionController _controller;
        private readonly RetentionRequestRow? _existingRequest;
        private readonly Customer? _preselectedCustomer;

        // Submission Controls
        private ComboBox _cboCustomer = null!;
        private ComboBox _cboSegment = null!;
        private ComboBox _cboActionType = null!;
        private TextBox _txtProposedIncentive = null!;
        private ComboBox _cboReasonCategory = null!;
        private TextBox _txtDetails = null!;
        private Button _btnSubmit = null!;
        private DetailSkeletonOverlay? _detailSkeleton;

        // Review Controls
        private TextBox _txtReviewerRemarks = null!;
        private TextBox _txtRejectionReason = null!;
        private Button _btnApprove = null!;
        private Button _btnReject = null!;
        private Button _btnClose = null!;

        public bool WasActionTaken { get; private set; }

        public RetentionRequestDialog(RetentionController controller, Customer? preselectedCustomer = null)
        {
            _isReviewMode = false;
            _controller = controller;
            _preselectedCustomer = preselectedCustomer;

            InitializeCommonLayout();
            BuildSubmissionLayout();
        }

        public RetentionRequestDialog(RetentionController controller, RetentionRequestRow existingRequest)
        {
            _isReviewMode = true;
            _controller = controller;
            _existingRequest = existingRequest;

            InitializeCommonLayout();
            BuildReviewLayout();
        }

        private void InitializeCommonLayout()
        {
            this.Text = _isReviewMode ? "Retention Request Review & Approval" : "Create Retention Request";
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.ShowInTaskbar = false;
            this.StartPosition = FormStartPosition.CenterParent;
            this.ClientSize = new Size(680, _isReviewMode ? 720 : 640);
            this.BackColor = Color.White;
        }

        #region Submission Mode Layout

        private void BuildSubmissionLayout()
        {
            // Header panel with accent left border
            var pnlHeader = new Panel
            {
                Dock = DockStyle.Top,
                Height = 74,
                BackColor = Color.FromArgb(248, 250, 252),
                Padding = new Padding(24, 14, 24, 14)
            };

            pnlHeader.Paint += (s, e) =>
            {
                using var b = new SolidBrush(Theme.SidebarAccent);
                e.Graphics.FillRectangle(b, 0, 0, 5, pnlHeader.Height);
            };

            var lblTitle = new Label
            {
                Text = "Request Complimentary Client Care Service",
                Font = new Font("Segoe UI", 13f, FontStyle.Bold),
                ForeColor = Color.FromArgb(15, 23, 42),
                Location = new Point(20, 12),
                AutoSize = true
            };

            var lblSub = new Label
            {
                Text = "Request supervisory approval for complimentary client care services (appraisals, deed reviews, neighborhood CMA reports)",
                Font = new Font("Segoe UI", 8.5f),
                ForeColor = Color.FromArgb(100, 116, 139),
                Location = new Point(20, 38),
                AutoSize = true
            };

            pnlHeader.Controls.Add(lblTitle);
            pnlHeader.Controls.Add(lblSub);

            // Form Body
            var pnlBody = new Panel
            {
                Dock = DockStyle.Fill,
                Padding = new Padding(28, 16, 28, 16),
                AutoScroll = true
            };

            int y = 14;

            var pnlNotice = new Panel
            {
                Location = new Point(10, y),
                Size = new Size(600, 36),
                BackColor = Color.FromArgb(240, 249, 255),
                Padding = new Padding(10, 8, 10, 8)
            };
            UiRadiusHelper.StyleCard(pnlNotice, 6);
            var lblNotice = new Label
            {
                Text = "Strict Policy: Real estate client care services only. Retail coupons, promotional vouchers, and discount sales gimmicks are strictly not permitted.",
                Font = new Font("Segoe UI", 8f, FontStyle.Bold),
                ForeColor = Color.FromArgb(3, 105, 161),
                Dock = DockStyle.Fill
            };
            pnlNotice.Controls.Add(lblNotice);
            pnlBody.Controls.Add(pnlNotice);
            y += 46;

            // Customer
            var lblCust = new Label { Text = "Target Client *", Font = new Font("Segoe UI", 9f, FontStyle.Bold), Location = new Point(10, y), AutoSize = true };
            y += 24;
            _cboCustomer = new ComboBox
            {
                Location = new Point(10, y),
                Size = new Size(600, 30),
                DropDownStyle = ComboBoxStyle.DropDownList,
                Font = new Font("Segoe UI", 9.5f)
            };
            y += 40;

            // Segment & Action Type Row
            var lblSeg = new Label { Text = "Retention Segment *", Font = new Font("Segoe UI", 9f, FontStyle.Bold), Location = new Point(10, y), AutoSize = true };
            var lblAction = new Label { Text = "Action Type *", Font = new Font("Segoe UI", 9f, FontStyle.Bold), Location = new Point(320, y), AutoSize = true };
            y += 24;

            _cboSegment = new ComboBox
            {
                Location = new Point(10, y),
                Size = new Size(290, 30),
                DropDownStyle = ComboBoxStyle.DropDownList,
                Font = new Font("Segoe UI", 9.5f)
            };
            _cboSegment.Items.AddRange(RetentionCalculationService.AllSegments);

            _cboActionType = new ComboBox
            {
                Location = new Point(320, y),
                Size = new Size(290, 30),
                DropDownStyle = ComboBoxStyle.DropDownList,
                Font = new Font("Segoe UI", 9.5f)
            };
            _cboActionType.Items.AddRange(new object[] { "Complimentary Client Care Service", "CMA & Property Valuation", "Client Re-engagement Consultation", "Deed & Legal Document Review" });
            _cboActionType.SelectedIndex = 0;
            y += 40;

            // Proposed Service Offering
            var lblIncentive = new Label { Text = "Complimentary Client Care Service Offering * (Licensed Appraisal, Deed Review, CMA Study)", Font = new Font("Segoe UI", 9f, FontStyle.Bold), Location = new Point(10, y), AutoSize = true };
            y += 24;
            _txtProposedIncentive = new TextBox
            {
                Location = new Point(10, y),
                Size = new Size(600, 30),
                Font = new Font("Segoe UI", 9.5f)
            };
            y += 40;

            // Reason Category
            var lblReason = new Label { Text = "Reason for Retention *", Font = new Font("Segoe UI", 9f, FontStyle.Bold), Location = new Point(10, y), AutoSize = true };
            y += 24;
            _cboReasonCategory = new ComboBox
            {
                Location = new Point(10, y),
                Size = new Size(600, 30),
                DropDownStyle = ComboBoxStyle.DropDownList,
                Font = new Font("Segoe UI", 9.5f)
            };
            _cboReasonCategory.Items.AddRange(new object[]
            {
                "Improve Customer Retention",
                "Prevent Customer Churn",
                "Increase Customer Lifetime Value",
                "Encourage Referral",
                "Win-Back Cold Client",
                "Promotional or Strategic Decision",
                "Other (specific justification required)"
            });
            _cboReasonCategory.SelectedIndex = 0;
            y += 40;

            // Details
            var lblDetails = new Label { Text = "Retention Details & Business Justification *", Font = new Font("Segoe UI", 9f, FontStyle.Bold), Location = new Point(10, y), AutoSize = true };
            y += 24;
            _txtDetails = new TextBox
            {
                Location = new Point(10, y),
                Size = new Size(600, 90),
                Multiline = true,
                ScrollBars = ScrollBars.Vertical,
                Font = new Font("Segoe UI", 9.5f)
            };
            y += 104;

            pnlBody.Controls.Add(lblCust);
            pnlBody.Controls.Add(_cboCustomer);
            pnlBody.Controls.Add(lblSeg);
            pnlBody.Controls.Add(lblAction);
            pnlBody.Controls.Add(_cboSegment);
            pnlBody.Controls.Add(_cboActionType);
            pnlBody.Controls.Add(lblIncentive);
            pnlBody.Controls.Add(_txtProposedIncentive);
            pnlBody.Controls.Add(lblReason);
            pnlBody.Controls.Add(_cboReasonCategory);
            pnlBody.Controls.Add(lblDetails);
            pnlBody.Controls.Add(_txtDetails);

            // Bottom action panel
            var pnlBottom = new Panel
            {
                Dock = DockStyle.Bottom,
                Height = 60,
                BackColor = Color.FromArgb(248, 250, 252),
                Padding = new Padding(24, 12, 24, 12)
            };

            _btnSubmit = new Button
            {
                Text = "Submit for Approval",
                Size = new Size(170, 38),
                BackColor = Theme.SidebarAccent,
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 9.5f, FontStyle.Bold),
                Location = new Point(480, 11),
                Anchor = AnchorStyles.Top | AnchorStyles.Right,
                Cursor = Cursors.Hand
            };
            _btnSubmit.FlatAppearance.BorderSize = 0;
            UiRadiusHelper.StyleButton(_btnSubmit, 8);
            _btnSubmit.Click += async (s, e) => await OnSubmitAsync();

            var btnCancel = new Button
            {
                Text = "Cancel",
                Size = new Size(90, 38),
                BackColor = Color.White,
                ForeColor = Color.FromArgb(71, 85, 105),
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 9f),
                Location = new Point(380, 11),
                Anchor = AnchorStyles.Top | AnchorStyles.Right,
                Cursor = Cursors.Hand
            };
            btnCancel.FlatAppearance.BorderColor = Color.FromArgb(226, 232, 240);
            UiRadiusHelper.StyleButton(btnCancel, 8);
            btnCancel.Click += (s, e) => this.Close();

            pnlBottom.Controls.Add(btnCancel);
            pnlBottom.Controls.Add(_btnSubmit);

            this.Controls.Add(pnlBody);
            _detailSkeleton = DetailSkeletonOverlay.CreateForContainer(pnlBody);
            this.Controls.Add(pnlBottom);
            this.Controls.Add(pnlHeader);

            // Populate customers
            _ = LoadCustomersForDropdownAsync();
        }

        private async Task LoadCustomersForDropdownAsync()
        {
            _detailSkeleton?.ShowSkeleton();
            try
            {
                var customers = await _controller.GetCustomersAsync();
                _cboCustomer.Items.Clear();

                int selectedIndex = 0;
                for (int i = 0; i < customers.Count; i++)
                {
                    var c = customers[i];
                    _cboCustomer.Items.Add(new CustomerComboItem(c.CustomerId, $"{c.FullName} ({c.Email}) - {c.CurrentSegment}"));
                    if (_preselectedCustomer != null && c.CustomerId == _preselectedCustomer.CustomerId)
                    {
                        selectedIndex = i;
                    }
                }

                if (_cboCustomer.Items.Count > 0)
                {
                    _cboCustomer.SelectedIndex = selectedIndex;
                    UpdateDefaultIncentiveForSelectedCustomer();
                }

                _cboCustomer.SelectedIndexChanged += (s, e) => UpdateDefaultIncentiveForSelectedCustomer();
                _cboSegment.SelectedIndexChanged += (s, e) =>
                {
                    if (_cboSegment.SelectedItem is string seg)
                    {
                        _txtProposedIncentive.Text = RetentionCalculationService.GetRecommendedIncentive(seg);
                    }
                };
            }
            finally
            {
                _detailSkeleton?.HideSkeleton();
            }
        }

        private void UpdateDefaultIncentiveForSelectedCustomer()
        {
            if (_cboCustomer.SelectedItem is CustomerComboItem item)
            {
                _ = Task.Run(async () =>
                {
                    var cust = await _controller.GetCustomerByIdAsync(item.CustomerId);
                    if (cust != null && !IsDisposed)
                    {
                        BeginInvoke(new Action(() =>
                        {
                            string seg = cust.CurrentRetentionSegment ?? RetentionCalculationService.SegmentProspective;
                            _cboSegment.SelectedItem = seg;
                            if (string.IsNullOrWhiteSpace(_txtProposedIncentive.Text))
                            {
                                _txtProposedIncentive.Text = RetentionCalculationService.GetRecommendedIncentive(seg);
                            }
                        }));
                    }
                });
            }
        }

        private async Task OnSubmitAsync()
        {
            if (_cboCustomer.SelectedItem is not CustomerComboItem cust)
            {
                MessageBox.Show("Please select a target client.", "Validation Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            string seg = _cboSegment.SelectedItem?.ToString() ?? RetentionCalculationService.SegmentProspective;
            string action = _cboActionType.SelectedItem?.ToString() ?? "Incentive Offer";
            string incentive = _txtProposedIncentive.Text.Trim();
            string reason = _cboReasonCategory.SelectedItem?.ToString() ?? "Improve Customer Retention";
            string details = _txtDetails.Text.Trim();

            if (string.IsNullOrWhiteSpace(incentive))
            {
                MessageBox.Show("Please provide a specific proposed service incentive.", "Validation Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                _txtProposedIncentive.Focus();
                return;
            }

            if (string.IsNullOrWhiteSpace(details))
            {
                MessageBox.Show("Please provide justification details for this retention proposal.", "Validation Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                _txtDetails.Focus();
                return;
            }

            try
            {
                _btnSubmit.Enabled = false;
                await _controller.SubmitRetentionRequestAsync(cust.CustomerId, seg, action, incentive, reason, details);
                WasActionTaken = true;
                MessageBox.Show("Retention request submitted successfully for management review.", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
                this.Close();
            }
            catch (Exception ex)
            {
                _btnSubmit.Enabled = true;
                MessageBox.Show($"Failed to submit retention request: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        #endregion

        #region Review Mode Layout

        private void BuildReviewLayout()
        {
            var req = _existingRequest!;

            // Header panel with accent left border
            var pnlHeader = new Panel
            {
                Dock = DockStyle.Top,
                Height = 74,
                BackColor = Color.FromArgb(248, 250, 252),
                Padding = new Padding(24, 14, 24, 14)
            };

            pnlHeader.Paint += (s, e) =>
            {
                using var b = new SolidBrush(Theme.SidebarAccent);
                e.Graphics.FillRectangle(b, 0, 0, 5, pnlHeader.Height);
            };

            var lblTitle = new Label
            {
                Text = $"Retention Proposal #{req.RequestId} — {req.CustomerName}",
                Font = new Font("Segoe UI", 13f, FontStyle.Bold),
                ForeColor = Color.FromArgb(15, 23, 42),
                Location = new Point(20, 12),
                AutoSize = true
            };

            var lblSub = new Label
            {
                Text = $"Target Segment: {req.TargetSegment}  •  Status: {req.Status.ToUpper()}",
                Font = new Font("Segoe UI", 9f),
                ForeColor = Color.FromArgb(100, 116, 139),
                Location = new Point(20, 38),
                AutoSize = true
            };

            pnlHeader.Controls.Add(lblTitle);
            pnlHeader.Controls.Add(lblSub);

            // Scrollable Timeline Body
            var pnlBody = new Panel
            {
                Dock = DockStyle.Fill,
                Padding = new Padding(24, 16, 24, 16),
                AutoScroll = true
            };

            int y = 10;

            // Self-approval warning banner if not allowed
            if (req.Status == "Pending" && !req.CanCurrentUserApprove)
            {
                var pnlAlert = new Panel
                {
                    Location = new Point(10, y),
                    Size = new Size(610, 48),
                    BackColor = Color.FromArgb(254, 242, 242), // light red
                    Padding = new Padding(12)
                };
                UiRadiusHelper.StyleCard(pnlAlert, 6);

                var lblAlert = new Label
                {
                    Text = $"⚠ Governance Lock: {req.CannotApproveReason}",
                    Font = new Font("Segoe UI", 9f, FontStyle.Bold),
                    ForeColor = Color.FromArgb(185, 28, 28),
                    Dock = DockStyle.Fill
                };
                pnlAlert.Controls.Add(lblAlert);
                pnlBody.Controls.Add(pnlAlert);
                y += 58;
            }

            // Timeline Card 1: "Submitted"
            var card1 = new Panel
            {
                Location = new Point(10, y),
                Size = new Size(610, 190),
                BackColor = Color.FromArgb(248, 250, 252),
                Padding = new Padding(16)
            };
            UiRadiusHelper.StyleCard(card1, 8);

            var lblNode1 = new Label
            {
                Text = "● NODE 1: SUBMITTED",
                Font = new Font("Segoe UI", 9f, FontStyle.Bold),
                ForeColor = Theme.SidebarAccent,
                Location = new Point(14, 12),
                AutoSize = true
            };

            var lblSubmitterInfo = new Label
            {
                Text = $"By: {req.SubmitterName} ({req.SubmitterRole})  •  {req.CreatedAt:MMM dd, yyyy hh:mm tt}",
                Font = new Font("Segoe UI", 9f, FontStyle.Regular),
                ForeColor = Color.FromArgb(71, 85, 105),
                Location = new Point(14, 34),
                AutoSize = true
            };

            var lblDetailsBlock = new Label
            {
                Text = $"• Client: {req.CustomerName} ({req.CustomerEmail})\n" +
                       $"• Target Segment: {req.TargetSegment}\n" +
                       $"• Action Type: {req.ActionType}\n" +
                       $"• Proposed Incentive: {req.ProposedIncentive}\n" +
                       $"• Reason Category: {req.ReasonCategory}\n" +
                       $"• Notes: {req.RetentionDetails}",
                Font = new Font("Segoe UI", 9f),
                ForeColor = Color.FromArgb(30, 41, 59),
                Location = new Point(14, 62),
                Size = new Size(580, 116)
            };

            card1.Controls.Add(lblNode1);
            card1.Controls.Add(lblSubmitterInfo);
            card1.Controls.Add(lblDetailsBlock);
            pnlBody.Controls.Add(card1);
            y += 204;

            // Timeline Card 2: "Reviewed"
            var card2 = new Panel
            {
                Location = new Point(10, y),
                Size = new Size(610, 140),
                BackColor = req.Status == "Approved" ? Color.FromArgb(240, 253, 244) :
                            req.Status == "Rejected" ? Color.FromArgb(254, 242, 242) :
                            Color.FromArgb(255, 251, 235),
                Padding = new Padding(16)
            };
            UiRadiusHelper.StyleCard(card2, 8);

            var lblNode2 = new Label
            {
                Text = $"● NODE 2: REVIEW STATUS — {req.Status.ToUpper()}",
                Font = new Font("Segoe UI", 9f, FontStyle.Bold),
                ForeColor = req.Status == "Approved" ? Color.FromArgb(22, 163, 74) :
                            req.Status == "Rejected" ? Color.FromArgb(220, 38, 38) :
                            Color.FromArgb(217, 119, 6),
                Location = new Point(14, 12),
                AutoSize = true
            };

            string reviewInfo = req.ReviewedAt.HasValue
                ? $"Reviewed By: {req.ReviewerName ?? "Supervisor"}  •  {req.ReviewedAt.Value:MMM dd, yyyy hh:mm tt}"
                : "Awaiting supervisory review and formal decision.";

            var lblReviewerInfo = new Label
            {
                Text = reviewInfo,
                Font = new Font("Segoe UI", 9f),
                ForeColor = Color.FromArgb(71, 85, 105),
                Location = new Point(14, 34),
                AutoSize = true
            };

            string feedbackText = !string.IsNullOrWhiteSpace(req.RejectionReason)
                ? $"Rejection Reason: {req.RejectionReason}\nRemarks: {req.ReviewerRemarks ?? "None"}"
                : $"Remarks: {req.ReviewerRemarks ?? "None"}";

            var lblFeedback = new Label
            {
                Text = feedbackText,
                Font = new Font("Segoe UI", 9f),
                ForeColor = Color.FromArgb(30, 41, 59),
                Location = new Point(14, 62),
                Size = new Size(580, 68)
            };

            card2.Controls.Add(lblNode2);
            card2.Controls.Add(lblReviewerInfo);
            card2.Controls.Add(lblFeedback);
            pnlBody.Controls.Add(card2);
            y += 154;

            // Review Inputs (if pending and allowed)
            if (req.Status == "Pending")
            {
                var lblRem = new Label { Text = "Reviewer Remarks / Guidance:", Font = new Font("Segoe UI", 9f, FontStyle.Bold), Location = new Point(10, y), AutoSize = true };
                y += 24;
                _txtReviewerRemarks = new TextBox
                {
                    Location = new Point(10, y),
                    Size = new Size(610, 40),
                    Multiline = true,
                    Font = new Font("Segoe UI", 9.5f),
                    Enabled = req.CanCurrentUserApprove
                };
                y += 48;

                var lblRej = new Label { Text = "Mandatory Rejection Reason (only needed if Rejecting):", Font = new Font("Segoe UI", 9f, FontStyle.Bold), Location = new Point(10, y), AutoSize = true };
                y += 24;
                _txtRejectionReason = new TextBox
                {
                    Location = new Point(10, y),
                    Size = new Size(610, 40),
                    Multiline = true,
                    Font = new Font("Segoe UI", 9.5f),
                    Enabled = req.CanCurrentUserApprove
                };
                y += 50;

                pnlBody.Controls.Add(lblRem);
                pnlBody.Controls.Add(_txtReviewerRemarks);
                pnlBody.Controls.Add(lblRej);
                pnlBody.Controls.Add(_txtRejectionReason);
            }

            // Bottom Buttons
            var pnlBottom = new Panel
            {
                Dock = DockStyle.Bottom,
                Height = 60,
                BackColor = Color.FromArgb(248, 250, 252),
                Padding = new Padding(24, 12, 24, 12)
            };

            _btnClose = new Button
            {
                Text = "Close",
                Size = new Size(84, 38),
                BackColor = Color.White,
                ForeColor = Color.FromArgb(71, 85, 105),
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 9f),
                Location = new Point(560, 11),
                Anchor = AnchorStyles.Top | AnchorStyles.Right,
                Cursor = Cursors.Hand
            };
            _btnClose.FlatAppearance.BorderColor = Color.FromArgb(226, 232, 240);
            UiRadiusHelper.StyleButton(_btnClose, 8);
            _btnClose.Click += (s, e) => this.Close();

            if (req.Status == "Pending" && req.CanCurrentUserApprove)
            {
                _btnReject = new Button
                {
                    Text = "✕ Reject Request",
                    Size = new Size(130, 38),
                    BackColor = Color.FromArgb(239, 68, 68),
                    ForeColor = Color.White,
                    FlatStyle = FlatStyle.Flat,
                    Font = new Font("Segoe UI", 9f, FontStyle.Bold),
                    Location = new Point(415, 11),
                    Anchor = AnchorStyles.Top | AnchorStyles.Right,
                    Cursor = Cursors.Hand
                };
                _btnReject.FlatAppearance.BorderSize = 0;
                UiRadiusHelper.StyleButton(_btnReject, 8);
                _btnReject.Click += async (s, e) => await OnRejectAsync();

                _btnApprove = new Button
                {
                    Text = "✓ Approve & Queue Campaign",
                    Size = new Size(205, 38),
                    BackColor = Color.FromArgb(22, 163, 74),
                    ForeColor = Color.White,
                    FlatStyle = FlatStyle.Flat,
                    Font = new Font("Segoe UI", 9f, FontStyle.Bold),
                    Location = new Point(195, 11),
                    Anchor = AnchorStyles.Top | AnchorStyles.Right,
                    Cursor = Cursors.Hand
                };
                _btnApprove.FlatAppearance.BorderSize = 0;
                UiRadiusHelper.StyleButton(_btnApprove, 8);
                _btnApprove.Click += async (s, e) => await OnApproveAsync();

                pnlBottom.Controls.Add(_btnApprove);
                pnlBottom.Controls.Add(_btnReject);
            }

            pnlBottom.Controls.Add(_btnClose);

            this.Controls.Add(pnlBody);
            this.Controls.Add(pnlBottom);
            this.Controls.Add(pnlHeader);
        }

        private async Task OnApproveAsync()
        {
            var req = _existingRequest!;
            string remarks = _txtReviewerRemarks.Text.Trim();

            var confirm = MessageBox.Show(
                $"Are you sure you want to approve retention proposal #{req.RequestId}?\n" +
                $"Proposed Incentive: {req.ProposedIncentive}\n\n" +
                $"This will automatically create a queued email campaign entry for this client.",
                "Confirm Approval",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Question);

            if (confirm != DialogResult.Yes) return;

            try
            {
                _btnApprove.Enabled = false;
                if (_btnReject != null) _btnReject.Enabled = false;

                await _controller.ApproveRetentionRequestAsync(req.RequestId, remarks);
                WasActionTaken = true;
                MessageBox.Show("Retention request approved and email campaign entry created successfully.", "Approved", MessageBoxButtons.OK, MessageBoxIcon.Information);
                this.Close();
            }
            catch (Exception ex)
            {
                _btnApprove.Enabled = true;
                if (_btnReject != null) _btnReject.Enabled = true;
                MessageBox.Show($"Failed to approve request: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private async Task OnRejectAsync()
        {
            var req = _existingRequest!;
            string rejection = _txtRejectionReason.Text.Trim();
            string remarks = _txtReviewerRemarks.Text.Trim();

            if (string.IsNullOrWhiteSpace(rejection))
            {
                MessageBox.Show("A specific rejection reason is mandatory.", "Validation Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                _txtRejectionReason.Focus();
                return;
            }

            try
            {
                if (_btnApprove != null) _btnApprove.Enabled = false;
                _btnReject.Enabled = false;

                await _controller.RejectRetentionRequestAsync(req.RequestId, rejection, remarks);
                WasActionTaken = true;
                MessageBox.Show("Retention request rejected.", "Rejected", MessageBoxButtons.OK, MessageBoxIcon.Information);
                this.Close();
            }
            catch (Exception ex)
            {
                if (_btnApprove != null) _btnApprove.Enabled = true;
                _btnReject.Enabled = true;
                MessageBox.Show($"Failed to reject request: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        #endregion

        private class CustomerComboItem
        {
            public int CustomerId { get; }
            public string DisplayText { get; }

            public CustomerComboItem(int id, string display)
            {
                CustomerId = id;
                DisplayText = display;
            }

            public override string ToString() => DisplayText;
        }
    }
}
