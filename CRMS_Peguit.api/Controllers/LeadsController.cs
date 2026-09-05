using System;

namespace CRMS_Peguit.domain.entities
{
    public class Lead
    {
        public int LeadId { get; set; }

        public int TenantId { get; set; }

        // --- Split name fields ---
        public string FirstName { get; set; } = string.Empty;
        public string? MiddleName { get; set; }
        public string LastName { get; set; } = string.Empty;
        public string? Suffix { get; set; }

        // Convenience read-only full name (NOT mapped to a column)
        public string FullName =>
            string.Join(" ", new[] { FirstName, MiddleName, LastName, Suffix }
                .Where(s => !string.IsNullOrWhiteSpace(s)));

        public string? Phone { get; set; }
        public string? Email { get; set; }
        public string? Source { get; set; }
        public int? AssignedAgentId { get; set; }
        public string Stage { get; set; } = string.Empty;
        public int? ConvertedCustomerId { get; set; }
        public DateTime CreatedAt { get; set; }

        // --- Soft delete ---
        public bool IsDeleted { get; set; }
        public DateTime? DeletedAt { get; set; }
    }
}