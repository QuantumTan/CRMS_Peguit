using System;

namespace CRMS_Peguit.winforms.Models.Analytics
{
    public class SalesReportRow
    {
        public string DealRef { get; set; } = string.Empty;
        public string CustomerName { get; set; } = string.Empty;
        public string PropertyAddress { get; set; } = string.Empty;
        public string PropertyType { get; set; } = string.Empty;
        public string AgentName { get; set; } = string.Empty;
        public decimal DealValue { get; set; }
        public decimal Commission { get; set; }
        public string Stage { get; set; } = string.Empty;
        public string ExpectedOrClosedDate { get; set; } = string.Empty;

        // Backward compatibility properties
        public decimal Value { get => DealValue; set => DealValue = value; }
        public string ClosedDate { get => ExpectedOrClosedDate; set => ExpectedOrClosedDate = value; }

        public SalesReportRow() { }

        public SalesReportRow(string dealRef, string customerName, string propertyAddress, string propertyType, string agentName, decimal dealValue, decimal commission, string stage, string date)
        {
            DealRef = dealRef;
            CustomerName = customerName;
            PropertyAddress = propertyAddress;
            PropertyType = propertyType;
            AgentName = agentName;
            DealValue = dealValue;
            Commission = commission;
            Stage = stage;
            ExpectedOrClosedDate = date;
        }
    }
    
    public class LeadProgressRow
    {
        public string LeadName { get; set; } = string.Empty;
        public string Source { get; set; } = string.Empty;
        public string Priority { get; set; } = string.Empty;
        public decimal EstimatedBudget { get; set; }
        public string AgentName { get; set; } = string.Empty;
        public string Stage { get; set; } = string.Empty;
        public int DaysInPipeline { get; set; }
        public string ConvertedToCustomer { get; set; } = string.Empty;
        public string CreatedDate { get; set; } = string.Empty;

        // Backward compatibility
        public int DaysInStage { get => DaysInPipeline; set => DaysInPipeline = value; }

        public LeadProgressRow() { }

        public LeadProgressRow(string leadName, string source, string priority, decimal budget, string agentName, string stage, int daysInPipeline, string converted, string createdDate)
        {
            LeadName = leadName;
            Source = source;
            Priority = priority;
            EstimatedBudget = budget;
            AgentName = agentName;
            Stage = stage;
            DaysInPipeline = daysInPipeline;
            ConvertedToCustomer = converted;
            CreatedDate = createdDate;
        }
    }
    
    public class CommissionReportRow
    {
        public string DealRef { get; set; } = string.Empty;
        public string PropertyAddress { get; set; } = string.Empty;
        public string CustomerName { get; set; } = string.Empty;
        public string AgentName { get; set; } = string.Empty;
        public decimal DealValue { get; set; }
        public decimal GrossCommission { get; set; }
        public decimal AgentSplitPercent { get; set; }
        public decimal AgentPayoutAmount { get; set; }
        public decimal BrokerageRetainedAmount { get; set; }
        public string CloseDate { get; set; } = string.Empty;
        public string SettlementStatus { get; set; } = "Settled";

        // Backward compatibility
        public decimal CommissionRate { get => AgentSplitPercent; set => AgentSplitPercent = value; }
        public decimal CommissionAmount { get => AgentPayoutAmount; set => AgentPayoutAmount = value; }

        public CommissionReportRow() { }

        public CommissionReportRow(string dealRef, string propertyAddress, string customerName, string agentName, decimal dealValue, decimal grossCommission, decimal agentSplit, decimal agentPayout, decimal brokerageRetained, string closeDate, string settlementStatus = "Settled")
        {
            DealRef = dealRef;
            PropertyAddress = propertyAddress;
            CustomerName = customerName;
            AgentName = agentName;
            DealValue = dealValue;
            GrossCommission = grossCommission;
            AgentSplitPercent = agentSplit;
            AgentPayoutAmount = agentPayout;
            BrokerageRetainedAmount = brokerageRetained;
            CloseDate = closeDate;
            SettlementStatus = settlementStatus;
        }
    }

    public class PropertyInventoryReportRow
    {
        public string PropertyRef { get; set; } = string.Empty;
        public string Address { get; set; } = string.Empty;
        public string PropertyType { get; set; } = string.Empty;
        public decimal ListingPrice { get; set; }
        public string Status { get; set; } = string.Empty;
        public string ListingAgent { get; set; } = string.Empty;
        public int DaysOnMarket { get; set; }
        public int AssociatedDeals { get; set; }
        public string ListedDate { get; set; } = string.Empty;

        public PropertyInventoryReportRow() { }

        public PropertyInventoryReportRow(string propertyRef, string address, string propertyType, decimal price, string status, string agent, int daysOnMarket, int deals, string listedDate)
        {
            PropertyRef = propertyRef;
            Address = address;
            PropertyType = propertyType;
            ListingPrice = price;
            Status = status;
            ListingAgent = agent;
            DaysOnMarket = daysOnMarket;
            AssociatedDeals = deals;
            ListedDate = listedDate;
        }
    }
    
    public class TicketResolutionRow
    {
        public string TicketNumber { get; set; } = string.Empty;
        public string CustomerName { get; set; } = string.Empty;
        public string Category { get; set; } = string.Empty;
        public string Priority { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public string AgentName { get; set; } = string.Empty;
        public string OpenedDate { get; set; } = string.Empty;
        public string ResolvedDate { get; set; } = string.Empty;
        public string ResolutionTime { get; set; } = string.Empty;
        public string SlaMet { get; set; } = string.Empty;

        public TicketResolutionRow() { }

        public TicketResolutionRow(string ticketNumber, string customerName, string category, string priority, string status, string agentName, string openedDate, string resolvedDate, string resolutionTime, string slaMet)
        {
            TicketNumber = ticketNumber;
            CustomerName = customerName;
            Category = category;
            Priority = priority;
            Status = status;
            AgentName = agentName;
            OpenedDate = openedDate;
            ResolvedDate = resolvedDate;
            ResolutionTime = resolutionTime;
            SlaMet = slaMet;
        }
    }
    
    public class AgentActivityRow
    {
        public string AgentName { get; set; } = string.Empty;
        public int ActiveLeads { get; set; }
        public int LeadsConverted { get; set; }
        public double ConversionRate { get; set; }
        public int DealsClosed { get; set; }
        public decimal TotalSalesVolume { get; set; }
        public decimal TotalCommissionEarned { get; set; }
        public int FollowUpsCompleted { get; set; }
        public int TicketsResolved { get; set; }

        // Backward compatibility
        public int LeadsWorked { get => ActiveLeads; set => ActiveLeads = value; }

        public AgentActivityRow() { }

        public AgentActivityRow(string agentName, int activeLeads, int leadsConverted, double conversionRate, int dealsClosed, decimal totalSalesVolume, decimal totalCommissionEarned, int followUpsCompleted, int ticketsResolved)
        {
            AgentName = agentName;
            ActiveLeads = activeLeads;
            LeadsConverted = leadsConverted;
            ConversionRate = conversionRate;
            DealsClosed = dealsClosed;
            TotalSalesVolume = totalSalesVolume;
            TotalCommissionEarned = totalCommissionEarned;
            FollowUpsCompleted = followUpsCompleted;
            TicketsResolved = ticketsResolved;
        }
    }

    public class ReportHeader
    {
        public string ReportName { get; set; } = string.Empty;
        public string DateRange { get; set; } = string.Empty;
        public string GeneratedBy { get; set; } = string.Empty;
        public DateTime GeneratedAt { get; set; } = DateTime.UtcNow;
    }
}
