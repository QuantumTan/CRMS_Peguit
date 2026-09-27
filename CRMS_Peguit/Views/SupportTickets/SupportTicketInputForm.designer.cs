namespace CRMS_Peguit.winforms.Views.SupportTickets
{
    partial class SupportTicketInputForm
    {
        private System.ComponentModel.IContainer components = null;

        private System.Windows.Forms.Label lblHeaderTitle = null!;
        private System.Windows.Forms.Label lblHeaderSub = null!;
        private System.Windows.Forms.Label lblCustomer = null!;
        private System.Windows.Forms.ComboBox cmbCustomer = null!;
        private System.Windows.Forms.Label lblCategory = null!;
        private System.Windows.Forms.ComboBox cmbCategory = null!;
        private System.Windows.Forms.Label lblPriority = null!;
        private System.Windows.Forms.ComboBox cmbPriority = null!;
        private System.Windows.Forms.Label lblDescription = null!;
        private System.Windows.Forms.TextBox txtDescription = null!;
        private System.Windows.Forms.Button btnSave = null!;
        private System.Windows.Forms.Button btnCancel = null!;
        private System.Windows.Forms.Panel pnlHeader = null!;
        private System.Windows.Forms.Panel pnlFooter = null!;
        private System.Windows.Forms.Panel pnlContent = null!;

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
            this.lblHeaderTitle = new System.Windows.Forms.Label();
            this.lblHeaderSub = new System.Windows.Forms.Label();
            this.lblCustomer = new System.Windows.Forms.Label();
            this.cmbCustomer = new System.Windows.Forms.ComboBox();
            this.lblCategory = new System.Windows.Forms.Label();
            this.cmbCategory = new System.Windows.Forms.ComboBox();
            this.lblPriority = new System.Windows.Forms.Label();
            this.cmbPriority = new System.Windows.Forms.ComboBox();
            this.lblDescription = new System.Windows.Forms.Label();
            this.txtDescription = new System.Windows.Forms.TextBox();
            this.btnSave = new System.Windows.Forms.Button();
            this.btnCancel = new System.Windows.Forms.Button();
            this.pnlHeader = new System.Windows.Forms.Panel();
            this.pnlFooter = new System.Windows.Forms.Panel();
            this.pnlContent = new System.Windows.Forms.Panel();
            this.pnlHeader.SuspendLayout();
            this.pnlFooter.SuspendLayout();
            this.pnlContent.SuspendLayout();
            this.SuspendLayout();
            // 
            // pnlHeader
            // 
            this.pnlHeader.BackColor = System.Drawing.Color.White;
            this.pnlHeader.Controls.Add(this.lblHeaderTitle);
            this.pnlHeader.Controls.Add(this.lblHeaderSub);
            this.pnlHeader.Dock = System.Windows.Forms.DockStyle.Top;
            this.pnlHeader.Location = new System.Drawing.Point(0, 0);
            this.pnlHeader.Name = "pnlHeader";
            this.pnlHeader.Padding = new System.Windows.Forms.Padding(24, 16, 24, 16);
            this.pnlHeader.Size = new System.Drawing.Size(540, 72);
            this.pnlHeader.TabIndex = 0;
            // 
            // lblHeaderTitle
            // 
            this.lblHeaderTitle.AutoSize = true;
            this.lblHeaderTitle.Font = new System.Drawing.Font("Segoe UI", 14F, System.Drawing.FontStyle.Bold);
            this.lblHeaderTitle.ForeColor = System.Drawing.Color.FromArgb(15, 23, 42);
            this.lblHeaderTitle.Location = new System.Drawing.Point(20, 14);
            this.lblHeaderTitle.Name = "lblHeaderTitle";
            this.lblHeaderTitle.Size = new System.Drawing.Size(193, 25);
            this.lblHeaderTitle.TabIndex = 0;
            this.lblHeaderTitle.Text = "Log Support Ticket";
            // 
            // lblHeaderSub
            // 
            this.lblHeaderSub.AutoSize = true;
            this.lblHeaderSub.Font = new System.Drawing.Font("Segoe UI", 9F);
            this.lblHeaderSub.ForeColor = System.Drawing.Color.FromArgb(100, 116, 139);
            this.lblHeaderSub.Location = new System.Drawing.Point(22, 42);
            this.lblHeaderSub.Name = "lblHeaderSub";
            this.lblHeaderSub.Size = new System.Drawing.Size(262, 15);
            this.lblHeaderSub.TabIndex = 1;
            this.lblHeaderSub.Text = "File a customer support or post-sale service request";
            // 
            // lblCustomer
            // 
            this.lblCustomer.AutoSize = true;
            this.lblCustomer.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Bold);
            this.lblCustomer.ForeColor = System.Drawing.Color.FromArgb(71, 85, 105);
            this.lblCustomer.Location = new System.Drawing.Point(24, 16);
            this.lblCustomer.Name = "lblCustomer";
            this.lblCustomer.Size = new System.Drawing.Size(71, 15);
            this.lblCustomer.TabIndex = 1;
            this.lblCustomer.Text = "CUSTOMER";
            // 
            // cmbCustomer
            // 
            this.cmbCustomer.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cmbCustomer.Font = new System.Drawing.Font("Segoe UI", 9.5F);
            this.cmbCustomer.FormattingEnabled = true;
            this.cmbCustomer.Location = new System.Drawing.Point(24, 38);
            this.cmbCustomer.Name = "cmbCustomer";
            this.cmbCustomer.Size = new System.Drawing.Size(470, 25);
            this.cmbCustomer.TabIndex = 2;
            // 
            // lblCategory
            // 
            this.lblCategory.AutoSize = true;
            this.lblCategory.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Bold);
            this.lblCategory.ForeColor = System.Drawing.Color.FromArgb(71, 85, 105);
            this.lblCategory.Location = new System.Drawing.Point(24, 76);
            this.lblCategory.Name = "lblCategory";
            this.lblCategory.Size = new System.Drawing.Size(68, 15);
            this.lblCategory.TabIndex = 3;
            this.lblCategory.Text = "CATEGORY";
            // 
            // cmbCategory
            // 
            this.cmbCategory.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cmbCategory.Font = new System.Drawing.Font("Segoe UI", 9.5F);
            this.cmbCategory.FormattingEnabled = true;
            this.cmbCategory.Location = new System.Drawing.Point(24, 98);
            this.cmbCategory.Name = "cmbCategory";
            this.cmbCategory.Size = new System.Drawing.Size(225, 25);
            this.cmbCategory.TabIndex = 4;
            // 
            // lblPriority
            // 
            this.lblPriority.AutoSize = true;
            this.lblPriority.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Bold);
            this.lblPriority.ForeColor = System.Drawing.Color.FromArgb(71, 85, 105);
            this.lblPriority.Location = new System.Drawing.Point(269, 76);
            this.lblPriority.Name = "lblPriority";
            this.lblPriority.Size = new System.Drawing.Size(89, 15);
            this.lblPriority.TabIndex = 5;
            this.lblPriority.Text = "PRIORITY (SLA)";
            // 
            // cmbPriority
            // 
            this.cmbPriority.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cmbPriority.Font = new System.Drawing.Font("Segoe UI", 9.5F);
            this.cmbPriority.FormattingEnabled = true;
            this.cmbPriority.Location = new System.Drawing.Point(269, 98);
            this.cmbPriority.Name = "cmbPriority";
            this.cmbPriority.Size = new System.Drawing.Size(225, 25);
            this.cmbPriority.TabIndex = 6;
            // 
            // lblDescription
            // 
            this.lblDescription.AutoSize = true;
            this.lblDescription.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Bold);
            this.lblDescription.ForeColor = System.Drawing.Color.FromArgb(71, 85, 105);
            this.lblDescription.Location = new System.Drawing.Point(24, 136);
            this.lblDescription.Name = "lblDescription";
            this.lblDescription.Size = new System.Drawing.Size(82, 15);
            this.lblDescription.TabIndex = 7;
            this.lblDescription.Text = "DESCRIPTION";
            // 
            // txtDescription
            // 
            this.txtDescription.BackColor = System.Drawing.Color.White;
            this.txtDescription.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.txtDescription.Font = new System.Drawing.Font("Segoe UI", 9.5F);
            this.txtDescription.ForeColor = System.Drawing.Color.FromArgb(15, 23, 42);
            this.txtDescription.Location = new System.Drawing.Point(24, 158);
            this.txtDescription.Multiline = true;
            this.txtDescription.Name = "txtDescription";
            this.txtDescription.PlaceholderText = "Describe the issue, request, or maintenance concern...";
            this.txtDescription.ScrollBars = System.Windows.Forms.ScrollBars.Vertical;
            this.txtDescription.Size = new System.Drawing.Size(470, 140);
            this.txtDescription.TabIndex = 8;
            // 
            // pnlContent
            // 
            this.pnlContent.AutoScroll = true;
            this.pnlContent.BackColor = System.Drawing.Color.FromArgb(244, 247, 251);
            this.pnlContent.Controls.Add(this.lblCustomer);
            this.pnlContent.Controls.Add(this.cmbCustomer);
            this.pnlContent.Controls.Add(this.lblCategory);
            this.pnlContent.Controls.Add(this.cmbCategory);
            this.pnlContent.Controls.Add(this.lblPriority);
            this.pnlContent.Controls.Add(this.cmbPriority);
            this.pnlContent.Controls.Add(this.lblDescription);
            this.pnlContent.Controls.Add(this.txtDescription);
            this.pnlContent.Dock = System.Windows.Forms.DockStyle.Fill;
            this.pnlContent.Location = new System.Drawing.Point(0, 72);
            this.pnlContent.Name = "pnlContent";
            this.pnlContent.Padding = new System.Windows.Forms.Padding(24, 16, 24, 16);
            this.pnlContent.Size = new System.Drawing.Size(540, 348);
            this.pnlContent.TabIndex = 1;
            // 
            // pnlFooter
            // 
            this.pnlFooter.BackColor = System.Drawing.Color.White;
            this.pnlFooter.Controls.Add(this.btnCancel);
            this.pnlFooter.Controls.Add(this.btnSave);
            this.pnlFooter.Dock = System.Windows.Forms.DockStyle.Bottom;
            this.pnlFooter.Location = new System.Drawing.Point(0, 420);
            this.pnlFooter.Name = "pnlFooter";
            this.pnlFooter.Padding = new System.Windows.Forms.Padding(24, 12, 24, 12);
            this.pnlFooter.Size = new System.Drawing.Size(540, 60);
            this.pnlFooter.TabIndex = 2;
            // 
            // btnCancel
            // 
            this.btnCancel.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.btnCancel.BackColor = System.Drawing.Color.White;
            this.btnCancel.Cursor = System.Windows.Forms.Cursors.Hand;
            this.btnCancel.FlatAppearance.BorderColor = System.Drawing.Color.FromArgb(226, 232, 240);
            this.btnCancel.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnCancel.Font = new System.Drawing.Font("Segoe UI", 9.5F, System.Drawing.FontStyle.Bold);
            this.btnCancel.ForeColor = System.Drawing.Color.FromArgb(71, 85, 105);
            this.btnCancel.Location = new System.Drawing.Point(304, 12);
            this.btnCancel.Name = "btnCancel";
            this.btnCancel.Size = new System.Drawing.Size(95, 36);
            this.btnCancel.TabIndex = 0;
            this.btnCancel.Text = "Cancel";
            this.btnCancel.UseVisualStyleBackColor = false;
            // 
            // btnSave
            // 
            this.btnSave.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.btnSave.BackColor = System.Drawing.Color.FromArgb(15, 91, 158);
            this.btnSave.Cursor = System.Windows.Forms.Cursors.Hand;
            this.btnSave.FlatAppearance.BorderSize = 0;
            this.btnSave.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnSave.Font = new System.Drawing.Font("Segoe UI", 9.5F, System.Drawing.FontStyle.Bold);
            this.btnSave.ForeColor = System.Drawing.Color.White;
            this.btnSave.Location = new System.Drawing.Point(409, 12);
            this.btnSave.Name = "btnSave";
            this.btnSave.Size = new System.Drawing.Size(105, 36);
            this.btnSave.TabIndex = 1;
            this.btnSave.Text = "+ Submit";
            this.btnSave.UseVisualStyleBackColor = false;
            // 
            // SupportTicketInputForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 15F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.AutoScroll = false;
            this.BackColor = System.Drawing.Color.FromArgb(244, 247, 251);
            this.ClientSize = new System.Drawing.Size(540, 480);
            this.Controls.Add(this.pnlContent);
            this.Controls.Add(this.pnlFooter);
            this.Controls.Add(this.pnlHeader);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.MinimumSize = new System.Drawing.Size(540, 440);
            this.Name = "SupportTicketInputForm";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            this.Text = "Log Support Ticket";
            this.pnlHeader.ResumeLayout(false);
            this.pnlHeader.PerformLayout();
            this.pnlContent.ResumeLayout(false);
            this.pnlContent.PerformLayout();
            this.pnlFooter.ResumeLayout(false);
            this.ResumeLayout(false);

        }

        #endregion
    }
}
