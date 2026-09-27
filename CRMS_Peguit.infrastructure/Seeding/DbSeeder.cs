using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CRMS_Peguit.domain.entities;
using CRMS_Peguit.infrastructure.data;
using CRMS_Peguit.infrastructure.Security;
using Microsoft.EntityFrameworkCore;

namespace CRMS_Peguit.infrastructure.Seeding
{
    public static class DbSeeder
    {
        public static async Task SeedTestUsersAsync(RealEstateDbContext db, int tenantId = 1)
        {
            await EnsureBranchesAsync(db, tenantId);
            await EnsureRolesAndBaseUsersAsync(db, tenantId);
            await EnsureAdditionalTeamMembersAsync(db, tenantId);
            await SeedSampleDataAsync(db, tenantId);
        }

        public static async Task EnsureBranchesAsync(RealEstateDbContext db, int tenantId = 1)
        {
            if (tenantId == 3 && !await db.Branches.AnyAsync())
            {
                var hq = new Branch { TenantId = 3, BranchCode = "BR-MNL", BranchName = "Metro Manila HQ", Address = "Ayala Ave, Makati City", Phone = "02-8888-0001", IsActive = true };
                var cebu = new Branch { TenantId = 3, BranchCode = "BR-CEB", BranchName = "Cebu Central Branch", Address = "IT Park, Cebu City", Phone = "032-411-0002", IsActive = true };
                var davao = new Branch { TenantId = 3, BranchCode = "BR-DVO", BranchName = "Davao Regional Branch", Address = "Bajada, Davao City", Phone = "082-222-0003", IsActive = true };
                db.Branches.AddRange(hq, cebu, davao);
                await db.SaveChangesAsync();
            }
        }

        public static async Task EnsureRolesAndBaseUsersAsync(RealEstateDbContext db, int tenantId = 1)
        {
            var adminRole = await db.Roles.FirstOrDefaultAsync(r => r.TenantId == tenantId && r.RoleName == "Admin");
            var managerRole = await db.Roles.FirstOrDefaultAsync(r => r.TenantId == tenantId && r.RoleName == "Manager");
            var agentRole = await db.Roles.FirstOrDefaultAsync(r => r.TenantId == tenantId && r.RoleName == "Agent");

            if (adminRole == null)
            {
                adminRole = new Role { TenantId = tenantId, RoleName = "Admin" };
                db.Roles.Add(adminRole);
            }
            if (managerRole == null)
            {
                managerRole = new Role { TenantId = tenantId, RoleName = "Manager" };
                db.Roles.Add(managerRole);
            }
            if (agentRole == null)
            {
                agentRole = new Role { TenantId = tenantId, RoleName = "Agent" };
                db.Roles.Add(agentRole);
            }
            await db.SaveChangesAsync();

            var branches = await db.Branches.OrderBy(b => b.BranchId).ToListAsync();
            int? hqBranchId = branches.FirstOrDefault(b => b.BranchCode == "BR-MNL")?.BranchId ?? branches.FirstOrDefault()?.BranchId;

            var existingEmails = await db.Users
                .Where(u => u.Email != null)
                .Select(u => u.Email!.ToLower())
                .ToListAsync();

            List<(string FirstName, string LastName, string Email, string Phone, int RoleId, string Password, int? BranchId)> baseUsersToEnsure;

            if (tenantId == 2)
            {
                baseUsersToEnsure = new List<(string FirstName, string LastName, string Email, string Phone, int RoleId, string Password, int? BranchId)>
                {
                    ("Tenant B", "Admin", "admin.b@test.com", "09180000001", adminRole.RoleId, "Admin123!", null),
                    ("Tenant B", "Admin", "tenantb_admin@test.com", "09180000099", adminRole.RoleId, "Admin123!", null),
                    ("Tenant B", "Manager", "manager.b@test.com", "09180000002", managerRole.RoleId, "Manager123!", null),
                    ("Tenant B", "Manager", "tenantb_manager@test.com", "09180000098", managerRole.RoleId, "Manager123!", null),
                    ("Tenant B", "Agent", "agent.b@test.com", "09180000003", agentRole.RoleId, "Agent123!", null),
                    ("Tenant B", "Agent", "tenantb_agent@test.com", "09180000097", agentRole.RoleId, "Agent123!", null)
                };
            }
            else if (tenantId == 3)
            {
                baseUsersToEnsure = new List<(string FirstName, string LastName, string Email, string Phone, int RoleId, string Password, int? BranchId)>
                {
                    ("Tenant C", "Admin", "admin.c@test.com", "09190000001", adminRole.RoleId, "Admin123!", hqBranchId),
                    ("Tenant C", "Admin", "tenantc_admin@test.com", "09190000099", adminRole.RoleId, "Admin123!", hqBranchId),
                    ("Tenant C", "Manager", "manager.c@test.com", "09190000002", managerRole.RoleId, "Manager123!", hqBranchId),
                    ("Tenant C", "Manager", "tenantc_manager@test.com", "09190000098", managerRole.RoleId, "Manager123!", hqBranchId),
                    ("Tenant C", "Agent", "agent.c@test.com", "09190000003", agentRole.RoleId, "Agent123!", hqBranchId),
                    ("Tenant C", "Agent", "tenantc_agent@test.com", "09190000097", agentRole.RoleId, "Agent123!", hqBranchId)
                };
            }
            else
            {
                baseUsersToEnsure = new List<(string FirstName, string LastName, string Email, string Phone, int RoleId, string Password, int? BranchId)>
                {
                    ("Tenant A", "Admin", "admin.a@test.com", "09170000001", adminRole.RoleId, "Admin123!", null),
                    ("Tenant A", "Admin", "tenanta_admin@test.com", "09170000099", adminRole.RoleId, "Admin123!", null),
                    ("System", "Admin", "admin@test.com", "09170000000", adminRole.RoleId, "Admin123!", null),
                    ("Tenant A", "Manager", "manager.a@test.com", "09170000002", managerRole.RoleId, "Manager123!", null),
                    ("Tenant A", "Manager", "tenanta_manager@test.com", "09170000098", managerRole.RoleId, "Manager123!", null),
                    ("Test", "Manager", "manager@test.com", "09170000010", managerRole.RoleId, "Manager123!", null),
                    ("Tenant A", "Agent", "agent.a@test.com", "09170000003", agentRole.RoleId, "Agent123!", null),
                    ("Tenant A", "Agent", "tenanta_agent@test.com", "09170000097", agentRole.RoleId, "Agent123!", null),
                    ("Test", "Agent", "agent@test.com", "09170000020", agentRole.RoleId, "Agent123!", null)
                };
            }

            var toAdd = new List<User>();
            foreach (var bu in baseUsersToEnsure)
            {
                if (!existingEmails.Contains(bu.Email.ToLower()))
                {
                    toAdd.Add(new User
                    {
                        FirstName = bu.FirstName,
                        LastName = bu.LastName,
                        Email = bu.Email,
                        Phone = bu.Phone,
                        PasswordHash = PasswordHasher.Hash(bu.Password),
                        RoleId = bu.RoleId,
                        BranchId = bu.BranchId,
                        Status = "Active",
                        CreatedAt = DateTime.UtcNow.AddMonths(-18)
                    });
                    existingEmails.Add(bu.Email.ToLower());
                }
            }

            if (toAdd.Count > 0)
            {
                db.Users.AddRange(toAdd);
                await db.SaveChangesAsync();
            }

            if (tenantId == 3 && hqBranchId != null)
            {
                var usersWithoutBranch = await db.Users.Where(u => u.BranchId == null).ToListAsync();
                if (usersWithoutBranch.Count > 0)
                {
                    foreach (var u in usersWithoutBranch)
                    {
                        u.BranchId = hqBranchId;
                    }
                    await db.SaveChangesAsync();
                }
            }
        }

