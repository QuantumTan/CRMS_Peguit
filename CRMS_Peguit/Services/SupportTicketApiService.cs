using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using CRMS_Peguit.domain.Common;
using CRMS_Peguit.domain.entities;
using CRMS_Peguit.winforms.Auth;

namespace CRMS_Peguit.winforms.Services
{
    public class SupportTicketKpiCounts
    {
        public int Open { get; set; }
        public int InProgress { get; set; }
        public int Resolved { get; set; }
        public int Overdue { get; set; }
        public int Total { get; set; }
    }

    public class SupportTicketApiService : BaseApiService
    {
        public SupportTicketApiService(string? baseUrl = null, HttpClient? httpClient = null)
            : base(baseUrl, httpClient)
        {
        }

        public List<SupportTicket> GetAll() =>
            Get<List<SupportTicket>>("api/supporttickets") ?? new();

        public async Task<PagedResult<SupportTicket>> GetPagedAsync(
            int pageNumber = 1,
            int pageSize = 25,
            string? search = null,
            string? status = null,
            string? priority = null,
            string? category = null,
            int? assignedToUserId = null,
            string? sortColumn = null,
            bool sortAscending = false)
        {
            var uri = $"api/supporttickets/paged?pageNumber={pageNumber}&pageSize={pageSize}";
            if (!string.IsNullOrWhiteSpace(search)) uri += $"&search={Uri.EscapeDataString(search)}";
            if (!string.IsNullOrWhiteSpace(status)) uri += $"&status={Uri.EscapeDataString(status)}";
            if (!string.IsNullOrWhiteSpace(priority)) uri += $"&priority={Uri.EscapeDataString(priority)}";
            if (!string.IsNullOrWhiteSpace(category)) uri += $"&category={Uri.EscapeDataString(category)}";
            if (assignedToUserId.HasValue) uri += $"&assignedToUserId={assignedToUserId.Value}";
            if (!string.IsNullOrWhiteSpace(sortColumn)) uri += $"&sortColumn={Uri.EscapeDataString(sortColumn)}&sortAscending={sortAscending}";

            return await GetAsync<PagedResult<SupportTicket>>(uri) ?? new PagedResult<SupportTicket>();
        }

        public PagedResult<SupportTicket> GetPaged(
            int pageNumber = 1,
            int pageSize = 25,
            string? search = null,
            string? status = null,
            string? priority = null,
            string? category = null,
            int? assignedToUserId = null,
            string? sortColumn = null,
            bool sortAscending = false) =>
            GetPagedAsync(pageNumber, pageSize, search, status, priority, category, assignedToUserId, sortColumn, sortAscending).GetAwaiter().GetResult();

        public SupportTicket? GetById(int id) =>
            Get<SupportTicket>($"api/supporttickets/{id}");

        public bool CanUserViewTicket(SupportTicket ticket)
        {
            if (RbacService.HasFullOversight) return true;
            return ticket.AssignedToUserId == CurrentSession.UserId || ticket.RaisedByUserId == CurrentSession.UserId;
        }

        public bool CanUserEditTicket(SupportTicket ticket)
        {
            if (RbacService.HasFullOversight) return true;
            return ticket.AssignedToUserId == CurrentSession.UserId;
        }

        public SupportTicket Add(SupportTicket ticket)
        {
            var result = Post<SupportTicket, SupportTicket>("api/supporttickets", ticket);
            return result ?? ticket;
        }

        public void UpdateStatus(int ticketId, string newStatus, string? note = null) =>
            Put($"api/supporttickets/{ticketId}/status", new { status = newStatus, note });

        public void Reopen(int ticketId, string reason) =>
            Post($"api/supporttickets/{ticketId}/reopen", new { reason });

        public void AssignTo(int ticketId, int? newAgentId, string? notes = null) =>
            Put($"api/supporttickets/{ticketId}/assign", new { agentId = newAgentId, notes });

        public List<TicketComment> GetComments(int ticketId) =>
            Get<List<TicketComment>>($"api/supporttickets/{ticketId}/comments") ?? new();

        public TicketComment AddComment(int ticketId, string commentText, bool isInternal = true)
        {
            var result = Post<object, TicketComment>($"api/supporttickets/{ticketId}/comments", new { commentText, isInternal });
            return result ?? new TicketComment { TicketId = ticketId, CommentText = commentText, IsInternal = isInternal };
        }

        public async Task<SupportTicketKpiCounts> GetKpiCountsAsync() =>
            await GetAsync<SupportTicketKpiCounts>("api/supporttickets/kpi") ?? new SupportTicketKpiCounts();

        public SupportTicketKpiCounts GetKpiCounts() =>
            GetKpiCountsAsync().GetAwaiter().GetResult();

        public void SoftDelete(int ticketId) =>
            Delete($"api/supporttickets/{ticketId}");

        public List<SupportTicket> GetArchived() =>
            Get<List<SupportTicket>>("api/supporttickets/archived") ?? new();

        public void Restore(int ticketId) =>
            Post($"api/supporttickets/{ticketId}/restore", new { });

        public static DateTime CalculateSlaDueDate(string priority, DateTime referenceTime)
        {
            return (priority ?? string.Empty).ToLowerInvariant() switch
            {
                "urgent" => referenceTime.AddHours(4),
                "high" => referenceTime.AddHours(24),
                "medium" => referenceTime.AddHours(48),
                "low" => referenceTime.AddHours(72),
                _ => referenceTime.AddHours(48)
            };
        }

        public List<AgentPickerItem> GetAgents() =>
            Get<List<AgentPickerItem>>("api/supporttickets/agents") ?? new();

        public Dictionary<int, string> GetAgentDictionary() =>
            GetAgents().ToDictionary(a => a.UserId, a => a.FullName);

        public string? GetAssignedAgentName(int? assignedAgentId)
        {
            if (!assignedAgentId.HasValue) return null;
            var agents = GetAgents();
            return agents.FirstOrDefault(a => a.UserId == assignedAgentId.Value)?.FullName;
        }

        public List<Customer> GetCustomers() =>
            Get<List<Customer>>("api/supporttickets/customers") ?? new();

        public int GetOpenTicketsCount() =>
            Get<int>("api/supporttickets/open-count");

        public List<AdminTicketAttentionItemDto> GetTicketsNeedingAttention(int count = 3) =>
            Get<List<AdminTicketAttentionItemDto>>($"api/supporttickets/attention?count={count}") ?? new();
    }
}
