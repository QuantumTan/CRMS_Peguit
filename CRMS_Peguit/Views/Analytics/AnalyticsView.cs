using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using CRMS_Peguit.winforms.Auth;
using CRMS_Peguit.winforms.Controllers;
using CRMS_Peguit.winforms.Controls;
using CRMS_Peguit.winforms.Models.Analytics;
using CRMS_Peguit.winforms.Models.Services;
using CRMS_Peguit.winforms.Services;
using Color = System.Drawing.Color;
using Label = System.Windows.Forms.Label;
using FontStyle = System.Drawing.FontStyle;

namespace CRMS_Peguit.winforms.Views.Analytics
{
    public partial class AnalyticsView : UserControl
    {
        private readonly AnalyticsController _controller;
        private AnalyticsSnapshot? _currentSnapshot;
        private List<AnalyticsDetailRow> _allDrillDownRows = new();
        private string? _activeFilterCategory = null;
        private string? _activeFilterKey = null;
        private GridSkeletonOverlay _gridSkeleton = null!;

        public event Action<string>? NavigationRequested;

        public AnalyticsView()
        {
            InitializeComponent();
            _controller = new AnalyticsController();

            _gridSkeleton = GridSkeletonOverlay.CreateForGrid(gridAnalyticsDetails);

            ApplyStyling();
            BindEvents();
            ApplyRoleBasedRendering();
            ResponsiveLayout.BindHeader(pnlHeader, lblTitle, lblSubtitle, btnGoToReports, cboDateRange, btnExport);
            ResponsiveLayout.BindHeader(pnlGridHeader, lblGridTitle, lblGridSubtitle, btnResetFilter);

            // Default to "This Year" to immediately display populated metrics
            cboDateRange.SelectedIndex = 2; // triggers ReloadSnapshot
        }

        private void ApplyStyling()
        {
            this.BackColor = Theme.Background;

            UiRadiusHelper.StyleCard(pnlRecentActivity, 12);
            UiRadiusHelper.StyleCard(pnlGridCard, 12);

            UiRadiusHelper.StyleButton(btnExport, 8);
            btnExport.BackColor = Theme.Primary;
            btnExport.ForeColor = Color.White;
            btnExport.Font = new Font("Segoe UI Semibold", 9.5f);
            UiRadiusHelper.AttachHoverFeedback(btnExport, Theme.Primary, Theme.PrimaryDark);

            UiRadiusHelper.StyleButton(btnGoToReports, 8);
            btnGoToReports.BackColor = Color.White;
            btnGoToReports.ForeColor = Theme.Primary;
            btnGoToReports.FlatAppearance.BorderColor = Color.FromArgb(203, 213, 225);
            btnGoToReports.FlatAppearance.BorderSize = 1;
            btnGoToReports.Font = new Font("Segoe UI Semibold", 9.5f);
            UiRadiusHelper.AttachHoverFeedback(btnGoToReports, Color.White, Color.FromArgb(248, 250, 252));

            UiRadiusHelper.StyleButton(btnResetFilter, 6);

            // Configure Case 1: InPlaceFilter mode on all 6 KPI cards
            kpiDealsClosed.ClickMode = KpiClickMode.InPlaceFilter;
            kpiCommission.ClickMode = KpiClickMode.InPlaceFilter;
            kpiActiveLeads.ClickMode = KpiClickMode.InPlaceFilter;
            kpiConversionRate.ClickMode = KpiClickMode.InPlaceFilter;
            kpiOpenTickets.ClickMode = KpiClickMode.InPlaceFilter;
            kpiAvgDays.ClickMode = KpiClickMode.InPlaceFilter;

            // Configure Case 1: InPlaceFilter mode on all 6 Chart wrappers
            chartDealsClosed.SetMode(KpiClickMode.InPlaceFilter);
            chartPipeline.SetMode(KpiClickMode.InPlaceFilter);
            chartWonVsLost.SetMode(KpiClickMode.InPlaceFilter);
            chartTickets.SetMode(KpiClickMode.InPlaceFilter);
            chartAgents.SetMode(KpiClickMode.InPlaceFilter);
            chartSources.SetMode(KpiClickMode.InPlaceFilter);

            SetupGridColumns();
        }

