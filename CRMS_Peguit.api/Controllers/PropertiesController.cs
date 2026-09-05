using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using CRMS_Peguit.domain.entities;
using CRMS_Peguit.infrastructure.data;

namespace CRMS_Peguit.api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class PropertiesController : ControllerBase
    {
        private readonly RealEstateDbContext _db;

        public PropertiesController(RealEstateDbContext db)
        {
            _db = db;
        }

        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var items = await _db.Properties.ToListAsync();
            return Ok(items);
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(int id)
        {
            var item = await _db.Properties.FindAsync(id);
            return item is null ? NotFound() : Ok(item);
        }

        [HttpPost]
        public async Task<IActionResult> Create(Property property)
        {
            var tenantResolver = HttpContext.RequestServices
                .GetRequiredService<ITenantResolver>();

            property.TenantId = tenantResolver.GetTenantId();
            property.CreatedAt = DateTime.UtcNow;

            _db.Properties.Add(property);
            await _db.SaveChangesAsync();

            return Created($"/api/properties/{property.PropertyId}", property);
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> Update(int id, Property updated)
        {
            var item = await _db.Properties.FindAsync(id);
            if (item is null) return NotFound();

            item.Address = updated.Address;
            item.PropertyType = updated.PropertyType;
            item.Price = updated.Price;
            item.Status = updated.Status;
            item.OwnerCustomerId = updated.OwnerCustomerId;
            item.ListedByAgentId = updated.ListedByAgentId;

            await _db.SaveChangesAsync();
            return Ok(item);
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            var item = await _db.Properties.FindAsync(id);
            if (item is null) return NotFound();

            _db.Properties.Remove(item);
            await _db.SaveChangesAsync();
            return NoContent();
        }
    }
}