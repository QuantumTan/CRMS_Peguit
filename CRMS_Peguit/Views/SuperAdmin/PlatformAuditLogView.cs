using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using CRMS_Peguit.domain.Common;
using CRMS_Peguit.winforms.Controls;
using CRMS_Peguit.winforms.Models.Services;
using CRMS_Peguit.winforms.Controllers;
using CRMS_Peguit.winforms.Services;

namespace CRMS_Peguit.winforms.Views.SuperAdmin
{
    public class PlatformAuditLogView : UserControl
    {
        private DataGridView _grid = null!;
        private GridSkeletonOverlay? _gridSkeleton;
        private Panel _topPanel = null!;
        private ComboBox _actionTypeFilter = null!;
        private DateTimePicker _fromDate = null!;
        private DateTimePicker _toDate = null!;
        private Button _filterBtn = null!;
        private TextBox _txtSearch = null!;
        private PaginationControl _pagination = null!;
        private readonly SuperAdminSyncHealthController _controller;
        private List<PlatformAuditLogDto> _allLogs = new();

        public PlatformAuditLogView()
        {
            _controller = new SuperAdminSyncHealthController();
            InitializeComponent();
            _ = LoadDataAsync();
        }

        private void InitializeComponent()
        {
            Dock = DockStyle.Fill;
            BackColor = Theme.Background;

            // 1. Top Page Header (Height = 126)
            _topPanel = new Panel
            {
                Dock = DockStyle.Top,
                Height = 126,
                Padding = new Padding(28, 16, 28, 0),
                BackColor = Theme.Surface
            };
            _topPanel.Paint += (s, e) =>
            {
                using var p = new Pen(Theme.Border, 1f);
                e.Graphics.DrawLine(p, 0, _topPanel.Height - 1, _topPanel.Width, _topPanel.Height - 1);
            };

            var titleLabel = new Label
            {
                Text = "Platform-Level Audit Log",
                Font = UiStyleConstants.PageTitleFont,
                ForeColor = Theme.TextPrimary,
                AutoSize = true,
                Location = new Point(28, 16)
            };

            var subLabel = new Label
            {
                Text = "Immutable append-only record of all administrative state-changing actions across the platform.",
                Font = UiStyleConstants.SubtitleFont,
                ForeColor = Theme.TextSecondary,
                AutoSize = true,
                Location = new Point(28, 52)
            };

            // Filter bar controls
            var lblAction = new Label
            {
                Text = "Action Type:",
                Font = new Font("Segoe UI", 9f, FontStyle.Bold),
                ForeColor = Theme.TextPrimary,
                Location = new Point(28, 85),
                AutoSize = true
            };

            _actionTypeFilter = new ComboBox
            {
                Location = new Point(112, 81),
                Width = 195,
                DropDownStyle = ComboBoxStyle.DropDownList,
                Font = new Font("Segoe UI", 9.5f)
            };
            UiRadiusHelper.StyleStandardComboBox(_actionTypeFilter);
            _actionTypeFilter.Items.AddRange(new object[]
            {
                "All Actions",
                "TenantCreated",
                "TenantSuspended",
                "TenantReactivated",
                "SubscriptionChanged",
                "BackupCreated",
                "BackupRestored",
                "SystemSettingChanged",
                "AdministratorCreated",
                "AdministratorDeactivated",
                "AdministratorActivated",
                "SyncRetried"
            });
            _actionTypeFilter.SelectedIndex = 0;

            var fromLabel = new Label
            {
                Text = "From:",
                Font = new Font("Segoe UI", 9f, FontStyle.Bold),
                Location = new Point(325, 85),
                AutoSize = true,
                ForeColor = Theme.TextPrimary
            };
            _fromDate = new DateTimePicker
            {
                Location = new Point(370, 82),
                Width = 115,
                Format = DateTimePickerFormat.Short,
                Font = new Font("Segoe UI", 9.5f),
                Value = DateTime.Today.AddDays(-30)
            };

            var toLabel = new Label
            {
                Text = "To:",
                Font = new Font("Segoe UI", 9f, FontStyle.Bold),
                Location = new Point(500, 85),
                AutoSize = true,
                ForeColor = Theme.TextPrimary
            };
            _toDate = new DateTimePicker
            {
                Location = new Point(530, 82),
                Width = 115,
                Format = DateTimePickerFormat.Short,
                Font = new Font("Segoe UI", 9.5f),
                Value = DateTime.Today
            };

            _filterBtn = new Button
            {
                Text = "Filter",
                Location = new Point(660, 80),
                Size = new Size(88, 30),
                BackColor = Theme.Primary,
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 9.5f, FontStyle.Bold),
                Cursor = Cursors.Hand
            };
            _filterBtn.FlatAppearance.BorderSize = 0;
            UiRadiusHelper.StyleButton(_filterBtn, 6);
            _filterBtn.Click += (_, _) => _ = LoadDataAsync();

