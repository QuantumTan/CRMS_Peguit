using System.ComponentModel.DataAnnotations.Schema;

namespace CRMS_Peguit.domain.entities
{
    public class Customer
    {
        public int CustomerId { get; set; }
        public int TenantId { get; set; }

        public string FirstName { get; set; } = string.Empty;
        public string? MiddleName { get; set; }
        public string LastName { get; set; } = string.Empty;
        public string? Suffix { get; set; }

        [NotMapped]
        public string FullName
        {
            get
            {
                return string.Join(" ",
                    new[]
                    {
                        FirstName,
                        MiddleName,
                        LastName,
                        Suffix
                    }
                    .Where(value => !string.IsNullOrWhiteSpace(value)));
            }
        }

        public string? Phone { get; set; }
        public string? Email { get; set; }

        public string Type { get; set; } = "buyer";
        public string Status { get; set; } = "active";

        public int? AssignedAgentId { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public bool IsDeleted { get; set; }
        public DateTime? DeletedAt { get; set; }
    }
}