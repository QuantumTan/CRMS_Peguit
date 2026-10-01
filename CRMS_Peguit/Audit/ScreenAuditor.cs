using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using CRMS_Peguit.domain.Common;
using CRMS_Peguit.domain.entities;
using CRMS_Peguit.infrastructure.data;
using CRMS_Peguit.infrastructure.Seeding;
using CRMS_Peguit.winforms.Auth;
using CRMS_Peguit.winforms.Controllers;
using CRMS_Peguit.winforms.Models.Roles;
using CRMS_Peguit.winforms.Models.Services;
using CRMS_Peguit.winforms.Views.Activities;
using CRMS_Peguit.winforms.Views.Analytics;
using CRMS_Peguit.winforms.Views.Archives;
using CRMS_Peguit.winforms.Views.Branching;
using CRMS_Peguit.winforms.Views.Controls;
using CRMS_Peguit.winforms.Views.Customers;
using CRMS_Peguit.winforms.Views.Dashboard;
using CRMS_Peguit.winforms.Views.Deals;
using CRMS_Peguit.winforms.Views.FollowUps;
using CRMS_Peguit.winforms.Views.Leads;
using CRMS_Peguit.winforms.Views.Management;
using CRMS_Peguit.winforms.Views.Marketing;
using CRMS_Peguit.winforms.Views.Notifications;
using CRMS_Peguit.winforms.Views.Properties;
using CRMS_Peguit.winforms.Views.Reports;
using CRMS_Peguit.winforms.Views.Shared;
using CRMS_Peguit.winforms.Views.SuperAdmin;
using CRMS_Peguit.winforms.Views.SupportTickets;
using CRMS_Peguit.winforms.Views.Sync;
using CRMS_Peguit.winforms.Views.Users;
using Microsoft.EntityFrameworkCore;

namespace CRMS_Peguit.winforms.Audit
{
    public class DefectRecord
    {
        public string Screen { get; set; } = string.Empty;
        public string Category { get; set; } = string.Empty; // A - H
        public string Severity { get; set; } = string.Empty; // Critical, High, Medium, Low
        public string Description { get; set; } = string.Empty;
        public string RootCause { get; set; } = string.Empty;
        public string FixApplied { get; set; } = string.Empty;
        public string ScreenshotBefore { get; set; } = string.Empty;
        public string ScreenshotAfter { get; set; } = string.Empty;
    }

    public static class ScreenAuditor
    {
        private static readonly List<DefectRecord> _defects = new();
        private static string _screenshotBaseDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "..", "..", "..", "..", "artifacts", "screenshots");

        public static bool IsAuditing { get; set; } = false;

        public static void RunAudit(string[] args)
        {
            IsAuditing = true;
            Console.WriteLine("================================================================================");
            Console.WriteLine(" NEXA CRM: SYSTEM-WIDE UI/UX VISUAL DEFECT AUDIT PASS");
            Console.WriteLine("================================================================================");

            // Resolve screenshot directory
            string fullPath = Path.GetFullPath(_screenshotBaseDir);
            Directory.CreateDirectory(Path.Combine(fullPath, "before"));
            Directory.CreateDirectory(Path.Combine(fullPath, "after"));
            Console.WriteLine($"[AUDITOR] Screenshots output directory: {fullPath}");

            // Ensure context seeded
            using (var initDb = LocalDb.CreateContext(1))
            {
                initDb.Database.EnsureCreated();
                DbSeeder.SeedTestUsersAsync(initDb, 1).GetAwaiter().GetResult();
                DbSeeder.SeedSampleDataAsync(initDb, 1).GetAwaiter().GetResult();
            }

            try
            {
                using var masterDb = LocalDb.CreateMasterContext();
                masterDb.Database.EnsureCreated();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[AUDITOR] Master DB initialization notice: {ex.Message}");
            }

            bool isAfter = args.Contains("--after") || (Directory.Exists(Path.Combine(fullPath, "before")) && Directory.GetFiles(Path.Combine(fullPath, "before")).Length > 0 && !args.Contains("--force-before"));

            // Execute audits
            AuditAllViews(isBefore: !isAfter);

            // Write report
            string reportFile = isAfter ? "defect_log_after.csv" : "defect_log_before.csv";
            WriteDefectReport(Path.Combine(fullPath, reportFile));

            Console.WriteLine("================================================================================");
            Console.WriteLine($"[AUDITOR] Audit pass complete! Total defects found: {_defects.Count}");
            Console.WriteLine("================================================================================");
        }

        public static void AuditAllViews(bool isBefore)
        {
            _defects.Clear();
            string subDir = isBefore ? "before" : "after";

            SafeAudit(() => AuditLoginForm(subDir), "LoginForm");
            SafeAudit(() => AuditMainShell(subDir), "MainShell");
            SafeAudit(() => AuditSuperAdminShell(subDir), "SuperAdminShell");
            SafeAudit(() => AuditDashboardView(subDir), "DashboardView");
            SafeAudit(() => AuditCustomersView(subDir), "CustomersView");
            SafeAudit(() => AuditLeadsView(subDir), "LeadsView");
            SafeAudit(() => AuditPropertiesView(subDir), "PropertiesView");
            SafeAudit(() => AuditDealsView(subDir), "DealsView");
            SafeAudit(() => AuditSupportTicketsView(subDir), "SupportTicketsView");
            SafeAudit(() => AuditFollowUpsView(subDir), "FollowUpsView");
            SafeAudit(() => AuditActivitiesView(subDir), "ActivitiesView");
            SafeAudit(() => AuditAnalyticsView(subDir), "AnalyticsView");
            SafeAudit(() => AuditReportsView(subDir), "ReportsView");
            SafeAudit(() => AuditClientRetentionView(subDir), "ClientRetentionView");
            SafeAudit(() => AuditCampaignsView(subDir), "CampaignsView");
            SafeAudit(() => AuditApprovalsView(subDir), "ApprovalsView");
            SafeAudit(() => AuditBranchesView(subDir), "BranchesView");
            SafeAudit(() => AuditUserManagementViews(subDir), "UserManagementViews");
            SafeAudit(() => AuditArchivesView(subDir), "ArchivesView");
            SafeAudit(() => AuditNotifications(subDir), "Notifications");
            SafeAudit(() => AuditCustomerForms(subDir), "CustomerForms");
            SafeAudit(() => AuditLeadForms(subDir), "LeadForms");
            SafeAudit(() => AuditPropertyForms(subDir), "PropertyForms");
            SafeAudit(() => AuditDealForms(subDir), "DealForms");
            SafeAudit(() => AuditSupportTicketForms(subDir), "SupportTicketForms");
            SafeAudit(() => AuditFollowUpDialogs(subDir), "FollowUpDialogs");
            SafeAudit(() => AuditSharedDialogs(subDir), "SharedDialogs");
            SafeAudit(() => AuditSuperAdminViews(subDir), "SuperAdminViews");
            SafeAudit(() => AuditSyncScreens(subDir), "SyncScreens");
        }