        public static async Task EnsureAdditionalTeamMembersAsync(RealEstateDbContext db, int tenantId = 1)
        {
            var agentRole = await db.Roles.FirstOrDefaultAsync(r => r.TenantId == tenantId && r.RoleName == "Agent");
            var managerRole = await db.Roles.FirstOrDefaultAsync(r => r.TenantId == tenantId && r.RoleName == "Manager");

            if (agentRole == null || managerRole == null) return;

            var existingEmails = await db.Users
                .Where(u => u.Email != null)
                .Select(u => u.Email!.ToLower())
                .ToListAsync();

            var branches = await db.Branches.OrderBy(b => b.BranchId).ToListAsync();
            int? hqBranchId = branches.FirstOrDefault(b => b.BranchCode == "BR-MNL")?.BranchId ?? branches.FirstOrDefault()?.BranchId;
            int? cebuBranchId = branches.FirstOrDefault(b => b.BranchCode == "BR-CEB")?.BranchId ?? branches.Skip(1).FirstOrDefault()?.BranchId;
            int? davaoBranchId = branches.FirstOrDefault(b => b.BranchCode == "BR-DVO")?.BranchId ?? branches.Skip(2).FirstOrDefault()?.BranchId;

            (string FirstName, string LastName, string Email, string Phone, int RoleId, string Password, int? BranchId)[] teamMembers;

            if (tenantId == 2)
            {
                teamMembers = new (string FirstName, string LastName, string Email, string Phone, int RoleId, string Password, int? BranchId)[]
                {
                    ("Valerie", "Cross", "valerie.cross@test.com", "09281122334", managerRole.RoleId, "Manager123!", null),
                    ("Elena", "Rostova", "elena.rostova@test.com", "09174455667", agentRole.RoleId, "Agent123!", null),
                    ("Marcus", "Vance", "marcus.vance@test.com", "09185566778", agentRole.RoleId, "Agent123!", null),
                    ("Chloe", "Bennett", "chloe.bennett@test.com", "09206677889", agentRole.RoleId, "Agent123!", null),
                    ("Nathan", "Drake", "nathan.drake@test.com", "09227788990", agentRole.RoleId, "Agent123!", null)
                };
            }
            else if (tenantId == 3)
            {
                teamMembers = new (string FirstName, string LastName, string Email, string Phone, int RoleId, string Password, int? BranchId)[]
                {
                    // Manila HQ (Branch 1)
                    ("Gabriel", "Santos", "gabriel.santos@test.com", "09171234567", agentRole.RoleId, "Agent123!", hqBranchId),

                    // Cebu Central (Branch 2)
                    ("Carlos", "Mendoza", "carlos.mendoza@test.com", "09321122334", managerRole.RoleId, "Manager123!", cebuBranchId),
                    ("Althea", "Garcia", "althea.garcia@test.com", "09322345678", agentRole.RoleId, "Agent123!", cebuBranchId),
                    ("Mateo", "Lim", "mateo.lim@test.com", "09323456789", agentRole.RoleId, "Agent123!", cebuBranchId),

                    // Davao Regional (Branch 3)
                    ("Beatrice", "Ong", "beatrice.ong@test.com", "09452233445", managerRole.RoleId, "Manager123!", davaoBranchId),
                    ("Patricia", "Alvarez", "patricia.alvarez@test.com", "09453456780", agentRole.RoleId, "Agent123!", davaoBranchId),
                    ("Dominic", "Suarez", "dominic.suarez@test.com", "09454567891", agentRole.RoleId, "Agent123!", davaoBranchId)
                };
            }
            else
            {
                teamMembers = new (string FirstName, string LastName, string Email, string Phone, int RoleId, string Password, int? BranchId)[]
                {
                    ("Sarah", "Jenkins", "sarah.jenkins@test.com", "09173344551", agentRole.RoleId, "Agent123!", null),
                    ("Michael", "Chang", "michael.chang@test.com", "09184455662", agentRole.RoleId, "Agent123!", null),
                    ("Jessica", "Torres", "jessica.torres@test.com", "09205566773", agentRole.RoleId, "Agent123!", null),
                    ("David", "Reyes", "david.reyes@test.com", "09226677884", agentRole.RoleId, "Agent123!", null),
                    ("Amanda", "Lim", "amanda.lim@test.com", "09157788995", agentRole.RoleId, "Agent123!", null),
                    ("Robert", "Tan", "robert.tan@test.com", "09278899006", managerRole.RoleId, "Manager123!", null)
                };
            }

            var toAdd = new List<User>();
            foreach (var member in teamMembers)
            {
                if (!existingEmails.Contains(member.Email.ToLower()))
                {
                    toAdd.Add(new User
                    {
                        FirstName = member.FirstName,
                        LastName = member.LastName,
                        Email = member.Email,
                        Phone = member.Phone,
                        PasswordHash = PasswordHasher.Hash(member.Password),
                        RoleId = member.RoleId,
                        BranchId = member.BranchId,
                        Status = "Active",
                        CreatedAt = DateTime.UtcNow.AddMonths(-16)
                    });
                    existingEmails.Add(member.Email.ToLower());
                }
            }

            if (toAdd.Count > 0)
            {
                db.Users.AddRange(toAdd);
                await db.SaveChangesAsync();
            }
        }

        public static async Task SeedSampleDataAsync(RealEstateDbContext db, int tenantId = 1)
        {
            await EnsureRolesAndBaseUsersAsync(db, tenantId);
            await EnsureAdditionalTeamMembersAsync(db, tenantId);

            var agents = await db.Users
                .Include(u => u.Role)
                .Where(u => u.Status.ToLower() == "active" && u.Role.RoleName == "Agent")
                .ToListAsync();

            if (agents.Count == 0)
            {
                agents = await db.Users.Include(u => u.Role).ToListAsync();
            }

            int fallbackUserId = agents.FirstOrDefault()?.UserId ?? 1;

            // 1. Customers & Buyer Profiles (at least 90 customers)
            await EnsureCustomersAndBuyerProfilesAsync(db, agents, fallbackUserId);

            // 2. Properties (at least 85 properties across prime locations)
            await EnsurePropertiesAsync(db, agents, fallbackUserId, tenantId);

            // 3. Transactions / Deals with DealContingency and DealClause (at least 350 transactions)
            await SeedTransactionsAsync(db, targetCount: 350, tenantId);

            // 4. Leads & Support Tickets with Comments (at least 75 leads, 40 tickets, 80+ comments)
            await SeedLeadsAndTicketsAsync(db, tenantId);

            // 5. Activities & Showings (at least 150 activities across calls, meetings, showings, emails)
            await SeedActivitiesAndShowingsAsync(db, tenantId);

            // 6. Follow-Up Task Reminders (at least 60 follow-ups across pending, overdue, completed)
            await SeedFollowUpsAsync(db, tenantId);

            // 7. Marketing Campaigns (at least 10 active/strategic campaigns)
            await SeedCampaignsAsync(db, tenantId);

            // 8. Notifications & Preferences
            await SeedNotificationsAsync(db, tenantId);
        }

        public static async Task EnsureCustomersAndBuyerProfilesAsync(RealEstateDbContext db, List<User> agents, int fallbackUserId)
        {
            int currentCount = await db.Customers.CountAsync(c => !c.IsDeleted);
            if (currentCount < 90)
            {
                int needed = 95 - currentCount;
                var newCustomers = GenerateCustomerPool(needed, agents, fallbackUserId);
                db.Customers.AddRange(newCustomers);
                await db.SaveChangesAsync();
            }

            // Ensure BuyerProfiles exist for buyers and investors
            var existingBuyerProfileCustomerIds = await db.BuyerProfiles
                .Select(bp => bp.CustomerId)
                .ToHashSetAsync();

            var buyersWithoutProfile = await db.Customers
                .Where(c => !c.IsDeleted && (c.Type.ToLower() == "buyer" || c.Type.ToLower() == "investor") && !existingBuyerProfileCustomerIds.Contains(c.CustomerId))
                .ToListAsync();

            if (buyersWithoutProfile.Count > 0)
            {
                var rnd = new Random(505);
                var locations = new[]
                {
                    "BGC, Taguig", "Makati CBD", "Cebu Business Park", "Davao City",
                    "Alabang, Muntinlupa", "Quezon City", "Ortigas, Pasig", "Nuvali, Santa Rosa",
                    "New Manila, Quezon City", "San Juan City", "Lahug, Cebu City", "Tagaytay City"
                };
                var propTypes = new[] { "condo", "house", "townhouse", "commercial", "lot" };
                var budgets = new[] { 3500000m, 5500000m, 8500000m, 12000000m, 18500000m, 24000000m, 35000000m, 48000000m };

                var profiles = new List<BuyerProfile>();
                foreach (var customer in buyersWithoutProfile)
                {
                    profiles.Add(new BuyerProfile
                    {
                        CustomerId = customer.CustomerId,
                        Budget = budgets[rnd.Next(budgets.Length)],
                        PreferredLocation = locations[rnd.Next(locations.Length)],
                        PreferredPropertyType = propTypes[rnd.Next(propTypes.Length)]
                    });
                }

                db.BuyerProfiles.AddRange(profiles);
                await db.SaveChangesAsync();
            }
        }

        public static async Task EnsurePropertiesAsync(RealEstateDbContext db, List<User> agents, int fallbackUserId, int tenantId = 1)
        {
            var branches = await db.Branches.OrderBy(b => b.BranchId).ToListAsync();
            int? hqBranchId = branches.FirstOrDefault(b => b.BranchCode == "BR-MNL")?.BranchId ?? branches.FirstOrDefault()?.BranchId;
            int? cebuBranchId = branches.FirstOrDefault(b => b.BranchCode == "BR-CEB")?.BranchId ?? branches.Skip(1).FirstOrDefault()?.BranchId;
            int? davaoBranchId = branches.FirstOrDefault(b => b.BranchCode == "BR-DVO")?.BranchId ?? branches.Skip(2).FirstOrDefault()?.BranchId;

            int currentCount = await db.Properties.CountAsync();
            if (currentCount < 85)
            {
                var customers = await db.Customers.Where(c => !c.IsDeleted).ToListAsync();
                if (customers.Count == 0) return;

                int needed = 90 - currentCount;
                var newProperties = GeneratePropertyCatalog(needed, customers, agents, fallbackUserId, tenantId, hqBranchId, cebuBranchId, davaoBranchId);
                db.Properties.AddRange(newProperties);
                await db.SaveChangesAsync();
            }

            if (tenantId == 3 && branches.Count > 0)
            {
                var propsWithoutBranch = await db.Properties.Where(p => p.BranchId == null).ToListAsync();
                if (propsWithoutBranch.Count > 0)
                {
                    foreach (var p in propsWithoutBranch)
                    {
                        p.BranchId = ResolveBranchForLocation(p.Address, hqBranchId, cebuBranchId, davaoBranchId);
                    }
                    await db.SaveChangesAsync();
                }
            }
        }

