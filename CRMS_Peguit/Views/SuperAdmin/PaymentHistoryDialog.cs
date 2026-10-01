using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using CRMS_Peguit.domain.Common;
using CRMS_Peguit.domain.entities;
using CRMS_Peguit.winforms.Controllers;
using CRMS_Peguit.winforms.Controls;
using CRMS_Peguit.winforms.Models.Services;
using CRMS_Peguit.winforms.Services;

namespace CRMS_Peguit.winforms.Views.SuperAdmin
{
    public class PaymentHistoryDialog : Form
    {
        private readonly SuperAdminSubscriptionController _controller = new();
        private readonly TenantSubscriptionDto _subscription;
        private List<PaymentRecordDto> _records = new();

        private readonly DataGridView _grid;
        private readonly StatusText _statusText;
        private readonly Label _lblPaidThrough;
        private readonly Label _lblSummaryTotal;
        private readonly Label _lblEmptyNotice;
        private readonly GridSkeletonOverlay _gridSkeleton;

        public PaymentHistoryDialog(TenantSubscriptionDto subscription)
        {
            _subscription = subscription ?? throw new ArgumentNullException(nameof(subscription));

            Text = $"Payment History — {_subscription.CompanyName}";
            Size = new Size(860, 580);
            UiRadiusHelper.StyleModal(this, 8);
            MaximizeBox = true;

            var pnlMain = new Panel
            {
                Dock = DockStyle.Fill,
                Padding = new Padding(24),
                BackColor = Theme.Background
            };
            Controls.Add(pnlMain);

            // 1. Top Header Card
            var pnlHeaderCard = new Panel
            {
                Dock = DockStyle.Top,
                Height = 88,
                BackColor = Color.White,
                Padding = new Padding(20, 16, 20, 16),
                Margin = new Padding(0, 0, 0, 16)
            };
            UiRadiusHelper.StyleCard(pnlHeaderCard, 8);

            var lblTitle = new Label
            {
                Text = $"Payment History: {_subscription.CompanyName}",
                Font = new Font("Segoe UI", 13f, FontStyle.Bold),
                ForeColor = Theme.TextPrimary,
                Location = new Point(20, 14),
                AutoSize = true,
                AutoEllipsis = true,
                MaximumSize = new Size(Math.Max(180, pnlHeaderCard.Width - 400), 26)
            };
            pnlHeaderCard.Controls.Add(lblTitle);

            var lblPlan = new Label
            {
                Text = $"Plan: {_subscription.PlanName} · Monthly Fee: ₱{_subscription.BillingAmount:N2}",
                Font = new Font("Segoe UI", 9.5f),
                ForeColor = Theme.TextSecondary,
                Location = new Point(20, 44),
                AutoSize = true,
                AutoEllipsis = true,
                MaximumSize = new Size(Math.Max(180, pnlHeaderCard.Width - 400), 22)
            };
            pnlHeaderCard.Controls.Add(lblPlan);

            // Paid Through & Status on the right of header
            var pnlStatusWrap = new FlowLayoutPanel
            {
                FlowDirection = FlowDirection.LeftToRight,
                AutoSize = true,
                Location = new Point(480, 16),
                Anchor = AnchorStyles.Top | AnchorStyles.Right,
                BackColor = Color.Transparent
            };

            var lblStatusPrefix = new Label
            {
                Text = "Status: ",
                Font = new Font("Segoe UI", 9.5f),
                ForeColor = Theme.TextSecondary,
                AutoSize = true,
                Margin = new Padding(0, 1, 4, 0)
            };
            _statusText = new StatusText(_subscription.Status);
            pnlStatusWrap.Controls.Add(lblStatusPrefix);
            pnlStatusWrap.Controls.Add(_statusText);
            pnlHeaderCard.Controls.Add(pnlStatusWrap);

            _lblPaidThrough = new Label
            {
                Text = $"Paid Through: {(_subscription.PaidThroughDate.HasValue ? _subscription.PaidThroughDate.Value.ToString("MMM dd, yyyy") : "Lifetime")}",
                Font = new Font("Segoe UI", 9.5f, FontStyle.Bold),
                ForeColor = Color.FromArgb(15, 23, 42),
                Location = new Point(480, 44),
                Anchor = AnchorStyles.Top | AnchorStyles.Right,
                AutoSize = true
            };
            pnlHeaderCard.Controls.Add(_lblPaidThrough);

            // Record Payment Button
            var btnRecordNew = new Button
            {
                Text = "＋ Record Payment",
                Size = new Size(160, 36),
                Anchor = AnchorStyles.Top | AnchorStyles.Right,
                Location = new Point(pnlHeaderCard.Width - 180, 25)
            };
            UiRadiusHelper.StylePrimaryButton(btnRecordNew, 8);
            btnRecordNew.Click += BtnRecordNew_Click;
            pnlHeaderCard.Controls.Add(btnRecordNew);
            lblTitle.MaximumSize = lblPlan.MaximumSize = Size.Empty;
            ResponsiveLayout.BindHeader(pnlHeaderCard, lblTitle, lblPlan, pnlStatusWrap, _lblPaidThrough, btnRecordNew);

            // 2. Bottom Summary Bar
            var pnlBottomBar = new Panel
            {
                Dock = DockStyle.Bottom,
                Height = 56,
                BackColor = Color.White,
                Padding = new Padding(20, 10, 20, 10)
            };
            UiRadiusHelper.StyleCard(pnlBottomBar, 8);

            _lblSummaryTotal = new Label
            {
                Text = "Total Recorded: 0 payments · ₱0.00",
                Font = new Font("Segoe UI", 10f, FontStyle.Bold),
                ForeColor = Color.FromArgb(15, 23, 42),
                Location = new Point(20, 16),
                AutoSize = true
            };
            pnlBottomBar.Controls.Add(_lblSummaryTotal);

            var btnClose = new Button
            {
                Text = "Close",
                Size = new Size(95, 36),
                Anchor = AnchorStyles.Top | AnchorStyles.Right,
                Location = new Point(pnlBottomBar.Width - 115, 10)
            };
            UiRadiusHelper.StyleSecondaryButton(btnClose, 8);
            btnClose.Click += (_, _) => Close();
            pnlBottomBar.SizeChanged += (_, _) =>
                btnClose.Location = new Point(pnlBottomBar.Width - 115, 10);
            pnlBottomBar.Controls.Add(btnClose);

            // Spacing
            var pnlGap = new Panel
            {
                Dock = DockStyle.Top,
                Height = 14,
                BackColor = Color.Transparent
            };

            // 3. Grid Container Card
            var pnlGridCard = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.White,
                Padding = new Padding(1)
            };
            UiRadiusHelper.StyleCard(pnlGridCard, 8);

