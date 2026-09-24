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
using CRMS_Peguit.winforms.Views.Shared;

namespace CRMS_Peguit.winforms.Views.SupportTickets
{
    public partial class SupportTicketDetailForm : Form
    {
        private SupportTicket _ticket;
        private readonly SupportTicketController _controller;

        private Panel? _pnlHeader;
        private Panel? _pnlFooter;
        private Panel? _pnlContent;
        private Button? _btnUpdateStatus;
        private Button? _btnReassign;
        private Button? _btnReopen;
        private Button? _btnClose;
        private TextBox? _txtNewComment;

        public SupportTicketDetailForm(SupportTicket ticket, SupportTicketController controller)
        {
            _ticket = ticket;
            _controller = controller;
            InitializeComponent();
            SetupFormProperties();
            BuildUi();
        }

        private void SetupFormProperties()
        {
            Text = $"Ticket {_ticket.TicketNumber} — {_ticket.Category}";
            Size = new Size(820, 720);
            MinimumSize = new Size(680, 540);
            StartPosition = FormStartPosition.CenterParent;
            BackColor = Theme.Background;
            DoubleBuffered = true;
            AppBrand.ApplyDarkTitleBar(this);
            AppBrand.ApplyAppIcon(this);
        }

        private void BuildUi()
        {
            Controls.Clear();

            // 1. Bottom footer
            BuildFooter();

            // 2. Header
            BuildHeader();

            // 3. Scrollable content
            BuildContent();

            Resize += (_, _) => LayoutResponsiveComponents();
            LayoutResponsiveComponents();
        }

        private void BuildHeader()
        {
            _pnlHeader = new Panel
            {
                Dock = DockStyle.Top,
                Height = 92,
                BackColor = Theme.Surface,
                Padding = new Padding(24, 16, 24, 16)
            };

            _pnlHeader.Paint += (s, e) =>
            {
                using var pen = new Pen(Theme.Border, 1f);
                e.Graphics.DrawLine(pen, 0, _pnlHeader.Height - 1, _pnlHeader.Width, _pnlHeader.Height - 1);
            };

            // Avatar circle / Ticket Badge
            var lblIcon = new Label
            {
                Text = "TCK",
                Font = new Font("Segoe UI", 12f, FontStyle.Bold),
                ForeColor = Theme.PrimaryDark,
                Size = new Size(52, 52),
                Location = new Point(24, 18),
                TextAlign = ContentAlignment.MiddleCenter,
                BackColor = Theme.PrimaryLight
            };
            UiRadiusHelper.ApplyRoundedCorners(lblIcon, 26);
            _pnlHeader.Controls.Add(lblIcon);

            // Title
            var lblTitle = new Label
            {
                Text = $"{_ticket.TicketNumber}: {_ticket.Category}",
                Font = new Font("Segoe UI", 15.5f, FontStyle.Bold),
                ForeColor = Theme.TextPrimary,
                Location = new Point(88, 16),
                AutoSize = true
            };
            _pnlHeader.Controls.Add(lblTitle);

            string custName = _ticket.Customer?.FullName ?? "Unknown Client";
            string createdStr = _ticket.CreatedAt != default ? _ticket.CreatedAt.ToLocalTime().ToString("MMM dd, yyyy hh:mm tt") : "N/A";
            var lblMeta = new Label
            {
                Text = $"Customer: {custName}   •   Logged: {createdStr}",
                Font = new Font("Segoe UI", 9f),
                ForeColor = Theme.TextSecondary,
                Location = new Point(88, 48),
                AutoSize = true
            };
            _pnlHeader.Controls.Add(lblMeta);

            // Status Badge
            var (sBg, sFg, sStroke) = UiDetailCardHelper.GetStatusColors(_ticket.Status);
            var statusBadge = UiDetailCardHelper.CreateStatusIndicator(UiDetailCardHelper.ToTitleCase(_ticket.Status), sFg);
            statusBadge.Name = "headerStatusBadge";
            _pnlHeader.Controls.Add(statusBadge);

            // Priority Badge
            var (pBg, pFg, pStroke) = UiDetailCardHelper.GetPriorityColors(_ticket.Priority);
            var priorityBadge = UiDetailCardHelper.CreateStatusIndicator(UiDetailCardHelper.ToTitleCase(_ticket.Priority), pFg);
            priorityBadge.Name = "headerPriorityBadge";
            _pnlHeader.Controls.Add(priorityBadge);

            Controls.Add(_pnlHeader);
        }

