using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using CRMS_Peguit.domain.Common;
using CRMS_Peguit.domain.entities;
using CRMS_Peguit.winforms.Models.Services;

namespace CRMS_Peguit.winforms.Services
{
    public class LeadStageCounts
    {
        public int Total { get; set; }
        public int New { get; set; }
        public int Contacted { get; set; }
        public int Qualified { get; set; }
        public int Converted { get; set; }
        public int Lost { get; set; }
    }

    public class LeadApiService : BaseApiService
    {
        public LeadApiService(string? baseUrl = null, HttpClient? httpClient = null)
            : base(baseUrl, httpClient)
        {
        }

        public List<Lead> GetAll() =>
            Get<List<Lead>>("api/leads") ?? new();

        public async Task<PagedResult<Lead>> GetPagedAsync(
            int pageNumber = 1,
            int pageSize = 25,
            string? search = null,
            string? stage = null,
            string? priority = null,
            int? agentId = null,
            string? sortColumn = null,
            bool sortAscending = false)
        {
            var uri = $"api/leads/paged?pageNumber={pageNumber}&pageSize={pageSize}";
            if (!string.IsNullOrWhiteSpace(search)) uri += $"&search={Uri.EscapeDataString(search)}";
            if (!string.IsNullOrWhiteSpace(stage)) uri += $"&stage={Uri.EscapeDataString(stage)}";
            if (!string.IsNullOrWhiteSpace(priority)) uri += $"&priority={Uri.EscapeDataString(priority)}";
            if (agentId.HasValue) uri += $"&agentId={agentId.Value}";
            if (!string.IsNullOrWhiteSpace(sortColumn)) uri += $"&sortColumn={Uri.EscapeDataString(sortColumn)}&sortAscending={sortAscending}";

            return await GetAsync<PagedResult<Lead>>(uri) ?? new PagedResult<Lead>();
        }

        public async Task<PagedResult<Lead>> GetPagedAsync(
            int pageNumber,
            int pageSize,
            string? search,
            string? stage,
            string? sortColumn,
            bool sortAscending) =>
            await GetPagedAsync(pageNumber, pageSize, search, stage, null, null, sortColumn, sortAscending);

        public PagedResult<Lead> GetPaged(
            int pageNumber = 1,
            int pageSize = 25,
            string? search = null,
            string? stage = null,
            string? priority = null,
            int? agentId = null,
            string? sortColumn = null,
            bool sortAscending = false) =>
            GetPagedAsync(pageNumber, pageSize, search, stage, priority, agentId, sortColumn, sortAscending).GetAwaiter().GetResult();

        public async Task<LeadStageCounts> GetStageCountsAsync() =>
            await GetAsync<LeadStageCounts>("api/leads/stage-counts") ?? new LeadStageCounts();

        public LeadStageCounts GetStageCounts() =>
            GetStageCountsAsync().GetAwaiter().GetResult();

        public Lead? GetById(int id) =>
            Get<Lead>($"api/leads/{id}");

        public Lead Add(Lead lead)
        {
            var result = Post<Lead, Lead>("api/leads", lead);
            return result ?? lead;
        }

        public void Update(Lead lead) =>
            Put<Lead, Lead>($"api/leads/{lead.LeadId}", lead);

        public void SoftDelete(Lead lead) =>
            Delete($"api/leads/{lead.LeadId}");

        public void Restore(Lead lead) =>
            Post($"api/leads/{lead.LeadId}/restore", new { });

        public List<Lead> GetArchived() =>
            Get<List<Lead>>("api/leads/archived") ?? new();

        public Customer ConvertToCustomer(Lead lead)
        {
            var result = Post<object, Customer>($"api/leads/{lead.LeadId}/convert", new { });
            return result ?? new Customer();
        }

        public void MarkLost(Lead lead) =>
            Post($"api/leads/{lead.LeadId}/lost", new { });

        public void RestoreFromLost(Lead lead) =>
            Post($"api/leads/{lead.LeadId}/restore-from-lost", new { });

        public void ApproveAssignment(Lead lead, string? notes = null) =>
            Post($"api/leads/{lead.LeadId}/assignment/approve", new { notes });

