using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using CRMS_Peguit.winforms.Auth;
using CRMS_Peguit.winforms.Controllers;
using CRMS_Peguit.winforms.Models.Analytics;
using CRMS_Peguit.winforms.Models.Services;
using CRMS_Peguit.winforms.Services;
using Color = System.Drawing.Color;

namespace CRMS_Peguit.winforms.Views.Reports
{
    public partial class ReportsView : UserControl
    {
        private enum ViewDisplayMode { Both, ChartsOnly, TableOnly }

        private readonly ReportsController _controller;
        private object? _currentData;
        private object? _unfilteredData;
        private ReportHeader? _currentHeader;
        private ViewDisplayMode _viewMode = ViewDisplayMode.Both;
        private int _selectedKpiIndex = 0;

        public event Action<string>? NavigationRequested;

        public ReportsView()
        {
            InitializeComponent();
            _controller = new ReportsController();

            if (!RbacService.HasFullOversight)
            {
                pnlTop.Visible = false;
                pnlFilters.Visible = false;
                pnlCharts.Visible = false;
                pnlGrid.Visible = false;
                lblAccessDenied.Visible = true;
                lblAccessDenied.BringToFront();
                return;
            }

            SetupUI();
            LoadDropdowns();
            WireEvents();

            // Run initial report on load
            this.Load += (_, _) =>
            {
                LayoutReportControls();
                BtnRunReport_Click(this, EventArgs.Empty);
            };
        }

        private void SetupUI()
        {
            this.BackColor = Theme.Background;
            UiGridHelper.ApplyModernGridStyle(gridData, 48);
            gridData.CellPainting += GridData_CellPainting;

            UiRadiusHelper.StyleCard(pnlChartCard1, 10);
            UiRadiusHelper.StyleCard(pnlChartCard2, 10);

            btnRunReport.BackColor = BiDisplayConstants.PrimaryAccent;
            btnRunReport.ForeColor = Theme.Surface;
            btnExportCsv.BackColor = Theme.Surface;
            btnExportCsv.ForeColor = Theme.TextPrimary;
            btnExportPdf.BackColor = Theme.Surface;
            btnExportPdf.ForeColor = Theme.TextPrimary;

            btnExportCsv.Enabled = false;
            btnExportPdf.Enabled = false;

            // Remove docking and anchor constraints so absolute sizing in LayoutReportControls works unhindered
            pnlCharts.Dock = DockStyle.None;
            pnlGrid.Dock = DockStyle.None;
            plotReport1.Anchor = AnchorStyles.None;
            plotReport2.Anchor = AnchorStyles.None;

            ConfigurePlot(plotReport1);
            ConfigurePlot(plotReport2);

            UpdateViewModeButtons();
        }

        private void ConfigurePlot(ScottPlot.WinForms.FormsPlot plot)
        {
            BiDisplayConstants.ConfigureStandardPlot(plot);
        }

        private void LoadDropdowns()
        {
            cboReportType.Items.AddRange(new object[]
            {
                "Sales & Revenue Report",
                "Commission & Payout Settlement Report",
                "Property Inventory & Absorption Report",
                "Lead Pipeline & Conversion Report",
                "Support Ticket Resolution & SLA Report",
                "Agent Productivity & Performance Report"
            });
            cboReportType.SelectedIndex = 0;

            cboDateRange.Items.AddRange(new object[]
            {
                "This Month",
                "This Quarter",
                "This Year",
                "Past 12 Months",
                "All Time",
                "Custom"
            });
            cboDateRange.SelectedIndex = 2; // Default to "This Year" for rich graphical view

            var agents = _controller.GetAgentList();
            cboAgentFilter.Items.Add(new AgentPickerItem(0, "All Agents", ""));
            foreach (var a in agents)
                cboAgentFilter.Items.Add(a);
            cboAgentFilter.SelectedIndex = 0;

            UpdateSecondaryFilter();
        }

        public void SelectReport(string reportNameOrKeyword, string? dateRange = null)
        {
            if (string.IsNullOrWhiteSpace(reportNameOrKeyword)) return;

            for (int i = 0; i < cboReportType.Items.Count; i++)
            {
                var itemText = cboReportType.Items[i]?.ToString() ?? "";
                if (itemText.Contains(reportNameOrKeyword, StringComparison.OrdinalIgnoreCase))
                {
                    cboReportType.SelectedIndex = i;
                    break;
                }
            }

            if (!string.IsNullOrWhiteSpace(dateRange))
            {
                for (int i = 0; i < cboDateRange.Items.Count; i++)
                {
                    var dtText = cboDateRange.Items[i]?.ToString() ?? "";
                    if (dtText.Contains(dateRange, StringComparison.OrdinalIgnoreCase))
                    {
                        cboDateRange.SelectedIndex = i;
                        break;
                    }
                }
            }

            UpdateSecondaryFilter();
            BtnRunReport_Click(this, EventArgs.Empty);
        }

        private void WireEvents()
        {
            cboReportType.SelectedIndexChanged += (s, e) =>
            {
                UpdateSecondaryFilter();
                btnExportCsv.Text = "Export CSV";
                btnExportPdf.Text = "Export PDF";
            };
            cboDateRange.SelectedIndexChanged += (s, e) =>
            {
                bool isCustom = cboDateRange.SelectedItem?.ToString() == "Custom";
                dtpStart.Visible = isCustom;
                lblTo.Visible = isCustom;
                dtpEnd.Visible = isCustom;
            };

            btnRunReport.Click += BtnRunReport_Click;
            btnExportCsv.Click += BtnExportCsv_Click;
            btnExportPdf.Click += BtnExportPdf_Click;

            btnViewBoth.Click += (_, _) => SetViewMode(ViewDisplayMode.Both);
            btnViewCharts.Click += (_, _) => SetViewMode(ViewDisplayMode.ChartsOnly);
            btnViewTable.Click += (_, _) => SetViewMode(ViewDisplayMode.TableOnly);

            kpi1.Click += (_, _) => ToggleKpiFilter(1);
            kpi2.Click += (_, _) => ToggleKpiFilter(2);
            kpi3.Click += (_, _) => ToggleKpiFilter(3);
            kpi4.Click += (_, _) => ToggleKpiFilter(4);

            btnGoToAnalytics.Click += (_, _) =>
            {
                if (ParentForm is MainForm main)
                {
                    main.NavigateTo("Analytics");
                }
                else
                {
                    NavigationRequested?.Invoke("Analytics");
                }
            };

            this.Resize += (_, _) => LayoutReportControls();
            pnlScrollableContent.Resize += (_, _) => LayoutReportControls();
        }

        private void SetViewMode(ViewDisplayMode mode)
        {
            _viewMode = mode;
            UpdateViewModeButtons();
            LayoutReportControls();
        }

