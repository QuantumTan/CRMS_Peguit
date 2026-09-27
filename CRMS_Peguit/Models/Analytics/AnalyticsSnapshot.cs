using System.Collections.Generic;

namespace CRMS_Peguit.winforms.Models.Analytics
{
    public class AnalyticsSnapshot
    {
        public int TotalDealsClosed { get; set; }
        public decimal TotalSalesVolume { get; set; }
        public decimal TotalCommissionEarned { get; set; }
        public decimal ActivePipelineValue { get; set; }
        public decimal AverageDealSize { get; set; }
        public int ActiveLeads { get; set; }
        public double LeadConversionRate { get; set; }
        public double WinRate { get; set; }
        public int OpenSupportTickets { get; set; }
        public double AverageDaysToClose { get; set; }
        public int ActivePropertiesCount { get; set; }
        public decimal ActiveInventoryValue { get; set; }
        
        public List<MonthlyMetric> DealsOverTime { get; set; } = new();
        public LeadFunnelData? LeadFunnel { get; set; }
        public WonLostData? DealsWonVsLost { get; set; }
        public TicketBreakdownData? TicketBreakdown { get; set; }
        
        public List<AgentPerformance>? TopAgents { get; set; }
        public List<SourceMetric>? LeadSourceBreakdown { get; set; }
        public List<PropertyDistributionMetric>? PropertyDistribution { get; set; }
        
        public List<ActivityFeedItem> RecentActivity { get; set; } = new();
    }
}
