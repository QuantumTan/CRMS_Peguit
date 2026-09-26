using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Linq;
using System.Windows.Forms;
using CRMS_Peguit.domain.Common;
using CRMS_Peguit.domain.entities;
using CRMS_Peguit.winforms.Auth;
using CRMS_Peguit.winforms.Services;
using CRMS_Peguit.winforms.Models.Services;
using CRMS_Peguit.winforms.Views.FollowUps;
using CRMS_Peguit.winforms.Views.Shared;

namespace CRMS_Peguit.winforms.Views.Customers
{
    public partial class CustomerDetailForm : Form
    {
        private Customer? _customer;
        private readonly CustomerApiService? _controller;
        private string _timelineFilter = "All";

        private Panel? _pnlHeader;
        private Panel? _pnlFooter;
        private Panel? _pnlContent;
        private Button? _btnEdit;
        private Button? _btnMessage;
        private Button? _btnClose;

        public CustomerDetailForm()
        {
            InitializeComponent();
        }

        public CustomerDetailForm(Customer customer, CustomerApiService controller)
        {
            _customer = customer;
            _controller = controller;
            InitializeComponent();
            SetupFormProperties();
            BuildUi();
        }

        private void SetupFormProperties()
        {
            string displayName = _customer is not null ? UiDetailCardHelper.ToTitleCase(_customer.FullName) : "Customer Details";
            Text = _customer is not null ? $"Customer Details - {displayName}" : "Customer Details";
            Size = new Size(760, 680);
            MinimumSize = new Size(620, 500);
            StartPosition = FormStartPosition.CenterParent;
            BackColor = Color.FromArgb(244, 247, 251);
            DoubleBuffered = true;
            AppBrand.ApplyDarkTitleBar(this);
            AppBrand.ApplyAppIcon(this);
        }

