using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using CRMS_Peguit.domain.entities;
using CRMS_Peguit.infrastructure.data;
using CRMS_Peguit.winforms.Auth;
using CRMS_Peguit.winforms.Models.Analytics;
using CRMS_Peguit.winforms.Models.Services;
using Microsoft.EntityFrameworkCore;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using ClosedXML.Excel;

namespace CRMS_Peguit.winforms.Controllers
{
    public class ReportsController : IDisposable
    {
        private readonly RealEstateDbContext _db;

        public ReportsController()
        {
            if (RbacService.IsSuperAdmin)
                throw new UnauthorizedAccessException("Super Administrators are restricted to platform infrastructure and cannot access tenant operational reports.");

            _db = LocalDb.CreateContext(CurrentSession.TenantId);
            QuestPDF.Settings.License = LicenseType.Community;
            QuestPDF.Settings.UseSystemFonts = true;
            QuestPDF.Settings.ThrowOnMissingFontFamilies = false;
        }

        public List<SalesReportRow> GetSalesReport(DateRangeFilter range, int? agentId = null, string? propertyType = null)
        {
            if (!RbacService.HasFullOversight)
                throw new UnauthorizedAccessException("Reports are restricted to Admin and Manager roles.");

            try
            {
                var query = _db.Deals
                    .Include(d => d.Customer)
                    .Include(d => d.Property)
                    .Include(d => d.Agent)
                    .Where(d => d.CreatedAt >= range.Start && d.CreatedAt <= range.End);

                if (CurrentSession.CanAccessBranching && CurrentSession.ActiveBranchId.HasValue)
                    query = query.Where(d => d.BranchId == CurrentSession.ActiveBranchId.Value);

                if (agentId.HasValue)
                    query = query.Where(d => d.AgentId == agentId.Value);

                if (!string.IsNullOrEmpty(propertyType))
                {
                    string ptLower = propertyType.Trim().ToLowerInvariant();
                    string mapped = ptLower switch
                    {
                        "residential" => "house",
                        "condominium" => "condo",
                        "land" => "lot",
                        _ => ptLower
                    };
                    query = query.Where(d => d.Property != null && (d.Property.PropertyType == mapped || d.Property.PropertyType == ptLower));
                }

                var deals = query.OrderByDescending(d => d.CreatedAt).ToList();

                var report = new List<SalesReportRow>();
                foreach (var d in deals)
                {
                    decimal commRate = d.CommissionRate > 1m ? d.CommissionRate / 100m : d.CommissionRate;
                    string pType = d.Property?.PropertyType ?? "General";
                    if (!string.IsNullOrEmpty(pType))
                        pType = char.ToUpper(pType[0]) + (pType.Length > 1 ? pType[1..] : "");

                    report.Add(new SalesReportRow
                    {
                        DealRef = $"DEAL-{d.DealId:D4}",
                        CustomerName = d.Customer?.FullName ?? "Unknown",
                        PropertyAddress = d.Property?.Address ?? "Unknown",
                        PropertyType = pType,
                        AgentName = d.Agent?.FullName ?? "Unassigned",
                        DealValue = d.Value,
                        Commission = d.Value * commRate,
                        Stage = d.Stage,
                        ExpectedOrClosedDate = d.ContractSignedDate?.ToString("MMM dd, yyyy") ?? d.ExpectedCloseDate?.ToString("MMM dd, yyyy") ?? d.CreatedAt.ToString("MMM dd, yyyy")
                    });
                }
                return report;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error in GetSalesReport: {ex.Message}");
                return new List<SalesReportRow>();
            }
        }

        public List<LeadProgressRow> GetLeadProgressReport(DateRangeFilter range, int? agentId = null, string? source = null)
        {
            if (!RbacService.HasFullOversight)
                throw new UnauthorizedAccessException("Reports are restricted to Admin and Manager roles.");

            try
            {
                var query = _db.Leads
                    .Include(l => l.AssignedAgent)
                    .Where(l => l.CreatedAt >= range.Start && l.CreatedAt <= range.End);

                if (CurrentSession.CanAccessBranching && CurrentSession.ActiveBranchId.HasValue)
                    query = query.Where(l => l.BranchId == CurrentSession.ActiveBranchId.Value);

                if (agentId.HasValue)
                    query = query.Where(l => l.AssignedAgentId == agentId.Value);

                if (!string.IsNullOrEmpty(source))
                    query = query.Where(l => l.Source == source);

                var leads = query.OrderByDescending(l => l.CreatedAt).ToList();

                var report = new List<LeadProgressRow>();
                foreach (var l in leads)
                {
                    report.Add(new LeadProgressRow
                    {
                        LeadName = l.FullName,
                        Source = l.Source ?? "Unknown",
                        Priority = string.IsNullOrWhiteSpace(l.Priority) ? "Normal" : l.Priority,
                        EstimatedBudget = l.ExpectedValue ?? 0m,
                        AgentName = l.AssignedAgent?.FullName ?? "Unassigned",
                        Stage = l.Stage,
                        DaysInPipeline = Math.Max(0, (int)(DateTime.UtcNow - l.CreatedAt).TotalDays),
                        ConvertedToCustomer = l.ConvertedCustomerId.HasValue ? "Yes" : "No",
                        CreatedDate = l.CreatedAt.ToString("MMM dd, yyyy")
                    });
                }
                return report;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error in GetLeadProgressReport: {ex.Message}");
                return new List<LeadProgressRow>();
            }
        }