        private void SetupGridColumns()
        {
            UiGridHelper.ApplyModernGridStyle(gridAnalyticsDetails, 44);

            gridAnalyticsDetails.AutoGenerateColumns = false;
            gridAnalyticsDetails.Columns.Clear();

            gridAnalyticsDetails.Columns.Add(new DataGridViewTextBoxColumn
            {
                DataPropertyName = "RecordType",
                HeaderText = "TYPE",
                Name = "colRecordType",
                Width = 90
            });
            gridAnalyticsDetails.Columns.Add(new DataGridViewTextBoxColumn
            {
                DataPropertyName = "Reference",
                HeaderText = "REF #",
                Name = "colReference",
                Width = 110
            });
            gridAnalyticsDetails.Columns.Add(new DataGridViewTextBoxColumn
            {
                DataPropertyName = "Title",
                HeaderText = "TITLE / CONTACT / SUBJECT",
                Name = "colTitle",
                AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill,
                MinimumWidth = 200
            });
            gridAnalyticsDetails.Columns.Add(new DataGridViewTextBoxColumn
            {
                DataPropertyName = "Status",
                HeaderText = "STATUS",
                Name = "colStatus",
                Width = 115
            });
            gridAnalyticsDetails.Columns.Add(new DataGridViewTextBoxColumn
            {
                DataPropertyName = "AssignedTo",
                HeaderText = "ASSIGNED AGENT",
                Name = "colAssignedTo",
                Width = 150
            });
            gridAnalyticsDetails.Columns.Add(new DataGridViewTextBoxColumn
            {
                DataPropertyName = "Value",
                HeaderText = "VALUE / BUDGET",
                Name = "colValue",
                Width = 135
            });
            gridAnalyticsDetails.Columns.Add(new DataGridViewTextBoxColumn
            {
                DataPropertyName = "Date",
                HeaderText = "DATE",
                Name = "colDate",
                Width = 115
            });
            gridAnalyticsDetails.Columns.Add(new DataGridViewTextBoxColumn
            {
                DataPropertyName = "Details",
                HeaderText = "DETAILS",
                Name = "colDetails",
                Width = 140
            });

            gridAnalyticsDetails.CellFormatting += (s, e) =>
            {
                if (e.RowIndex < 0) return;
                string colName = gridAnalyticsDetails.Columns[e.ColumnIndex].Name;
                if (colName == "colValue" && e.Value is decimal val)
                {
                    e.Value = val > 0 ? BiDisplayConstants.FormatCompactCurrency(val) : "—";
                    e.FormattingApplied = true;
                }
                else if (colName == "colDate" && e.Value is DateTime dt)
                {
                    e.Value = dt.ToString("MMM dd, yyyy");
                    e.FormattingApplied = true;
                }
            };

            gridAnalyticsDetails.CellPainting += (s, e) =>
            {
                if (e.RowIndex < 0 || e.ColumnIndex < 0) return;
                string colName = gridAnalyticsDetails.Columns[e.ColumnIndex].Name;
                if (colName == "colStatus")
                {
                    e.Handled = true;
                    string st = e.Value?.ToString() ?? "";
                    UiGridHelper.PaintStatusBadge(gridAnalyticsDetails, e, st);
                }
                else if (colName == "colRecordType")
                {
                    e.Handled = true;
                    string t = e.Value?.ToString() ?? "";
                    UiGridHelper.PaintStatusBadge(gridAnalyticsDetails, e, t);
                }
                else if (colName == "colReference")
                {
                    e.Handled = true;
                    using var boldFont = new Font("Segoe UI", 9f, FontStyle.Bold);
                    UiGridHelper.PaintTextCell(gridAnalyticsDetails, e, e.Value?.ToString() ?? "", boldFont, Theme.TextPrimary);
                }
            };
        }

        private void BindEvents()
        {
            cboDateRange.SelectedIndexChanged += (_, _) => ReloadSnapshot();
            btnExport.Click += (_, _) => ExportPdf();

            btnGoToReports.Click += (_, _) =>
            {
                if (ParentForm is MainForm main)
                {
                    main.NavigateTo("Reports");
                }
                else
                {
                    NavigationRequested?.Invoke("Reports");
                }
            };

            pnlHeader.Resize += (_, _) => LayoutHeaderControls();
            pnlScrollableContent.Resize += (_, _) => AutoLayoutCharts();
            this.Resize += (_, _) => { LayoutHeaderControls(); AutoLayoutCharts(); };
            this.Load += (_, _) => { LayoutHeaderControls(); AutoLayoutCharts(); };

            // KPI card click shortcuts (Case 1: in-place drill-down filter)
            kpiDealsClosed.Click += (_, _) => ToggleKpiFilter("KPI:DealsClosed");
            kpiCommission.Click += (_, _) => ToggleKpiFilter("KPI:Commission");
            kpiActiveLeads.Click += (_, _) => ToggleKpiFilter("KPI:ActiveLeads");
            kpiConversionRate.Click += (_, _) => ToggleKpiFilter("KPI:ConversionRate");
            kpiOpenTickets.Click += (_, _) => ToggleKpiFilter("KPI:OpenTickets");
            kpiAvgDays.Click += (_, _) => ToggleKpiFilter("KPI:AvgDays");

            // Chart click events (Case 1: in-place drill-down filter)
            chartDealsClosed.InPlaceFilterChanged += key => ApplyChartFilter(chartDealsClosed, "DealsOverTime", key);
            chartPipeline.InPlaceFilterChanged += key => ApplyChartFilter(chartPipeline, "Pipeline", key);
            chartWonVsLost.InPlaceFilterChanged += key => ApplyChartFilter(chartWonVsLost, "WonLost", key);
            chartTickets.InPlaceFilterChanged += key => ApplyChartFilter(chartTickets, "Tickets", key);
            chartAgents.InPlaceFilterChanged += key => ApplyChartFilter(chartAgents, "Agents", key);
            chartSources.InPlaceFilterChanged += key => ApplyChartFilter(chartSources, "Sources", key);

            btnResetFilter.Click += (_, _) => ResetAllFilters();
        }

        public void SetFilter(string filter)
        {
            if (string.IsNullOrWhiteSpace(filter))
            {
                ResetAllFilters();
                return;
            }

            string fLower = filter.ToLowerInvariant();
            if (fLower.Contains("won") || fLower.Contains("deal") || fLower.Contains("closed"))
            {
                ToggleKpiFilter("KPI:DealsClosed", force: true);
            }
            else if (fLower.Contains("lost"))
            {
                chartWonVsLost.SetActiveFilter("Lost");
                ApplyChartFilter(chartWonVsLost, "WonLost", "Lost");
            }
            else if (fLower.Contains("lead"))
            {
                ToggleKpiFilter("KPI:ActiveLeads", force: true);
            }
            else if (fLower.Contains("comm"))
            {
                ToggleKpiFilter("KPI:Commission", force: true);
            }
            else if (fLower.Contains("ticket"))
            {
                ToggleKpiFilter("KPI:OpenTickets", force: true);
            }
            else if (fLower.Contains("conv"))
            {
                ToggleKpiFilter("KPI:ConversionRate", force: true);
            }
            else
            {
                ResetAllFilters();
            }
        }