        private void BuildUi()
        {
            Controls.Clear();
            if (_customer == null || _controller == null) return;

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

            string formattedName = UiDetailCardHelper.ToTitleCase(_customer!.FullName);

            // Avatar circle (centered with ample margins)
            var lblAvatar = new Label
            {
                Text = UiDetailCardHelper.GetInitials(formattedName),
                Font = new Font("Segoe UI", 15f, FontStyle.Bold),
                ForeColor = Color.FromArgb(29, 78, 216),      // #1D4ED8
                BackColor = Color.FromArgb(239, 246, 255),    // #EFF6FF
                Size = new Size(54, 54),
                Location = new Point(24, 17),
                TextAlign = ContentAlignment.MiddleCenter,
                Margin = new Padding(0)
            };
            UiRadiusHelper.MakeCircularAvatar(lblAvatar);
            _pnlHeader.Controls.Add(lblAvatar);

            // Title & Subtitle block
            var lblName = new Label
            {
                Text = formattedName,
                Font = new Font("Segoe UI", 15.5f, FontStyle.Bold),
                ForeColor = Theme.TextPrimary,
                Location = new Point(88, 18),
                AutoSize = true
            };
            _pnlHeader.Controls.Add(lblName);

            var regDate = _customer.CreatedAt != default ? _customer.CreatedAt.ToString("MMM d, yyyy") : "N/A";
            var lblMeta = new Label
            {
                Text = $"Customer #{_customer.CustomerId}   •   Customer Since {regDate}",
                Font = new Font("Segoe UI", 9f, FontStyle.Regular),
                ForeColor = UiDetailCardHelper.LabelMutedColor,
                Location = new Point(88, 48),
                AutoSize = true
            };
            _pnlHeader.Controls.Add(lblMeta);

            // Badges in Header
            var (sBg, sFg, sStroke) = UiDetailCardHelper.GetStatusColors(_customer.Status);
            var statusBadge = UiDetailCardHelper.CreateStatusIndicator(UiDetailCardHelper.ToTitleCase(_customer.Status), sFg);
            statusBadge.Name = "headerStatusBadge";
            _pnlHeader.Controls.Add(statusBadge);

            string typeStr = UiDetailCardHelper.ToTitleCase(_customer.Type);
            var typeBadge = UiDetailCardHelper.CreateStatusIndicator(typeStr, Color.FromArgb(51, 65, 85));
            typeBadge.Name = "headerTypeBadge";
            _pnlHeader.Controls.Add(typeBadge);

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
            bool canEdit = _customer is not null && RbacService.CanEditRecord(_customer.AssignedAgentId, _customer.CreatedByUserId, _customer.AssignmentStatus);
            if (canEdit)
            {
                _btnEdit = new Button
                {
                    Text = "✏️ Edit Customer",
                    Size = new Size(130, 36),
                    BackColor = Theme.Primary,
                    ForeColor = Color.White,
                    Font = new Font("Segoe UI", 9.5f, FontStyle.Bold)
                };
                UiRadiusHelper.StyleButton(_btnEdit, 8);
                UiRadiusHelper.AttachHoverFeedback(_btnEdit, Theme.Primary, Theme.PrimaryDark);
                _btnEdit.Click += BtnEditClick;
                _pnlFooter.Controls.Add(_btnEdit);
            }

            // Send Email Button
            bool hasEmail = !string.IsNullOrWhiteSpace(_customer?.Email);
            _btnMessage = new Button
            {
                Text = "✉️ Send Email",
                Size = new Size(118, 36),
                BackColor = Color.White,
                ForeColor = hasEmail ? Theme.Primary : Color.FromArgb(148, 163, 184),
                Enabled = hasEmail,
                Font = new Font("Segoe UI", 9.5f, FontStyle.Bold)
            };
            UiRadiusHelper.StyleButton(_btnMessage, 8);
            _btnMessage.Paint += (s, e) =>
            {
                using var p = new Pen(UiDetailCardHelper.BorderColor, 1f);
                using var path = UiRadiusHelper.CreateRoundedPath(new Rectangle(0, 0, _btnMessage.Width - 1, _btnMessage.Height - 1), 8);
                e.Graphics.DrawPath(p, path);
            };
            if (hasEmail)
            {
                UiRadiusHelper.AttachHoverFeedback(_btnMessage, Color.White, Color.FromArgb(241, 245, 249));
                _btnMessage.Click += BtnMessageClick;
            }
            _pnlFooter.Controls.Add(_btnMessage);

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

            // --- Card 1: Contact Information ---
            var cardContact = CreateCardPanel(ref currentY);
            UiDetailCardHelper.AddControl(cardContact, UiDetailCardHelper.CreateCardHeader("👤  Contact Information"));
            UiDetailCardHelper.AddControl(cardContact, UiDetailCardHelper.CreateDivider());
            UiDetailCardHelper.AddControl(cardContact, UiDetailCardHelper.CreateKeyValueRow(
                "Email Address", string.IsNullOrWhiteSpace(_customer!.Email) ? "Not Provided" : _customer.Email,
                "Phone Number", string.IsNullOrWhiteSpace(_customer.Phone) ? "Not Provided" : _customer.Phone));
            UiDetailCardHelper.AddControl(cardContact, UiDetailCardHelper.CreateKeyValueRow(
                "Customer Type", UiDetailCardHelper.ToTitleCase(_customer.Type),
                "Account Status", UiDetailCardHelper.ToTitleCase(_customer.Status)));
            FinalizeCardHeight(cardContact, ref currentY);
            _pnlContent.Controls.Add(cardContact);

            // --- Card 2: Ownership & Assignment ---
            var cardOwner = CreateCardPanel(ref currentY);
            UiDetailCardHelper.AddControl(cardOwner, UiDetailCardHelper.CreateCardHeader("👥  Ownership Assignment"));
            UiDetailCardHelper.AddControl(cardOwner, UiDetailCardHelper.CreateDivider());
            var agentName = _controller!.GetAssignedAgentName(_customer.AssignedAgentId);
            UiDetailCardHelper.AddControl(cardOwner, UiDetailCardHelper.CreateKeyValueRow(
                "Assigned Agent", agentName ?? "Unassigned",
                "Assignment Review", UiDetailCardHelper.ToTitleCase(_customer.AssignmentStatus)));
            if (!string.IsNullOrWhiteSpace(_customer.AssignmentReviewNotes))
            {
                UiDetailCardHelper.AddControl(cardOwner, UiDetailCardHelper.CreateKeyValueRow(
                    "Review Notes", _customer.AssignmentReviewNotes));
            }
            FinalizeCardHeight(cardOwner, ref currentY);
            _pnlContent.Controls.Add(cardOwner);

            // --- Card 3: Owned Properties (For Sellers/Both) ---
            bool isSeller = string.Equals(_customer.Type, "seller", StringComparison.OrdinalIgnoreCase)
                || string.Equals(_customer.Type, "both", StringComparison.OrdinalIgnoreCase);

            if (isSeller)
            {
                var properties = _controller.GetOwnedProperties(_customer.CustomerId);
                var cardProps = CreateCardPanel(ref currentY);
                UiDetailCardHelper.AddControl(cardProps, UiDetailCardHelper.CreateCardHeader("🏢  Owned Properties", properties.Count.ToString()));
                UiDetailCardHelper.AddControl(cardProps, UiDetailCardHelper.CreateDivider());

                if (properties.Count == 0)
                {
                    var lblEmpty = new Label
                    {
                        Text = "No property listings currently linked to this customer.",
                        Font = new Font("Segoe UI", 9.5f, FontStyle.Italic),
                        ForeColor = UiDetailCardHelper.LabelMutedColor,
                        Dock = DockStyle.Top,
                        Height = 36,
                        TextAlign = ContentAlignment.MiddleLeft
                    };
                    UiDetailCardHelper.AddControl(cardProps, lblEmpty);
                }
                else
                {
                    foreach (var p in properties)
                    {
                        var propTile = CreatePropertyTile(p);
                        UiDetailCardHelper.AddControl(cardProps, propTile);
                    }
                }
                FinalizeCardHeight(cardProps, ref currentY);
                _pnlContent.Controls.Add(cardProps);
            }

            // --- Card 4: Unified Interaction Timeline ---
            using var actCtrl = new ActivityApiService();
            bool canViewTimeline = actCtrl.CanViewTimeline(_customer.AssignedAgentId);
            bool canLogActivity = (RbacService.IsAgent && _customer.AssignedAgentId == CurrentSession.UserId) || RbacService.IsSuperAdmin;
            var timelineItems = canViewTimeline ? actCtrl.GetTimeline(_customer.CustomerId, null, _timelineFilter) : new List<TimelineItemDto>();

            var cardActivities = CreateCardPanel(ref currentY);

            // Card Header with "+ Log Activity" Button
            var pnlActHeader = new Panel
            {
                Dock = DockStyle.Top,
                Height = 36,
                Margin = new Padding(0, 0, 0, 8)
            };

            var lblActTitle = new Label
            {
                Text = "⏱  Interaction Timeline",
                Font = new Font("Segoe UI", 11f, FontStyle.Bold),
                ForeColor = UiDetailCardHelper.SectionTitleColor,
                Location = new Point(0, 6),
                AutoSize = true
            };
            pnlActHeader.Controls.Add(lblActTitle);

            var countBadge = UiDetailCardHelper.CreatePillBadge(
                timelineItems.Count.ToString(),
                Color.FromArgb(241, 245, 249),
                Color.FromArgb(71, 85, 105),
                Color.FromArgb(203, 213, 225));
            countBadge.Location = new Point(lblActTitle.Right + 8, 6);
            pnlActHeader.Controls.Add(countBadge);

            if (canLogActivity)
            {
                var btnLogAct = new Button
                {
                    Text = "+ Log Activity",
                    Size = new Size(115, 28),
                    Font = new Font("Segoe UI", 8.5f, FontStyle.Bold),
                    BackColor = Theme.Primary,
                    ForeColor = Color.White,
                    FlatStyle = FlatStyle.Flat,
                    Cursor = Cursors.Hand,
                    Anchor = AnchorStyles.Top | AnchorStyles.Right
                };
                btnLogAct.Location = new Point(cardActivities.Width - cardActivities.Padding.Right - 115 - 18, 4);
                btnLogAct.FlatAppearance.BorderSize = 0;
                UiRadiusHelper.StyleButton(btnLogAct, 6);
                UiRadiusHelper.AttachHoverFeedback(btnLogAct, Theme.Primary, Theme.PrimaryDark);
                btnLogAct.Click += (_, _) =>
                {
                    using var dlg = new LogActivityDialog(preselectedCustomerId: _customer.CustomerId);
                    if (dlg.ShowDialog(this) == DialogResult.OK)
                    {
                        BuildUi();
                    }
                };
                pnlActHeader.Controls.Add(btnLogAct);
            }
            UiDetailCardHelper.AddControl(cardActivities, pnlActHeader);
            UiDetailCardHelper.AddControl(cardActivities, UiDetailCardHelper.CreateDivider());

            if (!canViewTimeline)
            {
                var lblRestricted = new Label
                {
                    Text = "🔒 Interaction history is restricted to the assigned sales agent.",
                    Font = new Font("Segoe UI", 9.5f, FontStyle.Italic),
                    ForeColor = UiDetailCardHelper.LabelMutedColor,
                    Dock = DockStyle.Top,
                    Height = 36,
                    TextAlign = ContentAlignment.MiddleLeft
                };
                UiDetailCardHelper.AddControl(cardActivities, lblRestricted);
            }
            else
            {
                // Filter bar: All / Calls / Emails / Meetings / System Events
                var pnlFilters = new Panel
                {
                    Dock = DockStyle.Top,
                    Height = 34,
                    Margin = new Padding(0, 0, 0, 10)
                };

                string[] filters = { "All", "Calls", "Emails", "Meetings", "System Events" };
                string[] filterLabels = { "All", "Calls 📞", "Emails ✉️", "Meetings 📅", "System Events ⚙️" };
                int btnX = 0;
                for (int i = 0; i < filters.Length; i++)
                {
                    string f = filters[i];
                    string label = filterLabels[i];
                    bool isActive = string.Equals(_timelineFilter, f, StringComparison.OrdinalIgnoreCase);

                    var btnF = new Button
                    {
                        Text = label,
                        Location = new Point(btnX, 2),
                        Height = 26,
                        AutoSize = true,
                        Font = new Font("Segoe UI", 8f, isActive ? FontStyle.Bold : FontStyle.Regular),
                        BackColor = isActive ? Theme.Primary : Color.FromArgb(241, 245, 249),
                        ForeColor = isActive ? Color.White : Color.FromArgb(71, 85, 105),
                        FlatStyle = FlatStyle.Flat,
                        Cursor = Cursors.Hand
                    };
                    btnF.FlatAppearance.BorderSize = 0;
                    UiRadiusHelper.StyleButton(btnF, 5);
                    btnF.Click += (_, _) =>
                    {
                        _timelineFilter = f;
                        BuildUi();
                    };
                    pnlFilters.Controls.Add(btnF);
                    btnX += btnF.PreferredSize.Width + 6;
                }
                UiDetailCardHelper.AddControl(cardActivities, pnlFilters);

                if (timelineItems.Count == 0)
                {
                    var lblEmpty = new Label
                    {
                        Text = $"No activity or event records match the filter '{_timelineFilter}'.",
                        Font = new Font("Segoe UI", 9.5f, FontStyle.Italic),
                        ForeColor = UiDetailCardHelper.LabelMutedColor,
                        Dock = DockStyle.Top,
                        Height = 36,
                        TextAlign = ContentAlignment.MiddleLeft
                    };
                    UiDetailCardHelper.AddControl(cardActivities, lblEmpty);
                }
                else
                {
                    foreach (var item in timelineItems)
                    {
                        var row = CreateUnifiedTimelineRow(item, actCtrl);
                        UiDetailCardHelper.AddControl(cardActivities, row);
                    }
                }
            }

            FinalizeCardHeight(cardActivities, ref currentY);
            _pnlContent.Controls.Add(cardActivities);

            Controls.Add(_pnlContent);
            _pnlContent.BringToFront();
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
            // Calculate height from stacked controls
            int totalHeight = card.Padding.Top;
            foreach (Control c in card.Controls)
            {
                totalHeight += c.Height + c.Margin.Bottom;
            }
            totalHeight += card.Padding.Bottom + 4;
            card.Height = totalHeight;

            currentY += totalHeight + 14;
        }