        public static async Task<int> SeedTransactionsAsync(RealEstateDbContext db, int targetCount = 350, int tenantId = 1)
        {
            var agents = await db.Users
                .Include(u => u.Role)
                .Where(u => u.Status.ToLower() == "active")
                .ToListAsync();

            var salesAgents = agents
                .Where(u => u.Role != null && u.Role.RoleName.Equals("Agent", StringComparison.OrdinalIgnoreCase))
                .ToList();

            if (salesAgents.Count == 0) salesAgents = agents;
            if (salesAgents.Count == 0) return 0;

            int fallbackUserId = salesAgents[0].UserId;

            var branches = await db.Branches.OrderBy(b => b.BranchId).ToListAsync();
            int? hqBranchId = branches.FirstOrDefault(b => b.BranchCode == "BR-MNL")?.BranchId ?? branches.FirstOrDefault()?.BranchId;
            int? cebuBranchId = branches.FirstOrDefault(b => b.BranchCode == "BR-CEB")?.BranchId ?? branches.Skip(1).FirstOrDefault()?.BranchId;
            int? davaoBranchId = branches.FirstOrDefault(b => b.BranchCode == "BR-DVO")?.BranchId ?? branches.Skip(2).FirstOrDefault()?.BranchId;

            // Ensure rich pools
            var existingCustomers = await db.Customers.Where(c => !c.IsDeleted).ToListAsync();
            if (existingCustomers.Count < 50)
            {
                var newCustomers = GenerateCustomerPool(60 - existingCustomers.Count, salesAgents, fallbackUserId);
                db.Customers.AddRange(newCustomers);
                await db.SaveChangesAsync();
                existingCustomers = await db.Customers.Where(c => !c.IsDeleted).ToListAsync();
            }

            var existingProperties = await db.Properties.ToListAsync();
            if (existingProperties.Count < 60)
            {
                var newProperties = GeneratePropertyCatalog(70 - existingProperties.Count, existingCustomers, salesAgents, fallbackUserId, tenantId, hqBranchId, cebuBranchId, davaoBranchId);
                db.Properties.AddRange(newProperties);
                await db.SaveChangesAsync();
                existingProperties = await db.Properties.ToListAsync();
            }

            int currentDealCount = await db.Deals.CountAsync();
            int dealsToCreate = targetCount - currentDealCount;
            if (dealsToCreate > 0)
            {
                var deals = GenerateDealTransactions(dealsToCreate, existingCustomers, existingProperties, salesAgents, fallbackUserId, tenantId);
                db.Deals.AddRange(deals);
                await db.SaveChangesAsync();

                // Generate contingencies and clauses for new deals
                AttachContingenciesAndClauses(db, deals);
                await db.SaveChangesAsync();
            }

            // Also check if any existing deals lack contingencies/clauses
            var dealsWithoutContingencies = await db.Deals
                .Include(d => d.Contingencies)
                .Include(d => d.DealClauses)
                .Where(d => d.Contingencies.Count == 0 || d.DealClauses.Count == 0)
                .Take(100)
                .ToListAsync();

            if (dealsWithoutContingencies.Count > 0)
            {
                AttachContingenciesAndClauses(db, dealsWithoutContingencies);
                await db.SaveChangesAsync();
            }

            if (tenantId == 3 && branches.Count > 0)
            {
                var deals = await db.Deals.Include(d => d.Agent).Include(d => d.Property).Take(500).ToListAsync();
                var hqAgents = salesAgents.Where(a => a.BranchId == hqBranchId || a.BranchId == null).ToList();
                var cebuAgents = salesAgents.Where(a => a.BranchId == cebuBranchId).ToList();
                var davaoAgents = salesAgents.Where(a => a.BranchId == davaoBranchId).ToList();
                bool anyChanged = false;

                for (int i = 0; i < deals.Count; i++)
                {
                    var d = deals[i];
                    int? targetBranchId = d.Property != null
                        ? (d.Property.BranchId ?? ResolveBranchForLocation(d.Property.Address, hqBranchId, cebuBranchId, davaoBranchId))
                        : (d.Agent?.BranchId ?? hqBranchId);

                    if (targetBranchId == cebuBranchId && cebuAgents.Count > 0)
                    {
                        var cAgent = cebuAgents[i % cebuAgents.Count];
                        if (d.AgentId != cAgent.UserId) { d.AgentId = cAgent.UserId; d.CreatedByUserId = cAgent.UserId; anyChanged = true; }
                    }
                    else if (targetBranchId == davaoBranchId && davaoAgents.Count > 0)
                    {
                        var dAgent = davaoAgents[i % davaoAgents.Count];
                        if (d.AgentId != dAgent.UserId) { d.AgentId = dAgent.UserId; d.CreatedByUserId = dAgent.UserId; anyChanged = true; }
                    }
                    else if (hqAgents.Count > 0)
                    {
                        var hAgent = hqAgents[i % hqAgents.Count];
                        if (d.AgentId != hAgent.UserId) { d.AgentId = hAgent.UserId; d.CreatedByUserId = hAgent.UserId; anyChanged = true; }
                    }

                    if (d.BranchId != targetBranchId)
                    {
                        d.BranchId = targetBranchId;
                        anyChanged = true;
                    }
                }
                if (anyChanged)
                {
                    await db.SaveChangesAsync();
                }
            }

            return Math.Max(dealsToCreate, 0);
        }

        private static void AttachContingenciesAndClauses(RealEstateDbContext db, IEnumerable<Deal> deals)
        {
            var rnd = new Random(707);

            var contingencyTemplates = new (string Name, string Desc)[]
            {
                ("Bank Financing Approval", "Buyer must secure formal bank loan approval certificate within 30 calendar days."),
                ("Appraisal Valuation", "Subject to bank appraiser property valuation meeting or exceeding the agreed sale price."),
                ("Home Inspection & Punchlist", "Seller guarantees rectification of all ocular inspection punchlist items prior to turnover."),
                ("Clear Title & CAR Release", "Seller shall produce original TCT and BIR Certificate Authorizing Registration clearance."),
                ("HOA Membership Clearance", "Buyer shall obtain official Homeowner Association clearance and car sticker registration."),
                ("Real Property Tax Settlement", "Seller shall present full-year paid Amilyar official tax clearance.")
            };

            var clauseTemplates = new (string Id, string Title, string Text)[]
            {
                ("PAY-01", "Payment Default & Forfeiture Clause", "Failure by the Buyer to settle installments beyond sixty (60) days grace period shall trigger Maceda Law remedies."),
                ("TAX-02", "Capital Gains Tax & Withholding Allocation", "Seller warrants direct settlement of 6% Capital Gains Tax and Documentary Stamp Tax with the Bureau of Internal Revenue."),
                ("TO-03", "Possession & Physical Turnover Clearance", "Physical possession and key turnover shall be delivered within fifteen (15) calendar days from receipt of formal Notice of Turnover."),
                ("DIS-04", "Mediation and Dispute Resolution", "Any dispute arising from this contract shall be submitted to conciliation before the Philippine Dispute Resolution Center prior to court litigation."),
                ("ASIS-05", "As-Is-Where-Is Architectural Conveyance", "The property is conveyed in its current condition with all built-in fixtures, cabinetry, and existing utility meters.")
            };

            foreach (var deal in deals)
            {
                if (deal.Contingencies == null || deal.Contingencies.Count == 0)
                {
                    int contCount = rnd.Next(2, 4);
                    for (int c = 0; c < contCount; c++)
                    {
                        var tpl = contingencyTemplates[(deal.DealId + c) % contingencyTemplates.Length];
                        bool isSatisfied = deal.Stage == "Closed" || (deal.Stage == "Contract" && rnd.Next(100) < 60);

                        db.DealContingencies.Add(new DealContingency
                        {
                            DealId = deal.DealId,
                            ContingencyName = tpl.Name,
                            Description = tpl.Desc,
                            DueDate = deal.ExpectedCloseDate ?? deal.CreatedAt.AddDays(30),
                            IsSatisfied = isSatisfied,
                            SatisfiedAt = isSatisfied ? (deal.ContractSignedDate ?? deal.CreatedAt.AddDays(15)) : null,
                            CreatedAt = deal.CreatedAt
                        });
                    }
                }

                if (deal.DealClauses == null || deal.DealClauses.Count == 0)
                {
                    int clauseCount = rnd.Next(2, 4);
                    for (int cl = 0; cl < clauseCount; cl++)
                    {
                        var tpl = clauseTemplates[(deal.DealId + cl) % clauseTemplates.Length];
                        db.DealClauses.Add(new DealClause
                        {
                            DealId = deal.DealId,
                            ClauseId = tpl.Id,
                            Title = tpl.Title,
                            ClauseText = tpl.Text,
                            IsApproved = true,
                            ApprovedAt = deal.CreatedAt.AddDays(1),
                            CreatedAt = deal.CreatedAt
                        });
                    }
                }
            }
        }

        private static List<Customer> GenerateCustomerPool(int count, List<User> agents, int fallbackUserId)
        {
            var firstNames = new[]
            {
                "Maria", "Juan", "Carlos", "Lourdes", "Jose", "Ana", "Miguel", "Carmela", "Eduardo", "Teresa",
                "Ferdinand", "Patricia", "Gabriel", "Isabel", "Ramon", "Rowena", "Antonio", "Elena", "Ricardo", "Cristina",
                "Paolo", "Kristine", "Angelo", "Bianca", "Marco", "Camille", "Rafael", "Stephanie", "Enrico", "Katrina",
                "Dante", "Jasmine", "Leandro", "Clarissa", "Benjamin", "Rochelle", "Dominic", "Vanessa", "Victor", "Giselle",
                "Arnel", "Rowell", "Maricel", "Renato", "Liezl", "Glenn", "Aileen", "Dennis", "Bernadette", "Noel",
                "Emmanuel", "Sheila", "Roderick", "Marilou", "Vincent", "Corazon", "Alvin", "Marissa", "Gerald", "Lourdes"
            };

            var lastNames = new[]
            {
                "Santos", "Dela Cruz", "Mendoza", "Reyes", "Bautista", "Aquino", "Garcia", "Gonzales", "Ramos", "Lopez",
                "Fernandez", "Castillo", "Villanueva", "Torres", "Salazar", "Rivera", "Mercado", "Pascual", "Valenzuela", "Naval",
                "Tan", "Sy", "Lim", "Co", "Chua", "Ong", "Go", "Uy", "Soriano", "Corpuz",
                "Espiritu", "Manalo", "Tolentino", "Alcantara", "Santiago", "Morales", "Perez", "Guerrero", "De Leon", "Cabrera",
                "Valderama", "Gutierrez", "Navarro", "Ocampo", "Flores", "Vergara", "Aguilar", "Del Rosario", "Salazar", "Cruz"
            };

            var rnd = new Random(101);
            var customers = new List<Customer>();

            for (int i = 0; i < count; i++)
            {
                string fn = firstNames[rnd.Next(firstNames.Length)];
                string ln = lastNames[rnd.Next(lastNames.Length)];
                string email = $"{fn.ToLower()}.{ln.ToLower()}{rnd.Next(10, 999)}@example.ph";
                string phone = $"09{rnd.Next(10, 99)}{rnd.Next(1000000, 9999999)}";

                var assignedAgent = agents[i % agents.Count];
                int typeRoll = rnd.Next(100);
                string custType = typeRoll < 60 ? "buyer" : (typeRoll < 85 ? "seller" : "investor");

                var customer = new Customer
                {
                    FirstName = fn,
                    LastName = ln,
                    Email = email,
                    Phone = phone,
                    Type = custType,
                    Status = "active",
                    AssignmentStatus = "approved",
                    AssignedAgentId = assignedAgent.UserId,
                    CreatedByUserId = assignedAgent.UserId,
                    CreatedAt = DateTime.UtcNow.AddMonths(-rnd.Next(1, 18)).AddDays(-rnd.Next(1, 28))
                };

                customers.Add(customer);
            }

            return customers;
        }

