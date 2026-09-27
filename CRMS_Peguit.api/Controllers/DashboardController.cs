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
    public class DashboardController : ControllerBase
    {
        private readonly RealEstateDbContext _db;

        public DashboardController(RealEstateDbContext db)
        {
            _db = db;
        }

        [HttpGet("agent")]
        public async Task<ActionResult<AgentDashboardDto>> GetAgentSnapshot([FromQuery] int? userId = null)
        {
            int callerId = ApiSecurityHelper.GetUserId(User);
            int targetUserId = (userId.HasValue && userId.Value > 0 && ApiSecurityHelper.HasFullOversight(User))
                ? userId.Value
                : callerId;

            var userEntity = await _db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.UserId == targetUserId);
            string fullName = userEntity?.FullName ?? "Agent";
            string firstName = GetFirstName(fullName);
            string todayText = DateTime.Now.ToString("dddd, MMMM d, yyyy");

            int activeLeads = await _db.Leads.AsNoTracking()
                .CountAsync(l => l.AssignedAgentId == targetUserId && l.Stage.ToLower() != "converted" && l.Stage.ToLower() != "lost");

            int openDeals = await _db.Deals.AsNoTracking()
                .CountAsync(d => d.AgentId == targetUserId && d.Stage.ToLower() != "closed" && d.Stage.ToLower() != "closed-won" && d.Stage.ToLower() != "won" && d.Stage.ToLower() != "lost" && d.Stage.ToLower() != "closed-lost");

            var today = DateTime.UtcNow.Date;
            var tomorrow = today.AddDays(1);
            int followUpsTodayCount = await _db.TaskReminders.AsNoTracking()
                .CountAsync(r => r.AssignedToUserId == targetUserId && r.Status != "Completed" && r.DueDate >= today && r.DueDate < tomorrow);

            int openTickets = await _db.SupportTickets.AsNoTracking()
                .CountAsync(t => t.AssignedToUserId == targetUserId && t.Status.ToLower() != "resolved" && t.Status.ToLower() != "closed");

            var sparkline = new List<double>();
            var thirtyDaysAgo = DateTime.UtcNow.Date.AddDays(-29);
            var closedDeals = await _db.Deals.AsNoTracking()
                .Where(d => d.AgentId == targetUserId && (d.Stage == "Closed" || d.Stage == "Closed-Won" || d.Stage == "Won") &&
                            ((d.ContractSignedDate != null && d.ContractSignedDate >= thirtyDaysAgo) || d.CreatedAt >= thirtyDaysAgo))
                .ToListAsync();

            for (int i = 0; i < 30; i++)
            {
                var date = thirtyDaysAgo.AddDays(i);
                int count = closedDeals.Count(d => (d.ContractSignedDate?.Date ?? d.CreatedAt.Date) == date);
                sparkline.Add((double)count);
            }

            var rawFollowUps = await _db.TaskReminders.AsNoTracking()
                .Include(r => r.RelatedCustomer)
                .Include(r => r.RelatedLead)
                .Where(r => r.AssignedToUserId == targetUserId && r.Status != "Completed" && r.DueDate >= today && r.DueDate < tomorrow)
                .OrderBy(r => r.DueDate)
                .Take(5)
                .ToListAsync();

            var followUpsToday = rawFollowUps.Select(r => new AgentFollowUpItemDto
            {
                TaskReminderId = r.TaskReminderId,
                Title = r.Title,
                DueTimeText = r.DueDate.ToLocalTime().ToString("h:mm tt"),
                Priority = r.Priority,
                Type = r.Type,
                RelatedName = r.RelatedCustomer?.FullName ?? r.RelatedLead?.FullName ?? string.Empty,
                Status = r.Status == "Overdue" || r.DueDate < DateTime.UtcNow ? "Overdue" : (r.DueDate <= DateTime.UtcNow.AddHours(2) ? "Pending" : "Active"),
                IsOverdue = r.Status == "Overdue" || r.DueDate < DateTime.UtcNow
            }).ToList();

            var rawActivities = await _db.Activities.AsNoTracking()
                .Include(a => a.RelatedCustomer)
                .Include(a => a.RelatedLead)
                .Where(a => a.LoggedByAgentId == targetUserId)
                .OrderByDescending(a => a.ActivityDate)
                .Take(5)
                .ToListAsync();

            var recentActivities = rawActivities.Select(a => new AgentActivityItemDto
            {
                ActivityId = a.ActivityId,
                Type = a.Type ?? "Activity",
                RelatedName = a.RelatedCustomer?.FullName ?? a.RelatedLead?.FullName ?? (string.IsNullOrWhiteSpace(a.Notes) ? "Client Contact" : a.Notes),
                Notes = a.Notes ?? string.Empty,
                Status = "Completed",
                TimeAgo = FormatTimeAgo(a.ActivityDate),
                Date = a.ActivityDate
            }).ToList();

            return Ok(new AgentDashboardDto
            {
                Greeting = $"Welcome back, {firstName}",
                DateText = todayText,
                ActiveLeadsCount = activeLeads,
                OpenDealsCount = openDeals,
                FollowUpsDueTodayCount = followUpsTodayCount,
                OpenSupportTicketsCount = openTickets,
                SparklineDealsClosed = sparkline,
                FollowUpsToday = followUpsToday,
                RecentActivities = recentActivities
            });
        }

        [HttpGet("manager")]
        public async Task<ActionResult<ManagerDashboardDto>> GetManagerSnapshot()
        {
            if (!ApiSecurityHelper.HasFullOversight(User))
                return Forbid();

            int callerId = ApiSecurityHelper.GetUserId(User);
            var userEntity = await _db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.UserId == callerId);
            string fullName = userEntity?.FullName ?? "Manager";
            string firstName = GetFirstName(fullName);
            string todayText = DateTime.Now.ToString("dddd, MMMM d, yyyy");

            var startOfMonth = new DateTime(DateTime.UtcNow.Year, DateTime.UtcNow.Month, 1);

            int teamDeals = await _db.Deals.AsNoTracking()
                .CountAsync(d => (d.Stage == "Closed" || d.Stage == "Closed-Won" || d.Stage == "Won") &&
                                 ((d.ContractSignedDate != null && d.ContractSignedDate >= startOfMonth) || d.CreatedAt >= startOfMonth));

            int teamTickets = await _db.SupportTickets.AsNoTracking()
                .CountAsync(t => t.Status.ToLower() != "resolved" && t.Status.ToLower() != "closed");

            int pendingCust = await _db.Customers.AsNoTracking().CountAsync(c => c.AssignedAgentId == null && !c.IsDeleted);
            int pendingLeads = await _db.Leads.AsNoTracking().CountAsync(l => l.AssignedAgentId == null && !l.IsDeleted);
            int totalPending = pendingCust + pendingLeads;

            int totalLeadsCount = await _db.Leads.AsNoTracking().CountAsync(l => !l.IsDeleted);
            int convertedLeadsCount = await _db.Leads.AsNoTracking().CountAsync(l => l.ConvertedCustomerId.HasValue && !l.IsDeleted);
            double conversionRate = totalLeadsCount == 0 ? 0.0 : Math.Round(((double)convertedLeadsCount / totalLeadsCount) * 100, 1);

            int dealsWon = teamDeals;
            int dealsLost = await _db.Deals.AsNoTracking()
                .CountAsync(d => (d.Stage == "Lost" || d.Stage == "Closed-Lost") && d.CreatedAt >= startOfMonth);

            var pendingApprovals = new List<PendingAssignmentItemDto>();
            var unassignedCustomers = await _db.Customers.AsNoTracking()
                .Where(c => c.AssignedAgentId == null && !c.IsDeleted)
                .OrderByDescending(c => c.CreatedAt)
                .Take(5)
                .ToListAsync();

            foreach (var c in unassignedCustomers)
            {
                pendingApprovals.Add(new PendingAssignmentItemDto
                {
                    Id = c.CustomerId,
                    Type = "Customer",
                    Name = c.FullName,
                    SubmitterName = "System",
                    Status = "Pending",
                    CreatedAt = c.CreatedAt,
                    TimeAgo = FormatTimeAgo(c.CreatedAt),
                    AssignedAgentId = null,
                    OriginalEntity = c
                });
            }

            var unassignedLeads = await _db.Leads.AsNoTracking()
                .Where(l => l.AssignedAgentId == null && !l.IsDeleted)
                .OrderByDescending(l => l.CreatedAt)
                .Take(5)
                .ToListAsync();

            foreach (var l in unassignedLeads)
            {
                pendingApprovals.Add(new PendingAssignmentItemDto
                {
                    Id = l.LeadId,
                    Type = "Lead",
                    Name = l.FullName,
                    SubmitterName = "System",
                    Status = "Pending",
                    CreatedAt = l.CreatedAt,
                    TimeAgo = FormatTimeAgo(l.CreatedAt),
                    AssignedAgentId = null,
                    OriginalEntity = l
                });
            }

            pendingApprovals = pendingApprovals.OrderByDescending(x => x.CreatedAt).Take(5).ToList();

            var recentClosedDeals = await _db.Deals.AsNoTracking()
                .Include(d => d.Agent)
                .Where(d => d.Stage == "Closed" || d.Stage == "Closed-Won" || d.Stage == "Won" || d.Stage == "Lost" || d.Stage == "Closed-Lost")
                .OrderByDescending(d => d.ContractSignedDate ?? d.CreatedAt)
                .Take(5)
                .ToListAsync();

            var recentResolvedTickets = await _db.SupportTickets.AsNoTracking()
                .Include(t => t.AssignedToUser)
                .Where(t => t.Status == "Resolved" || t.Status == "Closed")
                .OrderByDescending(t => t.ResolvedAt ?? t.CreatedAt)
                .Take(5)
                .ToListAsync();

            var teamEvents = new List<TeamActivityItemDto>();
            foreach (var d in recentClosedDeals)
            {
                bool isWon = d.Stage != "Lost" && d.Stage != "Closed-Lost";
                teamEvents.Add(new TeamActivityItemDto
                {
                    AgentName = d.Agent?.FullName ?? "Agent",
                    ActionTitle = $"Deal #{d.DealId}",
                    Description = $"Value: {d.Value:C}",
                    Outcome = isWon ? "Won" : "Lost",
                    Icon = "💼",
                    Timestamp = d.ContractSignedDate ?? d.CreatedAt,
                    TimeAgo = FormatTimeAgo(d.ContractSignedDate ?? d.CreatedAt)
                });
            }

            foreach (var t in recentResolvedTickets)
            {
                teamEvents.Add(new TeamActivityItemDto
                {
                    AgentName = t.AssignedToUser?.FullName ?? "Support Staff",
                    ActionTitle = $"Ticket #{t.TicketNumber}",
                    Description = t.Description ?? "Ticket issue resolved",
                    Outcome = "Resolved",
                    Icon = "🎟",
                    Timestamp = t.ResolvedAt ?? t.CreatedAt,
                    TimeAgo = FormatTimeAgo(t.ResolvedAt ?? t.CreatedAt)
                });
            }

            var feed = teamEvents.OrderByDescending(e => e.Timestamp).Take(5).ToList();

            return Ok(new ManagerDashboardDto
            {
                Greeting = $"Welcome back, {firstName}",
                DateText = todayText,
                TeamDealsThisMonthCount = teamDeals,
                TeamOpenTicketsCount = teamTickets,
                PendingAssignmentsCount = totalPending,
                TeamConversionRate = conversionRate,
                DealsWonThisMonthCount = dealsWon,
                DealsLostThisMonthCount = dealsLost,
                PendingAssignments = pendingApprovals,
                TeamRecentActivity = feed
            });
        }

        [HttpGet("admin")]
        public async Task<ActionResult<AdminDashboardDto>> GetAdminSnapshot()
        {
            if (!ApiSecurityHelper.IsAdmin(User))
                return Forbid();

            int callerId = ApiSecurityHelper.GetUserId(User);
            var userEntity = await _db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.UserId == callerId);
            string fullName = userEntity?.FullName ?? "Administrator";
            string firstName = GetFirstName(fullName);
            string todayText = DateTime.Now.ToString("dddd, MMMM d, yyyy");

            int totalActiveUsers = await _db.Users.AsNoTracking().CountAsync(u => u.Status == "Active");
            int managersCount = await _db.Users.AsNoTracking().Include(u => u.Role).CountAsync(u => u.Status == "Active" && u.Role.RoleName == "Manager");
            int agentsCount = await _db.Users.AsNoTracking().Include(u => u.Role).CountAsync(u => u.Status == "Active" && u.Role.RoleName == "Agent");
            string activeUsersSub = $"{managersCount} Manager{(managersCount == 1 ? "" : "s")} · {agentsCount} Agent{(agentsCount == 1 ? "" : "s")}";

            int openTickets = await _db.SupportTickets.AsNoTracking().CountAsync(t => t.Status == "Open");
            int inProgressTickets = await _db.SupportTickets.AsNoTracking().CountAsync(t => t.Status == "In Progress");
            int resolvedTickets = await _db.SupportTickets.AsNoTracking().CountAsync(t => t.Status == "Resolved");
            int overdueTickets = await _db.SupportTickets.AsNoTracking().CountAsync(t => t.DueDate < DateTime.UtcNow && t.Status != "Resolved" && t.Status != "Closed");
            string openTicketsSub = overdueTickets > 0 ? $"{overdueTickets} overdue" : string.Empty;

            var startOfMonth = new DateTime(DateTime.UtcNow.Year, DateTime.UtcNow.Month, 1);
            var closedDeals = await _db.Deals.AsNoTracking()
                .Where(d => (d.Stage == "Closed" || d.Stage == "Closed-Won" || d.Stage == "Won") &&
                            ((d.ContractSignedDate != null && d.ContractSignedDate >= startOfMonth) || d.CreatedAt >= startOfMonth))
                .ToListAsync();

            int dealsClosed = closedDeals.Count;
            decimal commEarned = closedDeals.Sum(d => d.Value * (d.CommissionRate > 1m ? d.CommissionRate / 100m : d.CommissionRate));
            string dealsClosedSub = $"{commEarned:C} commission earned";

            var (subStatus, subExpiry) = await GetSubscriptionDetailsAsync();

            var teamRoster = await _db.Users.AsNoTracking()
                .Include(u => u.Role)
                .Where(u => u.Status == "Active" && (u.Role.RoleName == "Manager" || u.Role.RoleName == "Agent"))
                .OrderBy(u => u.Role.RoleName).ThenBy(u => u.FullName)
                .Take(8)
                .Select(u => new AdminTeamRosterItemDto
                {
                    UserId = u.UserId,
                    FullName = u.FullName,
                    RoleName = u.Role.RoleName,
                    Email = u.Email,
                    Status = u.Status,
                    DealsCount = _db.Deals.Count(d => d.AgentId == u.UserId && (d.Stage == "Closed" || d.Stage == "Closed-Won" || d.Stage == "Won"))
                })
                .ToListAsync();

            var commTrend = new List<AdminCommissionTrendPointDto>();
            for (int i = 5; i >= 0; i--)
            {
                var monthDate = DateTime.UtcNow.AddMonths(-i);
                var mStart = new DateTime(monthDate.Year, monthDate.Month, 1);
                var mEnd = mStart.AddMonths(1);

                var mDeals = await _db.Deals.AsNoTracking()
                    .Where(d => (d.Stage == "Closed" || d.Stage == "Closed-Won" || d.Stage == "Won") &&
                                (d.ContractSignedDate ?? d.CreatedAt) >= mStart && (d.ContractSignedDate ?? d.CreatedAt) < mEnd)
                    .ToListAsync();

                decimal mComm = mDeals.Sum(d => d.Value * (d.CommissionRate > 1m ? d.CommissionRate / 100m : d.CommissionRate));
                commTrend.Add(new AdminCommissionTrendPointDto
                {
                    MonthLabel = mStart.ToString("MMM yyyy"),
                    CommissionAmount = mComm
                });
            }

            var ticketsAttention = await _db.SupportTickets.AsNoTracking()
                .Include(t => t.Customer)
                .Where(t => t.Status == "Open" || t.Status == "In Progress")
                .OrderBy(t => t.CreatedAt)
                .Take(3)
                .Select(t => new AdminTicketAttentionItemDto
                {
                    TicketId = t.TicketId,
                    TicketNumber = t.TicketNumber,
                    Category = t.Category,
                    Priority = t.Priority,
                    CustomerName = t.Customer != null ? t.Customer.FullName : "Customer",
                    DaysOpen = Math.Max(0, (int)(DateTime.UtcNow - t.CreatedAt).TotalDays)
                })
                .ToListAsync();

            var systemLogs = new List<SystemActivityItemDto>();
            try
            {
                var backups = await _db.BackupLogs.AsNoTracking()
                    .Include(b => b.PerformedByUser)
                    .OrderByDescending(b => b.BackupDate)
                    .Take(5)
                    .ToListAsync();

                foreach (var b in backups)
                {
                    string user = b.PerformedByUser?.FullName ?? "System";
                    systemLogs.Add(new SystemActivityItemDto
                    {
                        Title = "Database Backup",
                        Details = $"Status: {b.Status} · Executed by {user}",
                        Status = b.Status,
                        Timestamp = b.BackupDate,
                        TimeAgo = FormatTimeAgo(b.BackupDate),
                        Icon = "💾",
                        UseAvatar = false
                    });
                }
            }
            catch { }

            return Ok(new AdminDashboardDto
            {
                Greeting = $"Welcome back, {firstName}",
                DateText = todayText,
                TotalActiveUsersCount = totalActiveUsers,
                ActiveUsersSubtext = activeUsersSub,
                OpenTicketsCount = openTickets,
                OverdueTicketsCount = overdueTickets,
                OpenTicketsSubtext = openTicketsSub,
                DealsClosedThisMonthCount = dealsClosed,
                CommissionEarnedThisMonth = commEarned,
                DealsClosedSubtext = dealsClosedSub,
                SubscriptionStatus = subStatus,
                SubscriptionExpiryText = subExpiry,
                OpenTicketsBreakdown = openTickets,
                InProgressTicketsBreakdown = inProgressTickets,
                ResolvedTicketsBreakdown = resolvedTickets,
                TeamRoster = teamRoster,
                CommissionTrendLast6Months = commTrend,
                TicketsNeedingAttention = ticketsAttention,
                RecentSystemActivities = systemLogs
            });
        }

        [HttpGet("superadmin")]
        public async Task<ActionResult<SuperAdminDashboardDto>> GetSuperAdminSnapshot()
        {
            string firstName = "Super Admin";
            string todayText = DateTime.Now.ToString("dddd, MMMM d, yyyy");

            string backupStatus = "Active";
            string backupTime = "Up to date";
            try
            {
                var lastBackup = await _db.BackupLogs.AsNoTracking().OrderByDescending(b => b.BackupDate).FirstOrDefaultAsync();
                if (lastBackup != null && !string.IsNullOrWhiteSpace(lastBackup.Status))
                {
                    backupStatus = lastBackup.Status;
                    backupTime = FormatTimeAgo(lastBackup.BackupDate);
                }
            }
            catch { }

            var platformEvents = new List<PlatformActivityItemDto>();
            try
            {
                var backups = await _db.BackupLogs.AsNoTracking().Include(b => b.PerformedByUser).OrderByDescending(b => b.BackupDate).Take(3).ToListAsync();
                foreach (var b in backups)
                {
                    platformEvents.Add(new PlatformActivityItemDto
                    {
                        Title = "Platform Database Backup",
                        Details = $"Status: {b.Status} · Executed by {b.PerformedByUser?.FullName ?? "System"}",
                        Status = b.Status,
                        Timestamp = b.BackupDate,
                        TimeAgo = FormatTimeAgo(b.BackupDate),
                        Icon = "💾"
                    });
                }
            }
            catch { }

            return Ok(new SuperAdminDashboardDto
            {
                Greeting = $"Welcome back, {firstName}",
                DateText = todayText,
                TotalTenantsCount = 1,
                ActiveSubscriptionsCount = 1,
                SubscriptionsExpiringThisMonthCount = 0,
                ExpiredSubscriptionsCount = 0,
                LastBackupStatus = backupStatus,
                LastBackupTimeText = backupTime,
                RecentPlatformActivities = platformEvents
            });
        }

        private static string GetFirstName(string? fullName)
        {
            if (string.IsNullOrWhiteSpace(fullName)) return "User";
            var parts = fullName.Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries);
            return parts.Length > 0 ? parts[0] : fullName.Trim();
        }

        private static string FormatTimeAgo(DateTime utcTime)
        {
            var span = DateTime.UtcNow - utcTime;
            if (span.TotalMinutes < 1) return "Just now";
            if (span.TotalMinutes < 60) return $"{(int)span.TotalMinutes}m ago";
            if (span.TotalHours < 24) return $"{(int)span.TotalHours}h ago";
            if (span.TotalDays < 7) return $"{(int)span.TotalDays}d ago";
            return utcTime.ToLocalTime().ToString("MMM d");
        }

        private async Task<(string status, string expiry)> GetSubscriptionDetailsAsync()
        {
            try
            {
                var settings = await _db.SystemSettings
                    .AsNoTracking()
                    .Where(s => s.SettingKey == "SubscriptionStatus" || s.SettingKey == "SubscriptionExpiry")
                    .ToDictionaryAsync(s => s.SettingKey, s => s.SettingValue);

                string status = settings.TryGetValue("SubscriptionStatus", out var sVal) && !string.IsNullOrWhiteSpace(sVal) ? sVal.Trim() : "Active";
                string expiry = settings.TryGetValue("SubscriptionExpiry", out var eVal) && !string.IsNullOrWhiteSpace(eVal) ? eVal.Trim() : "Expires Jun 19, 2027";
                return (status, expiry);
            }
            catch
            {
                return ("Active", "Expires Jun 19, 2027");
            }
        }
    }
}
