using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Linq;
using System.Windows.Forms;
using CRMS_Peguit.domain.entities;
using CRMS_Peguit.winforms.Auth;
using CRMS_Peguit.winforms.Controllers;
using CRMS_Peguit.winforms.Controls;
using CRMS_Peguit.winforms.Models.Services;
using CRMS_Peguit.winforms.Services;
using CRMS_Peguit.winforms.Views.Customers;
using CRMS_Peguit.winforms.Views.Deals;
using CRMS_Peguit.winforms.Views.FollowUps;
using CRMS_Peguit.winforms.Views.Leads;
using CRMS_Peguit.winforms.Views.Notifications;
using CRMS_Peguit.winforms.Views.Properties;
using CRMS_Peguit.winforms.Views.SupportTickets;

namespace CRMS_Peguit.winforms.Views.Controls
{
    public class NotificationBell : Control
    {
        private int _unreadCount = 0;
        private readonly System.Windows.Forms.Timer _pollTimer;
        private readonly NotificationController _controller;
        private ToolStripDropDown? _dropdown;
        private bool _isHovered = false;

        public event Action<string>? NavigationRequested;

        public NotificationBell()
        {
            SetStyle(
                ControlStyles.AllPaintingInWmPaint |
                ControlStyles.UserPaint |
                ControlStyles.OptimizedDoubleBuffer |
                ControlStyles.ResizeRedraw |
                ControlStyles.SupportsTransparentBackColor,
                true);

            Size = new Size(36, 36);
            Cursor = Cursors.Hand;
            BackColor = Theme.HeaderBackground;

            _controller = new NotificationController();

            // 30-second polling interval (simple, responsive, zero-infrastructure)
            _pollTimer = new System.Windows.Forms.Timer
            {
                Interval = 30_000
            };
            _pollTimer.Tick += (_, _) => PollNotifications();
            _pollTimer.Start();
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _pollTimer.Stop();
                _pollTimer.Dispose();
                _dropdown?.Dispose();
                _controller.Dispose();
            }
            base.Dispose(disposing);
        }

        public void Initialize()
        {
            PollNotifications();
        }

        public void PollNotifications()
        {
            int currentUserId = CurrentSession.UserId;
            int tenantId = CurrentSession.TenantId;
            if (currentUserId <= 0) return;

            Task.Run(() =>
            {
                try
                {
                    using var ctrl = new NotificationController();

                    // Run role-specific trigger evaluation during polling
                    if (RbacService.IsAgent)
                    {
                        ctrl.CheckFollowUpReminders(currentUserId);
                    }

                    if (RbacService.IsAdmin || RbacService.IsSuperAdmin)
                    {
                        ctrl.CheckSubscriptionAlerts(tenantId);
                        ctrl.CheckBackupAlerts(tenantId);
                    }

                    int count = ctrl.GetUnreadCount(currentUserId);
                    if (!IsDisposed && _unreadCount != count)
                    {
                        BeginInvoke(() =>
                        {
                            if (!IsDisposed && _unreadCount != count)
                            {
                                _unreadCount = count;
                                Invalidate();
                            }
                        });
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"[NotificationBell.PollNotifications] Error: {ex.Message}");
                }
            });
        }

        protected override void OnMouseEnter(EventArgs e)
        {
            base.OnMouseEnter(e);
            _isHovered = true;
            Invalidate();
        }

