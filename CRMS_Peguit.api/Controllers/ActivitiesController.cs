using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using CRMS_Peguit.domain.entities;
using CRMS_Peguit.infrastructure.data;

namespace CRMS_Peguit.api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ActivitiesController : ControllerBase
    {
        private readonly RealEstateDbContext _db;

        public ActivitiesController(RealEstateDbContext db)
        {
            _db = db;
        }

        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var items = await _db.Activities.ToListAsync();
            return Ok(items);
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(int id)
        {
            var item = await _db.Activities.FindAsync(id);
            return item is null ? NotFound() : Ok(item);
        }

        [HttpPost]
        public async Task<IActionResult> Create(Activity activity)
        {
            var tenantResolver = HttpContext.RequestServices
                .GetRequiredService<ITenantResolver>();

            activity.TenantId = tenantResolver.GetTenantId();
            activity.ActivityDate = DateTime.UtcNow;

            _db.Activities.Add(activity);
            await _db.SaveChangesAsync();

            return Created($"/api/activities/{activity.ActivityId}", activity);
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> Update(int id, Activity updated)
        {
            var item = await _db.Activities.FindAsync(id);
            if (item is null) return NotFound();

            item.Type = updated.Type;
            item.RelatedLeadId = updated.RelatedLeadId;
            item.RelatedCustomerId = updated.RelatedCustomerId;
            item.LoggedByAgentId = updated.LoggedByAgentId;
            item.Notes = updated.Notes;

            await _db.SaveChangesAsync();
            return Ok(item);
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            var item = await _db.Activities.FindAsync(id);
            if (item is null) return NotFound();

            _db.Activities.Remove(item);
            await _db.SaveChangesAsync();
            return NoContent();
        }
    }
}