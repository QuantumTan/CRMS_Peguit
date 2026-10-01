using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using CRMS_Peguit.domain.entities;
using CRMS_Peguit.infrastructure.data;
using CRMS_Peguit.winforms.Auth;
using CRMS_Peguit.winforms.Models.Services;

namespace CRMS_Peguit.winforms.Controllers
{
    public class BranchItemDto
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

    public class BranchController
    {
        private int TenantId => CurrentSession.TenantId;

        public async Task<List<BranchItemDto>> GetAllBranchesAsync(int? tenantId = null)
        {
            int tid = tenantId ?? TenantId;
            var list = new List<BranchItemDto>();

            try
            {
                using var db = LocalDb.CreateContext(tid);
                var branches = await db.Branches
                    .Where(b => b.TenantId == tid)
                    .OrderBy(b => b.BranchCode)
                    .ToListAsync();

                foreach (var b in branches)
                {
                    int agents = await db.Users.CountAsync(u => u.BranchId == b.BranchId);
                    int props = await db.Properties.CountAsync(p => p.BranchId == b.BranchId);
                    int leads = await db.Leads.CountAsync(l => l.BranchId == b.BranchId);
                    int deals = await db.Deals.CountAsync(d => d.BranchId == b.BranchId);
                    decimal vol = await db.Deals.Where(d => d.BranchId == b.BranchId).SumAsync(d => (decimal?)d.Value) ?? 0m;

                    list.Add(new BranchItemDto
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
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[BranchController.GetAllBranchesAsync] Error: {ex.Message}");
            }

            return list;
        }

        public async Task<bool> SaveBranchAsync(Branch branch)
        {
            if (!RbacService.IsAdmin)
            {
                System.Diagnostics.Debug.WriteLine("[BranchController.SaveBranchAsync] Permission denied: Only administrators can modify branches.");
                return false;
            }

            try
            {
                using var db = LocalDb.CreateContext(TenantId);
                if (branch.BranchId <= 0)
                {
                    if (await db.Branches.AnyAsync(b => b.BranchCode == branch.BranchCode))
                    {
                        throw new InvalidOperationException("Branch code must be unique within the tenant.");
                    }

                    branch.TenantId = TenantId;
                    branch.CreatedAt = DateTime.UtcNow;
                    db.Branches.Add(branch);
                }
                else
                {
                    var existing = await db.Branches.FirstOrDefaultAsync(b => b.BranchId == branch.BranchId);
                    if (existing == null) return false;

                    if (existing.IsActive && !branch.IsActive &&
                        await db.Users.AnyAsync(u => u.BranchId == existing.BranchId && u.Status == "active"))
                    {
                        throw new InvalidOperationException("Reassign or deactivate the branch's active user accounts before deactivating this branch.");
                    }

                    bool duplicateCode = await db.Branches.AnyAsync(b =>
                        b.BranchId != existing.BranchId && b.BranchCode == branch.BranchCode);
                    if (duplicateCode)
                    {
                        throw new InvalidOperationException("Branch code must be unique within the tenant.");
                    }

                    existing.BranchCode = branch.BranchCode;
                    existing.BranchName = branch.BranchName;
                    existing.Address = branch.Address;
                    existing.Phone = branch.Phone;
                    existing.IsActive = branch.IsActive;
                }

                await db.SaveChangesAsync();
                return true;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[SaveBranchAsync] Error: {ex.Message}");
                return false;
            }
        }

        public async Task<bool> ToggleBranchStatusAsync(int branchId)
        {
            if (!RbacService.IsAdmin)
            {
                System.Diagnostics.Debug.WriteLine("[BranchController.ToggleBranchStatusAsync] Permission denied: Only administrators can modify branches.");
                return false;
            }

            try
            {
                using var db = LocalDb.CreateContext(TenantId);
                var branch = await db.Branches.FirstOrDefaultAsync(b => b.BranchId == branchId);
                if (branch != null)
                {
                    if (branch.IsActive &&
                        await db.Users.AnyAsync(u => u.BranchId == branchId && u.Status == "active"))
                    {
                        return false;
                    }

                    branch.IsActive = !branch.IsActive;
                    await db.SaveChangesAsync();
                    return true;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[ToggleBranchStatusAsync] Error: {ex.Message}");
            }
            return false;
        }
    }
}