        protected override void OnMouseLeave(EventArgs e)
        {
            base.OnMouseLeave(e);
            _isHovered = false;
            Invalidate();
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            var g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;

            // Optional hover background circle
            if (_isHovered)
            {
                using var hoverBrush = new SolidBrush(Color.FromArgb(20, 14, 165, 233));
                g.FillEllipse(hoverBrush, 2, 2, Width - 5, Height - 5);
            }

            // Draw Bell Icon 🔔
            using var bellFont = new Font("Segoe UI Emoji", 13f);
            using var bellBrush = new SolidBrush(_isHovered ? Theme.Primary : Theme.TextSecondary);
            var bellRect = new Rectangle(0, 2, Width, Height);
            var sf = new StringFormat
            {
                Alignment = StringAlignment.Center,
                LineAlignment = StringAlignment.Center
            };
            g.DrawString("🔔", bellFont, bellBrush, bellRect, sf);

            // Draw Unread Count Badge if > 0
            if (_unreadCount > 0)
            {
                string countText = _unreadCount > 99 ? "99+" : _unreadCount.ToString();
                using var badgeFont = new Font("Segoe UI", 7.5f, FontStyle.Bold);
                var textSize = g.MeasureString(countText, badgeFont);

                int badgeW = Math.Max(16, (int)textSize.Width + 6);
                int badgeH = 16;
                int badgeX = Width - badgeW - 1;
                int badgeY = 1;

                var badgeRect = new Rectangle(badgeX, badgeY, badgeW, badgeH);

                // Badge background (Alert red)
                using var badgeBrush = new SolidBrush(Theme.StatusAlert);
                using var badgePath = UiRadiusHelper.CreateRoundedPath(badgeRect, 8);
                g.FillPath(badgeBrush, badgePath);

                // Subtle white ring around badge for separation
                using var borderPen = new Pen(Color.White, 1.2f);
                g.DrawPath(borderPen, badgePath);

                // Text
                using var textBrush = new SolidBrush(Color.White);
                var textSf = new StringFormat
                {
                    Alignment = StringAlignment.Center,
                    LineAlignment = StringAlignment.Center
                };
                g.DrawString(countText, badgeFont, textBrush, badgeRect, textSf);
            }
        }

        protected override void OnClick(EventArgs e)
        {
            base.OnClick(e);
            ShowNotificationDropdown();
        }

