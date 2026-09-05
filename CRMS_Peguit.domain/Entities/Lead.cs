using System;
using System.Collections.Generic;
using System.Text;

namespace CRMS_Peguit.domain.entities
{
    public class Lead
    {
        public int LeadId { get; set; }

        public int TenantId { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? Phone { get; set; }
        public string? Email { get; set; }
        public string? Source { get; set; }
        public int? AssignedAgentId { get; set; }
        public string Stage { get; set; } = string.Empty;
        public int? ConvertedCustomerId { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}
