using System;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;

namespace CRMS_Peguit.domain.entities
{
    public class User
    {
        public int UserId { get; set; }
        public int PersonId { get; set; }

        private string? _firstName;
        private string? _middleName;
        private string? _lastName;
        private string? _suffix;
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

        public string Email
        {
            get => Person?.Email ?? _email ?? string.Empty;
            set
            {
                _email = value;
                if (Person != null) Person.Email = value;
            }
        }

        private string? _phone;
        [NotMapped]
        public string? Phone
        {
            get => Person?.Phone ?? _phone;
            set
            {
                _phone = value;
                if (Person != null) Person.Phone = value;
            }
        }

        [NotMapped]
        public string FullName =>
            Person?.FullName ??
            string.Join(" ", new[] { FirstName, MiddleName, LastName, Suffix }.Where(v => !string.IsNullOrWhiteSpace(v)));

        public string PasswordHash { get; set; } = string.Empty;
        public int RoleId { get; set; }
        public string Status { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public int? BranchId { get; set; }

        public virtual Person Person { get; set; } = null!;
        public virtual Role Role { get; set; } = null!;
        public virtual Branch? Branch { get; set; }
    }
}
