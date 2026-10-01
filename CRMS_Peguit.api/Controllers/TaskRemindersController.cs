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
    public record FollowUpKpiDto(int Total, int DueToday, int Upcoming, int Completed, int Overdue);
    public record CompleteTaskRequest(bool LogActivity = false, string? ActivityNotes = null);
    public record SnoozeTaskRequest(int Days = 1);
    public record RescheduleTaskRequest(DateTime NewDueDate);

    [ApiController]
    [Route("api/[controller]")]
    public class TaskRemindersController : ControllerBase
    {
        private readonly RealEstateDbContext _db;

        public TaskRemindersController(RealEstateDbContext db)
        {
            _db = db;
        }

        private (int UserId, string Role, int TenantId) CurrentUser =>
            ApiSecurityHelper.GetCurrentUserInfo(HttpContext);

        [HttpGet]
        public async Task<IActionResult> GetAll(
            [FromQuery] int? page = null,
            [FromQuery] int? pageSize = null,
            [FromQuery] int? assignedToUserId = null,
            [FromQuery] string? status = null,
            [FromQuery] string? priority = null,
            [FromQuery] string? search = null)
        {
            var user = CurrentUser;
            var query = _db.TaskReminders
                .Include(r => r.RelatedCustomer)
                .Include(r => r.RelatedLead)
                .Where(r => !r.IsDeleted)
                .AsQueryable();

            if (assignedToUserId.HasValue && assignedToUserId.Value > 0)
            {
                if (!ApiSecurityHelper.HasFullOversight(user.Role) && ApiSecurityHelper.IsAgent(user.Role) && assignedToUserId.Value != user.UserId)
                {
                    return Forbid();
                }
                query = query.Where(r => r.AssignedToUserId == assignedToUserId.Value);
            }
            else if (!ApiSecurityHelper.HasFullOversight(user.Role) && ApiSecurityHelper.IsAgent(user.Role) && user.UserId > 0)
            {
                query = query.Where(r => r.AssignedToUserId == user.UserId);
            }

            if (!string.IsNullOrWhiteSpace(status) && !status.Equals("All", StringComparison.OrdinalIgnoreCase))
            {
                if (status.Equals("Overdue", StringComparison.OrdinalIgnoreCase))
                {
                    var now = DateTime.UtcNow;
                    query = query.Where(r => r.Status != "Completed" && r.DueDate < now);
                }
                else
                {
                    query = query.Where(r => r.Status.ToLower() == status.ToLower());
                }
            }

            if (!string.IsNullOrWhiteSpace(priority) && !priority.Equals("All", StringComparison.OrdinalIgnoreCase))
            {
                query = query.Where(r => r.Priority.ToLower() == priority.ToLower());
            }

            if (!string.IsNullOrWhiteSpace(search))
            {
                string s = search.Trim();
                query = query.Where(r =>
                    r.Title.Contains(s) ||
                    (r.Notes != null && r.Notes.Contains(s)) ||
                    (r.RelatedCustomer != null && (r.RelatedCustomer.FirstName.Contains(s) || r.RelatedCustomer.LastName.Contains(s))) ||
                    (r.RelatedLead != null && (r.RelatedLead.FirstName.Contains(s) || r.RelatedLead.LastName.Contains(s))));
            }

            if (page.HasValue || pageSize.HasValue)
            {
                int pageNum = Math.Max(1, page.GetValueOrDefault(1));
                int size = Math.Clamp(pageSize.GetValueOrDefault(25), 1, 100);

                int totalCount = await query.CountAsync();
                var pagedList = await query.OrderBy(r => r.DueDate)
                    .Skip((pageNum - 1) * size)
                    .Take(size)
                    .ToListAsync();

                return Ok(new PagedResult<TaskReminder>(pagedList, totalCount, pageNum, size));
            }

            var items = await query.OrderBy(r => r.DueDate).ToListAsync();
            return Ok(items);
        }

        [HttpGet("kpis")]
        public async Task<IActionResult> GetKpis([FromQuery] int? userId = null)
        {
            var user = CurrentUser;
            if (userId.HasValue && !ApiSecurityHelper.HasFullOversight(user.Role) && ApiSecurityHelper.IsAgent(user.Role) && userId.Value != user.UserId)
            {
                return Forbid();
            }

            int effectiveUserId = userId ?? user.UserId;

            var query = _db.TaskReminders.AsNoTracking().Where(r => !r.IsDeleted);
            if (effectiveUserId > 0 && (!ApiSecurityHelper.HasFullOversight(user.Role) || userId.HasValue))
            {
                query = query.Where(r => r.AssignedToUserId == effectiveUserId);
            }

            var now = DateTime.UtcNow;
            var todayStart = now.Date;
            var todayEnd = todayStart.AddDays(1);

            int total = await query.CountAsync();
            int dueToday = await query.CountAsync(r => r.DueDate >= todayStart && r.DueDate < todayEnd && r.Status != "Completed");
            int upcoming = await query.CountAsync(r => r.DueDate >= todayEnd && r.Status != "Completed");
            int completed = await query.CountAsync(r => r.Status == "Completed");
            int overdue = await query.CountAsync(r => r.DueDate < todayStart && r.Status != "Completed");

            return Ok(new FollowUpKpiDto(total, dueToday, upcoming, completed, overdue));
        }

        [HttpGet("{id:int}")]
        public async Task<IActionResult> GetById(int id)
        {
            var user = CurrentUser;
            var item = await _db.TaskReminders
                .Include(r => r.RelatedCustomer)
                .Include(r => r.RelatedLead)
                .SingleOrDefaultAsync(r => r.TaskReminderId == id && !r.IsDeleted);

            if (item is null) return NotFound();

            if (!ApiSecurityHelper.HasFullOversight(user.Role) && ApiSecurityHelper.IsAgent(user.Role) && item.AssignedToUserId != user.UserId)
            {
                return Forbid();
            }

            return Ok(item);
        }

        [HttpPost]
        public async Task<IActionResult> Create(TaskReminder reminder)
        {
            var user = CurrentUser;
            if (user.UserId <= 0) return Unauthorized();

            if (string.IsNullOrWhiteSpace(reminder.Title))
                return BadRequest("Task title is required.");

            if (reminder.RelatedCustomerId <= 0) reminder.RelatedCustomerId = null;
            if (reminder.RelatedLeadId <= 0) reminder.RelatedLeadId = null;

            if (!ApiSecurityHelper.CanAssignRecords(user.Role) || reminder.AssignedToUserId <= 0)
            {
                reminder.AssignedToUserId = user.UserId;
            }

            reminder.CreatedAt = DateTime.UtcNow;
            reminder.UpdatedAt = DateTime.UtcNow;
            reminder.IsDeleted = false;
            reminder.DeletedAt = null;

            if (string.IsNullOrWhiteSpace(reminder.Status))
            {
                reminder.Status = reminder.DueDate < DateTime.UtcNow ? "Overdue" : "Pending";
            }

            _db.TaskReminders.Add(reminder);
            await _db.SaveChangesAsync();

            return CreatedAtAction(nameof(GetById), new { id = reminder.TaskReminderId }, reminder);
        }

        [HttpPut("{id:int}")]
        public async Task<IActionResult> Update(int id, TaskReminder updated)
        {
            var user = CurrentUser;
            var item = await _db.TaskReminders.SingleOrDefaultAsync(r => r.TaskReminderId == id && !r.IsDeleted);
            if (item is null) return NotFound();

            if (!ApiSecurityHelper.HasFullOversight(user.Role) && ApiSecurityHelper.IsAgent(user.Role) && item.AssignedToUserId != user.UserId)
            {
                return Forbid();
            }

            if (string.IsNullOrWhiteSpace(updated.Title))
                return BadRequest("Task title is required.");

            if (updated.RelatedCustomerId <= 0) updated.RelatedCustomerId = null;
            if (updated.RelatedLeadId <= 0) updated.RelatedLeadId = null;

            item.Title = updated.Title.Trim();
            item.DueDate = updated.DueDate;
            item.Status = updated.Status;
            item.Type = updated.Type;
            item.Notes = updated.Notes;
            item.Priority = updated.Priority;
            item.RelatedCustomerId = updated.RelatedCustomerId;
            item.RelatedLeadId = updated.RelatedLeadId;
            if (ApiSecurityHelper.CanAssignRecords(user.Role) && updated.AssignedToUserId > 0)
            {
                if (!await _db.Users.AnyAsync(u => u.UserId == updated.AssignedToUserId))
                    return BadRequest("Assigned user does not exist.");
                item.AssignedToUserId = updated.AssignedToUserId;
            }
            item.CompletedAt = updated.CompletedAt;
            item.UpdatedAt = DateTime.UtcNow;

            await _db.SaveChangesAsync();
            return Ok(item);
        }

        [HttpPost("{id:int}/complete")]
        public async Task<IActionResult> MarkComplete(int id, [FromBody] CompleteTaskRequest req)
        {
            var user = CurrentUser;
            if (user.UserId <= 0) return Unauthorized();

            var item = await _db.TaskReminders.SingleOrDefaultAsync(r => r.TaskReminderId == id && !r.IsDeleted);
            if (item is null) return NotFound();

            if (!ApiSecurityHelper.HasFullOversight(user.Role) && ApiSecurityHelper.IsAgent(user.Role) && item.AssignedToUserId != user.UserId)
            {
                return Forbid();
            }

            if (item.Status == "Completed")
            {
                return Ok(item);
            }

            item.Status = "Completed";
            item.CompletedAt = DateTime.UtcNow;
            item.UpdatedAt = DateTime.UtcNow;

            if (req.LogActivity)
            {
                _db.Activities.Add(new Activity
                {
                    Type = item.Type,
                    RelatedCustomerId = item.RelatedCustomerId,
                    RelatedLeadId = item.RelatedLeadId,
                    LoggedByAgentId = user.UserId,
                    Notes = string.IsNullOrWhiteSpace(req.ActivityNotes) ? $"Completed task: {item.Title}" : req.ActivityNotes,
                    ActivityDate = DateTime.UtcNow
                });
            }

            await _db.SaveChangesAsync();
            return Ok(item);
        }

        [HttpPost("{id:int}/snooze")]
        public async Task<IActionResult> Snooze(int id, [FromBody] SnoozeTaskRequest req)
        {
            if (req is null || req.Days < 1 || req.Days > 365)
            {
                return BadRequest(new { message = "Snooze days must be between 1 and 365." });
            }

            var user = CurrentUser;
            var item = await _db.TaskReminders.SingleOrDefaultAsync(r => r.TaskReminderId == id && !r.IsDeleted);
            if (item is null) return NotFound();

            if (!ApiSecurityHelper.HasFullOversight(user.Role) && ApiSecurityHelper.IsAgent(user.Role) && item.AssignedToUserId != user.UserId)
            {
                return Forbid();
            }

            if (item.Status == "Completed")
            {
                return BadRequest(new { message = "Cannot snooze a completed task." });
            }

            item.DueDate = item.DueDate.AddDays(req.Days);
            item.Status = "Pending";
            item.UpdatedAt = DateTime.UtcNow;

            await _db.SaveChangesAsync();
            return Ok(item);
        }

        [HttpPost("{id:int}/reschedule")]
        public async Task<IActionResult> Reschedule(int id, [FromBody] RescheduleTaskRequest req)
        {
            if (req is null || req.NewDueDate < DateTime.UtcNow.AddYears(-1) || req.NewDueDate > DateTime.UtcNow.AddYears(10))
            {
                return BadRequest(new { message = "Invalid reschedule due date." });
            }

            var user = CurrentUser;
            var item = await _db.TaskReminders.SingleOrDefaultAsync(r => r.TaskReminderId == id && !r.IsDeleted);
            if (item is null) return NotFound();

            if (!ApiSecurityHelper.HasFullOversight(user.Role) && ApiSecurityHelper.IsAgent(user.Role) && item.AssignedToUserId != user.UserId)
            {
                return Forbid();
            }

            if (item.Status == "Completed")
            {
                return BadRequest(new { message = "Cannot reschedule a completed task." });
            }

            item.DueDate = req.NewDueDate;
            item.Status = req.NewDueDate < DateTime.UtcNow ? "Overdue" : "Pending";
            item.UpdatedAt = DateTime.UtcNow;

            await _db.SaveChangesAsync();
            return Ok(item);
        }

        [HttpDelete("{id:int}")]
        public async Task<IActionResult> Delete(int id)
        {
            var user = CurrentUser;
            var item = await _db.TaskReminders.SingleOrDefaultAsync(r => r.TaskReminderId == id && !r.IsDeleted);
            if (item is null) return NotFound();

            if (!ApiSecurityHelper.HasFullOversight(user.Role) && ApiSecurityHelper.IsAgent(user.Role) && item.AssignedToUserId != user.UserId)
            {
                return Forbid();
            }

            item.IsDeleted = true;
            item.DeletedAt = DateTime.UtcNow;
            item.UpdatedAt = DateTime.UtcNow;

            await _db.SaveChangesAsync();
            return NoContent();
        }

        [HttpGet("assigned-customers")]
        public async Task<IActionResult> GetAssignedCustomers([FromQuery] int? userId = null)
        {
            var user = CurrentUser;
            if (userId.HasValue && !ApiSecurityHelper.HasFullOversight(user.Role) && ApiSecurityHelper.IsAgent(user.Role) && userId.Value != user.UserId)
            {
                return Forbid();
            }

            int effectiveUserId = userId ?? user.UserId;

            var query = _db.Customers.AsNoTracking().Where(c => !c.IsDeleted);
            if (effectiveUserId > 0 && !ApiSecurityHelper.HasFullOversight(user.Role))
            {
                query = query.Where(c => c.AssignedAgentId == effectiveUserId);
            }

            var list = await query
                .OrderBy(c => c.LastName)
                .ThenBy(c => c.FirstName)
                .ToListAsync();

            return Ok(list);
        }

        [HttpGet("assigned-leads")]
        public async Task<IActionResult> GetAssignedLeads([FromQuery] int? userId = null)
        {
            var user = CurrentUser;
            if (userId.HasValue && !ApiSecurityHelper.HasFullOversight(user.Role) && ApiSecurityHelper.IsAgent(user.Role) && userId.Value != user.UserId)
            {
                return Forbid();
            }

            int effectiveUserId = userId ?? user.UserId;

            var query = _db.Leads.AsNoTracking().Where(l => !l.IsDeleted && l.Stage.ToLower() != "converted" && l.Stage.ToLower() != "lost");
            if (effectiveUserId > 0 && !ApiSecurityHelper.HasFullOversight(user.Role))
            {
                query = query.Where(l => l.AssignedAgentId == effectiveUserId);
            }

            var list = await query
                .OrderBy(l => l.LastName)
                .ThenBy(l => l.FirstName)
                .ToListAsync();

            return Ok(list);
        }

        [HttpGet("due-today")]
        public async Task<IActionResult> GetFollowUpsDueToday([FromQuery] int? userId = null, [FromQuery] int maxCount = 5)
        {
            var user = CurrentUser;
            if (userId.HasValue && !ApiSecurityHelper.HasFullOversight(user.Role) && ApiSecurityHelper.IsAgent(user.Role) && userId.Value != user.UserId)
            {
                return Forbid();
            }

            int effectiveUserId = userId ?? user.UserId;
            maxCount = Math.Clamp(maxCount, 1, 100);

            var now = DateTime.UtcNow;
            var todayEnd = now.Date.AddDays(1);

            var query = _db.TaskReminders
                .AsNoTracking()
                .Include(r => r.RelatedCustomer)
                .Include(r => r.RelatedLead)
                .Where(r => !r.IsDeleted && r.Status != "Completed" && r.DueDate <= todayEnd);

            if (effectiveUserId > 0 && !ApiSecurityHelper.HasFullOversight(user.Role))
            {
                query = query.Where(r => r.AssignedToUserId == effectiveUserId);
            }

            var list = await query.OrderBy(r => r.DueDate).Take(maxCount).ToListAsync();
            return Ok(list);
        }
    }
}
