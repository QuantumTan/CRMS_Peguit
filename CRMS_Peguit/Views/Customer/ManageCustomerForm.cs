using CRMS_Peguit.domain.entities;
using CRMS_Peguit.winforms.Controllers;

namespace CRMS_Peguit.winforms.Views.Customers
{
    public partial class ManageCustomersForm : Form
    {
        private readonly CustomerController _controller;

        public ManageCustomersForm()
        {
            InitializeComponent();

            _controller = new CustomerController();
        }

        private void FormLoad(
            object? sender,
            EventArgs e)
        {
            RefreshGrid();
        }

        private void RefreshGrid()
        {
            dataGridView1.DataSource = null;
            dataGridView1.DataSource = _controller.GetAll();

            HideColumn(nameof(Customer.CustomerId));
            HideColumn(nameof(Customer.TenantId));
            HideColumn(nameof(Customer.IsDeleted));
            HideColumn(nameof(Customer.DeletedAt));

            if (dataGridView1.Columns[nameof(Customer.FullName)]
                is DataGridViewColumn fullNameColumn)
            {
                fullNameColumn.HeaderText = "Full Name";
                fullNameColumn.DisplayIndex = 0;
            }
        }

        private void BtnAddClick(
            object? sender,
            EventArgs e)
        {
            if (!ValidateName())
            {
                return;
            }

            var customer = new Customer
            {
                FirstName = txtFirstName.Text.Trim(),

                MiddleName =
                    NullIfEmpty(txtMiddleName.Text),

                LastName = txtLastName.Text.Trim(),

                Suffix =
                    NullIfEmpty(txtSuffix.Text),

                Phone =
                    NullIfEmpty(txtPhone.Text),

                Email =
                    NullIfEmpty(txtEmail.Text),

                Type = string.IsNullOrWhiteSpace(txtType.Text)
                    ? "buyer"
                    : txtType.Text.Trim(),

                Status = "active",
                CreatedAt = DateTime.UtcNow,
                IsDeleted = false
            };

            _controller.Add(customer);

            RefreshGrid();
            ClearFields();
        }

        private void BtnUpdateClick(
            object? sender,
            EventArgs e)
        {
            if (dataGridView1.CurrentRow?.DataBoundItem
                is not Customer customer)
            {
                return;
            }

            if (!ValidateName())
            {
                return;
            }

            customer.FirstName = txtFirstName.Text.Trim();

            customer.MiddleName =
                NullIfEmpty(txtMiddleName.Text);

            customer.LastName = txtLastName.Text.Trim();

            customer.Suffix =
                NullIfEmpty(txtSuffix.Text);

            customer.Phone =
                NullIfEmpty(txtPhone.Text);

            customer.Email =
                NullIfEmpty(txtEmail.Text);

            customer.Type =
                string.IsNullOrWhiteSpace(txtType.Text)
                    ? "buyer"
                    : txtType.Text.Trim();

            _controller.Update(customer);

            RefreshGrid();
            ClearFields();
        }

        private void BtnDeleteClick(
            object? sender,
            EventArgs e)
        {
            if (dataGridView1.CurrentRow?.DataBoundItem
                is not Customer customer)
            {
                return;
            }

            DialogResult confirmation = MessageBox.Show(
                $"Archive '{customer.FullName}'?",
                "Archive Customer",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Question);

            if (confirmation != DialogResult.Yes)
            {
                return;
            }

            _controller.SoftDelete(customer);

            RefreshGrid();
            ClearFields();
        }

        private void GridSelectionChanged(
            object? sender,
            EventArgs e)
        {
            if (dataGridView1.CurrentRow?.DataBoundItem
                is not Customer customer)
            {
                return;
            }

            txtFirstName.Text = customer.FirstName;
            txtMiddleName.Text = customer.MiddleName ?? string.Empty;
            txtLastName.Text = customer.LastName;
            txtSuffix.Text = customer.Suffix ?? string.Empty;

            txtPhone.Text = customer.Phone ?? string.Empty;
            txtEmail.Text = customer.Email ?? string.Empty;
            txtType.Text = customer.Type;
        }

        private bool ValidateName()
        {
            if (string.IsNullOrWhiteSpace(txtFirstName.Text))
            {
                MessageBox.Show(
                    "First name is required.",
                    "Validation Error",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);

                txtFirstName.Focus();
                return false;
            }

            if (string.IsNullOrWhiteSpace(txtLastName.Text))
            {
                MessageBox.Show(
                    "Last name is required.",
                    "Validation Error",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);

                txtLastName.Focus();
                return false;
            }

            return true;
        }

        private void HideColumn(string columnName)
        {
            if (dataGridView1.Columns[columnName] is not null)
            {
                dataGridView1.Columns[columnName].Visible = false;
            }
        }

        private void ClearFields()
        {
            txtFirstName.Clear();
            txtMiddleName.Clear();
            txtLastName.Clear();
            txtSuffix.Clear();

            txtPhone.Clear();
            txtEmail.Clear();
            txtType.Clear();

            txtFirstName.Focus();
        }

        private static string? NullIfEmpty(string? value)
        {
            return string.IsNullOrWhiteSpace(value)
                ? null
                : value.Trim();
        }

        protected override void OnFormClosed(
            FormClosedEventArgs e)
        {
            _controller.Dispose();

            base.OnFormClosed(e);
        }
    }
}