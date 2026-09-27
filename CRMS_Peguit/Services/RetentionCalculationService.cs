using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CRMS_Peguit.domain.entities;
using CRMS_Peguit.infrastructure.data;
using Microsoft.EntityFrameworkCore;

namespace CRMS_Peguit.winforms.Services
{
    public static class RetentionCalculationService
    {
        public const string SegmentActiveDealExcluded = "Active Deal (Excluded)";
        public const string SegmentRepeatClient = "Repeat Client";
        public const string SegmentNewClient = "New Client";
        public const string SegmentRecentClient = "Recent Client";
        public const string SegmentAtRisk = "At Risk";
        public const string SegmentInactive = "Inactive";
        public const string SegmentProspective = "Prospective Client";

        public static readonly string[] AllSegments = new[]
        {
            SegmentRepeatClient,
            SegmentNewClient,
            SegmentRecentClient,
            SegmentAtRisk,
            SegmentInactive,
            SegmentProspective,
            SegmentActiveDealExcluded
        };

        public static readonly string[] ActiveDealStages = new[]
        {
            "offer",
            "reservation",
            "contractsigned",
            "contract",
            "contract signed"
        };

        /// <summary>
        /// Computes the customer's current retention segment based on Deal & Activity history.
        /// Evaluated strictly in priority order per NEXA specification.
        /// </summary>
        public static string EvaluateSegment(
            Customer customer,
            IEnumerable<Deal> deals,
            IEnumerable<Activity> activities,
            DateTime? referenceDate = null)
        {
            var now = referenceDate ?? DateTime.UtcNow;

            var customerDeals = deals.Where(d => d.CustomerId == customer.CustomerId).ToList();

            // 1. EXCLUSION — Active Deal in Progress
            bool hasActiveDeal = customerDeals.Any(d =>
                !string.IsNullOrWhiteSpace(d.Stage) &&
                ActiveDealStages.Contains(d.Stage.Trim().ToLowerInvariant()));

            if (hasActiveDeal)
            {
                return SegmentActiveDealExcluded;
            }

            // Closed deals history
            var closedDeals = customerDeals
                .Where(d => string.Equals(d.Stage, "Closed", StringComparison.OrdinalIgnoreCase))
                .OrderByDescending(d => d.ContractSignedDate ?? d.CreatedAt)
                .ToList();

            DateTime? latestClosedDate = closedDeals.FirstOrDefault() != null
                ? (closedDeals.First().ContractSignedDate ?? closedDeals.First().CreatedAt)
                : null;

            // Activity history
            var customerActivities = activities
                .Where(a => a.RelatedCustomerId == customer.CustomerId)
                .OrderByDescending(a => a.ActivityDate)
                .ToList();

            DateTime? latestActivityDate = customerActivities.FirstOrDefault()?.ActivityDate;

            // Find latest interaction (either closed deal or logged activity)
            DateTime? latestInteractionDate = null;
            if (latestClosedDate.HasValue && latestActivityDate.HasValue)
            {
                latestInteractionDate = latestClosedDate.Value > latestActivityDate.Value
                    ? latestClosedDate.Value
                    : latestActivityDate.Value;
            }
            else if (latestClosedDate.HasValue)
            {
                latestInteractionDate = latestClosedDate.Value;
            }
            else if (latestActivityDate.HasValue)
            {
                latestInteractionDate = latestActivityDate.Value;
            }

            int closedDealCount = closedDeals.Count;

            // 2. Deal closed within past 6 months (183 days):
            // Distinct between New Client (first-time client) and Recent Client (returning client renewing contact)
            if (latestClosedDate.HasValue)
            {
                double daysSinceLastClosed = (now - latestClosedDate.Value).TotalDays;
                if (daysSinceLastClosed <= 183)
                {
                    if (closedDealCount == 1)
                    {
                        return SegmentNewClient;
                    }
                    else if (closedDealCount >= 2)
                    {
                        return SegmentRecentClient;
                    }
                }
            }

            // 3. Repeat Client: >= 2 Closed Deals total, most recent closed between 6 and 24 months (183 to 730 days)
            if (closedDealCount >= 2 && latestClosedDate.HasValue)
            {
                double daysSinceLastClosed = (now - latestClosedDate.Value).TotalDays;
                if (daysSinceLastClosed <= 730)
                {
                    return SegmentRepeatClient;
                }
            }

            // If customer has either a closed deal or logged activity
            if (latestInteractionDate.HasValue)
            {
                double daysSinceInteraction = (now - latestInteractionDate.Value).TotalDays;

                // 4. At Risk: most recent Closed Deal OR logged Activity was 6 to 18 months ago (183 - 548 days)
                if (daysSinceInteraction > 183 && daysSinceInteraction <= 548)
                {
                    return SegmentAtRisk;
                }

                // 5. Inactive: most recent Closed Deal OR logged Activity was > 18 months ago (> 548 days)
                if (daysSinceInteraction > 548)
                {
                    return SegmentInactive;
                }
            }

            // 7. Fallback: Prospective Client (0 Closed Deals, no recent deals)
            return SegmentProspective;
        }