            _topPanel.Controls.Add(titleLabel);
            _topPanel.Controls.Add(subLabel);
            _topPanel.Controls.Add(lblAction);
            _topPanel.Controls.Add(_actionTypeFilter);
            _topPanel.Controls.Add(fromLabel);
            _topPanel.Controls.Add(_fromDate);
            _topPanel.Controls.Add(toLabel);
            _topPanel.Controls.Add(_toDate);
            _topPanel.Controls.Add(_filterBtn);
            var filters = new Panel { Dock = DockStyle.Bottom };
            foreach (var control in new Control[] { lblAction, _actionTypeFilter, fromLabel, _fromDate, toLabel, _toDate, _filterBtn })
                filters.Controls.Add(control);
            _topPanel.Controls.Add(filters);
            ResponsiveLayout.BindToolbar(filters, 8, lblAction, _actionTypeFilter, fromLabel, _fromDate, toLabel, _toDate, _filterBtn);
            bool headerLayoutBusy = false;
            void LayoutHeader()
            {
                if (headerLayoutBusy) return;
                headerLayoutBusy = true;
                try
                {
                    int width = Math.Max(1, _topPanel.Width - 48);
                    int bottom = ResponsiveLayout.LabelBlock(titleLabel, 24, 16, width);
                    bottom = ResponsiveLayout.LabelBlock(subLabel, 24, bottom + 4, width);
                    _topPanel.Height = bottom + filters.Height + 12;
                }
                finally { headerLayoutBusy = false; }
            }
            _topPanel.SizeChanged += (_, _) => LayoutHeader();
            filters.SizeChanged += (_, _) => LayoutHeader();

            // 2. Table Card Wrapper (Dock = Fill)
            var pnlGridWrapper = new Panel
            {
                Dock = DockStyle.Fill,
                Padding = new Padding(28, 16, 28, 28),
                BackColor = Theme.Background
            };

            var pnlTableCard = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Theme.Surface,
                Padding = new Padding(0)
            };
            UiRadiusHelper.StyleCard(pnlTableCard, 8);

            // Card Header
            var pnlCardHeader = new Panel
            {
                Dock = DockStyle.Top,
                Height = 60,
                Padding = new Padding(24, 14, 24, 14),
                BackColor = Color.White
            };
            pnlCardHeader.Paint += (s, e) =>
            {
                using var p = new Pen(Color.FromArgb(241, 245, 249), 1f);
                e.Graphics.DrawLine(p, 0, pnlCardHeader.Height - 1, pnlCardHeader.Width, pnlCardHeader.Height - 1);
            };

            var lblCardTitle = new Label
            {
                Text = "Audit Event Stream",
                Font = new Font("Segoe UI", 12f, FontStyle.Bold),
                ForeColor = Color.FromArgb(15, 23, 42),
                AutoSize = true,
                Location = new Point(24, 16)
            };
            pnlCardHeader.Controls.Add(lblCardTitle);

            _txtSearch = new TextBox
            {
                PlaceholderText = "Search by user, action, detail, or tenant...",
                Font = new Font("Segoe UI", 9.5f),
                Size = new Size(300, 32),
                Anchor = AnchorStyles.Top | AnchorStyles.Right,
                Location = new Point(pnlCardHeader.Width - 324, 14)
            };
            _txtSearch.TextChanged += (_, _) => ApplyFilter(resetPage: true);
            pnlCardHeader.SizeChanged += (_, _) =>
                _txtSearch.Location = new Point(pnlCardHeader.Width - 324, 14);
            pnlCardHeader.Controls.Add(_txtSearch);
            ResponsiveLayout.BindHeader(pnlCardHeader, lblCardTitle, null, _txtSearch);

