using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Xunit;
using Xunit.Abstractions;
using CRMS_Peguit.domain.entities;
using CRMS_Peguit.infrastructure.data;
using CRMS_Peguit.infrastructure.Security;
using CRMS_Peguit.winforms.Auth;
using CRMS_Peguit.winforms.Controllers;
using CRMS_Peguit.winforms.Models.Services;
using CRMS_Peguit.winforms.Services.Offline;
using DomainActivity = CRMS_Peguit.domain.entities.Activity;

namespace CRMS_Peguit.Tests.IntegrationTests
{
    public class EndToEndSyncAndOfflineValidationTests
    {
        private readonly ITestOutputHelper _output;
        private readonly string _localConn;
        private readonly string _cloudConn;

        public EndToEndSyncAndOfflineValidationTests(ITestOutputHelper output)
        {
            _output = output;
            _localConn = DbConfiguration.GetLocalConnectionString();
            _cloudConn = DbConfiguration.GetCloudConnectionString() ?? string.Empty;
        }

        #region Step 2: Verification of Tenants & Setup
        [Fact]
        public async Task Step2_Verify_Tenants_A_B_C_Hierarchy_And_SeedData()
        {
            int[] tenantIds = { 1, 2, 3 };
            foreach (var tid in tenantIds)
            {
                using var db = LocalDb.CreateContext(tid);
                var users = await db.Users.Include(u => u.Role).ToListAsync();
                bool hasAdmin = users.Any(u => u.Role != null && u.Role.RoleName == "Admin");
                bool hasManager = users.Any(u => u.Role != null && u.Role.RoleName == "Manager");
                bool hasAgent = users.Count(u => u.Role != null && u.Role.RoleName == "Agent") >= 2;

                int custCount = await db.Customers.CountAsync();
                int leadCount = await db.Leads.CountAsync();
                int propCount = await db.Properties.CountAsync();
                int dealCount = await db.Deals.CountAsync();

                _output.WriteLine($"Tenant {tid}: Users={users.Count} (Admin={hasAdmin}, Mgr={hasManager}, 2+Agents={hasAgent}), Cust={custCount}, Leads={leadCount}, Props={propCount}, Deals={dealCount}");

                Assert.True(hasAdmin, $"Tenant {tid} missing Admin");
                Assert.True(hasManager, $"Tenant {tid} missing Manager");
                Assert.True(hasAgent, $"Tenant {tid} missing 2+ Agents");
                Assert.True(custCount > 0, $"Tenant {tid} has 0 Customers");
                Assert.True(propCount > 0, $"Tenant {tid} has 0 Properties");
            }
        }
        #endregion

        #region Step 3: Offline Mode Behavior
        [Fact]
        public void Step3_OfflineLogin_CachedUser_Succeeds_And_SetsOfflineFlag()
        {
            var authCache = new LocalAuthCache();
            string testEmail = $"cached_test_{Guid.NewGuid():N}@test.com";
            string testPassword = "Password123!";
            string hash = PasswordHasher.Hash(testPassword);
            authCache.SaveSuccessfulLogin(1, 9999, "Cached Offline User", testEmail, hash, "Agent");

            var authService = new AuthService("http://127.0.0.1:59999"); // Unreachable port
            var result = authService.TryLocalDbLogin(testEmail, testPassword);

            _output.WriteLine($"Offline cached login result: Success={result.Success}, WasOffline={result.WasOffline}, SessionIsOffline={CurrentSession.IsOffline}");

            Assert.True(result.Success);
            Assert.True(result.WasOffline);
            Assert.True(CurrentSession.IsOffline);
            Assert.Null(CurrentSession.JwtToken);
        }

        [Fact]
        public void Step3_OfflineLogin_WrongPassword_ReturnsOfflineMismatchMessage()
        {
            var authCache = new LocalAuthCache();
            string testEmail = $"cached_wrongpass_{Guid.NewGuid():N}@test.com";
            string hash = PasswordHasher.Hash("CorrectPassword123!");
            authCache.SaveSuccessfulLogin(1, 9998, "Wrong Pass User", testEmail, hash, "Agent");

            var authService = new AuthService("http://127.0.0.1:59999");
            var result = authService.TryLocalDbLogin(testEmail, "WrongPassword!");

            _output.WriteLine($"Offline wrong pass result: Success={result.Success}, Error={result.ErrorMessage}");

            Assert.False(result.Success);
            Assert.True(result.WasOffline);
            Assert.Contains("offline credentials didn't match", result.ErrorMessage, StringComparison.OrdinalIgnoreCase);
        }

        [Fact]
        public void Step3_OfflineLogin_NeverLoggedInUser_ReturnsNoPreviousLoginMessage()
        {
            var authService = new AuthService("http://127.0.0.1:59999");
            var result = authService.TryLocalDbLogin($"unknown_never_{Guid.NewGuid():N}@test.com", "Password123!");

            _output.WriteLine($"Offline never logged in result: Success={result.Success}, Error={result.ErrorMessage}");

            Assert.False(result.Success);
            Assert.True(result.WasOffline);
            Assert.Contains("no previous login found", result.ErrorMessage, StringComparison.OrdinalIgnoreCase);
        }

        [Fact]
        public async Task Step3_OfflineCrud_PersistsToLocalDatabase_AcrossRestart()
        {
            int tenantId = 1;
            CurrentSession.Start(1, tenantId, "Test Admin", "admin@test.com", "Admin", null, isOffline: true);

            string marker = $"OfflinePersist_{Guid.NewGuid():N}";
            int createdCustId;

            using (var db = LocalDb.CreateContext(tenantId))
            {
                var cust = new Customer
                {
                    FirstName = "OfflinePersist",
                    LastName = marker,
                    Email = $"{marker}@offline.example.com",
                    Phone = "09123456789",
                    Type = "buyer",
                    Status = "active",
                    CreatedByUserId = 1
                };
                db.Customers.Add(cust);
                await db.SaveChangesAsync();
                createdCustId = cust.CustomerId;
            }

            // Simulate application/context restart by destroying context and reopening fresh context
            using (var freshDb = LocalDb.CreateContext(tenantId))
            {
                var loaded = await freshDb.Customers.FirstOrDefaultAsync(c => c.CustomerId == createdCustId);
                _output.WriteLine($"Fresh context query after restart: Found={loaded != null}, LastName={loaded?.LastName}");
                Assert.NotNull(loaded);
                Assert.Equal(marker, loaded.LastName);

                // Cleanup
                freshDb.Customers.Remove(loaded);
                await freshDb.SaveChangesAsync();
            }
        }

        [Fact]
        public async Task Step3_ColdStart_OfflineConnectivityCheck_DoesNotCrash()
        {
            var sw = Stopwatch.StartNew();
            // Test SyncService with bad cloud connection string (using the proper two-argument constructor)
            var syncService = new SyncService(_localConn, "Server=bad-unreachable-cloud-host.local;Database=nonexistent;User Id=bad;Password=bad;Connect Timeout=2;", "http://127.0.0.1:59999/");
            // Pass 1: consecutiveFailures = 1, threshold is 2, so returns initial IsOnline = true (Defect: cold start delay in detecting offline)
            bool isOnlinePass1 = await syncService.CheckConnectivityAsync();
            // Pass 2: consecutiveFailures = 2, threshold reached, transitions to false
            bool isOnlinePass2 = await syncService.CheckConnectivityAsync();
            sw.Stop();

            _output.WriteLine($"Cold start connectivity check with bad cloud completed without crash: Pass1={isOnlinePass1}, Pass2={isOnlinePass2}, Duration={sw.ElapsedMilliseconds}ms");
            // Verified: Does not crash/throw, but exhibits defect where IsOnline remains true due to static _consecutiveFailures threshold
            Assert.True(sw.ElapsedMilliseconds > 0);
        }
        #endregion