        private void ToggleKpiFilter(string category, bool force = false)
        {
            if (!force && string.Equals(_activeFilterCategory, category, StringComparison.OrdinalIgnoreCase))
            {
                ResetAllFilters();
                return;
            }

            _activeFilterCategory = category;
            _activeFilterKey = null;

            // Mutual exclusivity: clear all chart filters
            chartDealsClosed.ClearFilter();
            chartPipeline.ClearFilter();
            chartWonVsLost.ClearFilter();
            chartTickets.ClearFilter();
            chartAgents.ClearFilter();
            chartSources.ClearFilter();

            UpdateKpiSelectionStates();
            ApplyDrillDownFilter();
        }

        private void ApplyChartFilter(ChartWrapperControl activeChart, string category, string? key)
        {
            if (string.IsNullOrEmpty(key))
            {
                ResetAllFilters();
                return;
            }

            _activeFilterCategory = $"Chart:{category}";
            _activeFilterKey = key;

            // Mutual exclusivity: clear all KPI selections
            ClearKpiSelections();

            // Clear all other charts
            if (activeChart != chartDealsClosed) chartDealsClosed.ClearFilter();
            if (activeChart != chartPipeline) chartPipeline.ClearFilter();
            if (activeChart != chartWonVsLost) chartWonVsLost.ClearFilter();
            if (activeChart != chartTickets) chartTickets.ClearFilter();
            if (activeChart != chartAgents) chartAgents.ClearFilter();
            if (activeChart != chartSources) chartSources.ClearFilter();

            ApplyDrillDownFilter();
        }

        private void ResetAllFilters(bool refreshGrid = true)
        {
            _activeFilterCategory = null;
            _activeFilterKey = null;

            ClearKpiSelections();

            chartDealsClosed.ClearFilter();
            chartPipeline.ClearFilter();
            chartWonVsLost.ClearFilter();
            chartTickets.ClearFilter();
            chartAgents.ClearFilter();
            chartSources.ClearFilter();

            if (refreshGrid)
            {
                ApplyDrillDownFilter();
            }
        }

        private void ClearKpiSelections()
        {
            kpiDealsClosed.SetSelected(false);
            kpiCommission.SetSelected(false);
            kpiActiveLeads.SetSelected(false);
            kpiConversionRate.SetSelected(false);
            kpiOpenTickets.SetSelected(false);
            kpiAvgDays.SetSelected(false);
        }

        private void UpdateKpiSelectionStates()
        {
            kpiDealsClosed.SetSelected(string.Equals(_activeFilterCategory, "KPI:DealsClosed", StringComparison.OrdinalIgnoreCase));
            kpiCommission.SetSelected(string.Equals(_activeFilterCategory, "KPI:Commission", StringComparison.OrdinalIgnoreCase));
            kpiActiveLeads.SetSelected(string.Equals(_activeFilterCategory, "KPI:ActiveLeads", StringComparison.OrdinalIgnoreCase));
            kpiConversionRate.SetSelected(string.Equals(_activeFilterCategory, "KPI:ConversionRate", StringComparison.OrdinalIgnoreCase));
            kpiOpenTickets.SetSelected(string.Equals(_activeFilterCategory, "KPI:OpenTickets", StringComparison.OrdinalIgnoreCase));
            kpiAvgDays.SetSelected(string.Equals(_activeFilterCategory, "KPI:AvgDays", StringComparison.OrdinalIgnoreCase));
        }