        public void AssignAgent(Lead lead, int? agentId, bool approve = true, string? notes = null) =>
            Post($"api/leads/{lead.LeadId}/assignment", new { agentId, approve, notes });

        public List<Lead> GetPendingReview() =>
            Get<List<Lead>>("api/leads/pending-review") ?? new();

        public List<AgentPickerItem> GetAgents() =>
            Get<List<AgentPickerItem>>("api/leads/agents") ?? new();

        public Dictionary<int, string> GetAgentDictionary() =>
            GetAgents().ToDictionary(a => a.UserId, a => a.FullName);

        public string? GetAssignedAgentName(int? assignedAgentId)
        {
            if (!assignedAgentId.HasValue) return null;
            var agents = GetAgents();
            return agents.FirstOrDefault(a => a.UserId == assignedAgentId.Value)?.FullName;
        }

        public List<Activity> GetActivityHistory(int leadId) =>
            Get<List<Activity>>($"api/leads/{leadId}/activities") ?? new();

        public void LogEmail(Lead lead, string subject)
        {
            Post("api/activities", new Activity
            {
                Type = "Email",
                RelatedLeadId = lead.LeadId,
                Notes = $"Sent email: {subject}",
                ActivityDate = DateTime.UtcNow
            });
        }

        public int GetActiveLeadsCount(int? agentId = null)
        {
            var uri = "api/leads/active-count";
            if (agentId.HasValue) uri += $"?agentId={agentId.Value}";
            return Get<int>(uri);
        }

        public double GetTeamConversionRate() =>
            Get<double>("api/leads/conversion-rate");

        public int GetPendingReviewCount() =>
            Get<int>("api/leads/pending-review/count");

        public static bool ValidateLeadInput(string firstName, string lastName, string? email, out string? errorMessage) =>
            ValidateLeadInput(firstName, lastName, email, null, null, out errorMessage);

        public static bool ValidateLeadInput(string firstName, string lastName, string? email, string? phone, decimal? expectedValue, out string? errorMessage)
        {
            if (string.IsNullOrWhiteSpace(firstName))
            {
                errorMessage = "First name is required.";
                return false;
            }

            if (string.IsNullOrWhiteSpace(lastName))
            {
                errorMessage = "Last name is required.";
                return false;
            }

            if (!string.IsNullOrWhiteSpace(email) && !ContactEmailService.IsValidEmail(email.Trim()))
            {
                errorMessage = "Invalid email format.";
                return false;
            }

            if (!string.IsNullOrWhiteSpace(phone) && phone.Trim().Length < 7)
            {
                errorMessage = "Phone number is too short.";
                return false;
            }

            if (expectedValue.HasValue && expectedValue.Value < 0)
            {
                errorMessage = "Estimated budget cannot be negative.";
                return false;
            }

            errorMessage = null;
            return true;
        }

        public static bool ValidateLeadInput(
            string firstName,
            string lastName,
            string? email,
            string? phone,
            string? expectedValueText,
            out decimal? expectedValue,
            out string? errorMessage,
            out string? errorField)
        {
            expectedValue = null;
            errorMessage = null;
            errorField = null;

            if (string.IsNullOrWhiteSpace(firstName))
            {
                errorMessage = "First name is required.";
                errorField = "FirstName";
                return false;
            }

            if (string.IsNullOrWhiteSpace(lastName))
            {
                errorMessage = "Last name is required.";
                errorField = "LastName";
                return false;
            }

            if (!string.IsNullOrWhiteSpace(email) && !ContactEmailService.IsValidEmail(email.Trim()))
            {
                errorMessage = "Invalid email format.";
                errorField = "Email";
                return false;
            }

            if (!string.IsNullOrWhiteSpace(phone) && phone.Trim().Length < 7)
            {
                errorMessage = "Phone number is too short.";
                errorField = "Phone";
                return false;
            }

            if (!string.IsNullOrWhiteSpace(expectedValueText))
            {
                var cleaned = expectedValueText.Replace(",", "").Trim();
                if (!decimal.TryParse(cleaned, out decimal parsedVal) || parsedVal < 0)
                {
                    errorMessage = "Expected value must be a valid positive number.";
                    errorField = "ExpectedValue";
                    return false;
                }
                expectedValue = parsedVal;
            }

            return true;
        }
    }
}