        /// <summary>
        /// Returns the default recommended real-estate service incentive for a given segment.
        /// </summary>
        public static string GetRecommendedIncentive(string segment)
        {
            return segment switch
            {
                SegmentNewClient => "Complimentary Move-In Vendor Directory & Settlement Review",
                SegmentRecentClient => "Post-Move Document Assistance & Tradesperson Network",
                SegmentRepeatClient => "Reduced Commission on Next Listing (0.75% Concession)",
                SegmentAtRisk => "Complimentary Home Equity & Neighborhood CMA Report",
                SegmentInactive => "Comprehensive Portfolio Property Valuation Consultation",
                SegmentProspective => "First-Time Buyer & Real Estate Investment Market Playbook",
                _ => "Complimentary Market Valuation & Advisory Consultation"
            };
        }

        /// <summary>
        /// Recalculates and updates the retention segment for a single customer.
        /// </summary>
        public static async Task<string> RecalculateCustomerAsync(RealEstateDbContext db, int customerId)
        {
            var customer = await db.Customers.FirstOrDefaultAsync(c => c.CustomerId == customerId && !c.IsDeleted);
            if (customer == null) return SegmentProspective;

            var deals = await db.Deals.AsNoTracking().Where(d => d.CustomerId == customerId).ToListAsync();
            var activities = await db.Activities.AsNoTracking().Where(a => a.RelatedCustomerId == customerId).ToListAsync();

            string segment = EvaluateSegment(customer, deals, activities);
            customer.CurrentRetentionSegment = segment;
            customer.RetentionSegmentCalculatedAt = DateTime.UtcNow;

            await db.SaveChangesAsync();
            return segment;
        }

        /// <summary>
        /// Recalculates retention segments across all non-deleted customers for a tenant.
        /// </summary>
        public static async Task<int> RecalculateAllCustomersAsync(RealEstateDbContext db, int tenantId)
        {
            var customers = await db.Customers.Where(c => !c.IsDeleted).ToListAsync();
            if (customers.Count == 0) return 0;

            var customerIds = customers.Select(c => c.CustomerId).ToList();
            var deals = await db.Deals.AsNoTracking().Where(d => customerIds.Contains(d.CustomerId)).ToListAsync();
            var activities = await db.Activities.AsNoTracking().Where(a => a.RelatedCustomerId.HasValue && customerIds.Contains(a.RelatedCustomerId.Value)).ToListAsync();

            var now = DateTime.UtcNow;
            int changedCount = 0;

            foreach (var customer in customers)
            {
                string newSegment = EvaluateSegment(customer, deals, activities, now);
                if (customer.CurrentRetentionSegment != newSegment)
                {
                    customer.CurrentRetentionSegment = newSegment;
                    changedCount++;
                }
                customer.RetentionSegmentCalculatedAt = now;
            }

            await db.SaveChangesAsync();
            return changedCount;
        }
    }
}
