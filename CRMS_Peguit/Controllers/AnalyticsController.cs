using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using CRMS_Peguit.domain.entities;
using CRMS_Peguit.infrastructure.data;
using CRMS_Peguit.winforms.Auth;
using CRMS_Peguit.winforms.Models.Analytics;
using CRMS_Peguit.winforms.Models.Services;
using Microsoft.EntityFrameworkCore;

namespace CRMS_Peguit.winforms.Controllers
{
    public class AnalyticsController : IDisposable
    {
        private readonly RealEstateDbContext _db;
        private int TenantId => CurrentSession.TenantId;

        public AnalyticsController()
        {
            _db = LocalDb.CreateContext(TenantId);
        }

        public void Dispose()
        {
            _db.Dispose();
        }

        private IQueryable<Deal> GetDealsQuery()
        {
            var query = _db.Deals.AsNoTracking();
            if (RbacService.IsAgent && !RbacService.HasFullOversight)
            {
                query = query.Where(d => d.AgentId == CurrentSession.UserId);
            }
            return query;
        }

        private IQueryable<Lead> GetLeadsQuery()
        {
            var query = _db.Leads.AsNoTracking();
            if (RbacService.IsAgent && !RbacService.HasFullOversight)
            {
                query = query.Where(l => l.AssignedAgentId == CurrentSession.UserId);
            }
            return query;
        }

        private IQueryable<SupportTicket> GetTicketsQuery()
        {
            var query = _db.SupportTickets.AsNoTracking();
            if (RbacService.IsAgent && !RbacService.HasFullOversight)
            {
                query = query.Where(t => t.AssignedToUserId == CurrentSession.UserId);
            }
            return query;
        }

        public AnalyticsSnapshot GetSnapshot(DateRangeFilter range)
        {
            try
            {
                var dealsQuery = GetDealsQuery();
                var leadsQuery = GetLeadsQuery();
                var ticketsQuery = GetTicketsQuery();

                var startDate = range.StartDate;
                var endDate = range.EndDate;

                var closedDeals = dealsQuery.Where(d => (d.Stage.ToLower() == "closed" || d.Stage.ToLower() == "closed-won" || d.Stage.ToLower() == "won") &&
                                                         (d.ContractSignedDate ?? d.CreatedAt) >= startDate && (d.ContractSignedDate ?? d.CreatedAt) <= endDate).ToList();
                
                var totalDealsClosed = closedDeals.Count;
                var totalSalesVolume = closedDeals.Sum(d => d.Value);
                var totalCommissionEarned = closedDeals.Sum(d => d.Value * (d.CommissionRate > 1m ? d.CommissionRate / 100m : d.CommissionRate));
                var averageDealSize = totalDealsClosed == 0 ? 0 : totalSalesVolume / totalDealsClosed;

                // Active Pipeline: Deals currently open / in progress
                var openDeals = dealsQuery.Where(d => d.Stage.ToLower() != "closed" && d.Stage.ToLower() != "closed-won" && d.Stage.ToLower() != "won" && d.Stage.ToLower() != "lost").ToList();
                var activePipelineValue = openDeals.Sum(d => d.Value);

                var activeLeads = leadsQuery.Count(l => l.Stage.ToLower() != "converted" && l.Stage.ToLower() != "lost");
                
                var leadsInRange = leadsQuery.Where(l => l.CreatedAt >= startDate && l.CreatedAt <= endDate).ToList();
                var totalLeadsInRange = leadsInRange.Count;
                var convertedLeadsInRange = leadsInRange.Count(l => l.Stage.ToLower() == "converted");
                var leadConversionRate = totalLeadsInRange == 0 ? 0 : ((double)convertedLeadsInRange / totalLeadsInRange) * 100;

                // Win rate: Won / (Won + Lost)
                var lostDealsCount = dealsQuery.Count(d => d.Stage.ToLower() == "lost" && d.CreatedAt >= startDate && d.CreatedAt <= endDate);
                var winRate = (totalDealsClosed + lostDealsCount) == 0 ? 0 : ((double)totalDealsClosed / (totalDealsClosed + lostDealsCount)) * 100;

                var openSupportTickets = ticketsQuery.Count(t => t.Status.ToLower() != "resolved" && t.Status.ToLower() != "closed");

                var closedDealsWithDates = dealsQuery.Where(d => (d.Stage.ToLower() == "closed" || d.Stage.ToLower() == "closed-won" || d.Stage.ToLower() == "won") && d.ContractSignedDate != null).ToList();
                var averageDaysToClose = closedDealsWithDates.Any() ? closedDealsWithDates.Average(d => ((d.ContractSignedDate ?? d.CreatedAt) - d.CreatedAt).TotalDays) : 0;

                // Active property inventory
                var propQuery = _db.Properties.AsNoTracking();
                if (RbacService.IsAgent && !RbacService.HasFullOversight)
                {
                    propQuery = propQuery.Where(p => p.ListedByAgentId == CurrentSession.UserId);
                }
                var activeProps = propQuery.Where(p => p.Status.ToLower() != "sold" && p.Status.ToLower() != "off market").ToList();
                var activePropsCount = activeProps.Count;
                var activeInventoryValue = activeProps.Sum(p => p.Price);

                return new AnalyticsSnapshot
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
                    DealsOverTime = GetDealsOverTime(range),
                    LeadFunnel = GetLeadFunnel(range),
                    DealsWonVsLost = GetDealsWonVsLost(range),
                    TicketBreakdown = GetTicketBreakdown(range),
                    TopAgents = RbacService.IsAgent ? null : GetTopAgents(range),
                    LeadSourceBreakdown = RbacService.IsAgent ? null : GetLeadSourceBreakdown(range),
                    PropertyDistribution = GetPropertyDistribution(range),
                    RecentActivity = GetRecentActivityFeed(15)
                };
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error in GetSnapshot: {ex.Message}");
                return new AnalyticsSnapshot();
            }
        }