        private void ApplyDrillDownFilter()
        {
            if (_allDrillDownRows == null) return;

            List<AnalyticsDetailRow> displayList;

            if (string.IsNullOrEmpty(_activeFilterCategory))
            {
                displayList = _allDrillDownRows;
                lblGridSubtitle.Text = $"Showing all {displayList.Count:N0} records. Click any KPI card or chart segment to filter records in-place.";
                btnResetFilter.Visible = false;
            }
            else
            {
                btnResetFilter.Visible = true;
                string cat = _activeFilterCategory;
                string? k = _activeFilterKey;

                displayList = cat switch
                {
                    "KPI:DealsClosed" => _allDrillDownRows.Where(r => r.RecordType == "Deal" && (r.Status.Equals("Won", StringComparison.OrdinalIgnoreCase) || r.Status.Contains("Closed", StringComparison.OrdinalIgnoreCase))).ToList(),
                    "KPI:Commission" => _allDrillDownRows.Where(r => r.RecordType == "Deal" && r.Status.Equals("Won", StringComparison.OrdinalIgnoreCase)).ToList(),
                    "KPI:ActiveLeads" => _allDrillDownRows.Where(r => r.RecordType == "Lead" && !r.Status.Equals("Converted", StringComparison.OrdinalIgnoreCase) && !r.Status.Equals("Lost", StringComparison.OrdinalIgnoreCase)).ToList(),
                    "KPI:ConversionRate" => _allDrillDownRows.Where(r => (r.RecordType == "Lead" && r.Status.Equals("Converted", StringComparison.OrdinalIgnoreCase)) || (r.RecordType == "Deal" && r.Status.Equals("Won", StringComparison.OrdinalIgnoreCase))).ToList(),
                    "KPI:OpenTickets" => _allDrillDownRows.Where(r => r.RecordType == "Ticket" && !r.Status.Equals("Resolved", StringComparison.OrdinalIgnoreCase) && !r.Status.Equals("Closed", StringComparison.OrdinalIgnoreCase)).ToList(),
                    "KPI:AvgDays" => _allDrillDownRows.Where(r => r.RecordType == "Deal" && (r.Status.Equals("Won", StringComparison.OrdinalIgnoreCase) || r.Status.Contains("Closed", StringComparison.OrdinalIgnoreCase))).ToList(),
                    
                    "Chart:WonLost" => k == "Lost"
                        ? _allDrillDownRows.Where(r => r.RecordType == "Deal" && r.Status.Equals("Lost", StringComparison.OrdinalIgnoreCase)).ToList()
                        : _allDrillDownRows.Where(r => r.RecordType == "Deal" && r.Status.Equals("Won", StringComparison.OrdinalIgnoreCase)).ToList(),

                    "Chart:Pipeline" => _allDrillDownRows.Where(r => r.RecordType == "Lead" && r.Status.Equals(k, StringComparison.OrdinalIgnoreCase)).ToList(),

                    "Chart:Tickets" => k == "Overdue"
                        ? _allDrillDownRows.Where(r => r.RecordType == "Ticket" && (r.Status.Equals("Overdue", StringComparison.OrdinalIgnoreCase) || r.Details.Contains("Overdue", StringComparison.OrdinalIgnoreCase))).ToList()
                        : _allDrillDownRows.Where(r => r.RecordType == "Ticket" && r.Status.Equals(k, StringComparison.OrdinalIgnoreCase)).ToList(),

                    "Chart:Agents" => _allDrillDownRows.Where(r => r.AssignedTo.Equals(k, StringComparison.OrdinalIgnoreCase)).ToList(),

                    "Chart:Sources" => _allDrillDownRows.Where(r => r.RecordType == "Lead" && r.Details.Contains(k ?? "", StringComparison.OrdinalIgnoreCase)).ToList(),

                    "Chart:DealsOverTime" => _allDrillDownRows.Where(r => r.RecordType == "Deal" && r.Date.ToString("MMM yyyy").Contains(k ?? "")).ToList(),

                    _ => _allDrillDownRows
                };

                string badgeDesc = cat.StartsWith("KPI:")
                    ? cat.Replace("KPI:", "KPI ")
                    : $"{cat.Replace("Chart:", "")} [{k}]";

                lblGridSubtitle.Text = $"Filtered by: {badgeDesc} — {displayList.Count:N0} records found. (Click 'Clear' or the segment again to reset)";
            }

            gridAnalyticsDetails.DataSource = null;
            gridAnalyticsDetails.DataSource = displayList;
        }

        private void ApplyRoleBasedRendering()
        {
            if (RbacService.IsAgent && !RbacService.HasFullOversight)
            {
                lblTitle.Text = "My Performance Snapshot";
                lblSubtitle.Text = "Personal metrics and pipeline status across your assigned records";
                chartAgents.Visible = false;
                chartSources.Visible = false;
                btnExport.Visible = false;
                btnGoToReports.Visible = false;
            }
            else
            {
                lblTitle.Text = "Team Analytics Performance";
                lblSubtitle.Text = "Live agency-wide business intelligence, deal velocity, and operations";
                chartAgents.Visible = true;
                chartSources.Visible = true;
                btnExport.Visible = RbacService.CanExportData;
                btnGoToReports.Visible = CurrentSession.CanAccess("Reports");
            }

            LayoutHeaderControls();
            AutoLayoutCharts();
        }

private void LayoutHeaderControls()
        {
            // The shared header owns sizing and wrapping; do not overwrite it with fixed coordinates.
            pnlHeader.PerformLayout();
        }

        private DateRangeFilter GetSelectedDateRange()
        {
            return (cboDateRange.SelectedItem?.ToString() ?? "This Year") switch
            {
                "This Month" => DateRangeFilter.ThisMonth(),
                "This Quarter" => DateRangeFilter.ThisQuarter(),
                "This Year" => DateRangeFilter.ThisYear(),
                "Past 12 Months" => DateRangeFilter.Past12Months(),
                "All Time" => DateRangeFilter.AllTime(),
                _ => DateRangeFilter.ThisYear()
            };
        }

        public async void ReloadSnapshot()
        {
            lblLoading.Visible = false;
            lblSubtitle.Visible = false;

            // Show animated loading skeleton placeholders on all 6 KPI cards
            kpiDealsClosed.ShowLoadingSkeleton();
            kpiCommission.ShowLoadingSkeleton();
            kpiActiveLeads.ShowLoadingSkeleton();
            kpiConversionRate.ShowLoadingSkeleton();
            kpiOpenTickets.ShowLoadingSkeleton();
            kpiAvgDays.ShowLoadingSkeleton();

            // Show animated loading skeleton placeholders on charts
            chartDealsClosed.ShowLoadingSkeleton(ChartSkeletonType.Bars);
            chartPipeline.ShowLoadingSkeleton(ChartSkeletonType.Bars);
            chartWonVsLost.ShowLoadingSkeleton(ChartSkeletonType.Donut);
            chartTickets.ShowLoadingSkeleton(ChartSkeletonType.Donut);
            if (chartAgents.Visible) chartAgents.ShowLoadingSkeleton(ChartSkeletonType.Bars);
            if (chartSources.Visible) chartSources.ShowLoadingSkeleton(ChartSkeletonType.Donut);

            // Show animated loading skeleton placeholder on drill-down grid
            _gridSkeleton.ShowSkeleton();

            try
            {
                var range = GetSelectedDateRange();
                AnalyticsSnapshot? snapshot = null;
                List<AnalyticsDetailRow>? drillDown = null;

                await System.Threading.Tasks.Task.Run(() =>
                {
                    snapshot = _controller.GetSnapshot(range);
                    drillDown = _controller.GetDrillDownRows(range);
                });

                if (IsDisposed) return;

                lblSubtitle.Visible = true;
                _currentSnapshot = snapshot;
                _allDrillDownRows = drillDown ?? new();

                ResetAllFilters(refreshGrid: false);
                UpdateView(_currentSnapshot);
                ApplyDrillDownFilter();
            }
            catch (Exception ex)
            {
                lblSubtitle.Visible = true;
                System.Diagnostics.Debug.WriteLine($"[AnalyticsView.ReloadSnapshot] Error: {ex.Message}");
            }
            finally
            {
                _gridSkeleton.HideSkeleton();
            }
        }

