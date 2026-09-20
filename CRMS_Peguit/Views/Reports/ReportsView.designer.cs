namespace CRMS_Peguit.winforms.Views.Reports
{
    partial class ReportsView
    {
        private System.ComponentModel.IContainer components = null;

        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            if (disposing && (_controller != null))
            {
                _controller.Dispose();
            }
            base.Dispose(disposing);
        }

        private void InitializeComponent()
        {
            this.pnlTop = new System.Windows.Forms.Panel();
            this.lblTitle = new System.Windows.Forms.Label();
            this.lblSubtitle = new System.Windows.Forms.Label();
            this.btnGoToAnalytics = new System.Windows.Forms.Button();

            this.pnlFilters = new System.Windows.Forms.Panel();
            this.lblReportType = new System.Windows.Forms.Label();
            this.cboReportType = new System.Windows.Forms.ComboBox();
            this.lblDateRange = new System.Windows.Forms.Label();
            this.cboDateRange = new System.Windows.Forms.ComboBox();
            this.dtpStart = new System.Windows.Forms.DateTimePicker();
            this.lblTo = new System.Windows.Forms.Label();
            this.dtpEnd = new System.Windows.Forms.DateTimePicker();
            this.lblAgentFilter = new System.Windows.Forms.Label();
            this.cboAgentFilter = new System.Windows.Forms.ComboBox();
            this.lblSecondaryFilter = new System.Windows.Forms.Label();
            this.cboSecondaryFilter = new System.Windows.Forms.ComboBox();
            this.btnRunReport = new System.Windows.Forms.Button();
            this.btnExportCsv = new System.Windows.Forms.Button();
            this.btnExportPdf = new System.Windows.Forms.Button();

            this.btnViewBoth = new System.Windows.Forms.Button();
            this.btnViewCharts = new System.Windows.Forms.Button();
            this.btnViewTable = new System.Windows.Forms.Button();

            this.pnlKpiContainer = new System.Windows.Forms.TableLayoutPanel();
            this.kpi1 = new CRMS_Peguit.winforms.Controls.KpiCard();
            this.kpi2 = new CRMS_Peguit.winforms.Controls.KpiCard();
            this.kpi3 = new CRMS_Peguit.winforms.Controls.KpiCard();
            this.kpi4 = new CRMS_Peguit.winforms.Controls.KpiCard();

            this.pnlCharts = new System.Windows.Forms.Panel();
            this.pnlChartCard1 = new System.Windows.Forms.Panel();
            this.lblChart1Title = new System.Windows.Forms.Label();
            this.plotReport1 = new ScottPlot.WinForms.FormsPlot();

            this.pnlChartCard2 = new System.Windows.Forms.Panel();
            this.lblChart2Title = new System.Windows.Forms.Label();
            this.plotReport2 = new ScottPlot.WinForms.FormsPlot();

            this.pnlGrid = new System.Windows.Forms.Panel();
            this.lblReportHeader = new System.Windows.Forms.Label();
            this.gridData = new System.Windows.Forms.DataGridView();
            this.lblLoading = new System.Windows.Forms.Label();
            this.lblAccessDenied = new System.Windows.Forms.Label();

            this.pnlScrollableContent = new System.Windows.Forms.Panel();

            this.pnlTop.SuspendLayout();
            this.pnlFilters.SuspendLayout();
            this.pnlKpiContainer.SuspendLayout();
            this.pnlScrollableContent.SuspendLayout();
            this.pnlCharts.SuspendLayout();
            this.pnlChartCard1.SuspendLayout();
            this.pnlChartCard2.SuspendLayout();
            this.pnlGrid.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.gridData)).BeginInit();
            this.SuspendLayout();

            // 
            // pnlTop
            // 
            this.pnlTop.BackColor = System.Drawing.Color.White;
            this.pnlTop.Controls.Add(this.btnGoToAnalytics);
            this.pnlTop.Controls.Add(this.lblSubtitle);
            this.pnlTop.Controls.Add(this.lblTitle);
            this.pnlTop.Dock = System.Windows.Forms.DockStyle.Top;
            this.pnlTop.Location = new System.Drawing.Point(0, 0);
            this.pnlTop.Name = "pnlTop";
            this.pnlTop.Padding = new System.Windows.Forms.Padding(20, 14, 20, 14);
            this.pnlTop.Size = new System.Drawing.Size(1100, 80);
            this.pnlTop.TabIndex = 0;

            // 
            // lblTitle
            // 
            this.lblTitle.AutoSize = true;
            this.lblTitle.Font = new System.Drawing.Font("Segoe UI", 18F, System.Drawing.FontStyle.Bold);
            this.lblTitle.ForeColor = CRMS_Peguit.winforms.Models.Services.Theme.TextPrimary;
            this.lblTitle.Location = new System.Drawing.Point(20, 12);
            this.lblTitle.Name = "lblTitle";
            this.lblTitle.Size = new System.Drawing.Size(240, 32);
            this.lblTitle.TabIndex = 0;
            this.lblTitle.Text = "Analytics & Reports";

            // 
            // lblSubtitle
            // 
            this.lblSubtitle.AutoSize = true;
            this.lblSubtitle.Font = new System.Drawing.Font("Segoe UI", 9.5F);
            this.lblSubtitle.ForeColor = CRMS_Peguit.winforms.Models.Services.Theme.TextSecondary;
            this.lblSubtitle.Location = new System.Drawing.Point(22, 48);
            this.lblSubtitle.Name = "lblSubtitle";
            this.lblSubtitle.Size = new System.Drawing.Size(420, 17);
            this.lblSubtitle.TabIndex = 1;
            this.lblSubtitle.Text = "Interactive data graphs, detailed tabular audits, and PDF/CSV export engine";

            // 
            // btnGoToAnalytics
            // 
            this.btnGoToAnalytics.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.btnGoToAnalytics.BackColor = System.Drawing.Color.FromArgb(241, 245, 249);
            this.btnGoToAnalytics.FlatAppearance.BorderSize = 1;
            this.btnGoToAnalytics.FlatAppearance.BorderColor = System.Drawing.Color.FromArgb(203, 213, 225);
            this.btnGoToAnalytics.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnGoToAnalytics.Font = new System.Drawing.Font("Segoe UI Semibold", 9F, System.Drawing.FontStyle.Bold);
            this.btnGoToAnalytics.ForeColor = System.Drawing.Color.FromArgb(37, 103, 156);
            this.btnGoToAnalytics.Location = new System.Drawing.Point(880, 18);
            this.btnGoToAnalytics.Name = "btnGoToAnalytics";
            this.btnGoToAnalytics.Size = new System.Drawing.Size(200, 36);
            this.btnGoToAnalytics.TabIndex = 2;
            this.btnGoToAnalytics.Text = "📊 Agency Analytics Dashboard";
            this.btnGoToAnalytics.UseVisualStyleBackColor = false;

            // 
            // pnlFilters
            // 
            this.pnlFilters.BackColor = System.Drawing.Color.White;
            this.pnlFilters.Controls.Add(this.btnViewTable);
            this.pnlFilters.Controls.Add(this.btnViewCharts);
            this.pnlFilters.Controls.Add(this.btnViewBoth);
            this.pnlFilters.Controls.Add(this.lblReportType);
            this.pnlFilters.Controls.Add(this.cboReportType);
            this.pnlFilters.Controls.Add(this.lblDateRange);
            this.pnlFilters.Controls.Add(this.cboDateRange);
            this.pnlFilters.Controls.Add(this.dtpStart);
            this.pnlFilters.Controls.Add(this.lblTo);
            this.pnlFilters.Controls.Add(this.dtpEnd);
            this.pnlFilters.Controls.Add(this.lblAgentFilter);
            this.pnlFilters.Controls.Add(this.cboAgentFilter);
            this.pnlFilters.Controls.Add(this.lblSecondaryFilter);
            this.pnlFilters.Controls.Add(this.cboSecondaryFilter);
            this.pnlFilters.Controls.Add(this.btnRunReport);
            this.pnlFilters.Controls.Add(this.btnExportCsv);
            this.pnlFilters.Controls.Add(this.btnExportPdf);
            this.pnlFilters.Dock = System.Windows.Forms.DockStyle.Top;
            this.pnlFilters.Location = new System.Drawing.Point(0, 72);
            this.pnlFilters.Name = "pnlFilters";
            this.pnlFilters.Padding = new System.Windows.Forms.Padding(20);
            this.pnlFilters.Size = new System.Drawing.Size(1100, 115);
            this.pnlFilters.TabIndex = 1;

            // 
            // lblReportType
            // 
            this.lblReportType.AutoSize = true;
            this.lblReportType.Font = new System.Drawing.Font("Segoe UI Semibold", 8.5F, System.Drawing.FontStyle.Bold);
            this.lblReportType.ForeColor = System.Drawing.Color.FromArgb(71, 85, 105);
            this.lblReportType.Location = new System.Drawing.Point(20, 10);
            this.lblReportType.Name = "lblReportType";
            this.lblReportType.Size = new System.Drawing.Size(70, 15);
            this.lblReportType.TabIndex = 0;
            this.lblReportType.Text = "Report Type";

            // 
            // cboReportType
            // 
            this.cboReportType.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cboReportType.FormattingEnabled = true;
            this.cboReportType.Location = new System.Drawing.Point(20, 30);
            this.cboReportType.Name = "cboReportType";
            this.cboReportType.Size = new System.Drawing.Size(220, 23);
            this.cboReportType.TabIndex = 1;

            // 
            // lblDateRange
            // 
            this.lblDateRange.AutoSize = true;
            this.lblDateRange.Font = new System.Drawing.Font("Segoe UI Semibold", 8.5F, System.Drawing.FontStyle.Bold);
            this.lblDateRange.ForeColor = System.Drawing.Color.FromArgb(71, 85, 105);
            this.lblDateRange.Location = new System.Drawing.Point(250, 10);
            this.lblDateRange.Name = "lblDateRange";
            this.lblDateRange.Size = new System.Drawing.Size(68, 15);
            this.lblDateRange.TabIndex = 2;
            this.lblDateRange.Text = "Date Range";

            // 
            // cboDateRange
            // 
            this.cboDateRange.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cboDateRange.FormattingEnabled = true;
            this.cboDateRange.Location = new System.Drawing.Point(250, 30);
            this.cboDateRange.Name = "cboDateRange";
            this.cboDateRange.Size = new System.Drawing.Size(140, 23);
            this.cboDateRange.TabIndex = 3;

            // 
            // dtpStart
            // 
            this.dtpStart.Format = System.Windows.Forms.DateTimePickerFormat.Short;
            this.dtpStart.Location = new System.Drawing.Point(400, 30);
            this.dtpStart.Name = "dtpStart";
            this.dtpStart.Size = new System.Drawing.Size(95, 23);
            this.dtpStart.TabIndex = 4;
            this.dtpStart.Visible = false;

            // 
            // lblTo
            // 
            this.lblTo.AutoSize = true;
            this.lblTo.Location = new System.Drawing.Point(500, 34);
            this.lblTo.Name = "lblTo";
            this.lblTo.Size = new System.Drawing.Size(18, 15);
            this.lblTo.TabIndex = 5;
            this.lblTo.Text = "to";
            this.lblTo.Visible = false;

            // 
            // dtpEnd
            // 
            this.dtpEnd.Format = System.Windows.Forms.DateTimePickerFormat.Short;
            this.dtpEnd.Location = new System.Drawing.Point(525, 30);
            this.dtpEnd.Name = "dtpEnd";
            this.dtpEnd.Size = new System.Drawing.Size(95, 23);
            this.dtpEnd.TabIndex = 6;
            this.dtpEnd.Visible = false;

            // 
            // lblAgentFilter
            // 
            this.lblAgentFilter.AutoSize = true;
            this.lblAgentFilter.Font = new System.Drawing.Font("Segoe UI Semibold", 8.5F, System.Drawing.FontStyle.Bold);
            this.lblAgentFilter.ForeColor = System.Drawing.Color.FromArgb(71, 85, 105);
            this.lblAgentFilter.Location = new System.Drawing.Point(635, 10);
            this.lblAgentFilter.Name = "lblAgentFilter";
            this.lblAgentFilter.Size = new System.Drawing.Size(39, 15);
            this.lblAgentFilter.TabIndex = 7;
            this.lblAgentFilter.Text = "Agent";

            // 
            // cboAgentFilter
            // 
            this.cboAgentFilter.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cboAgentFilter.FormattingEnabled = true;
            this.cboAgentFilter.Location = new System.Drawing.Point(635, 30);
            this.cboAgentFilter.Name = "cboAgentFilter";
            this.cboAgentFilter.Size = new System.Drawing.Size(160, 23);
            this.cboAgentFilter.TabIndex = 8;

            // 
            // lblSecondaryFilter
            // 
            this.lblSecondaryFilter.AutoSize = true;
            this.lblSecondaryFilter.Font = new System.Drawing.Font("Segoe UI Semibold", 8.5F, System.Drawing.FontStyle.Bold);
            this.lblSecondaryFilter.ForeColor = System.Drawing.Color.FromArgb(71, 85, 105);
            this.lblSecondaryFilter.Location = new System.Drawing.Point(810, 10);
            this.lblSecondaryFilter.Name = "lblSecondaryFilter";
            this.lblSecondaryFilter.Size = new System.Drawing.Size(32, 15);
            this.lblSecondaryFilter.TabIndex = 9;
            this.lblSecondaryFilter.Text = "Filter";

            // 
            // cboSecondaryFilter
            // 
            this.cboSecondaryFilter.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cboSecondaryFilter.FormattingEnabled = true;
            this.cboSecondaryFilter.Location = new System.Drawing.Point(810, 30);
            this.cboSecondaryFilter.Name = "cboSecondaryFilter";
            this.cboSecondaryFilter.Size = new System.Drawing.Size(140, 23);
            this.cboSecondaryFilter.TabIndex = 10;

            // 
            // btnRunReport
            // 
            this.btnRunReport.BackColor = System.Drawing.Color.FromArgb(37, 103, 156);
            this.btnRunReport.FlatAppearance.BorderSize = 0;
            this.btnRunReport.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnRunReport.Font = new System.Drawing.Font("Segoe UI Semibold", 9F, System.Drawing.FontStyle.Bold);
            this.btnRunReport.ForeColor = System.Drawing.Color.White;
            this.btnRunReport.Location = new System.Drawing.Point(20, 72);
            this.btnRunReport.Name = "btnRunReport";
            this.btnRunReport.Size = new System.Drawing.Size(130, 32);
            this.btnRunReport.TabIndex = 11;
            this.btnRunReport.Text = "▶ Run Report";
            this.btnRunReport.UseVisualStyleBackColor = false;

            // 
            // btnExportCsv
            // 
            this.btnExportCsv.BackColor = System.Drawing.Color.FromArgb(241, 245, 249);
            this.btnExportCsv.FlatAppearance.BorderSize = 1;
            this.btnExportCsv.FlatAppearance.BorderColor = System.Drawing.Color.FromArgb(203, 213, 225);
            this.btnExportCsv.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnExportCsv.Font = new System.Drawing.Font("Segoe UI Semibold", 9F);
            this.btnExportCsv.ForeColor = System.Drawing.Color.FromArgb(15, 23, 42);
            this.btnExportCsv.Location = new System.Drawing.Point(160, 72);
            this.btnExportCsv.Name = "btnExportCsv";
            this.btnExportCsv.Size = new System.Drawing.Size(110, 32);
            this.btnExportCsv.TabIndex = 12;
            this.btnExportCsv.Text = "📥 Export CSV";
            this.btnExportCsv.UseVisualStyleBackColor = false;

            // 
            // btnExportPdf
            // 
            this.btnExportPdf.BackColor = System.Drawing.Color.FromArgb(241, 245, 249);
            this.btnExportPdf.FlatAppearance.BorderSize = 1;
            this.btnExportPdf.FlatAppearance.BorderColor = System.Drawing.Color.FromArgb(203, 213, 225);
            this.btnExportPdf.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnExportPdf.Font = new System.Drawing.Font("Segoe UI Semibold", 9F);
            this.btnExportPdf.ForeColor = System.Drawing.Color.FromArgb(15, 23, 42);
            this.btnExportPdf.Location = new System.Drawing.Point(280, 72);
            this.btnExportPdf.Name = "btnExportPdf";
            this.btnExportPdf.Size = new System.Drawing.Size(110, 32);
            this.btnExportPdf.TabIndex = 13;
            this.btnExportPdf.Text = "📄 Export PDF";
            this.btnExportPdf.UseVisualStyleBackColor = false;

            // 
            // btnViewBoth
            // 
            this.btnViewBoth.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.btnViewBoth.BackColor = System.Drawing.Color.FromArgb(231, 238, 244);
            this.btnViewBoth.FlatAppearance.BorderSize = 0;
            this.btnViewBoth.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnViewBoth.Font = new System.Drawing.Font("Segoe UI Semibold", 8.5F, System.Drawing.FontStyle.Bold);
            this.btnViewBoth.ForeColor = System.Drawing.Color.FromArgb(37, 103, 156);
            this.btnViewBoth.Location = new System.Drawing.Point(740, 72);
            this.btnViewBoth.Name = "btnViewBoth";
            this.btnViewBoth.Size = new System.Drawing.Size(115, 30);
            this.btnViewBoth.TabIndex = 14;
            this.btnViewBoth.Text = "📊 Chart & Table";
            this.btnViewBoth.UseVisualStyleBackColor = false;

            // 
            // btnViewCharts
            // 
            this.btnViewCharts.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.btnViewCharts.BackColor = System.Drawing.Color.White;
            this.btnViewCharts.FlatAppearance.BorderSize = 1;
            this.btnViewCharts.FlatAppearance.BorderColor = System.Drawing.Color.FromArgb(203, 213, 225);
            this.btnViewCharts.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnViewCharts.Font = new System.Drawing.Font("Segoe UI", 8.5F);
            this.btnViewCharts.ForeColor = System.Drawing.Color.FromArgb(71, 85, 105);
            this.btnViewCharts.Location = new System.Drawing.Point(860, 72);
            this.btnViewCharts.Name = "btnViewCharts";
            this.btnViewCharts.Size = new System.Drawing.Size(100, 30);
            this.btnViewCharts.TabIndex = 15;
            this.btnViewCharts.Text = "📊 Chart Only";
            this.btnViewCharts.UseVisualStyleBackColor = false;

            // 
            // btnViewTable
            // 
            this.btnViewTable.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.btnViewTable.BackColor = System.Drawing.Color.White;
            this.btnViewTable.FlatAppearance.BorderSize = 1;
            this.btnViewTable.FlatAppearance.BorderColor = System.Drawing.Color.FromArgb(203, 213, 225);
            this.btnViewTable.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnViewTable.Font = new System.Drawing.Font("Segoe UI", 8.5F);
            this.btnViewTable.ForeColor = System.Drawing.Color.FromArgb(71, 85, 105);
            this.btnViewTable.Location = new System.Drawing.Point(965, 72);
            this.btnViewTable.Name = "btnViewTable";
            this.btnViewTable.Size = new System.Drawing.Size(95, 30);
            this.btnViewTable.TabIndex = 16;
            this.btnViewTable.Text = "📋 Table Only";
            this.btnViewTable.UseVisualStyleBackColor = false;

            // 
            // pnlKpiContainer
            // 
            this.pnlKpiContainer.BackColor = System.Drawing.Color.FromArgb(243, 247, 250);
            this.pnlKpiContainer.ColumnCount = 4;
            this.pnlKpiContainer.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 25F));
            this.pnlKpiContainer.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 25F));
            this.pnlKpiContainer.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 25F));
            this.pnlKpiContainer.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 25F));
            this.pnlKpiContainer.Controls.Add(this.kpi1, 0, 0);
            this.pnlKpiContainer.Controls.Add(this.kpi2, 1, 0);
            this.pnlKpiContainer.Controls.Add(this.kpi3, 2, 0);
            this.pnlKpiContainer.Controls.Add(this.kpi4, 3, 0);
            this.pnlKpiContainer.Dock = System.Windows.Forms.DockStyle.Top;
            this.pnlKpiContainer.Location = new System.Drawing.Point(0, 187);
            this.pnlKpiContainer.Name = "pnlKpiContainer";
            this.pnlKpiContainer.Padding = new System.Windows.Forms.Padding(20, 4, 20, 8);
            this.pnlKpiContainer.RowCount = 1;
            this.pnlKpiContainer.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.pnlKpiContainer.Size = new System.Drawing.Size(1100, 114);
            this.pnlKpiContainer.TabIndex = 2;
            // 
            // kpi1
            // 
            this.kpi1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.kpi1.Margin = new System.Windows.Forms.Padding(0, 0, 10, 0);
            this.kpi1.Name = "kpi1";
            // 
            // kpi2
            // 
            this.kpi2.Dock = System.Windows.Forms.DockStyle.Fill;
            this.kpi2.Margin = new System.Windows.Forms.Padding(0, 0, 10, 0);
            this.kpi2.Name = "kpi2";
            // 
            // kpi3
            // 
            this.kpi3.Dock = System.Windows.Forms.DockStyle.Fill;
            this.kpi3.Margin = new System.Windows.Forms.Padding(0, 0, 10, 0);
            this.kpi3.Name = "kpi3";
            // 
            // kpi4
            // 
            this.kpi4.Dock = System.Windows.Forms.DockStyle.Fill;
            this.kpi4.Margin = new System.Windows.Forms.Padding(0);
            this.kpi4.Name = "kpi4";

            // 
            // pnlCharts
            // 
            this.pnlCharts.BackColor = System.Drawing.Color.FromArgb(243, 247, 250);
            this.pnlCharts.Controls.Add(this.pnlChartCard2);
            this.pnlCharts.Controls.Add(this.pnlChartCard1);
            this.pnlCharts.Dock = System.Windows.Forms.DockStyle.Top;
            this.pnlCharts.Location = new System.Drawing.Point(0, 187);
            this.pnlCharts.Name = "pnlCharts";
            this.pnlCharts.Padding = new System.Windows.Forms.Padding(20, 10, 20, 10);
            this.pnlCharts.Size = new System.Drawing.Size(1100, 280);
            this.pnlCharts.TabIndex = 2;

            // 
            // pnlChartCard1
            // 
            this.pnlChartCard1.BackColor = System.Drawing.Color.White;
            this.pnlChartCard1.Controls.Add(this.lblChart1Title);
            this.pnlChartCard1.Controls.Add(this.plotReport1);
            this.pnlChartCard1.Location = new System.Drawing.Point(20, 10);
            this.pnlChartCard1.Name = "pnlChartCard1";
            this.pnlChartCard1.Padding = new System.Windows.Forms.Padding(12);
            this.pnlChartCard1.Size = new System.Drawing.Size(515, 260);
            this.pnlChartCard1.TabIndex = 0;

            // 
            // lblChart1Title
            // 
            this.lblChart1Title.AutoSize = true;
            this.lblChart1Title.Font = new System.Drawing.Font("Segoe UI Semibold", 10.5F, System.Drawing.FontStyle.Bold);
            this.lblChart1Title.ForeColor = System.Drawing.Color.FromArgb(15, 23, 42);
            this.lblChart1Title.Location = new System.Drawing.Point(12, 10);
            this.lblChart1Title.Name = "lblChart1Title";
            this.lblChart1Title.Size = new System.Drawing.Size(160, 19);
            this.lblChart1Title.TabIndex = 0;
            this.lblChart1Title.Text = "Primary Analysis Graph";

            // 
            // plotReport1
            // 
            this.plotReport1.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.plotReport1.Location = new System.Drawing.Point(10, 35);
            this.plotReport1.Name = "plotReport1";
            this.plotReport1.Size = new System.Drawing.Size(495, 215);
            this.plotReport1.TabIndex = 1;

            // 
            // pnlChartCard2
            // 
            this.pnlChartCard2.BackColor = System.Drawing.Color.White;
            this.pnlChartCard2.Controls.Add(this.lblChart2Title);
            this.pnlChartCard2.Controls.Add(this.plotReport2);
            this.pnlChartCard2.Location = new System.Drawing.Point(550, 10);
            this.pnlChartCard2.Name = "pnlChartCard2";
            this.pnlChartCard2.Padding = new System.Windows.Forms.Padding(12);
            this.pnlChartCard2.Size = new System.Drawing.Size(515, 260);
            this.pnlChartCard2.TabIndex = 1;

            // 
            // lblChart2Title
            // 
            this.lblChart2Title.AutoSize = true;
            this.lblChart2Title.Font = new System.Drawing.Font("Segoe UI Semibold", 10.5F, System.Drawing.FontStyle.Bold);
            this.lblChart2Title.ForeColor = System.Drawing.Color.FromArgb(15, 23, 42);
            this.lblChart2Title.Location = new System.Drawing.Point(12, 10);
            this.lblChart2Title.Name = "lblChart2Title";
            this.lblChart2Title.Size = new System.Drawing.Size(180, 19);
            this.lblChart2Title.TabIndex = 0;
            this.lblChart2Title.Text = "Distribution & Breakdown";

            // 
            // plotReport2
            // 
            this.plotReport2.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.plotReport2.Location = new System.Drawing.Point(10, 35);
            this.plotReport2.Name = "plotReport2";
            this.plotReport2.Size = new System.Drawing.Size(495, 215);
            this.plotReport2.TabIndex = 1;

            // 
            // pnlGrid
            // 
            this.pnlGrid.BackColor = System.Drawing.Color.FromArgb(243, 247, 250);
            this.pnlGrid.Controls.Add(this.lblLoading);
            this.pnlGrid.Controls.Add(this.gridData);
            this.pnlGrid.Controls.Add(this.lblReportHeader);
            this.pnlGrid.Dock = System.Windows.Forms.DockStyle.Fill;
            this.pnlGrid.Location = new System.Drawing.Point(0, 467);
            this.pnlGrid.Name = "pnlGrid";
            this.pnlGrid.Padding = new System.Windows.Forms.Padding(20, 0, 20, 15);
            this.pnlGrid.Size = new System.Drawing.Size(1100, 333);
            this.pnlGrid.TabIndex = 3;

            // 
            // lblReportHeader
            // 
            this.lblReportHeader.Dock = System.Windows.Forms.DockStyle.Top;
            this.lblReportHeader.Font = new System.Drawing.Font("Segoe UI", 9.5F, System.Drawing.FontStyle.Italic);
            this.lblReportHeader.ForeColor = System.Drawing.Color.FromArgb(100, 116, 139);
            this.lblReportHeader.Location = new System.Drawing.Point(20, 0);
            this.lblReportHeader.Name = "lblReportHeader";
            this.lblReportHeader.Size = new System.Drawing.Size(1060, 28);
            this.lblReportHeader.TabIndex = 0;
            this.lblReportHeader.Text = "Generate a report to see details here.";
            this.lblReportHeader.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;

            // 
            // gridData
            // 
            this.gridData.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            this.gridData.Dock = System.Windows.Forms.DockStyle.Fill;
            this.gridData.Location = new System.Drawing.Point(20, 28);
            this.gridData.Name = "gridData";
            this.gridData.ReadOnly = true;
            this.gridData.Size = new System.Drawing.Size(1060, 290);
            this.gridData.TabIndex = 1;

            // 
            // lblLoading
            // 
            this.lblLoading.Anchor = System.Windows.Forms.AnchorStyles.None;
            this.lblLoading.AutoSize = true;
            this.lblLoading.BackColor = System.Drawing.Color.White;
            this.lblLoading.Font = new System.Drawing.Font("Segoe UI Semibold", 12F, System.Drawing.FontStyle.Bold);
            this.lblLoading.ForeColor = System.Drawing.Color.FromArgb(37, 103, 156);
            this.lblLoading.Location = new System.Drawing.Point(460, 140);
            this.lblLoading.Name = "lblLoading";
            this.lblLoading.Padding = new System.Windows.Forms.Padding(20, 10, 20, 10);
            this.lblLoading.Size = new System.Drawing.Size(175, 41);
            this.lblLoading.TabIndex = 2;
            this.lblLoading.Text = "Loading Report...";
            this.lblLoading.Visible = false;

            // 
            // lblAccessDenied
            // 
            this.lblAccessDenied.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblAccessDenied.Font = new System.Drawing.Font("Segoe UI", 14F);
            this.lblAccessDenied.ForeColor = System.Drawing.Color.Red;
            this.lblAccessDenied.Location = new System.Drawing.Point(0, 0);
            this.lblAccessDenied.Name = "lblAccessDenied";
            this.lblAccessDenied.Size = new System.Drawing.Size(1100, 800);
            this.lblAccessDenied.TabIndex = 4;
            this.lblAccessDenied.Text = "Access Denied: Reports are restricted to Admin and Manager roles.";
            this.lblAccessDenied.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            this.lblAccessDenied.Visible = false;

            // 
            // pnlKpiContainer
            // 
            this.pnlKpiContainer.BackColor = System.Drawing.Color.FromArgb(243, 247, 250);
            this.pnlKpiContainer.ColumnCount = 4;
            this.pnlKpiContainer.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 25F));
            this.pnlKpiContainer.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 25F));
            this.pnlKpiContainer.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 25F));
            this.pnlKpiContainer.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 25F));
            this.pnlKpiContainer.Controls.Add(this.kpi1, 0, 0);
            this.pnlKpiContainer.Controls.Add(this.kpi2, 1, 0);
            this.pnlKpiContainer.Controls.Add(this.kpi3, 2, 0);
            this.pnlKpiContainer.Controls.Add(this.kpi4, 3, 0);
            this.pnlKpiContainer.Dock = System.Windows.Forms.DockStyle.Top;
            this.pnlKpiContainer.Height = 114;
            this.pnlKpiContainer.Location = new System.Drawing.Point(0, 187);
            this.pnlKpiContainer.Name = "pnlKpiContainer";
            this.pnlKpiContainer.Padding = new System.Windows.Forms.Padding(20, 6, 20, 4);
            this.pnlKpiContainer.RowCount = 1;
            this.pnlKpiContainer.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.pnlKpiContainer.TabIndex = 5;

            // 
            // kpi1
            // 
            this.kpi1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.kpi1.Margin = new System.Windows.Forms.Padding(0, 0, 8, 0);
            this.kpi1.Name = "kpi1";

            // 
            // kpi2
            // 
            this.kpi2.Dock = System.Windows.Forms.DockStyle.Fill;
            this.kpi2.Margin = new System.Windows.Forms.Padding(4, 0, 6, 0);
            this.kpi2.Name = "kpi2";

            // 
            // kpi3
            // 
            this.kpi3.Dock = System.Windows.Forms.DockStyle.Fill;
            this.kpi3.Margin = new System.Windows.Forms.Padding(6, 0, 4, 0);
            this.kpi3.Name = "kpi3";

            // 
            // kpi4
            // 
            this.kpi4.Dock = System.Windows.Forms.DockStyle.Fill;
            this.kpi4.Margin = new System.Windows.Forms.Padding(8, 0, 0, 0);
            this.kpi4.Name = "kpi4";

            // 
            // pnlScrollableContent
            // 
            this.pnlScrollableContent.AutoScroll = true;
            this.pnlScrollableContent.BackColor = System.Drawing.Color.FromArgb(243, 247, 250);
            this.pnlScrollableContent.Controls.Add(this.pnlGrid);
            this.pnlScrollableContent.Controls.Add(this.pnlCharts);
            this.pnlScrollableContent.Dock = System.Windows.Forms.DockStyle.Fill;
            this.pnlScrollableContent.Location = new System.Drawing.Point(0, 301);
            this.pnlScrollableContent.Name = "pnlScrollableContent";
            this.pnlScrollableContent.Size = new System.Drawing.Size(1100, 499);
            this.pnlScrollableContent.TabIndex = 2;

            // 
            // ReportsView
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 15F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.BackColor = System.Drawing.Color.FromArgb(243, 247, 250);
            this.Controls.Add(this.pnlScrollableContent);
            this.Controls.Add(this.pnlKpiContainer);
            this.Controls.Add(this.pnlFilters);
            this.Controls.Add(this.pnlTop);
            this.Controls.Add(this.lblAccessDenied);
            this.Font = new System.Drawing.Font("Segoe UI", 9F);
            this.Name = "ReportsView";
            this.Size = new System.Drawing.Size(1100, 800);

            this.pnlTop.ResumeLayout(false);
            this.pnlTop.PerformLayout();
            this.pnlFilters.ResumeLayout(false);
            this.pnlFilters.PerformLayout();
            this.pnlKpiContainer.ResumeLayout(false);
            this.pnlScrollableContent.ResumeLayout(false);
            this.pnlCharts.ResumeLayout(false);
            this.pnlChartCard1.ResumeLayout(false);
            this.pnlChartCard1.PerformLayout();
            this.pnlChartCard2.ResumeLayout(false);
            this.pnlChartCard2.PerformLayout();
            this.pnlGrid.ResumeLayout(false);
            this.pnlGrid.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.gridData)).EndInit();
            this.ResumeLayout(false);
        }

        private System.Windows.Forms.Panel pnlTop;
        private System.Windows.Forms.Label lblTitle;
        private System.Windows.Forms.Label lblSubtitle;
        private System.Windows.Forms.Button btnGoToAnalytics;

        private System.Windows.Forms.Panel pnlFilters;
        private System.Windows.Forms.Label lblReportType;
        private System.Windows.Forms.ComboBox cboReportType;
        private System.Windows.Forms.Label lblDateRange;
        private System.Windows.Forms.ComboBox cboDateRange;
        private System.Windows.Forms.DateTimePicker dtpStart;
        private System.Windows.Forms.Label lblTo;
        private System.Windows.Forms.DateTimePicker dtpEnd;
        private System.Windows.Forms.Label lblAgentFilter;
        private System.Windows.Forms.ComboBox cboAgentFilter;
        private System.Windows.Forms.Label lblSecondaryFilter;
        private System.Windows.Forms.ComboBox cboSecondaryFilter;
        private System.Windows.Forms.Button btnRunReport;
        private System.Windows.Forms.Button btnExportCsv;
        private System.Windows.Forms.Button btnExportPdf;

        private System.Windows.Forms.Button btnViewBoth;
        private System.Windows.Forms.Button btnViewCharts;
        private System.Windows.Forms.Button btnViewTable;

        private System.Windows.Forms.Panel pnlScrollableContent;
        private System.Windows.Forms.Panel pnlCharts;
        private System.Windows.Forms.Panel pnlChartCard1;
        private System.Windows.Forms.Label lblChart1Title;
        private ScottPlot.WinForms.FormsPlot plotReport1;

        private System.Windows.Forms.Panel pnlChartCard2;
        private System.Windows.Forms.Label lblChart2Title;
        private ScottPlot.WinForms.FormsPlot plotReport2;

        private System.Windows.Forms.Panel pnlGrid;
        private System.Windows.Forms.Label lblReportHeader;
        private System.Windows.Forms.DataGridView gridData;
        private System.Windows.Forms.Label lblLoading;
        private System.Windows.Forms.Label lblAccessDenied;

        private System.Windows.Forms.TableLayoutPanel pnlKpiContainer;
        private CRMS_Peguit.winforms.Controls.KpiCard kpi1;
        private CRMS_Peguit.winforms.Controls.KpiCard kpi2;
        private CRMS_Peguit.winforms.Controls.KpiCard kpi3;
        private CRMS_Peguit.winforms.Controls.KpiCard kpi4;
    }
}