        private void BuildFooter()
        {
            _pnlFooter = new Panel
            {
                Dock = DockStyle.Bottom,
                Height = 62,
                BackColor = Theme.Surface,
                Padding = new Padding(24, 12, 24, 12)
            };

            _pnlFooter.Paint += (s, e) =>
            {
                using var pen = new Pen(Theme.Border, 1f);
                e.Graphics.DrawLine(pen, 0, 0, _pnlFooter.Width, 0);
            };

            // Close
            _btnClose = new Button
            {
                Text = "Close",
                Size = new Size(88, 36),
                BackColor = Theme.Surface,
                ForeColor = Theme.TextPrimary,
                Font = new Font("Segoe UI", 9.5f, FontStyle.Bold),
                DialogResult = DialogResult.OK
            };
            UiRadiusHelper.StyleButton(_btnClose, 8);
            _btnClose.FlatAppearance.BorderColor = Theme.Border;
            _pnlFooter.Controls.Add(_btnClose);

            // Reassign (Manager / Admin ONLY) - standard secondary action button
            if (RbacService.CanAssignRecords)
            {
                _btnReassign = new Button
                {
                    Text = "Reassign Agent",
                    Size = new Size(136, 36),
                    BackColor = Color.White,
                    ForeColor = Theme.TextPrimary,
                    Font = new Font("Segoe UI", 9.5f, FontStyle.Bold)
                };
                UiRadiusHelper.StyleButton(_btnReassign, 8);
                _btnReassign.FlatAppearance.BorderColor = UiDetailCardHelper.BorderColor;
                UiRadiusHelper.AttachHoverFeedback(_btnReassign, Color.White, Color.FromArgb(241, 245, 249));
                _btnReassign.Click += BtnReassignClick;
                _pnlFooter.Controls.Add(_btnReassign);
            }

            // Update Status (Manager or owning Agent, if not Resolved)
            bool isResolved = string.Equals(_ticket.Status, "Resolved", StringComparison.OrdinalIgnoreCase);
            bool canEdit = _controller.CanUserEditTicket(_ticket);

            if (canEdit && !isResolved)
            {
                _btnUpdateStatus = new Button
                {
                    Text = "Update Status",
                    Size = new Size(130, 36),
                    BackColor = Theme.Primary,
                    ForeColor = Color.White,
                    Font = new Font("Segoe UI", 9.5f, FontStyle.Bold)
                };
                UiRadiusHelper.StyleButton(_btnUpdateStatus, 8);
                UiRadiusHelper.AttachHoverFeedback(_btnUpdateStatus, Theme.Primary, Theme.PrimaryDark);
                _btnUpdateStatus.Click += BtnUpdateStatusClick;
                _pnlFooter.Controls.Add(_btnUpdateStatus);
            }

            // Reopen Button (if resolved and user can edit)
            if (canEdit && isResolved)
            {
                _btnReopen = new Button
                {
                    Text = "Reopen Ticket",
                    Size = new Size(130, 36),
                    BackColor = Color.FromArgb(234, 88, 12), // Orange/Amber
                    ForeColor = Color.White,
                    Font = new Font("Segoe UI", 9.5f, FontStyle.Bold)
                };
                UiRadiusHelper.StyleButton(_btnReopen, 8);
                _btnReopen.Click += BtnReopenClick;
                _pnlFooter.Controls.Add(_btnReopen);
            }

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
                BackColor = Theme.Background,
                Padding = new Padding(24, 18, 24, 80)
            };

            int currentY = 16;
            bool isOverdue = !string.Equals(_ticket.Status, "Resolved", StringComparison.OrdinalIgnoreCase)
                             && _ticket.DueDate.HasValue && _ticket.DueDate.Value < DateTime.UtcNow;

