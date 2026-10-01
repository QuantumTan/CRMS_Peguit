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
    public record ActivityStatsDto(int Total, int Calls, int Meetings, int Showings, int Tasks, int Emails);

    [ApiController]
    [Route("api/[controller]")]
    public class ActivitiesController : ControllerBase
    {
        private readonly RealEstateDbContext _db;

        public ActivitiesController(RealEstateDbContext db)
        {
            _db = db;
        }

        private (int UserId, string Role, int TenantId) CurrentUser =>
            ApiSecurityHelper.GetCurrentUserInfo(HttpContext);

        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var user = CurrentUser;
            var query = _db.Activities
                .Include(a => a.LoggedByAgent)
                .Include(a => a.RelatedCustomer)
                .Include(a => a.RelatedLead)
                .AsQueryable();

            if (!ApiSecurityHelper.HasFullOversight(user.Role) && ApiSecurityHelper.IsAgent(user.Role) && user.UserId > 0)
            {
                query = query.Where(a => a.LoggedByAgentId == user.UserId);
            }

            var items = await query.OrderByDescending(a => a.ActivityDate).ToListAsync();
            return Ok(items);
        }

        [HttpGet("{id:int}")]
        public async Task<IActionResult> GetById(int id)
        {
            var user = CurrentUser;
            var item = await _db.Activities
                .Include(a => a.LoggedByAgent)
                .Include(a => a.RelatedCustomer)
                .Include(a => a.RelatedLead)
                .SingleOrDefaultAsync(a => a.ActivityId == id);

            if (item is null) return NotFound();

            if (!ApiSecurityHelper.HasFullOversight(user.Role) && ApiSecurityHelper.IsAgent(user.Role) && item.LoggedByAgentId != user.UserId)
            {
                return StatusCode(StatusCodes.Status403Forbidden, "You can only access your own activities.");
            }

            return Ok(item);
        }

        [HttpPost]
        public async Task<IActionResult> Create(Activity activity)
        {
            var user = CurrentUser;
            if (user.UserId <= 0)
            {
                return Unauthorized();
            }

            if (!ApiSecurityHelper.HasFullOversight(user.Role) || activity.LoggedByAgentId <= 0)
            {
                activity.LoggedByAgentId = user.UserId;
            }
            if (activity.ActivityDate == default)
            {
                activity.ActivityDate = DateTime.UtcNow;
            }

            _db.Activities.Add(activity);
            await _db.SaveChangesAsync();

            return CreatedAtAction(nameof(GetById), new { id = activity.ActivityId }, activity);
        }

        [HttpPut("{id:int}")]
        public async Task<IActionResult> Update(int id, Activity updated)
        {
            var user = CurrentUser;
            if (user.UserId <= 0) return Unauthorized();

            var item = await _db.Activities.FindAsync(id);
            if (item is null) return NotFound();

            if (!ApiSecurityHelper.HasFullOversight(user.Role) && ApiSecurityHelper.IsAgent(user.Role) && item.LoggedByAgentId != user.UserId)
            {
                return StatusCode(StatusCodes.Status403Forbidden, "You can only update your own activities.");
            }

            item.Type = updated.Type;
            item.RelatedLeadId = updated.RelatedLeadId;
            item.RelatedCustomerId = updated.RelatedCustomerId;
            if (ApiSecurityHelper.CanAssignRecords(user.Role) && updated.LoggedByAgentId > 0)
            {
                item.LoggedByAgentId = updated.LoggedByAgentId;
            }
            item.Notes = updated.Notes;
            item.ActivityDate = updated.ActivityDate;
            item.Outcome = updated.Outcome;
            item.DurationMinutes = updated.DurationMinutes;

            await _db.SaveChangesAsync();
            return Ok(item);
        }

        [HttpDelete("{id:int}")]
        public async Task<IActionResult> Delete(int id)
        {
            var user = CurrentUser;
            if (user.UserId <= 0) return Unauthorized();

            var item = await _db.Activities.FindAsync(id);
            if (item is null) return NotFound();

            if (!ApiSecurityHelper.HasFullOversight(user.Role) && ApiSecurityHelper.IsAgent(user.Role) && item.LoggedByAgentId != user.UserId)
            {
                return StatusCode(StatusCodes.Status403Forbidden, "You can only delete your own activities.");
            }

            _db.Activities.Remove(item);
            await _db.SaveChangesAsync();
            return NoContent();
        }

        [HttpGet("paged")]
        public async Task<IActionResult> GetPagedForAgent(
            [FromQuery] int? agentId = null,
            [FromQuery] string? category = null,
            [FromQuery] string? search = null,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 25)
        {
            var user = CurrentUser;
            if (agentId.HasValue && !ApiSecurityHelper.HasFullOversight(user.Role) && ApiSecurityHelper.IsAgent(user.Role) && agentId.Value != user.UserId)
            {
                return Forbid();
            }

            int effectiveAgentId = agentId ?? user.UserId;

            var query = _db.Activities
                .Include(a => a.LoggedByAgent)
                .Include(a => a.RelatedCustomer)
                .Include(a => a.RelatedLead)
                .AsQueryable();

            if (effectiveAgentId > 0 && (!ApiSecurityHelper.HasFullOversight(user.Role) || agentId.HasValue))
            {
                query = query.Where(a => a.LoggedByAgentId == effectiveAgentId);
            }

            if (!string.IsNullOrWhiteSpace(category) && !category.Equals("All", StringComparison.OrdinalIgnoreCase))
            {
                query = query.Where(a => a.Type.ToLower() == category.ToLower());
            }

            if (!string.IsNullOrWhiteSpace(search))
            {
                string s = search.Trim();
                query = query.Where(a =>
                    (a.Notes != null && a.Notes.Contains(s)) ||
                    a.Type.Contains(s) ||
                    (a.RelatedCustomer != null && a.RelatedCustomer.FirstName.Contains(s)) ||
                    (a.RelatedCustomer != null && a.RelatedCustomer.LastName.Contains(s)) ||
                    (a.RelatedLead != null && a.RelatedLead.FirstName.Contains(s)) ||
                    (a.RelatedLead != null && a.RelatedLead.LastName.Contains(s)));
            }

            int validPage = Math.Max(1, page);
            int validSize = Math.Clamp(pageSize, 1, 100);
            int total = await query.CountAsync();

            var items = await query
                .OrderByDescending(a => a.ActivityDate)
                .Skip((validPage - 1) * validSize)
                .Take(validSize)
                .ToListAsync();

            return Ok(new PagedResult<Activity>(items, total, validPage, validSize));
        }

        [HttpGet("stats")]
        public async Task<IActionResult> GetActivityStatsForAgent([FromQuery] int? agentId = null)
        {
            var user = CurrentUser;
            if (agentId.HasValue && !ApiSecurityHelper.HasFullOversight(user.Role) && ApiSecurityHelper.IsAgent(user.Role) && agentId.Value != user.UserId)
            {
                return Forbid();
            }

            int effectiveAgentId = agentId ?? user.UserId;

            var query = _db.Activities.AsNoTracking().AsQueryable();
            if (effectiveAgentId > 0 && (!ApiSecurityHelper.HasFullOversight(user.Role) || agentId.HasValue))
            {
                query = query.Where(a => a.LoggedByAgentId == effectiveAgentId);
            }

            int total = await query.CountAsync();
            int calls = await query.CountAsync(a => a.Type.ToLower() == "call");
            int meetings = await query.CountAsync(a => a.Type.ToLower() == "meeting");
            int showings = await query.CountAsync(a => a.Type.ToLower() == "showing");
            int tasks = await query.CountAsync(a => a.Type.ToLower() == "task");
            int emails = await query.CountAsync(a => a.Type.ToLower() == "email");

            return Ok(new ActivityStatsDto(total, calls, meetings, showings, tasks, emails));
        }

        [HttpGet("recent")]
        public async Task<IActionResult> GetRecentActivitiesForAgent([FromQuery] int? agentId = null, [FromQuery] int maxCount = 5)
        {
            var user = CurrentUser;
            if (agentId.HasValue && !ApiSecurityHelper.HasFullOversight(user.Role) && ApiSecurityHelper.IsAgent(user.Role) && agentId.Value != user.UserId)
            {
                return Forbid();
            }

            int effectiveAgentId = agentId ?? user.UserId;
            int validMax = Math.Clamp(maxCount, 1, 50);

            var query = _db.Activities
                .AsNoTracking()
                .Include(a => a.LoggedByAgent)
                .Include(a => a.RelatedCustomer)
                .Include(a => a.RelatedLead)
                .AsQueryable();

            if (effectiveAgentId > 0 && (!ApiSecurityHelper.HasFullOversight(user.Role) || agentId.HasValue))
            {
                query = query.Where(a => a.LoggedByAgentId == effectiveAgentId);
            }

            var items = await query
                .OrderByDescending(a => a.ActivityDate)
                .Take(validMax)
                .ToListAsync();

            return Ok(items);
        }

        [HttpGet("timeline")]
        public async Task<IActionResult> GetTimeline([FromQuery] int? customerId, [FromQuery] int? leadId, [FromQuery] string? filter = null)
        {
            var user = CurrentUser;
            if (!ApiSecurityHelper.HasFullOversight(user.Role) && ApiSecurityHelper.IsAgent(user.Role) && user.UserId > 0)
            {
                if (customerId.HasValue && customerId.Value > 0)
                {
                    var cust = await _db.Customers.FindAsync(customerId.Value);
                    if (cust != null && cust.AssignedAgentId != user.UserId && cust.CreatedByUserId != user.UserId)
                        return Forbid();
                }
                if (leadId.HasValue && leadId.Value > 0)
                {
                    var lead = await _db.Leads.FindAsync(leadId.Value);
                    if (lead != null && lead.AssignedAgentId != user.UserId && lead.CreatedByUserId != user.UserId)
                        return Forbid();
                }
            }

            var query = _db.Activities
                .Include(a => a.LoggedByAgent)
                .Include(a => a.RelatedCustomer)
                .Include(a => a.RelatedLead)
                .AsQueryable();

            if (customerId.HasValue && customerId.Value > 0)
            {
                query = query.Where(a => a.RelatedCustomerId == customerId.Value);
            }
            if (leadId.HasValue && leadId.Value > 0)
            {
                query = query.Where(a => a.RelatedLeadId == leadId.Value);
            }
            if (!string.IsNullOrWhiteSpace(filter) && !filter.Equals("All", StringComparison.OrdinalIgnoreCase))
            {
                query = query.Where(a => a.Type.ToLower() == filter.ToLower());
            }

            var list = await query.OrderByDescending(a => a.ActivityDate).ToListAsync();
            var dtos = list.Select(a => new TimelineItemDto
            {
                Id = a.ActivityId.ToString(),
                RawActivityId = a.ActivityId,
                Source = "Activity",
                Type = a.Type ?? "Activity",
                Category = a.Type ?? "Activity",
                Title = $"{a.Type}: {(a.RelatedCustomer != null ? a.RelatedCustomer.FullName : a.RelatedLead != null ? a.RelatedLead.FullName : "")}".TrimEnd(':', ' '),
                Notes = a.Notes,
                Timestamp = a.ActivityDate,
                ActorName = a.LoggedByAgent != null ? a.LoggedByAgent.FullName : "System",
                Outcome = a.Outcome,
                DurationMinutes = a.DurationMinutes,
                RelatedCustomerId = a.RelatedCustomerId,
                RelatedLeadId = a.RelatedLeadId,
                ClientName = a.RelatedCustomer != null ? a.RelatedCustomer.FullName : a.RelatedLead != null ? a.RelatedLead.FullName : "",
                ClientType = a.RelatedCustomerId.HasValue ? "Customer" : "Lead",
                CanCreateFollowUp = true
            }).ToList();

            return Ok(dtos);
        }
    }
}