            _grid = new DataGridView
            {
                Dock = DockStyle.Fill,
                BackgroundColor = Color.White,
                BorderStyle = BorderStyle.None,
                RowHeadersVisible = false,
                AllowUserToAddRows = false,
                AllowUserToDeleteRows = false,
                ReadOnly = true,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                MultiSelect = false,
                RowTemplate = { Height = 54 }
            };
            UiGridHelper.ApplyModernGridStyle(_grid);
            _gridSkeleton = GridSkeletonOverlay.CreateForGrid(_grid);

            // Columns matching requirements:
            // AmountPaid (right-aligned, currency formatted)
            // PaymentMethod (plain text category, not status badge)
            // PaymentReference
            // PaymentDate
            // recorded-by (AvatarLabel styled)
            _grid.Columns.Add(new DataGridViewTextBoxColumn
            {
                HeaderText = "Payment Date",
                DataPropertyName = "PaymentDateFormatted",
                Width = 120
            });
            _grid.Columns.Add(new DataGridViewTextBoxColumn
            {
                HeaderText = "Reference No.",
                DataPropertyName = "PaymentReference",
                Width = 170
            });
            _grid.Columns.Add(new DataGridViewTextBoxColumn
            {
                HeaderText = "Method",
                DataPropertyName = "PaymentMethodDisplay",
                Width = 120
            });
            _grid.Columns.Add(new DataGridViewTextBoxColumn
            {
                HeaderText = "Recorded By",
                DataPropertyName = "RecordedByName",
                Width = 180
            });
            _grid.Columns.Add(new DataGridViewTextBoxColumn
            {
                HeaderText = "Notes",
                DataPropertyName = "Notes",
                AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill
            });
            _grid.Columns.Add(new DataGridViewTextBoxColumn
            {
                HeaderText = "Amount Paid",
                DataPropertyName = "AmountPaidFormatted",
                Width = 130,
                DefaultCellStyle = new DataGridViewCellStyle
                {
                    Alignment = DataGridViewContentAlignment.MiddleRight,
                    Font = new Font("Segoe UI", 9.5f, FontStyle.Bold),
                    ForeColor = Color.FromArgb(15, 23, 42)
                }
            });

