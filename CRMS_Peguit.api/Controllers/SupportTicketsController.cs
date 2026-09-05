using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using CRMS_Peguit.domain.entities;
using CRMS_Peguit.infrastructure.data;

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
        public async Task<IActionResult> GetAll()
        {
            var items = await _db.SupportTickets.ToListAsync();
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
            var tenantResolver = HttpContext.RequestServices
                .GetRequiredService<ITenantResolver>();

            ticket.TenantId = tenantResolver.GetTenantId();
            ticket.CreatedAt = DateTime.UtcNow;

            _db.SupportTickets.Add(ticket);
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
            item.Description = updated.Description;
            item.Priority = updated.Priority;
            item.Status = updated.Status;
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