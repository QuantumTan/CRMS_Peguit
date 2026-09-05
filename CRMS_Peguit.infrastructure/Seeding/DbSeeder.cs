using CRMS_Peguit.domain.entities;
using CRMS_Peguit.domain.Entities;
using CRMS_Peguit.infrastructure.data;
using CRMS_Peguit.infrastructure.Security;
using Microsoft.EntityFrameworkCore;

namespace CRMS_Peguit.infrastructure.Seeding
{
    // Run this once (e.g. from a temporary button in the app, or a small
    // console runner) to get test accounts into the database. Delete or
    // guard this before shipping - it's for local/dev testing only.
    public static class DbSeeder
    {
        public static async Task SeedTestUsersAsync(RealEstateDbContext db, int tenantId = 1)
        {
            if (await db.Roles.AnyAsync(r => r.TenantId == tenantId))
                return; // already seeded

            var adminRole = new Role { TenantId = tenantId, RoleName = "Admin" };
            var managerRole = new Role { TenantId = tenantId, RoleName = "Manager" };
            var agentRole = new Role { TenantId = tenantId, RoleName = "Agent" };

            db.Roles.AddRange(adminRole, managerRole, agentRole);
            await db.SaveChangesAsync(); // so RoleId values are generated before use

            var users = new[]
            {
                new User
                {
                    TenantId = tenantId,
                    FullName = "Test Admin",
                    Email = "admin@test.com",
                    PasswordHash = PasswordHasher.Hash("Admin123!"),
                    RoleId = adminRole.RoleId,
                    Status = "Active",
                    CreatedAt = DateTime.UtcNow
                },
                new User
                {
                    TenantId = tenantId,
                    FullName = "Test Manager",
                    Email = "manager@test.com",
                    PasswordHash = PasswordHasher.Hash("Manager123!"),
                    RoleId = managerRole.RoleId,
                    Status = "Active",
                    CreatedAt = DateTime.UtcNow
                },
                new User
                {
                    TenantId = tenantId,
                    FullName = "Test Agent",
                    Email = "agent@test.com",
                    PasswordHash = PasswordHasher.Hash("Agent123!"),
                    RoleId = agentRole.RoleId,
                    Status = "Active",
                    CreatedAt = DateTime.UtcNow
                }
            };

            db.Users.AddRange(users);
            await db.SaveChangesAsync();
        }
    }
}