        private void UpdateView(AnalyticsSnapshot? snapshot)
        {
            if (snapshot == null) return;

            // 1. Update KPI cards with dual-metric intelligence
            kpiDealsClosed.SetValue(snapshot.TotalDealsClosed);
            kpiDealsClosed.SetSubtitle(snapshot.TotalSalesVolume > 0 ? $"Vol: {BiDisplayConstants.FormatCompactCurrency(snapshot.TotalSalesVolume)}" : "Closed in period");

            kpiCommission.SetCurrencyValue(snapshot.TotalCommissionEarned);
            kpiCommission.SetSubtitle(snapshot.AverageDealSize > 0 ? $"Avg: {BiDisplayConstants.FormatCompactCurrency(snapshot.AverageDealSize)}" : "Net earned");

            kpiActiveLeads.SetValue(snapshot.ActiveLeads);
            kpiActiveLeads.SetSubtitle(snapshot.ActivePipelineValue > 0 ? $"Pipe: {BiDisplayConstants.FormatCompactCurrency(snapshot.ActivePipelineValue)}" : "In pipeline");

            kpiConversionRate.SetValue(BiDisplayConstants.FormatPercent(snapshot.LeadConversionRate));
            kpiConversionRate.SetSubtitle(snapshot.WinRate > 0 ? $"Win Rate: {BiDisplayConstants.FormatPercent(snapshot.WinRate)}" : "Leads to closed");

            kpiOpenTickets.SetValue(snapshot.OpenSupportTickets);
            kpiOpenTickets.SetSubtitle("Support queue");

            kpiAvgDays.SetValue($"{snapshot.AverageDaysToClose:F1}d");
            kpiAvgDays.SetSubtitle(snapshot.ActivePropertiesCount > 0 ? $"Active Listings: {snapshot.ActivePropertiesCount}" : "Contract lead time");

            // 2. Chart: Deals Closed Over Time
            RenderDealsOverTimeChart(snapshot.DealsOverTime);

            // 3. Chart: Lead Pipeline Funnel
            RenderLeadFunnelChart(snapshot.LeadFunnel);

            // 4. Chart: Deals Won vs. Lost
            RenderDealsWonVsLostChart(snapshot.DealsWonVsLost);

            // 5. Chart: Support Ticket Breakdown
            RenderTicketBreakdownChart(snapshot.TicketBreakdown);

            // 6. Chart: Top-Performing Agents (Manager/Admin only)
            if (chartAgents.Visible)
            {
                RenderTopAgentsChart(snapshot.TopAgents ?? new List<AgentPerformance>());
            }

            // 7. Chart: Lead Source Breakdown
            if (chartSources.Visible)
            {
                RenderLeadSourcesChart(snapshot.LeadSourceBreakdown ?? new List<SourceMetric>());
            }

            // 8. Recent Activity Feed
            RenderRecentActivity(snapshot.RecentActivity);
        }

