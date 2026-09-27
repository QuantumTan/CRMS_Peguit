using System;
using System.Collections.Generic;
using System.Linq;
using CRMS_Peguit.domain.entities;
using CRMS_Peguit.winforms.Models.ViewModels;

namespace CRMS_Peguit.winforms.Controllers
{
    public class ApprovalController : IDisposable
    {
        private readonly LeadController _leadController;
        private readonly CustomerController _customerController;
        private readonly PropertyController _propertyController;

        public LeadController LeadController => _leadController;
        public CustomerController CustomerController => _customerController;
        public PropertyController PropertyController => _propertyController;

        public ApprovalController()
        {
            _leadController = new LeadController();
            _customerController = new CustomerController();
            _propertyController = new PropertyController();
        }

        public List<PendingApprovalItem> GetPendingApprovals()
        {
            var items = new List<PendingApprovalItem>();

            // 1. Pending Leads
            var leads = _leadController.GetPendingReview();
            foreach (var lead in leads)
            {
                string submitter = GetUserName(lead.CreatedByUserId);
                string assigned = _leadController.GetAssignedAgentName(lead.AssignedAgentId) ?? "Unassigned";

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

            // 2. Pending Customers
            var customers = _customerController.GetPendingReview();
            foreach (var cust in customers)
            {
                string submitter = GetUserName(cust.CreatedByUserId);
                string assigned = _customerController.GetAssignedAgentName(cust.AssignedAgentId) ?? "Unassigned";

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

            // 3. Pending Properties
            var properties = _propertyController.GetPendingReview();
            foreach (var prop in properties)
            {
                string submitter = GetUserName(prop.CreatedByUserId);
                string assigned = _propertyController.GetListedAgentName(prop.ListedByAgentId) ?? "Unassigned";

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

        public List<AgentPickerItem> GetAgents()
        {
            return _leadController.GetAgents();
        }

        public void AssignAgent(PendingApprovalItem item, int? agentId, bool approveNow, string? notes)
        {
            if (item.Type == "Lead" && item.OriginalEntity is Lead lead)
            {
                _leadController.AssignAgent(lead, agentId, approveNow, notes);
            }
            else if (item.Type == "Customer" && item.OriginalEntity is Customer cust)
            {
                _customerController.AssignAgent(cust, agentId, approveNow, notes);
            }
            else if (item.Type == "Property" && item.OriginalEntity is Property prop)
            {
                _propertyController.AssignAgent(prop, agentId, approveNow, notes);
            }
        }

        public void ApproveAssignment(PendingApprovalItem item, string? notes = null)
        {
            if (item.Type == "Lead" && item.OriginalEntity is Lead lead)
            {
                _leadController.ApproveAssignment(lead, notes);
            }
            else if (item.Type == "Customer" && item.OriginalEntity is Customer cust)
            {
                _customerController.ApproveAssignment(cust, notes);
            }
            else if (item.Type == "Property" && item.OriginalEntity is Property prop)
            {
                _propertyController.ApproveAssignment(prop, notes);
            }
        }

        private string GetUserName(int? userId)
        {
            if (!userId.HasValue || userId.Value <= 0) return "—";
            return _customerController.GetAssignedAgentName(userId.Value) ?? $"User #{userId.Value}";
        }

        public void Dispose()
        {
            _leadController.Dispose();
            _customerController.Dispose();
            _propertyController.Dispose();
        }
    }
}