using System;

namespace CRMS_Peguit.domain.entities
{
    public class MarketUpdateLog
    {
        public int LogId { get; set; }
        public int TenantId { get; set; } = 1;

        public int? CustomerId { get; set; }
        public string CustomerName { get; set; } = string.Empty;
        public string RecipientEmail { get; set; } = string.Empty;

        public string PropertyAddress { get; set; } = string.Empty;
        public string PropertyType { get; set; } = string.Empty;
        public decimal OriginalPrice { get; set; }
        public decimal EstimatedValue { get; set; }
        public decimal EquityGainAmount { get; set; }
        public decimal EquityGainPercent { get; set; }

        public string EmailFormat { get; set; } = "Html"; // "Html" or "PlainText"
        public string Status { get; set; } = "Sent";      // "Sent", "Failed", "Skipped"
        public string? ErrorMessage { get; set; }
        public string TriggerType { get; set; } = "Scheduler"; // "Scheduler", "ManualBatch", "TestDispatch"

        public DateTime SentAt { get; set; } = DateTime.UtcNow;

        public virtual Customer? Customer { get; set; }
    }
}
