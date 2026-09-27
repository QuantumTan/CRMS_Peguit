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
    public class CreateUserRequest
    {
        public string? FirstName { get; set; }
        public string? MiddleName { get; set; }
        public string? LastName { get; set; }
        public string? Suffix { get; set; }
        public string? Email { get; set; }
        public int RoleId { get; set; }
        public int? BranchId { get; set; }
        public string? Password { get; set; }

        // Support payload from WinForms UserApiService: new { user, plainTextPassword }
        public User? User { get; set; }
        public string? PlainTextPassword { get; set; }

        public string GetEffectiveFirstName() => User?.FirstName ?? FirstName ?? string.Empty;
        public string? GetEffectiveMiddleName() => User?.MiddleName ?? MiddleName;
        public string GetEffectiveLastName() => User?.LastName ?? LastName ?? string.Empty;
        public string? GetEffectiveSuffix() => User?.Suffix ?? Suffix;
        public string GetEffectiveEmail() => User?.Email ?? Email ?? string.Empty;
        public int GetEffectiveRoleId() => User != null && User.RoleId > 0 ? User.RoleId : RoleId;
        public int? GetEffectiveBranchId() => User?.BranchId ?? BranchId;
        public string GetEffectivePassword() => !string.IsNullOrEmpty(Password) ? Password : (PlainTextPassword ?? string.Empty);
    }

    public class UserDto
    {
        public int UserId { get; set; }
        public int PersonId { get; set; }
        public string FirstName { get; set; } = string.Empty;
        public string? MiddleName { get; set; }
        public string LastName { get; set; } = string.Empty;
        public string? Suffix { get; set; }
        public string Email { get; set; } = string.Empty;
        public string? Phone { get; set; }
        public string FullName { get; set; } = string.Empty;
        public int RoleId { get; set; }
        public string Status { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public int? BranchId { get; set; }
        public Role? Role { get; set; }
        public Branch? Branch { get; set; }
    }

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
            CurrentUser.UserId > 0 &&
            (ApiSecurityHelper.IsAdmin(CurrentUser.Role) || ApiSecurityHelper.IsSuperAdmin(CurrentUser.Role));

        private static UserDto ToDto(User u) => new()
        {
            UserId = u.UserId,
            PersonId = u.PersonId,
            FirstName = u.FirstName,
            MiddleName = u.MiddleName,
            LastName = u.LastName,
            Suffix = u.Suffix,
            Email = u.Email,
            Phone = u.Phone,
            FullName = u.FullName,
            RoleId = u.RoleId,
            Status = u.Status,
            CreatedAt = u.CreatedAt,
            BranchId = u.BranchId,
            Role = u.Role,
            Branch = u.Branch
        };

        [HttpGet]
        public async Task<IActionResult> GetAll([FromQuery] bool includeInactive = false, [FromQuery] int? branchId = null)
        {
            if (!IsAdmin)
                return StatusCode(StatusCodes.Status403Forbidden, "Only Admins can access users list.");

            var query = _db.Users
                .Include(u => u.Branch)
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
                .OrderBy(u => u.FirstName)
                .ThenBy(u => u.LastName)
                .ToListAsync();

            return Ok(list.Select(ToDto).ToList());
        }

        [HttpGet("{id:int}")]
        public async Task<IActionResult> GetById(int id)
        {
            if (!IsAdmin)
                return StatusCode(StatusCodes.Status403Forbidden, "Only Admins can access user details.");

            var user = await _db.Users
                .Include(u => u.Branch)
                .Include(u => u.Role)
                .AsNoTracking()
                .SingleOrDefaultAsync(u => u.UserId == id);

            return user is null ? NotFound() : Ok(ToDto(user));
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

            var email = req.GetEffectiveEmail().Trim();
            if (string.IsNullOrWhiteSpace(email))
            {
                return BadRequest("Email is required.");
            }

            if (await _db.Users.AnyAsync(u => u.Email == email))
            {
                return BadRequest("A user with this email already exists in your tenant.");
            }

            int roleId = req.GetEffectiveRoleId();
            var allowedRoleIds = await _db.Roles
                .Where(r => r.RoleName == "Manager" || r.RoleName == "Agent" || r.RoleName == "Sales Staff")
                .Select(r => r.RoleId)
                .ToListAsync();

            if (!allowedRoleIds.Contains(roleId))
            {
                return BadRequest("RoleId must be an allowed managed role (Manager, Agent, Sales Staff). Assigning SuperAdmin is not permitted.");
            }

            var targetRole = await _db.Roles.FirstOrDefaultAsync(r => r.RoleId == roleId);
            if (targetRole == null || ApiSecurityHelper.IsSuperAdmin(targetRole.RoleName))
            {
                return BadRequest("Cannot assign SuperAdmin role via this API.");
            }

            var password = req.GetEffectivePassword();
            if (string.IsNullOrWhiteSpace(password))
            {
                return BadRequest("Password is required.");
            }

            var user = new User
            {
                FirstName = req.GetEffectiveFirstName().Trim(),
                MiddleName = req.GetEffectiveMiddleName()?.Trim(),
                LastName = req.GetEffectiveLastName().Trim(),
                Suffix = req.GetEffectiveSuffix()?.Trim(),
                Email = email,
                RoleId = roleId,
                BranchId = req.GetEffectiveBranchId(),
                PasswordHash = PasswordHasher.Hash(password),
                Status = "active",
                CreatedAt = DateTime.UtcNow
            };

            _db.Users.Add(user);
            await _db.SaveChangesAsync();

            return CreatedAtAction(nameof(GetById), new { id = user.UserId }, ToDto(user));
        }

        [HttpPut("{id:int}")]
        public async Task<IActionResult> Update(int id, [FromBody] User updated)
        {
            if (!IsAdmin)
                return StatusCode(StatusCodes.Status403Forbidden, "Only Admins can update users.");

            var existing = await _db.Users
                .Include(u => u.Role)
                .SingleOrDefaultAsync(u => u.UserId == id);

            if (existing == null) return NotFound();

            if (existing.Role != null && ApiSecurityHelper.IsSuperAdmin(existing.Role.RoleName))
            {
                return StatusCode(StatusCodes.Status403Forbidden, "Cannot modify SuperAdmin accounts via this API.");
            }

            if (updated.RoleId > 0)
            {
                var allowedRoleIds = await _db.Roles
                    .Where(r => r.RoleName == "Manager" || r.RoleName == "Agent" || r.RoleName == "Sales Staff")
                    .Select(r => r.RoleId)
                    .ToListAsync();

                if (!allowedRoleIds.Contains(updated.RoleId))
                {
                    return BadRequest("RoleId must be an allowed managed role (Manager, Agent, Sales Staff). Assigning SuperAdmin is not permitted.");
                }

                var targetRole = await _db.Roles.FirstOrDefaultAsync(r => r.RoleId == updated.RoleId);
                if (targetRole == null || ApiSecurityHelper.IsSuperAdmin(targetRole.RoleName))
                {
                    return BadRequest("Cannot assign SuperAdmin role via this API.");
                }

                existing.RoleId = updated.RoleId;
            }

            if (await _db.Users.AnyAsync(u => u.Email == updated.Email.Trim() && u.UserId != id))
            {
                return BadRequest("A user with this email already exists in your tenant.");
            }

            existing.FirstName = updated.FirstName;
            existing.MiddleName = updated.MiddleName;
            existing.LastName = updated.LastName;
            existing.Suffix = updated.Suffix;
            existing.Email = updated.Email;
            existing.BranchId = updated.BranchId;

            await _db.SaveChangesAsync();
            return Ok(ToDto(existing));
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

            var user = await _db.Users
                .Include(u => u.Role)
                .SingleOrDefaultAsync(u => u.UserId == id);

            if (user == null) return NotFound();

            if (user.Role != null && ApiSecurityHelper.IsSuperAdmin(user.Role.RoleName))
            {
                return StatusCode(StatusCodes.Status403Forbidden, "Cannot deactivate SuperAdmin accounts via this API.");
            }

            user.Status = "inactive";
            await _db.SaveChangesAsync();
            return Ok(new { success = true });
        }

        [HttpPut("{id:int}/reactivate")]
        public async Task<IActionResult> Reactivate(int id)
        {
            if (!IsAdmin)
                return StatusCode(StatusCodes.Status403Forbidden, "Only Admins can reactivate users.");

            var user = await _db.Users
                .Include(u => u.Role)
                .SingleOrDefaultAsync(u => u.UserId == id);

            if (user == null) return NotFound();

            if (user.Role != null && ApiSecurityHelper.IsSuperAdmin(user.Role.RoleName))
            {
                return StatusCode(StatusCodes.Status403Forbidden, "Cannot reactivate SuperAdmin accounts via this API.");
            }

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
                .Include(u => u.Role)
                .Include(u => u.Branch)
                .AsNoTracking()
                .Where(u => u.Status.ToLower() == "inactive")
                .OrderBy(u => u.LastName)
                .ThenBy(u => u.FirstName)
                .ToListAsync();

            return Ok(users.Select(ToDto).ToList());
        }

        [HttpPut("{id:int}/change-password")]
        public async Task<IActionResult> ChangePassword(int id, [FromBody] ChangePasswordRequest req)
        {
            if (!IsAdmin)
                return StatusCode(StatusCodes.Status403Forbidden, "Only Admins can change user passwords.");

            var user = await _db.Users
                .Include(u => u.Role)
                .SingleOrDefaultAsync(u => u.UserId == id);

            if (user == null) return NotFound();

            if (user.Role != null && ApiSecurityHelper.IsSuperAdmin(user.Role.RoleName))
            {
                return StatusCode(StatusCodes.Status403Forbidden, "Cannot change SuperAdmin passwords via this API.");
            }

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
                .Where(u => managedRoles.Contains(u.Role.RoleName))
                .OrderByDescending(u => u.Role.RoleName == "Manager")
                .ThenBy(u => u.FirstName)
                .ThenBy(u => u.LastName)
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