        private void UpdateViewModeButtons()
        {
            btnViewBoth.BackColor = _viewMode == ViewDisplayMode.Both ? Theme.PrimaryLight : Color.White;
            btnViewBoth.ForeColor = _viewMode == ViewDisplayMode.Both ? Theme.Primary : Theme.TextSecondary;

            btnViewCharts.BackColor = _viewMode == ViewDisplayMode.ChartsOnly ? Theme.PrimaryLight : Color.White;
            btnViewCharts.ForeColor = _viewMode == ViewDisplayMode.ChartsOnly ? Theme.Primary : Theme.TextSecondary;

            btnViewTable.BackColor = _viewMode == ViewDisplayMode.TableOnly ? Theme.PrimaryLight : Color.White;
            btnViewTable.ForeColor = _viewMode == ViewDisplayMode.TableOnly ? Theme.Primary : Theme.TextSecondary;
        }

        private void LayoutReportControls()
        {
            if (pnlScrollableContent.ClientSize.Width <= 0) return;

            int scrollX = pnlScrollableContent.AutoScrollPosition.X;
            int scrollY = pnlScrollableContent.AutoScrollPosition.Y;
            pnlScrollableContent.AutoScrollPosition = Point.Empty;

            int containerWidth = pnlScrollableContent.ClientSize.Width;
            int containerHeight = pnlScrollableContent.ClientSize.Height;

            int totalContentHeight = 0;

            switch (_viewMode)
            {
                case ViewDisplayMode.Both:
                {
                    pnlCharts.Visible = true;
                    pnlGrid.Visible = true;

                    int chartHeight = 420;
                    pnlCharts.Location = new Point(0, 0);
                    pnlCharts.Size = new Size(containerWidth, chartHeight);

                    int gridHeight = Math.Max(380, containerHeight - chartHeight);
                    pnlGrid.Location = new Point(0, pnlCharts.Bottom);
                    pnlGrid.Size = new Size(containerWidth, gridHeight);

                    totalContentHeight = chartHeight + gridHeight;
                    break;
                }
                case ViewDisplayMode.ChartsOnly:
                {
                    pnlCharts.Visible = true;
                    pnlGrid.Visible = false;

                    int chartHeight = Math.Max(520, containerHeight - 20);
                    pnlCharts.Location = new Point(0, 0);
                    pnlCharts.Size = new Size(containerWidth, chartHeight);

                    totalContentHeight = chartHeight;
                    break;
                }
                case ViewDisplayMode.TableOnly:
                {
                    pnlCharts.Visible = false;
                    pnlGrid.Visible = true;

                    int gridHeight = Math.Max(400, containerHeight);
                    pnlGrid.Location = new Point(0, 0);
                    pnlGrid.Size = new Size(containerWidth, gridHeight);

                    totalContentHeight = gridHeight;
                    break;
                }
            }

            // Layout child chart cards inside pnlCharts if visible
            if (pnlCharts.Visible && containerWidth > 0)
            {
                int pad = 20;
                int totalChartWidth = containerWidth - (pad * 2);
                int cardWidth = Math.Max(300, (totalChartWidth - 16) / 2);
                int cardHeight = Math.Max(260, pnlCharts.Height - 20);

                pnlChartCard1.Location = new Point(pad, 10);
                pnlChartCard1.Size = new Size(cardWidth, cardHeight);

                pnlChartCard2.Location = new Point(pnlChartCard1.Right + 16, 10);
                pnlChartCard2.Size = new Size(cardWidth, cardHeight);

                // Explicitly size and position FormsPlots to generously fill the cards below title
                int plotWidth = Math.Max(100, cardWidth - 24);
                int plotHeight = Math.Max(100, cardHeight - 48);
                plotReport1.Location = new Point(12, 36);
                plotReport1.Size = new Size(plotWidth, plotHeight);
                plotReport2.Location = new Point(12, 36);
                plotReport2.Size = new Size(plotWidth, plotHeight);

                try
                {
                    plotReport1.Refresh();
                    plotReport2.Refresh();
                }
                catch { }
            }

            pnlScrollableContent.AutoScrollMinSize = new Size(0, totalContentHeight + 10);
            pnlScrollableContent.AutoScrollPosition = new Point(-scrollX, -scrollY);
        }

        private void UpdateSecondaryFilter()
        {
            var rpt = cboReportType.SelectedItem?.ToString() ?? "";
            cboSecondaryFilter.Items.Clear();

            if (rpt.Contains("Sales") || rpt.Contains("Property"))
            {
                lblSecondaryFilter.Visible = true;
                cboSecondaryFilter.Visible = true;
                lblSecondaryFilter.Text = "Property Type";
                cboSecondaryFilter.Items.AddRange(new object[] { "All Types", "House", "Condo", "Townhouse", "Commercial", "Lot" });
                cboSecondaryFilter.SelectedIndex = 0;
            }
            else if (rpt.Contains("Lead"))
            {
                lblSecondaryFilter.Visible = true;
                cboSecondaryFilter.Visible = true;
                lblSecondaryFilter.Text = "Source";
                cboSecondaryFilter.Items.AddRange(new object[] { "All Sources", "Website", "Referral", "Walk-in", "Social Media", "Cold Call" });
                cboSecondaryFilter.SelectedIndex = 0;
            }
            else if (rpt.Contains("Ticket"))
            {
                lblSecondaryFilter.Visible = true;
                cboSecondaryFilter.Visible = true;
                lblSecondaryFilter.Text = "Priority";
                cboSecondaryFilter.Items.AddRange(new object[] { "All Priorities", "Low", "Medium", "High", "Critical" });
                cboSecondaryFilter.SelectedIndex = 0;
            }
            else
            {
                lblSecondaryFilter.Visible = false;
                cboSecondaryFilter.Visible = false;
            }
        }

