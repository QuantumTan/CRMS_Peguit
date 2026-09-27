using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using CRMS_Peguit.domain.entities;
using CRMS_Peguit.winforms.Auth;
using CRMS_Peguit.winforms.Controllers;
using CRMS_Peguit.winforms.Controls;
using CRMS_Peguit.winforms.Models.Services;
using CRMS_Peguit.winforms.Services;

namespace CRMS_Peguit.winforms.Views.Archives
{
    public class ArchivedItemDto
    {
        public string Id { get; set; } = string.Empty;
        public string EntityType { get; set; } = string.Empty; // "Customer", "Lead", "User", "Ticket", "Task"
        public string Name { get; set; } = string.Empty;
        public string Subtitle { get; set; } = string.Empty;
        public DateTime? ArchivedAt { get; set; }
        public object RawEntity { get; set; } = null!;
    }

    public class ArchivesView : UserControl
    {
        private readonly CustomerController _customerController;
        private readonly LeadController _leadController;
        private readonly UserController _userController;
        private readonly SupportTicketController _supportTicketController;
        private readonly FollowUpController _followUpController;

        private Panel pnlCard = null!;
        private Label lblTitle = null!;
        private Label lblSubtitle = null!;
        private TextBox txtSearch = null!;
        private Button btnRefresh = null!;
        private Button btnRestoreAll = null!;
        private Button btnEmptyBin = null!;
        private Button btnFilterAll = null!;
        private Button btnFilterCustomers = null!;
        private Button btnFilterLeads = null!;
        private Button btnFilterTasks = null!;
        private Button btnFilterTickets = null!;
        private Button btnFilterUsers = null!;
        private DataGridView grid = null!;
        private GridSkeletonOverlay? _gridSkeleton;
        private Panel _pnlEmptyState = null!;

        private string _filterType = "All";
        private List<ArchivedItemDto> _allItems = new();
        private bool _isLoading = false;
        private PaginationControl _pagination = null!;

        public ArchivesView()
        {
            _customerController = new CustomerController();
            _leadController = new LeadController();
            _userController = new UserController();
            _supportTicketController = new SupportTicketController();
            _followUpController = new FollowUpController();

            InitializeComponent();
            InitPagination();
            ApplyStyling();
            BindEvents();
            UpdateFilterPillStyles();

            this.Load += async (_, _) => await RefreshDataAsync();
            this.Resize += (_, _) => LayoutToolbar();
        }

        private void InitPagination()
        {
            _pagination = new PaginationControl();
            _pagination.SetItemLabel("archived items");
            _pagination.PageChanged += (_, _) => ApplyFilterAndDisplay(resetPage: false);
            _pagination.PageSizeChanged += (_, _) => ApplyFilterAndDisplay(resetPage: true);
            pnlCard.Controls.Add(_pagination);
            _pagination.BringToFront();
        }

        private void InitializeComponent()
        {
            this.SuspendLayout();

            this.BackColor = Theme.Background;
            this.Dock = DockStyle.Fill;
            this.Font = new Font("Segoe UI", 9.5f);

            // Title
            lblTitle = new Label
            {
                Text = "🗑 Recycle Bin & Archives",
                Font = new Font("Segoe UI", 16f, FontStyle.Bold),
                ForeColor = Theme.TextPrimary,
                Location = new Point(24, 20),
                AutoSize = true
            };
            this.Controls.Add(lblTitle);

            // Subtitle
            lblSubtitle = new Label
            {
                Text = "Review, restore, or permanently purge archived records and deactivated accounts.",
                Font = new Font("Segoe UI", 9.5f),
                ForeColor = Theme.TextSecondary,
                Location = new Point(25, 52),
                AutoSize = true
            };
            this.Controls.Add(lblSubtitle);

            // Search
            txtSearch = new TextBox
            {
                PlaceholderText = "Search archived records...",
                Font = new Font("Segoe UI", 10f),
                Height = UiStyleConstants.ToolbarRowHeight,
                Width = 240
            };
            this.Controls.Add(txtSearch);

            // Filter pills
            btnFilterAll = CreateFilterButton("All Records");
            btnFilterCustomers = CreateFilterButton("Customers");
            btnFilterLeads = CreateFilterButton("Leads");
            btnFilterTasks = CreateFilterButton("Follow-Ups & Tasks");
            btnFilterTickets = CreateFilterButton("Support Tickets");
            btnFilterUsers = CreateFilterButton("Deactivated Users");

            this.Controls.Add(btnFilterAll);
            this.Controls.Add(btnFilterCustomers);
            this.Controls.Add(btnFilterLeads);
            this.Controls.Add(btnFilterTasks);
            this.Controls.Add(btnFilterTickets);
            this.Controls.Add(btnFilterUsers);

            // Action buttons
            btnRefresh = new Button
            {
                Text = "🔄 Refresh",
                BackColor = Color.White,
                ForeColor = Theme.TextPrimary,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 9.5f, FontStyle.Bold),
                Size = new Size(100, UiStyleConstants.ToolbarRowHeight),
                Cursor = Cursors.Hand
            };
            btnRefresh.FlatAppearance.BorderColor = Color.FromArgb(203, 213, 225);
            this.Controls.Add(btnRefresh);

