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
    public class TenantsView : UserControl
    {
        private DataGridView _grid = null!;
        private Panel _topPanel = null!;
        private Button _createBtn = null!;
        private TextBox _txtSearch = null!;
        private PaginationControl _pagination = null!;
        private readonly SuperAdminTenantController _controller;
        private List<TenantGridDto> _allTenants = new();

        public event Action<int>? NavigateToSubscription;
        public event Action<int>? NavigateToSyncHealth;

        public TenantsView()
        {
            _controller = new SuperAdminTenantController();
            InitializeComponent();
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
                Text = "Tenant & Company Management",
                Font = UiStyleConstants.PageTitleFont,
                ForeColor = Theme.TextPrimary,
                AutoSize = true,
                Location = new Point(28, 18)
            };

            var subLabel = new Label
            {
                Text = "Manage tenant organizations, administrator accounts, subscriptions, and operational statuses.",
                Font = UiStyleConstants.SubtitleFont,
                ForeColor = Theme.TextSecondary,
                AutoSize = true,
                Location = new Point(28, 56)
            };

            _createBtn = new Button
            {
                Text = "＋ Create Tenant",
                BackColor = Theme.Primary,
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Width = 144,
                Height = 38,
                Anchor = AnchorStyles.Top | AnchorStyles.Right,
                Font = new Font("Segoe UI", 9.5f, FontStyle.Bold),
                Cursor = Cursors.Hand
            };
            _createBtn.FlatAppearance.BorderSize = 0;
            UiRadiusHelper.StyleButton(_createBtn, 6);
            _createBtn.Click += CreateBtn_Click;
            _createBtn.Location = new Point(_topPanel.Width - 172, 29);
            _topPanel.SizeChanged += (_, _) => _createBtn.Location = new Point(_topPanel.Width - 172, 29);

            _topPanel.Controls.Add(titleLabel);
            _topPanel.Controls.Add(subLabel);
            _topPanel.Controls.Add(_createBtn);

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

            // Card Header inside Table Card
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
                Text = "All Tenant Organizations",
                Font = new Font("Segoe UI", 12f, FontStyle.Bold),
                ForeColor = Color.FromArgb(15, 23, 42),
                AutoSize = true,
                Location = new Point(24, 16)
            };
            pnlCardHeader.Controls.Add(lblCardTitle);

            _txtSearch = new TextBox
            {
                PlaceholderText = "Search by company, admin, or tier...",
                Font = new Font("Segoe UI", 9.5f),
                Size = new Size(280, 32),
                Anchor = AnchorStyles.Top | AnchorStyles.Right,
                Location = new Point(pnlCardHeader.Width - 304, 14)
            };
            _txtSearch.TextChanged += (_, _) => ApplyFilter(resetPage: true);
            pnlCardHeader.SizeChanged += (_, _) =>
                _txtSearch.Location = new Point(pnlCardHeader.Width - 304, 14);
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
                HeaderText = "Company Name",
                DataPropertyName = "CompanyName",
                FillWeight = 26,
                MinimumWidth = 180
            });
            _grid.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = "PrimaryAdmin",
                HeaderText = "Primary Admin",
                DataPropertyName = "PrimaryAdminName",
                FillWeight = 22,
                MinimumWidth = 150
            });
            _grid.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = "TierLevel",
                HeaderText = "Plan Tier",
                DataPropertyName = "TierLevel",
                FillWeight = 16,
                MinimumWidth = 120
            });
            _grid.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = "Status",
                HeaderText = "Status",
                DataPropertyName = "Status",
                FillWeight = 12,
                MinimumWidth = 90
            });
            _grid.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = "CreatedDate",
                HeaderText = "Created Date",
                DataPropertyName = "CreatedDateFormatted",
                FillWeight = 14,
                MinimumWidth = 110
            });
            _grid.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = "UserCount",
                HeaderText = "Users",
                DataPropertyName = "UserCount",
                FillWeight = 8,
                MinimumWidth = 70
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
                _allTenants = await _controller.GetTenantsAsync();
                ApplyFilter(resetPage: true);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to load tenants: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void ApplyFilter(bool resetPage = false)
        {
            if (resetPage)
            {
                _pagination.ResetPage();
            }

            var query = _allTenants.AsEnumerable();
            string s = _txtSearch?.Text.Trim() ?? "";
            if (!string.IsNullOrEmpty(s))
            {
                query = query.Where(t =>
                    t.CompanyName.Contains(s, StringComparison.OrdinalIgnoreCase) ||
                    t.CompanyCode.Contains(s, StringComparison.OrdinalIgnoreCase) ||
                    t.PrimaryAdminName.Contains(s, StringComparison.OrdinalIgnoreCase) ||
                    t.TierLevel.Contains(s, StringComparison.OrdinalIgnoreCase));
            }

            var fullList = query.ToList();
            int total = fullList.Count;

            var paged = fullList
                .Skip((_pagination.CurrentPage - 1) * _pagination.PageSize)
                .Take(_pagination.PageSize)
                .Select(t => new
                {
                    t.CompanyId,
                    t.CompanyName,
                    t.PrimaryAdminName,
                    t.TierLevel,
                    t.Status,
                    CreatedDateFormatted = t.CreatedAt.ToLocalTime().ToString("MMM dd, yyyy"),
                    t.UserCount,
                    Raw = t
                }).ToList();

            _grid.Rows.Clear();
            foreach (var item in paged)
            {
                int rowIndex = _grid.Rows.Add(
                    item.CompanyId,
                    item.CompanyName,
                    item.PrimaryAdminName,
                    item.TierLevel,
                    item.Status,
                    item.CreatedDateFormatted,
                    item.UserCount,
                    "");
                _grid.Rows[rowIndex].Tag = item.Raw;
            }

            _pagination.UpdatePagination(total, _pagination.CurrentPage, _pagination.PageSize);
        }

        private void Grid_CellPainting(object? sender, DataGridViewCellPaintingEventArgs e)
        {
            if (e.RowIndex < 0 || e.Graphics == null) return;

            var colCompany = _grid.Columns["CompanyName"];
            var colAdmin = _grid.Columns["PrimaryAdmin"];
            var colStatus = _grid.Columns["Status"];
            var colTier = _grid.Columns["TierLevel"];

            if ((colCompany != null && e.ColumnIndex == colCompany.Index) ||
                (colAdmin != null && e.ColumnIndex == colAdmin.Index))
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
            else if ((colStatus != null && e.ColumnIndex == colStatus.Index) ||
                     (colTier != null && e.ColumnIndex == colTier.Index))
            {
                e.PaintBackground(e.CellBounds, true);
                var text = e.Value?.ToString() ?? "";
                if (!string.IsNullOrEmpty(text))
                {
                    var color = StatusColorHelper.GetTextColor(text);
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
                if (_grid.Rows[e.RowIndex].Tag is not TenantGridDto tenant) return;
                int tenantId = tenant.CompanyId;
                string status = tenant.Status;

                var menu = new ContextMenuStrip();

                if (string.Equals(status, "Active", StringComparison.OrdinalIgnoreCase))
                {
                    var suspendItem = new ToolStripMenuItem("⏸  Suspend Tenant (Block Login)");
                    suspendItem.Click += async (_, _) =>
                    {
                        var confirm = MessageBox.Show(
                            $"Are you sure you want to suspend tenant '{tenant.CompanyName}'?\n\nThis will prevent all users from logging in until reactivated.",
                            "Confirm Suspension",
                            MessageBoxButtons.YesNo,
                            MessageBoxIcon.Warning);

                        if (confirm == DialogResult.Yes)
                        {
                            bool ok = await _controller.SuspendTenantAsync(tenantId);
                            if (ok)
                            {
                                MessageBox.Show($"Tenant '{tenant.CompanyName}' has been suspended.", "Tenant Suspended", MessageBoxButtons.OK, MessageBoxIcon.Information);
                                _ = LoadDataAsync();
                            }
                            else
                            {
                                MessageBox.Show("Failed to suspend tenant.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                            }
                        }
                    };
                    menu.Items.Add(suspendItem);
                }
                else
                {
                    var reactivateItem = new ToolStripMenuItem("▶  Reactivate Tenant");
                    reactivateItem.Click += async (_, _) =>
                    {
                        bool ok = await _controller.ReactivateTenantAsync(tenantId);
                        if (ok)
                        {
                            MessageBox.Show($"Tenant '{tenant.CompanyName}' has been reactivated.", "Tenant Reactivated", MessageBoxButtons.OK, MessageBoxIcon.Information);
                            _ = LoadDataAsync();
                        }
                        else
                        {
                            MessageBox.Show("Failed to reactivate tenant.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        }
                    };
                    menu.Items.Add(reactivateItem);
                }

                menu.Items.Add(new ToolStripSeparator());

                var subItem = new ToolStripMenuItem("💳  View Subscription");
                subItem.Click += (_, _) => NavigateToSubscription?.Invoke(tenantId);
                menu.Items.Add(subItem);

                var syncItem = new ToolStripMenuItem("🔄  View Sync Health");
                syncItem.Click += (_, _) => NavigateToSyncHealth?.Invoke(tenantId);
                menu.Items.Add(syncItem);

                var cellRect = _grid.GetCellDisplayRectangle(e.ColumnIndex, e.RowIndex, false);
                menu.Show(_grid, new Point(cellRect.Left, cellRect.Bottom));
            }
        }

        private async void CreateBtn_Click(object? sender, EventArgs e)
        {
            using var dlg = new CreateTenantDialog();
            if (dlg.ShowDialog(this) == DialogResult.OK)
            {
                var (success, error) = await _controller.CreateTenantAsync(dlg.Request);
                if (success)
                {
                    MessageBox.Show(
                        $"Tenant organization '{dlg.Request.CompanyName}' was successfully created with plan '{dlg.Request.TierLevel}'.",
                        "Tenant Created",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Information);
                    _ = LoadDataAsync();
                }
                else
                {
                    MessageBox.Show($"Failed to create tenant: {error}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }
    }
}
