using System;
using System.Collections.Generic;

namespace NEXA.Model
{
    // ============================================
    // DTO - not a stored entity, so it does NOT inherit BaseEntity.
    // It's a computed snapshot assembled on-demand from Deal/Lead/Activity/
    // SupportTicket data for the Manager's dashboard screens.
    // ============================================
    public class TeamPerformanceSnapshot
    {
        public int DealsClosed { get; set; }
        public int CallsMade { get; set; }
        public int TicketsResolved { get; set; }
        public double LeadConversionRate { get; set; }   // Leads -> Customers, as a %
        public double DealsWonVsLostRatio { get; set; }
        public List<StaffPerformance> StaffBreakdown { get; set; } = new List<StaffPerformance>();
    }

    public class StaffPerformance
    {
        public int UserId { get; set; }
        public string StaffName { get; set; }
        public int DealsClosed { get; set; }
        public decimal RevenueGenerated { get; set; }
    }
}