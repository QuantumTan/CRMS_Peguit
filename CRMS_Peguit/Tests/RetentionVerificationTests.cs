using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CRMS_Peguit.domain.entities;
using CRMS_Peguit.infrastructure.data;
using CRMS_Peguit.winforms.Auth;
using CRMS_Peguit.winforms.Controllers;
using CRMS_Peguit.winforms.Models.Roles;
using CRMS_Peguit.winforms.Models.Services;
using CRMS_Peguit.winforms.Services;
using CRMS_Peguit.winforms.Views.Marketing;
using Microsoft.EntityFrameworkCore;

namespace CRMS_Peguit.winforms.Tests
{
    public static class RetentionVerificationTests
    {
        private static int _passedCount = 0;
        private static int _failedCount = 0;

        private static void AssertTrue(bool condition, string testName, string? details = null)
        {
            if (condition)
            {
                _passedCount++;
                Console.ForegroundColor = ConsoleColor.Green;
                Console.Write("  [PASS] ");
                Console.ResetColor();
                Console.WriteLine(testName);
                if (!string.IsNullOrWhiteSpace(details))
                {
                    Console.ForegroundColor = ConsoleColor.DarkGray;
                    Console.WriteLine($"         └─ {details}");
                    Console.ResetColor();
                }
            }
            else
            {
                _failedCount++;
                Console.ForegroundColor = ConsoleColor.Red;
                Console.Write("  [FAIL] ");
                Console.ResetColor();
                Console.WriteLine(testName);
                if (!string.IsNullOrWhiteSpace(details))
                {
                    Console.ForegroundColor = ConsoleColor.Yellow;
                    Console.WriteLine($"         └─ Reason: {details}");
                    Console.ResetColor();
                }
            }
        }

        public static async Task<bool> RunAllTestsAsync()
        {
            _passedCount = 0;
            _failedCount = 0;

            Console.WriteLine("================================================================================");
            Console.WriteLine(" NEXA CRM: CUSTOMER RETENTION & EMAIL CAMPAIGNS - VERIFICATION TEST SUITE");
            Console.WriteLine("================================================================================");

            // Ensure DB schema and default templates exist
            using (var initDb = LocalDb.CreateContext(1))
            {
                initDb.Database.EnsureCreated();
            }

            RunSegmentPriorityUnitTests();
            await RunRbacAndAccessControlTestsAsync();
            await RunApprovalAndSelfApprovalPreventionTestsAsync();
            await RunAntiFatigueCooldownTestsAsync();
            RunUiInstantiationTests();

            Console.WriteLine("================================================================================");
            Console.Write(" VERIFICATION SUMMARY: ");
            if (_failedCount == 0)
            {
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine($"ALL {_passedCount} TESTS PASSED WITH 0 ERRORS!");
            }
            else
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"{_failedCount} FAILED, {_passedCount} PASSED.");
            }
            Console.ResetColor();
            Console.WriteLine("================================================================================");