        private async void BtnRunReport_Click(object? sender, EventArgs e)
        {
            lblLoading.Visible = true;
            btnRunReport.Enabled = false;
            btnExportCsv.Enabled = false;
            btnExportPdf.Enabled = false;
            lblReportHeader.Text = "Generating report and analytical charts...";

            try
            {
                var rpt = cboReportType.SelectedItem?.ToString() ?? "Sales & Revenue Report";
                var dr = GetDateRange();
                int? agentId = (cboAgentFilter.SelectedItem as AgentPickerItem)?.UserId;
                if (agentId == 0) agentId = null;

                string? secFilter = cboSecondaryFilter.SelectedItem?.ToString();
                if (secFilter != null && secFilter.StartsWith("All ")) secFilter = null;

                _currentHeader = new ReportHeader
                {
                    ReportName = rpt,
                    DateRange = dr.Label == "Custom" ? $"{dr.Start:MMM dd, yyyy} - {dr.End:MMM dd, yyyy}" : dr.Label,
                    GeneratedBy = CurrentSession.CurrentUser?.FullName ?? "Admin",
                    GeneratedAt = DateTime.Now
                };

                object? data = null;

                await Task.Run(() =>
                {
                    if (rpt.Contains("Sales"))
                        data = _controller.GetSalesReport(dr, agentId, secFilter);
                    else if (rpt.Contains("Commission"))
                        data = _controller.GetCommissionReport(dr, agentId);
                    else if (rpt.Contains("Property"))
                        data = _controller.GetPropertyInventoryReport(dr, secFilter, null);
                    else if (rpt.Contains("Lead"))
                        data = _controller.GetLeadProgressReport(dr, agentId, secFilter);
                    else if (rpt.Contains("Ticket"))
                        data = _controller.GetTicketResolutionReport(dr, secFilter, null);
                    else if (rpt.Contains("Agent"))
                        data = _controller.GetAgentActivityReport(dr, agentId);
                });

                _unfilteredData = data;
                _selectedKpiIndex = 0;
                UpdateKpiSelectionStates();
                ApplyKpiFilter();

                bool isFinancialReport = rpt.Contains("Commission");
                if (isFinancialReport && !RbacService.CanExportFinancialSettlements)
                {
                    btnExportCsv.Enabled = false;
                    btnExportPdf.Enabled = false;
                    btnExportCsv.Text = "Export (Admin Only)";
                    btnExportPdf.Text = "Export (Admin Only)";
                }
                else
                {
                    btnExportCsv.Enabled = _currentData != null;
                    btnExportPdf.Enabled = _currentData != null;
                    btnExportCsv.Text = "Export CSV";
                    btnExportPdf.Text = "Export PDF";
                }

                // Update dynamic KPI summary cards
                UpdateReportKpis(rpt, _unfilteredData);

                // Render accompanying charts
                RenderReportCharts(rpt, _unfilteredData);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error running report: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                lblLoading.Visible = false;
                btnRunReport.Enabled = true;
            }
        }