        private Panel CreatePropertyTile(Property p)
        {
            var tile = new Panel
            {
                Height = 54,
                Dock = DockStyle.Top,
                BackColor = UiDetailCardHelper.TileBg,
                Margin = new Padding(0, 0, 0, 8),
                Padding = new Padding(12, 8, 12, 8)
            };

            tile.Paint += (s, e) =>
            {
                e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                var rect = new Rectangle(0, 0, tile.Width - 1, tile.Height - 1);
                using var path = UiRadiusHelper.CreateRoundedPath(rect, 6);
                using var pen = new Pen(UiDetailCardHelper.BorderColor, 1f);
                e.Graphics.DrawPath(pen, path);
            };
            UiRadiusHelper.ApplyRoundedCorners(tile, 6);

            var lblIcon = new Label
            {
                Text = "🏢",
                Font = new Font("Segoe UI Emoji", 14f),
                Location = new Point(10, 10),
                Size = new Size(28, 28),
                TextAlign = ContentAlignment.MiddleCenter
            };
            tile.Controls.Add(lblIcon);

            var lblAddr = new Label
            {
                Text = p.Address,
                Font = new Font("Segoe UI", 9.5f, FontStyle.Bold),
                ForeColor = UiDetailCardHelper.ValueTextColor,
                Location = new Point(44, 8),
                AutoSize = true
            };
            tile.Controls.Add(lblAddr);

            var lblDetails = new Label
            {
                Text = $"{p.PropertyType}  •  ₱{p.Price:N2}",
                Font = new Font("Segoe UI", 8.5f, FontStyle.Regular),
                ForeColor = UiDetailCardHelper.LabelMutedColor,
                Location = new Point(44, 28),
                AutoSize = true
            };
            tile.Controls.Add(lblDetails);

            var (pBg, pFg, pStroke) = UiDetailCardHelper.GetStatusColors(p.Status);
            var badge = UiDetailCardHelper.CreatePillBadge(p.Status, pBg, pFg, pStroke);
            badge.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            badge.Location = new Point(tile.Width - badge.Width - 14, 14);
            tile.Controls.Add(badge);

            return tile;
        }

