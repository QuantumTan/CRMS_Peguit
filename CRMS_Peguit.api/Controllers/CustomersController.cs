using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using CRMS_Peguit.domain.entities;
using CRMS_Peguit.domain.Common;
using CRMS_Peguit.infrastructure.data;

namespace CRMS_Peguit.api.Controllers
{
    public record AssignAgentRequest(int? AgentId, bool Approve = true, string? Notes = null);
    public record ApproveAssignmentRequest(string? Notes = null);
    public record LogCustomerActivityRequest(string? Subject, string? Notes);
    public record AgentPickerDto(int UserId, string FullName, string Email);
    public record CustomerKpiCountsDto(int Total, int Active, int Inactive, int FollowUp, int ThisMonth);

    [ApiController]
    [Route("api/[controller]")]
    public class CustomersController : ControllerBase
    {
        private readonly RealEstateDbContext _db;

        public CustomersController(RealEstateDbContext db)
        {
            _db = db;
        }

        private (int UserId, string Role, int TenantId) CurrentUser =>
            ApiSecurityHelper.GetCurrentUserInfo(HttpContext);

        [HttpGet]
        public async Task<IActionResult> GetAll(
            [FromQuery] int? page = null,
            [FromQuery] int? pageSize = null,
            [FromQuery] string? search = null,
            [FromQuery] string? status = null)
        {
            var user = CurrentUser;
            var query = _db.Customers
                .Include(c => c.Person)
                .Include(c => c.AssignedAgent)
                .Include(c => c.CreatedByUser)
                .Where(c => !c.IsDeleted)
                .AsQueryable();

            // Ownership-Based Access Control (R23, R25, R26):
            // Manager/Admin retain full oversight. Agent sees only assigned records or their own pending records.
            if (!ApiSecurityHelper.HasFullOversight(user.Role) && ApiSecurityHelper.IsAgent(user.Role) && user.UserId > 0)
            {
                query = query.Where(c =>
                    (c.AssignedAgentId.HasValue && c.AssignedAgentId.Value > 0)
                        ? c.AssignedAgentId.Value == user.UserId
                        : c.CreatedByUserId == user.UserId);
            }

            if (!string.IsNullOrWhiteSpace(search))
            {
                string s = search.Trim();
                query = query.Where(c => (c.Person != null && (
                    c.Person.FirstName.Contains(s) ||
                    c.Person.LastName.Contains(s) ||
                    (c.Person.MiddleName != null && c.Person.MiddleName.Contains(s)) ||
                    (c.Person.Suffix != null && c.Person.Suffix.Contains(s)) ||
                    (c.Person.Email != null && c.Person.Email.Contains(s)) ||
                    (c.Person.Phone != null && c.Person.Phone.Contains(s)))) ||
                    c.Type.Contains(s));
            }

            if (!string.IsNullOrWhiteSpace(status) && !status.Equals("All", StringComparison.OrdinalIgnoreCase))
            {
                if (string.Equals(status, "Active", StringComparison.OrdinalIgnoreCase))
                    query = query.Where(c => c.Status.ToLower() == "active");
                else if (string.Equals(status, "Inactive", StringComparison.OrdinalIgnoreCase))
                    query = query.Where(c => c.Status.ToLower() == "inactive");
                else if (string.Equals(status, "Follow Up", StringComparison.OrdinalIgnoreCase))
                    query = query.Where(c => c.Status.ToLower() == "prospect" || c.AssignmentStatus.ToLower() == "pending");
                else
                    query = query.Where(c => c.Status == status);
            }

            if (page.HasValue || pageSize.HasValue)
            {
                int pageNum = Math.Max(1, page.GetValueOrDefault(1));
                int size = Math.Max(1, pageSize.GetValueOrDefault(25));

                int totalCount = await query.CountAsync();
                var items = await query
                    .OrderBy(x => x.Person.LastName)
                    .ThenBy(x => x.Person.FirstName)
                    .Skip((pageNum - 1) * size)
                    .Take(size)
                    .ToListAsync();

                return Ok(new PagedResult<Customer>(items, totalCount, pageNum, size));
            }

            var all = await query
                .OrderBy(x => x.Person.LastName)
                .ThenBy(x => x.Person.FirstName)
                .ToListAsync();
            return Ok(all);
        }

