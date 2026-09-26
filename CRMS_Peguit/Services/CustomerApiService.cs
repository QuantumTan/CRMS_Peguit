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
    public class CustomerKpiCounts
    {
        public int Total { get; set; }
        public int Active { get; set; }
        public int Inactive { get; set; }
        public int FollowUp { get; set; }
        public int ThisMonth { get; set; }
    }

    public class CustomerApiService : BaseApiService
    {
        public CustomerApiService(string? baseUrl = null, HttpClient? httpClient = null)
            : base(baseUrl, httpClient)
        {
        }

        public List<Customer> GetAll() =>
            Get<List<Customer>>("api/customers") ?? new();

        public async Task<PagedResult<Customer>> GetPagedAsync(
            int pageNumber = 1,
            int pageSize = 25,
            string? search = null,
            string? filterStatus = null)
        {
            var uri = $"api/customers/paged?pageNumber={pageNumber}&pageSize={pageSize}";
            if (!string.IsNullOrWhiteSpace(search)) uri += $"&search={Uri.EscapeDataString(search)}";
            if (!string.IsNullOrWhiteSpace(filterStatus)) uri += $"&filterStatus={Uri.EscapeDataString(filterStatus)}";

            return await GetAsync<PagedResult<Customer>>(uri) ?? new PagedResult<Customer>();
        }

        public PagedResult<Customer> GetPaged(
            int pageNumber = 1,
            int pageSize = 25,
            string? search = null,
            string? filterStatus = null) =>
            GetPagedAsync(pageNumber, pageSize, search, filterStatus).GetAwaiter().GetResult();

        public Customer? GetById(int id) =>
            Get<Customer>($"api/customers/{id}");

        public Customer Add(Customer customer)
        {
            var result = Post<Customer, Customer>("api/customers", customer);
            return result ?? customer;
        }

        public void Update(Customer customer) =>
            Put<Customer, Customer>($"api/customers/{customer.CustomerId}", customer);

        public void SoftDelete(Customer customer) =>
            Delete($"api/customers/{customer.CustomerId}");

        public void Delete(Customer customer) =>
            SoftDelete(customer);

        public void Restore(Customer customer) =>
            Post($"api/customers/{customer.CustomerId}/restore", new { });

        public List<Customer> GetArchived() =>
            Get<List<Customer>>("api/customers/archived") ?? new();

        public List<Property> GetOwnedProperties(int customerId) =>
            Get<List<Property>>($"api/customers/{customerId}/properties") ?? new();

        public List<Activity> GetActivityHistory(int customerId) =>
            Get<List<Activity>>($"api/customers/{customerId}/activities") ?? new();

        public void ApproveAssignment(Customer customer, string? notes = null) =>
            Post($"api/customers/{customer.CustomerId}/assignment/approve", new { notes });

        public void AssignAgent(Customer customer, int? agentId, bool approve = true, string? notes = null) =>
            Post($"api/customers/{customer.CustomerId}/assignment", new { agentId, approve, notes });

        public List<Customer> GetPendingReview() =>
            Get<List<Customer>>("api/customers/pending-review") ?? new();

        public List<AgentPickerItem> GetAgents() =>
            Get<List<AgentPickerItem>>("api/customers/agents") ?? new();

        public Dictionary<int, string> GetAgentDictionary() =>
            GetAgents().ToDictionary(a => a.UserId, a => a.FullName);

        public string? GetAssignedAgentName(int? assignedAgentId)
        {
            if (!assignedAgentId.HasValue) return null;
            var agents = GetAgents();
            return agents.FirstOrDefault(a => a.UserId == assignedAgentId.Value)?.FullName;
        }

        public async Task<CustomerKpiCounts> GetKpiCountsAsync() =>
            await GetAsync<CustomerKpiCounts>("api/customers/kpi") ?? new CustomerKpiCounts();

        public CustomerKpiCounts GetKpiCounts() =>
            GetKpiCountsAsync().GetAwaiter().GetResult();

        public void LogEmail(Customer customer, string subject)
        {
            Post("api/activities", new Activity
            {
                Type = "Email",
                RelatedCustomerId = customer.CustomerId,
                Notes = $"Sent email: {subject}",
                ActivityDate = DateTime.UtcNow
            });
        }

        public void LogCall(Customer customer, string notes)
        {
            Post("api/activities", new Activity
            {
                Type = "Call",
                RelatedCustomerId = customer.CustomerId,
                Notes = notes,
                ActivityDate = DateTime.UtcNow
            });
        }

        public void LogMeeting(Customer customer, string notes)
        {
            Post("api/activities", new Activity
            {
                Type = "Meeting",
                RelatedCustomerId = customer.CustomerId,
                Notes = notes,
                ActivityDate = DateTime.UtcNow
            });
        }

        public List<Activity> GetRecentActivitiesForAgent(int agentId, int maxCount = 5) =>
            Get<List<Activity>>($"api/customers/agents/{agentId}/activities?count={maxCount}") ?? new();

        public int GetPendingReviewCount() =>
            Get<int>("api/customers/pending-review/count");

        public static bool ValidateCustomerInput(string firstName, string lastName, string? email, out string? errorMessage) =>
            ValidateCustomerInput(firstName, lastName, email, null, out errorMessage);

        public static bool ValidateCustomerInput(string firstName, string lastName, string? email, string? phone, out string? errorMessage)
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

            errorMessage = null;
            return true;
        }
    }
}
