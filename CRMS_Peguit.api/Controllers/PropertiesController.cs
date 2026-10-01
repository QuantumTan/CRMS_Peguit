using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using CRMS_Peguit.domain.entities;
using CRMS_Peguit.domain.Common;
using CRMS_Peguit.infrastructure.data;

namespace CRMS_Peguit.api.Controllers
{
    public record PropertyCountsDto(int Total, int Available, int Pending, int Sold);
    public record CustomerPickerDto(int CustomerId, string FullName, string? Email);

    [ApiController]
    [Route("api/[controller]")]
    public class PropertiesController : ControllerBase
    {
        private readonly RealEstateDbContext _db;

        public PropertiesController(RealEstateDbContext db)
        {
            _db = db;
        }

        private (int UserId, string Role, int TenantId) CurrentUser =>
            ApiSecurityHelper.GetCurrentUserInfo(HttpContext);

        [HttpGet]
        public async Task<IActionResult> GetAll(
            [FromQuery] int? page = null,
            [FromQuery] int? pageSize = null,
            [FromQuery] string? search = null,
            [FromQuery] string? status = null)
        {
            var user = CurrentUser;
            var query = _db.Properties
                .Include(p => p.OwnerCustomer)
                .Include(p => p.ListedByAgent)
                .Include(p => p.CreatedByUser)
                .AsQueryable();

            // Ownership-Based Access Control:
            if (!ApiSecurityHelper.HasFullOversight(user.Role) && ApiSecurityHelper.IsAgent(user.Role) && user.UserId > 0)
            {
                query = query.Where(p =>
                    (p.ListedByAgentId.HasValue && p.ListedByAgentId.Value > 0)
                        ? p.ListedByAgentId.Value == user.UserId
                        : p.CreatedByUserId == user.UserId);
            }

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
                query = query.Where(p => p.Status.ToLower() == status.ToLower());
            }

            if (page.HasValue || pageSize.HasValue)
            {
                int pageNum = Math.Max(1, page.GetValueOrDefault(1));
                int size = Math.Max(1, pageSize.GetValueOrDefault(25));

                int totalCount = await query.CountAsync();
                var pagedList = await query.OrderByDescending(p => p.CreatedAt)
                    .Skip((pageNum - 1) * size)
                    .Take(size)
                    .ToListAsync();

                return Ok(new PagedResult<Property>(pagedList, totalCount, pageNum, size));
            }

            var items = await query.OrderByDescending(p => p.CreatedAt).ToListAsync();
            return Ok(items);
        }

        [HttpGet("counts")]
        public async Task<IActionResult> GetPropertyCounts()
        {
            var user = CurrentUser;
            var query = _db.Properties.AsNoTracking().AsQueryable();

            if (!ApiSecurityHelper.HasFullOversight(user.Role) && ApiSecurityHelper.IsAgent(user.Role) && user.UserId > 0)
            {
                query = query.Where(p =>
                    (p.ListedByAgentId.HasValue && p.ListedByAgentId.Value > 0)
                        ? p.ListedByAgentId.Value == user.UserId
                        : p.CreatedByUserId == user.UserId);
            }

            int total = await query.CountAsync();
            int available = await query.CountAsync(p => p.Status.ToLower() == "available" || p.Status.ToLower() == "for sale");
            int pending = await query.CountAsync(p => p.Status.ToLower() == "pending" || p.Status.ToLower() == "under contract");
            int sold = await query.CountAsync(p => p.Status.ToLower() == "sold");

            return Ok(new PropertyCountsDto(total, available, pending, sold));
        }

        [HttpGet("{id:int}")]
        public async Task<IActionResult> GetById(int id)
        {
            var user = CurrentUser;
            var item = await _db.Properties
                .Include(p => p.OwnerCustomer)
                .Include(p => p.ListedByAgent)
                .Include(p => p.CreatedByUser)
                .SingleOrDefaultAsync(p => p.PropertyId == id);

            if (item is null) return NotFound();

            if (!ApiSecurityHelper.HasFullOversight(user.Role) && ApiSecurityHelper.IsAgent(user.Role) && user.UserId > 0)
            {
                bool allowed = (item.ListedByAgentId.HasValue && item.ListedByAgentId.Value > 0)
                    ? item.ListedByAgentId.Value == user.UserId
                    : item.CreatedByUserId == user.UserId;
                if (!allowed) return Forbid();
            }

            return Ok(item);
        }

