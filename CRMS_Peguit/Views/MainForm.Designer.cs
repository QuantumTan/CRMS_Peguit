namespace CRMS_Peguit.winforms
{
    partial class MainForm
    {
        private System.ComponentModel.IContainer components = null;

        private System.Windows.Forms.Panel sidebarPanel = null!;
        private System.Windows.Forms.Panel pnlLogoHeader = null!;
        private System.Windows.Forms.PictureBox picLogo = null!;
        private System.Windows.Forms.Label lblLogo = null!;

        private System.Windows.Forms.Panel pnlNav = null!;
        private System.Windows.Forms.Button btnDashboard = null!;
        private System.Windows.Forms.Label lblSalesSection = null!;
        private System.Windows.Forms.Button btnLeads = null!;
        private System.Windows.Forms.Button btnCustomers = null!;
        private System.Windows.Forms.Button btnProperties = null!;
        private System.Windows.Forms.Button btnDeals = null!;
        private System.Windows.Forms.Button btnCampaigns = null!;
        private System.Windows.Forms.Button btnActivities = null!;
        private System.Windows.Forms.Button btnFollowUps = null!;
        private System.Windows.Forms.Label lblSupportSection = null!;
        private System.Windows.Forms.Button btnSupportTickets = null!;
        private System.Windows.Forms.Label lblInsightsSection = null!;
        private System.Windows.Forms.Button btnAnalytics = null!;
        private System.Windows.Forms.Button btnReports = null!;
        private System.Windows.Forms.Label lblAdminSection = null!;
        private System.Windows.Forms.Button btnApprovals = null!;
        private System.Windows.Forms.Button btnManageManagers = null!;
        private System.Windows.Forms.Button btnManageAgents = null!;
        private System.Windows.Forms.Button btnLogout = null!;

        private System.Windows.Forms.Panel contentWrapperPanel = null!;
        private System.Windows.Forms.Panel topHeaderPanel = null!;
        private System.Windows.Forms.Button btnToggleSidebar = null!;
        private System.Windows.Forms.TextBox txtGlobalSearch = null!;
        private CRMS_Peguit.winforms.Views.Controls.NotificationBell notificationBell = null!;
        private System.Windows.Forms.Label lblHeaderAvatar = null!;
        private System.Windows.Forms.Label lblHeaderUserName = null!;
        private System.Windows.Forms.Panel mainPanel = null!;
        private System.Windows.Forms.ToolTip mainToolTip = null!;

        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        private void InitializeComponent()
        {
            this.sidebarPanel = new System.Windows.Forms.Panel();
            this.pnlLogoHeader = new System.Windows.Forms.Panel();
            this.picLogo = new System.Windows.Forms.PictureBox();
            this.lblLogo = new System.Windows.Forms.Label();
            this.pnlNav = new System.Windows.Forms.Panel();
            this.btnDashboard = new System.Windows.Forms.Button();
            this.lblSalesSection = new System.Windows.Forms.Label();
            this.btnLeads = new System.Windows.Forms.Button();
            this.btnCustomers = new System.Windows.Forms.Button();
            this.btnProperties = new System.Windows.Forms.Button();
            this.btnDeals = new System.Windows.Forms.Button();
            this.btnCampaigns = new System.Windows.Forms.Button();
            this.btnActivities = new System.Windows.Forms.Button();
            this.btnFollowUps = new System.Windows.Forms.Button();
            this.lblSupportSection = new System.Windows.Forms.Label();
            this.btnSupportTickets = new System.Windows.Forms.Button();
            this.lblInsightsSection = new System.Windows.Forms.Label();
            this.btnAnalytics = new System.Windows.Forms.Button();
            this.btnReports = new System.Windows.Forms.Button();
            this.lblAdminSection = new System.Windows.Forms.Label();
            this.btnApprovals = new System.Windows.Forms.Button();
            this.btnManageManagers = new System.Windows.Forms.Button();
            this.btnManageAgents = new System.Windows.Forms.Button();
            this.btnLogout = new System.Windows.Forms.Button();
            this.contentWrapperPanel = new System.Windows.Forms.Panel();
            this.topHeaderPanel = new System.Windows.Forms.Panel();
            this.btnToggleSidebar = new System.Windows.Forms.Button();
            this.txtGlobalSearch = new System.Windows.Forms.TextBox();
            this.notificationBell = new CRMS_Peguit.winforms.Views.Controls.NotificationBell();
            this.lblHeaderAvatar = new System.Windows.Forms.Label();
            this.lblHeaderUserName = new System.Windows.Forms.Label();
            this.mainPanel = new System.Windows.Forms.Panel();
            this.mainToolTip = new System.Windows.Forms.ToolTip();
            this.sidebarPanel.SuspendLayout();
            this.pnlLogoHeader.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.picLogo)).BeginInit();
            this.pnlNav.SuspendLayout();
            this.contentWrapperPanel.SuspendLayout();
            this.topHeaderPanel.SuspendLayout();
            this.SuspendLayout();
            // 
            // sidebarPanel
            // 
            this.sidebarPanel.BackColor = System.Drawing.Color.FromArgb(15, 23, 42);
            this.sidebarPanel.Controls.Add(this.pnlNav);
            this.sidebarPanel.Controls.Add(this.btnLogout);
            this.sidebarPanel.Controls.Add(this.pnlLogoHeader);
            this.sidebarPanel.Dock = System.Windows.Forms.DockStyle.Left;
            this.sidebarPanel.Location = new System.Drawing.Point(0, 0);
            this.sidebarPanel.Name = "sidebarPanel";
            this.sidebarPanel.Padding = new System.Windows.Forms.Padding(0, 8, 0, 8);
            this.sidebarPanel.Size = new System.Drawing.Size(240, 720);
            this.sidebarPanel.TabIndex = 0;
            // 
            // pnlLogoHeader
            // 
            this.pnlLogoHeader.BackColor = System.Drawing.Color.Transparent;
            this.pnlLogoHeader.Controls.Add(this.picLogo);
            this.pnlLogoHeader.Controls.Add(this.lblLogo);
            this.pnlLogoHeader.Dock = System.Windows.Forms.DockStyle.Top;
            this.pnlLogoHeader.Location = new System.Drawing.Point(0, 8);
            this.pnlLogoHeader.Name = "pnlLogoHeader";
            this.pnlLogoHeader.Size = new System.Drawing.Size(240, 48);
            this.pnlLogoHeader.TabIndex = 0;
            // 
            // picLogo
            // 
            this.picLogo.BackColor = System.Drawing.Color.Transparent;
            this.picLogo.Location = new System.Drawing.Point(14, 9);
            this.picLogo.Name = "picLogo";
            this.picLogo.Size = new System.Drawing.Size(30, 30);
            this.picLogo.SizeMode = System.Windows.Forms.PictureBoxSizeMode.Zoom;
            this.picLogo.TabIndex = 0;
            this.picLogo.TabStop = false;
            // 
            // lblLogo
            // 
            this.lblLogo.AutoSize = true;
            this.lblLogo.Font = new System.Drawing.Font("Segoe UI", 12F, System.Drawing.FontStyle.Bold);
            this.lblLogo.ForeColor = System.Drawing.Color.White;
            this.lblLogo.Location = new System.Drawing.Point(50, 14);
            this.lblLogo.Name = "lblLogo";
            this.lblLogo.Size = new System.Drawing.Size(164, 21);
            this.lblLogo.TabIndex = 1;
            this.lblLogo.Text = "NEXA CRM SYSTEM";
            this.lblLogo.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // pnlNav
            // 
            this.pnlNav.AutoScroll = true;
            this.pnlNav.Controls.Add(this.btnManageAgents);
            this.pnlNav.Controls.Add(this.btnManageManagers);
            this.pnlNav.Controls.Add(this.btnApprovals);
            this.pnlNav.Controls.Add(this.lblAdminSection);
            this.pnlNav.Controls.Add(this.btnReports);
            this.pnlNav.Controls.Add(this.btnAnalytics);
            this.pnlNav.Controls.Add(this.lblInsightsSection);
            this.pnlNav.Controls.Add(this.btnSupportTickets);
            this.pnlNav.Controls.Add(this.lblSupportSection);
            this.pnlNav.Controls.Add(this.btnFollowUps);
            this.pnlNav.Controls.Add(this.btnActivities);
            this.pnlNav.Controls.Add(this.btnCampaigns);
            this.pnlNav.Controls.Add(this.btnDeals);
            this.pnlNav.Controls.Add(this.btnProperties);
            this.pnlNav.Controls.Add(this.btnCustomers);
            this.pnlNav.Controls.Add(this.btnLeads);
            this.pnlNav.Controls.Add(this.lblSalesSection);
            this.pnlNav.Controls.Add(this.btnDashboard);
            this.pnlNav.Dock = System.Windows.Forms.DockStyle.Fill;
            this.pnlNav.Location = new System.Drawing.Point(0, 118);
            this.pnlNav.Name = "pnlNav";
            this.pnlNav.Padding = new System.Windows.Forms.Padding(0, 4, 0, 4);
            this.pnlNav.Size = new System.Drawing.Size(240, 550);
            this.pnlNav.TabIndex = 2;
            // 
            // btnDashboard
            // 
            this.btnDashboard.Cursor = System.Windows.Forms.Cursors.Hand;
            this.btnDashboard.Dock = System.Windows.Forms.DockStyle.Top;
            this.btnDashboard.FlatAppearance.BorderSize = 0;
            this.btnDashboard.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnDashboard.Font = new System.Drawing.Font("Segoe UI", 9.5F);
            this.btnDashboard.ForeColor = System.Drawing.Color.FromArgb(203, 213, 225);
            this.btnDashboard.Location = new System.Drawing.Point(0, 0);
            this.btnDashboard.Name = "btnDashboard";
            this.btnDashboard.Padding = new System.Windows.Forms.Padding(16, 0, 0, 0);
            this.btnDashboard.Size = new System.Drawing.Size(240, 38);
            this.btnDashboard.TabIndex = 0;
            this.btnDashboard.Text = "  ⊞  Dashboard";
            this.btnDashboard.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            this.btnDashboard.UseVisualStyleBackColor = true;
            // 
            // lblSalesSection
            // 
            this.lblSalesSection.Dock = System.Windows.Forms.DockStyle.Top;
            this.lblSalesSection.Font = new System.Drawing.Font("Segoe UI", 7.5F, System.Drawing.FontStyle.Bold);
            this.lblSalesSection.ForeColor = System.Drawing.Color.FromArgb(88, 118, 147);
            this.lblSalesSection.Location = new System.Drawing.Point(0, 38);
            this.lblSalesSection.Name = "lblSalesSection";
            this.lblSalesSection.Padding = new System.Windows.Forms.Padding(18, 10, 0, 0);
            this.lblSalesSection.Size = new System.Drawing.Size(240, 28);
            this.lblSalesSection.TabIndex = 1;
            this.lblSalesSection.Text = "SALES";
            // 
            // btnLeads
            // 
            this.btnLeads.Cursor = System.Windows.Forms.Cursors.Hand;
            this.btnLeads.Dock = System.Windows.Forms.DockStyle.Top;
            this.btnLeads.FlatAppearance.BorderSize = 0;
            this.btnLeads.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnLeads.Font = new System.Drawing.Font("Segoe UI", 9.5F);
            this.btnLeads.ForeColor = System.Drawing.Color.FromArgb(203, 213, 225);
            this.btnLeads.Location = new System.Drawing.Point(0, 66);
            this.btnLeads.Name = "btnLeads";
            this.btnLeads.Padding = new System.Windows.Forms.Padding(16, 0, 0, 0);
            this.btnLeads.Size = new System.Drawing.Size(240, 38);
            this.btnLeads.TabIndex = 2;
            this.btnLeads.Text = "  ◎  Leads";
            this.btnLeads.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            this.btnLeads.UseVisualStyleBackColor = true;
            // 
            // btnCustomers
            // 
            this.btnCustomers.Cursor = System.Windows.Forms.Cursors.Hand;
            this.btnCustomers.Dock = System.Windows.Forms.DockStyle.Top;
            this.btnCustomers.FlatAppearance.BorderSize = 0;
            this.btnCustomers.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnCustomers.Font = new System.Drawing.Font("Segoe UI", 9.5F);
            this.btnCustomers.ForeColor = System.Drawing.Color.FromArgb(203, 213, 225);
            this.btnCustomers.Location = new System.Drawing.Point(0, 104);
            this.btnCustomers.Name = "btnCustomers";
            this.btnCustomers.Padding = new System.Windows.Forms.Padding(16, 0, 0, 0);
            this.btnCustomers.Size = new System.Drawing.Size(240, 38);
            this.btnCustomers.TabIndex = 3;
            this.btnCustomers.Text = "  👥  Customers";
            this.btnCustomers.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            this.btnCustomers.UseVisualStyleBackColor = true;
            // 
            // btnProperties
            // 
            this.btnProperties.Cursor = System.Windows.Forms.Cursors.Hand;
            this.btnProperties.Dock = System.Windows.Forms.DockStyle.Top;
            this.btnProperties.FlatAppearance.BorderSize = 0;
            this.btnProperties.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnProperties.Font = new System.Drawing.Font("Segoe UI", 9.5F);
            this.btnProperties.ForeColor = System.Drawing.Color.FromArgb(203, 213, 225);
            this.btnProperties.Location = new System.Drawing.Point(0, 142);
            this.btnProperties.Name = "btnProperties";
            this.btnProperties.Padding = new System.Windows.Forms.Padding(16, 0, 0, 0);
            this.btnProperties.Size = new System.Drawing.Size(240, 38);
            this.btnProperties.TabIndex = 4;
            this.btnProperties.Text = "  🏢  Properties";
            this.btnProperties.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            this.btnProperties.UseVisualStyleBackColor = true;
            // 
            // btnDeals
            // 
            this.btnDeals.Cursor = System.Windows.Forms.Cursors.Hand;
            this.btnDeals.Dock = System.Windows.Forms.DockStyle.Top;
            this.btnDeals.FlatAppearance.BorderSize = 0;
            this.btnDeals.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnDeals.Font = new System.Drawing.Font("Segoe UI", 9.5F);
            this.btnDeals.ForeColor = System.Drawing.Color.FromArgb(203, 213, 225);
            this.btnDeals.Location = new System.Drawing.Point(0, 180);
            this.btnDeals.Name = "btnDeals";
            this.btnDeals.Padding = new System.Windows.Forms.Padding(16, 0, 0, 0);
            this.btnDeals.Size = new System.Drawing.Size(240, 38);
            this.btnDeals.TabIndex = 5;
            this.btnDeals.Text = "  💼  Deals";
            this.btnDeals.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            this.btnDeals.UseVisualStyleBackColor = true;
            // 
            // btnCampaigns
            // 
            this.btnCampaigns.Cursor = System.Windows.Forms.Cursors.Hand;
            this.btnCampaigns.Dock = System.Windows.Forms.DockStyle.Top;
            this.btnCampaigns.FlatAppearance.BorderSize = 0;
            this.btnCampaigns.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnCampaigns.Font = new System.Drawing.Font("Segoe UI", 9.5F);
            this.btnCampaigns.ForeColor = System.Drawing.Color.FromArgb(203, 213, 225);
            this.btnCampaigns.Location = new System.Drawing.Point(0, 218);
            this.btnCampaigns.Name = "btnCampaigns";
            this.btnCampaigns.Padding = new System.Windows.Forms.Padding(16, 0, 0, 0);
            this.btnCampaigns.Size = new System.Drawing.Size(240, 38);
            this.btnCampaigns.TabIndex = 6;
            this.btnCampaigns.Text = "  📣  Campaigns";
            this.btnCampaigns.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            this.btnCampaigns.UseVisualStyleBackColor = true;
            // 
            // btnActivities
            // 
            this.btnActivities.Cursor = System.Windows.Forms.Cursors.Hand;
            this.btnActivities.Dock = System.Windows.Forms.DockStyle.Top;
            this.btnActivities.FlatAppearance.BorderSize = 0;
            this.btnActivities.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnActivities.Font = new System.Drawing.Font("Segoe UI", 9.5F);
            this.btnActivities.ForeColor = System.Drawing.Color.FromArgb(203, 213, 225);
            this.btnActivities.Location = new System.Drawing.Point(0, 218);
            this.btnActivities.Name = "btnActivities";
            this.btnActivities.Padding = new System.Windows.Forms.Padding(16, 0, 0, 0);
            this.btnActivities.Size = new System.Drawing.Size(240, 38);
            this.btnActivities.TabIndex = 6;
            this.btnActivities.Text = "  📈  Activities";
            this.btnActivities.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            this.btnActivities.UseVisualStyleBackColor = true;
            // 
            // btnFollowUps
            // 
            this.btnFollowUps.Cursor = System.Windows.Forms.Cursors.Hand;
            this.btnFollowUps.Dock = System.Windows.Forms.DockStyle.Top;
            this.btnFollowUps.FlatAppearance.BorderSize = 0;
            this.btnFollowUps.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnFollowUps.Font = new System.Drawing.Font("Segoe UI", 9.5F);
            this.btnFollowUps.ForeColor = System.Drawing.Color.FromArgb(203, 213, 225);
            this.btnFollowUps.Location = new System.Drawing.Point(0, 256);
            this.btnFollowUps.Name = "btnFollowUps";
            this.btnFollowUps.Padding = new System.Windows.Forms.Padding(16, 0, 0, 0);
            this.btnFollowUps.Size = new System.Drawing.Size(240, 38);
            this.btnFollowUps.TabIndex = 7;
            this.btnFollowUps.Text = "  ⏱  Follow-Ups";
            this.btnFollowUps.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            this.btnFollowUps.UseVisualStyleBackColor = true;
            // 
            // lblSupportSection
            // 
            this.lblSupportSection.Dock = System.Windows.Forms.DockStyle.Top;
            this.lblSupportSection.Font = new System.Drawing.Font("Segoe UI", 7.5F, System.Drawing.FontStyle.Bold);
            this.lblSupportSection.ForeColor = System.Drawing.Color.FromArgb(88, 118, 147);
            this.lblSupportSection.Location = new System.Drawing.Point(0, 294);
            this.lblSupportSection.Name = "lblSupportSection";
            this.lblSupportSection.Padding = new System.Windows.Forms.Padding(18, 10, 0, 0);
            this.lblSupportSection.Size = new System.Drawing.Size(240, 28);
            this.lblSupportSection.TabIndex = 8;
            this.lblSupportSection.Text = "SUPPORT";
            // 
            // btnSupportTickets
            // 
            this.btnSupportTickets.Cursor = System.Windows.Forms.Cursors.Hand;
            this.btnSupportTickets.Dock = System.Windows.Forms.DockStyle.Top;
            this.btnSupportTickets.FlatAppearance.BorderSize = 0;
            this.btnSupportTickets.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnSupportTickets.Font = new System.Drawing.Font("Segoe UI", 9.5F);
            this.btnSupportTickets.ForeColor = System.Drawing.Color.FromArgb(203, 213, 225);
            this.btnSupportTickets.Location = new System.Drawing.Point(0, 322);
            this.btnSupportTickets.Name = "btnSupportTickets";
            this.btnSupportTickets.Padding = new System.Windows.Forms.Padding(16, 0, 0, 0);
            this.btnSupportTickets.Size = new System.Drawing.Size(240, 38);
            this.btnSupportTickets.TabIndex = 9;
            this.btnSupportTickets.Text = "  🎟  Support Tickets";
            this.btnSupportTickets.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            this.btnSupportTickets.UseVisualStyleBackColor = true;
            // 
            // lblInsightsSection
            // 
            this.lblInsightsSection.Dock = System.Windows.Forms.DockStyle.Top;
            this.lblInsightsSection.Font = new System.Drawing.Font("Segoe UI", 7.5F, System.Drawing.FontStyle.Bold);
            this.lblInsightsSection.ForeColor = System.Drawing.Color.FromArgb(88, 118, 147);
            this.lblInsightsSection.Location = new System.Drawing.Point(0, 360);
            this.lblInsightsSection.Name = "lblInsightsSection";
            this.lblInsightsSection.Padding = new System.Windows.Forms.Padding(18, 10, 0, 0);
            this.lblInsightsSection.Size = new System.Drawing.Size(240, 28);
            this.lblInsightsSection.TabIndex = 10;
            this.lblInsightsSection.Text = "INSIGHTS";
            // 
            // btnAnalytics
            // 
            this.btnAnalytics.Cursor = System.Windows.Forms.Cursors.Hand;
            this.btnAnalytics.Dock = System.Windows.Forms.DockStyle.Top;
            this.btnAnalytics.FlatAppearance.BorderSize = 0;
            this.btnAnalytics.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnAnalytics.Font = new System.Drawing.Font("Segoe UI", 9.5F);
            this.btnAnalytics.ForeColor = System.Drawing.Color.FromArgb(203, 213, 225);
            this.btnAnalytics.Location = new System.Drawing.Point(0, 388);
            this.btnAnalytics.Name = "btnAnalytics";
            this.btnAnalytics.Padding = new System.Windows.Forms.Padding(16, 0, 0, 0);
            this.btnAnalytics.Size = new System.Drawing.Size(240, 38);
            this.btnAnalytics.TabIndex = 11;
            this.btnAnalytics.Text = "  📊  Analytics";
            this.btnAnalytics.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            this.btnAnalytics.UseVisualStyleBackColor = true;
            // 
            // btnReports
            // 
            this.btnReports.Cursor = System.Windows.Forms.Cursors.Hand;
            this.btnReports.Dock = System.Windows.Forms.DockStyle.Top;
            this.btnReports.FlatAppearance.BorderSize = 0;
            this.btnReports.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnReports.Font = new System.Drawing.Font("Segoe UI", 9.5F);
            this.btnReports.ForeColor = System.Drawing.Color.FromArgb(203, 213, 225);
            this.btnReports.Location = new System.Drawing.Point(0, 426);
            this.btnReports.Name = "btnReports";
            this.btnReports.Padding = new System.Windows.Forms.Padding(16, 0, 0, 0);
            this.btnReports.Size = new System.Drawing.Size(240, 38);
            this.btnReports.TabIndex = 12;
            this.btnReports.Text = "  📋  Reports & Exports";
            this.btnReports.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            this.btnReports.UseVisualStyleBackColor = true;
            // 
            // lblAdminSection
            // 
            this.lblAdminSection.Dock = System.Windows.Forms.DockStyle.Top;
            this.lblAdminSection.Font = new System.Drawing.Font("Segoe UI", 7.5F, System.Drawing.FontStyle.Bold);
            this.lblAdminSection.ForeColor = System.Drawing.Color.FromArgb(88, 118, 147);
            this.lblAdminSection.Location = new System.Drawing.Point(0, 426);
            this.lblAdminSection.Name = "lblAdminSection";
            this.lblAdminSection.Padding = new System.Windows.Forms.Padding(18, 10, 0, 0);
            this.lblAdminSection.Size = new System.Drawing.Size(240, 28);
            this.lblAdminSection.TabIndex = 12;
            this.lblAdminSection.Text = "ADMINISTRATION";
            // 
            // btnApprovals
            // 
            this.btnApprovals.Cursor = System.Windows.Forms.Cursors.Hand;
            this.btnApprovals.Dock = System.Windows.Forms.DockStyle.Top;
            this.btnApprovals.FlatAppearance.BorderSize = 0;
            this.btnApprovals.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnApprovals.Font = new System.Drawing.Font("Segoe UI", 9.5F);
            this.btnApprovals.ForeColor = System.Drawing.Color.FromArgb(203, 213, 225);
            this.btnApprovals.Location = new System.Drawing.Point(0, 454);
            this.btnApprovals.Name = "btnApprovals";
            this.btnApprovals.Padding = new System.Windows.Forms.Padding(16, 0, 0, 0);
            this.btnApprovals.Size = new System.Drawing.Size(240, 38);
            this.btnApprovals.TabIndex = 13;
            this.btnApprovals.Text = "  ✓  Approvals && Review";
            this.btnApprovals.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            this.btnApprovals.UseVisualStyleBackColor = true;
            // 
            // btnManageManagers
            // 
            this.btnManageManagers.Cursor = System.Windows.Forms.Cursors.Hand;
            this.btnManageManagers.Dock = System.Windows.Forms.DockStyle.Top;
            this.btnManageManagers.FlatAppearance.BorderSize = 0;
            this.btnManageManagers.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnManageManagers.Font = new System.Drawing.Font("Segoe UI", 9.5F);
            this.btnManageManagers.ForeColor = System.Drawing.Color.FromArgb(203, 213, 225);
            this.btnManageManagers.Location = new System.Drawing.Point(0, 454);
            this.btnManageManagers.Name = "btnManageManagers";
            this.btnManageManagers.Padding = new System.Windows.Forms.Padding(16, 0, 0, 0);
            this.btnManageManagers.Size = new System.Drawing.Size(240, 38);
            this.btnManageManagers.TabIndex = 13;
            this.btnManageManagers.Text = "  🛡  Manage Managers";
            this.btnManageManagers.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            this.btnManageManagers.UseVisualStyleBackColor = true;
            // 
            // btnManageAgents
            // 
            this.btnManageAgents.Cursor = System.Windows.Forms.Cursors.Hand;
            this.btnManageAgents.Dock = System.Windows.Forms.DockStyle.Top;
            this.btnManageAgents.FlatAppearance.BorderSize = 0;
            this.btnManageAgents.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnManageAgents.Font = new System.Drawing.Font("Segoe UI", 9.5F);
            this.btnManageAgents.ForeColor = System.Drawing.Color.FromArgb(203, 213, 225);
            this.btnManageAgents.Location = new System.Drawing.Point(0, 492);
            this.btnManageAgents.Name = "btnManageAgents";
            this.btnManageAgents.Padding = new System.Windows.Forms.Padding(16, 0, 0, 0);
            this.btnManageAgents.Size = new System.Drawing.Size(240, 38);
            this.btnManageAgents.TabIndex = 14;
            this.btnManageAgents.Text = "  👥  Manage Agents";
            this.btnManageAgents.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            this.btnManageAgents.UseVisualStyleBackColor = true;
            // 
            // btnLogout
            // 
            this.btnLogout.Cursor = System.Windows.Forms.Cursors.Hand;
            this.btnLogout.Dock = System.Windows.Forms.DockStyle.Bottom;
            this.btnLogout.FlatAppearance.BorderSize = 0;
            this.btnLogout.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnLogout.Font = new System.Drawing.Font("Segoe UI", 9.5F);
            this.btnLogout.ForeColor = System.Drawing.Color.FromArgb(203, 213, 225);
            this.btnLogout.Location = new System.Drawing.Point(0, 668);
            this.btnLogout.Name = "btnLogout";
            this.btnLogout.Padding = new System.Windows.Forms.Padding(16, 0, 0, 0);
            this.btnLogout.Size = new System.Drawing.Size(240, 44);
            this.btnLogout.TabIndex = 3;
            this.btnLogout.Text = "  ↪  Sign out";
            this.btnLogout.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            this.btnLogout.UseVisualStyleBackColor = true;
            // 
            // contentWrapperPanel
            // 
            this.contentWrapperPanel.Controls.Add(this.mainPanel);
            this.contentWrapperPanel.Controls.Add(this.topHeaderPanel);
            this.contentWrapperPanel.Dock = System.Windows.Forms.DockStyle.Fill;
            this.contentWrapperPanel.Location = new System.Drawing.Point(240, 0);
            this.contentWrapperPanel.Name = "contentWrapperPanel";
            this.contentWrapperPanel.Size = new System.Drawing.Size(1040, 720);
            this.contentWrapperPanel.TabIndex = 1;
            // 
            // topHeaderPanel
            // 
            this.topHeaderPanel.BackColor = System.Drawing.Color.White;
            this.topHeaderPanel.Controls.Add(this.btnToggleSidebar);
            this.topHeaderPanel.Controls.Add(this.txtGlobalSearch);
            this.topHeaderPanel.Controls.Add(this.notificationBell);
            this.topHeaderPanel.Controls.Add(this.lblHeaderAvatar);
            this.topHeaderPanel.Controls.Add(this.lblHeaderUserName);
            this.topHeaderPanel.Dock = System.Windows.Forms.DockStyle.Top;
            this.topHeaderPanel.Location = new System.Drawing.Point(0, 0);
            this.topHeaderPanel.Name = "topHeaderPanel";
            this.topHeaderPanel.Size = new System.Drawing.Size(1040, 58);
            this.topHeaderPanel.TabIndex = 0;
            // 
            // btnToggleSidebar
            // 
            this.btnToggleSidebar.Cursor = System.Windows.Forms.Cursors.Hand;
            this.btnToggleSidebar.FlatAppearance.BorderSize = 0;
            this.btnToggleSidebar.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnToggleSidebar.Font = new System.Drawing.Font("Segoe UI", 12F, System.Drawing.FontStyle.Bold);
            this.btnToggleSidebar.ForeColor = System.Drawing.Color.FromArgb(15, 23, 42);
            this.btnToggleSidebar.Location = new System.Drawing.Point(12, 12);
            this.btnToggleSidebar.Name = "btnToggleSidebar";
            this.btnToggleSidebar.Size = new System.Drawing.Size(36, 34);
            this.btnToggleSidebar.TabIndex = 0;
            this.btnToggleSidebar.Text = "☰";
            this.btnToggleSidebar.UseVisualStyleBackColor = true;
            // 
            // txtGlobalSearch
            // 
            this.txtGlobalSearch.BackColor = System.Drawing.Color.FromArgb(241, 245, 249);
            this.txtGlobalSearch.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.txtGlobalSearch.Font = new System.Drawing.Font("Segoe UI", 9.5F);
            this.txtGlobalSearch.ForeColor = System.Drawing.Color.FromArgb(15, 23, 42);
            this.txtGlobalSearch.Location = new System.Drawing.Point(58, 16);
            this.txtGlobalSearch.Name = "txtGlobalSearch";
            this.txtGlobalSearch.PlaceholderText = "🔍 Search records, contacts... (Ctrl+K)";
            this.txtGlobalSearch.Size = new System.Drawing.Size(320, 24);
            this.txtGlobalSearch.TabIndex = 2;
            // 
            // notificationBell
            // 
            this.notificationBell.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.notificationBell.Cursor = System.Windows.Forms.Cursors.Hand;
            this.notificationBell.Location = new System.Drawing.Point(796, 12);
            this.notificationBell.Name = "notificationBell";
            this.notificationBell.Size = new System.Drawing.Size(34, 34);
            this.notificationBell.TabIndex = 2;
            // 
            // lblHeaderAvatar
            // 
            this.lblHeaderAvatar.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.lblHeaderAvatar.BackColor = System.Drawing.Color.FromArgb(41, 98, 150);
            this.lblHeaderAvatar.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Bold);
            this.lblHeaderAvatar.ForeColor = System.Drawing.Color.White;
            this.lblHeaderAvatar.Location = new System.Drawing.Point(838, 14);
            this.lblHeaderAvatar.Name = "lblHeaderAvatar";
            this.lblHeaderAvatar.Size = new System.Drawing.Size(32, 32);
            this.lblHeaderAvatar.TabIndex = 3;
            this.lblHeaderAvatar.Text = "SJ";
            this.lblHeaderAvatar.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            // 
            // lblHeaderUserName
            // 
            this.lblHeaderUserName.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.lblHeaderUserName.Font = new System.Drawing.Font("Segoe UI", 8.5F, System.Drawing.FontStyle.Bold);
            this.lblHeaderUserName.ForeColor = System.Drawing.Color.FromArgb(15, 23, 42);
            this.lblHeaderUserName.Location = new System.Drawing.Point(876, 12);
            this.lblHeaderUserName.Name = "lblHeaderUserName";
            this.lblHeaderUserName.Size = new System.Drawing.Size(155, 36);
            this.lblHeaderUserName.TabIndex = 4;
            this.lblHeaderUserName.Text = "Sam Johnson\r\nAgent";
            this.lblHeaderUserName.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // mainPanel
            // 
            this.mainPanel.BackColor = System.Drawing.Color.FromArgb(244, 247, 251);
            this.mainPanel.Dock = System.Windows.Forms.DockStyle.Fill;
            this.mainPanel.Location = new System.Drawing.Point(0, 58);
            this.mainPanel.Name = "mainPanel";
            this.mainPanel.Size = new System.Drawing.Size(1040, 662);
            this.mainPanel.TabIndex = 1;
            // 
            // Form1
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 15F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.BackColor = System.Drawing.Color.FromArgb(244, 247, 251);
            this.ClientSize = new System.Drawing.Size(1280, 720);
            this.Controls.Add(this.contentWrapperPanel);
            this.Controls.Add(this.sidebarPanel);
            this.MinimumSize = new System.Drawing.Size(850, 540);
            this.Name = "MainForm";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = "NEXA CRM SYSTEM";
            ((System.ComponentModel.ISupportInitialize)(this.picLogo)).EndInit();
            this.pnlLogoHeader.ResumeLayout(false);
            this.pnlLogoHeader.PerformLayout();
            this.sidebarPanel.ResumeLayout(false);
            this.pnlNav.ResumeLayout(false);
            this.contentWrapperPanel.ResumeLayout(false);
            this.topHeaderPanel.ResumeLayout(false);
            this.topHeaderPanel.PerformLayout();
            this.ResumeLayout(false);

        }

        #endregion
    }
}
