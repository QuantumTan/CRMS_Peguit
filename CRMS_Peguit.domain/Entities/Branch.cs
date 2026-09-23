using System;
using System.Collections.Generic;

namespace CRMS_Peguit.domain.entities
{
    public class Branch
    {
        public int BranchId { get; set; }
        public int TenantId { get; set; } = 1;
        public string BranchCode { get; set; } = string.Empty;
        public string BranchName { get; set; } = string.Empty;
        public string Address { get; set; } = string.Empty;
        public string Phone { get; set; } = string.Empty;
        public int? ManagerUserId { get; set; }
        public bool IsActive { get; set; } = true;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public virtual ICollection<User> Users { get; set; } = new List<User>();
        public virtual ICollection<Property> Properties { get; set; } = new List<Property>();
        public virtual ICollection<Lead> Leads { get; set; } = new List<Lead>();
        public virtual ICollection<Deal> Deals { get; set; } = new List<Deal>();
    }
}
