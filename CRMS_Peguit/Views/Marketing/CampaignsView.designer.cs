namespace CRMS_Peguit.winforms.Views.Marketing
{
    partial class CampaignsView
    {
        private System.ComponentModel.IContainer components = null;

        private System.Windows.Forms.Panel pnlHeader = null!;
        private System.Windows.Forms.Label lblTitle = null!;
        private System.Windows.Forms.Label lblSubtitle = null!;
        private System.Windows.Forms.Button btnAddCampaign = null!;
        private System.Windows.Forms.Button btnRefresh = null!;

        private System.Windows.Forms.Panel pnlStats = null!;
        private System.Windows.Forms.Label lblStatChannels = null!;
        private System.Windows.Forms.Label lblStatTotalLeads = null!;
        private System.Windows.Forms.Label lblStatConversion = null!;

        private System.Windows.Forms.FlowLayoutPanel pnlSourcePills = null!;
        private System.Windows.Forms.Panel pnlGridCard = null!;
        private System.Windows.Forms.DataGridView gridLeads = null!;

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
            this.pnlHeader = new System.Windows.Forms.Panel();
            this.lblTitle = new System.Windows.Forms.Label();
            this.lblSubtitle = new System.Windows.Forms.Label();
            this.btnAddCampaign = new System.Windows.Forms.Button();
            this.btnRefresh = new System.Windows.Forms.Button();
            this.pnlStats = new System.Windows.Forms.Panel();
            this.lblStatChannels = new System.Windows.Forms.Label();
            this.lblStatTotalLeads = new System.Windows.Forms.Label();
            this.lblStatConversion = new System.Windows.Forms.Label();
            this.pnlSourcePills = new System.Windows.Forms.FlowLayoutPanel();
            this.pnlGridCard = new System.Windows.Forms.Panel();
            this.gridLeads = new System.Windows.Forms.DataGridView();
            this.pnlHeader.SuspendLayout();
            this.pnlStats.SuspendLayout();
            this.pnlGridCard.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.gridLeads)).BeginInit();
            this.SuspendLayout();
            // 
            // pnlHeader
            // 
            this.pnlHeader.BackColor = System.Drawing.Color.White;
            this.pnlHeader.Controls.Add(this.lblTitle);
            this.pnlHeader.Controls.Add(this.lblSubtitle);
            this.pnlHeader.Controls.Add(this.btnAddCampaign);
            this.pnlHeader.Controls.Add(this.btnRefresh);
            this.pnlHeader.Dock = System.Windows.Forms.DockStyle.Top;
            this.pnlHeader.Location = new System.Drawing.Point(0, 0);
            this.pnlHeader.Name = "pnlHeader";
            this.pnlHeader.Padding = new System.Windows.Forms.Padding(28, 16, 28, 16);
            this.pnlHeader.Size = new System.Drawing.Size(1000, 84);
            this.pnlHeader.TabIndex = 0;
            // 
            // lblTitle
            // 
            this.lblTitle.AutoSize = true;
            this.lblTitle.Font = CRMS_Peguit.winforms.Services.UiStyleConstants.PageTitleFont;
            this.lblTitle.ForeColor = CRMS_Peguit.winforms.Models.Services.Theme.TextPrimary;
            this.lblTitle.Location = new System.Drawing.Point(30, 14);
            this.lblTitle.Name = "lblTitle";
            this.lblTitle.Size = new System.Drawing.Size(276, 32);
            this.lblTitle.TabIndex = 0;
            this.lblTitle.Text = "Marketing Automation";
            // 
            // lblSubtitle
            // 
            this.lblSubtitle.AutoSize = true;
            this.lblSubtitle.Font = CRMS_Peguit.winforms.Services.UiStyleConstants.SubtitleFont;
            this.lblSubtitle.ForeColor = CRMS_Peguit.winforms.Models.Services.Theme.TextSecondary;
            this.lblSubtitle.Location = new System.Drawing.Point(30, 48);
            this.lblSubtitle.Name = "lblSubtitle";
            this.lblSubtitle.Size = new System.Drawing.Size(326, 17);
            this.lblSubtitle.TabIndex = 1;
            this.lblSubtitle.Text = "Campaign source attribution & lead channel analysis";
            // 
            // btnAddCampaign
            // 
            this.btnAddCampaign.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.btnAddCampaign.BackColor = CRMS_Peguit.winforms.Models.Services.Theme.Primary;
            this.btnAddCampaign.Cursor = System.Windows.Forms.Cursors.Hand;
            this.btnAddCampaign.FlatAppearance.BorderSize = 0;
            this.btnAddCampaign.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnAddCampaign.Font = new System.Drawing.Font("Segoe UI", 9.5F, System.Drawing.FontStyle.Bold);
            this.btnAddCampaign.ForeColor = System.Drawing.Color.White;
            this.btnAddCampaign.Location = new System.Drawing.Point(710, 20);
            this.btnAddCampaign.Name = "btnAddCampaign";
            this.btnAddCampaign.Size = new System.Drawing.Size(150, 40);
            this.btnAddCampaign.TabIndex = 2;
            this.btnAddCampaign.Text = "+ New Campaign";
            this.btnAddCampaign.UseVisualStyleBackColor = false;
            // 
            // btnRefresh
            // 
            this.btnRefresh.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.btnRefresh.BackColor = System.Drawing.Color.White;
            this.btnRefresh.Cursor = System.Windows.Forms.Cursors.Hand;
            this.btnRefresh.FlatAppearance.BorderColor = System.Drawing.Color.FromArgb(226, 232, 240);
            this.btnRefresh.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnRefresh.Font = new System.Drawing.Font("Segoe UI", 9.5F);
            this.btnRefresh.ForeColor = System.Drawing.Color.FromArgb(71, 85, 105);
            this.btnRefresh.Location = new System.Drawing.Point(870, 20);
            this.btnRefresh.Name = "btnRefresh";
            this.btnRefresh.Size = new System.Drawing.Size(100, 40);
            this.btnRefresh.TabIndex = 3;
            this.btnRefresh.Text = "↻ Refresh";
            this.btnRefresh.UseVisualStyleBackColor = false;
            // 
            // pnlStats
            // 
            this.pnlStats.BackColor = System.Drawing.Color.White;
            this.pnlStats.Controls.Add(this.lblStatChannels);
            this.pnlStats.Controls.Add(this.lblStatTotalLeads);
            this.pnlStats.Controls.Add(this.lblStatConversion);
            this.pnlStats.Dock = System.Windows.Forms.DockStyle.Top;
            this.pnlStats.Location = new System.Drawing.Point(0, 84);
            this.pnlStats.Name = "pnlStats";
            this.pnlStats.Padding = new System.Windows.Forms.Padding(28, 8, 28, 8);
            this.pnlStats.Size = new System.Drawing.Size(1000, 64);
            this.pnlStats.TabIndex = 1;
            // 
            // lblStatChannels
            // 
            this.lblStatChannels.AutoSize = true;
            this.lblStatChannels.Font = new System.Drawing.Font("Segoe UI", 10F, System.Drawing.FontStyle.Bold);
            this.lblStatChannels.ForeColor = System.Drawing.Color.FromArgb(15, 91, 158);
            this.lblStatChannels.Location = new System.Drawing.Point(28, 18);
            this.lblStatChannels.Name = "lblStatChannels";
            this.lblStatChannels.Size = new System.Drawing.Size(167, 19);
            this.lblStatChannels.TabIndex = 0;
            this.lblStatChannels.Text = "0 Active Lead Channels";
            // 
            // lblStatTotalLeads
            // 
            this.lblStatTotalLeads.AutoSize = true;
            this.lblStatTotalLeads.Font = new System.Drawing.Font("Segoe UI", 10F, System.Drawing.FontStyle.Bold);
            this.lblStatTotalLeads.ForeColor = System.Drawing.Color.FromArgb(16, 185, 129);
            this.lblStatTotalLeads.Location = new System.Drawing.Point(260, 18);
            this.lblStatTotalLeads.Name = "lblStatTotalLeads";
            this.lblStatTotalLeads.Size = new System.Drawing.Size(147, 19);
            this.lblStatTotalLeads.TabIndex = 1;
            this.lblStatTotalLeads.Text = "0 Attributed Leads";
            // 
            // lblStatConversion
            // 
            this.lblStatConversion.AutoSize = true;
            this.lblStatConversion.Font = new System.Drawing.Font("Segoe UI", 10F, System.Drawing.FontStyle.Bold);
            this.lblStatConversion.ForeColor = System.Drawing.Color.FromArgb(100, 116, 139);
            this.lblStatConversion.Location = new System.Drawing.Point(500, 18);
            this.lblStatConversion.Name = "lblStatConversion";
            this.lblStatConversion.Size = new System.Drawing.Size(126, 19);
            this.lblStatConversion.TabIndex = 2;
            this.lblStatConversion.Text = "Top Source: None";
            // 
            // pnlSourcePills
            // 
            this.pnlSourcePills.AutoScroll = true;
            this.pnlSourcePills.BackColor = System.Drawing.Color.FromArgb(248, 250, 252);
            this.pnlSourcePills.Dock = System.Windows.Forms.DockStyle.Top;
            this.pnlSourcePills.Location = new System.Drawing.Point(0, 148);
            this.pnlSourcePills.Name = "pnlSourcePills";
            this.pnlSourcePills.Padding = new System.Windows.Forms.Padding(28, 8, 28, 8);
            this.pnlSourcePills.Size = new System.Drawing.Size(1000, 52);
            this.pnlSourcePills.TabIndex = 2;
            // 
            // pnlGridCard
            // 
            this.pnlGridCard.BackColor = System.Drawing.Color.White;
            this.pnlGridCard.Controls.Add(this.gridLeads);
            this.pnlGridCard.Dock = System.Windows.Forms.DockStyle.Fill;
            this.pnlGridCard.Location = new System.Drawing.Point(0, 200);
            this.pnlGridCard.Name = "pnlGridCard";
            this.pnlGridCard.Padding = new System.Windows.Forms.Padding(16);
            this.pnlGridCard.Size = new System.Drawing.Size(1000, 500);
            this.pnlGridCard.TabIndex = 3;
            // 
            // gridLeads
            // 
            this.gridLeads.AllowUserToAddRows = false;
            this.gridLeads.AllowUserToDeleteRows = false;
            this.gridLeads.AutoSizeColumnsMode = System.Windows.Forms.DataGridViewAutoSizeColumnsMode.Fill;
            this.gridLeads.BackgroundColor = System.Drawing.Color.White;
            this.gridLeads.BorderStyle = System.Windows.Forms.BorderStyle.None;
            this.gridLeads.CellBorderStyle = System.Windows.Forms.DataGridViewCellBorderStyle.SingleHorizontal;
            this.gridLeads.Dock = System.Windows.Forms.DockStyle.Fill;
            this.gridLeads.GridColor = System.Drawing.Color.FromArgb(241, 245, 249);
            this.gridLeads.Location = new System.Drawing.Point(16, 16);
            this.gridLeads.MultiSelect = false;
            this.gridLeads.Name = "gridLeads";
            this.gridLeads.ReadOnly = true;
            this.gridLeads.RowHeadersVisible = false;
            this.gridLeads.SelectionMode = System.Windows.Forms.DataGridViewSelectionMode.FullRowSelect;
            this.gridLeads.Size = new System.Drawing.Size(968, 468);
            this.gridLeads.TabIndex = 0;
            // 
            // CampaignsView
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 17F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.BackColor = System.Drawing.Color.FromArgb(248, 250, 252);
            this.Controls.Add(this.pnlGridCard);
            this.Controls.Add(this.pnlSourcePills);
            this.Controls.Add(this.pnlStats);
            this.Controls.Add(this.pnlHeader);
            this.Font = new System.Drawing.Font("Segoe UI", 9.5F);
            this.Name = "CampaignsView";
            this.Size = new System.Drawing.Size(1000, 700);
            this.pnlHeader.ResumeLayout(false);
            this.pnlHeader.PerformLayout();
            this.pnlStats.ResumeLayout(false);
            this.pnlStats.PerformLayout();
            this.pnlGridCard.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.gridLeads)).EndInit();
            this.ResumeLayout(false);
        }

        #endregion
    }
}