            // --- Overdue Banner if applicable ---
            if (isOverdue)
            {
                var banner = new Panel
                {
                    Location = new Point(24, currentY),
                    Width = Math.Max(500, ClientSize.Width - 48 - SystemInformation.VerticalScrollBarWidth),
                    Height = 44,
                    BackColor = Color.FromArgb(254, 242, 242), // Light Red
                    Padding = new Padding(16, 10, 16, 10)
                };
                banner.Paint += (s, e) =>
                {
                    using var p = new Pen(Color.FromArgb(252, 165, 165), 1f);
                    e.Graphics.DrawRectangle(p, 0, 0, banner.Width - 1, banner.Height - 1);
                };
                UiRadiusHelper.ApplyRoundedCorners(banner, 8);

                var lblWarning = new Label
                {
                    Text = $"SLA OVERDUE: Target resolution deadline was {_ticket.DueDate!.Value.ToLocalTime():MMM dd, yyyy hh:mm tt}. Immediate attention required.",
                    Font = new Font("Segoe UI", 9.5f, FontStyle.Bold),
                    ForeColor = Theme.Danger,
                    Dock = DockStyle.Fill,
                    TextAlign = ContentAlignment.MiddleLeft
                };
                banner.Controls.Add(lblWarning);
                _pnlContent.Controls.Add(banner);
                currentY += banner.Height + 14;
            }

            // --- Card 1: Ticket Overview & SLA Details ---
            var cardSla = CreateCardPanel(ref currentY);
            UiDetailCardHelper.AddControl(cardSla, UiDetailCardHelper.CreateCardHeader("🕒  Ticket SLA Target Details"));
            UiDetailCardHelper.AddControl(cardSla, UiDetailCardHelper.CreateDivider());

            string dueText = _ticket.DueDate.HasValue
                ? $"{_ticket.DueDate.Value.ToLocalTime():MMM dd, yyyy hh:mm tt} {(isOverdue ? "(OVERDUE)" : "")}"
                : "No deadline specified";

            string firstResp = _ticket.FirstRespondedAt.HasValue
                ? _ticket.FirstRespondedAt.Value.ToLocalTime().ToString("MMM dd, yyyy hh:mm tt")
                : "Pending first response";

            string resolvedStr = _ticket.ResolvedAt.HasValue
                ? _ticket.ResolvedAt.Value.ToLocalTime().ToString("MMM dd, yyyy hh:mm tt")
                : "Not resolved yet";

            UiDetailCardHelper.AddControl(cardSla, UiDetailCardHelper.CreateKeyValueRow(
                "Category", UiDetailCardHelper.ToTitleCase(_ticket.Category),
                "Priority (SLA)", $"{UiDetailCardHelper.ToTitleCase(_ticket.Priority)} ({GetSlaWindowDescription(_ticket.Priority)})"));

            UiDetailCardHelper.AddControl(cardSla, UiDetailCardHelper.CreateKeyValueRow(
                "SLA Resolution Deadline", dueText,
                "Ticket Status", UiDetailCardHelper.ToTitleCase(_ticket.Status)));

            UiDetailCardHelper.AddControl(cardSla, UiDetailCardHelper.CreateKeyValueRow(
                "First Responded At", firstResp,
                "Resolved At", resolvedStr));

            FinalizeCardHeight(cardSla, ref currentY);
            _pnlContent.Controls.Add(cardSla);

            // --- Card 2: Requester Ownership ---
            var cardOwner = CreateCardPanel(ref currentY);
            UiDetailCardHelper.AddControl(cardOwner, UiDetailCardHelper.CreateCardHeader("👤  Requester Ownership"));
            UiDetailCardHelper.AddControl(cardOwner, UiDetailCardHelper.CreateDivider());

            string custName = _ticket.Customer?.FullName != null ? UiDetailCardHelper.ToTitleCase(_ticket.Customer.FullName) : "Unknown";
            string custEmail = _ticket.Customer?.Email ?? "No email";
            string custPhone = _ticket.Customer?.Phone ?? "No phone";
            string raisedBy = _ticket.RaisedByUser?.FullName != null ? UiDetailCardHelper.ToTitleCase(_ticket.RaisedByUser.FullName) : $"User #{_ticket.RaisedByUserId}";
            string? assignedAgent = _controller.GetAssignedAgentName(_ticket.AssignedToUserId);
            string assignedAgentDisplay = assignedAgent != null ? UiDetailCardHelper.ToTitleCase(assignedAgent) : "Unassigned (Pending)";