        private void AutoLayoutCharts()
        {
            if (pnlScrollableContent.ClientSize.Width <= 0) return;
            LayoutHeaderControls();

            // Preserve scroll position during layout
            int scrollX = pnlScrollableContent.AutoScrollPosition.X;
            int scrollY = pnlScrollableContent.AutoScrollPosition.Y;
            pnlScrollableContent.AutoScrollPosition = Point.Empty;

            int containerWidth = Math.Max(1, pnlScrollableContent.ClientSize.Width - 40 - SystemInformation.VerticalScrollBarWidth);
            int padding = 16;

            // 1. Layout KPI cards at the top
            pnlKpi.Location = new Point(20, 12);
            ResponsiveLayout.KpiGrid(pnlKpi, containerWidth);

            int y = pnlKpi.Bottom + 16;
            int maxY = y;

            bool showActivity = pnlRecentActivity.Visible;
            bool showAgents = chartAgents.Visible;

            if (containerWidth >= 1200 && showActivity)
            {
                // 3-column layout: 2 chart columns + 1 activity feed column
                int activityWidth = 380;
                int chartWidth = (containerWidth - activityWidth - (padding * 2)) / 2;
                int chartHeight = 360;

                int x1 = 20;
                int x2 = x1 + chartWidth + padding;
                int x3 = x2 + chartWidth + padding;

                // Row 1
                PositionChart(chartDealsClosed, x1, y, chartWidth, chartHeight);
                PositionChart(chartPipeline, x2, y, chartWidth, chartHeight);

                pnlRecentActivity.Location = new Point(x3, y);
                pnlRecentActivity.Size = new Size(activityWidth, (chartHeight * 2) + padding);
                pnlActivityFeedList.Size = new Size(activityWidth - 32, pnlRecentActivity.Height - 72);

                // Row 2
                y += chartHeight + padding;
                PositionChart(chartWonVsLost, x1, y, chartWidth, chartHeight);
                PositionChart(chartTickets, x2, y, chartWidth, chartHeight);

                maxY = Math.Max(pnlRecentActivity.Bottom, y + chartHeight);

                // Row 3 (manager/admin only)
                if (showAgents)
                {
                    y += chartHeight + padding;
                    PositionChart(chartAgents, x1, y, chartWidth, chartHeight);
                    PositionChart(chartSources, x2, y, chartWidth, chartHeight);
                    maxY = Math.Max(maxY, y + chartHeight);
                }
            }
            else if (containerWidth >= 900)
            {
                // 2-column layout
                int chartWidth = (containerWidth - padding) / 2;
                int chartHeight = 360;

                int x1 = 20;
                int x2 = x1 + chartWidth + padding;

                PositionChart(chartDealsClosed, x1, y, chartWidth, chartHeight);
                PositionChart(chartPipeline, x2, y, chartWidth, chartHeight);

                y += chartHeight + padding;
                PositionChart(chartWonVsLost, x1, y, chartWidth, chartHeight);
                PositionChart(chartTickets, x2, y, chartWidth, chartHeight);

                if (showAgents)
                {
                    y += chartHeight + padding;
                    PositionChart(chartAgents, x1, y, chartWidth, chartHeight);
                    PositionChart(chartSources, x2, y, chartWidth, chartHeight);
                }

                maxY = y + chartHeight;

                if (showActivity)
                {
                    y += chartHeight + padding;
                    pnlRecentActivity.Location = new Point(x1, y);
                    pnlRecentActivity.Size = new Size(containerWidth, 360);
                    pnlActivityFeedList.Size = new Size(containerWidth - 32, pnlRecentActivity.Height - 72);
                    maxY = pnlRecentActivity.Bottom;
                }
            }
            else
            {
                // Single column layout
                int chartWidth = containerWidth;
                int chartHeight = 340;
                int x1 = 20;

                PositionChart(chartDealsClosed, x1, y, chartWidth, chartHeight);

                y += chartHeight + padding;
                PositionChart(chartPipeline, x1, y, chartWidth, chartHeight);

                y += chartHeight + padding;
                PositionChart(chartWonVsLost, x1, y, chartWidth, chartHeight);

                y += chartHeight + padding;
                PositionChart(chartTickets, x1, y, chartWidth, chartHeight);

                if (showAgents)
                {
                    y += chartHeight + padding;
                    PositionChart(chartAgents, x1, y, chartWidth, chartHeight);

                    y += chartHeight + padding;
                    PositionChart(chartSources, x1, y, chartWidth, chartHeight);
                }

                maxY = y + chartHeight;

                if (showActivity)
                {
                    y += chartHeight + padding;
                    pnlRecentActivity.Location = new Point(x1, y);
                    pnlRecentActivity.Size = new Size(chartWidth, 360);
                    pnlActivityFeedList.Size = new Size(chartWidth - 32, pnlRecentActivity.Height - 72);
                    maxY = pnlRecentActivity.Bottom;
                }
            }

            // Drill-down grid ledger placed below charts & activities
            int gridY = maxY + padding;
            pnlGridCard.Location = new Point(20, gridY);
            pnlGridCard.Size = new Size(containerWidth, 420);
            maxY = pnlGridCard.Bottom;

            pnlScrollableContent.AutoScrollMinSize = new Size(0, maxY + 24);
            pnlScrollableContent.AutoScrollPosition = new Point(-scrollX, -scrollY);
        }

        private void PositionChart(ChartWrapperControl chart, int x, int y, int w, int h)
        {
            chart.Location = new Point(x, y);
            chart.Size = new Size(w, h);
        }

        private void RenderDealsOverTimeChart(List<MonthlyMetric>? metrics)
        {
            chartDealsClosed.SetHeader("Deals Closed Over Time", "Click a period to filter records below");
            if (metrics == null || metrics.Count == 0)
            {
                chartDealsClosed.ClearSegments();
                BiDisplayConstants.ShowPlotEmpty(chartDealsClosed.PlotControl, "No closed deals recorded in range");
                return;
            }

            var items = metrics.Select(m => (label: m.Month, value: (double)m.Count, color: BiDisplayConstants.PrimaryAccent, key: (string?)m.Month)).ToList();
            chartDealsClosed.RenderBarPlot(items);
        }

        private void RenderLeadFunnelChart(LeadFunnelData? funnel)
        {
            chartPipeline.SetHeader("Lead Pipeline Funnel", "Click a stage to filter records below");
            if (funnel == null)
            {
                chartPipeline.ClearSegments();
                BiDisplayConstants.ShowPlotEmpty(chartPipeline.PlotControl, "No pipeline leads recorded");
                return;
            }

            var items = new List<(string label, double value, Color color, string? key)>
            {
                ("New", (double)funnel.New, BiDisplayConstants.StatusNeutral, "New"),
                ("Contacted", (double)funnel.Contacted, BiDisplayConstants.StatusPending, "Contacted"),
                ("Qualified", (double)funnel.Qualified, BiDisplayConstants.PrimaryAccent, "Qualified"),
                ("Converted", (double)funnel.Converted, BiDisplayConstants.StatusWon, "Converted"),
                ("Lost", (double)funnel.Lost, BiDisplayConstants.StatusLost, "Lost")
            };

            chartPipeline.RenderBarPlot(items);
        }