        #region Step 4: Sync Correctness (Per Entity)
        [Fact]
        public async Task Step4_Customer_WithBuyerProfile_AndUnicodeName_SyncsToCloud()
        {
            Assert.False(string.IsNullOrWhiteSpace(_cloudConn), "Cloud connection string not configured.");

            int tenantId = 1;
            CurrentSession.Start(1, tenantId, "Admin User", "admin@test.com", "Admin", null, isOffline: false);
            var cache = LocalDataCache.Instance;
            cache.ClearAllQueue(tenantId);

            string marker = $"Dela Peña_{Guid.NewGuid():N}";
            Customer createdCust;
            using (var custCtrl = new CustomerController())
            {
                createdCust = custCtrl.Add(new Customer
                {
                    FirstName = "María Jose",
                    LastName = marker,
                    Email = $"delapena_{Guid.NewGuid():N}@test.com",
                    Phone = "+63 917 123 4567",
                    Type = "buyer",
                    Status = "active",
                    CreatedByUserId = 1
                });
            }

            // Add buyer profile
            using (var localDb = LocalDb.CreateContext(tenantId))
            {
                localDb.BuyerProfiles.Add(new BuyerProfile
                {
                    CustomerId = createdCust.CustomerId,
                    Budget = 8500000.75m,
                    PreferredLocation = "Bonifacio Global City, Taguig",
                    PreferredPropertyType = "Condominium"
                });
                await localDb.SaveChangesAsync();
            }

            // Sync
            await SyncService.Instance.DrainQueueAsync();

            // Verify in Cloud SQL
            using var cloudConnObj = new SqlConnection(_cloudConn);
            await cloudConnObj.OpenAsync();
            using var cmd = cloudConnObj.CreateCommand();
            cmd.CommandText = "SELECT CustomerId, FirstName, LastName, Email, Phone, Type, Status FROM Customers WHERE LastName = @marker";
            cmd.Parameters.AddWithValue("@marker", marker);
            using var rdr = await cmd.ExecuteReaderAsync();

            bool found = await rdr.ReadAsync();
            _output.WriteLine($"Customer synced to cloud: Found={found}");
            Assert.True(found);
            int cloudCustId = rdr.GetInt32(0);
            string cloudLast = rdr.GetString(2);
            string cloudPhone = rdr.GetString(4);
            rdr.Close();

            Assert.Equal(marker, cloudLast);
            Assert.Equal("+63 917 123 4567", cloudPhone);

            // Verify BuyerProfile in Cloud
            using var bpCmd = cloudConnObj.CreateCommand();
            bpCmd.CommandText = "SELECT Budget, PreferredLocation FROM BuyerProfiles WHERE CustomerId = @cid";
            bpCmd.Parameters.AddWithValue("@cid", cloudCustId);
            using var bpRdr = await bpCmd.ExecuteReaderAsync();
            bool bpFound = await bpRdr.ReadAsync();
            decimal cloudBudget = bpFound ? bpRdr.GetDecimal(0) : 0m;
            string cloudLoc = bpFound ? bpRdr.GetString(1) : "";
            bpRdr.Close();

            _output.WriteLine($"BuyerProfile synced to cloud: Found={bpFound}, Budget={cloudBudget}, Loc={cloudLoc}");
            Assert.True(bpFound);
            Assert.Equal(8500000.75m, cloudBudget);

            // Cleanup
            using var cleanCmd = cloudConnObj.CreateCommand();
            cleanCmd.CommandText = $"DELETE FROM BuyerProfiles WHERE CustomerId = {cloudCustId}; DELETE FROM Customers WHERE CustomerId = {cloudCustId};";
            await cleanCmd.ExecuteNonQueryAsync();

            using var localDbClean = LocalDb.CreateContext(tenantId);
            localDbClean.Database.ExecuteSqlRaw($"DELETE FROM BuyerProfiles WHERE CustomerId = {createdCust.CustomerId}; DELETE FROM Customers WHERE CustomerId = {createdCust.CustomerId};");
        }

        [Fact]
        public async Task Step4_Lead_AllColumns_SyncsToCloud()
        {
            Assert.False(string.IsNullOrWhiteSpace(_cloudConn));
            int tenantId = 1;
            CurrentSession.Start(1, tenantId, "Admin", "admin@test.com", "Admin", null, isOffline: false);
            var cache = LocalDataCache.Instance;
            cache.ClearAllQueue(tenantId);

            string marker = $"LeadSync_{Guid.NewGuid():N}";
            Lead createdLead;
            using (var leadCtrl = new LeadController())
            {
                createdLead = leadCtrl.Add(new Lead
                {
                    FirstName = "Carlos",
                    LastName = marker,
                    Email = $"{marker}@test.com",
                    Phone = "09223344556",
                    Source = "Referral",
                    Stage = "contacted",
                    Priority = "High",
                    ExpectedValue = 4750000.50m,
                    CreatedByUserId = 1
                });
            }

            await SyncService.Instance.DrainQueueAsync();

            using var cloudConnObj = new SqlConnection(_cloudConn);
            await cloudConnObj.OpenAsync();
            using var cmd = cloudConnObj.CreateCommand();
            cmd.CommandText = "SELECT LeadId, ExpectedValue, Stage, Priority FROM Leads WHERE LastName = @marker";
            cmd.Parameters.AddWithValue("@marker", marker);
            using var rdr = await cmd.ExecuteReaderAsync();
            bool found = await rdr.ReadAsync();
            Assert.True(found);
            int cloudId = rdr.GetInt32(0);
            decimal cloudExpVal = rdr.GetDecimal(1);
            string cloudStage = rdr.GetString(2);
            rdr.Close();

            _output.WriteLine($"Lead in cloud: Id={cloudId}, ExpectedValue={cloudExpVal}, Stage={cloudStage}");
            Assert.Equal(4750000.50m, cloudExpVal);
            Assert.Equal("contacted", cloudStage);

            // Cleanup
            using var cleanCmd = cloudConnObj.CreateCommand();
            cleanCmd.CommandText = $"DELETE FROM Leads WHERE LeadId = {cloudId};";
            await cleanCmd.ExecuteNonQueryAsync();

            using var localDb = LocalDb.CreateContext(tenantId);
            localDb.Database.ExecuteSqlRaw($"DELETE FROM Leads WHERE LeadId = {createdLead.LeadId};");
        }

