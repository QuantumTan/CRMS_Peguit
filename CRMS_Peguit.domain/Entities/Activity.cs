using System;
using System.Collections.Generic;
using System.Text;

namespace CRMS_Peguit.domain.entities
{
    public class Activity
    {
        public int ActivityId { get; set; }

        public int TenantId { get; set; }
        public string Type { get; set; } = string.Empty;
        public int? RelatedLeadId { get; set; }
        public int? RelatedCustomerId { get; set; }
        public int LoggedByAgentId { get; set; }
        public string? Notes { get; set; }
        public DateTime ActivityDate { get; set; }
    }
}
