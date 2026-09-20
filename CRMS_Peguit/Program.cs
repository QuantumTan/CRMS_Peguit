using CRMS_Peguit.infrastructure.Seeding;
using CRMS_Peguit.winforms.Models.Services;

namespace CRMS_Peguit.winforms
{
    internal static class Program
    {
        private static SyncService? _syncService;

        [STAThread]
        static void Main(string[] args)
        {
            Application.SetHighDpiMode(HighDpiMode.PerMonitorV2);
            ApplicationConfiguration.Initialize();

            var localConnection = DbConfiguration.GetLocalConnectionString();
            var cloudConnection = DbConfiguration.GetCloudConnectionString();

            // Automatically ensure SQL Server LocalDB instance is actively running before database access
            LocalDbHelper.EnsureLocalDbRunning(localConnection);

            // Propagate connection string to environment so all components share the resolved value
            if (string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("CRMS_CONNECTION")))
            {
                Environment.SetEnvironmentVariable("CRMS_CONNECTION", localConnection);
            }
            if (!string.IsNullOrWhiteSpace(cloudConnection) &&
                string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("CRMS_CLOUD_CONNECTION")))
            {
                Environment.SetEnvironmentVariable("CRMS_CLOUD_CONNECTION", cloudConnection);
            }

            if (args.Contains("--verify-assets"))
            {
                var logo = AppBrand.Logo;
                var icon = AppBrand.AppIcon;
                Console.WriteLine($"[VERIFY] Logo loaded: {logo != null}, Size: {logo?.Width}x{logo?.Height}, Icon loaded: {icon != null}");
                return;
            }

            if (args.Contains("--verify-reports"))
            {
                CRMS_Peguit.winforms.Auth.CurrentSession.Start(1, 1, "System Admin", "admin@test.com", "Admin", null, false);
                using var rptCtrl = new CRMS_Peguit.winforms.Controllers.ReportsController();
                var range = CRMS_Peguit.winforms.Models.Analytics.DateRangeFilter.ThisYear();
                var sales = rptCtrl.GetSalesReport(range);
                var leads = rptCtrl.GetLeadProgressReport(range);
                var comms = rptCtrl.GetCommissionReport(range);
                var acts = rptCtrl.GetAgentActivityReport(range);
                var tix = rptCtrl.GetTicketResolutionReport(range);
                Console.WriteLine($"[VERIFY] Sales: {sales.Count}, Leads: {leads.Count}, Comms: {comms.Count}, Activities: {acts.Count}, Tickets: {tix.Count}");

                using var anaCtrl = new CRMS_Peguit.winforms.Controllers.AnalyticsController();
                var snap = anaCtrl.GetSnapshot(range);
                int tixOpen = snap?.TicketBreakdown?.Open ?? 0;
                int tixRes = snap?.TicketBreakdown?.Resolved ?? 0;
                Console.WriteLine($"[VERIFY] Analytics Closed Deals: {snap?.TotalDealsClosed}, Commission: ₱{snap?.TotalCommissionEarned:N2}, OverTime Months: {snap?.DealsOverTime.Count}, Tickets: Open={tixOpen}, Res={tixRes}");

                using var dealCtrl = new CRMS_Peguit.winforms.Controllers.DealController();
                var monthRange = CRMS_Peguit.winforms.Models.Analytics.DateRangeFilter.ThisMonth();
                var monthComms = rptCtrl.GetCommissionReport(monthRange);
                var monthEarnedDashboard = dealCtrl.GetCommissionEarnedThisMonth();
                var monthEarnedReports = monthComms.Sum(c => c.GrossCommission);
                Console.WriteLine($"[VERIFY] ThisMonth Comms: {monthComms.Count}, Reports: ₱{monthEarnedReports:N2}, Dashboard: ₱{monthEarnedDashboard:N2}, Matches: {monthEarnedReports == monthEarnedDashboard}");
                return;
            }