        public List<MonthlyMetric> GetDealsOverTime(DateRangeFilter range)
        {
            try
            {
                var startDate = range.StartDate;
                var endDate = range.EndDate;

                var deals = GetDealsQuery()
                    .Where(d => (d.Stage.ToLower() == "closed" || d.Stage.ToLower() == "closed-won" || d.Stage.ToLower() == "won") &&
                                (d.ContractSignedDate ?? d.CreatedAt) >= startDate && (d.ContractSignedDate ?? d.CreatedAt) <= endDate)
                    .ToList();

                var durationDays = (endDate - startDate).TotalDays;

                if (durationDays <= 31)
                {
                    // Daily/Weekly breakdown for short ranges
                    var groupedDaily = deals.GroupBy(d => (d.ContractSignedDate ?? d.CreatedAt).Date)
                        .OrderBy(g => g.Key)
                        .Select(g => new MonthlyMetric
                        {
                            Month = g.Key.ToString("MMM dd"),
                            Count = g.Count(),
                            TotalValue = g.Sum(d => d.Value)
                        }).ToList();

                    return groupedDaily;
                }
                else
                {
                    var grouped = deals.GroupBy(d => new { (d.ContractSignedDate ?? d.CreatedAt).Year, (d.ContractSignedDate ?? d.CreatedAt).Month })
                        .OrderBy(g => g.Key.Year).ThenBy(g => g.Key.Month)
                        .Select(g => new MonthlyMetric
                        {
                            Month = new DateTime(g.Key.Year, g.Key.Month, 1).ToString("MMM yyyy"),
                            Count = g.Count(),
                            TotalValue = g.Sum(d => d.Value)
                        }).ToList();

                    return grouped;
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error in GetDealsOverTime: {ex.Message}");
                return new List<MonthlyMetric>();
            }
        }

        public LeadFunnelData GetLeadFunnel(DateRangeFilter range)
        {
            try
            {
                var leads = GetLeadsQuery().Where(l => l.CreatedAt >= range.StartDate && l.CreatedAt <= range.EndDate).ToList();
                
                return new LeadFunnelData
                {
                    NewCount = leads.Count(l => l.Stage.ToLower() == "new"),
                    ContactedCount = leads.Count(l => l.Stage.ToLower() == "contacted"),
                    QualifiedCount = leads.Count(l => l.Stage.ToLower() == "qualified"),
                    ConvertedCount = leads.Count(l => l.Stage.ToLower() == "converted"),
                    LostCount = leads.Count(l => l.Stage.ToLower() == "lost")
                };
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error in GetLeadFunnel: {ex.Message}");
                return new LeadFunnelData();
            }
        }

        public WonLostData GetDealsWonVsLost(DateRangeFilter range)
        {
            try
            {
                var deals = GetDealsQuery().Where(d => d.CreatedAt >= range.StartDate && d.CreatedAt <= range.EndDate).ToList();
                
                return new WonLostData
                {
                    WonCount = deals.Count(d => d.Stage.ToLower() == "closed"),
                    LostCount = deals.Count(d => d.Stage.ToLower() == "lost")
                };
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error in GetDealsWonVsLost: {ex.Message}");
                return new WonLostData();
            }
        }

        public TicketBreakdownData GetTicketBreakdown(DateRangeFilter range)
        {
            try
            {
                var tickets = GetTicketsQuery().Where(t => t.CreatedAt >= range.StartDate && t.CreatedAt <= range.EndDate).ToList();
                
                return new TicketBreakdownData
                {
                    OpenCount = tickets.Count(t => t.Status.ToLower() == "open"),
                    InProgressCount = tickets.Count(t => t.Status.ToLower() == "in progress"),
                    ResolvedCount = tickets.Count(t => t.Status.ToLower() == "resolved"),
                    OverdueCount = tickets.Count(t => t.DueDate < DateTime.UtcNow && t.Status.ToLower() != "resolved" && t.Status.ToLower() != "closed")
                };
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error in GetTicketBreakdown: {ex.Message}");
                return new TicketBreakdownData();
            }
        }

        public List<AgentPerformance> GetTopAgents(DateRangeFilter range)
        {
            try
            {
                if (RbacService.IsAgent)
                {
                    throw new InvalidOperationException("Agent role cannot access team performance data.");
                }

                var deals = GetDealsQuery()
                    .Include(d => d.Agent)
                    .Where(d => (d.Stage.ToLower() == "closed" || d.Stage.ToLower() == "closed-won" || d.Stage.ToLower() == "won") &&
                                (d.ContractSignedDate ?? d.CreatedAt) >= range.StartDate && (d.ContractSignedDate ?? d.CreatedAt) <= range.EndDate)
                    .ToList();

                var grouped = deals.GroupBy(d => d.Agent)
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

                return grouped;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error in GetTopAgents: {ex.Message}");
                if (ex is InvalidOperationException) throw;
                return new List<AgentPerformance>();
            }
        }

        public List<SourceMetric> GetLeadSourceBreakdown(DateRangeFilter range)
        {
            try
            {
                if (RbacService.IsAgent)
                {
                    return new List<SourceMetric>();
                }

                var leads = GetLeadsQuery().Where(l => l.CreatedAt >= range.StartDate && l.CreatedAt <= range.EndDate).ToList();
                
                var grouped = leads.GroupBy(l => string.IsNullOrEmpty(l.Source) ? "Unknown" : l.Source)
                    .Select(g => new SourceMetric
                    {
                        Source = g.Key,
                        Count = g.Count()
                    })
                    .OrderByDescending(x => x.Count)
                    .ToList();

                return grouped;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error in GetLeadSourceBreakdown: {ex.Message}");
                return new List<SourceMetric>();
            }
        }

        public List<PropertyDistributionMetric> GetPropertyDistribution(DateRangeFilter range)
        {
            try
            {
                var propQuery = _db.Properties.AsNoTracking();
                if (RbacService.IsAgent && !RbacService.HasFullOversight)
                {
                    propQuery = propQuery.Where(p => p.ListedByAgentId == CurrentSession.UserId);
                }

                var props = propQuery.ToList();
                var grouped = props.GroupBy(p => string.IsNullOrWhiteSpace(p.PropertyType) ? "General" : p.PropertyType)
                    .Select(g => new PropertyDistributionMetric
                    {
                        PropertyType = char.ToUpper(g.Key[0]) + (g.Key.Length > 1 ? g.Key[1..] : ""),
                        Count = g.Count(),
                        TotalValue = g.Sum(p => p.Price)
                    })
                    .OrderByDescending(x => x.Count)
                    .ToList();

                return grouped;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error in GetPropertyDistribution: {ex.Message}");
                return new List<PropertyDistributionMetric>();
            }
        }

        public List<ActivityFeedItem> GetRecentActivityFeed(int count = 15)
        {
            try
            {
                var deals = GetDealsQuery()
                    .Include(d => d.Customer)
                    .Where(d => d.Stage.ToLower() == "closed")
                    .OrderByDescending(d => d.ContractSignedDate ?? d.CreatedAt)
                    .Take(count)
                    .ToList();

                var tickets = GetTicketsQuery()
                    .Where(t => t.Status.ToLower() == "resolved" && t.ResolvedAt != null)
                    .OrderByDescending(t => t.ResolvedAt)
                    .Take(count)
                    .ToList();

                var leads = GetLeadsQuery()
                    .Where(l => l.Stage.ToLower() == "converted")
                    .OrderByDescending(l => l.CreatedAt)
                    .Take(count)
                    .ToList();

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
            catch (Exception ex)
            {
                Debug.WriteLine($"Error in GetRecentActivityFeed: {ex.Message}");
                return new List<ActivityFeedItem>();
            }
        }

        public List<AnalyticsDetailRow> GetDrillDownRows(DateRangeFilter range)
        {
            var list = new List<AnalyticsDetailRow>();
            try
            {
                var startDate = range.StartDate;
                var endDate = range.EndDate;

                // 1. Deals
                var deals = GetDealsQuery()
                    .Include(d => d.Agent)
                    .Include(d => d.Customer).ThenInclude(c => c!.Person)
                    .Include(d => d.Property)
                    .Where(d => (d.ContractSignedDate ?? d.CreatedAt) >= startDate && (d.ContractSignedDate ?? d.CreatedAt) <= endDate)
                    .ToList();

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

                // 2. Leads
                var leads = GetLeadsQuery()
                    .Include(l => l.AssignedAgent)
                    .Include(l => l.Person)
                    .Where(l => l.CreatedAt >= startDate && l.CreatedAt <= endDate)
                    .ToList();

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

                // 3. Support Tickets
                var tickets = GetTicketsQuery()
                    .Include(t => t.AssignedToUser)
                    .Include(t => t.Customer).ThenInclude(c => c.Person)
                    .Where(t => t.CreatedAt >= startDate && t.CreatedAt <= endDate)
                    .ToList();

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
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error in GetDrillDownRows: {ex.Message}");
            }

            return list.OrderByDescending(r => r.Date).ToList();
        }
    }
}
