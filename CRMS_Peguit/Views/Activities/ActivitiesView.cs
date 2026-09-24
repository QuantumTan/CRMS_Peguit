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
using CRMS_Peguit.winforms.Views.FollowUps;
using CRMS_Peguit.winforms.Views.Leads;
using CRMS_Peguit.winforms.Views.Shared;

namespace CRMS_Peguit.winforms.Views.Activities
{
    public partial class ActivitiesView : UserControl
    {
        private readonly ActivityController _controller;
        private string _filterCategory = "All"; // "All", "Calls", "Emails", "Meetings", "System Events"
        private List<TimelineItemDto> _items = new();
        private Panel _pnlEmptyState = null!;

        public ActivitiesView()
        {
            InitializeComponent();
            _controller = new ActivityController();

            InitGridColumns();
            InitEmptyState();
            ApplyStyling();
            BindEvents();
            UpdateFilterPillStyles();
            RefreshData();

            this.Load += (_, _) => LayoutToolbar();
            this.Resize += (_, _) => LayoutToolbar();
        }

        private void InitGridColumns()
        {
            grid.Columns.Clear();
            grid.AutoGenerateColumns = false;

            grid.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = "Id",
                HeaderText = "ID",
                Visible = false
            });

            grid.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = "Type",
                HeaderText = "TYPE",
                Width = 95,
                MinimumWidth = 85,
                FillWeight = 12,
                ReadOnly = true
            });

            grid.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = "Client",
                HeaderText = "CONTACT / CLIENT",
                Width = 190,
                MinimumWidth = 150,
                FillWeight = 22,
                ReadOnly = true
            });

            grid.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = "Title",
                HeaderText = "ACTIVITY SUMMARY",
                Width = 220,
                MinimumWidth = 180,
                FillWeight = 26,
                ReadOnly = true
            });

            grid.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = "Notes",
                HeaderText = "DETAILS / NOTES",
                Width = 260,
                MinimumWidth = 180,
                FillWeight = 30,
                ReadOnly = true
            });

            grid.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = "Date",
                HeaderText = "DATE / TIME",
                Width = 150,
                MinimumWidth = 130,
                FillWeight = 16,
                ReadOnly = true
            });

            grid.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = "Actor",
                HeaderText = "LOGGED BY",
                Width = 130,
                MinimumWidth = 110,
                FillWeight = 14,
                ReadOnly = true
            });

            UiGridHelper.ApplyModernGridStyle(grid, 52);
            grid.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
        }

        private void InitEmptyState()
        {
            _pnlEmptyState = UiGridHelper.CreateEmptyStatePanel(
                KpiIconType.Clock,
                "No Activities Found",
                "No activities match your search or filter criteria.\nLog calls, emails, and meetings to maintain a complete client relationship history.",
                () =>
                {
                    txtSearch.Clear();
                    ToggleOrSetFilter("All");
                },
                "Clear Filters & Search");

            pnlCard.Controls.Add(_pnlEmptyState);
            _pnlEmptyState.BringToFront();
        }

        private void ApplyStyling()
        {
            UiRadiusHelper.StyleButton(btnAdd, 8);
            UiRadiusHelper.AttachHoverFeedback(btnAdd, Theme.Primary, Theme.PrimaryDark);
            UiRadiusHelper.StyleCard(pnlCard, 12);

            var filterPills = new[] { btnFilterAll, btnFilterCalls, btnFilterEmails, btnFilterMeetings, btnFilterSystem };
            foreach (var pill in filterPills)
            {
                UiRadiusHelper.ApplyPillShape(pill);
            }
        }

        private void LayoutToolbar()
        {
            if (this.IsDisposed) return;

            int rightPadding = UiStyleConstants.PageMarginRight;
            int leftMargin = UiStyleConstants.PageMarginLeft;
            int totalWidth = ClientSize.Width;

            // 1. Position and size KPI container
            pnlKpiContainer.Left = leftMargin;
            pnlKpiContainer.Top = Math.Max(90, lblSubtitle.Bottom + 10);
            pnlKpiContainer.Width = Math.Max(100, totalWidth - leftMargin - rightPadding);
            pnlKpiContainer.Height = UiStyleConstants.KpiRowHeight;

            // 2. Position toolbar row (Search box on left, filter pills in center, action buttons rightmost at identical y)
            int y = pnlKpiContainer.Bottom + 16;
            int rightEdge = totalWidth - rightPadding;

            if (btnAdd.Visible)
            {
                btnAdd.Top = y;
                btnAdd.Height = UiStyleConstants.ToolbarRowHeight;
                btnAdd.Left = rightEdge - btnAdd.Width;
                rightEdge = btnAdd.Left - 10;
            }

            var pills = new[] { btnFilterSystem, btnFilterMeetings, btnFilterEmails, btnFilterCalls, btnFilterAll };
            int filterRight = rightEdge;
            int totalFilterWidth = 0;
            foreach (var p in pills) totalFilterWidth += p.Width + 6;

            int availableForSearch = filterRight - leftMargin - totalFilterWidth - 16;

            if (availableForSearch >= 180)
            {
                // Single row
                foreach (var p in pills)
                {
                    p.Top = y;
                    p.Height = UiStyleConstants.ToolbarRowHeight;
                    p.Left = filterRight - p.Width;
                    filterRight = p.Left - 6;
                }

                txtSearch.Top = y;
                txtSearch.Left = leftMargin;
                txtSearch.Height = UiStyleConstants.ToolbarRowHeight;
                txtSearch.Width = Math.Min(UiStyleConstants.SearchBoxWidth, availableForSearch);

                int cardTop = y + UiStyleConstants.ToolbarRowHeight + 14;
                pnlCard.Top = cardTop;
                pnlCard.Height = Math.Max(100, ClientSize.Height - cardTop - UiStyleConstants.PageMarginBottom);
            }
            else
            {
                // Two rows: search on row 1, filter pills wrapped to row 2
                txtSearch.Top = y;
                txtSearch.Left = leftMargin;
                txtSearch.Height = UiStyleConstants.ToolbarRowHeight;
                txtSearch.Width = Math.Max(180, rightEdge - leftMargin);

                int pillY = y + UiStyleConstants.ToolbarRowHeight + 10;
                int filterX = leftMargin;
                var forwardPills = new[] { btnFilterAll, btnFilterCalls, btnFilterEmails, btnFilterMeetings, btnFilterSystem };
                foreach (var p in forwardPills)
                {
                    p.Top = pillY;
                    p.Height = UiStyleConstants.ToolbarRowHeight;
                    p.Left = filterX;
                    filterX += p.Width + 6;
                }

                int cardTop = pillY + UiStyleConstants.ToolbarRowHeight + 14;
                pnlCard.Top = cardTop;
                pnlCard.Height = Math.Max(100, ClientSize.Height - cardTop - 20);
            }

            pnlCard.Left = leftMargin;
            pnlCard.Width = Math.Max(100, totalWidth - leftMargin - rightPadding);
        }

        private void BindEvents()
        {
            btnAdd.Click += (_, _) => BtnAddClick();

            kpiTotal.ClickMode = KpiClickMode.InPlaceFilter;
            kpiCalls.ClickMode = KpiClickMode.InPlaceFilter;
            kpiEmails.ClickMode = KpiClickMode.InPlaceFilter;
            kpiMeetings.ClickMode = KpiClickMode.InPlaceFilter;

            btnFilterAll.Click += (_, _) => ToggleOrSetFilter("All");
            btnFilterCalls.Click += (_, _) => ToggleOrSetFilter("Calls");
            btnFilterEmails.Click += (_, _) => ToggleOrSetFilter("Emails");
            btnFilterMeetings.Click += (_, _) => ToggleOrSetFilter("Meetings");
            btnFilterSystem.Click += (_, _) => ToggleOrSetFilter("System Events");

            kpiTotal.Click += (_, _) => ToggleOrSetFilter("All");
            kpiCalls.Click += (_, _) => ToggleOrSetFilter("Calls");
            kpiEmails.Click += (_, _) => ToggleOrSetFilter("Emails");
            kpiMeetings.Click += (_, _) => ToggleOrSetFilter("Meetings");

            txtSearch.TextChanged += (_, _) =>
            {
                if (!string.IsNullOrWhiteSpace(txtSearch.Text) && !string.Equals(_filterCategory, "All", StringComparison.OrdinalIgnoreCase))
                {
                    _filterCategory = "All";
                    UpdateFilterPillStyles();
                }
                RefreshData();
            };

            grid.CellPainting += Grid_CellPainting;
            grid.CellDoubleClick += Grid_CellDoubleClick;
            grid.CellMouseDown += Grid_CellMouseDown;
        }

        public void ToggleOrSetFilter(string filter)
        {
            if (string.Equals(_filterCategory, filter, StringComparison.OrdinalIgnoreCase) && !string.Equals(filter, "All", StringComparison.OrdinalIgnoreCase))
            {
                SetFilter("All");
            }
            else
            {
                SetFilter(filter);
            }
        }

        public void SetFilter(string filter)
        {
            _filterCategory = filter;
            if (!string.Equals(_filterCategory, "All", StringComparison.OrdinalIgnoreCase) && !string.IsNullOrEmpty(txtSearch.Text))
            {
                txtSearch.Clear();
            }
            UpdateFilterPillStyles();
            RefreshData();
        }

        private void UpdateFilterPillStyles()
        {
            var mapping = new[]
            {
                (btnFilterAll, "All"),
                (btnFilterCalls, "Calls"),
                (btnFilterEmails, "Emails"),
                (btnFilterMeetings, "Meetings"),
                (btnFilterSystem, "System Events")
            };

            foreach (var (btn, key) in mapping)
            {
                bool active = string.Equals(_filterCategory, key, StringComparison.OrdinalIgnoreCase);
                UiRadiusHelper.StyleFilterPill(btn, active);
            }

            kpiTotal.SetSelected(string.Equals(_filterCategory, "All", StringComparison.OrdinalIgnoreCase));
            kpiCalls.SetSelected(string.Equals(_filterCategory, "Calls", StringComparison.OrdinalIgnoreCase));
            kpiEmails.SetSelected(string.Equals(_filterCategory, "Emails", StringComparison.OrdinalIgnoreCase));
            kpiMeetings.SetSelected(string.Equals(_filterCategory, "Meetings", StringComparison.OrdinalIgnoreCase));
        }

        public void RefreshData()
        {
            int agentId = CurrentSession.UserId;
            if (agentId <= 0) return;

            // Load items
            _items = _controller.GetAllForAgent(agentId, _filterCategory, txtSearch.Text);

            // Update KPI cards
            var (total, calls, emails, meetings) = _controller.GetActivityStatsForAgent(agentId);
            kpiTotal.SetValue(total.ToString("N0"));
            kpiCalls.SetValue(calls.ToString("N0"));
            kpiEmails.SetValue(emails.ToString("N0"));
            kpiMeetings.SetValue(meetings.ToString("N0"));

            // Populate Grid
            grid.Rows.Clear();
            foreach (var item in _items)
            {
                int rowIndex = grid.Rows.Add(
                    item.Id,
                    item.Type,
                    item.ClientName,
                    item.Title,
                    item.Notes ?? string.Empty,
                    item.Timestamp.ToLocalTime().ToString("MMM d, yyyy h:mm tt"),
                    item.ActorName
                );
                grid.Rows[rowIndex].Tag = item;
            }

            bool hasData = _items.Count > 0;
            _pnlEmptyState.Visible = !hasData;
            grid.Visible = hasData;
        }

        private void BtnAddClick()
        {
            using var dlg = new LogActivityDialog();
            if (dlg.ShowDialog(this.FindForm()) == DialogResult.OK)
            {
                RefreshData();
            }
        }

        private void Grid_CellDoubleClick(object? sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex < 0) return;
            OpenContactDetailForSelectedRow(e.RowIndex);
        }

        private void Grid_CellMouseDown(object? sender, DataGridViewCellMouseEventArgs e)
        {
            if (e.Button != MouseButtons.Right || e.RowIndex < 0) return;

            grid.ClearSelection();
            grid.Rows[e.RowIndex].Selected = true;

            if (grid.Rows[e.RowIndex].Tag is not TimelineItemDto item) return;

            var menu = new ContextMenuStrip
            {
                BackColor = Theme.Surface,
                ForeColor = Theme.TextPrimary,
                Font = new Font("Segoe UI", 10)
            };

            var viewContactItem = new ToolStripMenuItem($"📄 View {item.ClientType} Details");
            viewContactItem.Click += (_, _) => OpenContactDetailForSelectedRow(e.RowIndex);
            menu.Items.Add(viewContactItem);

            if (RbacService.IsAgent && item.CanCreateFollowUp)
            {
                var fuItem = new ToolStripMenuItem("➕ Schedule Follow-Up");
                fuItem.Click += (_, _) =>
                {
                    using var fuCtrl = new FollowUpController();
                    var template = _controller.CreateFollowUpTemplate(item);
                    using var form = new FollowUpInputForm(fuCtrl, template);
                    if (form.ShowDialog(this.FindForm()) == DialogResult.OK && form.Result != null)
                    {
                        fuCtrl.Add(form.Result);
                        MessageBox.Show("Follow-up task scheduled successfully.", "Follow-Up Scheduled", MessageBoxButtons.OK, MessageBoxIcon.Information);
                        RefreshData();
                    }
                };
                menu.Items.Add(fuItem);
            }

            menu.Show(grid, grid.PointToClient(Cursor.Position));
        }

        private void OpenContactDetailForSelectedRow(int rowIndex)
        {
            if (grid.Rows[rowIndex].Tag is not TimelineItemDto item) return;

            if (item.RelatedCustomerId.HasValue)
            {
                using var custCtrl = new CustomerController();
                var customer = custCtrl.GetById(item.RelatedCustomerId.Value);
                if (customer != null)
                {
                    using var form = new CustomerDetailForm(customer, custCtrl);
                    form.ShowDialog(this.FindForm());
                    RefreshData();
                }
            }
            else if (item.RelatedLeadId.HasValue)
            {
                using var leadCtrl = new LeadController();
                var lead = leadCtrl.GetById(item.RelatedLeadId.Value);
                if (lead != null)
                {
                    using var form = new LeadDetailForm(lead, leadCtrl);
                    form.ShowDialog(this.FindForm());
                    RefreshData();
                }
            }
        }

        private void Grid_CellPainting(object? sender, DataGridViewCellPaintingEventArgs e)
        {
            if (e.RowIndex < 0 || e.Graphics == null) return;

            // Render type column with crisp, portable emoji glyphs and clean text
            if (grid.Columns[e.ColumnIndex].Name == "Type" && e.Value != null)
            {
                e.Handled = true;
                e.PaintBackground(e.CellBounds, true);

                string type = e.Value.ToString() ?? "";
                string icon = type switch
                {
                    "Call" => "📞",
                    "Email" => "✉️",
                    "Meeting" => "📅",
                    "Lead Created" => "👤",
                    "Lead Updated" => "✏️",
                    "Deal Created" => "💼",
                    "Follow-Up Scheduled" => "⏱",
                    "Status Change" => "🔄",
                    _ => "📋"
                };

                using var iconFont = new Font("Segoe UI Emoji", 9.5f);
                using var textFont = new Font("Segoe UI", 9f, FontStyle.Bold);
                using var iconBrush = new SolidBrush(Color.FromArgb(71, 85, 105));
                using var textBrush = new SolidBrush(Color.FromArgb(15, 23, 42));

                int startX = e.CellBounds.X + 12;
                var iconSize = TextRenderer.MeasureText(e.Graphics, icon, iconFont);
                var iconRect = new Rectangle(startX, e.CellBounds.Y, iconSize.Width, e.CellBounds.Height);
                var sf = new StringFormat { LineAlignment = StringAlignment.Center, Alignment = StringAlignment.Near };
                e.Graphics.DrawString(icon, iconFont, iconBrush, iconRect, sf);

                var textRect = new Rectangle(startX + iconSize.Width + 6, e.CellBounds.Y, e.CellBounds.Width - (iconSize.Width + 18), e.CellBounds.Height);
                e.Graphics.DrawString(type, textFont, textBrush, textRect, sf);

                using (var pen = new Pen(UiGridHelper.GridBorder, 1f))
                {
                    e.Graphics.DrawLine(pen, e.CellBounds.Left, e.CellBounds.Bottom - 1, e.CellBounds.Right, e.CellBounds.Bottom - 1);
                }
            }
        }
    }
}
