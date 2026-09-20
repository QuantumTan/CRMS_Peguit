using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using CRMS_Peguit.domain.entities;
using CRMS_Peguit.infrastructure.data;

using CRMS_Peguit.domain.Common;

namespace CRMS_Peguit.api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class LeadsController : ControllerBase
    {
        private readonly RealEstateDbContext _db;

        public LeadsController(RealEstateDbContext db)
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
            var query = _db.Leads
                .Include(l => l.Person)
                .Where(l => !l.IsDeleted)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(search))
            {
                string s = search.Trim();
                query = query.Where(l => (l.Person != null && (
                    l.Person.FirstName.Contains(s) ||
                    l.Person.LastName.Contains(s) ||
                    (l.Person.Email != null && l.Person.Email.Contains(s)) ||
                    (l.Person.Phone != null && l.Person.Phone.Contains(s)))) ||
                    (l.Source != null && l.Source.Contains(s)) ||
                    (l.Notes != null && l.Notes.Contains(s)));
            }

            if (!string.IsNullOrWhiteSpace(stage) && !stage.Equals("All", StringComparison.OrdinalIgnoreCase))
            {
                query = query.Where(l => l.Stage == stage);
            }

            if (page.HasValue || pageSize.HasValue)
            {
                int pageNum = page.GetValueOrDefault(1);
                int size = pageSize.GetValueOrDefault(25);
                if (pageNum < 1) pageNum = 1;
                if (size < 1) size = 25;

                int totalCount = await query.CountAsync();
                var items = await query.OrderByDescending(l => l.CreatedAt)
                    .Skip((pageNum - 1) * size)
                    .Take(size)
                    .ToListAsync();

                return Ok(new PagedResult<Lead>(items, totalCount, pageNum, size));
            }

            var leads = await query.ToListAsync();
            return Ok(leads);
        }

        [HttpGet("{id:int}")]
        public async Task<IActionResult> GetById(int id)
        {
            var lead = await _db.Leads
                .SingleOrDefaultAsync(x => x.LeadId == id);

            return lead is null ? NotFound() : Ok(lead);
        }

        [HttpPost]
        public async Task<IActionResult> Create(Lead lead)
        {
            if (lead.PersonId <= 0 && lead.Person == null)
            {
                lead.Person = new CRMS_Peguit.domain.entities.Person
                {
                    FirstName = lead.FirstName,
                    MiddleName = lead.MiddleName,
                    LastName = lead.LastName,
                    Suffix = lead.Suffix,
                    Email = lead.Email,
                    Phone = lead.Phone
                };
            }
            if (lead.CreatedByUserId <= 0)
            {
                lead.CreatedByUserId = 1;
            }
            lead.CreatedAt = DateTime.UtcNow;
            lead.IsDeleted = false;
            lead.DeletedAt = null;
            lead.AssignedAgentId = null; // R23. Default state is Unassigned
            lead.AssignmentStatus = string.IsNullOrWhiteSpace(lead.AssignmentStatus)
                ? "pending_review"
                : lead.AssignmentStatus;

            _db.Leads.Add(lead);
            await _db.SaveChangesAsync();

            return CreatedAtAction(nameof(GetById),
                new { id = lead.LeadId }, lead);
        }

        [HttpPut("{id:int}")]
        public async Task<IActionResult> Update(int id, Lead updated)
        {
            var item = await _db.Leads
                .SingleOrDefaultAsync(x => x.LeadId == id);
            if (item is null) return NotFound();

            item.FirstName = updated.FirstName;
            item.MiddleName = updated.MiddleName;
            item.LastName = updated.LastName;
            item.Suffix = updated.Suffix;
            item.Phone = updated.Phone;
            item.Email = updated.Email;
            item.Source = updated.Source;
            item.Stage = updated.Stage;
            item.Notes = updated.Notes;
            item.Priority = updated.Priority;
            item.ExpectedValue = updated.ExpectedValue;
            item.AssignedAgentId = updated.AssignedAgentId;
            item.AssignmentStatus = updated.AssignmentStatus;
            item.AssignmentReviewedByUserId = updated.AssignmentReviewedByUserId;
            item.AssignmentReviewedAt = updated.AssignmentReviewedAt;
            item.AssignmentReviewNotes = updated.AssignmentReviewNotes;

            await _db.SaveChangesAsync();
            return Ok(item);
        }

        [HttpPost("{id:int}/convert")]
        public async Task<IActionResult> ConvertToCustomer(int id)
        {
            var item = await _db.Leads.SingleOrDefaultAsync(x => x.LeadId == id);
            if (item is null) return NotFound();

            if (string.Equals(item.Stage, "converted", StringComparison.OrdinalIgnoreCase))
                return BadRequest("This lead has already been converted.");

            var customer = new Customer
            {
                PersonId = item.PersonId,
                Type = "buyer",
                Status = "active",
                AssignedAgentId = item.AssignedAgentId,
                CreatedByUserId = item.CreatedByUserId,
                AssignmentStatus = item.AssignmentStatus,
                AssignmentReviewedByUserId = item.AssignmentReviewedByUserId,
                AssignmentReviewedAt = item.AssignmentReviewedAt,
                AssignmentReviewNotes = item.AssignmentReviewNotes,
                CreatedAt = DateTime.UtcNow,
                IsDeleted = false,
                DeletedAt = null
            };

            _db.Customers.Add(customer);
            await _db.SaveChangesAsync();

            item.Stage = "converted";
            item.ConvertedCustomerId = customer.CustomerId;
            await _db.SaveChangesAsync();

            return Ok(customer);
        }

        [HttpDelete("{id:int}")]
        public async Task<IActionResult> Delete(int id)
        {
            var item = await _db.Leads
                .SingleOrDefaultAsync(x => x.LeadId == id);
            if (item is null) return NotFound();

            // Soft delete
            item.IsDeleted = true;
            item.DeletedAt = DateTime.UtcNow;

            await _db.SaveChangesAsync();
            return NoContent();
        }
    }
}