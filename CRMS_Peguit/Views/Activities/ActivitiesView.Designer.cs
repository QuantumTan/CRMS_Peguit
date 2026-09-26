namespace CRMS_Peguit.winforms.Views.Activities
{
    partial class ActivitiesView
    {
        private System.ComponentModel.IContainer components = null;

        private System.Windows.Forms.Label lblTitle = null!;
        private System.Windows.Forms.Label lblSubtitle = null!;
        private System.Windows.Forms.Button btnAdd = null!;
        private System.Windows.Forms.TableLayoutPanel pnlKpiContainer = null!;
        private CRMS_Peguit.winforms.Controls.KpiCard kpiTotal = null!;
        private CRMS_Peguit.winforms.Controls.KpiCard kpiCalls = null!;
        private CRMS_Peguit.winforms.Controls.KpiCard kpiEmails = null!;
        private CRMS_Peguit.winforms.Controls.KpiCard kpiMeetings = null!;

        private System.Windows.Forms.TextBox txtSearch = null!;
        private System.Windows.Forms.Button btnFilterAll = null!;
        private System.Windows.Forms.Button btnFilterCalls = null!;
        private System.Windows.Forms.Button btnFilterEmails = null!;
        private System.Windows.Forms.Button btnFilterMeetings = null!;
        private System.Windows.Forms.Button btnFilterSystem = null!;

        private System.Windows.Forms.Panel pnlCard = null!;
        private System.Windows.Forms.DataGridView grid = null!;

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
            this.btnAdd = new System.Windows.Forms.Button();
            this.pnlKpiContainer = new System.Windows.Forms.TableLayoutPanel();
            this.kpiTotal = new CRMS_Peguit.winforms.Controls.KpiCard("TOTAL LOGGED", "all", CRMS_Peguit.winforms.Models.Services.Theme.Primary);
            this.kpiCalls = new CRMS_Peguit.winforms.Controls.KpiCard("CALLS", "calls", CRMS_Peguit.winforms.Models.Services.Theme.StatusInfo);
            this.kpiEmails = new CRMS_Peguit.winforms.Controls.KpiCard("EMAILS", "emails", CRMS_Peguit.winforms.Models.Services.Theme.StatusPending);
            this.kpiMeetings = new CRMS_Peguit.winforms.Controls.KpiCard("MEETINGS", "meetings", CRMS_Peguit.winforms.Models.Services.Theme.StatusSuccess);

            this.txtSearch = new System.Windows.Forms.TextBox();
            this.btnFilterAll = new System.Windows.Forms.Button();
            this.btnFilterCalls = new System.Windows.Forms.Button();
            this.btnFilterEmails = new System.Windows.Forms.Button();
            this.btnFilterMeetings = new System.Windows.Forms.Button();
            this.btnFilterSystem = new System.Windows.Forms.Button();

            this.pnlCard = new System.Windows.Forms.Panel();
            this.grid = new System.Windows.Forms.DataGridView();

            this.pnlKpiContainer.SuspendLayout();
            this.pnlCard.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.grid)).BeginInit();
            this.SuspendLayout();
            // 
            // lblTitle
            // 
            this.lblTitle.AutoSize = true;
            this.lblTitle.Font = CRMS_Peguit.winforms.Services.UiStyleConstants.PageTitleFont;
            this.lblTitle.ForeColor = CRMS_Peguit.winforms.Models.Services.Theme.TextPrimary;
            this.lblTitle.Location = new System.Drawing.Point(CRMS_Peguit.winforms.Services.UiStyleConstants.PageMarginLeft, CRMS_Peguit.winforms.Services.UiStyleConstants.PageMarginTop);
            this.lblTitle.Name = "lblTitle";
            this.lblTitle.Size = new System.Drawing.Size(340, 37);
            this.lblTitle.TabIndex = 0;
            this.lblTitle.Text = "Client Activity Log";
            // 
            // lblSubtitle
            // 
            this.lblSubtitle.AutoSize = true;
            this.lblSubtitle.Font = CRMS_Peguit.winforms.Services.UiStyleConstants.SubtitleFont;
            this.lblSubtitle.ForeColor = CRMS_Peguit.winforms.Models.Services.Theme.TextSecondary;
            this.lblSubtitle.Location = new System.Drawing.Point(CRMS_Peguit.winforms.Services.UiStyleConstants.PageMarginLeft + 2, 58);
            this.lblSubtitle.Name = "lblSubtitle";
            this.lblSubtitle.Size = new System.Drawing.Size(430, 17);
            this.lblSubtitle.TabIndex = 1;
            this.lblSubtitle.Text = "Unified historical record of calls, emails, meetings, and touchpoints";
            // 
            // btnAdd
            // 
            this.btnAdd.BackColor = CRMS_Peguit.winforms.Models.Services.Theme.Primary;
            this.btnAdd.Cursor = System.Windows.Forms.Cursors.Hand;
            this.btnAdd.FlatAppearance.BorderSize = 0;
            this.btnAdd.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnAdd.Font = new System.Drawing.Font("Segoe UI", 9.5F, System.Drawing.FontStyle.Bold);
            this.btnAdd.ForeColor = System.Drawing.Color.White;
            this.btnAdd.Location = new System.Drawing.Point(860, 24);
            this.btnAdd.Name = "btnAdd";
            this.btnAdd.Size = new System.Drawing.Size(140, 36);
            this.btnAdd.TabIndex = 2;
            this.btnAdd.Text = "+ Log Activity";
            this.btnAdd.UseVisualStyleBackColor = false;
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
            this.pnlKpiContainer.Controls.Add(this.kpiTotal, 0, 0);
            this.pnlKpiContainer.Controls.Add(this.kpiCalls, 1, 0);
            this.pnlKpiContainer.Controls.Add(this.kpiEmails, 2, 0);
            this.pnlKpiContainer.Controls.Add(this.kpiMeetings, 3, 0);
            this.pnlKpiContainer.Location = new System.Drawing.Point(30, 90);
            this.pnlKpiContainer.Name = "pnlKpiContainer";
            this.pnlKpiContainer.RowCount = 1;
            this.pnlKpiContainer.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.pnlKpiContainer.Size = new System.Drawing.Size(970, 104);
            this.pnlKpiContainer.TabIndex = 3;
            // 
            // kpiTotal
            // 
            this.kpiTotal.Dock = System.Windows.Forms.DockStyle.Fill;
            this.kpiTotal.Location = new System.Drawing.Point(0, 0);
            this.kpiTotal.Margin = new System.Windows.Forms.Padding(0, 0, 8, 0);
            this.kpiTotal.Name = "kpiTotal";
            this.kpiTotal.Size = new System.Drawing.Size(234, 104);
            this.kpiTotal.TabIndex = 0;
            // 
            // kpiCalls
            // 
            this.kpiCalls.Dock = System.Windows.Forms.DockStyle.Fill;
            this.kpiCalls.Location = new System.Drawing.Point(242, 0);
            this.kpiCalls.Margin = new System.Windows.Forms.Padding(8, 0, 8, 0);
            this.kpiCalls.Name = "kpiCalls";
            this.kpiCalls.Size = new System.Drawing.Size(234, 104);
            this.kpiCalls.TabIndex = 1;
            // 
            // kpiEmails
            // 
            this.kpiEmails.Dock = System.Windows.Forms.DockStyle.Fill;
            this.kpiEmails.Location = new System.Drawing.Point(484, 0);
            this.kpiEmails.Margin = new System.Windows.Forms.Padding(8, 0, 8, 0);
            this.kpiEmails.Name = "kpiEmails";
            this.kpiEmails.Size = new System.Drawing.Size(234, 104);
            this.kpiEmails.TabIndex = 2;
            // 
            // kpiMeetings
            // 
            this.kpiMeetings.Dock = System.Windows.Forms.DockStyle.Fill;
            this.kpiMeetings.Location = new System.Drawing.Point(726, 0);
            this.kpiMeetings.Margin = new System.Windows.Forms.Padding(8, 0, 0, 0);
            this.kpiMeetings.Name = "kpiMeetings";
            this.kpiMeetings.Size = new System.Drawing.Size(236, 104);
            this.kpiMeetings.TabIndex = 3;
            // 
            // txtSearch
            // 
            this.txtSearch.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.txtSearch.BackColor = System.Drawing.Color.White;
            this.txtSearch.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.txtSearch.Font = new System.Drawing.Font("Segoe UI", 9.5F);
            this.txtSearch.ForeColor = System.Drawing.Color.FromArgb(15, 23, 42);
            this.txtSearch.Location = new System.Drawing.Point(750, 212);
            this.txtSearch.Name = "txtSearch";
            this.txtSearch.PlaceholderText = "Search activities, notes...";
            this.txtSearch.Size = new System.Drawing.Size(250, 24);
            this.txtSearch.TabIndex = 9;
            // 
            // btnFilterAll
            // 
            this.btnFilterAll.Cursor = System.Windows.Forms.Cursors.Hand;
            this.btnFilterAll.FlatAppearance.BorderSize = 0;
            this.btnFilterAll.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnFilterAll.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Bold);
            this.btnFilterAll.Location = new System.Drawing.Point(30, 210);
            this.btnFilterAll.Name = "btnFilterAll";
            this.btnFilterAll.Size = new System.Drawing.Size(65, 30);
            this.btnFilterAll.TabIndex = 4;
            this.btnFilterAll.Text = "All";
            this.btnFilterAll.UseVisualStyleBackColor = true;
            // 
            // btnFilterCalls
            // 
            this.btnFilterCalls.Cursor = System.Windows.Forms.Cursors.Hand;
            this.btnFilterCalls.FlatAppearance.BorderSize = 0;
            this.btnFilterCalls.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnFilterCalls.Font = new System.Drawing.Font("Segoe UI", 9F);
            this.btnFilterCalls.Location = new System.Drawing.Point(100, 210);
            this.btnFilterCalls.Name = "btnFilterCalls";
            this.btnFilterCalls.Size = new System.Drawing.Size(95, 30);
            this.btnFilterCalls.TabIndex = 5;
            this.btnFilterCalls.Text = "Calls 📞";
            this.btnFilterCalls.UseVisualStyleBackColor = true;
            // 
            // btnFilterEmails
            // 
            this.btnFilterEmails.Cursor = System.Windows.Forms.Cursors.Hand;
            this.btnFilterEmails.FlatAppearance.BorderSize = 0;
            this.btnFilterEmails.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnFilterEmails.Font = new System.Drawing.Font("Segoe UI", 9F);
            this.btnFilterEmails.Location = new System.Drawing.Point(200, 210);
            this.btnFilterEmails.Name = "btnFilterEmails";
            this.btnFilterEmails.Size = new System.Drawing.Size(95, 30);
            this.btnFilterEmails.TabIndex = 6;
            this.btnFilterEmails.Text = "Emails ✉️";
            this.btnFilterEmails.UseVisualStyleBackColor = true;
            // 
            // btnFilterMeetings
            // 
            this.btnFilterMeetings.Cursor = System.Windows.Forms.Cursors.Hand;
            this.btnFilterMeetings.FlatAppearance.BorderSize = 0;
            this.btnFilterMeetings.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnFilterMeetings.Font = new System.Drawing.Font("Segoe UI", 9F);
            this.btnFilterMeetings.Location = new System.Drawing.Point(300, 210);
            this.btnFilterMeetings.Name = "btnFilterMeetings";
            this.btnFilterMeetings.Size = new System.Drawing.Size(105, 30);
            this.btnFilterMeetings.TabIndex = 7;
            this.btnFilterMeetings.Text = "Meetings 📅";
            this.btnFilterMeetings.UseVisualStyleBackColor = true;
            // 
            // btnFilterSystem
            // 
            this.btnFilterSystem.Cursor = System.Windows.Forms.Cursors.Hand;
            this.btnFilterSystem.FlatAppearance.BorderSize = 0;
            this.btnFilterSystem.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnFilterSystem.Font = new System.Drawing.Font("Segoe UI", 9F);
            this.btnFilterSystem.Location = new System.Drawing.Point(410, 210);
            this.btnFilterSystem.Name = "btnFilterSystem";
            this.btnFilterSystem.Size = new System.Drawing.Size(125, 30);
            this.btnFilterSystem.TabIndex = 8;
            this.btnFilterSystem.Text = "System Events ⚙️";
            this.btnFilterSystem.UseVisualStyleBackColor = true;
            // 
            // pnlCard
            // 
            this.pnlCard.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.pnlCard.BackColor = System.Drawing.Color.White;
            this.pnlCard.Controls.Add(this.grid);
            this.pnlCard.Location = new System.Drawing.Point(30, 252);
            this.pnlCard.Name = "pnlCard";
            this.pnlCard.Padding = new System.Windows.Forms.Padding(1);
            this.pnlCard.Size = new System.Drawing.Size(970, 420);
            this.pnlCard.TabIndex = 10;
            // 
            // grid
            // 
            this.grid.AllowUserToAddRows = false;
            this.grid.AllowUserToDeleteRows = false;
            this.grid.AllowUserToResizeRows = false;
            this.grid.BackgroundColor = System.Drawing.Color.White;
            this.grid.BorderStyle = System.Windows.Forms.BorderStyle.None;
            this.grid.CellBorderStyle = System.Windows.Forms.DataGridViewCellBorderStyle.SingleHorizontal;
            this.grid.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            this.grid.Dock = System.Windows.Forms.DockStyle.Fill;
            this.grid.GridColor = System.Drawing.Color.FromArgb(241, 245, 249);
            this.grid.Location = new System.Drawing.Point(1, 1);
            this.grid.MultiSelect = false;
            this.grid.Name = "grid";
            this.grid.ReadOnly = true;
            this.grid.RowHeadersVisible = false;
            this.grid.SelectionMode = System.Windows.Forms.DataGridViewSelectionMode.FullRowSelect;
            this.grid.Size = new System.Drawing.Size(968, 418);
            this.grid.TabIndex = 0;
            // 
            // ActivitiesView
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 15F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.BackColor = System.Drawing.Color.FromArgb(248, 250, 252);
            this.Controls.Add(this.pnlCard);
            this.Controls.Add(this.txtSearch);
            this.Controls.Add(this.btnFilterSystem);
            this.Controls.Add(this.btnFilterMeetings);
            this.Controls.Add(this.btnFilterEmails);
            this.Controls.Add(this.btnFilterCalls);
            this.Controls.Add(this.btnFilterAll);
            this.Controls.Add(this.pnlKpiContainer);
            this.Controls.Add(this.btnAdd);
            this.Controls.Add(this.lblSubtitle);
            this.Controls.Add(this.lblTitle);
            this.Name = "ActivitiesView";
            this.Size = new System.Drawing.Size(1030, 700);
            this.pnlKpiContainer.ResumeLayout(false);
            this.pnlCard.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.grid)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();
        }

        #endregion
    }
}
