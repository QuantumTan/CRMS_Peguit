using System;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;

namespace CRMS_Peguit.domain.entities
{
    public class Lead
    {
        public int LeadId { get; set; }

        public string FirstName { get; set; } = string.Empty;
        public string? MiddleName { get; set; }
        public string LastName { get; set; } = string.Empty;
        public string? Suffix { get; set; }
        public string Email { get; set; } = string.Empty;
        public string? Phone { get; set; }

        [NotMapped]
        public string FullName =>
            string.Join(" ", new[] { FirstName, MiddleName, LastName, Suffix }.Where(v => !string.IsNullOrWhiteSpace(v)));

        public string? Source { get; set; }
        public string? Notes { get; set; }
        public string Stage { get; set; } = "new";
        public string? Priority { get; set; }
        public decimal? ExpectedValue { get; set; }

        public int CreatedByUserId { get; set; }
        public int? AssignedAgentId { get; set; }
        public string AssignmentStatus { get; set; } = "pending_review";
        public int? AssignmentReviewedByUserId { get; set; }
        public DateTime? AssignmentReviewedAt { get; set; }
        public string? AssignmentReviewNotes { get; set; }

        public int? ConvertedCustomerId { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public bool IsDeleted { get; set; }
        public DateTime? DeletedAt { get; set; }
        public int? BranchId { get; set; }

        [NotMapped]
        public int PersonId { get => LeadId; set { } }

        [NotMapped]
        public virtual Person? Person
        {
            get => new Person
            {
                PersonId = LeadId,
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
        public virtual Customer? ConvertedCustomer { get; set; }
        public virtual Branch? Branch { get; set; }
    }
}
