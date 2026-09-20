using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using CRMS_Peguit.domain.entities;
using CRMS_Peguit.infrastructure.data;

using CRMS_Peguit.domain.Common;

namespace CRMS_Peguit.api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class DealsController : ControllerBase
    {
        private readonly RealEstateDbContext _db;

        public DealsController(RealEstateDbContext db)
        {
            _db = db;
        }

        [HttpGet]
        public async Task<IActionResult> GetAll(
            [FromQuery] int? page = null,
            [FromQuery] int? pageSize = null,
            [FromQuery] string? search = null,
            [FromQuery] string? stage = null)
        {
            var query = _db.Deals
                .Include(d => d.Customer).ThenInclude(c => c!.Person)
                .Include(d => d.Property)
                .Include(d => d.Agent).ThenInclude(u => u!.Person)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(stage) && !stage.Equals("All", StringComparison.OrdinalIgnoreCase))
            {
                query = query.Where(d => d.Stage == stage);
            }

            if (!string.IsNullOrWhiteSpace(search))
            {
                string s = search.Trim();
                query = query.Where(d =>
                    (d.Customer != null && (d.Customer.Person.FirstName.Contains(s) || d.Customer.Person.LastName.Contains(s))) ||
                    (d.Property != null && d.Property.Address.Contains(s)) ||
                    d.Stage.Contains(s) ||
                    (d.PaymentScheme != null && d.PaymentScheme.Contains(s)) ||
                    (d.Agent != null && (d.Agent.Person.FirstName.Contains(s) || d.Agent.Person.LastName.Contains(s))));
            }

            if (page.HasValue || pageSize.HasValue)
            {
                int pageNum = page.GetValueOrDefault(1);
                int size = pageSize.GetValueOrDefault(25);
                if (pageNum < 1) pageNum = 1;
                if (size < 1) size = 25;

                int totalCount = await query.CountAsync();
                var pagedList = await query.OrderByDescending(d => d.CreatedAt)
                    .Skip((pageNum - 1) * size)
                    .Take(size)
                    .ToListAsync();

                return Ok(new PagedResult<Deal>(pagedList, totalCount, pageNum, size));
            }

            var items = await query.ToListAsync();
            return Ok(items);
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(int id)
        {
            var item = await _db.Deals.FindAsync(id);
            return item is null ? NotFound() : Ok(item);
        }

        [HttpPost]
        public async Task<IActionResult> Create(Deal deal)
        {
            if (deal.CreatedByUserId <= 0)
            {
                deal.CreatedByUserId = 1;
            }
            deal.CreatedAt = DateTime.UtcNow;
            deal.AgentId = null; // R23. Default state is Unassigned

            _db.Deals.Add(deal);
            await _db.SaveChangesAsync();

            return Created($"/api/deals/{deal.DealId}", deal);
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> Update(int id, Deal updated)
        {
            var item = await _db.Deals.FindAsync(id);
            if (item is null) return NotFound();

            item.CustomerId = updated.CustomerId;
            item.PropertyId = updated.PropertyId;
            item.AgentId = updated.AgentId;
            item.Value = updated.Value;
            item.CommissionRate = updated.CommissionRate;
            item.Stage = updated.Stage;
            item.ExpectedCloseDate = updated.ExpectedCloseDate;

            await _db.SaveChangesAsync();
            return Ok(item);
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            var item = await _db.Deals.FindAsync(id);
            if (item is null) return NotFound();

            _db.Deals.Remove(item);
            await _db.SaveChangesAsync();
            return NoContent();
        }
    }
}