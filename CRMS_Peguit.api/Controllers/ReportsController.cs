using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using CRMS_Peguit.domain.entities;
using CRMS_Peguit.domain.Common;
using CRMS_Peguit.infrastructure.data;

namespace CRMS_Peguit.api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ReportsController : ControllerBase
    {
        private readonly RealEstateDbContext _db;

        public ReportsController(RealEstateDbContext db)
        {
            _db = db;
        }

        private bool ValidateOversight()
        {
            return ApiSecurityHelper.HasFullOversight(User);
        }

        private static bool IsValidDateRange(DateRangeFilter? range, out ActionResult? errorResult)
        {
            if (range is null)
            {
                errorResult = new BadRequestObjectResult(new { message = "Date range filter is required." });
                return false;
            }
            if (range.Start > range.End)
            {
                errorResult = new BadRequestObjectResult(new { message = "Date range start cannot be after end date." });
                return false;
            }
            errorResult = null;
            return true;
        }

        [HttpPost("sales")]
        public async Task<ActionResult<List<SalesReportRow>>> GetSalesReport(
            [FromBody] DateRangeFilter range,
            [FromQuery] int? agentId = null,
            [FromQuery] string? propertyType = null)
        {
            if (!ValidateOversight()) return Forbid();
            if (!IsValidDateRange(range, out var error)) return error!;

            var query = _db.Deals
                .Include(d => d.Customer)
                .Include(d => d.Property)
                .Include(d => d.Agent)
                .Where(d => d.CreatedAt >= range.Start && d.CreatedAt <= range.End);

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

            var deals = await query.OrderByDescending(d => d.CreatedAt).ToListAsync();

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

            return Ok(report);
        }

        [HttpPost("lead-progress")]
        public async Task<ActionResult<List<LeadProgressRow>>> GetLeadProgressReport(
            [FromBody] DateRangeFilter range,
            [FromQuery] int? agentId = null,
            [FromQuery] string? source = null)
        {
            if (!ValidateOversight()) return Forbid();
            if (!IsValidDateRange(range, out var error)) return error!;

            var query = _db.Leads
                .Include(l => l.AssignedAgent)
                .Where(l => l.CreatedAt >= range.Start && l.CreatedAt <= range.End);

            if (agentId.HasValue)
                query = query.Where(l => l.AssignedAgentId == agentId.Value);

            if (!string.IsNullOrEmpty(source))
                query = query.Where(l => l.Source == source);

            var leads = await query.OrderByDescending(l => l.CreatedAt).ToListAsync();

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

            return Ok(report);
        }

        [HttpPost("commissions")]
        public async Task<ActionResult<List<CommissionReportRow>>> GetCommissionReport(
            [FromBody] DateRangeFilter range,
            [FromQuery] int? agentId = null)
        {
            if (!ValidateOversight()) return Forbid();
            if (!IsValidDateRange(range, out var error)) return error!;

            var query = _db.Deals
                .Include(d => d.Agent)
                .Include(d => d.Property)
                .Include(d => d.Customer)
                .Where(d => (d.Stage.ToLower() == "closed" || d.Stage.ToLower() == "closed-won" || d.Stage.ToLower() == "won") &&
                            (d.ContractSignedDate ?? d.CreatedAt) >= range.Start && (d.ContractSignedDate ?? d.CreatedAt) <= range.End);

            if (agentId.HasValue)
                query = query.Where(d => d.AgentId == agentId.Value);

            var deals = await query.ToListAsync();
            bool canViewBrokerageMargins = ApiSecurityHelper.IsAdmin(User);

            var report = new List<CommissionReportRow>();
            foreach (var d in deals)
            {
                decimal grossRate = d.CommissionRate > 1m ? d.CommissionRate / 100m : d.CommissionRate;
                decimal grossComm = d.Value * grossRate;
                decimal agentSplit = 70m;
                decimal agentPayout = grossComm * (agentSplit / 100m);
                decimal brokerageRetained = canViewBrokerageMargins ? (grossComm - agentPayout) : 0m;

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

            return Ok(report.OrderByDescending(r => r.CloseDate).ToList());
        }

        [HttpPost("inventory")]
        public async Task<ActionResult<List<PropertyInventoryReportRow>>> GetPropertyInventoryReport(
            [FromBody] DateRangeFilter range,
            [FromQuery] string? propertyType = null,
            [FromQuery] string? status = null)
        {
            if (!ValidateOversight()) return Forbid();
            if (!IsValidDateRange(range, out var error)) return error!;

            var query = _db.Properties
                .Include(p => p.ListedByAgent)
                .Include(p => p.OwnerCustomer)
                .Where(p => p.CreatedAt >= range.Start && p.CreatedAt <= range.End);

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

            var properties = await query.OrderByDescending(p => p.CreatedAt).ToListAsync();
            var propIds = properties.Select(p => p.PropertyId).ToList();
            var dealCounts = await _db.Deals
                .Where(d => propIds.Contains(d.PropertyId))
                .GroupBy(d => d.PropertyId)
                .Select(g => new { PropertyId = g.Key, Count = g.Count() })
                .ToDictionaryAsync(g => g.PropertyId, g => g.Count);

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

            return Ok(report);
        }

        [HttpPost("tickets")]
        public async Task<ActionResult<List<TicketResolutionRow>>> GetTicketResolutionReport(
            [FromBody] DateRangeFilter range,
            [FromQuery] string? priority = null,
            [FromQuery] string? status = null)
        {
            if (!ValidateOversight()) return Forbid();
            if (!IsValidDateRange(range, out var error)) return error!;

            var query = _db.SupportTickets
                .Include(t => t.Customer).ThenInclude(c => c.AssignedAgent)
                .Include(t => t.AssignedToUser)
                .Include(t => t.RaisedByUser)
                .Where(t => t.CreatedAt >= range.Start && t.CreatedAt <= range.End);

            if (!string.IsNullOrEmpty(priority))
                query = query.Where(t => t.Priority.ToLower() == priority.ToLower());

            if (!string.IsNullOrEmpty(status))
                query = query.Where(t => t.Status.ToLower() == status.ToLower());

            var tickets = await query.OrderByDescending(t => t.CreatedAt).ToListAsync();
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

            return Ok(report);
        }

        [HttpPost("agent-activity")]
        public async Task<ActionResult<List<AgentActivityRow>>> GetAgentActivityReport(
            [FromBody] DateRangeFilter range,
            [FromQuery] int? agentId = null)
        {
            if (!ValidateOversight()) return Forbid();
            if (!IsValidDateRange(range, out var error)) return error!;

            var query = _db.Users.AsNoTracking()
                .Include(u => u.Role)
                .Where(u => u.Role.RoleName.ToLower() == "agent" && u.Status.ToLower() != "inactive");

            if (agentId.HasValue)
                query = query.Where(u => u.UserId == agentId.Value);

            var agents = await query.ToListAsync();
            var report = new List<AgentActivityRow>();
            bool isManager = ApiSecurityHelper.IsManager(User);

            foreach (var agent in agents)
            {
                int activeLeads = await _db.Leads.CountAsync(l => l.AssignedAgentId == agent.UserId && l.CreatedAt >= range.Start && l.CreatedAt <= range.End);
                int leadsConverted = await _db.Leads.CountAsync(l => l.AssignedAgentId == agent.UserId && l.ConvertedCustomerId.HasValue && l.CreatedAt >= range.Start && l.CreatedAt <= range.End);
                double convRate = activeLeads == 0 ? 0 : Math.Round(((double)leadsConverted / activeLeads) * 100, 1);

                var deals = await _db.Deals.Where(d => d.AgentId == agent.UserId &&
                    (d.Stage.ToLower() == "closed" || d.Stage.ToLower() == "closed-won" || d.Stage.ToLower() == "won") &&
                    (d.ContractSignedDate ?? d.CreatedAt) >= range.Start && (d.ContractSignedDate ?? d.CreatedAt) <= range.End).ToListAsync();
                int dealsClosed = deals.Count;
                decimal totalSalesVol = deals.Sum(d => d.Value);
                decimal totalComm = deals.Sum(d => d.Value * (d.CommissionRate > 1m ? d.CommissionRate / 100m : d.CommissionRate));

                int ticketsResolved = await _db.SupportTickets.CountAsync(t => t.AssignedToUserId == agent.UserId && t.ResolvedAt.HasValue && t.ResolvedAt.Value >= range.Start && t.ResolvedAt.Value <= range.End);
                int followUpsCompleted = isManager ? 0 : await _db.TaskReminders.CountAsync(t => t.AssignedToUserId == agent.UserId && t.Status.ToLower() == "completed" && t.CompletedAt.HasValue && t.CompletedAt.Value >= range.Start && t.CompletedAt.Value <= range.End);

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

            return Ok(report.OrderByDescending(r => r.TotalSalesVolume).ThenByDescending(r => r.DealsClosed).ToList());
        }

        [HttpGet("agents")]
        public async Task<ActionResult<List<AgentPickerItem>>> GetAgentList()
        {
            var agents = await _db.Users.AsNoTracking()
                .Include(u => u.Role)
                .Where(u => u.Role.RoleName.ToLower() == "agent" && u.Status.ToLower() != "inactive")
                .Select(u => new AgentPickerItem(u.UserId, u.FullName, u.Email))
                .ToListAsync();

            return Ok(agents);
        }
    }
}