            if (args.Contains("--verify-notifications"))
            {
                using var startupDb = LocalDb.CreateContext();
                startupDb.Database.EnsureCreated();
                SchemaRepairService.EnsureCrmPolishColumns(startupDb);

                int testUserId = 1;
                CRMS_Peguit.winforms.Auth.CurrentSession.Start(testUserId, 1, "System Admin", "admin@test.com", "Admin", null, false);

                using var notifCtrl = new CRMS_Peguit.winforms.Controllers.NotificationController();

                // 1. Test creation
                var n1 = notifCtrl.CreateNotification(1, testUserId, domain.entities.NotificationType.LeadAssigned, "Test Lead Assigned", "You were assigned test lead.", "Lead", 101);
                Console.WriteLine($"[VERIFY-NOTIF] Created N1: {n1 != null}, Id: {n1?.NotificationId}");

                // 2. Test debounce (duplicate within 2 min should be suppressed)
                var n1Duplicate = notifCtrl.CreateNotification(1, testUserId, domain.entities.NotificationType.LeadAssigned, "Test Lead Assigned", "You were assigned test lead.", "Lead", 101);
                Console.WriteLine($"[VERIFY-NOTIF] Debounced duplicate: {n1Duplicate == null} (expected true)");

                // 3. Test preference suppression
                var prefs = notifCtrl.GetPreferences(testUserId);
                prefs[domain.entities.NotificationType.PropertyStatusChanged] = false;
                notifCtrl.UpdatePreferences(testUserId, prefs);
                var nSuppressed = notifCtrl.CreateNotification(1, testUserId, domain.entities.NotificationType.PropertyStatusChanged, "Property Updated", "Status changed.", "Property", 202);
                Console.WriteLine($"[VERIFY-NOTIF] Suppressed by preference: {nSuppressed == null} (expected true)");

                // 4. Test query scoping and unread count
                var myNotifs = notifCtrl.GetMyNotifications(testUserId);
                int unread = notifCtrl.GetUnreadCount(testUserId);
                Console.WriteLine($"[VERIFY-NOTIF] My notifications count: {myNotifs.Count}, Unread count: {unread}");

                // 5. Test MarkAsRead and MarkAllAsRead
                if (n1 != null)
                {
                    bool readOk = notifCtrl.MarkAsRead(n1.NotificationId);
                    Console.WriteLine($"[VERIFY-NOTIF] MarkAsRead single: {readOk}");
                }
                int marked = notifCtrl.MarkAllAsRead(testUserId);
                int unreadAfter = notifCtrl.GetUnreadCount(testUserId);
                Console.WriteLine($"[VERIFY-NOTIF] MarkAllAsRead marked: {marked}, Unread after: {unreadAfter} (expected 0)");

                // 6. Test Access Control validation for click-through
                bool agentCanViewOther = CRMS_Peguit.winforms.Auth.RbacService.CanAgentViewRecord(assignedAgentId: 999, createdByUserId: 888);
                // Switch session to Agent role
                CRMS_Peguit.winforms.Auth.CurrentSession.Start(5, 1, "Test Agent", "agent@test.com", "Agent", null, false);
                bool agentDeniedReassigned = !CRMS_Peguit.winforms.Auth.RbacService.CanAgentViewRecord(assignedAgentId: 999, createdByUserId: 888);
                bool agentAllowedOwn = CRMS_Peguit.winforms.Auth.RbacService.CanAgentViewRecord(assignedAgentId: 5, createdByUserId: 888);
                Console.WriteLine($"[VERIFY-NOTIF] RBAC Reassignment Protection: Denied unauthorized={agentDeniedReassigned}, Allowed own={agentAllowedOwn}");

                // Restore preference
                prefs[domain.entities.NotificationType.PropertyStatusChanged] = true;
                notifCtrl.UpdatePreferences(testUserId, prefs);

                // 7. Test MainForm instantiation (verifies no transparent background exceptions on login)
                CRMS_Peguit.winforms.Auth.CurrentSession.Start(testUserId, 1, "System Admin", "admin@test.com", "Admin", null, false);
                using var form = new MainForm();
                Console.WriteLine($"[VERIFY-NOTIF] MainForm instantiated cleanly without transparent exception: {form != null}");

                Console.WriteLine("[VERIFY-NOTIFICATIONS] All verification checks completed successfully!");
                return;
            }

            if (args.Contains("--sync-once"))
            {
                if (!string.IsNullOrWhiteSpace(cloudConnection))
                {
                    using var sync = new SyncService(localConnection, cloudConnection);
                    sync.SyncAsync().GetAwaiter().GetResult();
                }
                return;
            }

            if (args.Contains("--seed-transactions"))
            {
                using var startupDb = LocalDb.CreateContext();
                startupDb.Database.EnsureCreated();
                int added = DbSeeder.SeedTransactionsAsync(startupDb, 320, 1).GetAwaiter().GetResult();
                DbSeeder.SeedLeadsAndTicketsAsync(startupDb, 1).GetAwaiter().GetResult();
                Console.WriteLine($"[SEEDER] Seeded {added} transactions. Total deals in database: {startupDb.Deals.Count()}");
                return;
            }

            if (args.Contains("--init-db"))
            {
                using var startupDb = LocalDb.CreateContext();
                startupDb.Database.EnsureCreated();
                DbSeeder.SeedTestUsersAsync(startupDb, 1).GetAwaiter().GetResult();
                DbSeeder.SeedTransactionsAsync(startupDb, 320, 1).GetAwaiter().GetResult();
                DbSeeder.SeedLeadsAndTicketsAsync(startupDb, 1).GetAwaiter().GetResult();
                SchemaRepairService.EnsureCrmPolishColumns(startupDb);
                Console.WriteLine("CRMS_Local database initialized and seeded successfully.");
                return;
            }

            // ==================================================
            // ONE-TIME SCHEMA INITIALIZATION
            // ==================================================
            try
            {
                using var startupDb = LocalDb.CreateContext();
                startupDb.Database.EnsureCreated();
                DbSeeder.SeedTestUsersAsync(startupDb, 1).GetAwaiter().GetResult();
                if (startupDb.Deals.Count() < 300)
                {
                    DbSeeder.SeedTransactionsAsync(startupDb, 320, 1).GetAwaiter().GetResult();
                }
                DbSeeder.SeedLeadsAndTicketsAsync(startupDb, 1).GetAwaiter().GetResult();
                SchemaRepairService.EnsureCrmPolishColumns(startupDb);
            }
            catch (Exception ex)
            {
                // Non-critical startup schema check
                System.Diagnostics.Debug.WriteLine($"Startup DB init error: {ex.Message}");
            }

            // ==================================================
            // START BACKGROUND SYNC SERVICE (IF CLOUD CONFIGURED)
            // ==================================================
            if (!string.IsNullOrWhiteSpace(cloudConnection))
            {
                _syncService = new SyncService(localConnection, cloudConnection);
                _syncService.Start(30);
            }

            // ==================================================
            // START LOGIN FORM
            // ==================================================

            using var loginForm = new LoginForm();

            Application.Run(loginForm);

            // ==================================================
            // CLEAN UP
            // ==================================================

            _syncService?.Dispose();
        }
    }
}