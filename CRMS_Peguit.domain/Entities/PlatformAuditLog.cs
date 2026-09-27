using System;

namespace CRMS_Peguit.domain.entities
{
    /// <summary>
    /// Append-only platform-level audit log for Super Admin actions.
    /// Tracks WHO did WHAT and WHEN across the entire platform.
    /// Contains ZERO tenant operational data — only platform administration actions.
    /// </summary>
    public class PlatformAuditLog
    {
        public int AuditLogId { get; set; }
        
        /// <summary>SuperAdminId who performed the action.</summary>
        public int PerformedBySuperAdminId { get; set; }
        
        /// <summary>Display name of the actor (denormalized for read performance).</summary>
        public string PerformedByName { get; set; } = string.Empty;
        
        /// <summary>
        /// Action type: TenantCreated, TenantSuspended, TenantReactivated,
        /// SubscriptionChanged, BackupCreated, BackupRestored,
        /// SystemSettingChanged, AdministratorCreated, AdministratorDeactivated,
        /// AdministratorActivated, SyncRetried
        /// </summary>
        public string ActionType { get; set; } = string.Empty;
        
        /// <summary>
        /// Structured detail e.g. "TierLevel: Tenant A → Tenant B" or "Company: Apex Realty suspended"
        /// Never free-text-only — always structured key: value format.
        /// </summary>
        public string Detail { get; set; } = string.Empty;
        
        /// <summary>Optional: CompanyId affected, if applicable.</summary>
        public int? TargetCompanyId { get; set; }
        
        /// <summary>Optional: Company name affected (denormalized for display).</summary>
        public string? TargetCompanyName { get; set; }
        
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        
        // Navigation
        public virtual SuperAdmin? PerformedBySuperAdmin { get; set; }
    }
}