        public async void ShowNotificationDropdown()
        {
            int currentUserId = CurrentSession.UserId;
            if (currentUserId <= 0) return;

            _dropdown?.Close();
            _dropdown?.Dispose();

            var panel = new Panel
            {
                Size = new Size(380, 480),
                BackColor = Color.White,
                Padding = new Padding(0)
            };

            // 1. Dropdown Header
            var pnlHeader = new Panel
            {
                Dock = DockStyle.Top,
                Height = 48,
                BackColor = Theme.HeaderBackground,
                Padding = new Padding(16, 10, 16, 10)
            };

            var lblTitle = new Label
            {
                Text = $"Notifications ({_unreadCount})",
                Font = new Font("Segoe UI", 10.5f, FontStyle.Bold),
                ForeColor = Theme.TextPrimary,
                Location = new Point(14, 13),
                AutoSize = true
            };

            var btnMarkAll = new Button
            {
                Text = "Mark all read",
                Font = new Font("Segoe UI", 8.5f),
                ForeColor = Theme.Primary,
                BackColor = Theme.HeaderBackground,
                FlatStyle = FlatStyle.Flat,
                Size = new Size(92, 26),
                Location = new Point(panel.Width - 145, 11),
                Cursor = Cursors.Hand
            };
            btnMarkAll.FlatAppearance.BorderSize = 0;
            btnMarkAll.Click += (_, _) =>
            {
                _controller.MarkAllAsRead(currentUserId);
                _unreadCount = 0;
                Invalidate();
                ShowNotificationDropdown(); // refresh view
            };

            var btnPrefs = new Button
            {
                Text = "⚙",
                Font = new Font("Segoe UI Emoji", 10f),
                ForeColor = Theme.TextSecondary,
                BackColor = Theme.HeaderBackground,
                FlatStyle = FlatStyle.Flat,
                Size = new Size(30, 26),
                Location = new Point(panel.Width - 46, 11),
                Cursor = Cursors.Hand
            };
            btnPrefs.FlatAppearance.BorderSize = 0;
            btnPrefs.Click += (_, _) =>
            {
                _dropdown?.Close();
                using var prefsForm = new NotificationPreferencesView(currentUserId);
                prefsForm.ShowDialog(FindForm());
            };

            pnlHeader.Controls.Add(lblTitle);
            pnlHeader.Controls.Add(btnMarkAll);
            pnlHeader.Controls.Add(btnPrefs);

            pnlHeader.Paint += (s, e) =>
            {
                using var p = new Pen(Theme.HeaderBorder, 1);
                e.Graphics.DrawLine(p, 0, pnlHeader.Height - 1, pnlHeader.Width, pnlHeader.Height - 1);
            };

            // 2. Dropdown Footer
            var pnlFooter = new Panel
            {
                Dock = DockStyle.Bottom,
                Height = 38,
                BackColor = Theme.HeaderBackground,
                Padding = new Padding(12, 6, 12, 6)
            };

            var btnClearOld = new Button
            {
                Text = "Clear Old (90d+)",
                Font = new Font("Segoe UI", 8f),
                ForeColor = Theme.TextSecondary,
                BackColor = Theme.HeaderBackground,
                FlatStyle = FlatStyle.Flat,
                Size = new Size(110, 24),
                Location = new Point(10, 7),
                Cursor = Cursors.Hand
            };
            btnClearOld.FlatAppearance.BorderSize = 0;
            btnClearOld.Click += (_, _) =>
            {
                int pruned = _controller.PruneOldNotifications(currentUserId, 90);
                if (pruned > 0)
                {
                    ShowNotificationDropdown();
                }
            };

            var btnPrefLink = new Button
            {
                Text = "Notification Settings",
                Font = new Font("Segoe UI", 8f),
                ForeColor = Theme.Primary,
                BackColor = Theme.HeaderBackground,
                FlatStyle = FlatStyle.Flat,
                Size = new Size(130, 24),
                Location = new Point(panel.Width - 145, 7),
                Cursor = Cursors.Hand
            };
            btnPrefLink.FlatAppearance.BorderSize = 0;
            btnPrefLink.Click += (_, _) =>
            {
                _dropdown?.Close();
                using var prefsForm = new NotificationPreferencesView(currentUserId);
                prefsForm.ShowDialog(FindForm());
            };

            pnlFooter.Controls.Add(btnClearOld);
            pnlFooter.Controls.Add(btnPrefLink);

            pnlFooter.Paint += (s, e) =>
            {
                using var p = new Pen(Theme.HeaderBorder, 1);
                e.Graphics.DrawLine(p, 0, 0, pnlFooter.Width, 0);
            };

            // 3. Scrollable List of Notifications
            var pnlList = new Panel
            {
                Dock = DockStyle.Fill,
                AutoScroll = true,
                BackColor = Theme.Background,
                Padding = new Padding(0)
            };

            panel.Controls.Add(pnlList);
            panel.Controls.Add(pnlFooter);
            panel.Controls.Add(pnlHeader);

            var host = new ToolStripControlHost(panel)
            {
                Margin = Padding.Empty,
                Padding = Padding.Empty,
                AutoSize = false,
                Size = panel.Size
            };

            _dropdown = new ToolStripDropDown
            {
                Padding = Padding.Empty,
                Margin = Padding.Empty,
                AutoClose = true,
                DropShadowEnabled = true
            };
            _dropdown.Items.Add(host);

            // Show right-aligned below bell icon
            int dropX = Width - panel.Width;
            int dropY = Height + 4;
            _dropdown.Show(this, new Point(dropX, dropY));

            var skeleton = ListSkeletonOverlay.CreateForContainer(pnlList, 54);
            skeleton.ShowSkeleton(4);

            List<Notification> items;
            try
            {
                items = await Task.Run(() => _controller.GetMyNotifications(currentUserId, take: 40));
            }
            finally
            {
                skeleton.HideSkeleton();
                pnlList.Controls.Remove(skeleton);
            }

            if (items.Count == 0)
            {
                var lblEmpty = new Label
                {
                    Text = "🎉 All caught up!\nNo new notifications.",
                    Font = new Font("Segoe UI", 10.5f),
                    ForeColor = Theme.TextSecondary,
                    Dock = DockStyle.Fill,
                    TextAlign = ContentAlignment.MiddleCenter
                };
                pnlList.Controls.Add(lblEmpty);
            }
            else
            {
                int currentY = 0;
                foreach (var notif in items)
                {
                    var itemCard = CreateNotificationCard(notif, panel.Width - 18, currentUserId);
                    itemCard.Location = new Point(0, currentY);
                    pnlList.Controls.Add(itemCard);
                    currentY += itemCard.Height;
                }
            }
        }

