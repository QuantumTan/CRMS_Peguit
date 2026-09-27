using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using CRMS_Peguit.domain.entities;
using CRMS_Peguit.winforms.Auth;
using CRMS_Peguit.winforms.Controllers;
using CRMS_Peguit.winforms.Models.Services;

namespace CRMS_Peguit.winforms.Views.Deals
{
    public partial class DealDetailForm : Form
    {
        private static readonly string[] DealStages =
            { "Offer", "Reservation", "Contract", "Closed" };

        private Deal? _deal;
        private readonly DealController _controller;

        private Panel? _pnlHeader;
        private Panel? _pnlFooter;
        private Panel? _pnlContent;
        private Button? _btnEdit;
        private Button? _btnViewContract;
        private Button? _btnClose;

        public DealDetailForm() : this(new Deal(), new DealController())
        {
        }

        public DealDetailForm(Deal deal, DealController controller)
        {
            _deal = deal;
            _controller = controller;
            InitializeComponent();
            SetupFormProperties();
            BuildUi();
        }

        private void SetupFormProperties()
        {
            Text = _deal is not null ? $"Deal #{_deal.DealId} - Contract & Terms" : "Deal Details";
            Size = new Size(760, 720);
            MinimumSize = new Size(640, 540);
            StartPosition = FormStartPosition.CenterParent;
            BackColor = Color.FromArgb(244, 247, 251);
            DoubleBuffered = true;
            AppBrand.ApplyDarkTitleBar(this);
            AppBrand.ApplyAppIcon(this);
        }

        private void BuildUi()
        {
            Controls.Clear();
            if (_deal == null) return;

            // 1. Fixed Bottom Action Footer
            BuildFooter();

            // 2. Hero Header
            BuildHeader();

            // 3. Scrollable Content Canvas
            BuildContent();

            Resize += (_, _) => LayoutResponsiveComponents();
            LayoutResponsiveComponents();
        }

        private void BuildHeader()
        {
            _pnlHeader = new Panel
            {
                Dock = DockStyle.Top,
                Height = 88,
                BackColor = Color.White,
                Padding = new Padding(24, 16, 24, 16)
            };

            _pnlHeader.Paint += (s, e) =>
            {
                using var pen = new Pen(UiDetailCardHelper.BorderColor, 1f);
                e.Graphics.DrawLine(pen, 0, _pnlHeader.Height - 1, _pnlHeader.Width, _pnlHeader.Height - 1);
            };

            // Briefcase icon bubble 💼
            var pnlIcon = new Panel
            {
                Size = new Size(52, 52),
                Location = new Point(24, 18),
                BackColor = Color.FromArgb(238, 242, 255) // #EEF2FF
            };
            pnlIcon.Paint += (s, e) =>
            {
                e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                var rect = new Rectangle(0, 0, pnlIcon.Width - 1, pnlIcon.Height - 1);
                using var path = UiRadiusHelper.CreateRoundedPath(rect, 10);
                using var pen = new Pen(Color.FromArgb(199, 210, 254), 1f);
                e.Graphics.DrawPath(pen, path);
            };
            UiRadiusHelper.ApplyRoundedCorners(pnlIcon, 10);

            var lblIcon = new Label
            {
                Text = "💼",
                Font = new Font("Segoe UI Emoji", 18f),
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleCenter
            };
            pnlIcon.Controls.Add(lblIcon);
            _pnlHeader.Controls.Add(pnlIcon);

            // Title & Subtitle block
            var customers = _controller.GetCustomerNames();
            var properties = _controller.GetPropertyAddresses();
            string buyer = customers.TryGetValue(_deal!.CustomerId, out string? bName) ? bName : $"Customer #{_deal.CustomerId}";
            string prop = properties.TryGetValue(_deal.PropertyId, out string? pAddr) ? pAddr : $"Property #{_deal.PropertyId}";

            var lblValue = new Label
            {
                Text = $"₱{_deal.Value:N2}",
                Font = new Font("Segoe UI", 16f, FontStyle.Bold),
                ForeColor = Theme.TextPrimary,
                Location = new Point(88, 18),
                AutoSize = true
            };
            _pnlHeader.Controls.Add(lblValue);

            var lblMeta = new Label
            {
                Text = $"Deal #{_deal.DealId}   •   Buyer: {buyer}   •   {prop}",
                Font = new Font("Segoe UI", 9f, FontStyle.Regular),
                ForeColor = UiDetailCardHelper.LabelMutedColor,
                Location = new Point(88, 48),
                AutoSize = true,
                MaximumSize = new Size(380, 20)
            };
            _pnlHeader.Controls.Add(lblMeta);

            // Badges in Header
            var (sBg, sFg, sStroke) = UiDetailCardHelper.GetStatusColors(_deal.Stage);
            var stageBadge = UiDetailCardHelper.CreatePillBadge(_deal.Stage, sBg, sFg, sStroke);
            stageBadge.Name = "headerStageBadge";
            _pnlHeader.Controls.Add(stageBadge);

            var schemeBadge = UiDetailCardHelper.CreatePillBadge(
                string.IsNullOrWhiteSpace(_deal.PaymentScheme) ? "Cash" : _deal.PaymentScheme,
                Color.FromArgb(240, 253, 244),
                Color.FromArgb(22, 101, 52),
                Color.FromArgb(187, 247, 208));
            schemeBadge.Name = "headerSchemeBadge";
            _pnlHeader.Controls.Add(schemeBadge);

            Controls.Add(_pnlHeader);
        }

