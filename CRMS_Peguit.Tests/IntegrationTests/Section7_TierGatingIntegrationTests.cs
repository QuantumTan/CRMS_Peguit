using System;
using System.Linq;
using CRMS_Peguit.domain.entities;
using CRMS_Peguit.infrastructure.data;
using CRMS_Peguit.winforms.Auth;
using CRMS_Peguit.winforms.Models.Services;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace CRMS_Peguit.Tests.IntegrationTests
{
    public class Section7_TierGatingIntegrationTests
    {
        public Section7_TierGatingIntegrationTests()
        {
            Environment.SetEnvironmentVariable("CRMS_CONNECTION", DbConfiguration.GetLocalConnectionString());
        }

        #region 7.1 - 7.3 Tier Capability Access Matrix

        [Fact]
        public void TierGating_TierA_CannotAccessBI_Or_Actions()
        {
            // Requirement 7.1:
            // "A tenant on Tier A (Basic) cannot access Business Intelligence/Analytics or Actions
            //  (assignment workflows, Follow-Ups, Notifications)"

            CurrentSession.Start(1, 1, "Tenant A Admin", "tenanta_admin@test.com", "Admin", null, false, tier: TenantTier.TenantA);

            Assert.Equal(TenantTier.TenantA, CurrentSession.TenantTier);
            Assert.True(CurrentSession.CanAccessMainTransaction);
            Assert.True(CurrentSession.CanAccessDataCollection);

            // Tier A cannot access Business Intelligence
            Assert.False(CurrentSession.CanAccessBusinessIntelligence);
            Assert.False(CurrentSession.CanAccess("Analytics"));
            Assert.False(CurrentSession.CanAccess("Reports"));

            // Tier A cannot access Actions / Automation
            Assert.False(CurrentSession.CanAccessActions);
            Assert.False(CurrentSession.CanAccess("Campaigns"));
            Assert.False(CurrentSession.CanAccess("TasksReminders"));
            Assert.False(CurrentSession.CanAccess("FollowUps"));

            // Tier A cannot access Branching
            Assert.False(CurrentSession.CanAccessBranching);
            Assert.False(CurrentSession.CanAccess("Branching"));
        }

        [Fact]
        public void TierGating_TierB_CanAccessBIAndActions_CannotAccessBranching()
        {
            // Requirement 7.2:
            // "A tenant on Tier B (Standard) can access BI/Actions but not Branching -> confirm accordingly."

            CurrentSession.Start(2, 2, "Tenant B Admin", "tenantb_admin@test.com", "Admin", null, false, tier: TenantTier.TenantB);

            Assert.Equal(TenantTier.TenantB, CurrentSession.TenantTier);
            Assert.True(CurrentSession.CanAccessMainTransaction);
            Assert.True(CurrentSession.CanAccessDataCollection);

            // Tier B CAN access Business Intelligence
            Assert.True(CurrentSession.CanAccessBusinessIntelligence);

            // Tier B CAN access Actions
            Assert.True(CurrentSession.CanAccessActions);

            // Tier B CANNOT access Branching
            Assert.False(CurrentSession.CanAccessBranching);
            Assert.False(CurrentSession.CanAccess("Branching"));
        }

        [Fact]
        public void TierGating_TierC_CanAccessEverything_IncludingBranching()
        {
            // Requirement 7.3:
            // "A tenant on Tier C (Premium) can access everything including multi-branch features."

            CurrentSession.Start(3, 3, "Tenant C Admin", "tenantc_admin@test.com", "Admin", null, false, tier: TenantTier.TenantC);

            Assert.Equal(TenantTier.TenantC, CurrentSession.TenantTier);
            Assert.True(CurrentSession.CanAccessMainTransaction);
            Assert.True(CurrentSession.CanAccessDataCollection);
            Assert.True(CurrentSession.CanAccessBusinessIntelligence);
            Assert.True(CurrentSession.CanAccessActions);
            Assert.True(CurrentSession.CanAccessBranching);
            Assert.True(CurrentSession.CanAccess("Branching"));
        }

        #endregion

        #region 7.4 Downgrade Data Preservation

        [Fact]
        public void TierGating_DowngradingTier_PreservesExistingData_WithoutDeletion()
        {
            // Requirement 7.4:
            // "Downgrading a tenant's tier does NOT delete any previously-created data
            //  (e.g. existing Branch records survive a Premium -> Basic downgrade, just become inaccessible until re-upgraded)."

            // In Tenant 3 (Tier C), branches exist in the database:
            using var db3 = LocalDb.CreateContext(3);
            int branchCountBefore = db3.Branches.Count();
            Assert.True(branchCountBefore > 0);

            // Simulate downgrade of session to Tier A
            CurrentSession.Start(3, 3, "Downgraded Admin", "tenantc_admin@test.com", "Admin", null, false, tier: TenantTier.TenantA);

            // Inaccessible in session
            Assert.False(CurrentSession.CanAccessBranching);

            // Verify underlying database records are untouched
            int branchCountAfter = db3.Branches.Count();
            Assert.Equal(branchCountBefore, branchCountAfter);
        }

        #endregion

        #region 7.5 Subscription Calculation & Extension Math

        [Fact]
        public void Subscription_StatusCalculation_ActiveExpiringSoonExpired()
        {
            // Requirement 7.5:
            // "Recording a payment correctly extends Subscription.EndDate and recalculates Status (Active/Expiring Soon/Expired) automatically."

            var refDate = new DateTime(2026, 9, 27, 12, 0, 0, DateTimeKind.Utc);

            // 1. Expired (EndDate < refDate)
            string statusPast = Subscription.CalculateStatus(refDate.AddDays(-1), refDate);
            Assert.Equal("Expired", statusPast);

            // 2. Expiring Soon (0 <= days <= 7)
            string statusExpiring = Subscription.CalculateStatus(refDate.AddDays(5), refDate);
            Assert.Equal("Expiring Soon", statusExpiring);

            // 3. Active (> 7 days)
            string statusActive = Subscription.CalculateStatus(refDate.AddDays(30), refDate);
            Assert.Equal("Active", statusActive);
        }

        [Fact]
        public void Subscription_CalculateExtension_ExtendsCorrectly()
        {
            var refDate = new DateTime(2026, 9, 27, 12, 0, 0, DateTimeKind.Utc);
            var currentExpiry = refDate.AddDays(20); // Active, expiring in 20 days

            // Renew before expiry starts from current EndDate
            var (newEndDate, newStatus) = Subscription.CalculateExtension(currentExpiry, refDate, "Tenant B (Standard)", refDate);
            Assert.Equal(currentExpiry.AddMonths(1).Date, newEndDate.Date);
            Assert.Equal("Active", newStatus);

            // Lapsed renewal (current expiry was 10 days ago) starts from payment date
            var lapsedExpiry = refDate.AddDays(-10);
            var (renewedEndDate, renewedStatus) = Subscription.CalculateExtension(lapsedExpiry, refDate, "Tenant B (Standard)", refDate);
            Assert.Equal(refDate.AddMonths(1).Date, renewedEndDate.Date);
            Assert.Equal("Active", renewedStatus);

            // Annual plan adds 1 year
            var (annualEnd, annualStatus) = Subscription.CalculateExtension(currentExpiry, refDate, "Tenant C Annual Plan", refDate);
            Assert.Equal(currentExpiry.AddYears(1).Date, annualEnd.Date);
            Assert.Equal("Active", annualStatus);
        }

        #endregion
    }
}
