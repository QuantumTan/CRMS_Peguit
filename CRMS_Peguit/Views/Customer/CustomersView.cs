using CRMS_Peguit.domain.entities;
using CRMS_Peguit.winforms.Controllers;
using CRMS_Peguit.winforms.Models.Services;

namespace CRMS_Peguit.winforms.Views.Customers
{
    public class CustomersView : UserControl
    {
        private readonly CustomerController _controller;

        private DataGridView grid = null!;
        private TextBox txtSearch = null!;
        private ComboBox cmbFilter = null!;
        private Button btnAdd = null!;

        public CustomersView()
        {
            _controller = new CustomerController();

            InitializeUI();
            LayoutControls();
            RefreshGrid();

            Resize += (_, _) => LayoutControls();
        }

        private void InitializeUI()
        {
            BackColor = Theme.Background;
            Padding = new Padding(30);

            Controls.Add(new Label
            {
                Text = "Customers",
                Font = new Font(
                    "Segoe UI",
                    22,
                    FontStyle.Bold),

                ForeColor = Theme.TextPrimary,
                Location = new Point(30, 25),
                AutoSize = true
            });

            txtSearch = new TextBox
            {
                PlaceholderText =
                    "Search first name, last name, email, phone...",

                Font = new Font("Segoe UI", 11),
                BackColor = Theme.Surface,
                ForeColor = Theme.TextPrimary,
                BorderStyle = BorderStyle.FixedSingle
            };

            txtSearch.TextChanged += (_, _) => RefreshGrid();

            cmbFilter = new ComboBox
            {
                DropDownStyle = ComboBoxStyle.DropDownList,
                Font = new Font("Segoe UI", 11),
                BackColor = Theme.Surface,
                ForeColor = Theme.TextPrimary
            };

            cmbFilter.Items.AddRange(
                new[]
                {
                    "All Status",
                    "active",
                    "inactive",
                    "prospect"
                });

            cmbFilter.SelectedIndex = 0;
            cmbFilter.SelectedIndexChanged += (_, _) => RefreshGrid();

            btnAdd = CreateButton(
                "+ Add Customer",
                Theme.Primary,
                Theme.Surface);

            btnAdd.Click += BtnAddClick;

            grid = new DataGridView
            {
                AutoSizeColumnsMode =
                    DataGridViewAutoSizeColumnsMode.Fill,

                ReadOnly = true,
                AllowUserToAddRows = false,
                AllowUserToDeleteRows = false,
                MultiSelect = false,

                SelectionMode =
                    DataGridViewSelectionMode.FullRowSelect,

                BackgroundColor = Theme.Surface,
                ForeColor = Theme.TextPrimary,
                GridColor = Theme.Border,
                BorderStyle = BorderStyle.None,
                RowHeadersVisible = false,
                EnableHeadersVisualStyles = false,
                ColumnHeadersHeight = 45,

                ColumnHeadersBorderStyle =
                    DataGridViewHeaderBorderStyle.None
            };

            grid.RowTemplate.Height = 45;

            grid.DefaultCellStyle.BackColor = Theme.Surface;
            grid.DefaultCellStyle.ForeColor = Theme.TextPrimary;
            grid.DefaultCellStyle.SelectionBackColor = Theme.Border;
            grid.DefaultCellStyle.SelectionForeColor = Theme.TextPrimary;
            grid.DefaultCellStyle.Padding = new Padding(6, 0, 6, 0);
            grid.DefaultCellStyle.Font = new Font("Segoe UI", 10);

            grid.ColumnHeadersDefaultCellStyle.BackColor =
                Theme.Background;

            grid.ColumnHeadersDefaultCellStyle.ForeColor =
                Theme.TextPrimary;

            grid.ColumnHeadersDefaultCellStyle.Font =
                new Font("Segoe UI", 10, FontStyle.Bold);

            grid.CellContentClick += GridCellContentClick;

            Controls.Add(txtSearch);
            Controls.Add(cmbFilter);
            Controls.Add(btnAdd);
            Controls.Add(grid);
        }

        private static Button CreateButton(
            string text,
            Color backColor,
            Color foregroundColor)
        {
            var button = new Button
            {
                Text = text,
                FlatStyle = FlatStyle.Flat,
                BackColor = backColor,
                ForeColor = foregroundColor,

                Font = new Font(
                    "Segoe UI",
                    10,
                    FontStyle.Bold),

                Cursor = Cursors.Hand
            };

            button.FlatAppearance.BorderSize = 0;

            return button;
        }

