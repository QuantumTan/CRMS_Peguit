using CRMS_Peguit.domain.entities;

namespace CRMS_Peguit.domain.Common;

public static class PlatformReportRules
{
    public static string SubscriptionStatus(TenantSubscriptionDto subscription, DateTime? referenceNow = null)
    {
        var now = referenceNow ?? DateTime.UtcNow;
        if (subscription.SubscriptionId == 0) return "Not subscribed";
        if (subscription.StartDate > now) return "Scheduled";
        return Subscription.CalculateStatus(subscription.EndDate, now);
    }

    public static decimal CurrentMrr(IEnumerable<TenantSubscriptionDto> subscriptions, DateTime? referenceNow = null)
    {
        var now = referenceNow ?? DateTime.UtcNow;
        return subscriptions.Where(s => SubscriptionStatus(s, now) is "Active" or "Expiring Soon")
            .Sum(s => s.BillingAmount);
    }
}