        private Panel CreateUnifiedTimelineRow(TimelineItemDto item, ActivityApiService actCtrl)
        {
            var row = new Panel
            {
                Dock = DockStyle.Top,
                BackColor = Color.Transparent,
                Margin = new Padding(0, 0, 0, 8),
                AutoSize = true,
                Padding = new Padding(0, 4, 0, 6)
            };

            // Left icon
            string icon = item.Type switch
            {
                "Call" => "📞",
                "Email" => "✉️",
                "Meeting" => "📅",
                _ => "⚙️"
            };

            var lblIcon = new Label
            {
                Text = icon,
                Font = new Font("Segoe UI Emoji", 10f),
                Location = new Point(0, 6),
                Size = new Size(24, 22),
                TextAlign = ContentAlignment.MiddleCenter
            };
            row.Controls.Add(lblIcon);

            // Date chip
            var dateBadge = UiDetailCardHelper.CreatePillBadge(
                item.Timestamp.ToLocalTime().ToString("MMM d, yyyy h:mm tt"),
                Color.FromArgb(241, 245, 249),
                Color.FromArgb(71, 85, 105),
                Color.FromArgb(226, 232, 240));
            dateBadge.Location = new Point(28, 6);
            row.Controls.Add(dateBadge);

            // Title / Type label
            var lblTitle = new Label
            {
                Text = item.Title,
                Font = new Font("Segoe UI", 9f, FontStyle.Bold),
                ForeColor = UiDetailCardHelper.ValueTextColor,
                Location = new Point(dateBadge.Right + 8, 8),
                AutoSize = true
            };
            row.Controls.Add(lblTitle);

            int nextX = lblTitle.Right + 8;

            // Outcome badge (for calls)
            if (item.Outcome.HasValue)
            {
                bool isConnected = item.Outcome.Value == CallOutcome.Connected;
                var outcomeBadge = UiDetailCardHelper.CreatePillBadge(
                    ActivityApiService.FormatCallOutcome(item.Outcome.Value),
                    isConnected ? Color.FromArgb(236, 253, 245) : Color.FromArgb(254, 243, 199),
                    isConnected ? Color.FromArgb(4, 120, 87) : Color.FromArgb(180, 83, 9),
                    isConnected ? Color.FromArgb(167, 243, 208) : Color.FromArgb(253, 230, 138));
                outcomeBadge.Location = new Point(nextX, 6);
                row.Controls.Add(outcomeBadge);
                nextX = outcomeBadge.Right + 6;
            }

            // Duration badge (if available)
            if (item.DurationMinutes.HasValue && item.DurationMinutes.Value > 0)
            {
                var durBadge = UiDetailCardHelper.CreatePillBadge(
                    $"{item.DurationMinutes}m",
                    Color.FromArgb(239, 246, 255),
                    Color.FromArgb(29, 78, 216),
                    Color.FromArgb(191, 219, 254));
                durBadge.Location = new Point(nextX, 6);
                row.Controls.Add(durBadge);
                nextX = durBadge.Right + 6;
            }

            // Actor label
            var lblActor = new Label
            {
                Text = $"by {item.ActorName}",
                Font = new Font("Segoe UI", 8.5f, FontStyle.Italic),
                ForeColor = UiDetailCardHelper.LabelMutedColor,
                Location = new Point(nextX, 9),
                AutoSize = true
            };
            row.Controls.Add(lblActor);

            // Action button: "➕ Schedule Follow-Up" shortcut (if user is Agent and Actions module is enabled)
            if (CurrentSession.CanAccessActions && RbacService.IsAgent && item.CanCreateFollowUp)
            {
                var btnFollowUp = new Button
                {
                    Text = "➕ Follow-Up",
                    Size = new Size(95, 24),
                    Font = new Font("Segoe UI", 8f, FontStyle.Bold),
                    BackColor = Color.White,
                    ForeColor = Theme.Primary,
                    FlatStyle = FlatStyle.Flat,
                    Cursor = Cursors.Hand,
                    Anchor = AnchorStyles.Top | AnchorStyles.Right,
                    Location = new Point(Math.Max(500, row.Width - 110), 6)
                };
                btnFollowUp.FlatAppearance.BorderColor = Color.FromArgb(203, 213, 225);
                UiRadiusHelper.StyleButton(btnFollowUp, 4);
                UiRadiusHelper.AttachHoverFeedback(btnFollowUp, Color.White, Color.FromArgb(241, 245, 249));
                btnFollowUp.Click += (_, _) =>
                {
                    using var fuCtrl = new FollowUpApiService();
                    var template = actCtrl.CreateFollowUpTemplate(item);
                    using var fuForm = new FollowUpInputForm(fuCtrl, template);
                    if (fuForm.ShowDialog(this) == DialogResult.OK && fuForm.Result != null)
                    {
                        fuCtrl.Add(fuForm.Result);
                        MessageBox.Show("Follow-up scheduled successfully.", "Follow-Up Scheduled", MessageBoxButtons.OK, MessageBoxIcon.Information);
                        BuildUi();
                    }
                };
                row.Controls.Add(btnFollowUp);
            }

            // Notes row
            if (!string.IsNullOrWhiteSpace(item.Notes))
            {
                var lblNotes = new Label
                {
                    Text = item.Notes,
                    Font = new Font("Segoe UI", 9f),
                    ForeColor = Color.FromArgb(51, 65, 85),
                    Location = new Point(28, 34),
                    AutoSize = true,
                    MaximumSize = new Size(620, 0)
                };
                row.Controls.Add(lblNotes);
            }

            return row;
        }

