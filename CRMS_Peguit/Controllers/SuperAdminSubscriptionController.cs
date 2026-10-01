using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using CRMS_Peguit.domain.entities;
using CRMS_Peguit.domain.Common;
using CRMS_Peguit.infrastructure.data;
using CRMS_Peguit.winforms.Models.Services;
using CRMS_Peguit.winforms.Auth;
using System.Text.Json;
using CRMS_Peguit.winforms.Views.SuperAdmin;

namespace CRMS_Peguit.winforms.Controllers
{
    public class SuperAdminSubscriptionController
    {
        private MasterCrmsDbContext CreateMasterDb()
        {
            return LocalDb.CreateMasterContext();
        }

        public async Task<PlatformBiSummaryDto> GetPlatformBiSummaryAsync()
        {
            var summary = new PlatformBiSummaryDto();

            try
            {
                using var masterDb = CreateMasterDb();
                var companies = await masterDb.Companies
                    .Include(c => c.Subscriptions)
                    .AsNoTracking()
                    .ToListAsync();

                summary.TotalTenants = companies.Count;

                var allSubs = companies.SelectMany(c => c.Subscriptions).ToList();
                var now = DateTime.UtcNow;

                summary.ActiveSubscriptions = allSubs.Count(s => Subscription.CalculateStatus(s.EndDate, now) == "Active");
                summary.ExpiringSubscriptions = allSubs.Count(s => Subscription.CalculateStatus(s.EndDate, now) == "Expiring Soon");
                summary.ExpiredSubscriptions = allSubs.Count(s => Subscription.CalculateStatus(s.EndDate, now) == "Expired");
                summary.TotalMrr = allSubs.Where(s => s.StartDate <= now && Subscription.CalculateStatus(s.EndDate, now) != "Expired").Sum(s => s.BillingAmount);

                summary.TenantACount = allSubs.Count(s => s.PlanName.Contains("Tenant A", StringComparison.OrdinalIgnoreCase));
                summary.TenantBCount = allSubs.Count(s => s.PlanName.Contains("Tenant B", StringComparison.OrdinalIgnoreCase));
                summary.TenantCCount = allSubs.Count(s => s.PlanName.Contains("Tenant C", StringComparison.OrdinalIgnoreCase));
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[GetPlatformBiSummaryAsync] Error: {ex.Message}");
            }

            return summary;
        }

