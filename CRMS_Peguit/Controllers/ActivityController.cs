using System;
using System.Collections.Generic;
using System.Linq;
using CRMS_Peguit.domain.entities;
using CRMS_Peguit.infrastructure.data;
using CRMS_Peguit.winforms.Auth;
using CRMS_Peguit.winforms.Models.Services;
using CRMS_Peguit.winforms.Services;
using CRMS_Peguit.winforms.Services.Offline;
using Microsoft.EntityFrameworkCore;

namespace CRMS_Peguit.winforms.Controllers
{
    public class TimelineItemDto
    {
        public string Id { get; set; } = string.Empty;
        public string Source { get; set; } = "Manual"; // "Manual" or "System"
        public string Type { get; set; } = "Call"; // Call, Email, Meeting, Showing, FollowUp, Deal, SupportTicket, Note
        public string Category { get; set; } = "Calls"; // Calls, Emails, Meetings, System Events
        public string Title { get; set; } = string.Empty;
        public string? Notes { get; set; }
        public DateTime Timestamp { get; set; }
        public string ActorName { get; set; } = "System";
        public CallOutcome? Outcome { get; set; }
        public int? DurationMinutes { get; set; }
        public int? RelatedCustomerId { get; set; }
        public int? RelatedLeadId { get; set; }
        public string ClientName { get; set; } = string.Empty;
        public string ClientType { get; set; } = "Customer";
        public int? RawActivityId { get; set; }
        public bool CanCreateFollowUp { get; set; } = true;
    }

    public class ActivityController : IDisposable
    {
        private readonly RealEstateDbContext _db;
        private int TenantId => CurrentSession.TenantId;

        /// <summary>
        /// Decision Flag: Per NEXA finalized Use Case diagram, only Agent has "Manage Activities & Follow Ups".
        /// By default, Managers do not have visibility into Agent call logs. If true, enables read-only coaching oversight.
        /// </summary>
        public static bool AllowManagerActivityVisibility { get; set; } = false;

        public ActivityController()
        {
            _db = LocalDb.CreateContext(TenantId);
        }

        /// <summary>
        /// Checks whether the current user is permitted to view the interaction timeline for this contact.
        /// </summary>
        public bool CanViewTimeline(int? assignedAgentId)
        {
            // SuperAdmin, Admin, and Manager have system/management oversight
            if (RbacService.HasFullOversight) return true;

            // Strict Agent boundary: Agent sees activities only for assigned customers/leads
            if (RbacService.IsAgent && assignedAgentId == CurrentSession.UserId) return true;

            return false;
        }

