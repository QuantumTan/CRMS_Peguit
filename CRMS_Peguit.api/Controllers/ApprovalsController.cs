using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using CRMS_Peguit.domain.entities;
using CRMS_Peguit.infrastructure.data;

namespace CRMS_Peguit.api.Controllers
{
    public record PendingApprovalDto(
        int Id,
        string Type,
        string Title,
        string SubmitterName,
        DateTime CreatedAt,
        string AssignedTo,
        int? AssignedAgentId,
        string Status);

    public record ApprovalAssignRequest(string Type, int Id, int? AgentId, bool ApproveNow, string? Notes);
    public record ApprovalActionRequest(string Type, int Id, string? Notes);

    [ApiController]
    [Route("api/[controller]")]
    public class ApprovalsController : ControllerBase
    {
        private readonly RealEstateDbContext _db;

        public ApprovalsController(RealEstateDbContext db)
        {
            _db = db;
        }

        private (int UserId, string Role, int TenantId) CurrentUser =>
            ApiSecurityHelper.GetCurrentUserInfo(HttpContext);

        [HttpGet("pending")]
        public async Task<IActionResult> GetPendingApprovals()
        {
            var user = CurrentUser;
            if (!ApiSecurityHelper.HasFullOversight(user.Role))
            {
                return StatusCode(StatusCodes.Status403Forbidden, "Only Manager or Admin can access approvals.");
            }

            var items = new List<PendingApprovalDto>();

            // 1. Leads
            var leads = await _db.Leads
                .AsNoTracking()
                .Include(l => l.AssignedAgent)
                .Include(l => l.CreatedByUser)
                .Where(l => !l.IsDeleted && (l.AssignmentStatus == "pending_review" || l.AssignedAgentId == null))
                .OrderByDescending(l => l.CreatedAt)
                .ToListAsync();

            foreach (var l in leads)
            {
                items.Add(new PendingApprovalDto(
                    l.LeadId,
                    "Lead",
                    l.FullName,
                    l.CreatedByUser?.FullName ?? $"User #{l.CreatedByUserId}",
                    l.CreatedAt,
                    l.AssignedAgent?.FullName ?? "Unassigned",
                    l.AssignedAgentId,
                    "PENDING REVIEW"));
            }

            // 2. Customers
            var customers = await _db.Customers
                .AsNoTracking()
                .Include(c => c.AssignedAgent)
                .Include(c => c.CreatedByUser)
                .Where(c => !c.IsDeleted && (c.AssignmentStatus == "pending_review" || c.AssignedAgentId == null))
                .OrderByDescending(c => c.CreatedAt)
                .ToListAsync();

            foreach (var c in customers)
            {
                items.Add(new PendingApprovalDto(
                    c.CustomerId,
                    "Customer",
                    c.FullName,
                    c.CreatedByUser?.FullName ?? $"User #{c.CreatedByUserId}",
                    c.CreatedAt,
                    c.AssignedAgent?.FullName ?? "Unassigned",
                    c.AssignedAgentId,
                    "PENDING REVIEW"));
            }

            // 3. Properties
            var properties = await _db.Properties
                .AsNoTracking()
                .Include(p => p.ListedByAgent)
                .Include(p => p.CreatedByUser)
                .Where(p => p.AssignmentStatus == "pending_review" || p.ListedByAgentId == null)
                .OrderByDescending(p => p.CreatedAt)
                .ToListAsync();

            foreach (var p in properties)
            {
                items.Add(new PendingApprovalDto(
                    p.PropertyId,
                    "Property",
                    p.Address,
                    p.CreatedByUser?.FullName ?? $"User #{p.CreatedByUserId}",
                    p.CreatedAt,
                    p.ListedByAgent?.FullName ?? "Unassigned",
                    p.ListedByAgentId,
                    "PENDING REVIEW"));
            }

            return Ok(items.OrderByDescending(x => x.CreatedAt).ToList());
        }

        [HttpGet("agents")]
        public async Task<IActionResult> GetAgents()
        {
            var agentRoleIds = await _db.Roles
                .AsNoTracking()
                .Where(r => r.RoleName.ToLower() == "agent" || r.RoleName.ToLower() == "salesstaff")
                .Select(r => r.RoleId)
                .ToListAsync();

            var agents = await _db.Users
                .AsNoTracking()
                .Where(u => agentRoleIds.Contains(u.RoleId) && u.Status.ToLower() != "inactive")
                .OrderBy(u => u.LastName)
                .ThenBy(u => u.FirstName)
                .Select(u => new AgentPickerDto(u.UserId, u.FullName, u.Email))
                .ToListAsync();

            return Ok(agents);
        }