        private static int? ResolveBranchForLocation(string address, int? hqBranchId, int? cebuBranchId, int? davaoBranchId)
        {
            if (string.IsNullOrWhiteSpace(address)) return hqBranchId;
            string lower = address.ToLower();
            if (lower.Contains("cebu") || lower.Contains("mandaue") || lower.Contains("bohol") || lower.Contains("iloilo"))
                return cebuBranchId ?? hqBranchId;
            if (lower.Contains("davao"))
                return davaoBranchId ?? hqBranchId;
            return hqBranchId;
        }

        private static List<Property> GeneratePropertyCatalog(
            int count,
            List<Customer> customers,
            List<User> agents,
            int fallbackUserId,
            int tenantId = 1,
            int? hqBranchId = null,
            int? cebuBranchId = null,
            int? davaoBranchId = null)
        {
            var propertyTemplates = new (string Address, string Type, decimal Price)[]
            {
                // Condos - Metro Manila & Regional Centers
                ("Unit 1204, One Serendra, Bonifacio Global City, Taguig", "condo", 18500000m),
                ("Unit 802, The Rise Makati, Malugay St, San Antonio, Makati", "condo", 7800000m),
                ("Unit 24B, Proscenium at Rockwell, Makati City", "condo", 24000000m),
                ("Unit 1805, Azure Urban Resort Residences, Parañaque City", "condo", 4200000m),
                ("Unit 912, The Florence, McKinley Hill, Taguig", "condo", 6500000m),
                ("Unit 301, Solinea Tower 2, Cebu Business Park, Cebu City", "condo", 5800000m),
                ("Unit 1408, Marco Polo Residences, Nivel Hills, Cebu City", "condo", 7200000m),
                ("Unit 703, Abreeza Residences, Bajada, Davao City", "condo", 5100000m),
                ("Unit 510, Avida Towers Centera, Mandaluyong City", "condo", 4600000m),
                ("Unit 1601, Light Residences, EDSA, Mandaluyong City", "condo", 3800000m),
                ("Unit 2203, Two Maridien, High Street South, BGC, Taguig", "condo", 15200000m),
                ("Unit 1109, Sheridan Towers, Pasig City", "condo", 6300000m),
                ("Unit 808, Flair Towers, Reliance St, Mandaluyong City", "condo", 5500000m),
                ("Unit 405, The Residences at Commonwealth, Quezon City", "condo", 4100000m),
                ("Unit 1902, Megaworld Iloilo Business Park, Mandurriao, Iloilo", "condo", 4900000m),
                ("Unit 615, 32 Sanson by Rockwell, Lahug, Cebu City", "condo", 9800000m),
                ("Unit 1020, One Oasis Davao, Eco West Drive, Davao City", "condo", 3500000m),
                ("Unit 1508, The Lerato Tower 1, Bel-Air, Makati City", "condo", 8900000m),
                ("Unit 28A, Grand Hyatt Manila Residences, BGC, Taguig", "condo", 36000000m),
                ("Unit 1704, Tivoli Garden Residences, Coronado, Mandaluyong", "condo", 4700000m),
                ("Unit 14C, Park Central Towers, Makati City", "condo", 42000000m),
                ("Unit 2108, Horizons 101, General Maxilom Ave, Cebu City", "condo", 4400000m),
                ("Unit 502, Aeon Towers, J.P. Laurel Ave, Bajada, Davao City", "condo", 6800000m),
                ("Unit 310, Mandani Bay Suites, F.E. Zuellig Ave, Mandaue City", "condo", 5300000m),
                ("Unit 12A, The Albany, McKinley West, Fort Bonifacio, Taguig", "condo", 28000000m),

                // Houses & Subdivisions
                ("Lot 14 Blk 8, Ayala Alabang Village, Muntinlupa City", "house", 38000000m),
                ("Blk 5 Lot 12, Hillsborough Alabang, Muntinlupa", "house", 26000000m),
                ("Blk 2 Lot 9, Corinthian Gardens, Quezon City", "house", 45000000m),
                ("Lot 22 Blk 3, Greenmeadows Subdivision, Quezon City", "house", 42000000m),
                ("Blk 17 Lot 4, Valle Verde 5, Pasig City", "house", 31000000m),
                ("Lot 8 Blk 10, San Lorenzo Village, Makati City", "house", 48000000m),
                ("Blk 3 Lot 15, Magallanes Village, Makati City", "house", 35000000m),
                ("Blk 9 Lot 7, Grand Villas Subdivision, Buhangin, Davao City", "house", 8500000m),
                ("Lot 11 Blk 2, Maria Luisa Estate Park, Banilad, Cebu City", "house", 22000000m),
                ("Blk 6 Lot 18, North Town Homes, Cabancalan, Mandaue City", "house", 16500000m),
                ("Blk 4 Lot 21, South Peak Subdivision, San Pedro, Laguna", "house", 5200000m),
                ("Blk 8 Lot 3, Woodridge Park, Ma-a, Davao City", "house", 9800000m),
                ("Blk 12 Lot 16, Ladislawa Garden Village, Buhangin, Davao City", "house", 12500000m),
                ("Blk 15 Lot 22, Portofino Heights, Daang Hari, Las Piñas", "house", 21000000m),
                ("Blk 7 Lot 10, Ayala Greenfield Estates, Calamba, Laguna", "house", 17800000m),
                ("Lot 31 Blk 6, Bel-Air Village Phase 2, Makati City", "house", 39000000m),
                ("Blk 19 Lot 5, Verdana Homes Mamplasan, Biñan, Laguna", "house", 14500000m),
                ("Lot 2 Blk 8, South Forbes Golf City, Silang, Cavite", "house", 19200000m),
                ("Blk 11 Lot 14, Havila Townscape, Taytay, Rizal", "house", 7600000m),

                // Townhouses
                ("Townhouse Unit 4, Mahogany Place 3, Acacia Estates, Taguig", "townhouse", 14500000m),
                ("Townhouse 2B, Scout Tuason, Diliman, Quezon City", "townhouse", 11200000m),
                ("Townhouse 7, Gilmore Townhomes, New Manila, Quezon City", "townhouse", 16800000m),
                ("Unit C-12, Ametta Place, Mercedes Ave, Pasig City", "townhouse", 9500000m),
                ("Townhouse Unit 8, Alabang 400 Village, Muntinlupa City", "townhouse", 10800000m),
                ("Townhouse 3A, West Greenhills, San Juan City", "townhouse", 22500000m),
                ("Townhouse 10, Casa Verde, Pasig City", "townhouse", 12900000m),
                ("Townhouse Unit 5, Capitol Green Village, Tandang Sora, QC", "townhouse", 8700000m),
                ("Townhouse 6, Loyola Grand Villas, Quezon City", "townhouse", 15400000m),
                ("Townhouse 1B, Horseshoe Village, Quezon City", "townhouse", 19500000m),
                ("Townhouse 4D, Scout Rallos, Laging Handa, Quezon City", "townhouse", 13800000m),
                ("Townhouse 2, San Juan Heights, Addition Hills, San Juan", "townhouse", 17200000m),

                // Lots & Commercial
                ("Commercial Lot 105, Madrigal Business Park, Alabang, Muntinlupa", "commercial", 42000000m),
                ("Prime Commercial Space 301, IT Park, Lahug, Cebu City", "commercial", 19000000m),
                ("Corner Commercial Lot, J.P. Laurel Ave, Bajada, Davao City", "commercial", 28000000m),
                ("Lot 45, Nuvali Heights, Santa Rosa, Laguna", "lot", 6800000m),
                ("Residential Lot 12, Anvaya Cove, Morong, Bataan", "lot", 9200000m),
                ("Industrial Lot 8, Light Industry & Science Park, Cabuyao, Laguna", "lot", 24000000m),
                ("Commercial Lot, Ortigas East, Pasig City", "commercial", 36000000m),
                ("Commercial Building 2A, Shaw Boulevard, Mandaluyong City", "commercial", 48000000m),
                ("Retail Unit 102, Eastwood Citywalk 2, Bagumbayan, Quezon City", "commercial", 14000000m),
                ("Commercial Office 504, Cebu Exchange, Salinas Drive, Cebu City", "commercial", 16500000m),
                ("Prime Commercial Lot, Lanang Premier Business Park, Davao City", "commercial", 32000000m),
                ("Commercial Warehouse 3, Silangan Industrial Estate, Canlubang, Laguna", "commercial", 29000000m),
                ("Agricultural Lot 4, Alfonso, Cavite near Tagaytay", "lot", 7500000m),
                ("Eco-Tourism Lot 9, Panglao Island, Bohol", "lot", 13500000m),
                ("Residential Beachfront Lot 18, Punta Fuego, Nasugbu, Batangas", "lot", 22000000m),
                ("Commercial Space 104, Clark Global City, Pampanga", "commercial", 21500000m)
            };

            var rnd = new Random(202);
            var properties = new List<Property>();

            for (int i = 0; i < count; i++)
            {
                var template = propertyTemplates[i % propertyTemplates.Length];
                string address = i >= propertyTemplates.Length
                    ? $"{template.Address} - Phase {i / propertyTemplates.Length + 1}"
                    : template.Address;

                var owner = customers[rnd.Next(customers.Count)];
                var agent = agents[rnd.Next(agents.Count)];

                int statRoll = rnd.Next(100);
                string status = statRoll < 60 ? "available" : (statRoll < 80 ? "reserved" : (statRoll < 92 ? "sold" : "under offer"));

                int? branchId = tenantId == 3 ? ResolveBranchForLocation(address, hqBranchId, cebuBranchId, davaoBranchId) : null;

                var prop = new Property
                {
                    Address = address,
                    PropertyType = template.Type,
                    Price = template.Price,
                    Status = status,
                    AssignmentStatus = "approved",
                    OwnerCustomerId = owner.CustomerId,
                    ListedByAgentId = agent.UserId,
                    CreatedByUserId = agent.UserId,
                    BranchId = branchId,
                    CreatedAt = DateTime.UtcNow.AddMonths(-rnd.Next(1, 18)).AddDays(-rnd.Next(1, 28))
                };

                properties.Add(prop);
            }

            return properties;
        }