        /// <summary>
        /// Logs a new manual activity (Call, Email, Meeting, Note).
        /// Enforces contact ownership, outcome/duration rules, and immutable timestamps.
        /// </summary>
        public Activity LogActivity(Activity activity)
        {
            if (activity == null) throw new ArgumentNullException(nameof(activity));

            // Validate contact exclusivity (Customer XOR Lead)
            if (activity.RelatedCustomerId.HasValue == activity.RelatedLeadId.HasValue)
            {
                throw new InvalidOperationException("An activity must be linked to exactly one Customer or Lead.");
            }

            int currentUserId = CurrentSession.UserId;
            if (currentUserId <= 0)
            {
                throw new InvalidOperationException("An active user session is required to log activity.");
            }

            // Normalization & field validation
            activity.LoggedByAgentId = currentUserId;
            if (string.IsNullOrWhiteSpace(activity.Type)) activity.Type = "Call";

            if (activity.Type.Equals("Call", StringComparison.OrdinalIgnoreCase))
            {
                activity.Outcome ??= CallOutcome.Connected;
            }
            else
            {
                activity.Outcome = null; // Outcome is call-specific
            }

            if (!activity.Type.Equals("Call", StringComparison.OrdinalIgnoreCase) &&
                !activity.Type.Equals("Meeting", StringComparison.OrdinalIgnoreCase))
            {
                activity.DurationMinutes = null; // Duration is Call/Meeting specific
            }
            else if (activity.DurationMinutes.HasValue && activity.DurationMinutes.Value < 0)
            {
                activity.DurationMinutes = 0;
            }

            // Disallow future timestamps beyond a 5-minute clock drift margin
            if (activity.ActivityDate > DateTime.UtcNow.AddMinutes(5))
            {
                activity.ActivityDate = DateTime.UtcNow;
            }

            // Verify assignment to current agent (unless SuperAdmin)
            if (!RbacService.IsSuperAdmin)
            {
                if (activity.RelatedCustomerId.HasValue)
                {
                    var customer = _db.Customers.AsNoTracking().FirstOrDefault(c => c.CustomerId == activity.RelatedCustomerId.Value);
                    if (customer == null || customer.AssignedAgentId != currentUserId)
                    {
                        throw new UnauthorizedAccessException("You can only log activities for customers assigned to you.");
                    }
                }
                else if (activity.RelatedLeadId.HasValue)
                {
                    var lead = _db.Leads.AsNoTracking().FirstOrDefault(l => l.LeadId == activity.RelatedLeadId.Value);
                    if (lead == null || lead.AssignedAgentId != currentUserId)
                    {
                        throw new UnauthorizedAccessException("You can only log activities for leads assigned to you.");
                    }
                }
            }

            _db.Activities.Add(activity);
            _db.SaveChanges();
            try { LocalDataCache.Instance.SaveActivitiesMirror(TenantId, new[] { activity }); } catch { }

            SyncService.Instance.EnqueueOfflineCreate("Activity", activity, TenantId, currentUserId, activity.ActivityId.ToString());
            if (SyncService.Instance.IsOnline && !CurrentSession.IsOffline)
            {
                _ = Task.Run(() => SyncService.Instance.SyncAsync());
            }

            // Auto-advance lead from "new" to "contacted" on interaction
            if (activity.RelatedLeadId.HasValue)
            {
                try
                {
                    var lead = _db.Leads.FirstOrDefault(l => l.LeadId == activity.RelatedLeadId.Value);
                    if (lead != null && string.Equals(lead.Stage, "new", StringComparison.OrdinalIgnoreCase))
                    {
                        lead.Stage = "contacted";
                        _db.SaveChanges();
                        SyncService.Instance.EnqueueOfflineUpdate("Lead", lead.LeadId, lead, TenantId, currentUserId, lead.CreatedAt);
                        if (SyncService.Instance.IsOnline && !CurrentSession.IsOffline)
                        {
                            _ = Task.Run(() => SyncService.Instance.SyncAsync());
                        }
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"[ActivityController] Error advancing lead stage: {ex.Message}");
                }
            }

            if (activity.RelatedCustomerId.HasValue)
            {
                int custId = activity.RelatedCustomerId.Value;
                int tenant = TenantId;
                _ = Task.Run(async () =>
                {
                    try
                    {
                        using var dbContext = LocalDb.CreateContext(tenant);
                        await RetentionCalculationService.RecalculateCustomerAsync(dbContext, custId);
                    }
                    catch { }
                });
            }

            return activity;
        }