            return _failedCount == 0;
        }

        #region 1. Segment Calculation Priority Unit Tests

        private static void RunSegmentPriorityUnitTests()
        {
            Console.WriteLine("\n[SUITE 1] SEGMENT CALCULATION PRIORITY & REAL ESTATE INCENTIVES");
            var refDate = new DateTime(2026, 9, 26, 12, 0, 0, DateTimeKind.Utc);

            var dummyCustomer = new Customer
            {
                CustomerId = 100,
                FirstName = "John",
                LastName = "Doe"
            };

            // Test 1: Active Deal in Offer stage
            var dealsActiveOffer = new List<Deal>
            {
                new Deal { DealId = 1, CustomerId = 100, Stage = "Offer", CreatedAt = refDate.AddDays(-10) },
                new Deal { DealId = 2, CustomerId = 100, Stage = "Closed", ContractSignedDate = refDate.AddDays(-100) }
            };
            string seg1 = RetentionCalculationService.EvaluateSegment(dummyCustomer, dealsActiveOffer, new List<Activity>(), refDate);
            AssertTrue(seg1 == RetentionCalculationService.SegmentActiveDealExcluded,
                "Priority 1: Active Deal (Offer) Excludes customer from retention campaigns",
                $"Calculated: '{seg1}', Expected: '{RetentionCalculationService.SegmentActiveDealExcluded}'");

            // Test 1b: Active Deal in Reservation stage
            var dealsActiveRes = new List<Deal>
            {
                new Deal { DealId = 1, CustomerId = 100, Stage = "Reservation", CreatedAt = refDate.AddDays(-5) }
            };
            string seg1b = RetentionCalculationService.EvaluateSegment(dummyCustomer, dealsActiveRes, new List<Activity>(), refDate);
            AssertTrue(seg1b == RetentionCalculationService.SegmentActiveDealExcluded,
                "Priority 1b: Active Deal (Reservation) Excludes customer",
                $"Calculated: '{seg1b}'");

            // Test 1c: Active Deal in Contract Signed stage
            var dealsActiveContract = new List<Deal>
            {
                new Deal { DealId = 1, CustomerId = 100, Stage = "Contract Signed", CreatedAt = refDate.AddDays(-2) }
            };
            string seg1c = RetentionCalculationService.EvaluateSegment(dummyCustomer, dealsActiveContract, new List<Activity>(), refDate);
            AssertTrue(seg1c == RetentionCalculationService.SegmentActiveDealExcluded,
                "Priority 1c: Active Deal (Contract Signed) Excludes customer",
                $"Calculated: '{seg1c}'");

            // Test 2: New Client (1 closed deal within 6 months, no prior deals)
            var dealsNew = new List<Deal>
            {
                new Deal { DealId = 1, CustomerId = 100, Stage = "Closed", ContractSignedDate = refDate.AddDays(-30) }
            };
            string seg2 = RetentionCalculationService.EvaluateSegment(dummyCustomer, dealsNew, new List<Activity>(), refDate);
            AssertTrue(seg2 == RetentionCalculationService.SegmentNewClient,
                "Priority 2: 1 Closed Deal within 183 days -> New Client (First-time transaction)",
                $"Calculated: '{seg2}', Expected: '{RetentionCalculationService.SegmentNewClient}'");

            // Test 3: Recent Client (2+ closed deals, most recent within 6 months)
            var dealsRecent = new List<Deal>
            {
                new Deal { DealId = 1, CustomerId = 100, Stage = "Closed", ContractSignedDate = refDate.AddDays(-400) },
                new Deal { DealId = 2, CustomerId = 100, Stage = "Closed", ContractSignedDate = refDate.AddDays(-45) }
            };
            string seg3 = RetentionCalculationService.EvaluateSegment(dummyCustomer, dealsRecent, new List<Activity>(), refDate);
            AssertTrue(seg3 == RetentionCalculationService.SegmentRecentClient,
                "Priority 2b: 2+ Closed Deals with latest within 183 days -> Recent Client (Post-move check-in)",
                $"Calculated: '{seg3}', Expected: '{RetentionCalculationService.SegmentRecentClient}'");

            // Test 4: Repeat Client (2+ closed deals, most recent between 6 and 24 months / 183 to 730 days)
            var dealsRepeat = new List<Deal>
            {
                new Deal { DealId = 1, CustomerId = 100, Stage = "Closed", ContractSignedDate = refDate.AddDays(-600) },
                new Deal { DealId = 2, CustomerId = 100, Stage = "Closed", ContractSignedDate = refDate.AddDays(-300) }
            };
            string seg4 = RetentionCalculationService.EvaluateSegment(dummyCustomer, dealsRepeat, new List<Activity>(), refDate);
            AssertTrue(seg4 == RetentionCalculationService.SegmentRepeatClient,
                "Priority 3: 2+ Closed Deals with latest at 300 days -> Repeat Client (VIP concession)",
                $"Calculated: '{seg4}', Expected: '{RetentionCalculationService.SegmentRepeatClient}'");

            // Test 5: At Risk (1 closed deal at 300 days ago, 183-548 days)
            var dealsAtRisk = new List<Deal>
            {
                new Deal { DealId = 1, CustomerId = 100, Stage = "Closed", ContractSignedDate = refDate.AddDays(-300) }
            };
            string seg5 = RetentionCalculationService.EvaluateSegment(dummyCustomer, dealsAtRisk, new List<Activity>(), refDate);
            AssertTrue(seg5 == RetentionCalculationService.SegmentAtRisk,
                "Priority 4: 1 Closed Deal at 300 days ago -> At Risk (Neighborhood CMA)",
                $"Calculated: '{seg5}', Expected: '{RetentionCalculationService.SegmentAtRisk}'");

            // Test 5b: At Risk via Activity (0 closed deals, activity at 250 days ago)
            var actsAtRisk = new List<Activity>
            {
                new Activity { ActivityId = 1, RelatedCustomerId = 100, ActivityDate = refDate.AddDays(-250) }
            };
            string seg5b = RetentionCalculationService.EvaluateSegment(dummyCustomer, new List<Deal>(), actsAtRisk, refDate);
            AssertTrue(seg5b == RetentionCalculationService.SegmentAtRisk,
                "Priority 4b: 0 Closed Deals, Activity at 250 days ago -> At Risk",
                $"Calculated: '{seg5b}', Expected: '{RetentionCalculationService.SegmentAtRisk}'");

            // Test 6: Inactive (1 closed deal at 600 days ago, >548 days)
            var dealsInactive = new List<Deal>
            {
                new Deal { DealId = 1, CustomerId = 100, Stage = "Closed", ContractSignedDate = refDate.AddDays(-600) }
            };
            string seg6 = RetentionCalculationService.EvaluateSegment(dummyCustomer, dealsInactive, new List<Activity>(), refDate);
            AssertTrue(seg6 == RetentionCalculationService.SegmentInactive,
                "Priority 5: 1 Closed Deal at 600 days ago (> 18 mo) -> Inactive (Portfolio valuation)",
                $"Calculated: '{seg6}', Expected: '{RetentionCalculationService.SegmentInactive}'");

            // Test 7: Prospective Client (0 closed deals, 0 activities)
            string seg7 = RetentionCalculationService.EvaluateSegment(dummyCustomer, new List<Deal>(), new List<Activity>(), refDate);
            AssertTrue(seg7 == RetentionCalculationService.SegmentProspective,
                "Priority 6: 0 Closed Deals, 0 Activities -> Prospective Client (First-time playbook)",
                $"Calculated: '{seg7}', Expected: '{RetentionCalculationService.SegmentProspective}'");

            // Test 8: Real estate service incentives mapping
            string incNew = RetentionCalculationService.GetRecommendedIncentive(RetentionCalculationService.SegmentNewClient);
            string incRecent = RetentionCalculationService.GetRecommendedIncentive(RetentionCalculationService.SegmentRecentClient);
            string incRepeat = RetentionCalculationService.GetRecommendedIncentive(RetentionCalculationService.SegmentRepeatClient);
            string incAtRisk = RetentionCalculationService.GetRecommendedIncentive(RetentionCalculationService.SegmentAtRisk);
            string incInactive = RetentionCalculationService.GetRecommendedIncentive(RetentionCalculationService.SegmentInactive);
            string incProspective = RetentionCalculationService.GetRecommendedIncentive(RetentionCalculationService.SegmentProspective);

            AssertTrue(incNew.Contains("Vendor") && incRepeat.Contains("Commission") && incAtRisk.Contains("CMA") && incInactive.Contains("Valuation"),
                "Recommended incentives use professional real estate service concessions, not retail discounts",
                $"Repeat: {incRepeat} | At Risk: {incAtRisk}");
        }

        #endregion

        #region 2. RBAC & Multi-Tenant Boundary Tests

        private static async Task RunRbacAndAccessControlTestsAsync()
        {
            Console.WriteLine("\n[SUITE 2] ROLE-BASED ACCESS CONTROL & TENANT ISOLATION");

            // Test 9: Super Admin has zero tenant customer retention access
            CurrentSession.Start(1, 0, "Platform Super Admin", "superadmin@crms.com", "SuperAdmin", null, false);
            bool superAdminBlocked = false;
            try
            {
                using var ctrl = new RetentionController();
                await ctrl.GetSummaryAsync();
            }
            catch (UnauthorizedAccessException ex)
            {
                superAdminBlocked = ex.Message.Contains("Super Admin has zero access");
            }
            AssertTrue(superAdminBlocked,
                "R26: Super Admin is strictly blocked from accessing tenant retention data (Zero Access Policy)");

            // Test 10: Agent ownership filtering
            CurrentSession.Start(5, 1, "Agent User", "agent@test.com", "Agent", null, false);
            AssertTrue(RbacService.IsAgent && !RbacService.HasFullOversight,
                "Agent session correctly identified with scoped oversight");

            // Test 10b: Admin session full oversight
            CurrentSession.Start(1, 1, "Admin User", "admin@test.com", "Admin", null, false);
            AssertTrue(RbacService.IsAdmin && RbacService.HasFullOversight,
                "Admin session correctly identified with tenant-wide oversight");
        }

        #endregion

        #region 3. Proposal Approval & Self-Approval Prevention Tests

        private static async Task RunApprovalAndSelfApprovalPreventionTestsAsync()
        {
            Console.WriteLine("\n[SUITE 3] PROPOSAL APPROVAL WORKFLOW & SELF-APPROVAL PREVENTION");

            using var db = LocalDb.CreateContext(1);

            // Find or create test customer
            var customer = await db.Customers.FirstOrDefaultAsync(c => !c.IsDeleted);
            if (customer == null)
            {
                customer = new Customer
                {
                    FirstName = "RetentionTest",
                    LastName = "Customer",
                    Email = "retentiontest@client.com",
                    CreatedAt = DateTime.UtcNow
                };
                db.Customers.Add(customer);
                await db.SaveChangesAsync();
            }

            // Dynamically query real seeded users for Tenant 1
            var agentUser = await db.Users.Include(u => u.Role).FirstOrDefaultAsync(u => u.Role != null && (u.Role.RoleName.Contains("Staff") || u.Role.RoleName.Contains("Agent")));
            var managerUser = await db.Users.Include(u => u.Role).FirstOrDefaultAsync(u => u.Role != null && u.Role.RoleName.Contains("Manager"));
            var adminUser = await db.Users.Include(u => u.Role).FirstOrDefaultAsync(u => u.Role != null && u.Role.RoleName.Contains("Admin"));

            if (agentUser == null || managerUser == null || adminUser == null)
            {
                throw new InvalidOperationException("Seeded users for Agent, Manager, and Admin must exist in Tenant 1.");
            }

            var peerAgent = await db.Users.Include(u => u.Role)
                .FirstOrDefaultAsync(u => u.UserId != agentUser.UserId && u.Role != null && (u.Role.RoleName.Contains("Staff") || u.Role.RoleName.Contains("Agent")));
            int peerAgentId = peerAgent?.UserId ?? 998;

            var peerManager = await db.Users.Include(u => u.Role)
                .FirstOrDefaultAsync(u => u.UserId != managerUser.UserId && u.Role != null && u.Role.RoleName.Contains("Manager"));
            int peerManagerId = peerManager?.UserId ?? 999;

            // Ensure test customer is assigned to the agent so ownership rules allow proposal creation
            customer.AssignedAgentId = agentUser.UserId;
            await db.SaveChangesAsync();

            // Scenario 1: Agent submits a retention request
            CurrentSession.Start(agentUser.UserId, 1, agentUser.FullName ?? "Agent Submitter", agentUser.Email ?? "agent@test.com", "Agent", null, false);
            RetentionRequest agentRequest;
            using (var ctrl = new RetentionController())
            {
                agentRequest = await ctrl.SubmitRetentionRequestAsync(
                    customer.CustomerId,
                    RetentionCalculationService.SegmentAtRisk,
                    "CMA Incentive Outreach",
                    "Complimentary Home Equity & Neighborhood CMA Report",
                    "Property Equity Update",
                    "Client's property has experienced significant neighborhood appreciation.");
            }

            AssertTrue(agentRequest.RequestId > 0 && agentRequest.Status == "Pending",
                "Agent successfully submits a retention proposal",
                $"RequestId: {agentRequest.RequestId}, Status: {agentRequest.Status}");

            // Scenario 2: Agent attempts to self-approve their own request
            bool selfApprovalBlocked = false;
            using (var ctrl = new RetentionController())
            {
                var rows = await ctrl.GetRetentionRequestsAsync();
                var agentRow = rows.FirstOrDefault(r => r.RequestId == agentRequest.RequestId);
                bool canApproveFlag = agentRow?.CanCurrentUserApprove ?? true;

                try
                {
                    await ctrl.ApproveRetentionRequestAsync(agentRequest.RequestId, "Self-approval test");
                }
                catch (InvalidOperationException ex)
                {
                    selfApprovalBlocked = ex.Message.Contains("Self-Approval Prevention") && !canApproveFlag;
                }
            }
            AssertTrue(selfApprovalBlocked,
                "Self-Approval Prevention: Creator is strictly forbidden from approving their own request");

            // Scenario 3: Agent attempts to self-reject their own request
            bool selfRejectBlocked = false;
            using (var ctrl = new RetentionController())
            {
                try
                {
                    await ctrl.RejectRetentionRequestAsync(agentRequest.RequestId, "Invalid offer", "Self reject");
                }
                catch (InvalidOperationException ex)
                {
                    selfRejectBlocked = ex.Message.Contains("Self-Approval Prevention");
                }
            }
            AssertTrue(selfRejectBlocked,
                "Self-Approval Prevention: Creator is forbidden from reviewing/rejecting their own request");

            // Scenario 4: Another Agent attempts to approve
            CurrentSession.Start(peerAgentId, 1, peerAgent?.FullName ?? "Peer Agent", peerAgent?.Email ?? "peeragent@test.com", "Agent", null, false);
            bool peerAgentBlocked = false;
            using (var ctrl = new RetentionController())
            {
                var rows = await ctrl.GetRetentionRequestsAsync();
                var row = rows.FirstOrDefault(r => r.RequestId == agentRequest.RequestId);
                bool canApproveFlag = row?.CanCurrentUserApprove ?? false;

                try
                {
                    await ctrl.ApproveRetentionRequestAsync(agentRequest.RequestId, "Peer approve test");
                }
                catch (UnauthorizedAccessException ex)
                {
                    peerAgentBlocked = ex.Message.Contains("Agents are not authorized") && !canApproveFlag;
                }
            }
            AssertTrue(peerAgentBlocked,
                "Agents are strictly forbidden from approving retention requests submitted by any user");

            // Scenario 5: Manager submits their own request
            CurrentSession.Start(managerUser.UserId, 1, managerUser.FullName ?? "Manager Submitter", managerUser.Email ?? "manager@test.com", "Manager", null, false);
            RetentionRequest managerRequest;
            using (var ctrl = new RetentionController())
            {
                managerRequest = await ctrl.SubmitRetentionRequestAsync(
                    customer.CustomerId,
                    RetentionCalculationService.SegmentRepeatClient,
                    "Listing Commission Concession",
                    "Reduced Commission on Next Listing (0.75% Concession)",
                    "VIP Client Re-engagement",
                    "High-value repeat client considering selling vacation property.");
            }

            // Scenario 6: Peer Manager attempts to approve Manager-submitted request (Requires Admin)
            CurrentSession.Start(peerManagerId, 1, peerManager?.FullName ?? "Peer Manager", peerManager?.Email ?? "peermanager@test.com", "Manager", null, false);
            bool peerManagerBlocked = false;
            using (var ctrl = new RetentionController())
            {
                var rows = await ctrl.GetRetentionRequestsAsync();
                var row = rows.FirstOrDefault(r => r.RequestId == managerRequest.RequestId);
                bool canApproveFlag = row?.CanCurrentUserApprove ?? true;

                try
                {
                    await ctrl.ApproveRetentionRequestAsync(managerRequest.RequestId, "Peer manager approve");
                }
                catch (UnauthorizedAccessException ex)
                {
                    peerManagerBlocked = ex.Message.Contains("Manager-submitted requests require Admin approval") && !canApproveFlag;
                }
            }
            AssertTrue(peerManagerBlocked,
                "Manager submissions cannot be approved by other managers (Admin executive approval required)");

            // Scenario 7: Manager approves Agent request
            CurrentSession.Start(peerManagerId, 1, peerManager?.FullName ?? "Peer Manager", peerManager?.Email ?? "peermanager@test.com", "Manager", null, false);
            bool managerApprovedAgent = false;
            using (var ctrl = new RetentionController())
            {
                var rows = await ctrl.GetRetentionRequestsAsync();
                var row = rows.FirstOrDefault(r => r.RequestId == agentRequest.RequestId);
                if (row?.CanCurrentUserApprove == true)
                {
                    await ctrl.ApproveRetentionRequestAsync(agentRequest.RequestId, "Approved by sales manager.");
                    managerApprovedAgent = true;
                }
            }
            AssertTrue(managerApprovedAgent,
                "Manager can review and approve Agent retention requests");

            // Scenario 8: Admin approves Manager request
            CurrentSession.Start(adminUser.UserId, 1, adminUser.FullName ?? "Executive Admin", adminUser.Email ?? "admin@test.com", "Admin", null, false);
            bool adminApprovedManager = false;
            using (var ctrl = new RetentionController())
            {
                var rows = await ctrl.GetRetentionRequestsAsync();
                var row = rows.FirstOrDefault(r => r.RequestId == managerRequest.RequestId);
                if (row?.CanCurrentUserApprove == true)
                {
                    await ctrl.ApproveRetentionRequestAsync(managerRequest.RequestId, "Executive Admin approval granted.");
                    adminApprovedManager = true;
                }
            }
            AssertTrue(adminApprovedManager,
                "Admin can review and approve Manager retention requests");

            // Scenario 9: Idempotency check - Approving an already reviewed request throws
            bool duplicateApprovalBlocked = false;
            using (var ctrl = new RetentionController())
            {
                try
                {
                    await ctrl.ApproveRetentionRequestAsync(agentRequest.RequestId, "Duplicate approval test");
                }
                catch (InvalidOperationException ex)
                {
                    duplicateApprovalBlocked = ex.Message.Contains("already been reviewed");
                }
            }
            AssertTrue(duplicateApprovalBlocked,
                "Idempotency: Re-approving an already reviewed request is strictly blocked");

            // Scenario 10: Verify campaign queue entry created exactly once
            using (var ctrl = new RetentionController())
            {
                var queue = await ctrl.GetCampaignQueueAsync();
                var queuedForAgentReq = queue.Where(q => q.RetentionRequestId == agentRequest.RequestId).ToList();
                AssertTrue(queuedForAgentReq.Count == 1,
                    "Idempotency: Exactly one campaign queue entry is auto-generated upon approval",
                    $"Queue count for Request #{agentRequest.RequestId}: {queuedForAgentReq.Count}");
            }
        }

        #endregion

        #region 4. Anti-Fatigue Cooldown & Override Validation Tests

        private static async Task RunAntiFatigueCooldownTestsAsync()
        {
            Console.WriteLine("\n[SUITE 4] 30-DAY ANTI-FATIGUE COOLDOWN POLICY & OVERRIDE AUDIT");

            using var db = LocalDb.CreateContext(1);
            var adminUser = await db.Users.Include(u => u.Role).FirstOrDefaultAsync(u => u.Role != null && u.Role.RoleName.Contains("Admin"))
                ?? await db.Users.FirstAsync();

            CurrentSession.Start(adminUser.UserId, 1, adminUser.FullName ?? "Admin User", adminUser.Email ?? "admin@test.com", "Admin", null, false);

            // Create a test customer with last email sent 10 days ago (active cooldown: 20 days remaining)
            var customer = new Customer
            {
                FirstName = "Cooldown",
                LastName = "Tester",
                Email = "cooldown@test.com",
                CreatedByUserId = adminUser.UserId,
                AssignedAgentId = adminUser.UserId,
                LastRetentionEmailSentAt = DateTime.UtcNow.AddDays(-10),
                CurrentRetentionSegment = RetentionCalculationService.SegmentAtRisk,
                CreatedAt = DateTime.UtcNow
            };
            db.Customers.Add(customer);
            await db.SaveChangesAsync();

            var logEntry = new RetentionEmailLog
            {
                TenantId = 1,
                CustomerId = customer.CustomerId,
                RecipientEmail = "cooldown@test.com",
                RecipientName = "Cooldown Tester",
                Segment = RetentionCalculationService.SegmentAtRisk,
                Subject = "Neighborhood Real Estate Report",
                Body = "Here is your updated home valuation.",
                IncentiveOffered = "Complimentary Home Equity & Neighborhood CMA Report",
                Status = "Queued",
                GenerationSource = "ManualSend",
                CreatedAt = DateTime.UtcNow
            };
            db.RetentionEmailLogs.Add(logEntry);
            await db.SaveChangesAsync();

            // Test 11: Attempt dispatch during 30-day cooldown without override
            bool cooldownEnforced = false;
            using (var ctrl = new RetentionController())
            {
                try
                {
                    await ctrl.DispatchQueuedEmailAsync(
                        logEntry.EmailLogId,
                        logEntry.Subject,
                        logEntry.Body,
                        overrideCooldown: false);
                }
                catch (InvalidOperationException ex)
                {
                    cooldownEnforced = ex.Message.Contains("anti-fatigue cooldown") && ex.Message.Contains("days remaining");
                }
            }
            AssertTrue(cooldownEnforced,
                "Anti-Fatigue Cooldown: Dispatch during 30-day window is blocked without manual override");

            // Test 12: Override attempted without documented rationale
            bool missingRationaleBlocked = false;
            using (var ctrl = new RetentionController())
            {
                try
                {
                    await ctrl.DispatchQueuedEmailAsync(
                        logEntry.EmailLogId,
                        logEntry.Subject,
                        logEntry.Body,
                        overrideCooldown: true,
                        overrideReason: "");
                }
                catch (ArgumentException ex)
                {
                    missingRationaleBlocked = ex.Message.Contains("documented business rationale is mandatory");
                }
            }
            AssertTrue(missingRationaleBlocked,
                "Cooldown Override: Blank override rationale is rejected as mandatory validation");

            // Test 13: Valid override with rationale logged to RetentionAuditLog
            using (var ctrl = new RetentionController())
            {
                try
                {
                    await ctrl.DispatchQueuedEmailAsync(
                        logEntry.EmailLogId,
                        logEntry.Subject,
                        logEntry.Body,
                        overrideCooldown: true,
                        overrideReason: "Client explicitly requested urgent CMA report prior to scheduled appraisal.");
                }
                catch (InvalidOperationException)
                {
                    // Expected if SMTP credentials are mock/unconfigured; the override audit log is committed BEFORE email send attempt
                }
            }

            var auditLog = await db.RetentionAuditLogs
                .OrderByDescending(a => a.CreatedAt)
                .FirstOrDefaultAsync(a => a.TargetCustomerId == customer.CustomerId && a.ActionType == "CooldownOverride");

            AssertTrue(auditLog != null && auditLog.Detail.Contains("Client explicitly requested urgent CMA"),
                "Cooldown Override: Valid override with documented justification is immutably logged to RetentionAuditLogs",
                $"Audit Log: {auditLog?.Detail}");

            // Cleanup test customer
            customer.IsDeleted = true;
            await db.SaveChangesAsync();
        }

        #endregion

        #region 5. WinForms Presentation Layer Instantiation Tests

        private static void RunUiInstantiationTests()
        {
            Console.WriteLine("\n[SUITE 5] WINFORMS PRESENTATION LAYER INSTANTIATION & SECURITY GATING");

            // Test 14: ClientRetentionView instantiates cleanly under Admin session
            CurrentSession.Start(1, 1, "Admin User", "admin@test.com", "Admin", null, false);
            bool viewCreated = false;
            try
            {
                using var view = new ClientRetentionView();
                viewCreated = view != null && view.Controls.Count > 0;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"      UI Error: {ex.Message}");
            }
            AssertTrue(viewCreated,
                "ClientRetentionView instantiates and initializes all 5 tabs and KPI cards cleanly");

            // Test 15: RetentionRequestDialog instantiates in create mode
            bool requestDialogCreated = false;
            try
            {
                using var ctrl = new RetentionController();
                using var dlg = new RetentionRequestDialog(ctrl);
                requestDialogCreated = dlg != null && dlg.Controls.Count > 0;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"      UI Error: {ex.Message}");
            }
            AssertTrue(requestDialogCreated,
                "RetentionRequestDialog instantiates cleanly in proposal submission mode");

            // Test 16: CooldownOverrideDialog instantiates cleanly
            bool cooldownDialogCreated = false;
            try
            {
                using var dlg = new CooldownOverrideDialog("Jane Doe", DateTime.UtcNow.AddDays(-16), 14);
                cooldownDialogCreated = dlg != null && dlg.Controls.Count > 0;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"      UI Error: {ex.Message}");
            }
            AssertTrue(cooldownDialogCreated,
                "CooldownOverrideDialog instantiates cleanly with remaining days indicator");
        }

        #endregion
    }
}
