using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using CRMS_Peguit.winforms.Auth;
using CRMS_Peguit.winforms.Models.Services;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using Colors = QuestPDF.Helpers.Colors;

namespace CRMS_Peguit.winforms.Services
{
    /// <summary>
    /// Universal PDF export engine using QuestPDF.
    /// Provides consistent, high-fidelity corporate documents across all CRM modules.
    /// </summary>
    public static class PdfExportHelper
    {
        static PdfExportHelper()
        {
            QuestPDF.Settings.License = LicenseType.Community;
            QuestPDF.Settings.UseSystemFonts = true;
            QuestPDF.Settings.ThrowOnMissingFontFamilies = false;
        }

        /// <summary>
        /// Safely exports a tabular dataset to a branded PDF document.
        /// </summary>
        public static bool TryExportTable(
            string reportTitle,
            string[] headers,
            List<string[]> rows,
            string filePath,
            out string? errorMessage,
            string? activeFilter = null,
            List<(string Title, string Value, string ColorHex)>? kpis = null,
            bool landscape = true)
        {
            try
            {
                ExportTable(reportTitle, headers, rows, filePath, activeFilter, kpis, landscape);
                errorMessage = null;
                return true;
            }
            catch (IOException ex)
            {
                errorMessage = $"The file cannot be accessed because it is open in another application (such as a PDF viewer).\n\nDetails: {ex.Message}";
                return false;
            }
            catch (UnauthorizedAccessException ex)
            {
                errorMessage = $"Access to the destination path is denied. Please choose a different location.\n\nDetails: {ex.Message}";
                return false;
            }
            catch (Exception ex)
            {
                errorMessage = $"Failed to generate PDF: {ex.Message}";
                return false;
            }
        }

