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

namespace CRMS_Peguit.winforms.Views.FollowUps
{
    public partial class FollowUpsView : UserControl
    {
        private readonly FollowUpController _controller;
        private string _filterCategory = "All"; // "Overdue", "Today", "Upcoming", "All", "Completed"
        private List<TaskReminder> _allReminders = new();
        private List<TaskReminder> _filteredReminders = new();
        private Panel _pnlEmptyState = null!;

        public FollowUpsView()
        {
            InitializeComponent();
            _controller = new FollowUpController();

            InitGridColumns();
            InitEmptyState();
            ApplyStyling();
            BindEvents();
            UpdateFilterPillStyles();
            RefreshData();
        }

        private void InitGridColumns()
        {
            grid.Columns.Clear();
            grid.AutoGenerateColumns = false;

            grid.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = "Type",
                HeaderText = "TYPE",
                Width = 90,
                MinimumWidth = 80,
                FillWeight = 14,
                ReadOnly = true
            });

            grid.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = "Title",
                HeaderText = "FOLLOW-UP / TOPIC",
                Width = 240,
                MinimumWidth = 180,
                FillWeight = 36,
                ReadOnly = true
            });

            grid.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = "Client",
                HeaderText = "ASSIGNED CLIENT",
                Width = 180,
                MinimumWidth = 140,
                FillWeight = 24,
                ReadOnly = true
            });

            grid.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = "DueDate",
                HeaderText = "SCHEDULED DUE",
                Width = 180,
                MinimumWidth = 150,
                FillWeight = 24,
                ReadOnly = true
            });

            grid.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = "Priority",
                HeaderText = "PRIORITY",
                Width = 90,
                MinimumWidth = 80,
                FillWeight = 14,
                ReadOnly = true
            });

            grid.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = "Status",
                HeaderText = "STATUS",
                Width = 110,
                MinimumWidth = 100,
                FillWeight = 16,
                ReadOnly = true
            });

            UiGridHelper.ApplyModernGridStyle(grid, 52);
            UiGridHelper.AddActionsColumn(grid, 60);

            grid.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
        }

        private void InitEmptyState()
        {
            _pnlEmptyState = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.White,
                Visible = false
            };

            var innerPanel = new Panel
            {
                Size = new Size(440, 230),
                BackColor = Color.Transparent
            };

            var pnlIcon = new Panel
            {
                Size = new Size(48, 48),
                Location = new Point((440 - 48) / 2, 10),
                BackColor = Color.FromArgb(239, 246, 255)
            };
            UiRadiusHelper.ApplyRoundedCorners(pnlIcon, 12);
            pnlIcon.Paint += (s, e) =>
            {
                e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                using var font = new Font("Segoe UI Emoji", 18f);
                using var brush = new SolidBrush(Theme.Primary);
                var sf = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center };
                e.Graphics.DrawString("⏱", font, brush, new RectangleF(0, 0, 48, 48), sf);
            };

            var lblTitle = new Label
            {
                Text = "No Follow-Ups Found",
                Font = new Font("Segoe UI", 13f, FontStyle.Bold),
                ForeColor = Color.FromArgb(15, 23, 42),
                TextAlign = ContentAlignment.MiddleCenter,
                Size = new Size(440, 26),
                Location = new Point(0, 68)
            };

            var lblDesc = new Label
            {
                Text = "No follow-up reminders match your search or filter criteria.\nTry clearing your query or scheduling a new client touchpoint.",
                Font = new Font("Segoe UI", 9.5f),
                ForeColor = Color.FromArgb(100, 116, 139),
                TextAlign = ContentAlignment.MiddleCenter,
                Size = new Size(420, 42),
                Location = new Point(10, 98)
            };

            var btnReset = new Button
            {
                Text = "Clear Filters & Search",
                Font = new Font("Segoe UI", 9.5f, FontStyle.Bold),
                ForeColor = Color.FromArgb(15, 91, 158),
                BackColor = Color.FromArgb(239, 246, 255),
                Cursor = Cursors.Hand,
                FlatStyle = FlatStyle.Flat,
                Size = new Size(180, 36),
                Location = new Point((440 - 180) / 2, 154)
            };
            btnReset.FlatAppearance.BorderColor = Color.FromArgb(191, 219, 254);
            UiRadiusHelper.StyleButton(btnReset, 8);
            btnReset.Click += (_, _) =>
            {
                txtSearch.Clear();
                SetFilter("All");
            };

            innerPanel.Controls.Add(pnlIcon);
            innerPanel.Controls.Add(lblTitle);
            innerPanel.Controls.Add(lblDesc);
            innerPanel.Controls.Add(btnReset);

            _pnlEmptyState.Controls.Add(innerPanel);
            _pnlEmptyState.Resize += (_, _) =>
            {
                innerPanel.Location = new Point((_pnlEmptyState.Width - innerPanel.Width) / 2, Math.Max(20, (_pnlEmptyState.Height - innerPanel.Height) / 2));
            };

            pnlCard.Controls.Add(_pnlEmptyState);
            _pnlEmptyState.BringToFront();
        }

        private void ApplyStyling()
        {
            UiRadiusHelper.StyleCard(pnlCard, 12);
            UiRadiusHelper.StyleButton(btnAdd, 8);

            // Pill widths (wide enough for counts like "Completed (128)")
            btnFilterAll.Width = 105;
            btnFilterOverdue.Width = 115;
            btnFilterToday.Width = 105;
            btnFilterUpcoming.Width = 125;
            btnFilterCompleted.Width = 130;

            LayoutPills();

            UiRadiusHelper.ApplyPillShape(btnFilterAll);
            UiRadiusHelper.ApplyPillShape(btnFilterOverdue);
            UiRadiusHelper.ApplyPillShape(btnFilterToday);
            UiRadiusHelper.ApplyPillShape(btnFilterUpcoming);
            UiRadiusHelper.ApplyPillShape(btnFilterCompleted);

            this.Load += (_, _) => LayoutPills();
            this.Resize += (_, _) => LayoutPills();
            pnlCard.SizeChanged += (_, _) => LayoutPills();
        }

        private void LayoutPills()
        {
            // On-screen order, left to right
            var pills = new[] { btnFilterAll, btnFilterOverdue, btnFilterToday, btnFilterUpcoming, btnFilterCompleted };

            // Right-align: last pill ends at the card's right edge, others stack leftward
            int x = pnlCard.Right;
            for (int i = pills.Length - 1; i >= 0; i--)
            {
                var p = pills[i];
                p.AutoSize = false;
                p.Anchor = AnchorStyles.Top | AnchorStyles.Left;
                p.Top = txtSearch.Top + (txtSearch.Height - p.Height) / 2;
                p.Left = x - p.Width;
                x = p.Left - 8;
            }
        }

        private void BindEvents()
        {
            btnAdd.Click += (_, _) => ShowCreateDialog();
            txtSearch.TextChanged += (_, _) => ApplyFilterAndSearch();

            btnFilterOverdue.Click += (_, _) => SetFilter("Overdue");
            btnFilterToday.Click += (_, _) => SetFilter("Today");
            btnFilterUpcoming.Click += (_, _) => SetFilter("Upcoming");
            btnFilterAll.Click += (_, _) => SetFilter("All");
            btnFilterCompleted.Click += (_, _) => SetFilter("Completed");

            // KPI card click shortcuts
            kpiOverdue.Cursor = Cursors.Hand;
            kpiToday.Cursor = Cursors.Hand;
            kpiUpcoming.Cursor = Cursors.Hand;
            kpiCompleted.Cursor = Cursors.Hand;

            kpiOverdue.Click += (_, _) => SetFilter("Overdue");
            kpiToday.Click += (_, _) => SetFilter("Today");
            kpiUpcoming.Click += (_, _) => SetFilter("Upcoming");
            kpiCompleted.Click += (_, _) => SetFilter("Completed");

            grid.CellPainting += GridCellPainting;
            grid.CellFormatting += GridCellFormatting;
            grid.CellContentClick += GridCellContentClick;
        }

        public void RefreshData()
        {
            _allReminders = _controller.GetAll();
            UpdateKpiCounts();
            ApplyFilterAndSearch();
        }

        private void UpdateKpiCounts()
        {
            var counts = _controller.GetKpiCounts();
            kpiOverdue.SetValue(counts.Overdue.ToString("N0"));
            kpiToday.SetValue(counts.Today.ToString("N0"));
            kpiUpcoming.SetValue(counts.Upcoming.ToString("N0"));
            kpiCompleted.SetValue(counts.Completed.ToString("N0"));

            btnFilterOverdue.Text = $"Overdue ({counts.Overdue})";
            btnFilterToday.Text = $"Today ({counts.Today})";
            btnFilterUpcoming.Text = $"Upcoming ({counts.Upcoming})";
            btnFilterAll.Text = "All Active";
            btnFilterCompleted.Text = $"Completed ({counts.Completed})";
        }

        private void SetFilter(string filter)
        {
            _filterCategory = filter;
            UpdateFilterPillStyles();
            ApplyFilterAndSearch();
        }

        private void UpdateFilterPillStyles()
        {
            var filterButtons = new[]
            {
                (btnFilterOverdue, "Overdue"),
                (btnFilterToday, "Today"),
                (btnFilterUpcoming, "Upcoming"),
                (btnFilterAll, "All"),
                (btnFilterCompleted, "Completed")
            };

            foreach (var (btn, name) in filterButtons)
            {
                bool active = string.Equals(_filterCategory, name, StringComparison.OrdinalIgnoreCase);
                UiRadiusHelper.StyleFilterPill(btn, active);
            }
        }

        private void ApplyFilterAndSearch()
        {
            string query = txtSearch.Text.Trim().ToLowerInvariant();
            var now = DateTime.UtcNow;
            var todayLocal = DateTime.Today;
            var tomorrowLocal = todayLocal.AddDays(1);

            IEnumerable<TaskReminder> source = _allReminders;

            // Bucket filter
            source = _filterCategory switch
            {
                "Overdue" => source.Where(r => r.Status != "Completed" && (r.Status == "Overdue" || r.DueDate < now)),
                "Today" => source.Where(r => r.Status != "Completed" && r.DueDate.ToLocalTime().Date == todayLocal),
                "Upcoming" => source.Where(r => r.Status != "Completed" && r.DueDate.ToLocalTime().Date >= tomorrowLocal),
                "Completed" => source.Where(r => r.Status == "Completed"),
                _ => source.Where(r => r.Status != "Completed") // "All Active"
            };

            // Search filter
            if (!string.IsNullOrWhiteSpace(query))
            {
                source = source.Where(r =>
                    (r.Title != null && r.Title.ToLowerInvariant().Contains(query)) ||
                    (r.Notes != null && r.Notes.ToLowerInvariant().Contains(query)) ||
                    (r.Type != null && r.Type.ToLowerInvariant().Contains(query)) ||
                    (r.Priority != null && r.Priority.ToLowerInvariant().Contains(query)) ||
                    (r.RelatedCustomer != null && r.RelatedCustomer.FullName.ToLowerInvariant().Contains(query)) ||
                    (r.RelatedCustomer != null && r.RelatedCustomer.Email != null && r.RelatedCustomer.Email.ToLowerInvariant().Contains(query)) ||
                    (r.RelatedLead != null && r.RelatedLead.FullName.ToLowerInvariant().Contains(query)) ||
                    (r.RelatedLead != null && r.RelatedLead.Email != null && r.RelatedLead.Email.ToLowerInvariant().Contains(query))
                );
            }

            _filteredReminders = source.OrderBy(r => r.DueDate).ToList();
            PopulateGrid();
        }

        private void PopulateGrid()
        {
            grid.Rows.Clear();

            if (_filteredReminders.Count == 0)
            {
                _pnlEmptyState.Visible = true;
                return;
            }

            _pnlEmptyState.Visible = false;

            foreach (var r in _filteredReminders)
            {
                string clientTag = r.RelatedCustomerId.HasValue ? "Customer" : "Lead";
                string clientName = r.RelatedCustomer?.FullName
                                    ?? r.RelatedLead?.FullName
                                    ?? "Unknown";

                string clientDisplay = $"[{clientTag}] {clientName}";

                var localDue = r.DueDate.ToLocalTime();
                string dueText = FormatDueDate(localDue, r.Status);

                int rowIndex = grid.Rows.Add(
                    r.Type,
                    r.Title,
                    clientDisplay,
                    dueText,
                    r.Priority,
                    r.Status,
                    "⋮"
                );

                grid.Rows[rowIndex].Tag = r;
            }
        }

        private static string FormatDueDate(DateTime localDue, string status)
        {
            var dateOnly = localDue.Date;
            var today = DateTime.Today;

            string relative = "";
            if (status == "Completed")
            {
                relative = "";
            }
            else if (dateOnly < today)
            {
                int daysAgo = (today - dateOnly).Days;
                relative = daysAgo == 1 ? " (Yesterday)" : $" ({daysAgo}d overdue)";
            }
            else if (dateOnly == today)
            {
                relative = " (Today)";
            }
            else if (dateOnly == today.AddDays(1))
            {
                relative = " (Tomorrow)";
            }

            return $"{localDue:MMM dd, yyyy · hh:mm tt}{relative}";
        }

        private void GridCellFormatting(object? sender, DataGridViewCellFormattingEventArgs e)
        {
            if (e.RowIndex < 0 || e.ColumnIndex < 0) return;

            string colName = grid.Columns[e.ColumnIndex].Name;

            if (colName == "Type" && e.Value is string typeVal)
            {
                string icon = typeVal.ToLowerInvariant() switch
                {
                    "call" => "📞",
                    "email" => "✉️",
                    "meeting" => "👥",
                    "text" => "💬",
                    _ => "⏱"
                };
                e.Value = $"{icon} {typeVal}";
                e.FormattingApplied = true;
            }
            else if (colName == "Priority" && e.Value is string prioVal)
            {
                e.CellStyle.Font = new Font("Segoe UI", 9f, FontStyle.Bold);
                e.CellStyle.ForeColor = prioVal.ToLowerInvariant() switch
                {
                    "high" => Color.FromArgb(220, 38, 38),   // Red
                    "medium" => Color.FromArgb(217, 119, 6), // Amber
                    "low" => Color.FromArgb(71, 85, 105),    // Slate
                    _ => Theme.TextPrimary
                };
            }
        }

        private void GridCellPainting(object? sender, DataGridViewCellPaintingEventArgs e)
        {
            if (e.RowIndex < 0 || e.ColumnIndex < 0) return;

            string colName = grid.Columns[e.ColumnIndex].Name;
            if (colName == "Status")
            {
                if (grid.Rows[e.RowIndex].Tag is TaskReminder reminder)
                {
                    UiGridHelper.PaintStatusIndicator(grid, e, reminder.Status, center: false);
                }
            }
            else if (colName == "Priority" && e.Value != null)
            {
                string prio = e.Value.ToString() ?? "";
                UiGridHelper.PaintStatusIndicator(grid, e, prio, center: false);
            }
        }

        private void GridCellContentClick(object? sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex < 0 || e.ColumnIndex < 0) return;
            if (grid.Columns[e.ColumnIndex] is not ActionsColumn) return;

            grid.Rows[e.RowIndex].Selected = true;
            grid.CurrentCell = grid.Rows[e.RowIndex].Cells[e.ColumnIndex];

            if (grid.Rows[e.RowIndex].Tag is not TaskReminder reminder) return;

            ShowActionMenu(reminder, e.RowIndex, e.ColumnIndex);
        }

        private void ShowActionMenu(TaskReminder reminder, int rowIndex, int colIndex)
        {
            var menu = new ContextMenuStrip
            {
                BackColor = Theme.Surface,
                ForeColor = Theme.TextPrimary,
                Font = new Font("Segoe UI", 9.5f)
            };

            string? clientEmail = reminder.RelatedCustomer?.Email ?? reminder.RelatedLead?.Email;
            string clientName = reminder.RelatedCustomer?.FullName ?? reminder.RelatedLead?.FullName ?? "Client";
            bool hasValidEmail = !string.IsNullOrWhiteSpace(clientEmail) && ContactEmailService.IsValidEmail(clientEmail);

            // 0. Message / Email Shortcut
            var messageItem = new ToolStripMenuItem($"✉️  Message {clientName}");
            messageItem.Font = new Font("Segoe UI", 9.5f, FontStyle.Bold);
            messageItem.Enabled = hasValidEmail;
            if (!hasValidEmail)
            {
                messageItem.ToolTipText = "No valid email address recorded for this client.";
            }
            messageItem.Click += (_, _) =>
            {
                using var msgForm = new Shared.EmailMessageForm(clientName, clientEmail, $"Regarding: {reminder.Title}");
                if (msgForm.ShowDialog(this) == DialogResult.OK)
                {
                    var askComplete = MessageBox.Show(
                        $"Email sent to {clientName}.\n\nWould you like to mark this follow-up as completed?",
                        "Follow-Up Action",
                        MessageBoxButtons.YesNo,
                        MessageBoxIcon.Question);

                    if (askComplete == DialogResult.Yes)
                    {
                        _controller.MarkComplete(reminder.TaskReminderId, logActivity: true, activityNotes: $"Sent email regarding '{reminder.Title}': {msgForm.SentSubject}");
                    }
                    RefreshData();
                }
            };
            menu.Items.Add(messageItem);
            menu.Items.Add(new ToolStripSeparator());

            bool isCompleted = string.Equals(reminder.Status, "Completed", StringComparison.OrdinalIgnoreCase);

            // 1. Mark Complete
            if (!isCompleted)
            {
                var completeItem = new ToolStripMenuItem("✓  Mark Complete");
                completeItem.Click += (_, _) => CompleteFollowUp(reminder);
                menu.Items.Add(completeItem);

                // 2. Snooze Quick Action Submenu
                var snoozeMenu = new ToolStripMenuItem("⏱  Snooze");

                var snooze1Day = new ToolStripMenuItem("+1 Day (Tomorrow)");
                snooze1Day.Click += (_, _) =>
                {
                    _controller.Snooze(reminder.TaskReminderId, TimeSpan.FromDays(1));
                    RefreshData();
                };

                var snooze3Days = new ToolStripMenuItem("+3 Days");
                snooze3Days.Click += (_, _) =>
                {
                    _controller.Snooze(reminder.TaskReminderId, TimeSpan.FromDays(3));
                    RefreshData();
                };

                var snooze1Week = new ToolStripMenuItem("+1 Week");
                snooze1Week.Click += (_, _) =>
                {
                    _controller.Snooze(reminder.TaskReminderId, TimeSpan.FromDays(7));
                    RefreshData();
                };

                snoozeMenu.DropDownItems.Add(snooze1Day);
                snoozeMenu.DropDownItems.Add(snooze3Days);
                snoozeMenu.DropDownItems.Add(snooze1Week);
                menu.Items.Add(snoozeMenu);

                // 3. Reschedule
                var rescheduleItem = new ToolStripMenuItem("📅  Reschedule...");
                rescheduleItem.Click += (_, _) => RescheduleFollowUp(reminder);
                menu.Items.Add(rescheduleItem);
            }

            // 4. Edit
            var editItem = new ToolStripMenuItem("✏️  Edit Details...");
            editItem.Click += (_, _) => EditFollowUp(reminder);
            menu.Items.Add(editItem);

            menu.Items.Add(new ToolStripSeparator());

            // 5. Soft Delete / Archive
            var archiveItem = new ToolStripMenuItem("🗑  Archive Follow-Up")
            {
                ForeColor = Color.FromArgb(185, 28, 28) // Muted danger red
            };
            archiveItem.Click += (_, _) => ArchiveFollowUp(reminder);
            menu.Items.Add(archiveItem);

            var cellRect = grid.GetCellDisplayRectangle(colIndex, rowIndex, true);
            menu.Show(grid, new Point(cellRect.Left, cellRect.Bottom));
        }

        private void ShowCreateDialog()
        {
            using var dlg = new FollowUpInputForm(_controller);
            if (dlg.ShowDialog(this) == DialogResult.OK && dlg.Result != null)
            {
                _controller.Add(dlg.Result);
                RefreshData();
            }
        }

        private void EditFollowUp(TaskReminder reminder)
        {
            using var dlg = new FollowUpInputForm(_controller, reminder);
            if (dlg.ShowDialog(this) == DialogResult.OK && dlg.Result != null)
            {
                _controller.Update(dlg.Result);
                RefreshData();
            }
        }

        private void CompleteFollowUp(TaskReminder reminder)
        {
            using var dlg = new CompleteFollowUpDialog(reminder);
            if (dlg.ShowDialog(this) == DialogResult.OK)
            {
                _controller.MarkComplete(reminder.TaskReminderId, dlg.LogActivity, dlg.ActivityNotes);
                RefreshData();
            }
        }

        private void RescheduleFollowUp(TaskReminder reminder)
        {
            using var dlg = new RescheduleDialog(reminder);
            if (dlg.ShowDialog(this) == DialogResult.OK)
            {
                _controller.Reschedule(reminder.TaskReminderId, dlg.NewDueDate);
                RefreshData();
            }
        }

        private void ArchiveFollowUp(TaskReminder reminder)
        {
            var confirm = MessageBox.Show(
                $"Are you sure you want to archive the follow-up '{reminder.Title}'?\n\nThis will remove it from your active list while preserving historical data.",
                "Archive Follow-Up",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Question
            );

            if (confirm == DialogResult.Yes)
            {
                _controller.SoftDelete(reminder.TaskReminderId);
                RefreshData();
            }
        }
    }
}