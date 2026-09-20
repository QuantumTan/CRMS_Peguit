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
using Color = System.Drawing.Color;
using Label = System.Windows.Forms.Label;
using FontStyle = System.Drawing.FontStyle;

namespace CRMS_Peguit.winforms.Views.Analytics
{
    public partial class AnalyticsView : UserControl
    {
        private readonly AnalyticsController _controller;
        private AnalyticsSnapshot? _currentSnapshot;

        public event Action<string>? NavigationRequested;

        public AnalyticsView()
        {
            InitializeComponent();
            _controller = new AnalyticsController();

            ApplyStyling();
            BindEvents();
            ApplyRoleBasedRendering();

            // Default to "This Year" or "Past 12 Months" to immediately display populated charts
            cboDateRange.SelectedIndex = 2; // "This Year" triggers ReloadSnapshot
        }

        private void ApplyStyling()
        {
            this.BackColor = Theme.Background;

            UiRadiusHelper.StyleCard(pnlChartDealsClosed, 12);
            UiRadiusHelper.StyleCard(pnlChartPipeline, 12);
            UiRadiusHelper.StyleCard(pnlChartWonVsLost, 12);
            UiRadiusHelper.StyleCard(pnlChartTickets, 12);
            UiRadiusHelper.StyleCard(pnlChartAgents, 12);
            UiRadiusHelper.StyleCard(pnlChartSources, 12);
            UiRadiusHelper.StyleCard(pnlRecentActivity, 12);

            UiRadiusHelper.StyleButton(btnExport, 8);
            btnExport.BackColor = Color.White;
            btnExport.ForeColor = Theme.Primary;
            btnExport.FlatAppearance.BorderColor = Theme.BorderAccessible;
            btnExport.FlatAppearance.BorderSize = 1;

            UiRadiusHelper.StyleButton(btnGoToReports, 8);

            ConfigurePlot(plotDealsClosed);
            ConfigurePlot(plotPipeline);
            ConfigurePlot(plotWonVsLost);
            ConfigurePlot(plotTickets);
            ConfigurePlot(plotAgents);
            ConfigurePlot(plotSources);
        }

        private void ConfigurePlot(ScottPlot.WinForms.FormsPlot plot)
        {
            BiDisplayConstants.ConfigureStandardPlot(plot);
        }

        private void BindEvents()
        {
            cboDateRange.SelectedIndexChanged += (_, _) => ReloadSnapshot();
            btnExport.Click += (_, _) => ExportCsv();

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

            pnlScrollableContent.Resize += (_, _) => AutoLayoutCharts();
            this.Resize += (_, _) => AutoLayoutCharts();
            this.Load += (_, _) => AutoLayoutCharts();
        }

