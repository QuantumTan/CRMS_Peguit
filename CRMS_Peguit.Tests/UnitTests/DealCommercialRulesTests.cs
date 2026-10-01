using System.Text.Json;
using CRMS_Peguit.domain.Common;
using CRMS_Peguit.domain.entities;

namespace CRMS_Peguit.Tests.UnitTests;

public class DealCommercialRulesTests
{
    private static Deal ValidDeal() => new()
    {
        CustomerId = 1, PropertyId = 1, Value = 1000m, CommissionRate = .03m,
        DownPaymentPercent = 20m, ReservationFee = 50m
    };

    [Theory]
    [InlineData(-1, 20, 50)]
    [InlineData(1.01, 20, 50)]
    [InlineData(.03, -1, 50)]
    [InlineData(.03, 101, 50)]
    [InlineData(.03, 20, -1)]
    [InlineData(.03, 20, 1001)]
    [InlineData(.03, 80, 300)]
    public void InvalidCommercialTermsAreRejected(double commission, double downPercent, double reservation)
    {
        var deal = ValidDeal();
        deal.CommissionRate = (decimal)commission;
        deal.DownPaymentPercent = (decimal)downPercent;
        deal.ReservationFee = (decimal)reservation;
        Assert.NotNull(DealCommercialRules.Validate(deal));
    }

    [Fact]
    public void ValidFinancingAndCashAgreeWithComputedBalances()
    {
        var deal = ValidDeal();
        Assert.Null(DealCommercialRules.Validate(deal));
        Assert.Equal(200m, deal.DownPaymentAmount);
        Assert.Equal(750m, deal.BalanceAmount);
        deal.PaymentScheme = "Spot Cash";
        Assert.Null(DealCommercialRules.Validate(deal));
        Assert.Equal(1000m, deal.DownPaymentAmount);
        Assert.Equal(0m, deal.BalanceAmount);
    }

    [Fact]
    public void ClauseEditsRetainExistingApprovalHistory()
    {
        var target = ValidDeal();
        var retained = new DealClause { DealClauseId = 7, ClauseId = "TTL-01" };
        target.DealClauses.Add(retained);
        target.DealClauses.Add(new DealClause { ClauseId = "TAX-01" });
        var edited = new Deal { ApprovedClauseIds = "TTL-01,FIN-01,FIN-01" };
        DealCommercialRules.ApplyClauseSelection(target, edited);
        Assert.Equal(2, target.DealClauses.Count);
        Assert.Contains(retained, target.DealClauses);
        Assert.DoesNotContain(target.DealClauses, c => c.ClauseId == "TAX-01");
    }

    [Fact]
    public void EmptyClauseSelectionSurvivesSyncSerialization()
    {
        var target = ValidDeal();
        target.DealClauses.Add(new DealClause { ClauseId = "TTL-01" });
        DealCommercialRules.ApplyClauseSelection(target, new Deal { ApprovedClauseIds = "" });
        var roundTrip = JsonSerializer.Deserialize<Deal>(JsonSerializer.Serialize(target))!;
        Assert.True(roundTrip.ClauseSelectionProvided);
        Assert.Empty(roundTrip.DealClauses);
    }

    [Fact]
    public void MissingClauseSelectionDoesNotDeleteClauses()
    {
        var target = ValidDeal();
        target.DealClauses.Add(new DealClause { ClauseId = "TTL-01" });
        DealCommercialRules.ApplyClauseSelection(target, new Deal());
        Assert.Single(target.DealClauses);
    }

    [Fact]
    public void ContractTermsSurviveSyncSerialization()
    {
        var deal = ValidDeal();
        deal.ApprovedClauseIds = "TTL-01,FIN-01";
        deal.Contingencies.Add(new DealContingency { ContingencyName = "Approval", IsSatisfied = true });
        var copy = JsonSerializer.Deserialize<Deal>(JsonSerializer.Serialize(deal))!;
        Assert.Equal(2, copy.DealClauses.Count);
        Assert.True(Assert.Single(copy.Contingencies).IsSatisfied);
    }
}
