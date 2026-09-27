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
        public ICollection<PaymentRecord> PaymentRecords { get; set; } = new List<PaymentRecord>();

        /// <summary>
        /// Computed "Paid Through" date: The EndDate of the period covered by the most recent payment record
        /// or the current subscription EndDate.
        /// </summary>
        public DateTime? PaidThroughDate => EndDate;

        /// <summary>
        /// Automatically determines Status based on EndDate:
        /// - Active: EndDate comfortably in the future (> 7 days from now)
        /// - Expiring Soon: within 7 days of EndDate (now <= EndDate <= now + 7 days)
        /// - Expired: EndDate has passed (< now)
        /// </summary>
        public string ComputedStatus => CalculateStatus(EndDate);

        public TenantTier Tier => ParseTier(PlanName);

        /// <summary>
        /// Calculates the status for a given end date relative to reference date (defaults to UTC now).
        /// </summary>
        public static string CalculateStatus(DateTime? endDate, DateTime? referenceNow = null)
        {
            if (!endDate.HasValue) return "Active";

            var now = (referenceNow ?? DateTime.UtcNow).Date;
            var exp = endDate.Value.Date;

            if (exp < now)
                return "Expired";

            var daysRemaining = (exp - now).TotalDays;
            if (daysRemaining <= 7)
                return "Expiring Soon";

            return "Active";
        }

        /// <summary>
        /// Core Extension Math:
        /// When recording a payment:
        /// - If renewing before expiry (current EndDate > payment date), extension starts from current EndDate.
        /// - If renewing after a lapse (current EndDate <= payment date), extension starts from payment date.
        /// - Plan period is +1 year for Annual/Year plans, or +1 month for standard plans.
        /// - Status is automatically recalculated.
        /// </summary>
        public static (DateTime NewEndDate, string NewStatus) CalculateExtension(
            DateTime? currentEndDate,
            DateTime paymentDate,
            string? planName,
            DateTime? referenceNow = null)
        {
            DateTime baseDate;
            if (currentEndDate.HasValue && currentEndDate.Value.Date > paymentDate.Date)
            {
                baseDate = currentEndDate.Value;
            }
            else
            {
                baseDate = paymentDate;
            }

            bool isAnnual = !string.IsNullOrWhiteSpace(planName) &&
                (planName.Contains("Annual", StringComparison.OrdinalIgnoreCase) ||
                 planName.Contains("Year", StringComparison.OrdinalIgnoreCase));

            DateTime newEndDate = isAnnual ? baseDate.AddYears(1) : baseDate.AddMonths(1);
            string newStatus = CalculateStatus(newEndDate, referenceNow);

            return (newEndDate, newStatus);
        }

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
