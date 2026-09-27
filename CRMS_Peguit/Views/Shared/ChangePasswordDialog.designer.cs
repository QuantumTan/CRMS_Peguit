namespace CRMS_Peguit.winforms.Views.Shared
{
    partial class ChangePasswordDialog
    {
        private System.ComponentModel.IContainer components = null;

        private System.Windows.Forms.Label lblTitle = null!;
        private System.Windows.Forms.Label lblSubtitle = null!;
        private System.Windows.Forms.Label lblNewPassword = null!;
        private System.Windows.Forms.TextBox txtNewPassword = null!;
        private System.Windows.Forms.Label lblConfirmPassword = null!;
        private System.Windows.Forms.TextBox txtConfirmPassword = null!;
        private System.Windows.Forms.CheckBox chkShowPassword = null!;
        private System.Windows.Forms.Label lblError = null!;
        private System.Windows.Forms.Button btnSave = null!;
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
            this.lblTitle = new System.Windows.Forms.Label();
            this.lblSubtitle = new System.Windows.Forms.Label();
            this.lblNewPassword = new System.Windows.Forms.Label();
            this.txtNewPassword = new System.Windows.Forms.TextBox();
            this.lblConfirmPassword = new System.Windows.Forms.Label();
            this.txtConfirmPassword = new System.Windows.Forms.TextBox();
            this.chkShowPassword = new System.Windows.Forms.CheckBox();
            this.lblError = new System.Windows.Forms.Label();
            this.btnSave = new System.Windows.Forms.Button();
            this.btnCancel = new System.Windows.Forms.Button();
            this.pnlFooter = new System.Windows.Forms.Panel();
            this.pnlContent = new System.Windows.Forms.Panel();
            this.pnlFooter.SuspendLayout();
            this.pnlContent.SuspendLayout();
            this.SuspendLayout();
            // 
            // lblTitle
            // 
            this.lblTitle.AutoSize = true;
            this.lblTitle.Font = new System.Drawing.Font("Segoe UI", 13F, System.Drawing.FontStyle.Bold);
            this.lblTitle.ForeColor = System.Drawing.Color.FromArgb(8, 52, 87);
            this.lblTitle.Location = new System.Drawing.Point(24, 20);
            this.lblTitle.Name = "lblTitle";
            this.lblTitle.Size = new System.Drawing.Size(161, 25);
            this.lblTitle.TabIndex = 0;
            this.lblTitle.Text = "Change Password";
            // 
            // lblSubtitle
            // 
            this.lblSubtitle.AutoEllipsis = true;
            this.lblSubtitle.Font = new System.Drawing.Font("Segoe UI", 9F);
            this.lblSubtitle.ForeColor = System.Drawing.Color.FromArgb(71, 85, 105);
            this.lblSubtitle.Location = new System.Drawing.Point(25, 48);
            this.lblSubtitle.Name = "lblSubtitle";
            this.lblSubtitle.Size = new System.Drawing.Size(350, 20);
            this.lblSubtitle.TabIndex = 1;
            this.lblSubtitle.Text = "Set a new secure password for this account.";
            // 
            // lblNewPassword
            // 
            this.lblNewPassword.AutoSize = true;
            this.lblNewPassword.Font = new System.Drawing.Font("Segoe UI", 9.5F, System.Drawing.FontStyle.Bold);
            this.lblNewPassword.ForeColor = System.Drawing.Color.FromArgb(8, 52, 87);
            this.lblNewPassword.Location = new System.Drawing.Point(24, 78);
            this.lblNewPassword.Name = "lblNewPassword";
            this.lblNewPassword.Size = new System.Drawing.Size(107, 17);
            this.lblNewPassword.TabIndex = 2;
            this.lblNewPassword.Text = "New Password *";
            // 
            // txtNewPassword
            // 
            this.txtNewPassword.BackColor = System.Drawing.Color.White;
            this.txtNewPassword.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.txtNewPassword.Font = new System.Drawing.Font("Segoe UI", 10F);
            this.txtNewPassword.ForeColor = System.Drawing.Color.FromArgb(8, 52, 87);
            this.txtNewPassword.Location = new System.Drawing.Point(25, 100);
            this.txtNewPassword.MaxLength = 100;
            this.txtNewPassword.Name = "txtNewPassword";
            this.txtNewPassword.PasswordChar = '●';
            this.txtNewPassword.Size = new System.Drawing.Size(350, 25);
            this.txtNewPassword.TabIndex = 3;
            // 
            // lblConfirmPassword
            // 
            this.lblConfirmPassword.AutoSize = true;
            this.lblConfirmPassword.Font = new System.Drawing.Font("Segoe UI", 9.5F, System.Drawing.FontStyle.Bold);
            this.lblConfirmPassword.ForeColor = System.Drawing.Color.FromArgb(8, 52, 87);
            this.lblConfirmPassword.Location = new System.Drawing.Point(24, 138);
            this.lblConfirmPassword.Name = "lblConfirmPassword";
            this.lblConfirmPassword.Size = new System.Drawing.Size(129, 17);
            this.lblConfirmPassword.TabIndex = 4;
            this.lblConfirmPassword.Text = "Confirm Password *";
            // 
            // txtConfirmPassword
            // 
            this.txtConfirmPassword.BackColor = System.Drawing.Color.White;
            this.txtConfirmPassword.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.txtConfirmPassword.Font = new System.Drawing.Font("Segoe UI", 10F);
            this.txtConfirmPassword.ForeColor = System.Drawing.Color.FromArgb(8, 52, 87);
            this.txtConfirmPassword.Location = new System.Drawing.Point(25, 160);
            this.txtConfirmPassword.MaxLength = 100;
            this.txtConfirmPassword.Name = "txtConfirmPassword";
            this.txtConfirmPassword.PasswordChar = '●';
            this.txtConfirmPassword.Size = new System.Drawing.Size(350, 25);
            this.txtConfirmPassword.TabIndex = 5;
            // 
            // chkShowPassword
            // 
            this.chkShowPassword.AutoSize = true;
            this.chkShowPassword.Font = new System.Drawing.Font("Segoe UI", 9F);
            this.chkShowPassword.ForeColor = System.Drawing.Color.FromArgb(71, 85, 105);
            this.chkShowPassword.Location = new System.Drawing.Point(25, 195);
            this.chkShowPassword.Name = "chkShowPassword";
            this.chkShowPassword.Size = new System.Drawing.Size(108, 19);
            this.chkShowPassword.TabIndex = 6;
            this.chkShowPassword.Text = "Show passwords";
            this.chkShowPassword.UseVisualStyleBackColor = true;
            // 
            // lblError
            // 
            this.lblError.AutoEllipsis = true;
            this.lblError.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Bold);
            this.lblError.ForeColor = System.Drawing.Color.FromArgb(185, 28, 28);
            this.lblError.Location = new System.Drawing.Point(24, 222);
            this.lblError.Name = "lblError";
            this.lblError.Size = new System.Drawing.Size(350, 18);
            this.lblError.TabIndex = 7;
            this.lblError.Visible = false;
            // 
            // btnSave
            // 
            this.btnSave.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.btnSave.BackColor = System.Drawing.Color.FromArgb(37, 103, 156);
            this.btnSave.Cursor = System.Windows.Forms.Cursors.Hand;
            this.btnSave.FlatAppearance.BorderSize = 0;
            this.btnSave.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnSave.Font = new System.Drawing.Font("Segoe UI", 9.5F, System.Drawing.FontStyle.Bold);
            this.btnSave.ForeColor = System.Drawing.Color.White;
            this.btnSave.Location = new System.Drawing.Point(235, 12);
            this.btnSave.Name = "btnSave";
            this.btnSave.Size = new System.Drawing.Size(140, 36);
            this.btnSave.TabIndex = 8;
            this.btnSave.Text = "Update Password";
            this.btnSave.UseVisualStyleBackColor = false;
            // 
            // btnCancel
            // 
            this.btnCancel.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.btnCancel.BackColor = System.Drawing.Color.White;
            this.btnCancel.Cursor = System.Windows.Forms.Cursors.Hand;
            this.btnCancel.DialogResult = System.Windows.Forms.DialogResult.Cancel;
            this.btnCancel.FlatAppearance.BorderColor = System.Drawing.Color.FromArgb(180, 198, 217);
            this.btnCancel.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnCancel.Font = new System.Drawing.Font("Segoe UI", 9.5F);
            this.btnCancel.ForeColor = System.Drawing.Color.FromArgb(8, 52, 87);
            this.btnCancel.Location = new System.Drawing.Point(135, 12);
            this.btnCancel.Name = "btnCancel";
            this.btnCancel.Size = new System.Drawing.Size(90, 36);
            this.btnCancel.TabIndex = 9;
            this.btnCancel.Text = "Cancel";
            this.btnCancel.UseVisualStyleBackColor = false;
            // 
            // pnlFooter
            // 
            this.pnlFooter.BackColor = System.Drawing.Color.FromArgb(248, 250, 252);
            this.pnlFooter.Controls.Add(this.btnCancel);
            this.pnlFooter.Controls.Add(this.btnSave);
            this.pnlFooter.Dock = System.Windows.Forms.DockStyle.Bottom;
            this.pnlFooter.Location = new System.Drawing.Point(0, 260);
            this.pnlFooter.Name = "pnlFooter";
            this.pnlFooter.Size = new System.Drawing.Size(400, 60);
            this.pnlFooter.TabIndex = 1;
            // 
            // pnlContent
            // 
            this.pnlContent.AutoScroll = true;
            this.pnlContent.Controls.Add(this.lblError);
            this.pnlContent.Controls.Add(this.chkShowPassword);
            this.pnlContent.Controls.Add(this.txtConfirmPassword);
            this.pnlContent.Controls.Add(this.lblConfirmPassword);
            this.pnlContent.Controls.Add(this.txtNewPassword);
            this.pnlContent.Controls.Add(this.lblNewPassword);
            this.pnlContent.Controls.Add(this.lblSubtitle);
            this.pnlContent.Controls.Add(this.lblTitle);
            this.pnlContent.Dock = System.Windows.Forms.DockStyle.Fill;
            this.pnlContent.Location = new System.Drawing.Point(0, 0);
            this.pnlContent.Name = "pnlContent";
            this.pnlContent.Size = new System.Drawing.Size(400, 260);
            this.pnlContent.TabIndex = 0;
            // 
            // ChangePasswordDialog
            // 
            this.AcceptButton = this.btnSave;
            this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 15F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.AutoScroll = false;
            this.BackColor = System.Drawing.Color.White;
            this.CancelButton = this.btnCancel;
            this.ClientSize = new System.Drawing.Size(400, 320);
            this.Controls.Add(this.pnlContent);
            this.Controls.Add(this.pnlFooter);
            this.Font = new System.Drawing.Font("Segoe UI", 9F);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.MinimumSize = new System.Drawing.Size(380, 300);
            this.Name = "ChangePasswordDialog";
            this.ShowInTaskbar = false;
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            this.Text = "Change Password";
            this.pnlFooter.ResumeLayout(false);
            this.pnlContent.ResumeLayout(false);
            this.pnlContent.PerformLayout();
            this.ResumeLayout(false);
        }

        #endregion
    }
}