        private void BuildFooter()
        {
            _pnlFooter = new Panel
            {
                Dock = DockStyle.Bottom,
                Height = 62,
                BackColor = Color.White,
                Padding = new Padding(24, 12, 24, 12)
            };

            _pnlFooter.Paint += (s, e) =>
            {
                using var pen = new Pen(UiDetailCardHelper.BorderColor, 1f);
                e.Graphics.DrawLine(pen, 0, 0, _pnlFooter.Width, 0);
            };

            // Close Button
            _btnClose = new Button
            {
                Text = "Close",
                Size = new Size(88, 36),
                BackColor = Color.FromArgb(241, 245, 249),
                ForeColor = Color.FromArgb(30, 41, 59),
                Font = new Font("Segoe UI", 9.5f, FontStyle.Bold),
                DialogResult = DialogResult.OK
            };
            UiRadiusHelper.StyleButton(_btnClose, 8);
            UiRadiusHelper.AttachHoverFeedback(_btnClose, Color.FromArgb(241, 245, 249), Color.FromArgb(226, 232, 240));
            _pnlFooter.Controls.Add(_btnClose);

            // Edit Button (if permitted)
            bool canEdit = _deal != null && RbacService.CanEditRecord(_deal.AgentId, _deal.CreatedByUserId);
            if (canEdit)
            {
                _btnEdit = new Button
                {
                    Text = "✏️ Edit Deal & Terms",
                    Size = new Size(160, 36),
                    BackColor = Theme.Primary,
                    ForeColor = Color.White,
                    Font = new Font("Segoe UI", 9.5f, FontStyle.Bold)
                };
                UiRadiusHelper.StyleButton(_btnEdit, 8);
                UiRadiusHelper.AttachHoverFeedback(_btnEdit, Theme.Primary, Theme.PrimaryDark);
                _btnEdit.Click += BtnEditClick;
                _pnlFooter.Controls.Add(_btnEdit);
            }

            // View Contract & Terms Button
            _btnViewContract = new Button
            {
                Text = "📜 View Contract & Terms",
                Size = new Size(185, 36),
                BackColor = Color.White,
                ForeColor = Theme.Primary,
                Font = new Font("Segoe UI", 9.5f, FontStyle.Bold),
                Cursor = Cursors.Hand
            };
            UiRadiusHelper.StyleButton(_btnViewContract, 8);
            _btnViewContract.Paint += (s, e) =>
            {
                using var p = new Pen(UiDetailCardHelper.BorderColor, 1f);
                using var path = UiRadiusHelper.CreateRoundedPath(new Rectangle(0, 0, _btnViewContract.Width - 1, _btnViewContract.Height - 1), 8);
                e.Graphics.DrawPath(p, path);
            };
            UiRadiusHelper.AttachHoverFeedback(_btnViewContract, Color.White, Color.FromArgb(241, 245, 249));
            _btnViewContract.Click += BtnViewContractClick;
            _pnlFooter.Controls.Add(_btnViewContract);

            AcceptButton = _btnClose;
            CancelButton = _btnClose;
            Controls.Add(_pnlFooter);
        }