        private static void SafeAudit(Action action, string name)
        {
            try
            {
                action();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[AUDITOR WARNING] Exception auditing {name}: {ex.Message}");
            }
        }

        #region Shells & Auth

        private static void AuditLoginForm(string subDir)
        {
            Console.WriteLine("--> Auditing LoginForm...");
            SetSession(UserRole.Admin);
            using var form = new LoginForm();
            form.Show();
            Application.DoEvents();

            CaptureForm(form, $"LoginForm_Default", subDir);
            RunLayoutChecks(form, "LoginForm");

            // Extreme: long error text
            var lblError = form.Controls.Find("lblError", true).FirstOrDefault() as Label;
            if (lblError != null)
            {
                lblError.Text = "Unexpected system error occurred: Connection to authentication server at monsterasp.cloud.database.crm.com timed out after 30000ms. Please check network.";
                CaptureForm(form, $"LoginForm_LongError", subDir);
                RunLayoutChecks(form, "LoginForm");
            }
            form.Close();
        }

        private static void AuditMainShell(string subDir)
        {
            Console.WriteLine("--> Auditing MainForm Shell for all roles and sizes...");
            var roles = new[] { UserRole.SuperAdmin, UserRole.Admin, UserRole.Manager, UserRole.SalesStaff };
            var sizes = new[]
            {
                ("Min", new Size(1024, 700)),
                ("Mid", new Size(1150, 768)),
                ("Default", new Size(1280, 800)),
                ("Max", new Size(1920, 1080))
            };

            foreach (var role in roles)
            {
                SetSession(role);
                using var form = new MainForm();
                form.Show();
                Application.DoEvents();

                foreach (var (sizeName, size) in sizes)
                {
                    form.Size = size;
                    Application.DoEvents();
                    CaptureForm(form, $"MainForm_{role}_{sizeName}", subDir);
                    RunLayoutChecks(form, $"MainForm_{role}_{sizeName}");
                }
                form.Close();
            }
        }

        private static void AuditSuperAdminShell(string subDir)
        {
            Console.WriteLine("--> Auditing SuperAdminForm Shell...");
            SetSession(UserRole.SuperAdmin);
            using var form = new SuperAdminForm();
            form.Show();
            Application.DoEvents();

            var sizes = new[]
            {
                ("Min", form.MinimumSize),
                ("Default", new Size(1440, 900)),
                ("Max", new Size(1920, 1080))
            };

            foreach (var (sizeName, size) in sizes)
            {
                form.Size = size;
                Application.DoEvents();
                CaptureForm(form, $"SuperAdminForm_{sizeName}", subDir);
                RunLayoutChecks(form, $"SuperAdminForm_{sizeName}");
            }
            form.Close();
        }

        #endregion

        #region Operational Views

        private static void AuditDashboardView(string subDir)
        {
            Console.WriteLine("--> Auditing DashboardView (all roles)...");
            var roles = new[] { UserRole.SuperAdmin, UserRole.Admin, UserRole.Manager, UserRole.SalesStaff };
            foreach (var role in roles)
            {
                SetSession(role);
                using var host = new Form { Size = new Size(1280, 800), StartPosition = FormStartPosition.Manual, Location = new Point(-2000, -2000) };
                var view = new DashboardView { Dock = DockStyle.Fill };
                host.Controls.Add(view);
                host.Show();
                Application.DoEvents();

                CaptureControl(view, $"DashboardView_{role}_Default", subDir);
                RunLayoutChecks(view, $"DashboardView_{role}");

                // Narrow size check
                host.Size = new Size(1024, 700);
                Application.DoEvents();
                CaptureControl(view, $"DashboardView_{role}_Min", subDir);
                RunLayoutChecks(view, $"DashboardView_{role}_Min");

                host.Close();
            }
        }

        private static void AuditCustomersView(string subDir)
        {
            Console.WriteLine("--> Auditing CustomersView...");
            SetSession(UserRole.Admin);
            using var host = new Form { Size = new Size(1280, 800), StartPosition = FormStartPosition.Manual, Location = new Point(-2000, -2000) };
            var view = new CustomersView { Dock = DockStyle.Fill };
            host.Controls.Add(view);
            host.Show();
            Application.DoEvents();

            CaptureControl(view, $"CustomersView_Default", subDir);
            RunLayoutChecks(view, "CustomersView");

            host.Size = new Size(1024, 700);
            Application.DoEvents();
            CaptureControl(view, $"CustomersView_Min", subDir);
            RunLayoutChecks(view, "CustomersView_Min");

            host.Close();
        }

        private static void AuditLeadsView(string subDir)
        {
            Console.WriteLine("--> Auditing LeadsView...");
            SetSession(UserRole.Admin);
            using var host = new Form { Size = new Size(1280, 800), StartPosition = FormStartPosition.Manual, Location = new Point(-2000, -2000) };
            var view = new LeadsView { Dock = DockStyle.Fill };
            host.Controls.Add(view);
            host.Show();
            Application.DoEvents();

            CaptureControl(view, $"LeadsView_Default", subDir);
            RunLayoutChecks(view, "LeadsView");

            host.Size = new Size(1024, 700);
            Application.DoEvents();
            CaptureControl(view, $"LeadsView_Min", subDir);
            RunLayoutChecks(view, "LeadsView_Min");

            host.Close();
        }

        private static void AuditPropertiesView(string subDir)
        {
            Console.WriteLine("--> Auditing PropertiesView...");
            SetSession(UserRole.Admin);
            using var host = new Form { Size = new Size(1280, 800), StartPosition = FormStartPosition.Manual, Location = new Point(-2000, -2000) };
            var view = new PropertiesView { Dock = DockStyle.Fill };
            host.Controls.Add(view);
            host.Show();
            Application.DoEvents();

            CaptureControl(view, $"PropertiesView_Default", subDir);
            RunLayoutChecks(view, "PropertiesView");

            host.Size = new Size(1024, 700);
            Application.DoEvents();
            CaptureControl(view, $"PropertiesView_Min", subDir);
            RunLayoutChecks(view, "PropertiesView_Min");

            host.Close();
        }

        private static void AuditDealsView(string subDir)
        {
            Console.WriteLine("--> Auditing DealsView...");
            SetSession(UserRole.Admin);
            using var host = new Form { Size = new Size(1280, 800), StartPosition = FormStartPosition.Manual, Location = new Point(-2000, -2000) };
            var view = new DealsView { Dock = DockStyle.Fill };
            host.Controls.Add(view);
            host.Show();
            Application.DoEvents();

            CaptureControl(view, $"DealsView_Default", subDir);
            RunLayoutChecks(view, "DealsView");

            host.Size = new Size(1024, 700);
            Application.DoEvents();
            CaptureControl(view, $"DealsView_Min", subDir);
            RunLayoutChecks(view, "DealsView_Min");

            host.Close();
        }

