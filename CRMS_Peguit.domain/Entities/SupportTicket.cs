using System;
using System.Collections.Generic;
using System.Text;

namespace CRMS_Peguit.domain.entities
{
    public class SupportTicket
    {
        public int TicketId { get; set; }
        public int CustomerId { get; set; }
        public int RaisedByUserId { get; set; }
        public int? AssignedToUserId { get; set; }
        public string Description { get; set; } = string.Empty;
        public string Priority { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public DateTime? ResolvedAt { get; set; }
    }
}