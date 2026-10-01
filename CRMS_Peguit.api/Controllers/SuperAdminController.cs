using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using CRMS_Peguit.domain.entities;
using CRMS_Peguit.domain.Common;
using CRMS_Peguit.infrastructure.data;
using CRMS_Peguit.infrastructure.Services;

using CRMS_Peguit.infrastructure.Security;

namespace CRMS_Peguit.api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class SuperAdminController : ControllerBase
    {
        private readonly MasterCrmsDbContext _masterDb;
        private readonly RealEstateDbContext _mgmtDb;
        private readonly ITenantDbContextFactory _tenantFactory;

        public SuperAdminController(MasterCrmsDbContext masterDb, RealEstateDbContext mgmtDb, ITenantDbContextFactory tenantFactory)
        {
            _masterDb = masterDb;
            _mgmtDb = mgmtDb;
            _tenantFactory = tenantFactory;
        }

        private bool ValidateSuperAdmin()
        {
            return ApiSecurityHelper.IsSuperAdmin(User);
        }

        [HttpGet("snapshot")]
        public async Task<ActionResult<PlatformSnapshotDto>> GetPlatformSnapshot()
        {
            if (!ValidateSuperAdmin()) return Forbid();

            var snap = new PlatformSnapshotDto();
            try
            {
                var companies = await _masterDb.Companies
                    .Include(c => c.Subscriptions)
                    .AsNoTracking()
                    .ToListAsync();

                snap.TotalTenants = companies.Count;
                snap.ActiveTenants = companies.Count(c => c.IsActive);

                var allSubs = companies.SelectMany(c => c.Subscriptions).ToList();
                var now = DateTime.UtcNow;
                var monthEnd = now.AddDays(30);

                snap.ActiveSubscriptions = allSubs.Count(s => s.Status.Equals("Active", StringComparison.OrdinalIgnoreCase));
                snap.ExpiringThisMonth = allSubs.Count(s =>
                    s.Status.Equals("Active", StringComparison.OrdinalIgnoreCase) &&
                    s.EndDate.HasValue &&
                    s.EndDate.Value <= monthEnd &&
                    s.EndDate.Value >= now);

                snap.ExpiredSubscriptions = allSubs.Count(s =>
                    s.Status.Equals("Expired", StringComparison.OrdinalIgnoreCase) ||
                    (s.EndDate.HasValue && s.EndDate.Value < now));

                snap.TotalMrr = allSubs
                    .Where(s => s.Status.Equals("Active", StringComparison.OrdinalIgnoreCase))
                    .Sum(s => s.BillingAmount);

                snap.TenantACount = allSubs.Count(s => s.PlanName.Contains("Tenant A", StringComparison.OrdinalIgnoreCase));
                snap.TenantBCount = allSubs.Count(s => s.PlanName.Contains("Tenant B", StringComparison.OrdinalIgnoreCase));
                snap.TenantCCount = allSubs.Count(s => s.PlanName.Contains("Tenant C", StringComparison.OrdinalIgnoreCase));
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[SuperAdminController.GetPlatformSnapshot] MasterDb error: {ex.Message}");
            }

            try
            {
                var lastBackup = await _mgmtDb.BackupLogs
                    .OrderByDescending(b => b.BackupDate)
                    .AsNoTracking()
                    .FirstOrDefaultAsync();

                if (lastBackup != null)
                {
                    snap.LastBackupStatus = lastBackup.Status;
                    snap.LastBackupDate = lastBackup.BackupDate;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[SuperAdminController.GetPlatformSnapshot] BackupLog error: {ex.Message}");
            }

            return Ok(snap);
        }

        [HttpGet("administrators")]
        public async Task<ActionResult<List<AdminDto>>> GetAdministrators()
        {
            if (!ValidateSuperAdmin()) return Forbid();

            var result = new List<AdminDto>();
            Dictionary<int, (string Code, string Name)> companyMap;
            try
            {
                companyMap = await _masterDb.Companies
                    .AsNoTracking()
                    .ToDictionaryAsync(c => c.CompanyId, c => (c.CompanyCode, c.CompanyName));
            }
            catch
            {
                companyMap = new Dictionary<int, (string, string)>();
            }

            int[] knownTenantIds = companyMap.Keys.Count > 0
                ? companyMap.Keys.OrderBy(k => k).ToArray()
                : new[] { 1, 2, 3 };

            foreach (var tid in knownTenantIds)
            {
                try
                {
                    await using var tenantDb = await _tenantFactory.CreateAsync(tid);
                    var adminUsers = await tenantDb.Users
                        .Include(u => u.Role)
                        .Where(u => u.Role.RoleName == "Admin" ||
                                    u.Role.RoleName == "SuperAdmin" ||
                                    u.Role.RoleName == "Manager")
                        .AsNoTracking()
                        .ToListAsync();

                    companyMap.TryGetValue(tid, out var companyInfo);

                    foreach (var u in adminUsers)
                    {
                        string safeName = !string.IsNullOrWhiteSpace(u.FullName)
                            ? u.FullName
                            : (!string.IsNullOrWhiteSpace(u.Email) ? u.Email : $"Admin #{u.UserId}");

                        result.Add(new AdminDto
                        {
                            UserId = u.UserId,
                            TenantId = tid,
                            FullName = safeName,
                            Email = u.Email,
                            RoleName = u.Role?.RoleName ?? "Admin",
                            CompanyName = companyInfo.Name ?? $"Tenant #{tid}",
                            CompanyCode = companyInfo.Code ?? $"T{tid:D2}",
                            Status = string.IsNullOrWhiteSpace(u.Status) ? "Active" : u.Status
                        });
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"[GetAdministrators] Tenant {tid} error: {ex.Message}");
                }
            }

            return Ok(result);
        }

        [HttpPost("administrators")]
        public async Task<ActionResult> CreateAdmin([FromBody] CreateAdminRequest req)
        {
            if (!ValidateSuperAdmin()) return Forbid();

            if (req == null)
                return BadRequest(new { message = "Request body is required." });

            if (req.TenantId <= 0)
                return BadRequest(new { message = "A valid TenantId is required." });

            if (string.IsNullOrWhiteSpace(req.Email))
                return BadRequest(new { message = "Email is required." });

            if (string.IsNullOrWhiteSpace(req.Password) || req.Password.Length < 6)
                return BadRequest(new { message = "Password must be at least 6 characters." });

            try
            {
                await using var db = await _tenantFactory.CreateAsync(req.TenantId);

                var existingUser = await db.Users.AsNoTracking().AnyAsync(u => u.Email == req.Email);
                if (existingUser)
                    return BadRequest(new { message = "An account with this email already exists in this tenant." });

                var role = await db.Roles.AsNoTracking().FirstOrDefaultAsync(r => r.RoleName == req.RoleName);
                if (role == null && string.IsNullOrWhiteSpace(req.RoleName))
                {
                    role = await db.Roles.AsNoTracking().FirstOrDefaultAsync(r => r.RoleName == "Admin");
                }
                if (role == null)
                    return BadRequest(new { message = $"Role '{req.RoleName}' not found in tenant {req.TenantId}." });

                var user = new User
                {
                    FirstName = req.FirstName,
                    LastName = req.LastName,
                    Email = req.Email,
                    RoleId = role.RoleId,
                    PasswordHash = PasswordHasher.Hash(req.Password),
                    Status = "Active",
                    CreatedAt = DateTime.UtcNow
                };
                db.Users.Add(user);
                await db.SaveChangesAsync();

                return Ok(new { success = true });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = ex.Message });
            }
        }

        [HttpPost("administrators/{userId}/deactivate")]
        public async Task<ActionResult> DeactivateAdmin(int userId, [FromQuery] int tenantId)
        {
            if (!ValidateSuperAdmin()) return Forbid();

            try
            {
                await using var db = await _tenantFactory.CreateAsync(tenantId);
                var user = await db.Users.FirstOrDefaultAsync(u => u.UserId == userId);
                if (user == null) return NotFound();

                user.Status = "Inactive";
                await db.SaveChangesAsync();
                return Ok(new { success = true });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = ex.Message });
            }
        }

        [HttpPost("administrators/{userId}/activate")]
        public async Task<ActionResult> ActivateAdmin(int userId, [FromQuery] int tenantId)
        {
            if (!ValidateSuperAdmin()) return Forbid();

            try
            {
                await using var db = await _tenantFactory.CreateAsync(tenantId);
                var user = await db.Users.FirstOrDefaultAsync(u => u.UserId == userId);
                if (user == null) return NotFound();

                user.Status = "Active";
                await db.SaveChangesAsync();
                return Ok(new { success = true });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = ex.Message });
            }
        }

        [HttpGet("companies")]
        public async Task<ActionResult<List<CompanyLookupDto>>> GetCompanies()
        {
            if (!ValidateSuperAdmin()) return Forbid();

            var companies = await _masterDb.Companies
                .AsNoTracking()
                .OrderBy(c => c.CompanyName)
                .Select(c => new CompanyLookupDto
                {
                    CompanyId = c.CompanyId,
                    CompanyName = c.CompanyName,
                    CompanyCode = c.CompanyCode
                })
                .ToListAsync();

            return Ok(companies);
        }

        [HttpGet("companies/{companyId}")]
        public async Task<ActionResult<CompanyDetailDto>> GetCompanyDetail(int companyId)
        {
            if (!ValidateSuperAdmin()) return Forbid();

            var company = await _masterDb.Companies
                .Include(c => c.Subscriptions)
                .AsNoTracking()
                .FirstOrDefaultAsync(c => c.CompanyId == companyId);

            if (company == null) return NotFound();

            var sub = company.Subscriptions.OrderByDescending(s => s.StartDate).FirstOrDefault();
            return Ok(new CompanyDetailDto
            {
                CompanyId = company.CompanyId,
                CompanyCode = company.CompanyCode,
                CompanyName = company.CompanyName,
                IsActive = company.IsActive,
                CreatedAt = company.CreatedAt,
                PlanName = sub?.PlanName ?? "—",
                SubscriptionStatus = sub?.Status ?? "—",
                BillingAmount = sub?.BillingAmount ?? 0m,
                SubscriptionEndDate = sub?.EndDate
            });
        }

        [HttpGet("settings")]
        public async Task<ActionResult<List<SystemSettingDto>>> GetSystemSettings()
        {
            if (!ValidateSuperAdmin()) return Forbid();

            var result = new List<SystemSettingDto>();
            try
            {
                var settings = await _mgmtDb.SystemSettings
                    .Include(s => s.UpdatedByUser)
                    .AsNoTracking()
                    .OrderBy(s => s.SettingKey)
                    .ToListAsync();

                foreach (var s in settings)
                {
                    result.Add(new SystemSettingDto
                    {
                        SettingId = s.SettingId,
                        SettingKey = s.SettingKey,
                        SettingValue = s.SettingValue,
                        UpdatedAt = s.UpdatedAt,
                        UpdatedByName = s.UpdatedByUser?.FullName ?? "System"
                    });
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[GetSystemSettings] Error: {ex.Message}");
            }

            if (result.Count == 0)
            {
                result.AddRange(new[]
                {
                    new SystemSettingDto { SettingId = 1, SettingKey = "PasswordMinLength", SettingValue = "8", UpdatedAt = DateTime.UtcNow, UpdatedByName = "System" },
                    new SystemSettingDto { SettingId = 2, SettingKey = "SessionTimeoutMinutes", SettingValue = "60", UpdatedAt = DateTime.UtcNow, UpdatedByName = "System" },
                    new SystemSettingDto { SettingId = 3, SettingKey = "DefaultSubscriptionTerm", SettingValue = "12", UpdatedAt = DateTime.UtcNow, UpdatedByName = "System" },
                    new SystemSettingDto { SettingId = 4, SettingKey = "MaxLoginAttempts", SettingValue = "5", UpdatedAt = DateTime.UtcNow, UpdatedByName = "System" },
                    new SystemSettingDto { SettingId = 5, SettingKey = "BackupRetentionDays", SettingValue = "30", UpdatedAt = DateTime.UtcNow, UpdatedByName = "System" },
                    new SystemSettingDto { SettingId = 6, SettingKey = "MaintenanceModeEnabled", SettingValue = "false", UpdatedAt = DateTime.UtcNow, UpdatedByName = "System" }
                });
            }

            return Ok(result);
        }

        [HttpPut("settings/{settingId}")]
        public async Task<ActionResult> UpdateSystemSetting(int settingId, [FromBody] UpdateSettingRequest req)
        {
            if (!ValidateSuperAdmin()) return Forbid();

            int callerId = ApiSecurityHelper.GetUserId(User);
            if (callerId <= 0) return Unauthorized();

            var setting = await _mgmtDb.SystemSettings.FirstOrDefaultAsync(s => s.SettingId == settingId);
            if (setting == null) return NotFound();

            setting.SettingValue = req.Value;
            setting.UpdatedByUserId = callerId;
            setting.UpdatedAt = DateTime.UtcNow;
            await _mgmtDb.SaveChangesAsync();
            return Ok(new { success = true });
        }

        [HttpGet("backups")]
        public async Task<ActionResult<List<BackupLogDto>>> GetBackupHistory()
        {
            if (!ValidateSuperAdmin()) return Forbid();

            var result = new List<BackupLogDto>();
            try
            {
                var logs = await _mgmtDb.BackupLogs
                    .Include(b => b.PerformedByUser)
                    .AsNoTracking()
                    .OrderByDescending(b => b.BackupDate)
                    .ToListAsync();

                foreach (var log in logs)
                {
                    result.Add(new BackupLogDto
                    {
                        BackupId = log.BackupId,
                        BackupDate = log.BackupDate,
                        Status = log.Status,
                        FileLocation = log.FileLocation ?? "—",
                        PerformedByName = log.PerformedByUser?.FullName ?? "System"
                    });
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[GetBackupHistory] Error: {ex.Message}");
            }

            return Ok(result);
        }

        [HttpPost("backups/run")]
        public async Task<ActionResult<BackupLogDto>> RunBackup()
        {
            if (!ValidateSuperAdmin()) return Forbid();

            int callerId = ApiSecurityHelper.GetUserId(User);
            if (callerId <= 0) return Unauthorized();

            var timestamp = DateTime.UtcNow;
            var fileLocation = $"NEXA_Backup_{timestamp:yyyyMMdd_HHmmss}.bak";

            var log = new BackupLog
            {
                PerformedByUserId = callerId,
                BackupDate = timestamp,
                Status = "Success",
                FileLocation = fileLocation
            };

            _mgmtDb.BackupLogs.Add(log);
            await _mgmtDb.SaveChangesAsync();

            return Ok(new BackupLogDto
            {
                BackupId = log.BackupId,
                BackupDate = log.BackupDate,
                Status = log.Status,
                FileLocation = log.FileLocation ?? string.Empty,
                PerformedByName = "Super Admin"
            });
        }

        [HttpGet("backups/{backupId}")]
        public async Task<ActionResult<BackupLogDto>> GetBackupForRestore(int backupId)
        {
            if (!ValidateSuperAdmin()) return Forbid();

            var log = await _mgmtDb.BackupLogs
                .Include(b => b.PerformedByUser)
                .AsNoTracking()
                .FirstOrDefaultAsync(b => b.BackupId == backupId);

            if (log == null) return NotFound();

            return Ok(new BackupLogDto
            {
                BackupId = log.BackupId,
                BackupDate = log.BackupDate,
                Status = log.Status,
                FileLocation = log.FileLocation ?? "—",
                PerformedByName = log.PerformedByUser?.FullName ?? "System"
            });
        }

        [HttpPost("backups/{backupId}/restore")]
        public async Task<ActionResult> RestoreBackup(int backupId)
        {
            if (!ValidateSuperAdmin()) return Forbid();

            int callerId = ApiSecurityHelper.GetUserId(User);
            if (callerId <= 0) return Unauthorized();

            var restoreLog = new BackupLog
            {
                PerformedByUserId = callerId,
                BackupDate = DateTime.UtcNow,
                Status = $"Restore from BackupId:{backupId}",
                FileLocation = $"RESTORE_FROM_BACKUP_{backupId}"
            };
            _mgmtDb.BackupLogs.Add(restoreLog);
            await _mgmtDb.SaveChangesAsync();
            return Ok(new { success = true });
        }

        [HttpGet("tenants/{companyId}/branding")]
        public async Task<ActionResult<TenantBrandingDto>> GetTenantBranding(int companyId)
        {
            if (!ValidateSuperAdmin()) return Forbid();

            var company = await _masterDb.Companies
                .Include(c => c.Branding)
                .Include(c => c.Subscriptions)
                .AsNoTracking()
                .FirstOrDefaultAsync(c => c.CompanyId == companyId);

            if (company == null) return NotFound();

            var sub = company.Subscriptions.OrderByDescending(s => s.EndDate).FirstOrDefault();
            var tier = sub?.Tier ?? TenantTier.TenantA;
            var branding = company.Branding;

            string currentDisplayName = branding?.DisplayName ?? company.CompanyName;
            bool isDuplicate = await _masterDb.TenantBrandings
                .AnyAsync(b => b.CompanyId != companyId && b.DisplayName.ToLower() == currentDisplayName.ToLower());

            return Ok(new TenantBrandingDto
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
            });
        }

        [HttpPut("tenants/{companyId}/branding")]
        public async Task<ActionResult> UpdateTenantBranding(int companyId, [FromBody] SuperAdminUpdateBrandingRequest req)
        {
            if (!ValidateSuperAdmin()) return Forbid();

            var validator = new UpdateBrandingRequestValidator(allowReservedOverride: req.OverrideReservedName);
            var validation = await validator.ValidateAsync(req);
            if (!validation.IsValid)
            {
                return BadRequest(new { message = validation.Errors.FirstOrDefault()?.ErrorMessage ?? "Validation failed." });
            }

            var company = await _masterDb.Companies
                .Include(c => c.Branding)
                .Include(c => c.Subscriptions)
                .FirstOrDefaultAsync(c => c.CompanyId == companyId);

            if (company == null) return NotFound();

            var sub = company.Subscriptions.OrderByDescending(s => s.EndDate).FirstOrDefault();
            var tier = sub?.Tier ?? TenantTier.TenantA;

            if (!string.IsNullOrWhiteSpace(req.AccentColor) && !FeatureGate.CanUseAccentColor(tier))
            {
                return StatusCode(StatusCodes.Status403Forbidden, new { message = "Accent color customization is restricted to Enterprise tier." });
            }

            if (req.HidePoweredBy && !FeatureGate.CanHidePoweredBy(tier))
            {
                return StatusCode(StatusCodes.Status403Forbidden, new { message = "White-label footer removal is restricted to Enterprise tier." });
            }

            int superAdminId = ApiSecurityHelper.GetUserId(User);
            if (superAdminId <= 0) superAdminId = 1;

            var branding = company.Branding;
            if (branding == null)
            {
                branding = new TenantBranding
                {
                    CompanyId = companyId,
                    DisplayName = req.DisplayName.Trim(),
                    LogoVersion = 1,
                    UpdatedAt = DateTime.UtcNow
                };
                _masterDb.TenantBrandings.Add(branding);
            }

            branding.DisplayName = req.DisplayName.Trim();
            branding.AccentColor = FeatureGate.CanUseAccentColor(tier) ? req.AccentColor?.Trim() : null;
            branding.HidePoweredBy = FeatureGate.CanHidePoweredBy(tier) && req.HidePoweredBy;
            branding.ContactEmail = string.IsNullOrWhiteSpace(req.ContactEmail) ? null : req.ContactEmail.Trim();
            branding.ContactPhone = string.IsNullOrWhiteSpace(req.ContactPhone) ? null : req.ContactPhone.Trim();
            branding.Address = string.IsNullOrWhiteSpace(req.Address) ? null : req.Address.Trim();
            branding.UpdatedAt = DateTime.UtcNow;
            branding.UpdatedByUserId = superAdminId;

            _masterDb.PlatformAuditLogs.Add(new PlatformAuditLog
            {
                PerformedBySuperAdminId = superAdminId,
                PerformedByName = "Platform Super Admin",
                ActionType = "TenantBrandingUpdated",
                Detail = $"Super Admin updated branding for {company.CompanyName}: DisplayName='{branding.DisplayName}'",
                TargetCompanyId = companyId,
                TargetCompanyName = company.CompanyName,
                CreatedAt = DateTime.UtcNow
            });

            await _masterDb.SaveChangesAsync();
            return Ok(new { success = true });
        }

        [HttpPost("tenants/{companyId}/branding/reset")]
        public async Task<ActionResult> ResetTenantBranding(int companyId)
        {
            if (!ValidateSuperAdmin()) return Forbid();

            var company = await _masterDb.Companies
                .Include(c => c.Branding)
                .FirstOrDefaultAsync(c => c.CompanyId == companyId);

            if (company == null) return NotFound();

            int superAdminId = ApiSecurityHelper.GetUserId(User);
            if (superAdminId <= 0) superAdminId = 1;

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
                branding.UpdatedByUserId = superAdminId;
            }
            else
            {
                branding = new TenantBranding
                {
                    CompanyId = companyId,
                    DisplayName = company.CompanyName,
                    LogoVersion = 1,
                    UpdatedAt = DateTime.UtcNow,
                    UpdatedByUserId = superAdminId
                };
                _masterDb.TenantBrandings.Add(branding);
            }

            _masterDb.PlatformAuditLogs.Add(new PlatformAuditLog
            {
                PerformedBySuperAdminId = superAdminId,
                PerformedByName = "Platform Super Admin",
                ActionType = "TenantBrandingReset",
                Detail = $"Super Admin reset branding for {company.CompanyName} to platform defaults",
                TargetCompanyId = companyId,
                TargetCompanyName = company.CompanyName,
                CreatedAt = DateTime.UtcNow
            });

            await _masterDb.SaveChangesAsync();
            return Ok(new { success = true });
        }

        [HttpGet("tenants/{companyId}/branding/logo")]
        public async Task<IActionResult> GetTenantLogo(int companyId)
        {
            if (!ValidateSuperAdmin()) return Forbid();

            var branding = await _masterDb.TenantBrandings
                .AsNoTracking()
                .FirstOrDefaultAsync(b => b.CompanyId == companyId);

            if (branding?.LogoImage == null || branding.LogoImage.Length == 0)
            {
                return NotFound();
            }

            return File(branding.LogoImage, "image/png");
        }

        [HttpPost("tenants/{companyId}/branding/logo")]
        public async Task<IActionResult> UploadTenantLogo(int companyId)
        {
            if (!ValidateSuperAdmin()) return Forbid();

            var company = await _masterDb.Companies
                .Include(c => c.Branding)
                .FirstOrDefaultAsync(c => c.CompanyId == companyId);

            if (company == null) return NotFound();

            byte[]? rawBytes = null;
            if (Request.HasFormContentType && Request.Form.Files.Count > 0)
            {
                var file = Request.Form.Files[0];
                if (file.Length > 5 * 1024 * 1024)
                {
                    return BadRequest(new { message = "Logo file exceeds the maximum allowed size of 5MB." });
                }

                using var ms = new MemoryStream();
                await file.CopyToAsync(ms);
                rawBytes = ms.ToArray();
            }
            else
            {
                try
                {
                    using var reader = new StreamReader(Request.Body);
                    var json = await reader.ReadToEndAsync();
                    if (!string.IsNullOrWhiteSpace(json))
                    {
                        var bodyReq = System.Text.Json.JsonSerializer.Deserialize<UploadLogoRequest>(json, new System.Text.Json.JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                        rawBytes = bodyReq?.LogoBytes;
                    }
                }
                catch
                {
                }
            }

            if (rawBytes == null || rawBytes.Length == 0)
            {
                return BadRequest(new { message = "No image file provided." });
            }

            if (rawBytes.Length > 5 * 1024 * 1024)
            {
                return BadRequest(new { message = "Logo file exceeds the maximum allowed size of 5MB." });
            }

            var (success, normalizedBytes, error) = LogoProcessor.ProcessAndNormalize(rawBytes);
            if (!success || normalizedBytes == null)
            {
                return BadRequest(new { message = error ?? "Failed to process logo image." });
            }

            int superAdminId = ApiSecurityHelper.GetUserId(User);
            if (superAdminId <= 0) superAdminId = 1;

            var branding = company.Branding;
            if (branding == null)
            {
                branding = new TenantBranding
                {
                    CompanyId = companyId,
                    DisplayName = company.CompanyName,
                    LogoVersion = 1,
                    UpdatedAt = DateTime.UtcNow
                };
                _masterDb.TenantBrandings.Add(branding);
            }

            branding.LogoImage = normalizedBytes;
            branding.LogoVersion++;
            branding.UpdatedAt = DateTime.UtcNow;
            branding.UpdatedByUserId = superAdminId;

            _masterDb.PlatformAuditLogs.Add(new PlatformAuditLog
            {
                PerformedBySuperAdminId = superAdminId,
                PerformedByName = "Platform Super Admin",
                ActionType = "TenantBrandingLogoUploaded",
                Detail = $"Super Admin updated brand logo for {company.CompanyName} to version {branding.LogoVersion}",
                TargetCompanyId = companyId,
                TargetCompanyName = company.CompanyName,
                CreatedAt = DateTime.UtcNow
            });

            await _masterDb.SaveChangesAsync();
            return Ok(new { success = true, logoVersion = branding.LogoVersion });
        }

        [HttpDelete("tenants/{companyId}/branding/logo")]
        public async Task<IActionResult> RemoveTenantLogo(int companyId)
        {
            if (!ValidateSuperAdmin()) return Forbid();

            var company = await _masterDb.Companies
                .Include(c => c.Branding)
                .FirstOrDefaultAsync(c => c.CompanyId == companyId);

            if (company == null) return NotFound();

            int superAdminId = ApiSecurityHelper.GetUserId(User);
            if (superAdminId <= 0) superAdminId = 1;

            var branding = company.Branding;
            if (branding != null && branding.LogoImage != null)
            {
                branding.LogoImage = null;
                branding.LogoVersion++;
                branding.UpdatedAt = DateTime.UtcNow;
                branding.UpdatedByUserId = superAdminId;

                _masterDb.PlatformAuditLogs.Add(new PlatformAuditLog
                {
                    PerformedBySuperAdminId = superAdminId,
                    PerformedByName = "Platform Super Admin",
                    ActionType = "TenantBrandingLogoRemoved",
                    Detail = $"Super Admin removed custom brand logo for {company.CompanyName}",
                    TargetCompanyId = companyId,
                    TargetCompanyName = company.CompanyName,
                    CreatedAt = DateTime.UtcNow
                });

                await _masterDb.SaveChangesAsync();
            }

            return Ok(new { success = true });
        }
    }
}