        private void ApplyRoleBasedRendering()
        {
            if (RbacService.IsAgent && !RbacService.HasFullOversight)
            {
                lblTitle.Text = "My Performance Snapshot";
                lblSubtitle.Text = "Personal metrics and pipeline status across your assigned records";
                pnlChartAgents.Visible = false;
                pnlChartSources.Visible = false;
                btnExport.Visible = false;
                btnGoToReports.Visible = false;
            }
            else
            {
                lblTitle.Text = "Team Analytics Performance";
                lblSubtitle.Text = "Live agency-wide business intelligence, deal velocity, and operations";
                pnlChartAgents.Visible = true;
                pnlChartSources.Visible = true;
                btnExport.Visible = RbacService.CanExportData;
                btnGoToReports.Visible = CurrentSession.CanAccess("Reports");
            }

            AutoLayoutCharts();
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

        private async void ReloadSnapshot()
        {
            try
            {
                lblLoading.Visible = true;
                lblSubtitle.Visible = false;

                var range = GetSelectedDateRange();
                AnalyticsSnapshot? snapshot = null;
                await System.Threading.Tasks.Task.Run(() =>
                {
                    snapshot = _controller.GetSnapshot(range);
                });

                if (IsDisposed) return;

                lblLoading.Visible = false;
                lblSubtitle.Visible = true;
                _currentSnapshot = snapshot;
                UpdateView(_currentSnapshot);
            }
            catch (Exception ex)
            {
                lblLoading.Visible = false;
                lblSubtitle.Visible = true;
                System.Diagnostics.Debug.WriteLine($"[AnalyticsView.ReloadSnapshot] Error: {ex.Message}");
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

            // 2. Chart: Deals Closed Over Time (Trend = Line chart)
            RenderDealsOverTimeChart(snapshot.DealsOverTime);

            // 3. Chart: Lead Pipeline Funnel
            RenderLeadFunnelChart(snapshot.LeadFunnel);

            // 4. Chart: Deals Won vs. Lost
            RenderDealsWonVsLostChart(snapshot.DealsWonVsLost);

            // 5. Chart: Support Ticket Breakdown
            RenderTicketBreakdownChart(snapshot.TicketBreakdown);

            // 6. Chart: Top-Performing Agents (Manager/Admin only)
            if (pnlChartAgents.Visible && snapshot.TopAgents != null)
            {
                RenderTopAgentsChart(snapshot.TopAgents);
            }

            // 7. Chart: Lead Source Breakdown (Manager/Admin only)
            if (pnlChartSources.Visible && snapshot.LeadSourceBreakdown != null)
            {
                RenderLeadSourcesChart(snapshot.LeadSourceBreakdown);
            }

            // 8. Recent Activity Feed
            RenderRecentActivity(snapshot.RecentActivity);
        }

        private void AutoLayoutCharts()
        {
            if (pnlScrollableContent.ClientSize.Width <= 0) return;

            // Preserve and temporarily reset scroll offset during calculation to prevent coordinates shifting
            int scrollX = pnlScrollableContent.AutoScrollPosition.X;
            int scrollY = pnlScrollableContent.AutoScrollPosition.Y;
            pnlScrollableContent.AutoScrollPosition = Point.Empty;

            int containerWidth = pnlScrollableContent.ClientSize.Width - 40; // padding
            int padding = 16;

            // 1. Layout KPI cards at the top of scrollable host
            pnlKpi.Location = new Point(20, 12);
            pnlKpi.Size = new Size(containerWidth, 114);

            int y = pnlKpi.Bottom + 16;
            int maxY = y;

            bool showActivity = pnlRecentActivity.Visible;
            bool showAgents = pnlChartAgents.Visible;

            if (containerWidth >= 1200 && showActivity)
            {
                // 3-column layout: 2 columns for charts + 1 column for activity feed
                int activityWidth = 360;
                int chartWidth = (containerWidth - activityWidth - (padding * 2)) / 2;
                int chartHeight = 310;

                int x1 = 20;
                int x2 = x1 + chartWidth + padding;
                int x3 = x2 + chartWidth + padding;

                // Row 1
                PositionChart(pnlChartDealsClosed, plotDealsClosed, x1, y, chartWidth, chartHeight);
                PositionChart(pnlChartPipeline, plotPipeline, x2, y, chartWidth, chartHeight);

                pnlRecentActivity.Location = new Point(x3, y);
                pnlRecentActivity.Size = new Size(activityWidth, (chartHeight * 2) + padding);
                pnlActivityFeedList.Size = new Size(activityWidth - 32, pnlRecentActivity.Height - 65);

                // Row 2
                y += chartHeight + padding;
                PositionChart(pnlChartWonVsLost, plotWonVsLost, x1, y, chartWidth, chartHeight);
                PositionChart(pnlChartTickets, plotTickets, x2, y, chartWidth, chartHeight);

                maxY = Math.Max(pnlRecentActivity.Bottom, y + chartHeight);

                // Row 3
                if (showAgents)
                {
                    y += chartHeight + padding;
                    PositionChart(pnlChartAgents, plotAgents, x1, y, chartWidth, chartHeight);
                    PositionChart(pnlChartSources, plotSources, x2, y, chartWidth, chartHeight);
                    maxY = Math.Max(maxY, y + chartHeight);
                }
            }
            else
            {
                // 2-column or 1-column layout
                int cols = containerWidth >= 750 ? 2 : 1;
                int chartWidth = cols == 2 ? (containerWidth - padding) / 2 : containerWidth;
                int chartHeight = 300;

                int x1 = 20;
                int x2 = cols == 2 ? x1 + chartWidth + padding : x1;

                PositionChart(pnlChartDealsClosed, plotDealsClosed, x1, y, chartWidth, chartHeight);
                if (cols == 2)
                {
                    PositionChart(pnlChartPipeline, plotPipeline, x2, y, chartWidth, chartHeight);
                }
                else
                {
                    y += chartHeight + padding;
                    PositionChart(pnlChartPipeline, plotPipeline, x1, y, chartWidth, chartHeight);
                }

                y += chartHeight + padding;
                PositionChart(pnlChartWonVsLost, plotWonVsLost, x1, y, chartWidth, chartHeight);
                if (cols == 2)
                {
                    PositionChart(pnlChartTickets, plotTickets, x2, y, chartWidth, chartHeight);
                }
                else
                {
                    y += chartHeight + padding;
                    PositionChart(pnlChartTickets, plotTickets, x1, y, chartWidth, chartHeight);
                }

                if (showAgents)
                {
                    y += chartHeight + padding;
                    PositionChart(pnlChartAgents, plotAgents, x1, y, chartWidth, chartHeight);
                    if (cols == 2)
                    {
                        PositionChart(pnlChartSources, plotSources, x2, y, chartWidth, chartHeight);
                    }
                    else
                    {
                        y += chartHeight + padding;
                        PositionChart(pnlChartSources, plotSources, x1, y, chartWidth, chartHeight);
                    }
                }

                maxY = y + chartHeight;

                if (showActivity)
                {
                    y += chartHeight + padding;
                    pnlRecentActivity.Location = new Point(x1, y);
                    pnlRecentActivity.Size = new Size(cols == 2 ? containerWidth : chartWidth, 380);
                    pnlActivityFeedList.Size = new Size(pnlRecentActivity.Width - 32, pnlRecentActivity.Height - 65);
                    maxY = pnlRecentActivity.Bottom;
                }
            }

            pnlScrollableContent.AutoScrollMinSize = new Size(0, maxY + 24);
            pnlScrollableContent.AutoScrollPosition = new Point(-scrollX, -scrollY);
        }

        private void PositionChart(Panel panel, ScottPlot.WinForms.FormsPlot plot, int x, int y, int w, int h)
        {
            panel.Location = new Point(x, y);
            panel.Size = new Size(w, h);
            plot.Location = new Point(14, 44);
            plot.Size = new Size(Math.Max(100, w - 28), Math.Max(100, h - 56));
        }

        private void RenderDealsOverTimeChart(List<MonthlyMetric>? metrics)
        {
            if (metrics == null || metrics.Count == 0)
            {
                BiDisplayConstants.ShowPlotEmpty(plotDealsClosed, "No closed deals recorded in range");
                return;
            }

            var trendData = metrics.Select(m => (m.Month, (double)m.Count)).ToList();
            BiDisplayConstants.RenderTrendLinePlot(plotDealsClosed, trendData, BiDisplayConstants.PrimaryAccent, BiDisplayConstants.SecondaryAccent);
        }

        private void RenderLeadFunnelChart(LeadFunnelData? funnel)
        {
            if (funnel == null)
            {
                BiDisplayConstants.ShowPlotEmpty(plotPipeline, "No pipeline leads recorded");
                return;
            }

            var items = new List<(string label, double value, Color color)>
            {
                ("New", funnel.New, BiDisplayConstants.StatusNeutral),
                ("Contacted", funnel.Contacted, BiDisplayConstants.StatusPending),
                ("Qualified", funnel.Qualified, BiDisplayConstants.PrimaryAccent),
                ("Converted", funnel.Converted, BiDisplayConstants.StatusWon),
                ("Lost", funnel.Lost, BiDisplayConstants.StatusLost)
            };

            BiDisplayConstants.RenderBarPlot(plotPipeline, items);
        }

        private void RenderDealsWonVsLostChart(WonLostData? wonLost)
        {
            if (wonLost == null || (wonLost.Won == 0 && wonLost.Lost == 0))
            {
                BiDisplayConstants.ShowPlotEmpty(plotWonVsLost, "No closed/lost deals in range");
                return;
            }

            var slices = new List<(string label, double value, Color color)>
            {
                ("Won", wonLost.Won, BiDisplayConstants.StatusWon),
                ("Lost", wonLost.Lost, BiDisplayConstants.StatusLost)
            };

            BiDisplayConstants.RenderDonutPlot(plotWonVsLost, slices);
        }

        private void RenderTicketBreakdownChart(TicketBreakdownData? tickets)
        {
            if (tickets == null)
            {
                BiDisplayConstants.ShowPlotEmpty(plotTickets, "No support tickets recorded");
                return;
            }

            var items = new List<(string label, double value, Color color)>
            {
                ("Open", tickets.Open, BiDisplayConstants.StatusPending),
                ("In Progress", tickets.InProgress, BiDisplayConstants.PrimaryAccent),
                ("Resolved", tickets.Resolved, BiDisplayConstants.StatusWon),
                ("Overdue", tickets.Overdue, BiDisplayConstants.StatusLost)
            };

            BiDisplayConstants.RenderBarPlot(plotTickets, items);
        }

        private void RenderTopAgentsChart(List<AgentPerformance> topAgents)
        {
            if (topAgents == null || topAgents.Count == 0)
            {
                BiDisplayConstants.ShowPlotEmpty(plotAgents, "No agent performance data in range");
                return;
            }

            lblChartAgentsTitle.Text = "Top Agents by Sales Volume (₱ Millions)";
            var items = topAgents
                .Select(a => (a.AgentName, (double)(a.TotalValue / 1_000_000m), BiDisplayConstants.PrimaryAccent))
                .ToList();

            BiDisplayConstants.RenderBarPlot(plotAgents, items, rotation: -30);
        }

        private void RenderLeadSourcesChart(List<SourceMetric> sources)
        {
            if (sources == null || sources.Count == 0)
            {
                BiDisplayConstants.ShowPlotEmpty(plotSources, "No lead sources recorded in range");
                return;
            }

            var items = sources
                .Select(s => (s.Source, (double)s.Count, BiDisplayConstants.SecondaryAccent))
                .ToList();

            BiDisplayConstants.RenderBarPlot(plotSources, items, rotation: -30);
        }

        private void ShowPlotEmpty(ScottPlot.WinForms.FormsPlot plot, string message)
        {
            BiDisplayConstants.ShowPlotEmpty(plot, message);
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
                    Padding = new Padding(10)
                };
                pnlActivityFeedList.Controls.Add(lblEmpty);
                return;
            }

            foreach (var item in feed)
            {
                var rowPanel = new Panel
                {
                    Width = Math.Max(280, pnlActivityFeedList.ClientSize.Width - 10),
                    Height = 48,
                    BackColor = Color.FromArgb(248, 250, 252),
                    Margin = new Padding(0, 0, 0, 6)
                };
                UiRadiusHelper.ApplyRoundedCorners(rowPanel, 8);

                var lblIcon = new Label
                {
                    Text = item.Icon,
                    Font = new Font("Segoe UI", 12f),
                    Size = new Size(32, 32),
                    Location = new Point(8, 8),
                    TextAlign = ContentAlignment.MiddleCenter
                };

                var lblDesc = new Label
                {
                    Text = item.Description,
                    Font = new Font("Segoe UI", 9f, FontStyle.Bold),
                    ForeColor = Theme.TextPrimary,
                    Location = new Point(44, 6),
                    Size = new Size(rowPanel.Width - 52, 18),
                    AutoEllipsis = true
                };

                var lblTime = new Label
                {
                    Text = BiDisplayConstants.FormatDateTime(item.Timestamp),
                    Font = new Font("Segoe UI", 7.5F),
                    ForeColor = Theme.TextSecondary,
                    Location = new Point(44, 26),
                    Size = new Size(rowPanel.Width - 52, 16)
                };

                rowPanel.Controls.Add(lblIcon);
                rowPanel.Controls.Add(lblDesc);
                rowPanel.Controls.Add(lblTime);

                pnlActivityFeedList.Controls.Add(rowPanel);
            }
        }

