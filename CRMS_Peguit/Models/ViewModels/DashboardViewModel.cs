using System;
using System.Collections.Generic;

namespace CRMS_Peguit.winforms.Models.ViewModels
{
    // =========================================================================
    // AGENT DASHBOARD DTO
    // =========================================================================
    public class AgentDashboardDto
    {
        public string Greeting { get; set; } = string.Empty;
        public string DateText { get; set; } = string.Empty;

        // KPI Counts
        public int ActiveLeadsCount { get; set; }
        public int OpenDealsCount { get; set; }
        public int FollowUpsDueTodayCount { get; set; }
        public int OpenSupportTicketsCount { get; set; }

        // Glanceable Chart (Sparkline: daily closed deals last 30 days)
        public List<double> SparklineDealsClosed { get; set; } = new();

        // Short Lists (max 5 items)
        public List<AgentFollowUpItemDto> FollowUpsToday { get; set; } = new();
        public List<AgentActivityItemDto> RecentActivities { get; set; } = new();
    }

    public class AgentFollowUpItemDto
    {
        public int TaskReminderId { get; set; }
        public string Title { get; set; } = string.Empty;
        public string DueTimeText { get; set; } = string.Empty;
        public string Priority { get; set; } = "Medium";
        public string Type { get; set; } = "Call";
        public string RelatedName { get; set; } = string.Empty;
        public string Status { get; set; } = "On Track";
        public bool IsOverdue { get; set; }
    }

    public class AgentActivityItemDto
    {
        public int ActivityId { get; set; }
        public string Type { get; set; } = string.Empty;
        public string RelatedName { get; set; } = string.Empty;
        public string Notes { get; set; } = string.Empty;
        public string Status { get; set; } = "Completed";
        public string TimeAgo { get; set; } = string.Empty;
        public DateTime Date { get; set; }
    }

    // =========================================================================
    // MANAGER DASHBOARD DTO
    // =========================================================================
    public class ManagerDashboardDto
    {
        public string Greeting { get; set; } = string.Empty;
        public string DateText { get; set; } = string.Empty;

        // KPI Counts
        public int TeamDealsThisMonthCount { get; set; }
        public int TeamOpenTicketsCount { get; set; }
        public int PendingAssignmentsCount { get; set; }
        public double TeamConversionRate { get; set; }

        // Glanceable Chart (Donut: Deals Won vs. Lost this month)
        public int DealsWonThisMonthCount { get; set; }
        public int DealsLostThisMonthCount { get; set; }

        // Short Lists (max 5 items)
        public List<PendingAssignmentItemDto> PendingAssignments { get; set; } = new();
        public List<TeamActivityItemDto> TeamRecentActivity { get; set; } = new();
    }

    public class PendingAssignmentItemDto
    {
        public int Id { get; set; }
        public string Type { get; set; } = string.Empty; // "Lead" or "Customer"
        public string Name { get; set; } = string.Empty;
        public string SubmitterName { get; set; } = string.Empty;
        public string Status { get; set; } = "Pending";
        public DateTime CreatedAt { get; set; }
        public string TimeAgo { get; set; } = string.Empty;
        public int? AssignedAgentId { get; set; }
        public object? OriginalEntity { get; set; }
    }

    public class TeamActivityItemDto
    {
        public string AgentName { get; set; } = string.Empty;
        public string ActionTitle { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string Outcome { get; set; } = string.Empty;
        public string Icon { get; set; } = string.Empty;
        public DateTime Timestamp { get; set; }
        public string TimeAgo { get; set; } = string.Empty;
    }

    // =========================================================================
    // ADMIN DASHBOARD DTO
    // =========================================================================
    public class AdminDashboardDto
    {
        public string Greeting { get; set; } = string.Empty;
        public string DateText { get; set; } = string.Empty;

        // KPI Counts & Contextual Subtext
        public int TotalActiveUsersCount { get; set; }
        public string ActiveUsersSubtext { get; set; } = string.Empty;

        public int OpenTicketsCount { get; set; }
        public int OverdueTicketsCount { get; set; }
        public string OpenTicketsSubtext { get; set; } = string.Empty;

        public int DealsClosedThisMonthCount { get; set; }
        public decimal CommissionEarnedThisMonth { get; set; }
        public string DealsClosedSubtext { get; set; } = string.Empty;

