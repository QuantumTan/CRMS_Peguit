using System;
using System.Collections.Generic;

namespace CRMS_Peguit.domain.Common
{
    public class DateRangeFilter
    {
        public DateTime Start { get; set; }
        public DateTime End { get; set; }
        public string Label { get; set; } = string.Empty;

        public DateTime StartDate { get => Start; set => Start = value; }
        public DateTime EndDate { get => End; set => End = value; }

        public static DateRangeFilter ThisMonth()
        {
            var now = DateTime.UtcNow;
            return new DateRangeFilter
            {
                Start = new DateTime(now.Year, now.Month, 1, 0, 0, 0, DateTimeKind.Utc),
                End = now,
                Label = "This Month"
            };
        }

        public static DateRangeFilter ThisQuarter()
        {
            var now = DateTime.UtcNow;
            int quarter = (now.Month - 1) / 3 + 1;
            int startMonth = (quarter - 1) * 3 + 1;
            return new DateRangeFilter
            {
                Start = new DateTime(now.Year, startMonth, 1, 0, 0, 0, DateTimeKind.Utc),
                End = now,
                Label = "This Quarter"
            };
        }

        public static DateRangeFilter ThisYear()
        {
            var now = DateTime.UtcNow;
            return new DateRangeFilter
            {
                Start = new DateTime(now.Year, 1, 1, 0, 0, 0, DateTimeKind.Utc),
                End = now,
                Label = "This Year"
            };
        }

        public static DateRangeFilter Past12Months()
        {
            var now = DateTime.UtcNow;
            return new DateRangeFilter
            {
                Start = now.AddMonths(-12),
                End = now,
                Label = "Past 12 Months"
            };
        }

        public static DateRangeFilter AllTime()
        {
            return new DateRangeFilter
            {
                Start = new DateTime(2020, 1, 1, 0, 0, 0, DateTimeKind.Utc),
                End = DateTime.UtcNow.AddDays(1),
                Label = "All Time"
            };
        }

        public static DateRangeFilter Custom(DateTime start, DateTime end)
        {
            return new DateRangeFilter
            {
                Start = start,
                End = end,
                Label = "Custom"
            };
        }
    }

    public class MonthlyMetric
    {
        public string Month { get; set; } = string.Empty;
        public int Count { get; set; }
        public decimal Value { get; set; }
        public decimal TotalValue { get => Value; set => Value = value; }

        public MonthlyMetric() { }
        public MonthlyMetric(string month, int count, decimal value)
        {
            Month = month;
            Count = count;
            Value = value;
        }
    }

    public class LeadFunnelData
    {
        public int New { get; set; }
        public int Contacted { get; set; }
        public int Qualified { get; set; }
        public int Converted { get; set; }
        public int Lost { get; set; }

        public int NewCount { get => New; set => New = value; }
        public int ContactedCount { get => Contacted; set => Contacted = value; }
        public int QualifiedCount { get => Qualified; set => Qualified = value; }
        public int ConvertedCount { get => Converted; set => Converted = value; }
        public int LostCount { get => Lost; set => Lost = value; }

        public LeadFunnelData() { }
        public LeadFunnelData(int @new, int contacted, int qualified, int converted, int lost)
        {
            New = @new;
            Contacted = contacted;
            Qualified = qualified;
            Converted = converted;
            Lost = lost;
        }
    }

    public class WonLostData
    {
        public int Won { get; set; }
        public int Lost { get; set; }

        public int WonCount { get => Won; set => Won = value; }
        public int LostCount { get => Lost; set => Lost = value; }

        public WonLostData() { }
        public WonLostData(int won, int lost)
        {
            Won = won;
            Lost = lost;
        }
    }

    public class TicketBreakdownData
    {
        public int Open { get; set; }
        public int InProgress { get; set; }
        public int Resolved { get; set; }
        public int Overdue { get; set; }

        public int OpenCount { get => Open; set => Open = value; }
        public int InProgressCount { get => InProgress; set => InProgress = value; }
        public int ResolvedCount { get => Resolved; set => Resolved = value; }
        public int OverdueCount { get => Overdue; set => Overdue = value; }

        public TicketBreakdownData() { }
        public TicketBreakdownData(int open, int inProgress, int resolved, int overdue)
        {
            Open = open;
            InProgress = inProgress;
            Resolved = resolved;
            Overdue = overdue;
        }
    }

    public class AgentPerformance
    {
        public int UserId { get; set; }
        public string AgentName { get; set; } = string.Empty;
        public int DealsClosed { get; set; }
        public decimal CommissionEarned { get; set; }
        public decimal TotalValue { get; set; }

        public AgentPerformance() { }
        public AgentPerformance(int userId, string agentName, int dealsClosed, decimal commissionEarned)
        {
            UserId = userId;
            AgentName = agentName;
            DealsClosed = dealsClosed;
            CommissionEarned = commissionEarned;
        }
    }

    public class SourceMetric
    {
        public string Source { get; set; } = string.Empty;
        public int Count { get; set; }

        public SourceMetric() { }
        public SourceMetric(string source, int count)
        {
            Source = source;
            Count = count;
        }
    }

    public class ActivityFeedItem
    {
        public string Icon { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;

        public ActivityFeedItem() { }
        public ActivityFeedItem(string icon, string description, DateTime timestamp)
        {
            Icon = icon;
            Description = description;
            Timestamp = timestamp;
        }
    }

    public class PropertyDistributionMetric
    {
        public string PropertyType { get; set; } = string.Empty;
        public int Count { get; set; }
        public decimal TotalValue { get; set; }

        public PropertyDistributionMetric() { }
        public PropertyDistributionMetric(string propertyType, int count, decimal totalValue)
        {
            PropertyType = propertyType;
            Count = count;
            TotalValue = totalValue;
        }
    }

    public class AnalyticsDetailRow
    {
        public string RecordType { get; set; } = string.Empty;
        public string Reference { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public string AssignedTo { get; set; } = string.Empty;
        public decimal Value { get; set; }
        public DateTime Date { get; set; }
        public string Details { get; set; } = string.Empty;
    }

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