        private static void AuditSupportTicketsView(string subDir)
        {
            Console.WriteLine("--> Auditing SupportTicketsView...");
            SetSession(UserRole.Admin);
            using var host = new Form { Size = new Size(1280, 800), StartPosition = FormStartPosition.Manual, Location = new Point(-2000, -2000) };
            var view = new SupportTicketsView { Dock = DockStyle.Fill };
            host.Controls.Add(view);
            host.Show();
            Application.DoEvents();

            CaptureControl(view, $"SupportTicketsView_Default", subDir);
            RunLayoutChecks(view, "SupportTicketsView");

            host.Size = new Size(1024, 700);
            Application.DoEvents();
            CaptureControl(view, $"SupportTicketsView_Min", subDir);
            RunLayoutChecks(view, "SupportTicketsView_Min");

            host.Close();
        }

        private static void AuditFollowUpsView(string subDir)
        {
            Console.WriteLine("--> Auditing FollowUpsView...");
            SetSession(UserRole.Admin);
            using var host = new Form { Size = new Size(1280, 800), StartPosition = FormStartPosition.Manual, Location = new Point(-2000, -2000) };
            var view = new FollowUpsView { Dock = DockStyle.Fill };
            host.Controls.Add(view);
            host.Show();
            Application.DoEvents();

            CaptureControl(view, $"FollowUpsView_Default", subDir);
            RunLayoutChecks(view, "FollowUpsView");

            host.Size = new Size(1024, 700);
            Application.DoEvents();
            CaptureControl(view, $"FollowUpsView_Min", subDir);
            RunLayoutChecks(view, "FollowUpsView_Min");

            host.Close();
        }

        private static void AuditActivitiesView(string subDir)
        {
            Console.WriteLine("--> Auditing ActivitiesView...");
            SetSession(UserRole.Admin);
            using var host = new Form { Size = new Size(1280, 800), StartPosition = FormStartPosition.Manual, Location = new Point(-2000, -2000) };
            var view = new ActivitiesView { Dock = DockStyle.Fill };
            host.Controls.Add(view);
            host.Show();
            Application.DoEvents();

            CaptureControl(view, $"ActivitiesView_Default", subDir);
            RunLayoutChecks(view, "ActivitiesView");

            host.Close();
        }

        private static void AuditAnalyticsView(string subDir)
        {
            Console.WriteLine("--> Auditing AnalyticsView...");
            SetSession(UserRole.Admin);
            using var host = new Form { Size = new Size(1280, 800), StartPosition = FormStartPosition.Manual, Location = new Point(-2000, -2000) };
            var view = new AnalyticsView { Dock = DockStyle.Fill };
            host.Controls.Add(view);
            host.Show();
            Application.DoEvents();

            CaptureControl(view, $"AnalyticsView_Default", subDir);
            RunLayoutChecks(view, "AnalyticsView");

            host.Size = new Size(1024, 700);
            Application.DoEvents();
            CaptureControl(view, $"AnalyticsView_Min", subDir);
            RunLayoutChecks(view, "AnalyticsView_Min");

            host.Close();
        }

        private static void AuditReportsView(string subDir)
        {
            Console.WriteLine("--> Auditing ReportsView...");
            SetSession(UserRole.Admin);
            using var host = new Form { Size = new Size(1280, 800), StartPosition = FormStartPosition.Manual, Location = new Point(-2000, -2000) };
            var view = new ReportsView { Dock = DockStyle.Fill };
            host.Controls.Add(view);
            host.Show();
            Application.DoEvents();

            CaptureControl(view, $"ReportsView_Default", subDir);
            RunLayoutChecks(view, "ReportsView");

            host.Size = new Size(1024, 700);
            Application.DoEvents();
            CaptureControl(view, $"ReportsView_Min", subDir);
            RunLayoutChecks(view, "ReportsView_Min");

            host.Close();
        }

        private static void AuditClientRetentionView(string subDir)
        {
            Console.WriteLine("--> Auditing ClientRetentionView...");
            SetSession(UserRole.Admin);
            using var host = new Form { Size = new Size(1280, 800), StartPosition = FormStartPosition.Manual, Location = new Point(-2000, -2000) };
            var view = new ClientRetentionView { Dock = DockStyle.Fill };
            host.Controls.Add(view);
            host.Show();
            Application.DoEvents();

            CaptureControl(view, $"ClientRetentionView_Default", subDir);
            RunLayoutChecks(view, "ClientRetentionView");

            host.Size = new Size(1024, 700);
            Application.DoEvents();
            CaptureControl(view, $"ClientRetentionView_Min", subDir);
            RunLayoutChecks(view, "ClientRetentionView_Min");

            host.Close();
        }

        private static void AuditCampaignsView(string subDir)
        {
            Console.WriteLine("--> Auditing CampaignsView...");
            SetSession(UserRole.Admin);
            using var host = new Form { Size = new Size(1280, 800), StartPosition = FormStartPosition.Manual, Location = new Point(-2000, -2000) };
            var view = new CampaignsView { Dock = DockStyle.Fill };
            host.Controls.Add(view);
            host.Show();
            Application.DoEvents();

            CaptureControl(view, $"CampaignsView_Default", subDir);
            RunLayoutChecks(view, "CampaignsView");

            host.Close();
        }

        private static void AuditApprovalsView(string subDir)
        {
            Console.WriteLine("--> Auditing ApprovalsView...");
            SetSession(UserRole.Admin);
            using var host = new Form { Size = new Size(1280, 800), StartPosition = FormStartPosition.Manual, Location = new Point(-2000, -2000) };
            var view = new ApprovalsView { Dock = DockStyle.Fill };
            host.Controls.Add(view);
            host.Show();
            Application.DoEvents();

            CaptureControl(view, $"ApprovalsView_Default", subDir);
            RunLayoutChecks(view, "ApprovalsView");

            host.Close();
        }

        private static void AuditBranchesView(string subDir)
        {
            Console.WriteLine("--> Auditing BranchesView...");
            SetSession(UserRole.Admin, tenantId: 3);
            using var host = new Form { Size = new Size(1280, 800), StartPosition = FormStartPosition.Manual, Location = new Point(-2000, -2000) };
            var view = new BranchesView { Dock = DockStyle.Fill };
            host.Controls.Add(view);
            host.Show();
            Application.DoEvents();

            CaptureControl(view, $"BranchesView_Default", subDir);
            RunLayoutChecks(view, "BranchesView");

            host.Close();
        }

