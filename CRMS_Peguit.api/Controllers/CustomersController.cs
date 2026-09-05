using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using CRMS_Peguit.domain.entities;
using CRMS_Peguit.infrastructure.data;

namespace CRMS_Peguit.api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class CustomersController : ControllerBase
    {
        private readonly RealEstateDbContext _db;

        public CustomersController(RealEstateDbContext db)
        {
            _db = db;
        }

        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var customers = await _db.Customers.ToListAsync();
            return Ok(customers);
        }

        [HttpGet("{id:int}")]
        public async Task<IActionResult> GetById(int id)
        {
            var customer = await _db.Customers
                .SingleOrDefaultAsync(x => x.CustomerId == id);

            return customer is null ? NotFound() : Ok(customer);
        }

        [HttpPost]
        public async Task<IActionResult> Create(Customer customer)
        {
            var tenantResolver = HttpContext.RequestServices
                .GetRequiredService<ITenantResolver>();

            customer.TenantId = tenantResolver.GetTenantId();
            customer.CreatedAt = DateTime.UtcNow;
            customer.IsDeleted = false;
            customer.DeletedAt = null;

            _db.Customers.Add(customer);
            await _db.SaveChangesAsync();

            return CreatedAtAction(nameof(GetById),
                new { id = customer.CustomerId }, customer);
        }

        [HttpPut("{id:int}")]
        public async Task<IActionResult> Update(int id, Customer updated)
        {
            var item = await _db.Customers
                .SingleOrDefaultAsync(x => x.CustomerId == id);
            if (item is null) return NotFound();

            item.FirstName = updated.FirstName;
            item.MiddleName = updated.MiddleName;
            item.LastName = updated.LastName;
            item.Suffix = updated.Suffix;
            item.Phone = updated.Phone;
            item.Email = updated.Email;
            item.Type = updated.Type;
            item.Status = updated.Status;
            item.AssignedAgentId = updated.AssignedAgentId;

            await _db.SaveChangesAsync();
            return Ok(item);
        }

        [HttpDelete("{id:int}")]
        public async Task<IActionResult> Delete(int id)
        {
            var item = await _db.Customers
                .SingleOrDefaultAsync(x => x.CustomerId == id);
            if (item is null) return NotFound();

            // Soft delete
            item.IsDeleted = true;
            item.DeletedAt = DateTime.UtcNow;

            await _db.SaveChangesAsync();
            return NoContent();
        }
    }
}