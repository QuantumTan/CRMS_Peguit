namespace RealEstateCRM.Entities;

public class Subscription
{
    public int SubscriptionID { get; set; }
    public string PlanName { get; set; } = string.Empty;
    public DateTime StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public decimal BillingAmount { get; set; }
    public string Status { get; set; } = string.Empty;
}