        public List<CommissionReportRow> GetCommissionReport(DateRangeFilter range, int? agentId = null)
        {
            if (!RbacService.HasFullOversight)
                throw new UnauthorizedAccessException("Reports are restricted to Admin and Manager roles.");

            try
            {
                var query = _db.Deals
                    .Include(d => d.Agent)
                    .Include(d => d.Property)
                    .Include(d => d.Customer)
                    .Where(d => (d.Stage.ToLower() == "closed" || d.Stage.ToLower() == "closed-won" || d.Stage.ToLower() == "won") &&
                                (d.ContractSignedDate ?? d.CreatedAt) >= range.Start && (d.ContractSignedDate ?? d.CreatedAt) <= range.End);

                if (CurrentSession.CanAccessBranching && CurrentSession.ActiveBranchId.HasValue)
                    query = query.Where(d => d.BranchId == CurrentSession.ActiveBranchId.Value);

                if (agentId.HasValue)
                    query = query.Where(d => d.AgentId == agentId.Value);

                var deals = query.ToList();

                var report = new List<CommissionReportRow>();
                foreach (var d in deals)
                {
                    decimal grossRate = d.CommissionRate > 1m ? d.CommissionRate / 100m : d.CommissionRate;
                    decimal grossComm = d.Value * grossRate;
                    decimal agentSplit = 70m; // Real estate industry standard agent payout split
                    decimal agentPayout = grossComm * (agentSplit / 100m);
                    decimal brokerageRetained = RbacService.CanViewBrokerageMargins ? (grossComm - agentPayout) : 0m;

                    report.Add(new CommissionReportRow
                    {
                        DealRef = $"DEAL-{d.DealId:D4}",
                        PropertyAddress = d.Property?.Address ?? "Unknown",
                        CustomerName = d.Customer?.FullName ?? "Unknown",
                        AgentName = d.Agent?.FullName ?? "Unassigned",
                        DealValue = d.Value,
                        GrossCommission = grossComm,
                        AgentSplitPercent = agentSplit,
                        AgentPayoutAmount = agentPayout,
                        BrokerageRetainedAmount = brokerageRetained,
                        CloseDate = d.ContractSignedDate?.ToString("MMM dd, yyyy") ?? d.CreatedAt.ToString("MMM dd, yyyy"),
                        SettlementStatus = "Settled"
                    });
                }
                
                return report.OrderByDescending(r => r.CloseDate).ToList();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error in GetCommissionReport: {ex.Message}");
                return new List<CommissionReportRow>();
            }
        }

        public List<PropertyInventoryReportRow> GetPropertyInventoryReport(DateRangeFilter range, string? propertyType = null, string? status = null)
        {
            if (!RbacService.HasFullOversight)
                throw new UnauthorizedAccessException("Reports are restricted to Admin and Manager roles.");

            try
            {
                var query = _db.Properties
                    .Include(p => p.ListedByAgent)
                    .Include(p => p.OwnerCustomer)
                    .Where(p => p.CreatedAt >= range.Start && p.CreatedAt <= range.End);

                if (CurrentSession.CanAccessBranching && CurrentSession.ActiveBranchId.HasValue)
                    query = query.Where(p => p.BranchId == CurrentSession.ActiveBranchId.Value);

                if (!string.IsNullOrEmpty(propertyType))
                {
                    string ptLower = propertyType.Trim().ToLowerInvariant();
                    string mapped = ptLower switch
                    {
                        "residential" => "house",
                        "condominium" => "condo",
                        "land" => "lot",
                        _ => ptLower
                    };
                    query = query.Where(p => p.PropertyType == mapped || p.PropertyType == ptLower);
                }

                if (!string.IsNullOrEmpty(status))
                    query = query.Where(p => p.Status.ToLower() == status.ToLower());

                var properties = query.OrderByDescending(p => p.CreatedAt).ToList();
                var propIds = properties.Select(p => p.PropertyId).ToList();
                var dealCounts = _db.Deals
                    .Where(d => propIds.Contains(d.PropertyId))
                    .GroupBy(d => d.PropertyId)
                    .ToDictionary(g => g.Key, g => g.Count());

                var report = new List<PropertyInventoryReportRow>();
                foreach (var p in properties)
                {
                    int deals = dealCounts.TryGetValue(p.PropertyId, out int cnt) ? cnt : 0;
                    int dom = Math.Max(0, (int)(DateTime.UtcNow - p.CreatedAt).TotalDays);

                    string pType = p.PropertyType ?? "General";
                    if (!string.IsNullOrEmpty(pType))
                        pType = char.ToUpper(pType[0]) + (pType.Length > 1 ? pType[1..] : "");

                    string stat = p.Status ?? "Available";
                    if (!string.IsNullOrEmpty(stat))
                        stat = char.ToUpper(stat[0]) + (stat.Length > 1 ? stat[1..] : "");

                    report.Add(new PropertyInventoryReportRow
                    {
                        PropertyRef = $"PROP-{p.PropertyId:D4}",
                        Address = p.Address,
                        PropertyType = pType,
                        ListingPrice = p.Price,
                        Status = stat,
                        ListingAgent = p.ListedByAgent?.FullName ?? "Unassigned",
                        DaysOnMarket = dom,
                        AssociatedDeals = deals,
                        ListedDate = p.CreatedAt.ToString("MMM dd, yyyy")
                    });
                }
                return report;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error in GetPropertyInventoryReport: {ex.Message}");
                return new List<PropertyInventoryReportRow>();
            }
        }

