namespace CRMS_Peguit.winforms.Views.Shared
{
    partial class EmailMessageForm
    {
        private System.ComponentModel.IContainer components = null;

        private System.Windows.Forms.Panel pnlNotice = null!;
        private System.Windows.Forms.Label lblNotice = null!;
        private System.Windows.Forms.Label lblRecipient = null!;
        private System.Windows.Forms.TextBox txtRecipient = null!;
        private System.Windows.Forms.Label lblTemplate = null!;
        private System.Windows.Forms.ComboBox cboTemplate = null!;
        private System.Windows.Forms.Label lblSubject = null!;
        private System.Windows.Forms.TextBox txtSubject = null!;
        private System.Windows.Forms.Label lblBody = null!;
        private System.Windows.Forms.TextBox txtBody = null!;
        private System.Windows.Forms.Button btnSend = null!;
        private System.Windows.Forms.Button btnCancel = null!;
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
            this.pnlNotice = new System.Windows.Forms.Panel();
            this.lblNotice = new System.Windows.Forms.Label();
            this.lblRecipient = new System.Windows.Forms.Label();
            this.txtRecipient = new System.Windows.Forms.TextBox();
            this.lblTemplate = new System.Windows.Forms.Label();
            this.cboTemplate = new System.Windows.Forms.ComboBox();
            this.lblSubject = new System.Windows.Forms.Label();
            this.txtSubject = new System.Windows.Forms.TextBox();
            this.lblBody = new System.Windows.Forms.Label();
            this.txtBody = new System.Windows.Forms.TextBox();
            this.btnSend = new System.Windows.Forms.Button();
            this.btnCancel = new System.Windows.Forms.Button();
            this.pnlFooter = new System.Windows.Forms.Panel();
            this.pnlContent = new System.Windows.Forms.Panel();
            this.pnlNotice.SuspendLayout();
            this.pnlFooter.SuspendLayout();
            this.pnlContent.SuspendLayout();
            this.SuspendLayout();
            // 
            // pnlNotice
            // 
            this.pnlNotice.BackColor = System.Drawing.Color.FromArgb(239, 246, 255);
            this.pnlNotice.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.pnlNotice.Controls.Add(this.lblNotice);
            this.pnlNotice.Location = new System.Drawing.Point(20, 15);
            this.pnlNotice.Name = "pnlNotice";
            this.pnlNotice.Size = new System.Drawing.Size(520, 44);
            this.pnlNotice.TabIndex = 0;
            // 
            // lblNotice
            // 
            this.lblNotice.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblNotice.Font = new System.Drawing.Font("Segoe UI", 8.75F, System.Drawing.FontStyle.Regular);
            this.lblNotice.ForeColor = System.Drawing.Color.FromArgb(30, 64, 175);
            this.lblNotice.Location = new System.Drawing.Point(0, 0);
            this.lblNotice.Name = "lblNotice";
            this.lblNotice.Padding = new System.Windows.Forms.Padding(8, 0, 8, 0);
            this.lblNotice.Size = new System.Drawing.Size(518, 42);
            this.lblNotice.TabIndex = 0;
            this.lblNotice.Text = "🔒 Corporate Template Enforced: Sales Staff must use approved organizational templates and cannot modify the subject or message.";
            this.lblNotice.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // lblRecipient
            // 
            this.lblRecipient.AutoSize = true;
            this.lblRecipient.ForeColor = System.Drawing.Color.FromArgb(8, 52, 87);
            this.lblRecipient.Location = new System.Drawing.Point(20, 68);
            this.lblRecipient.Name = "lblRecipient";
            this.lblRecipient.Size = new System.Drawing.Size(108, 19);
            this.lblRecipient.TabIndex = 1;
            this.lblRecipient.Text = "Recipient Email *";
            // 
            // txtRecipient
            // 
            this.txtRecipient.BackColor = System.Drawing.Color.White;
            this.txtRecipient.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.txtRecipient.ForeColor = System.Drawing.Color.FromArgb(8, 52, 87);
            this.txtRecipient.Location = new System.Drawing.Point(20, 90);
            this.txtRecipient.Name = "txtRecipient";
            this.txtRecipient.Size = new System.Drawing.Size(520, 25);
            this.txtRecipient.TabIndex = 2;
            // 
            // lblTemplate
            // 
            this.lblTemplate.AutoSize = true;
            this.lblTemplate.ForeColor = System.Drawing.Color.FromArgb(8, 52, 87);
            this.lblTemplate.Location = new System.Drawing.Point(20, 124);
            this.lblTemplate.Name = "lblTemplate";
            this.lblTemplate.Size = new System.Drawing.Size(155, 19);
            this.lblTemplate.TabIndex = 3;
            this.lblTemplate.Text = "Select Email Template *";
            // 
            // cboTemplate
            // 
            this.cboTemplate.BackColor = System.Drawing.Color.White;
            this.cboTemplate.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cboTemplate.Font = new System.Drawing.Font("Segoe UI", 9.5F);
            this.cboTemplate.ForeColor = System.Drawing.Color.FromArgb(8, 52, 87);
            this.cboTemplate.FormattingEnabled = true;
            this.cboTemplate.Location = new System.Drawing.Point(20, 146);
            this.cboTemplate.Name = "cboTemplate";
            this.cboTemplate.Size = new System.Drawing.Size(520, 25);
            this.cboTemplate.TabIndex = 4;
            // 
            // lblSubject
            // 
            this.lblSubject.AutoSize = true;
            this.lblSubject.ForeColor = System.Drawing.Color.FromArgb(8, 52, 87);
            this.lblSubject.Location = new System.Drawing.Point(20, 180);
            this.lblSubject.Name = "lblSubject";
            this.lblSubject.Size = new System.Drawing.Size(60, 19);
            this.lblSubject.TabIndex = 5;
            this.lblSubject.Text = "Subject *";
            // 
            // txtSubject
            // 
            this.txtSubject.BackColor = System.Drawing.Color.White;
            this.txtSubject.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.txtSubject.ForeColor = System.Drawing.Color.FromArgb(8, 52, 87);
            this.txtSubject.Location = new System.Drawing.Point(20, 202);
            this.txtSubject.Name = "txtSubject";
            this.txtSubject.Size = new System.Drawing.Size(520, 25);
            this.txtSubject.TabIndex = 6;
            // 
            // lblBody
            // 
            this.lblBody.AutoSize = true;
            this.lblBody.ForeColor = System.Drawing.Color.FromArgb(8, 52, 87);
            this.lblBody.Location = new System.Drawing.Point(20, 236);
            this.lblBody.Name = "lblBody";
            this.lblBody.Size = new System.Drawing.Size(73, 19);
            this.lblBody.TabIndex = 7;
            this.lblBody.Text = "Message *";
            // 
            // txtBody
            // 
            this.txtBody.BackColor = System.Drawing.Color.White;
            this.txtBody.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.txtBody.ForeColor = System.Drawing.Color.FromArgb(8, 52, 87);
            this.txtBody.Location = new System.Drawing.Point(20, 258);
            this.txtBody.Multiline = true;
            this.txtBody.Name = "txtBody";
            this.txtBody.ScrollBars = System.Windows.Forms.ScrollBars.Vertical;
            this.txtBody.Size = new System.Drawing.Size(520, 195);
            this.txtBody.TabIndex = 8;
            // 
            // pnlContent
            // 
            this.pnlContent.AutoScroll = true;
            this.pnlContent.BackColor = System.Drawing.Color.FromArgb(243, 247, 250);
            this.pnlContent.Controls.Add(this.pnlNotice);
            this.pnlContent.Controls.Add(this.lblRecipient);
            this.pnlContent.Controls.Add(this.txtRecipient);
            this.pnlContent.Controls.Add(this.lblTemplate);
            this.pnlContent.Controls.Add(this.cboTemplate);
            this.pnlContent.Controls.Add(this.lblSubject);
            this.pnlContent.Controls.Add(this.txtSubject);
            this.pnlContent.Controls.Add(this.lblBody);
            this.pnlContent.Controls.Add(this.txtBody);
            this.pnlContent.Dock = System.Windows.Forms.DockStyle.Fill;
            this.pnlContent.Location = new System.Drawing.Point(0, 0);
            this.pnlContent.Name = "pnlContent";
            this.pnlContent.Padding = new System.Windows.Forms.Padding(20);
            this.pnlContent.Size = new System.Drawing.Size(560, 465);
            this.pnlContent.TabIndex = 0;
            // 
            // btnCancel
            // 
            this.btnCancel.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.btnCancel.BackColor = System.Drawing.Color.White;
            this.btnCancel.Cursor = System.Windows.Forms.Cursors.Hand;
            this.btnCancel.DialogResult = System.Windows.Forms.DialogResult.Cancel;
            this.btnCancel.FlatAppearance.BorderColor = System.Drawing.Color.FromArgb(180, 198, 217);
            this.btnCancel.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnCancel.Font = new System.Drawing.Font("Segoe UI", 10F, System.Drawing.FontStyle.Bold);
            this.btnCancel.ForeColor = System.Drawing.Color.FromArgb(8, 52, 87);
            this.btnCancel.Location = new System.Drawing.Point(340, 11);
            this.btnCancel.Name = "btnCancel";
            this.btnCancel.Size = new System.Drawing.Size(90, 38);
            this.btnCancel.TabIndex = 9;
            this.btnCancel.Text = "Cancel";
            this.btnCancel.UseVisualStyleBackColor = false;
            // 
            // btnSend
            // 
            this.btnSend.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.btnSend.BackColor = System.Drawing.Color.FromArgb(37, 103, 156);
            this.btnSend.Cursor = System.Windows.Forms.Cursors.Hand;
            this.btnSend.FlatAppearance.BorderSize = 0;
            this.btnSend.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnSend.Font = new System.Drawing.Font("Segoe UI", 10F, System.Drawing.FontStyle.Bold);
            this.btnSend.ForeColor = System.Drawing.Color.White;
            this.btnSend.Location = new System.Drawing.Point(440, 11);
            this.btnSend.Name = "btnSend";
            this.btnSend.Size = new System.Drawing.Size(90, 38);
            this.btnSend.TabIndex = 10;
            this.btnSend.Text = "Send";
            this.btnSend.UseVisualStyleBackColor = false;
            // 
            // pnlFooter
            // 
            this.pnlFooter.BackColor = System.Drawing.Color.FromArgb(243, 247, 250);
            this.pnlFooter.Controls.Add(this.btnCancel);
            this.pnlFooter.Controls.Add(this.btnSend);
            this.pnlFooter.Dock = System.Windows.Forms.DockStyle.Bottom;
            this.pnlFooter.Location = new System.Drawing.Point(0, 465);
            this.pnlFooter.Name = "pnlFooter";
            this.pnlFooter.Padding = new System.Windows.Forms.Padding(20, 10, 20, 10);
            this.pnlFooter.Size = new System.Drawing.Size(560, 60);
            this.pnlFooter.TabIndex = 1;
            // 
            // EmailMessageForm
            // 
            this.AcceptButton = this.btnSend;
            this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 15F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.AutoScroll = false;
            this.BackColor = System.Drawing.Color.FromArgb(243, 247, 250);
            this.CancelButton = this.btnCancel;
            this.ClientSize = new System.Drawing.Size(560, 525);
            this.Controls.Add(this.pnlContent);
            this.Controls.Add(this.pnlFooter);
            this.Font = new System.Drawing.Font("Segoe UI", 10F);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.MinimumSize = new System.Drawing.Size(560, 525);
            this.Name = "EmailMessageForm";
            this.ShowInTaskbar = false;
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            this.Text = "Send Email";
            this.pnlNotice.ResumeLayout(false);
            this.pnlContent.ResumeLayout(false);
            this.pnlContent.PerformLayout();
            this.pnlFooter.ResumeLayout(false);
            this.ResumeLayout(false);
        }

        #endregion
    }
}