        [HttpGet("{id:int}")]
        public async Task<IActionResult> GetById(int id)
        {
            var user = CurrentUser;
            var customer = await _db.Customers
                .Include(c => c.Person)
                .Include(c => c.AssignedAgent)
                .Include(c => c.CreatedByUser)
                .SingleOrDefaultAsync(x => x.CustomerId == id);

            if (customer is null) return NotFound();

            // OBAC check
            if (!ApiSecurityHelper.HasFullOversight(user.Role) && ApiSecurityHelper.IsAgent(user.Role) && user.UserId > 0)
            {
                bool allowed = (customer.AssignedAgentId.HasValue && customer.AssignedAgentId.Value > 0)
                    ? customer.AssignedAgentId.Value == user.UserId
                    : customer.CreatedByUserId == user.UserId;
                if (!allowed) return Forbid();
            }

            return Ok(customer);
        }

        [HttpPost]
        public async Task<IActionResult> Create(Customer customer)
        {
            var user = CurrentUser;
            if (customer.PersonId <= 0 && customer.Person == null)
            {
                customer.Person = new Person
                {
                    FirstName = customer.FirstName,
                    MiddleName = customer.MiddleName,
                    LastName = customer.LastName,
                    Suffix = customer.Suffix,
                    Email = customer.Email,
                    Phone = customer.Phone
                };
            }

            customer.CreatedAt = DateTime.UtcNow;
            customer.CreatedByUserId = user.UserId > 0 ? user.UserId : (customer.CreatedByUserId > 0 ? customer.CreatedByUserId : 1);
            customer.IsDeleted = false;
            customer.DeletedAt = null;

            if (ApiSecurityHelper.CanAssignRecords(user.Role))
            {
                if (customer.AssignedAgentId <= 0) customer.AssignedAgentId = null;
            }
            else
            {
                // R23: Default state is Unassigned - never auto-assigned to creator
                customer.AssignedAgentId = null;
                customer.AssignmentStatus = "pending_review";
            }

            _db.Customers.Add(customer);
            await _db.SaveChangesAsync();

            // Log activity
            LogActivityInternal("Customer Created", null, customer.CustomerId, $"Customer '{customer.FullName}' was created.", user.UserId);

            return CreatedAtAction(nameof(GetById), new { id = customer.CustomerId }, customer);
        }

        [HttpPut("{id:int}")]
        public async Task<IActionResult> Update(int id, Customer updated)
        {
            var user = CurrentUser;
            var item = await _db.Customers
                .Include(c => c.Person)
                .SingleOrDefaultAsync(x => x.CustomerId == id);
            if (item is null) return NotFound();

            // OBAC edit check
            if (!ApiSecurityHelper.HasFullOversight(user.Role) && ApiSecurityHelper.IsAgent(user.Role) && user.UserId > 0)
            {
                bool allowed = (item.AssignedAgentId.HasValue && item.AssignedAgentId.Value > 0)
                    ? item.AssignedAgentId.Value == user.UserId
                    : item.CreatedByUserId == user.UserId;
                if (!allowed) return Forbid();

                if (string.Equals(item.AssignmentStatus, "pending_review", StringComparison.OrdinalIgnoreCase))
                    return BadRequest("Editing is locked while record is pending management review.");
            }

            item.FirstName = updated.FirstName;
            item.MiddleName = updated.MiddleName;
            item.LastName = updated.LastName;
            item.Suffix = updated.Suffix;
            item.Phone = updated.Phone;
            item.Email = updated.Email;
            item.Type = updated.Type;
            item.Status = updated.Status;

            // R24: Only Manager or Admin may set or change ownership
            if (ApiSecurityHelper.CanAssignRecords(user.Role))
            {
                var oldAgentId = item.AssignedAgentId;
                var newAgentId = updated.AssignedAgentId <= 0 ? null : updated.AssignedAgentId;

                item.AssignedAgentId = newAgentId;
                item.AssignmentStatus = updated.AssignmentStatus;
                item.AssignmentReviewedByUserId = updated.AssignmentReviewedByUserId;
                item.AssignmentReviewedAt = updated.AssignmentReviewedAt;
                item.AssignmentReviewNotes = updated.AssignmentReviewNotes;

                if (oldAgentId != newAgentId)
                {
                    LogActivityInternal("Customer Assignment Changed", null, item.CustomerId,
                        $"Customer '{item.FullName}' assignment changed from Agent #{oldAgentId?.ToString() ?? "Unassigned"} to Agent #{newAgentId?.ToString() ?? "Unassigned"} by User #{user.UserId}.", user.UserId);
                }
            }

            await _db.SaveChangesAsync();
            LogActivityInternal("Customer Updated", null, item.CustomerId, $"Customer '{item.FullName}' was updated.", user.UserId);

            return Ok(item);
        }

