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
    public record DealKpiDto(
        int Total,
        int Offer,
        int Contract,
        int Closed,
        int Lost,
        decimal TotalVolume,
        decimal ClosedVolume,
        decimal ContractVolume);

    public record UpdateContingencyRequest(int ContingencyIndex, string NewStatus, string? Notes = null);
    public record TrendPointDto(string MonthLabel, double CommissionAmount);

    [ApiController]
    [Route("api/[controller]")]
    public class DealsController : ControllerBase
    {
        private readonly RealEstateDbContext _db;

        public DealsController(RealEstateDbContext db)
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
            [FromQuery] string? stage = null)
        {
            var user = CurrentUser;
            var query = _db.Deals
                .Include(d => d.Customer)
                .Include(d => d.Property)
                .Include(d => d.Agent)
                .AsQueryable();

            // Ownership-Based Access Control:
            if (!ApiSecurityHelper.HasFullOversight(user.Role) && ApiSecurityHelper.IsAgent(user.Role) && user.UserId > 0)
            {
                query = query.Where(d =>
                    (d.AgentId.HasValue && d.AgentId.Value > 0)
                        ? d.AgentId.Value == user.UserId
                        : d.CreatedByUserId == user.UserId);
            }

            if (!string.IsNullOrWhiteSpace(stage) && !stage.Equals("All", StringComparison.OrdinalIgnoreCase))
            {
                query = query.Where(d => d.Stage.ToLower() == stage.ToLower());
            }

            if (!string.IsNullOrWhiteSpace(search))
            {
                string s = search.Trim();
                query = query.Where(d =>
                    (d.Customer != null && (d.Customer.FirstName.Contains(s) || d.Customer.LastName.Contains(s))) ||
                    (d.Property != null && d.Property.Address.Contains(s)) ||
                    d.Stage.Contains(s) ||
                    (d.PaymentScheme != null && d.PaymentScheme.Contains(s)) ||
                    (d.Agent != null && (d.Agent.FirstName.Contains(s) || d.Agent.LastName.Contains(s))));
            }

            if (page.HasValue || pageSize.HasValue)
            {
                int pageNum = Math.Max(1, page.GetValueOrDefault(1));
                int size = Math.Clamp(pageSize.GetValueOrDefault(25), 1, 100);

                int totalCount = await query.CountAsync();
                var pagedList = await query.OrderByDescending(d => d.CreatedAt)
                    .Skip((pageNum - 1) * size)
                    .Take(size)
                    .ToListAsync();

                return Ok(new PagedResult<Deal>(pagedList, totalCount, pageNum, size));
            }

            var items = await query.OrderByDescending(d => d.CreatedAt).ToListAsync();
            return Ok(items);
        }

        [HttpGet("kpi")]
        public async Task<IActionResult> GetDealKpis()
        {
            var user = CurrentUser;
            var query = _db.Deals.AsNoTracking().AsQueryable();

            if (!ApiSecurityHelper.HasFullOversight(user.Role) && ApiSecurityHelper.IsAgent(user.Role) && user.UserId > 0)
            {
                query = query.Where(d =>
                    (d.AgentId.HasValue && d.AgentId.Value > 0)
                        ? d.AgentId.Value == user.UserId
                        : d.CreatedByUserId == user.UserId);
            }

            var list = await query.ToListAsync();
            int total = list.Count;
            int offer = list.Count(d => string.Equals(d.Stage, "Offer", StringComparison.OrdinalIgnoreCase));
            int contract = list.Count(d => string.Equals(d.Stage, "Under Contract", StringComparison.OrdinalIgnoreCase) || string.Equals(d.Stage, "Contract", StringComparison.OrdinalIgnoreCase));
            int closed = list.Count(d => string.Equals(d.Stage, "Closed", StringComparison.OrdinalIgnoreCase));
            int lost = list.Count(d => string.Equals(d.Stage, "Lost", StringComparison.OrdinalIgnoreCase) || string.Equals(d.Stage, "Cancelled", StringComparison.OrdinalIgnoreCase));

            decimal totalVol = list.Sum(d => d.Value);
            decimal closedVol = list.Where(d => string.Equals(d.Stage, "Closed", StringComparison.OrdinalIgnoreCase)).Sum(d => d.Value);
            decimal contractVol = list.Where(d => string.Equals(d.Stage, "Under Contract", StringComparison.OrdinalIgnoreCase) || string.Equals(d.Stage, "Contract", StringComparison.OrdinalIgnoreCase)).Sum(d => d.Value);

            return Ok(new DealKpiDto(total, offer, contract, closed, lost, totalVol, closedVol, contractVol));
        }

        [HttpGet("{id:int}")]
        public async Task<IActionResult> GetById(int id)
        {
            var user = CurrentUser;
            var item = await _db.Deals
                .Include(d => d.Customer)
                .Include(d => d.Property)
                .Include(d => d.Agent)
                .Include(d => d.Contingencies)
                .Include(d => d.DealClauses)
                .SingleOrDefaultAsync(d => d.DealId == id);

            if (item is null) return NotFound();

            if (!ApiSecurityHelper.HasFullOversight(user.Role) && ApiSecurityHelper.IsAgent(user.Role) && user.UserId > 0)
            {
                bool allowed = (item.AgentId.HasValue && item.AgentId.Value > 0)
                    ? item.AgentId.Value == user.UserId
                    : item.CreatedByUserId == user.UserId;
                if (!allowed) return Forbid();
            }

            return Ok(item);
        }

        [HttpPost]
        public async Task<IActionResult> Create(Deal deal)
        {
            var user = CurrentUser;
            if (user.UserId <= 0) return Unauthorized();

            if (deal.CustomerId <= 0)
                return BadRequest(new { message = "A valid CustomerId is required." });
            if (deal.PropertyId <= 0)
                return BadRequest(new { message = "A valid PropertyId is required." });
            if (DealCommercialRules.Validate(deal) is string error)
                return BadRequest(new { message = error });

            if (string.IsNullOrWhiteSpace(deal.Stage))
                deal.Stage = "Offer";

            deal.CreatedAt = DateTime.UtcNow;
            deal.CreatedByUserId = user.UserId;

            if (!ApiSecurityHelper.CanAssignRecords(user.Role))
            {
                deal.AgentId = null;
            }

            _db.Deals.Add(deal);
            await _db.SaveChangesAsync();

            return CreatedAtAction(nameof(GetById), new { id = deal.DealId }, deal);
        }

        [HttpPut("{id:int}")]
        public async Task<IActionResult> Update(int id, Deal updated)
        {
            var user = CurrentUser;
            var item = await _db.Deals.Include(d => d.Contingencies).Include(d => d.DealClauses).SingleOrDefaultAsync(x => x.DealId == id);
            if (item is null) return NotFound();

            if (!ApiSecurityHelper.HasFullOversight(user.Role) && ApiSecurityHelper.IsAgent(user.Role) && user.UserId > 0)
            {
                bool allowed = (item.AgentId.HasValue && item.AgentId.Value > 0)
                    ? item.AgentId.Value == user.UserId
                    : item.CreatedByUserId == user.UserId;
                if (!allowed) return Forbid();
            }

            if (DealCommercialRules.Validate(updated) is string error)
                return BadRequest(new { message = error });

            item.CustomerId = updated.CustomerId;
            item.PropertyId = updated.PropertyId;
            item.Value = updated.Value;
            item.CommissionRate = updated.CommissionRate;
            item.Stage = updated.Stage;
            item.ExpectedCloseDate = updated.ExpectedCloseDate;
            item.PaymentScheme = updated.PaymentScheme;
            item.ReservationFee = updated.ReservationFee;
            item.DownPaymentPercent = updated.DownPaymentPercent;
            item.CgtPayer = updated.CgtPayer;
            item.DstPayer = updated.DstPayer;
            item.TransferTaxPayer = updated.TransferTaxPayer;
            item.RegistrationFeePayer = updated.RegistrationFeePayer;
            item.SpecialStipulations = updated.SpecialStipulations;
            item.ContractSignedDate = updated.ContractSignedDate;
            DealCommercialRules.ApplyClauseSelection(item, updated);

            if (ApiSecurityHelper.CanAssignRecords(user.Role))
            {
                item.AgentId = updated.AgentId <= 0 ? null : updated.AgentId;
            }

            await _db.SaveChangesAsync();
            return Ok(item);
        }

        [HttpPut("{id:int}/contingency")]
        public async Task<IActionResult> UpdateContingencyStatus(int id, [FromBody] UpdateContingencyRequest req)
        {
            var user = CurrentUser;
            var item = await _db.Deals.Include(d => d.Contingencies).SingleOrDefaultAsync(x => x.DealId == id);
            if (item is null) return NotFound();

            if (!ApiSecurityHelper.HasFullOversight(user.Role) && ApiSecurityHelper.IsAgent(user.Role) && user.UserId > 0)
            {
                bool allowed = (item.AgentId.HasValue && item.AgentId.Value > 0)
                    ? item.AgentId.Value == user.UserId
                    : item.CreatedByUserId == user.UserId;
                if (!allowed) return Forbid();
            }

            var allowedStatuses = new[] { "Pending", "Satisfied", "Waived", "Failed" };
            if (string.IsNullOrWhiteSpace(req.NewStatus) || !allowedStatuses.Any(s => s.Equals(req.NewStatus, StringComparison.OrdinalIgnoreCase)))
            {
                return BadRequest(new { message = $"Invalid contingency status '{req.NewStatus}'. Allowed: {string.Join(", ", allowedStatuses)}" });
            }

            var contingencies = item.Contingencies.OrderBy(c => c.DealContingencyId).ToList();
            if (req.ContingencyIndex < 0 || req.ContingencyIndex >= contingencies.Count)
            {
                return BadRequest(new { message = $"Contingency index {req.ContingencyIndex} is out of bounds (total contingencies: {contingencies.Count})." });
            }

            var c = contingencies[req.ContingencyIndex];
            c.Status = req.NewStatus;
            if (string.Equals(req.NewStatus, "Satisfied", StringComparison.OrdinalIgnoreCase))
            {
                c.SatisfiedAt = DateTime.UtcNow;
                c.IsSatisfied = true;
            }
            else
            {
                c.SatisfiedAt = null;
                c.IsSatisfied = false;
            }
            if (!string.IsNullOrWhiteSpace(req.Notes))
            {
                c.Notes = req.Notes.Trim();
            }
            await _db.SaveChangesAsync();

            return Ok(item);
        }

        [HttpDelete("{id:int}")]
        public async Task<IActionResult> Delete(int id)
        {
            var user = CurrentUser;
            var item = await _db.Deals
                .Include(d => d.Contingencies)
                .Include(d => d.DealClauses)
                .FirstOrDefaultAsync(d => d.DealId == id);
            if (item is null) return NotFound();

            if (!ApiSecurityHelper.HasFullOversight(user.Role) && ApiSecurityHelper.IsAgent(user.Role) && user.UserId > 0)
            {
                bool allowed = (item.AgentId.HasValue && item.AgentId.Value > 0)
                    ? item.AgentId.Value == user.UserId
                    : item.CreatedByUserId == user.UserId;
                if (!allowed) return Forbid();
            }

            _db.Deals.Remove(item);
            await _db.SaveChangesAsync();
            return NoContent();
        }

        [HttpGet("customer-names")]
        public async Task<IActionResult> GetCustomerNames()
        {
            var dict = await _db.Customers
                .AsNoTracking()
                .ToDictionaryAsync(c => c.CustomerId, c => c.FullName);
            return Ok(dict);
        }

        [HttpGet("property-addresses")]
        public async Task<IActionResult> GetPropertyAddresses()
        {
            var dict = await _db.Properties
                .AsNoTracking()
                .ToDictionaryAsync(p => p.PropertyId, p => p.Address);
            return Ok(dict);
        }

        [HttpGet("agent-names")]
        public async Task<IActionResult> GetAgentNames()
        {
            var dict = await _db.Users
                .AsNoTracking()
                .ToDictionaryAsync(u => u.UserId, u => u.FullName);
            return Ok(dict);
        }

        [HttpGet("customer-picker")]
        public async Task<IActionResult> GetCustomerPickerList([FromQuery] int? includeCustomerId = null)
        {
            var user = CurrentUser;
            var query = _db.Customers.AsNoTracking().Where(c => !c.IsDeleted).AsQueryable();

            if (!ApiSecurityHelper.HasFullOversight(user.Role) && ApiSecurityHelper.IsAgent(user.Role) && user.UserId > 0)
            {
                query = query.Where(c =>
                    (c.AssignedAgentId.HasValue && c.AssignedAgentId.Value > 0)
                        ? c.AssignedAgentId.Value == user.UserId
                        : c.CreatedByUserId == user.UserId || (includeCustomerId.HasValue && c.CustomerId == includeCustomerId.Value));
            }

            var list = await query
                .OrderBy(c => c.LastName)
                .ThenBy(c => c.FirstName)
                .Select(c => new KeyValuePair<int, string>(c.CustomerId, c.FullName))
                .ToListAsync();

            return Ok(list);
        }

        [HttpGet("property-picker")]
        public async Task<IActionResult> GetPropertyPickerList()
        {
            var list = await _db.Properties
                .AsNoTracking()
                .Where(p => p.Status.ToLower() != "sold")
                .OrderBy(p => p.Address)
                .Select(p => new KeyValuePair<int, string>(p.PropertyId, $"{p.Address} (₱{p.Price:N0})"))
                .ToListAsync();

            return Ok(list);
        }

        [HttpGet("agent-picker")]
        public async Task<IActionResult> GetAgentPickerList()
        {
            var agentRoleIds = await _db.Roles
                .AsNoTracking()
                .Where(r => r.RoleName.ToLower() == "agent" || r.RoleName.ToLower() == "salesstaff")
                .Select(r => r.RoleId)
                .ToListAsync();

            var list = await _db.Users
                .AsNoTracking()
                .Where(u => agentRoleIds.Contains(u.RoleId) && u.Status != null && !u.Status.Equals("inactive", StringComparison.OrdinalIgnoreCase))
                .OrderBy(u => u.LastName)
                .ThenBy(u => u.FirstName)
                .Select(u => new KeyValuePair<int, string>(u.UserId, u.FullName))
                .ToListAsync();

            return Ok(list);
        }

        [HttpGet("stats/open-count")]
        public async Task<IActionResult> GetOpenDealsCount([FromQuery] int? agentId = null)
        {
            var user = CurrentUser;
            var query = _db.Deals.AsNoTracking().Where(d => d.Stage.ToLower() != "closed" && d.Stage.ToLower() != "lost");

            if (agentId.HasValue && agentId.Value > 0)
            {
                if (!ApiSecurityHelper.HasFullOversight(user.Role) && ApiSecurityHelper.IsAgent(user.Role) && agentId.Value != user.UserId)
                {
                    return Forbid();
                }
                query = query.Where(d => d.AgentId == agentId.Value);
            }
            else if (!ApiSecurityHelper.HasFullOversight(user.Role) && ApiSecurityHelper.IsAgent(user.Role) && user.UserId > 0)
            {
                query = query.Where(d =>
                    (d.AgentId.HasValue && d.AgentId.Value > 0)
                        ? d.AgentId.Value == user.UserId
                        : d.CreatedByUserId == user.UserId);
            }

            int count = await query.CountAsync();
            return Ok(new { count });
        }

        [HttpGet("stats/closed-this-month")]
        public async Task<IActionResult> GetDealsClosedThisMonthCount()
        {
            var user = CurrentUser;
            var now = DateTime.UtcNow;
            var query = _db.Deals.AsNoTracking().Where(d =>
                d.Stage.ToLower() == "closed" &&
                d.ExpectedCloseDate.HasValue &&
                d.ExpectedCloseDate.Value.Year == now.Year &&
                d.ExpectedCloseDate.Value.Month == now.Month);

            if (!ApiSecurityHelper.HasFullOversight(user.Role) && ApiSecurityHelper.IsAgent(user.Role) && user.UserId > 0)
            {
                query = query.Where(d =>
                    (d.AgentId.HasValue && d.AgentId.Value > 0)
                        ? d.AgentId.Value == user.UserId
                        : d.CreatedByUserId == user.UserId);
            }

            int count = await query.CountAsync();
            return Ok(new { count });
        }

        [HttpGet("stats/commission-this-month")]
        public async Task<IActionResult> GetCommissionEarnedThisMonth()
        {
            var user = CurrentUser;
            var now = DateTime.UtcNow;
            var query = _db.Deals.AsNoTracking()
                .Where(d => d.Stage.ToLower() == "closed" &&
                            d.ExpectedCloseDate.HasValue &&
                            d.ExpectedCloseDate.Value.Year == now.Year &&
                            d.ExpectedCloseDate.Value.Month == now.Month);

            if (!ApiSecurityHelper.HasFullOversight(user.Role) && ApiSecurityHelper.IsAgent(user.Role) && user.UserId > 0)
            {
                query = query.Where(d =>
                    (d.AgentId.HasValue && d.AgentId.Value > 0)
                        ? d.AgentId.Value == user.UserId
                        : d.CreatedByUserId == user.UserId);
            }

            var closed = await query.ToListAsync();
            decimal totalCommission = closed.Sum(d =>
            {
                decimal rate = d.CommissionRate > 1m ? d.CommissionRate / 100m : d.CommissionRate;
                return d.Value * rate;
            });
            return Ok(new { commission = totalCommission });
        }

        [HttpGet("stats/commission-trend")]
        public async Task<IActionResult> GetCommissionTrendLast6Months([FromQuery] int months = 6)
        {
            var user = CurrentUser;
            var now = DateTime.UtcNow;
            int clampedMonths = Math.Clamp(months, 1, 24);
            var query = _db.Deals.AsNoTracking()
                .Where(d => d.Stage.ToLower() == "closed" && d.ExpectedCloseDate.HasValue);

            if (!ApiSecurityHelper.HasFullOversight(user.Role) && ApiSecurityHelper.IsAgent(user.Role) && user.UserId > 0)
            {
                query = query.Where(d =>
                    (d.AgentId.HasValue && d.AgentId.Value > 0)
                        ? d.AgentId.Value == user.UserId
                        : d.CreatedByUserId == user.UserId);
            }

            var closedDeals = await query.ToListAsync();

            var points = new List<TrendPointDto>();
            for (int i = clampedMonths - 1; i >= 0; i--)
            {
                var dt = now.AddMonths(-i);
                decimal monthComm = closedDeals
                    .Where(d => d.ExpectedCloseDate!.Value.Year == dt.Year && d.ExpectedCloseDate.Value.Month == dt.Month)
                    .Sum(d =>
                    {
                        decimal rate = d.CommissionRate > 1m ? d.CommissionRate / 100m : d.CommissionRate;
                        return d.Value * rate;
                    });

                points.Add(new TrendPointDto(dt.ToString("MMM yyyy"), (double)monthComm));
            }

            return Ok(points);
        }
    }
}
