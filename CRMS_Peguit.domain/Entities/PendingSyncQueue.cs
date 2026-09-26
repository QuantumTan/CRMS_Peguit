using System;

namespace CRMS_Peguit.domain.entities
{
    public enum SyncStatus
    {
        Pending,
        Syncing,
        Failed,
        Conflict,
        Synced
    }

    public enum SyncOperation
    {
        Create,
        Update
    }

    public class PendingSyncQueue
    {
        public int QueueId { get; set; }
        public int TenantId { get; set; }
        public int UserId { get; set; }

        /// <summary>
        /// Entity type being synced: "Customer", "Lead", "Deal", "Activity", "TaskReminder", "SupportTicket".
        /// </summary>
        public string EntityType { get; set; } = string.Empty;

        /// <summary>
        /// Temporary local reference before a real server ID exists (e.g. "TEMP-CUST-1234"),
        /// or existing server ID as string for update operations.
        /// </summary>
        public string EntityLocalId { get; set; } = string.Empty;

        /// <summary>
        /// Server-assigned primary key after successful sync.
        /// </summary>
        public int? ServerEntityId { get; set; }

        /// <summary>
        /// Operation type: "Create" or "Update".
        /// </summary>
        public string Operation { get; set; } = "Create";

        /// <summary>
        /// The JSON payload representing the change to be synced.
        /// </summary>
        public string PayloadJson { get; set; } = string.Empty;

        /// <summary>
        /// ISO 8601 timestamp when action was queued.
        /// </summary>
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// Current status: "Pending", "Syncing", "Failed", "Conflict", "Synced".
        /// </summary>
        public string Status { get; set; } = "Pending";

        /// <summary>
        /// Reason for failure if API rejected the action or validation failed.
        /// </summary>
        public string? FailureReason { get; set; }

        /// <summary>
        /// The UpdatedAt/CreatedAt timestamp of the record when locally read/edited.
        /// Used for optimistic concurrency and conflict detection against server's current state.
        /// </summary>
        public DateTime? ServerVersionTimestamp { get; set; }

        /// <summary>
        /// JSON snapshot of the server's record when a conflict was detected.
        /// </summary>
        public string? ServerConflictPayload { get; set; }

        /// <summary>
        /// How many sync attempts have occurred.
        /// </summary>
        public int AttemptCount { get; set; }

        /// <summary>
        /// Timestamp of last sync attempt.
        /// </summary>
        public DateTime? LastAttemptAt { get; set; }
    }
}
