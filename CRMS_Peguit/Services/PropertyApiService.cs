using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using CRMS_Peguit.domain.Common;
using CRMS_Peguit.domain.entities;

namespace CRMS_Peguit.winforms.Services
{
    public class PropertyApiService : BaseApiService
    {
        public PropertyApiService(string? baseUrl = null, HttpClient? httpClient = null)
            : base(baseUrl, httpClient)
        {
        }

        public List<Property> GetAll() =>
            Get<List<Property>>("api/properties") ?? new();

        public async Task<PagedResult<Property>> GetPagedAsync(
            int pageNumber = 1,
            int pageSize = 25,
            string? search = null,
            string? propertyType = null,
            string? status = null,
            int? agentId = null,
            string? sortColumn = null,
            bool sortAscending = false)
        {
            var uri = $"api/properties/paged?pageNumber={pageNumber}&pageSize={pageSize}";
            if (!string.IsNullOrWhiteSpace(search)) uri += $"&search={Uri.EscapeDataString(search)}";
            if (!string.IsNullOrWhiteSpace(propertyType)) uri += $"&propertyType={Uri.EscapeDataString(propertyType)}";
            if (!string.IsNullOrWhiteSpace(status)) uri += $"&status={Uri.EscapeDataString(status)}";
            if (agentId.HasValue) uri += $"&agentId={agentId.Value}";
            if (!string.IsNullOrWhiteSpace(sortColumn)) uri += $"&sortColumn={Uri.EscapeDataString(sortColumn)}&sortAscending={sortAscending}";

            return await GetAsync<PagedResult<Property>>(uri) ?? new PagedResult<Property>();
        }

        public PagedResult<Property> GetPaged(
            int pageNumber = 1,
            int pageSize = 25,
            string? search = null,
            string? propertyType = null,
            string? status = null,
            int? agentId = null,
            string? sortColumn = null,
            bool sortAscending = false) =>
            GetPagedAsync(pageNumber, pageSize, search, propertyType, status, agentId, sortColumn, sortAscending).GetAwaiter().GetResult();

        public async Task<PropertyCounts> GetPropertyCountsAsync() =>
            await GetAsync<PropertyCounts>("api/properties/counts") ?? new PropertyCounts();

        public PropertyCounts GetPropertyCounts() =>
            GetPropertyCountsAsync().GetAwaiter().GetResult();

        public Property? GetById(int id) =>
            Get<Property>($"api/properties/{id}");

        public Property Add(Property property)
        {
            var result = Post<Property, Property>("api/properties", property);
            return result ?? property;
        }

        public void Update(Property property) =>
            Put<Property, Property>($"api/properties/{property.PropertyId}", property);

        public void Delete(Property property) =>
            Delete($"api/properties/{property.PropertyId}");

        public void ApproveAssignment(Property property, string? notes = null) =>
            Post($"api/properties/{property.PropertyId}/assignment/approve", new { notes });

        public void AssignAgent(Property property, int? agentId, bool approve = true, string? notes = null) =>
            Post($"api/properties/{property.PropertyId}/assignment", new { agentId, approve, notes });

        public List<Property> GetPendingReview() =>
            Get<List<Property>>("api/properties/pending-review") ?? new();

        public List<CustomerPickerItem> GetOwnerCustomers() =>
            Get<List<CustomerPickerItem>>("api/properties/owner-customers") ?? new();

        public List<AgentPickerItem> GetAgents() =>
            Get<List<AgentPickerItem>>("api/properties/agents") ?? new();

        public string? GetOwnerName(int ownerCustomerId)
        {
            var owners = GetOwnerCustomers();
            return owners.FirstOrDefault(o => o.CustomerId == ownerCustomerId)?.FullName;
        }

        public string? GetListedAgentName(int? listedByAgentId)
        {
            if (!listedByAgentId.HasValue) return null;
            var agents = GetAgents();
            return agents.FirstOrDefault(a => a.UserId == listedByAgentId.Value)?.FullName;
        }
    }
}