            UiDetailCardHelper.AddControl(cardOwner, UiDetailCardHelper.CreateKeyValueRow(
                "Client / Requester", $"{custName} ({custEmail})",
                "Client Contact", custPhone));

            UiDetailCardHelper.AddControl(cardOwner, UiDetailCardHelper.CreateKeyValueRow(
                "Logged By", raisedBy,
                "Assigned Agent", assignedAgentDisplay));

            FinalizeCardHeight(cardOwner, ref currentY);
            _pnlContent.Controls.Add(cardOwner);

            // --- Card 3: Ticket Description ---
            var cardDesc = CreateCardPanel(ref currentY);
            UiDetailCardHelper.AddControl(cardDesc, UiDetailCardHelper.CreateCardHeader("⚠️  Initial Issue Description"));
            UiDetailCardHelper.AddControl(cardDesc, UiDetailCardHelper.CreateDivider());

            var lblDesc = new Label
            {
                Text = string.IsNullOrWhiteSpace(_ticket.Description) ? "No description provided." : _ticket.Description,
                Font = new Font("Segoe UI", 10f),
                ForeColor = Theme.TextPrimary,
                Dock = DockStyle.Top,
                AutoSize = true,
                MaximumSize = new Size(Math.Max(400, ClientSize.Width - 100), 0),
                Margin = new Padding(0, 4, 0, 10)
            };
            UiDetailCardHelper.AddControl(cardDesc, lblDesc);

            FinalizeCardHeight(cardDesc, ref currentY);
            _pnlContent.Controls.Add(cardDesc);

            // --- Card 4: Comments & Internal Notes Thread ---
            var comments = _controller.GetComments(_ticket.TicketId);
            var cardComments = CreateCardPanel(ref currentY);
            UiDetailCardHelper.AddControl(cardComments, UiDetailCardHelper.CreateCardHeader("💬  Comment & Reply Thread", comments.Count(c => c.CommentType == "Comment").ToString()));
            UiDetailCardHelper.AddControl(cardComments, UiDetailCardHelper.CreateDivider());

            var userComments = comments.Where(c => c.CommentType == "Comment").ToList();
            if (userComments.Count == 0)
            {
                var lblEmpty = new Label
                {
                    Text = "No comments or replies posted yet. Use the box below to add notes.",
                    Font = new Font("Segoe UI", 9.5f, FontStyle.Italic),
                    ForeColor = Theme.TextSecondary,
                    Dock = DockStyle.Top,
                    Height = 36,
                    TextAlign = ContentAlignment.MiddleLeft
                };
                UiDetailCardHelper.AddControl(cardComments, lblEmpty);
            }
            else
            {
                foreach (var c in userComments)
                {
                    var commentBubble = CreateCommentTile(c);
                    UiDetailCardHelper.AddControl(cardComments, commentBubble);
                }
            }

            // Reply input panel positioned below comments/empty notice
            var replyPanel = new Panel
            {
                Dock = DockStyle.Top,
                Height = 84,
                BackColor = Theme.Background,
                Padding = new Padding(10, 8, 10, 8),
                Margin = new Padding(0, 10, 0, 8)
            };
            UiRadiusHelper.ApplyRoundedCorners(replyPanel, 8);

            _txtNewComment = new TextBox
            {
                Dock = DockStyle.Fill,
                Multiline = true,
                Font = new Font("Segoe UI", 9.5f),
                PlaceholderText = "Write an internal note or reply to this ticket...",
                ScrollBars = System.Windows.Forms.ScrollBars.Vertical,
                BackColor = Color.White
            };
            UiRadiusHelper.SetPadding(_txtNewComment, 8, 8);

