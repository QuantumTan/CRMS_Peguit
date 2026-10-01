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
    public record LeadStageCountsDto(int Total, int New, int Contacted, int Qualified, int Converted);

    [ApiController]
    [Route("api/[controller]")]
    public class LeadsController : ControllerBase
    {
        private readonly RealEstateDbContext _db;

        public LeadsController(RealEstateDbContext db)
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
            [FromQuery] string? stage = null)
        {
            var user = CurrentUser;
            var query = _db.Leads
                .Include(l => l.AssignedAgent)
                .Include(l => l.CreatedByUser)
                .Where(l => !l.IsDeleted)
                .AsQueryable();

            // Ownership-Based Access Control (R23, R25, R26):
            if (!ApiSecurityHelper.HasFullOversight(user.Role) && ApiSecurityHelper.IsAgent(user.Role) && user.UserId > 0)
            {
                query = query.Where(l =>
                    (l.AssignedAgentId.HasValue && l.AssignedAgentId.Value > 0)
                        ? l.AssignedAgentId.Value == user.UserId
                        : l.CreatedByUserId == user.UserId);
            }

            if (!string.IsNullOrWhiteSpace(search))
            {
                string s = search.Trim();
                query = query.Where(l =>
                    l.FirstName.Contains(s) ||
                    l.LastName.Contains(s) ||
                    (l.MiddleName != null && l.MiddleName.Contains(s)) ||
                    (l.Suffix != null && l.Suffix.Contains(s)) ||
                    (l.Email != null && l.Email.Contains(s)) ||
                    (l.Phone != null && l.Phone.Contains(s)) ||
                    (l.Source != null && l.Source.Contains(s)) ||
                    (l.Notes != null && l.Notes.Contains(s)));
            }

            if (!string.IsNullOrWhiteSpace(stage) && !stage.Equals("All", StringComparison.OrdinalIgnoreCase))
            {
                query = query.Where(l => l.Stage.ToLower() == stage.ToLower());
            }

            if (page.HasValue || pageSize.HasValue)
            {
                int pageNum = Math.Max(1, page.GetValueOrDefault(1));
                int size = Math.Clamp(pageSize.GetValueOrDefault(25), 1, 100);

                int totalCount = await query.CountAsync();
                var items = await query.OrderByDescending(l => l.CreatedAt)
                    .Skip((pageNum - 1) * size)
                    .Take(size)
                    .ToListAsync();

                return Ok(new PagedResult<Lead>(items, totalCount, pageNum, size));
            }

            var leads = await query.OrderByDescending(l => l.CreatedAt).ToListAsync();
            return Ok(leads);
        }

        [HttpGet("stage-counts")]
        public async Task<IActionResult> GetStageCounts()
        {
            var user = CurrentUser;
            var query = _db.Leads.AsNoTracking().Where(l => !l.IsDeleted);

            if (!ApiSecurityHelper.HasFullOversight(user.Role) && ApiSecurityHelper.IsAgent(user.Role) && user.UserId > 0)
            {
                query = query.Where(l =>
                    (l.AssignedAgentId.HasValue && l.AssignedAgentId.Value > 0)
                        ? l.AssignedAgentId.Value == user.UserId
                        : l.CreatedByUserId == user.UserId);
            }

            int total = await query.CountAsync();
            int newCount = await query.CountAsync(l => l.Stage.ToLower() == "new");
            int contacted = await query.CountAsync(l => l.Stage.ToLower() == "contacted");
            int qualified = await query.CountAsync(l => l.Stage.ToLower() == "qualified");
            int converted = await query.CountAsync(l => l.Stage.ToLower() == "converted");

            return Ok(new LeadStageCountsDto(total, newCount, contacted, qualified, converted));
        }

        [HttpGet("{id:int}")]
        public async Task<IActionResult> GetById(int id)
        {
            var user = CurrentUser;
            var lead = await _db.Leads
                .Include(l => l.AssignedAgent)
                .Include(l => l.CreatedByUser)
                .SingleOrDefaultAsync(x => x.LeadId == id);

            if (lead is null) return NotFound();

            if (!ApiSecurityHelper.HasFullOversight(user.Role) && ApiSecurityHelper.IsAgent(user.Role) && user.UserId > 0)
            {
                bool allowed = (lead.AssignedAgentId.HasValue && lead.AssignedAgentId.Value > 0)
                    ? lead.AssignedAgentId.Value == user.UserId
                    : lead.CreatedByUserId == user.UserId;
                if (!allowed) return Forbid();
            }

            return Ok(lead);
        }

        [HttpPost]
        public async Task<IActionResult> Create(Lead lead)
        {
            var user = CurrentUser;
            if (user.UserId <= 0) return Unauthorized();

            lead.CreatedAt = DateTime.UtcNow;
            lead.CreatedByUserId = user.UserId;
            lead.IsDeleted = false;
            lead.DeletedAt = null;

            if (ApiSecurityHelper.CanAssignRecords(user.Role))
            {
                if (lead.AssignedAgentId <= 0) lead.AssignedAgentId = null;
            }
            else
            {
                lead.AssignedAgentId = null;
                lead.AssignmentStatus = "pending_review";
            }

            _db.Leads.Add(lead);
            await _db.SaveChangesAsync();

            await LogActivityInternalAsync("Lead Created", lead.LeadId, null, $"Lead '{lead.FullName}' was created.", user.UserId);

            return CreatedAtAction(nameof(GetById), new { id = lead.LeadId }, lead);
        }

        [HttpPut("{id:int}")]
        public async Task<IActionResult> Update(int id, Lead updated)
        {
            var user = CurrentUser;
            var item = await _db.Leads
                .SingleOrDefaultAsync(x => x.LeadId == id);
            if (item is null) return NotFound();

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
            item.Source = updated.Source;
            item.Stage = updated.Stage;
            item.Notes = updated.Notes;
            item.Priority = updated.Priority;
            item.ExpectedValue = updated.ExpectedValue;

            if (ApiSecurityHelper.CanAssignRecords(user.Role))
            {
                var oldAgentId = item.AssignedAgentId;
                var newAgentId = updated.AssignedAgentId <= 0 ? null : updated.AssignedAgentId;

                item.AssignedAgentId = newAgentId;
                if (!string.IsNullOrWhiteSpace(updated.AssignmentStatus) && updated.AssignmentStatus != item.AssignmentStatus)
                {
                    item.AssignmentStatus = updated.AssignmentStatus;
                    item.AssignmentReviewedByUserId = user.UserId > 0 ? user.UserId : null;
                    item.AssignmentReviewedAt = DateTime.UtcNow;
                    item.AssignmentReviewNotes = updated.AssignmentReviewNotes;
                }

                if (oldAgentId != newAgentId)
                {
                    await LogActivityInternalAsync("Lead Assignment Changed", item.LeadId, null,
                        $"Lead '{item.FullName}' assignment changed from Agent #{oldAgentId?.ToString() ?? "Unassigned"} to Agent #{newAgentId?.ToString() ?? "Unassigned"} by User #{user.UserId}.", user.UserId);
                }
            }

            await _db.SaveChangesAsync();
            await LogActivityInternalAsync("Lead Updated", item.LeadId, null, $"Lead '{item.FullName}' was updated.", user.UserId);

            return Ok(item);
        }

        [HttpPost("{id:int}/convert")]
        public async Task<IActionResult> ConvertToCustomer(int id)
        {
            var user = CurrentUser;
            var item = await _db.Leads.SingleOrDefaultAsync(x => x.LeadId == id);
            if (item is null) return NotFound();

            if (!ApiSecurityHelper.HasFullOversight(user.Role) && ApiSecurityHelper.IsAgent(user.Role) && user.UserId > 0)
            {
                bool allowed = (item.AssignedAgentId.HasValue && item.AssignedAgentId.Value > 0)
                    ? item.AssignedAgentId.Value == user.UserId
                    : item.CreatedByUserId == user.UserId;
                if (!allowed) return Forbid();
            }

            if (string.Equals(item.Stage, "converted", StringComparison.OrdinalIgnoreCase))
                return BadRequest("This lead has already been converted.");

            var customer = new Customer
            {
                FirstName = item.FirstName,
                MiddleName = item.MiddleName,
                LastName = item.LastName,
                Suffix = item.Suffix,
                Email = item.Email,
                Phone = item.Phone,
                Type = "buyer",
                Status = "active",
                AssignedAgentId = item.AssignedAgentId,
                CreatedByUserId = item.CreatedByUserId,
                AssignmentStatus = item.AssignmentStatus,
                AssignmentReviewedByUserId = item.AssignmentReviewedByUserId,
                AssignmentReviewedAt = item.AssignmentReviewedAt,
                AssignmentReviewNotes = item.AssignmentReviewNotes,
                CreatedAt = DateTime.UtcNow,
                IsDeleted = false,
                DeletedAt = null
            };

            var strategy = _db.Database.CreateExecutionStrategy();
            await strategy.ExecuteAsync(async () =>
            {
                using var tx = await _db.Database.BeginTransactionAsync();
                try
                {
                    _db.Customers.Add(customer);
                    await _db.SaveChangesAsync();

                    item.Stage = "converted";
                    item.ConvertedCustomerId = customer.CustomerId;
                    await _db.SaveChangesAsync();

                    await tx.CommitAsync();
                }
                catch
                {
                    await tx.RollbackAsync();
                    throw;
                }
            });

            await LogActivityInternalAsync("Lead Converted", item.LeadId, customer.CustomerId, $"Lead '{item.FullName}' converted to Customer.", user.UserId);

            return Ok(customer);
        }

        [HttpPost("{id:int}/mark-lost")]
        public async Task<IActionResult> MarkLost(int id)
        {
            var user = CurrentUser;
            var item = await _db.Leads.SingleOrDefaultAsync(x => x.LeadId == id);
            if (item is null) return NotFound();

            if (!ApiSecurityHelper.HasFullOversight(user.Role) && ApiSecurityHelper.IsAgent(user.Role) && user.UserId > 0)
            {
                bool allowed = (item.AssignedAgentId.HasValue && item.AssignedAgentId.Value > 0)
                    ? item.AssignedAgentId.Value == user.UserId
                    : item.CreatedByUserId == user.UserId;
                if (!allowed) return Forbid();
            }

            item.Stage = "lost";
            await _db.SaveChangesAsync();
            await LogActivityInternalAsync("Lead Marked Lost", item.LeadId, null, $"Lead '{item.FullName}' was marked as lost.", user.UserId);
            return Ok(item);
        }

        [HttpPost("{id:int}/restore-lost")]
        public async Task<IActionResult> RestoreFromLost(int id)
        {
            var user = CurrentUser;
            var item = await _db.Leads.SingleOrDefaultAsync(x => x.LeadId == id);
            if (item is null) return NotFound();

            if (!ApiSecurityHelper.HasFullOversight(user.Role) && ApiSecurityHelper.IsAgent(user.Role) && user.UserId > 0)
            {
                bool allowed = (item.AssignedAgentId.HasValue && item.AssignedAgentId.Value > 0)
                    ? item.AssignedAgentId.Value == user.UserId
                    : item.CreatedByUserId == user.UserId;
                if (!allowed) return Forbid();
            }

            item.Stage = "new";
            await _db.SaveChangesAsync();
            await LogActivityInternalAsync("Lead Restored", item.LeadId, null, $"Lead '{item.FullName}' was restored to New stage.", user.UserId);
            return Ok(item);
        }

        [HttpDelete("{id:int}")]
        public async Task<IActionResult> Delete(int id)
        {
            var user = CurrentUser;
            var item = await _db.Leads.SingleOrDefaultAsync(x => x.LeadId == id);
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

            await LogActivityInternalAsync("Lead Archived", item.LeadId, null, $"Lead '{item.FullName}' was archived.", user.UserId);
            return NoContent();
        }

        [HttpPost("{id:int}/restore")]
        public async Task<IActionResult> Restore(int id)
        {
            var user = CurrentUser;
            if (user.UserId <= 0) return Unauthorized();

            var query = _db.Leads
                .IgnoreQueryFilters()
                .Include(l => l.CreatedByUser).ThenInclude(u => u.Role)
                .Where(x => x.LeadId == id && x.IsDeleted);

            if (!ApiSecurityHelper.IsSuperAdmin(user.Role))
            {
                query = query.Where(x => x.CreatedByUser != null && x.CreatedByUser.Role != null && x.CreatedByUser.Role.TenantId == user.TenantId);
            }

            var item = await query.SingleOrDefaultAsync();
            if (item is null) return NotFound();

            if (!ApiSecurityHelper.HasFullOversight(user.Role) && ApiSecurityHelper.IsAgent(user.Role) && user.UserId > 0)
            {
                bool allowed = (item.AssignedAgentId.HasValue && item.AssignedAgentId.Value > 0)
                    ? item.AssignedAgentId.Value == user.UserId
                    : item.CreatedByUserId == user.UserId;
                if (!allowed) return Forbid();
            }

            item.IsDeleted = false;
            item.DeletedAt = null;
            await _db.SaveChangesAsync();

            await LogActivityInternalAsync("Lead Restored", item.LeadId, null, $"Lead '{item.FullName}' was restored from archive.", user.UserId);
            return Ok(item);
        }

        [HttpGet("archived")]
        public async Task<IActionResult> GetArchived()
        {
            var user = CurrentUser;
            if (user.UserId <= 0) return Unauthorized();

            var query = _db.Leads
                .IgnoreQueryFilters()
                .Include(l => l.CreatedByUser).ThenInclude(u => u.Role)
                .Where(l => l.IsDeleted);

            if (!ApiSecurityHelper.IsSuperAdmin(user.Role))
            {
                query = query.Where(l => l.CreatedByUser != null && l.CreatedByUser.Role != null && l.CreatedByUser.Role.TenantId == user.TenantId);
            }

            if (!ApiSecurityHelper.HasFullOversight(user.Role) && ApiSecurityHelper.IsAgent(user.Role) && user.UserId > 0)
            {
                query = query.Where(l =>
                    (l.AssignedAgentId.HasValue && l.AssignedAgentId.Value > 0)
                        ? l.AssignedAgentId.Value == user.UserId
                        : l.CreatedByUserId == user.UserId);
            }

            var items = await query
                .OrderByDescending(l => l.DeletedAt)
                .Take(100)
                .ToListAsync();
            return Ok(items);
        }

        [HttpPost("{id:int}/assign")]
        public async Task<IActionResult> AssignAgent(int id, [FromBody] AssignAgentRequest request)
        {
            var user = CurrentUser;
            if (!ApiSecurityHelper.CanAssignRecords(user.Role))
                return StatusCode(StatusCodes.Status403Forbidden, "Only Manager or Admin may assign records.");

            var item = await _db.Leads.SingleOrDefaultAsync(x => x.LeadId == id);
            if (item is null) return NotFound();

            var oldAgentId = item.AssignedAgentId;
            var newAgentId = request.AgentId <= 0 ? null : request.AgentId;

            if (newAgentId.HasValue && !await _db.Users.AnyAsync(u => u.UserId == newAgentId.Value))
            {
                return BadRequest(new { message = $"Agent with ID {newAgentId.Value} does not exist." });
            }

            item.AssignedAgentId = newAgentId;
            item.AssignmentStatus = request.Approve ? "approved" : "pending_review";
            item.AssignmentReviewedByUserId = user.UserId > 0 ? user.UserId : null;
            item.AssignmentReviewedAt = DateTime.UtcNow;
            item.AssignmentReviewNotes = string.IsNullOrWhiteSpace(request.Notes) ? null : request.Notes.Trim();

            await _db.SaveChangesAsync();

            if (oldAgentId != newAgentId)
            {
                await LogActivityInternalAsync("Lead Assignment Changed", item.LeadId, null,
                    $"Lead '{item.FullName}' assigned to Agent #{newAgentId?.ToString() ?? "Unassigned"} by User #{user.UserId}.", user.UserId);
            }

            return Ok(item);
        }

        [HttpPost("{id:int}/approve")]
        public async Task<IActionResult> ApproveAssignment(int id, [FromBody] ApproveAssignmentRequest request)
        {
            var user = CurrentUser;
            if (!ApiSecurityHelper.CanAssignRecords(user.Role))
                return StatusCode(StatusCodes.Status403Forbidden, "Only Manager or Admin may approve assignments.");

            var item = await _db.Leads.SingleOrDefaultAsync(x => x.LeadId == id);
            if (item is null) return NotFound();

            item.AssignmentStatus = "approved";
            item.AssignmentReviewedByUserId = user.UserId > 0 ? user.UserId : null;
            item.AssignmentReviewedAt = DateTime.UtcNow;
            item.AssignmentReviewNotes = string.IsNullOrWhiteSpace(request.Notes) ? null : request.Notes.Trim();

            await _db.SaveChangesAsync();
            await LogActivityInternalAsync("Lead Assignment Approved", item.LeadId, null, $"Assignment for '{item.FullName}' was approved.", user.UserId);

            return Ok(item);
        }

        [HttpGet("pending-review")]
        public async Task<IActionResult> GetPendingReview()
        {
            var user = CurrentUser;
            if (!ApiSecurityHelper.HasFullOversight(user.Role))
                return StatusCode(StatusCodes.Status403Forbidden, "Only Manager or Admin can view records pending review.");

            var items = await _db.Leads
                .AsNoTracking()
                .Where(l => !l.IsDeleted && (l.AssignmentStatus == "pending_review" || l.AssignedAgentId == null))
                .OrderByDescending(l => l.CreatedAt)
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
                .Where(u => agentRoleIds.Contains(u.RoleId) && u.Status != null && !u.Status.Equals("inactive", StringComparison.OrdinalIgnoreCase))
                .OrderBy(u => u.LastName)
                .ThenBy(u => u.FirstName)
                .Select(u => new AgentPickerDto(u.UserId, u.FullName, u.Email))
                .ToListAsync();

            return Ok(agents);
        }

        [HttpGet("agent-dict")]
        public async Task<IActionResult> GetAgentDictionary()
        {
            var dict = await _db.Users
                .AsNoTracking()
                .ToDictionaryAsync(u => u.UserId, u => u.FullName);
            return Ok(dict);
        }

        [HttpGet("{id:int}/activities")]
        public async Task<IActionResult> GetActivityHistory(int id)
        {
            var activities = await _db.Activities
                .AsNoTracking()
                .Where(a => a.RelatedLeadId == id)
                .OrderByDescending(a => a.ActivityDate)
                .ToListAsync();
            return Ok(activities);
        }

        [HttpPost("{id:int}/log-email")]
        public async Task<IActionResult> LogEmail(int id, [FromBody] LogCustomerActivityRequest req)
        {
            var user = CurrentUser;
            var lead = await _db.Leads.SingleOrDefaultAsync(l => l.LeadId == id);
            if (lead == null) return NotFound();

            await LogActivityInternalAsync("Email", id, null, $"Email sent to '{lead.FullName}'. Subject: {req.Subject}", user.UserId);
            return Ok(new { success = true });
        }

        [HttpGet("stats/active-count")]
        public async Task<IActionResult> GetActiveLeadsCount([FromQuery] int? agentId = null)
        {
            var user = CurrentUser;
            var query = _db.Leads.AsNoTracking().Where(l => !l.IsDeleted && l.Stage.ToLower() != "lost" && l.Stage.ToLower() != "converted");

            if (agentId.HasValue && agentId.Value > 0)
            {
                query = query.Where(l => l.AssignedAgentId == agentId.Value);
            }
            else if (!ApiSecurityHelper.HasFullOversight(user.Role) && ApiSecurityHelper.IsAgent(user.Role) && user.UserId > 0)
            {
                query = query.Where(l =>
                    (l.AssignedAgentId.HasValue && l.AssignedAgentId.Value > 0)
                        ? l.AssignedAgentId.Value == user.UserId
                        : l.CreatedByUserId == user.UserId);
            }

            int count = await query.CountAsync();
            return Ok(new { count });
        }

        [HttpGet("stats/conversion-rate")]
        public async Task<IActionResult> GetTeamConversionRate()
        {
            var query = _db.Leads.AsNoTracking().Where(l => !l.IsDeleted);
            int total = await query.CountAsync();
            if (total == 0) return Ok(new { conversionRate = 0.0 });

            int converted = await query.CountAsync(l => l.Stage.ToLower() == "converted");
            double rate = Math.Round((double)converted / total * 100.0, 1);
            return Ok(new { conversionRate = rate });
        }

        private async Task LogActivityInternalAsync(string type, int? leadId, int? customerId, string notes, int agentId)
        {
            try
            {
                if (agentId <= 0) return;
                _db.Activities.Add(new Activity
                {
                    Type = type,
                    RelatedLeadId = leadId,
                    RelatedCustomerId = customerId,
                    LoggedByAgentId = agentId,
                    Notes = notes,
                    ActivityDate = DateTime.UtcNow
                });
                await _db.SaveChangesAsync();
            }
            catch { }
        }
    }
}