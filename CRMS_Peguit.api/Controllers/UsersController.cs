using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using CRMS_Peguit.domain.entities;
using CRMS_Peguit.infrastructure.data;
using CRMS_Peguit.infrastructure.Security;

namespace CRMS_Peguit.api.Controllers
{
    public record CreateUserRequest(
        string FirstName,
        string? MiddleName,
        string LastName,
        string? Suffix,
        string Email,
        int RoleId,
        int? BranchId,
        string Password);

    public record ChangePasswordRequest(string NewPassword);
    public record TeamRosterDto(int UserId, string FullName, string RoleName, string Status);
    public record StaffCountsDto(int Managers, int Agents);

    [ApiController]
    [Route("api/[controller]")]
    public class UsersController : ControllerBase
    {
        private readonly RealEstateDbContext _db;

        public UsersController(RealEstateDbContext db)
        {
            _db = db;
        }

        private (int UserId, string Role, int TenantId) CurrentUser =>
            ApiSecurityHelper.GetCurrentUserInfo(HttpContext);

        private bool IsAdmin =>
            ApiSecurityHelper.IsAdmin(CurrentUser.Role) || ApiSecurityHelper.IsSuperAdmin(CurrentUser.Role);

        [HttpGet]
        public async Task<IActionResult> GetAll([FromQuery] bool includeInactive = false, [FromQuery] int? branchId = null)
        {
            if (!IsAdmin)
                return StatusCode(StatusCodes.Status403Forbidden, "Only Admins can access users list.");

            var query = _db.Users
                .Include(u => u.Branch)
                .Include(u => u.Person)
                .Include(u => u.Role)
                .AsNoTracking();

            if (!includeInactive)
            {
                query = query.Where(u => u.Status.ToLower() == "active");
            }

            if (branchId.HasValue && branchId.Value > 0)
            {
                query = query.Where(u => u.BranchId == branchId.Value);
            }

            var managedRoleIds = await _db.Roles
                .Where(r => r.RoleName == "Manager" || r.RoleName == "Agent" || r.RoleName == "Sales Staff")
                .Select(r => r.RoleId)
                .ToListAsync();

            query = query.Where(u => managedRoleIds.Contains(u.RoleId));

            var list = await query
                .OrderBy(u => u.Person.FirstName)
                .ThenBy(u => u.Person.LastName)
                .ToListAsync();

            return Ok(list);
        }

        [HttpGet("{id:int}")]
        public async Task<IActionResult> GetById(int id)
        {
            if (!IsAdmin)
                return StatusCode(StatusCodes.Status403Forbidden, "Only Admins can access user details.");

            var user = await _db.Users
                .Include(u => u.Branch)
                .Include(u => u.Person)
                .Include(u => u.Role)
                .AsNoTracking()
                .SingleOrDefaultAsync(u => u.UserId == id);

            return user is null ? NotFound() : Ok(user);
        }

