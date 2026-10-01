using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using CRMS_Peguit.domain.entities;
using CRMS_Peguit.infrastructure.data;

namespace CRMS_Peguit.api.Controllers
{
    public class BranchDto
    {
        public int BranchId { get; set; }
        public int TenantId { get; set; }
        public string BranchCode { get; set; } = string.Empty;
        public string BranchName { get; set; } = string.Empty;
        public string Address { get; set; } = string.Empty;
        public string Phone { get; set; } = string.Empty;
        public bool IsActive { get; set; }
        public DateTime CreatedAt { get; set; }
        public int AssignedAgentsCount { get; set; }
        public int PropertiesCount { get; set; }
        public int LeadsCount { get; set; }
        public int DealsCount { get; set; }
        public decimal TotalDealVolume { get; set; }
    }

    [ApiController]
    [Route("api/[controller]")]
    public class BranchesController : ControllerBase
    {
        private readonly RealEstateDbContext _db;

        public BranchesController(RealEstateDbContext db)
        {
            _db = db;
        }

        private (int UserId, string Role, int TenantId) CurrentUser =>
            ApiSecurityHelper.GetCurrentUserInfo(HttpContext);

        [HttpGet]
        public async Task<IActionResult> GetAllBranches([FromQuery] int? tenantId = null)
        {
            var user = CurrentUser;
            int tid = user.TenantId;
            if (ApiSecurityHelper.IsSuperAdmin(user.Role) && tenantId.HasValue && tenantId.Value > 0)
            {
                tid = tenantId.Value;
            }

            if (tid <= 0) return Unauthorized();

            var branches = await _db.Branches
                .Where(b => b.TenantId == tid)
                .OrderBy(b => b.BranchCode)
                .ToListAsync();

            if (branches.Count == 0)
            {
                return Ok(new List<BranchDto>());
            }

            var branchIds = branches.Select(b => b.BranchId).ToList();

            var agentCounts = await _db.Users
                .Where(u => u.BranchId.HasValue && branchIds.Contains(u.BranchId.Value))
                .GroupBy(u => u.BranchId!.Value)
                .Select(g => new { BranchId = g.Key, Count = g.Count() })
                .ToDictionaryAsync(x => x.BranchId, x => x.Count);

            var propCounts = await _db.Properties
                .Where(p => p.BranchId.HasValue && branchIds.Contains(p.BranchId.Value))
                .GroupBy(p => p.BranchId!.Value)
                .Select(g => new { BranchId = g.Key, Count = g.Count() })
                .ToDictionaryAsync(x => x.BranchId, x => x.Count);

            var leadCounts = await _db.Leads
                .Where(l => l.BranchId.HasValue && branchIds.Contains(l.BranchId.Value))
                .GroupBy(l => l.BranchId!.Value)
                .Select(g => new { BranchId = g.Key, Count = g.Count() })
                .ToDictionaryAsync(x => x.BranchId, x => x.Count);

            var dealStats = await _db.Deals
                .Where(d => d.BranchId.HasValue && branchIds.Contains(d.BranchId.Value))
                .GroupBy(d => d.BranchId!.Value)
                .Select(g => new { BranchId = g.Key, Count = g.Count(), Volume = g.Sum(d => d.Value) })
                .ToDictionaryAsync(x => x.BranchId, x => new { x.Count, x.Volume });

            var list = new List<BranchDto>();
            foreach (var b in branches)
            {
                int agents = agentCounts.TryGetValue(b.BranchId, out var ac) ? ac : 0;
                int props = propCounts.TryGetValue(b.BranchId, out var pc) ? pc : 0;
                int leads = leadCounts.TryGetValue(b.BranchId, out var lc) ? lc : 0;
                int deals = dealStats.TryGetValue(b.BranchId, out var ds) ? ds.Count : 0;
                decimal vol = dealStats.TryGetValue(b.BranchId, out var dsVol) ? dsVol.Volume : 0m;

                list.Add(new BranchDto
                {
                    BranchId = b.BranchId,
                    TenantId = b.TenantId,
                    BranchCode = b.BranchCode,
                    BranchName = b.BranchName,
                    Address = b.Address,
                    Phone = b.Phone,
                    IsActive = b.IsActive,
                    CreatedAt = b.CreatedAt,
                    AssignedAgentsCount = agents,
                    PropertiesCount = props,
                    LeadsCount = leads,
                    DealsCount = deals,
                    TotalDealVolume = vol
                });
            }

            return Ok(list);
        }

        [HttpPost]
        public async Task<IActionResult> SaveBranch([FromBody] Branch branch)
        {
            var user = CurrentUser;
            if (user.TenantId <= 0) return Unauthorized();
            if (!ApiSecurityHelper.IsAdmin(user.Role))
                return StatusCode(StatusCodes.Status403Forbidden, "Only Admin can manage branches.");

            if (branch is null || string.IsNullOrWhiteSpace(branch.BranchCode) || string.IsNullOrWhiteSpace(branch.BranchName))
                return BadRequest(new { message = "Branch code and Branch name are required." });

            string normalizedCode = branch.BranchCode.Trim();
            bool duplicateCode = await _db.Branches.AnyAsync(b =>
                b.BranchId != branch.BranchId && b.BranchCode == normalizedCode);
            if (duplicateCode)
                return BadRequest(new { message = "Branch code must be unique within the tenant." });

            if (branch.BranchId <= 0)
            {
                branch.TenantId = user.TenantId;
                branch.BranchCode = normalizedCode;
                branch.CreatedAt = DateTime.UtcNow;
                _db.Branches.Add(branch);
            }
            else
            {
                var existing = await _db.Branches.FirstOrDefaultAsync(b => b.BranchId == branch.BranchId && b.TenantId == user.TenantId);
                if (existing == null) return NotFound();

                if (existing.IsActive && !branch.IsActive &&
                    await _db.Users.AnyAsync(u => u.BranchId == existing.BranchId && u.Status == "active"))
                {
                    return BadRequest(new { message = "Reassign or deactivate the branch's active user accounts before deactivating this branch." });
                }

                existing.BranchCode = normalizedCode;
                existing.BranchName = branch.BranchName;
                existing.Address = branch.Address;
                existing.Phone = branch.Phone;
                existing.IsActive = branch.IsActive;
            }

            await _db.SaveChangesAsync();
            return Ok(new { success = true });
        }

        [HttpPut("{id:int}/toggle-status")]
        public async Task<IActionResult> ToggleBranchStatus(int id)
        {
            var user = CurrentUser;
            if (!ApiSecurityHelper.IsAdmin(user.Role))
                return StatusCode(StatusCodes.Status403Forbidden, "Only Admin can manage branches.");

            var branch = await _db.Branches.FirstOrDefaultAsync(b => b.BranchId == id);
            if (branch == null) return NotFound();

            if (branch.IsActive &&
                await _db.Users.AnyAsync(u => u.BranchId == branch.BranchId && u.Status == "active"))
            {
                return BadRequest(new { message = "Reassign or deactivate the branch's active user accounts before deactivating this branch." });
            }

            branch.IsActive = !branch.IsActive;
            await _db.SaveChangesAsync();
            return Ok(new { success = true, isActive = branch.IsActive });
        }
    }
}