        public string SubscriptionStatus { get; set; } = "Active";
        public string SubscriptionExpiryText { get; set; } = string.Empty;

        // Glanceable Chart 1 (Donut: Ticket Status Breakdown: Open / In Progress / Resolved)
        public int OpenTicketsBreakdown { get; set; }
        public int InProgressTicketsBreakdown { get; set; }
        public int ResolvedTicketsBreakdown { get; set; }

        // Glanceable Chart 2 (Bar/Sparkline: Commission Earned — Last 6 Months)
        public List<AdminCommissionTrendPointDto> CommissionTrendLast6Months { get; set; } = new();

        // Team Roster Snapshot (Short list: max 6–8 rows)
        public List<AdminTeamRosterItemDto> TeamRoster { get; set; } = new();

        // Tickets Needing Attention (Top 3 oldest still-open tickets)
        public List<AdminTicketAttentionItemDto> TicketsNeedingAttention { get; set; } = new();

        // Short Lists (real data only; empty/omitted if no logs exist)
        public List<SystemActivityItemDto> RecentSystemActivities { get; set; } = new();
    }

    public class AdminTeamRosterItemDto
    {
        public int UserId { get; set; }
        public string FullName { get; set; } = string.Empty;
        public string RoleName { get; set; } = string.Empty;
        public string Status { get; set; } = "Active";
    }

    public class AdminCommissionTrendPointDto
    {
        public string MonthLabel { get; set; } = string.Empty;
        public double CommissionAmount { get; set; }
    }

    public class AdminTicketAttentionItemDto
    {
        public int TicketId { get; set; }
        public string TicketNumber { get; set; } = string.Empty;
        public string CustomerName { get; set; } = string.Empty;
        public string Category { get; set; } = string.Empty;
        public string Priority { get; set; } = "Medium";
        public string Status { get; set; } = "Open";
        public string OpenedAgoText { get; set; } = string.Empty;
    }

    public class SystemActivityItemDto
    {
        public string Title { get; set; } = string.Empty;
        public string Details { get; set; } = string.Empty;
        public string Status { get; set; } = "Completed";
        public DateTime Timestamp { get; set; }
        public string TimeAgo { get; set; } = string.Empty;
        public string Icon { get; set; } = "⚙️";
        public bool UseAvatar { get; set; }
        public string AvatarName { get; set; } = string.Empty;
    }

    // =========================================================================
    // SUPER ADMIN DASHBOARD DTO
    // =========================================================================
    public class SuperAdminDashboardDto
    {
        public string Greeting { get; set; } = string.Empty;
        public string DateText { get; set; } = string.Empty;

        // KPI Counts
        public int TotalTenantsCount { get; set; }
        public int ActiveSubscriptionsCount { get; set; }
        public int SubscriptionsExpiringThisMonthCount { get; set; }
        public string LastBackupStatus { get; set; } = "Active";
        public string LastBackupTimeText { get; set; } = string.Empty;

        // Glanceable Chart (Bar: Subscriptions by Status: Active / Expiring / Expired)
        public int ExpiredSubscriptionsCount { get; set; }

        // Short Lists (max 5 items)
        public List<PlatformActivityItemDto> RecentPlatformActivities { get; set; } = new();
    }

    public class PlatformActivityItemDto
    {
        public string Title { get; set; } = string.Empty;
        public string Details { get; set; } = string.Empty;
        public string Status { get; set; } = "Active";
        public DateTime Timestamp { get; set; }
        public string TimeAgo { get; set; } = string.Empty;
        public string Icon { get; set; } = "🏢";
    }

    // =========================================================================
    // LEGACY VIEW MODELS (Retained for backward compatibility)
    // =========================================================================
    public class DashboardSummaryViewModel
    {
        public int TotalCustomers { get; set; }
        public int ActiveProperties { get; set; }
        public int QualifiedLeads { get; set; }
        public int TotalDeals { get; set; }
        public decimal PipelineValue { get; set; }
        public int TotalAgents { get; set; }
        public List<RecentLeadItemViewModel> RecentLeads { get; set; } = new();
    }

    public class RecentLeadItemViewModel
    {
        public string Lead { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Source { get; set; } = string.Empty;
        public string Value { get; set; } = string.Empty;
        public string Stage { get; set; } = string.Empty;
    }
}
