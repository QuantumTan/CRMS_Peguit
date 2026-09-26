using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using CRMS_Peguit.domain.Common;
using CRMS_Peguit.domain.entities;

namespace CRMS_Peguit.winforms.Services
{
    public class ActivityStatsDto
    {
        public int TotalActivities { get; set; }
        public int TotalCalls { get; set; }
        public int TotalEmails { get; set; }
        public int TotalMeetings { get; set; }
        public int TotalNotes { get; set; }
    }

    public class ActivityApiService : BaseApiService
    {
        public ActivityApiService(string? baseUrl = null, HttpClient? httpClient = null)
            : base(baseUrl, httpClient)
        {
        }

        public List<Activity> GetAll() =>
            Get<List<Activity>>("api/activities") ?? new();

        public async Task<PagedResult<Activity>> GetPagedAsync(
            int pageNumber = 1,
            int pageSize = 25,
            string? search = null,
            string? type = null,
            int? agentId = null,
            string? sortColumn = null,
            bool sortAscending = false)
        {
            var uri = $"api/activities/paged?pageNumber={pageNumber}&pageSize={pageSize}";
            if (!string.IsNullOrWhiteSpace(search)) uri += $"&search={Uri.EscapeDataString(search)}";
            if (!string.IsNullOrWhiteSpace(type)) uri += $"&type={Uri.EscapeDataString(type)}";
            if (agentId.HasValue) uri += $"&agentId={agentId.Value}";
            if (!string.IsNullOrWhiteSpace(sortColumn)) uri += $"&sortColumn={Uri.EscapeDataString(sortColumn)}&sortAscending={sortAscending}";

            return await GetAsync<PagedResult<Activity>>(uri) ?? new PagedResult<Activity>();
        }

        public PagedResult<Activity> GetPaged(
            int pageNumber = 1,
            int pageSize = 25,
            string? search = null,
            string? type = null,
            int? agentId = null,
            string? sortColumn = null,
            bool sortAscending = false) =>
            GetPagedAsync(pageNumber, pageSize, search, type, agentId, sortColumn, sortAscending).GetAwaiter().GetResult();

        public Activity? GetById(int id) =>
            Get<Activity>($"api/activities/{id}");

        public Activity Add(Activity activity)
        {
            var result = Post<Activity, Activity>("api/activities", activity);
            return result ?? activity;
        }

        public void Update(Activity activity) =>
            Put<Activity, Activity>($"api/activities/{activity.ActivityId}", activity);

        public void Delete(int id) =>
            Delete($"api/activities/{id}");

        public ActivityStatsDto GetStats() =>
            Get<ActivityStatsDto>("api/activities/stats") ?? new ActivityStatsDto();

        public List<Activity> GetRecentActivities(int count = 15) =>
            Get<List<Activity>>($"api/activities/recent?count={count}") ?? new();

        public List<Activity> GetRecentActivitiesForAgent(int agentId, int count = 5) =>
            Get<List<Activity>>($"api/activities/agent/{agentId}?count={count}") ?? new();

        public List<AgentPickerItem> GetAgents() =>
            Get<List<AgentPickerItem>>("api/activities/agents") ?? new();

        public void LogActivity(Activity activity) =>
            Add(activity);

        public bool CanViewTimeline(int? assignedAgentId)
        {
            if (CRMS_Peguit.winforms.Auth.RbacService.IsSuperAdmin ||
                CRMS_Peguit.winforms.Auth.RbacService.HasFullOversight ||
                CRMS_Peguit.winforms.Auth.RbacService.IsManager)
                return true;

            return assignedAgentId == CRMS_Peguit.winforms.Auth.CurrentSession.UserId;
        }

        public List<TimelineItemDto> GetTimeline(int? customerId, int? leadId, string? filter = null)
        {
            var uri = $"api/activities/timeline?customerId={customerId}&leadId={leadId}";
            if (!string.IsNullOrWhiteSpace(filter)) uri += $"&filter={Uri.EscapeDataString(filter)}";
            return Get<List<TimelineItemDto>>(uri) ?? new();
        }

        public TaskReminder CreateFollowUpTemplate(TimelineItemDto item)
        {
            return new TaskReminder
            {
                Title = $"Follow up: {item.Title}",
                Type = item.Type,
                DueDate = DateTime.Today.AddDays(1).AddHours(9),
                Priority = "Medium",
                Status = "Pending",
                AssignedToUserId = CRMS_Peguit.winforms.Auth.CurrentSession.UserId,
                RelatedCustomerId = item.RelatedCustomerId,
                RelatedLeadId = item.RelatedLeadId,
                Notes = item.Notes
            };
        }

        public PagedResult<TimelineItemDto> GetPagedForAgent(int? agentId, string? filterCategory, string? search, int page, int pageSize)
        {
            var uri = $"api/activities/paged?page={page}&pageSize={pageSize}";
            if (agentId.HasValue) uri += $"&agentId={agentId.Value}";
            if (!string.IsNullOrWhiteSpace(filterCategory)) uri += $"&category={Uri.EscapeDataString(filterCategory)}";
            if (!string.IsNullOrWhiteSpace(search)) uri += $"&search={Uri.EscapeDataString(search)}";

            var paged = Get<PagedResult<Activity>>(uri) ?? new PagedResult<Activity>();

            var items = paged.Items.Select(a =>
            {
                string clientName = "";
                string clientType = "Customer";
                if (a.RelatedCustomer?.Person != null)
                {
                    clientName = $"{a.RelatedCustomer.Person.FirstName} {a.RelatedCustomer.Person.LastName}".Trim();
                    clientType = "Customer";
                }
                else if (a.RelatedLead?.Person != null)
                {
                    clientName = $"{a.RelatedLead.Person.FirstName} {a.RelatedLead.Person.LastName}".Trim();
                    clientType = "Lead";
                }

                string actorName = a.LoggedByAgent?.Person != null
                    ? $"{a.LoggedByAgent.Person.FirstName} {a.LoggedByAgent.Person.LastName}".Trim()
                    : "System";

                return new TimelineItemDto
                {
                    Id = a.ActivityId.ToString(),
                    RawActivityId = a.ActivityId,
                    Source = "Activity",
                    Type = a.Type ?? "Activity",
                    Category = a.Type ?? "Activity",
                    Title = $"{a.Type}: {clientName}".TrimEnd(':', ' '),
                    Notes = a.Notes,
                    Timestamp = a.ActivityDate,
                    ActorName = actorName,
                    Outcome = a.Outcome,
                    DurationMinutes = a.DurationMinutes,
                    RelatedCustomerId = a.RelatedCustomerId,
                    RelatedLeadId = a.RelatedLeadId,
                    ClientName = clientName,
                    ClientType = clientType,
                    CanCreateFollowUp = true
                };
            }).ToList();

            return new PagedResult<TimelineItemDto>(items, paged.TotalCount, paged.PageNumber, paged.PageSize);
        }

        public (int total, int calls, int emails, int meetings) GetActivityStatsForAgent(int? agentId)
        {
            var uri = agentId.HasValue ? $"api/activities/stats?agentId={agentId.Value}" : "api/activities/stats";
            var stats = Get<ApiActivityStatsModel>(uri);
            return stats != null ? (stats.Total, stats.Calls, stats.Emails, stats.Meetings) : (0, 0, 0, 0);
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
    }

    public class ApiActivityStatsModel
    {
        public int Total { get; set; }
        public int Calls { get; set; }
        public int Meetings { get; set; }
        public int Showings { get; set; }
        public int Tasks { get; set; }
        public int Emails { get; set; }
    }
}