        public List<TicketResolutionRow> GetTicketResolutionReport(DateRangeFilter range, string? priority = null, string? status = null)
        {
            if (!RbacService.HasFullOversight)
                throw new UnauthorizedAccessException("Reports are restricted to Admin and Manager roles.");

            try
            {
                var query = _db.SupportTickets
                    .Include(t => t.Customer).ThenInclude(c => c.AssignedAgent)
                    .Include(t => t.AssignedToUser)
                    .Include(t => t.RaisedByUser)
                    .Where(t => t.CreatedAt >= range.Start && t.CreatedAt <= range.End);

                if (CurrentSession.CanAccessBranching && CurrentSession.ActiveBranchId.HasValue)
                {
                    int bId = CurrentSession.ActiveBranchId.Value;
                    query = query.Where(t =>
                        (t.AssignedToUser != null && t.AssignedToUser.BranchId == bId) ||
                        (t.RaisedByUser != null && t.RaisedByUser.BranchId == bId) ||
                        (t.Customer != null && t.Customer.AssignedAgent != null && t.Customer.AssignedAgent.BranchId == bId));
                }

                if (!string.IsNullOrEmpty(priority))
                    query = query.Where(t => t.Priority.ToLower() == priority.ToLower());

                if (!string.IsNullOrEmpty(status))
                    query = query.Where(t => t.Status.ToLower() == status.ToLower());

                var tickets = query.OrderByDescending(t => t.CreatedAt).ToList();
                var report = new List<TicketResolutionRow>();

                foreach (var t in tickets)
                {
                    string resolutionTime = "-";
                    if (t.ResolvedAt.HasValue)
                    {
                        var diff = t.ResolvedAt.Value - t.CreatedAt;
                        if (diff.TotalDays >= 1)
                            resolutionTime = $"{diff.TotalDays:F1} days";
                        else
                            resolutionTime = $"{diff.TotalHours:F1} hrs";
                    }

                    string slaMet = "N/A";
                    if (t.DueDate.HasValue)
                    {
                        if (t.ResolvedAt.HasValue && t.ResolvedAt.Value <= t.DueDate.Value)
                            slaMet = "Yes";
                        else if (t.ResolvedAt.HasValue && t.ResolvedAt.Value > t.DueDate.Value)
                            slaMet = "No";
                        else if (DateTime.UtcNow > t.DueDate.Value)
                            slaMet = "No";
                        else
                            slaMet = "Pending";
                    }

                    report.Add(new TicketResolutionRow
                    {
                        TicketNumber = string.IsNullOrWhiteSpace(t.TicketNumber) ? $"TICK-{t.TicketId:D4}" : t.TicketNumber,
                        CustomerName = t.Customer?.FullName ?? "Unknown",
                        Category = t.Category,
                        Priority = t.Priority,
                        Status = t.Status,
                        AgentName = t.AssignedToUser?.FullName ?? "Unassigned",
                        OpenedDate = t.CreatedAt.ToString("MMM dd, yyyy"),
                        ResolvedDate = t.ResolvedAt?.ToString("MMM dd, yyyy") ?? "-",
                        ResolutionTime = resolutionTime,
                        SlaMet = slaMet
                    });
                }
                return report;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error in GetTicketResolutionReport: {ex.Message}");
                return new List<TicketResolutionRow>();
            }
        }

        public List<AgentActivityRow> GetAgentActivityReport(DateRangeFilter range, int? agentId = null)
        {
            if (!RbacService.HasFullOversight)
                throw new UnauthorizedAccessException("Reports are restricted to Admin and Manager roles.");

            try
            {
                var query = _db.Users.AsNoTracking()
                    .Include(u => u.Role)
                    .Where(u => u.Role.RoleName.ToLower() == "agent" && u.Status.ToLower() != "inactive");

                if (CurrentSession.CanAccessBranching && CurrentSession.ActiveBranchId.HasValue)
                    query = query.Where(u => u.BranchId == CurrentSession.ActiveBranchId.Value);

                if (agentId.HasValue)
                    query = query.Where(u => u.UserId == agentId.Value);

                var agents = query.ToList();
                var report = new List<AgentActivityRow>();

                foreach (var agent in agents)
                {
                    int activeLeads = _db.Leads.Count(l => l.AssignedAgentId == agent.UserId && l.CreatedAt >= range.Start && l.CreatedAt <= range.End);
                    int leadsConverted = _db.Leads.Count(l => l.AssignedAgentId == agent.UserId && l.ConvertedCustomerId.HasValue && l.CreatedAt >= range.Start && l.CreatedAt <= range.End);
                    double convRate = activeLeads == 0 ? 0 : Math.Round(((double)leadsConverted / activeLeads) * 100, 1);

                    var deals = _db.Deals.Where(d => d.AgentId == agent.UserId &&
                        (d.Stage.ToLower() == "closed" || d.Stage.ToLower() == "closed-won" || d.Stage.ToLower() == "won") &&
                        (d.ContractSignedDate ?? d.CreatedAt) >= range.Start && (d.ContractSignedDate ?? d.CreatedAt) <= range.End).ToList();
                    int dealsClosed = deals.Count;
                    decimal totalSalesVol = deals.Sum(d => d.Value);
                    decimal totalComm = deals.Sum(d => d.Value * (d.CommissionRate > 1m ? d.CommissionRate / 100m : d.CommissionRate));

                    int ticketsResolved = _db.SupportTickets.Count(t => t.AssignedToUserId == agent.UserId && t.ResolvedAt.HasValue && t.ResolvedAt.Value >= range.Start && t.ResolvedAt.Value <= range.End);
                    // Strictly enforce RBAC boundary: Manager has NO visibility into individual Agent follow-up task records
                    int followUpsCompleted = RbacService.IsManager ? 0 : _db.TaskReminders.Count(t => t.AssignedToUserId == agent.UserId && t.Status.ToLower() == "completed" && t.CompletedAt.HasValue && t.CompletedAt.Value >= range.Start && t.CompletedAt.Value <= range.End);

                    report.Add(new AgentActivityRow
                    {
                        AgentName = agent.FullName,
                        ActiveLeads = activeLeads,
                        LeadsConverted = leadsConverted,
                        ConversionRate = convRate,
                        DealsClosed = dealsClosed,
                        TotalSalesVolume = totalSalesVol,
                        TotalCommissionEarned = totalComm,
                        TicketsResolved = ticketsResolved,
                        FollowUpsCompleted = followUpsCompleted
                    });
                }

                return report.OrderByDescending(r => r.TotalSalesVolume).ThenByDescending(r => r.DealsClosed).ToList();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error in GetAgentActivityReport: {ex.Message}");
                return new List<AgentActivityRow>();
            }
        }