        [HttpGet("roles")]
        public async Task<IActionResult> GetManagedRoles()
        {
            var roles = await _db.Roles
                .AsNoTracking()
                .Where(r => r.RoleName == "Manager" || r.RoleName == "Agent" || r.RoleName == "Sales Staff")
                .ToListAsync();
            return Ok(roles);
        }

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] CreateUserRequest req)
        {
            if (!IsAdmin)
                return StatusCode(StatusCodes.Status403Forbidden, "Only Admins can create user accounts.");

            if (await _db.Users.AnyAsync(u => u.Person.Email == req.Email.Trim()))
            {
                return BadRequest("A user with this email already exists in your tenant.");
            }

            var person = new Person
            {
                FirstName = req.FirstName.Trim(),
                MiddleName = req.MiddleName?.Trim(),
                LastName = req.LastName.Trim(),
                Suffix = req.Suffix?.Trim(),
                Email = req.Email.Trim()
            };

            var user = new User
            {
                Person = person,
                RoleId = req.RoleId,
                BranchId = req.BranchId,
                PasswordHash = PasswordHasher.Hash(req.Password),
                Status = "active",
                CreatedAt = DateTime.UtcNow
            };

            _db.Users.Add(user);
            await _db.SaveChangesAsync();

            return CreatedAtAction(nameof(GetById), new { id = user.UserId }, user);
        }

        [HttpPut("{id:int}")]
        public async Task<IActionResult> Update(int id, [FromBody] User updated)
        {
            if (!IsAdmin)
                return StatusCode(StatusCodes.Status403Forbidden, "Only Admins can update users.");

            var existing = await _db.Users.Include(u => u.Person).SingleOrDefaultAsync(u => u.UserId == id);
            if (existing == null) return NotFound();

            if (await _db.Users.AnyAsync(u => u.Person.Email == updated.Email.Trim() && u.UserId != id))
            {
                return BadRequest("A user with this email already exists in your tenant.");
            }

            existing.FirstName = updated.FirstName;
            existing.MiddleName = updated.MiddleName;
            existing.LastName = updated.LastName;
            existing.Suffix = updated.Suffix;
            existing.Email = updated.Email;
            existing.RoleId = updated.RoleId;
            existing.BranchId = updated.BranchId;

            await _db.SaveChangesAsync();
            return Ok(existing);
        }

        [HttpPut("{id:int}/deactivate")]
        public async Task<IActionResult> Deactivate(int id)
        {
            if (!IsAdmin)
                return StatusCode(StatusCodes.Status403Forbidden, "Only Admins can deactivate users.");

            if (id == CurrentUser.UserId)
            {
                return BadRequest("You cannot deactivate your own account.");
            }

            var user = await _db.Users.SingleOrDefaultAsync(u => u.UserId == id);
            if (user == null) return NotFound();

            user.Status = "inactive";
            await _db.SaveChangesAsync();
            return Ok(new { success = true });
        }

        [HttpPut("{id:int}/reactivate")]
        public async Task<IActionResult> Reactivate(int id)
        {
            if (!IsAdmin)
                return StatusCode(StatusCodes.Status403Forbidden, "Only Admins can reactivate users.");

            var user = await _db.Users.SingleOrDefaultAsync(u => u.UserId == id);
            if (user == null) return NotFound();

            user.Status = "active";
            await _db.SaveChangesAsync();
            return Ok(new { success = true });
        }

        [HttpGet("deactivated")]
        public async Task<IActionResult> GetDeactivated()
        {
            if (!IsAdmin)
                return StatusCode(StatusCodes.Status403Forbidden, "Only Admins can view deactivated users.");

            var users = await _db.Users
                .Include(u => u.Person)
                .Include(u => u.Role)
                .AsNoTracking()
                .Where(u => u.Status.ToLower() == "inactive")
                .OrderBy(u => u.Person.LastName)
                .ThenBy(u => u.Person.FirstName)
                .ToListAsync();

            return Ok(users);
        }

        [HttpPut("{id:int}/change-password")]
        public async Task<IActionResult> ChangePassword(int id, [FromBody] ChangePasswordRequest req)
        {
            if (!IsAdmin)
                return StatusCode(StatusCodes.Status403Forbidden, "Only Admins can change user passwords.");

            var user = await _db.Users.SingleOrDefaultAsync(u => u.UserId == id);
            if (user == null) return NotFound();

            user.PasswordHash = PasswordHasher.Hash(req.NewPassword);
            await _db.SaveChangesAsync();
            return Ok(new { success = true });
        }

        [HttpGet("stats/active-count")]
        public async Task<IActionResult> GetActiveUsersCount()
        {
            int count = await _db.Users.AsNoTracking().CountAsync(u => u.Status.ToLower() == "active");
            return Ok(new { count });
        }

        [HttpGet("stats/active-staff-counts")]
        public async Task<IActionResult> GetActiveStaffCounts()
        {
            int managers = await _db.Users.AsNoTracking()
                .CountAsync(u => u.Status.ToLower() == "active" && u.Role.RoleName == "Manager");
            int agents = await _db.Users.AsNoTracking()
                .CountAsync(u => u.Status.ToLower() == "active" && (u.Role.RoleName == "Agent" || u.Role.RoleName == "Sales Staff"));

            return Ok(new StaffCountsDto(managers, agents));
        }

        [HttpGet("stats/team-roster")]
        public async Task<IActionResult> GetTeamRoster([FromQuery] int maxCount = 8)
        {
            var managedRoles = new[] { "Manager", "Agent", "Sales Staff" };
            var users = await _db.Users
                .AsNoTracking()
                .Include(u => u.Role)
                .Include(u => u.Person)
                .Where(u => managedRoles.Contains(u.Role.RoleName))
                .OrderByDescending(u => u.Role.RoleName == "Manager")
                .ThenBy(u => u.Person.FirstName)
                .ThenBy(u => u.Person.LastName)
                .Take(maxCount)
                .ToListAsync();

            var list = users.Select(u => new TeamRosterDto(
                u.UserId,
                u.FullName,
                u.Role?.RoleName == "Sales Staff" ? "Agent" : (u.Role?.RoleName ?? "Agent"),
                string.Equals(u.Status, "active", StringComparison.OrdinalIgnoreCase) ? "Active" : "Inactive"
            )).ToList();

            return Ok(list);
        }
    }
}