        private static List<Deal> GenerateDealTransactions(
            int count,
            List<Customer> customers,
            List<Property> properties,
            List<User> agents,
            int fallbackUserId,
            int tenantId = 1)
        {
            var stipulationsPool = new[]
            {
                "Subject to standard bank loan appraisal and formal credit approval within 30 days.",
                "Includes 1 designated basement parking slot and existing built-in kitchen cabinetry.",
                "Seller warrants settlement of all capital gains tax and real property taxes through closing date.",
                "Property conveyed in as-is-where-is condition with complete architectural turnover clearance.",
                "Move-in and key turnover immediately upon full release of clear bank loan proceeds.",
                "Includes transfer of active country club share and homeowner association membership privileges.",
                "Reservation fee is strictly credited toward the mandatory down payment schedule.",
                "Buyer assumes responsibility for documentary stamp tax, local transfer tax, and title registration fees.",
                "Turnover guaranteed within 45 calendar days following receipt of final contract signatures.",
                "Seller guarantees clear, unencumbered title free from any adverse liens or lis pendens."
            };

            var commissionRates = new[] { 0.03m, 0.035m, 0.04m, 0.045m, 0.05m };
            var paymentSchemes = new[] { "Bank Financing", "Spot Cash", "Deferred In-House" };
            var downPaymentPercents = new[] { 10m, 20m, 30m };
            var reservationFees = new[] { 25000m, 50000m, 75000m, 100000m, 150000m };

            var rnd = new Random(303);
            var deals = new List<Deal>(count);

            DateTime now = DateTime.UtcNow;
            DateTime startDate = now.AddMonths(-18);

            for (int i = 0; i < count; i++)
            {
                int monthOffset = (i * 18) / count;
                int dayOffset = rnd.Next(1, 28);
                int hour = rnd.Next(8, 19);
                int minute = rnd.Next(0, 60);

                DateTime dealDate = startDate.AddMonths(monthOffset).AddDays(dayOffset).AddHours(hour).AddMinutes(minute);
                if (dealDate > now)
                {
                    dealDate = now.AddDays(-rnd.Next(1, 5)).AddHours(-rnd.Next(1, 10));
                }

                var customer = customers[rnd.Next(customers.Count)];
                var property = properties[rnd.Next(properties.Count)];
                var agent = agents[i % agents.Count]; // Even distribution across all sales agents

                string stage;
                int roll = rnd.Next(100);
                if (monthOffset < 15) // Older deals
                {
                    if (roll < 78) stage = "Closed";
                    else if (roll < 90) stage = "Lost";
                    else if (roll < 96) stage = "Contract";
                    else stage = "Reservation";
                }
                else // Recent deals
                {
                    if (roll < 22) stage = "Closed";
                    else if (roll < 48) stage = "Contract";
                    else if (roll < 72) stage = "Reservation";
                    else if (roll < 90) stage = "Offer";
                    else stage = "Lost";
                }

                decimal basePrice = property.Price > 100000m ? property.Price : 4500000m;
                double multiplier = 0.90 + (rnd.NextDouble() * 0.18);
                decimal dealValue = Math.Round((basePrice * (decimal)multiplier) / 50000m) * 50000m;

                decimal commissionRate = commissionRates[rnd.Next(commissionRates.Length)];
                string scheme = paymentSchemes[rnd.Next(paymentSchemes.Length)];
                decimal downPayment = scheme == "Spot Cash" ? 100m : downPaymentPercents[rnd.Next(downPaymentPercents.Length)];
                decimal reservationFee = reservationFees[rnd.Next(reservationFees.Length)];
                string stipulation = stipulationsPool[rnd.Next(stipulationsPool.Length)];

                DateTime? expectedClose = null;
                DateTime? contractSigned = null;

                if (stage == "Closed")
                {
                    int daysToClose = rnd.Next(18, 55);
                    DateTime signedDate = dealDate.AddDays(daysToClose);
                    if (signedDate > now) signedDate = now.AddDays(-1);
                    contractSigned = signedDate;
                    expectedClose = signedDate.AddDays(-rnd.Next(0, 7));
                }
                else if (stage == "Contract")
                {
                    expectedClose = dealDate.AddDays(rnd.Next(25, 75));
                }
                else if (stage == "Reservation")
                {
                    expectedClose = dealDate.AddDays(rnd.Next(40, 110));
                }
                else if (stage == "Offer")
                {
                    expectedClose = dealDate.AddDays(rnd.Next(30, 90));
                }
                else // Lost
                {
                    expectedClose = dealDate.AddDays(rnd.Next(14, 45));
                    stipulation = "Client retracted offer due to mortgage contingency or competing property acquisition.";
                }

                int? dealBranchId = tenantId == 3 ? (agent.BranchId ?? property.BranchId) : null;

                var deal = new Deal
                {
                    CustomerId = customer.CustomerId,
                    PropertyId = property.PropertyId,
                    AgentId = agent.UserId,
                    CreatedByUserId = agent.UserId,
                    BranchId = dealBranchId,
                    Value = dealValue,
                    CommissionRate = commissionRate,
                    Stage = stage,
                    PaymentScheme = scheme,
                    ReservationFee = reservationFee,
                    DownPaymentPercent = downPayment,
                    CgtPayer = rnd.Next(100) < 85 ? "Seller" : "50/50 Shared",
                    DstPayer = rnd.Next(100) < 85 ? "Buyer" : "50/50 Shared",
                    TransferTaxPayer = "Buyer",
                    RegistrationFeePayer = "Buyer",
                    SpecialStipulations = stipulation,
                    ExpectedCloseDate = expectedClose,
                    ContractSignedDate = contractSigned,
                    CreatedAt = dealDate
                };

                deals.Add(deal);
            }

            return deals;
        }