        private void BuildContent()
        {
            _pnlContent = new Panel
            {
                Dock = DockStyle.Fill,
                AutoScroll = true,
                BackColor = Color.FromArgb(244, 247, 251),
                Padding = new Padding(24, 18, 24, 80)
            };

            int currentY = 16;

            // --- Card 1: Pipeline Stage Progression ---
            var cardPipeline = CreateCardPanel(ref currentY);
            UiDetailCardHelper.AddControl(cardPipeline, UiDetailCardHelper.CreateCardHeader("📊  Deal Stage Progression"));
            UiDetailCardHelper.AddControl(cardPipeline, UiDetailCardHelper.CreateDivider());

            bool isLost = string.Equals(_deal!.Stage, "Lost", StringComparison.OrdinalIgnoreCase);
            if (isLost)
            {
                var lostBanner = new Panel
                {
                    Height = 56,
                    Dock = DockStyle.Top,
                    BackColor = Color.FromArgb(254, 242, 242),
                    Padding = new Padding(14, 10, 14, 10)
                };
                var lblLost = new Label
                {
                    Text = "⚠️  This deal has been marked as LOST and is no longer active in the pipeline.",
                    Font = new Font("Segoe UI", 9.5f, FontStyle.Bold),
                    ForeColor = Color.FromArgb(153, 27, 27),
                    Dock = DockStyle.Fill,
                    TextAlign = ContentAlignment.MiddleLeft
                };
                lostBanner.Controls.Add(lblLost);
                UiDetailCardHelper.AddControl(cardPipeline, lostBanner);
            }
            else
            {
                var stepper = CreatePipelineStepper();
                UiDetailCardHelper.AddControl(cardPipeline, stepper);
            }
            FinalizeCardHeight(cardPipeline, ref currentY);
            _pnlContent.Controls.Add(cardPipeline);

            // --- Card 2: Commercial Financial Terms ---
            var cardFinancials = CreateCardPanel(ref currentY);
            UiDetailCardHelper.AddControl(cardFinancials, UiDetailCardHelper.CreateCardHeader("💳  Commercial Terms & Financing Structure"));
            UiDetailCardHelper.AddControl(cardFinancials, UiDetailCardHelper.CreateDivider());

            // 4 summary tiles
            var pnlFinancialTiles = new Panel
            {
                Height = 68,
                Dock = DockStyle.Top,
                BackColor = Color.Transparent,
                Margin = new Padding(0, 0, 0, 10)
            };

            decimal resFee = _deal.ReservationFee ?? 0;
            decimal downAmt = _deal.DownPaymentAmount;
            decimal balAmt = _deal.BalanceAmount;

            AddSummaryTile(pnlFinancialTiles, 0, "TOTAL PURCHASE PRICE", $"₱{_deal.Value:N2}", Color.FromArgb(15, 23, 42));
            AddSummaryTile(pnlFinancialTiles, 164, "RESERVATION DEPOSIT", $"₱{resFee:N2}", Color.FromArgb(30, 41, 59));
            AddSummaryTile(pnlFinancialTiles, 328, $"DOWNPAYMENT ({_deal.DownPaymentPercent ?? 20:0.##}%)", $"₱{downAmt:N2}", Color.FromArgb(21, 128, 61));
            AddSummaryTile(pnlFinancialTiles, 492, "BALANCE TO FINANCE", $"₱{balAmt:N2}", Color.FromArgb(29, 78, 216));
            UiDetailCardHelper.AddControl(cardFinancials, pnlFinancialTiles);

            string closeDateStr = _deal.ExpectedCloseDate.HasValue ? _deal.ExpectedCloseDate.Value.ToString("MMMM d, yyyy") : "Not set";
            decimal commVal = _deal.Value * _deal.CommissionRate;
            UiDetailCardHelper.AddControl(cardFinancials, UiDetailCardHelper.CreateKeyValueRow(
                "Payment Scheme", _deal.PaymentScheme ?? "Spot Cash",
                "Target Closing Date", closeDateStr));
            UiDetailCardHelper.AddControl(cardFinancials, UiDetailCardHelper.CreateKeyValueRow(
                "Brokerage Commission", $"{_deal.CommissionRate:P1} (₱{commVal:N2})",
                "Contract Status", string.IsNullOrWhiteSpace(_deal.Stage) ? "Offer" : _deal.Stage));
            FinalizeCardHeight(cardFinancials, ref currentY);
            _pnlContent.Controls.Add(cardFinancials);

            // --- Card 3: Statutory Tax & Closing Cost Allocation ---
            var cardTaxes = CreateCardPanel(ref currentY);
            UiDetailCardHelper.AddControl(cardTaxes, UiDetailCardHelper.CreateCardHeader("⚖️  Statutory Tax & Closing Cost Allocation"));
            UiDetailCardHelper.AddControl(cardTaxes, UiDetailCardHelper.CreateDivider());

            UiDetailCardHelper.AddControl(cardTaxes, UiDetailCardHelper.CreateKeyValueRow(
                "Capital Gains Tax (6%)", $"Shouldered by {_deal.CgtPayer}",
                "Doc Stamp Tax (1.5%)", $"Shouldered by {_deal.DstPayer}"));
            UiDetailCardHelper.AddControl(cardTaxes, UiDetailCardHelper.CreateKeyValueRow(
                "Local Transfer Tax", $"Shouldered by {_deal.TransferTaxPayer}",
                "Title Registration Fees", $"Shouldered by {_deal.RegistrationFeePayer}"));
            FinalizeCardHeight(cardTaxes, ref currentY);
            _pnlContent.Controls.Add(cardTaxes);

            // --- Card 4: Closing Contingencies Tracker ---
            var contingencies = (_deal.Contingencies != null && _deal.Contingencies.Count > 0)
                ? _deal.Contingencies.OrderBy(c => c.DealContingencyId).ToList()
                : DealContingency.DeserializeList(_deal.ContingenciesJson);
            var cardContingencies = CreateCardPanel(ref currentY);
            UiDetailCardHelper.AddControl(cardContingencies, UiDetailCardHelper.CreateCardHeader("✅  Closing Contingencies & Conditions Precedent", contingencies.Count.ToString()));
            UiDetailCardHelper.AddControl(cardContingencies, UiDetailCardHelper.CreateDivider());

            if (contingencies.Count == 0)
            {
                var lblEmpty = new Label
                {
                    Text = "No closing contingencies logged for this deal.",
                    Font = new Font("Segoe UI", 9.5f, FontStyle.Italic),
                    ForeColor = UiDetailCardHelper.LabelMutedColor,
                    Dock = DockStyle.Top,
                    Height = 36,
                    TextAlign = ContentAlignment.MiddleLeft
                };
                UiDetailCardHelper.AddControl(cardContingencies, lblEmpty);
            }
            else
            {
                for (int i = 0; i < contingencies.Count; i++)
                {
                    var itemPanel = CreateContingencyItem(contingencies[i], i);
                    UiDetailCardHelper.AddControl(cardContingencies, itemPanel);
                }
            }
            FinalizeCardHeight(cardContingencies, ref currentY);
            _pnlContent.Controls.Add(cardContingencies);

            // --- Card 5: Contract Clauses & Special Stipulations ---
            var cardClauses = CreateCardPanel(ref currentY);
            UiDetailCardHelper.AddControl(cardClauses, UiDetailCardHelper.CreateCardHeader("📜  Agreed Brokerage Clauses & Special Stipulations"));
            UiDetailCardHelper.AddControl(cardClauses, UiDetailCardHelper.CreateDivider());

            var activeIds = (_deal.ApprovedClauseIds ?? "").Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            var allClauses = DealClauseLibrary.GetStandardClauses();
            var activeClauses = allClauses.Where(c => activeIds.Contains(c.Id)).ToList();
            if (activeClauses.Count == 0) activeClauses = allClauses.Where(c => c.IsDefaultSelected).ToList();

            foreach (var clause in activeClauses)
            {
                var clausePanel = CreateClauseItem(clause);
                UiDetailCardHelper.AddControl(cardClauses, clausePanel);
            }

            if (!string.IsNullOrWhiteSpace(_deal.SpecialStipulations))
            {
                var pnlStip = new Panel
                {
                    Dock = DockStyle.Top,
                    BackColor = Color.FromArgb(248, 250, 252),
                    Padding = new Padding(12, 10, 12, 10),
                    Margin = new Padding(0, 8, 0, 4),
                    AutoSize = true
                };
                pnlStip.Paint += (s, e) =>
                {
                    using var pen = new Pen(Color.FromArgb(147, 197, 253), 1f);
                    e.Graphics.DrawRectangle(pen, 0, 0, pnlStip.Width - 1, pnlStip.Height - 1);
                };
                var lblStipTitle = new Label
                {
                    Text = "SPECIAL STIPULATIONS & RIDERS",
                    Font = new Font("Segoe UI", 8.5f, FontStyle.Bold),
                    ForeColor = Color.FromArgb(29, 78, 216),
                    Dock = DockStyle.Top,
                    Height = 20
                };
                var lblStipBody = new Label
                {
                    Text = _deal.SpecialStipulations,
                    Font = new Font("Segoe UI", 9.5f),
                    ForeColor = UiDetailCardHelper.ValueTextColor,
                    Dock = DockStyle.Top,
                    AutoSize = true,
                    MaximumSize = new Size(580, 0)
                };
                pnlStip.Controls.Add(lblStipTitle);
                lblStipTitle.SendToBack();
                pnlStip.Controls.Add(lblStipBody);
                lblStipBody.SendToBack();
                UiDetailCardHelper.AddControl(cardClauses, pnlStip);
            }
            FinalizeCardHeight(cardClauses, ref currentY);
            _pnlContent.Controls.Add(cardClauses);

            Controls.Add(_pnlContent);
            _pnlContent.BringToFront();
        }

