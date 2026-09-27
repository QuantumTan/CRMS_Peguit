using System;

namespace CRMS_Peguit.domain.entities
{
    /// <summary>
    /// Append-only tenant-level audit log for Customer Retention actions.
    /// Records WHO did WHAT and WHEN regarding segments, incentives, approvals, and campaign dispatch.
    /// </summary>
    public class RetentionAuditLog
    {
        public int AuditId { get; set; }
        public int TenantId { get; set; } = 1;

        public int PerformedByUserId { get; set; }
        public string PerformedByName { get; set; } = string.Empty;
        public string UserRole { get; set; } = string.Empty;

        /// <summary>
        /// Action types: SegmentRecalculated, CooldownOverride, RequestSubmitted,
        /// RequestApproved, RequestRejected, CampaignQueued, EmailDispatched, TemplateUpdated
        /// </summary>
        public string ActionType { get; set; } = string.Empty;

        public int? TargetCustomerId { get; set; }
        public string? TargetCustomerName { get; set; }

        /// <summary>
        /// Key: Value structured details (never free-text only)
        /// </summary>
        public string Detail { get; set; } = string.Empty;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Navigation
        public virtual User? PerformedByUser { get; set; }
        public virtual Customer? TargetCustomer { get; set; }
    }
}