        public void ExportToCsv<T>(List<T> data, string filePath, ReportHeader header, string? activeFilter = null)
        {
            if (typeof(T) == typeof(CommissionReportRow) && !RbacService.CanExportFinancialSettlements)
                throw new UnauthorizedAccessException("Commission and financial settlement exports are restricted to Administrators.");

            try
            {
                if (File.Exists(filePath))
                {
                    try { File.SetAttributes(filePath, FileAttributes.Normal); } catch { }
                }

                using var writer = new StreamWriter(filePath, false, new UTF8Encoding(true));
                writer.WriteLine($"# NEXA CRM — Executive Report");
                writer.WriteLine($"# Report Name: {header.ReportName}");
                writer.WriteLine($"# Date Range: {header.DateRange}");
                writer.WriteLine($"# Generated By: {header.GeneratedBy}");
                writer.WriteLine($"# Generated At: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
                if (!string.IsNullOrEmpty(activeFilter))
                    writer.WriteLine($"# Active Filter: {activeFilter}");
                writer.WriteLine();

                PropertyInfo[] props = GetReportDisplayProperties<T>();
                var headerLine = string.Join(",", props.Select(p => EscapeCsv(FormatHeaderName(p.Name))));
                writer.WriteLine(headerLine);

                var numericProps = props.Where(p => (p.PropertyType == typeof(decimal) || p.PropertyType == typeof(double) || p.PropertyType == typeof(int)) &&
                                                    !p.Name.Contains("Rate") && !p.Name.Contains("Percent")).ToList();
                var totals = new Dictionary<string, decimal>();
                foreach (var np in numericProps) totals[np.Name] = 0m;

                foreach (var item in data)
                {
                    var values = props.Select(p =>
                    {
                        var val = p.GetValue(item, null);
                        
                        if (val != null && totals.ContainsKey(p.Name))
                        {
                            if (val is decimal dec) totals[p.Name] += dec;
                            else if (val is double dbl) totals[p.Name] += (decimal)dbl;
                            else if (val is int i) totals[p.Name] += i;
                        }
                        
                        return EscapeCsv(FormatCellValue(p, val));
                    });
                    writer.WriteLine(string.Join(",", values));
                }

                if (data.Count > 0)
                {
                    var totalsValues = props.Select((p, i) =>
                    {
                        if (i == 0) return EscapeCsv("TOTALS");
                        if (totals.ContainsKey(p.Name))
                        {
                            return EscapeCsv(FormatCellValue(p, totals[p.Name]));
                        }
                        return "";
                    });
                    writer.WriteLine(string.Join(",", totalsValues));
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error in ExportToCsv: {ex.Message}");
                throw;
            }
        }

        public void ExportToExcel<T>(List<T> data, string filePath, ReportHeader header, string? activeFilter = null)
        {
            if (typeof(T) == typeof(CommissionReportRow) && !RbacService.CanExportFinancialSettlements)
                throw new UnauthorizedAccessException("Commission and financial settlement exports are restricted to Administrators.");

            try
            {
                using var workbook = new XLWorkbook();
                var wsName = header.ReportName.Length > 31 ? header.ReportName.Substring(0, 31) : header.ReportName;
                wsName = string.Join("_", wsName.Split(Path.GetInvalidFileNameChars())).Replace(":", "").Replace("/", "").Replace("\\", "").Replace("?", "").Replace("*", "").Replace("[", "").Replace("]", "");
                if (string.IsNullOrWhiteSpace(wsName)) wsName = "Report";
                var ws = workbook.Worksheets.Add(wsName);

                PropertyInfo[] props = GetReportDisplayProperties<T>();
                int colCount = props.Length;

                var titleCell = ws.Cell(1, 1);
                titleCell.Value = "NEXA CRM — Executive Report";
                titleCell.Style.Font.Bold = true;
                titleCell.Style.Font.FontSize = 14;
                ws.Range(1, 1, 1, colCount).Merge();

                ws.Cell(2, 1).Value = "Report Name:";
                ws.Cell(2, 2).Value = header.ReportName;
                ws.Cell(2, 2).Style.Font.Bold = true;
                ws.Cell(2, 2).Style.Font.FontSize = 11;

                ws.Cell(3, 1).Value = "Date Range:";
                ws.Cell(3, 2).Value = header.DateRange;

                ws.Cell(4, 1).Value = "Generated By:";
                ws.Cell(4, 2).Value = header.GeneratedBy;
                ws.Cell(4, 3).Value = "Generated At:";
                ws.Cell(4, 4).Value = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");

                if (!string.IsNullOrEmpty(activeFilter))
                {
                    ws.Cell(5, 1).Value = "Active Filter:";
                    ws.Cell(5, 2).Value = activeFilter;
                }

                int row = 7;
                for (int i = 0; i < props.Length; i++)
                {
                    var cell = ws.Cell(row, i + 1);
                    cell.Value = FormatHeaderName(props[i].Name);
                    cell.Style.Font.Bold = true;
                    cell.Style.Fill.BackgroundColor = XLColor.FromHtml("#25679C");
                    cell.Style.Font.FontColor = XLColor.White;
                    cell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                }

                row++;
                int startDataRow = row;
                foreach (var item in data)
                {
                    bool isEvenRow = (row - startDataRow) % 2 == 1;
                    
                    for (int i = 0; i < props.Length; i++)
                    {
                        var cell = ws.Cell(row, i + 1);
                        if (isEvenRow) cell.Style.Fill.BackgroundColor = XLColor.FromHtml("#F8FAFC");
                        
                        var val = props[i].GetValue(item, null);
                        if (val != null)
                        {
                            Type t = props[i].PropertyType;
                            if (t == typeof(decimal) || t == typeof(double) || t == typeof(int))
                            {
                                if (t == typeof(decimal) || t == typeof(double))
                                {
                                    if (props[i].Name.Contains("Percent") || props[i].Name.Contains("Rate"))
                                    {
                                        cell.Value = Convert.ToDouble(val);
                                        cell.Style.NumberFormat.Format = "0.0\"%\"";
                                    }
                                    else
                                    {
                                        cell.Value = Convert.ToDouble(val);
                                        cell.Style.NumberFormat.Format = "₱#,##0.00";
                                    }
                                }
                                else if (t == typeof(int))
                                {
                                    cell.Value = Convert.ToInt32(val);
                                    cell.Style.NumberFormat.Format = "#,##0";
                                }
                            }
                            else
                            {
                                cell.Value = val.ToString();
                            }
                        }
                    }
                    row++;
                }

                if (data.Count > 0)
                {
                    ws.Cell(row, 1).Value = "TOTALS";
                    ws.Row(row).Style.Font.Bold = true;
                    ws.Row(row).Style.Fill.BackgroundColor = XLColor.FromHtml("#E2E8F0");

                    for (int i = 0; i < props.Length; i++)
                    {
                        Type t = props[i].PropertyType;
                        if ((t == typeof(decimal) || t == typeof(double) || t == typeof(int)) && 
                            !props[i].Name.Contains("Percent") && !props[i].Name.Contains("Rate"))
                        {
                            string colLetter = ws.Cell(1, i + 1).WorksheetColumn().ColumnLetter();
                            ws.Cell(row, i + 1).FormulaA1 = $"SUM({colLetter}{startDataRow}:{colLetter}{row - 1})";
                        }
                    }
                }

                ws.Columns().AdjustToContents();
                ws.SheetView.FreezeRows(7);
                ws.Range(7, 1, row - 1, colCount).SetAutoFilter();
                ws.Protect();

                workbook.SaveAs(filePath);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error in ExportToExcel: {ex.Message}");
                throw;
            }
        }

        private string EscapeCsv(string value)
        {
            if (string.IsNullOrEmpty(value)) return "";
            if (value.Contains(",") || value.Contains("\"") || value.Contains("\n") || value.Contains("\r"))
            {
                return $"\"{value.Replace("\"", "\"\"")}\"";
            }
            return value;
        }

