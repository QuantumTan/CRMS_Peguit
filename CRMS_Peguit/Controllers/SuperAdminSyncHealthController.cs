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
using CRMS_Peguit.winforms.Services.Offline;

namespace CRMS_Peguit.winforms.Controllers
{
    /// <summary>
    /// NEXA Super Admin Controller for Sync Health & Platform Audit Log.
    /// STRICT DATA BOUNDARY ENFORCED: Never accesses operational CRM tables.
    /// Only reads sync queue metadata (counts and timestamps), never queue payload contents.
    /// </summary>
    public class SuperAdminSyncHealthController
    {
        public async Task<List<SyncHealthDto>> GetSyncHealthAsync()
        {
            var results = new List<SyncHealthDto>();
            using var masterDb = LocalDb.CreateMasterContext();
            var companies = await masterDb.Companies.AsNoTracking().ToListAsync();

            foreach (var company in companies)
            {
                var dto = new SyncHealthDto
                {
                    CompanyId = company.CompanyId,
                    CompanyName = company.CompanyName
                };

                try
                {
                    // COUNTS AND TIMESTAMPS ONLY! Never payload or record details.
                    var counts = LocalDataCache.Instance.GetQueueCounts(company.CompanyId);
                    int pendingCount = counts.Pending + counts.Syncing;
                    int failedCount = counts.Failed;
                    var lastSuccess = LocalDataCache.Instance.GetLastSuccessfulSync(company.CompanyId);

                    dto.PendingCount = pendingCount;
                    dto.FailedCount = failedCount;
                    dto.LastSuccessfulSync = lastSuccess;

                    if (failedCount > 0)
                    {
                        dto.SyncStatus = "Failing";
                    }
                    else if (pendingCount >= 10)
                    {
                        dto.SyncStatus = "Delayed";
                    }
                    else
                    {
                        dto.SyncStatus = "Healthy";
                    }
                }
                catch
                {
                    dto.PendingCount = 0;
                    dto.FailedCount = 0;
                    dto.LastSuccessfulSync = null;
                    dto.SyncStatus = "Healthy";
                }

                results.Add(dto);
            }

            return results;
        }

        public async Task<bool> RetrySyncAsync(int companyId)
        {
            try
            {
                using var masterDb = LocalDb.CreateMasterContext();
                var company = await masterDb.Companies.FindAsync(companyId);
                if (company == null) return false;

                // Trigger sync service safely
                try
                {
                    if (SyncService.Instance != null)
                    {
                        var method = SyncService.Instance.GetType().GetMethod("ForceSyncAsync")
                            ?? SyncService.Instance.GetType().GetMethod("TriggerSyncAsync")
                            ?? SyncService.Instance.GetType().GetMethod("SyncAsync");

                        if (method != null)
                        {
                            var pars = method.GetParameters();
                            if (pars.Length == 1 && pars[0].ParameterType == typeof(int))
                            {
                                var task = (Task?)method.Invoke(SyncService.Instance, new object[] { companyId });
                                if (task != null) await task;
                            }
                            else if (pars.Length == 0)
                            {
                                var task = (Task?)method.Invoke(SyncService.Instance, null);
                                if (task != null) await task;
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Sync trigger note: {ex.Message}");
                }

                await WriteAuditLogAsync(masterDb, "SyncRetried",
                    $"Manual sync retry triggered for tenant: {company.CompanyName} (ID: {companyId})",
                    company.CompanyId, company.CompanyName);

                return true;
            }
            catch
            {
                return false;
            }
        }

        public async Task<List<PlatformAuditLogDto>> GetAuditLogsAsync(string? actionTypeFilter = null, DateTime? fromDate = null, DateTime? toDate = null)
        {
            using var masterDb = LocalDb.CreateMasterContext();
            var query = masterDb.PlatformAuditLogs.AsNoTracking().AsQueryable();

            if (!string.IsNullOrWhiteSpace(actionTypeFilter) && !actionTypeFilter.Equals("All", StringComparison.OrdinalIgnoreCase))
            {
                query = query.Where(l => l.ActionType == actionTypeFilter);
            }

            if (fromDate.HasValue)
            {
                var fromUtc = fromDate.Value.ToUniversalTime();
                query = query.Where(l => l.CreatedAt >= fromUtc);
            }

            if (toDate.HasValue)
            {
                var toUtc = toDate.Value.Date.AddDays(1).ToUniversalTime();
                query = query.Where(l => l.CreatedAt < toUtc);
            }

            var logs = await query
                .OrderByDescending(l => l.CreatedAt)
                .Take(200)
                .ToListAsync();

            return logs.Select(l => new PlatformAuditLogDto
            {
                AuditLogId = l.AuditLogId,
                PerformedByName = l.PerformedByName,
                ActionType = l.ActionType,
                Detail = l.Detail,
                TargetCompanyName = l.TargetCompanyName,
                CreatedAt = l.CreatedAt
            }).ToList();
        }

        public async Task<List<PlatformAuditLogDto>> GetRecentAuditLogsAsync(int count = 10)
        {
            using var masterDb = LocalDb.CreateMasterContext();
            var logs = await masterDb.PlatformAuditLogs
                .OrderByDescending(l => l.CreatedAt)
                .Take(count)
                .AsNoTracking()
                .ToListAsync();

            return logs.Select(l => new PlatformAuditLogDto
            {
                AuditLogId = l.AuditLogId,
                PerformedByName = l.PerformedByName,
                ActionType = l.ActionType,
                Detail = l.Detail,
                TargetCompanyName = l.TargetCompanyName,
                CreatedAt = l.CreatedAt
            }).ToList();
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
    }
}