        private void UpdateReportKpis(string rptType, object? data)
        {
            if (data == null)
            {
                kpi1.SetValue(0);
                kpi2.SetValue("₱0.00");
                kpi3.SetValue("₱0.00");
                kpi4.SetValue("0");
                return;
            }

            if (rptType.Contains("Sales") && data is List<SalesReportRow> sales)
            {
                int count = sales.Count;
                decimal totalVolume = sales.Sum(s => s.DealValue);
                decimal totalComm = sales.Sum(s => s.Commission);
                decimal avgSize = count == 0 ? 0 : totalVolume / count;

                kpi1.SetValue(count);
                kpi1.SetSubtitle("Closed & active deals");
                kpi1.SetIcon(CRMS_Peguit.winforms.Models.Services.KpiIconType.Briefcase, BiDisplayConstants.PrimaryAccent);

                kpi2.SetValue(BiDisplayConstants.FormatCompactCurrency(totalVolume));
                kpi2.SetSubtitle($"Gross: {BiDisplayConstants.FormatCurrency(totalVolume)}");
                kpi2.SetIcon(CRMS_Peguit.winforms.Models.Services.KpiIconType.Currency, BiDisplayConstants.StatusWon);

                kpi3.SetValue(BiDisplayConstants.FormatCompactCurrency(totalComm));
                kpi3.SetSubtitle($"Net: {BiDisplayConstants.FormatCurrency(totalComm)}");
                kpi3.SetIcon(CRMS_Peguit.winforms.Models.Services.KpiIconType.Currency, BiDisplayConstants.StatusWon);

                kpi4.SetValue(BiDisplayConstants.FormatCompactCurrency(avgSize));
                kpi4.SetSubtitle("Per deal average");
                kpi4.SetIcon(CRMS_Peguit.winforms.Models.Services.KpiIconType.Target, BiDisplayConstants.HighlightAccent);
            }
            else if (rptType.Contains("Commission") && data is List<CommissionReportRow> comms)
            {
                int count = comms.Count;
                decimal grossComm = comms.Sum(c => c.GrossCommission);
                decimal agentPayouts = comms.Sum(c => c.AgentPayoutAmount);
                decimal brokerageNet = comms.Sum(c => c.BrokerageRetainedAmount);

                kpi1.SetValue(count);
                kpi1.SetSubtitle("Commission records");
                kpi1.SetIcon(CRMS_Peguit.winforms.Models.Services.KpiIconType.Briefcase, BiDisplayConstants.PrimaryAccent);

                kpi2.SetValue(BiDisplayConstants.FormatCompactCurrency(grossComm));
                kpi2.SetSubtitle($"Total: {BiDisplayConstants.FormatCurrency(grossComm)}");
                kpi2.SetIcon(CRMS_Peguit.winforms.Models.Services.KpiIconType.Currency, BiDisplayConstants.StatusWon);

                kpi3.SetValue(BiDisplayConstants.FormatCompactCurrency(agentPayouts));
                kpi3.SetSubtitle($"Disbursed: {BiDisplayConstants.FormatCurrency(agentPayouts)}");
                kpi3.SetIcon(CRMS_Peguit.winforms.Models.Services.KpiIconType.Currency, BiDisplayConstants.StatusPending);

                if (RbacService.CanViewBrokerageMargins)
                {
                    kpi4.SetValue(BiDisplayConstants.FormatCompactCurrency(brokerageNet));
                    kpi4.SetSubtitle($"Retained: {BiDisplayConstants.FormatCurrency(brokerageNet)}");
                }
                else
                {
                    kpi4.SetValue("Restricted");
                    kpi4.SetSubtitle("Admin oversight only");
                }
                kpi4.SetIcon(CRMS_Peguit.winforms.Models.Services.KpiIconType.Building, BiDisplayConstants.SkyAccent);
            }
            else if (rptType.Contains("Property") && data is List<PropertyInventoryReportRow> props)
            {
                int count = props.Count;
                decimal totalValue = props.Sum(p => p.ListingPrice);
                double avgDom = count == 0 ? 0 : props.Average(p => p.DaysOnMarket);
                int activeDeals = props.Sum(p => p.AssociatedDeals);

                kpi1.SetValue(count);
                kpi1.SetSubtitle("Tracked properties");
                kpi1.SetIcon(CRMS_Peguit.winforms.Models.Services.KpiIconType.Building, BiDisplayConstants.PrimaryAccent);

                kpi2.SetValue(BiDisplayConstants.FormatCompactCurrency(totalValue));
                kpi2.SetSubtitle($"Total: {BiDisplayConstants.FormatCurrency(totalValue)}");
                kpi2.SetIcon(CRMS_Peguit.winforms.Models.Services.KpiIconType.Currency, BiDisplayConstants.StatusWon);

                kpi3.SetValue($"{avgDom:F1}d");
                kpi3.SetSubtitle("Listing absorption speed");
                kpi3.SetIcon(CRMS_Peguit.winforms.Models.Services.KpiIconType.Clock, BiDisplayConstants.StatusPending);

                kpi4.SetValue(activeDeals);
                kpi4.SetSubtitle("Linked under negotiation");
                kpi4.SetIcon(CRMS_Peguit.winforms.Models.Services.KpiIconType.Target, BiDisplayConstants.HighlightAccent);
            }
            else if (rptType.Contains("Lead") && data is List<LeadProgressRow> leads)
            {
                int count = leads.Count;
                int converted = leads.Count(l => l.ConvertedToCustomer == "Yes");
                double convRate = count == 0 ? 0 : ((double)converted / count) * 100;
                decimal estBudget = leads.Sum(l => l.EstimatedBudget);

                kpi1.SetValue(count);
                kpi1.SetSubtitle("Acquired in period");
                kpi1.SetIcon(CRMS_Peguit.winforms.Models.Services.KpiIconType.Target, BiDisplayConstants.PrimaryAccent);

                kpi2.SetValue(converted);
                kpi2.SetSubtitle("Won to customers");
                kpi2.SetIcon(CRMS_Peguit.winforms.Models.Services.KpiIconType.Users, BiDisplayConstants.StatusWon);

                kpi3.SetValue(BiDisplayConstants.FormatPercent(convRate));
                kpi3.SetSubtitle("Pipeline efficiency");
                kpi3.SetIcon(CRMS_Peguit.winforms.Models.Services.KpiIconType.Refresh, BiDisplayConstants.HighlightAccent);

                kpi4.SetValue(BiDisplayConstants.FormatCompactCurrency(estBudget));
                kpi4.SetSubtitle($"Est: {BiDisplayConstants.FormatCurrency(estBudget)}");
                kpi4.SetIcon(CRMS_Peguit.winforms.Models.Services.KpiIconType.Currency, BiDisplayConstants.SkyAccent);
            }
            else if (rptType.Contains("Ticket") && data is List<TicketResolutionRow> tickets)
            {
                int count = tickets.Count;
                int resolved = tickets.Count(t => string.Equals(t.Status, "Resolved", StringComparison.OrdinalIgnoreCase) || string.Equals(t.Status, "Closed", StringComparison.OrdinalIgnoreCase));
                int met = tickets.Count(t => t.SlaMet == "Yes");
                int missed = tickets.Count(t => t.SlaMet == "No");
                double slaRate = count == 0 ? 0 : ((double)met / count) * 100;

                kpi1.SetValue(count);
                kpi1.SetSubtitle("Logged support requests");
                kpi1.SetIcon(CRMS_Peguit.winforms.Models.Services.KpiIconType.Ticket, BiDisplayConstants.PrimaryAccent);

                kpi2.SetValue(resolved);
                kpi2.SetSubtitle("Closed & satisfied");
                kpi2.SetIcon(CRMS_Peguit.winforms.Models.Services.KpiIconType.Target, BiDisplayConstants.StatusWon);

                kpi3.SetValue(BiDisplayConstants.FormatPercent(slaRate));
                kpi3.SetSubtitle($"{met} met SLA target");
                kpi3.SetIcon(CRMS_Peguit.winforms.Models.Services.KpiIconType.Clock, slaRate >= 80 ? BiDisplayConstants.StatusWon : BiDisplayConstants.StatusLost);

                kpi4.SetValue(missed);
                kpi4.SetSubtitle("Missed resolution SLA");
                kpi4.SetIcon(CRMS_Peguit.winforms.Models.Services.KpiIconType.AlertTriangle, BiDisplayConstants.StatusLost);
            }
            else if (rptType.Contains("Agent") && data is List<AgentActivityRow> acts)
            {
                int count = acts.Count;
                int totalDeals = acts.Sum(a => a.DealsClosed);
                decimal totalVolume = acts.Sum(a => a.TotalSalesVolume);
                int totalTickets = acts.Sum(a => a.TicketsResolved);

                kpi1.SetValue(count);
                kpi1.SetSubtitle("Frontline personnel");
                kpi1.SetIcon(CRMS_Peguit.winforms.Models.Services.KpiIconType.Users, BiDisplayConstants.PrimaryAccent);

                kpi2.SetValue(totalDeals);
                kpi2.SetSubtitle("Team closed transactions");
                kpi2.SetIcon(CRMS_Peguit.winforms.Models.Services.KpiIconType.Briefcase, BiDisplayConstants.StatusWon);

                kpi3.SetValue(BiDisplayConstants.FormatCompactCurrency(totalVolume));
                kpi3.SetSubtitle($"Gross: {BiDisplayConstants.FormatCurrency(totalVolume)}");
                kpi3.SetIcon(CRMS_Peguit.winforms.Models.Services.KpiIconType.Currency, BiDisplayConstants.StatusWon);

                kpi4.SetValue(totalTickets);
                kpi4.SetSubtitle("Client issues resolved");
                kpi4.SetIcon(CRMS_Peguit.winforms.Models.Services.KpiIconType.Ticket, BiDisplayConstants.SkyAccent);
            }
        }

