using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using CRMS_Peguit.winforms.Controllers;
using CRMS_Peguit.winforms.Controls;
using CRMS_Peguit.winforms.Models.Services;

// =============================================================================
// SubscriptionsView — Tenant subscription management.
//
// DATA BOUNDARY: SuperAdminSubscriptionController accesses
// MasterCrmsDbContext.Companies + Subscriptions ONLY.
// NO Customer/Lead/Deal/Property/Activity/SupportTicket/TaskReminder/Notification.
// =============================================================================

namespace CRMS_Peguit.winforms.Views.SuperAdmin
{
    public class SubscriptionsView : UserControl
    {
        private readonly SuperAdminSubscriptionController _controller = new();
        private List<TenantSubscriptionDto> _allSubscriptions = new();
        private string _activeTierFilter = "All";

        private TextBox _txtSearch = null!;
        private DataGridView _grid = null!;
        private Button _btnChangeTier = null!;
        private Button _btnRenew = null!;
        private Panel _pnlPills = null!;

        public SubscriptionsView()
        {
            InitializeComponent();
            _ = LoadDataAsync();
        }

        private void InitializeComponent()
        {
            Dock = DockStyle.Fill;
            BackColor = Theme.Background;

            // 1. Page Header (Height = 76)
            var pnlPageHeader = new Panel
            {
                Dock = DockStyle.Top,
                Height = 76,
                BackColor = Theme.Surface,
                Padding = new Padding(28, 14, 28, 0)
            };
            pnlPageHeader.Paint += (s, e) =>
            {
                using var p = new Pen(Theme.Border, 1f);
                e.Graphics.DrawLine(p, 0, pnlPageHeader.Height - 1, pnlPageHeader.Width, pnlPageHeader.Height - 1);
            };

            var lblTitle = new Label
            {
                Text = "Tenant Subscriptions",
                Font = new Font("Segoe UI", 16f, FontStyle.Bold),
                ForeColor = Theme.TextPrimary,
                AutoSize = true,
                Location = new Point(28, 14)
            };
            var lblSub = new Label
            {
                Text = "Manage tenant billing tiers, subscription statuses, and annual renewals — Billing metadata only",
                Font = new Font("Segoe UI", 9.5f),
                ForeColor = Theme.TextSecondary,
                AutoSize = true,
                Location = new Point(28, 42)
            };
            pnlPageHeader.Controls.Add(lblSub);
            pnlPageHeader.Controls.Add(lblTitle);

            // 2. Toolbar (Height = 56)
            var pnlToolbar = new Panel
            {
                Dock = DockStyle.Top,
                Height = 56,
                Padding = new Padding(24, 10, 24, 0),
                BackColor = Theme.Background
            };

            _txtSearch = new TextBox
            {
                PlaceholderText = "🔍  Search company code or name...",
                Font = new Font("Segoe UI", 10f),
                Size = new Size(260, 32),
                Location = new Point(24, 12)
            };
            _txtSearch.TextChanged += (_, _) => ApplyFilter();
            pnlToolbar.Controls.Add(_txtSearch);

            // Tier filter pills
            _pnlPills = new Panel
            {
                Location = new Point(296, 12),
                Size = new Size(420, 34),
                BackColor = Color.Transparent
            };
            BuildPills();
            pnlToolbar.Controls.Add(_pnlPills);

            // Action buttons on right
            var pnlActions = new FlowLayoutPanel
            {
                FlowDirection = FlowDirection.RightToLeft,
                Anchor = AnchorStyles.Top | AnchorStyles.Right,
                Height = 40,
                Width = 320,
                Location = new Point(pnlToolbar.Width - 344, 8),
                BackColor = Color.Transparent
            };
            pnlToolbar.SizeChanged += (_, _) =>
                pnlActions.Location = new Point(pnlToolbar.Width - 344, 8);

            _btnRenew = new Button
            {
                Text = "↻  Renew (+1 Yr)",
                Size = new Size(135, 34),
                BackColor = Theme.Surface,
                ForeColor = Theme.TextPrimary,
                Font = new Font("Segoe UI", 9f, FontStyle.Bold),
                Cursor = Cursors.Hand,
                Margin = new Padding(4, 0, 0, 0)
            };
            UiRadiusHelper.StyleButton(_btnRenew, 6);
            _btnRenew.Click += BtnRenew_Click;

            _btnChangeTier = new Button
            {
                Text = "⚡  Change Tier",
                Size = new Size(130, 34),
                BackColor = Theme.Primary,
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 9f, FontStyle.Bold),
                Cursor = Cursors.Hand,
                Margin = new Padding(4, 0, 0, 0)
            };
            UiRadiusHelper.StyleButton(_btnChangeTier, 6);
            _btnChangeTier.Click += BtnChangeTier_Click;