            var btnPostComment = new Button
            {
                Text = "Post Reply",
                Dock = DockStyle.Right,
                Width = 100,
                BackColor = Theme.Primary,
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 9f, FontStyle.Bold),
                Cursor = Cursors.Hand
            };
            UiRadiusHelper.StyleButton(btnPostComment, 6);
            btnPostComment.Click += BtnPostCommentClick;

            replyPanel.Controls.Add(_txtNewComment);
            replyPanel.Controls.Add(btnPostComment);
            UiDetailCardHelper.AddControl(cardComments, replyPanel);

            FinalizeCardHeight(cardComments, ref currentY);
            _pnlContent.Controls.Add(cardComments);

            // --- Card 5: Full Status & Reassignment Audit Trail ---
            var auditEvents = comments.Where(c => c.CommentType != "Comment").OrderByDescending(c => c.CreatedAt).ToList();
            var cardHistory = CreateCardPanel(ref currentY);
            UiDetailCardHelper.AddControl(cardHistory, UiDetailCardHelper.CreateCardHeader("Status & Assignment History", auditEvents.Count.ToString()));
            UiDetailCardHelper.AddControl(cardHistory, UiDetailCardHelper.CreateDivider());

            if (auditEvents.Count == 0)
            {
                var lblEmptyHistory = new Label
                {
                    Text = "No prior status changes or reassignments recorded.",
                    Font = new Font("Segoe UI", 9.5f, FontStyle.Italic),
                    ForeColor = Theme.TextSecondary,
                    Dock = DockStyle.Top,
                    Height = 36,
                    TextAlign = ContentAlignment.MiddleLeft
                };
                UiDetailCardHelper.AddControl(cardHistory, lblEmptyHistory);
            }
            else
            {
                foreach (var a in auditEvents)
                {
                    var auditTile = CreateAuditTile(a);
                    UiDetailCardHelper.AddControl(cardHistory, auditTile);
                }
            }

            FinalizeCardHeight(cardHistory, ref currentY);
            _pnlContent.Controls.Add(cardHistory);