        private Panel CreateNotificationCard(Notification notif, int cardWidth, int currentUserId)
        {
            var card = new Panel
            {
                Size = new Size(cardWidth, 68),
                BackColor = notif.IsRead ? Color.White : Color.FromArgb(240, 249, 255), // subtle blue tint for unread
                Cursor = Cursors.Hand,
                Padding = new Padding(12, 8, 12, 8)
            };

            // Icon by type
            var (icon, iconColor) = GetTypeIcon(notif.Type);
            var lblIcon = new Label
            {
                Text = icon,
                Font = new Font("Segoe UI Emoji", 11.5f),
                ForeColor = iconColor,
                Location = new Point(10, 10),
                Size = new Size(26, 26),
                TextAlign = ContentAlignment.MiddleCenter,
                Cursor = Cursors.Hand
            };

            // Unread dot
            var pnlDot = new Panel
            {
                Size = new Size(7, 7),
                Location = new Point(4, 28),
                Visible = !notif.IsRead,
                BackColor = Theme.Primary
            };
            UiRadiusHelper.ApplyRoundedCorners(pnlDot, 3);

            // Title
            var lblTitle = new Label
            {
                Text = notif.Title,
                Font = new Font("Segoe UI", 9f, notif.IsRead ? FontStyle.Regular : FontStyle.Bold),
                ForeColor = Theme.TextPrimary,
                Location = new Point(38, 8),
                Size = new Size(cardWidth - 110, 18),
                AutoEllipsis = true,
                Cursor = Cursors.Hand
            };

            // Relative Time
            var lblTime = new Label
            {
                Text = GetRelativeTime(notif.CreatedAt),
                Font = new Font("Segoe UI", 7.5f),
                ForeColor = Theme.TextSecondary,
                Location = new Point(cardWidth - 78, 9),
                Size = new Size(70, 16),
                TextAlign = ContentAlignment.TopRight,
                Cursor = Cursors.Hand
            };

            // Message preview
            var lblMessage = new Label
            {
                Text = notif.Message,
                Font = new Font("Segoe UI", 8.25f),
                ForeColor = Theme.TextSecondary,
                Location = new Point(38, 28),
                Size = new Size(cardWidth - 50, 32),
                AutoEllipsis = true,
                Cursor = Cursors.Hand
            };

            card.Controls.Add(lblIcon);
            card.Controls.Add(pnlDot);
            card.Controls.Add(lblTitle);
            card.Controls.Add(lblTime);
            card.Controls.Add(lblMessage);

            card.Paint += (s, e) =>
            {
                using var p = new Pen(Theme.HeaderBorder, 1);
                e.Graphics.DrawLine(p, 0, card.Height - 1, card.Width, card.Height - 1);
            };

            EventHandler handleClick = (_, _) =>
            {
                // Mark notification read
                if (!notif.IsRead)
                {
                    _controller.MarkAsRead(notif.NotificationId);
                    notif.IsRead = true;
                    _unreadCount = Math.Max(0, _unreadCount - 1);
                    Invalidate();
                }

                _dropdown?.Close();

                // Re-validate access and navigate to detail screen
                HandleClickThrough(notif);
            };

            card.Click += handleClick;
            lblIcon.Click += handleClick;
            lblTitle.Click += handleClick;
            lblTime.Click += handleClick;
            lblMessage.Click += handleClick;

            // Hover styling
            card.MouseEnter += (_, _) =>
            {
                card.BackColor = Color.FromArgb(241, 245, 249);
            };
            card.MouseLeave += (_, _) =>
            {
                card.BackColor = notif.IsRead ? Color.White : Color.FromArgb(240, 249, 255);
            };

            return card;
        }

        private (string Icon, Color Color) GetTypeIcon(NotificationType type)
        {
            return type switch
            {
                NotificationType.LeadAssigned or NotificationType.LeadStageChanged => ("◎", Theme.Primary),
                NotificationType.LeadUnassigned => ("◎", Theme.StatusPending),
                NotificationType.CustomerAssigned or NotificationType.CustomerUnassigned => ("👥", Theme.Primary),
                NotificationType.DealStageChanged or NotificationType.DealClosed => ("💼", Theme.StatusSuccess),
                NotificationType.FollowUpDueSoon or NotificationType.FollowUpOverdue => ("⏱", Theme.StatusAlert),
                NotificationType.TicketAssigned or NotificationType.TicketStatusChanged or NotificationType.TicketCreated => ("🎟", Theme.StatusPending),
                NotificationType.PropertyAssigned or NotificationType.PropertyStatusChanged => ("🏢", Theme.Primary),
                NotificationType.SubscriptionExpiring or NotificationType.SubscriptionExpired => ("💳", Theme.StatusAlert),
                NotificationType.BackupFailed => ("⚠️", Theme.StatusAlert),
                NotificationType.AdminAccountCreated => ("🛡", Theme.Primary),
                _ => ("🔔", Theme.TextSecondary)
            };
        }

