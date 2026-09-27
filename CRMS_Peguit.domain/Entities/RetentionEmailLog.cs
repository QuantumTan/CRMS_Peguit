using System;

namespace CRMS_Peguit.domain.entities
{
    public class RetentionEmailLog
    {
        public int EmailLogId { get; set; }
        public int TenantId { get; set; } = 1;

        public int CustomerId { get; set; }
        public int? RetentionRequestId { get; set; }

        public string RecipientEmail { get; set; } = string.Empty;
        public string RecipientName { get; set; } = string.Empty;
        public string Segment { get; set; } = string.Empty;
        public string Subject { get; set; } = string.Empty;
        public string Body { get; set; } = string.Empty;
        public string? IncentiveOffered { get; set; }

        public string Status { get; set; } = "Queued"; // Queued, Dispatched, Failed, Cancelled
        public string GenerationSource { get; set; } = "AutomatedRequest"; // AutomatedRequest, ManualSend

        public int? DispatchedByUserId { get; set; }
        public DateTime? DispatchedAt { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public string? ErrorMessage { get; set; }
        public bool IsCooldownOverride { get; set; } = false;
        public string? CooldownOverrideReason { get; set; }

        // Navigation
        public virtual Customer Customer { get; set; } = null!;
        public virtual RetentionRequest? RetentionRequest { get; set; }
        public virtual User? DispatchedByUser { get; set; }
    }
}