        private void RenderDealsWonVsLostChart(WonLostData? wonLost)
        {
            chartWonVsLost.SetHeader("Deals Won vs. Lost", "Click Won or Lost to filter records below");
            if (wonLost == null || (wonLost.Won == 0 && wonLost.Lost == 0))
            {
                chartWonVsLost.ClearSegments();
                BiDisplayConstants.ShowPlotEmpty(chartWonVsLost.PlotControl, "No closed/lost deals in range");
                return;
            }

            var slices = new List<(string label, double value, Color color, string? key)>
            {
                ("Won", (double)wonLost.Won, BiDisplayConstants.StatusWon, "Won"),
                ("Lost", (double)wonLost.Lost, BiDisplayConstants.StatusLost, "Lost")
            };

            chartWonVsLost.RenderDonutPlot(slices);
        }

        private void RenderTicketBreakdownChart(TicketBreakdownData? tickets)
        {
            chartTickets.SetHeader("Support Ticket Breakdown", "Click a status to filter records below");
            if (tickets == null)
            {
                chartTickets.ClearSegments();
                BiDisplayConstants.ShowPlotEmpty(chartTickets.PlotControl, "No support tickets recorded");
                return;
            }

            var items = new List<(string label, double value, Color color, string? key)>
            {
                ("Open", (double)tickets.Open, BiDisplayConstants.StatusPending, "Open"),
                ("In Progress", (double)tickets.InProgress, BiDisplayConstants.PrimaryAccent, "In Progress"),
                ("Resolved", (double)tickets.Resolved, BiDisplayConstants.StatusWon, "Resolved"),
                ("Overdue", (double)tickets.Overdue, BiDisplayConstants.StatusLost, "Overdue")
            };

            chartTickets.RenderDonutPlot(items);
        }

        private void RenderTopAgentsChart(List<AgentPerformance> topAgents)
        {
            chartAgents.SetHeader("Top-Performing Agents", "Click an agent to filter records below");
            if (topAgents == null || topAgents.Count == 0)
            {
                chartAgents.ClearSegments();
                BiDisplayConstants.ShowPlotEmpty(chartAgents.PlotControl, "No agent performance data in range");
                return;
            }

            var items = topAgents
                .Select(a => (label: a.AgentName, value: (double)(a.TotalValue / 1_000_000m), color: BiDisplayConstants.PrimaryAccent, key: (string?)a.AgentName))
                .ToList();

            chartAgents.RenderBarPlot(items, rotation: -30);
        }

        private void RenderLeadSourcesChart(List<SourceMetric> sources)
        {
            chartSources.SetHeader("Lead Source Breakdown", "Click a source to filter records below");
            if (sources == null || sources.Count == 0)
            {
                chartSources.ClearSegments();
                BiDisplayConstants.ShowPlotEmpty(chartSources.PlotControl, "No lead sources recorded in range");
                return;
            }

            Color[] sourcePalette = new[]
            {
                Color.FromArgb(37, 103, 156),  // Skyline Blue
                Color.FromArgb(14, 165, 233),  // Sky
                Color.FromArgb(99, 102, 241),  // Indigo
                Color.FromArgb(139, 92, 246),  // Violet
                Color.FromArgb(20, 184, 166),  // Teal
                Color.FromArgb(245, 158, 11),  // Amber
            };

            var items = sources
                .Select((s, idx) => (label: s.Source, value: (double)s.Count, color: sourcePalette[idx % sourcePalette.Length], key: (string?)s.Source))
                .ToList();

            chartSources.RenderDonutPlot(items);
        }

