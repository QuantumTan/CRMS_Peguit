namespace CRMS_Peguit.winforms.Views.Deals
{
    partial class DealsView
    {
        private System.ComponentModel.IContainer components = null;

        private System.Windows.Forms.Label lblTitle = null!;
        private System.Windows.Forms.Label lblSubtitle = null!;
        private System.Windows.Forms.TableLayoutPanel pnlKpiContainer = null!;
        private CRMS_Peguit.winforms.Controls.KpiCard kpiTotal = null!;
        private CRMS_Peguit.winforms.Controls.KpiCard kpiOffer = null!;
        private CRMS_Peguit.winforms.Controls.KpiCard kpiContract = null!;
        private CRMS_Peguit.winforms.Controls.KpiCard kpiClosed = null!;
        private CRMS_Peguit.winforms.Controls.KpiCard kpiLost = null!;
        private System.Windows.Forms.TextBox txtSearch = null!;
        private System.Windows.Forms.Button btnFilterAll = null!;
        private System.Windows.Forms.Button btnFilterOffer = null!;
        private System.Windows.Forms.Button btnFilterContract = null!;
        private System.Windows.Forms.Button btnFilterClosed = null!;
        private System.Windows.Forms.Button btnFilterLost = null!;
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
            this.pnlKpiContainer = new System.Windows.Forms.TableLayoutPanel();
            this.kpiTotal = new CRMS_Peguit.winforms.Controls.KpiCard("TOTAL DEALS", "all", CRMS_Peguit.winforms.Models.Services.Theme.Primary, CRMS_Peguit.winforms.Models.Services.KpiIconType.Briefcase, "All registered");
            this.kpiOffer = new CRMS_Peguit.winforms.Controls.KpiCard("OFFER", "offer", System.Drawing.Color.FromArgb(217, 119, 6), CRMS_Peguit.winforms.Models.Services.KpiIconType.Target, "Initial offers");
            this.kpiContract = new CRMS_Peguit.winforms.Controls.KpiCard("CONTRACT", "contract", System.Drawing.Color.FromArgb(37, 99, 235), CRMS_Peguit.winforms.Models.Services.KpiIconType.Briefcase, "Under contract");
            this.kpiClosed = new CRMS_Peguit.winforms.Controls.KpiCard("CLOSED WON", "closed", System.Drawing.Color.FromArgb(22, 163, 74), CRMS_Peguit.winforms.Models.Services.KpiIconType.Currency, "Closed won");
            this.kpiLost = new CRMS_Peguit.winforms.Controls.KpiCard("LOST", "lost", System.Drawing.Color.FromArgb(220, 38, 38), CRMS_Peguit.winforms.Models.Services.KpiIconType.AlertTriangle, "Lost / cancelled");
            this.txtSearch = new System.Windows.Forms.TextBox();
            this.btnFilterAll = new System.Windows.Forms.Button();
            this.btnFilterOffer = new System.Windows.Forms.Button();
            this.btnFilterContract = new System.Windows.Forms.Button();
            this.btnFilterClosed = new System.Windows.Forms.Button();
            this.btnFilterLost = new System.Windows.Forms.Button();
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
            this.lblTitle.Font = new System.Drawing.Font("Segoe UI", 20F, System.Drawing.FontStyle.Bold);
            this.lblTitle.ForeColor = System.Drawing.Color.FromArgb(15, 23, 42);
            this.lblTitle.Location = new System.Drawing.Point(30, 20);
            this.lblTitle.Name = "lblTitle";
            this.lblTitle.Size = new System.Drawing.Size(86, 37);
            this.lblTitle.TabIndex = 0;
            this.lblTitle.Text = "Deals";
            // 
            // lblSubtitle
            // 
            this.lblSubtitle.AutoSize = true;
            this.lblSubtitle.Font = new System.Drawing.Font("Segoe UI", 9.5F);
            this.lblSubtitle.ForeColor = System.Drawing.Color.FromArgb(100, 116, 139);
            this.lblSubtitle.Location = new System.Drawing.Point(32, 58);
            this.lblSubtitle.Name = "lblSubtitle";
            this.lblSubtitle.Size = new System.Drawing.Size(120, 17);
            this.lblSubtitle.TabIndex = 1;
            this.lblSubtitle.Text = "0 total · ₱0.00 pipeline";
            // 
            // pnlKpiContainer
            // 
            this.pnlKpiContainer.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.pnlKpiContainer.ColumnCount = 5;
            this.pnlKpiContainer.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 20F));
            this.pnlKpiContainer.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 20F));
            this.pnlKpiContainer.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 20F));
            this.pnlKpiContainer.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 20F));
            this.pnlKpiContainer.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 20F));
            this.pnlKpiContainer.Controls.Add(this.kpiTotal, 0, 0);
            this.pnlKpiContainer.Controls.Add(this.kpiOffer, 1, 0);
            this.pnlKpiContainer.Controls.Add(this.kpiContract, 2, 0);
            this.pnlKpiContainer.Controls.Add(this.kpiClosed, 3, 0);
            this.pnlKpiContainer.Controls.Add(this.kpiLost, 4, 0);
            this.pnlKpiContainer.Location = new System.Drawing.Point(30, 90);
            this.pnlKpiContainer.Name = "pnlKpiContainer";
            this.pnlKpiContainer.RowCount = 1;
            this.pnlKpiContainer.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.pnlKpiContainer.Size = new System.Drawing.Size(970, 104);
            this.pnlKpiContainer.TabIndex = 2;
            // 
            // kpiTotal
            // 
            this.kpiTotal.Dock = System.Windows.Forms.DockStyle.Fill;
            this.kpiTotal.Location = new System.Drawing.Point(0, 0);
            this.kpiTotal.Margin = new System.Windows.Forms.Padding(0, 0, 8, 0);
            this.kpiTotal.Name = "kpiTotal";
            this.kpiTotal.Size = new System.Drawing.Size(186, 104);
            this.kpiTotal.TabIndex = 0;
            // 
            // kpiOffer
            // 
            this.kpiOffer.Dock = System.Windows.Forms.DockStyle.Fill;
            this.kpiOffer.Location = new System.Drawing.Point(194, 0);
            this.kpiOffer.Margin = new System.Windows.Forms.Padding(0, 0, 8, 0);
            this.kpiOffer.Name = "kpiOffer";
            this.kpiOffer.Size = new System.Drawing.Size(186, 104);
            this.kpiOffer.TabIndex = 1;
            // 
            // kpiContract
            // 
            this.kpiContract.Dock = System.Windows.Forms.DockStyle.Fill;
            this.kpiContract.Location = new System.Drawing.Point(388, 0);
            this.kpiContract.Margin = new System.Windows.Forms.Padding(0, 0, 8, 0);
            this.kpiContract.Name = "kpiContract";
            this.kpiContract.Size = new System.Drawing.Size(186, 104);
            this.kpiContract.TabIndex = 2;
            // 
            // kpiClosed
            // 
            this.kpiClosed.Dock = System.Windows.Forms.DockStyle.Fill;
            this.kpiClosed.Location = new System.Drawing.Point(582, 0);
            this.kpiClosed.Margin = new System.Windows.Forms.Padding(0, 0, 8, 0);
            this.kpiClosed.Name = "kpiClosed";
            this.kpiClosed.Size = new System.Drawing.Size(186, 104);
            this.kpiClosed.TabIndex = 3;
            // 
            // kpiLost
            // 
            this.kpiLost.Dock = System.Windows.Forms.DockStyle.Fill;
            this.kpiLost.Location = new System.Drawing.Point(776, 0);
            this.kpiLost.Margin = new System.Windows.Forms.Padding(0);
            this.kpiLost.Name = "kpiLost";
            this.kpiLost.Size = new System.Drawing.Size(194, 104);
            this.kpiLost.TabIndex = 4;
            // 
            // txtSearch
            // 
            this.txtSearch.BackColor = System.Drawing.Color.White;
            this.txtSearch.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.txtSearch.Font = new System.Drawing.Font("Segoe UI", 9.5F);
            this.txtSearch.ForeColor = System.Drawing.Color.FromArgb(15, 23, 42);
            this.txtSearch.Location = new System.Drawing.Point(30, 90);
            this.txtSearch.Name = "txtSearch";
            this.txtSearch.PlaceholderText = "Search deals by buyer, property, or agent...";
            this.txtSearch.Size = new System.Drawing.Size(360, 24);
            this.txtSearch.TabIndex = 2;
            // 
            // btnFilterAll
            // 
            this.btnFilterAll.BackColor = System.Drawing.Color.FromArgb(15, 91, 158);
            this.btnFilterAll.Cursor = System.Windows.Forms.Cursors.Hand;
            this.btnFilterAll.FlatAppearance.BorderSize = 0;
            this.btnFilterAll.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnFilterAll.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Bold);
            this.btnFilterAll.ForeColor = System.Drawing.Color.White;
            this.btnFilterAll.Location = new System.Drawing.Point(630, 88);
            this.btnFilterAll.Name = "btnFilterAll";
            this.btnFilterAll.Size = new System.Drawing.Size(55, 28);
            this.btnFilterAll.TabIndex = 3;
            this.btnFilterAll.Text = "All";
            this.btnFilterAll.UseVisualStyleBackColor = false;
            // 
            // btnFilterOffer
            // 
            this.btnFilterOffer.BackColor = System.Drawing.Color.White;
            this.btnFilterOffer.Cursor = System.Windows.Forms.Cursors.Hand;
            this.btnFilterOffer.FlatAppearance.BorderColor = System.Drawing.Color.FromArgb(226, 232, 240);
            this.btnFilterOffer.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnFilterOffer.Font = new System.Drawing.Font("Segoe UI", 9F);
            this.btnFilterOffer.ForeColor = System.Drawing.Color.FromArgb(71, 85, 105);
            this.btnFilterOffer.Location = new System.Drawing.Point(691, 88);
            this.btnFilterOffer.Name = "btnFilterOffer";
            this.btnFilterOffer.Size = new System.Drawing.Size(65, 28);
            this.btnFilterOffer.TabIndex = 4;
            this.btnFilterOffer.Text = "Offer";
            this.btnFilterOffer.UseVisualStyleBackColor = false;
            // 
            // btnFilterContract
            // 
            this.btnFilterContract.BackColor = System.Drawing.Color.White;
            this.btnFilterContract.Cursor = System.Windows.Forms.Cursors.Hand;
            this.btnFilterContract.FlatAppearance.BorderColor = System.Drawing.Color.FromArgb(226, 232, 240);
            this.btnFilterContract.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnFilterContract.Font = new System.Drawing.Font("Segoe UI", 9F);
            this.btnFilterContract.ForeColor = System.Drawing.Color.FromArgb(71, 85, 105);
            this.btnFilterContract.Location = new System.Drawing.Point(762, 88);
            this.btnFilterContract.Name = "btnFilterContract";
            this.btnFilterContract.Size = new System.Drawing.Size(80, 28);
            this.btnFilterContract.TabIndex = 5;
            this.btnFilterContract.Text = "Contract";
            this.btnFilterContract.UseVisualStyleBackColor = false;
            // 
            // btnFilterClosed
            // 
            this.btnFilterClosed.BackColor = System.Drawing.Color.White;
            this.btnFilterClosed.Cursor = System.Windows.Forms.Cursors.Hand;
            this.btnFilterClosed.FlatAppearance.BorderColor = System.Drawing.Color.FromArgb(226, 232, 240);
            this.btnFilterClosed.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnFilterClosed.Font = new System.Drawing.Font("Segoe UI", 9F);
            this.btnFilterClosed.ForeColor = System.Drawing.Color.FromArgb(71, 85, 105);
            this.btnFilterClosed.Location = new System.Drawing.Point(848, 88);
            this.btnFilterClosed.Name = "btnFilterClosed";
            this.btnFilterClosed.Size = new System.Drawing.Size(75, 28);
            this.btnFilterClosed.TabIndex = 6;
            this.btnFilterClosed.Text = "Closed";
            this.btnFilterClosed.UseVisualStyleBackColor = false;
            // 
            // btnFilterLost
            // 
            this.btnFilterLost.BackColor = System.Drawing.Color.White;
            this.btnFilterLost.Cursor = System.Windows.Forms.Cursors.Hand;
            this.btnFilterLost.FlatAppearance.BorderColor = System.Drawing.Color.FromArgb(226, 232, 240);
            this.btnFilterLost.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnFilterLost.Font = new System.Drawing.Font("Segoe UI", 9F);
            this.btnFilterLost.ForeColor = System.Drawing.Color.FromArgb(71, 85, 105);
            this.btnFilterLost.Location = new System.Drawing.Point(929, 88);
            this.btnFilterLost.Name = "btnFilterLost";
            this.btnFilterLost.Size = new System.Drawing.Size(71, 28);
            this.btnFilterLost.TabIndex = 7;
            this.btnFilterLost.Text = "Lost";
            this.btnFilterLost.UseVisualStyleBackColor = false;
            this.pnlCard.BackColor = System.Drawing.Color.White;
            this.pnlCard.Controls.Add(this.grid);
            this.pnlCard.Location = new System.Drawing.Point(30, 126);
            this.pnlCard.Name = "pnlCard";
            this.pnlCard.Padding = new System.Windows.Forms.Padding(1);
            this.pnlCard.Size = new System.Drawing.Size(970, 520);
            this.pnlCard.TabIndex = 8;
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
            this.grid.Size = new System.Drawing.Size(968, 518);
            this.grid.TabIndex = 0;
            // 
            // DealsView
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 15F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.BackColor = System.Drawing.Color.FromArgb(244, 247, 251);
            this.Controls.Add(this.pnlKpiContainer);
            this.Controls.Add(this.pnlCard);
            this.Controls.Add(this.btnFilterLost);
            this.Controls.Add(this.btnFilterClosed);
            this.Controls.Add(this.btnFilterContract);
            this.Controls.Add(this.btnFilterOffer);
            this.Controls.Add(this.btnFilterAll);
            this.Controls.Add(this.txtSearch);
            this.Controls.Add(this.lblSubtitle);
            this.Controls.Add(this.lblTitle);
            this.Name = "DealsView";
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
