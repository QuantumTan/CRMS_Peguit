namespace CRMS_Peguit.winforms.Views.Leads
{
    partial class LeadInputForm
    {
        private System.ComponentModel.IContainer components = null;

        private System.Windows.Forms.Label lblFirstName = null!;
        private System.Windows.Forms.TextBox txtFirstName = null!;
        private System.Windows.Forms.Label lblMiddleName = null!;
        private System.Windows.Forms.TextBox txtMiddleName = null!;
        private System.Windows.Forms.Label lblLastName = null!;
        private System.Windows.Forms.TextBox txtLastName = null!;
        private System.Windows.Forms.Label lblSuffix = null!;
        private System.Windows.Forms.ComboBox cmbSuffix = null!;

        private System.Windows.Forms.Label lblEmail = null!;
        private System.Windows.Forms.TextBox txtEmail = null!;
        private System.Windows.Forms.Label lblPhone = null!;
        private System.Windows.Forms.TextBox txtPhone = null!;

        private System.Windows.Forms.Label lblSource = null!;
        private System.Windows.Forms.ComboBox cmbSource = null!;
        private System.Windows.Forms.Label lblStage = null!;
        private System.Windows.Forms.ComboBox cmbStage = null!;

        private System.Windows.Forms.Label lblPriority = null!;
        private System.Windows.Forms.ComboBox cmbPriority = null!;
        private System.Windows.Forms.Label lblExpectedValue = null!;
        private System.Windows.Forms.TextBox txtExpectedValue = null!;

        private System.Windows.Forms.Label lblNotes = null!;
        private System.Windows.Forms.TextBox txtNotes = null!;

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
            lblFirstName = new Label();
            txtFirstName = new TextBox();
            lblMiddleName = new Label();
            txtMiddleName = new TextBox();
            lblLastName = new Label();
            txtLastName = new TextBox();
            lblSuffix = new Label();
            cmbSuffix = new ComboBox();
            lblEmail = new Label();
            txtEmail = new TextBox();
            lblPhone = new Label();
            txtPhone = new TextBox();
            lblSource = new Label();
            cmbSource = new ComboBox();
            lblStage = new Label();
            cmbStage = new ComboBox();
            lblPriority = new Label();
            cmbPriority = new ComboBox();
            lblExpectedValue = new Label();
            txtExpectedValue = new TextBox();
            lblNotes = new Label();
            txtNotes = new TextBox();
            btnSave = new Button();
            btnCancel = new Button();
            pnlFooter = new Panel();
            pnlContent = new Panel();
            pnlFooter.SuspendLayout();
            pnlContent.SuspendLayout();
            SuspendLayout();
            // 
            // lblFirstName
            // 
            lblFirstName.AutoSize = true;
            lblFirstName.ForeColor = Color.FromArgb(8, 52, 87);
            lblFirstName.Location = new Point(20, 20);
            lblFirstName.Name = "lblFirstName";
            lblFirstName.Size = new Size(104, 23);
            lblFirstName.TabIndex = 0;
            lblFirstName.Text = "First Name *";
            // 
            // txtFirstName
            // 
            txtFirstName.BackColor = Color.White;
            txtFirstName.BorderStyle = BorderStyle.FixedSingle;
            txtFirstName.ForeColor = Color.FromArgb(8, 52, 87);
            txtFirstName.Location = new Point(20, 45);
            txtFirstName.MaxLength = 100;
            txtFirstName.Name = "txtFirstName";
            txtFirstName.Size = new Size(220, 30);
            txtFirstName.TabIndex = 1;
            // 
            // lblMiddleName
            // 
            lblMiddleName.AutoSize = true;
            lblMiddleName.ForeColor = Color.FromArgb(8, 52, 87);
            lblMiddleName.Location = new Point(260, 20);
            lblMiddleName.Name = "lblMiddleName";
            lblMiddleName.Size = new Size(113, 23);
            lblMiddleName.TabIndex = 2;
            lblMiddleName.Text = "Middle Name";
            // 
            // txtMiddleName
            // 
            txtMiddleName.BackColor = Color.White;
            txtMiddleName.BorderStyle = BorderStyle.FixedSingle;
            txtMiddleName.ForeColor = Color.FromArgb(8, 52, 87);
            txtMiddleName.Location = new Point(260, 45);
            txtMiddleName.MaxLength = 100;
            txtMiddleName.Name = "txtMiddleName";
            txtMiddleName.Size = new Size(220, 30);
            txtMiddleName.TabIndex = 3;
            // 
            // lblLastName
            // 
            lblLastName.AutoSize = true;
            lblLastName.ForeColor = Color.FromArgb(8, 52, 87);
            lblLastName.Location = new Point(20, 90);
            lblLastName.Name = "lblLastName";
            lblLastName.Size = new Size(103, 23);
            lblLastName.TabIndex = 4;
            lblLastName.Text = "Last Name *";
            // 
            // txtLastName
            // 
            txtLastName.BackColor = Color.White;
            txtLastName.BorderStyle = BorderStyle.FixedSingle;
            txtLastName.ForeColor = Color.FromArgb(8, 52, 87);
            txtLastName.Location = new Point(20, 115);
            txtLastName.MaxLength = 100;
            txtLastName.Name = "txtLastName";
            txtLastName.Size = new Size(220, 30);
            txtLastName.TabIndex = 5;
            // 
            // lblSuffix
            // 
            lblSuffix.AutoSize = true;
            lblSuffix.ForeColor = Color.FromArgb(8, 52, 87);
            lblSuffix.Location = new Point(260, 90);
            lblSuffix.Name = "lblSuffix";
            lblSuffix.Size = new Size(51, 23);
            lblSuffix.TabIndex = 6;
            lblSuffix.Text = "Suffix";
            // 
            // cmbSuffix
            // 
            cmbSuffix.BackColor = Color.White;
            cmbSuffix.ForeColor = Color.FromArgb(8, 52, 87);
            cmbSuffix.FormattingEnabled = true;
            cmbSuffix.Items.AddRange(new object[] { "", "Jr.", "Sr.", "II", "III", "IV", "V" });
            cmbSuffix.Location = new Point(260, 115);
            cmbSuffix.MaxLength = 20;
            cmbSuffix.Name = "cmbSuffix";
            cmbSuffix.Size = new Size(220, 31);
            cmbSuffix.TabIndex = 7;
            // 
            // lblEmail
            // 
            lblEmail.AutoSize = true;
            lblEmail.ForeColor = Color.FromArgb(8, 52, 87);
            lblEmail.Location = new Point(20, 160);
            lblEmail.Name = "lblEmail";
            lblEmail.Size = new Size(51, 23);
            lblEmail.TabIndex = 8;
            lblEmail.Text = "Email *";
            // 
            // txtEmail
            // 
            txtEmail.BackColor = Color.White;
            txtEmail.BorderStyle = BorderStyle.FixedSingle;
            txtEmail.ForeColor = Color.FromArgb(8, 52, 87);
            txtEmail.Location = new Point(20, 185);
            txtEmail.MaxLength = 255;
            txtEmail.Name = "txtEmail";
            txtEmail.Size = new Size(220, 30);
            txtEmail.TabIndex = 9;
            // 
            // lblPhone
            // 
            lblPhone.AutoSize = true;
            lblPhone.ForeColor = Color.FromArgb(8, 52, 87);
            lblPhone.Location = new Point(260, 160);
            lblPhone.Name = "lblPhone";
            lblPhone.Size = new Size(59, 23);
            lblPhone.TabIndex = 10;
            lblPhone.Text = "Phone";
            // 
            // txtPhone
            // 
            txtPhone.BackColor = Color.White;
            txtPhone.BorderStyle = BorderStyle.FixedSingle;
            txtPhone.ForeColor = Color.FromArgb(8, 52, 87);
            txtPhone.Location = new Point(260, 185);
            txtPhone.MaxLength = 50;
            txtPhone.Name = "txtPhone";
            txtPhone.Size = new Size(220, 30);
            txtPhone.TabIndex = 11;
            // 
            // lblSource
            // 
            lblSource.AutoSize = true;
            lblSource.ForeColor = Color.FromArgb(8, 52, 87);
            lblSource.Location = new Point(20, 230);
            lblSource.Name = "lblSource";
            lblSource.Size = new Size(157, 23);
            lblSource.TabIndex = 12;
            lblSource.Text = "Source / Campaign";
            // 
            // cmbSource
            // 
            cmbSource.AutoCompleteMode = AutoCompleteMode.SuggestAppend;
            cmbSource.AutoCompleteSource = AutoCompleteSource.ListItems;
            cmbSource.BackColor = Color.White;
            cmbSource.ForeColor = Color.FromArgb(8, 52, 87);
            cmbSource.FormattingEnabled = true;
            cmbSource.Location = new Point(20, 255);
            cmbSource.MaxLength = 100;
            cmbSource.Name = "cmbSource";
            cmbSource.Size = new Size(220, 31);
            cmbSource.TabIndex = 13;
            // 
            // lblStage
            // 
            lblStage.AutoSize = true;
            lblStage.ForeColor = Color.FromArgb(8, 52, 87);
            lblStage.Location = new Point(260, 230);
            lblStage.Name = "lblStage";
            lblStage.Size = new Size(52, 23);
            lblStage.TabIndex = 14;
            lblStage.Text = "Stage";
            // 
            // cmbStage
            // 
            cmbStage.BackColor = Color.White;
            cmbStage.DropDownStyle = ComboBoxStyle.DropDownList;
            cmbStage.ForeColor = Color.FromArgb(8, 52, 87);
            cmbStage.FormattingEnabled = true;
            cmbStage.Items.AddRange(new object[] { "new", "contacted", "qualified", "proposal", "negotiation", "converted", "lost" });
            cmbStage.Location = new Point(260, 255);
            cmbStage.Name = "cmbStage";
            cmbStage.Size = new Size(220, 31);
            cmbStage.TabIndex = 15;
            // 
            // lblPriority
            // 
            lblPriority.AutoSize = true;
            lblPriority.ForeColor = Color.FromArgb(8, 52, 87);
            lblPriority.Location = new Point(20, 300);
            lblPriority.Name = "lblPriority";
            lblPriority.Size = new Size(64, 23);
            lblPriority.TabIndex = 16;
            lblPriority.Text = "Priority";
            // 
            // cmbPriority
            // 
            cmbPriority.BackColor = Color.White;
            cmbPriority.DropDownStyle = ComboBoxStyle.DropDownList;
            cmbPriority.ForeColor = Color.FromArgb(8, 52, 87);
            cmbPriority.FormattingEnabled = true;
            cmbPriority.Items.AddRange(new object[] { "low", "medium", "high" });
            cmbPriority.Location = new Point(20, 325);
            cmbPriority.Name = "cmbPriority";
            cmbPriority.Size = new Size(220, 31);
            cmbPriority.TabIndex = 17;
            // 
            // lblExpectedValue
            // 
            lblExpectedValue.AutoSize = true;
            lblExpectedValue.ForeColor = Color.FromArgb(8, 52, 87);
            lblExpectedValue.Location = new Point(260, 300);
            lblExpectedValue.Name = "lblExpectedValue";
            lblExpectedValue.Size = new Size(173, 23);
            lblExpectedValue.TabIndex = 18;
            lblExpectedValue.Text = "Expected Value (PHP)";
            // 
            // txtExpectedValue
            // 
            txtExpectedValue.BackColor = Color.White;
            txtExpectedValue.BorderStyle = BorderStyle.FixedSingle;
            txtExpectedValue.ForeColor = Color.FromArgb(8, 52, 87);
            txtExpectedValue.Location = new Point(260, 325);
            txtExpectedValue.MaxLength = 18;
            txtExpectedValue.Name = "txtExpectedValue";
            txtExpectedValue.PlaceholderText = "e.g. 5000000";
            txtExpectedValue.Size = new Size(220, 30);
            txtExpectedValue.TabIndex = 19;
            // 
            // lblNotes
            // 
            lblNotes.AutoSize = true;
            lblNotes.ForeColor = Color.FromArgb(8, 52, 87);
            lblNotes.Location = new Point(20, 370);
            lblNotes.Name = "lblNotes";
            lblNotes.Size = new Size(55, 23);
            lblNotes.TabIndex = 20;
            lblNotes.Text = "Notes";
            // 
            // txtNotes
            // 
            txtNotes.BackColor = Color.White;
            txtNotes.BorderStyle = BorderStyle.FixedSingle;
            txtNotes.ForeColor = Color.FromArgb(8, 52, 87);
            txtNotes.Location = new Point(20, 395);
            txtNotes.MaxLength = 2000;
            txtNotes.Multiline = true;
            txtNotes.Name = "txtNotes";
            txtNotes.ScrollBars = ScrollBars.Vertical;
            txtNotes.Size = new Size(460, 120);
            txtNotes.TabIndex = 21;
            // 
            // btnSave
            // 
            btnSave.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            btnSave.BackColor = Color.FromArgb(37, 103, 156);
            btnSave.Cursor = Cursors.Hand;
            btnSave.FlatAppearance.BorderSize = 0;
            btnSave.FlatStyle = FlatStyle.Flat;
            btnSave.Font = new Font("Segoe UI", 10F, FontStyle.Bold);
            btnSave.ForeColor = Color.White;
            btnSave.Location = new Point(425, 11);
            btnSave.Name = "btnSave";
            btnSave.Size = new Size(95, 38);
            btnSave.TabIndex = 23;
            btnSave.Text = "Save";
            btnSave.UseVisualStyleBackColor = false;
            // 
            // btnCancel
            // 
            btnCancel.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            btnCancel.BackColor = Color.White;
            btnCancel.Cursor = Cursors.Hand;
            btnCancel.DialogResult = DialogResult.Cancel;
            btnCancel.FlatAppearance.BorderColor = Color.FromArgb(180, 198, 217);
            btnCancel.FlatStyle = FlatStyle.Flat;
            btnCancel.Font = new Font("Segoe UI", 10F, FontStyle.Bold);
            btnCancel.ForeColor = Color.FromArgb(8, 52, 87);
            btnCancel.Location = new Point(320, 11);
            btnCancel.Name = "btnCancel";
            btnCancel.Size = new Size(95, 38);
            btnCancel.TabIndex = 22;
            btnCancel.Text = "Cancel";
            btnCancel.UseVisualStyleBackColor = false;
            // 
            // pnlFooter
            // 
            pnlFooter.BackColor = Color.FromArgb(243, 247, 250);
            pnlFooter.Controls.Add(btnCancel);
            pnlFooter.Controls.Add(btnSave);
            pnlFooter.Dock = DockStyle.Bottom;
            pnlFooter.Location = new Point(0, 610);
            pnlFooter.Name = "pnlFooter";
            pnlFooter.Padding = new Padding(20, 10, 20, 10);
            pnlFooter.Size = new Size(540, 60);
            pnlFooter.TabIndex = 1;
            // 
            // pnlContent
            // 
            pnlContent.AutoScroll = true;
            pnlContent.BackColor = Color.FromArgb(243, 247, 250);
            pnlContent.Controls.Add(lblFirstName);
            pnlContent.Controls.Add(txtFirstName);
            pnlContent.Controls.Add(lblMiddleName);
            pnlContent.Controls.Add(txtMiddleName);
            pnlContent.Controls.Add(lblLastName);
            pnlContent.Controls.Add(txtLastName);
            pnlContent.Controls.Add(lblSuffix);
            pnlContent.Controls.Add(cmbSuffix);
            pnlContent.Controls.Add(lblEmail);
            pnlContent.Controls.Add(txtEmail);
            pnlContent.Controls.Add(lblPhone);
            pnlContent.Controls.Add(txtPhone);
            pnlContent.Controls.Add(lblSource);
            pnlContent.Controls.Add(cmbSource);
            pnlContent.Controls.Add(lblStage);
            pnlContent.Controls.Add(cmbStage);
            pnlContent.Controls.Add(lblPriority);
            pnlContent.Controls.Add(cmbPriority);
            pnlContent.Controls.Add(lblExpectedValue);
            pnlContent.Controls.Add(txtExpectedValue);
            pnlContent.Controls.Add(lblNotes);
            pnlContent.Controls.Add(txtNotes);
            pnlContent.Dock = DockStyle.Fill;
            pnlContent.Location = new Point(0, 0);
            pnlContent.Name = "pnlContent";
            pnlContent.Padding = new Padding(20);
            pnlContent.Size = new Size(540, 610);
            pnlContent.TabIndex = 0;
            // 
            // LeadInputForm
            // 
            AcceptButton = btnSave;
            AutoScaleDimensions = new SizeF(9F, 23F);
            AutoScaleMode = AutoScaleMode.Font;
            BackColor = Color.FromArgb(243, 247, 250);
            CancelButton = btnCancel;
            ClientSize = new Size(540, 670);
            Controls.Add(pnlContent);
            Controls.Add(pnlFooter);
            Font = new Font("Segoe UI", 10F);
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;
            MinimumSize = new Size(540, 520);
            Name = "LeadInputForm";
            ShowInTaskbar = false;
            StartPosition = FormStartPosition.CenterParent;
            Text = "Lead Details";
            pnlFooter.ResumeLayout(false);
            pnlContent.ResumeLayout(false);
            pnlContent.PerformLayout();
            ResumeLayout(false);
        }

        #endregion
    }
}
