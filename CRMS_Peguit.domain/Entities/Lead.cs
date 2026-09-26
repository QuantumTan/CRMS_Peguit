using System;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;

namespace CRMS_Peguit.domain.entities
{
    public class Lead
    {
        public int LeadId { get; set; }
        public int PersonId { get; set; }

        private string? _firstName;
        private string? _middleName;
        private string? _lastName;
        private string? _suffix;
        private string? _phone;
        private string? _email;

        public string FirstName
        {
            get => Person?.FirstName ?? _firstName ?? string.Empty;
            set
            {
                _firstName = value;
                if (Person != null) Person.FirstName = value;
            }
        }

        public string? MiddleName
        {
            get => Person?.MiddleName ?? _middleName;
            set
            {
                _middleName = value;
                if (Person != null) Person.MiddleName = value;
            }
        }

        public string LastName
        {
            get => Person?.LastName ?? _lastName ?? string.Empty;
            set
            {
                _lastName = value;
                if (Person != null) Person.LastName = value;
            }
        }

        public string? Suffix
        {
            get => Person?.Suffix ?? _suffix;
            set
            {
                _suffix = value;
                if (Person != null) Person.Suffix = value;
            }
        }

        [NotMapped]
        public string FullName =>
            Person?.FullName ??
            string.Join(" ", new[] { FirstName, MiddleName, LastName, Suffix }.Where(v => !string.IsNullOrWhiteSpace(v)));

        public string? Phone
        {
            get => Person?.Phone ?? _phone;
            set
            {
                _phone = value;
                if (Person != null) Person.Phone = value;
            }
        }

        public string? Email
        {
            get => Person?.Email ?? _email;
            set
            {
                _email = value;
                if (Person != null) Person.Email = value;
            }
        }

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

        public virtual Person Person { get; set; } = null!;
        public virtual User CreatedByUser { get; set; } = null!;
        public virtual User? AssignedAgent { get; set; }
        public virtual Customer? ConvertedCustomer { get; set; }
        public virtual Branch? Branch { get; set; }
    }
}