        public void ExportToPdf<T>(List<T> data, string filePath, ReportHeader header, string? activeFilter = null)
        {
            if (typeof(T) == typeof(CommissionReportRow) && !RbacService.CanExportFinancialSettlements)
                throw new UnauthorizedAccessException("Commission and financial settlement exports are restricted to Administrators.");

            try
            {
                PropertyInfo[] props = GetReportDisplayProperties<T>();

                Document.Create(container =>
                {
                    container.Page(page =>
                    {
                        page.Margin(30);
                        page.Size(PageSizes.A4.Landscape());
                        page.PageColor(Colors.White);
                        page.DefaultTextStyle(x => x.FontSize(9).FontFamily("Segoe UI"));

                        if (typeof(T) == typeof(CommissionReportRow))
                        {
                            page.Foreground().AlignCenter().AlignMiddle().Text("CONFIDENTIAL")
                                .FontFamily("Segoe UI").FontSize(100).FontColor(Colors.Grey.Lighten3).SemiBold();
                        }

                        page.Header().Column(col =>
                        {
                            col.Item().Row(r =>
                            {
                                r.ConstantItem(150).AlignLeft().AlignMiddle().Element(e =>
                                {
                                    try
                                    {
                                        var stream = System.Reflection.Assembly.GetExecutingAssembly()
                                            .GetManifestResourceStream("CRMS_Peguit.winforms.Assets.logo.png");
                                        if (stream != null)
                                        {
                                            e.Height(40).Image(stream);
                                        }
                                        else
                                        {
                                            e.Text("");
                                        }
                                    }
                                    catch
                                    {
                                        e.Text("");
                                    }
                                });
                                r.RelativeItem().Column(c =>
                                {
                                    c.Item().Text("NEXA REAL ESTATE CRM").Bold().FontSize(18).FontColor(Colors.Blue.Darken2);
                                    c.Item().Text(header.ReportName).Bold().FontSize(13).FontColor(Colors.Grey.Darken3);
                                    if (!string.IsNullOrEmpty(activeFilter))
                                    {
                                        c.Item().Text($"Filtered by: {activeFilter}").FontSize(9).FontColor(Colors.Grey.Medium);
                                    }
                                });
                                r.RelativeItem().AlignRight().Column(c =>
                                {
                                    c.Item().Text($"Generated: {header.GeneratedAt:yyyy-MM-dd HH:mm}").FontSize(8).FontColor(Colors.Grey.Darken1);
                                    c.Item().Text($"By: {header.GeneratedBy}").FontSize(8).FontColor(Colors.Grey.Darken1);
                                    c.Item().Text($"Period: {header.DateRange}").FontSize(8).FontColor(Colors.Grey.Darken1);
                                });
                            });
                            col.Item().PaddingVertical(5).LineHorizontal(1).LineColor(Colors.Blue.Darken2);
                        });

                        page.Content().PaddingVertical(10).Column(content =>
                        {
                            content.Item().PaddingBottom(15).Row(r =>
                            {
                                if (typeof(T) == typeof(SalesReportRow))
                                {
                                    var rows = data.Cast<SalesReportRow>().ToList();
                                    AddKpiCard(r, "Total Deals", rows.Count.ToString(), "#25679C");
                                    AddKpiCard(r, "Total Volume", $"₱ {rows.Sum(x => x.DealValue):N2}", "#059669");
                                    AddKpiCard(r, "Total Commission", $"₱ {rows.Sum(x => x.Commission):N2}", "#D97706");
                                    AddKpiCard(r, "Avg Deal Size", $"₱ {(rows.Any() ? rows.Average(x => x.DealValue) : 0):N2}", "#8B5CF6");
                                }
                                else if (typeof(T) == typeof(CommissionReportRow))
                                {
                                    var rows = data.Cast<CommissionReportRow>().ToList();
                                    AddKpiCard(r, "Records", rows.Count.ToString(), "#25679C");
                                    AddKpiCard(r, "Gross Commission", $"₱ {rows.Sum(x => x.GrossCommission):N2}", "#059669");
                                    AddKpiCard(r, "Agent Payouts", $"₱ {rows.Sum(x => x.AgentPayoutAmount):N2}", "#D97706");
                                    string brokerNet = RbacService.CanViewBrokerageMargins ? $"₱ {rows.Sum(x => x.BrokerageRetainedAmount):N2}" : "Restricted";
                                    AddKpiCard(r, "Brokerage Net", brokerNet, "#8B5CF6");
                                }
                                else if (typeof(T) == typeof(PropertyInventoryReportRow))
                                {
                                    var rows = data.Cast<PropertyInventoryReportRow>().ToList();
                                    AddKpiCard(r, "Total Properties", rows.Count.ToString(), "#25679C");
                                    AddKpiCard(r, "Total Value", $"₱ {rows.Sum(x => x.ListingPrice):N2}", "#059669");
                                    AddKpiCard(r, "Avg DOM", $"{(rows.Any() ? rows.Average(x => x.DaysOnMarket) : 0):N0}", "#D97706");
                                    AddKpiCard(r, "Active Deals", rows.Sum(x => x.AssociatedDeals).ToString(), "#8B5CF6");
                                }
                                else if (typeof(T) == typeof(LeadProgressRow))
                                {
                                    var rows = data.Cast<LeadProgressRow>().ToList();
                                    int conv = rows.Count(x => x.ConvertedToCustomer == "Yes");
                                    AddKpiCard(r, "Total Leads", rows.Count.ToString(), "#25679C");
                                    AddKpiCard(r, "Converted", conv.ToString(), "#059669");
                                    AddKpiCard(r, "Conv Rate", $"{(rows.Any() ? (conv * 100.0 / rows.Count) : 0):N1}%", "#D97706");
                                    AddKpiCard(r, "Est Pipeline Value", $"₱ {rows.Sum(x => x.EstimatedBudget):N2}", "#8B5CF6");
                                }
                                else if (typeof(T) == typeof(TicketResolutionRow))
                                {
                                    var rows = data.Cast<TicketResolutionRow>().ToList();
                                    int res = rows.Count(x => x.Status == "Resolved" || x.Status == "Closed");
                                    int breached = rows.Count(x => x.SlaMet == "No");
                                    int slaMet = rows.Count(x => x.SlaMet == "Yes");
                                    AddKpiCard(r, "Total Tickets", rows.Count.ToString(), "#25679C");
                                    AddKpiCard(r, "Resolved", res.ToString(), "#059669");
                                    AddKpiCard(r, "SLA Rate", $"{(rows.Any() ? (slaMet * 100.0 / rows.Count) : 0):N1}%", "#D97706");
                                    AddKpiCard(r, "Breached", breached.ToString(), "#DC2626");
                                }
                                else if (typeof(T) == typeof(AgentActivityRow))
                                {
                                    var rows = data.Cast<AgentActivityRow>().ToList();
                                    AddKpiCard(r, "Total Agents", rows.Count.ToString(), "#25679C");
                                    AddKpiCard(r, "Deals Closed", rows.Sum(x => x.DealsClosed).ToString(), "#059669");
                                    AddKpiCard(r, "Sales Volume", $"₱ {rows.Sum(x => x.TotalSalesVolume):N2}", "#D97706");
                                    AddKpiCard(r, "Tickets Resolved", rows.Sum(x => x.TicketsResolved).ToString(), "#8B5CF6");
                                }
                            });

                            content.Item().Table(table =>
                            {
                                table.ColumnsDefinition(columns =>
                                {
                                    foreach (var p in props)
                                    {
                                        if (p.Name.Contains("Address") || p.Name.Contains("Customer") || p.Name.Contains("LeadName"))
                                            columns.RelativeColumn(3);
                                        else if (p.Name.Contains("Ref") || p.Name.Contains("Agent") || p.Name.Contains("Status") || p.Name.Contains("Stage"))
                                            columns.RelativeColumn(2);
                                        else
                                            columns.RelativeColumn(1.8f);
                                    }
                                });

                                table.Header(h =>
                                {
                                    foreach (var p in props)
                                    {
                                        h.Cell().Background(Colors.Grey.Lighten3).Padding(4)
                                            .Text(FormatHeaderName(p.Name)).Bold().FontSize(8).FontColor(Colors.Grey.Darken3);
                                    }
                                });

                                int rowIndex = 0;
                                foreach (var item in data)
                                {
                                    string rowBgColor = (rowIndex % 2 == 1) ? "#F8FAFC" : "#FFFFFF"; // Odd rows white, Even rows light gray (0-indexed so even are 1, 3, etc. or vice versa depending on definition; user said even rows very light gray, odd rows white. Row 1 (index 0, odd) white. Row 2 (index 1, even) gray)
                                    foreach (var p in props)
                                    {
                                        var val = p.GetValue(item, null);
                                        string cellText = FormatCellValue(p, val);
                                        bool isNumeric = p.PropertyType == typeof(decimal) || p.PropertyType == typeof(double) || p.PropertyType == typeof(int);

                                        var cell = table.Cell().Background(rowBgColor).BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten2).Padding(4);

                                        if (p.Name == "Status" || p.Name == "Stage" || p.Name == "SettlementStatus" || p.Name == "Priority" || p.Name == "SlaMet")
                                        {
                                            string textColor = GetStatusColor(cellText);
                                            cell.AlignLeft().Text(cellText).FontSize(8).FontColor(textColor).Bold();
                                        }
                                        else if (isNumeric)
                                        {
                                            cell.AlignRight().Text(cellText).FontSize(8);
                                        }
                                        else
                                        {
                                            cell.AlignLeft().Text(cellText).FontSize(8);
                                        }
                                    }
                                    rowIndex++;
                                }

                                table.Cell().ColumnSpan((uint)props.Length).PaddingTop(2).LineHorizontal(1).LineColor(Colors.Black);
                                
                                bool firstTextFound = false;
                                foreach (var p in props)
                                {
                                    var cell = table.Cell().Padding(4);
                                    bool isNumeric = p.PropertyType == typeof(decimal) || p.PropertyType == typeof(double) || p.PropertyType == typeof(int);
                                    bool isPercentage = p.Name.Contains("Rate") || p.Name.Contains("Percent");

                                    if (!isNumeric)
                                    {
                                        if (!firstTextFound)
                                        {
                                            cell.AlignLeft().Text("TOTALS").Bold().FontSize(8);
                                            firstTextFound = true;
                                        }
                                        else
                                        {
                                            cell.Text("");
                                        }
                                    }
                                    else if (isPercentage)
                                    {
                                         cell.Text("");
                                    }
                                    else
                                    {
                                        decimal sum = 0;
                                        foreach (var item in data)
                                        {
                                            var val = p.GetValue(item, null);
                                            if (val != null)
                                            {
                                                if (decimal.TryParse(val.ToString(), out decimal d)) sum += d;
                                            }
                                        }
                                        string sumText = p.PropertyType == typeof(int) ? sum.ToString("N0") : (p.Name.Contains("Value") || p.Name.Contains("Amount") || p.Name.Contains("Commission") || p.Name.Contains("Price") || p.Name.Contains("Budget") || p.Name.Contains("Sales") ? $"₱ {sum:N2}" : sum.ToString("N2"));
                                        cell.AlignRight().Text(sumText).Bold().FontSize(8);
                                    }
                                }
                            });
                        });

                        page.Footer().Column(f =>
                        {
                            f.Item().LineHorizontal(0.5f).LineColor(Colors.Grey.Lighten2);
                            f.Item().PaddingTop(4).Row(r =>
                            {
                                r.RelativeItem().Text("CONFIDENTIAL · Real Estate Business Intelligence").FontSize(8).FontColor(Colors.Grey.Darken1);
                                r.RelativeItem().AlignRight().Text(x =>
                                {
                                    x.DefaultTextStyle(t => t.FontSize(8).FontColor(Colors.Grey.Darken1));
                                    x.Span("Page ");
                                    x.CurrentPageNumber();
                                    x.Span(" of ");
                                    x.TotalPages();
                                });
                            });
                        });
                    });
                })
                .GeneratePdf(filePath);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error in ExportToPdf: {ex.Message}");
                try { if (File.Exists(filePath)) File.Delete(filePath); } catch { }
                throw;
            }
        }