        private Panel CreatePipelineStepper()
        {
            int currentIndex = Array.FindIndex(DealStages,
                s => string.Equals(s, _deal?.Stage, StringComparison.OrdinalIgnoreCase));
            if (currentIndex < 0) currentIndex = 1; // Default to Reservation

            var panel = new Panel
            {
                Height = 74,
                Dock = DockStyle.Top,
                BackColor = Color.FromArgb(248, 250, 252),
                Margin = new Padding(0, 4, 0, 8),
                Padding = new Padding(16, 10, 16, 10)
            };

            panel.Paint += (s, e) =>
            {
                e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                e.Graphics.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;

                var borderRect = new Rectangle(0, 0, panel.Width - 1, panel.Height - 1);
                using var pPath = UiRadiusHelper.CreateRoundedPath(borderRect, 8);
                using var bPen = new Pen(UiDetailCardHelper.BorderColor, 1f);
                e.Graphics.DrawPath(bPen, pPath);

                int count = DealStages.Length;
                int startX = 60;
                int endX = panel.Width - 60;
                int lineY = 26;

                var xs = new int[count];
                for (int i = 0; i < count; i++)
                {
                    xs[i] = startX + (int)((long)(endX - startX) * i / (count - 1));
                }

                for (int i = 0; i < count - 1; i++)
                {
                    bool segDone = i < currentIndex;
                    using var linePen = new Pen(segDone ? Color.FromArgb(34, 197, 94) : Color.FromArgb(226, 232, 240), 3f);
                    e.Graphics.DrawLine(linePen, xs[i] + 14, lineY, xs[i + 1] - 14, lineY);
                }

                int circleSize = 28;
                for (int i = 0; i < count; i++)
                {
                    int cx = xs[i] - circleSize / 2;
                    int cy = lineY - circleSize / 2;
                    var circleRect = new Rectangle(cx, cy, circleSize, circleSize);

                    bool isPassed = i < currentIndex;
                    bool isCurrent = i == currentIndex;

                    if (isPassed)
                    {
                        using var brush = new SolidBrush(Color.FromArgb(34, 197, 94));
                        e.Graphics.FillEllipse(brush, circleRect);

                        using var font = new Font("Segoe UI", 9f, FontStyle.Bold);
                        using var tBrush = new SolidBrush(Color.White);
                        using var sf = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center };
                        e.Graphics.DrawString("✓", font, tBrush, circleRect, sf);
                    }
                    else if (isCurrent)
                    {
                        using var ringPen = new Pen(Color.FromArgb(191, 219, 254), 3f);
                        e.Graphics.DrawEllipse(ringPen, cx - 2, cy - 2, circleSize + 4, circleSize + 4);

                        using var brush = new SolidBrush(Theme.Primary);
                        e.Graphics.FillEllipse(brush, circleRect);

                        using var font = new Font("Segoe UI", 9f, FontStyle.Bold);
                        using var tBrush = new SolidBrush(Color.White);
                        using var sf = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center };
                        e.Graphics.DrawString((i + 1).ToString(), font, tBrush, circleRect, sf);
                    }
                    else
                    {
                        using var brush = new SolidBrush(Color.White);
                        e.Graphics.FillEllipse(brush, circleRect);

                        using var pen = new Pen(Color.FromArgb(203, 213, 225), 1.5f);
                        e.Graphics.DrawEllipse(pen, circleRect);

                        using var font = new Font("Segoe UI", 8.5f, FontStyle.Bold);
                        using var tBrush = new SolidBrush(Color.FromArgb(148, 163, 184));
                        using var sf = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center };
                        e.Graphics.DrawString((i + 1).ToString(), font, tBrush, circleRect, sf);
                    }

