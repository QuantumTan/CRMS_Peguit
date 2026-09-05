using System;
using System.Collections.Generic;
using System.Text;

namespace CRMS_Peguit.domain.entities
{
    public class PropertyShowingDetail
    {
        public int ShowingDetailId { get; set; }

        public int TenantId { get; set; }
        public int ActivityId { get; set; }
        public int PropertyId { get; set; }
        public DateTime? ScheduledDate { get; set; }
        public string? FeedbackNotes { get; set; }
    }
}