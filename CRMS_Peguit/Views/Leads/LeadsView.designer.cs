namespace CRMS_Peguit.winforms.Views.Leads
{
    partial class LeadsView
    {
        private System.ComponentModel.IContainer components = null;

        private System.Windows.Forms.Label lblTitle = null!;
        private System.Windows.Forms.Label lblSubtitle = null!;
        private System.Windows.Forms.Button btnAdd = null!;
        private System.Windows.Forms.TextBox txtSearch = null!;
        private System.Windows.Forms.Button btnFilterAll = null!;
        private System.Windows.Forms.Button btnFilterNew = null!;
        private System.Windows.Forms.Button btnFilterContacted = null!;
        private System.Windows.Forms.Button btnFilterQualified = null!;
        private System.Windows.Forms.Button btnFilterConverted = null!;
        private System.Windows.Forms.Panel pnlCard = null!;
        private System.Windows.Forms.DataGridView grid = null!;

        // Backward compatibility
        private System.Windows.Forms.ComboBox cmbFilter = null!;

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
            this.txtSearch = new System.Windows.Forms.TextBox();
            this.btnFilterAll = new System.Windows.Forms.Button();
            this.btnFilterNew = new System.Windows.Forms.Button();
            this.btnFilterContacted = new System.Windows.Forms.Button();
            this.btnFilterQualified = new System.Windows.Forms.Button();
            this.btnFilterConverted = new System.Windows.Forms.Button();
            this.pnlCard = new System.Windows.Forms.Panel();
            this.grid = new System.Windows.Forms.DataGridView();
            this.cmbFilter = new System.Windows.Forms.ComboBox();
            this.pnlCard.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.grid)).BeginInit();
            this.SuspendLayout();
            // 
            // lblTitle
            // 
            this.lblTitle.AutoSize = true;
            this.lblTitle.Font = new System.Drawing.Font("Segoe UI", 20F, System.Drawing.FontStyle.Bold);
            this.lblTitle.ForeColor = CRMS_Peguit.winforms.Models.Services.Theme.TextPrimary;
            this.lblTitle.Location = new System.Drawing.Point(30, 20);
            this.lblTitle.Name = "lblTitle";
            this.lblTitle.Size = new System.Drawing.Size(90, 37);
            this.lblTitle.TabIndex = 0;
            this.lblTitle.Text = "Leads";
            // 
            // lblSubtitle
            // 
            this.lblSubtitle.AutoSize = true;
            this.lblSubtitle.Font = new System.Drawing.Font("Segoe UI", 9.5F);
            this.lblSubtitle.ForeColor = CRMS_Peguit.winforms.Models.Services.Theme.TextSecondary;
            this.lblSubtitle.Location = new System.Drawing.Point(32, 58);
            this.lblSubtitle.Name = "lblSubtitle";
            this.lblSubtitle.Size = new System.Drawing.Size(120, 17);
            this.lblSubtitle.TabIndex = 1;
            this.lblSubtitle.Text = "0 total · 0 qualified";
            // 
            // btnAdd
            // 
            this.btnAdd.BackColor = CRMS_Peguit.winforms.Models.Services.Theme.Primary;
            this.btnAdd.Cursor = System.Windows.Forms.Cursors.Hand;
            this.btnAdd.FlatAppearance.BorderSize = 0;
            this.btnAdd.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnAdd.Font = new System.Drawing.Font("Segoe UI", 9.5F, System.Drawing.FontStyle.Bold);
            this.btnAdd.ForeColor = System.Drawing.Color.White;
            this.btnAdd.Location = new System.Drawing.Point(880, 24);
            this.btnAdd.Name = "btnAdd";
            this.btnAdd.Size = new System.Drawing.Size(120, 36);
            this.btnAdd.TabIndex = 2;
            this.btnAdd.Text = "+ Add Lead";
            this.btnAdd.UseVisualStyleBackColor = false;
            // 
            // txtSearch
            // 
            this.txtSearch.BackColor = System.Drawing.Color.White;
            this.txtSearch.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.txtSearch.Font = new System.Drawing.Font("Segoe UI", 9.5F);
            this.txtSearch.ForeColor = System.Drawing.Color.FromArgb(15, 23, 42);
            this.txtSearch.Location = new System.Drawing.Point(32, 80);
            this.txtSearch.Name = "txtSearch";
            this.txtSearch.PlaceholderText = "🔍 Search leads by name, email, phone...";
            this.txtSearch.Size = new System.Drawing.Size(340, 24);
            this.txtSearch.TabIndex = 3;
            // 
            // btnFilterAll
            // 
            this.btnFilterAll.BackColor = System.Drawing.Color.FromArgb(15, 91, 158);
            this.btnFilterAll.Cursor = System.Windows.Forms.Cursors.Hand;
            this.btnFilterAll.FlatAppearance.BorderSize = 0;
            this.btnFilterAll.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnFilterAll.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Bold);
            this.btnFilterAll.ForeColor = System.Drawing.Color.White;
            this.btnFilterAll.Location = new System.Drawing.Point(595, 88);
            this.btnFilterAll.Name = "btnFilterAll";
            this.btnFilterAll.Size = new System.Drawing.Size(55, 28);
            this.btnFilterAll.TabIndex = 4;
            this.btnFilterAll.Text = "All";
            this.btnFilterAll.UseVisualStyleBackColor = false;
            // 
            // btnFilterNew
            // 
            this.btnFilterNew.BackColor = System.Drawing.Color.White;
            this.btnFilterNew.Cursor = System.Windows.Forms.Cursors.Hand;
            this.btnFilterNew.FlatAppearance.BorderColor = System.Drawing.Color.FromArgb(226, 232, 240);
            this.btnFilterNew.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnFilterNew.Font = new System.Drawing.Font("Segoe UI", 9F);
            this.btnFilterNew.ForeColor = System.Drawing.Color.FromArgb(71, 85, 105);
            this.btnFilterNew.Location = new System.Drawing.Point(656, 88);
            this.btnFilterNew.Name = "btnFilterNew";
            this.btnFilterNew.Size = new System.Drawing.Size(65, 28);
            this.btnFilterNew.TabIndex = 5;
            this.btnFilterNew.Text = "New";
            this.btnFilterNew.UseVisualStyleBackColor = false;
            // 
            // btnFilterContacted
            // 
            this.btnFilterContacted.BackColor = System.Drawing.Color.White;
            this.btnFilterContacted.Cursor = System.Windows.Forms.Cursors.Hand;
            this.btnFilterContacted.FlatAppearance.BorderColor = System.Drawing.Color.FromArgb(226, 232, 240);
            this.btnFilterContacted.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnFilterContacted.Font = new System.Drawing.Font("Segoe UI", 9F);
            this.btnFilterContacted.ForeColor = System.Drawing.Color.FromArgb(71, 85, 105);
            this.btnFilterContacted.Location = new System.Drawing.Point(727, 88);
            this.btnFilterContacted.Name = "btnFilterContacted";
            this.btnFilterContacted.Size = new System.Drawing.Size(95, 28);
            this.btnFilterContacted.TabIndex = 6;
            this.btnFilterContacted.Text = "Contacted";
            this.btnFilterContacted.UseVisualStyleBackColor = false;
            // 
            // btnFilterQualified
            // 
            this.btnFilterQualified.BackColor = System.Drawing.Color.White;
            this.btnFilterQualified.Cursor = System.Windows.Forms.Cursors.Hand;
            this.btnFilterQualified.FlatAppearance.BorderColor = System.Drawing.Color.FromArgb(226, 232, 240);
            this.btnFilterQualified.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnFilterQualified.Font = new System.Drawing.Font("Segoe UI", 9F);
            this.btnFilterQualified.ForeColor = System.Drawing.Color.FromArgb(71, 85, 105);
            this.btnFilterQualified.Location = new System.Drawing.Point(828, 88);
            this.btnFilterQualified.Name = "btnFilterQualified";
            this.btnFilterQualified.Size = new System.Drawing.Size(85, 28);
            this.btnFilterQualified.TabIndex = 7;
            this.btnFilterQualified.Text = "Qualified";
            this.btnFilterQualified.UseVisualStyleBackColor = false;
            // 
            // btnFilterConverted
            // 
            this.btnFilterConverted.BackColor = System.Drawing.Color.White;
            this.btnFilterConverted.Cursor = System.Windows.Forms.Cursors.Hand;
            this.btnFilterConverted.FlatAppearance.BorderColor = System.Drawing.Color.FromArgb(226, 232, 240);
            this.btnFilterConverted.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnFilterConverted.Font = new System.Drawing.Font("Segoe UI", 9F);
            this.btnFilterConverted.ForeColor = System.Drawing.Color.FromArgb(71, 85, 105);
            this.btnFilterConverted.Location = new System.Drawing.Point(919, 88);
            this.btnFilterConverted.Name = "btnFilterConverted";
            this.btnFilterConverted.Size = new System.Drawing.Size(81, 28);
            this.btnFilterConverted.TabIndex = 8;
            this.btnFilterConverted.Text = "Converted";
            this.btnFilterConverted.UseVisualStyleBackColor = false;
            // 
            // pnlCard
            // 
            this.pnlCard.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.pnlCard.BackColor = System.Drawing.Color.White;
            this.pnlCard.Controls.Add(this.grid);
            this.pnlCard.Location = new System.Drawing.Point(30, 126);
            this.pnlCard.Name = "pnlCard";
            this.pnlCard.Padding = new System.Windows.Forms.Padding(1);
            this.pnlCard.Size = new System.Drawing.Size(970, 520);
            this.pnlCard.TabIndex = 9;
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
            // cmbFilter
            // 
            this.cmbFilter.Location = new System.Drawing.Point(0, 0);
            this.cmbFilter.Name = "cmbFilter";
            this.cmbFilter.Size = new System.Drawing.Size(0, 21);
            this.cmbFilter.TabIndex = 11;
            this.cmbFilter.Visible = false;
            // 
            // LeadsView
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 15F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.BackColor = System.Drawing.Color.FromArgb(244, 247, 251);
            this.Controls.Add(this.pnlCard);
            this.Controls.Add(this.btnFilterConverted);
            this.Controls.Add(this.btnFilterQualified);
            this.Controls.Add(this.btnFilterContacted);
            this.Controls.Add(this.btnFilterNew);
            this.Controls.Add(this.btnFilterAll);
            this.Controls.Add(this.txtSearch);
            this.Controls.Add(this.btnAdd);
            this.Controls.Add(this.lblSubtitle);
            this.Controls.Add(this.lblTitle);
            this.Controls.Add(this.cmbFilter);
            this.Name = "LeadsView";
            this.Size = new System.Drawing.Size(1030, 700);
            this.pnlCard.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.grid)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();
        }

        #endregion
    }
}
