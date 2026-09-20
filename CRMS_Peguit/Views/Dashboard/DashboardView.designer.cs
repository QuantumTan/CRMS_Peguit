namespace CRMS_Peguit.winforms.Views.Dashboard
{
    partial class DashboardView
    {
        private System.ComponentModel.IContainer components = null;

        private System.Windows.Forms.Label lblTitle = null!;
        private System.Windows.Forms.Label lblSubtitle = null!;
        private System.Windows.Forms.FlowLayoutPanel pnlQuickActions = null!;

        private System.Windows.Forms.TableLayoutPanel pnlKpiContainer = null!;
        private CRMS_Peguit.winforms.Controls.KpiCard kpi1 = null!;
        private CRMS_Peguit.winforms.Controls.KpiCard kpi2 = null!;
        private CRMS_Peguit.winforms.Controls.KpiCard kpi3 = null!;
        private CRMS_Peguit.winforms.Controls.KpiCard kpi4 = null!;

        private System.Windows.Forms.TableLayoutPanel pnlContentSplit = null!;

        private System.Windows.Forms.Panel pnlLeftCard = null!;
        private System.Windows.Forms.Label lblLeftTitle = null!;
        private System.Windows.Forms.Label lblLeftSubtitle = null!;
        private System.Windows.Forms.Panel pnlLeftList = null!;
        private System.Windows.Forms.Label lblLeftEmpty = null!;

        private System.Windows.Forms.Panel pnlRightCard = null!;
        private System.Windows.Forms.Label lblRightTitle = null!;
        private System.Windows.Forms.Label lblRightSubtitle = null!;
        private System.Windows.Forms.Panel pnlRightList = null!;
        private System.Windows.Forms.Label lblRightEmpty = null!;

        private System.Windows.Forms.Panel pnlChartCard = null!;
        private System.Windows.Forms.Label lblChartTitle = null!;
        private System.Windows.Forms.Label lblChartSubtitle = null!;
        private ScottPlot.WinForms.FormsPlot plotGlanceable = null!;
        private System.Windows.Forms.Label lblChartFooter = null!;

        private System.Windows.Forms.Label lblLoading = null!;

        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Component Designer generated code

        private void InitializeComponent()
        {
            this.lblTitle = new System.Windows.Forms.Label();
            this.lblSubtitle = new System.Windows.Forms.Label();
            this.lblLoading = new System.Windows.Forms.Label();
            this.pnlQuickActions = new System.Windows.Forms.FlowLayoutPanel();

            this.pnlKpiContainer = new System.Windows.Forms.TableLayoutPanel();
            this.kpi1 = new CRMS_Peguit.winforms.Controls.KpiCard();
            this.kpi2 = new CRMS_Peguit.winforms.Controls.KpiCard();
            this.kpi3 = new CRMS_Peguit.winforms.Controls.KpiCard();
            this.kpi4 = new CRMS_Peguit.winforms.Controls.KpiCard();

            this.pnlContentSplit = new System.Windows.Forms.TableLayoutPanel();

            this.pnlLeftCard = new System.Windows.Forms.Panel();
            this.lblLeftTitle = new System.Windows.Forms.Label();
            this.lblLeftSubtitle = new System.Windows.Forms.Label();
            this.pnlLeftList = new System.Windows.Forms.Panel();
            this.lblLeftEmpty = new System.Windows.Forms.Label();

            this.pnlRightCard = new System.Windows.Forms.Panel();
            this.lblRightTitle = new System.Windows.Forms.Label();
            this.lblRightSubtitle = new System.Windows.Forms.Label();
            this.pnlRightList = new System.Windows.Forms.Panel();
            this.lblRightEmpty = new System.Windows.Forms.Label();

            this.pnlChartCard = new System.Windows.Forms.Panel();
            this.lblChartTitle = new System.Windows.Forms.Label();
            this.lblChartSubtitle = new System.Windows.Forms.Label();
            this.plotGlanceable = new ScottPlot.WinForms.FormsPlot();
            this.lblChartFooter = new System.Windows.Forms.Label();

            this.pnlKpiContainer.SuspendLayout();
            this.pnlContentSplit.SuspendLayout();
            this.pnlLeftCard.SuspendLayout();
            this.pnlRightCard.SuspendLayout();
            this.pnlChartCard.SuspendLayout();
            this.SuspendLayout();

            // 
            // lblTitle
            // 
            this.lblTitle.AutoSize = true;
            this.lblTitle.Font = new System.Drawing.Font("Segoe UI", 18F, System.Drawing.FontStyle.Bold);
            this.lblTitle.ForeColor = System.Drawing.Color.FromArgb(15, 23, 42);
            this.lblTitle.Location = new System.Drawing.Point(30, 20);
            this.lblTitle.Name = "lblTitle";
            this.lblTitle.Size = new System.Drawing.Size(200, 32);
            this.lblTitle.TabIndex = 0;
            this.lblTitle.Text = "Welcome Back";

            // 
            // lblSubtitle
            // 
            this.lblSubtitle.AutoSize = true;
            this.lblSubtitle.Font = new System.Drawing.Font("Segoe UI", 9.5F);
            this.lblSubtitle.ForeColor = System.Drawing.Color.FromArgb(100, 116, 139);
            this.lblSubtitle.Location = new System.Drawing.Point(32, 56);
            this.lblSubtitle.Name = "lblSubtitle";
            this.lblSubtitle.Size = new System.Drawing.Size(120, 17);
            this.lblSubtitle.TabIndex = 1;
            this.lblSubtitle.Text = "Today's Overview";
            // 
            // lblLoading
            // 
            this.lblLoading.AutoSize = true;
            this.lblLoading.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Italic);
            this.lblLoading.ForeColor = System.Drawing.Color.FromArgb(100, 116, 139);
            this.lblLoading.Location = new System.Drawing.Point(32, 56);
            this.lblLoading.Name = "lblLoading";
            this.lblLoading.Size = new System.Drawing.Size(150, 15);
            this.lblLoading.TabIndex = 4;
            this.lblLoading.Text = "Refreshing live snapshot...";
            this.lblLoading.Visible = false;

            // 
            // pnlQuickActions
            // 
            this.pnlQuickActions.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.pnlQuickActions.AutoSize = true;
            this.pnlQuickActions.FlowDirection = System.Windows.Forms.FlowDirection.RightToLeft;
            this.pnlQuickActions.Location = new System.Drawing.Point(620, 22);
            this.pnlQuickActions.Name = "pnlQuickActions";
            this.pnlQuickActions.Size = new System.Drawing.Size(380, 42);
            this.pnlQuickActions.TabIndex = 2;
            this.pnlQuickActions.WrapContents = false;

            // 
            // pnlKpiContainer
            // 
            this.pnlKpiContainer.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.pnlKpiContainer.ColumnCount = 4;
            this.pnlKpiContainer.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 25F));
            this.pnlKpiContainer.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 25F));
            this.pnlKpiContainer.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 25F));
            this.pnlKpiContainer.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 25F));
            this.pnlKpiContainer.Controls.Add(this.kpi1, 0, 0);
            this.pnlKpiContainer.Controls.Add(this.kpi2, 1, 0);
            this.pnlKpiContainer.Controls.Add(this.kpi3, 2, 0);
            this.pnlKpiContainer.Controls.Add(this.kpi4, 3, 0);
            this.pnlKpiContainer.Location = new System.Drawing.Point(30, 88);
            this.pnlKpiContainer.Name = "pnlKpiContainer";
            this.pnlKpiContainer.RowCount = 1;
            this.pnlKpiContainer.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.pnlKpiContainer.Size = new System.Drawing.Size(970, 104);
            this.pnlKpiContainer.TabIndex = 3;

            // 
            // kpi1
            // 
            this.kpi1.BackColor = System.Drawing.Color.White;
            this.kpi1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.kpi1.Margin = new System.Windows.Forms.Padding(0, 0, 8, 0);
            this.kpi1.Name = "kpi1";
            this.kpi1.Size = new System.Drawing.Size(234, 104);
            this.kpi1.TabIndex = 0;

            // 
            // kpi2
            // 
            this.kpi2.BackColor = System.Drawing.Color.White;
            this.kpi2.Dock = System.Windows.Forms.DockStyle.Fill;
            this.kpi2.Margin = new System.Windows.Forms.Padding(4, 0, 6, 0);
            this.kpi2.Name = "kpi2";
            this.kpi2.Size = new System.Drawing.Size(234, 104);
            this.kpi2.TabIndex = 1;

            // 
            // kpi3
            // 
            this.kpi3.BackColor = System.Drawing.Color.White;
            this.kpi3.Dock = System.Windows.Forms.DockStyle.Fill;
            this.kpi3.Margin = new System.Windows.Forms.Padding(6, 0, 4, 0);
            this.kpi3.Name = "kpi3";
            this.kpi3.Size = new System.Drawing.Size(234, 104);
            this.kpi3.TabIndex = 2;

            // 
            // kpi4
            // 
            this.kpi4.BackColor = System.Drawing.Color.White;
            this.kpi4.Dock = System.Windows.Forms.DockStyle.Fill;
            this.kpi4.Margin = new System.Windows.Forms.Padding(8, 0, 0, 0);
            this.kpi4.Name = "kpi4";
            this.kpi4.Size = new System.Drawing.Size(234, 104);
            this.kpi4.TabIndex = 3;

            // 
            // pnlContentSplit
            // 
            this.pnlContentSplit.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.pnlContentSplit.ColumnCount = 3;
            this.pnlContentSplit.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 38F));
            this.pnlContentSplit.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 34F));
            this.pnlContentSplit.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 28F));
            this.pnlContentSplit.Controls.Add(this.pnlLeftCard, 0, 0);
            this.pnlContentSplit.Controls.Add(this.pnlRightCard, 1, 0);
            this.pnlContentSplit.Controls.Add(this.pnlChartCard, 2, 0);
            this.pnlContentSplit.Location = new System.Drawing.Point(30, 206);
            this.pnlContentSplit.Name = "pnlContentSplit";
            this.pnlContentSplit.RowCount = 1;
            this.pnlContentSplit.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.pnlContentSplit.Size = new System.Drawing.Size(970, 470);
            this.pnlContentSplit.TabIndex = 4;

            // 
            // pnlLeftCard
            // 
            this.pnlLeftCard.BackColor = System.Drawing.Color.White;
            this.pnlLeftCard.Controls.Add(this.lblLeftEmpty);
            this.pnlLeftCard.Controls.Add(this.pnlLeftList);
            this.pnlLeftCard.Controls.Add(this.lblLeftSubtitle);
            this.pnlLeftCard.Controls.Add(this.lblLeftTitle);
            this.pnlLeftCard.Dock = System.Windows.Forms.DockStyle.Fill;
            this.pnlLeftCard.Margin = new System.Windows.Forms.Padding(0, 0, 8, 0);
            this.pnlLeftCard.Name = "pnlLeftCard";
            this.pnlLeftCard.Padding = new System.Windows.Forms.Padding(20);
            this.pnlLeftCard.Size = new System.Drawing.Size(496, 470);
            this.pnlLeftCard.TabIndex = 0;

            // 
            // lblLeftTitle
            // 
            this.lblLeftTitle.AutoSize = true;
            this.lblLeftTitle.Font = new System.Drawing.Font("Segoe UI", 12F, System.Drawing.FontStyle.Bold);
            this.lblLeftTitle.ForeColor = System.Drawing.Color.FromArgb(15, 23, 42);
            this.lblLeftTitle.Location = new System.Drawing.Point(20, 16);
            this.lblLeftTitle.Name = "lblLeftTitle";
            this.lblLeftTitle.Size = new System.Drawing.Size(120, 21);
            this.lblLeftTitle.TabIndex = 0;
            this.lblLeftTitle.Text = "Left Card Title";

            // 
            // lblLeftSubtitle
            // 
            this.lblLeftSubtitle.AutoSize = true;
            this.lblLeftSubtitle.Font = new System.Drawing.Font("Segoe UI", 8.5F);
            this.lblLeftSubtitle.ForeColor = System.Drawing.Color.FromArgb(100, 116, 139);
            this.lblLeftSubtitle.Location = new System.Drawing.Point(21, 40);
            this.lblLeftSubtitle.Name = "lblLeftSubtitle";
            this.lblLeftSubtitle.Size = new System.Drawing.Size(140, 15);
            this.lblLeftSubtitle.TabIndex = 1;
            this.lblLeftSubtitle.Text = "Subtitle or counter info";

            // 
            // pnlLeftList
            // 
            this.pnlLeftList.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.pnlLeftList.AutoScroll = true;
            this.pnlLeftList.Location = new System.Drawing.Point(20, 64);
            this.pnlLeftList.Name = "pnlLeftList";
            this.pnlLeftList.Size = new System.Drawing.Size(456, 386);
            this.pnlLeftList.TabIndex = 2;

            // 
            // lblLeftEmpty
            // 
            this.lblLeftEmpty.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.lblLeftEmpty.Font = new System.Drawing.Font("Segoe UI", 10F);
            this.lblLeftEmpty.ForeColor = System.Drawing.Color.FromArgb(100, 116, 139);
            this.lblLeftEmpty.Location = new System.Drawing.Point(20, 64);
            this.lblLeftEmpty.Name = "lblLeftEmpty";
            this.lblLeftEmpty.Size = new System.Drawing.Size(456, 386);
            this.lblLeftEmpty.TabIndex = 3;
            this.lblLeftEmpty.Text = "No items to display.";
            this.lblLeftEmpty.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            this.lblLeftEmpty.Visible = false;

            // 
            // pnlRightCard
            // 
            this.pnlRightCard.BackColor = System.Drawing.Color.White;
            this.pnlRightCard.Controls.Add(this.lblRightEmpty);
            this.pnlRightCard.Controls.Add(this.pnlRightList);
            this.pnlRightCard.Controls.Add(this.lblRightSubtitle);
            this.pnlRightCard.Controls.Add(this.lblRightTitle);
            this.pnlRightCard.Dock = System.Windows.Forms.DockStyle.Fill;
            this.pnlRightCard.Margin = new System.Windows.Forms.Padding(8, 0, 0, 0);
            this.pnlRightCard.Name = "pnlRightCard";
            this.pnlRightCard.Padding = new System.Windows.Forms.Padding(20);
            this.pnlRightCard.Size = new System.Drawing.Size(458, 470);
            this.pnlRightCard.TabIndex = 1;

            // 
            // lblRightTitle
            // 
            this.lblRightTitle.AutoSize = true;
            this.lblRightTitle.Font = new System.Drawing.Font("Segoe UI", 12F, System.Drawing.FontStyle.Bold);
            this.lblRightTitle.ForeColor = System.Drawing.Color.FromArgb(15, 23, 42);
            this.lblRightTitle.Location = new System.Drawing.Point(20, 16);
            this.lblRightTitle.Name = "lblRightTitle";
            this.lblRightTitle.Size = new System.Drawing.Size(130, 21);
            this.lblRightTitle.TabIndex = 0;
            this.lblRightTitle.Text = "Right Card Title";

            // 
            // lblRightSubtitle
            // 
            this.lblRightSubtitle.AutoSize = true;
            this.lblRightSubtitle.Font = new System.Drawing.Font("Segoe UI", 8.5F);
            this.lblRightSubtitle.ForeColor = System.Drawing.Color.FromArgb(100, 116, 139);
            this.lblRightSubtitle.Location = new System.Drawing.Point(21, 40);
            this.lblRightSubtitle.Name = "lblRightSubtitle";
            this.lblRightSubtitle.Size = new System.Drawing.Size(140, 15);
            this.lblRightSubtitle.TabIndex = 1;
            this.lblRightSubtitle.Text = "Subtitle or counter info";

            // 
            // pnlRightList
            // 
            this.pnlRightList.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.pnlRightList.AutoScroll = true;
            this.pnlRightList.Location = new System.Drawing.Point(20, 64);
            this.pnlRightList.Name = "pnlRightList";
            this.pnlRightList.Size = new System.Drawing.Size(418, 386);
            this.pnlRightList.TabIndex = 2;

            // 
            // lblRightEmpty
            // 
            this.lblRightEmpty.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.lblRightEmpty.Font = new System.Drawing.Font("Segoe UI", 10F);
            this.lblRightEmpty.ForeColor = System.Drawing.Color.FromArgb(100, 116, 139);
            this.lblRightEmpty.Location = new System.Drawing.Point(20, 64);
            this.lblRightEmpty.Name = "lblRightEmpty";
            this.lblRightEmpty.Size = new System.Drawing.Size(418, 386);
            this.lblRightEmpty.TabIndex = 3;
            this.lblRightEmpty.Text = "No items to display.";
            this.lblRightEmpty.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            this.lblRightEmpty.Visible = false;

            // 
            // pnlChartCard
            // 
            this.pnlChartCard.BackColor = System.Drawing.Color.White;
            this.pnlChartCard.Controls.Add(this.lblChartFooter);
            this.pnlChartCard.Controls.Add(this.plotGlanceable);
            this.pnlChartCard.Controls.Add(this.lblChartSubtitle);
            this.pnlChartCard.Controls.Add(this.lblChartTitle);
            this.pnlChartCard.Dock = System.Windows.Forms.DockStyle.Fill;
            this.pnlChartCard.Margin = new System.Windows.Forms.Padding(8, 0, 0, 0);
            this.pnlChartCard.Name = "pnlChartCard";
            this.pnlChartCard.Padding = new System.Windows.Forms.Padding(16);
            this.pnlChartCard.Size = new System.Drawing.Size(270, 470);
            this.pnlChartCard.TabIndex = 2;

            // 
            // lblChartTitle
            // 
            this.lblChartTitle.AutoSize = true;
            this.lblChartTitle.Font = new System.Drawing.Font("Segoe UI", 12F, System.Drawing.FontStyle.Bold);
            this.lblChartTitle.ForeColor = System.Drawing.Color.FromArgb(15, 23, 42);
            this.lblChartTitle.Location = new System.Drawing.Point(16, 16);
            this.lblChartTitle.Name = "lblChartTitle";
            this.lblChartTitle.Size = new System.Drawing.Size(120, 21);
            this.lblChartTitle.TabIndex = 0;
            this.lblChartTitle.Text = "Glanceable Chart";

            // 
            // lblChartSubtitle
            // 
            this.lblChartSubtitle.AutoSize = true;
            this.lblChartSubtitle.Font = new System.Drawing.Font("Segoe UI", 8.5F);
            this.lblChartSubtitle.ForeColor = System.Drawing.Color.FromArgb(100, 116, 139);
            this.lblChartSubtitle.Location = new System.Drawing.Point(17, 40);
            this.lblChartSubtitle.Name = "lblChartSubtitle";
            this.lblChartSubtitle.Size = new System.Drawing.Size(140, 15);
            this.lblChartSubtitle.TabIndex = 1;
            this.lblChartSubtitle.Text = "Trend & overview";

            // 
            // plotGlanceable
            // 
            this.plotGlanceable.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.plotGlanceable.Location = new System.Drawing.Point(12, 64);
            this.plotGlanceable.Name = "plotGlanceable";
            this.plotGlanceable.Size = new System.Drawing.Size(246, 360);
            this.plotGlanceable.TabIndex = 2;

            // 
            // lblChartFooter
            // 
            this.lblChartFooter.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.lblChartFooter.Font = new System.Drawing.Font("Segoe UI Semibold", 8.5F);
            this.lblChartFooter.ForeColor = System.Drawing.Color.FromArgb(37, 103, 156);
            this.lblChartFooter.Location = new System.Drawing.Point(16, 432);
            this.lblChartFooter.Name = "lblChartFooter";
            this.lblChartFooter.Size = new System.Drawing.Size(240, 22);
            this.lblChartFooter.TabIndex = 3;
            this.lblChartFooter.Text = "Click for full analytics →";
            this.lblChartFooter.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            this.lblChartFooter.Cursor = System.Windows.Forms.Cursors.Hand;

            // 
            // DashboardView
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 15F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.BackColor = System.Drawing.Color.FromArgb(244, 247, 251);
            this.Controls.Add(this.pnlContentSplit);
            this.Controls.Add(this.pnlKpiContainer);
            this.Controls.Add(this.pnlQuickActions);
            this.Controls.Add(this.lblLoading);
            this.Controls.Add(this.lblSubtitle);
            this.Controls.Add(this.lblTitle);
            this.Name = "DashboardView";
            this.Size = new System.Drawing.Size(1030, 700);
            this.pnlKpiContainer.ResumeLayout(false);
            this.pnlContentSplit.ResumeLayout(false);
            this.pnlLeftCard.ResumeLayout(false);
            this.pnlLeftCard.PerformLayout();
            this.pnlRightCard.ResumeLayout(false);
            this.pnlRightCard.PerformLayout();
            this.pnlChartCard.ResumeLayout(false);
            this.pnlChartCard.PerformLayout();
            this.ResumeLayout(false);
            this.PerformLayout();
        }

        #endregion
    }
}