        private void AddKpiCard(RowDescriptor row, string label, string value, string topBorderColor)
        {
            row.RelativeItem().PaddingRight(10).Column(c =>
            {
                c.Item().Height(3).Background(topBorderColor);
                c.Item().Background(Colors.Grey.Lighten4).Padding(10).Column(card =>
                {
                    card.Item().Text(label).FontSize(8).FontColor(Colors.Grey.Darken2);
                    card.Item().Text(value).FontSize(12).Bold().FontColor(Colors.Black);
                });
            });
        }

        private string GetStatusColor(string status)
        {
            if (string.IsNullOrWhiteSpace(status)) return "#475569";
            var s = status.Trim().ToLowerInvariant();
            if (s == "won" || s == "closed" || s == "resolved" || s == "active" || s == "yes" || s == "completed" || s == "available")
                return "#059669";
            if (s == "in progress" || s == "contacted" || s == "pending" || s == "offer" || s == "under contract" || s == "qualified")
                return "#D97706";
            if (s == "lost" || s == "overdue" || s == "breached" || s == "cancelled" || s == "critical" || s == "urgent" || s == "high" || s == "no" || s == "inactive")
                return "#DC2626";
            return "#475569";
        }

        private static PropertyInfo[] GetReportDisplayProperties<T>()
        {
            var ignoredAliases = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            {
                "Value", "ClosedDate", "DaysInStage", "CommissionRate", "CommissionAmount", "LeadsWorked"
            };

            if (!RbacService.CanViewBrokerageMargins)
            {
                ignoredAliases.Add("BrokerageRetainedAmount");
            }

            return typeof(T).GetProperties()
                .Where(p => !ignoredAliases.Contains(p.Name))
                .ToArray();
        }

