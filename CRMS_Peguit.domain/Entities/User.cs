using System;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;

namespace CRMS_Peguit.domain.entities
{
    public class User
    {
        public int UserId { get; set; }

        public string FirstName { get; set; } = string.Empty;
        public string? MiddleName { get; set; }
        public string LastName { get; set; } = string.Empty;
        public string? Suffix { get; set; }
        public string Email { get; set; } = string.Empty;
        public string? Phone { get; set; }

        [NotMapped]
        public string FullName =>
            string.Join(" ", new[] { FirstName, MiddleName, LastName, Suffix }.Where(v => !string.IsNullOrWhiteSpace(v)));

        public string PasswordHash { get; set; } = string.Empty;
        public int RoleId { get; set; }
        public string Status { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public int? BranchId { get; set; }

        [NotMapped]
        public int PersonId { get => UserId; set { } }

        [NotMapped]
        public virtual Person? Person
        {
            get => new Person
            {
                PersonId = UserId,
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

        public virtual Role Role { get; set; } = null!;
        public virtual Branch? Branch { get; set; }
    }
}