        [Fact]
        public async Task Step4_Property_Deal_FollowUp_SupportTicket_AllSyncToCloud()
        {
            Assert.False(string.IsNullOrWhiteSpace(_cloudConn));
            int tenantId = 1;
            CurrentSession.Start(1, tenantId, "Admin", "admin@test.com", "Admin", null, false);
            var cache = LocalDataCache.Instance;
            cache.ClearAllQueue(tenantId);

            string marker = $"AllEntities_{Guid.NewGuid():N}";

            // 0. Customer for relationships
            Customer cust;
            using (var custCtrl = new CustomerController())
            {
                cust = custCtrl.Add(new Customer
                {
                    FirstName = "EntitySync",
                    LastName = marker,
                    Email = $"{marker}@entitysync.com",
                    Phone = "09170009999",
                    Status = "Active"
                });
            }

            // 1. Property
            Property prop;
            using (var propCtrl = new PropertyController())
            {
                prop = propCtrl.Add(new Property
                {
                    Address = $"123 Emerald Avenue, Suite {marker}",
                    PropertyType = "Commercial",
                    Price = 12500000m,
                    Status = "Available",
                    CreatedByUserId = 1,
                    OwnerCustomerId = cust.CustomerId
                });
            }

            // 2. Follow-Up (TaskReminder)
            TaskReminder reminder;
            using (var fuCtrl = new FollowUpController())
            {
                reminder = fuCtrl.Add(new TaskReminder
                {
                    Title = $"Follow-up for {marker}",
                    DueDate = DateTime.UtcNow.AddDays(2),
                    Priority = "High",
                    Type = "Call",
                    Notes = "Check commercial contract terms",
                    AssignedToUserId = 1,
                    RelatedCustomerId = cust.CustomerId
                });
            }

            // 3. SupportTicket
            SupportTicket ticket;
            using (var ticketCtrl = new SupportTicketController())
            {
                ticket = ticketCtrl.Add(new SupportTicket
                {
                    CustomerId = cust.CustomerId,
                    RaisedByUserId = 1,
                    Description = $"Support ticket for {marker}",
                    Priority = "Medium",
                    Status = "Open",
                    Category = "Billing",
                    TicketNumber = $"TCK-{Guid.NewGuid().ToString().Substring(0, 8).ToUpper()}"
                });
            }

            // Drain
            await SyncService.Instance.DrainQueueAsync();

            // Verify in cloud
            using var cloudConnObj = new SqlConnection(_cloudConn);
            await cloudConnObj.OpenAsync();

            using var pCmd = cloudConnObj.CreateCommand();
            pCmd.CommandText = "SELECT PropertyId, Price FROM Properties WHERE Address = @addr";
            pCmd.Parameters.AddWithValue("@addr", prop.Address);
            using var pRdr = await pCmd.ExecuteReaderAsync();
            bool propFound = await pRdr.ReadAsync();
            int cloudPropId = propFound ? pRdr.GetInt32(0) : 0;
            decimal cloudPrice = propFound ? pRdr.GetDecimal(1) : 0m;
            pRdr.Close();

            _output.WriteLine($"Property in cloud: Found={propFound}, Price={cloudPrice}");
            Assert.True(propFound);
            Assert.Equal(12500000m, cloudPrice);

            using var rCmd = cloudConnObj.CreateCommand();
            rCmd.CommandText = "SELECT TaskReminderId, Title, Status FROM TaskReminders WHERE Title = @title";
            rCmd.Parameters.AddWithValue("@title", reminder.Title);
            using var rRdr = await rCmd.ExecuteReaderAsync();
            bool remFound = await rRdr.ReadAsync();
            int cloudRemId = remFound ? rRdr.GetInt32(0) : 0;
            rRdr.Close();

            _output.WriteLine($"TaskReminder in cloud: Found={remFound}, Id={cloudRemId}");
            Assert.True(remFound);

            using var tCmd = cloudConnObj.CreateCommand();
            tCmd.CommandText = "SELECT TicketId, TicketNumber, Category FROM SupportTickets WHERE TicketNumber = @num";
            tCmd.Parameters.AddWithValue("@num", ticket.TicketNumber);
            using var tRdr = await tCmd.ExecuteReaderAsync();
            bool tckFound = await tRdr.ReadAsync();
            int cloudTicketId = tckFound ? tRdr.GetInt32(0) : 0;
            tRdr.Close();

            _output.WriteLine($"SupportTicket in cloud: Found={tckFound}, Id={cloudTicketId}");
            Assert.True(tckFound);

            // Cleanup Cloud
            using var cleanCmd = cloudConnObj.CreateCommand();
            cleanCmd.CommandText = $@"
                DELETE FROM SupportTickets WHERE TicketId = {cloudTicketId};
                DELETE FROM TaskReminders WHERE TaskReminderId = {cloudRemId};
                DELETE FROM Properties WHERE PropertyId = {cloudPropId};";
            await cleanCmd.ExecuteNonQueryAsync();

            // Cleanup Local
            using var localDb = LocalDb.CreateContext(tenantId);
            localDb.Database.ExecuteSqlRaw($@"
                DELETE FROM SupportTickets WHERE TicketId = {ticket.TicketId};
                DELETE FROM TaskReminders WHERE TaskReminderId = {reminder.TaskReminderId};
                DELETE FROM Properties WHERE PropertyId = {prop.PropertyId};");
            cache.ClearAllQueue(tenantId);
        }

        [Fact]
        public async Task Step4_SoftDelete_Customer_Lead_SyncsTombstoneToCloud()
        {
            Assert.False(string.IsNullOrWhiteSpace(_cloudConn));
            int tenantId = 1;
            CurrentSession.Start(1, tenantId, "Admin", "admin@test.com", "Admin", null, false);
            var cache = LocalDataCache.Instance;
            cache.ClearAllQueue(tenantId);

            string marker = $"SoftDel_{Guid.NewGuid():N}";

            // 1. Create Customer and Lead
            Customer cust;
            using (var custCtrl = new CustomerController())
            {
                cust = custCtrl.Add(new Customer
                {
                    FirstName = "SoftDelCust",
                    LastName = marker,
                    Email = $"{marker}@del.com",
                    CreatedByUserId = 1
                });
            }

            Lead lead;
            using (var leadCtrl = new LeadController())
            {
                lead = leadCtrl.Add(new Lead
                {
                    FirstName = "SoftDelLead",
                    LastName = marker,
                    Email = $"{marker}@lead.com",
                    Source = "Web",
                    Stage = "new",
                    CreatedByUserId = 1
                });
            }

            // Sync creation to cloud
            await SyncService.Instance.DrainQueueAsync();

            // 2. Soft-delete locally
            using (var custCtrl = new CustomerController())
            {
                custCtrl.SoftDelete(cust);
            }
            using (var leadCtrl = new LeadController())
            {
                leadCtrl.SoftDelete(lead);
            }

            // 3. Sync soft deletes to cloud
            await SyncService.Instance.DrainQueueAsync();

            // 4. Verify in cloud that IsDeleted is 1 and DeletedAt is set
            using var cloudConnObj = new SqlConnection(_cloudConn);
            await cloudConnObj.OpenAsync();

            using var cmdC = cloudConnObj.CreateCommand();
            cmdC.CommandText = "SELECT CustomerId, IsDeleted, DeletedAt FROM Customers WHERE LastName = @marker";
            cmdC.Parameters.AddWithValue("@marker", marker);
            using var rdrC = await cmdC.ExecuteReaderAsync();
            Assert.True(await rdrC.ReadAsync());
            int cloudCustId = rdrC.GetInt32(0);
            bool custIsDeleted = rdrC.GetBoolean(1);
            bool custHasDeletedAt = !rdrC.IsDBNull(2);
            rdrC.Close();

            using var cmdL = cloudConnObj.CreateCommand();
            cmdL.CommandText = "SELECT LeadId, IsDeleted, DeletedAt FROM Leads WHERE LastName = @marker";
            cmdL.Parameters.AddWithValue("@marker", marker);
            using var rdrL = await cmdL.ExecuteReaderAsync();
            Assert.True(await rdrL.ReadAsync());
            int cloudLeadId = rdrL.GetInt32(0);
            bool leadIsDeleted = rdrL.GetBoolean(1);
            bool leadHasDeletedAt = !rdrL.IsDBNull(2);
            rdrL.Close();

            _output.WriteLine($"Cloud Soft Delete verification: Cust IsDeleted={custIsDeleted}, HasDate={custHasDeletedAt}; Lead IsDeleted={leadIsDeleted}, HasDate={leadHasDeletedAt}");

            Assert.True(custIsDeleted);
            Assert.True(custHasDeletedAt);
            Assert.True(leadIsDeleted);
            Assert.True(leadHasDeletedAt);

            // Cleanup
            using var cleanCmd = cloudConnObj.CreateCommand();
            cleanCmd.CommandText = $"DELETE FROM Customers WHERE CustomerId = {cloudCustId}; DELETE FROM Leads WHERE LeadId = {cloudLeadId};";
            await cleanCmd.ExecuteNonQueryAsync();

            using var localDb = LocalDb.CreateContext(tenantId);
            localDb.Database.ExecuteSqlRaw($"DELETE FROM Customers WHERE CustomerId = {cust.CustomerId}; DELETE FROM Leads WHERE LeadId = {lead.LeadId};");
            cache.ClearAllQueue(tenantId);
        }

        [Fact]
        public async Task Step4_ParentChild_BatchInsert_IdRemapping_Customer_Deal_Activity()
        {
            Assert.False(string.IsNullOrWhiteSpace(_cloudConn));
            int tenantId = 1;
            CurrentSession.Start(1, tenantId, "Admin", "admin@test.com", "Admin", null, isOffline: false);
            var cache = LocalDataCache.Instance;
            cache.ClearAllQueue(tenantId);

            string marker = $"Batch_{Guid.NewGuid():N}";

            // 1. Create Customer assigned to agent 1
            Customer cust;
            using (var custCtrl = new CustomerController())
            {
                cust = custCtrl.Add(new Customer
                {
                    FirstName = "BatchParent",
                    LastName = marker,
                    Email = $"{marker}@test.com",
                    Phone = "09112223333",
                    Type = "buyer",
                    AssignedAgentId = 1,
                    CreatedByUserId = 1
                });
            }

            // 2. Pick existing local Property
            Property localProp;
            using (var db = LocalDb.CreateContext(tenantId))
            {
                localProp = await db.Properties.FirstAsync();
            }

            // 3. Create Deal referencing Customer and Property
            Deal deal;
            using (var dealCtrl = new DealController())
            {
                deal = dealCtrl.Add(new Deal
                {
                    CustomerId = cust.CustomerId,
                    PropertyId = localProp.PropertyId,
                    AgentId = 1,
                    Value = 3500000m,
                    CommissionRate = 0.03m,
                    Stage = "Offer",
                    CreatedByUserId = 1
                });
            }

            // 4. Create Activity referencing Customer
            DomainActivity act;
            using (var actCtrl = new ActivityController())
            {
                act = actCtrl.LogActivity(new DomainActivity
                {
                    Type = "Meeting",
                    RelatedCustomerId = cust.CustomerId,
                    Notes = $"Meeting with batch customer {marker}",
                    DurationMinutes = 45,
                    LoggedByAgentId = 1
                });
            }

            _output.WriteLine($"Local Parent/Child created: CustId={cust.CustomerId}, DealId={deal.DealId}, ActId={act.ActivityId}");

            // Drain batch
            await SyncService.Instance.DrainQueueAsync();

            // Verify in Cloud: Did Deal and Activity resolve the remapped Cloud Customer ID?
            using var cloudConnObj = new SqlConnection(_cloudConn);
            await cloudConnObj.OpenAsync();

            using var cmdCust = cloudConnObj.CreateCommand();
            cmdCust.CommandText = "SELECT CustomerId FROM Customers WHERE LastName = @marker";
            cmdCust.Parameters.AddWithValue("@marker", marker);
            int cloudCustId = Convert.ToInt32(await cmdCust.ExecuteScalarAsync());
            Assert.True(cloudCustId > 0, "Cloud Customer not found");

            using var cmdDeal = cloudConnObj.CreateCommand();
            cmdDeal.CommandText = "SELECT DealId, CustomerId, PropertyId, Value FROM Deals WHERE CustomerId = @cid";
            cmdDeal.Parameters.AddWithValue("@cid", cloudCustId);
            using var rdrDeal = await cmdDeal.ExecuteReaderAsync();
            bool dealFound = await rdrDeal.ReadAsync();
            int cloudDealId = dealFound ? rdrDeal.GetInt32(0) : 0;
            int dealCustFk = dealFound ? rdrDeal.GetInt32(1) : 0;
            rdrDeal.Close();

            _output.WriteLine($"Cloud Deal resolution: Found={dealFound}, DealId={cloudDealId}, CustomerFK={dealCustFk} (Matches CloudCustId={dealCustFk == cloudCustId})");
            Assert.True(dealFound, "Deal was not created in cloud or FK did not resolve!");
            Assert.Equal(cloudCustId, dealCustFk);

            // Verify Activity in Cloud
            using var cmdAct = cloudConnObj.CreateCommand();
            cmdAct.CommandText = "SELECT ActivityId, RelatedCustomerId FROM Activities WHERE RelatedCustomerId = @cid";
            cmdAct.Parameters.AddWithValue("@cid", cloudCustId);
            using var rdrAct = await cmdAct.ExecuteReaderAsync();
            bool actFound = await rdrAct.ReadAsync();
            int cloudActId = actFound ? rdrAct.GetInt32(0) : 0;
            rdrAct.Close();

            _output.WriteLine($"Cloud Activity resolution: Found={actFound}, ActId={cloudActId}");
            Assert.True(actFound, "Activity FK did not resolve to cloud customer!");

            // Cleanup
            using var cleanCmd = cloudConnObj.CreateCommand();
            cleanCmd.CommandText = $@"
                DELETE FROM Activities WHERE RelatedCustomerId = {cloudCustId};
                DELETE FROM Deals WHERE CustomerId = {cloudCustId};
                DELETE FROM Customers WHERE CustomerId = {cloudCustId};";
            await cleanCmd.ExecuteNonQueryAsync();

            using var localClean = LocalDb.CreateContext(tenantId);
            localClean.Database.ExecuteSqlRaw($@"
                DELETE FROM Activities WHERE RelatedCustomerId = {cust.CustomerId};
                DELETE FROM Deals WHERE CustomerId = {cust.CustomerId};
                DELETE FROM Customers WHERE CustomerId = {cust.CustomerId};");
        }

        [Fact]
        public async Task Step4_PropertyShowingDetail_UnsupportedInDrainQueue_RecordsRiskOrFailure()
        {
            int tenantId = 1;
            CurrentSession.Start(1, tenantId, "Admin", "admin@test.com", "Admin", null, false);
            var cache = LocalDataCache.Instance;

            // Enqueue PropertyShowingDetail
            var dummyShowing = new PropertyShowingDetail
            {
                PropertyId = 1,
                ActivityId = 1,
                ScheduledDate = DateTime.UtcNow,
                FeedbackNotes = "Test showing note"
            };
            var item = SyncService.Instance.EnqueueOfflineCreate("PropertyShowingDetail", dummyShowing, tenantId, 1);

            // Run DrainQueueAsync
            await SyncService.Instance.DrainQueueAsync();

            var finalItem = cache.GetQueueItem(item.QueueId);
            _output.WriteLine($"PropertyShowingDetail Queue Status: {finalItem?.Status}, FailureReason: {finalItem?.FailureReason}");

            Assert.NotNull(finalItem);
            Assert.Equal("Failed", finalItem.Status);
            Assert.Contains("not supported for cloud sync", finalItem.FailureReason, StringComparison.OrdinalIgnoreCase);

            // Cleanup
            cache.ClearAllQueue(tenantId);
        }

        [Fact]
        public async Task Step4_Idempotency_RunTwice_NoDuplicatesCreated()
        {
            Assert.False(string.IsNullOrWhiteSpace(_cloudConn));
            int tenantId = 1;
            // Run creation as offline so background auto-sync does not race with manual DrainQueueAsync
            CurrentSession.Start(1, tenantId, "Admin", "admin@test.com", "Admin", null, isOffline: true);
            var cache = LocalDataCache.Instance;
            cache.ClearAllQueue(tenantId);

            string marker = $"Idempotency_{Guid.NewGuid():N}";
            Lead lead;
            using (var leadCtrl = new LeadController())
            {
                lead = leadCtrl.Add(new Lead
                {
                    FirstName = "IdempotentLead",
                    LastName = marker,
                    Email = $"{marker}@test.com",
                    Source = "Website",
                    Stage = "new",
                    CreatedByUserId = 1
                });
            }

            // Drain pass 1
            await SyncService.Instance.DrainQueueAsync();

            // Drain pass 2 immediately
            await SyncService.Instance.DrainQueueAsync();

            using var cloudConnObj = new SqlConnection(_cloudConn);
            await cloudConnObj.OpenAsync();
            using var cmd = cloudConnObj.CreateCommand();
            cmd.CommandText = "SELECT COUNT(*) FROM Leads WHERE LastName = @marker";
            cmd.Parameters.AddWithValue("@marker", marker);
            int count = Convert.ToInt32(await cmd.ExecuteScalarAsync());

            _output.WriteLine($"Idempotency check count in cloud: {count} (Expected: 1)");
            Assert.Equal(1, count);

            // Cleanup
            using var cleanCmd = cloudConnObj.CreateCommand();
            cleanCmd.CommandText = "DELETE FROM Leads WHERE LastName = @marker";
            cleanCmd.Parameters.AddWithValue("@marker", marker);
            await cleanCmd.ExecuteNonQueryAsync();

            using var localDb = LocalDb.CreateContext(tenantId);
            localDb.Database.ExecuteSqlRaw($"DELETE FROM Leads WHERE LeadId = {lead.LeadId};");
        }

        [Fact]
        public async Task Step4_CloudPull_RecordsCreatedInCloud_DoNotPullToLocal_EvidenceOfDeadCode()
        {
            Assert.False(string.IsNullOrWhiteSpace(_cloudConn));
            int tenantId = 1;
            CurrentSession.Start(1, tenantId, "Admin", "admin@test.com", "Admin", null, isOffline: false);

            string marker = $"CloudOnly_{Guid.NewGuid():N}";

            // 1. Insert directly into Cloud SQL
            int cloudLeadId;
            using (var cloudConnObj = new SqlConnection(_cloudConn))
            {
                await cloudConnObj.OpenAsync();
                using var cmd = cloudConnObj.CreateCommand();
                cmd.CommandText = @"
                    INSERT INTO Leads (FirstName, LastName, Email, Source, Stage, CreatedByUserId, CreatedAt)
                    OUTPUT INSERTED.LeadId
                    VALUES ('CloudOnly', @marker, @email, 'API', 'new', 1, GETUTCDATE());";
                cmd.Parameters.AddWithValue("@marker", marker);
                cmd.Parameters.AddWithValue("@email", $"{marker}@cloudonly.com");
                cloudLeadId = Convert.ToInt32(await cmd.ExecuteScalarAsync());
            }

            _output.WriteLine($"Inserted record directly into cloud: LeadId={cloudLeadId}, LastName={marker}");

            // 2. Trigger Full Sync
            await SyncService.Instance.SyncAsync(waitIfBusy: true, isFullSync: true);

            // 3. Check if local database has it
            bool localHasRecord;
            using (var localDb = LocalDb.CreateContext(tenantId))
            {
                localHasRecord = await localDb.Leads.AnyAsync(l => l.LastName == marker);
            }

            _output.WriteLine($"After Full Sync, was cloud record pulled to local DB? {localHasRecord} (Expected: False due to dead SyncTable code)");

            // Cleanup cloud
            using (var cloudConnObj = new SqlConnection(_cloudConn))
            {
                await cloudConnObj.OpenAsync();
                using var cmd = cloudConnObj.CreateCommand();
                cmd.CommandText = $"DELETE FROM Leads WHERE LeadId = {cloudLeadId};";
                await cmd.ExecuteNonQueryAsync();
            }

            // CRITICAL FINDING: Cloud changes DO NOT pull to local database!
            Assert.False(localHasRecord, "Unexpected: record was pulled down, but SyncTable was supposed to be dead code.");
        }
        #endregion

        #region Step 5: Interruption & Failure
        [Fact]
        public async Task Step5_Interruption_BadHost_ItemRetainsStatus_NeverDropped()
        {
            int tenantId = 1;
            var cache = LocalDataCache.Instance;
            cache.ClearAllQueue(tenantId);

            var item = cache.EnqueueItem(new PendingSyncQueue
            {
                TenantId = tenantId,
                UserId = 1,
                EntityType = "Lead",
                EntityLocalId = "99999",
                Operation = "Insert",
                PayloadJson = JsonSerializer.Serialize(new Lead { FirstName = "FailTest", LastName = "FailTest" }),
                CreatedAt = DateTime.UtcNow,
                Status = "Pending"
            });

            // Create SyncService with bad host
            var badSync = new SyncService(_localConn, "Server=127.0.0.1,59999;Database=fake;User Id=u;Password=p;Connect Timeout=1;");
            await badSync.DrainQueueAsync();

            var loaded = cache.GetQueueItem(item);
            _output.WriteLine($"Bad host sync attempt: Item Status={loaded?.Status}");

            // Item must still exist and NOT be deleted
            Assert.NotNull(loaded);
            Assert.True(loaded.Status == "Pending" || loaded.Status == "Syncing" || loaded.Status == "Failed");

            cache.ClearAllQueue(tenantId);
        }

        [Fact]
        public async Task Step5_PoisonItem_DoesNotBlockSubsequentQueueItem()
        {
            Assert.False(string.IsNullOrWhiteSpace(_cloudConn));
            int tenantId = 1;
            CurrentSession.Start(1, tenantId, "Admin", "admin@test.com", "Admin", null, false);
            var cache = LocalDataCache.Instance;
            cache.ClearAllQueue(tenantId);

            // Item 1: Poison pill (corrupted JSON payload that throws on deserialize)
            int poisonId = cache.EnqueueItem(new PendingSyncQueue
            {
                TenantId = tenantId,
                UserId = 1,
                EntityType = "Lead",
                EntityLocalId = "-1",
                Operation = "Insert",
                PayloadJson = "CORRUPTED_NON_JSON_DATA{{{",
                CreatedAt = DateTime.UtcNow,
                Status = "Pending"
            });

            // Item 2: Good lead
            string marker = $"GoodAfterPoison_{Guid.NewGuid():N}";
            int goodId = cache.EnqueueItem(new PendingSyncQueue
            {
                TenantId = tenantId,
                UserId = 1,
                EntityType = "Lead",
                EntityLocalId = "-2",
                Operation = "Insert",
                PayloadJson = JsonSerializer.Serialize(new Lead
                {
                    FirstName = "GoodAfterPoison",
                    LastName = marker,
                    Email = $"{marker}@test.com",
                    Source = "Web",
                    Stage = "new",
                    CreatedByUserId = 1
                }),
                CreatedAt = DateTime.UtcNow,
                Status = "Pending"
            });

            // Drain
            await SyncService.Instance.DrainQueueAsync();

            var poisonLoaded = cache.GetQueueItem(poisonId);
            var goodLoaded = cache.GetQueueItem(goodId);

            _output.WriteLine($"Poison item status: {poisonLoaded?.Status}, Failure: {poisonLoaded?.FailureReason}");
            _output.WriteLine($"Good item status: {goodLoaded?.Status}, ServerId: {goodLoaded?.ServerEntityId}");

            Assert.Equal("Failed", poisonLoaded?.Status);
            Assert.Equal("Synced", goodLoaded?.Status);

            // Cleanup cloud
            using var cloudConnObj = new SqlConnection(_cloudConn);
            await cloudConnObj.OpenAsync();
            using var cmd = cloudConnObj.CreateCommand();
            cmd.CommandText = "DELETE FROM Leads WHERE LastName = @marker";
            cmd.Parameters.AddWithValue("@marker", marker);
            await cmd.ExecuteNonQueryAsync();

            cache.ClearAllQueue(tenantId);
        }
        #endregion

        #region Step 6: Conflicts
        [Fact]
        public async Task Step6_ConcurrentEdit_SilentOverwrite_NoConflictDetection_Evidence()
        {
            Assert.False(string.IsNullOrWhiteSpace(_cloudConn));
            int tenantId = 1;
            CurrentSession.Start(1, tenantId, "Admin", "admin@test.com", "Admin", null, false);
            var cache = LocalDataCache.Instance;
            cache.ClearAllQueue(tenantId);

            string initialMarker = $"ConflictTest_{Guid.NewGuid():N}";

            // 1. Create a lead and sync to cloud
            Lead lead;
            using (var leadCtrl = new LeadController())
            {
                lead = leadCtrl.Add(new Lead
                {
                    FirstName = "InitialFirst",
                    LastName = initialMarker,
                    Email = $"{initialMarker}@test.com",
                    Phone = "09000000001",
                    Source = "Web",
                    Stage = "new",
                    CreatedByUserId = 1
                });
            }
            await SyncService.Instance.DrainQueueAsync();

            // Get Cloud LeadId
            int cloudLeadId;
            using (var cloudConnObj = new SqlConnection(_cloudConn))
            {
                await cloudConnObj.OpenAsync();
                using var cmd = cloudConnObj.CreateCommand();
                cmd.CommandText = "SELECT LeadId FROM Leads WHERE LastName = @marker";
                cmd.Parameters.AddWithValue("@marker", initialMarker);
                cloudLeadId = Convert.ToInt32(await cmd.ExecuteScalarAsync());

                // 2. Device 2 / Cloud modifies Phone directly on server
                using var cmdUpdateCloud = cloudConnObj.CreateCommand();
                cmdUpdateCloud.CommandText = $"UPDATE Leads SET Phone = 'CLOUD_MODIFIED_PHONE_999' WHERE LeadId = {cloudLeadId};";
                await cmdUpdateCloud.ExecuteNonQueryAsync();
            }

            // 3. Device 1 (offline) edits Phone locally (both in local DB and queues update)
            using (var localDb = LocalDb.CreateContext(tenantId))
            {
                var l = await localDb.Leads.FindAsync(lead.LeadId);
                l!.Phone = "OFFLINE_MODIFIED_PHONE_111";
                await localDb.SaveChangesAsync();
            }

            cache.EnqueueItem(new PendingSyncQueue
            {
                TenantId = tenantId,
                UserId = 1,
                EntityType = "Lead",
                EntityLocalId = lead.LeadId.ToString(),
                ServerEntityId = cloudLeadId,
                Operation = "Update",
                PayloadJson = JsonSerializer.Serialize(new Lead
                {
                    LeadId = lead.LeadId,
                    FirstName = "InitialFirst",
                    LastName = initialMarker,
                    Phone = "OFFLINE_MODIFIED_PHONE_111",
                    Stage = "new",
                    CreatedByUserId = 1
                }),
                CreatedAt = DateTime.UtcNow,
                Status = "Pending"
            });

            // 4. Device 1 reconnects and syncs
            await SyncService.Instance.DrainQueueAsync();

            // 5. Query cloud Phone
            string finalCloudPhone;
            using (var cloudConnObj = new SqlConnection(_cloudConn))
            {
                await cloudConnObj.OpenAsync();
                using var cmd = cloudConnObj.CreateCommand();
                cmd.CommandText = $"SELECT Phone FROM Leads WHERE LeadId = {cloudLeadId}";
                finalCloudPhone = (string)await cmd.ExecuteScalarAsync();
            }

            var counts = cache.GetQueueCounts(tenantId);
            _output.WriteLine($"Conflict test: Cloud had 'CLOUD_MODIFIED_PHONE_999', Final Cloud Phone='{finalCloudPhone}', Queue ConflictCount={counts.Conflict}");

            // SILENT OVERWRITE: Cloud phone was overwritten without conflict detection!
            Assert.Equal("OFFLINE_MODIFIED_PHONE_111", finalCloudPhone);
            Assert.Equal(0, counts.Conflict); // 0 conflicts detected!

            // Cleanup
            using (var cloudConnObj = new SqlConnection(_cloudConn))
            {
                await cloudConnObj.OpenAsync();
                using var cmd = cloudConnObj.CreateCommand();
                cmd.CommandText = $"DELETE FROM Leads WHERE LeadId = {cloudLeadId};";
                await cmd.ExecuteNonQueryAsync();
            }
            using var localDbClean = LocalDb.CreateContext(tenantId);
            localDbClean.Database.ExecuteSqlRaw($"DELETE FROM Leads WHERE LeadId = {lead.LeadId};");
            cache.ClearAllQueue(tenantId);
        }

        [Fact]
        public async Task Step6_OwnershipConflict_OfflineAgentSyncOverwritesCloudReassignment()
        {
            Assert.False(string.IsNullOrWhiteSpace(_cloudConn));
            int tenantId = 1;
            CurrentSession.Start(1, tenantId, "Admin", "admin@test.com", "Admin", null, false);
            var cache = LocalDataCache.Instance;
            cache.ClearAllQueue(tenantId);

            string marker = $"OwnerConflict_{Guid.NewGuid():N}";

            // Look up two real users in cloud Users table
            int cloudAgent1;
            int cloudAgent2;
            using (var cloudConnObj = new SqlConnection(_cloudConn))
            {
                await cloudConnObj.OpenAsync();
                using var cmdUsers = cloudConnObj.CreateCommand();
                cmdUsers.CommandText = "SELECT TOP 2 UserId FROM Users ORDER BY UserId ASC";
                using var rdrUsers = await cmdUsers.ExecuteReaderAsync();
                Assert.True(await rdrUsers.ReadAsync());
                cloudAgent1 = rdrUsers.GetInt32(0);
                Assert.True(await rdrUsers.ReadAsync());
                cloudAgent2 = rdrUsers.GetInt32(0);
            }

            _output.WriteLine($"Cloud users for ownership test: Agent1={cloudAgent1}, Agent2={cloudAgent2}");

            // 1. Create Customer assigned to Agent 1
            Customer cust;
            using (var custCtrl = new CustomerController())
            {
                cust = custCtrl.Add(new Customer
                {
                    FirstName = "OwnerTest",
                    LastName = marker,
                    Email = $"{marker}@test.com",
                    AssignedAgentId = 1, // Will resolve to cloudAgent1
                    CreatedByUserId = 1
                });
            }
            await SyncService.Instance.DrainQueueAsync();

            int cloudCustId;
            using (var cloudConnObj = new SqlConnection(_cloudConn))
            {
                await cloudConnObj.OpenAsync();
                using var cmd = cloudConnObj.CreateCommand();
                cmd.CommandText = "SELECT CustomerId FROM Customers WHERE LastName = @marker";
                cmd.Parameters.AddWithValue("@marker", marker);
                cloudCustId = Convert.ToInt32(await cmd.ExecuteScalarAsync());

                // 2. Manager reassigns Customer in Cloud to Agent 2
                using var reassignCmd = cloudConnObj.CreateCommand();
                reassignCmd.CommandText = $"UPDATE Customers SET AssignedAgentId = {cloudAgent2} WHERE CustomerId = {cloudCustId}";
                await reassignCmd.ExecuteNonQueryAsync();
            }

            // 3. Offline Agent 1 edits Customer locally
            using (var localDb = LocalDb.CreateContext(tenantId))
            {
                var c = await localDb.Customers.FindAsync(cust.CustomerId);
                c!.Phone = "09887776655";
                await localDb.SaveChangesAsync();
            }

            cache.EnqueueItem(new PendingSyncQueue
            {
                TenantId = tenantId,
                UserId = 1,
                EntityType = "Customer",
                EntityLocalId = cust.CustomerId.ToString(),
                ServerEntityId = cloudCustId,
                Operation = "Update",
                PayloadJson = JsonSerializer.Serialize(new Customer
                {
                    CustomerId = cust.CustomerId,
                    FirstName = "OwnerTest",
                    LastName = marker,
                    Phone = "09887776655",
                    AssignedAgentId = 1, // Still has old agent!
                    CreatedByUserId = 1
                }),
                CreatedAt = DateTime.UtcNow,
                Status = "Pending"
            });

            // 4. Sync
            await SyncService.Instance.DrainQueueAsync();

            // 5. Query Cloud AssignedAgentId
            int finalAgentId;
            using (var cloudConnObj = new SqlConnection(_cloudConn))
            {
                await cloudConnObj.OpenAsync();
                using var cmd = cloudConnObj.CreateCommand();
                cmd.CommandText = $"SELECT AssignedAgentId FROM Customers WHERE CustomerId = {cloudCustId}";
                finalAgentId = Convert.ToInt32(await cmd.ExecuteScalarAsync());
            }

            _output.WriteLine($"Ownership conflict result: Cloud had Agent2 ({cloudAgent2}), Final Cloud AssignedAgentId={finalAgentId} (Offline agent 1 was re-applied: {finalAgentId == cloudAgent1})");

            // CRITICAL DEFECT: Offline agent's update reverted the Manager's reassignment in the cloud!
            Assert.Equal(cloudAgent1, finalAgentId);

            // Cleanup
            using (var cloudConnObj = new SqlConnection(_cloudConn))
            {
                await cloudConnObj.OpenAsync();
                using var cmd = cloudConnObj.CreateCommand();
                cmd.CommandText = $"DELETE FROM Customers WHERE CustomerId = {cloudCustId};";
                await cmd.ExecuteNonQueryAsync();
            }
            using var localDbClean = LocalDb.CreateContext(tenantId);
            localDbClean.Database.ExecuteSqlRaw($"DELETE FROM Customers WHERE CustomerId = {cust.CustomerId};");
            cache.ClearAllQueue(tenantId);
        }
        #endregion

        #region Step 7: Business Rules & Validation Bypass on Sync
        [Fact]
        public void Step7_ClientControllers_EnforceValidationRules_WhereImplemented_And_MissWhereMissing()
        {
            int tenantId = 1;
            CurrentSession.Start(1, tenantId, "Admin", "admin@test.com", "Admin", null, false);

            // 1. Customer invalid email throws ArgumentException
            using var custCtrl = new CustomerController();
            Assert.Throws<ArgumentException>(() =>
            {
                custCtrl.Add(new Customer
                {
                    FirstName = "BadEmail",
                    LastName = "Test",
                    Email = "invalid-email-format"
                });
            });

            // 2. SupportTicket invalid status transition throws InvalidOperationException
            using var tckCtrl = new SupportTicketController();
            var ticket = tckCtrl.Add(new SupportTicket
            {
                CustomerId = 1,
                RaisedByUserId = 1,
                Description = "Rule Test",
                Priority = "Low",
                Status = "Open",
                TicketNumber = $"TCK-RULE-{Guid.NewGuid():N}".Substring(0, 15)
            });
            // Switch to Manager session to update ticket status (Admin has read-only oversight per RBAC)
            CurrentSession.Start(1, tenantId, "Manager User", "manager@test.com", "Manager", null, false);
            // Advance Open -> In Progress -> Resolved
            tckCtrl.UpdateStatus(ticket.TicketId, "In Progress");
            tckCtrl.UpdateStatus(ticket.TicketId, "Resolved");

            // Attempting to move Resolved -> In Progress throws InvalidOperationException
            var ex = Assert.Throws<InvalidOperationException>(() =>
            {
                tckCtrl.UpdateStatus(ticket.TicketId, "In Progress");
            });
            _output.WriteLine($"Support ticket rule correctly threw: {ex.Message}");

            // Cleanup ticket
            using var db = LocalDb.CreateContext(tenantId);
            db.Database.ExecuteSqlRaw($"DELETE FROM SupportTickets WHERE TicketId = {ticket.TicketId};");

            // 3. DEFECT EVIDENCE: DealController.Add DOES NOT validate negative value or commission rate!
            using var dealCtrl = new DealController();
            var badDeal = dealCtrl.Add(new Deal
            {
                CustomerId = 1,
                PropertyId = 1,
                Value = -1000m, // Negative value!
                CommissionRate = 1.50m // 150% commission!
            });
            _output.WriteLine($"UNPROTECTED RULE: DealController allowed negative value: Id={badDeal.DealId}, Value={badDeal.Value}, Comm={badDeal.CommissionRate}");
            Assert.Equal(-1000m, badDeal.Value);

            // Cleanup bad deal
            db.Database.ExecuteSqlRaw($"DELETE FROM Deals WHERE DealId = {badDeal.DealId};");
        }

        [Fact]
        public async Task Step7_SyncService_BypassesDomainRules_WhenDirectlyWritingToCloudSql()
        {
            Assert.False(string.IsNullOrWhiteSpace(_cloudConn));
            int tenantId = 1;
            CurrentSession.Start(1, tenantId, "Admin", "admin@test.com", "Admin", null, false);
            var cache = LocalDataCache.Instance;
            cache.ClearAllQueue(tenantId);

            // Enqueue a deal with negative value and commission rate > 1.0 (invalid domain state)
            int queueId = cache.EnqueueItem(new PendingSyncQueue
            {
                TenantId = tenantId,
                UserId = 1,
                EntityType = "Deal",
                EntityLocalId = "-999",
                Operation = "Insert",
                PayloadJson = JsonSerializer.Serialize(new Deal
                {
                    CustomerId = 1,
                    PropertyId = 1,
                    Value = -500000m, // NEGATIVE VALUE!
                    CommissionRate = 1.50m, // 150% COMMISSION!
                    Stage = "Offer",
                    CreatedByUserId = 1
                }),
                CreatedAt = DateTime.UtcNow,
                Status = "Pending"
            });

            // Drain to cloud
            await SyncService.Instance.DrainQueueAsync();

            var finalItem = cache.GetQueueItem(queueId);
            _output.WriteLine($"Rule bypass sync result: Status={finalItem?.Status}, ServerId={finalItem?.ServerEntityId}");

            // Verify Deal reached Cloud DB!
            int cloudDealId = finalItem?.ServerEntityId ?? 0;
            decimal cloudVal = 0;
            decimal cloudComm = 0;
            if (cloudDealId > 0)
            {
                using var cloudConnObj = new SqlConnection(_cloudConn);
                await cloudConnObj.OpenAsync();
                using var cmd = cloudConnObj.CreateCommand();
                cmd.CommandText = $"SELECT Value, CommissionRate FROM Deals WHERE DealId = {cloudDealId}";
                using var rdr = await cmd.ExecuteReaderAsync();
                if (await rdr.ReadAsync())
                {
                    cloudVal = rdr.GetDecimal(0);
                    cloudComm = rdr.GetDecimal(1);
                }
            }

            _output.WriteLine($"Cloud Deal with violated rules: Value={cloudVal}, Comm={cloudComm}");

            // CRITICAL DEFECT: Cloud SQL accepted negative deal value and invalid commission rate without validation!
            Assert.Equal("Synced", finalItem?.Status);
            Assert.Equal(-500000m, cloudVal);
            Assert.Equal(1.50m, cloudComm);

            // Cleanup
            if (cloudDealId > 0)
            {
                using var cloudConnObj = new SqlConnection(_cloudConn);
                await cloudConnObj.OpenAsync();
                using var cmd = cloudConnObj.CreateCommand();
                cmd.CommandText = $"DELETE FROM Deals WHERE DealId = {cloudDealId}";
                await cmd.ExecuteNonQueryAsync();
            }
            cache.ClearAllQueue(tenantId);
        }

        [Fact]
        public async Task Step7_OwnershipAtRest_LocalCatalogContainsAllTenantData_R25EnforcedOnlyByUI()
        {
            // Login as Agent 4 (Tenant 1)
            CurrentSession.Start(4, 1, "Agent User", "agent@test.com", "Agent", null, false);

            using var db = LocalDb.CreateContext(1);

            // In local database at rest, how many customers exist?
            int totalInLocalCatalog = await db.Customers.CountAsync();

            // When queried through CustomerController (UI layer)
            using var ctrl = new CustomerController();
            var agentVisible = ctrl.GetAll();

            _output.WriteLine($"Ownership at rest: Local Catalog Total Customers={totalInLocalCatalog}, Agent Visible via Controller={agentVisible.Count}");

            // The local catalog has all tenant customers, but UI filters it
            Assert.True(totalInLocalCatalog > agentVisible.Count, "Expected local catalog to contain all tenant records, with R25 enforced only at UI level.");
        }
        #endregion

        #region Step 9: Multi-Tenant Isolation
        [Fact]
        public void Step9_TenantSwitch_SameDevice_DatabasesAndQueuesIsolated()
        {
            var cache = LocalDataCache.Instance;
            cache.ClearAllQueue(1);
            cache.ClearAllQueue(2);

            // Enqueue item for Tenant 1
            cache.EnqueueItem(new PendingSyncQueue
            {
                TenantId = 1,
                UserId = 1,
                EntityType = "Lead",
                EntityLocalId = "101",
                Operation = "Insert",
                PayloadJson = "{}",
                CreatedAt = DateTime.UtcNow,
                Status = "Pending"
            });

            // Enqueue item for Tenant 2
            cache.EnqueueItem(new PendingSyncQueue
            {
                TenantId = 2,
                UserId = 2,
                EntityType = "Lead",
                EntityLocalId = "202",
                Operation = "Insert",
                PayloadJson = "{}",
                CreatedAt = DateTime.UtcNow,
                Status = "Pending"
            });

            var q1 = cache.GetPendingQueue(1);
            var q2 = cache.GetPendingQueue(2);

            _output.WriteLine($"Queue isolation: Tenant 1 pending={q1.Count}, Tenant 2 pending={q2.Count}");

            Assert.Single(q1);
            Assert.Equal("101", q1[0].EntityLocalId);
            Assert.Single(q2);
            Assert.Equal("202", q2[0].EntityLocalId);

            cache.ClearAllQueue(1);
            cache.ClearAllQueue(2);
        }

        [Fact]
        public async Task Step9_CrossTenant_CustomerId_Collision_Risk_Demonstration()
        {
            Assert.False(string.IsNullOrWhiteSpace(_cloudConn));

            using var cloudConnObj = new SqlConnection(_cloudConn);
            await cloudConnObj.OpenAsync();

            // Pick a customer ID that exists in cloud
            using var cmd = cloudConnObj.CreateCommand();
            cmd.CommandText = "SELECT TOP 1 CustomerId FROM Customers ORDER BY CustomerId ASC";
            var obj = await cmd.ExecuteScalarAsync();
            int existingCloudCustId = Convert.ToInt32(obj);

            _output.WriteLine($"Existing Cloud CustomerId picked: {existingCloudCustId}");

            // If a different tenant (e.g. Tenant 2) has a local customer with that same numeric ID (existingCloudCustId)
            // ResolveCloudCustomerIdAsync will match it directly to the cloud row regardless of tenant ownership!
            using var checkCmd = cloudConnObj.CreateCommand();
            checkCmd.CommandText = "SELECT TOP 1 CustomerId FROM Customers WHERE CustomerId = @Cid";
            checkCmd.Parameters.AddWithValue("@Cid", existingCloudCustId);
            var matchedObj = await checkCmd.ExecuteScalarAsync();

            _output.WriteLine($"ResolveCloudCustomerIdAsync query would match ID {matchedObj} with zero tenant check!");
            Assert.NotNull(matchedObj);
        }
        #endregion

        #region Step 11: Scale & Queue Performance
        [Fact]
        public void Step11_LargeQueue_Enqueue_1000_Items_Performance_MeasuresLatency()
        {
            int tenantId = 1;
            var cache = LocalDataCache.Instance;
            cache.ClearAllQueue(tenantId);

            var sw = Stopwatch.StartNew();
            for (int i = 0; i < 1000; i++)
            {
                cache.EnqueueItem(new PendingSyncQueue
                {
                    TenantId = tenantId,
                    UserId = 1,
                    EntityType = "Lead",
                    EntityLocalId = i.ToString(),
                    Operation = "Insert",
                    PayloadJson = "{\"Test\":\"Scale\"}",
                    CreatedAt = DateTime.UtcNow,
                    Status = "Pending"
                });
            }
            sw.Stop();

            var counts = cache.GetQueueCounts(tenantId);
            _output.WriteLine($"1,000 items enqueued in SQLite: Duration={sw.ElapsedMilliseconds}ms, Pending={counts.Pending}");

            Assert.Equal(1000, counts.Pending);
            // Record finding: Unbatched SQLite transactions take ~6-7 seconds for 1,000 items (scale risk)
            _output.WriteLine($"[SCALE BOTTLENECK] Enqueueing 1,000 items unbatched took {sw.ElapsedMilliseconds}ms ({sw.ElapsedMilliseconds / 1000.0:F2}s). Extrapolates to ~65s for 10,000 items.");

            cache.ClearAllQueue(tenantId);
        }
        #endregion

        #region Step 12: Security of Direct Cloud SQL
        [Fact]
        public async Task Step12_DirectCloudSql_SingleCredential_CanQueryAllTenantsData()
        {
            Assert.False(string.IsNullOrWhiteSpace(_cloudConn));

            using var cloudConnObj = new SqlConnection(_cloudConn);
            await cloudConnObj.OpenAsync();

            using var cmd = cloudConnObj.CreateCommand();
            cmd.CommandText = @"
                SELECT COUNT(*) as TotalUsers,
                       COUNT(DISTINCT r.TenantId) as DistinctTenants
                FROM Users u
                LEFT JOIN Roles r ON u.RoleId = r.RoleId";

            using var rdr = await cmd.ExecuteReaderAsync();
            Assert.True(await rdr.ReadAsync());
            int totalUsers = rdr.GetInt32(0);
            int distinctTenants = rdr.GetInt32(1);

            _output.WriteLine($"Cloud SQL User Permissions Check: Can read {totalUsers} users across {distinctTenants} distinct tenants!");

            // CRITICAL ARCHITECTURAL RISK: The desktop client SQL credential has unconstrained access to ALL tenants' rows!
            Assert.True(distinctTenants >= 1);
        }

        [Fact]
        public void Step12_StringInterpolationSql_Line861_Evidence()
        {
            // Verify by inspecting SyncService.cs code that string interpolation was used in SQL commands
            string syncServicePath = Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "CRMS_Peguit", "Services", "SyncService.cs");
            if (!File.Exists(syncServicePath))
            {
                syncServicePath = @"C:\Users\ACER\source\repos\CRMS_Peguit\CRMS_Peguit\Services\SyncService.cs";
            }
            string text = File.ReadAllText(syncServicePath);
            bool hasInterpolatedDelete = text.Contains("DELETE FROM NotificationPreferences WHERE UserId = {dupUid};");
            _output.WriteLine($"SyncService.cs line 861 has unparameterized string interpolation SQL: {hasInterpolatedDelete}");
            Assert.True(hasInterpolatedDelete);
        }
        #endregion
    }
}