        private static void AuditUserManagementViews(string subDir)
        {
            Console.WriteLine("--> Auditing AdminUserListForm (Managers, Agents, Admins)...");
            SetSession(UserRole.Admin);
            var filters = new[] { "Manager", "Agent", "All Roles" };
            foreach (var filt in filters)
            {
                using var host = new Form { Size = new Size(1280, 800), StartPosition = FormStartPosition.Manual, Location = new Point(-2000, -2000) };
                var view = new AdminUserListForm(filt) { Dock = DockStyle.Fill };
                host.Controls.Add(view);
                host.Show();
                for (int p = 0; p < 5; p++) { Application.DoEvents(); System.Threading.Thread.Sleep(30); }

                CaptureControl(view, $"AdminUserList_{filt}", subDir);
                RunLayoutChecks(view, $"AdminUserList_{filt}");

                host.Controls.Clear();
                host.Close();
                view.Dispose();
            }
        }

        private static void AuditArchivesView(string subDir)
        {
            Console.WriteLine("--> Auditing ArchivesView...");
            SetSession(UserRole.Admin);
            using var host = new Form { Size = new Size(1280, 800), StartPosition = FormStartPosition.Manual, Location = new Point(-2000, -2000) };
            var view = new ArchivesView { Dock = DockStyle.Fill };
            host.Controls.Add(view);
            host.Show();
            Application.DoEvents();

            CaptureControl(view, $"ArchivesView_Default", subDir);
            RunLayoutChecks(view, "ArchivesView");

            host.Close();
        }

        private static void AuditNotifications(string subDir)
        {
            Console.WriteLine("--> Auditing NotificationPreferencesView...");
            SetSession(UserRole.Admin);
            using var view = new NotificationPreferencesView();
            view.StartPosition = FormStartPosition.Manual;
            view.Location = new Point(0, 0);
            view.Show();
            Application.DoEvents();

            CaptureForm(view, $"NotificationPreferencesView_Default", subDir);
            RunLayoutChecks(view, "NotificationPreferencesView");

            view.Close();
        }

        #endregion

        #region Detail & Input Dialogs

        private static void AuditCustomerForms(string subDir)
        {
            Console.WriteLine("--> Auditing CustomerDetailForm & CustomerInputForm...");
            SetSession(UserRole.Admin);

            using var db = LocalDb.CreateContext(1);
            var cust = db.Customers.FirstOrDefault()
                ?? new Customer { CustomerId = 1, FirstName = "Test", LastName = "Customer", Email = "test@customer.com", Phone = "09123456789", Status = "Active" };

            // Detail Form
            using var ctrl = new CustomerController();
            using var detailForm = new CustomerDetailForm(cust, ctrl);
            detailForm.Show();
            Application.DoEvents();
            CaptureForm(detailForm, "CustomerDetailForm_Default", subDir);
            RunLayoutChecks(detailForm, "CustomerDetailForm");
            detailForm.Close();

            // Input Form (Create Mode)
            using var inputForm = new CustomerInputForm();
            inputForm.Show();
            Application.DoEvents();
            CaptureForm(inputForm, "CustomerInputForm_Create", subDir);
            RunLayoutChecks(inputForm, "CustomerInputForm_Create");
            inputForm.Close();

            // Input Form (Edit Mode with Extreme long values)
            var extremeCust = new Customer
            {
                CustomerId = 9999,
                FirstName = "Bartholomew-Alexander-Maximilian-Cornelius",
                LastName = "Montgomery-Vanderbilt-Fitzgerald-Wellington",
                Email = "bartholomew.alexander.maximilian.montgomery.vanderbilt@verylongcompanydomainnamewithoutspaces.org",
                Phone = "+63-917-555-0199-ext-99881122",
                Status = "Active",
                Type = "Individual"
            };
            using var inputEdit = new CustomerInputForm(extremeCust);
            inputEdit.Show();
            Application.DoEvents();
            CaptureForm(inputEdit, "CustomerInputForm_Extreme", subDir);
            RunLayoutChecks(inputEdit, "CustomerInputForm_Extreme");
            inputEdit.Close();
        }

        private static void AuditLeadForms(string subDir)
        {
            Console.WriteLine("--> Auditing LeadDetailForm & LeadInputForm...");
            SetSession(UserRole.Admin);
            using var db = LocalDb.CreateContext(1);
            var lead = db.Leads.FirstOrDefault()
                ?? new Lead { LeadId = 1, FirstName = "LeadFirst", LastName = "LeadLast", Email = "lead@test.com", Phone = "09123456789", Stage = "new", ExpectedValue = 25000000m };

            using var ctrl = new LeadController();
            using var detailForm = new LeadDetailForm(lead, ctrl);
            detailForm.Show();
            Application.DoEvents();
            CaptureForm(detailForm, "LeadDetailForm_Default", subDir);
            RunLayoutChecks(detailForm, "LeadDetailForm");
            detailForm.Close();

            using var inputForm = new LeadInputForm();
            inputForm.Show();
            Application.DoEvents();
            CaptureForm(inputForm, "LeadInputForm_Create", subDir);
            RunLayoutChecks(inputForm, "LeadInputForm_Create");
            inputForm.Close();
        }

        private static void AuditPropertyForms(string subDir)
        {
            Console.WriteLine("--> Auditing PropertyDetailForm & PropertyInputForm...");
            SetSession(UserRole.Admin);
            using var db = LocalDb.CreateContext(1);
            var prop = db.Properties.FirstOrDefault()
                ?? new Property { PropertyId = 1, Price = 85000000m, PropertyType = "Condo", Status = "Available", Address = "Fort Bonifacio, Taguig" };

            using var ctrl = new PropertyController();
            using var detailForm = new PropertyDetailForm(prop, ctrl);
            detailForm.Show();
            Application.DoEvents();
            CaptureForm(detailForm, "PropertyDetailForm_Default", subDir);
            RunLayoutChecks(detailForm, "PropertyDetailForm");
            detailForm.Close();

            using var inputForm = new PropertyInputForm();
            inputForm.Show();
            Application.DoEvents();
            CaptureForm(inputForm, "PropertyInputForm_Create", subDir);
            RunLayoutChecks(inputForm, "PropertyInputForm_Create");
            inputForm.Close();
        }

        private static void AuditDealForms(string subDir)
        {
            Console.WriteLine("--> Auditing DealDetailForm, DealInputForm & ContractTermsViewerDialog...");
            SetSession(UserRole.Admin);
            using var db = LocalDb.CreateContext(1);
            var deal = db.Deals.Include(d => d.Customer).Include(d => d.Property).Include(d => d.Contingencies).Include(d => d.DealClauses).FirstOrDefault()
                ?? new Deal { DealId = 1, Value = 125000000m, Stage = "Offer", CommissionRate = 0.05m, ExpectedCloseDate = DateTime.Today.AddMonths(1) };

            using var ctrl = new DealController();
            using var detailForm = new DealDetailForm(deal, ctrl);
            detailForm.Show();
            Application.DoEvents();
            CaptureForm(detailForm, "DealDetailForm_Default", subDir);
            RunLayoutChecks(detailForm, "DealDetailForm");
            detailForm.Close();

            using var inputForm = new DealInputForm(ctrl, deal);
            inputForm.Show();
            Application.DoEvents();
            CaptureForm(inputForm, "DealInputForm_Default", subDir);
            RunLayoutChecks(inputForm, "DealInputForm");
            inputForm.Close();

            using var termsDialog = new ContractTermsViewerDialog(deal, ctrl);
            termsDialog.Show();
            Application.DoEvents();
            CaptureForm(termsDialog, "ContractTermsViewerDialog_Default", subDir);
            RunLayoutChecks(termsDialog, "ContractTermsViewerDialog");
            termsDialog.Close();
        }

