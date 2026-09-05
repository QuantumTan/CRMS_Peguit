namespace CRMS_Peguit.winforms.Views.Customers
{
    partial class ManageCustomersForm
    {
        private System.ComponentModel.IContainer? components;

        private DataGridView dataGridView1 = null!;

        private TextBox txtFirstName = null!;
        private TextBox txtMiddleName = null!;
        private TextBox txtLastName = null!;
        private TextBox txtSuffix = null!;

        private TextBox txtPhone = null!;
        private TextBox txtEmail = null!;
        private TextBox txtType = null!;

        private Button btnAdd = null!;
        private Button btnUpdate = null!;
        private Button btnDelete = null!;

        private Label lblFirstName = null!;
        private Label lblMiddleName = null!;
        private Label lblLastName = null!;
        private Label lblSuffix = null!;
        private Label lblPhone = null!;
        private Label lblEmail = null!;
        private Label lblType = null!;

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                components?.Dispose();
            }

            base.Dispose(disposing);
        }

        private void InitializeComponent()
        {
            dataGridView1 = new DataGridView();

            txtFirstName = new TextBox();
            txtMiddleName = new TextBox();
            txtLastName = new TextBox();
            txtSuffix = new TextBox();

            txtPhone = new TextBox();
            txtEmail = new TextBox();
            txtType = new TextBox();

            btnAdd = new Button();
            btnUpdate = new Button();
            btnDelete = new Button();

            lblFirstName = new Label();
            lblMiddleName = new Label();
            lblLastName = new Label();
            lblSuffix = new Label();
            lblPhone = new Label();
            lblEmail = new Label();
            lblType = new Label();

            ((System.ComponentModel.ISupportInitialize)dataGridView1)
                .BeginInit();

            SuspendLayout();

            dataGridView1.Location = new Point(12, 12);
            dataGridView1.Size = new Size(860, 350);
            dataGridView1.AutoSizeColumnsMode =
                DataGridViewAutoSizeColumnsMode.Fill;

            dataGridView1.ReadOnly = true;
            dataGridView1.AllowUserToAddRows = false;
            dataGridView1.AllowUserToDeleteRows = false;
            dataGridView1.MultiSelect = false;

            dataGridView1.SelectionMode =
                DataGridViewSelectionMode.FullRowSelect;

            dataGridView1.SelectionChanged +=
                GridSelectionChanged;

            ConfigureLabel(
                lblFirstName,
                "First Name:",
                12,
                380);

            ConfigureTextBox(
                txtFirstName,
                105,
                377,
                180);

            ConfigureLabel(
                lblMiddleName,
                "Middle Name:",
                310,
                380);

            ConfigureTextBox(
                txtMiddleName,
                410,
                377,
                180);

            ConfigureLabel(
                lblLastName,
                "Last Name:",
                12,
                420);

            ConfigureTextBox(
                txtLastName,
                105,
                417,
                180);

            ConfigureLabel(
                lblSuffix,
                "Suffix:",
                310,
                420);

            ConfigureTextBox(
                txtSuffix,
                410,
                417,
                180);

            ConfigureLabel(
                lblPhone,
                "Phone:",
                12,
                460);

            ConfigureTextBox(
                txtPhone,
                105,
                457,
                180);

            ConfigureLabel(
                lblEmail,
                "Email:",
                310,
                460);

            ConfigureTextBox(
                txtEmail,
                410,
                457,
                250);

            ConfigureLabel(
                lblType,
                "Type:",
                12,
                500);

            ConfigureTextBox(
                txtType,
                105,
                497,
                180);

            ConfigureButton(
                btnAdd,
                "Add",
                12,
                545,
                Color.FromArgb(45, 45, 68));

            btnAdd.Click += BtnAddClick;

            ConfigureButton(
                btnUpdate,
                "Update",
                110,
                545,
                Color.FromArgb(45, 45, 68));

            btnUpdate.Click += BtnUpdateClick;

            ConfigureButton(
                btnDelete,
                "Archive",
                208,
                545,
                Color.FromArgb(200, 60, 60));

            btnDelete.Click += BtnDeleteClick;

            ClientSize = new Size(885, 600);
            BackColor = Color.FromArgb(245, 245, 250);
            Text = "Manage Customers";
            StartPosition = FormStartPosition.CenterScreen;

            Controls.AddRange(
                new Control[]
                {
                    dataGridView1,

                    lblFirstName,
                    txtFirstName,
                    lblMiddleName,
                    txtMiddleName,
                    lblLastName,
                    txtLastName,
                    lblSuffix,
                    txtSuffix,

                    lblPhone,
                    txtPhone,
                    lblEmail,
                    txtEmail,
                    lblType,
                    txtType,

                    btnAdd,
                    btnUpdate,
                    btnDelete
                });

            Load += FormLoad;

            ((System.ComponentModel.ISupportInitialize)dataGridView1)
                .EndInit();

            ResumeLayout(false);
            PerformLayout();
        }

        private static void ConfigureLabel(
            Label label,
            string text,
            int x,
            int y)
        {
            label.Text = text;
            label.Location = new Point(x, y);
            label.AutoSize = true;
        }

        private static void ConfigureTextBox(
            TextBox textBox,
            int x,
            int y,
            int width)
        {
            textBox.Location = new Point(x, y);
            textBox.Size = new Size(width, 27);
        }

        private static void ConfigureButton(
            Button button,
            string text,
            int x,
            int y,
            Color backColor)
        {
            button.Text = text;
            button.Location = new Point(x, y);
            button.Size = new Size(90, 35);
            button.BackColor = backColor;
            button.ForeColor = Color.White;
            button.FlatStyle = FlatStyle.Flat;
        }
    }
}