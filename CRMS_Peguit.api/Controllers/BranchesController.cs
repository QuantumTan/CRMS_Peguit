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
            int tid = tenantId ?? user.TenantId;
            if (tid <= 0) tid = 1;

            var branches = await _db.Branches
                .Where(b => b.TenantId == tid)
                .OrderBy(b => b.BranchCode)
                .ToListAsync();

            if (branches.Count == 0)
            {
                branches = new List<Branch>
                {
                    new Branch { TenantId = tid, BranchCode = "HQ-MNL", BranchName = "Metro Manila Head Office", Address = "Ayala Ave, Makati City", Phone = "(02) 8888-0100", IsActive = true, CreatedAt = DateTime.UtcNow.AddMonths(-12) },
                    new Branch { TenantId = tid, BranchCode = "BR-CEB", BranchName = "Cebu Regional Branch", Address = "Cebu Business Park, Cebu City", Phone = "(032) 234-5678", IsActive = true, CreatedAt = DateTime.UtcNow.AddMonths(-8) },
                    new Branch { TenantId = tid, BranchCode = "BR-DVO", BranchName = "Davao Commercial Branch", Address = "J.P. Laurel Ave, Davao City", Phone = "(082) 299-8877", IsActive = true, CreatedAt = DateTime.UtcNow.AddMonths(-5) }
                };
                _db.Branches.AddRange(branches);
                await _db.SaveChangesAsync();
            }

            var list = new List<BranchDto>();
            foreach (var b in branches)
            {
                int agents = await _db.Users.CountAsync(u => u.BranchId == b.BranchId);
                int props = await _db.Properties.CountAsync(p => p.BranchId == b.BranchId);
                int leads = await _db.Leads.CountAsync(l => l.BranchId == b.BranchId);
                int deals = await _db.Deals.CountAsync(d => d.BranchId == b.BranchId);
                decimal vol = await _db.Deals.Where(d => d.BranchId == b.BranchId).SumAsync(d => (decimal?)d.Value) ?? 0m;

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
            if (!ApiSecurityHelper.HasFullOversight(user.Role))
                return StatusCode(StatusCodes.Status403Forbidden, "Only Manager or Admin can manage branches.");

            if (branch.BranchId <= 0)
            {
                branch.TenantId = user.TenantId;
                branch.CreatedAt = DateTime.UtcNow;
                _db.Branches.Add(branch);
            }
            else
            {
                var existing = await _db.Branches.FirstOrDefaultAsync(b => b.BranchId == branch.BranchId);
                if (existing == null) return NotFound();

                existing.BranchCode = branch.BranchCode;
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
            if (!ApiSecurityHelper.HasFullOversight(user.Role))
                return StatusCode(StatusCodes.Status403Forbidden, "Only Manager or Admin can manage branches.");

            var branch = await _db.Branches.FirstOrDefaultAsync(b => b.BranchId == id);
            if (branch == null) return NotFound();

            branch.IsActive = !branch.IsActive;
            await _db.SaveChangesAsync();
            return Ok(new { success = true, isActive = branch.IsActive });
        }
    }
}
