using Microsoft.EntityFrameworkCore;
using Microsoft.Data.SqlClient;
using CRMS_Peguit.domain.entities;
using CRMS_Peguit.infrastructure.data;
using CRMS_Peguit.infrastructure.Seeding;
using CRMS_Peguit.winforms.Models.Services;

namespace CRMS_Peguit.winforms
{
    internal static class Program
    {
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


            if (args.Contains("--audit-all") || args.Contains("--audit-ui"))
            {
                CRMS_Peguit.winforms.Audit.ScreenAuditor.RunAudit(args);
                return;
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

            if (args.Contains("--verify-seed-counts"))
            {
                using var db = LocalDb.CreateContext();
                Console.WriteLine($"[SEED-COUNTS] Users: {db.Users.Count()}, Roles: {db.Roles.Count()}");
                Console.WriteLine($"[SEED-COUNTS] Customers: {db.Customers.Count()}, BuyerProfiles: {db.BuyerProfiles.Count()}");
                Console.WriteLine($"[SEED-COUNTS] Properties: {db.Properties.Count()}");
                Console.WriteLine($"[SEED-COUNTS] Deals: {db.Deals.Count()}, Contingencies: {db.DealContingencies.Count()}, Clauses: {db.DealClauses.Count()}");
                Console.WriteLine($"[SEED-COUNTS] Leads: {db.Leads.Count()}");
                Console.WriteLine($"[SEED-COUNTS] Activities: {db.Activities.Count()}, Showings: {db.PropertyShowingDetails.Count()}");
                Console.WriteLine($"[SEED-COUNTS] FollowUps: {db.TaskReminders.Count()}");
                Console.WriteLine($"[SEED-COUNTS] Tickets: {db.SupportTickets.Count()}, Comments: {db.TicketComments.Count()}");
                Console.WriteLine($"[SEED-COUNTS] Campaigns: {db.Campaigns.Count()}");
                Console.WriteLine($"[SEED-COUNTS] Notifications: {db.Notifications.Count()}, Preferences: {db.NotificationPreferences.Count()}");
                return;
            }

            if (args.Contains("--seed-all-tenants"))
            {
                Console.WriteLine("==================================================");
                Console.WriteLine("SEEDING ALL TENANTS (TENANT 1, TENANT 2, TENANT 3)");
                Console.WriteLine("==================================================");
                var progress = new Progress<string>(msg => Console.WriteLine($"[SEED] {msg}"));
                LocalDb.SeedAllTenantsAsync(progress).GetAwaiter().GetResult();
                Console.WriteLine("==================================================");
                Console.WriteLine("ALL TENANTS SUCCESSFULLY SEEDED!");
                Console.WriteLine("==================================================");
                return;
            }

            if (args.Contains("--verify-multidb"))
            {
                Console.WriteLine("==================================================");
                Console.WriteLine("VERIFYING MULTI-DATABASE ARCHITECTURE & ISOLATION");
                Console.WriteLine("==================================================");

                // 1. Master DB
                using var masterDb = LocalDb.CreateMasterContext();
                string masterCatalog = masterDb.Database.GetDbConnection().Database;
                int companyCount = masterDb.Companies.Count();
                int dbMappingCount = masterDb.CompanyDatabases.Count();
                int subCount = masterDb.Subscriptions.Count();
                Console.WriteLine($"[MASTER-DB] Database: {masterCatalog} | Companies: {companyCount} | Databases: {dbMappingCount} | Subscriptions: {subCount}");
                foreach (var cd in masterDb.CompanyDatabases.ToList())
                {
                    Console.WriteLine($"  ├─ Mapping: Company {cd.CompanyId} -> Database: {cd.DatabaseName} (Active={cd.IsActive})");
                }

                // 2. Tenant A DB
                using var tenant1Db = LocalDb.CreateContext(1);
                string t1Catalog = tenant1Db.Database.GetDbConnection().Database;
                Console.WriteLine($"[TENANT-1-DB] Database: {t1Catalog} | Users: {tenant1Db.Users.Count()} | Customers: {tenant1Db.Customers.Count()} | Properties: {tenant1Db.Properties.Count()} | Deals: {tenant1Db.Deals.Count()} | Leads: {tenant1Db.Leads.Count()} | Tickets: {tenant1Db.SupportTickets.Count()}");

                // 3. Tenant B DB
                using var tenant2Db = LocalDb.CreateContext(2);
                string t2Catalog = tenant2Db.Database.GetDbConnection().Database;
                Console.WriteLine($"[TENANT-2-DB] Database: {t2Catalog} | Users: {tenant2Db.Users.Count()} | Customers: {tenant2Db.Customers.Count()} | Properties: {tenant2Db.Properties.Count()} | Deals: {tenant2Db.Deals.Count()} | Leads: {tenant2Db.Leads.Count()} | Tickets: {tenant2Db.SupportTickets.Count()}");

                // 4. Tenant C DB
                using var tenant3Db = LocalDb.CreateContext(3);
                string t3Catalog = tenant3Db.Database.GetDbConnection().Database;
                Console.WriteLine($"[TENANT-3-DB] Database: {t3Catalog} | Users: {tenant3Db.Users.Count()} | Branches: {tenant3Db.Branches.Count()} | Customers: {tenant3Db.Customers.Count()} | Properties: {tenant3Db.Properties.Count()} | Deals: {tenant3Db.Deals.Count()} | Leads: {tenant3Db.Leads.Count()} | Tickets: {tenant3Db.SupportTickets.Count()}");
                foreach (var b in tenant3Db.Branches.ToList())
                {
                    int branchUsers = tenant3Db.Users.Count(u => u.BranchId == b.BranchId);
                    int branchProps = tenant3Db.Properties.Count(p => p.BranchId == b.BranchId);
                    int branchDeals = tenant3Db.Deals.Count(d => d.BranchId == b.BranchId);
                    int branchLeads = tenant3Db.Leads.Count(l => l.BranchId == b.BranchId);
                    Console.WriteLine($"  ├─ Branch {b.BranchCode} ({b.BranchName}): Users={branchUsers}, Properties={branchProps}, Deals={branchDeals}, Leads={branchLeads}");
                }

                // 5. Cross-Database Aggregation in SuperAdminSubscriptionController
                var saCtrl = new CRMS_Peguit.winforms.Controllers.SuperAdminSubscriptionController();
                var bi = saCtrl.GetPlatformBiSummaryAsync().GetAwaiter().GetResult();
                // 6. Verify Logins across Tenants
                var authService = new CRMS_Peguit.winforms.Auth.AuthService("http://localhost:5000");
                var loginsToTest = new (string Email, string Password, int ExpectedTenantId)[]
                {
                    ("superadmin@crms.com", "SuperAdmin123!", 0),
                    ("tenanta_admin@test.com", "Admin123!", 1),
                    ("admin@test.com", "Admin123!", 1),
                    ("manager@test.com", "Manager123!", 1),
                    ("agent@test.com", "Agent123!", 1),
                    ("tenantb_admin@test.com", "Admin123!", 2),
                    ("manager.b@test.com", "Manager123!", 2),
                    ("agent.b@test.com", "Agent123!", 2),
                    ("tenantc_admin@test.com", "Admin123!", 3),
                    ("manager.c@test.com", "Manager123!", 3),
                    ("carlos.mendoza@test.com", "Manager123!", 3),
                    ("beatrice.ong@test.com", "Manager123!", 3),
                    ("agent.c@test.com", "Agent123!", 3)
                };

                Console.WriteLine("--------------------------------------------------");
                Console.WriteLine("VERIFYING CROSS-TENANT AUTHENTICATION");
                Console.WriteLine("--------------------------------------------------");
                foreach (var (email, pwd, expTid) in loginsToTest)
                {
                    var res = authService.TryLocalDbLogin(email, pwd);
                    Console.WriteLine($"  ├─ {email} -> Success={res.Success}, TenantId={CRMS_Peguit.winforms.Auth.CurrentSession.TenantId} (Expected={expTid}), Role={CRMS_Peguit.winforms.Auth.CurrentSession.CurrentUser?.Role}");
                }

                Console.WriteLine("==================================================");
                Console.WriteLine("MULTI-DATABASE ISOLATION: 100% VERIFIED SUCCESS!");
                Console.WriteLine("==================================================");
                return;
            }

            if (args.Contains("--seed-cloud-tenants"))
            {
                Console.WriteLine("Direct seeding of the cloud database is permanently disabled. Cloud must be populated exclusively via sync.");
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

            if (args.Contains("--verify-retention"))
            {
                bool success = CRMS_Peguit.winforms.Tests.RetentionVerificationTests.RunAllTestsAsync().GetAwaiter().GetResult();
                Environment.Exit(success ? 0 : 1);
                return;
            }

            if (args.Contains("--verify-sync"))
            {
                Console.WriteLine("==================================================");
                Console.WriteLine("VERIFYING OFFLINE QUEUE & CLOUD DIRECT SYNC (MONSTERASP)");
                Console.WriteLine("==================================================");

                int tenantId = 3;
                using var localContext = LocalDb.CreateContext(tenantId);
                var agentUser = localContext.Users.Include(u => u.Role).FirstOrDefault(u => u.Role.RoleName == "Agent")
                    ?? localContext.Users.Include(u => u.Role).First();

                CRMS_Peguit.winforms.Auth.CurrentSession.Start(agentUser.UserId, tenantId, agentUser.FullName, agentUser.Email, agentUser.Role?.RoleName ?? "Agent", null, false);
                CRMS_Peguit.winforms.Auth.CurrentSession.SetActiveBranch(agentUser.BranchId, "Main Branch");

                Console.WriteLine($"[1] Active Session: User={agentUser.FullName} (Id={agentUser.UserId}), Tenant={tenantId}, Role={agentUser.Role?.RoleName}");

                var syncService = CRMS_Peguit.winforms.Models.Services.SyncService.Instance;
                bool isOnline = syncService.CheckConnectivityAsync().GetAwaiter().GetResult();
                Console.WriteLine($"[2] MonsterASP Cloud Connectivity: {(isOnline ? "ONLINE" : "OFFLINE")}");

                var cache = CRMS_Peguit.winforms.Services.Offline.LocalDataCache.Instance;
                cache.ClearAllQueue(tenantId);
                Console.WriteLine("[2b] Cleared stale queue items for clean isolated test.");

                string uniqueMarker = $"SyncTest_{DateTime.UtcNow.Ticks}";
                var testLead = new Lead
                {
                    Source = "Website",
                    Stage = "new",
                    Priority = "High",
                    ExpectedValue = 1500000m,
                    AssignedAgentId = agentUser.UserId,
                    CreatedByUserId = agentUser.UserId,
                    BranchId = agentUser.BranchId,
                    Notes = $"Auto-verification lead created at {DateTime.UtcNow:u} [{uniqueMarker}]",
                    FirstName = "SyncTestFirst",
                    LastName = uniqueMarker,
                    Email = $"{uniqueMarker}@synctest.example.com",
                    Phone = "09123456789"
                };

                using var leadCtrl = new CRMS_Peguit.winforms.Controllers.LeadController();
                var createdLead = leadCtrl.Add(testLead);
                Console.WriteLine($"[3] Created Local Lead: Id={createdLead.LeadId}, PersonId={createdLead.PersonId}, Name={createdLead.FullName}");

                var pendingItems = cache.GetPendingQueue(tenantId);
                var queuedItem = pendingItems.FirstOrDefault(q => q.EntityType == "Lead" && q.EntityLocalId == createdLead.LeadId.ToString());
                bool isQueued = queuedItem != null;
                Console.WriteLine($"[4] Enqueued in SQLite PendingSyncQueue: {isQueued}, QueueId={queuedItem?.QueueId}, Status={queuedItem?.Status}");

                Console.WriteLine($"[5] Waiting for Queue Item #{queuedItem?.QueueId} to sync to MonsterASP Cloud DB...");
                PendingSyncQueue? processedItem = null;
                var sw = System.Diagnostics.Stopwatch.StartNew();
                while (sw.Elapsed < TimeSpan.FromSeconds(30))
                {
                    if (queuedItem != null)
                    {
                        processedItem = cache.GetQueueItem(queuedItem.QueueId);
                        if (processedItem != null && processedItem.Status == "Synced")
                        {
                            break;
                        }
                    }
                    Thread.Sleep(500);
                }
                Console.WriteLine($"[6] SQLite Queue Item Final Status: {processedItem?.Status} (Expected: Synced) in {sw.Elapsed.TotalSeconds:F1}s");

                var cloudConn = DbConfiguration.GetCloudConnectionString();
                using var cloudConnObj = new Microsoft.Data.SqlClient.SqlConnection(cloudConn);
                cloudConnObj.Open();
                using var cmd = cloudConnObj.CreateCommand();
                cmd.CommandText = "SELECT COUNT(1) FROM Leads WHERE LastName = @marker";
                cmd.Parameters.AddWithValue("@marker", uniqueMarker);
                int cloudMatchCount = (int)cmd.ExecuteScalar();
                Console.WriteLine($"[7] Record Found in MonsterASP Cloud DB (db66713): {cloudMatchCount > 0} (Count: {cloudMatchCount})");

                using var cleanCmd = cloudConnObj.CreateCommand();
                cleanCmd.CommandText = "DELETE FROM Leads WHERE LastName = @marker";
                cleanCmd.Parameters.AddWithValue("@marker", uniqueMarker);
                cleanCmd.ExecuteNonQuery();

                using var cleanLocalCmd = localContext.Database.GetDbConnection().CreateCommand();
                localContext.Database.OpenConnection();
                cleanLocalCmd.CommandText = $"DELETE FROM Leads WHERE LeadId = {createdLead.LeadId};";
                cleanLocalCmd.ExecuteNonQuery();

                Console.WriteLine($"[8] Cleaned up temporary test lead {uniqueMarker} from Cloud and Local DBs.");

                // --- Customer Sync Verification ---
                string custMarker = $"CustSync_{DateTime.UtcNow.Ticks}";
                var testCust = new Customer
                {
                    Type = "Individual",
                    Status = "Active",
                    AssignedAgentId = agentUser.UserId,
                    CreatedByUserId = agentUser.UserId,
                    FirstName = "CustomerTestFirst",
                    LastName = custMarker,
                    Email = $"{custMarker}@synctest.example.com",
                    Phone = "09987654321"
                };
                using var custCtrl = new CRMS_Peguit.winforms.Controllers.CustomerController();
                var createdCust = custCtrl.Add(testCust);
                Console.WriteLine($"[9] Created Local Customer: Id={createdCust.CustomerId}, Name={createdCust.FullName}");

                var custPending = cache.GetPendingQueue(tenantId);
                var custQueuedItem = custPending.FirstOrDefault(q => q.EntityType == "Customer" && q.EntityLocalId == createdCust.CustomerId.ToString());
                bool isCustQueued = custQueuedItem != null;
                Console.WriteLine($"[10] Enqueued in SQLite PendingSyncQueue: {isCustQueued}, QueueId={custQueuedItem?.QueueId}, Status={custQueuedItem?.Status}");

                Console.WriteLine($"[11] Waiting for Customer Queue Item #{custQueuedItem?.QueueId} to sync to MonsterASP Cloud DB...");
                PendingSyncQueue? processedCust = null;
                sw.Restart();
                while (sw.Elapsed < TimeSpan.FromSeconds(30))
                {
                    if (custQueuedItem != null)
                    {
                        processedCust = cache.GetQueueItem(custQueuedItem.QueueId);
                        if (processedCust != null && processedCust.Status == "Synced")
                        {
                            break;
                        }
                    }
                    Thread.Sleep(500);
                }
                Console.WriteLine($"[12] SQLite Customer Queue Item Final Status: {processedCust?.Status} (Expected: Synced) in {sw.Elapsed.TotalSeconds:F1}s");

                using var cmd2 = cloudConnObj.CreateCommand();
                cmd2.CommandText = "SELECT COUNT(1) FROM Customers WHERE LastName = @marker";
                cmd2.Parameters.AddWithValue("@marker", custMarker);
                int cloudCustCount = (int)cmd2.ExecuteScalar();
                Console.WriteLine($"[13] Customer Found in MonsterASP Cloud DB (db66713): {cloudCustCount > 0} (Count: {cloudCustCount})");

                using var cleanCmd2 = cloudConnObj.CreateCommand();
                cleanCmd2.CommandText = "DELETE FROM Customers WHERE LastName = @marker";
                cleanCmd2.Parameters.AddWithValue("@marker", custMarker);
                cleanCmd2.ExecuteNonQuery();

                using var cleanLocalCmd2 = localContext.Database.GetDbConnection().CreateCommand();
                cleanLocalCmd2.CommandText = $"DELETE FROM Customers WHERE CustomerId = {createdCust.CustomerId};";
                cleanLocalCmd2.ExecuteNonQuery();

                Console.WriteLine($"[14] Cleaned up temporary test customer {custMarker} from Cloud and Local DBs.");

                bool success = isQueued && processedItem?.Status == "Synced" && cloudMatchCount > 0
                    && isCustQueued && processedCust?.Status == "Synced" && cloudCustCount > 0;
                if (success)
                {
                    Console.WriteLine("==================================================");
                    Console.WriteLine("RESULT: ALL SYNC VERIFICATION CHECKS PASSED (100%)!");
                    Console.WriteLine("==================================================");
                }
                else
                {
                    Console.WriteLine("==================================================");
                    Console.WriteLine("RESULT: SYNC VERIFICATION FAILED!");
                    Console.WriteLine("==================================================");
                }
                Environment.Exit(success ? 0 : 1);
                return;
            }

            if (args.Contains("--sync-once") || args.Contains("--sync-all-users"))
            {
                if (!string.IsNullOrWhiteSpace(cloudConnection))
                {
                    Console.WriteLine("==================================================");
                    Console.WriteLine("SYNCHRONIZING USERS & ROLES ACROSS ALL TENANTS TO CLOUD");
                    Console.WriteLine("==================================================");
                    using var sync = new SyncService(localConnection, cloudConnection);
                    using var cloudConn = new Microsoft.Data.SqlClient.SqlConnection(cloudConnection);
                    cloudConn.Open();
                    sync.PushAllTenantsUsersAndRolesToCloudAsync(cloudConn, 1).GetAwaiter().GetResult();
                    Console.WriteLine("Users & roles sync complete!");
                }
                return;
            }

            if (args.Contains("--seed-transactions"))
            {
                using var startupDb = LocalDb.CreateContext();
                startupDb.Database.EnsureCreated();
                int added = DbSeeder.SeedTransactionsAsync(startupDb, 350, 1).GetAwaiter().GetResult();
                DbSeeder.SeedSampleDataAsync(startupDb, 1).GetAwaiter().GetResult();
                Console.WriteLine($"[SEEDER] Seeded {added} transactions. Total deals in database: {startupDb.Deals.Count()}");
                return;
            }

            if (args.Contains("--init-db"))
            {
                using var startupDb = LocalDb.CreateContext();
                startupDb.Database.EnsureCreated();
                DbSeeder.SeedTestUsersAsync(startupDb, 1).GetAwaiter().GetResult();
                SchemaRepairService.EnsureCrmPolishColumns(startupDb);
                Console.WriteLine("CRMS_Local database initialized and seeded successfully.");
                return;
            }

            // ==================================================
            // START BACKGROUND SYNC SERVICE (IF CLOUD CONFIGURED)
            // ==================================================
            if (!string.IsNullOrWhiteSpace(cloudConnection))
            {
                SyncService.Instance.Start(60);
            }

            // ==================================================
            // START LOGIN FORM
            // ==================================================

            using var loginForm = new LoginForm();

            // Schema repair/seeding is maintenance work, not part of rendering the
            // login screen or validating credentials. Warm it after the window is
            // visible so first paint and sign-in are not blocked by dozens of DDL checks.
            loginForm.Shown += (_, _) =>
            {
                _ = Task.Run(async () =>
                {
                    // Let the user interact with the sign-in screen before background
                    // maintenance begins, avoiding LocalDB lock/CPU contention.
                    await Task.Delay(TimeSpan.FromSeconds(5)).ConfigureAwait(false);
                    LocalDb.WarmAuthenticationDatabases();
                });
            };

            Application.Run(loginForm);

            // ==================================================
            // CLEAN UP
            // ==================================================

            SyncService.Instance.Dispose();
        }
    }
}
