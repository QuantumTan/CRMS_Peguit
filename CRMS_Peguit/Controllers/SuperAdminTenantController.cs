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
using CRMS_Peguit.winforms.Services;

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
                .Include(c => c.Branding)
                .AsNoTracking()
                .ToListAsync()
                .ConfigureAwait(false);

            foreach (var company in companies)
            {
                var dto = new TenantGridDto
                {
                    CompanyId = company.CompanyId,
                    CompanyCode = company.CompanyCode,
                    CompanyName = company.CompanyName,
                    DisplayName = company.Branding?.DisplayName ?? company.CompanyName,
                    Status = company.IsActive ? "Active" : "Suspended",
                    CreatedAt = company.CreatedAt
                };

                var currentSub = company.Subscriptions.OrderByDescending(s => s.EndDate).FirstOrDefault();
                dto.TierLevel = currentSub?.PlanName ?? "Tenant A";

                // Strictly accessing Users, Roles, Persons ONLY - NO CRM DATA.
                try
                {
                    using var tenantDb = LocalDb.CreateContext(company.CompanyId);
                    dto.UserCount = await tenantDb.Users.AsNoTracking().CountAsync().ConfigureAwait(false);
                    var adminUser = await tenantDb.Users
                        .Include(u => u.Role)
                        .AsNoTracking()
                        .Where(u => u.Role != null && (u.Role.RoleName == "Admin" || u.Role.RoleName == "SuperAdmin"))
                        .FirstOrDefaultAsync()
                        .ConfigureAwait(false);

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

                string displayName = !string.IsNullOrWhiteSpace(req.DisplayName)
                    ? req.DisplayName.Trim()
                    : req.CompanyName.Trim();

                var branding = new TenantBranding
                {
                    CompanyId = company.CompanyId,
                    DisplayName = displayName,
                    LogoVersion = 1,
                    HidePoweredBy = req.TierLevel.Contains("Tenant C"),
                    AccentColor = req.TierLevel.Contains("Tenant C") ? "#0284C7" : null,
                    UpdatedAt = DateTime.UtcNow,
                    UpdatedByUserId = CurrentSession.UserId > 0 ? CurrentSession.UserId : 1
                };
                masterDb.TenantBrandings.Add(branding);

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

        public async Task<TenantBrandingDto?> GetTenantBrandingAsync(int companyId)
        {
            try
            {
                using var masterDb = LocalDb.CreateMasterContext();
                var company = await masterDb.Companies
                    .Include(c => c.Branding)
                    .Include(c => c.Subscriptions)
                    .AsNoTracking()
                    .FirstOrDefaultAsync(c => c.CompanyId == companyId);

                if (company == null) return null;

                var sub = company.Subscriptions.OrderByDescending(s => s.EndDate).FirstOrDefault();
                var tier = sub?.Tier ?? TenantTier.TenantA;
                var branding = company.Branding;

                string currentDisplayName = branding?.DisplayName ?? company.CompanyName;
                bool isDuplicate = await masterDb.TenantBrandings
                    .AnyAsync(b => b.CompanyId != companyId && b.DisplayName.ToLower() == currentDisplayName.ToLower());

                return new TenantBrandingDto
                {
                    CompanyId = company.CompanyId,
                    CompanyName = company.CompanyName,
                    DisplayName = currentDisplayName,
                    HasCustomLogo = branding?.LogoImage != null && branding.LogoImage.Length > 0,
                    LogoVersion = branding?.LogoVersion ?? 1,
                    AccentColor = branding?.AccentColor,
                    ContactEmail = branding?.ContactEmail,
                    ContactPhone = branding?.ContactPhone,
                    Address = branding?.Address,
                    HidePoweredBy = branding?.HidePoweredBy ?? false,
                    CanCustomizeAccent = FeatureGate.CanUseAccentColor(tier),
                    CanHidePoweredBy = FeatureGate.CanHidePoweredBy(tier),
                    IsDuplicateName = isDuplicate,
                    UpdatedAt = branding?.UpdatedAt ?? company.CreatedAt
                };
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"GetTenantBrandingAsync error: {ex.Message}");
                return null;
            }
        }

        public async Task<byte[]?> GetTenantLogoAsync(int companyId)
        {
            try
            {
                using var masterDb = LocalDb.CreateMasterContext();
                var branding = await masterDb.TenantBrandings
                    .AsNoTracking()
                    .FirstOrDefaultAsync(b => b.CompanyId == companyId);
                return branding?.LogoImage;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"GetTenantLogoAsync error: {ex.Message}");
                return null;
            }
        }

        public async Task<(bool Success, string? Error)> UpdateTenantBrandingAsync(
            int companyId, 
            UpdateBrandingRequest req, 
            bool overrideReserved = false,
            byte[]? logoBytes = null,
            bool logoChanged = false)
        {
            var validator = new UpdateBrandingRequestValidator(allowReservedOverride: overrideReserved);
            var validation = await validator.ValidateAsync(req);
            if (!validation.IsValid)
            {
                return (false, validation.Errors.FirstOrDefault()?.ErrorMessage ?? "Validation failed.");
            }

            try
            {
                using var masterDb = LocalDb.CreateMasterContext();
                var company = await masterDb.Companies
                    .Include(c => c.Branding)
                    .Include(c => c.Subscriptions)
                    .FirstOrDefaultAsync(c => c.CompanyId == companyId);

                if (company == null) return (false, "Tenant not found.");

                var sub = company.Subscriptions.OrderByDescending(s => s.EndDate).FirstOrDefault();
                var tier = sub?.Tier ?? TenantTier.TenantA;

                if (!string.IsNullOrWhiteSpace(req.AccentColor) && !FeatureGate.CanUseAccentColor(tier))
                {
                    return (false, "Accent color customization is an Enterprise tier feature.");
                }

                if (req.HidePoweredBy && !FeatureGate.CanHidePoweredBy(tier))
                {
                    return (false, "Hiding the platform footer is an Enterprise tier feature.");
                }

                var branding = company.Branding;
                if (branding == null)
                {
                    branding = new TenantBranding
                    {
                        CompanyId = companyId,
                        DisplayName = req.DisplayName.Trim(),
                        LogoImage = logoChanged ? logoBytes : null,
                        LogoVersion = 1,
                        UpdatedAt = DateTime.UtcNow
                    };
                    masterDb.TenantBrandings.Add(branding);
                }
                else
                {
                    branding.DisplayName = req.DisplayName.Trim();
                    branding.AccentColor = FeatureGate.CanUseAccentColor(tier) ? req.AccentColor?.Trim() : null;
                    branding.HidePoweredBy = FeatureGate.CanHidePoweredBy(tier) && req.HidePoweredBy;
                    branding.ContactEmail = string.IsNullOrWhiteSpace(req.ContactEmail) ? null : req.ContactEmail.Trim();
                    branding.ContactPhone = string.IsNullOrWhiteSpace(req.ContactPhone) ? null : req.ContactPhone.Trim();
                    branding.Address = string.IsNullOrWhiteSpace(req.Address) ? null : req.Address.Trim();
                    branding.UpdatedAt = DateTime.UtcNow;
                    branding.UpdatedByUserId = CurrentSession.UserId > 0 ? CurrentSession.UserId : 1;

                    if (logoChanged)
                    {
                        branding.LogoImage = logoBytes;
                        branding.LogoVersion++;
                    }
                }

                await masterDb.SaveChangesAsync();

                await WriteAuditLogAsync(masterDb, "TenantBrandingUpdated",
                    $"Branding updated for {company.CompanyName}: DisplayName='{branding.DisplayName}', LogoVersion={branding.LogoVersion}, HidePoweredBy={branding.HidePoweredBy}",
                    company.CompanyId, company.CompanyName);

                if (CurrentSession.TenantId == companyId)
                {
                    BrandingService.ApplyDto(new TenantBrandingDto
                    {
                        CompanyId = companyId,
                        CompanyName = company.CompanyName,
                        DisplayName = branding.DisplayName,
                        HasCustomLogo = branding.LogoImage != null && branding.LogoImage.Length > 0,
                        LogoVersion = branding.LogoVersion,
                        AccentColor = null,
                        ContactEmail = branding.ContactEmail,
                        ContactPhone = branding.ContactPhone,
                        Address = branding.Address,
                        HidePoweredBy = branding.HidePoweredBy,
                        CanCustomizeAccent = false,
                        CanHidePoweredBy = FeatureGate.CanHidePoweredBy(tier),
                        UpdatedAt = branding.UpdatedAt
                    }, branding.LogoImage);
                }

                return (true, null);
            }
            catch (Exception ex)
            {
                return (false, ex.Message);
            }
        }

        public async Task<(bool Success, string? Error)> ResetTenantBrandingAsync(int companyId)
        {
            try
            {
                using var masterDb = LocalDb.CreateMasterContext();
                var company = await masterDb.Companies
                    .Include(c => c.Branding)
                    .FirstOrDefaultAsync(c => c.CompanyId == companyId);

                if (company == null) return (false, "Tenant not found.");

                var branding = company.Branding;
                if (branding != null)
                {
                    branding.DisplayName = company.CompanyName;
                    branding.LogoImage = null;
                    branding.LogoVersion++;
                    branding.AccentColor = null;
                    branding.HidePoweredBy = false;
                    branding.ContactEmail = null;
                    branding.ContactPhone = null;
                    branding.Address = null;
                    branding.UpdatedAt = DateTime.UtcNow;
                    branding.UpdatedByUserId = CurrentSession.UserId > 0 ? CurrentSession.UserId : 1;
                }
                else
                {
                    branding = new TenantBranding
                    {
                        CompanyId = companyId,
                        DisplayName = company.CompanyName,
                        LogoVersion = 1,
                        UpdatedAt = DateTime.UtcNow
                    };
                    masterDb.TenantBrandings.Add(branding);
                }

                await masterDb.SaveChangesAsync();

                await WriteAuditLogAsync(masterDb, "TenantBrandingReset",
                    $"Branding reset to platform defaults for {company.CompanyName}",
                    company.CompanyId, company.CompanyName);

                return (true, null);
            }
            catch (Exception ex)
            {
                return (false, ex.Message);
            }
        }
    }
}