        /// <summary>
        /// Retrieves the unified interaction timeline combining manually logged activities and
        /// query-time system events (completed follow-ups, showings, deal stages, support tickets).
        /// </summary>
        public List<TimelineItemDto> GetTimeline(int? customerId, int? leadId, string filter = "All")
        {
            var timeline = new List<TimelineItemDto>();

            try
            {
                // 1. Manually Logged Activities
                var activitiesQuery = _db.Activities
                    .AsNoTracking()
                    .Include(a => a.LoggedByAgent)
                    .AsQueryable();

                if (customerId.HasValue)
                    activitiesQuery = activitiesQuery.Where(a => a.RelatedCustomerId == customerId.Value);
                else if (leadId.HasValue)
                    activitiesQuery = activitiesQuery.Where(a => a.RelatedLeadId == leadId.Value);
                else
                    return timeline;

                var manualActivities = activitiesQuery.ToList();
                foreach (var a in manualActivities)
                {
                    string actor = a.LoggedByAgent?.FullName ?? a.LoggedByAgent?.Email ?? "Agent";
                    string cat = "System Events";
                    string displayTitle = a.Type;

                    if (a.Type.Equals("Call", StringComparison.OrdinalIgnoreCase))
                    {
                        cat = "Calls";
                        string outcomeStr = a.Outcome.HasValue ? FormatCallOutcome(a.Outcome.Value) : "Logged";
                        string durStr = a.DurationMinutes.HasValue && a.DurationMinutes.Value > 0 ? $" ({a.DurationMinutes}m)" : "";
                        displayTitle = $"Call: {outcomeStr}{durStr}";
                    }
                    else if (a.Type.Equals("Email", StringComparison.OrdinalIgnoreCase))
                    {
                        cat = "Emails";
                        displayTitle = "Email Interaction";
                    }
                    else if (a.Type.Equals("Meeting", StringComparison.OrdinalIgnoreCase))
                    {
                        cat = "Meetings";
                        string durStr = a.DurationMinutes.HasValue && a.DurationMinutes.Value > 0 ? $" ({a.DurationMinutes}m)" : "";
                        displayTitle = $"Meeting{durStr}";
                    }

                    timeline.Add(new TimelineItemDto
                    {
                        Id = $"act-{a.ActivityId}",
                        Source = "Manual",
                        Type = a.Type,
                        Category = cat,
                        Title = displayTitle,
                        Notes = a.Notes,
                        Timestamp = a.ActivityDate,
                        ActorName = actor,
                        Outcome = a.Outcome,
                        DurationMinutes = a.DurationMinutes,
                        RelatedCustomerId = a.RelatedCustomerId,
                        RelatedLeadId = a.RelatedLeadId,
                        RawActivityId = a.ActivityId,
                        CanCreateFollowUp = true
                    });
                }

                // 2. System-generated Events: Completed Follow-Ups (TaskReminders)
                var tasksQuery = _db.TaskReminders
                    .AsNoTracking()
                    .Include(t => t.AssignedToUser)
                    .Where(t => !t.IsDeleted && t.Status == "Completed");

                if (customerId.HasValue)
                    tasksQuery = tasksQuery.Where(t => t.RelatedCustomerId == customerId.Value);
                else if (leadId.HasValue)
                    tasksQuery = tasksQuery.Where(t => t.RelatedLeadId == leadId.Value);

                var completedTasks = tasksQuery.ToList();
                foreach (var t in completedTasks)
                {
                    string actor = t.AssignedToUser?.FullName ?? t.AssignedToUser?.Email ?? "Agent";
                    timeline.Add(new TimelineItemDto
                    {
                        Id = $"task-{t.TaskReminderId}",
                        Source = "System",
                        Type = "FollowUp",
                        Category = "System Events",
                        Title = $"Completed Follow-Up: {t.Title}",
                        Notes = t.Notes,
                        Timestamp = t.CompletedAt ?? t.DueDate,
                        ActorName = actor,
                        RelatedCustomerId = t.RelatedCustomerId,
                        RelatedLeadId = t.RelatedLeadId,
                        CanCreateFollowUp = true
                    });
                }

                // 3. System-generated Events: Completed Showings
                var showingsQuery = _db.PropertyShowingDetails
                    .AsNoTracking()
                    .Include(p => p.Property)
                    .Include(p => p.Activity)
                        .ThenInclude(a => a.LoggedByAgent)
                    .AsQueryable();

                if (customerId.HasValue)
                    showingsQuery = showingsQuery.Where(p => p.Activity.RelatedCustomerId == customerId.Value);
                else if (leadId.HasValue)
                    showingsQuery = showingsQuery.Where(p => p.Activity.RelatedLeadId == leadId.Value);

                var completedShowings = showingsQuery.ToList();
                foreach (var s in completedShowings)
                {
                    string actor = s.Activity?.LoggedByAgent?.FullName ?? "Agent";
                    string propTitle = !string.IsNullOrWhiteSpace(s.Property?.Address) ? s.Property.Address : $"Property #{s.PropertyId}";
                    timeline.Add(new TimelineItemDto
                    {
                        Id = $"showing-{s.ShowingDetailId}",
                        Source = "System",
                        Type = "Showing",
                        Category = "System Events",
                        Title = $"Showing Completed: {propTitle}",
                        Notes = s.FeedbackNotes ?? "Property showing conducted.",
                        Timestamp = s.ScheduledDate ?? s.Activity?.ActivityDate ?? DateTime.UtcNow,
                        ActorName = actor,
                        RelatedCustomerId = customerId,
                        RelatedLeadId = leadId,
                        CanCreateFollowUp = true
                    });
                }

                // 4. System-generated Events for Customers: Deals & Support Tickets
                if (customerId.HasValue)
                {
                    // Deals
                    var deals = _db.Deals
                        .AsNoTracking()
                        .Include(d => d.Property)
                        .Include(d => d.Agent)
                        .Where(d => d.CustomerId == customerId.Value)
                        .ToList();

                    foreach (var d in deals)
                    {
                        string actor = d.Agent?.FullName ?? d.Agent?.Email ?? "Sales Agent";
                        string propTitle = !string.IsNullOrWhiteSpace(d.Property?.Address) ? d.Property.Address : $"Property #{d.PropertyId}";
                        timeline.Add(new TimelineItemDto
                        {
                            Id = $"deal-{d.DealId}",
                            Source = "System",
                            Type = "Deal",
                            Category = "System Events",
                            Title = $"Deal Stage: {d.Stage} (₱{d.Value:N2}) - {propTitle}",
                            Notes = $"Scheme: {d.PaymentScheme ?? "N/A"}. {(string.IsNullOrWhiteSpace(d.SpecialStipulations) ? "" : "Terms: " + d.SpecialStipulations)}",
                            Timestamp = d.ContractSignedDate ?? d.CreatedAt,
                            ActorName = actor,
                            RelatedCustomerId = customerId,
                            CanCreateFollowUp = true
                        });
                    }

                    // Support Tickets
                    var tickets = _db.SupportTickets
                        .AsNoTracking()
                        .Include(t => t.RaisedByUser)
                        .Where(t => t.CustomerId == customerId.Value && !t.IsDeleted)
                        .ToList();

                    foreach (var t in tickets)
                    {
                        string actor = t.RaisedByUser?.FullName ?? t.RaisedByUser?.Email ?? "Support Staff";
                        timeline.Add(new TimelineItemDto
                        {
                            Id = $"ticket-{t.TicketId}",
                            Source = "System",
                            Type = "SupportTicket",
                            Category = "System Events",
                            Title = $"Support Ticket #{t.TicketNumber}: {t.Status} ({t.Category})",
                            Notes = t.Description,
                            Timestamp = t.ResolvedAt ?? t.CreatedAt,
                            ActorName = actor,
                            RelatedCustomerId = customerId,
                            CanCreateFollowUp = true
                        });
                    }
                }

                // Sort reverse-chronological (most recent first)
                timeline = timeline.OrderByDescending(t => t.Timestamp).ToList();

                // Apply category filtering
                if (!string.IsNullOrWhiteSpace(filter) && !filter.Equals("All", StringComparison.OrdinalIgnoreCase))
                {
                    timeline = timeline.Where(t => t.Category.Equals(filter, StringComparison.OrdinalIgnoreCase)).ToList();
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[ActivityController.GetTimeline] Error: {ex.Message}");
                var cachedActivities = LocalDataCache.Instance.GetCachedActivities(TenantId, CurrentSession.UserId, RbacService.IsAgent);
                if (customerId.HasValue)
                    cachedActivities = cachedActivities.Where(a => a.RelatedCustomerId == customerId.Value).ToList();
                else if (leadId.HasValue)
                    cachedActivities = cachedActivities.Where(a => a.RelatedLeadId == leadId.Value).ToList();
                else
                    return timeline;

                foreach (var a in cachedActivities)
                {
                    string cat = "System Events";
                    string displayTitle = a.Type;

                    if (a.Type.Equals("Call", StringComparison.OrdinalIgnoreCase))
                    {
                        cat = "Calls";
                        string outcomeStr = a.Outcome.HasValue ? FormatCallOutcome(a.Outcome.Value) : "Logged";
                        string durStr = a.DurationMinutes.HasValue && a.DurationMinutes.Value > 0 ? $" ({a.DurationMinutes}m)" : "";
                        displayTitle = $"Call: {outcomeStr}{durStr}";
                    }
                    else if (a.Type.Equals("Email", StringComparison.OrdinalIgnoreCase))
                    {
                        cat = "Emails";
                        displayTitle = "Email Interaction";
                    }
                    else if (a.Type.Equals("Meeting", StringComparison.OrdinalIgnoreCase))
                    {
                        cat = "Meetings";
                        string durStr = a.DurationMinutes.HasValue && a.DurationMinutes.Value > 0 ? $" ({a.DurationMinutes}m)" : "";
                        displayTitle = $"Meeting{durStr}";
                    }

                    timeline.Add(new TimelineItemDto
                    {
                        Id = $"act-{a.ActivityId}",
                        Source = "Manual",
                        Type = a.Type,
                        Category = cat,
                        Title = displayTitle,
                        Notes = a.Notes,
                        Timestamp = a.ActivityDate,
                        ActorName = "Agent",
                        Outcome = a.Outcome,
                        DurationMinutes = a.DurationMinutes,
                        RelatedCustomerId = a.RelatedCustomerId,
                        RelatedLeadId = a.RelatedLeadId,
                        RawActivityId = a.ActivityId,
                        CanCreateFollowUp = true
                    });
                }

                var cachedTasks = LocalDataCache.Instance.GetCachedTaskReminders(TenantId, CurrentSession.UserId, RbacService.IsAgent)
                    .Where(t => t.Status == "Completed");
                if (customerId.HasValue)
                    cachedTasks = cachedTasks.Where(t => t.RelatedCustomerId == customerId.Value);
                else if (leadId.HasValue)
                    cachedTasks = cachedTasks.Where(t => t.RelatedLeadId == leadId.Value);

                foreach (var t in cachedTasks)
                {
                    timeline.Add(new TimelineItemDto
                    {
                        Id = $"task-{t.TaskReminderId}",
                        Source = "System",
                        Type = "FollowUp",
                        Category = "System Events",
                        Title = $"Completed Follow-Up: {t.Title}",
                        Notes = t.Notes,
                        Timestamp = t.CompletedAt ?? t.DueDate,
                        ActorName = "Agent",
                        RelatedCustomerId = t.RelatedCustomerId,
                        RelatedLeadId = t.RelatedLeadId,
                        CanCreateFollowUp = true
                    });
                }

                timeline = timeline.OrderByDescending(t => t.Timestamp).ToList();

                if (!string.IsNullOrWhiteSpace(filter) && !filter.Equals("All", StringComparison.OrdinalIgnoreCase))
                {
                    timeline = timeline.Where(t => t.Category.Equals(filter, StringComparison.OrdinalIgnoreCase)).ToList();
                }
            }

            return timeline;
        }

        /// <summary>
        /// Creates a pre-populated Follow-Up (TaskReminder) template based on an activity item.
        /// </summary>
        public TaskReminder CreateFollowUpTemplate(TimelineItemDto item)
        {
            if (item == null) throw new ArgumentNullException(nameof(item));

            string followUpType = item.Type switch
            {
                "Email" => "Email",
                "Meeting" => "Meeting",
                _ => "Call"
            };

            var localNow = DateTime.Now;
            var tomorrowNextHour = new DateTime(localNow.Year, localNow.Month, localNow.Day, localNow.Hour, 0, 0)
                .AddDays(1);

            return new TaskReminder
            {
                Title = $"Follow-up regarding {item.Type} ({item.Timestamp.ToLocalTime():MMM d})",
                Type = followUpType,
                Priority = "Medium",
                DueDate = tomorrowNextHour.ToUniversalTime(),
                Notes = $"Follow-up action item from {item.Type.ToLower()}: {item.Notes}",
                RelatedCustomerId = item.RelatedCustomerId,
                RelatedLeadId = item.RelatedLeadId,
                AssignedToUserId = CurrentSession.UserId
            };
        }

        public static string FormatCallOutcome(CallOutcome outcome) => outcome switch
        {
            CallOutcome.Connected => "Connected",
            CallOutcome.LeftVoicemail => "Left Voicemail",
            CallOutcome.NoAnswer => "No Answer",
            CallOutcome.Busy => "Busy",
            CallOutcome.WrongNumber => "Wrong Number",
            _ => outcome.ToString()
        };

        /// <summary>
        /// Retrieves all interactions and events for an agent's activities view, with searching and filtering.
        /// </summary>
        public List<TimelineItemDto> GetAllForAgent(int? agentId = null, string filter = "All", string? search = null)
        {
            var list = new List<TimelineItemDto>();

            try
            {
                // 1. Activities logged by this agent (or all agents if agentId is null)
                var manualQuery = _db.Activities
                    .AsNoTracking()
                    .Include(a => a.LoggedByAgent)
                    .Include(a => a.RelatedCustomer)
                    .Include(a => a.RelatedLead)
                    .AsQueryable();

                if (agentId.HasValue && agentId.Value > 0)
                {
                    manualQuery = manualQuery.Where(a => a.LoggedByAgentId == agentId.Value);
                }

                var manualActivities = manualQuery.ToList();
                foreach (var a in manualActivities)
                {
                    string actor = a.LoggedByAgent?.FullName ?? a.LoggedByAgent?.Email ?? "Agent";
                    string clientName = a.RelatedCustomer?.FullName ?? a.RelatedLead?.FullName ?? "Unknown Contact";
                    string clientType = a.RelatedCustomerId.HasValue ? "Customer" : "Lead";
                    string cat = "System Events";
                    string displayTitle = a.Type;

                    if (a.Type.Equals("Call", StringComparison.OrdinalIgnoreCase))
                    {
                        cat = "Calls";
                        string outcomeStr = a.Outcome.HasValue ? FormatCallOutcome(a.Outcome.Value) : "Logged";
                        string durStr = a.DurationMinutes.HasValue && a.DurationMinutes.Value > 0 ? $" ({a.DurationMinutes}m)" : "";
                        displayTitle = $"Call: {outcomeStr}{durStr}";
                    }
                    else if (a.Type.Equals("Email", StringComparison.OrdinalIgnoreCase))
                    {
                        cat = "Emails";
                        displayTitle = "Email Interaction";
                    }
                    else if (a.Type.Equals("Meeting", StringComparison.OrdinalIgnoreCase))
                    {
                        cat = "Meetings";
                        string durStr = a.DurationMinutes.HasValue && a.DurationMinutes.Value > 0 ? $" ({a.DurationMinutes}m)" : "";
                        displayTitle = $"Meeting{durStr}";
                    }

                    list.Add(new TimelineItemDto
                    {
                        Id = $"act-{a.ActivityId}",
                        Source = "Manual",
                        Type = a.Type,
                        Category = cat,
                        Title = displayTitle,
                        Notes = a.Notes,
                        Timestamp = a.ActivityDate,
                        ActorName = actor,
                        ClientName = clientName,
                        ClientType = clientType,
                        Outcome = a.Outcome,
                        DurationMinutes = a.DurationMinutes,
                        RelatedCustomerId = a.RelatedCustomerId,
                        RelatedLeadId = a.RelatedLeadId,
                        RawActivityId = a.ActivityId,
                        CanCreateFollowUp = true
                    });
                }

                // 2. Completed Follow-Ups assigned to this agent (or all if agentId is null)
                var taskQuery = _db.TaskReminders
                    .AsNoTracking()
                    .Include(t => t.AssignedToUser)
                    .Include(t => t.RelatedCustomer)
                    .Include(t => t.RelatedLead)
                    .Where(t => !t.IsDeleted && t.Status == "Completed");

                if (agentId.HasValue && agentId.Value > 0)
                {
                    taskQuery = taskQuery.Where(t => t.AssignedToUserId == agentId.Value);
                }

                var completedTasks = taskQuery.ToList();
                foreach (var t in completedTasks)
                {
                    string actor = t.AssignedToUser?.FullName ?? t.AssignedToUser?.Email ?? "Agent";
                    string clientName = t.RelatedCustomer?.FullName ?? t.RelatedLead?.FullName ?? "Unknown Contact";
                    string clientType = t.RelatedCustomerId.HasValue ? "Customer" : "Lead";

                    list.Add(new TimelineItemDto
                    {
                        Id = $"task-{t.TaskReminderId}",
                        Source = "System",
                        Type = "FollowUp",
                        Category = "System Events",
                        Title = $"Completed Follow-Up: {t.Title}",
                        Notes = t.Notes,
                        Timestamp = t.CompletedAt ?? t.DueDate,
                        ActorName = actor,
                        ClientName = clientName,
                        ClientType = clientType,
                        RelatedCustomerId = t.RelatedCustomerId,
                        RelatedLeadId = t.RelatedLeadId,
                        CanCreateFollowUp = true
                    });
                }

                // 3. Completed Showings
                var showingQuery = _db.PropertyShowingDetails
                    .AsNoTracking()
                    .Include(p => p.Property)
                    .Include(p => p.Activity).ThenInclude(a => a.LoggedByAgent)
                    .Include(p => p.Activity).ThenInclude(a => a.RelatedCustomer)
                    .Include(p => p.Activity).ThenInclude(a => a.RelatedLead)
                    .AsQueryable();

                if (agentId.HasValue && agentId.Value > 0)
                {
                    showingQuery = showingQuery.Where(p => p.Activity.LoggedByAgentId == agentId.Value);
                }

                var completedShowings = showingQuery.ToList();
                foreach (var s in completedShowings)
                {
                    string actor = s.Activity?.LoggedByAgent?.FullName ?? "Agent";
                    string clientName = s.Activity?.RelatedCustomer?.FullName ?? s.Activity?.RelatedLead?.FullName ?? "Unknown Contact";
                    string clientType = s.Activity?.RelatedCustomerId.HasValue == true ? "Customer" : "Lead";
                    string propTitle = !string.IsNullOrWhiteSpace(s.Property?.Address) ? s.Property.Address : $"Property #{s.PropertyId}";

                    list.Add(new TimelineItemDto
                    {
                        Id = $"showing-{s.ShowingDetailId}",
                        Source = "System",
                        Type = "Showing",
                        Category = "System Events",
                        Title = $"Showing Completed: {propTitle}",
                        Notes = s.FeedbackNotes ?? "Property showing conducted.",
                        Timestamp = s.ScheduledDate ?? s.Activity?.ActivityDate ?? DateTime.UtcNow,
                        ActorName = actor,
                        ClientName = clientName,
                        ClientType = clientType,
                        RelatedCustomerId = s.Activity?.RelatedCustomerId,
                        RelatedLeadId = s.Activity?.RelatedLeadId,
                        CanCreateFollowUp = true
                    });
                }

                // Sort reverse-chronological
                list = list.OrderByDescending(x => x.Timestamp).ToList();

                // Apply search filter
                if (!string.IsNullOrWhiteSpace(search))
                {
                    string s = search.Trim();
                    list = list.Where(item =>
                        item.Title.Contains(s, StringComparison.OrdinalIgnoreCase) ||
                        item.ClientName.Contains(s, StringComparison.OrdinalIgnoreCase) ||
                        item.Type.Contains(s, StringComparison.OrdinalIgnoreCase) ||
                        (item.Notes != null && item.Notes.Contains(s, StringComparison.OrdinalIgnoreCase))
                    ).ToList();
                }

                // Apply category filter
                if (!string.IsNullOrWhiteSpace(filter) && !filter.Equals("All", StringComparison.OrdinalIgnoreCase))
                {
                    list = list.Where(x => x.Category.Equals(filter, StringComparison.OrdinalIgnoreCase)).ToList();
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[ActivityController.GetAllForAgent] Error: {ex.Message}");
                int effectiveUserId = agentId ?? CurrentSession.UserId;
                var cachedActivities = LocalDataCache.Instance.GetCachedActivities(TenantId, effectiveUserId, RbacService.IsAgent);
                foreach (var a in cachedActivities)
                {
                    string cat = "System Events";
                    string displayTitle = a.Type;

                    if (a.Type.Equals("Call", StringComparison.OrdinalIgnoreCase))
                    {
                        cat = "Calls";
                        string outcomeStr = a.Outcome.HasValue ? FormatCallOutcome(a.Outcome.Value) : "Logged";
                        string durStr = a.DurationMinutes.HasValue && a.DurationMinutes.Value > 0 ? $" ({a.DurationMinutes}m)" : "";
                        displayTitle = $"Call: {outcomeStr}{durStr}";
                    }
                    else if (a.Type.Equals("Email", StringComparison.OrdinalIgnoreCase))
                    {
                        cat = "Emails";
                        displayTitle = "Email Interaction";
                    }
                    else if (a.Type.Equals("Meeting", StringComparison.OrdinalIgnoreCase))
                    {
                        cat = "Meetings";
                        string durStr = a.DurationMinutes.HasValue && a.DurationMinutes.Value > 0 ? $" ({a.DurationMinutes}m)" : "";
                        displayTitle = $"Meeting{durStr}";
                    }

                    list.Add(new TimelineItemDto
                    {
                        Id = $"act-{a.ActivityId}",
                        Source = "Manual",
                        Type = a.Type,
                        Category = cat,
                        Title = displayTitle,
                        Notes = a.Notes,
                        Timestamp = a.ActivityDate,
                        ActorName = "Agent",
                        ClientName = "Contact",
                        ClientType = a.RelatedCustomerId.HasValue ? "Customer" : "Lead",
                        Outcome = a.Outcome,
                        DurationMinutes = a.DurationMinutes,
                        RelatedCustomerId = a.RelatedCustomerId,
                        RelatedLeadId = a.RelatedLeadId,
                        RawActivityId = a.ActivityId,
                        CanCreateFollowUp = true
                    });
                }

                var cachedTasks = LocalDataCache.Instance.GetCachedTaskReminders(TenantId, effectiveUserId, RbacService.IsAgent)
                    .Where(t => t.Status == "Completed");
                foreach (var t in cachedTasks)
                {
                    list.Add(new TimelineItemDto
                    {
                        Id = $"task-{t.TaskReminderId}",
                        Source = "System",
                        Type = "FollowUp",
                        Category = "System Events",
                        Title = $"Completed Follow-Up: {t.Title}",
                        Notes = t.Notes,
                        Timestamp = t.CompletedAt ?? t.DueDate,
                        ActorName = "Agent",
                        ClientName = "Contact",
                        ClientType = t.RelatedCustomerId.HasValue ? "Customer" : "Lead",
                        RelatedCustomerId = t.RelatedCustomerId,
                        RelatedLeadId = t.RelatedLeadId,
                        CanCreateFollowUp = true
                    });
                }

                list = list.OrderByDescending(x => x.Timestamp).ToList();

                if (!string.IsNullOrWhiteSpace(search))
                {
                    string s = search.Trim();
                    list = list.Where(item =>
                        item.Title.Contains(s, StringComparison.OrdinalIgnoreCase) ||
                        item.ClientName.Contains(s, StringComparison.OrdinalIgnoreCase) ||
                        item.Type.Contains(s, StringComparison.OrdinalIgnoreCase) ||
                        (item.Notes != null && item.Notes.Contains(s, StringComparison.OrdinalIgnoreCase))
                    ).ToList();
                }

                if (!string.IsNullOrWhiteSpace(filter) && !filter.Equals("All", StringComparison.OrdinalIgnoreCase))
                {
                    list = list.Where(x => x.Category.Equals(filter, StringComparison.OrdinalIgnoreCase)).ToList();
                }
            }
            return list;
        }

        public CRMS_Peguit.domain.Common.PagedResult<TimelineItemDto> GetPagedForAgent(int? agentId = null, string filter = "All", string? search = null, int pageNumber = 1, int pageSize = 25)
        {
            var all = GetAllForAgent(agentId, filter, search);
            int total = all.Count;
            int page = Math.Max(1, pageNumber);
            int size = Math.Max(1, pageSize);
            var pagedItems = all.Skip((page - 1) * size).Take(size).ToList();
            return new CRMS_Peguit.domain.Common.PagedResult<TimelineItemDto>(pagedItems, total, page, size);
        }

        public (int total, int calls, int emails, int meetings) GetActivityStatsForAgent(int? agentId = null)
        {
            try
            {
                var query = _db.Activities.AsNoTracking().AsQueryable();
                if (agentId.HasValue && agentId.Value > 0)
                {
                    query = query.Where(a => a.LoggedByAgentId == agentId.Value);
                }
                int total = query.Count();
                int calls = query.Count(a => a.Type == "Call");
                int emails = query.Count(a => a.Type == "Email");
                int meetings = query.Count(a => a.Type == "Meeting");
                return (total, calls, emails, meetings);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[ActivityController.GetActivityStatsForAgent] Error: {ex.Message}");
                int effectiveUserId = agentId ?? CurrentSession.UserId;
                var list = LocalDataCache.Instance.GetCachedActivities(TenantId, effectiveUserId, RbacService.IsAgent);
                int total = list.Count;
                int calls = list.Count(a => a.Type == "Call");
                int emails = list.Count(a => a.Type == "Email");
                int meetings = list.Count(a => a.Type == "Meeting");
                return (total, calls, emails, meetings);
            }
        }

        public void Dispose()
        {
            _db.Dispose();
        }
    }
}
