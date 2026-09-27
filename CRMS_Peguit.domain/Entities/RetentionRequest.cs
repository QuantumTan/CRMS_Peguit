using System;
using System.ComponentModel.DataAnnotations.Schema;

namespace CRMS_Peguit.domain.entities
{
    public class RetentionRequest
    {
        public int RequestId { get; set; }
        public int TenantId { get; set; } = 1;

        public int CustomerId { get; set; }
        public int SubmittedByUserId { get; set; }
        public int? AssignedAgentId { get; set; }

        public string TargetSegment { get; set; } = string.Empty;
        public string ActionType { get; set; } = "Incentive Offer";
        public string ProposedIncentive { get; set; } = string.Empty;
        public string RetentionDetails { get; set; } = string.Empty;
        public string ReasonCategory { get; set; } = "Improve Customer Retention";

        public string Status { get; set; } = "Pending"; // Pending, Approved, Rejected

        public int? ReviewedByUserId { get; set; }
        public DateTime? ReviewedAt { get; set; }
        public string? ReviewerRemarks { get; set; }
        public string? RejectionReason { get; set; }

        public bool AddedToCampaign { get; set; } = false;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Navigation
        public virtual Customer Customer { get; set; } = null!;
        public virtual User SubmittedByUser { get; set; } = null!;
        public virtual User? ReviewedByUser { get; set; }
        public virtual User? AssignedAgent { get; set; }
    }
}
