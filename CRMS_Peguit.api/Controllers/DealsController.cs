using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using CRMS_Peguit.domain.entities;
using CRMS_Peguit.infrastructure.data;

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
        public async Task<IActionResult> GetAll()
        {
            var items = await _db.Deals.ToListAsync();
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
            var tenantResolver = HttpContext.RequestServices
                .GetRequiredService<ITenantResolver>();

            deal.TenantId = tenantResolver.GetTenantId();
            deal.CreatedAt = DateTime.UtcNow;

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