        [HttpPost]
        public async Task<IActionResult> Create(Property property)
        {
            var user = CurrentUser;
            if (user.UserId <= 0) return Unauthorized();

            if (string.IsNullOrWhiteSpace(property.Address))
                return BadRequest(new { message = "Property address is required." });
            if (property.Price < 0)
                return BadRequest(new { message = "Property price cannot be negative." });
            if (property.OwnerCustomerId <= 0)
                return BadRequest(new { message = "A valid OwnerCustomerId is required." });
            if (string.IsNullOrWhiteSpace(property.Status))
                property.Status = "Available";

            property.CreatedAt = DateTime.UtcNow;
            property.CreatedByUserId = user.UserId;

            if (ApiSecurityHelper.CanAssignRecords(user.Role))
            {
                if (property.ListedByAgentId <= 0) property.ListedByAgentId = null;
            }
            else
            {
                property.ListedByAgentId = null;
                property.AssignmentStatus = "pending_review";
            }

            _db.Properties.Add(property);
            await _db.SaveChangesAsync();

            return CreatedAtAction(nameof(GetById), new { id = property.PropertyId }, property);
        }

        [HttpPut("{id:int}")]
        public async Task<IActionResult> Update(int id, Property updated)
        {
            var user = CurrentUser;
            var item = await _db.Properties.FindAsync(id);
            if (item is null) return NotFound();

            if (!ApiSecurityHelper.HasFullOversight(user.Role) && ApiSecurityHelper.IsAgent(user.Role) && user.UserId > 0)
            {
                bool allowed = (item.ListedByAgentId.HasValue && item.ListedByAgentId.Value > 0)
                    ? item.ListedByAgentId.Value == user.UserId
                    : item.CreatedByUserId == user.UserId;
                if (!allowed) return Forbid();
            }

            if (string.IsNullOrWhiteSpace(updated.Address))
                return BadRequest(new { message = "Property address is required." });
            if (updated.Price < 0)
                return BadRequest(new { message = "Property price cannot be negative." });
            if (updated.OwnerCustomerId <= 0)
                return BadRequest(new { message = "A valid OwnerCustomerId is required." });

            item.Address = updated.Address;
            item.PropertyType = updated.PropertyType;
            item.Price = updated.Price;
            item.Status = updated.Status;
            item.OwnerCustomerId = updated.OwnerCustomerId;
            item.BranchId = updated.BranchId;

            if (ApiSecurityHelper.CanAssignRecords(user.Role))
            {
                item.ListedByAgentId = updated.ListedByAgentId <= 0 ? null : updated.ListedByAgentId;
                if (!string.IsNullOrWhiteSpace(updated.AssignmentStatus) && updated.AssignmentStatus != item.AssignmentStatus)
                {
                    item.AssignmentStatus = updated.AssignmentStatus;
                    item.AssignmentReviewedByUserId = user.UserId > 0 ? user.UserId : null;
                    item.AssignmentReviewedAt = DateTime.UtcNow;
                    item.AssignmentReviewNotes = updated.AssignmentReviewNotes;
                }
            }

            await _db.SaveChangesAsync();
            return Ok(item);
        }

        [HttpDelete("{id:int}")]
        public async Task<IActionResult> Delete(int id)
        {
            var user = CurrentUser;
            var item = await _db.Properties.FindAsync(id);
            if (item is null) return NotFound();

            if (!ApiSecurityHelper.HasFullOversight(user.Role) && ApiSecurityHelper.IsAgent(user.Role) && user.UserId > 0)
            {
                bool allowed = (item.ListedByAgentId.HasValue && item.ListedByAgentId.Value > 0)
                    ? item.ListedByAgentId.Value == user.UserId
                    : item.CreatedByUserId == user.UserId;
                if (!allowed) return Forbid();
            }

            if (await _db.Deals.AnyAsync(d => d.PropertyId == id))
            {
                return BadRequest(new { message = "Cannot delete property because it is associated with one or more deals. Please reassign or delete the associated deals first." });
            }

            _db.Properties.Remove(item);
            await _db.SaveChangesAsync();
            return NoContent();
        }