        public static async Task SeedLeadsAndTicketsAsync(RealEstateDbContext db, int tenantId = 1)
        {
            var agents = await db.Users
                .Include(u => u.Role)
                .Where(u => u.Status.ToLower() == "active")
                .ToListAsync();

            if (agents.Count == 0) return;

            var customers = await db.Customers
                .Where(c => !c.IsDeleted)
                .ToListAsync();

            var rnd = new Random(42);

            // 1. Seed Leads (target: at least 75 leads)
            int leadCount = await db.Leads.CountAsync();
            if (leadCount < 75)
            {
                var leadNames = new (string Fn, string Ln, string Email, string Phone, string Note)[]
                {
                    ("Ramon", "Valderama", "ramon.valderama@yahoo.com", "09171112233", "Inquired about 2BR penthouse in BGC. Overseas investor looking for rental yield."),
                    ("Beatrice", "Tan", "beatrice.tan@gmail.com", "09182223344", "First-time buyer interested in Avida Towers Centera with bank financing pre-approval."),
                    ("Leandro", "Santos", "leandro.santos@outlook.com", "09203334455", "Looking for 3BR house and lot in Nuvali Santa Rosa. Budget around 15M."),
                    ("Maricris", "Reyes", "maricris.reyes@gmail.com", "09224445566", "Met at Cebu IT Park showroom. Wants floor plans and amortization breakdown."),
                    ("Joshua", "Alcantara", "joshua.alcantara@yahoo.com", "09155556677", "Inquiring about commercial lot in Davao City for logistics warehouse."),
                    ("Catherine", "Lim", "catherine.lim@gmail.com", "09276667788", "Interested in Proscenium Rockwell unit. Prefers high floor facing amenities."),
                    ("Gabriel", "De Jesus", "gabriel.dejesus@outlook.com", "09187778899", "Seeking pre-selling townhouse in New Manila with 2-car garage."),
                    ("Michelle", "Soriano", "michelle.soriano@gmail.com", "09208889900", "Referral from Juan Dela Cruz. Looking for residential lot in Tagaytay."),
                    ("Antonio", "Chua", "antonio.chua@yahoo.com", "09229990011", "Commercial space inquiry for clinic franchise at Ortigas Center."),
                    ("Daphne", "Aquino", "daphne.aquino@gmail.com", "09170001122", "Attended Open House at Azure Residences. Spot cash buyer requesting discount."),
                    ("Paolo", "Gutierrez", "paolo.gutierrez@outlook.com", "09181113355", "Family relocating from Davao to Makati. Looking for townhouse near Greenbelt."),
                    ("Rowena", "Villanueva", "rowena.villanueva@gmail.com", "09202224466", "Interested in Maria Luisa Estate Park home in Cebu. Requires fast closing."),
                    ("Kenneth", "Gonzales", "kenneth.gonzales@yahoo.com", "09153335577", "Inquired via Facebook ad regarding Grand Hyatt Residences luxury suites."),
                    ("Clarisse", "Navarro", "clarisse.navarro@gmail.com", "09274446688", "OFW nurse in UK looking for affordable condo near MRT for siblings."),
                    ("Eduardo", "Castillo", "eduardo.castillo@outlook.com", "09185557799", "Requested site inspection for Ayala Greenfield Estates golf lot."),
                    ("Krizza", "Bautista", "krizza.bautista@gmail.com", "09206668800", "Looking for corner commercial lot in Bajada, Davao City."),
                    ("Victor", "Mendoza", "victor.mendoza@yahoo.com", "09227779911", "Walk-in inquiry at Rockwell showroom. Budget 25M."),
                    ("Aileen", "Santiago", "aileen.santiago@gmail.com", "09178880022", "Looking for rental investment studio unit in Cebu Business Park."),
                    ("Christian", "Tolentino", "christian.tolentino@outlook.com", "09189991133", "Inquired about Portofino Heights Italian villa. Wants ocular showing."),
                    ("Bernadette", "Ocampo", "bernadette.ocampo@gmail.com", "09200002244", "Inquired about 1BR Sheridan Towers unit. Bank loan processing ongoing."),
                    ("Jerome", "Cruz", "jerome.cruz@yahoo.com", "09151113366", "Seeking townhouse in Acacia Estates Taguig for expanding family."),
                    ("Stephanie", "Flores", "stephanie.flores@gmail.com", "09272224477", "Looking for vacation home in Anvaya Cove Bataan."),
                    ("Rafael", "Mercado", "rafael.mercado@outlook.com", "09183335588", "Commercial lease inquiry for IT BPO office in Cebu Exchange."),
                    ("Giselle", "Pascual", "giselle.pascual@gmail.com", "09204446699", "Inquired through website contact form. Looking for condo in Mandaluyong."),
                    ("Lorenzo", "Aguilar", "lorenzo.aguilar@yahoo.com", "09225557700", "Interested in 4BR house in Valle Verde 5. Ready for offer submission."),
                    ("Patricia", "Del Rosario", "patricia.delrosario@gmail.com", "09176668811", "Young professional looking for studio unit near BGC High Street."),
                    ("Francis", "Ramos", "francis.ramos@outlook.com", "09187779922", "Inquiring about industrial lot in Laguna Science Park for fabrication plant."),
                    ("Diana", "Morales", "diana.morales@gmail.com", "09208880033", "Looking for 2BR Solinea condo in Cebu with ocean view."),
                    ("Manuel", "Vergara", "manuel.vergara@yahoo.com", "09159991144", "Wants to view Woodridge Park house in Davao City this Sunday."),
                    ("Theresa", "Salazar", "theresa.salazar@gmail.com", "09270002255", "Inquired regarding deferred financing terms for The Rise Makati."),
                    ("Alfonso", "Magsaysay", "alfonso.magsaysay@gmail.com", "09173334488", "Looking for prime agricultural lot in Alfonso Cavite near Tagaytay."),
                    ("Kristine", "Puyat", "kristine.puyat@outlook.com", "09184445599", "Architect looking for corner townhouse lot in San Juan."),
                    ("Dominador", "Sy", "dominador.sy@yahoo.com", "09205556611", "Investor inquiring about bulk purchase of 3 studio units in Iloilo."),
                    ("Maria Elena", "Laurel", "elena.laurel@gmail.com", "09226667722", "Interested in Corinthian Gardens property. Preparing letter of intent."),
                    ("Felipe", "Macapagal", "felipe.macapagal@outlook.com", "09157778833", "Inquired about beach lot in Panglao Bohol for eco-resort development."),
                    ("Katrina", "Zobel", "katrina.zobel@gmail.com", "09278889944", "Inquired on Park Central Towers penthouse. High-net-worth client."),
                    ("Gerardo", "Roxas", "gerardo.roxas@yahoo.com", "09179990055", "Looking for commercial building along Shaw Boulevard Mandaluyong."),
                    ("Rosanna", "Cojuangco", "rosanna.cojuangco@outlook.com", "09180001166", "Wants updated price list and site plan for Nuvali Heights Santa Rosa."),
                    ("Arturo", "Osmeña", "arturo.osmena@gmail.com", "09201112277", "Inquiring about luxury condominium in Lahug Cebu City."),
                    ("Lourdes", "Quezon", "lourdes.quezon@yahoo.com", "09222223388", "Senior couple downsizing. Looking for low-density condo in Bel-Air Makati."),
                    ("Vicente", "Sotto", "vicente.sotto@outlook.com", "09153334499", "Inquired about commercial showroom unit in Eastwood Citywalk 2."),
                    ("Tessie", "Coseteng", "tessie.coseteng@gmail.com", "09274445500", "Looking for modern townhouse in Loyola Grand Villas Quezon City."),
                    ("Nestor", "Romualdez", "nestor.romualdez@yahoo.com", "09175556611", "Seeking commercial warehouse in Canlubang Laguna for logistics hub."),
                    ("Corazon", "Madrigal", "corazon.madrigal@outlook.com", "09186667722", "Interested in Alabang 400 Village family townhouse."),
                    ("Danilo", "Tuason", "danilo.tuason@gmail.com", "09207778833", "Inquired on Scout Tuason Quezon City modern townhouse development.")
                };

                var sources = new[] { "Website", "Referral", "Walk-in", "Facebook Ad", "Property Portal", "Google Ads", "Billboard / Outdoor", "Open House / Event" };
                var stages = new[] { "new", "contacted", "qualified", "proposal", "converted", "lost" };
                var priorities = new[] { "low", "medium", "high" };

                var usedCustomerIds = await db.Leads
                    .Where(l => l.ConvertedCustomerId.HasValue)
                    .Select(l => l.ConvertedCustomerId!.Value)
                    .ToHashSetAsync();

                var availableCustomers = customers
                    .Where(c => !usedCustomerIds.Contains(c.CustomerId))
                    .ToList();

                var leadsToAdd = new List<Lead>();
                for (int i = 0; i < leadNames.Length; i++)
                {
                    var item = leadNames[i];
                    var agent = agents[i % agents.Count];
                    int daysAgo = rnd.Next(5, 450);
                    var created = DateTime.UtcNow.AddDays(-daysAgo);

                    string stage = stages[rnd.Next(stages.Length)];
                    int? convertedCustId = null;
                    if (stage == "converted" && availableCustomers.Count > 0)
                    {
                        var cust = availableCustomers[0];
                        availableCustomers.RemoveAt(0);
                        convertedCustId = cust.CustomerId;
                    }
                    else if (stage == "converted")
                    {
                        stage = "qualified";
                    }

                    var lead = new Lead
                    {
                        FirstName = item.Fn,
                        LastName = item.Ln,
                        Email = item.Email,
                        Phone = item.Phone,
                        Source = sources[rnd.Next(sources.Length)],
                        Stage = stage,
                        Priority = priorities[rnd.Next(priorities.Length)],
                        ExpectedValue = rnd.Next(28, 220) * 100000m,
                        Notes = item.Note,
                        AssignedAgentId = agent.UserId,
                        CreatedByUserId = agent.UserId,
                        BranchId = tenantId == 3 ? agent.BranchId : null,
                        AssignmentStatus = "approved",
                        ConvertedCustomerId = convertedCustId,
                        CreatedAt = created
                    };

                    leadsToAdd.Add(lead);
                }

                db.Leads.AddRange(leadsToAdd);
                await db.SaveChangesAsync();
            }

            if (tenantId == 3)
            {
                var branches = await db.Branches.OrderBy(b => b.BranchId).ToListAsync();
                int? defaultBranchId = branches.FirstOrDefault()?.BranchId;
                var leadsWithoutBranch = await db.Leads.Include(l => l.AssignedAgent).Where(l => l.BranchId == null).Take(500).ToListAsync();
                if (leadsWithoutBranch.Count > 0)
                {
                    foreach (var l in leadsWithoutBranch)
                    {
                        l.BranchId = l.AssignedAgent?.BranchId ?? defaultBranchId;
                    }
                    await db.SaveChangesAsync();
                }
            }

            // 2. Seed Support Tickets & Comments (target: at least 40 tickets)
            int ticketCount = await db.SupportTickets.CountAsync();
            if (ticketCount < 40 && customers.Count > 0)
            {
                var ticketTemplates = new[]
                {
                    ("Billing", "Inquiry on Capital Gains Tax and Documentary Stamp Tax payment computation and schedule", "Medium", "Resolved", 3, 5),
                    ("Billing", "Request for official receipt and updated statement of account for reservation fee", "Low", "Resolved", 2, 2),
                    ("Contract Inquiry", "Clarification regarding Contract to Sell clause 14 amortization schedule", "High", "In Progress", 7, 0),
                    ("Property Inspection", "Punchlisting and ocular inspection request prior to unit turnover", "Medium", "Resolved", 4, 3),
                    ("Title Transfer", "Status update inquiry for Transfer Certificate of Title release at Registry of Deeds", "High", "In Progress", 14, 0),
                    ("Documentation", "Correction needed for buyer middle name spelling in formal Deed of Absolute Sale", "Critical", "Resolved", 2, 1),
                    ("Maintenance", "Minor drywall hairline crack inspection request during turnover warranty", "Low", "Resolved", 10, 8),
                    ("Billing", "Bank financing letter of guarantee verification for developer release", "High", "Open", 5, 0),
                    ("Contract Inquiry", "Request for addendum on dedicated basement parking slot assignment", "Medium", "Open", 6, 0),
                    ("Title Transfer", "Tax Declaration transfer status follow-up with City Assessor Office", "High", "Resolved", 15, 12),
                    ("Property Inspection", "Follow-up re-inspection for bathroom fixtures after developer rectification", "Medium", "Resolved", 5, 4),
                    ("Documentation", "Request for certified true copies of Master Deed of Declaration of Restrictions", "Low", "Resolved", 7, 6),
                    ("Billing", "Early settlement discount computation for remaining developer financing balance", "Medium", "In Progress", 5, 0),
                    ("Contract Inquiry", "Client requesting extension on down payment installment due date", "Critical", "Resolved", 3, 4),
                    ("Maintenance", "Aircon water drainage tapping inspection request", "Low", "Resolved", 6, 7),
                    ("Documentation", "Authority to Inspect and Move-in Clearance release request", "High", "Resolved", 3, 2),
                    ("Property Inspection", "Electrical load testing and water pressure verification request", "Medium", "Open", 4, 0),
                    ("Title Transfer", "BIR Certificate Authorizing Registration (CAR) release inquiry", "Critical", "In Progress", 10, 0),
                    ("Billing", "Post-dated checks replacement request due to bank branch consolidation", "Medium", "Resolved", 4, 3),
                    ("Contract Inquiry", "Assignment of rights to family member request requirements", "High", "Resolved", 8, 6),
                    ("Documentation", "Homeowners Association membership briefing and car sticker application", "Low", "Resolved", 5, 4),
                    ("Maintenance", "Intercom unit repair and reception connection troubleshooting", "Low", "Open", 7, 0),
                    ("Billing", "Real Property Tax (Amilyar) tax clearance copy request for loan release", "Medium", "Resolved", 4, 3),
                    ("Documentation", "Occupancy Permit and Fire Safety Inspection Certificate verification", "High", "Resolved", 6, 5),
                    ("Property Inspection", "Water meter installation coordination with utility provider", "Medium", "In Progress", 5, 0),
                    ("HOA Coordination", "Application for construction bond refund following interior fit-out completion", "Low", "Resolved", 14, 10),
                    ("Maintenance", "Balcony sliding door rubber gasket replacement under warranty", "Low", "Resolved", 8, 6),
                    ("Billing", "Re-computation of late payment penalty waiver request", "Medium", "Open", 3, 0),
                    ("Contract Inquiry", "Inquiry regarding pre-termination terms for in-house deferred financing", "High", "In Progress", 5, 0),
                    ("Documentation", "Request for duplicate copy of notarized Contract to Sell", "Low", "Resolved", 3, 2),
                    ("Title Transfer", "Clarification on local transfer tax payment computation by Provincial Treasurer", "High", "Resolved", 12, 9),
                    ("Property Inspection", "Kitchen exhaust ducting and grease trap ocular check", "Medium", "Open", 4, 0),
                    ("Billing", "Request for breakdown of miscellaneous closing fees and utility deposit charges", "Medium", "Resolved", 5, 4),
                    ("Documentation", "Special Power of Attorney (SPA) consularization verification for overseas buyer", "High", "Resolved", 7, 5),
                    ("Maintenance", "Water heater circuit breaker tripping troubleshooting", "Medium", "Resolved", 4, 2)
                };

                var ticketsToAdd = new List<SupportTicket>();
                int tNum = 1001 + ticketCount;

                foreach (var tpl in ticketTemplates)
                {
                    var cust = customers[rnd.Next(customers.Count)];
                    var agent = agents[rnd.Next(agents.Count)];
                    int daysAgo = rnd.Next(5, 300);
                    var createdAt = DateTime.UtcNow.AddDays(-daysAgo);
                    var dueDate = createdAt.AddDays(tpl.Item5);

                    DateTime? resolvedAt = null;
                    if (tpl.Item4 == "Resolved")
                    {
                        resolvedAt = createdAt.AddDays(tpl.Item6);
                    }

                    var ticket = new SupportTicket
                    {
                        TicketNumber = $"TICK-{tNum++}",
                        CustomerId = cust.CustomerId,
                        RaisedByUserId = agent.UserId,
                        AssignedToUserId = agent.UserId,
                        Category = tpl.Item1,
                        Description = tpl.Item2,
                        Priority = tpl.Item3,
                        Status = tpl.Item4,
                        DueDate = dueDate,
                        CreatedAt = createdAt,
                        FirstRespondedAt = createdAt.AddHours(rnd.Next(1, 12)),
                        ResolvedAt = resolvedAt
                    };

                    ticketsToAdd.Add(ticket);
                }

                db.SupportTickets.AddRange(ticketsToAdd);
                await db.SaveChangesAsync();

                // Attach TicketComments
                var commentsToAdd = new List<TicketComment>();
                foreach (var t in ticketsToAdd)
                {
                    int authorId = t.AssignedToUserId ?? t.RaisedByUserId;

                    commentsToAdd.Add(new TicketComment
                    {
                        TicketId = t.TicketId,
                        AuthorUserId = authorId,
                        CommentText = $"Ticket opened and categorized under {t.Category}. Reviewing customer documentation.",
                        CommentType = "Comment",
                        IsInternal = false,
                        CreatedAt = t.CreatedAt.AddMinutes(15)
                    });

                    if (t.Status == "In Progress" || t.Status == "Resolved")
                    {
                        commentsToAdd.Add(new TicketComment
                        {
                            TicketId = t.TicketId,
                            AuthorUserId = authorId,
                            CommentText = "Coordination in progress with developer accounts and documentation department.",
                            CommentType = "StatusChange",
                            IsInternal = true,
                            CreatedAt = t.CreatedAt.AddHours(4)
                        });
                    }

                    if (t.Status == "Resolved")
                    {
                        commentsToAdd.Add(new TicketComment
                        {
                            TicketId = t.TicketId,
                            AuthorUserId = authorId,
                            CommentText = "Resolution confirmed with client. Clearance released and filed in records.",
                            CommentType = "Comment",
                            IsInternal = false,
                            CreatedAt = t.ResolvedAt ?? t.CreatedAt.AddDays(2)
                        });
                    }
                }

                if (commentsToAdd.Count > 0)
                {
                    db.TicketComments.AddRange(commentsToAdd);
                    await db.SaveChangesAsync();
                }
            }
        }