                    Color labelColor = isCurrent ? Theme.Primary : isPassed ? Color.FromArgb(22, 101, 52) : Color.FromArgb(100, 116, 139);
                    var labelFont = new Font("Segoe UI", 8.5f, isCurrent ? FontStyle.Bold : FontStyle.Regular);
                    using (var lBrush = new SolidBrush(labelColor))
                    using (var sf = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Near })
                    {
                        var textRect = new RectangleF(xs[i] - 50, lineY + 18, 100, 20);
                        e.Graphics.DrawString(DealStages[i], labelFont, lBrush, textRect, sf);
                    }
                }
            };

            UiRadiusHelper.ApplyRoundedCorners(panel, 8);
            return panel;
        }

        private static void AddSummaryTile(Panel parent, int x, string caption, string amount, Color textCol)
        {
            var tile = new Panel
            {
                Location = new Point(x, 2),
                Size = new Size(156, 62),
                BackColor = Color.FromArgb(248, 250, 252)
            };
            tile.Paint += (s, e) =>
            {
                using var p = new Pen(UiDetailCardHelper.BorderColor, 1f);
                using var path = UiRadiusHelper.CreateRoundedPath(new Rectangle(0, 0, tile.Width - 1, tile.Height - 1), 6);
                e.Graphics.DrawPath(p, path);
            };
            UiRadiusHelper.ApplyRoundedCorners(tile, 6);

            var lblCap = new Label
            {
                Text = caption,
                Font = new Font("Segoe UI", 7.5f, FontStyle.Bold),
                ForeColor = UiDetailCardHelper.LabelMutedColor,
                Location = new Point(8, 8),
                AutoSize = true
            };
            var lblAmt = new Label
            {
                Text = amount,
                Font = new Font("Segoe UI", 11.5f, FontStyle.Bold),
                ForeColor = textCol,
                Location = new Point(8, 28),
                AutoSize = true
            };
            tile.Controls.Add(lblCap);
            tile.Controls.Add(lblAmt);
            parent.Controls.Add(tile);
        }

        private Panel CreateContingencyItem(DealContingency c, int index)
        {
            var pnl = new Panel
            {
                Height = 52,
                Dock = DockStyle.Top,
                BackColor = Color.FromArgb(248, 250, 252),
                Margin = new Padding(0, 0, 0, 6),
                Padding = new Padding(12, 8, 12, 8)
            };
            pnl.Paint += (s, e) =>
            {
                using var p = new Pen(UiDetailCardHelper.BorderColor, 1f);
                using var path = UiRadiusHelper.CreateRoundedPath(new Rectangle(0, 0, pnl.Width - 1, pnl.Height - 1), 6);
                e.Graphics.DrawPath(p, path);
            };
            UiRadiusHelper.ApplyRoundedCorners(pnl, 6);

            var (cBg, cFg, cStroke) = UiDetailCardHelper.GetStatusColors(c.Status);
            var badge = UiDetailCardHelper.CreatePillBadge(c.Status, cBg, cFg, cStroke);
            badge.Location = new Point(10, 14);
            badge.Cursor = Cursors.Hand;
            badge.Click += (_, _) =>
            {
                string nextStatus = string.Equals(c.Status, "Satisfied", StringComparison.OrdinalIgnoreCase) ? "Pending" : "Satisfied";
                _controller.UpdateContingencyStatus(_deal!.DealId, index, nextStatus);
                _deal = _controller.GetById(_deal.DealId) ?? _deal;
                BuildUi();
            };
            pnl.Controls.Add(badge);

            var lblName = new Label
            {
                Text = c.Name,
                Font = new Font("Segoe UI", 9.5f, FontStyle.Bold),
                ForeColor = UiDetailCardHelper.ValueTextColor,
                Location = new Point(badge.Right + 12, 8),
                AutoSize = true
            };
            pnl.Controls.Add(lblName);

            string dueInfo = c.DueDate.HasValue ? $"Due by {c.DueDate.Value:MMM d, yyyy}" : "";
            if (c.ResolvedAt.HasValue) dueInfo += $" • Resolved on {c.ResolvedAt.Value:MMM d, yyyy}";
            if (!string.IsNullOrWhiteSpace(c.Notes)) dueInfo += $" • {c.Notes}";

            var lblDue = new Label
            {
                Text = dueInfo,
                Font = new Font("Segoe UI", 8.5f),
                ForeColor = UiDetailCardHelper.LabelMutedColor,
                Location = new Point(badge.Right + 12, 28),
                AutoSize = true
            };
            pnl.Controls.Add(lblDue);

            return pnl;
        }

        private static Panel CreateClauseItem(BrokerageClause clause)
        {
            var pnl = new Panel
            {
                Dock = DockStyle.Top,
                BackColor = Color.Transparent,
                Margin = new Padding(0, 0, 0, 8),
                AutoSize = true
            };

            var lblTitle = new Label
            {
                Text = $"[{clause.Id}]  {clause.Title}",
                Font = new Font("Segoe UI", 9.5f, FontStyle.Bold),
                ForeColor = UiDetailCardHelper.SectionTitleColor,
                Dock = DockStyle.Top,
                Height = 22
            };
            var lblText = new Label
            {
                Text = clause.ClauseText,
                Font = new Font("Segoe UI", 9f),
                ForeColor = Color.FromArgb(71, 85, 105),
                Dock = DockStyle.Top,
                AutoSize = true,
                MaximumSize = new Size(580, 0)
            };
            pnl.Controls.Add(lblText);
            pnl.Controls.Add(lblTitle);
            return pnl;
        }

        private Panel CreateCardPanel(ref int currentY)
        {
            var card = new Panel
            {
                Location = new Point(24, currentY),
                Width = Math.Max(500, ClientSize.Width - 48 - SystemInformation.VerticalScrollBarWidth),
                BackColor = UiDetailCardHelper.CardBg,
                Padding = new Padding(18, 14, 18, 14)
            };

            card.Paint += (s, e) =>
            {
                e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                var rect = new Rectangle(0, 0, card.Width - 1, card.Height - 1);
                using var path = UiRadiusHelper.CreateRoundedPath(rect, 10);
                using var pen = new Pen(UiDetailCardHelper.BorderColor, 1f);
                e.Graphics.DrawPath(pen, path);
            };

            UiRadiusHelper.ApplyRoundedCorners(card, 10);
            return card;
        }

        private void FinalizeCardHeight(Panel card, ref int currentY)
        {
            int totalHeight = card.Padding.Top;
            foreach (Control c in card.Controls)
            {
                totalHeight += c.Height + c.Margin.Bottom;
            }
            totalHeight += card.Padding.Bottom + 4;
            card.Height = totalHeight;

            currentY += totalHeight + 14;
        }

        private void LayoutResponsiveComponents()
        {
            if (_pnlHeader != null)
            {
                var stageBadge = _pnlHeader.Controls["headerStageBadge"];
                var schemeBadge = _pnlHeader.Controls["headerSchemeBadge"];

                int right = _pnlHeader.ClientSize.Width - 24;
                if (stageBadge != null)
                {
                    stageBadge.Location = new Point(right - stageBadge.Width, 22);
                    right -= (stageBadge.Width + 8);
                }
                if (schemeBadge != null)
                {
                    schemeBadge.Location = new Point(right - schemeBadge.Width, 22);
                }
            }

            if (_pnlFooter != null)
            {
                int right = _pnlFooter.ClientSize.Width - 24;
                if (_btnEdit != null && _btnEdit.Visible)
                {
                    _btnEdit.Location = new Point(right - _btnEdit.Width, 13);
                    right -= (_btnEdit.Width + 8);
                }
                if (_btnClose != null)
                {
                    _btnClose.Location = new Point(right - _btnClose.Width, 13);
                }
                if (_btnViewContract != null)
                {
                    _btnViewContract.Location = new Point(24, 13);
                }
            }

            if (_pnlContent != null)
            {
                int targetWidth = Math.Max(400, _pnlContent.ClientSize.Width - 48);
                foreach (Control c in _pnlContent.Controls)
                {
                    if (c is Panel card)
                    {
                        card.Width = targetWidth;
                    }
                }
            }
        }

        private void BtnEditClick(object? sender, EventArgs e)
        {
            if (_deal == null || !RbacService.CanEditRecord(_deal.AgentId, _deal.CreatedByUserId)) return;

            using var editForm = new DealInputForm(_controller, _deal);
            if (editForm.ShowDialog(this) == DialogResult.OK && editForm.Result is not null)
            {
                _controller.Update(editForm.Result);
                _deal = _controller.GetById(_deal.DealId) ?? editForm.Result;
                BuildUi();
            }
        }

        private void BtnViewContractClick(object? sender, EventArgs e)
        {
            if (_deal == null) return;

            using var viewer = new ContractTermsViewerDialog(_deal, _controller);
            viewer.ShowDialog(this);
        }
    }
}
