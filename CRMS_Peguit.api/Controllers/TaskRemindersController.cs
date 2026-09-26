using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using CRMS_Peguit.domain.entities;
using CRMS_Peguit.infrastructure.data;
using CRMS_Peguit.domain.Common;

namespace CRMS_Peguit.api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class TaskRemindersController : ControllerBase
    {
        private readonly RealEstateDbContext _db;

        public TaskRemindersController(RealEstateDbContext db)
        {
            _db = db;
        }

        [HttpGet]
        public async Task<IActionResult> GetAll(
            [FromQuery] int? page = null,
            [FromQuery] int? pageSize = null,
            [FromQuery] int? assignedToUserId = null,
            [FromQuery] string? status = null)
        {
            var query = _db.TaskReminders
                .Include(r => r.RelatedCustomer).ThenInclude(c => c!.Person)
                .Include(r => r.RelatedLead).ThenInclude(l => l!.Person)
                .Where(r => !r.IsDeleted)
                .AsQueryable();

            if (assignedToUserId.HasValue && assignedToUserId.Value > 0)
            {
                query = query.Where(r => r.AssignedToUserId == assignedToUserId.Value);
            }

            if (!string.IsNullOrWhiteSpace(status) && !status.Equals("All", StringComparison.OrdinalIgnoreCase))
            {
                query = query.Where(r => r.Status == status);
            }

            if (page.HasValue || pageSize.HasValue)
            {
                int pageNum = page.GetValueOrDefault(1);
                int size = pageSize.GetValueOrDefault(25);
                if (pageNum < 1) pageNum = 1;
                if (size < 1) size = 25;

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

        [HttpGet("{id:int}")]
        public async Task<IActionResult> GetById(int id)
        {
            var item = await _db.TaskReminders
                .Include(r => r.RelatedCustomer).ThenInclude(c => c!.Person)
                .Include(r => r.RelatedLead).ThenInclude(l => l!.Person)
                .SingleOrDefaultAsync(r => r.TaskReminderId == id && !r.IsDeleted);

            return item is null ? NotFound() : Ok(item);
        }

        [HttpPost]
        public async Task<IActionResult> Create(TaskReminder reminder)
        {
            if (reminder.AssignedToUserId <= 0)
            {
                reminder.AssignedToUserId = 1;
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
            var item = await _db.TaskReminders.SingleOrDefaultAsync(r => r.TaskReminderId == id && !r.IsDeleted);
            if (item is null) return NotFound();

            item.Title = updated.Title;
            item.DueDate = updated.DueDate;
            item.Status = updated.Status;
            item.Type = updated.Type;
            item.Notes = updated.Notes;
            item.Priority = updated.Priority;
            item.RelatedCustomerId = updated.RelatedCustomerId;
            item.RelatedLeadId = updated.RelatedLeadId;
            item.AssignedToUserId = updated.AssignedToUserId;
            item.CompletedAt = updated.CompletedAt;
            item.UpdatedAt = DateTime.UtcNow;

            await _db.SaveChangesAsync();
            return Ok(item);
        }

        [HttpDelete("{id:int}")]
        public async Task<IActionResult> Delete(int id)
        {
            var item = await _db.TaskReminders.SingleOrDefaultAsync(r => r.TaskReminderId == id);
            if (item is null) return NotFound();

            item.IsDeleted = true;
            item.DeletedAt = DateTime.UtcNow;
            item.UpdatedAt = DateTime.UtcNow;

            await _db.SaveChangesAsync();
            return NoContent();
        }
    }
}
