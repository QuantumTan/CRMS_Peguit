using System;
using System.Collections.Generic;
using System.Text;

namespace CRMS_Peguit.domain.entities
{
    public enum TenantTier
    {
        TenantA = 1,
        TenantB = 2,
        TenantC = 3,
        Master = 99
    }

    public class Subscription
    {
        public const string TierTenantA = "Tenant A";
        public const string TierTenantB = "Tenant B";
        public const string TierTenantC = "Tenant C";

        public int SubscriptionId { get; set; }

        public int CompanyId { get; set; }
        public string PlanName { get; set; } = TierTenantA;
        public DateTime StartDate { get; set; } = DateTime.UtcNow;
        public DateTime? EndDate { get; set; }
        public decimal BillingAmount { get; set; }
        public string Status { get; set; } = "Active";

        public Company? Company { get; set; }

        public TenantTier Tier => ParseTier(PlanName);

        public static TenantTier ParseTier(string? planName)
        {
            if (string.IsNullOrWhiteSpace(planName)) return TenantTier.TenantA;

            var normalized = planName.Trim().ToLowerInvariant();
            if (normalized.Contains("master") || normalized.Contains("superadmin") || normalized.Contains("super admin"))
                return TenantTier.Master;
            if (normalized.Contains("tenant c") || normalized.Contains("tenantc") || normalized.Contains("enterprise"))
                return TenantTier.TenantC;
            if (normalized.Contains("tenant b") || normalized.Contains("tenantb") || normalized.Contains("professional") || normalized.Contains("pro"))
                return TenantTier.TenantB;
            return TenantTier.TenantA;
        }
    }
}