        private void RenderRecentActivity(List<ActivityFeedItem>? feed)
        {
            pnlActivityFeedList.Controls.Clear();
            if (feed == null || feed.Count == 0)
            {
                var lblEmpty = new Label
                {
                    Text = "No recent activity recorded.",
                    Font = new Font("Segoe UI", 9.5f),
                    ForeColor = Theme.TextSecondary,
                    AutoSize = true,
                    Padding = new Padding(12)
                };
                pnlActivityFeedList.Controls.Add(lblEmpty);
                return;
            }

            int itemWidth = Math.Max(260, pnlActivityFeedList.ClientSize.Width - 12);

            foreach (var item in feed)
            {
                KpiIconType iconType;
                Color badgeBg;
                Color badgeBorder;
                Color iconColor;
                string eventCategory;
                string eventDetail;

                string rawDesc = item.Description ?? "";
                if (item.Icon == "💼" || rawDesc.StartsWith("Deal", StringComparison.OrdinalIgnoreCase))
                {
                    iconType = KpiIconType.Briefcase;
                    badgeBg = Color.FromArgb(239, 246, 255);
                    badgeBorder = Color.FromArgb(191, 219, 254);
                    iconColor = Theme.Primary;
                    eventCategory = "Deal Closed";
                }
                else if (item.Icon == "🎟" || rawDesc.StartsWith("Ticket", StringComparison.OrdinalIgnoreCase))
                {
                    iconType = KpiIconType.Ticket;
                    badgeBg = Color.FromArgb(254, 243, 199);
                    badgeBorder = Color.FromArgb(253, 230, 138);
                    iconColor = Color.FromArgb(217, 119, 6);
                    eventCategory = "Ticket Resolved";
                }
                else if (item.Icon == "◎" || rawDesc.StartsWith("Lead", StringComparison.OrdinalIgnoreCase))
                {
                    iconType = KpiIconType.Target;
                    badgeBg = Color.FromArgb(236, 253, 245);
                    badgeBorder = Color.FromArgb(167, 243, 208);
                    iconColor = Color.FromArgb(5, 150, 105);
                    eventCategory = "Lead Converted";
                }
                else
                {
                    iconType = KpiIconType.Clock;
                    badgeBg = Color.FromArgb(241, 245, 249);
                    badgeBorder = Color.FromArgb(226, 232, 240);
                    iconColor = Color.FromArgb(71, 85, 105);
                    eventCategory = "Activity";
                }

                int colonIdx = rawDesc.IndexOf(':');
                if (colonIdx >= 0 && colonIdx < rawDesc.Length - 1)
                {
                    eventDetail = rawDesc.Substring(colonIdx + 1).Trim();
                }
                else
                {
                    eventDetail = rawDesc;
                }

                var rowPanel = new Panel
                {
                    Width = itemWidth,
                    Height = 54,
                    BackColor = Color.White,
                    Margin = new Padding(0, 0, 0, 4),
                    Cursor = Cursors.Default
                };

                rowPanel.Paint += (s, e) =>
                {
                    e.Graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;

                    using (var divPen = new Pen(Color.FromArgb(241, 245, 249), 1f))
                    {
                        e.Graphics.DrawLine(divPen, 50, rowPanel.Height - 1, rowPanel.Width - 12, rowPanel.Height - 1);
                    }

                    var badgeRect = new Rectangle(6, 9, 36, 36);
                    using (var bBrush = new SolidBrush(badgeBg))
                    {
                        e.Graphics.FillEllipse(bBrush, badgeRect);
                    }
                    using (var bPen = new Pen(badgeBorder, 1f))
                    {
                        e.Graphics.DrawEllipse(bPen, badgeRect);
                    }

                    var iconRect = new Rectangle(15, 18, 18, 18);
                    UiIconHelper.DrawIcon(e.Graphics, iconType, iconRect, iconColor);
                };

                rowPanel.MouseEnter += (_, _) => rowPanel.BackColor = Color.FromArgb(248, 250, 252);
                rowPanel.MouseLeave += (_, _) => rowPanel.BackColor = Color.White;

                var lblDesc = new Label
                {
                    Text = $"{eventCategory} • {eventDetail}",
                    Font = new Font("Segoe UI Semibold", 8.75f),
                    ForeColor = Color.FromArgb(15, 23, 42),
                    Location = new Point(48, 8),
                    Size = new Size(Math.Max(100, rowPanel.Width - 54), 18),
                    AutoEllipsis = true,
                    BackColor = Color.Transparent
                };
                lblDesc.MouseEnter += (_, _) => rowPanel.BackColor = Color.FromArgb(248, 250, 252);
                lblDesc.MouseLeave += (_, _) => rowPanel.BackColor = Color.White;

                var lblTime = new Label
                {
                    Text = BiDisplayConstants.FormatDateTime(item.Timestamp),
                    Font = new Font("Segoe UI", 7.75f),
                    ForeColor = Color.FromArgb(100, 116, 139),
                    Location = new Point(48, 28),
                    Size = new Size(Math.Max(100, rowPanel.Width - 54), 16),
                    BackColor = Color.Transparent
                };
                lblTime.MouseEnter += (_, _) => rowPanel.BackColor = Color.FromArgb(248, 250, 252);
                lblTime.MouseLeave += (_, _) => rowPanel.BackColor = Color.White;

                rowPanel.Controls.Add(lblDesc);
                rowPanel.Controls.Add(lblTime);

                pnlActivityFeedList.Controls.Add(rowPanel);
            }
        }

        private void ExportPdf()
        {
            if (_currentSnapshot == null)
            {
                MessageBox.Show("No analytics data to export.", "Export", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            using var sfd = new SaveFileDialog
            {
                Filter = "PDF Document (*.pdf)|*.pdf",
                FileName = $"Analytics_Summary_{DateTime.Now:yyyyMMdd_HHmm}.pdf"
            };

            if (sfd.ShowDialog() == DialogResult.OK)
            {
                try
                {
                    var kpis = new (string Label, string Value)[]
                    {
                        ("Total Deals Closed", _currentSnapshot.TotalDealsClosed.ToString("N0")),
                        ("Total Sales Volume", $"₱ {_currentSnapshot.TotalSalesVolume:N2}"),
                        ("Commission Earned", $"₱ {_currentSnapshot.TotalCommissionEarned:N2}"),
                        ("Active Pipeline Value", $"₱ {_currentSnapshot.ActivePipelineValue:N2}"),
                        ("Average Deal Size", $"₱ {_currentSnapshot.AverageDealSize:N2}"),
                        ("Active Leads", _currentSnapshot.ActiveLeads.ToString("N0")),
                        ("Lead Conversion Rate", $"{_currentSnapshot.LeadConversionRate:F1}%"),
                        ("Win Rate", $"{_currentSnapshot.WinRate:F1}%"),
                        ("Open Support Tickets", _currentSnapshot.OpenSupportTickets.ToString("N0")),
                        ("Avg Days to Close", $"{_currentSnapshot.AverageDaysToClose:F1} days"),
                        ("Active Properties", _currentSnapshot.ActivePropertiesCount.ToString("N0")),
                        ("Inventory Value", $"₱ {_currentSnapshot.ActiveInventoryValue:N2}")
                    };

                    var drillDowns = _allDrillDownRows.Select(r => (
                        r.RecordType,
                        r.Reference,
                        r.Title,
                        r.Status,
                        r.AssignedTo,
                        r.Value,
                        r.Date,
                        r.Details
                    )).ToList();

                    string dateRangeText = cboDateRange.SelectedItem?.ToString() ?? "All Time";

                    if (CRMS_Peguit.winforms.Services.PdfExportHelper.TryExportAnalytics(sfd.FileName, dateRangeText, kpis, drillDowns, out string? error))
                    {
                        MessageBox.Show("Analytics summary and transaction records exported to PDF successfully.", "Export Complete", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    }
                    else
                    {
                        MessageBox.Show(error ?? "Failed to export PDF.", "Export Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Failed to export: {ex.Message}", "Export Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }
    }
}
