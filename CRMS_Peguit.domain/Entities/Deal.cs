using System;
using System.Collections.Generic;
using System.Text;

namespace CRMS_Peguit.domain.entities
{
    public class Deal
    {
        public int DealId { get; set; }
        public int CustomerId { get; set; }
        public int PropertyId { get; set; }
        public int AgentId { get; set; }
        public decimal Value { get; set; }
        public decimal CommissionRate { get; set; }
        public string Stage { get; set; } = string.Empty;
        public DateTime? ExpectedCloseDate { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}