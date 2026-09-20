using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using CRMS_Peguit.domain.entities;
using CRMS_Peguit.infrastructure.data;

using CRMS_Peguit.domain.Common;

namespace CRMS_Peguit.api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class SupportTicketsController : ControllerBase
    {
        private readonly RealEstateDbContext _db;

        public SupportTicketsController(RealEstateDbContext db)
        {
            _db = db;
        }

        [HttpGet]
        public async Task<IActionResult> GetAll(
            [FromQuery] int? page = null,
            [FromQuery] int? pageSize = null,
            [FromQuery] string? search = null,
            [FromQuery] string? status = null)
        {
            var query = _db.SupportTickets
                .Include(t => t.Customer).ThenInclude(c => c!.Person)
                .Include(t => t.AssignedToUser).ThenInclude(u => u!.Person)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(status) && !status.Equals("All", StringComparison.OrdinalIgnoreCase))
            {
                if (status.Equals("Overdue", StringComparison.OrdinalIgnoreCase))
                {
                    var now = DateTime.UtcNow;
                    query = query.Where(t => t.Status != "Resolved" && t.DueDate.HasValue && t.DueDate.Value < now);
                }
                else
                {
                    query = query.Where(t => t.Status == status);
                }
            }

            if (!string.IsNullOrWhiteSpace(search))
            {
                string s = search.Trim();
                query = query.Where(t =>
                    t.TicketNumber.Contains(s) ||
                    t.Category.Contains(s) ||
                    t.Description.Contains(s) ||
                    (t.Customer != null && (t.Customer.Person.FirstName.Contains(s) || t.Customer.Person.LastName.Contains(s) || (t.Customer.Person.Email != null && t.Customer.Person.Email.Contains(s)))));
            }

            if (page.HasValue || pageSize.HasValue)
            {
                int pageNum = page.GetValueOrDefault(1);
                int size = pageSize.GetValueOrDefault(25);
                if (pageNum < 1) pageNum = 1;
                if (size < 1) size = 25;

                int totalCount = await query.CountAsync();
                var pagedList = await query.OrderByDescending(t => t.CreatedAt)
                    .Skip((pageNum - 1) * size)
                    .Take(size)
                    .ToListAsync();

                return Ok(new PagedResult<SupportTicket>(pagedList, totalCount, pageNum, size));
            }

            var items = await query.ToListAsync();
            return Ok(items);
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(int id)
        {
            var item = await _db.SupportTickets.FindAsync(id);
            return item is null ? NotFound() : Ok(item);
        }

        [HttpPost]
        public async Task<IActionResult> Create(SupportTicket ticket)
        {
            if (ticket.RaisedByUserId <= 0)
            {
                ticket.RaisedByUserId = 1;
            }
            ticket.CreatedAt = DateTime.UtcNow;
            ticket.AssignedToUserId = null; // R23. Default state is Unassigned
            if (string.IsNullOrWhiteSpace(ticket.Category)) ticket.Category = "Other";
            if (string.IsNullOrWhiteSpace(ticket.Priority)) ticket.Priority = "Medium";
            if (string.IsNullOrWhiteSpace(ticket.Status)) ticket.Status = "Open";

            ticket.DueDate = (ticket.Priority.ToLower()) switch
            {
                "high" or "urgent" => ticket.CreatedAt.AddHours(24),
                "medium" => ticket.CreatedAt.AddDays(3),
                "low" => ticket.CreatedAt.AddDays(7),
                _ => ticket.CreatedAt.AddDays(3)
            };

            ticket.TicketNumber = "TCK-TEMP";
            _db.SupportTickets.Add(ticket);
            await _db.SaveChangesAsync();

            ticket.TicketNumber = $"TCK-{ticket.TicketId:D5}";
            await _db.SaveChangesAsync();

            return Created($"/api/supporttickets/{ticket.TicketId}", ticket);
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> Update(int id, SupportTicket updated)
        {
            var item = await _db.SupportTickets.FindAsync(id);
            if (item is null) return NotFound();

            item.CustomerId = updated.CustomerId;
            item.RaisedByUserId = updated.RaisedByUserId;
            item.AssignedToUserId = updated.AssignedToUserId;
            item.Category = updated.Category;
            item.Description = updated.Description;
            item.Priority = updated.Priority;
            item.Status = updated.Status;
            item.DueDate = updated.DueDate;
            item.FirstRespondedAt = updated.FirstRespondedAt;
            item.ResolvedAt = updated.ResolvedAt;

            await _db.SaveChangesAsync();
            return Ok(item);
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            var item = await _db.SupportTickets.FindAsync(id);
            if (item is null) return NotFound();

            _db.SupportTickets.Remove(item);
            await _db.SaveChangesAsync();
            return NoContent();
        }
    }
}