        /// <summary>
        /// Generates a branded corporate PDF document containing an overview header, KPI cards, and data grid.
        /// </summary>
        public static void ExportTable(
            string reportTitle,
            string[] headers,
            List<string[]> rows,
            string filePath,
            string? activeFilter = null,
            List<(string Title, string Value, string ColorHex)>? kpis = null,
            bool landscape = true)
        {
            if (File.Exists(filePath))
            {
                try { File.SetAttributes(filePath, FileAttributes.Normal); } catch { }
            }

            Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Margin(26);
                    page.Size(landscape ? PageSizes.A4.Landscape() : PageSizes.A4);
                    page.PageColor(Colors.White);
                    page.DefaultTextStyle(x => x.FontSize(8.5f).FontFamily("Segoe UI"));

                    // Header
                    page.Header().Column(col =>
                    {
                        col.Item().Row(r =>
                        {
                            // Tenant Logo
                            r.ConstantItem(120).AlignLeft().AlignMiddle().Element(e =>
                            {
                                try
                                {
                                    var logo = BrandingService.GetLogo();
                                    if (logo != null)
                                    {
                                        using var ms = new MemoryStream();
                                        logo.Save(ms, System.Drawing.Imaging.ImageFormat.Png);
                                        e.Height(36).Image(ms.ToArray());
                                    }
                                    else
                                    {
                                        var stream = Assembly.GetExecutingAssembly()
                                            .GetManifestResourceStream("CRMS_Peguit.winforms.Assets.logo.png");
                                        if (stream != null) e.Height(36).Image(stream);
                                        else e.Text("");
                                    }
                                }
                                catch
                                {
                                    e.Text("");
                                }
                            });

                            // Title & Subtitle
                            r.RelativeItem().Column(c =>
                            {
                                c.Item().Text(BrandingService.GetDisplayName().ToUpperInvariant())
                                    .Bold().FontSize(15).FontColor(Colors.Blue.Darken2);
                                c.Item().Text(reportTitle)
                                    .Bold().FontSize(12).FontColor(Colors.Grey.Darken3);
                                if (!string.IsNullOrWhiteSpace(activeFilter))
                                {
                                    c.Item().Text($"Filter: {activeFilter}").FontSize(8.5f).FontColor(Colors.Grey.Medium);
                                }
                            });

                            // Metadata Block
                            r.RelativeItem().AlignRight().Column(c =>
                            {
                                c.Item().Text($"Generated: {DateTime.Now:yyyy-MM-dd HH:mm}").FontSize(8).FontColor(Colors.Grey.Darken1);
                                c.Item().Text($"By: {CurrentSession.CurrentUser?.FullName ?? CurrentSession.CurrentUser?.Email ?? "System"}").FontSize(8).FontColor(Colors.Grey.Darken1);
                                if (CurrentSession.ActiveBranchName != null)
                                {
                                    c.Item().Text($"Branch: {CurrentSession.ActiveBranchName}").FontSize(8).FontColor(Colors.Grey.Darken1);
                                }
                                c.Item().Text($"Total Records: {rows.Count:N0}").FontSize(8).FontColor(Colors.Grey.Darken1);
                            });
                        });

                        col.Item().PaddingVertical(4).LineHorizontal(1).LineColor(Colors.Blue.Darken2);
                    });

                    // Body
                    page.Content().PaddingVertical(8).Column(content =>
                    {
                        // KPI Cards
                        if (kpis != null && kpis.Count > 0)
                        {
                            content.Item().PaddingBottom(10).Row(r =>
                            {
                                foreach (var kpi in kpis)
                                {
                                    r.RelativeItem().PaddingRight(6).Background(Colors.Grey.Lighten4)
                                        .Border(0.5f).BorderColor(Colors.Grey.Lighten2)
                                        .Padding(6).Column(c =>
                                        {
                                            c.Item().Text(kpi.Title).FontSize(7.5f).FontColor(Colors.Grey.Darken1).SemiBold();
                                            c.Item().Text(kpi.Value).FontSize(11).Bold().FontColor(kpi.ColorHex);
                                        });
                                }
                            });
                        }

                        // Data Table
                        content.Item().Table(table =>
                        {
                            table.ColumnsDefinition(cols =>
                            {
                                for (int i = 0; i < headers.Length; i++)
                                {
                                    string h = headers[i].ToLowerInvariant();
                                    if (h.Contains("address") || h.Contains("name") || h.Contains("customer") || h.Contains("details") || h.Contains("title"))
                                        cols.RelativeColumn(2.5f);
                                    else if (h.Contains("email") || h.Contains("phone") || h.Contains("agent") || h.Contains("property"))
                                        cols.RelativeColumn(2f);
                                    else if (h.Contains("id") || h.Contains("ref") || h.Contains("code") || h.Contains("type"))
                                        cols.RelativeColumn(1f);
                                    else
                                        cols.RelativeColumn(1.3f);
                                }
                            });

                            // Table Header
                            table.Header(h =>
                            {
                                foreach (var header in headers)
                                {
                                    h.Cell().Background(Colors.Grey.Lighten3).Padding(4)
                                        .Text(header).Bold().FontSize(8).FontColor(Colors.Grey.Darken3);
                                }
                            });

                            // Table Rows
                            for (int rIdx = 0; rIdx < rows.Count; rIdx++)
                            {
                                var rowData = rows[rIdx];
                                string rowBg = (rIdx % 2 == 1) ? "#F8FAFC" : "#FFFFFF";

                                for (int cIdx = 0; cIdx < headers.Length; cIdx++)
                                {
                                    string val = cIdx < rowData.Length ? (rowData[cIdx] ?? "") : "";
                                    var cell = table.Cell().Background(rowBg).BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten2).Padding(4);

                                    string h = headers[cIdx].ToLowerInvariant();
                                    if (h.Contains("status") || h.Contains("stage") || h.Contains("priority"))
                                    {
                                        string statusColor = GetStatusColorHex(val);
                                        cell.AlignLeft().Text(val).FontSize(8).FontColor(statusColor).Bold();
                                    }
                                    else if (val.StartsWith("₱") || val.EndsWith("%") || (decimal.TryParse(val, out _) && !val.Contains("-") && val.Length < 12))
                                    {
                                        cell.AlignRight().Text(val).FontSize(8);
                                    }
                                    else
                                    {
                                        cell.AlignLeft().Text(val).FontSize(8);
                                    }
                                }
                            }
                        });
                    });