        [HttpDelete("{id:int}")]
        public async Task<IActionResult> Delete(int id)
        {
            var user = CurrentUser;
            var item = await _db.Customers.SingleOrDefaultAsync(x => x.CustomerId == id);
            if (item is null) return NotFound();

            if (!ApiSecurityHelper.HasFullOversight(user.Role) && ApiSecurityHelper.IsAgent(user.Role) && user.UserId > 0)
            {
                bool allowed = (item.AssignedAgentId.HasValue && item.AssignedAgentId.Value > 0)
                    ? item.AssignedAgentId.Value == user.UserId
                    : item.CreatedByUserId == user.UserId;
                if (!allowed) return Forbid();
            }

            item.IsDeleted = true;
            item.DeletedAt = DateTime.UtcNow;
            await _db.SaveChangesAsync();

            LogActivityInternal("Customer Archived", null, item.CustomerId, $"Customer '{item.FullName}' was archived.", user.UserId);
            return NoContent();
        }

        [HttpPost("{id:int}/restore")]
        public async Task<IActionResult> Restore(int id)
        {
            var user = CurrentUser;
            var item = await _db.Customers.IgnoreQueryFilters().SingleOrDefaultAsync(x => x.CustomerId == id);
            if (item is null) return NotFound();

            item.IsDeleted = false;
            item.DeletedAt = null;
            await _db.SaveChangesAsync();

            LogActivityInternal("Customer Restored", null, item.CustomerId, $"Customer '{item.FullName}' was restored from archive.", user.UserId);
            return Ok(item);
        }

        [HttpGet("archived")]
        public async Task<IActionResult> GetArchived()
        {
            var items = await _db.Customers
                .IgnoreQueryFilters()
                .Include(c => c.Person)
                .Where(c => c.IsDeleted)
                .OrderByDescending(c => c.DeletedAt)
                .ToListAsync();
            return Ok(items);
        }

        [HttpGet("{id:int}/owned-properties")]
        public async Task<IActionResult> GetOwnedProperties(int id)
        {
            var properties = await _db.Properties
                .AsNoTracking()
                .Where(p => p.OwnerCustomerId == id)
                .OrderByDescending(p => p.CreatedAt)
                .ToListAsync();
            return Ok(properties);
        }

        [HttpGet("{id:int}/activities")]
        public async Task<IActionResult> GetActivityHistory(int id)
        {
            var activities = await _db.Activities
                .AsNoTracking()
                .Where(a => a.RelatedCustomerId == id)
                .OrderByDescending(a => a.ActivityDate)
                .ToListAsync();
            return Ok(activities);
        }

        [HttpPost("{id:int}/assign")]
        public async Task<IActionResult> AssignAgent(int id, [FromBody] AssignAgentRequest request)
        {
            var user = CurrentUser;
            if (!ApiSecurityHelper.CanAssignRecords(user.Role))
                return StatusCode(StatusCodes.Status403Forbidden, "Only Manager or Admin may assign records.");

            var item = await _db.Customers.SingleOrDefaultAsync(x => x.CustomerId == id);
            if (item is null) return NotFound();

            var oldAgentId = item.AssignedAgentId;
            var newAgentId = request.AgentId <= 0 ? null : request.AgentId;

            if (newAgentId.HasValue && !await _db.Users.AnyAsync(u => u.UserId == newAgentId.Value))
            {
                newAgentId = null;
            }

            item.AssignedAgentId = newAgentId;
            item.AssignmentStatus = request.Approve ? "approved" : "pending_review";
            item.AssignmentReviewedByUserId = user.UserId > 0 ? user.UserId : null;
            item.AssignmentReviewedAt = DateTime.UtcNow;
            item.AssignmentReviewNotes = string.IsNullOrWhiteSpace(request.Notes) ? null : request.Notes.Trim();

            await _db.SaveChangesAsync();

            if (oldAgentId != newAgentId)
            {
                LogActivityInternal("Customer Assignment Changed", null, item.CustomerId,
                    $"Customer '{item.FullName}' assigned to Agent #{newAgentId?.ToString() ?? "Unassigned"} by User #{user.UserId}.", user.UserId);
            }

            return Ok(item);
        }

        [HttpPost("{id:int}/approve")]
        public async Task<IActionResult> ApproveAssignment(int id, [FromBody] ApproveAssignmentRequest request)
        {
            var user = CurrentUser;
            if (!ApiSecurityHelper.CanAssignRecords(user.Role))
                return StatusCode(StatusCodes.Status403Forbidden, "Only Manager or Admin may approve assignments.");

            var item = await _db.Customers.SingleOrDefaultAsync(x => x.CustomerId == id);
            if (item is null) return NotFound();

            item.AssignmentStatus = "approved";
            item.AssignmentReviewedByUserId = user.UserId > 0 ? user.UserId : null;
            item.AssignmentReviewedAt = DateTime.UtcNow;
            item.AssignmentReviewNotes = string.IsNullOrWhiteSpace(request.Notes) ? null : request.Notes.Trim();

            await _db.SaveChangesAsync();
            LogActivityInternal("Customer Assignment Approved", null, item.CustomerId, $"Assignment for '{item.FullName}' was approved.", user.UserId);

            return Ok(item);
        }