        private void LayoutResponsiveComponents()
        {
            if (_pnlHeader != null)
            {
                var statusBadge = _pnlHeader.Controls["headerStatusBadge"];
                var typeBadge = _pnlHeader.Controls["headerTypeBadge"];

                int right = _pnlHeader.ClientSize.Width - 24;
                if (statusBadge != null)
                {
                    statusBadge.Location = new Point(right - statusBadge.Width, 22);
                    right -= (statusBadge.Width + 8);
                }
                if (typeBadge != null)
                {
                    typeBadge.Location = new Point(right - typeBadge.Width, 22);
                }
            }

            if (_pnlFooter != null)
            {
                // Left-aligned contextual action
                if (_btnMessage != null)
                {
                    _btnMessage.Location = new Point(24, 13);
                }

                // Right-aligned dialog actions (8px spacing) — Primary action rightmost, Close to its left
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
            if (_customer == null || _controller == null) return;

            using var editForm = new CustomerInputForm(_customer);
            if (editForm.ShowDialog(this) == DialogResult.OK && editForm.Result is not null)
            {
                _controller.Update(editForm.Result);
                _customer = _controller.GetById(_customer.CustomerId) ?? editForm.Result;
                BuildUi();
            }
        }

        private void BtnMessageClick(object? sender, EventArgs e)
        {
            if (_customer == null || string.IsNullOrWhiteSpace(_customer.Email)) return;

            using var emailForm = new EmailMessageForm(_customer.FullName, _customer.Email);
            emailForm.ShowDialog(this);
        }
    }
}