            _grid.Columns.Add(new ActionsColumn
            {
                AutoSizeMode = DataGridViewAutoSizeColumnMode.None,
                Width = 64
            });

            _grid.CellPainting += Grid_CellPainting;

            _grid.CellClick += (s, e) =>
            {
                if (e.RowIndex < 0 || e.ColumnIndex < 0) return;
                string colName = _grid.Columns[e.ColumnIndex].Name;
                if (colName == "Actions" || _grid.Columns[e.ColumnIndex] is ActionsColumn)
                {
                    if (e.RowIndex >= _records.Count) return;
                    var rec = _records[e.RowIndex];

                    var cellRect = _grid.GetCellDisplayRectangle(e.ColumnIndex, e.RowIndex, false);
                    var menu = new ContextMenuStrip();

                    var receiptItem = new ToolStripMenuItem("📄  View Receipt Summary");
                    receiptItem.Click += (_, _) =>
                    {
                        string tenant = _subscription.CompanyName;
                        string summary = $"NEXA MANUAL PAYMENT RECEIPT\n\n" +
                            $"Reference No: {rec.PaymentReference}\n" +
                            $"Payment Date: {rec.PaymentDate:MMM dd, yyyy}\n" +
                            $"Billed Organization: {tenant}\n" +
                            $"Payment Method: {rec.PaymentMethodDisplay}\n" +
                            $"Amount Paid: ₱{rec.AmountPaid:N2}\n" +
                            $"Recorded By: {rec.RecordedByName}\n" +
                            $"Notes: {rec.Notes ?? "N/A"}\n\n" +
                            $"Proof-of-payment recorded in Master DB and logged to Platform Audit Log.";

                        MessageBox.Show(summary, $"Payment Receipt — {rec.PaymentReference}", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    };
                    menu.Items.Add(receiptItem);

                    var copyRef = new ToolStripMenuItem("📋  Copy Reference No.");
                    copyRef.Click += (_, _) =>
                    {
                        if (!string.IsNullOrEmpty(rec.PaymentReference))
                        {
                            Clipboard.SetText(rec.PaymentReference);
                            MessageBox.Show("Payment reference copied to clipboard.", "Copied", MessageBoxButtons.OK, MessageBoxIcon.Information);
                        }
                    };
                    menu.Items.Add(copyRef);

                    menu.Show(_grid, new Point(Math.Max(0, cellRect.Right - 220), cellRect.Bottom));
                }
            };

            _lblEmptyNotice = new Label
            {
                Text = "No payment records found for this tenant subscription yet.\nClick '+ Record Payment' above to record the first offline transaction.",
                Font = new Font("Segoe UI", 10.5f),
                ForeColor = Theme.TextSecondary,
                TextAlign = ContentAlignment.MiddleCenter,
                Dock = DockStyle.Fill,
                Visible = false
            };
            pnlGridCard.Controls.Add(_lblEmptyNotice);
            pnlGridCard.Controls.Add(_grid);

            // Order of controls
            pnlMain.Controls.Add(pnlGridCard);
            pnlMain.Controls.Add(pnlBottomBar);
            pnlMain.Controls.Add(pnlGap);
            pnlMain.Controls.Add(pnlHeaderCard);

            _ = LoadHistoryAsync();
        }

