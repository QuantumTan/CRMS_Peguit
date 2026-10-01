using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using CRMS_Peguit.domain.Common;
using CRMS_Peguit.domain.entities;
using CRMS_Peguit.infrastructure.data;
using CRMS_Peguit.winforms.Auth;
using CRMS_Peguit.winforms.Services;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace CRMS_Peguit.Tests.IntegrationTests
{
    public class Section8_TenantBrandingIntegrationTests : IDisposable
    {
        private readonly string _testDbPath;
        private readonly LocalAuthCache _cache;

        public Section8_TenantBrandingIntegrationTests()
        {
            _testDbPath = Path.Combine(Path.GetTempPath(), $"crms_branding_test_{Guid.NewGuid():N}.db");
            _cache = new LocalAuthCache(_testDbPath);
        }

        public void Dispose()
        {
            try
            {
                if (File.Exists(_testDbPath))
                {
                    File.Delete(_testDbPath);
                }
            }
            catch { }
        }

        #region 8.5 Offline SQLite Cache & Cross-Tenant Isolation

        [Fact]
        public void LocalAuthCache_SavesAndRetrievesBrandingOffline()
        {
            int tenantId = 42;
            string displayName = "Vanguard Real Estate Group";
            byte[] logo = new byte[] { 0x89, 0x50, 0x4E, 0x47, 0x01, 0x02, 0x03, 0x04 };
            int version = 3;
            string accent = "#0284C7";
            string email = "info@vanguardrealty.com";
            string phone = "+63 917 123 4567";
            string addr = "Ayala Tower One, Makati City";
            bool hidePoweredBy = true;
            DateTime updated = DateTime.UtcNow;

            _cache.SaveTenantBranding(tenantId, displayName, logo, version, accent, email, phone, addr, hidePoweredBy, updated);

            var retrieved = _cache.GetTenantBranding(tenantId);

            Assert.NotNull(retrieved);
            Assert.Equal(tenantId, retrieved.TenantId);
            Assert.Equal(displayName, retrieved.DisplayName);
            Assert.NotNull(retrieved.LogoBytes);
            Assert.Equal(logo.Length, retrieved.LogoBytes!.Length);
            Assert.Equal(version, retrieved.LogoVersion);
            Assert.Equal(accent, retrieved.AccentColor);
            Assert.Equal(email, retrieved.ContactEmail);
            Assert.Equal(phone, retrieved.ContactPhone);
            Assert.Equal(addr, retrieved.Address);
            Assert.True(retrieved.HidePoweredBy);
        }

        [Fact]
        public void LocalAuthCache_EnforcesCrossTenantIsolation()
        {
            // Tenant A: Coastal Properties
            int tenantA = 101;
            string nameA = "Coastal Properties";
            byte[] logoA = new byte[] { 0x89, 0x50, 0x4E, 0x47, 0xAA };
            _cache.SaveTenantBranding(tenantA, nameA, logoA, 1, "#0284C7", "a@coastal.com", "111", "Manila", false, DateTime.UtcNow);

            // Tenant B: Highland Realty
            int tenantB = 102;
            string nameB = "Highland Realty";
            byte[] logoB = new byte[] { 0x89, 0x50, 0x4E, 0x47, 0xBB };
            _cache.SaveTenantBranding(tenantB, nameB, logoB, 2, "#7C3AED", "b@highland.com", "222", "Baguio", true, DateTime.UtcNow);

            // Verify Tenant A data
            var brandA = _cache.GetTenantBranding(tenantA);
            Assert.NotNull(brandA);
            Assert.Equal(nameA, brandA.DisplayName);
            Assert.Equal(logoA[4], brandA.LogoBytes![4]);
            Assert.Equal("#0284C7", brandA.AccentColor);
            Assert.False(brandA.HidePoweredBy);

            // Verify Tenant B data
            var brandB = _cache.GetTenantBranding(tenantB);
            Assert.NotNull(brandB);
            Assert.Equal(nameB, brandB.DisplayName);
            Assert.Equal(logoB[4], brandB.LogoBytes![4]);
            Assert.Equal("#7C3AED", brandB.AccentColor);
            Assert.True(brandB.HidePoweredBy);

            // Verify non-existent tenant returns null
            Assert.Null(_cache.GetTenantBranding(999));
        }

        [Fact]
        public void LocalAuthCache_LastAuthenticatedTenantId_PreservesPreLoginBrand()
        {
            int tenantA = 201;
            _cache.SaveTenantBranding(tenantA, "Metro Realty", null, 1, null, null, null, null, false, DateTime.UtcNow);

            int tenantB = 202;
            _cache.SaveTenantBranding(tenantB, "Zenith Estates", null, 1, null, null, null, null, true, DateTime.UtcNow);

            // Set Tenant A as last logged in
            _cache.SetLastAuthenticatedTenantId(tenantA);
            var lastBrand = _cache.GetLastAuthenticatedTenantBranding();
            Assert.NotNull(lastBrand);
            Assert.Equal("Metro Realty", lastBrand.DisplayName);

            // User switches to Tenant B
            _cache.SetLastAuthenticatedTenantId(tenantB);
            lastBrand = _cache.GetLastAuthenticatedTenantBranding();
            Assert.NotNull(lastBrand);
            Assert.Equal("Zenith Estates", lastBrand.DisplayName);
        }

        #endregion

        #region 8.6 MasterDb 1:1 Entity Mapping & Backfill

        [Fact]
        public async Task MasterDb_TenantBranding_OneToOneRelationship()
        {
            using var connection = new Microsoft.Data.Sqlite.SqliteConnection("Data Source=:memory:");
            connection.Open();

            var options = new DbContextOptionsBuilder<MasterCrmsDbContext>()
                .UseSqlite(connection)
                .Options;

            using var db = new MasterCrmsDbContext(options);
            db.Database.EnsureCreated();

            var company = new Company
            {
                CompanyId = 50,
                CompanyName = "Horizon Prime Realty",
                CompanyCode = "HORIZON",
                CreatedAt = DateTime.UtcNow
            };
            db.Companies.Add(company);
            await db.SaveChangesAsync();

            var branding = new TenantBranding
            {
                CompanyId = company.CompanyId,
                DisplayName = "Horizon Prime Estates",
                LogoVersion = 1,
                AccentColor = "#38BDF8",
                HidePoweredBy = false,
                UpdatedAt = DateTime.UtcNow
            };
            db.TenantBrandings.Add(branding);
            await db.SaveChangesAsync();

            // Reload and verify 1:1 navigation
            var loadedCompany = await db.Companies
                .Include(c => c.Branding)
                .FirstOrDefaultAsync(c => c.CompanyId == 50);

            Assert.NotNull(loadedCompany);
            Assert.NotNull(loadedCompany!.Branding);
            Assert.Equal("Horizon Prime Estates", loadedCompany.Branding!.DisplayName);
            Assert.Equal("#38BDF8", loadedCompany.Branding.AccentColor);
        }

        #endregion
    }
}
