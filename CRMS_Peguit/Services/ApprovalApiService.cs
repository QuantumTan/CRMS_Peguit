using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using CRMS_Peguit.domain.Common;
using CRMS_Peguit.domain.entities;
using CRMS_Peguit.winforms.Models.ViewModels;

namespace CRMS_Peguit.winforms.Services
{
    public class ApprovalApiService : BaseApiService
    {
        private CustomerApiService? _customerService;
        private LeadApiService? _leadService;
        private PropertyApiService? _propertyService;

        public CustomerApiService CustomerService => _customerService ??= new CustomerApiService(httpClient: Client);
        public LeadApiService LeadService => _leadService ??= new LeadApiService(httpClient: Client);
        public PropertyApiService PropertyService => _propertyService ??= new PropertyApiService(httpClient: Client);

        // Aliases for compatibility
        public CustomerApiService CustomerController => CustomerService;
        public LeadApiService LeadController => LeadService;
        public PropertyApiService PropertyController => PropertyService;

        public ApprovalApiService(string? baseUrl = null, HttpClient? httpClient = null)
            : base(baseUrl, httpClient)
        {
        }

        public List<PendingApprovalItem> GetPendingApprovals()
        {
            var items = new List<PendingApprovalItem>();

            var leads = LeadService.GetPendingReview();
            foreach (var lead in leads)
            {
                string submitter = GetUserName(lead.CreatedByUserId);
                string assigned = LeadService.GetAssignedAgentName(lead.AssignedAgentId) ?? "Unassigned";

                items.Add(new PendingApprovalItem
                {
                    Id = lead.LeadId,
                    Type = "Lead",
                    Title = lead.FullName,
                    SubmitterName = submitter,
                    CreatedAt = lead.CreatedAt,
                    AssignedTo = assigned,
                    AssignedAgentId = lead.AssignedAgentId,
                    Status = "PENDING REVIEW",
                    OriginalEntity = lead
                });
            }

            var customers = CustomerService.GetPendingReview();
            foreach (var cust in customers)
            {
                string submitter = GetUserName(cust.CreatedByUserId);
                string assigned = CustomerService.GetAssignedAgentName(cust.AssignedAgentId) ?? "Unassigned";

                items.Add(new PendingApprovalItem
                {
                    Id = cust.CustomerId,
                    Type = "Customer",
                    Title = cust.FullName,
                    SubmitterName = submitter,
                    CreatedAt = cust.CreatedAt,
                    AssignedTo = assigned,
                    AssignedAgentId = cust.AssignedAgentId,
                    Status = "PENDING REVIEW",
                    OriginalEntity = cust
                });
            }

            var properties = PropertyService.GetPendingReview();
            foreach (var prop in properties)
            {
                string submitter = GetUserName(prop.CreatedByUserId);
                string assigned = PropertyService.GetListedAgentName(prop.ListedByAgentId) ?? "Unassigned";

                items.Add(new PendingApprovalItem
                {
                    Id = prop.PropertyId,
                    Type = "Property",
                    Title = prop.Address,
                    SubmitterName = submitter,
                    CreatedAt = prop.CreatedAt,
                    AssignedTo = assigned,
                    AssignedAgentId = prop.ListedByAgentId,
                    Status = "PENDING REVIEW",
                    OriginalEntity = prop
                });
            }

            return items.OrderByDescending(x => x.CreatedAt).ToList();
        }

        public List<AgentPickerItem> GetAgents() => LeadService.GetAgents();

        public void AssignAgent(PendingApprovalItem item, int? agentId, bool approveNow, string? notes)
        {
            if (item.Type == "Lead" && item.OriginalEntity is Lead lead)
            {
                LeadService.AssignAgent(lead, agentId, approveNow, notes);
            }
            else if (item.Type == "Customer" && item.OriginalEntity is Customer cust)
            {
                CustomerService.AssignAgent(cust, agentId, approveNow, notes);
            }
            else if (item.Type == "Property" && item.OriginalEntity is Property prop)
            {
                PropertyService.AssignAgent(prop, agentId, approveNow, notes);
            }
        }

        public void ApproveAssignment(PendingApprovalItem item, string? notes = null)
        {
            if (item.Type == "Lead" && item.OriginalEntity is Lead lead)
            {
                LeadService.ApproveAssignment(lead, notes);
            }
            else if (item.Type == "Customer" && item.OriginalEntity is Customer cust)
            {
                CustomerService.ApproveAssignment(cust, notes);
            }
            else if (item.Type == "Property" && item.OriginalEntity is Property prop)
            {
                PropertyService.ApproveAssignment(prop, notes);
            }
        }

        private string GetUserName(int? userId)
        {
            if (!userId.HasValue || userId.Value <= 0) return "—";
            return CustomerService.GetAssignedAgentName(userId.Value) ?? $"User #{userId.Value}";
        }

        public override void Dispose()
        {
            base.Dispose();
            _customerService?.Dispose();
            _leadService?.Dispose();
            _propertyService?.Dispose();
        }
    }
}
