using System;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;

namespace CRMS_Peguit.domain.entities
{
    public class Customer
    {
        public int CustomerId { get; set; }

        public string FirstName { get; set; } = string.Empty;
        public string? MiddleName { get; set; }
        public string LastName { get; set; } = string.Empty;
        public string? Suffix { get; set; }
        public string Email { get; set; } = string.Empty;
        public string? Phone { get; set; }

        [NotMapped]
        public string FullName =>
            string.Join(" ", new[] { FirstName, MiddleName, LastName, Suffix }.Where(v => !string.IsNullOrWhiteSpace(v)));

        public string Type { get; set; } = "buyer";
        public string Status { get; set; } = "active";

        public int CreatedByUserId { get; set; }
        public int? AssignedAgentId { get; set; }
        public string AssignmentStatus { get; set; } = "pending_review";
        public int? AssignmentReviewedByUserId { get; set; }
        public DateTime? AssignmentReviewedAt { get; set; }
        public string? AssignmentReviewNotes { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime? LastMarketUpdateSentAt { get; set; }

        // --- Customer Retention & Lifecycle Segment ---
        public string CurrentRetentionSegment { get; set; } = "Prospective Client";
        public DateTime? RetentionSegmentCalculatedAt { get; set; }
        public DateTime? LastRetentionEmailSentAt { get; set; }

        public bool IsDeleted { get; set; }
        public DateTime? DeletedAt { get; set; }

        [NotMapped]
        public int PersonId { get => CustomerId; set { } }

        [NotMapped]
        public virtual Person? Person
        {
            get => new Person
            {
                PersonId = CustomerId,
                FirstName = FirstName,
                MiddleName = MiddleName,
                LastName = LastName,
                Suffix = Suffix,
                Email = Email,
                Phone = Phone,
                CreatedAt = CreatedAt
            };
            set
            {
                if (value != null)
                {
                    FirstName = value.FirstName;
                    MiddleName = value.MiddleName;
                    LastName = value.LastName;
                    Suffix = value.Suffix;
                    Email = value.Email ?? string.Empty;
                    Phone = value.Phone;
                }
            }
        }

        public virtual User CreatedByUser { get; set; } = null!;
        public virtual User? AssignedAgent { get; set; }
    }
}
