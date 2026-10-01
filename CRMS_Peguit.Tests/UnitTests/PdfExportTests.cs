using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using CRMS_Peguit.domain.entities;
using CRMS_Peguit.winforms.Auth;
using CRMS_Peguit.winforms.Services;
using Xunit;

namespace CRMS_Peguit.Tests.UnitTests
{
    public class PdfExportTests
    {
        public PdfExportTests()
        {
            CurrentSession.Start(
                userId: 1,
                tenantId: 1,
                fullName: "Test Admin",
                email: "admin@test.com",
                roleName: "Admin",
                jwtToken: null,
                isOffline: false,
                tier: TenantTier.TenantA,
                tenantName: "NEXA Real Estate");
        }

        [Fact]
        public void PdfExportHelper_TryExportTable_CreatesValidPdfDocument()
        {
            string tempPdf = Path.Combine(Path.GetTempPath(), $"test_table_export_{Guid.NewGuid():N}.pdf");
            try
            {
                string[] headers = new[] { "ID", "Name", "Status", "Amount" };
                var rows = new List<string[]>
                {
                    new[] { "1", "Alice Johnson", "Active", "₱ 1,500,000.00" },
                    new[] { "2", "Bob Smith", "Pending", "₱ 750,000.00" },
                    new[] { "3", "Charlie Brown", "Closed", "₱ 2,200,000.00" }
                };

                var kpis = new List<(string Title, string Value, string ColorHex)>
                {
                    ("Total Records", "3", "#25679C"),
                    ("Total Volume", "₱ 4,450,000.00", "#059669")
                };

                bool ok = PdfExportHelper.TryExportTable(
                    reportTitle: "Unit Test Table Export",
                    headers: headers,
                    rows: rows,
                    filePath: tempPdf,
                    errorMessage: out string? err,
                    activeFilter: "All",
                    kpis: kpis);

                Assert.True(ok, err);
                Assert.True(File.Exists(tempPdf));
                var bytes = File.ReadAllBytes(tempPdf);
                Assert.True(bytes.Length > 100);

                // Check PDF magic header %PDF-
                string header = Encoding.ASCII.GetString(bytes, 0, Math.Min(5, bytes.Length));
                Assert.Equal("%PDF-", header);
            }
            finally
            {
                if (File.Exists(tempPdf))
                {
                    try { File.Delete(tempPdf); } catch { }
                }
            }
        }

        [Fact]
        public void PdfExportHelper_TryExportAnalytics_CreatesValidPdfDocument()
        {
            string tempPdf = Path.Combine(Path.GetTempPath(), $"test_analytics_export_{Guid.NewGuid():N}.pdf");
            try
            {
                var kpis = new (string Label, string Value)[]
                {
                    ("Deals Closed", "24"),
                    ("Sales Volume", "₱ 120,000,000.00"),
                    ("Active Leads", "45"),
                    ("Win Rate", "68.5%")
                };

                var drillDowns = new List<(string RecordType, string Reference, string Title, string Status, string AssignedTo, decimal Value, DateTime Date, string Details)>
                {
                    ("Deal", "DEAL-0001", "Azure Residences Unit 401", "Closed", "Agent Maria", 5000000m, DateTime.UtcNow, "Full payment verified"),
                    ("Deal", "DEAL-0002", "Grand Villa Lot 12", "Under Contract", "Agent John", 8500000m, DateTime.UtcNow, "Escrow pending")
                };

                bool ok = PdfExportHelper.TryExportAnalytics(
                    tempPdf,
                    dateRangeText: "This Year",
                    kpis: kpis,
                    drillDowns: drillDowns,
                    out string? err);

                Assert.True(ok, err);
                Assert.True(File.Exists(tempPdf));
                var bytes = File.ReadAllBytes(tempPdf);
                Assert.True(bytes.Length > 100);

                string header = Encoding.ASCII.GetString(bytes, 0, Math.Min(5, bytes.Length));
                Assert.Equal("%PDF-", header);
            }
            finally
            {
                if (File.Exists(tempPdf))
                {
                    try { File.Delete(tempPdf); } catch { }
                }
            }
        }
    }
}
