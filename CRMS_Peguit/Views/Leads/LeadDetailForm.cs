using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Linq;
using System.Windows.Forms;
using CRMS_Peguit.domain.entities;
using CRMS_Peguit.winforms.Auth;
using CRMS_Peguit.winforms.Controllers;
using CRMS_Peguit.winforms.Models.Services;
using CRMS_Peguit.winforms.Views.FollowUps;
using CRMS_Peguit.winforms.Views.Shared;

using Lead = CRMS_Peguit.domain.entities.Lead;

namespace CRMS_Peguit.winforms.Views.Leads
{
    public partial class LeadDetailForm : Form
    {
        private static readonly string[] PipelineStages =
            { "new", "contacted", "qualified", "converted" };

        private static readonly string[] StageDisplayNames =
            { "New", "Contacted", "Qualified", "Converted" };

        private Lead? _lead;
        private readonly LeadController? _controller;
        private string _timelineFilter = "All";

        private Panel? _pnlHeader;
        private Panel? _pnlFooter;
        private Panel? _pnlContent;
        private Button? _btnEdit;
        private Button? _btnConvert;
        private Button? _btnMessage;
        private Button? _btnClose;

        public LeadDetailForm()
        {
            InitializeComponent();
        }

        public LeadDetailForm(Lead lead, LeadController controller)
        {
            _lead = lead;
            _controller = controller;
            InitializeComponent();
            SetupFormProperties();
            BuildUi();
        }

        private void SetupFormProperties()
        {
            string displayName = _lead is not null ? UiDetailCardHelper.ToTitleCase(_lead.FullName) : "Lead Details";
            Text = _lead is not null ? $"Lead Details - {displayName}" : "Lead Details";
            Size = new Size(760, 700);
            MinimumSize = new Size(620, 520);
            StartPosition = FormStartPosition.CenterParent;
            BackColor = Color.FromArgb(244, 247, 251);
            DoubleBuffered = true;
            AppBrand.ApplyDarkTitleBar(this);
            AppBrand.ApplyAppIcon(this);
        }

