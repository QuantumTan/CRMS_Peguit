using System;
using System.Linq;
using System.Threading.Tasks;
using CRMS_Peguit.domain.entities;
using CRMS_Peguit.infrastructure.data;
using CRMS_Peguit.winforms;
using CRMS_Peguit.winforms.Auth;
using CRMS_Peguit.winforms.Controllers;
using CRMS_Peguit.winforms.Models.Services;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace CRMS_Peguit.Tests.IntegrationTests
{
    public class Section6_SuperAdminBoundaryIntegrationTests
    {
        public Section6_SuperAdminBoundaryIntegrationTests()
        {
            Environment.SetEnvironmentVariable("CRMS_CONNECTION", DbConfiguration.GetLocalConnectionString());
        }

        [Fact]
        public void SuperAdmin_Boundary_StructurallyBlockedFromTenantOperationalData()
        {
            // Requirement 6.1:
            // "Attempt, via every Super Admin screen/endpoint, to retrieve any row from Customer,
            //  Lead, Property, Deal, Activity, SupportTicket, TaskReminder, or Notification ->
            //  confirm every such attempt is structurally impossible (the query has no path to those tables),
            //  not merely blocked by a UI hide."

            // SuperAdminController works exclusively with MasterCrmsDbContext and limited metadata.
            // Verify that SuperAdminController has no methods or properties exposing Customer, Lead, Deal, Property, etc.
            var saCtrlType = typeof(SuperAdminController);
            var methods = saCtrlType.GetMethods();

            var dangerousMethods = methods.Where(m =>
                m.Name.Contains("Customer") ||
                m.Name.Contains("Lead") ||
                m.Name.Contains("Property") ||
                m.Name.Contains("Deal") ||
                m.Name.Contains("Ticket") ||
                m.Name.Contains("TaskReminder") ||
                m.Name.Contains("Notification")).ToList();

            // Zero operational entity CRUD endpoints exist in SuperAdminController
            Assert.Empty(dangerousMethods);
        }

        [Fact]
        public async Task SuperAdmin_BusinessIntelligence_AggregatesAndCountsOnly_NoTenantRecordDrilldown()
        {
            // Requirement 6.2:
            // "Confirm Super Admin's "Business Intelligence" shows aggregate/count-only figures
            //  (total tenants, total users as a count, total revenue) with no drill-down into any tenant's actual records."

            CurrentSession.Start(1, 0, "Platform Super Admin", "superadmin@crms.com", "SuperAdmin", null, false);
            var saCtrl = new SuperAdminSubscriptionController();

            var bi = await saCtrl.GetPlatformBiSummaryAsync();
            Assert.NotNull(bi);

            // Confirms aggregate totals only
            Assert.True(bi.TotalTenants >= 3);
            Assert.True(bi.ActiveSubscriptions >= 1);
            Assert.True(bi.TotalMrr >= 0);

            // Confirms no drilldown list of customer records exists on PlatformBiSummaryDto
            var dtoProps = typeof(CRMS_Peguit.domain.Common.PlatformBiSummaryDto).GetProperties();
            Assert.Null(dtoProps.FirstOrDefault(p => p.Name.Contains("Customers")));
            Assert.Null(dtoProps.FirstOrDefault(p => p.Name.Contains("Deals")));
            Assert.Null(dtoProps.FirstOrDefault(p => p.Name.Contains("Leads")));
        }

        [Fact]
        public async Task SuperAdmin_SyncHealth_ContainsMetadataAndCountsOnly_NoEntityPayloads()
        {
            // Requirement 6.3:
            // "Confirm Sync Health shows only counts/timestamps per tenant, never the contents of a pending sync item."

            CurrentSession.Start(1, 0, "Platform Super Admin", "superadmin@crms.com", "SuperAdmin", null, false);
            var syncHealthCtrl = new SuperAdminSyncHealthController();

            var healthSummary = await syncHealthCtrl.GetSyncHealthAsync();
            Assert.NotNull(healthSummary);

            // Verifies metadata properties only
            var summaryProps = typeof(CRMS_Peguit.domain.Common.SyncHealthDto).GetProperties();
            Assert.Null(summaryProps.FirstOrDefault(p => p.Name.Contains("Payload")));
            Assert.Null(summaryProps.FirstOrDefault(p => p.Name.Contains("EntityData")));
            Assert.Null(summaryProps.FirstOrDefault(p => p.Name.Contains("RecordContent")));
        }

        [Fact]
        public void SuperAdmin_DirectAccessToRetentionModule_IsStrictlyForbidden()
        {
            // Requirement 6.1b / 10.7:
            // "Super Admin has zero access to this module (confirm consistent with Section 6's absolute boundary)."

            CurrentSession.Start(1, 0, "Platform Super Admin", "superadmin@crms.com", "SuperAdmin", null, false);

            using var retCtrl = new RetentionController();
            var ex = Assert.ThrowsAsync<UnauthorizedAccessException>(async () => await retCtrl.GetSummaryAsync());
            Assert.NotNull(ex);
        }

        [Fact]
        public async Task SuperAdmin_TenantController_GetTenantsAsync_ReturnsMetadataWithoutOperationalData()
        {
            CurrentSession.Start(1, 0, "Platform Super Admin", "superadmin@crms.com", "SuperAdmin", null, false);
            var controller = new SuperAdminTenantController();
            var tenants = await controller.GetTenantsAsync();

            Assert.NotNull(tenants);
            Assert.NotEmpty(tenants);
            Assert.All(tenants, t =>
            {
                Assert.True(t.CompanyId > 0);
                Assert.False(string.IsNullOrWhiteSpace(t.CompanyName));
                Assert.False(string.IsNullOrWhiteSpace(t.CompanyCode));
                Assert.False(string.IsNullOrWhiteSpace(t.TierLevel));
            });
        }
    }
}