        private static void AuditSupportTicketForms(string subDir)
        {
            Console.WriteLine("--> Auditing SupportTicketDetailForm & SupportTicketInputForm...");
            SetSession(UserRole.Admin);
            using var db = LocalDb.CreateContext(1);
            var ticket = db.SupportTickets.Include(t => t.Comments).FirstOrDefault()
                ?? new SupportTicket { TicketId = 1, Category = "Legal", Description = "Title deed annotation mismatch with registry.", Status = "Open", Priority = "High" };

            using var ctrl = new SupportTicketController();
            using var detailForm = new SupportTicketDetailForm(ticket, ctrl);
            detailForm.Show();
            Application.DoEvents();
            CaptureForm(detailForm, "SupportTicketDetailForm_Default", subDir);
            RunLayoutChecks(detailForm, "SupportTicketDetailForm");
            detailForm.Close();

            using var inputForm = new SupportTicketInputForm(ctrl);
            inputForm.Show();
            Application.DoEvents();
            CaptureForm(inputForm, "SupportTicketInputForm_Default", subDir);
            RunLayoutChecks(inputForm, "SupportTicketInputForm");
            inputForm.Close();
        }

        private static void AuditFollowUpDialogs(string subDir)
        {
            Console.WriteLine("--> Auditing FollowUp Dialogs (FollowUpInputForm, CompleteFollowUpDialog, RescheduleDialog)...");
            SetSession(UserRole.Admin);
            using var db = LocalDb.CreateContext(1);
            var reminder = db.TaskReminders.FirstOrDefault()
                ?? new TaskReminder { TaskReminderId = 1, Title = "Contract signing follow-up", DueDate = DateTime.Today.AddDays(2), Status = "Pending", Priority = "Urgent" };

            using var ctrl = new FollowUpController();
            using var inputForm = new FollowUpInputForm(ctrl, reminder);
            inputForm.Show();
            Application.DoEvents();
            CaptureForm(inputForm, "FollowUpInputForm_Default", subDir);
            RunLayoutChecks(inputForm, "FollowUpInputForm");
            inputForm.Close();

            using var completeDialog = new CompleteFollowUpDialog(reminder);
            completeDialog.Show();
            Application.DoEvents();
            CaptureForm(completeDialog, "CompleteFollowUpDialog_Default", subDir);
            RunLayoutChecks(completeDialog, "CompleteFollowUpDialog");
            completeDialog.Close();

            using var rescheduleDialog = new RescheduleDialog(reminder);
            rescheduleDialog.Show();
            Application.DoEvents();
            CaptureForm(rescheduleDialog, "RescheduleDialog_Default", subDir);
            RunLayoutChecks(rescheduleDialog, "RescheduleDialog");
            rescheduleDialog.Close();
        }

        private static void AuditSharedDialogs(string subDir)
        {
            Console.WriteLine("--> Auditing Shared Dialogs (AssignAgentDialog, ChangePasswordDialog, EmailMessageForm, LogActivityDialog)...");
            SetSession(UserRole.Admin);

            using var assignDlg = new AssignAgentDialog();
            assignDlg.Show();
            Application.DoEvents();
            CaptureForm(assignDlg, "AssignAgentDialog_Default", subDir);
            RunLayoutChecks(assignDlg, "AssignAgentDialog");
            assignDlg.Close();

            using var changePwd = new ChangePasswordDialog("admin@test.com");
            changePwd.Show();
            Application.DoEvents();
            CaptureForm(changePwd, "ChangePasswordDialog_Default", subDir);
            RunLayoutChecks(changePwd, "ChangePasswordDialog");
            changePwd.Close();

            using var emailForm = new EmailMessageForm("Maria Santos", "maria.santos@megacityrealty.com.ph", "Property Consultation Summary");
            emailForm.Show();
            Application.DoEvents();
            CaptureForm(emailForm, "EmailMessageForm_Default", subDir);
            RunLayoutChecks(emailForm, "EmailMessageForm");
            emailForm.Close();

            using var logActDlg = new LogActivityDialog();
            logActDlg.Show();
            Application.DoEvents();
            CaptureForm(logActDlg, "LogActivityDialog_Default", subDir);
            RunLayoutChecks(logActDlg, "LogActivityDialog");
            logActDlg.Close();

            using var branchEditDlg = new BranchEditDialog();
            branchEditDlg.Show();
            Application.DoEvents();
            CaptureForm(branchEditDlg, "BranchEditDialog_Default", subDir);
            RunLayoutChecks(branchEditDlg, "BranchEditDialog");
            branchEditDlg.Close();

            using var retCtrl = new RetentionController();
            using var retReqDlg = new RetentionRequestDialog(retCtrl);
            retReqDlg.Show();
            Application.DoEvents();
            CaptureForm(retReqDlg, "RetentionRequestDialog_Default", subDir);
            RunLayoutChecks(retReqDlg, "RetentionRequestDialog");
            retReqDlg.Close();

            using var cooldownDlg = new CooldownOverrideDialog("Roberto Dela Cruz", DateTime.UtcNow.AddDays(-14), 16);
            cooldownDlg.Show();
            Application.DoEvents();
            CaptureForm(cooldownDlg, "CooldownOverrideDialog_Default", subDir);
            RunLayoutChecks(cooldownDlg, "CooldownOverrideDialog");
            cooldownDlg.Close();

            using var userCtrl = new UserController();
            using var userEditDlg = new AdminUserEditForm(userCtrl, null);
            userEditDlg.Show();
            Application.DoEvents();
            CaptureForm(userEditDlg, "AdminUserEditForm_Create", subDir);
            RunLayoutChecks(userEditDlg, "AdminUserEditForm");
            userEditDlg.Close();
        }

        #endregion

        #region Super Admin Views & Dialogs