        [HttpPost("{id:int}/assign")]
        public async Task<IActionResult> AssignAgent(int id, [FromBody] AssignAgentRequest request)
        {
            var user = CurrentUser;
            if (!ApiSecurityHelper.CanAssignRecords(user.Role))
                return StatusCode(StatusCodes.Status403Forbidden, "Only Manager or Admin may assign records.");

            var item = await _db.Properties.FindAsync(id);
            if (item is null) return NotFound();

            var newAgentId = request.AgentId <= 0 ? null : request.AgentId;
            if (newAgentId.HasValue && !await _db.Users.AnyAsync(u => u.UserId == newAgentId.Value))
            {
                return BadRequest(new { message = $"Agent with ID {newAgentId.Value} does not exist." });
            }

            item.ListedByAgentId = newAgentId;
            item.AssignmentStatus = request.Approve ? "approved" : "pending_review";
            item.AssignmentReviewedByUserId = user.UserId > 0 ? user.UserId : null;
            item.AssignmentReviewedAt = DateTime.UtcNow;
            item.AssignmentReviewNotes = string.IsNullOrWhiteSpace(request.Notes) ? null : request.Notes.Trim();

            await _db.SaveChangesAsync();
            return Ok(item);
        }

        [HttpPost("{id:int}/approve")]
        public async Task<IActionResult> ApproveAssignment(int id, [FromBody] ApproveAssignmentRequest request)
        {
            var user = CurrentUser;
            if (!ApiSecurityHelper.CanAssignRecords(user.Role))
                return StatusCode(StatusCodes.Status403Forbidden, "Only Manager or Admin may approve assignments.");

            var item = await _db.Properties.FindAsync(id);
            if (item is null) return NotFound();

            item.AssignmentStatus = "approved";
            item.AssignmentReviewedByUserId = user.UserId > 0 ? user.UserId : null;
            item.AssignmentReviewedAt = DateTime.UtcNow;
            item.AssignmentReviewNotes = string.IsNullOrWhiteSpace(request.Notes) ? null : request.Notes.Trim();

            await _db.SaveChangesAsync();
            return Ok(item);
        }

        [HttpGet("pending-review")]
        public async Task<IActionResult> GetPendingReview()
        {
            var user = CurrentUser;
            if (!ApiSecurityHelper.HasFullOversight(user.Role))
                return StatusCode(StatusCodes.Status403Forbidden, "Only Manager or Admin can view records pending review.");

            var items = await _db.Properties
                .AsNoTracking()
                .Where(p => p.AssignmentStatus == "pending_review" || p.ListedByAgentId == null)
                .OrderByDescending(p => p.CreatedAt)
                .ToListAsync();
            return Ok(items);
        }

        [HttpGet("owners")]
        public async Task<IActionResult> GetOwnerCustomers()
        {
            var owners = await _db.Customers
                .AsNoTracking()
                .Where(c => !c.IsDeleted)
                .OrderBy(c => c.LastName)
                .ThenBy(c => c.FirstName)
                .Select(c => new CustomerPickerDto(c.CustomerId, c.FullName, c.Email))
                .ToListAsync();

            return Ok(owners);
        }

        [HttpGet("agents")]
        public async Task<IActionResult> GetAgents()
        {
            var agentRoleIds = await _db.Roles
                .AsNoTracking()
                .Where(r => r.RoleName.ToLower() == "agent" || r.RoleName.ToLower() == "salesstaff")
                .Select(r => r.RoleId)
                .ToListAsync();

            var agents = await _db.Users
                .AsNoTracking()
                .Where(u => agentRoleIds.Contains(u.RoleId) && u.Status != null && !u.Status.Equals("inactive", StringComparison.OrdinalIgnoreCase))
                .OrderBy(u => u.LastName)
                .ThenBy(u => u.FirstName)
                .Select(u => new AgentPickerDto(u.UserId, u.FullName, u.Email))
                .ToListAsync();

            return Ok(agents);
        }
    }
}
