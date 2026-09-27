using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using CRMS_Peguit.domain.Common;
using CRMS_Peguit.domain.entities;
using CRMS_Peguit.infrastructure.data;
using CRMS_Peguit.winforms.Models.Services;
using CRMS_Peguit.winforms.Auth;

namespace CRMS_Peguit.winforms.Controllers
{
    /// <summary>
    /// NEXA Super Admin Controller for Tenant Management.
    /// STRICT DATA BOUNDARY ENFORCED: Never accesses operational CRM tables.
    /// Accesses ONLY MasterCrmsDbContext (Companies, Subscriptions, CompanyDatabases, PlatformAuditLogs)
    /// and RealEstateDbContext metadata tables (Users, Roles, Persons).
    /// </summary>
    public class SuperAdminTenantController
    {
        public async Task<List<TenantGridDto>> GetTenantsAsync()
        {
            var results = new List<TenantGridDto>();
            using var masterDb = LocalDb.CreateMasterContext();
            var companies = await masterDb.Companies
                .Include(c => c.Subscriptions)
                .AsNoTracking()
                .ToListAsync();

            foreach (var company in companies)
            {
                var dto = new TenantGridDto
                {
                    CompanyId = company.CompanyId,
                    CompanyCode = company.CompanyCode,
                    CompanyName = company.CompanyName,
                    Status = company.IsActive ? "Active" : "Suspended",
                    CreatedAt = company.CreatedAt
                };

                var currentSub = company.Subscriptions.OrderByDescending(s => s.EndDate).FirstOrDefault();
                dto.TierLevel = currentSub?.PlanName ?? "Tenant A";

                // Strictly accessing Users, Roles, Persons ONLY - NO CRM DATA.
                try
                {
                    using var tenantDb = LocalDb.CreateContext(company.CompanyId);
                    dto.UserCount = await tenantDb.Users.CountAsync();
                    var adminUser = await tenantDb.Users
                        .Include(u => u.Role)
                        .Where(u => u.Role != null && (u.Role.RoleName == "Admin" || u.Role.RoleName == "SuperAdmin"))
                        .FirstOrDefaultAsync();

                    dto.PrimaryAdminName = !string.IsNullOrWhiteSpace(adminUser?.FullName)
                        ? adminUser.FullName
                        : "Admin";
                }
                catch
                {
                    dto.PrimaryAdminName = "Not Configured";
                    dto.UserCount = 0;
                }

                results.Add(dto);
            }

            return results;
        }

        public async Task<(bool Success, string Error)> CreateTenantAsync(CreateTenantRequest req)
        {
            try
            {
                using var masterDb = LocalDb.CreateMasterContext();

                var existing = await masterDb.Companies.FirstOrDefaultAsync(c => c.CompanyCode == req.CompanyCode);
                if (existing != null)
                {
                    return (false, "Company code already exists.");
                }

                var company = new Company
                {
                    CompanyCode = req.CompanyCode.Trim().ToUpperInvariant(),
                    CompanyName = req.CompanyName.Trim(),
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow
                };

                masterDb.Companies.Add(company);
                await masterDb.SaveChangesAsync();

                decimal billingAmount = req.TierLevel switch
                {
                    "Tenant C" => 15000m,
                    "Tenant B" => 7500m,
                    _ => 2500m
                };

                var subscription = new Subscription
                {
                    CompanyId = company.CompanyId,
                    PlanName = string.IsNullOrWhiteSpace(req.TierLevel) ? "Tenant A" : req.TierLevel,
                    StartDate = DateTime.UtcNow,
                    EndDate = DateTime.UtcNow.AddYears(1),
                    BillingAmount = billingAmount,
                    Status = "Active"
                };

                masterDb.Subscriptions.Add(subscription);

                var companyDb = new CompanyDatabase
                {
                    CompanyId = company.CompanyId,
                    DatabaseName = $"CRMS_Tenant_{company.CompanyId}",
                    ServerName = "(localdb)\\mssqllocaldb",
                    CredentialKey = company.CompanyCode,
                    IsActive = true
                };
                masterDb.CompanyDatabases.Add(companyDb);

                await masterDb.SaveChangesAsync();

                // Initialize tenant admin in tenant DB
                try
                {
                    using var tenantDb = LocalDb.CreateContext(company.CompanyId);
                    var adminRole = await tenantDb.Roles.FirstOrDefaultAsync(r => r.RoleName == "Admin");
                    if (adminRole == null)
                    {
                        adminRole = new Role { RoleName = "Admin", TenantId = company.CompanyId };
                        tenantDb.Roles.Add(adminRole);
                        await tenantDb.SaveChangesAsync();
                    }

                    var user = new User
                    {
                        FirstName = req.AdminFirstName.Trim(),
                        LastName = req.AdminLastName.Trim(),
                        Email = req.AdminEmail.Trim(),
                        PasswordHash = HashPassword(req.AdminPassword),
                        RoleId = adminRole.RoleId,
                        Status = "Active",
                        CreatedAt = DateTime.UtcNow
                    };
                    tenantDb.Users.Add(user);
                    await tenantDb.SaveChangesAsync();
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Tenant DB admin creation error: {ex.Message}");
                }

                await WriteAuditLogAsync(masterDb, "TenantCreated",
                    $"Company: {company.CompanyName} ({company.CompanyCode}) created with tier {subscription.PlanName} and admin {req.AdminEmail}",
                    company.CompanyId, company.CompanyName);

                return (true, string.Empty);
            }
            catch (Exception ex)
            {
                return (false, ex.Message);
            }
        }

