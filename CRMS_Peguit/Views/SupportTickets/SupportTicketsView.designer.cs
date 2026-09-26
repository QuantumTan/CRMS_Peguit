namespace CRMS_Peguit.winforms.Views.SupportTickets
{
    partial class SupportTicketsView
    {
        private System.ComponentModel.IContainer components = null;

        private System.Windows.Forms.Label lblTitle = null!;
        private System.Windows.Forms.Label lblSubtitle = null!;
        private System.Windows.Forms.Button btnAdd = null!;
        private System.Windows.Forms.TableLayoutPanel pnlKpiContainer = null!;
        private CRMS_Peguit.winforms.Controls.KpiCard kpiTotal = null!;
        private CRMS_Peguit.winforms.Controls.KpiCard kpiOpen = null!;
        private CRMS_Peguit.winforms.Controls.KpiCard kpiInProgress = null!;
        private CRMS_Peguit.winforms.Controls.KpiCard kpiOverdue = null!;
        private System.Windows.Forms.TextBox txtSearch = null!;
        private System.Windows.Forms.Button btnFilterAll = null!;
        private System.Windows.Forms.Button btnFilterOpen = null!;
        private System.Windows.Forms.Button btnFilterInProgress = null!;
        private System.Windows.Forms.Button btnFilterResolved = null!;
        private System.Windows.Forms.Button btnFilterOverdue = null!;
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
            this.kpiTotal = new CRMS_Peguit.winforms.Controls.KpiCard("TOTAL TICKETS", "total", CRMS_Peguit.winforms.Models.Services.Theme.Primary);
            this.kpiOpen = new CRMS_Peguit.winforms.Controls.KpiCard("OPEN TICKETS", "open", CRMS_Peguit.winforms.Models.Services.Theme.StatusInfo);
            this.kpiInProgress = new CRMS_Peguit.winforms.Controls.KpiCard("IN PROGRESS", "in_progress", CRMS_Peguit.winforms.Models.Services.Theme.StatusPending);
            this.kpiOverdue = new CRMS_Peguit.winforms.Controls.KpiCard("OVERDUE (SLA)", "overdue", CRMS_Peguit.winforms.Models.Services.Theme.StatusAlert);
            this.txtSearch = new System.Windows.Forms.TextBox();
            this.btnFilterAll = new System.Windows.Forms.Button();
            this.btnFilterOpen = new System.Windows.Forms.Button();
            this.btnFilterInProgress = new System.Windows.Forms.Button();
            this.btnFilterResolved = new System.Windows.Forms.Button();
            this.btnFilterOverdue = new System.Windows.Forms.Button();
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
            this.lblTitle.Size = new System.Drawing.Size(215, 37);
            this.lblTitle.TabIndex = 0;
            this.lblTitle.Text = "Support Tickets";
            // 
            // lblSubtitle
            // 
            this.lblSubtitle.AutoSize = true;
            this.lblSubtitle.Font = CRMS_Peguit.winforms.Services.UiStyleConstants.SubtitleFont;
            this.lblSubtitle.ForeColor = CRMS_Peguit.winforms.Models.Services.Theme.TextSecondary;
            this.lblSubtitle.Location = new System.Drawing.Point(CRMS_Peguit.winforms.Services.UiStyleConstants.PageMarginLeft + 2, 58);
            this.lblSubtitle.Name = "lblSubtitle";
            this.lblSubtitle.Size = new System.Drawing.Size(220, 17);
            this.lblSubtitle.TabIndex = 1;
            this.lblSubtitle.Text = "Customer post-sale service tracking";
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
            this.btnAdd.Text = "+ New Ticket";
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
            this.pnlKpiContainer.Controls.Add(this.kpiOpen, 1, 0);
            this.pnlKpiContainer.Controls.Add(this.kpiInProgress, 2, 0);
            this.pnlKpiContainer.Controls.Add(this.kpiOverdue, 3, 0);
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
            // kpiOpen
            // 
            this.kpiOpen.Dock = System.Windows.Forms.DockStyle.Fill;
            this.kpiOpen.Location = new System.Drawing.Point(242, 0);
            this.kpiOpen.Margin = new System.Windows.Forms.Padding(4, 0, 6, 0);
            this.kpiOpen.Name = "kpiOpen";
            this.kpiOpen.Size = new System.Drawing.Size(232, 104);
            this.kpiOpen.TabIndex = 1;
            // 
            // kpiInProgress
            // 
            this.kpiInProgress.Dock = System.Windows.Forms.DockStyle.Fill;
            this.kpiInProgress.Location = new System.Drawing.Point(486, 0);
            this.kpiInProgress.Margin = new System.Windows.Forms.Padding(6, 0, 4, 0);
            this.kpiInProgress.Name = "kpiInProgress";
            this.kpiInProgress.Size = new System.Drawing.Size(232, 104);
            this.kpiInProgress.TabIndex = 2;
            // 
            // kpiOverdue
            // 
            this.kpiOverdue.Dock = System.Windows.Forms.DockStyle.Fill;
            this.kpiOverdue.Location = new System.Drawing.Point(728, 0);
            this.kpiOverdue.Margin = new System.Windows.Forms.Padding(8, 0, 0, 0);
            this.kpiOverdue.Name = "kpiOverdue";
            this.kpiOverdue.Size = new System.Drawing.Size(242, 104);
            this.kpiOverdue.TabIndex = 3;
            // 
            // txtSearch
            // 
            this.txtSearch.BackColor = System.Drawing.Color.White;
            this.txtSearch.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.txtSearch.Font = new System.Drawing.Font("Segoe UI", 9.5F);
            this.txtSearch.ForeColor = System.Drawing.Color.FromArgb(15, 23, 42);
            this.txtSearch.Location = new System.Drawing.Point(30, 192);
            this.txtSearch.Name = "txtSearch";
            this.txtSearch.PlaceholderText = "Search by ticket #, client, category, or keyword...";
            this.txtSearch.Size = new System.Drawing.Size(360, 24);
            this.txtSearch.TabIndex = 4;
            // 
            // btnFilterAll
            // 
            this.btnFilterAll.BackColor = System.Drawing.Color.FromArgb(15, 91, 158);
            this.btnFilterAll.Cursor = System.Windows.Forms.Cursors.Hand;
            this.btnFilterAll.FlatAppearance.BorderSize = 0;
            this.btnFilterAll.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnFilterAll.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Bold);
            this.btnFilterAll.ForeColor = System.Drawing.Color.White;
            this.btnFilterAll.Location = new System.Drawing.Point(620, 190);
            this.btnFilterAll.Name = "btnFilterAll";
            this.btnFilterAll.Size = new System.Drawing.Size(55, 28);
            this.btnFilterAll.TabIndex = 5;
            this.btnFilterAll.Text = "All";
            this.btnFilterAll.UseVisualStyleBackColor = false;
            // 
            // btnFilterOpen
            // 
            this.btnFilterOpen.BackColor = System.Drawing.Color.White;
            this.btnFilterOpen.Cursor = System.Windows.Forms.Cursors.Hand;
            this.btnFilterOpen.FlatAppearance.BorderColor = System.Drawing.Color.FromArgb(226, 232, 240);
            this.btnFilterOpen.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnFilterOpen.Font = new System.Drawing.Font("Segoe UI", 9F);
            this.btnFilterOpen.ForeColor = System.Drawing.Color.FromArgb(71, 85, 105);
            this.btnFilterOpen.Location = new System.Drawing.Point(681, 190);
            this.btnFilterOpen.Name = "btnFilterOpen";
            this.btnFilterOpen.Size = new System.Drawing.Size(65, 28);
            this.btnFilterOpen.TabIndex = 6;
            this.btnFilterOpen.Text = "Open";
            this.btnFilterOpen.UseVisualStyleBackColor = false;
            // 
            // btnFilterInProgress
            // 
            this.btnFilterInProgress.BackColor = System.Drawing.Color.White;
            this.btnFilterInProgress.Cursor = System.Windows.Forms.Cursors.Hand;
            this.btnFilterInProgress.FlatAppearance.BorderColor = System.Drawing.Color.FromArgb(226, 232, 240);
            this.btnFilterInProgress.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnFilterInProgress.Font = new System.Drawing.Font("Segoe UI", 9F);
            this.btnFilterInProgress.ForeColor = System.Drawing.Color.FromArgb(71, 85, 105);
            this.btnFilterInProgress.Location = new System.Drawing.Point(752, 190);
            this.btnFilterInProgress.Name = "btnFilterInProgress";
            this.btnFilterInProgress.Size = new System.Drawing.Size(95, 28);
            this.btnFilterInProgress.TabIndex = 7;
            this.btnFilterInProgress.Text = "In Progress";
            this.btnFilterInProgress.UseVisualStyleBackColor = false;
            // 
            // btnFilterResolved
            // 
            this.btnFilterResolved.BackColor = System.Drawing.Color.White;
            this.btnFilterResolved.Cursor = System.Windows.Forms.Cursors.Hand;
            this.btnFilterResolved.FlatAppearance.BorderColor = System.Drawing.Color.FromArgb(226, 232, 240);
            this.btnFilterResolved.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnFilterResolved.Font = new System.Drawing.Font("Segoe UI", 9F);
            this.btnFilterResolved.ForeColor = System.Drawing.Color.FromArgb(71, 85, 105);
            this.btnFilterResolved.Location = new System.Drawing.Point(853, 190);
            this.btnFilterResolved.Name = "btnFilterResolved";
            this.btnFilterResolved.Size = new System.Drawing.Size(75, 28);
            this.btnFilterResolved.TabIndex = 8;
            this.btnFilterResolved.Text = "Resolved";
            this.btnFilterResolved.UseVisualStyleBackColor = false;
            // 
            // btnFilterOverdue
            // 
            this.btnFilterOverdue.BackColor = System.Drawing.Color.White;
            this.btnFilterOverdue.Cursor = System.Windows.Forms.Cursors.Hand;
            this.btnFilterOverdue.FlatAppearance.BorderColor = System.Drawing.Color.FromArgb(226, 232, 240);
            this.btnFilterOverdue.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnFilterOverdue.Font = new System.Drawing.Font("Segoe UI", 9F);
            this.btnFilterOverdue.ForeColor = System.Drawing.Color.FromArgb(185, 28, 28);
            this.btnFilterOverdue.Location = new System.Drawing.Point(934, 190);
            this.btnFilterOverdue.Name = "btnFilterOverdue";
            this.btnFilterOverdue.Size = new System.Drawing.Size(70, 28);
            this.btnFilterOverdue.TabIndex = 9;
            this.btnFilterOverdue.Text = "Overdue";
            this.btnFilterOverdue.UseVisualStyleBackColor = false;
            // 
            // pnlCard
            // 
            this.pnlCard.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.pnlCard.BackColor = System.Drawing.Color.White;
            this.pnlCard.Controls.Add(this.grid);
            this.pnlCard.Location = new System.Drawing.Point(30, 230);
            this.pnlCard.Name = "pnlCard";
            this.pnlCard.Padding = new System.Windows.Forms.Padding(1);
            this.pnlCard.Size = new System.Drawing.Size(970, 440);
            this.pnlCard.TabIndex = 10;
            // 
            // grid
            // 
            this.grid.AllowUserToAddRows = false;
            this.grid.AllowUserToDeleteRows = false;
            this.grid.AutoSizeColumnsMode = System.Windows.Forms.DataGridViewAutoSizeColumnsMode.Fill;
            this.grid.BackgroundColor = System.Drawing.Color.White;
            this.grid.BorderStyle = System.Windows.Forms.BorderStyle.None;
            this.grid.CellBorderStyle = System.Windows.Forms.DataGridViewCellBorderStyle.SingleHorizontal;
            this.grid.ColumnHeadersBorderStyle = System.Windows.Forms.DataGridViewHeaderBorderStyle.None;
            this.grid.ColumnHeadersHeight = 44;
            this.grid.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.DisableResizing;
            this.grid.Dock = System.Windows.Forms.DockStyle.Fill;
            this.grid.GridColor = System.Drawing.Color.FromArgb(241, 245, 249);
            this.grid.Location = new System.Drawing.Point(1, 1);
            this.grid.MultiSelect = false;
            this.grid.Name = "grid";
            this.grid.ReadOnly = true;
            this.grid.RowHeadersVisible = false;
            this.grid.RowTemplate.Height = 52;
            this.grid.SelectionMode = System.Windows.Forms.DataGridViewSelectionMode.FullRowSelect;
            this.grid.Size = new System.Drawing.Size(968, 438);
            this.grid.TabIndex = 0;
            // 
            // SupportTicketsView
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 15F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.BackColor = System.Drawing.Color.FromArgb(244, 247, 251);
            this.Controls.Add(this.pnlCard);
            this.Controls.Add(this.btnFilterOverdue);
            this.Controls.Add(this.btnFilterResolved);
            this.Controls.Add(this.btnFilterInProgress);
            this.Controls.Add(this.btnFilterOpen);
            this.Controls.Add(this.btnFilterAll);
            this.Controls.Add(this.txtSearch);
            this.Controls.Add(this.pnlKpiContainer);
            this.Controls.Add(this.btnAdd);
            this.Controls.Add(this.lblSubtitle);
            this.Controls.Add(this.lblTitle);
            this.Name = "SupportTicketsView";
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