        private static void AuditSuperAdminViews(string subDir)
        {
            Console.WriteLine("--> Auditing SuperAdmin Child Views & Dialogs...");
            SetSession(UserRole.SuperAdmin);

            // Views
            SafeAudit(() => AuditUserControlInHost(new SuperAdminDashboardView(), "SuperAdminDashboardView", subDir), "SuperAdminDashboardView");
            SafeAudit(() => AuditUserControlInHost(new TenantsView(), "TenantsView", subDir), "TenantsView");
            SafeAudit(() => AuditUserControlInHost(new AdministratorsView(), "AdministratorsView", subDir), "AdministratorsView");
            SafeAudit(() => AuditUserControlInHost(new SubscriptionsView(), "SubscriptionsView", subDir), "SubscriptionsView");
            SafeAudit(() => AuditUserControlInHost(new SyncHealthView(), "SyncHealthView", subDir), "SyncHealthView");
            SafeAudit(() => AuditUserControlInHost(new SystemSettingsView(), "SystemSettingsView", subDir), "SystemSettingsView");
            SafeAudit(() => AuditUserControlInHost(new BackupsView(), "BackupsView", subDir), "BackupsView");
            SafeAudit(() => AuditUserControlInHost(new PlatformAuditLogView(), "PlatformAuditLogView", subDir), "PlatformAuditLogView");

            // Dialogs
            SafeAudit(() =>
            {
                using var createTenantDlg = new CreateTenantDialog();
                createTenantDlg.Show();
                Application.DoEvents();
                CaptureForm(createTenantDlg, "CreateTenantDialog_Default", subDir);
                RunLayoutChecks(createTenantDlg, "CreateTenantDialog");
                createTenantDlg.Close();
            }, "CreateTenantDialog");

            SafeAudit(() =>
            {
                using var createAdminDlg = new CreateAdminDialog(new List<(int, string)> { (1, "Tenant 1 Realty") });
                createAdminDlg.Show();
                Application.DoEvents();
                CaptureForm(createAdminDlg, "CreateAdminDialog_Default", subDir);
                RunLayoutChecks(createAdminDlg, "CreateAdminDialog");
                createAdminDlg.Close();
            }, "CreateAdminDialog");

            SafeAudit(() =>
            {
                using var editSettingDlg = new EditSettingDialog("Security.SessionTimeoutMinutes", "60");
                editSettingDlg.Show();
                Application.DoEvents();
                CaptureForm(editSettingDlg, "EditSettingDialog_Default", subDir);
                RunLayoutChecks(editSettingDlg, "EditSettingDialog");
                editSettingDlg.Close();
            }, "EditSettingDialog");

            SafeAudit(() =>
            {
                using var tierChangeDlg = new SubscriptionTierChangeDialog("Apex Properties Inc.", "Enterprise", "Active", 185000m);
                tierChangeDlg.Show();
                Application.DoEvents();
                CaptureForm(tierChangeDlg, "SubscriptionTierChangeDialog_Default", subDir);
                RunLayoutChecks(tierChangeDlg, "SubscriptionTierChangeDialog");
                tierChangeDlg.Close();
            }, "SubscriptionTierChangeDialog");

            SafeAudit(() =>
            {
                var subDto = new TenantSubscriptionDto
                {
                    CompanyId = 1,
                    CompanyName = "Apex Properties Inc.",
                    PlanName = "Enterprise",
                    BillingAmount = 185000m,
                    Status = "Active"
                };
                using var payHistoryDlg = new PaymentHistoryDialog(subDto);
                payHistoryDlg.Show();
                Application.DoEvents();
                CaptureForm(payHistoryDlg, "PaymentHistoryDialog_Default", subDir);
                RunLayoutChecks(payHistoryDlg, "PaymentHistoryDialog");
                payHistoryDlg.Close();
            }, "PaymentHistoryDialog");

            SafeAudit(() =>
            {
                var subDto = new TenantSubscriptionDto
                {
                    CompanyId = 1,
                    CompanyName = "Apex Properties Inc.",
                    PlanName = "Enterprise",
                    BillingAmount = 185000m,
                    Status = "Active"
                };
                using var recordPayDlg = new RecordPaymentDialog(subDto);
                recordPayDlg.Show();
                Application.DoEvents();
                CaptureForm(recordPayDlg, "RecordPaymentDialog_Default", subDir);
                RunLayoutChecks(recordPayDlg, "RecordPaymentDialog");
                recordPayDlg.Close();
            }, "RecordPaymentDialog");

            SafeAudit(() =>
            {
                using var termsDlg = new TenantTermsAndConditionsDialog("Apex Properties Inc.", "Enterprise", false);
                termsDlg.Show();
                Application.DoEvents();
                CaptureForm(termsDlg, "TenantTermsAndConditionsDialog_Default", subDir);
                RunLayoutChecks(termsDlg, "TenantTermsAndConditionsDialog");
                termsDlg.Close();
            }, "TenantTermsAndConditionsDialog");

            SafeAudit(() =>
            {
                var backupDto = new CRMS_Peguit.winforms.Controllers.BackupLogDto
                {
                    BackupId = 1,
                    FileLocation = "CRMS_Tenant1_20260928_Full.bak",
                    BackupDate = DateTime.UtcNow.AddHours(-3),
                    Status = "Success",
                    PerformedByName = "Super Admin"
                };
                using var restoreConfirmDlg = new RestoreConfirmDialog(backupDto);
                restoreConfirmDlg.Show();
                Application.DoEvents();
                CaptureForm(restoreConfirmDlg, "RestoreConfirmDialog_Default", subDir);
                RunLayoutChecks(restoreConfirmDlg, "RestoreConfirmDialog");
                restoreConfirmDlg.Close();
            }, "RestoreConfirmDialog");
        }

        #endregion

        #region Sync Screens

        private static void AuditSyncScreens(string subDir)
        {
            Console.WriteLine("--> Auditing Sync Screens (SyncStatusForm, SyncConflictResolutionForm)...");
            SetSession(UserRole.Admin);

            using var syncStatus = new SyncStatusForm();
            syncStatus.Show();
            Application.DoEvents();
            CaptureForm(syncStatus, "SyncStatusForm_Default", subDir);
            RunLayoutChecks(syncStatus, "SyncStatusForm");
            syncStatus.Close();

            using var conflictDlg = new SyncConflictResolutionForm(1);
            conflictDlg.Show();
            Application.DoEvents();
            CaptureForm(conflictDlg, "SyncConflictResolutionForm_Default", subDir);
            RunLayoutChecks(conflictDlg, "SyncConflictResolutionForm");
            conflictDlg.Close();
        }

        #endregion

        #region Helpers & Diagnostic Inspection

        private static void AuditUserControlInHost(UserControl uc, string name, string subDir)
        {
            using var host = new Form { Size = new Size(1280, 800), StartPosition = FormStartPosition.Manual, Location = new Point(-2000, -2000) };
            uc.Dock = DockStyle.Fill;
            host.Controls.Add(uc);
            host.Show();
            Application.DoEvents();

            CaptureControl(uc, $"{name}_Default", subDir);
            RunLayoutChecks(uc, name);

            host.Size = new Size(1024, 700);
            Application.DoEvents();
            CaptureControl(uc, $"{name}_Min", subDir);
            RunLayoutChecks(uc, $"{name}_Min");

            host.Close();
        }

        public static void RunLayoutChecks(Control root, string screenName)
        {
            CheckOverlapsRecursive(root, screenName);
            CheckTextClippingRecursive(root, screenName);
            CheckBoundsOverflowRecursive(root, screenName);
            CheckDataGridViewsRecursive(root, screenName);
            CheckFormDialogKeys(root, screenName);
            CheckStandardsRecursive(root, screenName);
        }

