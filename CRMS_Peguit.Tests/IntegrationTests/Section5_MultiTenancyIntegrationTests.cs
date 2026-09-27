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
    public class Section5_MultiTenancyIntegrationTests
    {
        public Section5_MultiTenancyIntegrationTests()
        {
            Environment.SetEnvironmentVariable("CRMS_CONNECTION", DbConfiguration.GetLocalConnectionString());
        }

        #region 5.1 Three Test Tenants Isolation Verification

        [Fact]
        public void MultiTenancy_ThreeTenants_DataIsCompletelyIsolated()
        {
            // Requirement 5.1:
            // "Using three real test tenants (A, B, C): create/edit records in each independently;
            //  confirm no tenant's data ever appears in another tenant's queries, grids, exports, or reports."

            // Create unique markers per tenant
            string markerA = $"TenantA_{Guid.NewGuid():N}";
            string markerB = $"TenantB_{Guid.NewGuid():N}";
            string markerC = $"TenantC_{Guid.NewGuid():N}";

            // Insert a Customer in Tenant 1 (Tenant A)
            using (var db1 = LocalDb.CreateContext(1))
            {
                var custA = new Customer
                {
                    FirstName = "TestA",
                    LastName = markerA,
                    Email = $"{markerA}@test.com",
                    Type = "Individual",
                    Status = "Active",
                    CreatedByUserId = 1,
                    CreatedAt = DateTime.UtcNow
                };
                db1.Customers.Add(custA);
                db1.SaveChanges();
            }

            // Insert a Customer in Tenant 2 (Tenant B)
            using (var db2 = LocalDb.CreateContext(2))
            {
                var custB = new Customer
                {
                    FirstName = "TestB",
                    LastName = markerB,
                    Email = $"{markerB}@test.com",
                    Type = "Individual",
                    Status = "Active",
                    CreatedByUserId = db2.Users.First().UserId,
                    CreatedAt = DateTime.UtcNow
                };
                db2.Customers.Add(custB);
                db2.SaveChanges();
            }

            // Insert a Customer in Tenant 3 (Tenant C)
            using (var db3 = LocalDb.CreateContext(3))
            {
                var custC = new Customer
                {
                    FirstName = "TestC",
                    LastName = markerC,
                    Email = $"{markerC}@test.com",
                    Type = "Individual",
                    Status = "Active",
                    CreatedByUserId = db3.Users.First().UserId,
                    CreatedAt = DateTime.UtcNow
                };
                db3.Customers.Add(custC);
                db3.SaveChanges();
            }

            // Verify Tenant 1 queries: can see markerA, but CANNOT see markerB or markerC
            using (var verifyDb1 = LocalDb.CreateContext(1))
            {
                Assert.True(verifyDb1.Customers.Any(c => c.LastName == markerA));
                Assert.False(verifyDb1.Customers.Any(c => c.LastName == markerB));
                Assert.False(verifyDb1.Customers.Any(c => c.LastName == markerC));
            }

            // Verify Tenant 2 queries: can see markerB, but CANNOT see markerA or markerC
            using (var verifyDb2 = LocalDb.CreateContext(2))
            {
                Assert.False(verifyDb2.Customers.Any(c => c.LastName == markerA));
                Assert.True(verifyDb2.Customers.Any(c => c.LastName == markerB));
                Assert.False(verifyDb2.Customers.Any(c => c.LastName == markerC));
            }

            // Verify Tenant 3 queries: can see markerC, but CANNOT see markerA or markerB
            using (var verifyDb3 = LocalDb.CreateContext(3))
            {
                Assert.False(verifyDb3.Customers.Any(c => c.LastName == markerA));
                Assert.False(verifyDb3.Customers.Any(c => c.LastName == markerB));
                Assert.True(verifyDb3.Customers.Any(c => c.LastName == markerC));
            }

            // Clean up temporary records
            using (var cleanDb1 = LocalDb.CreateContext(1))
            {
                var item = cleanDb1.Customers.FirstOrDefault(c => c.LastName == markerA);
                if (item != null) cleanDb1.Customers.Remove(item);
                cleanDb1.SaveChanges();
            }
            using (var cleanDb2 = LocalDb.CreateContext(2))
            {
                var item = cleanDb2.Customers.FirstOrDefault(c => c.LastName == markerB);
                if (item != null) cleanDb2.Customers.Remove(item);
                cleanDb2.SaveChanges();
            }
            using (var cleanDb3 = LocalDb.CreateContext(3))
            {
                var item = cleanDb3.Customers.FirstOrDefault(c => c.LastName == markerC);
                if (item != null) cleanDb3.Customers.Remove(item);
                cleanDb3.SaveChanges();
            }
        }

        #endregion

        #region 5.3 MasterDb Isolation

        [Fact]
        public void MultiTenancy_MasterDbTables_ExistOnlyInMasterContext()
        {
            // Requirement 5.3:
            // "Confirm MasterDb tables (Company registry, Subscription, PaymentRecord, SystemSetting, BackupLog,
            //  PlatformAuditLog) are reachable ONLY from Super Admin's panel/endpoints, never from any tenant-facing screen or API route."

            using var masterDb = LocalDb.CreateMasterContext();
            Assert.True(masterDb.Companies.Any());
            Assert.True(masterDb.Subscriptions.Any());

            // RealEstateDbContext does NOT contain Companies, Subscriptions, or PaymentRecords DbSets
            var tenantContextProps = typeof(RealEstateDbContext).GetProperties();
            Assert.Null(tenantContextProps.FirstOrDefault(p => p.Name == "Companies"));
            Assert.Null(tenantContextProps.FirstOrDefault(p => p.Name == "Subscriptions"));
            Assert.Null(tenantContextProps.FirstOrDefault(p => p.Name == "PaymentRecords"));
            Assert.Null(tenantContextProps.FirstOrDefault(p => p.Name == "SuperAdmins"));
        }

        #endregion
    }
}
