using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using CRMS_Peguit.domain.entities;
using CRMS_Peguit.infrastructure.data;

using CRMS_Peguit.domain.Common;

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
        public async Task<IActionResult> GetAll(
            [FromQuery] int? page = null,
            [FromQuery] int? pageSize = null,
            [FromQuery] string? search = null,
            [FromQuery] string? status = null)
        {
            var query = _db.Properties.AsQueryable();

            if (!string.IsNullOrWhiteSpace(search))
            {
                string s = search.Trim();
                query = query.Where(p =>
                    p.Address.Contains(s) ||
                    (p.PropertyType != null && p.PropertyType.Contains(s)) ||
                    p.Status.Contains(s));
            }

            if (!string.IsNullOrWhiteSpace(status) && !status.Equals("All", StringComparison.OrdinalIgnoreCase))
            {
                query = query.Where(p => p.Status == status);
            }

            if (page.HasValue || pageSize.HasValue)
            {
                int pageNum = page.GetValueOrDefault(1);
                int size = pageSize.GetValueOrDefault(25);
                if (pageNum < 1) pageNum = 1;
                if (size < 1) size = 25;

                int totalCount = await query.CountAsync();
                var pagedList = await query.OrderByDescending(p => p.CreatedAt)
                    .Skip((pageNum - 1) * size)
                    .Take(size)
                    .ToListAsync();

                return Ok(new PagedResult<Property>(pagedList, totalCount, pageNum, size));
            }

            var items = await query.ToListAsync();
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
            if (property.CreatedByUserId <= 0)
            {
                property.CreatedByUserId = 1;
            }
            property.CreatedAt = DateTime.UtcNow;
            property.ListedByAgentId = null; // R23. Default state is Unassigned
            property.AssignmentStatus = string.IsNullOrWhiteSpace(property.AssignmentStatus)
                ? "pending_review"
                : property.AssignmentStatus;

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
            item.AssignmentStatus = updated.AssignmentStatus;
            item.AssignmentReviewedByUserId = updated.AssignmentReviewedByUserId;
            item.AssignmentReviewedAt = updated.AssignmentReviewedAt;
            item.AssignmentReviewNotes = updated.AssignmentReviewNotes;

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
