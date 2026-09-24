namespace CRMS_Peguit.winforms.Views.Analytics
{
    partial class AnalyticsView
    {
        private System.ComponentModel.IContainer components = null;

        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            if (disposing && _controller != null)
            {
                _controller.Dispose();
            }
            base.Dispose(disposing);
        }

        private void InitializeComponent()
        {
            this.pnlHeader = new System.Windows.Forms.Panel();
            this.lblTitle = new System.Windows.Forms.Label();
            this.lblSubtitle = new System.Windows.Forms.Label();
            this.lblLoading = new System.Windows.Forms.Label();
            this.btnGoToReports = new System.Windows.Forms.Button();
            this.cboDateRange = new System.Windows.Forms.ComboBox();
            this.btnExport = new System.Windows.Forms.Button();
            
            this.pnlKpi = new System.Windows.Forms.TableLayoutPanel();
            this.kpiDealsClosed = new CRMS_Peguit.winforms.Controls.KpiCard("DEALS CLOSED", "deals_closed", CRMS_Peguit.winforms.Models.Services.Theme.Primary, CRMS_Peguit.winforms.Models.Services.KpiIconType.Briefcase, "In selected period");
            this.kpiCommission = new CRMS_Peguit.winforms.Controls.KpiCard("COMMISSION", "commission", CRMS_Peguit.winforms.Models.Services.Theme.StatusSuccess, CRMS_Peguit.winforms.Models.Services.KpiIconType.Currency, "Net earned");
            this.kpiActiveLeads = new CRMS_Peguit.winforms.Controls.KpiCard("ACTIVE LEADS", "active_leads", CRMS_Peguit.winforms.Models.Services.Theme.StatusPending, CRMS_Peguit.winforms.Models.Services.KpiIconType.Target, "In pipeline");
            this.kpiConversionRate = new CRMS_Peguit.winforms.Controls.KpiCard("CONVERSION RATE", "conversion_rate", CRMS_Peguit.winforms.Models.Services.Theme.PrimaryDark, CRMS_Peguit.winforms.Models.Services.KpiIconType.Refresh, "Leads to closed");
            this.kpiOpenTickets = new CRMS_Peguit.winforms.Controls.KpiCard("OPEN TICKETS", "open_tickets", CRMS_Peguit.winforms.Models.Services.Theme.StatusAlert, CRMS_Peguit.winforms.Models.Services.KpiIconType.Ticket, "Support queue");
            this.kpiAvgDays = new CRMS_Peguit.winforms.Controls.KpiCard("AVG DAYS TO CLOSE", "avg_days", CRMS_Peguit.winforms.Models.Services.Theme.StatusNeutral, CRMS_Peguit.winforms.Models.Services.KpiIconType.Clock, "Contract lead time");

            this.pnlScrollableContent = new System.Windows.Forms.Panel();
            
            this.chartDealsClosed = new CRMS_Peguit.winforms.Controls.ChartWrapperControl("Deals Closed Over Time", "Monthly closed deals in selected period", CRMS_Peguit.winforms.Controls.KpiClickMode.InPlaceFilter);
            this.chartPipeline = new CRMS_Peguit.winforms.Controls.ChartWrapperControl("Lead Pipeline Funnel", "Stage-by-stage lead progression", CRMS_Peguit.winforms.Controls.KpiClickMode.InPlaceFilter);
            this.chartWonVsLost = new CRMS_Peguit.winforms.Controls.ChartWrapperControl("Deals Won vs. Lost", "Win/loss outcome distribution", CRMS_Peguit.winforms.Controls.KpiClickMode.InPlaceFilter);
            this.chartTickets = new CRMS_Peguit.winforms.Controls.ChartWrapperControl("Support Ticket Breakdown", "Ticket status across the queue", CRMS_Peguit.winforms.Controls.KpiClickMode.InPlaceFilter);
            this.chartAgents = new CRMS_Peguit.winforms.Controls.ChartWrapperControl("Top-Performing Agents", "By total sales volume (₱ millions)", CRMS_Peguit.winforms.Controls.KpiClickMode.InPlaceFilter);
            this.chartSources = new CRMS_Peguit.winforms.Controls.ChartWrapperControl("Lead Source Breakdown", "Leads generated per acquisition channel", CRMS_Peguit.winforms.Controls.KpiClickMode.InPlaceFilter);

            this.pnlRecentActivity = new System.Windows.Forms.Panel();
            this.lblRecentActivityTitle = new System.Windows.Forms.Label();
            this.lblRecentActivitySubtitle = new System.Windows.Forms.Label();
            this.pnlActivityFeedList = new System.Windows.Forms.FlowLayoutPanel();

            this.pnlGridCard = new System.Windows.Forms.Panel();
            this.pnlGridHeader = new System.Windows.Forms.Panel();
            this.lblGridTitle = new System.Windows.Forms.Label();
            this.lblGridSubtitle = new System.Windows.Forms.Label();
            this.btnResetFilter = new System.Windows.Forms.Button();
            this.gridAnalyticsDetails = new System.Windows.Forms.DataGridView();

            this.pnlHeader.SuspendLayout();
            this.pnlKpi.SuspendLayout();
            this.pnlScrollableContent.SuspendLayout();
            this.pnlRecentActivity.SuspendLayout();
            this.pnlGridCard.SuspendLayout();
            this.pnlGridHeader.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.gridAnalyticsDetails)).BeginInit();
            this.SuspendLayout();

            // pnlHeader
            this.pnlHeader.Controls.Add(this.btnGoToReports);
            this.pnlHeader.Controls.Add(this.btnExport);
            this.pnlHeader.Controls.Add(this.cboDateRange);
            this.pnlHeader.Controls.Add(this.lblLoading);
            this.pnlHeader.Controls.Add(this.lblSubtitle);
            this.pnlHeader.Controls.Add(this.lblTitle);
            this.pnlHeader.Dock = System.Windows.Forms.DockStyle.Top;
            this.pnlHeader.Height = 85;
            this.pnlHeader.Padding = new System.Windows.Forms.Padding(20);
            
            // lblTitle
            this.lblTitle.AutoSize = true;
            this.lblTitle.Font = CRMS_Peguit.winforms.Services.UiStyleConstants.PageTitleFont;
            this.lblTitle.ForeColor = CRMS_Peguit.winforms.Models.Services.Theme.TextPrimary;
            this.lblTitle.Location = new System.Drawing.Point(20, 15);
            this.lblTitle.Text = "Analytics & Insights";

            // lblSubtitle
            this.lblSubtitle.AutoSize = true;
            this.lblSubtitle.Font = CRMS_Peguit.winforms.Services.UiStyleConstants.SubtitleFont;
            this.lblSubtitle.ForeColor = CRMS_Peguit.winforms.Models.Services.Theme.TextSecondary;
            this.lblSubtitle.Location = new System.Drawing.Point(22, 50);
            this.lblSubtitle.Text = "Live business intelligence across your deals, leads, and operations";

            // lblLoading
            this.lblLoading.AutoSize = true;
            this.lblLoading.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Italic);
            this.lblLoading.ForeColor = CRMS_Peguit.winforms.Models.Services.Theme.TextSecondary;
            this.lblLoading.Location = new System.Drawing.Point(22, 50);
            this.lblLoading.Text = "Calculating live metrics & charts...";
            this.lblLoading.Visible = false;

            // btnGoToReports
            this.btnGoToReports.Anchor = System.Windows.Forms.AnchorStyles.None;
            this.btnGoToReports.BackColor = System.Drawing.Color.White;
            this.btnGoToReports.FlatAppearance.BorderColor = System.Drawing.Color.FromArgb(203, 213, 225);
            this.btnGoToReports.FlatAppearance.BorderSize = 1;
            this.btnGoToReports.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnGoToReports.Font = new System.Drawing.Font("Segoe UI Semibold", 9F);
            this.btnGoToReports.ForeColor = System.Drawing.Color.FromArgb(37, 103, 156);
            this.btnGoToReports.Location = new System.Drawing.Point(360, 23);
            this.btnGoToReports.Name = "btnGoToReports";
            this.btnGoToReports.Size = new System.Drawing.Size(140, 34);
            this.btnGoToReports.TabIndex = 0;
            this.btnGoToReports.Text = "📋 View Reports";
            this.btnGoToReports.UseVisualStyleBackColor = false;

            // cboDateRange
            this.cboDateRange.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cboDateRange.Font = new System.Drawing.Font("Segoe UI", 9.5F);
            this.cboDateRange.Items.AddRange(new object[] { "This Month", "This Quarter", "This Year", "Past 12 Months", "All Time" });
            this.cboDateRange.Location = new System.Drawing.Point(525, 26);
            this.cboDateRange.Width = 150;
            this.cboDateRange.Anchor = System.Windows.Forms.AnchorStyles.None;

            // btnExport
            this.btnExport.Font = new System.Drawing.Font("Segoe UI", 9.5F, System.Drawing.FontStyle.Bold);
            this.btnExport.Location = new System.Drawing.Point(690, 23);
            this.btnExport.Width = 130;
            this.btnExport.Height = 34;
            this.btnExport.Text = "📥 Export CSV";
            this.btnExport.Anchor = System.Windows.Forms.AnchorStyles.None;

            // pnlKpi
            this.pnlKpi.Dock = System.Windows.Forms.DockStyle.None;
            this.pnlKpi.Height = 114;
            this.pnlKpi.ColumnCount = 6;
            this.pnlKpi.RowCount = 1;
            this.pnlKpi.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 16.666f));
            this.pnlKpi.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 16.666f));
            this.pnlKpi.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 16.666f));
            this.pnlKpi.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 16.666f));
            this.pnlKpi.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 16.666f));
            this.pnlKpi.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 16.666f));
            this.pnlKpi.Padding = new System.Windows.Forms.Padding(16, 0, 16, 8);

            kpiDealsClosed.Dock = System.Windows.Forms.DockStyle.Fill;
            kpiCommission.Dock = System.Windows.Forms.DockStyle.Fill;
            kpiActiveLeads.Dock = System.Windows.Forms.DockStyle.Fill;
            kpiConversionRate.Dock = System.Windows.Forms.DockStyle.Fill;
            kpiOpenTickets.Dock = System.Windows.Forms.DockStyle.Fill;
            kpiAvgDays.Dock = System.Windows.Forms.DockStyle.Fill;

            this.pnlKpi.Controls.Add(kpiDealsClosed, 0, 0);
            this.pnlKpi.Controls.Add(kpiCommission, 1, 0);
            this.pnlKpi.Controls.Add(kpiActiveLeads, 2, 0);
            this.pnlKpi.Controls.Add(kpiConversionRate, 3, 0);
            this.pnlKpi.Controls.Add(kpiOpenTickets, 4, 0);
            this.pnlKpi.Controls.Add(kpiAvgDays, 5, 0);

            // pnlScrollableContent
            this.pnlScrollableContent.Dock = System.Windows.Forms.DockStyle.Fill;
            this.pnlScrollableContent.AutoScroll = true;
            this.pnlScrollableContent.Padding = new System.Windows.Forms.Padding(20);

            // pnlRecentActivity
            this.pnlRecentActivity.BackColor = System.Drawing.Color.White;
            this.lblRecentActivityTitle.Text = "Recent Activity Feed";
            this.lblRecentActivityTitle.Font = new System.Drawing.Font("Segoe UI", 12F, System.Drawing.FontStyle.Bold);
            this.lblRecentActivityTitle.ForeColor = CRMS_Peguit.winforms.Models.Services.Theme.TextPrimary;
            this.lblRecentActivityTitle.Location = new System.Drawing.Point(16, 14);
            this.lblRecentActivityTitle.AutoSize = true;

            this.lblRecentActivitySubtitle.Text = "Latest CRM events across all agents";
            this.lblRecentActivitySubtitle.Font = new System.Drawing.Font("Segoe UI", 8.5F);
            this.lblRecentActivitySubtitle.ForeColor = CRMS_Peguit.winforms.Models.Services.Theme.TextSecondary;
            this.lblRecentActivitySubtitle.Location = new System.Drawing.Point(18, 38);
            this.lblRecentActivitySubtitle.AutoSize = true;
            
            this.pnlActivityFeedList.Location = new System.Drawing.Point(16, 60);
            this.pnlActivityFeedList.AutoScroll = true;
            this.pnlActivityFeedList.FlowDirection = System.Windows.Forms.FlowDirection.TopDown;
            this.pnlActivityFeedList.WrapContents = false;

            this.pnlRecentActivity.Controls.Add(lblRecentActivityTitle);
            this.pnlRecentActivity.Controls.Add(lblRecentActivitySubtitle);
            this.pnlRecentActivity.Controls.Add(pnlActivityFeedList);

            // pnlGridCard
            this.pnlGridCard.BackColor = System.Drawing.Color.White;
            this.pnlGridCard.Padding = new System.Windows.Forms.Padding(16, 12, 16, 16);

            // pnlGridHeader
            this.pnlGridHeader.Dock = System.Windows.Forms.DockStyle.Top;
            this.pnlGridHeader.Height = 56;
            this.pnlGridHeader.BackColor = System.Drawing.Color.Transparent;

            this.lblGridTitle.Text = "Analytics Drill-Down Ledger";
            this.lblGridTitle.Font = new System.Drawing.Font("Segoe UI", 12F, System.Drawing.FontStyle.Bold);
            this.lblGridTitle.ForeColor = CRMS_Peguit.winforms.Models.Services.Theme.TextPrimary;
            this.lblGridTitle.Location = new System.Drawing.Point(0, 4);
            this.lblGridTitle.AutoSize = true;

            this.lblGridSubtitle.Text = "Click any KPI card or chart segment to filter records in-place (Case 1 standard)";
            this.lblGridSubtitle.Font = new System.Drawing.Font("Segoe UI", 8.5F);
            this.lblGridSubtitle.ForeColor = CRMS_Peguit.winforms.Models.Services.Theme.TextSecondary;
            this.lblGridSubtitle.Location = new System.Drawing.Point(1, 28);
            this.lblGridSubtitle.AutoSize = true;

            this.btnResetFilter.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.btnResetFilter.Font = new System.Drawing.Font("Segoe UI Semibold", 8.5F);
            this.btnResetFilter.Text = "✕ Clear Drill-Down Filter";
            this.btnResetFilter.Size = new System.Drawing.Size(180, 32);
            this.btnResetFilter.Cursor = System.Windows.Forms.Cursors.Hand;
            this.btnResetFilter.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnResetFilter.BackColor = System.Drawing.Color.FromArgb(241, 245, 249);
            this.btnResetFilter.ForeColor = CRMS_Peguit.winforms.Models.Services.Theme.TextSecondary;

            this.pnlGridHeader.Controls.Add(this.lblGridTitle);
            this.pnlGridHeader.Controls.Add(this.lblGridSubtitle);
            this.pnlGridHeader.Controls.Add(this.btnResetFilter);

            // gridAnalyticsDetails
            this.gridAnalyticsDetails.Dock = System.Windows.Forms.DockStyle.Fill;
            this.gridAnalyticsDetails.AllowUserToAddRows = false;
            this.gridAnalyticsDetails.AllowUserToDeleteRows = false;
            this.gridAnalyticsDetails.ReadOnly = true;
            this.gridAnalyticsDetails.SelectionMode = System.Windows.Forms.DataGridViewSelectionMode.FullRowSelect;
            this.gridAnalyticsDetails.BackgroundColor = System.Drawing.Color.White;
            this.gridAnalyticsDetails.BorderStyle = System.Windows.Forms.BorderStyle.None;

            this.pnlGridCard.Controls.Add(this.gridAnalyticsDetails);
            this.pnlGridCard.Controls.Add(this.pnlGridHeader);

            // Add all to scrollable content
            this.pnlScrollableContent.Controls.Add(this.pnlKpi);
            this.pnlScrollableContent.Controls.Add(this.chartDealsClosed);
            this.pnlScrollableContent.Controls.Add(this.chartPipeline);
            this.pnlScrollableContent.Controls.Add(this.chartWonVsLost);
            this.pnlScrollableContent.Controls.Add(this.chartTickets);
            this.pnlScrollableContent.Controls.Add(this.chartAgents);
            this.pnlScrollableContent.Controls.Add(this.chartSources);
            this.pnlScrollableContent.Controls.Add(this.pnlRecentActivity);
            this.pnlScrollableContent.Controls.Add(this.pnlGridCard);

            // AnalyticsView
            this.Controls.Add(this.pnlScrollableContent);
            this.Controls.Add(this.pnlHeader);
            this.Name = "AnalyticsView";
            this.Size = new System.Drawing.Size(1200, 850);
            
            this.pnlHeader.ResumeLayout(false);
            this.pnlHeader.PerformLayout();
            this.pnlKpi.ResumeLayout(false);
            this.pnlRecentActivity.ResumeLayout(false);
            this.pnlGridHeader.ResumeLayout(false);
            this.pnlGridHeader.PerformLayout();
            this.pnlGridCard.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.gridAnalyticsDetails)).EndInit();
            this.pnlScrollableContent.ResumeLayout(false);
            this.ResumeLayout(false);
        }

        private System.Windows.Forms.Panel pnlHeader;
        private System.Windows.Forms.Label lblTitle;
        private System.Windows.Forms.Label lblSubtitle;
        private System.Windows.Forms.Label lblLoading;
        private System.Windows.Forms.Button btnGoToReports;
        private System.Windows.Forms.ComboBox cboDateRange;
        private System.Windows.Forms.Button btnExport;
        
        private System.Windows.Forms.TableLayoutPanel pnlKpi;
        private CRMS_Peguit.winforms.Controls.KpiCard kpiDealsClosed;
        private CRMS_Peguit.winforms.Controls.KpiCard kpiCommission;
        private CRMS_Peguit.winforms.Controls.KpiCard kpiActiveLeads;
        private CRMS_Peguit.winforms.Controls.KpiCard kpiConversionRate;
        private CRMS_Peguit.winforms.Controls.KpiCard kpiOpenTickets;
        private CRMS_Peguit.winforms.Controls.KpiCard kpiAvgDays;

        private System.Windows.Forms.Panel pnlScrollableContent;
        
        private CRMS_Peguit.winforms.Controls.ChartWrapperControl chartDealsClosed;
        private CRMS_Peguit.winforms.Controls.ChartWrapperControl chartPipeline;
        private CRMS_Peguit.winforms.Controls.ChartWrapperControl chartWonVsLost;
        private CRMS_Peguit.winforms.Controls.ChartWrapperControl chartTickets;
        private CRMS_Peguit.winforms.Controls.ChartWrapperControl chartAgents;
        private CRMS_Peguit.winforms.Controls.ChartWrapperControl chartSources;

        private System.Windows.Forms.Panel pnlRecentActivity;
        private System.Windows.Forms.Label lblRecentActivityTitle;
        private System.Windows.Forms.Label lblRecentActivitySubtitle;
        private System.Windows.Forms.FlowLayoutPanel pnlActivityFeedList;

        private System.Windows.Forms.Panel pnlGridCard;
        private System.Windows.Forms.Panel pnlGridHeader;
        private System.Windows.Forms.Label lblGridTitle;
        private System.Windows.Forms.Label lblGridSubtitle;
        private System.Windows.Forms.Button btnResetFilter;
        private System.Windows.Forms.DataGridView gridAnalyticsDetails;
    }
}