        public async Task<bool> SuspendTenantAsync(int companyId)
        {
            try
            {
                using var masterDb = LocalDb.CreateMasterContext();
                var company = await masterDb.Companies.FindAsync(companyId);
                if (company == null) return false;

                company.IsActive = false;
                await masterDb.SaveChangesAsync();

                try
                {
                    using var tenantDb = LocalDb.CreateContext(companyId);
                    var users = await tenantDb.Users.ToListAsync();
                    foreach (var user in users)
                    {
                        user.Status = "Suspended";
                    }
                    await tenantDb.SaveChangesAsync();
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"User suspension error: {ex.Message}");
                }

                await WriteAuditLogAsync(masterDb, "TenantSuspended",
                    $"Company: {company.CompanyName} (ID: {companyId}) suspended. User logins blocked.",
                    company.CompanyId, company.CompanyName);

                return true;
            }
            catch
            {
                return false;
            }
        }

        public async Task<bool> ReactivateTenantAsync(int companyId)
        {
            try
            {
                using var masterDb = LocalDb.CreateMasterContext();
                var company = await masterDb.Companies.FindAsync(companyId);
                if (company == null) return false;

                company.IsActive = true;
                await masterDb.SaveChangesAsync();

                try
                {
                    using var tenantDb = LocalDb.CreateContext(companyId);
                    var users = await tenantDb.Users.Where(u => u.Status == "Suspended").ToListAsync();
                    foreach (var user in users)
                    {
                        user.Status = "Active";
                    }
                    await tenantDb.SaveChangesAsync();
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"User reactivation error: {ex.Message}");
                }

                await WriteAuditLogAsync(masterDb, "TenantReactivated",
                    $"Company: {company.CompanyName} (ID: {companyId}) reactivated. User logins restored.",
                    company.CompanyId, company.CompanyName);

                return true;
            }
            catch
            {
                return false;
            }
        }

        public static async Task WriteAuditLogAsync(MasterCrmsDbContext masterDb, string actionType, string detail, int? companyId = null, string? companyName = null)
        {
            masterDb.PlatformAuditLogs.Add(new PlatformAuditLog
            {
                PerformedBySuperAdminId = CurrentSession.UserId > 0 ? CurrentSession.UserId : 1,
                PerformedByName = CurrentSession.CurrentUser?.FullName ?? "Super Admin",
                ActionType = actionType,
                Detail = detail,
                TargetCompanyId = companyId,
                TargetCompanyName = companyName,
                CreatedAt = DateTime.UtcNow
            });
            await masterDb.SaveChangesAsync();
        }

        private static string HashPassword(string password)
        {
            using var sha = System.Security.Cryptography.SHA256.Create();
            var bytes = sha.ComputeHash(System.Text.Encoding.UTF8.GetBytes(password));
            return Convert.ToBase64String(bytes);
        }
    }
}