        private void RenderReportCharts(string rptType, object? data)
        {
            plotReport1.Plot.Clear();
            plotReport2.Plot.Clear();

            BiDisplayConstants.ConfigureStandardPlot(plotReport1);
            BiDisplayConstants.ConfigureStandardPlot(plotReport2);

            if (data == null)
            {
                BiDisplayConstants.ShowPlotEmpty(plotReport1, "No data available");
                BiDisplayConstants.ShowPlotEmpty(plotReport2, "No data available");
                return;
            }

            if (rptType.Contains("Sales") && data is List<SalesReportRow> sales)
            {
                lblChart1Title.Text = "Sales Volume by Agent (₱ Millions)";
                lblChart2Title.Text = "Transactions by Pipeline Stage";

                if (sales.Count == 0)
                {
                    BiDisplayConstants.ShowPlotEmpty(plotReport1, "No sales in selected period");
                    BiDisplayConstants.ShowPlotEmpty(plotReport2, "No transactions in selected period");
                    return;
                }

                // Chart 1: Sales volume by agent
                var agentSales = sales
                    .GroupBy(r => string.IsNullOrWhiteSpace(r.AgentName) ? "Unassigned" : r.AgentName)
                    .Select(g => new { Name = g.Key, TotalM = (double)(g.Sum(r => r.DealValue) / 1_000_000m) })
                    .OrderByDescending(x => x.TotalM)
                    .Take(7)
                    .ToList();

                BiDisplayConstants.RenderBarPlot(plotReport1,
                    agentSales.Select(x => x.Name).ToArray(),
                    agentSales.Select(x => x.TotalM).ToArray(),
                    agentSales.Select(_ => BiDisplayConstants.PrimaryAccent).ToArray());

                // Chart 2: Transactions by stage
                var stageGroups = sales
                    .GroupBy(r => string.IsNullOrWhiteSpace(r.Stage) ? "Unknown" : r.Stage)
                    .Select(g => new { Stage = g.Key, Count = (double)g.Count() })
                    .OrderByDescending(x => x.Count)
                    .ToList();

                var stageColors = new[] { BiDisplayConstants.StatusWon, BiDisplayConstants.PrimaryAccent, BiDisplayConstants.StatusPending, BiDisplayConstants.SkyAccent, BiDisplayConstants.StatusLost, BiDisplayConstants.StatusNeutral };
                BiDisplayConstants.RenderBarPlot(plotReport2,
                    stageGroups.Select(x => x.Stage).ToArray(),
                    stageGroups.Select(x => x.Count).ToArray(),
                    stageGroups.Select((_, idx) => stageColors[idx % stageColors.Length]).ToArray());
            }
            else if (rptType.Contains("Commission") && data is List<CommissionReportRow> comms)
            {
                lblChart1Title.Text = "Commissions Earned by Agent (₱k)";
                lblChart2Title.Text = RbacService.CanViewBrokerageMargins
                    ? "Commission Revenue Split (₱k)"
                    : "Agent Commission Share (%)";

                if (comms.Count == 0)
                {
                    BiDisplayConstants.ShowPlotEmpty(plotReport1, "No commissions in selected period");
                    BiDisplayConstants.ShowPlotEmpty(plotReport2, "No commissions in selected period");
                    return;
                }

                // Chart 1: Commissions per agent
                var agentComms = comms
                    .GroupBy(r => string.IsNullOrWhiteSpace(r.AgentName) ? "Unassigned" : r.AgentName)
                    .Select(g => new { Name = g.Key, TotalK = (double)(g.Sum(r => r.AgentPayoutAmount) / 1_000m) })
                    .OrderByDescending(x => x.TotalK)
                    .Take(7)
                    .ToList();

                BiDisplayConstants.RenderBarPlot(plotReport1,
                    agentComms.Select(x => x.Name).ToArray(),
                    agentComms.Select(x => x.TotalK).ToArray(),
                    agentComms.Select(_ => BiDisplayConstants.StatusWon).ToArray());

                // Chart 2: Payout vs Brokerage Split (Admin) OR Agent Commission Share (Manager)
                if (RbacService.CanViewBrokerageMargins)
                {
                    double totalPayoutK = (double)(comms.Sum(c => c.AgentPayoutAmount) / 1_000m);
                    double totalBrokerageK = (double)(comms.Sum(c => c.BrokerageRetainedAmount) / 1_000m);

                    var slices = new (string Label, double Value, Color Color)[]
                    {
                        ("Agent Payouts", Math.Max(0.01, totalPayoutK), BiDisplayConstants.StatusWon),
                        ("Brokerage Net", Math.Max(0.01, totalBrokerageK), BiDisplayConstants.PrimaryAccent)
                    };
                    BiDisplayConstants.RenderDonutPlot(plotReport2, slices);
                }
                else
                {
                    var palette = new[] { BiDisplayConstants.PrimaryAccent, BiDisplayConstants.StatusWon, BiDisplayConstants.StatusPending, BiDisplayConstants.SkyAccent, BiDisplayConstants.HighlightAccent };
                    var slices = agentComms.Select((x, idx) => (x.Name, Math.Max(0.01, x.TotalK), palette[idx % palette.Length])).ToList();
                    BiDisplayConstants.RenderDonutPlot(plotReport2, slices);
                }
            }
            else if (rptType.Contains("Property") && data is List<PropertyInventoryReportRow> props)
            {
                lblChart1Title.Text = "Inventory by Property Type";
                lblChart2Title.Text = "Average Days on Market (DOM)";

                if (props.Count == 0)
                {
                    BiDisplayConstants.ShowPlotEmpty(plotReport1, "No properties in selected period");
                    BiDisplayConstants.ShowPlotEmpty(plotReport2, "No properties in selected period");
                    return;
                }

                // Chart 1: Inventory by Property Type
                var typeGroups = props
                    .GroupBy(p => string.IsNullOrWhiteSpace(p.PropertyType) ? "General" : p.PropertyType)
                    .Select(g => new { Type = g.Key, Count = (double)g.Count(), AvgDom = g.Average(p => p.DaysOnMarket) })
                    .OrderByDescending(x => x.Count)
                    .ToList();

                BiDisplayConstants.RenderBarPlot(plotReport1,
                    typeGroups.Select(x => x.Type).ToArray(),
                    typeGroups.Select(x => x.Count).ToArray(),
                    typeGroups.Select(_ => BiDisplayConstants.PrimaryAccent).ToArray());

                // Chart 2: Average Days on Market
                BiDisplayConstants.RenderBarPlot(plotReport2,
                    typeGroups.Select(x => x.Type).ToArray(),
                    typeGroups.Select(x => Math.Round(x.AvgDom, 1)).ToArray(),
                    typeGroups.Select(_ => BiDisplayConstants.StatusPending).ToArray());
            }
            else if (rptType.Contains("Lead") && data is List<LeadProgressRow> leads)
            {
                lblChart1Title.Text = "Leads by Acquisition Source";
                lblChart2Title.Text = "Pipeline Stage Breakdown";

                if (leads.Count == 0)
                {
                    BiDisplayConstants.ShowPlotEmpty(plotReport1, "No leads in selected period");
                    BiDisplayConstants.ShowPlotEmpty(plotReport2, "No leads in selected period");
                    return;
                }

                // Chart 1: Source
                var sources = leads
                    .GroupBy(r => string.IsNullOrWhiteSpace(r.Source) ? "Unknown" : r.Source)
                    .Select(g => new { Source = g.Key, Count = (double)g.Count() })
                    .OrderByDescending(x => x.Count)
                    .Take(7)
                    .ToList();

                BiDisplayConstants.RenderBarPlot(plotReport1,
                    sources.Select(x => x.Source).ToArray(),
                    sources.Select(x => x.Count).ToArray(),
                    sources.Select(_ => BiDisplayConstants.PrimaryAccent).ToArray());

                // Chart 2: Pipeline Stages
                var stages = leads
                    .GroupBy(r => string.IsNullOrWhiteSpace(r.Stage) ? "Unknown" : r.Stage)
                    .Select(g => new { Stage = g.Key, Count = (double)g.Count() })
                    .OrderByDescending(x => x.Count)
                    .ToList();

                var stageColors = new[] { BiDisplayConstants.StatusNeutral, BiDisplayConstants.StatusPending, BiDisplayConstants.PrimaryAccent, BiDisplayConstants.StatusWon, BiDisplayConstants.StatusLost };
                BiDisplayConstants.RenderBarPlot(plotReport2,
                    stages.Select(x => x.Stage).ToArray(),
                    stages.Select(x => x.Count).ToArray(),
                    stages.Select((_, idx) => stageColors[idx % stageColors.Length]).ToArray());
            }
            else if (rptType.Contains("Ticket") && data is List<TicketResolutionRow> tickets)
            {
                lblChart1Title.Text = "Tickets by Priority";
                lblChart2Title.Text = "SLA Compliance Performance";

                if (tickets.Count == 0)
                {
                    BiDisplayConstants.ShowPlotEmpty(plotReport1, "No tickets in selected period");
                    BiDisplayConstants.ShowPlotEmpty(plotReport2, "No tickets in selected period");
                    return;
                }

                // Chart 1: Priority
                var priorities = tickets
                    .GroupBy(r => string.IsNullOrWhiteSpace(r.Priority) ? "Normal" : r.Priority)
                    .Select(g => new { Priority = g.Key, Count = (double)g.Count() })
                    .OrderByDescending(x => x.Count)
                    .ToList();

                var prioColors = priorities.Select(p => p.Priority.ToLowerInvariant() switch
                {
                    "urgent" or "critical" or "high" => BiDisplayConstants.StatusLost,
                    "medium" or "normal" => BiDisplayConstants.StatusPending,
                    _ => BiDisplayConstants.StatusNeutral
                }).ToArray();

                BiDisplayConstants.RenderBarPlot(plotReport1,
                    priorities.Select(x => x.Priority).ToArray(),
                    priorities.Select(x => x.Count).ToArray(),
                    prioColors);

                // Chart 2: SLA Met vs Missed (Donut)
                int met = tickets.Count(t => t.SlaMet == "Yes");
                int missed = tickets.Count(t => t.SlaMet == "No");
                int pending = tickets.Count(t => t.SlaMet != "Yes" && t.SlaMet != "No");

                var slices = new List<(string Label, double Value, Color Color)>();
                if (met > 0) slices.Add(("Met SLA", (double)met, BiDisplayConstants.StatusWon));
                if (missed > 0) slices.Add(("Breached", (double)missed, BiDisplayConstants.StatusLost));
                if (pending > 0) slices.Add(("In Progress", (double)pending, BiDisplayConstants.StatusNeutral));

                BiDisplayConstants.RenderDonutPlot(plotReport2, slices);
            }
            else if (rptType.Contains("Agent") && data is List<AgentActivityRow> acts)
            {
                lblChart1Title.Text = "Sales Volume by Agent (₱ Millions)";
                lblChart2Title.Text = "Agency Operational Activity Breakdown";

                if (acts.Count == 0)
                {
                    BiDisplayConstants.ShowPlotEmpty(plotReport1, "No activities recorded in selected period");
                    BiDisplayConstants.ShowPlotEmpty(plotReport2, "No deals recorded in selected period");
                    return;
                }

                // Chart 1: Sales volume by agent
                var topVol = acts.OrderByDescending(a => a.TotalSalesVolume).Take(7).ToList();
                BiDisplayConstants.RenderBarPlot(plotReport1,
                    topVol.Select(x => x.AgentName).ToArray(),
                    topVol.Select(x => (double)(x.TotalSalesVolume / 1_000_000m)).ToArray(),
                    topVol.Select(_ => BiDisplayConstants.PrimaryAccent).ToArray());

                // Chart 2: Operations breakdown
                int totalLeads = acts.Sum(a => a.ActiveLeads);
                int totalDeals = acts.Sum(a => a.DealsClosed);
                int totalFollowUps = acts.Sum(a => a.FollowUpsCompleted);
                int totalTickets = acts.Sum(a => a.TicketsResolved);

                List<string> labels = new();
                List<double> vals = new();
                List<Color> colors = new();

                labels.Add("Leads"); vals.Add(totalLeads); colors.Add(BiDisplayConstants.PrimaryAccent);
                labels.Add("Deals"); vals.Add(totalDeals); colors.Add(BiDisplayConstants.StatusWon);

                // Strictly enforce Manager RBAC boundary: NO individual Agent Follow-Ups visibility
                if (!RbacService.IsManager)
                {
                    labels.Add("Follow-Ups"); vals.Add(totalFollowUps); colors.Add(BiDisplayConstants.StatusPending);
                }

                labels.Add("Tickets"); vals.Add(totalTickets); colors.Add(BiDisplayConstants.SkyAccent);

                BiDisplayConstants.RenderBarPlot(plotReport2, labels.ToArray(), vals.ToArray(), colors.ToArray());
            }
        }

