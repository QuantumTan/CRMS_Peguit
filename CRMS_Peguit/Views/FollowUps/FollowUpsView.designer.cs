namespace CRMS_Peguit.winforms.Views.FollowUps
{
    partial class FollowUpsView
    {
        private System.ComponentModel.IContainer components = null;

        private System.Windows.Forms.Label lblTitle = null!;
        private System.Windows.Forms.Label lblSubtitle = null!;
        private System.Windows.Forms.Button btnAdd = null!;
        private System.Windows.Forms.TableLayoutPanel pnlKpiContainer = null!;
        private CRMS_Peguit.winforms.Controls.KpiCard kpiOverdue = null!;
        private CRMS_Peguit.winforms.Controls.KpiCard kpiToday = null!;
        private CRMS_Peguit.winforms.Controls.KpiCard kpiUpcoming = null!;
        private CRMS_Peguit.winforms.Controls.KpiCard kpiCompleted = null!;

        private System.Windows.Forms.TextBox txtSearch = null!;
        private System.Windows.Forms.Button btnFilterOverdue = null!;
        private System.Windows.Forms.Button btnFilterToday = null!;
        private System.Windows.Forms.Button btnFilterUpcoming = null!;
        private System.Windows.Forms.Button btnFilterAll = null!;
        private System.Windows.Forms.Button btnFilterCompleted = null!;

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
            this.kpiOverdue = new CRMS_Peguit.winforms.Controls.KpiCard("OVERDUE", "overdue", CRMS_Peguit.winforms.Models.Services.Theme.StatusAlert);
            this.kpiToday = new CRMS_Peguit.winforms.Controls.KpiCard("DUE TODAY", "today", CRMS_Peguit.winforms.Models.Services.Theme.Primary);
            this.kpiUpcoming = new CRMS_Peguit.winforms.Controls.KpiCard("UPCOMING", "upcoming", CRMS_Peguit.winforms.Models.Services.Theme.StatusPending);
            this.kpiCompleted = new CRMS_Peguit.winforms.Controls.KpiCard("COMPLETED", "completed", CRMS_Peguit.winforms.Models.Services.Theme.StatusSuccess);

            this.txtSearch = new System.Windows.Forms.TextBox();
            this.btnFilterOverdue = new System.Windows.Forms.Button();
            this.btnFilterToday = new System.Windows.Forms.Button();
            this.btnFilterUpcoming = new System.Windows.Forms.Button();
            this.btnFilterAll = new System.Windows.Forms.Button();
            this.btnFilterCompleted = new System.Windows.Forms.Button();

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
            this.lblTitle.Size = new System.Drawing.Size(320, 37);
            this.lblTitle.TabIndex = 0;
            this.lblTitle.Text = "Follow-Up Reminders";
            this.lblTitle.UseMnemonic = false;
            // 
            // lblSubtitle
            // 
            this.lblSubtitle.AutoSize = true;
            this.lblSubtitle.Font = CRMS_Peguit.winforms.Services.UiStyleConstants.SubtitleFont;
            this.lblSubtitle.ForeColor = CRMS_Peguit.winforms.Models.Services.Theme.TextSecondary;
            this.lblSubtitle.Location = new System.Drawing.Point(CRMS_Peguit.winforms.Services.UiStyleConstants.PageMarginLeft + 2, 58);
            this.lblSubtitle.Name = "lblSubtitle";
            this.lblSubtitle.Size = new System.Drawing.Size(390, 17);
            this.lblSubtitle.TabIndex = 1;
            this.lblSubtitle.Text = "Forward-looking client touchpoints, calls, emails, and meetings";
            // 
            // btnAdd
            // 
            this.btnAdd.BackColor = CRMS_Peguit.winforms.Models.Services.Theme.Primary;
            this.btnAdd.Cursor = System.Windows.Forms.Cursors.Hand;
            this.btnAdd.FlatAppearance.BorderSize = 0;
            this.btnAdd.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnAdd.Font = new System.Drawing.Font("Segoe UI", 9.5F, System.Drawing.FontStyle.Bold);
            this.btnAdd.ForeColor = System.Drawing.Color.White;
            this.btnAdd.Location = new System.Drawing.Point(850, 24);
            this.btnAdd.Name = "btnAdd";
            this.btnAdd.Size = new System.Drawing.Size(150, 36);
            this.btnAdd.TabIndex = 2;
            this.btnAdd.Text = "+ New Follow-Up";
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
            this.pnlKpiContainer.Controls.Add(this.kpiOverdue, 0, 0);
            this.pnlKpiContainer.Controls.Add(this.kpiToday, 1, 0);
            this.pnlKpiContainer.Controls.Add(this.kpiUpcoming, 2, 0);
            this.pnlKpiContainer.Controls.Add(this.kpiCompleted, 3, 0);
            this.pnlKpiContainer.Location = new System.Drawing.Point(30, 88);
            this.pnlKpiContainer.Name = "pnlKpiContainer";
            this.pnlKpiContainer.RowCount = 1;
            this.pnlKpiContainer.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.pnlKpiContainer.Size = new System.Drawing.Size(970, 94);
            this.pnlKpiContainer.TabIndex = 3;
            // 
            // kpiOverdue
            // 
            this.kpiOverdue.Dock = System.Windows.Forms.DockStyle.Fill;
            this.kpiOverdue.Location = new System.Drawing.Point(0, 0);
            this.kpiOverdue.Margin = new System.Windows.Forms.Padding(0, 0, 8, 0);
            this.kpiOverdue.Name = "kpiOverdue";
            this.kpiOverdue.Size = new System.Drawing.Size(234, 94);
            this.kpiOverdue.TabIndex = 0;
            // 
            // kpiToday
            // 
            this.kpiToday.Dock = System.Windows.Forms.DockStyle.Fill;
            this.kpiToday.Location = new System.Drawing.Point(242, 0);
            this.kpiToday.Margin = new System.Windows.Forms.Padding(4, 0, 6, 0);
            this.kpiToday.Name = "kpiToday";
            this.kpiToday.Size = new System.Drawing.Size(232, 94);
            this.kpiToday.TabIndex = 1;
            // 
            // kpiUpcoming
            // 
            this.kpiUpcoming.Dock = System.Windows.Forms.DockStyle.Fill;
            this.kpiUpcoming.Location = new System.Drawing.Point(486, 0);
            this.kpiUpcoming.Margin = new System.Windows.Forms.Padding(6, 0, 4, 0);
            this.kpiUpcoming.Name = "kpiUpcoming";
            this.kpiUpcoming.Size = new System.Drawing.Size(232, 94);
            this.kpiUpcoming.TabIndex = 2;
            // 
            // kpiCompleted
            // 
            this.kpiCompleted.Dock = System.Windows.Forms.DockStyle.Fill;
            this.kpiCompleted.Location = new System.Drawing.Point(728, 0);
            this.kpiCompleted.Margin = new System.Windows.Forms.Padding(8, 0, 0, 0);
            this.kpiCompleted.Name = "kpiCompleted";
            this.kpiCompleted.Size = new System.Drawing.Size(242, 94);
            this.kpiCompleted.TabIndex = 3;
            // 
            // txtSearch
            // 
            this.txtSearch.BackColor = System.Drawing.Color.White;
            this.txtSearch.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.txtSearch.Font = new System.Drawing.Font("Segoe UI", 9.5F);
            this.txtSearch.ForeColor = System.Drawing.Color.FromArgb(15, 23, 42);
            this.txtSearch.Location = new System.Drawing.Point(30, 196);
            this.txtSearch.Name = "txtSearch";
            this.txtSearch.PlaceholderText = "Search by title, client, channel, or keyword...";
            this.txtSearch.Size = new System.Drawing.Size(360, 24);
            this.txtSearch.TabIndex = 4;
            // 
            // btnFilterOverdue
            // 
            this.btnFilterOverdue.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.btnFilterOverdue.BackColor = System.Drawing.Color.White;
            this.btnFilterOverdue.Cursor = System.Windows.Forms.Cursors.Hand;
            this.btnFilterOverdue.FlatAppearance.BorderColor = System.Drawing.Color.FromArgb(226, 232, 240);
            this.btnFilterOverdue.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnFilterOverdue.Font = new System.Drawing.Font("Segoe UI", 9F);
            this.btnFilterOverdue.ForeColor = System.Drawing.Color.FromArgb(71, 85, 105);
            this.btnFilterOverdue.Location = new System.Drawing.Point(544, 194);
            this.btnFilterOverdue.Name = "btnFilterOverdue";
            this.btnFilterOverdue.Size = new System.Drawing.Size(85, 28);
            this.btnFilterOverdue.TabIndex = 5;
            this.btnFilterOverdue.Text = "Overdue";
            this.btnFilterOverdue.UseVisualStyleBackColor = false;
            // 
            // btnFilterToday
            // 
            this.btnFilterToday.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.btnFilterToday.BackColor = System.Drawing.Color.White;
            this.btnFilterToday.Cursor = System.Windows.Forms.Cursors.Hand;
            this.btnFilterToday.FlatAppearance.BorderColor = System.Drawing.Color.FromArgb(226, 232, 240);
            this.btnFilterToday.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnFilterToday.Font = new System.Drawing.Font("Segoe UI", 9F);
            this.btnFilterToday.ForeColor = System.Drawing.Color.FromArgb(71, 85, 105);
            this.btnFilterToday.Location = new System.Drawing.Point(635, 194);
            this.btnFilterToday.Name = "btnFilterToday";
            this.btnFilterToday.Size = new System.Drawing.Size(75, 28);
            this.btnFilterToday.TabIndex = 6;
            this.btnFilterToday.Text = "Today";
            this.btnFilterToday.UseVisualStyleBackColor = false;
            // 
            // btnFilterUpcoming
            // 
            this.btnFilterUpcoming.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.btnFilterUpcoming.BackColor = System.Drawing.Color.White;
            this.btnFilterUpcoming.Cursor = System.Windows.Forms.Cursors.Hand;
            this.btnFilterUpcoming.FlatAppearance.BorderColor = System.Drawing.Color.FromArgb(226, 232, 240);
            this.btnFilterUpcoming.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnFilterUpcoming.Font = new System.Drawing.Font("Segoe UI", 9F);
            this.btnFilterUpcoming.ForeColor = System.Drawing.Color.FromArgb(71, 85, 105);
            this.btnFilterUpcoming.Location = new System.Drawing.Point(716, 194);
            this.btnFilterUpcoming.Name = "btnFilterUpcoming";
            this.btnFilterUpcoming.Size = new System.Drawing.Size(90, 28);
            this.btnFilterUpcoming.TabIndex = 7;
            this.btnFilterUpcoming.Text = "Upcoming";
            this.btnFilterUpcoming.UseVisualStyleBackColor = false;
            // 
            // btnFilterAll
            // 
            this.btnFilterAll.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.btnFilterAll.BackColor = System.Drawing.Color.FromArgb(15, 91, 158);
            this.btnFilterAll.Cursor = System.Windows.Forms.Cursors.Hand;
            this.btnFilterAll.FlatAppearance.BorderSize = 0;
            this.btnFilterAll.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnFilterAll.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Bold);
            this.btnFilterAll.ForeColor = System.Drawing.Color.White;
            this.btnFilterAll.Location = new System.Drawing.Point(812, 194);
            this.btnFilterAll.Name = "btnFilterAll";
            this.btnFilterAll.Size = new System.Drawing.Size(95, 28);
            this.btnFilterAll.TabIndex = 8;
            this.btnFilterAll.Text = "All Active";
            this.btnFilterAll.UseVisualStyleBackColor = false;
            // 
            // btnFilterCompleted
            // 
            this.btnFilterCompleted.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.btnFilterCompleted.BackColor = System.Drawing.Color.White;
            this.btnFilterCompleted.Cursor = System.Windows.Forms.Cursors.Hand;
            this.btnFilterCompleted.FlatAppearance.BorderColor = System.Drawing.Color.FromArgb(226, 232, 240);
            this.btnFilterCompleted.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnFilterCompleted.Font = new System.Drawing.Font("Segoe UI", 9F);
            this.btnFilterCompleted.ForeColor = System.Drawing.Color.FromArgb(71, 85, 105);
            this.btnFilterCompleted.Location = new System.Drawing.Point(913, 194);
            this.btnFilterCompleted.Name = "btnFilterCompleted";
            this.btnFilterCompleted.Size = new System.Drawing.Size(87, 28);
            this.btnFilterCompleted.TabIndex = 9;
            this.btnFilterCompleted.Text = "Completed";
            this.btnFilterCompleted.UseVisualStyleBackColor = false;
            // 
            // pnlCard
            // 
            this.pnlCard.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.pnlCard.BackColor = System.Drawing.Color.White;
            this.pnlCard.Controls.Add(this.grid);
            this.pnlCard.Location = new System.Drawing.Point(30, 234);
            this.pnlCard.Name = "pnlCard";
            this.pnlCard.Padding = new System.Windows.Forms.Padding(1);
            this.pnlCard.Size = new System.Drawing.Size(970, 410);
            this.pnlCard.TabIndex = 10;
            // 
            // grid
            // 
            this.grid.AllowUserToAddRows = false;
            this.grid.AllowUserToDeleteRows = false;
            this.grid.AllowUserToResizeRows = false;
            this.grid.BackgroundColor = System.Drawing.Color.White;
            this.grid.BorderStyle = System.Windows.Forms.BorderStyle.None;
            this.grid.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.DisableResizing;
            this.grid.Dock = System.Windows.Forms.DockStyle.Fill;
            this.grid.Location = new System.Drawing.Point(1, 1);
            this.grid.MultiSelect = false;
            this.grid.Name = "grid";
            this.grid.ReadOnly = true;
            this.grid.RowHeadersVisible = false;
            this.grid.SelectionMode = System.Windows.Forms.DataGridViewSelectionMode.FullRowSelect;
            this.grid.Size = new System.Drawing.Size(968, 408);
            this.grid.TabIndex = 0;
            // 
            // FollowUpsView
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 15F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.BackColor = System.Drawing.Color.FromArgb(243, 247, 250);
            this.Controls.Add(this.pnlCard);
            this.Controls.Add(this.btnFilterCompleted);
            this.Controls.Add(this.btnFilterAll);
            this.Controls.Add(this.btnFilterUpcoming);
            this.Controls.Add(this.btnFilterToday);
            this.Controls.Add(this.btnFilterOverdue);
            this.Controls.Add(this.txtSearch);
            this.Controls.Add(this.pnlKpiContainer);
            this.Controls.Add(this.btnAdd);
            this.Controls.Add(this.lblSubtitle);
            this.Controls.Add(this.lblTitle);
            this.Name = "FollowUpsView";
            this.Size = new System.Drawing.Size(1030, 670);
            this.pnlKpiContainer.ResumeLayout(false);
            this.pnlCard.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.grid)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();
        }

        #endregion
    }
}
