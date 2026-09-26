using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using CRMS_Peguit.domain.Common;

namespace CRMS_Peguit.winforms.Services
{
    public class AnalyticsApiService : BaseApiService
    {
        public AnalyticsApiService(string? baseUrl = null, HttpClient? httpClient = null)
            : base(baseUrl, httpClient)
        {
        }

        public AnalyticsSnapshot GetSnapshot(DateRangeFilter range) =>
            Post<DateRangeFilter, AnalyticsSnapshot>("api/analytics/snapshot", range) ?? new AnalyticsSnapshot();

        public List<MonthlyMetric> GetDealsOverTime(DateRangeFilter range) =>
            Post<DateRangeFilter, List<MonthlyMetric>>("api/analytics/deals-over-time", range) ?? new();

        public LeadFunnelData GetLeadFunnel(DateRangeFilter range) =>
            Post<DateRangeFilter, LeadFunnelData>("api/analytics/lead-funnel", range) ?? new();

        public WonLostData GetDealsWonVsLost(DateRangeFilter range) =>
            Post<DateRangeFilter, WonLostData>("api/analytics/deals-won-lost", range) ?? new();

        public TicketBreakdownData GetTicketBreakdown(DateRangeFilter range) =>
            Post<DateRangeFilter, TicketBreakdownData>("api/analytics/ticket-breakdown", range) ?? new();

        public List<AgentPerformance> GetTopAgents(DateRangeFilter range) =>
            Post<DateRangeFilter, List<AgentPerformance>>("api/analytics/top-agents", range) ?? new();

        public List<SourceMetric> GetLeadSourceBreakdown(DateRangeFilter range) =>
            Post<DateRangeFilter, List<SourceMetric>>("api/analytics/lead-sources", range) ?? new();

        public List<PropertyDistributionMetric> GetPropertyDistribution(DateRangeFilter range) =>
            Post<DateRangeFilter, List<PropertyDistributionMetric>>("api/analytics/property-distribution", range) ?? new();

        public List<ActivityFeedItem> GetRecentActivityFeed(int count = 15) =>
            Get<List<ActivityFeedItem>>($"api/analytics/recent-activity?count={count}") ?? new();

        public List<AnalyticsDetailRow> GetDrillDownRows(DateRangeFilter range) =>
            Post<DateRangeFilter, List<AnalyticsDetailRow>>("api/analytics/drilldown", range) ?? new();
    }
}
