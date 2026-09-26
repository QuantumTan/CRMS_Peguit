using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using CRMS_Peguit.domain.Common;
using CRMS_Peguit.domain.entities;
using CRMS_Peguit.winforms.Auth;
using CRMS_Peguit.winforms.Controls;
using CRMS_Peguit.winforms.Models.Services;
using CRMS_Peguit.winforms.Services;

namespace CRMS_Peguit.winforms.Views.Branching
{
    public class BranchesView : UserControl
    {
        private readonly BranchApiService _controller;
        private List<BranchItemDto> _branches = new();

        private Panel _pnlHeader = null!;
        private KpiCard _kpiTotal = null!;
        private KpiCard _kpiActive = null!;
        private KpiCard _kpiStaff = null!;
        private KpiCard _kpiRevenue = null!;

        private TextBox _txtSearch = null!;
        private DataGridView _grid = null!;
        private PaginationControl _pagination = null!;
        private Button _btnAdd = null!;
        private Button _btnEdit = null!;
        private Button _btnToggleStatus = null!;
        private Button _btnSetActiveBranch = null!;
        private Label _lblActiveBranchBanner = null!;

        public event Action<int?, string?>? ActiveBranchChanged;

        public BranchesView()
        {
            _controller = new BranchApiService();
            InitializeComponent();
            LoadDataAsync();
        }

