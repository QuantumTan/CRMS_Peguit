using CRMS_Peguit.domain.entities;

namespace CRMS_Peguit.domain.Common;

public static class DealCommercialRules
{
    public static string? Validate(Deal deal)
    {
        if (deal.CustomerId <= 0 || deal.PropertyId <= 0)
            return "A valid customer and property are required.";
        if (deal.Value <= 0) return "Deal value must be greater than zero.";
        if (deal.CommissionRate < 0 || deal.CommissionRate > 1)
            return "Commission rate must be between 0% and 100%.";
        if (deal.DownPaymentPercent is < 0 or > 100)
            return "Down payment must be between 0% and 100%.";
        if (deal.ReservationFee is < 0 || deal.ReservationFee > deal.Value)
            return "Reservation fee must be between zero and the deal value.";
        if (!string.Equals(deal.PaymentScheme, "Spot Cash", StringComparison.OrdinalIgnoreCase) &&
            deal.DownPaymentAmount + (deal.ReservationFee ?? 0) > deal.Value)
            return "Reservation fee plus down payment cannot exceed the deal value.";
        return null;
    }

    // Keep approved clause history when editing a transaction; remove only deselected rows.
    public static void ApplyClauseSelection(Deal target, Deal source)
    {
        if (!source.ClauseSelectionProvided) return;
        target.MarkClauseSelectionProvided();
        var ids = source.DealClauses.Select(c => c.ClauseId).ToHashSet(StringComparer.OrdinalIgnoreCase);
        foreach (var clause in target.DealClauses.Where(c => !ids.Contains(c.ClauseId)).ToList())
            target.DealClauses.Remove(clause);
        foreach (var clause in source.DealClauses)
            if (!target.DealClauses.Any(c => string.Equals(c.ClauseId, clause.ClauseId, StringComparison.OrdinalIgnoreCase)))
                target.DealClauses.Add(new DealClause
                {
                    ClauseId = clause.ClauseId, Title = clause.Title, ClauseText = clause.ClauseText,
                    IsApproved = clause.IsApproved, ApprovedAt = clause.ApprovedAt, CreatedAt = clause.CreatedAt
                });
    }
}