        public static async Task SeedActivitiesAndShowingsAsync(RealEstateDbContext db, int tenantId = 1)
        {
            int currentCount = await db.Activities.CountAsync();
            if (currentCount >= 120) return;

            var agents = await db.Users
                .Include(u => u.Role)
                .Where(u => u.Status.ToLower() == "active" && u.Role.RoleName == "Agent")
                .ToListAsync();

            if (agents.Count == 0) return;

            var customers = await db.Customers.Where(c => !c.IsDeleted).ToListAsync();
            var leads = await db.Leads.Where(l => !l.IsDeleted).ToListAsync();
            var properties = await db.Properties.ToListAsync();

            if (customers.Count == 0 || properties.Count == 0) return;

            var callNotes = new[]
            {
                "Discussed spot cash discount and reservation terms for BGC condominium unit.",
                "Client requested sample amortization computation for 15-year bank financing.",
                "Conducted virtual tour walkthrough via Zoom for overseas buyer currently in Dubai.",
                "Followed up on appraisal inspection schedule with bank accredited surveyor.",
                "Confirmed receipt of proof of billing and two government-issued identification cards.",
                "Client inquired about dedicated parking slot availability and monthly dues rates.",
                "Discussed counter-offer terms submitted by seller for the Corinthian Gardens house.",
                "Client confirmed availability for Saturday ocular walkthrough inspection."
            };

            var emailNotes = new[]
            {
                "Transmitted official project e-brochure, floor layout diagrams, and inventory price list.",
                "Sent drafted Contract to Sell (CTS) with revised turnover schedule addendum.",
                "Emailed formal bank pre-qualification endorsement letter to accredited mortgage broker.",
                "Furnished certified true copies of Condominium Certificate of Title and Tax Declaration.",
                "Forwarded Homeowner Association rules and guidelines on construction renovation."
            };

            var meetingNotes = new[]
            {
                "In-person conference at developer showroom to inspect scale model and mock-up finishes.",
                "Contract review meeting and notarization of Deed of Absolute Sale.",
                "Orientation meeting with property manager regarding turnover clearance and move-in date.",
                "Client briefing on capital gains tax computation and Bureau of Internal Revenue clearances."
            };

            var showingNotes = new[]
            {
                "Conducted detailed ocular showing. Client commended high ceilings, corner natural lighting, and unobstructed view.",
                "Property viewing completed with buyer and family. Client requested second viewing with structural engineer.",
                "Showed prime commercial office unit. Client evaluated electrical capacity and server room provisions.",
                "Townhouse showing conducted. Client appreciated spacious 2-car garage and quiet gated community ambiance."
            };

            var rnd = new Random(808);
            var activitiesToAdd = new List<Activity>();
            var showingsToAdd = new List<(Activity Activity, Property Property, string Notes)>();

            int targetActivities = 150 - currentCount;
            DateTime now = DateTime.UtcNow;

            for (int i = 0; i < targetActivities; i++)
            {
                var agent = agents[i % agents.Count];
                int daysAgo = rnd.Next(1, 365);
                DateTime actDate = now.AddDays(-daysAgo).AddHours(-rnd.Next(1, 10));

                bool isCustomer = rnd.Next(100) < 60;
                int? custId = isCustomer ? customers[rnd.Next(customers.Count)].CustomerId : null;
                int? leadId = !isCustomer && leads.Count > 0 ? leads[rnd.Next(leads.Count)].LeadId : null;

                if (custId == null && leadId == null)
                {
                    custId = customers[0].CustomerId;
                }

                int typeRoll = rnd.Next(100);
                string type;
                CallOutcome? outcome = null;
                int? duration = null;
                string notes;

                if (typeRoll < 40) // Call
                {
                    type = "Call";
                    var outcomes = new[] { CallOutcome.Connected, CallOutcome.Connected, CallOutcome.Connected, CallOutcome.LeftVoicemail, CallOutcome.NoAnswer, CallOutcome.Busy };
                    outcome = outcomes[rnd.Next(outcomes.Length)];
                    duration = outcome == CallOutcome.Connected ? rnd.Next(5, 30) : rnd.Next(1, 3);
                    notes = callNotes[rnd.Next(callNotes.Length)];
                }
                else if (typeRoll < 65) // Email
                {
                    type = "Email";
                    notes = emailNotes[rnd.Next(emailNotes.Length)];
                }
                else if (typeRoll < 85) // Showing
                {
                    type = "Showing";
                    duration = rnd.Next(30, 75);
                    notes = showingNotes[rnd.Next(showingNotes.Length)];
                }
                else if (typeRoll < 95) // Meeting
                {
                    type = "Meeting";
                    duration = rnd.Next(30, 90);
                    notes = meetingNotes[rnd.Next(meetingNotes.Length)];
                }
                else // Note
                {
                    type = "Note";
                    notes = "Internal CRM update: Contact profile details and mortgage verification status reviewed.";
                }

                var activity = new Activity
                {
                    Type = type,
                    RelatedCustomerId = custId,
                    RelatedLeadId = leadId,
                    LoggedByAgentId = agent.UserId,
                    Outcome = outcome,
                    DurationMinutes = duration,
                    Notes = notes,
                    ActivityDate = actDate
                };

                activitiesToAdd.Add(activity);

                if (type == "Showing")
                {
                    var prop = properties[rnd.Next(properties.Count)];
                    showingsToAdd.Add((activity, prop, notes));
                }
            }

            db.Activities.AddRange(activitiesToAdd);
            await db.SaveChangesAsync();

            // Link PropertyShowingDetails
            if (showingsToAdd.Count > 0)
            {
                var showingDetails = new List<PropertyShowingDetail>();
                foreach (var item in showingsToAdd)
                {
                    showingDetails.Add(new PropertyShowingDetail
                    {
                        ActivityId = item.Activity.ActivityId,
                        PropertyId = item.Property.PropertyId,
                        ScheduledDate = item.Activity.ActivityDate,
                        FeedbackNotes = item.Notes
                    });
                }
                db.PropertyShowingDetails.AddRange(showingDetails);
                await db.SaveChangesAsync();
            }
        }

