using System;

namespace CRMS_Peguit.domain.Common
{
    public sealed record CustomerPickerItem(int CustomerId, string FullName, string? Email)
    {
        public override string ToString() => FullName;
    }

    public sealed record AgentPickerItem(int UserId, string FullName, string Email)
    {
        public override string ToString() => FullName;
    }

    public class PropertyCounts
    {
        public int Total { get; set; }
        public int Available { get; set; }
        public int Pending { get; set; }
        public int Sold { get; set; }
    }

    public class BranchItemDto
    {
        public int BranchId { get; set; }
        public int TenantId { get; set; }
        public string BranchCode { get; set; } = string.Empty;
        public string BranchName { get; set; } = string.Empty;
        public string Address { get; set; } = string.Empty;
        public string Phone { get; set; } = string.Empty;
        public bool IsActive { get; set; }
        public DateTime CreatedAt { get; set; }
        public int AssignedAgentsCount { get; set; }
        public int PropertiesCount { get; set; }
        public int LeadsCount { get; set; }
        public int DealsCount { get; set; }
        public decimal TotalDealVolume { get; set; }
    }

    public class DealKpiResult
    {
        public int Total { get; set; }
        public decimal TotalVolume { get; set; }
        public int Offer { get; set; }
        public int Contract { get; set; }
        public decimal ContractVolume { get; set; }
        public int Closed { get; set; }
        public decimal ClosedVolume { get; set; }
        public int Lost { get; set; }
    }
}
