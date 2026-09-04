using System;
using System.Collections.Generic;
using System.Text;

namespace CRMS_Peguit.domain.entities
{
    public class Subscription
    {
        public int SubscriptionId { get; set; }
        public string PlanName { get; set; } = string.Empty;
        public DateTime StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public decimal BillingAmount { get; set; }
        public string Status { get; set; } = string.Empty;
    }
}