        private void LayoutControls()
        {
            int availableWidth = Math.Max(0, Width - 60);
            int x = 30;

            txtSearch.Location = new Point(x, 75);
            txtSearch.Size = new Size(
                Math.Max(200, (int)(availableWidth * 0.35)),
                36);

            cmbFilter.Location = new Point(
                x + txtSearch.Width + 15,
                75);

            cmbFilter.Size = new Size(150, 36);

            btnAdd.Location = new Point(
                x + txtSearch.Width + cmbFilter.Width + 30,
                73);

            btnAdd.Size = new Size(150, 38);

            grid.Location = new Point(x, 130);
            grid.Size = new Size(
                availableWidth,
                Math.Max(0, Height - 160));
        }

        private void RefreshGrid()
        {
            grid.Columns.Clear();

            IEnumerable<Customer> query =
                _controller.GetAll();

            string search = txtSearch.Text.Trim();

            if (!string.IsNullOrWhiteSpace(search))
            {
                query = query.Where(customer =>
                    ContainsText(customer.FirstName, search) ||
                    ContainsText(customer.MiddleName, search) ||
                    ContainsText(customer.LastName, search) ||
                    ContainsText(customer.Suffix, search) ||
                    ContainsText(customer.FullName, search) ||
                    ContainsText(customer.Email, search) ||
                    ContainsText(customer.Phone, search));
            }

            string? filter =
                cmbFilter.SelectedItem?.ToString();

            if (!string.IsNullOrWhiteSpace(filter) &&
                filter != "All Status")
            {
                query = query.Where(customer =>
                    string.Equals(
                        customer.Status,
                        filter,
                        StringComparison.OrdinalIgnoreCase));
            }

            grid.DataSource = query
                .Select(customer => new
                {
                    customer.CustomerId,

                    Name = customer.FullName,

                    Email = string.IsNullOrWhiteSpace(customer.Email)
                        ? "-"
                        : customer.Email,

                    Phone = string.IsNullOrWhiteSpace(customer.Phone)
                        ? "-"
                        : customer.Phone,

                    customer.Type,
                    customer.Status
                })
                .ToList();

            if (grid.Columns["CustomerId"] is not null)
            {
                grid.Columns["CustomerId"].Visible = false;
            }

            if (grid.Columns["Name"] is not null)
            {
                grid.Columns["Name"].HeaderText = "Full Name";
                grid.Columns["Name"].FillWeight = 160;
            }

            grid.Columns.Add(new ActionsColumn());
        }

        private Customer? GetSelectedCustomer()
        {
            if (grid.CurrentRow is null)
            {
                return null;
            }

            object? idValue =
                grid.CurrentRow.Cells["CustomerId"].Value;

            if (idValue is null ||
                !int.TryParse(
                    idValue.ToString(),
                    out int customerId))
            {
                return null;
            }

            return _controller
                .GetAll()
                .FirstOrDefault(customer =>
                    customer.CustomerId == customerId);
        }

        private void GridCellContentClick(
            object? sender,
            DataGridViewCellEventArgs e)
        {
            if (e.RowIndex < 0 || e.ColumnIndex < 0)
            {
                return;
            }

            if (grid.Columns[e.ColumnIndex] is not ActionsColumn)
            {
                return;
            }

            grid.Rows[e.RowIndex].Selected = true;
            grid.CurrentCell =
                grid.Rows[e.RowIndex].Cells[e.ColumnIndex];

            Customer? customer = GetSelectedCustomer();

            if (customer is null)
            {
                return;
            }

            var menu = new ContextMenuStrip
            {
                BackColor = Theme.Surface,
                ForeColor = Theme.TextPrimary,
                Font = new Font("Segoe UI", 10)
            };

            var editItem = new ToolStripMenuItem("Edit");

            editItem.Click += (_, _) =>
                EditCustomer(customer);

            var archiveItem =
                new ToolStripMenuItem("Archive");

            archiveItem.Click += (_, _) =>
                ArchiveCustomer(customer);

            menu.Items.Add(editItem);
            menu.Items.Add(archiveItem);

            menu.Show(
                grid,
                grid.PointToClient(Cursor.Position));
        }

        private void EditCustomer(Customer customer)
        {
            using var form =
                new CustomerInputForm(customer);

            if (form.ShowDialog() == DialogResult.OK &&
                form.Result is not null)
            {
                _controller.Update(form.Result);
                RefreshGrid();
            }
        }

        private void ArchiveCustomer(Customer customer)
        {
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
        }

        private void BtnAddClick(
            object? sender,
            EventArgs e)
        {
            using var form = new CustomerInputForm();

            if (form.ShowDialog() == DialogResult.OK &&
                form.Result is not null)
            {
                _controller.Add(form.Result);
                RefreshGrid();
            }
        }

        private static bool ContainsText(
            string? value,
            string search)
        {
            return !string.IsNullOrWhiteSpace(value) &&
                   value.Contains(
                       search,
                       StringComparison.OrdinalIgnoreCase);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _controller.Dispose();
            }

            base.Dispose(disposing);
        }
    }
}