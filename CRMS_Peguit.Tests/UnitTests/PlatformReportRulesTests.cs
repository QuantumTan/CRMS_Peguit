using CRMS_Peguit.domain.Common;

namespace CRMS_Peguit.Tests.UnitTests;

public class PlatformReportRulesTests
{
    private static readonly DateTime Now = new(2026, 10, 1, 12, 0, 0, DateTimeKind.Utc);

    [Theory]
    [InlineData(0, -30, 30, "Not subscribed")]
    [InlineData(1, 1, 30, "Scheduled")]
    [InlineData(1, -30, -1, "Expired")]
    [InlineData(1, -30, 7, "Expiring Soon")]
    [InlineData(1, -30, 30, "Active")]
    public void ReportStatusRespectsActualSubscriptionDates(int id, int start, int end, string expected)
    {
        var subscription = new TenantSubscriptionDto
        {
            SubscriptionId = id, StartDate = Now.AddDays(start), EndDate = Now.AddDays(end)
        };
        Assert.Equal(expected, PlatformReportRules.SubscriptionStatus(subscription, Now));
    }

    [Fact]
    public void CurrentMrrExcludesExpiredScheduledAndMissingSubscriptions()
    {
        var subscriptions = new[]
        {
            new TenantSubscriptionDto { SubscriptionId = 1, StartDate = Now.AddMonths(-1), EndDate = Now.AddDays(30), BillingAmount = 2500 },
            new TenantSubscriptionDto { SubscriptionId = 2, StartDate = Now.AddMonths(-1), EndDate = Now.AddDays(3), BillingAmount = 5000 },
            new TenantSubscriptionDto { SubscriptionId = 3, StartDate = Now.AddMonths(-1), EndDate = Now.AddDays(-1), BillingAmount = 10000 },
            new TenantSubscriptionDto { SubscriptionId = 4, StartDate = Now.AddDays(1), BillingAmount = 10000 },
            new TenantSubscriptionDto { SubscriptionId = 0, BillingAmount = 2500 }
        };
        Assert.Equal(7500m, PlatformReportRules.CurrentMrr(subscriptions, Now));
    }
}
