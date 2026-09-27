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
    public class AnalyticsController : ControllerBase
    {
        private readonly RealEstateDbContext _db;

        public AnalyticsController(RealEstateDbContext db)
        {
            _db = db;
        }

        private IQueryable<Deal> GetDealsQuery()
        {
            var query = _db.Deals.AsNoTracking();
            if (ApiSecurityHelper.IsAgent(User) && !ApiSecurityHelper.HasFullOversight(User))
            {
                int userId = ApiSecurityHelper.GetUserId(User);
                query = query.Where(d => d.AgentId == userId);
            }
            return query;
        }

        private IQueryable<Lead> GetLeadsQuery()
        {
            var query = _db.Leads.AsNoTracking();
            if (ApiSecurityHelper.IsAgent(User) && !ApiSecurityHelper.HasFullOversight(User))
            {
                int userId = ApiSecurityHelper.GetUserId(User);
                query = query.Where(l => l.AssignedAgentId == userId);
            }
            return query;
        }

        private IQueryable<SupportTicket> GetTicketsQuery()
        {
            var query = _db.SupportTickets.AsNoTracking();
            if (ApiSecurityHelper.IsAgent(User) && !ApiSecurityHelper.HasFullOversight(User))
            {
                int userId = ApiSecurityHelper.GetUserId(User);
                query = query.Where(t => t.AssignedToUserId == userId);
            }
            return query;
        }

        private IQueryable<Property> GetPropertiesQuery()
        {
            var query = _db.Properties.AsNoTracking();
            if (ApiSecurityHelper.IsAgent(User) && !ApiSecurityHelper.HasFullOversight(User))
            {
                int userId = ApiSecurityHelper.GetUserId(User);
                query = query.Where(p => p.ListedByAgentId == userId);
            }
            return query;
        }

        [HttpPost("snapshot")]
        public async Task<ActionResult<AnalyticsSnapshot>> GetSnapshot([FromBody] DateRangeFilter range)
        {
            var startDate = range.StartDate;
            var endDate = range.EndDate;

            var dealsQuery = GetDealsQuery();
            var leadsQuery = GetLeadsQuery();
            var ticketsQuery = GetTicketsQuery();
            var propQuery = GetPropertiesQuery();

            var closedDeals = await dealsQuery
                .Where(d => (d.Stage.ToLower() == "closed" || d.Stage.ToLower() == "closed-won" || d.Stage.ToLower() == "won") &&
                            (d.ContractSignedDate ?? d.CreatedAt) >= startDate && (d.ContractSignedDate ?? d.CreatedAt) <= endDate)
                .ToListAsync();

            var totalDealsClosed = closedDeals.Count;
            var totalSalesVolume = closedDeals.Sum(d => d.Value);
            var totalCommissionEarned = closedDeals.Sum(d => d.Value * (d.CommissionRate > 1m ? d.CommissionRate / 100m : d.CommissionRate));
            var averageDealSize = totalDealsClosed == 0 ? 0 : totalSalesVolume / totalDealsClosed;

            var openDeals = await dealsQuery
                .Where(d => d.Stage.ToLower() != "closed" && d.Stage.ToLower() != "closed-won" && d.Stage.ToLower() != "won" && d.Stage.ToLower() != "lost")
                .ToListAsync();
            var activePipelineValue = openDeals.Sum(d => d.Value);

            var activeLeads = await leadsQuery.CountAsync(l => l.Stage.ToLower() != "converted" && l.Stage.ToLower() != "lost");
            var leadsInRange = await leadsQuery.Where(l => l.CreatedAt >= startDate && l.CreatedAt <= endDate).ToListAsync();
            var totalLeadsInRange = leadsInRange.Count;
            var convertedLeadsInRange = leadsInRange.Count(l => l.Stage.ToLower() == "converted");
            var leadConversionRate = totalLeadsInRange == 0 ? 0 : ((double)convertedLeadsInRange / totalLeadsInRange) * 100;

            var lostDealsCount = await dealsQuery.CountAsync(d => d.Stage.ToLower() == "lost" && d.CreatedAt >= startDate && d.CreatedAt <= endDate);
            var winRate = (totalDealsClosed + lostDealsCount) == 0 ? 0 : ((double)totalDealsClosed / (totalDealsClosed + lostDealsCount)) * 100;

            var openSupportTickets = await ticketsQuery.CountAsync(t => t.Status.ToLower() != "resolved" && t.Status.ToLower() != "closed");

            var closedDealsWithDates = closedDeals.Where(d => d.ContractSignedDate != null).ToList();
            var averageDaysToClose = closedDealsWithDates.Any()
                ? closedDealsWithDates.Average(d => ((d.ContractSignedDate ?? d.CreatedAt) - d.CreatedAt).TotalDays)
                : 0;

            var activeProps = await propQuery.Where(p => p.Status.ToLower() != "sold" && p.Status.ToLower() != "off market").ToListAsync();
            var activePropsCount = activeProps.Count;
            var activeInventoryValue = activeProps.Sum(p => p.Price);

            bool isAgent = ApiSecurityHelper.IsAgent(User) && !ApiSecurityHelper.HasFullOversight(User);

            var snapshot = new AnalyticsSnapshot
            {
                TotalDealsClosed = totalDealsClosed,
                TotalSalesVolume = totalSalesVolume,
                TotalCommissionEarned = totalCommissionEarned,
                ActivePipelineValue = activePipelineValue,
                AverageDealSize = averageDealSize,
                ActiveLeads = activeLeads,
                LeadConversionRate = leadConversionRate,
                WinRate = winRate,
                OpenSupportTickets = openSupportTickets,
                AverageDaysToClose = averageDaysToClose,
                ActivePropertiesCount = activePropsCount,
                ActiveInventoryValue = activeInventoryValue,
                DealsOverTime = await QueryDealsOverTime(range),
                LeadFunnel = await QueryLeadFunnel(range),
                DealsWonVsLost = await QueryDealsWonVsLost(range),
                TicketBreakdown = await QueryTicketBreakdown(range),
                TopAgents = isAgent ? null : await QueryTopAgents(range),
                LeadSourceBreakdown = isAgent ? null : await QueryLeadSourceBreakdown(range),
                PropertyDistribution = await QueryPropertyDistribution(),
                RecentActivity = await QueryRecentActivity(15)
            };

            return Ok(snapshot);
        }

        [HttpPost("deals-over-time")]
        public async Task<ActionResult<List<MonthlyMetric>>> GetDealsOverTime([FromBody] DateRangeFilter range)
        {
            return Ok(await QueryDealsOverTime(range));
        }

        private async Task<List<MonthlyMetric>> QueryDealsOverTime(DateRangeFilter range)
        {
            var startDate = range.StartDate;
            var endDate = range.EndDate;

            var deals = await GetDealsQuery()
                .Where(d => (d.Stage.ToLower() == "closed" || d.Stage.ToLower() == "closed-won" || d.Stage.ToLower() == "won") &&
                            (d.ContractSignedDate ?? d.CreatedAt) >= startDate && (d.ContractSignedDate ?? d.CreatedAt) <= endDate)
                .ToListAsync();

            var durationDays = (endDate - startDate).TotalDays;
            if (durationDays <= 31)
            {
                return deals.GroupBy(d => (d.ContractSignedDate ?? d.CreatedAt).Date)
                    .OrderBy(g => g.Key)
                    .Select(g => new MonthlyMetric
                    {
                        Month = g.Key.ToString("MMM dd"),
                        Count = g.Count(),
                        TotalValue = g.Sum(d => d.Value)
                    }).ToList();
            }
            else
            {
                return deals.GroupBy(d => new { (d.ContractSignedDate ?? d.CreatedAt).Year, (d.ContractSignedDate ?? d.CreatedAt).Month })
                    .OrderBy(g => g.Key.Year).ThenBy(g => g.Key.Month)
                    .Select(g => new MonthlyMetric
                    {
                        Month = new DateTime(g.Key.Year, g.Key.Month, 1).ToString("MMM yyyy"),
                        Count = g.Count(),
                        TotalValue = g.Sum(d => d.Value)
                    }).ToList();
            }
        }

        [HttpPost("lead-funnel")]
        public async Task<ActionResult<LeadFunnelData>> GetLeadFunnel([FromBody] DateRangeFilter range)
        {
            return Ok(await QueryLeadFunnel(range));
        }

        private async Task<LeadFunnelData> QueryLeadFunnel(DateRangeFilter range)
        {
            var leads = await GetLeadsQuery().Where(l => l.CreatedAt >= range.StartDate && l.CreatedAt <= range.EndDate).ToListAsync();
            return new LeadFunnelData
            {
                NewCount = leads.Count(l => l.Stage.ToLower() == "new"),
                ContactedCount = leads.Count(l => l.Stage.ToLower() == "contacted"),
                QualifiedCount = leads.Count(l => l.Stage.ToLower() == "qualified"),
                ConvertedCount = leads.Count(l => l.Stage.ToLower() == "converted"),
                LostCount = leads.Count(l => l.Stage.ToLower() == "lost")
            };
        }

        [HttpPost("deals-won-lost")]
        public async Task<ActionResult<WonLostData>> GetDealsWonVsLost([FromBody] DateRangeFilter range)
        {
            return Ok(await QueryDealsWonVsLost(range));
        }

        private async Task<WonLostData> QueryDealsWonVsLost(DateRangeFilter range)
        {
            var deals = await GetDealsQuery().Where(d => d.CreatedAt >= range.StartDate && d.CreatedAt <= range.EndDate).ToListAsync();
            return new WonLostData
            {
                WonCount = deals.Count(d => d.Stage.ToLower() == "closed" || d.Stage.ToLower() == "closed-won" || d.Stage.ToLower() == "won"),
                LostCount = deals.Count(d => d.Stage.ToLower() == "lost" || d.Stage.ToLower() == "closed-lost")
            };
        }

        [HttpPost("ticket-breakdown")]
        public async Task<ActionResult<TicketBreakdownData>> GetTicketBreakdown([FromBody] DateRangeFilter range)
        {
            return Ok(await QueryTicketBreakdown(range));
        }

        private async Task<TicketBreakdownData> QueryTicketBreakdown(DateRangeFilter range)
        {
            var tickets = await GetTicketsQuery().Where(t => t.CreatedAt >= range.StartDate && t.CreatedAt <= range.EndDate).ToListAsync();
            return new TicketBreakdownData
            {
                OpenCount = tickets.Count(t => t.Status.ToLower() == "open"),
                InProgressCount = tickets.Count(t => t.Status.ToLower() == "in progress"),
                ResolvedCount = tickets.Count(t => t.Status.ToLower() == "resolved"),
                OverdueCount = tickets.Count(t => t.DueDate < DateTime.UtcNow && t.Status.ToLower() != "resolved" && t.Status.ToLower() != "closed")
            };
        }

        [HttpPost("top-agents")]
        public async Task<ActionResult<List<AgentPerformance>>> GetTopAgents([FromBody] DateRangeFilter range)
        {
            if (ApiSecurityHelper.IsAgent(User) && !ApiSecurityHelper.HasFullOversight(User))
                return Forbid();

            return Ok(await QueryTopAgents(range));
        }

        private async Task<List<AgentPerformance>> QueryTopAgents(DateRangeFilter range)
        {
            var deals = await GetDealsQuery()
                .Include(d => d.Agent)
                .Where(d => (d.Stage.ToLower() == "closed" || d.Stage.ToLower() == "closed-won" || d.Stage.ToLower() == "won") &&
                            (d.ContractSignedDate ?? d.CreatedAt) >= range.StartDate && (d.ContractSignedDate ?? d.CreatedAt) <= range.EndDate)
                .ToListAsync();

            return deals.GroupBy(d => d.Agent)
                .Select(g => new AgentPerformance
                {
                    AgentName = g.Key != null ? g.Key.FullName : "Unknown",
                    DealsClosed = g.Count(),
                    TotalValue = g.Sum(d => d.Value),
                    CommissionEarned = g.Sum(d => d.Value * (d.CommissionRate > 1m ? d.CommissionRate / 100m : d.CommissionRate))
                })
                .OrderByDescending(x => x.TotalValue).ThenByDescending(x => x.DealsClosed)
                .Take(10)
                .ToList();
        }

        [HttpPost("lead-sources")]
        public async Task<ActionResult<List<SourceMetric>>> GetLeadSourceBreakdown([FromBody] DateRangeFilter range)
        {
            if (ApiSecurityHelper.IsAgent(User) && !ApiSecurityHelper.HasFullOversight(User))
                return Forbid();

            return Ok(await QueryLeadSourceBreakdown(range));
        }

        private async Task<List<SourceMetric>> QueryLeadSourceBreakdown(DateRangeFilter range)
        {
            var leads = await GetLeadsQuery().Where(l => l.CreatedAt >= range.StartDate && l.CreatedAt <= range.EndDate).ToListAsync();
            return leads.GroupBy(l => string.IsNullOrEmpty(l.Source) ? "Unknown" : l.Source)
                .Select(g => new SourceMetric
                {
                    Source = g.Key,
                    Count = g.Count()
                })
                .OrderByDescending(x => x.Count)
                .ToList();
        }

        [HttpPost("property-distribution")]
        public async Task<ActionResult<List<PropertyDistributionMetric>>> GetPropertyDistribution()
        {
            return Ok(await QueryPropertyDistribution());
        }

        private async Task<List<PropertyDistributionMetric>> QueryPropertyDistribution()
        {
            var props = await GetPropertiesQuery().ToListAsync();
            return props.GroupBy(p => string.IsNullOrWhiteSpace(p.PropertyType) ? "General" : p.PropertyType)
                .Select(g => new PropertyDistributionMetric
                {
                    PropertyType = char.ToUpper(g.Key[0]) + (g.Key.Length > 1 ? g.Key[1..] : ""),
                    Count = g.Count(),
                    TotalValue = g.Sum(p => p.Price)
                })
                .OrderByDescending(x => x.Count)
                .ToList();
        }

        [HttpGet("recent-activity")]
        public async Task<ActionResult<List<ActivityFeedItem>>> GetRecentActivityFeed([FromQuery] int count = 15)
        {
            return Ok(await QueryRecentActivity(count));
        }

        private async Task<List<ActivityFeedItem>> QueryRecentActivity(int count)
        {
            var deals = await GetDealsQuery()
                .Include(d => d.Customer)
                .Where(d => d.Stage.ToLower() == "closed" || d.Stage.ToLower() == "closed-won" || d.Stage.ToLower() == "won")
                .OrderByDescending(d => d.ContractSignedDate ?? d.CreatedAt)
                .Take(count)
                .ToListAsync();

            var tickets = await GetTicketsQuery()
                .Where(t => t.Status.ToLower() == "resolved" && t.ResolvedAt != null)
                .OrderByDescending(t => t.ResolvedAt)
                .Take(count)
                .ToListAsync();

            var leads = await GetLeadsQuery()
                .Where(l => l.Stage.ToLower() == "converted")
                .OrderByDescending(l => l.CreatedAt)
                .Take(count)
                .ToListAsync();

            var feed = new List<ActivityFeedItem>();
            foreach (var deal in deals)
            {
                var customerName = deal.Customer != null ? deal.Customer.FullName : "Unknown Customer";
                feed.Add(new ActivityFeedItem
                {
                    Icon = "💼",
                    Description = $"Deal closed: {customerName} — {deal.Value:C}",
                    Timestamp = deal.ContractSignedDate ?? deal.CreatedAt
                });
            }

            foreach (var ticket in tickets)
            {
                feed.Add(new ActivityFeedItem
                {
                    Icon = "🎟",
                    Description = $"Ticket resolved: {ticket.TicketNumber}",
                    Timestamp = ticket.ResolvedAt ?? ticket.CreatedAt
                });
            }

            foreach (var lead in leads)
            {
                feed.Add(new ActivityFeedItem
                {
                    Icon = "◎",
                    Description = $"Lead converted: {lead.FullName}",
                    Timestamp = lead.CreatedAt
                });
            }

            return feed.OrderByDescending(f => f.Timestamp).Take(count).ToList();
        }

        [HttpPost("drilldown")]
        public async Task<ActionResult<List<AnalyticsDetailRow>>> GetDrillDownRows([FromBody] DateRangeFilter range)
        {
            var list = new List<AnalyticsDetailRow>();
            var startDate = range.StartDate;
            var endDate = range.EndDate;

            var deals = await GetDealsQuery()
                .Include(d => d.Agent)
                .Include(d => d.Customer)
                .Include(d => d.Property)
                .Where(d => (d.ContractSignedDate ?? d.CreatedAt) >= startDate && (d.ContractSignedDate ?? d.CreatedAt) <= endDate)
                .ToListAsync();

            foreach (var d in deals)
            {
                string st = d.Stage;
                if (st.Equals("closed", StringComparison.OrdinalIgnoreCase) || st.Equals("closed-won", StringComparison.OrdinalIgnoreCase))
                    st = "Won";

                string dealTitle = d.Property?.Address ?? (d.Customer?.FullName ?? $"Deal #{d.DealId}");
                list.Add(new AnalyticsDetailRow
                {
                    RecordType = "Deal",
                    Reference = $"DL-{d.DealId:D4}",
                    Title = dealTitle,
                    Status = st,
                    AssignedTo = d.Agent?.FullName ?? "Unassigned",
                    Value = d.Value,
                    Date = d.ContractSignedDate ?? d.CreatedAt,
                    Details = $"Rate: {d.CommissionRate:F1}%"
                });
            }

            var leads = await GetLeadsQuery()
                .Include(l => l.AssignedAgent)
                .Where(l => l.CreatedAt >= startDate && l.CreatedAt <= endDate)
                .ToListAsync();

            foreach (var l in leads)
            {
                list.Add(new AnalyticsDetailRow
                {
                    RecordType = "Lead",
                    Reference = $"LD-{l.LeadId:D4}",
                    Title = l.FullName,
                    Status = l.Stage,
                    AssignedTo = l.AssignedAgent?.FullName ?? "Unassigned",
                    Value = l.ExpectedValue ?? 0m,
                    Date = l.CreatedAt,
                    Details = string.IsNullOrWhiteSpace(l.Source) ? "Direct" : l.Source
                });
            }

            var tickets = await GetTicketsQuery()
                .Include(t => t.AssignedToUser)
                .Include(t => t.Customer)
                .Where(t => t.CreatedAt >= startDate && t.CreatedAt <= endDate)
                .ToListAsync();

            foreach (var t in tickets)
            {
                string ticketTitle = !string.IsNullOrWhiteSpace(t.Category)
                    ? (t.Customer != null ? $"{t.Category} ({t.Customer.FullName})" : t.Category)
                    : (t.Description.Length > 40 ? t.Description[..40] + "..." : t.Description);

                list.Add(new AnalyticsDetailRow
                {
                    RecordType = "Ticket",
                    Reference = string.IsNullOrWhiteSpace(t.TicketNumber) ? $"TK-{t.TicketId:D4}" : t.TicketNumber,
                    Title = ticketTitle,
                    Status = t.Status,
                    AssignedTo = t.AssignedToUser?.FullName ?? "Unassigned",
                    Value = 0,
                    Date = t.CreatedAt,
                    Details = $"Priority: {t.Priority}"
                });
            }

            return Ok(list.OrderByDescending(r => r.Date).ToList());
        }
    }
}