        private static string FormatHeaderName(string propName)
        {
            return propName switch
            {
                "DealRef" => "Deal Ref",
                "CustomerName" => "Customer Name",
                "PropertyAddress" => "Property Address",
                "PropertyType" => "Property Type",
                "AgentName" => "Assigned Agent",
                "DealValue" => "Deal Value (₱)",
                "Commission" => "Commission (₱)",
                "GrossCommission" => "Gross Comm (₱)",
                "AgentSplitPercent" => "Agent Split %",
                "AgentPayoutAmount" => "Agent Payout (₱)",
                "BrokerageRetainedAmount" => "Brokerage Net (₱)",
                "SettlementStatus" => "Settlement Status",
                "ExpectedOrClosedDate" => "Closing / Expected Date",
                "PropertyRef" => "Property Ref",
                "ListingPrice" => "Listing Price (₱)",
                "ListingAgent" => "Listing Agent",
                "DaysOnMarket" => "Days on Market",
                "AssociatedDeals" => "Active Deals",
                "ListedDate" => "Listed Date",
                "LeadName" => "Lead Name",
                "EstimatedBudget" => "Est. Budget (₱)",
                "DaysInPipeline" => "Days in Pipeline",
                "ConvertedToCustomer" => "Converted",
                "CreatedDate" => "Created Date",
                "TicketNumber" => "Ticket #",
                "OpenedDate" => "Opened Date",
                "ResolvedDate" => "Resolved Date",
                "ResolutionTime" => "Resolution Time",
                "SlaMet" => "SLA Met",
                "ActiveLeads" => "Active Leads",
                "LeadsConverted" => "Converted Leads",
                "ConversionRate" => "Conv. Rate %",
                "DealsClosed" => "Deals Closed",
                "TotalSalesVolume" => "Sales Volume (₱)",
                "TotalCommissionEarned" => "Commission Earned (₱)",
                "FollowUpsCompleted" => "Follow-Ups Done",
                "TicketsResolved" => "Tickets Resolved",
                _ => Regex.Replace(propName, "([A-Z])", " $1").Trim()
            };
        }

