using System;
using CRMS_Peguit.domain.entities;

namespace CRMS_Peguit.domain.Common
{
    public class TimelineItemDto
    {
        public string Id { get; set; } = string.Empty;
        public string Source { get; set; } = "Manual";
        public string Type { get; set; } = "Call";
        public string Category { get; set; } = "Calls";
        public string Title { get; set; } = string.Empty;
        public string? Notes { get; set; }
        public DateTime Timestamp { get; set; }
        public string ActorName { get; set; } = "System";
        public CallOutcome? Outcome { get; set; }
        public int? DurationMinutes { get; set; }
        public int? RelatedCustomerId { get; set; }
        public int? RelatedLeadId { get; set; }
        public string ClientName { get; set; } = string.Empty;
        public string ClientType { get; set; } = "Customer";
        public int? RawActivityId { get; set; }
        public bool CanCreateFollowUp { get; set; } = true;
    }
}