        private void BuildUi()
        {
            Controls.Clear();
            if (_lead == null || _controller == null) return;

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

            string formattedName = UiDetailCardHelper.ToTitleCase(_lead!.FullName);

            // Avatar circle (centered with ample margins)
            var lblAvatar = new Label
            {
                Text = UiDetailCardHelper.GetInitials(formattedName),
                Font = new Font("Segoe UI", 15f, FontStyle.Bold),
                ForeColor = Color.FromArgb(180, 83, 9),       // #B45309 Amber
                BackColor = Color.FromArgb(254, 243, 199),     // #FEF3C7
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

            var regDate = _lead.CreatedAt != default ? _lead.CreatedAt.ToString("MMM d, yyyy") : "N/A";
            var lblMeta = new Label
            {
                Text = $"Lead #{_lead.LeadId}   •   Registered {regDate}",
                Font = new Font("Segoe UI", 9f, FontStyle.Regular),
                ForeColor = UiDetailCardHelper.LabelMutedColor,
                Location = new Point(88, 48),
                AutoSize = true
            };
            _pnlHeader.Controls.Add(lblMeta);

            // Badges in Header
            var (sBg, sFg, sStroke) = UiDetailCardHelper.GetStatusColors(_lead.Stage);
            var stageBadge = UiDetailCardHelper.CreateStatusIndicator(UiDetailCardHelper.ToTitleCase(_lead.Stage), sFg);
            stageBadge.Name = "headerStageBadge";
            _pnlHeader.Controls.Add(stageBadge);

            string priorityStr = string.IsNullOrWhiteSpace(_lead.Priority) ? "Normal" : UiDetailCardHelper.ToTitleCase(_lead.Priority);
            var (pBg, pFg, pStroke) = UiDetailCardHelper.GetPriorityColors(priorityStr);
            var priorityBadge = UiDetailCardHelper.CreateStatusIndicator(priorityStr, pFg);
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
            bool canEdit = _lead is not null && RbacService.CanEditRecord(_lead.AssignedAgentId, _lead.CreatedByUserId, _lead.AssignmentStatus);
            if (canEdit)
            {
                _btnEdit = new Button
                {
                    Text = "✏️ Edit Lead",
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

            // Convert to Customer Button (if lead is not already converted)
            bool canConvert = _lead is not null &&
                              !string.Equals(_lead.Stage, "converted", StringComparison.OrdinalIgnoreCase) &&
                              (RbacService.IsSuperAdmin || RbacService.IsManager || RbacService.CanCreateSalesRecord);
            if (canConvert)
            {
                _btnConvert = new Button
                {
                    Text = "🔄 Convert to Customer",
                    Size = new Size(165, 36),
                    BackColor = Color.White,
                    ForeColor = Color.FromArgb(16, 185, 129),
                    Font = new Font("Segoe UI", 9.5f, FontStyle.Bold),
                    Cursor = Cursors.Hand
                };
                UiRadiusHelper.StyleButton(_btnConvert, 8);
                _btnConvert.Paint += (s, e) =>
                {
                    using var p = new Pen(Color.FromArgb(167, 243, 208), 1f);
                    using var path = UiRadiusHelper.CreateRoundedPath(new Rectangle(0, 0, _btnConvert.Width - 1, _btnConvert.Height - 1), 8);
                    e.Graphics.DrawPath(p, path);
                };
                UiRadiusHelper.AttachHoverFeedback(_btnConvert, Color.White, Color.FromArgb(236, 253, 245));
                _btnConvert.Click += BtnConvertClick;
                _pnlFooter.Controls.Add(_btnConvert);
            }

            // Send Email Button
            bool hasEmail = !string.IsNullOrWhiteSpace(_lead?.Email);
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

            // --- Card 1: Pipeline Progression Stepper ---
            var cardPipeline = CreateCardPanel(ref currentY);
            UiDetailCardHelper.AddControl(cardPipeline, UiDetailCardHelper.CreateCardHeader("📊  Pipeline Progression"));
            UiDetailCardHelper.AddControl(cardPipeline, UiDetailCardHelper.CreateDivider());

            bool isLost = string.Equals(_lead!.Stage, "lost", StringComparison.OrdinalIgnoreCase);
            if (isLost)
            {
                var lostBanner = new Panel
                {
                    Height = 56,
                    Dock = DockStyle.Top,
                    BackColor = Color.FromArgb(254, 242, 242), // #FEF2F2
                    Padding = new Padding(14, 10, 14, 10)
                };
                lostBanner.Paint += (s, e) =>
                {
                    using var p = new Pen(Color.FromArgb(252, 165, 165), 1f);
                    e.Graphics.DrawRectangle(p, 0, 0, lostBanner.Width - 1, lostBanner.Height - 1);
                };
                var lblLost = new Label
                {
                    Text = "⚠️  This lead was marked LOST and is no longer actively progressing through the sales pipeline.",
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

            // --- Card 2: Lead Information ---
            var cardLeadInfo = CreateCardPanel(ref currentY);
            UiDetailCardHelper.AddControl(cardLeadInfo, UiDetailCardHelper.CreateCardHeader("👤  Lead Information"));
            UiDetailCardHelper.AddControl(cardLeadInfo, UiDetailCardHelper.CreateDivider());

            // Expected Value Highlight Tile (clean floating metric card)
            if (_lead.ExpectedValue.HasValue && _lead.ExpectedValue.Value > 0)
            {
                var valBanner = new Panel
                {
                    Height = 56,
                    Dock = DockStyle.Top,
                    BackColor = Color.FromArgb(248, 250, 252), // Subtle #F8FAFC
                    Margin = new Padding(0, 0, 0, 12),
                    Padding = new Padding(14, 8, 14, 8)
                };
                valBanner.Paint += (s, e) =>
                {
                    e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                    using var pen = new Pen(Color.FromArgb(226, 232, 240), 1f); // Subtle #E2E8F0
                    using var path = UiRadiusHelper.CreateRoundedPath(new Rectangle(0, 0, valBanner.Width - 1, valBanner.Height - 1), 8);
                    e.Graphics.DrawPath(pen, path);
                };
                UiRadiusHelper.ApplyRoundedCorners(valBanner, 8);

                var lblValCap = new Label
                {
                    Text = "EXPECTED VALUE",
                    Font = new Font("Segoe UI", 8f, FontStyle.Bold),
                    ForeColor = Color.FromArgb(100, 116, 139), // Muted #64748B
                    Location = new Point(12, 8),
                    AutoSize = true
                };
                var lblValAmt = new Label
                {
                    Text = $"₱{_lead.ExpectedValue.Value:N2}",
                    Font = new Font("Segoe UI", 13f, FontStyle.Bold),
                    ForeColor = Color.FromArgb(15, 23, 42), // High-contrast #0F172A
                    Location = new Point(12, 26),
                    AutoSize = true
                };
                valBanner.Controls.Add(lblValCap);
                valBanner.Controls.Add(lblValAmt);
                UiDetailCardHelper.AddControl(cardLeadInfo, valBanner);
            }

            UiDetailCardHelper.AddControl(cardLeadInfo, UiDetailCardHelper.CreateKeyValueRow(
                "Email Address", string.IsNullOrWhiteSpace(_lead.Email) ? "Not Provided" : _lead.Email,
                "Phone Number", string.IsNullOrWhiteSpace(_lead.Phone) ? "Not Provided" : _lead.Phone));
            UiDetailCardHelper.AddControl(cardLeadInfo, UiDetailCardHelper.CreateKeyValueRow(
                "Lead Source", string.IsNullOrWhiteSpace(_lead.Source) ? "Direct / Other" : _lead.Source,
                "Priority Level", string.IsNullOrWhiteSpace(_lead.Priority) ? "Normal" : UiDetailCardHelper.ToTitleCase(_lead.Priority)));
            FinalizeCardHeight(cardLeadInfo, ref currentY);
            _pnlContent.Controls.Add(cardLeadInfo);

            // --- Card 3: Ownership & Assignment ---
            var cardOwner = CreateCardPanel(ref currentY);
            UiDetailCardHelper.AddControl(cardOwner, UiDetailCardHelper.CreateCardHeader("👥  Assignment Review"));
            UiDetailCardHelper.AddControl(cardOwner, UiDetailCardHelper.CreateDivider());
            var agentName = _controller!.GetAssignedAgentName(_lead.AssignedAgentId);
            UiDetailCardHelper.AddControl(cardOwner, UiDetailCardHelper.CreateKeyValueRow(
                "Assigned Agent", agentName ?? "Unassigned",
                "Assignment Review", UiDetailCardHelper.ToTitleCase(_lead.AssignmentStatus)));
            if (!string.IsNullOrWhiteSpace(_lead.AssignmentReviewNotes))
            {
                UiDetailCardHelper.AddControl(cardOwner, UiDetailCardHelper.CreateKeyValueRow(
                    "Review Notes", _lead.AssignmentReviewNotes));
            }
            FinalizeCardHeight(cardOwner, ref currentY);
            _pnlContent.Controls.Add(cardOwner);

            // --- Card 4: Notes ---
            var cardNotes = CreateCardPanel(ref currentY);
            UiDetailCardHelper.AddControl(cardNotes, UiDetailCardHelper.CreateCardHeader("📝  Notes & Remarks"));
            UiDetailCardHelper.AddControl(cardNotes, UiDetailCardHelper.CreateDivider());

            var pnlNoteBox = new Panel
            {
                Dock = DockStyle.Top,
                BackColor = Color.FromArgb(248, 250, 252),
                Padding = new Padding(14, 10, 14, 10),
                AutoSize = true
            };
            pnlNoteBox.Paint += (s, e) =>
            {
                // Left accent line
                using var brush = new SolidBrush(Color.FromArgb(59, 130, 246));
                e.Graphics.FillRectangle(brush, 0, 0, 4, pnlNoteBox.Height);

                using var pen = new Pen(UiDetailCardHelper.BorderColor, 1f);
                e.Graphics.DrawRectangle(pen, 0, 0, pnlNoteBox.Width - 1, pnlNoteBox.Height - 1);
            };
            var lblNoteText = new Label
            {
                Text = string.IsNullOrWhiteSpace(_lead.Notes) ? "No notes recorded for this lead." : _lead.Notes,
                Font = new Font("Segoe UI", 9.5f, string.IsNullOrWhiteSpace(_lead.Notes) ? FontStyle.Italic : FontStyle.Regular),
                ForeColor = string.IsNullOrWhiteSpace(_lead.Notes) ? UiDetailCardHelper.LabelMutedColor : UiDetailCardHelper.ValueTextColor,
                Dock = DockStyle.Top,
                AutoSize = true,
                MaximumSize = new Size(580, 0)
            };
            pnlNoteBox.Controls.Add(lblNoteText);
            UiDetailCardHelper.AddControl(cardNotes, pnlNoteBox);

            FinalizeCardHeight(cardNotes, ref currentY);
            _pnlContent.Controls.Add(cardNotes);

            // --- Card 5: Unified Interaction Timeline ---
            using var actCtrl = new ActivityController();
            bool canViewTimeline = actCtrl.CanViewTimeline(_lead.AssignedAgentId);
            bool canLogActivity = (RbacService.IsAgent && _lead.AssignedAgentId == CurrentSession.UserId) || RbacService.IsSuperAdmin;
            var timelineItems = canViewTimeline ? actCtrl.GetTimeline(null, _lead.LeadId, _timelineFilter) : new List<TimelineItemDto>();

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
                    using var dlg = new LogActivityDialog(preselectedLeadId: _lead.LeadId);
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

        private Panel CreatePipelineStepper()
        {
            int currentIndex = Array.FindIndex(PipelineStages,
                s => string.Equals(s, _lead?.Stage, StringComparison.OrdinalIgnoreCase));
            if (currentIndex < 0) currentIndex = 0;

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

                // Outer border
                var borderRect = new Rectangle(0, 0, panel.Width - 1, panel.Height - 1);
                using var pPath = UiRadiusHelper.CreateRoundedPath(borderRect, 8);
                using var bPen = new Pen(UiDetailCardHelper.BorderColor, 1f);
                e.Graphics.DrawPath(bPen, pPath);

                int count = PipelineStages.Length;
                int startX = 60;
                int endX = panel.Width - 60;
                int lineY = 26;

                // Step positions
                var xs = new int[count];
                for (int i = 0; i < count; i++)
                {
                    xs[i] = startX + (int)((long)(endX - startX) * i / (count - 1));
                }

                // Draw connecting track lines first
                for (int i = 0; i < count - 1; i++)
                {
                    bool segDone = i < currentIndex;
                    using var linePen = new Pen(segDone ? Color.FromArgb(34, 197, 94) : Color.FromArgb(226, 232, 240), 3f);
                    e.Graphics.DrawLine(linePen, xs[i] + 14, lineY, xs[i + 1] - 14, lineY);
                }

                // Draw each node circle & stage label
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
                        // Passed: Emerald circle with checkmark
                        using var brush = new SolidBrush(Color.FromArgb(34, 197, 94));
                        e.Graphics.FillEllipse(brush, circleRect);

                        using var font = new Font("Segoe UI", 9f, FontStyle.Bold);
                        using var tBrush = new SolidBrush(Color.White);
                        using var sf = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center };
                        e.Graphics.DrawString("✓", font, tBrush, circleRect, sf);
                    }
                    else if (isCurrent)
                    {
                        bool isConverted = (i == count - 1) && string.Equals(_lead?.Stage, "converted", StringComparison.OrdinalIgnoreCase);

                        if (isConverted)
                        {
                            // Step 4 Converted: Completed emerald circle with checkmark rather than number 4
                            using var ringPen = new Pen(Color.FromArgb(187, 247, 208), 3f);
                            e.Graphics.DrawEllipse(ringPen, cx - 2, cy - 2, circleSize + 4, circleSize + 4);

                            using var brush = new SolidBrush(Color.FromArgb(34, 197, 94));
                            e.Graphics.FillEllipse(brush, circleRect);

                            using var font = new Font("Segoe UI", 9f, FontStyle.Bold);
                            using var tBrush = new SolidBrush(Color.White);
                            using var sf = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center };
                            e.Graphics.DrawString("✓", font, tBrush, circleRect, sf);
                        }
                        else
                        {
                            // Current: Primary blue circle with step number and outer ring
                            using var ringPen = new Pen(Color.FromArgb(191, 219, 254), 3f);
                            e.Graphics.DrawEllipse(ringPen, cx - 2, cy - 2, circleSize + 4, circleSize + 4);

                            using var brush = new SolidBrush(Theme.Primary);
                            e.Graphics.FillEllipse(brush, circleRect);

                            using var font = new Font("Segoe UI", 9f, FontStyle.Bold);
                            using var tBrush = new SolidBrush(Color.White);
                            using var sf = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center };
                            e.Graphics.DrawString((i + 1).ToString(), font, tBrush, circleRect, sf);
                        }
                    }
                    else
                    {
                        // Future: Gray outlined circle
                        using var brush = new SolidBrush(Color.White);
                        e.Graphics.FillEllipse(brush, circleRect);

                        using var pen = new Pen(Color.FromArgb(203, 213, 225), 1.5f);
                        e.Graphics.DrawEllipse(pen, circleRect);

                        using var font = new Font("Segoe UI", 8.5f, FontStyle.Bold);
                        using var tBrush = new SolidBrush(Color.FromArgb(148, 163, 184));
                        using var sf = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center };
                        e.Graphics.DrawString((i + 1).ToString(), font, tBrush, circleRect, sf);
                    }

                    // Stage Name Label below
                    Color labelColor = isCurrent ? Theme.Primary : isPassed ? Color.FromArgb(22, 101, 52) : Color.FromArgb(100, 116, 139);
                    var labelFont = new Font("Segoe UI", 8.5f, isCurrent ? FontStyle.Bold : FontStyle.Regular);
                    using (var lBrush = new SolidBrush(labelColor))
                    using (var sf = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Near })
                    {
                        var textRect = new RectangleF(xs[i] - 50, lineY + 18, 100, 20);
                        e.Graphics.DrawString(StageDisplayNames[i], labelFont, lBrush, textRect, sf);
                    }
                }
            };