            Controls.Add(_pnlContent);
            _pnlContent.BringToFront();
        }

        private Panel CreateCommentTile(TicketComment comment)
        {
            var tile = new Panel
            {
                Dock = DockStyle.Top,
                BackColor = Color.FromArgb(248, 250, 252),
                Padding = new Padding(12, 10, 12, 10),
                Margin = new Padding(0, 0, 0, 10)
            };

            string author = comment.AuthorUser?.FullName ?? $"User #{comment.AuthorUserId}";
            string role = comment.AuthorUser?.Role?.RoleName ?? "Staff";
            string dateStr = comment.CreatedAt.ToLocalTime().ToString("MMM dd, yyyy hh:mm tt");

            var lblAuthor = new Label
            {
                Text = $"{author}  [{role}]  •  {dateStr}",
                Font = new Font("Segoe UI", 8.5f, FontStyle.Bold),
                ForeColor = Theme.PrimaryDark,
                Dock = DockStyle.Top,
                Height = 20
            };

            var lblText = new Label
            {
                Text = comment.CommentText,
                Font = new Font("Segoe UI", 9.5f),
                ForeColor = Theme.TextPrimary,
                Dock = DockStyle.Top,
                AutoSize = true,
                Padding = new Padding(0, 4, 0, 4)
            };

            tile.Controls.Add(lblText);
            tile.Controls.Add(lblAuthor);

            int calculatedHeight = 20 + TextRenderer.MeasureText(comment.CommentText, lblText.Font, new Size(Math.Max(400, ClientSize.Width - 120), 0), TextFormatFlags.WordBreak).Height + 24;
            tile.Height = Math.Max(58, calculatedHeight);

            tile.Paint += (s, e) =>
            {
                using var p = new Pen(Theme.Border, 1f);
                var rect = new Rectangle(0, 0, tile.Width - 1, tile.Height - 1);
                using var path = UiRadiusHelper.CreateRoundedPath(rect, 6);
                e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                e.Graphics.DrawPath(p, path);
            };

            return tile;
        }

        private Panel CreateAuditTile(TicketComment audit)
        {
            var tile = new Panel
            {
                Dock = DockStyle.Top,
                Height = 48,
                BackColor = Color.White,
                Padding = new Padding(8, 6, 8, 6),
                Margin = new Padding(0, 0, 0, 6)
            };

            Color dotColor = audit.CommentType switch
            {
                "Assignment" => Theme.Primary,
                "Reopened" => Color.FromArgb(234, 88, 12),
                _ => Theme.Success
            };

            var dotPanel = new Panel
            {
                Size = new Size(10, 10),
                Location = new Point(12, 18),
                BackColor = dotColor
            };
            UiRadiusHelper.ApplyRoundedCorners(dotPanel, 5);

            var lblDesc = new Label
            {
                Text = audit.CommentText,
                Font = new Font("Segoe UI", 9f, FontStyle.Regular),
                ForeColor = Theme.TextPrimary,
                Location = new Point(32, 6),
                AutoSize = true
            };

            var lblDate = new Label
            {
                Text = audit.CreatedAt.ToLocalTime().ToString("MMM dd, yyyy hh:mm tt"),
                Font = new Font("Segoe UI", 8f),
                ForeColor = Theme.TextSecondary,
                Location = new Point(32, 26),
                AutoSize = true
            };

            tile.Controls.Add(dotPanel);
            tile.Controls.Add(lblDesc);
            tile.Controls.Add(lblDate);

            tile.Paint += (s, e) =>
            {
                using var p = new Pen(Color.FromArgb(241, 245, 249), 1f);
                e.Graphics.DrawLine(p, 0, tile.Height - 1, tile.Width, tile.Height - 1);
            };

            return tile;
        }

        private Panel CreateCardPanel(ref int currentY)
        {
            var card = new Panel
            {
                Location = new Point(24, currentY),
                Width = Math.Max(500, ClientSize.Width - 48 - SystemInformation.VerticalScrollBarWidth),
                BackColor = Theme.Surface,
                Padding = new Padding(18, 14, 18, 14)
            };

            card.Paint += (s, e) =>
            {
                e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                var rect = new Rectangle(0, 0, card.Width - 1, card.Height - 1);
                using var path = UiRadiusHelper.CreateRoundedPath(rect, 10);
                using var pen = new Pen(Theme.Border, 1f);
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
            if (_pnlHeader is not null)
            {
                var statusBadge = _pnlHeader.Controls["headerStatusBadge"];
                var priorityBadge = _pnlHeader.Controls["headerPriorityBadge"];

                int rightEdge = _pnlHeader.Width - 24;
                if (statusBadge is not null)
                {
                    statusBadge.Location = new Point(rightEdge - statusBadge.Width, 32);
                    rightEdge = statusBadge.Left - 10;
                }
                if (priorityBadge is not null)
                {
                    priorityBadge.Location = new Point(rightEdge - priorityBadge.Width, 32);
                }
            }

            if (_pnlFooter is not null)
            {
                int rightEdge = _pnlFooter.Width - 24;

                // 1. Primary action (Update Status or Reopen) rightmost
                if (_btnUpdateStatus is not null && _btnUpdateStatus.Visible)
                {
                    _btnUpdateStatus.Location = new Point(rightEdge - _btnUpdateStatus.Width, 14);
                    rightEdge = _btnUpdateStatus.Left - 8;
                }
                else if (_btnReopen is not null && _btnReopen.Visible)
                {
                    _btnReopen.Location = new Point(rightEdge - _btnReopen.Width, 14);
                    rightEdge = _btnReopen.Left - 8;
                }

                // 2. Close button to the left of primary
                if (_btnClose is not null)
                {
                    _btnClose.Location = new Point(rightEdge - _btnClose.Width, 14);
                    rightEdge = _btnClose.Left - 8;
                }

                // 3. Secondary actions (e.g. Reassign)
                if (_btnReassign is not null && _btnReassign.Visible)
                {
                    _btnReassign.Location = new Point(rightEdge - _btnReassign.Width, 14);
                }
            }
        }

        private void BtnPostCommentClick(object? sender, EventArgs e)
        {
            if (_txtNewComment is null) return;
            string text = _txtNewComment.Text.Trim();
            if (string.IsNullOrWhiteSpace(text)) return;

            try
            {
                _controller.AddComment(_ticket.TicketId, text, isInternal: true);
                _txtNewComment.Clear();
                ReloadTicketData();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Error Posting Comment", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void BtnUpdateStatusClick(object? sender, EventArgs e)
        {
            var menu = new ContextMenuStrip();
            if (string.Equals(_ticket.Status, "Open", StringComparison.OrdinalIgnoreCase))
            {
                menu.Items.Add("Mark In Progress", null, (_, _) => ExecuteStatusChange("In Progress"));
                menu.Items.Add("Resolve Ticket", null, (_, _) => ExecuteStatusChange("Resolved"));
            }
            else if (string.Equals(_ticket.Status, "In Progress", StringComparison.OrdinalIgnoreCase))
            {
                menu.Items.Add("Resolve Ticket", null, (_, _) => ExecuteStatusChange("Resolved"));
            }

            if (menu.Items.Count > 0 && _btnUpdateStatus is not null)
            {
                menu.Show(_btnUpdateStatus, new Point(0, -menu.Height));
            }
        }

        private void ExecuteStatusChange(string newStatus)
        {
            try
            {
                _controller.UpdateStatus(_ticket.TicketId, newStatus);
                ReloadTicketData();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Status Update Failed", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        private void BtnReopenClick(object? sender, EventArgs e)
        {
            using var prompt = new Form
            {
                Text = "Reopen Support Ticket",
                Size = new Size(420, 220),
                StartPosition = FormStartPosition.CenterParent,
                FormBorderStyle = FormBorderStyle.FixedDialog,
                MaximizeBox = false,
                MinimizeBox = false,
                BackColor = Theme.Surface
            };

            var lbl = new Label { Text = "Reason for reopening ticket:", Location = new Point(20, 16), AutoSize = true, Font = new Font("Segoe UI", 9.5f, FontStyle.Bold) };
            var txtReason = new TextBox { Location = new Point(20, 42), Width = 360, Height = 60, Multiline = true };
            var btnOk = new Button { Text = "Reopen", Location = new Point(210, 120), Width = 80, Height = 32, DialogResult = DialogResult.OK, BackColor = Theme.Primary, ForeColor = Color.White };
            var btnCan = new Button { Text = "Cancel", Location = new Point(300, 120), Width = 80, Height = 32, DialogResult = DialogResult.Cancel };
            UiRadiusHelper.StyleButton(btnOk, 6);
            UiRadiusHelper.StyleButton(btnCan, 6);

            prompt.Controls.AddRange(new Control[] { lbl, txtReason, btnOk, btnCan });
            prompt.AcceptButton = btnOk;
            prompt.CancelButton = btnCan;

            if (prompt.ShowDialog(this) == DialogResult.OK)
            {
                string reason = txtReason.Text.Trim();
                if (string.IsNullOrWhiteSpace(reason))
                    reason = "Customer requested additional assistance.";

                try
                {
                    _controller.Reopen(_ticket.TicketId, reason);
                    ReloadTicketData();
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.Message, "Reopen Failed", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                }
            }
        }

        private void BtnReassignClick(object? sender, EventArgs e)
        {
            using var dlg = new AssignAgentDialog(_ticket.TicketNumber, _controller.GetAgents(), _ticket.AssignedToUserId);
            if (dlg.ShowDialog(this) == DialogResult.OK)
            {
                try
                {
                    _controller.AssignTo(_ticket.TicketId, dlg.SelectedAgentId, dlg.ReviewNotes);
                    ReloadTicketData();
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.Message, "Reassignment Failed", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                }
            }
        }

        private void ReloadTicketData()
        {
            var refreshed = _controller.GetById(_ticket.TicketId);
            if (refreshed is not null)
            {
                _ticket = refreshed;
                BuildUi();
            }
        }

        private static string GetSlaWindowDescription(string? priority)
        {
            return (priority?.ToLowerInvariant()) switch
            {
                "high" or "urgent" => "24-Hour Resolution Target",
                "medium" => "3-Day Resolution Target",
                "low" => "7-Day Resolution Target",
                _ => "3-Day Resolution Target"
            };
        }
    }
}