        private void InitializeComponent()
        {
            Dock = DockStyle.Fill;
            BackColor = Theme.Background;
            AutoScroll = true;

            // 1. TOP HEADER
            _pnlHeader = new Panel
            {
                Dock = DockStyle.Top,
                Height = 85,
                BackColor = Theme.Surface,
                Padding = new Padding(24, 16, 24, 0)
            };

            var lblTitle = new Label
            {
                Text = "🏢 Multi-Branch Management (Tenant C)",
                Font = new Font("Segoe UI", 18f, FontStyle.Bold),
                ForeColor = Theme.TextPrimary,
                AutoSize = true,
                Location = new Point(24, 14)
            };
            _pnlHeader.Controls.Add(lblTitle);

            var lblSubtitle = new Label
            {
                Text = "Enterprise Tier Feature — Branch offices, localized staff assignment & regional transaction scoping",
                Font = new Font("Segoe UI", 9.5f),
                ForeColor = Theme.TextSecondary,
                AutoSize = true,
                Location = new Point(24, 46)
            };
            _pnlHeader.Controls.Add(lblSubtitle);

            var btnRefresh = new Button
            {
                Text = "↻ Refresh",
                Size = new Size(90, 32),
                Location = new Point(_pnlHeader.Width - 114, 20),
                Anchor = AnchorStyles.Top | AnchorStyles.Right,
                Font = new Font("Segoe UI", 9f, FontStyle.Bold),
                Cursor = Cursors.Hand,
                BackColor = Theme.Surface,
                ForeColor = Theme.TextPrimary
            };
            UiRadiusHelper.StyleButton(btnRefresh, 6);
            btnRefresh.Click += (_, _) => LoadDataAsync();
            _pnlHeader.Controls.Add(btnRefresh);

            Controls.Add(_pnlHeader);

            // 2. MAIN CONTENT WRAPPER
            var pnlContent = new Panel
            {
                Dock = DockStyle.Fill,
                Padding = new Padding(24, 16, 24, 24),
                AutoScroll = true
            };

            // Active Branch Banner
            var pnlBanner = new Panel
            {
                Dock = DockStyle.Top,
                Height = 40,
                BackColor = Color.FromArgb(243, 232, 255),
                Padding = new Padding(14, 0, 14, 0)
            };
            UiRadiusHelper.StyleCard(pnlBanner, 6);

            _lblActiveBranchBanner = new Label
            {
                Dock = DockStyle.Fill,
                BackColor = Color.Transparent,
                ForeColor = Color.FromArgb(107, 33, 168),  // Dark purple
                Font = new Font("Segoe UI", 9.5f, FontStyle.Bold),
                TextAlign = ContentAlignment.MiddleLeft,
                Text = "📍 Current Working Context: All Branches (Company-Wide Overview)"
            };
            pnlBanner.Controls.Add(_lblActiveBranchBanner);
            // (added to pnlContent later, in correct dock order)


            // 3. KPI CARDS
            var pnlKpis = new TableLayoutPanel
            {
                Dock = DockStyle.Top,
                Height = 115,
                ColumnCount = 4,
                RowCount = 1,
                Padding = new Padding(0, 12, 0, 12)
            };
            pnlKpis.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25f));
            pnlKpis.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25f));
            pnlKpis.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25f));
            pnlKpis.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25f));

            _kpiTotal = new KpiCard { Dock = DockStyle.Fill };
            _kpiActive = new KpiCard { Dock = DockStyle.Fill };
            _kpiStaff = new KpiCard { Dock = DockStyle.Fill };
            _kpiRevenue = new KpiCard { Dock = DockStyle.Fill };

            pnlKpis.Controls.Add(_kpiTotal, 0, 0);
            pnlKpis.Controls.Add(_kpiActive, 1, 0);
            pnlKpis.Controls.Add(_kpiStaff, 2, 0);
            pnlKpis.Controls.Add(_kpiRevenue, 3, 0);
            // (added to pnlContent later, in correct dock order)


            // 4. TOOLBAR
            var pnlToolbar = new Panel
            {
                Dock = DockStyle.Top,
                Height = 48,
                Padding = new Padding(0, 4, 0, 8)
            };

            _txtSearch = new TextBox
            {
                PlaceholderText = "Search by branch code, name, or city...",
                Font = new Font("Segoe UI", 10f),
                Size = new Size(300, 32),
                Location = new Point(0, 6)
            };
            UiRadiusHelper.SetPadding(_txtSearch, 6, 6);
            _txtSearch.TextChanged += (_, _) => ApplyFilter();
            pnlToolbar.Controls.Add(_txtSearch);

            _btnAdd = new Button
            {
                Text = "➕  New Branch",
                Size = new Size(130, 32),
                Anchor = AnchorStyles.Top | AnchorStyles.Right,
                Location = new Point(pnlContent.Width - 460, 6),
                BackColor = Theme.Primary,
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 9f, FontStyle.Bold),
                Cursor = Cursors.Hand
            };
            UiRadiusHelper.StyleButton(_btnAdd, 6);
            _btnAdd.Click += BtnAdd_Click;
            pnlToolbar.Controls.Add(_btnAdd);

            _btnEdit = new Button
            {
                Text = "✏️ Edit",
                Size = new Size(80, 32),
                Anchor = AnchorStyles.Top | AnchorStyles.Right,
                Location = new Point(pnlContent.Width - 320, 6),
                BackColor = Theme.Surface,
                ForeColor = Theme.TextPrimary,
                Font = new Font("Segoe UI", 9f, FontStyle.Bold),
                Cursor = Cursors.Hand
            };
            UiRadiusHelper.StyleButton(_btnEdit, 6);
            _btnEdit.Click += BtnEdit_Click;
            pnlToolbar.Controls.Add(_btnEdit);

            _btnSetActiveBranch = new Button
            {
                Text = "📍 Set Active",
                Size = new Size(110, 32),
                Anchor = AnchorStyles.Top | AnchorStyles.Right,
                Location = new Point(pnlContent.Width - 230, 6),
                BackColor = Theme.Surface,
                ForeColor = Color.FromArgb(107, 33, 168),
                Font = new Font("Segoe UI", 9f, FontStyle.Bold),
                Cursor = Cursors.Hand
            };
            UiRadiusHelper.StyleButton(_btnSetActiveBranch, 6);
            _btnSetActiveBranch.Click += BtnSetActiveBranch_Click;
            pnlToolbar.Controls.Add(_btnSetActiveBranch);

            _btnToggleStatus = new Button
            {
                Text = "Toggle Status",
                Size = new Size(105, 32),
                Anchor = AnchorStyles.Top | AnchorStyles.Right,
                Location = new Point(pnlContent.Width - 110, 6),
                BackColor = Theme.Surface,
                ForeColor = Theme.TextSecondary,
                Font = new Font("Segoe UI", 9f),
                Cursor = Cursors.Hand
            };
            UiRadiusHelper.StyleButton(_btnToggleStatus, 6);
            _btnToggleStatus.Click += BtnToggleStatus_Click;
            pnlToolbar.Controls.Add(_btnToggleStatus);
            // (added to pnlContent later, in correct dock order)


            // 5. GRID CARD
            var pnlGridCard = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Theme.Surface,
                Padding = new Padding(1)
            };
            UiRadiusHelper.StyleCard(pnlGridCard, 8);

            _grid = new DataGridView
            {
                Dock = DockStyle.Fill,
                BackgroundColor = Theme.Surface,
                BorderStyle = BorderStyle.None,
                RowHeadersVisible = false,
                AllowUserToAddRows = false,
                AllowUserToDeleteRows = false,
                ReadOnly = true,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                MultiSelect = false,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill
            };
            UiGridHelper.ApplyModernGridStyle(_grid);

            _grid.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Branch Code", DataPropertyName = "BranchCode", FillWeight = 14 });
            _grid.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Branch Name", DataPropertyName = "BranchName", FillWeight = 26 });
            _grid.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Address / Location", DataPropertyName = "Address", FillWeight = 22 });
            _grid.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Phone", DataPropertyName = "Phone", FillWeight = 14 });
            _grid.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Assigned Team", DataPropertyName = "AssignedAgentsFormatted", FillWeight = 14 });
            _grid.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Deals Volume", DataPropertyName = "DealVolumeFormatted", FillWeight = 18 });
            _grid.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Status", DataPropertyName = "StatusFormatted", FillWeight = 12 });

            _grid.CellFormatting += (s, e) =>
            {
                if (e.RowIndex >= 0)
                {
                    if (_grid.Columns[e.ColumnIndex].HeaderText == "Branch Code")
                    {
                        e.CellStyle.Font = new Font("Segoe UI", 9.5f, FontStyle.Bold);
                        e.CellStyle.ForeColor = Color.FromArgb(107, 33, 168);
                    }
                    if (_grid.Columns[e.ColumnIndex].HeaderText == "Status" && e.Value != null)
                    {
                        string st = e.Value.ToString() ?? "";
                        e.CellStyle.ForeColor = st.Equals("Active", StringComparison.OrdinalIgnoreCase) ? Theme.StatusSuccess : Theme.StatusAlert;
                        e.CellStyle.Font = new Font("Segoe UI", 9.5f, FontStyle.Bold);
                    }
                }
            };

            _pagination = new PaginationControl();
            _pagination.SetItemLabel("branches");
            _pagination.Dock = DockStyle.Bottom;
            _pagination.PageChanged += (_, _) => ApplyFilter(resetPage: false);
            _pagination.PageSizeChanged += (_, _) => ApplyFilter(resetPage: true);

            pnlGridCard.Controls.Add(_grid);
            pnlGridCard.Controls.Add(_pagination);
            _pagination.BringToFront();

            // ── WinForms dock order: Fill added FIRST, then Top items added in REVERSE display order ──
            // Display order (top→bottom): pnlBanner → pnlKpis → pnlToolbar → pnlGridCard(Fill)
            // Add order to Controls:      pnlGridCard(Fill) first, then pnlToolbar, pnlKpis, pnlBanner

            pnlContent.Controls.Add(pnlGridCard);     // Fill — must be first
            pnlContent.Controls.Add(pnlToolbar);       // Top — added in reverse (last Top shown = added last)
            pnlContent.Controls.Add(pnlKpis);          // Top
            pnlContent.Controls.Add(pnlBanner);        // Top — added last so it renders at the very top

            Controls.Add(pnlContent);
        }

        private static void ConfigureKpi(KpiCard card, string title, object value, string subtitle, Color accentColor, KpiIconType icon)
        {
            card.SetTitle(title);
            if (value is int intVal) card.SetValue(intVal);
            else card.SetValue(value?.ToString() ?? "0");
            card.SetSubtitle(subtitle, Color.FromArgb(100, 116, 139));
            card.SetIcon(icon, accentColor);
        }

        private async void LoadDataAsync()
        {
            try
            {
                _branches = await _controller.GetAllBranchesAsync();

                int total = _branches.Count;
                int active = _branches.Count(b => b.IsActive);
                int totalStaff = _branches.Sum(b => b.AssignedAgentsCount);
                decimal totalVol = _branches.Sum(b => b.TotalDealVolume);

                ConfigureKpi(_kpiTotal, "TOTAL BRANCHES", total, "Registered regional branches", Theme.Primary, KpiIconType.Building);
                ConfigureKpi(_kpiActive, "ACTIVE BRANCHES", active, "Operating branch offices", Theme.StatusSuccess, KpiIconType.Building);
                ConfigureKpi(_kpiStaff, "BRANCH PERSONNEL", totalStaff, "Agents & branch managers", BiDisplayConstants.SkyAccent, KpiIconType.Users);
                ConfigureKpi(_kpiRevenue, "BRANCH VOLUME", $"₱{totalVol:N0}", "Aggregated transaction value", Theme.PrimaryDark, KpiIconType.Currency);

                UpdateActiveBranchBanner();
                ApplyFilter(resetPage: true);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading branches: {ex.Message}", "Branch Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void UpdateActiveBranchBanner()
        {
            if (CurrentSession.ActiveBranchId.HasValue && !string.IsNullOrWhiteSpace(CurrentSession.ActiveBranchName))
            {
                _lblActiveBranchBanner.Text = $"📍 Active Working Context: {CurrentSession.ActiveBranchName} (Filtered View)";
                _lblActiveBranchBanner.BackColor = Color.FromArgb(237, 233, 254);
            }
            else
            {
                _lblActiveBranchBanner.Text = "📍 Active Working Context: All Branches (Company-Wide Overview)";
                _lblActiveBranchBanner.BackColor = Color.FromArgb(243, 232, 255);
            }
        }

        private void ApplyFilter(bool resetPage = false)
        {
            var query = _branches.AsEnumerable();
            if (!string.IsNullOrWhiteSpace(_txtSearch.Text))
            {
                string s = _txtSearch.Text.Trim();
                query = query.Where(b => b.BranchCode.Contains(s, StringComparison.OrdinalIgnoreCase) ||
                                         b.BranchName.Contains(s, StringComparison.OrdinalIgnoreCase) ||
                                         b.Address.Contains(s, StringComparison.OrdinalIgnoreCase));
            }

            var allFiltered = query.ToList();
            int total = allFiltered.Count;
            int page = resetPage ? 1 : (_pagination?.CurrentPage ?? 1);
            int pageSize = _pagination?.PageSize ?? 25;

            _pagination?.UpdatePagination(total, page, pageSize);

            if (total == 0)
            {
                _grid.DataSource = null;
                return;
            }

            int effectivePage = _pagination?.CurrentPage ?? 1;
            var pageItems = allFiltered.Skip((effectivePage - 1) * pageSize).Take(pageSize).ToList();

            var displayList = pageItems.Select(b => new
            {
                b.BranchId,
                b.BranchCode,
                b.BranchName,
                b.Address,
                b.Phone,
                AssignedAgentsFormatted = $"{b.AssignedAgentsCount} Staff · {b.PropertiesCount} Props",
                DealVolumeFormatted = $"₱{b.TotalDealVolume:N2} ({b.DealsCount} Deals)",
                StatusFormatted = b.IsActive ? "Active" : "Inactive"
            }).ToList();

            _grid.DataSource = displayList;
            if (_grid.Columns["BranchId"] is { } col) col.Visible = false;
        }

        private async void BtnAdd_Click(object? sender, EventArgs e)
        {
            using var dlg = new BranchEditDialog();
            if (dlg.ShowDialog(this) == DialogResult.OK)
            {
                bool ok = await _controller.SaveBranchAsync(dlg.BranchResult);
                if (ok)
                {
                    MessageBox.Show($"Branch '{dlg.BranchResult.BranchName}' created successfully!", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    LoadDataAsync();
                }
            }
        }

        private async void BtnEdit_Click(object? sender, EventArgs e)
        {
            if (_grid.CurrentRow?.DataBoundItem == null)
            {
                MessageBox.Show("Please select a branch to edit.", "Notice", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            dynamic row = _grid.CurrentRow.DataBoundItem;
            int branchId = row.BranchId;
            var item = _branches.FirstOrDefault(b => b.BranchId == branchId);
            if (item == null) return;

            var branch = new Branch
            {
                BranchId = item.BranchId,
                TenantId = item.TenantId,
                BranchCode = item.BranchCode,
                BranchName = item.BranchName,
                Address = item.Address,
                Phone = item.Phone,
                IsActive = item.IsActive
            };

            using var dlg = new BranchEditDialog(branch);
            if (dlg.ShowDialog(this) == DialogResult.OK)
            {
                bool ok = await _controller.SaveBranchAsync(dlg.BranchResult);
                if (ok)
                {
                    MessageBox.Show($"Branch '{dlg.BranchResult.BranchName}' updated successfully!", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    LoadDataAsync();
                }
            }
        }

        private async void BtnToggleStatus_Click(object? sender, EventArgs e)
        {
            if (_grid.CurrentRow?.DataBoundItem == null)
            {
                MessageBox.Show("Please select a branch.", "Notice", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            dynamic row = _grid.CurrentRow.DataBoundItem;
            int branchId = row.BranchId;

            bool ok = await _controller.ToggleBranchStatusAsync(branchId);
            if (ok)
            {
                LoadDataAsync();
            }
        }

        private void BtnSetActiveBranch_Click(object? sender, EventArgs e)
        {
            if (_grid.CurrentRow?.DataBoundItem == null)
            {
                // Reset to all branches
                CurrentSession.SetActiveBranch(null, null);
                UpdateActiveBranchBanner();
                ActiveBranchChanged?.Invoke(null, null);
                MessageBox.Show("Context reset to: All Branches (Company-wide)", "Branch Filter", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            dynamic row = _grid.CurrentRow.DataBoundItem;
            int branchId = row.BranchId;
            string branchName = row.BranchName;

            if (CurrentSession.ActiveBranchId == branchId)
            {
                // Toggle back to All Branches
                CurrentSession.SetActiveBranch(null, null);
                UpdateActiveBranchBanner();
                ActiveBranchChanged?.Invoke(null, null);
                MessageBox.Show("Context switched back to: All Branches", "Branch Filter", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            else
            {
                CurrentSession.SetActiveBranch(branchId, branchName);
                UpdateActiveBranchBanner();
                ActiveBranchChanged?.Invoke(branchId, branchName);
                MessageBox.Show($"Active Branch context switched to:\n{branchName} ({row.BranchCode})", "Branch Filter", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }
    }
}