        private void Grid_CellPainting(object? sender, DataGridViewCellPaintingEventArgs e)
        {
            if (e.Graphics == null) return;
            // Custom avatar icon drawing for "Recorded By" column
            if (e.RowIndex >= 0 && e.ColumnIndex >= 0 && _grid.Columns[e.ColumnIndex].HeaderText == "Recorded By")
            {
                e.Paint(e.CellBounds, DataGridViewPaintParts.All & ~DataGridViewPaintParts.ContentForeground);

                var name = e.Value?.ToString() ?? "Super Admin";
                var (bg, fg) = AvatarLabel.GetDeterministicAvatarColors(name);
                string initials = AvatarLabel.GetInitials(name);

                int avatarSize = 26;
                int x = e.CellBounds.Left + 8;
                int y = e.CellBounds.Top + (e.CellBounds.Height - avatarSize) / 2;

                using var brush = new SolidBrush(bg);
                e.Graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
                e.Graphics.FillEllipse(brush, x, y, avatarSize, avatarSize);

                using var textBrush = new SolidBrush(fg);
                using var font = new Font("Segoe UI", 8.5f, FontStyle.Bold);
                using var sf = new StringFormat
                {
                    Alignment = StringAlignment.Center,
                    LineAlignment = StringAlignment.Center
                };
                e.Graphics.DrawString(initials, font, textBrush, new RectangleF(x, y, avatarSize, avatarSize), sf);

                // Draw name text
                var textRect = new Rectangle(x + avatarSize + 8, e.CellBounds.Top, e.CellBounds.Width - avatarSize - 16, e.CellBounds.Height);
                using var nameBrush = new SolidBrush(Theme.TextPrimary);
                using var nameFont = new Font("Segoe UI", 9.5f);
                var textSf = new StringFormat
                {
                    LineAlignment = StringAlignment.Center
                };
                e.Graphics.DrawString(name, nameFont, nameBrush, textRect, textSf);

                // Bottom grid line
                using (var linePen = new Pen(UiGridHelper.GridBorder, 1f))
                {
                    e.Graphics.DrawLine(linePen, e.CellBounds.Left, e.CellBounds.Bottom - 1, e.CellBounds.Right, e.CellBounds.Bottom - 1);
                }

                e.Handled = true;
            }
        }

        private async Task LoadHistoryAsync()
        {
            _gridSkeleton?.ShowSkeleton();
            try
            {
                _records = await _controller.GetPaymentRecordsAsync(_subscription.SubscriptionId);

                if (_records.Count == 0 && _subscription.CompanyId > 0)
                {
                    _records = await _controller.GetCompanyPaymentHistoryAsync(_subscription.CompanyId);
                }

                var displayList = _records.Select(r => new
                {
                    r.PaymentRecordId,
                    r.SubscriptionId,
                    PaymentDateFormatted = r.PaymentDate.ToString("MMM dd, yyyy"),
                    r.PaymentReference,
                    r.PaymentMethodDisplay,
                    r.RecordedByName,
                    Notes = string.IsNullOrWhiteSpace(r.Notes) ? "—" : r.Notes,
                    AmountPaidFormatted = $"₱{r.AmountPaid:N2}"
                }).ToList();

                _grid.DataSource = displayList;

                decimal totalAmount = _records.Sum(r => r.AmountPaid);
                _lblSummaryTotal.Text = $"Total Recorded: {_records.Count} payment{(_records.Count == 1 ? "" : "s")} · ₱{totalAmount:N2}";

                // Refresh header details
                _statusText.SetStatus(_subscription.Status);
                _lblPaidThrough.Text = $"Paid Through: {(_subscription.PaidThroughDate.HasValue ? _subscription.PaidThroughDate.Value.ToString("MMM dd, yyyy") : "Lifetime")}";
            }
            finally
            {
                _gridSkeleton?.HideSkeleton();
                bool hasData = _records.Count > 0;
                _lblEmptyNotice.Visible = !hasData;
                _grid.Visible = hasData;
                if (!hasData)
                {
                    _lblEmptyNotice.BringToFront();
                }
            }
        }

        private async void BtnRecordNew_Click(object? sender, EventArgs e)
        {
            using var dlg = new RecordPaymentDialog(_subscription);
            if (dlg.ShowDialog(this) == DialogResult.OK)
            {
                // Update subscription fields
                var subs = await _controller.GetAllSubscriptionsAsync();
                var updated = subs.FirstOrDefault(s => s.SubscriptionId == _subscription.SubscriptionId || s.CompanyId == _subscription.CompanyId);
                if (updated != null)
                {
                    _subscription.EndDate = updated.EndDate;
                    _subscription.Status = updated.Status;
                }

                await LoadHistoryAsync();
            }
        }
    }
}