        private static void CheckOverlapsRecursive(Control parent, string screenName)
        {
            var visibleChildren = parent.Controls.Cast<Control>()
                .Where(c => c.Visible && c.Width > 0 && c.Height > 0 && c.Dock == DockStyle.None)
                .ToList();

            for (int i = 0; i < visibleChildren.Count; i++)
            {
                var c1 = visibleChildren[i];
                for (int j = i + 1; j < visibleChildren.Count; j++)
                {
                    var c2 = visibleChildren[j];
                    if (c1.Bounds.IntersectsWith(c2.Bounds))
                    {
                        var intersection = Rectangle.Intersect(c1.Bounds, c2.Bounds);
                        // Filter out trivial 1px border intersections or explicitly layered components
                        if (intersection.Width > 4 && intersection.Height > 4)
                        {
                            string c1Name = string.IsNullOrEmpty(c1.Name) ? c1.GetType().Name : c1.Name;
                            string c2Name = string.IsNullOrEmpty(c2.Name) ? c2.GetType().Name : c2.Name;

                            _defects.Add(new DefectRecord
                            {
                                Screen = screenName,
                                Category = "A (Overlap)",
                                Severity = "High",
                                Description = $"Sibling controls overlap: '{c1Name}' ({c1.Bounds}) intersects '{c2Name}' ({c2.Bounds}) by {intersection.Width}x{intersection.Height}px",
                                RootCause = $"{c1.GetType().Name} / {c2.GetType().Name} layout coordinates"
                            });
                        }
                    }
                }

                if (c1.HasChildren)
                {
                    CheckOverlapsRecursive(c1, screenName);
                }
            }
        }

        private static void CheckTextClippingRecursive(Control parent, string screenName)
        {
            foreach (Control c in parent.Controls)
            {
                if (c.Visible && !string.IsNullOrWhiteSpace(c.Text))
                {
                    if (c is Label lbl && !lbl.AutoSize && !lbl.AutoEllipsis && lbl.Width > 0)
                    {
                        var measured = TextRenderer.MeasureText(lbl.Text, lbl.Font);
                        if (measured.Width > lbl.ClientSize.Width + 4 && !lbl.Text.Contains('\n'))
                        {
                            _defects.Add(new DefectRecord
                            {
                                Screen = screenName,
                                Category = "B (Cut-off / Clipping)",
                                Severity = "Medium",
                                Description = $"Text truncated mid-character without AutoEllipsis: '{lbl.Name}' text='{lbl.Text}' (measured width {measured.Width}px > client width {lbl.ClientSize.Width}px)",
                                RootCause = $"Label '{lbl.Name}' has AutoSize=false and AutoEllipsis=false"
                            });
                        }
                    }
                    else if (c is Button btn && btn.Width > 0 && !btn.Text.StartsWith(" ") && !btn.Text.Contains('\n'))
                    {
                        var measured = TextRenderer.MeasureText(btn.Text, btn.Font);
                        if (measured.Width > btn.ClientSize.Width - 8)
                        {
                            _defects.Add(new DefectRecord
                            {
                                Screen = screenName,
                                Category = "B (Cut-off / Clipping)",
                                Severity = "High",
                                Description = $"Button label clipped at edge: '{btn.Name}' text='{btn.Text}' (measured width {measured.Width}px > button client width {btn.ClientSize.Width}px)",
                                RootCause = $"Button '{btn.Name}' fixed width too narrow for text"
                            });
                        }
                    }
                }

                if (c.HasChildren)
                {
                    CheckTextClippingRecursive(c, screenName);
                }
            }
        }

        private static void CheckBoundsOverflowRecursive(Control parent, string screenName)
        {
            if (parent is ScrollableControl sc && sc.AutoScroll)
            {
                // Has scrolling enabled, overflowing children are reachable via scrollbar
                return;
            }

            foreach (Control c in parent.Controls)
            {
                if (c.Visible && c.Dock == DockStyle.None)
                {
                    if (c.Right > parent.ClientSize.Width + 4 && parent.ClientSize.Width > 0)
                    {
                        _defects.Add(new DefectRecord
                        {
                            Screen = screenName,
                            Category = "B (Cut-off / Clipping)",
                            Severity = "High",
                            Description = $"Control overflows right edge of container without scrolling: '{c.Name}' Right={c.Right} > Parent '{parent.Name}' Width={parent.ClientSize.Width}",
                            RootCause = $"Missing Anchor/Dock/AutoScroll on '{parent.Name}'"
                        });
                    }
                    if (c.Bottom > parent.ClientSize.Height + 4 && parent.ClientSize.Height > 0)
                    {
                        _defects.Add(new DefectRecord
                        {
                            Screen = screenName,
                            Category = "B (Cut-off / Clipping)",
                            Severity = "High",
                            Description = $"Control overflows bottom edge of container without scrolling: '{c.Name}' Bottom={c.Bottom} > Parent '{parent.Name}' Height={parent.ClientSize.Height}",
                            RootCause = $"Missing AutoScroll or fixed height container '{parent.Name}'"
                        });
                    }
                }

                if (c.HasChildren)
                {
                    CheckBoundsOverflowRecursive(c, screenName);
                }
            }
        }

        private static void CheckDataGridViewsRecursive(Control parent, string screenName)
        {
            foreach (Control c in parent.Controls)
            {
                if (c is DataGridView dgv)
                {
                    foreach (DataGridViewColumn col in dgv.Columns)
                    {
                        string header = col.HeaderText ?? string.Empty;
                        bool isNumeric = header.Contains("Amount", StringComparison.OrdinalIgnoreCase) ||
                                         header.Contains("Price", StringComparison.OrdinalIgnoreCase) ||
                                         header.Contains("Commission", StringComparison.OrdinalIgnoreCase) ||
                                         header.Contains("Revenue", StringComparison.OrdinalIgnoreCase) ||
                                         header.Contains("Value", StringComparison.OrdinalIgnoreCase) ||
                                         header.Contains("Balance", StringComparison.OrdinalIgnoreCase) ||
                                         header.Contains("Rate", StringComparison.OrdinalIgnoreCase) ||
                                         header.Contains("Count", StringComparison.OrdinalIgnoreCase);

                        if (isNumeric)
                        {
                            var align = col.DefaultCellStyle.Alignment;
                            if (align != DataGridViewContentAlignment.MiddleRight &&
                                align != DataGridViewContentAlignment.TopRight &&
                                align != DataGridViewContentAlignment.BottomRight)
                            {
                                _defects.Add(new DefectRecord
                                {
                                    Screen = screenName,
                                    Category = "C (Alignment & Spacing)",
                                    Severity = "High",
                                    Description = $"Numeric/currency column not right-aligned: Grid '{dgv.Name}' column '{header}' has alignment '{align}' instead of MiddleRight",
                                    RootCause = $"Column '{col.Name}' DefaultCellStyle.Alignment"
                                });
                            }
                        }
                    }
                }

                if (c.HasChildren)
                {
                    CheckDataGridViewsRecursive(c, screenName);
                }
            }
        }