            btnRestoreAll = new Button
            {
                Text = "♻ Restore All",
                BackColor = Color.FromArgb(240, 253, 244),
                ForeColor = Color.FromArgb(22, 101, 52),
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 9.5f, FontStyle.Bold),
                Size = new Size(115, UiStyleConstants.ToolbarRowHeight),
                Cursor = Cursors.Hand
            };
            btnRestoreAll.FlatAppearance.BorderColor = Color.FromArgb(187, 247, 208);
            this.Controls.Add(btnRestoreAll);

            btnEmptyBin = new Button
            {
                Text = "🗑 Empty Bin",
                BackColor = Color.FromArgb(254, 242, 242),
                ForeColor = Color.FromArgb(185, 28, 28),
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 9.5f, FontStyle.Bold),
                Size = new Size(115, UiStyleConstants.ToolbarRowHeight),
                Cursor = Cursors.Hand
            };
            btnEmptyBin.FlatAppearance.BorderColor = Color.FromArgb(254, 202, 202);
            this.Controls.Add(btnEmptyBin);

            // Card Panel
            pnlCard = new Panel
            {
                BackColor = Theme.Surface
            };
            this.Controls.Add(pnlCard);

            // DataGridView
            grid = new DataGridView
            {
                Dock = DockStyle.Fill,
                BackgroundColor = Theme.Surface,
                BorderStyle = BorderStyle.None,
                CellBorderStyle = DataGridViewCellBorderStyle.SingleHorizontal,
                GridColor = Color.FromArgb(241, 245, 249),
                RowHeadersVisible = false,
                AllowUserToAddRows = false,
                AllowUserToDeleteRows = false,
                AllowUserToResizeRows = false,
                ReadOnly = true,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                MultiSelect = false,
                AutoGenerateColumns = false
            };

            ConfigureGridColumns();
            pnlCard.Controls.Add(grid);

            // Empty state
            _pnlEmptyState = UiGridHelper.CreateEmptyStatePanel(
                KpiIconType.Briefcase,
                "Recycle Bin is Empty",
                "No archived records or deactivated accounts match your criteria.",
                () => { txtSearch.Clear(); SetFilter("All"); },
                "Reset Search & Filters"
            );
            pnlCard.Controls.Add(_pnlEmptyState);

            this.ResumeLayout(false);
            this.PerformLayout();
        }

        private Button CreateFilterButton(string text)
        {
            var btn = new Button
            {
                Text = text,
                AutoSize = true,
                Height = UiStyleConstants.ToolbarRowHeight,
                Cursor = Cursors.Hand,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 9f, FontStyle.Regular),
                Padding = new Padding(12, 0, 12, 0)
            };
            btn.FlatAppearance.BorderSize = 1;
            btn.FlatAppearance.BorderColor = Color.FromArgb(226, 232, 240);
            return btn;
        }

        private void ConfigureGridColumns()
        {
            grid.Columns.Clear();

            var colId = new DataGridViewTextBoxColumn { Name = "Id", Visible = false };
            var colType = new DataGridViewTextBoxColumn { Name = "Type", HeaderText = "RECORD TYPE", Width = 140 };
            var colName = new DataGridViewTextBoxColumn { Name = "Name", HeaderText = "NAME / TITLE", Width = 260 };
            var colSubtitle = new DataGridViewTextBoxColumn { Name = "Subtitle", HeaderText = "IDENTIFIER / DETAILS", AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill };
            var colDate = new DataGridViewTextBoxColumn { Name = "ArchivedDate", HeaderText = "ARCHIVED / DEACTIVATED", Width = 190 };
            
            var colRestore = new DataGridViewButtonColumn
            {
                Name = "Action",
                HeaderText = "RESTORE",
                Text = "♻ Restore",
                UseColumnTextForButtonValue = true,
                Width = 110
            };

            var colDelete = new DataGridViewButtonColumn
            {
                Name = "DeleteAction",
                HeaderText = "PURGE",
                Text = "🗑 Delete",
                UseColumnTextForButtonValue = true,
                Width = 110
            };

            grid.Columns.AddRange(colId, colType, colName, colSubtitle, colDate, colRestore, colDelete);
        }

        private void ApplyStyling()
        {
            UiRadiusHelper.StyleCard(pnlCard, 12);
            UiRadiusHelper.StyleButton(btnRefresh, 8);
            UiRadiusHelper.StyleButton(btnRestoreAll, 8);
            UiRadiusHelper.StyleButton(btnEmptyBin, 8);
            UiRadiusHelper.ApplyModernGridStyle(grid, 50);
            _gridSkeleton = GridSkeletonOverlay.CreateForGrid(grid);
            UiRadiusHelper.SetPadding(txtSearch, 10, 8);
        }

        private void BindEvents()
        {
            btnRefresh.Click += async (_, _) => await RefreshDataAsync();
            btnRestoreAll.Click += async (_, _) => await RestoreAllFilteredAsync();
            btnEmptyBin.Click += async (_, _) => await EmptyBinFilteredAsync();
            txtSearch.TextChanged += (_, _) => ApplyFilterAndDisplay();

            btnFilterAll.Click += (_, _) => SetFilter("All");
            btnFilterCustomers.Click += (_, _) => SetFilter("Customer");
            btnFilterLeads.Click += (_, _) => SetFilter("Lead");
            btnFilterTasks.Click += (_, _) => SetFilter("Task");
            btnFilterTickets.Click += (_, _) => SetFilter("Ticket");
            btnFilterUsers.Click += (_, _) => SetFilter("User");

            grid.CellContentClick += async (s, e) =>
            {
                if (e.RowIndex < 0 || e.ColumnIndex < 0) return;
                string colName = grid.Columns[e.ColumnIndex].Name;

                if (grid.Rows[e.RowIndex].Tag is ArchivedItemDto item)
                {
                    if (colName == "Action")
                    {
                        await RestoreItemAsync(item);
                    }
                    else if (colName == "DeleteAction")
                    {
                        await PermanentDeleteItemAsync(item);
                    }
                }
            };

            grid.CellPainting += (s, e) =>
            {
                if (e.RowIndex < 0 || e.Graphics is null) return;
                if (grid.Columns[e.ColumnIndex].Name == "Type" && e.Value != null)
                {
                    UiGridHelper.PaintStatusBadge(grid, e, e.Value.ToString() ?? "", center: false);
                }
            };
        }

        public void SetFilter(string type)
        {
            _filterType = type;
            UpdateFilterPillStyles();
            ApplyFilterAndDisplay();
        }

        private void UpdateFilterPillStyles()
        {
            var pills = new[]
            {
                (btnFilterAll, "All"),
                (btnFilterCustomers, "Customer"),
                (btnFilterLeads, "Lead"),
                (btnFilterTasks, "Task"),
                (btnFilterTickets, "Ticket"),
                (btnFilterUsers, "User")
            };

            foreach (var (btn, type) in pills)
            {
                bool isSelected = string.Equals(_filterType, type, StringComparison.OrdinalIgnoreCase);
                UiRadiusHelper.StyleFilterPill(btn, isSelected);
            }
        }

        private void LayoutToolbar()
        {
            int leftMargin = 24;
            int rightPadding = 24;
            int y = 92;

            // Action buttons on the right
            btnEmptyBin.Top = y;
            btnEmptyBin.Left = ClientSize.Width - rightPadding - btnEmptyBin.Width;

            btnRestoreAll.Top = y;
            btnRestoreAll.Left = btnEmptyBin.Left - 8 - btnRestoreAll.Width;

            btnRefresh.Top = y;
            btnRefresh.Left = btnRestoreAll.Left - 8 - btnRefresh.Width;

            // Search and filter pills on the left
            txtSearch.Top = y;
            txtSearch.Left = leftMargin;

            int pillX = txtSearch.Right + 10;
            var pills = new[] { btnFilterAll, btnFilterCustomers, btnFilterLeads, btnFilterTasks, btnFilterTickets, btnFilterUsers };
            foreach (var p in pills)
            {
                p.Top = y;
                p.Left = pillX;
                pillX += p.Width + 6;
            }

            int cardTop = y + UiStyleConstants.ToolbarRowHeight + 14;
            pnlCard.Location = new Point(leftMargin, cardTop);
            pnlCard.Size = new Size(Math.Max(300, ClientSize.Width - leftMargin - rightPadding), Math.Max(200, ClientSize.Height - cardTop - 24));
        }

        public async Task RefreshDataAsync()
        {
            if (_isLoading) return;
            _isLoading = true;
            _gridSkeleton?.ShowSkeleton();

            try
            {
                _allItems.Clear();

                // 1. Archived Customers
                var customers = _customerController.GetArchived();
                foreach (var c in customers)
                {
                    _allItems.Add(new ArchivedItemDto
                    {
                        Id = $"c-{c.CustomerId}",
                        EntityType = "Customer",
                        Name = c.FullName,
                        Subtitle = $"{c.Email ?? "No email"} · {c.Phone ?? "No phone"} · Type: {c.Type}",
                        ArchivedAt = c.DeletedAt ?? c.CreatedAt,
                        RawEntity = c
                    });
                }

                // 2. Archived Leads
                var leads = _leadController.GetArchived();
                foreach (var l in leads)
                {
                    _allItems.Add(new ArchivedItemDto
                    {
                        Id = $"l-{l.LeadId}",
                        EntityType = "Lead",
                        Name = l.FullName,
                        Subtitle = $"{l.Email ?? "No email"} · Source: {l.Source ?? "Direct"} · Stage: {l.Stage}",
                        ArchivedAt = l.DeletedAt ?? l.CreatedAt,
                        RawEntity = l
                    });
                }

                // 3. Archived Follow-Ups / Tasks
                var tasks = _followUpController.GetArchived();
                foreach (var tr in tasks)
                {
                    _allItems.Add(new ArchivedItemDto
                    {
                        Id = $"tr-{tr.TaskReminderId}",
                        EntityType = "Task",
                        Name = tr.Title,
                        Subtitle = $"{tr.Type} · Priority: {tr.Priority} · Due: {(tr.DueDate != default ? tr.DueDate.ToLocalTime().ToString("MMM dd, yyyy") : "N/A")}",
                        ArchivedAt = tr.DeletedAt ?? tr.CreatedAt,
                        RawEntity = tr
                    });
                }

                // 4. Archived Support Tickets
                var tickets = _supportTicketController.GetArchived();
                foreach (var t in tickets)
                {
                    _allItems.Add(new ArchivedItemDto
                    {
                        Id = $"t-{t.TicketId}",
                        EntityType = "Ticket",
                        Name = $"Ticket #{t.TicketNumber}: {t.Category}",
                        Subtitle = t.Description,
                        ArchivedAt = t.DeletedAt ?? t.ResolvedAt ?? t.CreatedAt,
                        RawEntity = t
                    });
                }

                // 5. Deactivated Users
                var users = await _userController.GetDeactivatedAsync();
                foreach (var u in users)
                {
                    _allItems.Add(new ArchivedItemDto
                    {
                        Id = $"u-{u.UserId}",
                        EntityType = "User",
                        Name = u.FullName,
                        Subtitle = $"{u.Email} · Role: {u.Role?.RoleName ?? "Staff"} · Deactivated",
                        ArchivedAt = u.CreatedAt,
                        RawEntity = u
                    });
                }

                // Sort by date descending
                _allItems = _allItems.OrderByDescending(x => x.ArchivedAt ?? DateTime.MinValue).ToList();

                ApplyFilterAndDisplay();
            }
            finally
            {
                _gridSkeleton?.HideSkeleton();
                _isLoading = false;
            }
        }

        private void ApplyFilterAndDisplay(bool resetPage = false)
        {
            var query = _allItems.AsEnumerable();

            if (!string.Equals(_filterType, "All", StringComparison.OrdinalIgnoreCase))
            {
                query = query.Where(x => string.Equals(x.EntityType, _filterType, StringComparison.OrdinalIgnoreCase));
            }

            string search = txtSearch.Text.Trim();
            if (!string.IsNullOrWhiteSpace(search))
            {
                query = query.Where(x =>
                    x.Name.Contains(search, StringComparison.OrdinalIgnoreCase) ||
                    x.Subtitle.Contains(search, StringComparison.OrdinalIgnoreCase) ||
                    x.EntityType.Contains(search, StringComparison.OrdinalIgnoreCase)
                );
            }

            var items = query.ToList();
            int total = items.Count;
            int page = resetPage ? 1 : (_pagination?.CurrentPage ?? 1);
            int pageSize = _pagination?.PageSize ?? 25;

            _pagination?.UpdatePagination(total, page, pageSize);

            grid.Rows.Clear();

            btnEmptyBin.Enabled = total > 0;
            btnRestoreAll.Enabled = total > 0;

            if (total == 0)
            {
                grid.Visible = false;
                _pnlEmptyState.Visible = true;
                lblSubtitle.Text = $"{_allItems.Count:N0} total archived items · 0 displayed";
                return;
            }

            _pnlEmptyState.Visible = false;
            grid.Visible = true;

            int effectivePage = _pagination?.CurrentPage ?? 1;
            var pageItems = items.Skip((effectivePage - 1) * pageSize).Take(pageSize).ToList();

            foreach (var item in pageItems)
            {
                int rowIndex = grid.Rows.Add(
                    item.Id,
                    item.EntityType,
                    item.Name,
                    item.Subtitle,
                    item.ArchivedAt.HasValue ? item.ArchivedAt.Value.ToLocalTime().ToString("MMM dd, yyyy  h:mm tt") : "Unknown",
                    "♻ Restore",
                    "🗑 Delete"
                );
                grid.Rows[rowIndex].Tag = item;
            }

            lblSubtitle.Text = $"{_allItems.Count:N0} total archived items · {items.Count:N0} filtered";
        }

        private async Task RestoreItemAsync(ArchivedItemDto item)
        {
            var confirm = MessageBox.Show(
                $"Are you sure you want to restore {item.EntityType.ToLower()} '{item.Name}' back to active status?",
                "Confirm Restore",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Question);

            if (confirm != DialogResult.Yes) return;

            try
            {
                if (item.RawEntity is Customer cust)
                {
                    _customerController.Restore(cust);
                }
                else if (item.RawEntity is Lead lead)
                {
                    _leadController.Restore(lead);
                }
                else if (item.RawEntity is TaskReminder task)
                {
                    _followUpController.Restore(task.TaskReminderId);
                }
                else if (item.RawEntity is SupportTicket ticket)
                {
                    _supportTicketController.Restore(ticket.TicketId);
                }
                else if (item.RawEntity is User user)
                {
                    await _userController.ReactivateAsync(user.UserId);
                }

                MessageBox.Show(
                    $"'{item.Name}' has been restored successfully.",
                    "Record Restored",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information);

                await RefreshDataAsync();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Error Restoring Record", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private async Task PermanentDeleteItemAsync(ArchivedItemDto item)
        {
            var confirm = MessageBox.Show(
                $"Are you sure you want to PERMANENTLY delete {item.EntityType.ToLower()} '{item.Name}'?\n\nThis will completely erase the record from the database and cannot be undone.",
                "Confirm Permanent Deletion",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Warning);

            if (confirm != DialogResult.Yes) return;

            try
            {
                if (item.RawEntity is Customer cust)
                {
                    _customerController.HardDelete(cust.CustomerId);
                }
                else if (item.RawEntity is Lead lead)
                {
                    _leadController.HardDelete(lead.LeadId);
                }
                else if (item.RawEntity is TaskReminder task)
                {
                    _followUpController.HardDelete(task.TaskReminderId);
                }
                else if (item.RawEntity is SupportTicket ticket)
                {
                    _supportTicketController.HardDelete(ticket.TicketId);
                }
                else if (item.RawEntity is User user)
                {
                    await _userController.HardDeleteAsync(user.UserId);
                }

                MessageBox.Show(
                    $"'{item.Name}' was permanently purged from the database.",
                    "Record Permanently Deleted",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information);

                await RefreshDataAsync();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Deletion Restricted", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private async Task RestoreAllFilteredAsync()
        {
            var itemsToRestore = _allItems.AsEnumerable();
            if (!string.Equals(_filterType, "All", StringComparison.OrdinalIgnoreCase))
            {
                itemsToRestore = itemsToRestore.Where(x => string.Equals(x.EntityType, _filterType, StringComparison.OrdinalIgnoreCase));
            }
            var list = itemsToRestore.ToList();
            if (list.Count == 0) return;

            var confirm = MessageBox.Show(
                $"Are you sure you want to restore all {list.Count:N0} archived item(s) back to active status?",
                "Restore All Records",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Question);

            if (confirm != DialogResult.Yes) return;

            int restored = 0;
            foreach (var item in list)
            {
                try
                {
                    if (item.RawEntity is Customer cust) _customerController.Restore(cust);
                    else if (item.RawEntity is Lead lead) _leadController.Restore(lead);
                    else if (item.RawEntity is TaskReminder task) _followUpController.Restore(task.TaskReminderId);
                    else if (item.RawEntity is SupportTicket ticket) _supportTicketController.Restore(ticket.TicketId);
                    else if (item.RawEntity is User user) await _userController.ReactivateAsync(user.UserId);
                    restored++;
                }
                catch { }
            }

            MessageBox.Show($"Successfully restored {restored:N0} record(s).", "Batch Restore Completed", MessageBoxButtons.OK, MessageBoxIcon.Information);
            await RefreshDataAsync();
        }

        private async Task EmptyBinFilteredAsync()
        {
            var itemsToDelete = _allItems.AsEnumerable();
            if (!string.Equals(_filterType, "All", StringComparison.OrdinalIgnoreCase))
            {
                itemsToDelete = itemsToDelete.Where(x => string.Equals(x.EntityType, _filterType, StringComparison.OrdinalIgnoreCase));
            }
            var list = itemsToDelete.ToList();
            if (list.Count == 0) return;

            var confirm = MessageBox.Show(
                $"WARNING: You are about to permanently purge {list.Count:N0} item(s) from the database!\n\nThis operation CANNOT be reversed or undone. Proceed?",
                "Empty Recycle Bin",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Warning);

            if (confirm != DialogResult.Yes) return;

            int purged = 0;
            int failed = 0;
            foreach (var item in list)
            {
                try
                {
                    if (item.RawEntity is Customer cust) _customerController.HardDelete(cust.CustomerId);
                    else if (item.RawEntity is Lead lead) _leadController.HardDelete(lead.LeadId);
                    else if (item.RawEntity is TaskReminder task) _followUpController.HardDelete(task.TaskReminderId);
                    else if (item.RawEntity is SupportTicket ticket) _supportTicketController.HardDelete(ticket.TicketId);
                    else if (item.RawEntity is User user) await _userController.HardDeleteAsync(user.UserId);
                    purged++;
                }
                catch
                {
                    failed++;
                }
            }

            string msg = $"Purged {purged:N0} record(s) permanently.";
            if (failed > 0) msg += $" ({failed:N0} record(s) could not be removed due to foreign key constraints).";
            MessageBox.Show(msg, "Recycle Bin Emptied", MessageBoxButtons.OK, MessageBoxIcon.Information);

            await RefreshDataAsync();
        }
    }
}