        public async Task<List<TenantSubscriptionDto>> GetAllSubscriptionsAsync(bool throwOnError = false)
        {
            var list = new List<TenantSubscriptionDto>();

            try
            {
                using var masterDb = CreateMasterDb();
                var companies = await masterDb.Companies
                    .Include(c => c.Subscriptions)
                    .AsNoTracking()
                    .ToListAsync();

                foreach (var comp in companies)
                {
                    if (comp.Subscriptions.Count == 0)
                    {
                        list.Add(new TenantSubscriptionDto
                        {
                            SubscriptionId = 0,
                            CompanyId = comp.CompanyId,
                            CompanyCode = comp.CompanyCode,
                            CompanyName = comp.CompanyName,
                            PlanName = "No subscription",
                            Tier = TenantTier.TenantA,
                            StartDate = comp.CreatedAt,
                            BillingAmount = 0m,
                            Status = "Not subscribed"
                        });
                    }
                    else
                    {
                        foreach (var sub in comp.Subscriptions)
                        {
                            list.Add(new TenantSubscriptionDto
                            {
                                SubscriptionId = sub.SubscriptionId,
                                CompanyId = comp.CompanyId,
                                CompanyCode = comp.CompanyCode,
                                CompanyName = comp.CompanyName,
                                PlanName = sub.PlanName,
                                Tier = sub.Tier,
                                StartDate = sub.StartDate,
                                EndDate = sub.EndDate,
                                BillingAmount = sub.BillingAmount,
                                Status = Subscription.CalculateStatus(sub.EndDate)
                            });
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[GetAllSubscriptionsAsync] Error: {ex.Message}");
                if (throwOnError) throw;
            }

            return list;
        }

        private static async Task WriteAuditLogAsync(MasterCrmsDbContext masterDb, string actionType, string detail, int? companyId = null, string? companyName = null)
        {
            try
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
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[WriteAuditLogAsync] Error: {ex.Message}");
            }
        }

        public async Task<bool> UpdateSubscriptionAsync(int subscriptionId, string newPlanName, string newStatus, decimal billingAmount, DateTime? endDate)
        {
            try
            {
                using var masterDb = CreateMasterDb();
                var sub = await masterDb.Subscriptions
                    .Include(s => s.Company)
                    .FirstOrDefaultAsync(s => s.SubscriptionId == subscriptionId);
                if (sub != null)
                {
                    string oldPlan = sub.PlanName;
                    string oldStatus = sub.Status;
                    sub.PlanName = newPlanName;
                    sub.Status = newStatus;
                    sub.BillingAmount = billingAmount;
                    sub.EndDate = endDate;
                    await masterDb.SaveChangesAsync();

                    await WriteAuditLogAsync(masterDb, "SubscriptionChanged",
                        $"Subscription #{subscriptionId} ({sub.Company?.CompanyName ?? "Tenant"}): Plan '{oldPlan}' → '{newPlanName}', Status '{oldStatus}' → '{newStatus}', Amount ₱{billingAmount:N2}",
                        sub.CompanyId, sub.Company?.CompanyName);

                    return true;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[UpdateSubscriptionAsync] Error: {ex.Message}");
            }
            return false;
        }

        public async Task<bool> ChangeTenantTierAsync(int companyId, string newPlanName)
        {
            try
            {
                using var masterDb = CreateMasterDb();
                var company = await masterDb.Companies.FindAsync(companyId);
                var sub = await masterDb.Subscriptions
                    .Where(s => s.CompanyId == companyId)
                    .OrderByDescending(s => s.StartDate)
                    .FirstOrDefaultAsync();

                if (sub != null)
                {
                    string oldPlan = sub.PlanName;
                    sub.PlanName = newPlanName;
                    await masterDb.SaveChangesAsync();

                    await WriteAuditLogAsync(masterDb, "SubscriptionChanged",
                        $"Tenant '{company?.CompanyName ?? $"ID {companyId}"}' tier plan changed from '{oldPlan}' to '{newPlanName}'",
                        companyId, company?.CompanyName);

                    return true;
                }
                else
                {
                    var newSub = new Subscription
                    {
                        CompanyId = companyId,
                        PlanName = newPlanName,
                        StartDate = DateTime.UtcNow,
                        EndDate = DateTime.UtcNow.AddYears(1),
                        BillingAmount = newPlanName.Contains("Tenant C") ? 9500m : (newPlanName.Contains("Tenant B") ? 5500m : 2500m),
                        Status = "Active"
                    };
                    masterDb.Subscriptions.Add(newSub);
                    await masterDb.SaveChangesAsync();

                    await WriteAuditLogAsync(masterDb, "SubscriptionChanged",
                        $"Tenant '{company?.CompanyName ?? $"ID {companyId}"}' initial plan set to '{newPlanName}'",
                        companyId, company?.CompanyName);

                    return true;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[ChangeTenantTierAsync] Error: {ex.Message}");
                return false;
            }
        }

        public async Task<(bool Success, string Message, PaymentRecordDto? Record)> RecordPaymentAsync(RecordPaymentRequest req)
        {
            if (req.SubscriptionId <= 0)
                return (false, "Please select a valid subscription.", null);

            if (req.AmountPaid <= 0)
                return (false, "Amount paid must be greater than zero.", null);

            if (string.IsNullOrWhiteSpace(req.PaymentReference))
                return (false, "Payment Reference is required. Please provide a bank transaction ID, GCash reference number, or check number.", null);

            try
            {
                using var masterDb = CreateMasterDb();
                var sub = await masterDb.Subscriptions
                    .Include(s => s.Company)
                    .FirstOrDefaultAsync(s => s.SubscriptionId == req.SubscriptionId);

                if (sub == null)
                    return (false, "Subscription record not found.", null);

                // Core Extension Math:
                // If renewing before expiry (current EndDate > paymentDate), extend from current EndDate.
                // If renewing after lapse (current EndDate <= paymentDate), extend from paymentDate.
                var (newEndDate, newStatus) = Subscription.CalculateExtension(sub.EndDate, req.PaymentDate, sub.PlanName);

                sub.EndDate = newEndDate;
                sub.Status = newStatus;

                int superAdminId = CurrentSession.UserId > 0 ? CurrentSession.UserId : 1;
                string superAdminName = CurrentSession.CurrentUser?.FullName ?? "Platform Super Admin";

                var record = new PaymentRecord
                {
                    SubscriptionId = sub.SubscriptionId,
                    AmountPaid = req.AmountPaid,
                    PaymentMethod = req.PaymentMethod,
                    PaymentReference = req.PaymentReference.Trim(),
                    PaymentDate = req.PaymentDate,
                    RecordedByUserId = superAdminId,
                    Notes = req.Notes?.Trim(),
                    CreatedAt = DateTime.UtcNow
                };

                masterDb.PaymentRecords.Add(record);
                await masterDb.SaveChangesAsync();

                string methodDisplay = record.PaymentMethod switch
                {
                    PaymentMethod.BankTransfer => "Bank Transfer",
                    PaymentMethod.GCash => "GCash",
                    PaymentMethod.Check => "Check",
                    PaymentMethod.Cash => "Cash",
                    _ => "Other"
                };

                // Platform Audit Log write (who recorded it, which tenant, amount, method, reference)
                string auditDetail = $"Payment of ₱{req.AmountPaid:N2} via {methodDisplay} (Ref: {req.PaymentReference.Trim()}) recorded for '{sub.Company?.CompanyName ?? "Tenant"}'. Paid Through extended to {newEndDate:MMM dd, yyyy} (Status: {newStatus}).";
                await WriteAuditLogAsync(masterDb, "PaymentRecorded", auditDetail, sub.CompanyId, sub.Company?.CompanyName);

                var dto = new PaymentRecordDto
                {
                    PaymentRecordId = record.PaymentRecordId,
                    SubscriptionId = record.SubscriptionId,
                    CompanyId = sub.CompanyId,
                    CompanyName = sub.Company?.CompanyName ?? string.Empty,
                    CompanyCode = sub.Company?.CompanyCode ?? string.Empty,
                    AmountPaid = record.AmountPaid,
                    PaymentMethod = record.PaymentMethod,
                    PaymentReference = record.PaymentReference,
                    PaymentDate = record.PaymentDate,
                    RecordedByUserId = record.RecordedByUserId,
                    RecordedByName = superAdminName,
                    Notes = record.Notes,
                    CreatedAt = record.CreatedAt
                };

                return (true, "Payment recorded successfully.", dto);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[RecordPaymentAsync] Error: {ex.Message}");
                return (false, $"Failed to record payment: {ex.Message}", null);
            }
        }

        public async Task<List<PaymentRecordDto>> GetPaymentRecordsAsync(int subscriptionId)
        {
            var list = new List<PaymentRecordDto>();
            try
            {
                using var masterDb = CreateMasterDb();
                var records = await masterDb.PaymentRecords
                    .Include(p => p.Subscription)
                        .ThenInclude(s => s!.Company)
                    .Include(p => p.RecordedBySuperAdmin)
                    .Where(p => p.SubscriptionId == subscriptionId)
                    .OrderByDescending(p => p.PaymentDate)
                    .ThenByDescending(p => p.PaymentRecordId)
                    .AsNoTracking()
                    .ToListAsync();

                foreach (var r in records)
                {
                    list.Add(new PaymentRecordDto
                    {
                        PaymentRecordId = r.PaymentRecordId,
                        SubscriptionId = r.SubscriptionId,
                        CompanyId = r.Subscription?.CompanyId ?? 0,
                        CompanyName = r.Subscription?.Company?.CompanyName ?? string.Empty,
                        CompanyCode = r.Subscription?.Company?.CompanyCode ?? string.Empty,
                        AmountPaid = r.AmountPaid,
                        PaymentMethod = r.PaymentMethod,
                        PaymentReference = r.PaymentReference,
                        PaymentDate = r.PaymentDate,
                        RecordedByUserId = r.RecordedByUserId,
                        RecordedByName = r.RecordedBySuperAdmin != null
                            ? $"{r.RecordedBySuperAdmin.FirstName} {r.RecordedBySuperAdmin.LastName}".Trim()
                            : "Platform Super Admin",
                        Notes = r.Notes,
                        CreatedAt = r.CreatedAt
                    });
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[GetPaymentRecordsAsync] Error: {ex.Message}");
            }
            return list;
        }

        public async Task<List<PaymentRecordDto>> GetCompanyPaymentHistoryAsync(int companyId)
        {
            var list = new List<PaymentRecordDto>();
            try
            {
                using var masterDb = CreateMasterDb();
                var records = await masterDb.PaymentRecords
                    .Include(p => p.Subscription)
                        .ThenInclude(s => s!.Company)
                    .Include(p => p.RecordedBySuperAdmin)
                    .Where(p => p.Subscription != null && p.Subscription.CompanyId == companyId)
                    .OrderByDescending(p => p.PaymentDate)
                    .ThenByDescending(p => p.PaymentRecordId)
                    .AsNoTracking()
                    .ToListAsync();

                foreach (var r in records)
                {
                    list.Add(new PaymentRecordDto
                    {
                        PaymentRecordId = r.PaymentRecordId,
                        SubscriptionId = r.SubscriptionId,
                        CompanyId = r.Subscription?.CompanyId ?? 0,
                        CompanyName = r.Subscription?.Company?.CompanyName ?? string.Empty,
                        CompanyCode = r.Subscription?.Company?.CompanyCode ?? string.Empty,
                        AmountPaid = r.AmountPaid,
                        PaymentMethod = r.PaymentMethod,
                        PaymentReference = r.PaymentReference,
                        PaymentDate = r.PaymentDate,
                        RecordedByUserId = r.RecordedByUserId,
                        RecordedByName = r.RecordedBySuperAdmin != null
                            ? $"{r.RecordedBySuperAdmin.FirstName} {r.RecordedBySuperAdmin.LastName}".Trim()
                            : "Platform Super Admin",
                        Notes = r.Notes,
                        CreatedAt = r.CreatedAt
                    });
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[GetCompanyPaymentHistoryAsync] Error: {ex.Message}");
            }
            return list;
        }

        public async Task<List<PaymentRecordDto>> GetAllPaymentRecordsAsync(bool throwOnError = false)
        {
            var list = new List<PaymentRecordDto>();
            try
            {
                using var masterDb = CreateMasterDb();
                var records = await masterDb.PaymentRecords
                    .Include(p => p.Subscription)
                        .ThenInclude(s => s!.Company)
                    .Include(p => p.RecordedBySuperAdmin)
                    .OrderByDescending(p => p.PaymentDate)
                    .ThenByDescending(p => p.PaymentRecordId)
                    .AsNoTracking()
                    .ToListAsync();

                foreach (var r in records)
                {
                    list.Add(new PaymentRecordDto
                    {
                        PaymentRecordId = r.PaymentRecordId,
                        SubscriptionId = r.SubscriptionId,
                        CompanyId = r.Subscription?.CompanyId ?? 0,
                        CompanyName = r.Subscription?.Company?.CompanyName ?? "Unknown Tenant",
                        CompanyCode = r.Subscription?.Company?.CompanyCode ?? "—",
                        AmountPaid = r.AmountPaid,
                        PaymentMethod = r.PaymentMethod,
                        PaymentReference = r.PaymentReference,
                        PaymentDate = r.PaymentDate,
                        RecordedByUserId = r.RecordedByUserId,
                        RecordedByName = r.RecordedBySuperAdmin != null
                            ? $"{r.RecordedBySuperAdmin.FirstName} {r.RecordedBySuperAdmin.LastName}".Trim()
                            : "Platform Super Admin",
                        Notes = r.Notes ?? $"Plan: {r.Subscription?.PlanName ?? "Subscription"}",
                        CreatedAt = r.CreatedAt
                    });
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[GetAllPaymentRecordsAsync] Error: {ex.Message}");
                if (throwOnError) throw;
            }

            return list;
        }

        public async Task<string> GetMasterTermsAsync()
        {
            try
            {
                using var masterDb = CreateMasterDb();
                var setting = await masterDb.GlobalSettings
                    .AsNoTracking()
                    .FirstOrDefaultAsync(s => s.SettingKey == "Platform.MasterTermsAndConditions");

                if (setting != null && !string.IsNullOrWhiteSpace(setting.SettingValue))
                {
                    return setting.SettingValue;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[GetMasterTermsAsync] Error: {ex.Message}");
            }

            return TenantTermsAndConditionsDialog.DefaultTermsText;
        }

        public async Task<(bool Success, string? Error)> SaveMasterTermsAsync(string termsContent, int superAdminId = 1)
        {
            try
            {
                using var masterDb = CreateMasterDb();
                var setting = await masterDb.GlobalSettings
                    .FirstOrDefaultAsync(s => s.SettingKey == "Platform.MasterTermsAndConditions");

                if (setting == null)
                {
                    setting = new GlobalSetting
                    {
                        SettingKey = "Platform.MasterTermsAndConditions",
                        SettingValue = termsContent,
                        UpdatedBySuperAdminId = superAdminId > 0 ? superAdminId : 1,
                        UpdatedAt = DateTime.UtcNow
                    };
                    masterDb.GlobalSettings.Add(setting);
                }
                else
                {
                    setting.SettingValue = termsContent;
                    setting.UpdatedBySuperAdminId = superAdminId > 0 ? superAdminId : 1;
                    setting.UpdatedAt = DateTime.UtcNow;
                }

                await masterDb.SaveChangesAsync();
                return (true, null);
            }
            catch (Exception ex)
            {
                return (false, ex.Message);
            }
        }

        public async Task<(bool Success, string Error)> CreateTenantAsync(CreateTenantRequest req)
        {
            var tenantController = new SuperAdminTenantController();
            return await tenantController.CreateTenantAsync(req);
        }

        private static bool _entitlementsLoaded = false;
        private static readonly object _entitlementsLock = new();

        /// <summary>
        /// Loads persisted tier entitlement overrides from MasterDb into FeatureGate if not already loaded.
        /// </summary>
        public async Task EnsureTierEntitlementsLoadedAsync()
        {
            if (_entitlementsLoaded) return;

            try
            {
                using var masterDb = CreateMasterDb();
                var setting = await masterDb.GlobalSettings
                    .AsNoTracking()
                    .FirstOrDefaultAsync(s => s.SettingKey == "Platform.TierEntitlementsMatrix");

                if (setting != null && !string.IsNullOrWhiteSpace(setting.SettingValue))
                {
                    var matrix = JsonSerializer.Deserialize<Dictionary<string, bool>>(setting.SettingValue);
                    if (matrix != null)
                    {
                        FeatureGate.SetOverrides(matrix);
                    }
                }

                lock (_entitlementsLock)
                {
                    _entitlementsLoaded = true;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[EnsureTierEntitlementsLoadedAsync] Error: {ex.Message}");
            }
        }

        /// <summary>
        /// Retrieves the current tier entitlements matrix, incorporating database overrides and factory defaults.
        /// </summary>
        public async Task<Dictionary<string, bool>> GetTierEntitlementsMatrixAsync()
        {
            await EnsureTierEntitlementsLoadedAsync();
            return FeatureGate.GetCurrentMatrix();
        }

        /// <summary>
        /// Persists the Super Admin's tier entitlement matrix to MasterDb and immediately activates it in-memory.
        /// </summary>
        public async Task<(bool Success, string? Error)> SaveTierEntitlementsMatrixAsync(Dictionary<string, bool> matrix, int superAdminId = 1)
        {
            const string key = "Platform.TierEntitlementsMatrix";
            try
            {
                using var masterDb = CreateMasterDb();
                var setting = await masterDb.GlobalSettings
                    .FirstOrDefaultAsync(s => s.SettingKey == key);

                string json = JsonSerializer.Serialize(matrix, new JsonSerializerOptions { WriteIndented = true });

                if (setting == null)
                {
                    setting = new GlobalSetting
                    {
                        SettingKey = key,
                        SettingValue = json,
                        UpdatedBySuperAdminId = superAdminId > 0 ? superAdminId : 1,
                        UpdatedAt = DateTime.UtcNow
                    };
                    masterDb.GlobalSettings.Add(setting);
                }
                else
                {
                    setting.SettingValue = json;
                    setting.UpdatedBySuperAdminId = superAdminId > 0 ? superAdminId : 1;
                    setting.UpdatedAt = DateTime.UtcNow;
                }

                // Immediately apply in-memory across the client
                FeatureGate.SetOverrides(matrix);
                lock (_entitlementsLock)
                {
                    _entitlementsLoaded = true;
                }

                await WriteAuditLogAsync(masterDb, "SystemSettingChanged", "Updated platform tier entitlements matrix");
                await masterDb.SaveChangesAsync();
                return (true, null);
            }
            catch (Exception ex)
            {
                return (false, ex.Message);
            }
        }

        /// <summary>
        /// Resets the tier entitlements matrix to standard factory defaults and clears custom plan inclusions.
        /// </summary>
        public async Task<(bool Success, string? Error)> ResetTierEntitlementsMatrixAsync(int superAdminId = 1)
        {
            const string key = "Platform.TierEntitlementsMatrix";
            try
            {
                using var masterDb = CreateMasterDb();
                var setting = await masterDb.GlobalSettings
                    .FirstOrDefaultAsync(s => s.SettingKey == key);

                if (setting != null)
                {
                    masterDb.GlobalSettings.Remove(setting);
                }

                var textSettings = await masterDb.GlobalSettings
                    .Where(s => s.SettingKey.StartsWith("PlanInclusions."))
                    .ToListAsync();

                if (textSettings.Count > 0)
                {
                    masterDb.GlobalSettings.RemoveRange(textSettings);
                }

                FeatureGate.ClearOverrides();
                lock (_entitlementsLock)
                {
                    _entitlementsLoaded = true;
                }

                await WriteAuditLogAsync(masterDb, "SystemSettingChanged", "Reset platform tier entitlements matrix to defaults");
                await masterDb.SaveChangesAsync();
                return (true, null);
            }
            catch (Exception ex)
            {
                return (false, ex.Message);
            }
        }

        public static string[] GetDefaultPlanInclusions(TenantTier tier)
        {
            return FeatureGate.RegisteredFeatures
                .Where(f => FeatureGate.IsFeatureEnabled(tier, f.Key))
                .Select(f => f.DisplayName)
                .ToArray();
        }

        public async Task<List<string>> GetPlanInclusionsAsync(TenantTier tier)
        {
            await EnsureTierEntitlementsLoadedAsync();

            string key = $"PlanInclusions.{tier}";
            try
            {
                using var masterDb = CreateMasterDb();
                var setting = await masterDb.GlobalSettings
                    .AsNoTracking()
                    .FirstOrDefaultAsync(s => s.SettingKey == key);

                if (setting != null && !string.IsNullOrWhiteSpace(setting.SettingValue))
                {
                    var lines = setting.SettingValue
                        .Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.RemoveEmptyEntries)
                        .Select(l => l.Trim())
                        .Where(l => !string.IsNullOrWhiteSpace(l))
                        .ToList();

                    if (lines.Count > 0) return lines;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[GetPlanInclusionsAsync] Error: {ex.Message}");
            }

            return GetDefaultPlanInclusions(tier).ToList();
        }

        public async Task<(bool Success, string? Error)> SavePlanInclusionsAsync(TenantTier tier, List<string> features, int superAdminId = 1)
        {
            string key = $"PlanInclusions.{tier}";
            string content = string.Join("\r\n", features.Where(f => !string.IsNullOrWhiteSpace(f)).Select(f => f.Trim()));

            try
            {
                using var masterDb = CreateMasterDb();
                var setting = await masterDb.GlobalSettings
                    .FirstOrDefaultAsync(s => s.SettingKey == key);

                if (setting == null)
                {
                    setting = new GlobalSetting
                    {
                        SettingKey = key,
                        SettingValue = content,
                        UpdatedBySuperAdminId = superAdminId > 0 ? superAdminId : 1,
                        UpdatedAt = DateTime.UtcNow
                    };
                    masterDb.GlobalSettings.Add(setting);
                }
                else
                {
                    setting.SettingValue = content;
                    setting.UpdatedBySuperAdminId = superAdminId > 0 ? superAdminId : 1;
                    setting.UpdatedAt = DateTime.UtcNow;
                }

                await WriteAuditLogAsync(masterDb, "SystemSettingChanged", $"Updated plan inclusions for {tier}");
                await masterDb.SaveChangesAsync();
                return (true, null);
            }
            catch (Exception ex)
            {
                return (false, ex.Message);
            }
        }

        public async Task<(bool Success, string? Error)> ResetPlanInclusionsAsync(TenantTier tier, int superAdminId = 1)
        {
            string key = $"PlanInclusions.{tier}";
            try
            {
                using var masterDb = CreateMasterDb();
                var setting = await masterDb.GlobalSettings
                    .FirstOrDefaultAsync(s => s.SettingKey == key);

                if (setting != null)
                {
                    masterDb.GlobalSettings.Remove(setting);
                    await WriteAuditLogAsync(masterDb, "SystemSettingChanged", $"Reset plan inclusions to default for {tier}");
                    await masterDb.SaveChangesAsync();
                }
                return (true, null);
            }
            catch (Exception ex)
            {
                return (false, ex.Message);
            }
        }
    }
}
