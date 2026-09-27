namespace CRMS_Peguit.winforms.Views.Users
{
    partial class AdminUserDetailsForm
    {
        private System.ComponentModel.IContainer components = null;

        private System.Windows.Forms.Label lblNameHeader = null!;
        private System.Windows.Forms.Label lblNameValue = null!;
        private System.Windows.Forms.Label lblEmailHeader = null!;
        private System.Windows.Forms.Label lblEmailValue = null!;
        private System.Windows.Forms.Label lblRoleHeader = null!;
        private System.Windows.Forms.Label lblRoleValue = null!;
        private System.Windows.Forms.Label lblStatusHeader = null!;
        private System.Windows.Forms.Label lblStatusValue = null!;
        private System.Windows.Forms.Button btnChangePassword = null!;
        private System.Windows.Forms.Button btnToggleStatus = null!;
        private System.Windows.Forms.Button btnClose = null!;
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
            this.lblNameHeader = new System.Windows.Forms.Label();
            this.lblNameValue = new System.Windows.Forms.Label();
            this.lblEmailHeader = new System.Windows.Forms.Label();
            this.lblEmailValue = new System.Windows.Forms.Label();
            this.lblRoleHeader = new System.Windows.Forms.Label();
            this.lblRoleValue = new System.Windows.Forms.Label();
            this.lblStatusHeader = new System.Windows.Forms.Label();
            this.lblStatusValue = new System.Windows.Forms.Label();
            this.btnChangePassword = new System.Windows.Forms.Button();
            this.btnToggleStatus = new System.Windows.Forms.Button();
            this.btnClose = new System.Windows.Forms.Button();
            this.pnlFooter = new System.Windows.Forms.Panel();
            this.pnlContent = new System.Windows.Forms.Panel();
            this.pnlFooter.SuspendLayout();
            this.pnlContent.SuspendLayout();
            this.SuspendLayout();
            // 
            // lblNameHeader
            // 
            this.lblNameHeader.AutoSize = true;
            this.lblNameHeader.Font = new System.Drawing.Font("Segoe UI", 10F, System.Drawing.FontStyle.Bold);
            this.lblNameHeader.Location = new System.Drawing.Point(20, 20);
            this.lblNameHeader.Name = "lblNameHeader";
            this.lblNameHeader.Size = new System.Drawing.Size(53, 19);
            this.lblNameHeader.TabIndex = 0;
            this.lblNameHeader.Text = "Name:";
            // 
            // lblNameValue
            // 
            this.lblNameValue.AutoSize = true;
            this.lblNameValue.Font = new System.Drawing.Font("Segoe UI", 10F);
            this.lblNameValue.Location = new System.Drawing.Point(120, 20);
            this.lblNameValue.Name = "lblNameValue";
            this.lblNameValue.Size = new System.Drawing.Size(45, 19);
            this.lblNameValue.TabIndex = 1;
            this.lblNameValue.Text = "Name";
            // 
            // lblEmailHeader
            // 
            this.lblEmailHeader.AutoSize = true;
            this.lblEmailHeader.Font = new System.Drawing.Font("Segoe UI", 10F, System.Drawing.FontStyle.Bold);
            this.lblEmailHeader.Location = new System.Drawing.Point(20, 60);
            this.lblEmailHeader.Name = "lblEmailHeader";
            this.lblEmailHeader.Size = new System.Drawing.Size(49, 19);
            this.lblEmailHeader.TabIndex = 2;
            this.lblEmailHeader.Text = "Email:";
            // 
            // lblEmailValue
            // 
            this.lblEmailValue.AutoSize = true;
            this.lblEmailValue.Font = new System.Drawing.Font("Segoe UI", 10F);
            this.lblEmailValue.Location = new System.Drawing.Point(120, 60);
            this.lblEmailValue.Name = "lblEmailValue";
            this.lblEmailValue.Size = new System.Drawing.Size(41, 19);
            this.lblEmailValue.TabIndex = 3;
            this.lblEmailValue.Text = "Email";
            // 
            // lblRoleHeader
            // 
            this.lblRoleHeader.AutoSize = true;
            this.lblRoleHeader.Font = new System.Drawing.Font("Segoe UI", 10F, System.Drawing.FontStyle.Bold);
            this.lblRoleHeader.Location = new System.Drawing.Point(20, 100);
            this.lblRoleHeader.Name = "lblRoleHeader";
            this.lblRoleHeader.Size = new System.Drawing.Size(43, 19);
            this.lblRoleHeader.TabIndex = 4;
            this.lblRoleHeader.Text = "Role:";
            // 
            // lblRoleValue
            // 
            this.lblRoleValue.AutoSize = true;
            this.lblRoleValue.Font = new System.Drawing.Font("Segoe UI", 10F);
            this.lblRoleValue.Location = new System.Drawing.Point(120, 100);
            this.lblRoleValue.Name = "lblRoleValue";
            this.lblRoleValue.Size = new System.Drawing.Size(35, 19);
            this.lblRoleValue.TabIndex = 5;
            this.lblRoleValue.Text = "Role";
            // 
            // lblStatusHeader
            // 
            this.lblStatusHeader.AutoSize = true;
            this.lblStatusHeader.Font = new System.Drawing.Font("Segoe UI", 10F, System.Drawing.FontStyle.Bold);
            this.lblStatusHeader.Location = new System.Drawing.Point(20, 140);
            this.lblStatusHeader.Name = "lblStatusHeader";
            this.lblStatusHeader.Size = new System.Drawing.Size(53, 19);
            this.lblStatusHeader.TabIndex = 6;
            this.lblStatusHeader.Text = "Status:";
            // 
            // lblStatusValue
            // 
            this.lblStatusValue.AutoSize = true;
            this.lblStatusValue.Font = new System.Drawing.Font("Segoe UI", 10F);
            this.lblStatusValue.Location = new System.Drawing.Point(120, 140);
            this.lblStatusValue.Name = "lblStatusValue";
            this.lblStatusValue.Size = new System.Drawing.Size(47, 19);
            this.lblStatusValue.TabIndex = 7;
            this.lblStatusValue.Text = "Status";
            // 
            // btnChangePassword
            // 
            this.btnChangePassword.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)));
            this.btnChangePassword.BackColor = System.Drawing.Color.FromArgb(37, 103, 156);
            this.btnChangePassword.Cursor = System.Windows.Forms.Cursors.Hand;
            this.btnChangePassword.FlatAppearance.BorderSize = 0;
            this.btnChangePassword.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnChangePassword.Font = new System.Drawing.Font("Segoe UI", 9.5F, System.Drawing.FontStyle.Bold);
            this.btnChangePassword.ForeColor = System.Drawing.Color.White;
            this.btnChangePassword.Location = new System.Drawing.Point(16, 12);
            this.btnChangePassword.Name = "btnChangePassword";
            this.btnChangePassword.Size = new System.Drawing.Size(145, 36);
            this.btnChangePassword.TabIndex = 8;
            this.btnChangePassword.Text = "Change Password";
            this.btnChangePassword.UseVisualStyleBackColor = false;
            // 
            // btnToggleStatus
            // 
            this.btnToggleStatus.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)));
            this.btnToggleStatus.BackColor = System.Drawing.Color.FromArgb(153, 27, 27);
            this.btnToggleStatus.Cursor = System.Windows.Forms.Cursors.Hand;
            this.btnToggleStatus.FlatAppearance.BorderSize = 0;
            this.btnToggleStatus.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnToggleStatus.Font = new System.Drawing.Font("Segoe UI", 9.5F, System.Drawing.FontStyle.Bold);
            this.btnToggleStatus.ForeColor = System.Drawing.Color.White;
            this.btnToggleStatus.Location = new System.Drawing.Point(169, 12);
            this.btnToggleStatus.Name = "btnToggleStatus";
            this.btnToggleStatus.Size = new System.Drawing.Size(145, 36);
            this.btnToggleStatus.TabIndex = 9;
            this.btnToggleStatus.Text = "Deactivate User";
            this.btnToggleStatus.UseVisualStyleBackColor = false;
            // 
            // btnClose
            // 
            this.btnClose.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.btnClose.BackColor = System.Drawing.Color.Gray;
            this.btnClose.Cursor = System.Windows.Forms.Cursors.Hand;
            this.btnClose.DialogResult = System.Windows.Forms.DialogResult.OK;
            this.btnClose.FlatAppearance.BorderSize = 0;
            this.btnClose.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnClose.Font = new System.Drawing.Font("Segoe UI", 9.5F, System.Drawing.FontStyle.Bold);
            this.btnClose.ForeColor = System.Drawing.Color.White;
            this.btnClose.Location = new System.Drawing.Point(360, 12);
            this.btnClose.Name = "btnClose";
            this.btnClose.Size = new System.Drawing.Size(94, 36);
            this.btnClose.TabIndex = 10;
            this.btnClose.Text = "Close";
            this.btnClose.UseVisualStyleBackColor = false;
            // 
            // pnlFooter
            // 
            this.pnlFooter.BackColor = System.Drawing.Color.FromArgb(248, 250, 252);
            this.pnlFooter.Controls.Add(this.btnChangePassword);
            this.pnlFooter.Controls.Add(this.btnToggleStatus);
            this.pnlFooter.Controls.Add(this.btnClose);
            this.pnlFooter.Dock = System.Windows.Forms.DockStyle.Bottom;
            this.pnlFooter.Location = new System.Drawing.Point(0, 200);
            this.pnlFooter.Name = "pnlFooter";
            this.pnlFooter.Size = new System.Drawing.Size(470, 60);
            this.pnlFooter.TabIndex = 1;
            // 
            // pnlContent
            // 
            this.pnlContent.AutoScroll = true;
            this.pnlContent.Controls.Add(this.lblNameHeader);
            this.pnlContent.Controls.Add(this.lblNameValue);
            this.pnlContent.Controls.Add(this.lblEmailHeader);
            this.pnlContent.Controls.Add(this.lblEmailValue);
            this.pnlContent.Controls.Add(this.lblRoleHeader);
            this.pnlContent.Controls.Add(this.lblRoleValue);
            this.pnlContent.Controls.Add(this.lblStatusHeader);
            this.pnlContent.Controls.Add(this.lblStatusValue);
            this.pnlContent.Dock = System.Windows.Forms.DockStyle.Fill;
            this.pnlContent.Location = new System.Drawing.Point(0, 0);
            this.pnlContent.Name = "pnlContent";
            this.pnlContent.Size = new System.Drawing.Size(470, 200);
            this.pnlContent.TabIndex = 0;
            // 
            // AdminUserDetailsForm
            // 
            this.AcceptButton = this.btnClose;
            this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 15F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.AutoScroll = false;
            this.BackColor = System.Drawing.Color.White;
            this.CancelButton = this.btnClose;
            this.ClientSize = new System.Drawing.Size(470, 260);
            this.Controls.Add(this.pnlContent);
            this.Controls.Add(this.pnlFooter);
            this.ForeColor = System.Drawing.Color.FromArgb(8, 52, 87);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.MinimumSize = new System.Drawing.Size(470, 260);
            this.Name = "AdminUserDetailsForm";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            this.Text = "User Details";
            this.pnlFooter.ResumeLayout(false);
            this.pnlContent.ResumeLayout(false);
            this.pnlContent.PerformLayout();
            this.ResumeLayout(false);
        }

        #endregion
    }
}