        [HttpPost("assign")]
        public async Task<IActionResult> AssignAgent([FromBody] ApprovalAssignRequest req)
        {
            var user = CurrentUser;
            if (!ApiSecurityHelper.CanAssignRecords(user.Role))
                return StatusCode(StatusCodes.Status403Forbidden, "Only Manager or Admin may assign records.");

            var newAgentId = req.AgentId <= 0 ? null : req.AgentId;
            if (newAgentId.HasValue && !await _db.Users.AnyAsync(u => u.UserId == newAgentId.Value))
            {
                newAgentId = null;
            }

            if (req.Type.Equals("Lead", StringComparison.OrdinalIgnoreCase))
            {
                var item = await _db.Leads.FindAsync(req.Id);
                if (item == null) return NotFound();
                item.AssignedAgentId = newAgentId;
                item.AssignmentStatus = req.ApproveNow ? "approved" : "pending_review";
                item.AssignmentReviewedByUserId = user.UserId > 0 ? user.UserId : null;
                item.AssignmentReviewedAt = DateTime.UtcNow;
                item.AssignmentReviewNotes = req.Notes;
            }
            else if (req.Type.Equals("Customer", StringComparison.OrdinalIgnoreCase))
            {
                var item = await _db.Customers.FindAsync(req.Id);
                if (item == null) return NotFound();
                item.AssignedAgentId = newAgentId;
                item.AssignmentStatus = req.ApproveNow ? "approved" : "pending_review";
                item.AssignmentReviewedByUserId = user.UserId > 0 ? user.UserId : null;
                item.AssignmentReviewedAt = DateTime.UtcNow;
                item.AssignmentReviewNotes = req.Notes;
            }
            else if (req.Type.Equals("Property", StringComparison.OrdinalIgnoreCase))
            {
                var item = await _db.Properties.FindAsync(req.Id);
                if (item == null) return NotFound();
                item.ListedByAgentId = newAgentId;
                item.AssignmentStatus = req.ApproveNow ? "approved" : "pending_review";
                item.AssignmentReviewedByUserId = user.UserId > 0 ? user.UserId : null;
                item.AssignmentReviewedAt = DateTime.UtcNow;
                item.AssignmentReviewNotes = req.Notes;
            }

            await _db.SaveChangesAsync();
            return Ok(new { success = true });
        }

        [HttpPost("approve")]
        public async Task<IActionResult> ApproveAssignment([FromBody] ApprovalActionRequest req)
        {
            var user = CurrentUser;
            if (!ApiSecurityHelper.CanAssignRecords(user.Role))
                return StatusCode(StatusCodes.Status403Forbidden, "Only Manager or Admin may approve assignments.");

            if (req.Type.Equals("Lead", StringComparison.OrdinalIgnoreCase))
            {
                var item = await _db.Leads.FindAsync(req.Id);
                if (item == null) return NotFound();
                item.AssignmentStatus = "approved";
                item.AssignmentReviewedByUserId = user.UserId > 0 ? user.UserId : null;
                item.AssignmentReviewedAt = DateTime.UtcNow;
                item.AssignmentReviewNotes = req.Notes;
            }
            else if (req.Type.Equals("Customer", StringComparison.OrdinalIgnoreCase))
            {
                var item = await _db.Customers.FindAsync(req.Id);
                if (item == null) return NotFound();
                item.AssignmentStatus = "approved";
                item.AssignmentReviewedByUserId = user.UserId > 0 ? user.UserId : null;
                item.AssignmentReviewedAt = DateTime.UtcNow;
                item.AssignmentReviewNotes = req.Notes;
            }
            else if (req.Type.Equals("Property", StringComparison.OrdinalIgnoreCase))
            {
                var item = await _db.Properties.FindAsync(req.Id);
                if (item == null) return NotFound();
                item.AssignmentStatus = "approved";
                item.AssignmentReviewedByUserId = user.UserId > 0 ? user.UserId : null;
                item.AssignmentReviewedAt = DateTime.UtcNow;
                item.AssignmentReviewNotes = req.Notes;
            }

            await _db.SaveChangesAsync();
            return Ok(new { success = true });
        }
    }
}