            // Pagination
            _pagination = new PaginationControl
            {
                Dock = DockStyle.Bottom
            };
            _pagination.PageChanged += (_, _) => ApplyFilter(resetPage: false);
            _pagination.PageSizeChanged += (_, _) => ApplyFilter(resetPage: true);

            // DataGridView with Full-Width Proportional Columns
            _grid = new DataGridView
            {
                Dock = DockStyle.Fill,
                AllowUserToAddRows = false,
                AllowUserToDeleteRows = false,
                ReadOnly = true,
                RowTemplate = { Height = 56 },
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                MultiSelect = false,
                AutoGenerateColumns = false,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                BackgroundColor = Color.White,
                BorderStyle = BorderStyle.None,
                RowHeadersVisible = false
            };
            UiGridHelper.ApplyModernGridStyle(_grid, rowHeight: 56);

            _grid.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = "Who",
                HeaderText = "Performed By (WHO)",
                DataPropertyName = "PerformedByName",
                FillWeight = 20,
                MinimumWidth = 160
            });
            _grid.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = "ActionType",
                HeaderText = "Action (WHAT)",
                DataPropertyName = "ActionType",
                FillWeight = 18,
                MinimumWidth = 130
            });
            _grid.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = "Detail",
                HeaderText = "Structured Detail",
                DataPropertyName = "Detail",
                FillWeight = 34,
                MinimumWidth = 200
            });
            _grid.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = "TargetCompany",
                HeaderText = "Target Tenant",
                DataPropertyName = "TargetCompanyDisplay",
                FillWeight = 14,
                MinimumWidth = 110
            });
            _grid.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = "When",
                HeaderText = "Timestamp (WHEN)",
                DataPropertyName = "WhenFormatted",
                FillWeight = 14,
                MinimumWidth = 120
            });

            _grid.CellPainting += Grid_CellPainting;

            pnlTableCard.Controls.Add(_grid);
            _gridSkeleton = GridSkeletonOverlay.CreateForGrid(_grid);
            pnlTableCard.Controls.Add(_pagination);
            pnlTableCard.Controls.Add(pnlCardHeader);
            _pagination.SendToBack();

            pnlGridWrapper.Controls.Add(pnlTableCard);

            Controls.Add(pnlGridWrapper);
            Controls.Add(_topPanel);
        }

        public async Task LoadDataAsync()
        {
            _gridSkeleton?.ShowSkeleton();
            try
            {
                string? actionType = _actionTypeFilter.SelectedItem?.ToString();
                if (actionType == "All Actions") actionType = null;
                DateTime? from = _fromDate.Value.Date;
                DateTime? to = _toDate.Value.Date;

                _allLogs = await _controller.GetAuditLogsAsync(actionType, from, to);
                ApplyFilter(resetPage: true);
            }
            catch (Exception ex)
            {
                if (!CRMS_Peguit.winforms.Audit.ScreenAuditor.IsAuditing)
                {
                    MessageBox.Show($"Failed to load audit logs: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
                else
                {
                    Console.WriteLine($"[AUDITOR WARNING] Failed to load audit logs: {ex.Message}");
                }
            }
            finally
            {
                _gridSkeleton?.HideSkeleton();
            }
        }

        private void ApplyFilter(bool resetPage = false)
        {
            if (resetPage)
            {
                _pagination.ResetPage();
            }

            var query = _allLogs.AsEnumerable();
            string s = _txtSearch?.Text.Trim() ?? "";
            if (!string.IsNullOrEmpty(s))
            {
                query = query.Where(l =>
                    l.PerformedByName.Contains(s, StringComparison.OrdinalIgnoreCase) ||
                    l.ActionType.Contains(s, StringComparison.OrdinalIgnoreCase) ||
                    l.Detail.Contains(s, StringComparison.OrdinalIgnoreCase) ||
                    (!string.IsNullOrEmpty(l.TargetCompanyName) && l.TargetCompanyName.Contains(s, StringComparison.OrdinalIgnoreCase)));
            }

            var fullList = query.ToList();
            int total = fullList.Count;

            var paged = fullList
                .Skip((_pagination.CurrentPage - 1) * _pagination.PageSize)
                .Take(_pagination.PageSize)
                .Select(log => new
                {
                    log.PerformedByName,
                    log.ActionType,
                    log.Detail,
                    TargetCompanyDisplay = string.IsNullOrWhiteSpace(log.TargetCompanyName) ? "Platform-Wide" : log.TargetCompanyName,
                    WhenFormatted = log.CreatedAt.ToLocalTime().ToString("MMM dd, yyyy  h:mm tt"),
                    Raw = log
                }).ToList();

            _grid.Rows.Clear();
            foreach (var item in paged)
            {
                int rowIndex = _grid.Rows.Add(
                    item.PerformedByName,
                    item.ActionType,
                    item.Detail,
                    item.TargetCompanyDisplay,
                    item.WhenFormatted);
                _grid.Rows[rowIndex].Tag = item.Raw;
            }

            _pagination.UpdatePagination(total, _pagination.CurrentPage, _pagination.PageSize);
        }

        private void Grid_CellPainting(object? sender, DataGridViewCellPaintingEventArgs e)
        {
            if (e.RowIndex < 0 || e.Graphics == null) return;

            var colWho = _grid.Columns["Who"];
            var colAction = _grid.Columns["ActionType"];

            if (colWho != null && e.ColumnIndex == colWho.Index)
            {
                e.PaintBackground(e.CellBounds, true);
                var text = e.Value?.ToString() ?? "";
                if (!string.IsNullOrEmpty(text))
                {
                    int size = 28;
                    int avatarY = e.CellBounds.Y + (e.CellBounds.Height - size) / 2;
                    var avatarRect = new Rectangle(e.CellBounds.X + 12, avatarY, size, size);

                    var (bg, fg) = AvatarLabel.GetDeterministicAvatarColors(text);
                    using (var b = new SolidBrush(bg))
                    {
                        e.Graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
                        e.Graphics.FillEllipse(b, avatarRect);
                    }

                    string initials = AvatarLabel.GetInitials(text);
                    using (var f = new Font("Segoe UI", 8.5f, FontStyle.Bold))
                    {
                        TextRenderer.DrawText(e.Graphics, initials, f, avatarRect, fg,
                            TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
                    }

                    int textLeft = avatarRect.Right + 10;
                    var textRect = new Rectangle(textLeft, e.CellBounds.Y, Math.Max(10, e.CellBounds.Width - (textLeft - e.CellBounds.X) - 8), e.CellBounds.Height);
                    using (var font = new Font("Segoe UI", 9.5f, FontStyle.Bold))
                    {
                        TextRenderer.DrawText(e.Graphics, text, font, textRect, Theme.TextPrimary,
                            TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis);
                    }
                }

                using (var linePen = new Pen(UiGridHelper.GridBorder, 1f))
                {
                    e.Graphics.DrawLine(linePen, e.CellBounds.Left, e.CellBounds.Bottom - 1, e.CellBounds.Right, e.CellBounds.Bottom - 1);
                }
                e.Handled = true;
            }
            else if (colAction != null && e.ColumnIndex == colAction.Index)
            {
                e.PaintBackground(e.CellBounds, true);
                var text = e.Value?.ToString() ?? "";
                if (!string.IsNullOrEmpty(text))
                {
                    Color color = text.Contains("Suspend", StringComparison.OrdinalIgnoreCase) || text.Contains("Deactivate", StringComparison.OrdinalIgnoreCase) ? Theme.StatusAlert :
                                  text.Contains("Restore", StringComparison.OrdinalIgnoreCase) ? Theme.StatusPending :
                                  Theme.Primary;

                    using var f = new Font("Segoe UI Semibold", 9f, FontStyle.Bold);
                    var textRect = new Rectangle(e.CellBounds.X + 12, e.CellBounds.Y, e.CellBounds.Width - 16, e.CellBounds.Height);
                    TextRenderer.DrawText(e.Graphics, text, f, textRect, color,
                        TextFormatFlags.VerticalCenter | TextFormatFlags.Left);
                }

                using (var linePen = new Pen(UiGridHelper.GridBorder, 1f))
                {
                    e.Graphics.DrawLine(linePen, e.CellBounds.Left, e.CellBounds.Bottom - 1, e.CellBounds.Right, e.CellBounds.Bottom - 1);
                }
                e.Handled = true;
            }
        }
    }
}
