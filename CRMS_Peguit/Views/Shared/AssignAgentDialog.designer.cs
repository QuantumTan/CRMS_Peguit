namespace CRMS_Peguit.winforms.Views.Shared
{
    partial class AssignAgentDialog
    {
        private System.ComponentModel.IContainer components = null;

        // Layout containers
        private System.Windows.Forms.Panel pnlAccent = null!;
        private System.Windows.Forms.Panel pnlApproveCard = null!;
        private System.Windows.Forms.Panel pnlFooter = null!;
        private System.Windows.Forms.Panel pnlContent = null!;

        // Header section
        private System.Windows.Forms.Label lblHeader = null!;
        private System.Windows.Forms.Label lblSubtitle = null!;

        // Record info
        private System.Windows.Forms.Label lblRecord = null!;

        // Agent picker
        private System.Windows.Forms.Label lblAgent = null!;
        private System.Windows.Forms.Label lblAgentCount = null!;
        private System.Windows.Forms.ComboBox cmbAgents = null!;

        // Notes section
        private System.Windows.Forms.Label lblNotes = null!;
        private System.Windows.Forms.TextBox txtNotes = null!;

        // Approve checkbox inside its card
        private System.Windows.Forms.CheckBox chkApprove = null!;
        private System.Windows.Forms.Label lblApproveDesc = null!;

        // Action buttons
        private System.Windows.Forms.Button btnSave = null!;
        private System.Windows.Forms.Button btnCancel = null!;

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
            this.pnlAccent = new System.Windows.Forms.Panel();
            this.pnlApproveCard = new System.Windows.Forms.Panel();
            this.pnlFooter = new System.Windows.Forms.Panel();
            this.pnlContent = new System.Windows.Forms.Panel();
            this.lblHeader = new System.Windows.Forms.Label();
            this.lblSubtitle = new System.Windows.Forms.Label();
            this.lblRecord = new System.Windows.Forms.Label();
            this.lblAgent = new System.Windows.Forms.Label();
            this.lblAgentCount = new System.Windows.Forms.Label();
            this.cmbAgents = new System.Windows.Forms.ComboBox();
            this.lblNotes = new System.Windows.Forms.Label();
            this.txtNotes = new System.Windows.Forms.TextBox();
            this.chkApprove = new System.Windows.Forms.CheckBox();
            this.lblApproveDesc = new System.Windows.Forms.Label();
            this.btnSave = new System.Windows.Forms.Button();
            this.btnCancel = new System.Windows.Forms.Button();

            this.pnlFooter.SuspendLayout();
            this.pnlContent.SuspendLayout();
            this.pnlApproveCard.SuspendLayout();
            this.SuspendLayout();

            // pnlAccent — 5px primary-blue top strip
            this.pnlAccent.BackColor = System.Drawing.Color.FromArgb(37, 103, 156);
            this.pnlAccent.Dock = System.Windows.Forms.DockStyle.Top;
            this.pnlAccent.Height = 5;
            this.pnlAccent.Name = "pnlAccent";
            this.pnlAccent.TabIndex = 0;

            // lblHeader
            this.lblHeader.AutoSize = true;
            this.lblHeader.Font = new System.Drawing.Font("Segoe UI", 13F, System.Drawing.FontStyle.Bold);
            this.lblHeader.ForeColor = System.Drawing.Color.FromArgb(8, 52, 87);
            this.lblHeader.Location = new System.Drawing.Point(24, 20);
            this.lblHeader.Name = "lblHeader";
            this.lblHeader.TabIndex = 1;
            this.lblHeader.Text = "👤  Assign Agent";

            // lblSubtitle
            this.lblSubtitle.AutoSize = true;
            this.lblSubtitle.Font = new System.Drawing.Font("Segoe UI", 8.5F);
            this.lblSubtitle.ForeColor = System.Drawing.Color.FromArgb(100, 116, 139);
            this.lblSubtitle.Location = new System.Drawing.Point(26, 50);
            this.lblSubtitle.Name = "lblSubtitle";
            this.lblSubtitle.TabIndex = 2;
            this.lblSubtitle.Text = "Assign a sales agent to handle this record and set review status.";

            // lblRecord — record name chip
            this.lblRecord.AutoEllipsis = true;
            this.lblRecord.BackColor = System.Drawing.Color.FromArgb(241, 245, 249);
            this.lblRecord.Font = new System.Drawing.Font("Segoe UI", 9.5F, System.Drawing.FontStyle.Bold);
            this.lblRecord.ForeColor = System.Drawing.Color.FromArgb(30, 41, 59);
            this.lblRecord.Location = new System.Drawing.Point(24, 76);
            this.lblRecord.Name = "lblRecord";
            this.lblRecord.Padding = new System.Windows.Forms.Padding(10, 6, 10, 6);
            this.lblRecord.Size = new System.Drawing.Size(412, 30);
            this.lblRecord.TabIndex = 3;
            this.lblRecord.Text = "Record: —";

            // lblAgent
            this.lblAgent.AutoSize = true;
            this.lblAgent.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Bold);
            this.lblAgent.ForeColor = System.Drawing.Color.FromArgb(8, 52, 87);
            this.lblAgent.Location = new System.Drawing.Point(24, 120);
            this.lblAgent.Name = "lblAgent";
            this.lblAgent.TabIndex = 4;
            this.lblAgent.Text = "Assign To Agent *";

            // lblAgentCount — right-aligned "X agents available"
            this.lblAgentCount.AutoSize = true;
            this.lblAgentCount.Font = new System.Drawing.Font("Segoe UI", 8.5F);
            this.lblAgentCount.ForeColor = System.Drawing.Color.FromArgb(100, 116, 139);
            this.lblAgentCount.Location = new System.Drawing.Point(290, 122);
            this.lblAgentCount.Name = "lblAgentCount";
            this.lblAgentCount.Size = new System.Drawing.Size(150, 15);
            this.lblAgentCount.TabIndex = 5;
            this.lblAgentCount.Text = "";
            this.lblAgentCount.TextAlign = System.Drawing.ContentAlignment.MiddleRight;

            // cmbAgents
            this.cmbAgents.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cmbAgents.Font = new System.Drawing.Font("Segoe UI", 10F);
            this.cmbAgents.FormattingEnabled = true;
            this.cmbAgents.Location = new System.Drawing.Point(24, 142);
            this.cmbAgents.Name = "cmbAgents";
            this.cmbAgents.Size = new System.Drawing.Size(412, 25);
            this.cmbAgents.TabIndex = 6;

            // lblNotes
            this.lblNotes.AutoSize = true;
            this.lblNotes.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Bold);
            this.lblNotes.ForeColor = System.Drawing.Color.FromArgb(8, 52, 87);
            this.lblNotes.Location = new System.Drawing.Point(24, 180);
            this.lblNotes.Name = "lblNotes";
            this.lblNotes.TabIndex = 7;
            this.lblNotes.Text = "Review Notes  (optional)";

            // txtNotes
            this.txtNotes.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.txtNotes.Font = new System.Drawing.Font("Segoe UI", 9.5F);
            this.txtNotes.ForeColor = System.Drawing.Color.FromArgb(15, 23, 42);
            this.txtNotes.BackColor = System.Drawing.Color.FromArgb(248, 250, 252);
            this.txtNotes.Location = new System.Drawing.Point(24, 200);
            this.txtNotes.Multiline = true;
            this.txtNotes.Name = "txtNotes";
            this.txtNotes.PlaceholderText = "e.g. Reassigning due to territory realignment...";
            this.txtNotes.ScrollBars = System.Windows.Forms.ScrollBars.Vertical;
            this.txtNotes.Size = new System.Drawing.Size(412, 60);
            this.txtNotes.TabIndex = 8;

            // pnlApproveCard — light card containing checkbox + description
            this.pnlApproveCard.BackColor = System.Drawing.Color.FromArgb(240, 249, 255);
            this.pnlApproveCard.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.pnlApproveCard.Location = new System.Drawing.Point(24, 274);
            this.pnlApproveCard.Name = "pnlApproveCard";
            this.pnlApproveCard.Size = new System.Drawing.Size(412, 52);
            this.pnlApproveCard.TabIndex = 9;

            // chkApprove — inside pnlApproveCard
            this.chkApprove.AutoSize = true;
            this.chkApprove.Checked = true;
            this.chkApprove.CheckState = System.Windows.Forms.CheckState.Checked;
            this.chkApprove.Font = new System.Drawing.Font("Segoe UI", 9.5F, System.Drawing.FontStyle.Bold);
            this.chkApprove.ForeColor = System.Drawing.Color.FromArgb(3, 105, 161);
            this.chkApprove.Location = new System.Drawing.Point(12, 8);
            this.chkApprove.Name = "chkApprove";
            this.chkApprove.TabIndex = 0;
            this.chkApprove.Text = "Auto-approve upon assignment";
            this.chkApprove.UseVisualStyleBackColor = true;

            // lblApproveDesc — inside pnlApproveCard
            this.lblApproveDesc.AutoSize = true;
            this.lblApproveDesc.Font = new System.Drawing.Font("Segoe UI", 8F);
            this.lblApproveDesc.ForeColor = System.Drawing.Color.FromArgb(71, 85, 105);
            this.lblApproveDesc.Location = new System.Drawing.Point(32, 30);
            this.lblApproveDesc.Name = "lblApproveDesc";
            this.lblApproveDesc.TabIndex = 1;
            this.lblApproveDesc.Text = "Uncheck to send this assignment for separate manager review.";

            // Wire children into pnlApproveCard
            this.pnlApproveCard.Controls.Add(this.chkApprove);
            this.pnlApproveCard.Controls.Add(this.lblApproveDesc);

            // pnlContent
            this.pnlContent.AutoScroll = true;
            this.pnlContent.BackColor = System.Drawing.Color.White;
            this.pnlContent.Controls.Add(this.lblHeader);
            this.pnlContent.Controls.Add(this.lblSubtitle);
            this.pnlContent.Controls.Add(this.lblRecord);
            this.pnlContent.Controls.Add(this.lblAgent);
            this.pnlContent.Controls.Add(this.lblAgentCount);
            this.pnlContent.Controls.Add(this.cmbAgents);
            this.pnlContent.Controls.Add(this.lblNotes);
            this.pnlContent.Controls.Add(this.txtNotes);
            this.pnlContent.Controls.Add(this.pnlApproveCard);
            this.pnlContent.Dock = System.Windows.Forms.DockStyle.Fill;
            this.pnlContent.Location = new System.Drawing.Point(0, 5);
            this.pnlContent.Name = "pnlContent";
            this.pnlContent.Padding = new System.Windows.Forms.Padding(20);
            this.pnlContent.Size = new System.Drawing.Size(480, 355);
            this.pnlContent.TabIndex = 1;

            // btnCancel
            this.btnCancel.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.btnCancel.BackColor = System.Drawing.Color.White;
            this.btnCancel.DialogResult = System.Windows.Forms.DialogResult.Cancel;
            this.btnCancel.FlatAppearance.BorderColor = System.Drawing.Color.FromArgb(203, 213, 225);
            this.btnCancel.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnCancel.Font = new System.Drawing.Font("Segoe UI", 9.5F);
            this.btnCancel.ForeColor = System.Drawing.Color.FromArgb(71, 85, 105);
            this.btnCancel.Location = new System.Drawing.Point(234, 12);
            this.btnCancel.Name = "btnCancel";
            this.btnCancel.Size = new System.Drawing.Size(104, 36);
            this.btnCancel.TabIndex = 10;
            this.btnCancel.Text = "Cancel";
            this.btnCancel.UseVisualStyleBackColor = false;

            // btnSave
            this.btnSave.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.btnSave.BackColor = System.Drawing.Color.FromArgb(37, 103, 156);
            this.btnSave.FlatAppearance.BorderSize = 0;
            this.btnSave.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnSave.Font = new System.Drawing.Font("Segoe UI", 9.5F, System.Drawing.FontStyle.Bold);
            this.btnSave.ForeColor = System.Drawing.Color.White;
            this.btnSave.Location = new System.Drawing.Point(348, 12);
            this.btnSave.Name = "btnSave";
            this.btnSave.Size = new System.Drawing.Size(102, 36);
            this.btnSave.TabIndex = 11;
            this.btnSave.Text = "Assign ✓";
            this.btnSave.UseVisualStyleBackColor = false;

            // pnlFooter
            this.pnlFooter.BackColor = System.Drawing.Color.FromArgb(248, 250, 252);
            this.pnlFooter.Controls.Add(this.btnCancel);
            this.pnlFooter.Controls.Add(this.btnSave);
            this.pnlFooter.Dock = System.Windows.Forms.DockStyle.Bottom;
            this.pnlFooter.Location = new System.Drawing.Point(0, 360);
            this.pnlFooter.Name = "pnlFooter";
            this.pnlFooter.Padding = new System.Windows.Forms.Padding(20, 10, 20, 10);
            this.pnlFooter.Size = new System.Drawing.Size(480, 60);
            this.pnlFooter.TabIndex = 2;

            // AssignAgentDialog — root
            this.AcceptButton = this.btnSave;
            this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 15F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.AutoScroll = false;
            this.BackColor = System.Drawing.Color.White;
            this.CancelButton = this.btnCancel;
            this.ClientSize = new System.Drawing.Size(480, 420);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.MinimumSize = new System.Drawing.Size(480, 420);
            this.Name = "AssignAgentDialog";
            this.ShowIcon = false;
            this.ShowInTaskbar = false;
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            this.Text = "Assign Agent";
            this.Font = new System.Drawing.Font("Segoe UI", 9F);

            this.Controls.Add(this.pnlContent);
            this.Controls.Add(this.pnlFooter);
            this.Controls.Add(this.pnlAccent);

            this.pnlApproveCard.ResumeLayout(false);
            this.pnlApproveCard.PerformLayout();
            this.pnlContent.ResumeLayout(false);
            this.pnlContent.PerformLayout();
            this.pnlFooter.ResumeLayout(false);
            this.ResumeLayout(false);
        }

        #endregion
    }
}