        private static string FormatCellValue(PropertyInfo prop, object? val)
        {
            if (val == null) return "-";
            if (val is decimal dec)
            {
                if (prop.Name.Contains("Percent") || prop.Name.Contains("Rate"))
                    return $"{dec:F1}%";
                return dec.ToString("N2", CultureInfo.InvariantCulture);
            }
            if (val is double dbl)
            {
                if (prop.Name.Contains("Rate") || prop.Name.Contains("Percent"))
                    return $"{dbl:F1}%";
                return dbl.ToString("N1", CultureInfo.InvariantCulture);
            }
            if (val is int num)
            {
                return num.ToString("N0", CultureInfo.InvariantCulture);
            }
            return val.ToString() ?? "-";
        }

        public List<AgentPickerItem> GetAgentList()
        {
            try
            {
                using var db = LocalDb.CreateContext(CurrentSession.TenantId);
                return db.Users.AsNoTracking()
                    .Include(u => u.Role)
                    .Where(u => u.Role.RoleName.ToLower() == "agent" && u.Status.ToLower() != "inactive")
                    .AsEnumerable()
                    .Select(u => new AgentPickerItem(u.UserId, u.FullName, u.Email))
                    .ToList();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error in GetAgentList: {ex.Message}");
                return new List<AgentPickerItem>();
            }
        }

        public void Dispose()
        {
            _db?.Dispose();
        }
    }
}
