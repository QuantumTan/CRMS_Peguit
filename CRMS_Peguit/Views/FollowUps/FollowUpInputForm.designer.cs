namespace CRMS_Peguit.winforms.Views.FollowUps
{
    partial class FollowUpInputForm
    {
        private System.ComponentModel.IContainer components = null;

        private System.Windows.Forms.Panel pnlHeader = null!;
        private System.Windows.Forms.Label lblHeaderTitle = null!;
        private System.Windows.Forms.Label lblHeaderSub = null!;
        private System.Windows.Forms.Panel pnlFooter = null!;
        private System.Windows.Forms.Button btnCancel = null!;
        private System.Windows.Forms.Button btnSave = null!;
        private System.Windows.Forms.Panel pnlContent = null!;

        private System.Windows.Forms.Label lblTitle = null!;
        private System.Windows.Forms.TextBox txtTitle = null!;
        private System.Windows.Forms.Label lblType = null!;
        private System.Windows.Forms.ComboBox cmbType = null!;
        private System.Windows.Forms.Label lblPriority = null!;
        private System.Windows.Forms.ComboBox cmbPriority = null!;
        private System.Windows.Forms.Label lblDueDate = null!;
        private System.Windows.Forms.DateTimePicker dtpDueDate = null!;
        private System.Windows.Forms.Label lblDueTime = null!;
        private System.Windows.Forms.DateTimePicker dtpDueTime = null!;
        private System.Windows.Forms.Label lblRelatedTo = null!;
        private System.Windows.Forms.RadioButton rbCustomer = null!;
        private System.Windows.Forms.RadioButton rbLead = null!;
        private System.Windows.Forms.ComboBox cmbClient = null!;
        private System.Windows.Forms.Label lblNotes = null!;
        private System.Windows.Forms.TextBox txtNotes = null!;

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
            this.pnlHeader = new System.Windows.Forms.Panel();
            this.lblHeaderTitle = new System.Windows.Forms.Label();
            this.lblHeaderSub = new System.Windows.Forms.Label();
            this.pnlFooter = new System.Windows.Forms.Panel();
            this.btnCancel = new System.Windows.Forms.Button();
            this.btnSave = new System.Windows.Forms.Button();
            this.pnlContent = new System.Windows.Forms.Panel();
            this.lblTitle = new System.Windows.Forms.Label();
            this.txtTitle = new System.Windows.Forms.TextBox();
            this.lblType = new System.Windows.Forms.Label();
            this.cmbType = new System.Windows.Forms.ComboBox();
            this.lblPriority = new System.Windows.Forms.Label();
            this.cmbPriority = new System.Windows.Forms.ComboBox();
            this.lblDueDate = new System.Windows.Forms.Label();
            this.dtpDueDate = new System.Windows.Forms.DateTimePicker();
            this.lblDueTime = new System.Windows.Forms.Label();
            this.dtpDueTime = new System.Windows.Forms.DateTimePicker();
            this.lblRelatedTo = new System.Windows.Forms.Label();
            this.rbCustomer = new System.Windows.Forms.RadioButton();
            this.rbLead = new System.Windows.Forms.RadioButton();
            this.cmbClient = new System.Windows.Forms.ComboBox();
            this.lblNotes = new System.Windows.Forms.Label();
            this.txtNotes = new System.Windows.Forms.TextBox();
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
            this.pnlHeader.Size = new System.Drawing.Size(560, 74);
            this.pnlHeader.TabIndex = 0;
            // 
            // lblHeaderTitle
            // 
            this.lblHeaderTitle.AutoSize = true;
            this.lblHeaderTitle.Font = new System.Drawing.Font("Segoe UI", 13.5F, System.Drawing.FontStyle.Bold);
            this.lblHeaderTitle.ForeColor = System.Drawing.Color.FromArgb(15, 23, 42);
            this.lblHeaderTitle.Location = new System.Drawing.Point(22, 14);
            this.lblHeaderTitle.Name = "lblHeaderTitle";
            this.lblHeaderTitle.Size = new System.Drawing.Size(180, 25);
            this.lblHeaderTitle.TabIndex = 0;
            this.lblHeaderTitle.Text = "Schedule Follow-Up";
            // 
            // lblHeaderSub
            // 
            this.lblHeaderSub.AutoSize = true;
            this.lblHeaderSub.Font = new System.Drawing.Font("Segoe UI", 9F);
            this.lblHeaderSub.ForeColor = System.Drawing.Color.FromArgb(100, 116, 139);
            this.lblHeaderSub.Location = new System.Drawing.Point(24, 42);
            this.lblHeaderSub.Name = "lblHeaderSub";
            this.lblHeaderSub.Size = new System.Drawing.Size(320, 15);
            this.lblHeaderSub.TabIndex = 1;
            this.lblHeaderSub.Text = "Set forward-looking tasks and touchpoints for assigned clients";
            // 
            // pnlFooter
            // 
            this.pnlFooter.BackColor = System.Drawing.Color.FromArgb(248, 250, 252);
            this.pnlFooter.Controls.Add(this.btnCancel);
            this.pnlFooter.Controls.Add(this.btnSave);
            this.pnlFooter.Dock = System.Windows.Forms.DockStyle.Bottom;
            this.pnlFooter.Location = new System.Drawing.Point(0, 520);
            this.pnlFooter.Name = "pnlFooter";
            this.pnlFooter.Size = new System.Drawing.Size(560, 60);
            this.pnlFooter.TabIndex = 2;
            // 
            // btnCancel
            // 
            this.btnCancel.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.btnCancel.BackColor = System.Drawing.Color.White;
            this.btnCancel.Cursor = System.Windows.Forms.Cursors.Hand;
            this.btnCancel.FlatAppearance.BorderColor = System.Drawing.Color.FromArgb(203, 213, 225);
            this.btnCancel.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnCancel.Font = new System.Drawing.Font("Segoe UI", 9.5F, System.Drawing.FontStyle.Bold);
            this.btnCancel.ForeColor = System.Drawing.Color.FromArgb(71, 85, 105);
            this.btnCancel.Location = new System.Drawing.Point(310, 12);
            this.btnCancel.Name = "btnCancel";
            this.btnCancel.Size = new System.Drawing.Size(100, 36);
            this.btnCancel.TabIndex = 1;
            this.btnCancel.Text = "Cancel";
            this.btnCancel.UseVisualStyleBackColor = false;
            // 
            // btnSave
            // 
            this.btnSave.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.btnSave.BackColor = System.Drawing.Color.FromArgb(15, 91, 158);
            this.btnSave.Cursor = System.Windows.Forms.Cursors.Hand;
            this.btnSave.FlatAppearance.BorderSize = 0;
            this.btnSave.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnSave.Font = new System.Drawing.Font("Segoe UI", 9.5F, System.Drawing.FontStyle.Bold);
            this.btnSave.ForeColor = System.Drawing.Color.White;
            this.btnSave.Location = new System.Drawing.Point(420, 12);
            this.btnSave.Name = "btnSave";
            this.btnSave.Size = new System.Drawing.Size(120, 36);
            this.btnSave.TabIndex = 0;
            this.btnSave.Text = "Save Follow-Up";
            this.btnSave.UseVisualStyleBackColor = false;
            // 
            // pnlContent
            // 
            this.pnlContent.AutoScroll = true;
            this.pnlContent.BackColor = System.Drawing.Color.White;
            this.pnlContent.Controls.Add(this.lblTitle);
            this.pnlContent.Controls.Add(this.txtTitle);
            this.pnlContent.Controls.Add(this.lblType);
            this.pnlContent.Controls.Add(this.cmbType);
            this.pnlContent.Controls.Add(this.lblPriority);
            this.pnlContent.Controls.Add(this.cmbPriority);
            this.pnlContent.Controls.Add(this.lblDueDate);
            this.pnlContent.Controls.Add(this.dtpDueDate);
            this.pnlContent.Controls.Add(this.lblDueTime);
            this.pnlContent.Controls.Add(this.dtpDueTime);
            this.pnlContent.Controls.Add(this.lblRelatedTo);
            this.pnlContent.Controls.Add(this.rbCustomer);
            this.pnlContent.Controls.Add(this.rbLead);
            this.pnlContent.Controls.Add(this.cmbClient);
            this.pnlContent.Controls.Add(this.lblNotes);
            this.pnlContent.Controls.Add(this.txtNotes);
            this.pnlContent.Dock = System.Windows.Forms.DockStyle.Fill;
            this.pnlContent.Location = new System.Drawing.Point(0, 74);
            this.pnlContent.Name = "pnlContent";
            this.pnlContent.Padding = new System.Windows.Forms.Padding(24, 16, 24, 16);
            this.pnlContent.Size = new System.Drawing.Size(560, 446);
            this.pnlContent.TabIndex = 1;
            // 
            // lblTitle
            // 
            this.lblTitle.AutoSize = true;
            this.lblTitle.Font = new System.Drawing.Font("Segoe UI", 8.5F, System.Drawing.FontStyle.Bold);
            this.lblTitle.ForeColor = System.Drawing.Color.FromArgb(71, 85, 105);
            this.lblTitle.Location = new System.Drawing.Point(24, 16);
            this.lblTitle.Name = "lblTitle";
            this.lblTitle.Size = new System.Drawing.Size(125, 15);
            this.lblTitle.TabIndex = 0;
            this.lblTitle.Text = "FOLLOW-UP TITLE *";
            // 
            // txtTitle
            // 
            this.txtTitle.Font = new System.Drawing.Font("Segoe UI", 9.5F);
            this.txtTitle.Location = new System.Drawing.Point(26, 36);
            this.txtTitle.Name = "txtTitle";
            this.txtTitle.PlaceholderText = "e.g. Call client regarding contract updates";
            this.txtTitle.Size = new System.Drawing.Size(506, 25);
            this.txtTitle.TabIndex = 1;
            // 
            // lblType
            // 
            this.lblType.AutoSize = true;
            this.lblType.Font = new System.Drawing.Font("Segoe UI", 8.5F, System.Drawing.FontStyle.Bold);
            this.lblType.ForeColor = System.Drawing.Color.FromArgb(71, 85, 105);
            this.lblType.Location = new System.Drawing.Point(24, 76);
            this.lblType.Name = "lblType";
            this.lblType.Size = new System.Drawing.Size(107, 15);
            this.lblType.TabIndex = 2;
            this.lblType.Text = "CHANNEL / TYPE";
            // 
            // cmbType
            // 
            this.cmbType.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cmbType.Font = new System.Drawing.Font("Segoe UI", 9.5F);
            this.cmbType.FormattingEnabled = true;
            this.cmbType.Location = new System.Drawing.Point(26, 96);
            this.cmbType.Name = "cmbType";
            this.cmbType.Size = new System.Drawing.Size(240, 25);
            this.cmbType.TabIndex = 3;
            // 
            // lblPriority
            // 
            this.lblPriority.AutoSize = true;
            this.lblPriority.Font = new System.Drawing.Font("Segoe UI", 8.5F, System.Drawing.FontStyle.Bold);
            this.lblPriority.ForeColor = System.Drawing.Color.FromArgb(71, 85, 105);
            this.lblPriority.Location = new System.Drawing.Point(288, 76);
            this.lblPriority.Name = "lblPriority";
            this.lblPriority.Size = new System.Drawing.Size(63, 15);
            this.lblPriority.TabIndex = 4;
            this.lblPriority.Text = "PRIORITY";
            // 
            // cmbPriority
            // 
            this.cmbPriority.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cmbPriority.Font = new System.Drawing.Font("Segoe UI", 9.5F);
            this.cmbPriority.FormattingEnabled = true;
            this.cmbPriority.Location = new System.Drawing.Point(292, 96);
            this.cmbPriority.Name = "cmbPriority";
            this.cmbPriority.Size = new System.Drawing.Size(240, 25);
            this.cmbPriority.TabIndex = 5;
            // 
            // lblDueDate
            // 
            this.lblDueDate.AutoSize = true;
            this.lblDueDate.Font = new System.Drawing.Font("Segoe UI", 8.5F, System.Drawing.FontStyle.Bold);
            this.lblDueDate.ForeColor = System.Drawing.Color.FromArgb(71, 85, 105);
            this.lblDueDate.Location = new System.Drawing.Point(24, 136);
            this.lblDueDate.Name = "lblDueDate";
            this.lblDueDate.Size = new System.Drawing.Size(73, 15);
            this.lblDueDate.TabIndex = 6;
            this.lblDueDate.Text = "DUE DATE *";
            // 
            // dtpDueDate
            // 
            this.dtpDueDate.Font = new System.Drawing.Font("Segoe UI", 9.5F);
            this.dtpDueDate.Format = System.Windows.Forms.DateTimePickerFormat.Short;
            this.dtpDueDate.Location = new System.Drawing.Point(26, 156);
            this.dtpDueDate.Name = "dtpDueDate";
            this.dtpDueDate.Size = new System.Drawing.Size(240, 25);
            this.dtpDueDate.TabIndex = 7;
            // 
            // lblDueTime
            // 
            this.lblDueTime.AutoSize = true;
            this.lblDueTime.Font = new System.Drawing.Font("Segoe UI", 8.5F, System.Drawing.FontStyle.Bold);
            this.lblDueTime.ForeColor = System.Drawing.Color.FromArgb(71, 85, 105);
            this.lblDueTime.Location = new System.Drawing.Point(288, 136);
            this.lblDueTime.Name = "lblDueTime";
            this.lblDueTime.Size = new System.Drawing.Size(71, 15);
            this.lblDueTime.TabIndex = 8;
            this.lblDueTime.Text = "DUE TIME *";
            // 
            // dtpDueTime
            // 
            this.dtpDueTime.CustomFormat = "hh:mm tt";
            this.dtpDueTime.Font = new System.Drawing.Font("Segoe UI", 9.5F);
            this.dtpDueTime.Format = System.Windows.Forms.DateTimePickerFormat.Custom;
            this.dtpDueTime.Location = new System.Drawing.Point(292, 156);
            this.dtpDueTime.Name = "dtpDueTime";
            this.dtpDueTime.ShowUpDown = true;
            this.dtpDueTime.Size = new System.Drawing.Size(240, 25);
            this.dtpDueTime.TabIndex = 9;
            // 
            // lblRelatedTo
            // 
            this.lblRelatedTo.AutoSize = true;
            this.lblRelatedTo.Font = new System.Drawing.Font("Segoe UI", 8.5F, System.Drawing.FontStyle.Bold);
            this.lblRelatedTo.ForeColor = System.Drawing.Color.FromArgb(71, 85, 105);
            this.lblRelatedTo.Location = new System.Drawing.Point(24, 198);
            this.lblRelatedTo.Name = "lblRelatedTo";
            this.lblRelatedTo.Size = new System.Drawing.Size(184, 15);
            this.lblRelatedTo.TabIndex = 10;
            this.lblRelatedTo.Text = "LINK TO (EXACTLY ONE) *";
            // 
            // rbCustomer
            // 
            this.rbCustomer.AutoSize = true;
            this.rbCustomer.Checked = true;
            this.rbCustomer.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Bold);
            this.rbCustomer.ForeColor = System.Drawing.Color.FromArgb(15, 23, 42);
            this.rbCustomer.Location = new System.Drawing.Point(28, 222);
            this.rbCustomer.Name = "rbCustomer";
            this.rbCustomer.Size = new System.Drawing.Size(80, 19);
            this.rbCustomer.TabIndex = 11;
            this.rbCustomer.TabStop = true;
            this.rbCustomer.Text = "Customer";
            this.rbCustomer.UseVisualStyleBackColor = true;
            // 
            // rbLead
            // 
            this.rbLead.AutoSize = true;
            this.rbLead.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Bold);
            this.rbLead.ForeColor = System.Drawing.Color.FromArgb(15, 23, 42);
            this.rbLead.Location = new System.Drawing.Point(128, 222);
            this.rbLead.Name = "rbLead";
            this.rbLead.Size = new System.Drawing.Size(51, 19);
            this.rbLead.TabIndex = 12;
            this.rbLead.Text = "Lead";
            this.rbLead.UseVisualStyleBackColor = true;
            // 
            // cmbClient
            // 
            this.cmbClient.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cmbClient.Font = new System.Drawing.Font("Segoe UI", 9.5F);
            this.cmbClient.FormattingEnabled = true;
            this.cmbClient.Location = new System.Drawing.Point(26, 248);
            this.cmbClient.Name = "cmbClient";
            this.cmbClient.Size = new System.Drawing.Size(506, 25);
            this.cmbClient.TabIndex = 13;
            // 
            // lblNotes
            // 
            this.lblNotes.AutoSize = true;
            this.lblNotes.Font = new System.Drawing.Font("Segoe UI", 8.5F, System.Drawing.FontStyle.Bold);
            this.lblNotes.ForeColor = System.Drawing.Color.FromArgb(71, 85, 105);
            this.lblNotes.Location = new System.Drawing.Point(24, 290);
            this.lblNotes.Name = "lblNotes";
            this.lblNotes.Size = new System.Drawing.Size(193, 15);
            this.lblNotes.TabIndex = 14;
            this.lblNotes.Text = "DESCRIPTION / CONTEXT NOTES";
            // 
            // txtNotes
            // 
            this.txtNotes.Font = new System.Drawing.Font("Segoe UI", 9.5F);
            this.txtNotes.Location = new System.Drawing.Point(26, 310);
            this.txtNotes.Multiline = true;
            this.txtNotes.Name = "txtNotes";
            this.txtNotes.PlaceholderText = "Add any helpful preparation context, specific questions, or discussion points...";
            this.txtNotes.ScrollBars = System.Windows.Forms.ScrollBars.Vertical;
            this.txtNotes.Size = new System.Drawing.Size(506, 110);
            this.txtNotes.TabIndex = 15;
            // 
            // FollowUpInputForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 15F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.BackColor = System.Drawing.Color.White;
            this.ClientSize = new System.Drawing.Size(560, 580);
            this.AcceptButton = this.btnSave;
            this.CancelButton = this.btnCancel;
            this.MinimumSize = new System.Drawing.Size(560, 520);
            this.Controls.Add(this.pnlContent);
            this.Controls.Add(this.pnlFooter);
            this.Controls.Add(this.pnlHeader);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "FollowUpInputForm";
            this.ShowInTaskbar = false;
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            this.Text = "Follow-Up";
            this.pnlHeader.ResumeLayout(false);
            this.pnlHeader.PerformLayout();
            this.pnlFooter.ResumeLayout(false);
            this.pnlContent.ResumeLayout(false);
            this.pnlContent.PerformLayout();
            this.ResumeLayout(false);
        }

        #endregion
    }
}