        private void ShowPlotEmpty(ScottPlot.WinForms.FormsPlot plot, string message)
        {
            BiDisplayConstants.ShowPlotEmpty(plot, message);
        }

        private DateRangeFilter GetDateRange()
        {
            string sel = cboDateRange.SelectedItem?.ToString() ?? "This Month";
            if (sel == "This Month") return DateRangeFilter.ThisMonth();
            if (sel == "This Quarter") return DateRangeFilter.ThisQuarter();
            if (sel == "This Year") return DateRangeFilter.ThisYear();
            if (sel == "Past 12 Months") return DateRangeFilter.Past12Months();
            if (sel == "All Time") return DateRangeFilter.AllTime();
            return DateRangeFilter.Custom(dtpStart.Value.Date, dtpEnd.Value.Date.AddDays(1).AddSeconds(-1));
        }

        private void FormatGrid(string rptType)
        {
            if (gridData.Columns.Count == 0) return;

            // Hide backward compatibility alias columns
            string[] hiddenCols = { "Value", "ClosedDate", "DaysInStage", "CommissionRate", "CommissionAmount", "LeadsWorked" };
            foreach (var colName in hiddenCols)
            {
                if (gridData.Columns.Contains(colName) && gridData.Columns[colName] != null)
                    gridData.Columns[colName]!.Visible = false;
            }

            // Hide internal brokerage retained margin from managers
            if (!RbacService.CanViewBrokerageMargins && gridData.Columns.Contains("BrokerageRetainedAmount") && gridData.Columns["BrokerageRetainedAmount"] != null)
            {
                gridData.Columns["BrokerageRetainedAmount"]!.Visible = false;
            }

            // Strictly hide Agent Follow-Ups from Managers (personal snapshot only)
            if (RbacService.IsManager && gridData.Columns.Contains("FollowUpsCompleted") && gridData.Columns["FollowUpsCompleted"] != null)
            {
                gridData.Columns["FollowUpsCompleted"]!.Visible = false;
            }

            foreach (DataGridViewColumn col in gridData.Columns)
            {
                col.AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill;
                col.HeaderText = FormatGridHeader(col.Name);

                if (col.ValueType == typeof(decimal))
                {
                    if (col.Name.Contains("Percent") || col.Name.Contains("Rate"))
                    {
                        col.DefaultCellStyle.Format = "0.0'%'";
                    }
                    else
                    {
                        col.DefaultCellStyle.Format = "₱#,##0.00";
                    }
                    col.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight;
                    col.HeaderCell.Style.Alignment = DataGridViewContentAlignment.MiddleRight;
                }
                else if (col.ValueType == typeof(double))
                {
                    if (col.Name.Contains("Rate") || col.Name.Contains("Percent"))
                    {
                        col.DefaultCellStyle.Format = "0.0'%'";
                    }
                    else
                    {
                        col.DefaultCellStyle.Format = "N1";
                    }
                    col.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight;
                    col.HeaderCell.Style.Alignment = DataGridViewContentAlignment.MiddleRight;
                }
                else if (col.ValueType == typeof(int))
                {
                    col.DefaultCellStyle.Format = "N0";
                    col.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight;
                    col.HeaderCell.Style.Alignment = DataGridViewContentAlignment.MiddleRight;
                }
            }

            UiGridHelper.EnforceTableStandards(gridData);
        }

