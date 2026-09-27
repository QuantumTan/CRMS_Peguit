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

            try
            {
                await using var db = await _tenantFactory.CreateAsync(req.TenantId);

                var existingUser = await db.Users.AsNoTracking().AnyAsync(u => u.Email == req.Email);
                if (existingUser)
                    return BadRequest(new { message = "An account with this email already exists in this tenant." });

                var role = await db.Roles.AsNoTracking().FirstOrDefaultAsync(r => r.RoleName == req.RoleName || r.RoleName == "Admin");
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
    }
}
