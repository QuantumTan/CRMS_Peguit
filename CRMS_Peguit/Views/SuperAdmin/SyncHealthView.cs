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
    public class SyncHealthView : UserControl
    {
        private DataGridView _grid = null!;
        private Panel _topPanel = null!;
        private TextBox _txtSearch = null!;
        private PaginationControl _pagination = null!;
        private readonly SuperAdminSyncHealthController _controller;
        private List<SyncHealthDto> _allRecords = new();
        private int? _filterCompanyId;
        private Label _lblFilterActive = null!;
        private Button _btnClearFilter = null!;

        public SyncHealthView()
        {
            _controller = new SuperAdminSyncHealthController();
            InitializeComponent();
            _ = LoadDataAsync();
        }

        public void FilterByCompany(int companyId)
        {
            _filterCompanyId = companyId;
            _lblFilterActive.Visible = true;
            _btnClearFilter.Visible = true;
            _ = LoadDataAsync();
        }

        private void InitializeComponent()
        {
            Dock = DockStyle.Fill;
            BackColor = Theme.Background;

            // 1. Top Page Header (Dock = Top, Height = 96)
            _topPanel = new Panel
            {
                Dock = DockStyle.Top,
                Height = 96,
                Padding = new Padding(28, 18, 28, 0),
                BackColor = Theme.Surface
            };
            _topPanel.Paint += (s, e) =>
            {
                using var p = new Pen(Theme.Border, 1f);
                e.Graphics.DrawLine(p, 0, _topPanel.Height - 1, _topPanel.Width, _topPanel.Height - 1);
            };

            var titleLabel = new Label
            {
                Text = "Multi-Tenant Sync Health Monitoring",
                Font = UiStyleConstants.PageTitleFont,
                ForeColor = Theme.TextPrimary,
                AutoSize = true,
                Location = new Point(28, 18)
            };

            var subLabel = new Label
            {
                Text = "Queue health, pending operations, and sync statuses across tenant databases (counts and timestamps only).",
                Font = UiStyleConstants.SubtitleFont,
                ForeColor = Theme.TextSecondary,
                AutoSize = true,
                Location = new Point(28, 56)
            };

            _lblFilterActive = new Label
            {
                Text = "Filter: Showing single tenant",
                Font = new Font("Segoe UI", 9f, FontStyle.Bold),
                ForeColor = Theme.Primary,
                AutoSize = true,
                Anchor = AnchorStyles.Top | AnchorStyles.Right,
                Location = new Point(_topPanel.Width - 300, 36),
                Visible = false
            };

            _btnClearFilter = new Button
            {
                Text = "Show All",
                Size = new Size(86, 32),
                Anchor = AnchorStyles.Top | AnchorStyles.Right,
                Location = new Point(_topPanel.Width - 110, 32),
                Visible = false,
                Cursor = Cursors.Hand
            };
            UiRadiusHelper.StyleButton(_btnClearFilter, 4);
            _btnClearFilter.Click += (_, _) =>
            {
                _filterCompanyId = null;
                _lblFilterActive.Visible = false;
                _btnClearFilter.Visible = false;
                _ = LoadDataAsync();
            };

            _topPanel.SizeChanged += (_, _) =>
            {
                _btnClearFilter.Location = new Point(_topPanel.Width - 110, 32);
                _lblFilterActive.Location = new Point(_topPanel.Width - 300, 36);
            };

            _topPanel.Controls.Add(titleLabel);
            _topPanel.Controls.Add(subLabel);
            _topPanel.Controls.Add(_lblFilterActive);
            _topPanel.Controls.Add(_btnClearFilter);

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
                Text = "Database Sync Status & Queue Health",
                Font = new Font("Segoe UI", 12f, FontStyle.Bold),
                ForeColor = Color.FromArgb(15, 23, 42),
                AutoSize = true,
                Location = new Point(24, 16)
            };
            pnlCardHeader.Controls.Add(lblCardTitle);

            _txtSearch = new TextBox
            {
                PlaceholderText = "Search by tenant company...",
                Font = new Font("Segoe UI", 9.5f),
                Size = new Size(260, 32),
                Anchor = AnchorStyles.Top | AnchorStyles.Right,
                Location = new Point(pnlCardHeader.Width - 284, 14)
            };
            _txtSearch.TextChanged += (_, _) => ApplyFilter(resetPage: true);
            pnlCardHeader.SizeChanged += (_, _) =>
                _txtSearch.Location = new Point(pnlCardHeader.Width - 284, 14);
            pnlCardHeader.Controls.Add(_txtSearch);

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

            _grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "Id", HeaderText = "ID", Visible = false });

            _grid.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = "CompanyName",
                HeaderText = "Tenant Company",
                DataPropertyName = "CompanyName",
                FillWeight = 28,
                MinimumWidth = 180
            });
            _grid.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = "LastSync",
                HeaderText = "Last Successful Sync",
                DataPropertyName = "LastSyncFormatted",
                FillWeight = 22,
                MinimumWidth = 150
            });
            _grid.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = "PendingCount",
                HeaderText = "Pending Queue",
                DataPropertyName = "PendingDisplay",
                FillWeight = 16,
                MinimumWidth = 110
            });
            _grid.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = "FailedCount",
                HeaderText = "Failed Queue",
                DataPropertyName = "FailedDisplay",
                FillWeight = 16,
                MinimumWidth = 110
            });
            _grid.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = "SyncStatus",
                HeaderText = "Sync Status",
                DataPropertyName = "SyncStatus",
                FillWeight = 14,
                MinimumWidth = 100
            });
            _grid.Columns.Add(new ActionsColumn
            {
                AutoSizeMode = DataGridViewAutoSizeColumnMode.None,
                Width = 64
            });

            _grid.CellPainting += Grid_CellPainting;
            _grid.CellClick += Grid_CellClick;

            pnlTableCard.Controls.Add(_grid);
            pnlTableCard.Controls.Add(_pagination);
            pnlTableCard.Controls.Add(pnlCardHeader);
            _pagination.BringToFront();

            pnlGridWrapper.Controls.Add(pnlTableCard);

            Controls.Add(pnlGridWrapper);
            Controls.Add(_topPanel);
        }

        public async Task LoadDataAsync()
        {
            try
            {
                _allRecords = await _controller.GetSyncHealthAsync();
                ApplyFilter(resetPage: true);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to load sync health: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void ApplyFilter(bool resetPage = false)
        {
            if (resetPage)
            {
                _pagination.ResetPage();
            }

            var query = _allRecords.AsEnumerable();
            if (_filterCompanyId.HasValue)
            {
                query = query.Where(r => r.CompanyId == _filterCompanyId.Value);
            }

            string s = _txtSearch?.Text.Trim() ?? "";
            if (!string.IsNullOrEmpty(s))
            {
                query = query.Where(r =>
                    r.CompanyName.Contains(s, StringComparison.OrdinalIgnoreCase) ||
                    r.SyncStatus.Contains(s, StringComparison.OrdinalIgnoreCase));
            }

            var fullList = query.ToList();
            int total = fullList.Count;

            var paged = fullList
                .Skip((_pagination.CurrentPage - 1) * _pagination.PageSize)
                .Take(_pagination.PageSize)
                .Select(r => new
                {
                    r.CompanyId,
                    r.CompanyName,
                    LastSyncFormatted = r.LastSuccessfulSync.HasValue
                        ? r.LastSuccessfulSync.Value.ToLocalTime().ToString("MMM dd, yyyy  h:mm tt")
                        : "Never Synced",
                    PendingDisplay = $"{r.PendingCount} items",
                    FailedDisplay = $"{r.FailedCount} items",
                    r.SyncStatus,
                    Raw = r
                }).ToList();

            _grid.Rows.Clear();
            foreach (var item in paged)
            {
                int rowIndex = _grid.Rows.Add(
                    item.CompanyId,
                    item.CompanyName,
                    item.LastSyncFormatted,
                    item.PendingDisplay,
                    item.FailedDisplay,
                    item.SyncStatus,
                    "");
                _grid.Rows[rowIndex].Tag = item.Raw;
            }

            _pagination.UpdatePagination(total, _pagination.CurrentPage, _pagination.PageSize);
        }

        private void Grid_CellPainting(object? sender, DataGridViewCellPaintingEventArgs e)
        {
            if (e.RowIndex < 0 || e.Graphics == null) return;

            var colCompany = _grid.Columns["CompanyName"];
            var colFailed = _grid.Columns["FailedCount"];
            var colStatus = _grid.Columns["SyncStatus"];

            if (colCompany != null && e.ColumnIndex == colCompany.Index)
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
            else if (colFailed != null && e.ColumnIndex == colFailed.Index)
            {
                e.PaintBackground(e.CellBounds, true);
                if (_grid.Rows[e.RowIndex].Tag is SyncHealthDto dto)
                {
                    Color color = dto.FailedCount > 0 ? Theme.StatusAlert : Theme.TextSecondary;
                    using var f = new Font("Segoe UI Semibold", 9f, dto.FailedCount > 0 ? FontStyle.Bold : FontStyle.Regular);
                    var textRect = new Rectangle(e.CellBounds.X + 12, e.CellBounds.Y, e.CellBounds.Width - 16, e.CellBounds.Height);
                    TextRenderer.DrawText(e.Graphics, $"{dto.FailedCount} items", f, textRect, color,
                        TextFormatFlags.VerticalCenter | TextFormatFlags.Left);
                }

                using (var linePen = new Pen(UiGridHelper.GridBorder, 1f))
                {
                    e.Graphics.DrawLine(linePen, e.CellBounds.Left, e.CellBounds.Bottom - 1, e.CellBounds.Right, e.CellBounds.Bottom - 1);
                }
                e.Handled = true;
            }
            else if (colStatus != null && e.ColumnIndex == colStatus.Index)
            {
                e.PaintBackground(e.CellBounds, true);
                var text = e.Value?.ToString() ?? "";
                if (!string.IsNullOrEmpty(text))
                {
                    Color color = text.Equals("Healthy", StringComparison.OrdinalIgnoreCase) ? Theme.StatusSuccess :
                                  text.Equals("Delayed", StringComparison.OrdinalIgnoreCase) ? Theme.StatusPending :
                                  Theme.StatusAlert;

                    using var f = new Font("Segoe UI Semibold", 9f, FontStyle.Bold);
                    var textRect = new Rectangle(e.CellBounds.X + 12, e.CellBounds.Y, e.CellBounds.Width - 16, e.CellBounds.Height);
                    TextRenderer.DrawText(e.Graphics, StatusColorHelper.ToTitleCase(text), f, textRect, color,
                        TextFormatFlags.VerticalCenter | TextFormatFlags.Left);
                }

                using (var linePen = new Pen(UiGridHelper.GridBorder, 1f))
                {
                    e.Graphics.DrawLine(linePen, e.CellBounds.Left, e.CellBounds.Bottom - 1, e.CellBounds.Right, e.CellBounds.Bottom - 1);
                }
                e.Handled = true;
            }
        }

        private async void Grid_CellClick(object? sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex < 0 || e.ColumnIndex < 0) return;

            var colActions = _grid.Columns["Actions"];
            if (colActions != null && e.ColumnIndex == colActions.Index)
            {
                if (_grid.Rows[e.RowIndex].Tag is not SyncHealthDto record) return;

                var menu = new ContextMenuStrip();
                var retryItem = new ToolStripMenuItem("🔄  Retry Sync");
                retryItem.Click += async (_, _) =>
                {
                    bool ok = await _controller.RetrySyncAsync(record.CompanyId);
                    if (ok)
                    {
                        MessageBox.Show($"Sync retry triggered for {record.CompanyName}.", "Sync Retried", MessageBoxButtons.OK, MessageBoxIcon.Information);
                        _ = LoadDataAsync();
                    }
                    else
                    {
                        MessageBox.Show("Failed to trigger sync retry.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                };
                menu.Items.Add(retryItem);

                var cellRect = _grid.GetCellDisplayRectangle(e.ColumnIndex, e.RowIndex, false);
                menu.Show(_grid, new Point(cellRect.Left, cellRect.Bottom));
            }
        }
    }
}