        private static string FormatGridHeader(string propName)
        {
            return propName switch
            {
                "DealRef" => "Deal Ref",
                "CustomerName" => "Customer Name",
                "PropertyAddress" => "Property Address",
                "PropertyType" => "Property Type",
                "AgentName" => "Assigned Agent",
                "DealValue" => "Deal Value",
                "Commission" => "Commission",
                "GrossCommission" => "Gross Commission",
                "AgentSplitPercent" => "Agent Split %",
                "AgentPayoutAmount" => "Agent Payout",
                "BrokerageRetainedAmount" => "Brokerage Net",
                "SettlementStatus" => "Status",
                "ExpectedOrClosedDate" => "Closing / Expected Date",
                "PropertyRef" => "Property Ref",
                "ListingPrice" => "Listing Price",
                "ListingAgent" => "Listing Agent",
                "DaysOnMarket" => "Days on Market",
                "AssociatedDeals" => "Active Deals",
                "ListedDate" => "Listed Date",
                "LeadName" => "Lead Name",
                "EstimatedBudget" => "Est. Budget",
                "DaysInPipeline" => "Days in Pipeline",
                "ConvertedToCustomer" => "Converted",
                "CreatedDate" => "Created Date",
                "TicketNumber" => "Ticket #",
                "OpenedDate" => "Opened Date",
                "ResolvedDate" => "Resolved Date",
                "ResolutionTime" => "Resolution Time",
                "SlaMet" => "SLA Met",
                "ActiveLeads" => "Active Leads",
                "LeadsConverted" => "Converted Leads",
                "ConversionRate" => "Conv. Rate %",
                "DealsClosed" => "Deals Closed",
                "TotalSalesVolume" => "Sales Volume",
                "TotalCommissionEarned" => "Total Commission",
                "FollowUpsCompleted" => "Follow-Ups Done",
                "TicketsResolved" => "Tickets Resolved",
                _ => System.Text.RegularExpressions.Regex.Replace(propName, "([A-Z])", " $1").Trim()
            };
        }

