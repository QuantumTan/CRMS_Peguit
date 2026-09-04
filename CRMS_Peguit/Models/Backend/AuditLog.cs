using NEXA.Model;
using System;

namespace CRMS_Peguit.winform.Models.Backend
{
    /// <summary>
    /// Generic audit log for tracking changes to any table.
    /// Inherits BaseEntity (Id, CreatedAt, etc.).
    /// </summary>
    public class AuditLog : BaseEntity
    {
        // Id, CreatedAt from BaseEntity – we use ChangedAt separately if needed.

        public string TableName { get; set; }
        public int RecordId { get; set; }
        public string Action { get; set; }          // "Insert", "Update", "Delete"
        public string ColumnName { get; set; }
        public string OldValue { get; set; }
        public string NewValue { get; set; }

        public int ChangedByUserId { get; set; }
        public virtual User ChangedBy { get; set; }

        // We keep a separate ChangedAt for audit precision,
        // but you could also use CreatedAt from BaseEntity.
        public DateTime ChangedAt { get; set; } = DateTime.UtcNow;
    }
}