        [HttpGet("pending-review")]
        public async Task<IActionResult> GetPendingReview()
        {
            var items = await _db.Customers
                .AsNoTracking()
                .Include(c => c.Person)
                .Where(c => !c.IsDeleted && (c.AssignmentStatus == "pending_review" || c.AssignedAgentId == null))
                .OrderByDescending(c => c.CreatedAt)
                .ToListAsync();
            return Ok(items);
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
                .Include(u => u.Person)
                .Where(u => agentRoleIds.Contains(u.RoleId) && u.Status.ToLower() != "inactive")
                .OrderBy(u => u.Person.LastName)
                .ThenBy(u => u.Person.FirstName)
                .Select(u => new AgentPickerDto(u.UserId, u.FullName, u.Email))
                .ToListAsync();

            return Ok(agents);
        }

        [HttpGet("agent-dict")]
        public async Task<IActionResult> GetAgentDictionary()
        {
            var dict = await _db.Users
                .AsNoTracking()
                .Include(u => u.Person)
                .ToDictionaryAsync(u => u.UserId, u => u.FullName);
            return Ok(dict);
        }

        [HttpGet("kpi-counts")]
        public async Task<IActionResult> GetKpiCounts()
        {
            var user = CurrentUser;
            var query = _db.Customers.AsNoTracking().Where(c => !c.IsDeleted);

            if (!ApiSecurityHelper.HasFullOversight(user.Role) && ApiSecurityHelper.IsAgent(user.Role) && user.UserId > 0)
            {
                query = query.Where(c =>
                    (c.AssignedAgentId.HasValue && c.AssignedAgentId.Value > 0)
                        ? c.AssignedAgentId.Value == user.UserId
                        : c.CreatedByUserId == user.UserId);
            }

            var now = DateTime.UtcNow;
            int total = await query.CountAsync();
            int active = await query.CountAsync(c => c.Status.ToLower() == "active");
            int inactive = await query.CountAsync(c => c.Status.ToLower() == "inactive");
            int followUp = await query.CountAsync(c => c.Status.ToLower() == "prospect" || c.AssignmentStatus.ToLower() == "pending");
            int thisMonth = await query.CountAsync(c => c.CreatedAt.Year == now.Year && c.CreatedAt.Month == now.Month);

            return Ok(new CustomerKpiCountsDto(total, active, inactive, followUp, thisMonth));
        }

        [HttpPost("{id:int}/log-email")]
        public async Task<IActionResult> LogEmail(int id, [FromBody] LogCustomerActivityRequest req)
        {
            var user = CurrentUser;
            var customer = await _db.Customers.Include(c => c.Person).SingleOrDefaultAsync(c => c.CustomerId == id);
            if (customer == null) return NotFound();

            LogActivityInternal("Email", null, id, $"Email sent to '{customer.FullName}'. Subject: {req.Subject}", user.UserId);
            return Ok(new { success = true });
        }

        [HttpPost("{id:int}/log-call")]
        public async Task<IActionResult> LogCall(int id, [FromBody] LogCustomerActivityRequest req)
        {
            var user = CurrentUser;
            var customer = await _db.Customers.Include(c => c.Person).SingleOrDefaultAsync(c => c.CustomerId == id);
            if (customer == null) return NotFound();

            LogActivityInternal("Call", null, id, $"Call logged for '{customer.FullName}': {req.Notes}", user.UserId);
            return Ok(new { success = true });
        }

        [HttpPost("{id:int}/log-meeting")]
        public async Task<IActionResult> LogMeeting(int id, [FromBody] LogCustomerActivityRequest req)
        {
            var user = CurrentUser;
            var customer = await _db.Customers.Include(c => c.Person).SingleOrDefaultAsync(c => c.CustomerId == id);
            if (customer == null) return NotFound();

            LogActivityInternal("Meeting", null, id, $"Meeting held with '{customer.FullName}': {req.Notes}", user.UserId);
            return Ok(new { success = true });
        }

        private void LogActivityInternal(string type, int? leadId, int? customerId, string notes, int agentId)
        {
            try
            {
                if (agentId <= 0) agentId = 1;
                _db.Activities.Add(new Activity
                {
                    Type = type,
                    RelatedLeadId = leadId,
                    RelatedCustomerId = customerId,
                    LoggedByAgentId = agentId,
                    Notes = notes,
                    ActivityDate = DateTime.UtcNow
                });
                _db.SaveChanges();
            }
            catch { }
        }
    }
}