        private void ExportCsv()
        {
            if (_currentSnapshot == null)
            {
                MessageBox.Show("No analytics data to export.", "Export", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            using var sfd = new SaveFileDialog
            {
                Filter = "CSV File (*.csv)|*.csv",
                FileName = $"Analytics_Summary_{DateTime.Now:yyyyMMdd_HHmm}.csv"
            };

            if (sfd.ShowDialog() == DialogResult.OK)
            {
                try
                {
                    var sb = new StringBuilder();
                    sb.AppendLine($"# NEXA CRM Analytics Summary");
                    sb.AppendLine($"# Date Range: {cboDateRange.SelectedItem}");
                    sb.AppendLine($"# Generated By: {CurrentSession.CurrentUser?.FullName ?? "System"}");
                    sb.AppendLine($"# Generated At: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
                    sb.AppendLine();
                    sb.AppendLine("Metric,Value");
                    sb.AppendLine($"Total Deals Closed,{_currentSnapshot.TotalDealsClosed}");
                    sb.AppendLine($"Total Sales Volume (₱),{_currentSnapshot.TotalSalesVolume:F2}");
                    sb.AppendLine($"Total Commission Earned (₱),{_currentSnapshot.TotalCommissionEarned:F2}");
                    sb.AppendLine($"Active Pipeline Value (₱),{_currentSnapshot.ActivePipelineValue:F2}");
                    sb.AppendLine($"Average Deal Size (₱),{_currentSnapshot.AverageDealSize:F2}");
                    sb.AppendLine($"Active Leads,{_currentSnapshot.ActiveLeads}");
                    sb.AppendLine($"Lead Conversion Rate (%),{_currentSnapshot.LeadConversionRate:F2}");
                    sb.AppendLine($"Win Rate (%),{_currentSnapshot.WinRate:F2}");
                    sb.AppendLine($"Open Support Tickets,{_currentSnapshot.OpenSupportTickets}");
                    sb.AppendLine($"Average Days to Close,{_currentSnapshot.AverageDaysToClose:F2}");
                    sb.AppendLine($"Active Properties Count,{_currentSnapshot.ActivePropertiesCount}");
                    sb.AppendLine($"Active Inventory Value (₱),{_currentSnapshot.ActiveInventoryValue:F2}");
                    sb.AppendLine();

                    if (_currentSnapshot.DealsOverTime.Count > 0)
                    {
                        sb.AppendLine("Period,Closed Deals,Total Value (₱)");
                        foreach (var m in _currentSnapshot.DealsOverTime)
                        {
                            sb.AppendLine($"\"{m.Month}\",{m.Count},{m.Value:F2}");
                        }
                    }

                    File.WriteAllText(sfd.FileName, sb.ToString());
                    MessageBox.Show("Analytics summary exported successfully.", "Export Complete", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Failed to export: {ex.Message}", "Export Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }
    }
}