            UiRadiusHelper.ApplyRoundedCorners(panel, 8);
            return panel;
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

        private Panel CreateUnifiedTimelineRow(TimelineItemDto item, ActivityController actCtrl)
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
                    ActivityController.FormatCallOutcome(item.Outcome.Value),
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

            // Action button: "➕ Schedule Follow-Up" shortcut (if user is Agent)
            if (RbacService.IsAgent && item.CanCreateFollowUp)
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
                    using var fuCtrl = new FollowUpController();
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
                var stageBadge = _pnlHeader.Controls["headerStageBadge"];
                var priorityBadge = _pnlHeader.Controls["headerPriorityBadge"];

                int right = _pnlHeader.ClientSize.Width - 24;
                if (stageBadge != null)
                {
                    stageBadge.Location = new Point(right - stageBadge.Width, 22);
                    right -= (stageBadge.Width + 8);
                }
                if (priorityBadge != null)
                {
                    priorityBadge.Location = new Point(right - priorityBadge.Width, 22);
                }
            }

            if (_pnlFooter != null)
            {
                // Left-aligned contextual actions (8px spacing)
                int left = 24;
                if (_btnMessage != null)
                {
                    _btnMessage.Location = new Point(left, 13);
                    left += _btnMessage.Width + 8;
                }
                if (_btnConvert != null && _btnConvert.Visible)
                {
                    _btnConvert.Location = new Point(left, 13);
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
            if (_lead == null || _controller == null) return;

            using var editForm = new LeadInputForm(_lead);
            if (editForm.ShowDialog(this) == DialogResult.OK && editForm.Result is not null)
            {
                _controller.Update(editForm.Result);
                _lead = _controller.GetById(_lead.LeadId) ?? editForm.Result;
                BuildUi();
            }
        }

        private void BtnMessageClick(object? sender, EventArgs e)
        {
            if (_lead == null || string.IsNullOrWhiteSpace(_lead.Email)) return;

            using var emailForm = new EmailMessageForm(_lead.FullName, _lead.Email);
            emailForm.ShowDialog(this);
        }

        private void BtnConvertClick(object? sender, EventArgs e)
        {
            if (_lead == null || _controller == null) return;

            if (!CRMS_Peguit.winforms.Services.UiStyleConstants.ConfirmAction(this, "convert", _lead.FullName, "A new customer record will be created and this lead will be marked as converted."))
                return;

            _controller.ConvertToCustomer(_lead);
            _lead = _controller.GetById(_lead.LeadId) ?? _lead;
            BuildUi();
            MessageBox.Show($"Lead '{_lead.FullName}' converted to customer successfully.", "Lead Converted", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
    }
}