        private void BtnExportCsv_Click(object? sender, EventArgs e)
        {
            if (_currentData == null || _currentHeader == null) return;
            var rpt = cboReportType.SelectedItem?.ToString() ?? "";
            if (rpt.Contains("Commission") && !RbacService.CanExportFinancialSettlements)
            {
                MessageBox.Show("Commission and financial settlement exports are restricted to Administrators.", "Access Restricted", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            using var sfd = new SaveFileDialog { Filter = "CSV Files|*.csv", FileName = $"{_currentHeader.ReportName.Replace(" ", "_")}_{DateTime.Now:yyyyMMdd}.csv" };
            if (sfd.ShowDialog() == DialogResult.OK)
            {
                ExportDynamic(sfd.FileName, true);
                MessageBox.Show("Exported to CSV successfully.", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }

        private void BtnExportPdf_Click(object? sender, EventArgs e)
        {
            if (_currentData == null || _currentHeader == null) return;
            var rpt = cboReportType.SelectedItem?.ToString() ?? "";
            if (rpt.Contains("Commission") && !RbacService.CanExportFinancialSettlements)
            {
                MessageBox.Show("Commission and financial settlement exports are restricted to Administrators.", "Access Restricted", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            using var sfd = new SaveFileDialog { Filter = "PDF Files|*.pdf", FileName = $"{_currentHeader.ReportName.Replace(" ", "_")}_{DateTime.Now:yyyyMMdd}.pdf" };
            if (sfd.ShowDialog() == DialogResult.OK)
            {
                ExportDynamic(sfd.FileName, false);
                MessageBox.Show("Exported to PDF successfully.", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }

        private void ExportDynamic(string filePath, bool isCsv)
        {
            if (_currentData == null || _currentHeader == null) return;
            var rpt = cboReportType.SelectedItem?.ToString() ?? "";

            if (rpt.Contains("Sales"))
            {
                if (isCsv) _controller.ExportToCsv((List<SalesReportRow>)_currentData, filePath, _currentHeader);
                else _controller.ExportToPdf((List<SalesReportRow>)_currentData, filePath, _currentHeader);
            }
            else if (rpt.Contains("Commission"))
            {
                if (isCsv) _controller.ExportToCsv((List<CommissionReportRow>)_currentData, filePath, _currentHeader);
                else _controller.ExportToPdf((List<CommissionReportRow>)_currentData, filePath, _currentHeader);
            }
            else if (rpt.Contains("Property"))
            {
                if (isCsv) _controller.ExportToCsv((List<PropertyInventoryReportRow>)_currentData, filePath, _currentHeader);
                else _controller.ExportToPdf((List<PropertyInventoryReportRow>)_currentData, filePath, _currentHeader);
            }
            else if (rpt.Contains("Lead"))
            {
                if (isCsv) _controller.ExportToCsv((List<LeadProgressRow>)_currentData, filePath, _currentHeader);
                else _controller.ExportToPdf((List<LeadProgressRow>)_currentData, filePath, _currentHeader);
            }
            else if (rpt.Contains("Ticket"))
            {
                if (isCsv) _controller.ExportToCsv((List<TicketResolutionRow>)_currentData, filePath, _currentHeader);
                else _controller.ExportToPdf((List<TicketResolutionRow>)_currentData, filePath, _currentHeader);
            }
            else if (rpt.Contains("Agent"))
            {
                if (isCsv) _controller.ExportToCsv((List<AgentActivityRow>)_currentData, filePath, _currentHeader);
                else _controller.ExportToPdf((List<AgentActivityRow>)_currentData, filePath, _currentHeader);
            }
        }

        private void ToggleKpiFilter(int kpiIndex)
        {
            if (_unfilteredData == null) return;

            if (_selectedKpiIndex == kpiIndex)
            {
                _selectedKpiIndex = 0;
            }
            else
            {
                _selectedKpiIndex = kpiIndex;
            }

            UpdateKpiSelectionStates();
            ApplyKpiFilter();
        }

        private void UpdateKpiSelectionStates()
        {
            kpi1.SetSelected(_selectedKpiIndex == 1);
            kpi2.SetSelected(_selectedKpiIndex == 2);
            kpi3.SetSelected(_selectedKpiIndex == 3);
            kpi4.SetSelected(_selectedKpiIndex == 4);
        }

        private void ApplyKpiFilter()
        {
            if (_unfilteredData == null) return;
            var rpt = cboReportType.SelectedItem?.ToString() ?? "Sales & Revenue Report";

            object? displayData = _unfilteredData;

            if (_selectedKpiIndex > 0)
            {
                if (rpt.Contains("Sales") && _unfilteredData is List<SalesReportRow> sales)
                {
                    displayData = _selectedKpiIndex switch
                    {
                        1 => sales,
                        2 => sales.Where(s => s.Stage.Contains("Closed", StringComparison.OrdinalIgnoreCase) || s.Stage.Contains("Won", StringComparison.OrdinalIgnoreCase)).ToList(),
                        3 => sales.Where(s => s.Commission > 0).ToList(),
                        4 => sales.Where(s => s.DealValue >= (sales.Count == 0 ? 0 : sales.Average(x => x.DealValue))).ToList(),
                        _ => sales
                    };
                }
                else if (rpt.Contains("Commission") && _unfilteredData is List<CommissionReportRow> comms)
                {
                    displayData = _selectedKpiIndex switch
                    {
                        1 => comms,
                        2 => comms.Where(c => c.GrossCommission > 0).ToList(),
                        3 => comms.Where(c => c.AgentPayoutAmount > 0).ToList(),
                        4 => comms.Where(c => c.BrokerageRetainedAmount > 0).ToList(),
                        _ => comms
                    };
                }
                else if (rpt.Contains("Property") && _unfilteredData is List<PropertyInventoryReportRow> props)
                {
                    displayData = _selectedKpiIndex switch
                    {
                        1 => props,
                        2 => props.Where(p => p.Status.Contains("Available", StringComparison.OrdinalIgnoreCase) || p.Status.Contains("Active", StringComparison.OrdinalIgnoreCase)).ToList(),
                        3 => props.Where(p => p.DaysOnMarket >= 30).ToList(),
                        4 => props.Where(p => p.AssociatedDeals > 0).ToList(),
                        _ => props
                    };
                }
                else if (rpt.Contains("Lead") && _unfilteredData is List<LeadProgressRow> leads)
                {
                    displayData = _selectedKpiIndex switch
                    {
                        1 => leads,
                        2 => leads.Where(l => string.Equals(l.ConvertedToCustomer, "Yes", StringComparison.OrdinalIgnoreCase)).ToList(),
                        3 => leads.Where(l => l.DaysInPipeline > 14).ToList(),
                        4 => leads.Where(l => l.EstimatedBudget > 0).ToList(),
                        _ => leads
                    };
                }
                else if (rpt.Contains("Ticket") && _unfilteredData is List<TicketResolutionRow> tickets)
                {
                    displayData = _selectedKpiIndex switch
                    {
                        1 => tickets,
                        2 => tickets.Where(t => string.Equals(t.Status, "Resolved", StringComparison.OrdinalIgnoreCase) || string.Equals(t.Status, "Closed", StringComparison.OrdinalIgnoreCase)).ToList(),
                        3 => tickets.Where(t => string.Equals(t.SlaMet, "Yes", StringComparison.OrdinalIgnoreCase)).ToList(),
                        4 => tickets.Where(t => string.Equals(t.SlaMet, "No", StringComparison.OrdinalIgnoreCase)).ToList(),
                        _ => tickets
                    };
                }
                else if (rpt.Contains("Agent") && _unfilteredData is List<AgentActivityRow> acts)
                {
                    displayData = _selectedKpiIndex switch
                    {
                        1 => acts,
                        2 => acts.Where(a => a.DealsClosed > 0).ToList(),
                        3 => acts.Where(a => a.TotalSalesVolume > 0).ToList(),
                        4 => acts.Where(a => a.TicketsResolved > 0).ToList(),
                        _ => acts
                    };
                }
            }

            _currentData = displayData;
            gridData.DataSource = null;
            gridData.DataSource = _currentData;
            FormatGrid(rpt);

            if (_currentHeader != null)
            {
                string filterSuffix = _selectedKpiIndex switch
                {
                    1 => " (All Items)",
                    2 => " (KPI 2 Filter)",
                    3 => " (KPI 3 Filter)",
                    4 => " (KPI 4 Filter)",
                    _ => ""
                };
                lblReportHeader.Text = $"{_currentHeader.ReportName}{filterSuffix} · {_currentHeader.DateRange} · Generated by {_currentHeader.GeneratedBy} at {_currentHeader.GeneratedAt:g}";
            }
        }

        private void GridData_CellPainting(object? sender, DataGridViewCellPaintingEventArgs e)
        {
            if (e.RowIndex < 0 || e.ColumnIndex < 0) return;

            var col = gridData.Columns[e.ColumnIndex];
            string colName = col.Name;
            object? rawVal = e.Value;
            string text = rawVal?.ToString() ?? string.Empty;

            // 1. Status & Stage pill badges
            if (colName is "Status" or "Stage" or "SettlementStatus" or "Priority" or "SlaMet")
            {
                e.Handled = true;
                UiGridHelper.PaintStatusBadge(gridData, e, text);
                return;
            }

            // 2. Bold identifiers
            if (colName.EndsWith("Ref") || colName == "TicketNumber" || colName.EndsWith("Id"))
            {
                e.Handled = true;
                using var boldFont = new Font("Segoe UI", 9.5f, FontStyle.Bold);
                UiGridHelper.PaintTextCell(gridData, e, text, boldFont, Theme.TextPrimary);
                return;
            }
        }
    }
}
