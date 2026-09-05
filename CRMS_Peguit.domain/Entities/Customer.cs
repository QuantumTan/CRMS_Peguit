using System;
using System.Collections.Generic;
using System.Text;

namespace CRMS_Peguit.domain.entities
{
    public class Customer
    {
        public int CustomerId { get; set; }

        public int TenantId { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? Phone { get; set; }
        public string? Email { get; set; }
        public string Type { get; set; } = string.Empty;
        public int? AssignedAgentId { get; set; }
        public string Status { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
    }
}