        private static void CheckFormDialogKeys(Control root, string screenName)
        {
            if (root is Form form && form.FormBorderStyle != FormBorderStyle.Sizable && form.FormBorderStyle != FormBorderStyle.None)
            {
                if (form.AcceptButton == null)
                {
                    var primaryBtn = form.Controls.Cast<Control>()
                        .SelectMany(GetAllControls)
                        .OfType<Button>()
                        .FirstOrDefault(b => b.Text.Contains("Save", StringComparison.OrdinalIgnoreCase) ||
                                             b.Text.Contains("Submit", StringComparison.OrdinalIgnoreCase) ||
                                             b.Text.Contains("Sign In", StringComparison.OrdinalIgnoreCase) ||
                                             b.Text.Contains("OK", StringComparison.OrdinalIgnoreCase) ||
                                             b.Text.Contains("Confirm", StringComparison.OrdinalIgnoreCase));

                    if (primaryBtn != null)
                    {
                        _defects.Add(new DefectRecord
                        {
                            Screen = screenName,
                            Category = "F (UX Friction)",
                            Severity = "Medium",
                            Description = $"Form has no AcceptButton wired to primary action button '{primaryBtn.Text}'",
                            RootCause = $"Form.AcceptButton not assigned in '{form.GetType().Name}'"
                        });
                    }
                }

                if (form.CancelButton == null)
                {
                    var cancelBtn = form.Controls.Cast<Control>()
                        .SelectMany(GetAllControls)
                        .OfType<Button>()
                        .FirstOrDefault(b => b.Text.Contains("Cancel", StringComparison.OrdinalIgnoreCase) ||
                                             b.Text.Contains("Close", StringComparison.OrdinalIgnoreCase));

                    if (cancelBtn != null)
                    {
                        _defects.Add(new DefectRecord
                        {
                            Screen = screenName,
                            Category = "F (UX Friction)",
                            Severity = "Medium",
                            Description = $"Form has no CancelButton wired to cancel/close button '{cancelBtn.Text}' (Esc key non-functional)",
                            RootCause = $"Form.CancelButton not assigned in '{form.GetType().Name}'"
                        });
                    }
                }

                if (form.MinimumSize == Size.Empty || form.MinimumSize.Width < 200)
                {
                    _defects.Add(new DefectRecord
                    {
                        Screen = screenName,
                        Category = "E (Resize / Layout Robustness)",
                        Severity = "Medium",
                        Description = $"Form MinimumSize is not defined: allows window to collapse completely on resize",
                        RootCause = $"Form.MinimumSize not configured in '{form.GetType().Name}'"
                    });
                }
            }
        }

        private static void CheckStandardsRecursive(Control parent, string screenName)
        {
            foreach (Control c in parent.Controls)
            {
                // CSV Export button forbidden - Standard requires PDF exports only
                if (c is Button btn && (btn.Text.Contains("CSV", StringComparison.OrdinalIgnoreCase) || btn.Name.Contains("Csv", StringComparison.OrdinalIgnoreCase)))
                {
                    _defects.Add(new DefectRecord
                    {
                        Screen = screenName,
                        Category = "H (Consistency with NEXA Standards)",
                        Severity = "High",
                        Description = $"CSV Export button present ('{btn.Text}'): Standard requires PDF exports only",
                        RootCause = $"btnExportCsv in '{screenName}'"
                    });
                }

                // Status pill badge forbidden (must be StatusText)
                if (c.GetType().Name == "StatusBadge")
                {
                    _defects.Add(new DefectRecord
                    {
                        Screen = screenName,
                        Category = "H (Consistency with NEXA Standards)",
                        Severity = "Medium",
                        Description = $"Status shown as StatusBadge pill instead of StatusText colored bold text standard",
                        RootCause = $"StatusBadge control usage in '{screenName}'"
                    });
                }

                if (c.HasChildren)
                {
                    CheckStandardsRecursive(c, screenName);
                }
            }
        }

        private static IEnumerable<Control> GetAllControls(Control root)
        {
            yield return root;
            foreach (Control child in root.Controls)
            {
                foreach (Control sub in GetAllControls(child))
                {
                    yield return sub;
                }
            }
        }

        private static void CaptureForm(Form form, string fileName, string subDir)
        {
            try
            {
                string path = Path.Combine(Path.GetFullPath(_screenshotBaseDir), subDir, $"{fileName}.png");
                using var bmp = new Bitmap(form.Width, form.Height);
                form.DrawToBitmap(bmp, new Rectangle(0, 0, form.Width, form.Height));
                bmp.Save(path, ImageFormat.Png);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[AUDITOR] Error capturing form {fileName}: {ex.Message}");
            }
        }

        private static void CaptureControl(Control ctrl, string fileName, string subDir)
        {
            try
            {
                string path = Path.Combine(Path.GetFullPath(_screenshotBaseDir), subDir, $"{fileName}.png");
                using var bmp = new Bitmap(ctrl.Width, ctrl.Height);
                ctrl.DrawToBitmap(bmp, new Rectangle(0, 0, ctrl.Width, ctrl.Height));
                bmp.Save(path, ImageFormat.Png);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[AUDITOR] Error capturing control {fileName}: {ex.Message}");
            }
        }

        private static void SetSession(UserRole role, int tenantId = 1)
        {
            string roleName = role switch
            {
                UserRole.SuperAdmin => "SuperAdmin",
                UserRole.Admin => "Admin",
                UserRole.Manager => "Manager",
                UserRole.SalesStaff => "Agent",
                _ => "Agent"
            };

            int userId = role switch
            {
                UserRole.SuperAdmin => 1,
                UserRole.Admin => 1,
                UserRole.Manager => 2,
                UserRole.SalesStaff => 3,
                _ => 3
            };

            int tId = role == UserRole.SuperAdmin ? 0 : tenantId;
            CurrentSession.Start(userId, tId, $"{roleName} User", $"{roleName.ToLower()}@test.com", roleName, null, false);
            if (tId == 3)
            {
                CurrentSession.SetActiveBranch(1, "Main Branch");
            }
        }

        private static void WriteDefectReport(string filePath)
        {
            using var sw = new StreamWriter(filePath);
            sw.WriteLine("Index,Screen,Category,Severity,Description,RootCause");
            int idx = 1;
            foreach (var d in _defects)
            {
                string desc = d.Description.Replace("\"", "\"\"");
                string rc = d.RootCause.Replace("\"", "\"\"");
                sw.WriteLine($"{idx++},\"{d.Screen}\",\"{d.Category}\",\"{d.Severity}\",\"{desc}\",\"{rc}\"");
            }
        }

        #endregion
    }
}