        private string GetRelativeTime(DateTime utcTime)
        {
            var diff = DateTime.UtcNow - utcTime;
            if (diff.TotalSeconds < 60) return "just now";
            if (diff.TotalMinutes < 60) return $"{(int)diff.TotalMinutes}m ago";
            if (diff.TotalHours < 24) return $"{(int)diff.TotalHours}h ago";
            if (diff.TotalDays < 2) return "Yesterday";
            if (diff.TotalDays < 7) return $"{(int)diff.TotalDays}d ago";
            return utcTime.ToLocalTime().ToString("MMM dd");
        }

        /// <summary>
        /// Clicking a notification click-through dynamically re-validates access before navigating.
        /// If the record was reassigned, displays an access restricted notification rather than leaking data.
        /// </summary>
        private void HandleClickThrough(Notification notif)
        {
            if (string.IsNullOrWhiteSpace(notif.RelatedEntityType) || !notif.RelatedEntityId.HasValue)
            {
                return;
            }

            int entityId = notif.RelatedEntityId.Value;
            var parentForm = FindForm();

            try
            {
                switch (notif.RelatedEntityType.ToLowerInvariant())
                {
                    case "lead":
                        using (var leadCtrl = new LeadController())
                        {
                            var lead = leadCtrl.GetById(entityId);
                            if (lead == null)
                            {
                                MessageBox.Show("You no longer have access to this lead or it has been reassigned/deleted.", "Access Restricted", MessageBoxButtons.OK, MessageBoxIcon.Information);
                                return;
                            }

                            using var detail = new LeadDetailForm(lead, leadCtrl);
                            detail.ShowDialog(parentForm);
                        }
                        break;

                    case "customer":
                        using (var custCtrl = new CustomerController())
                        {
                            var customer = custCtrl.GetById(entityId);
                            if (customer == null)
                            {
                                MessageBox.Show("You no longer have access to this customer or it has been reassigned/deleted.", "Access Restricted", MessageBoxButtons.OK, MessageBoxIcon.Information);
                                return;
                            }

                            using var detail = new CustomerDetailForm(customer, custCtrl);
                            detail.ShowDialog(parentForm);
                        }
                        break;

                    case "deal":
                        using (var dealCtrl = new DealController())
                        {
                            var deal = dealCtrl.GetById(entityId);
                            if (deal == null)
                            {
                                MessageBox.Show("You no longer have access to this deal or it has been reassigned/deleted.", "Access Restricted", MessageBoxButtons.OK, MessageBoxIcon.Information);
                                return;
                            }

                            using var detail = new DealDetailForm(deal, dealCtrl);
                            detail.ShowDialog(parentForm);
                        }
                        break;

                    case "property":
                        using (var propCtrl = new PropertyController())
                        {
                            var prop = propCtrl.GetById(entityId);
                            if (prop == null)
                            {
                                MessageBox.Show("You no longer have access to this property listing or it has been reassigned/deleted.", "Access Restricted", MessageBoxButtons.OK, MessageBoxIcon.Information);
                                return;
                            }

                            using var detail = new PropertyDetailForm(prop, propCtrl);
                            detail.ShowDialog(parentForm);
                        }
                        break;

                    case "supportticket":
                    case "ticket":
                        using (var tckCtrl = new SupportTicketController())
                        {
                            var ticket = tckCtrl.GetById(entityId);
                            if (ticket == null || !tckCtrl.CanUserViewTicket(ticket))
                            {
                                MessageBox.Show("You no longer have access to this support ticket or it has been reassigned.", "Access Restricted", MessageBoxButtons.OK, MessageBoxIcon.Information);
                                return;
                            }

                            using var detail = new SupportTicketDetailForm(ticket, tckCtrl);
                            detail.ShowDialog(parentForm);
                        }
                        break;

                    case "followup":
                    case "taskreminder":
                        NavigationRequested?.Invoke("followups");
                        break;

                    case "subscription":
                        NavigationRequested?.Invoke("reports");
                        break;

                    case "backuplog":
                        NavigationRequested?.Invoke("reports");
                        break;

                    default:
                        break;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[NotificationBell.HandleClickThrough] Error: {ex.Message}");
            }
        }
    }
}