                    // Footer
                    page.Footer().AlignRight().Text(text =>
                    {
                        text.Span("Page ");
                        text.CurrentPageNumber();
                        text.Span(" of ");
                        text.TotalPages();
                    });
                });
            }).GeneratePdf(filePath);
        }

        /// <summary>
        /// Exports Analytics metrics overview and drill-down rows to a formatted PDF document.
        /// </summary>
        public static bool TryExportAnalytics(
            string filePath,
            string dateRangeText,
            (string Label, string Value)[] kpis,
            List<(string RecordType, string Reference, string Title, string Status, string AssignedTo, decimal Value, DateTime Date, string Details)> drillDowns,
            out string? errorMessage)
        {
            try
            {
                if (File.Exists(filePath))
                {
                    try { File.SetAttributes(filePath, FileAttributes.Normal); } catch { }
                }

                Document.Create(container =>
                {
                    container.Page(page =>
                    {
                        page.Margin(26);
                        page.Size(PageSizes.A4.Landscape());
                        page.PageColor(Colors.White);
                        page.DefaultTextStyle(x => x.FontSize(8.5f).FontFamily("Segoe UI"));

                        // Header
                        page.Header().Column(col =>
                        {
                            col.Item().Row(r =>
                            {
                                r.ConstantItem(120).AlignLeft().AlignMiddle().Element(e =>
                                {
                                    try
                                    {
                                        var logo = BrandingService.GetLogo();
                                        if (logo != null)
                                        {
                                            using var ms = new MemoryStream();
                                            logo.Save(ms, System.Drawing.Imaging.ImageFormat.Png);
                                            e.Height(36).Image(ms.ToArray());
                                        }
                                        else
                                        {
                                            var stream = Assembly.GetExecutingAssembly()
                                                .GetManifestResourceStream("CRMS_Peguit.winforms.Assets.logo.png");
                                            if (stream != null) e.Height(36).Image(stream);
                                            else e.Text("");
                                        }
                                    }
                                    catch
                                    {
                                        e.Text("");
                                    }
                                });

                                r.RelativeItem().Column(c =>
                                {
                                    c.Item().Text(BrandingService.GetDisplayName().ToUpperInvariant())
                                        .Bold().FontSize(15).FontColor(Colors.Blue.Darken2);
                                    c.Item().Text("Analytics & Business Intelligence Summary")
                                        .Bold().FontSize(12).FontColor(Colors.Grey.Darken3);
                                    c.Item().Text($"Date Range: {dateRangeText}").FontSize(8.5f).FontColor(Colors.Grey.Medium);
                                });

                                r.RelativeItem().AlignRight().Column(c =>
                                {
                                    c.Item().Text($"Generated: {DateTime.Now:yyyy-MM-dd HH:mm}").FontSize(8).FontColor(Colors.Grey.Darken1);
                                    c.Item().Text($"By: {CurrentSession.CurrentUser?.FullName ?? CurrentSession.CurrentUser?.Email ?? "System"}").FontSize(8).FontColor(Colors.Grey.Darken1);
                                    if (CurrentSession.ActiveBranchName != null)
                                    {
                                        c.Item().Text($"Branch: {CurrentSession.ActiveBranchName}").FontSize(8).FontColor(Colors.Grey.Darken1);
                                    }
                                });
                            });

                            col.Item().PaddingVertical(4).LineHorizontal(1).LineColor(Colors.Blue.Darken2);
                        });

                        // Content
                        page.Content().PaddingVertical(8).Column(content =>
                        {
                            // 1. KPI Summary Cards Grid
                            content.Item().Text("Executive Performance Metrics").Bold().FontSize(10).FontColor(Colors.Grey.Darken3);
                            content.Item().PaddingVertical(6).Table(kpiTable =>
                            {
                                kpiTable.ColumnsDefinition(cd =>
                                {
                                    cd.RelativeColumn(1);
                                    cd.RelativeColumn(1);
                                    cd.RelativeColumn(1);
                                    cd.RelativeColumn(1);
                                });

                                for (int i = 0; i < kpis.Length; i++)
                                {
                                    var item = kpis[i];
                                    kpiTable.Cell().Padding(3).Background(Colors.Grey.Lighten4)
                                        .Border(0.5f).BorderColor(Colors.Grey.Lighten2)
                                        .Padding(6).Column(c =>
                                        {
                                            c.Item().Text(item.Label).FontSize(7.5f).FontColor(Colors.Grey.Darken1).SemiBold();
                                            c.Item().Text(item.Value).FontSize(11).Bold().FontColor("#0F172A");
                                        });
                                }
                            });

                            // 2. Drill-down details table
                            if (drillDowns.Count > 0)
                            {
                                content.Item().PaddingTop(12).Text($"Underlying Transaction Records ({drillDowns.Count:N0})")
                                    .Bold().FontSize(10).FontColor(Colors.Grey.Darken3);

                                content.Item().PaddingTop(4).Table(table =>
                                {
                                    table.ColumnsDefinition(cols =>
                                    {
                                        cols.RelativeColumn(1f); // Type
                                        cols.RelativeColumn(1.2f); // Ref
                                        cols.RelativeColumn(2.5f); // Title
                                        cols.RelativeColumn(1.2f); // Status
                                        cols.RelativeColumn(1.8f); // Assigned To
                                        cols.RelativeColumn(1.5f); // Value
                                        cols.RelativeColumn(1.2f); // Date
                                        cols.RelativeColumn(2f); // Details
                                    });

                                    table.Header(h =>
                                    {
                                        string[] headers = { "Type", "Ref", "Title / Entity", "Status", "Assigned Agent", "Value", "Date", "Details" };
                                        foreach (var hdr in headers)
                                        {
                                            h.Cell().Background(Colors.Grey.Lighten3).Padding(4)
                                                .Text(hdr).Bold().FontSize(8).FontColor(Colors.Grey.Darken3);
                                        }
                                    });

                                    for (int i = 0; i < drillDowns.Count; i++)
                                    {
                                        var row = drillDowns[i];
                                        string rowBg = (i % 2 == 1) ? "#F8FAFC" : "#FFFFFF";

                                        table.Cell().Background(rowBg).BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten2).Padding(4).Text(row.RecordType).FontSize(8);
                                        table.Cell().Background(rowBg).BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten2).Padding(4).Text(row.Reference).FontSize(8).Bold();
                                        table.Cell().Background(rowBg).BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten2).Padding(4).Text(row.Title).FontSize(8);

                                        string statusColor = GetStatusColorHex(row.Status);
                                        table.Cell().Background(rowBg).BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten2).Padding(4)
                                            .Text(row.Status).FontSize(8).FontColor(statusColor).Bold();

                                        table.Cell().Background(rowBg).BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten2).Padding(4).Text(row.AssignedTo).FontSize(8);
                                        table.Cell().Background(rowBg).BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten2).Padding(4).AlignRight()
                                            .Text($"₱ {row.Value:N2}").FontSize(8);
                                        table.Cell().Background(rowBg).BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten2).Padding(4)
                                            .Text(row.Date.ToString("yyyy-MM-dd")).FontSize(8);
                                        table.Cell().Background(rowBg).BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten2).Padding(4).Text(row.Details).FontSize(8);
                                    }
                                });
                            }
                        });

                        // Footer
                        page.Footer().AlignRight().Text(text =>
                        {
                            text.Span("Page ");
                            text.CurrentPageNumber();
                            text.Span(" of ");
                            text.TotalPages();
                        });
                    });
                }).GeneratePdf(filePath);

                errorMessage = null;
                return true;
            }
            catch (Exception ex)
            {
                errorMessage = ex.Message;
                return false;
            }
        }

        private static string GetStatusColorHex(string? status)
        {
            if (string.IsNullOrWhiteSpace(status)) return "#475569";
            string s = status.Trim().ToLowerInvariant();
            return s switch
            {
                "active" or "won" or "closed" or "resolved" or "converted" or "available" or "yes" or "approved" => "#059669",
                "pending" or "in progress" or "contacted" or "offer" or "contract" or "qualified" or "under review" => "#D97706",
                "lost" or "inactive" or "overdue" or "breached" or "cancelled" or "urgent" or "critical" or "high" or "no" or "rejected" => "#DC2626",
                _ => "#475569"
            };
        }
    }
}