            pnlActions.Controls.Add(_btnRenew);
            pnlActions.Controls.Add(_btnChangeTier);
            pnlToolbar.Controls.Add(pnlActions);

            // 3. Grid wrapper (Dock = Fill)
            var pnlWrapper = new Panel
            {
                Dock = DockStyle.Fill,
                Padding = new Padding(24, 8, 24, 24),
                BackColor = Theme.Background
            };

            var pnlCard = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Theme.Surface,
                Padding = new Padding(1)
            };
            UiRadiusHelper.StyleCard(pnlCard, 8);

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
                AutoGenerateColumns = false,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill
            };
            UiGridHelper.ApplyModernGridStyle(_grid, rowHeight: 52);

            _grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "SubscriptionId", DataPropertyName = "SubscriptionId", Visible = false });
            _grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "CompanyId", DataPropertyName = "CompanyId", Visible = false });

            _grid.Columns.Add(new DataGridViewTextBoxColumn
            {
                HeaderText = "CODE",
                Name = "CompanyCode",
                DataPropertyName = "CompanyCode",
                FillWeight = 12,
                MinimumWidth = 80
            });
            _grid.Columns.Add(new DataGridViewTextBoxColumn
            {
                HeaderText = "ORGANIZATION / TENANT",
                Name = "CompanyName",
                DataPropertyName = "CompanyName",
                FillWeight = 30,
                MinimumWidth = 180
            });
            _grid.Columns.Add(new DataGridViewTextBoxColumn
            {
                HeaderText = "SUBSCRIPTION TIER",
                Name = "PlanName",
                DataPropertyName = "PlanName",
                FillWeight = 20,
                MinimumWidth = 140
            });
            _grid.Columns.Add(new DataGridViewTextBoxColumn
            {
                HeaderText = "MONTHLY BILLING",
                Name = "BillingAmountFormatted",
                DataPropertyName = "BillingAmountFormatted",
                FillWeight = 18,
                MinimumWidth = 120
            });
            _grid.Columns.Add(new DataGridViewTextBoxColumn
            {
                HeaderText = "STATUS",
                Name = "Status",
                DataPropertyName = "Status",
                FillWeight = 12,
                MinimumWidth = 90
            });
            _grid.Columns.Add(new DataGridViewTextBoxColumn
            {
                HeaderText = "END DATE / EXPIRY",
                Name = "EndDateFormatted",
                DataPropertyName = "EndDateFormatted",
                FillWeight = 16,
                MinimumWidth = 120
            });

            _grid.CellPainting += (s, e) =>
            {
                if (e.RowIndex < 0 || e.Graphics == null) return;
                string colName = _grid.Columns[e.ColumnIndex].Name;

                if (colName == "Status" && e.Value != null)
                {
                    string status = e.Value.ToString() ?? "Active";
                    UiGridHelper.PaintStatusText(_grid, e, status, center: false);
                }
                else if (colName == "PlanName" && e.Value != null)
                {
                    string plan = e.Value.ToString() ?? "";
                    Color planColor = plan.Contains("Tenant C") ? Color.FromArgb(109, 40, 217)
                        : (plan.Contains("Tenant B") ? Color.FromArgb(3, 105, 161) : Color.FromArgb(71, 85, 105));
                    if (e.CellStyle != null)
                    {
                        e.CellStyle.ForeColor = planColor;
                        e.CellStyle.Font = new Font("Segoe UI", 9.5f, FontStyle.Bold);
                    }
                }
            };

            pnlCard.Controls.Add(_grid);
            pnlWrapper.Controls.Add(pnlCard);

            // Add in reverse docking order
            Controls.Add(pnlWrapper);    // Dock = Fill
            Controls.Add(pnlToolbar);    // Dock = Top (below Header)
            Controls.Add(pnlPageHeader); // Dock = Top (at the very top)
        }

        private void BuildPills()
        {
            _pnlPills.Controls.Clear();
            string[] tierFilters = { "All", "Tenant A", "Tenant B", "Tenant C" };
            int x = 0;
            foreach (var filter in tierFilters)
            {
                bool isActive = filter == _activeTierFilter;
                var btn = new Button
                {
                    Text = filter,
                    Size = new Size(filter == "All" ? 54 : 92, 32),
                    Location = new Point(x, 0),
                    FlatStyle = FlatStyle.Flat,
                    Font = new Font("Segoe UI", 9f, isActive ? FontStyle.Bold : FontStyle.Regular),
                    BackColor = isActive ? Theme.Primary : Theme.Surface,
                    ForeColor = isActive ? Color.White : Theme.TextSecondary,
                    Cursor = Cursors.Hand,
                    Tag = filter
                };
                UiRadiusHelper.StyleButton(btn, 6);
                btn.Click += (s, _) =>
                {
                    _activeTierFilter = btn.Tag?.ToString() ?? "All";
                    BuildPills();
                    ApplyFilter();
                };
                _pnlPills.Controls.Add(btn);
                x += btn.Width + 6;
            }
        }

        private async Task LoadDataAsync()
        {
            try
            {
                _allSubscriptions = await _controller.GetAllSubscriptionsAsync();
                ApplyFilter();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to load subscriptions: {ex.Message}",
                    "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void ApplyFilter()
        {
            var query = _allSubscriptions.AsEnumerable();

            var s = _txtSearch.Text.Trim();
            if (!string.IsNullOrEmpty(s))
                query = query.Where(x => x.CompanyName.Contains(s, StringComparison.OrdinalIgnoreCase) ||
                                         x.CompanyCode.Contains(s, StringComparison.OrdinalIgnoreCase));

            if (_activeTierFilter != "All")
                query = query.Where(x => x.PlanName.Contains(_activeTierFilter, StringComparison.OrdinalIgnoreCase));

            var list = query.Select(x => new
            {
                x.SubscriptionId,
                x.CompanyId,
                x.CompanyCode,
                x.CompanyName,
                x.PlanName,
                BillingAmountFormatted = $"₱{x.BillingAmount:N2}",
                x.Status,
                EndDateFormatted = x.EndDate?.ToString("MMM dd, yyyy") ?? "Lifetime"
            }).ToList();

            _grid.DataSource = list;
            UiGridHelper.EnforceTableStandards(_grid);
        }

        private async void BtnChangeTier_Click(object? sender, EventArgs e)
        {
            if (_grid.CurrentRow?.DataBoundItem == null)
            {
                MessageBox.Show("Please select a tenant to modify.", "Notice",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            dynamic row = _grid.CurrentRow.DataBoundItem;
            int subId = row.SubscriptionId;
            int compId = row.CompanyId;
            string compName = row.CompanyName;
            string currentPlan = row.PlanName;
            string currentStatus = row.Status;

            var currentSub = _allSubscriptions.FirstOrDefault(s => s.CompanyId == compId);
            decimal currentAmount = currentSub?.BillingAmount ?? 2500m;

            using var dlg = new SubscriptionTierChangeDialog(compName, currentPlan, currentStatus, currentAmount);
            if (dlg.ShowDialog(this) != DialogResult.OK) return;

            bool ok = await _controller.UpdateSubscriptionAsync(
                subId, dlg.SelectedPlan, dlg.SelectedStatus, dlg.BillingAmount,
                DateTime.UtcNow.AddYears(1));

            if (!ok) await _controller.ChangeTenantTierAsync(compId, dlg.SelectedPlan);

            MessageBox.Show($"'{compName}' plan updated to {dlg.SelectedPlan} ({dlg.SelectedStatus}).",
                "Updated", MessageBoxButtons.OK, MessageBoxIcon.Information);
            await LoadDataAsync();
        }

        private async void BtnRenew_Click(object? sender, EventArgs e)
        {
            if (_grid.CurrentRow?.DataBoundItem == null)
            {
                MessageBox.Show("Please select a tenant to renew.", "Notice",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            dynamic row = _grid.CurrentRow.DataBoundItem;
            int subId = row.SubscriptionId;
            string compName = row.CompanyName;
            string currentPlan = row.PlanName;

            var currentSub = _allSubscriptions.FirstOrDefault(s => s.SubscriptionId == subId);
            decimal amount = currentSub?.BillingAmount ?? 2500m;
            var newEnd = (currentSub?.EndDate ?? DateTime.UtcNow).AddYears(1);

            bool ok = await _controller.UpdateSubscriptionAsync(subId, currentPlan, "Active", amount, newEnd);
            if (ok)
            {
                MessageBox.Show($"'{compName}' renewed through {newEnd:MMM dd, yyyy}.",
                    "Renewed", MessageBoxButtons.OK, MessageBoxIcon.Information);
                await LoadDataAsync();
            }
        }
    }
}
