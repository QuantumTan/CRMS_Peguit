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

namespace CRMS_Peguit.winforms.Views.Properties
{
    public partial class PropertyDetailForm : Form
    {
        private Property? _property;
        private readonly PropertyController? _controller;

        private Panel? _pnlHeader;
        private Panel? _pnlFooter;
        private Panel? _pnlContent;
        private Button? _btnEdit;
        private Button? _btnClose;

        public PropertyDetailForm()
        {
            InitializeComponent();
        }

        public PropertyDetailForm(Property property, PropertyController controller)
        {
            _property = property;
            _controller = controller;
            InitializeComponent();
            SetupFormProperties();
            BuildUi();
        }

        private void SetupFormProperties()
        {
            Text = _property is not null ? $"Property Details - #{_property.PropertyId}" : "Property Details";
            Size = new Size(740, 640);
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
            if (_property == null || _controller == null) return;

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

            // Building icon bubble 🏢
            var pnlIcon = new Panel
            {
                Size = new Size(52, 52),
                Location = new Point(24, 18),
                BackColor = Color.FromArgb(224, 242, 254) // #E0F2FE
            };
            pnlIcon.Paint += (s, e) =>
            {
                e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                var rect = new Rectangle(0, 0, pnlIcon.Width - 1, pnlIcon.Height - 1);
                using var path = UiRadiusHelper.CreateRoundedPath(rect, 10);
                using var pen = new Pen(Color.FromArgb(186, 230, 253), 1f);
                e.Graphics.DrawPath(pen, path);
            };
            UiRadiusHelper.ApplyRoundedCorners(pnlIcon, 10);

            var lblIcon = new Label
            {
                Text = "🏢",
                Font = new Font("Segoe UI Emoji", 18f),
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleCenter
            };
            pnlIcon.Controls.Add(lblIcon);
            _pnlHeader.Controls.Add(pnlIcon);

            // Title & Subtitle block
            var lblAddress = new Label
            {
                Text = _property!.Address,
                Font = new Font("Segoe UI", 14f, FontStyle.Bold),
                ForeColor = Theme.TextPrimary,
                Location = new Point(88, 18),
                AutoSize = true,
                MaximumSize = new Size(380, 28)
            };
            _pnlHeader.Controls.Add(lblAddress);

            var regDate = _property.CreatedAt != default ? _property.CreatedAt.ToString("MMM d, yyyy") : "N/A";
            var lblMeta = new Label
            {
                Text = $"Property #{_property.PropertyId}   •   Listed on {regDate}",
                Font = new Font("Segoe UI", 9f, FontStyle.Regular),
                ForeColor = UiDetailCardHelper.LabelMutedColor,
                Location = new Point(88, 48),
                AutoSize = true
            };
            _pnlHeader.Controls.Add(lblMeta);

            // Badges in Header
            var (sBg, sFg, sStroke) = UiDetailCardHelper.GetStatusColors(_property.Status);
            var statusBadge = UiDetailCardHelper.CreateStatusIndicator(UiDetailCardHelper.ToTitleCase(_property.Status), sFg);
            statusBadge.Name = "headerStatusBadge";
            _pnlHeader.Controls.Add(statusBadge);

            string propType = string.IsNullOrWhiteSpace(_property.PropertyType) ? "Property" : UiDetailCardHelper.ToTitleCase(_property.PropertyType);
            var typeBadge = UiDetailCardHelper.CreateStatusIndicator(propType, Color.FromArgb(51, 65, 85));
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
            bool canEdit = _property is not null && RbacService.CanEditRecord(_property.ListedByAgentId, _property.CreatedByUserId, _property.AssignmentStatus);
            if (canEdit)
            {
                _btnEdit = new Button
                {
                    Text = "✏️ Edit Property",
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

            // --- Card 1: Valuation Hero Banner (clean floating metric card) ---
            var cardPrice = CreateCardPanel(ref currentY);
            var pnlPriceHero = new Panel
            {
                Height = 68,
                Dock = DockStyle.Top,
                BackColor = Color.FromArgb(248, 250, 252), // Subtle #F8FAFC
                Margin = new Padding(0, 0, 0, 12),
                Padding = new Padding(18, 10, 18, 10)
            };
            pnlPriceHero.Paint += (s, e) =>
            {
                e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                var rect = new Rectangle(0, 0, pnlPriceHero.Width - 1, pnlPriceHero.Height - 1);
                using var path = UiRadiusHelper.CreateRoundedPath(rect, 8);
                using var pen = new Pen(Color.FromArgb(226, 232, 240), 1f); // Subtle #E2E8F0
                e.Graphics.DrawPath(pen, path);
            };
            UiRadiusHelper.ApplyRoundedCorners(pnlPriceHero, 8);

            var lblPriceCap = new Label
            {
                Text = "LISTING PRICE",
                Font = new Font("Segoe UI", 8.5f, FontStyle.Bold),
                ForeColor = Color.FromArgb(100, 116, 139), // Muted #64748B
                Location = new Point(14, 10),
                AutoSize = true
            };
            pnlPriceHero.Controls.Add(lblPriceCap);

            var lblPriceVal = new Label
            {
                Text = $"₱{_property!.Price:N2}",
                Font = new Font("Segoe UI", 18f, FontStyle.Bold),
                ForeColor = Color.FromArgb(15, 23, 42), // Clean high-contrast #0F172A
                Location = new Point(14, 28),
                AutoSize = true
            };
            pnlPriceHero.Controls.Add(lblPriceVal);

            UiDetailCardHelper.AddControl(cardPrice, pnlPriceHero);
            FinalizeCardHeight(cardPrice, ref currentY);
            _pnlContent.Controls.Add(cardPrice);

            // --- Card 2: Property Specifications ---
            var cardSpecs = CreateCardPanel(ref currentY);
            UiDetailCardHelper.AddControl(cardSpecs, UiDetailCardHelper.CreateCardHeader("🏢  Property Specifications"));
            UiDetailCardHelper.AddControl(cardSpecs, UiDetailCardHelper.CreateDivider());

            UiDetailCardHelper.AddControl(cardSpecs, UiDetailCardHelper.CreateKeyValueRow(
                "Address", _property.Address,
                "Property Type", string.IsNullOrWhiteSpace(_property.PropertyType) ? "Unspecified" : UiDetailCardHelper.ToTitleCase(_property.PropertyType)));
            UiDetailCardHelper.AddControl(cardSpecs, UiDetailCardHelper.CreateKeyValueRow(
                "Listing Status", UiDetailCardHelper.ToTitleCase(_property.Status),
                "Listing Date", _property.CreatedAt != default ? _property.CreatedAt.ToString("MMMM d, yyyy") : "N/A"));
            FinalizeCardHeight(cardSpecs, ref currentY);
            _pnlContent.Controls.Add(cardSpecs);

            // --- Card 3: Ownership & Listing Agent ---
            var cardOwnership = CreateCardPanel(ref currentY);
            UiDetailCardHelper.AddControl(cardOwnership, UiDetailCardHelper.CreateCardHeader("👥  Ownership & Listing Agent"));
            UiDetailCardHelper.AddControl(cardOwnership, UiDetailCardHelper.CreateDivider());

            var ownerName = _controller!.GetOwnerName(_property.OwnerCustomerId);
            var agentName = _controller.GetListedAgentName(_property.ListedByAgentId);

            UiDetailCardHelper.AddControl(cardOwnership, UiDetailCardHelper.CreateKeyValueRow(
                "Property Owner", ownerName != null ? UiDetailCardHelper.ToTitleCase(ownerName) : $"Customer #{_property.OwnerCustomerId}",
                "Listing Agent", agentName != null ? UiDetailCardHelper.ToTitleCase(agentName) : (_property.ListedByAgentId.HasValue ? $"User #{_property.ListedByAgentId.Value}" : "Unassigned")));
            UiDetailCardHelper.AddControl(cardOwnership, UiDetailCardHelper.CreateKeyValueRow(
                "Assignment Status", UiDetailCardHelper.ToTitleCase(_property.AssignmentStatus),
                "Reviewed By", _property.AssignmentReviewedByUserId.HasValue ? $"User #{_property.AssignmentReviewedByUserId.Value}" : "Pending Review"));

            if (!string.IsNullOrWhiteSpace(_property.AssignmentReviewNotes))
            {
                UiDetailCardHelper.AddControl(cardOwnership, UiDetailCardHelper.CreateKeyValueRow(
                    "Review Notes", _property.AssignmentReviewNotes));
            }

            FinalizeCardHeight(cardOwnership, ref currentY);
            _pnlContent.Controls.Add(cardOwnership);

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
            if (_property == null || _controller == null) return;

            var owners = _controller.GetOwnerCustomers();
            var agents = _controller.GetAgents();

            EnsureExistingOwnerAndAgent(_property, owners, agents);

            using var editForm = new PropertyInputForm(owners, agents, _property);
            if (editForm.ShowDialog(this) == DialogResult.OK && editForm.Result is not null)
            {
                _controller.Update(editForm.Result);
                _property = _controller.GetById(_property.PropertyId) ?? editForm.Result;
                BuildUi();
            }
        }

        private static void EnsureExistingOwnerAndAgent(
            Property property,
            List<CustomerPickerItem> owners,
            List<AgentPickerItem> agents)
        {
            if (property.OwnerCustomerId > 0 && !owners.Any(o => o.CustomerId == property.OwnerCustomerId))
            {
                owners.Insert(0, new CustomerPickerItem(
                    property.OwnerCustomerId,
                    $"Customer #{property.OwnerCustomerId}",
                    null));
            }

            if (property.ListedByAgentId.HasValue && property.ListedByAgentId.Value > 0 &&
                !agents.Any(a => a.UserId == property.ListedByAgentId.Value))
            {
                agents.Insert(0, new AgentPickerItem(
                    property.ListedByAgentId.Value,
                    $"User #{property.ListedByAgentId.Value}",
                    string.Empty));
            }
        }
    }
}