        public static async Task SeedFollowUpsAsync(RealEstateDbContext db, int tenantId = 1)
        {
            int currentCount = await db.TaskReminders.CountAsync(t => !t.IsDeleted);
            if (currentCount >= 50) return;

            var agents = await db.Users
                .Include(u => u.Role)
                .Where(u => u.Status.ToLower() == "active" && u.Role.RoleName == "Agent")
                .ToListAsync();

            if (agents.Count == 0) return;

            var customers = await db.Customers.Where(c => !c.IsDeleted).ToListAsync();
            var leads = await db.Leads.Where(l => !l.IsDeleted).ToListAsync();

            if (customers.Count == 0) return;

            var taskTemplates = new (string Title, string Type, string Priority, string Notes)[]
            {
                ("Call client to verify bank loan pre-approval status", "Call", "High", "Check if security appraisal was approved by BDO mortgage department."),
                ("Send revised Contract to Sell draft with parking addendum", "Email", "Medium", "Include revised turnover date per developer coordination."),
                ("Coordinate second ocular inspection walkthrough with buyer spouse", "Meeting", "High", "Bring floor plans and electrical layout diagrams."),
                ("Collect signed BIR Form 1904 and 2 valid IDs for CAR processing", "Email", "Critical", "Mandatory for Bureau of Internal Revenue submission."),
                ("Follow up on counter-proposal for Corinthian Gardens house", "Call", "High", "Verify if seller agreed to 35M revised purchase price."),
                ("Prepare formal reservation agreement and wire transfer instructions", "Email", "Medium", "Send official developer bank details for reservation fee."),
                ("Inspect bathroom punchlist rectification before turnover ceremony", "Inspection", "Medium", "Verify developer repaired hairline grout defect in master bath."),
                ("Review post-dated checks and amortization schedule with client", "Meeting", "Medium", "Ensure check numbers match Schedule A payment schedule."),
                ("Follow up on Homeowners Association membership clearance", "Call", "Low", "Confirm RFID vehicle stickers and gate pass application."),
                ("Conduct 30-day post-turnover customer satisfaction follow-up", "Call", "Low", "Ensure client has settled in and utility accounts are transferred."),
                ("Send Lamudi and Property24 links for new listing in BGC", "Email", "Low", "Share marketing syndication links with seller client."),
                ("Confirm Saturday 2:00 PM showroom walkthrough with client", "Call", "Medium", "Meet at Azure Residences sales lounge.")
            };

            var rnd = new Random(909);
            var tasksToAdd = new List<TaskReminder>();
            DateTime now = DateTime.UtcNow;

            int targetToSeed = 60 - currentCount;

            for (int i = 0; i < targetToSeed; i++)
            {
                var tpl = taskTemplates[i % taskTemplates.Length];
                var agent = agents[i % agents.Count];

                bool isCustomer = rnd.Next(100) < 65;
                int? custId = isCustomer ? customers[rnd.Next(customers.Count)].CustomerId : null;
                int? leadId = !isCustomer && leads.Count > 0 ? leads[rnd.Next(leads.Count)].LeadId : null;

                if (custId == null && leadId == null) custId = customers[0].CustomerId;

                // Status distribution: 30% Pending (today/upcoming), 25% Overdue, 45% Completed
                int roll = rnd.Next(100);
                string status;
                DateTime dueDate;
                DateTime createdAt;
                DateTime? completedAt = null;

                if (roll < 30) // Pending
                {
                    status = "Pending";
                    createdAt = now.AddDays(-rnd.Next(1, 10));
                    int daysAhead = rnd.Next(0, 10);
                    dueDate = now.Date.AddDays(daysAhead).AddHours(rnd.Next(9, 17));
                }
                else if (roll < 55) // Overdue
                {
                    status = "Overdue";
                    createdAt = now.AddDays(-rnd.Next(7, 20));
                    dueDate = now.AddDays(-rnd.Next(1, 6)).AddHours(rnd.Next(9, 17));
                }
                else // Completed
                {
                    status = "Completed";
                    createdAt = now.AddDays(-rnd.Next(15, 60));
                    dueDate = createdAt.AddDays(rnd.Next(2, 7));
                    completedAt = dueDate.AddHours(rnd.Next(1, 8));
                }

                tasksToAdd.Add(new TaskReminder
                {
                    Title = tpl.Title,
                    Type = tpl.Type,
                    Priority = tpl.Priority,
                    Notes = tpl.Notes,
                    Status = status,
                    DueDate = dueDate,
                    CreatedAt = createdAt,
                    CompletedAt = completedAt,
                    AssignedToUserId = agent.UserId,
                    RelatedCustomerId = custId,
                    RelatedLeadId = leadId,
                    IsDeleted = false
                });
            }

            db.TaskReminders.AddRange(tasksToAdd);
            await db.SaveChangesAsync();
        }

        public static async Task SeedCampaignsAsync(RealEstateDbContext db, int tenantId = 1)
        {
            int currentCount = await db.Campaigns.CountAsync();
            if (currentCount >= 8) return;

            var campaigns = new (string Name, string Channel, decimal Budget, int MonthsAgo, int DurationMonths)[]
            {
                ("Q1 2026 Metro Manila Luxury Living Expo", "Open House / Event", 150000m, 3, 4),
                ("BGC High-Street Premium Condos - Meta Ads", "Facebook Ad", 85000m, 5, 6),
                ("OFW Investment Roadshow - Dubai & SG", "Referral", 250000m, 8, 8),
                ("Cebu IT Park Pre-Selling Push", "Google Ads", 60000m, 4, 5),
                ("EDSA Guadalupe Digital Billboard Campaign", "Billboard / Outdoor", 350000m, 6, 6),
                ("Davao Grand Villas Phase 2 Grand Launch", "Walk-in", 120000m, 7, 5),
                ("Property24 & Lamudi Featured Portals", "Property Portal", 95000m, 9, 10),
                ("First-Time Homeowner Search Network", "Google Ads", 45000m, 3, 4),
                ("Nuvali Eco-Living Community Weekend", "Open House / Event", 75000m, 2, 3),
                ("VIP Client Referral & Loyalty Program", "Referral", 100000m, 12, 12)
            };

            var toAdd = new List<Campaign>();
            DateTime now = DateTime.UtcNow;

            foreach (var c in campaigns)
            {
                var startDate = now.AddMonths(-c.MonthsAgo);
                var endDate = startDate.AddMonths(c.DurationMonths);

                toAdd.Add(new Campaign
                {
                    TenantId = tenantId,
                    Name = c.Name,
                    Channel = c.Channel,
                    Status = endDate > now ? "Active" : "Completed",
                    Budget = c.Budget,
                    StartDate = startDate,
                    EndDate = endDate,
                    IsActive = true,
                    CreatedAt = startDate
                });
            }

            db.Campaigns.AddRange(toAdd);
            await db.SaveChangesAsync();
        }

        public static async Task SeedNotificationsAsync(RealEstateDbContext db, int tenantId = 1)
        {
            int currentCount = await db.Notifications.CountAsync();
            if (currentCount >= 10) return;

            var users = await db.Users.Include(u => u.Role).Where(u => u.Status.ToLower() == "active").ToListAsync();
            if (users.Count == 0) return;

            var notifications = new (NotificationType Type, string Title, string Msg, string Entity, bool IsRead)[]
            {
                (NotificationType.LeadAssigned, "New Lead Assigned", "You have been assigned new qualified lead Ramon Valderama.", "Lead", false),
                (NotificationType.DealStageChanged, "Deal Advanced to Contract", "DEAL-0012 for Unit 1204 One Serendra has moved to Contract stage.", "Deal", false),
                (NotificationType.FollowUpDueSoon, "Follow-Up Due Today", "Reminder: Call Maria Santos regarding bank loan pre-approval status.", "TaskReminder", false),
                (NotificationType.TicketAssigned, "New Support Ticket Assigned", "Ticket TICK-1005: Title Transfer status update inquiry assigned to you.", "SupportTicket", false),
                (NotificationType.DealClosed, "Deal Closed & Won", "Congratulations! DEAL-0045 has been officially closed and recorded.", "Deal", true),
                (NotificationType.PropertyStatusChanged, "Property Reserved", "Azure Condominium Unit 1502 status updated from Available to Reserved.", "Property", true),
                (NotificationType.CustomerAssigned, "Customer Assigned", "Customer Juan Dela Cruz has been assigned to your sales portfolio.", "Customer", true),
                (NotificationType.TicketStatusChanged, "Ticket Resolved", "Support Ticket TICK-1002 has been marked Resolved by administration.", "SupportTicket", true)
            };

            var toAdd = new List<Notification>();
            var rnd = new Random(111);
            DateTime now = DateTime.UtcNow;

            foreach (var user in users)
            {
                for (int i = 0; i < 3; i++)
                {
                    var n = notifications[rnd.Next(notifications.Length)];
                    int hoursAgo = rnd.Next(1, 72);
                    var createdAt = now.AddHours(-hoursAgo);

                    toAdd.Add(new Notification
                    {
                        TenantId = tenantId,
                        RecipientUserId = user.UserId,
                        Type = n.Type,
                        Title = n.Title,
                        Message = n.Msg,
                        RelatedEntityType = n.Entity,
                        IsRead = n.IsRead,
                        CreatedAt = createdAt,
                        ReadAt = n.IsRead ? createdAt.AddMinutes(30) : null
                    });
                }

                // Ensure default notification preferences exist
                foreach (NotificationType nType in Enum.GetValues(typeof(NotificationType)))
                {
                    if (!await db.NotificationPreferences.AnyAsync(p => p.UserId == user.UserId && p.Type == nType))
                    {
                        db.NotificationPreferences.Add(new NotificationPreference
                        {
                            TenantId = tenantId,
                            UserId = user.UserId,
                            Type = nType,
                            IsEnabled = true
                        });
                    }
                }
            }

            db.Notifications.AddRange(toAdd);
            await db.SaveChangesAsync();
        }
    }
}