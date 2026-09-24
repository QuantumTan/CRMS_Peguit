using Microsoft.EntityFrameworkCore;
using CRMS_Peguit.domain.Common;
using CRMS_Peguit.domain.entities;
using CRMS_Peguit.infrastructure.data;
using CRMS_Peguit.winforms.Auth;
using CRMS_Peguit.winforms.Models.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace CRMS_Peguit.winforms.Controllers
{
    public class DealController : IDisposable
    {
        private readonly RealEstateDbContext _db;
        public int TenantId => CurrentSession.TenantId;
        private readonly NotificationController _notifCtrl;

        public DealController()
        {
            _db = LocalDb.CreateContext(tenantId: TenantId);
            _notifCtrl = new NotificationController(_db);
        }

        public List<Deal> GetAll()
        {
            try
            {
                var query = _db.Deals
                    .Include(d => d.Customer).ThenInclude(c => c!.Person)
                    .Include(d => d.Property)
                    .Include(d => d.Agent).ThenInclude(u => u!.Person)
                    .AsNoTracking();

                if (!RbacService.HasFullOversight && RbacService.IsAgent)
                {
                    int currentUserId = CurrentSession.UserId;
                    query = query.Where(d => d.AgentId == currentUserId);
                }

                return query
                    .OrderByDescending(x => x.CreatedAt)
                    .ToList();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[DealController.GetAll] Error: {ex.Message}");
                return new List<Deal>();
            }
        }

        public async Task<PagedResult<Deal>> GetPagedAsync(
            int pageNumber = 1,
            int pageSize = 25,
            string? search = null,
            string? filterStage = null)
        {
            try
            {
                using var db = LocalDb.CreateContext(tenantId: TenantId);
                var query = db.Deals
                    .Include(d => d.Customer).ThenInclude(c => c!.Person)
                    .Include(d => d.Property)
                    .Include(d => d.Agent).ThenInclude(u => u!.Person)
                    .AsNoTracking();

                if (!RbacService.HasFullOversight && RbacService.IsAgent)
                {
                    int currentUserId = CurrentSession.UserId;
                    query = query.Where(d => d.AgentId == currentUserId);
                }

                if (!string.IsNullOrWhiteSpace(filterStage) && !string.Equals(filterStage, "All", StringComparison.OrdinalIgnoreCase))
                {
                    if (string.Equals(filterStage, "Offer", StringComparison.OrdinalIgnoreCase))
                    {
                        query = query.Where(d => d.Stage.ToLower() == "offer" || d.Stage.ToLower() == "reservation");
                    }
                    else if (string.Equals(filterStage, "Contract", StringComparison.OrdinalIgnoreCase))
                    {
                        query = query.Where(d => d.Stage.ToLower() == "contractsigned" || d.Stage.ToLower() == "contract");
                    }
                    else if (string.Equals(filterStage, "Closed", StringComparison.OrdinalIgnoreCase))
                    {
                        query = query.Where(d => d.Stage.ToLower() == "closed");
                    }
                    else if (string.Equals(filterStage, "Lost", StringComparison.OrdinalIgnoreCase))
                    {
                        query = query.Where(d => d.Stage.ToLower() == "lost");
                    }
                }

                if (!string.IsNullOrWhiteSpace(search))
                {
                    string s = search.Trim();
                    query = query.Where(d =>
                        (d.Customer != null && (d.Customer.Person.FirstName.Contains(s) || d.Customer.Person.LastName.Contains(s))) ||
                        (d.Property != null && d.Property.Address.Contains(s)) ||
                        d.Stage.Contains(s) ||
                        (d.PaymentScheme != null && d.PaymentScheme.Contains(s)) ||
                        (d.Agent != null && (d.Agent.Person.FirstName.Contains(s) || d.Agent.Person.LastName.Contains(s))));
                }

                int totalCount = await query.CountAsync();

                int validPage = Math.Max(1, pageNumber);
                int validPageSize = Math.Max(1, pageSize);

                var items = await query
                    .OrderByDescending(x => x.CreatedAt)
                    .Skip((validPage - 1) * validPageSize)
                    .Take(validPageSize)
                    .ToListAsync();

                return new PagedResult<Deal>(items, totalCount, validPage, validPageSize);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[DealController.GetPagedAsync] Error: {ex.Message}");
                return new PagedResult<Deal>(new List<Deal>(), 0, pageNumber, pageSize);
            }
        }

        public async Task<DealKpiResult> GetDealKpisAsync()
        {
            try
            {
                using var db = LocalDb.CreateContext(tenantId: TenantId);
                var query = db.Deals.AsNoTracking();

                if (!RbacService.HasFullOversight && RbacService.IsAgent)
                {
                    int currentUserId = CurrentSession.UserId;
                    query = query.Where(d => d.AgentId == currentUserId);
                }

                var list = await query.Select(d => new { d.Stage, d.Value }).ToListAsync();

                int total = list.Count;
                int offer = list.Count(d => string.Equals(d.Stage, "Offer", StringComparison.OrdinalIgnoreCase) || string.Equals(d.Stage, "Reservation", StringComparison.OrdinalIgnoreCase));
                int contract = list.Count(d => string.Equals(d.Stage, "ContractSigned", StringComparison.OrdinalIgnoreCase) || string.Equals(d.Stage, "Contract", StringComparison.OrdinalIgnoreCase));
                int closed = list.Count(d => string.Equals(d.Stage, "Closed", StringComparison.OrdinalIgnoreCase));
                int lost = list.Count(d => string.Equals(d.Stage, "Lost", StringComparison.OrdinalIgnoreCase));

                decimal totalVol = list.Sum(d => d.Value);
                decimal closedVol = list.Where(d => string.Equals(d.Stage, "Closed", StringComparison.OrdinalIgnoreCase)).Sum(d => d.Value);
                decimal contractVol = list.Where(d => string.Equals(d.Stage, "ContractSigned", StringComparison.OrdinalIgnoreCase) || string.Equals(d.Stage, "Contract", StringComparison.OrdinalIgnoreCase)).Sum(d => d.Value);

                return new DealKpiResult
                {
                    Total = total,
                    Offer = offer,
                    Contract = contract,
                    Closed = closed,
                    Lost = lost,
                    TotalVolume = totalVol,
                    ClosedVolume = closedVol,
                    ContractVolume = contractVol
                };
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[DealController.GetDealKpisAsync] Error: {ex.Message}");
                return new DealKpiResult();
            }
        }

        public Deal? GetById(int id)
        {
            var item = _db.Deals
                .Include(d => d.Contingencies)
                .Include(d => d.DealClauses)
                .AsNoTracking()
                .SingleOrDefault(x => x.DealId == id);

            if (item is null) return null;

            if (!RbacService.HasFullOversight && RbacService.IsAgent && item.AgentId != CurrentSession.UserId)
                return null;

            return item;
        }

        public Deal Add(Deal deal)
        {
            deal.CreatedByUserId = CurrentSession.UserId > 0 ? CurrentSession.UserId : 1;
            deal.CreatedAt = DateTime.UtcNow;

            // Apply defaults for commercial terms if not specified
            if (string.IsNullOrWhiteSpace(deal.PaymentScheme))
                deal.PaymentScheme = "Bank Financing";

            if (deal.Contingencies.Count == 0)
            {
                var defaults = DealClauseLibrary.GetDefaultContingencies(deal.PaymentScheme);
                foreach (var dc in defaults)
                {
                    deal.Contingencies.Add(new DealContingency
                    {
                        ContingencyName = dc.ContingencyName,
                        Description = dc.Description,
                        DueDate = dc.DueDate,
                        IsSatisfied = dc.IsSatisfied,
                        SatisfiedAt = dc.SatisfiedAt,
                        CreatedAt = DateTime.UtcNow
                    });
                }
            }

            if (deal.DealClauses.Count == 0)
            {
                var defaultClauseIds = new[] { "TTL-01", "TAX-01", "FIN-01", "TRN-01", "DEF-01" };
                foreach (var cid in defaultClauseIds)
                {
                    var clauseDef = DealClauseLibrary.GetStandardClauses().FirstOrDefault(c => c.Id == cid);
                    deal.DealClauses.Add(new DealClause
                    {
                        ClauseId = cid,
                        Title = clauseDef?.Title ?? cid,
                        ClauseText = clauseDef?.ClauseText,
                        IsApproved = true,
                        ApprovedAt = DateTime.UtcNow,
                        CreatedAt = DateTime.UtcNow
                    });
                }
            }

            _db.Deals.Add(deal);
            _db.SaveChanges();
            return deal;
        }

        public void Update(Deal deal)
        {
            var item = _db.Deals.Include(d => d.Contingencies).Include(d => d.DealClauses).SingleOrDefault(x => x.DealId == deal.DealId);
            if (item is null) return;

            var oldStage = item.Stage;
            var newStage = deal.Stage;

            item.CustomerId = deal.CustomerId;
            item.PropertyId = deal.PropertyId;
            item.AgentId = deal.AgentId;
            item.Value = deal.Value;
            item.CommissionRate = deal.CommissionRate;
            item.Stage = deal.Stage;
            item.ExpectedCloseDate = deal.ExpectedCloseDate;

            // Structured Terms and Conditions updates
            item.PaymentScheme = deal.PaymentScheme;
            item.ReservationFee = deal.ReservationFee;
            item.DownPaymentPercent = deal.DownPaymentPercent;
            item.CgtPayer = deal.CgtPayer;
            item.DstPayer = deal.DstPayer;
            item.TransferTaxPayer = deal.TransferTaxPayer;
            item.RegistrationFeePayer = deal.RegistrationFeePayer;
            item.SpecialStipulations = deal.SpecialStipulations;
            item.ContractSignedDate = deal.ContractSignedDate;

            _db.SaveChanges();

            if (!string.Equals(oldStage, newStage, StringComparison.OrdinalIgnoreCase))
            {
                var advancedStages = new[] { "Reservation", "Contract Signed", "Closed" };
                if (item.AgentId.HasValue && item.AgentId.Value > 0 && advancedStages.Any(s => string.Equals(s, newStage, StringComparison.OrdinalIgnoreCase)))
                {
                    _notifCtrl.CreateNotification(
                        TenantId,
                        item.AgentId.Value,
                        NotificationType.DealStageChanged,
                        "Deal Stage Advanced",
                        $"Deal #{item.DealId} has advanced to '{newStage}'.",
                        "Deal",
                        item.DealId);
                }

                if (string.Equals(newStage, "Closed", StringComparison.OrdinalIgnoreCase))
                {
                    _notifCtrl.NotifyManagers(
                        TenantId,
                        NotificationType.DealClosed,
                        "Deal Closed",
                        $"Deal #{item.DealId} was successfully Closed for ₱{item.Value:N2}.",
                        "Deal",
                        item.DealId);
                }
            }
        }

        public void UpdateContingencyStatus(int dealId, int contingencyIndex, string newStatus, string? notes = null)
        {
            var item = _db.Deals.Include(d => d.Contingencies).SingleOrDefault(x => x.DealId == dealId);
            if (item is null) return;

            var contingencies = item.Contingencies.OrderBy(c => c.DealContingencyId).ToList();
            if (contingencyIndex >= 0 && contingencyIndex < contingencies.Count)
            {
                var c = contingencies[contingencyIndex];
                c.Status = newStatus;
                if (string.Equals(newStatus, "Satisfied", StringComparison.OrdinalIgnoreCase))
                {
                    c.SatisfiedAt = DateTime.UtcNow;
                    c.IsSatisfied = true;
                }
                else
                {
                    c.SatisfiedAt = null;
                    c.IsSatisfied = false;
                }
                if (!string.IsNullOrWhiteSpace(notes))
                {
                    c.Description = notes;
                }
                _db.SaveChanges();
            }
        }

        public void Delete(Deal deal)
        {
            var item = _db.Deals.SingleOrDefault(x => x.DealId == deal.DealId);
            if (item is null) return;

            _db.Deals.Remove(item);
            _db.SaveChanges();
        }

        public Dictionary<int, string> GetCustomerNames()
        {
            return _db.Customers
                .Include(x => x.Person)
                .AsNoTracking()
                .OrderBy(x => x.Person.LastName)
                .ThenBy(x => x.Person.FirstName)
                .ToDictionary(
                    x => x.CustomerId,
                    x => x.FullName
                );
        }

        public Dictionary<int, string> GetPropertyAddresses()
        {
            return _db.Properties
                .AsNoTracking()
                .OrderBy(x => x.Address)
                .ToDictionary(
                    x => x.PropertyId,
                    x => x.Address
                );
        }

        public Dictionary<int, string> GetAgentNames()
        {
            return _db.Users
                .Include(x => x.Person)
                .AsNoTracking()
                .OrderBy(x => x.Person.FirstName)
                .ThenBy(x => x.Person.LastName)
                .ToDictionary(
                    x => x.UserId,
                    x => x.FullName
                );
        }

        public List<KeyValuePair<int, string>> GetCustomerPickerList(int? includeCustomerId = null)
        {
            var buyerTypes = new[] { "buyer", "both" };

            return _db.Customers
                .Include(c => c.Person)
                .AsNoTracking()
                .Where(c => (buyerTypes.Contains(c.Type.ToLower()) && c.Status.ToLower() != "inactive") ||
                            (includeCustomerId.HasValue && c.CustomerId == includeCustomerId.Value))
                .OrderBy(c => c.Person.LastName)
                .ThenBy(c => c.Person.FirstName)
                .ToList()
                .Select(c => new KeyValuePair<int, string>(c.CustomerId, c.FullName))
                .ToList();
        }

        public List<KeyValuePair<int, string>> GetPropertyPickerList()
        {
            return _db.Properties
                .AsNoTracking()
                .OrderBy(p => p.Address)
                .Select(p => new KeyValuePair<int, string>(p.PropertyId, $"{p.Address} (₱{p.Price:N2})"))
                .ToList();
        }

        public List<KeyValuePair<int, string>> GetAgentPickerList()
        {
            return _db.Users
                .Include(u => u.Person)
                .AsNoTracking()
                .Where(u => u.Status != "inactive")
                .OrderBy(u => u.Person.LastName)
                .ToList()
                .Select(u => new KeyValuePair<int, string>(u.UserId, u.FullName))
                .ToList();
        }

        public static DealFinancingResult CalculateFinancing(decimal dealValue, decimal downPaymentPercent, string? paymentScheme)
        {
            if (string.Equals(paymentScheme, "Spot Cash", StringComparison.OrdinalIgnoreCase))
            {
                return new DealFinancingResult
                {
                    DownPaymentAmount = dealValue,
                    BalanceAmount = 0,
                    DownPaymentDisplay = $"Full Payment: ₱{dealValue:N2}",
                    BalanceDisplay = "Balance: ₱0.00 (Cash Settlement)"
                };
            }

            decimal downAmt = dealValue * (downPaymentPercent / 100m);
            decimal balAmt = Math.Max(0, dealValue - downAmt);
            return new DealFinancingResult
            {
                DownPaymentAmount = downAmt,
                BalanceAmount = balAmt,
                DownPaymentDisplay = $"Downpayment ({downPaymentPercent:0.##}%): ₱{downAmt:N2}",
                BalanceDisplay = $"Balance to Finance: ₱{balAmt:N2}"
            };
        }

        public static bool ValidateDealInput(object? customerValue, object? propertyValue, string dealValueText, out decimal dealValue, out string? errorMessage)
        {
            return ValidateDealInput(customerValue, propertyValue, dealValueText, null, out dealValue, out errorMessage);
        }

        public static bool ValidateDealInput(object? customerValue, object? propertyValue, string dealValueText, DateTime? expectedCloseDate, out decimal dealValue, out string? errorMessage)
        {
            dealValue = 0;
            if (customerValue == null)
            {
                errorMessage = "Please select a Buyer / Customer.";
                return false;
            }

            if (propertyValue == null)
            {
                errorMessage = "Please select a Subject Property.";
                return false;
            }

            if (!decimal.TryParse(dealValueText.Replace(",", "").Trim(), out dealValue) || dealValue <= 0)
            {
                errorMessage = "Please enter a valid positive Deal Value.";
                return false;
            }

            if (expectedCloseDate.HasValue && expectedCloseDate.Value.Date < DateTime.Today)
            {
                errorMessage = "Expected closing date must be today or in the future.";
                return false;
            }

            errorMessage = null;
            return true;
        }

        public int GetOpenDealsCount(int? agentId = null)
        {
            try
            {
                var query = _db.Deals.AsNoTracking().Where(d => d.Stage.ToLower() != "closed" && d.Stage.ToLower() != "lost");
                if (agentId.HasValue && agentId.Value > 0)
                {
                    int uid = agentId.Value;
                    query = query.Where(d => d.AgentId == uid);
                }
                else if (!RbacService.HasFullOversight && RbacService.IsAgent)
                {
                    int uid = CurrentSession.UserId;
                    query = query.Where(d => d.AgentId == uid);
                }

                return query.Count();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[DealController.GetOpenDealsCount] Error: {ex.Message}");
                return 0;
            }
        }

        public int GetDealsClosedThisMonthCount()
        {
            try
            {
                var now = DateTime.UtcNow;
                return _db.Deals
                    .AsNoTracking()
                    .Count(d => d.Stage.ToLower() == "closed" &&
                               ((d.ContractSignedDate.HasValue && d.ContractSignedDate.Value.Year == now.Year && d.ContractSignedDate.Value.Month == now.Month) ||
                                (d.CreatedAt.Year == now.Year && d.CreatedAt.Month == now.Month)));
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[DealController.GetDealsClosedThisMonthCount] Error: {ex.Message}");
                return 0;
            }
        }

        public decimal GetCommissionEarnedThisMonth()
        {
            try
            {
                var now = DateTime.UtcNow;
                var closedDealsThisMonth = _db.Deals
                    .AsNoTracking()
                    .Where(d => d.Stage.ToLower() == "closed" &&
                               ((d.ContractSignedDate.HasValue && d.ContractSignedDate.Value.Year == now.Year && d.ContractSignedDate.Value.Month == now.Month) ||
                                (d.CreatedAt.Year == now.Year && d.CreatedAt.Month == now.Month)))
                    .ToList();

                return closedDealsThisMonth.Sum(d => d.Value * (d.CommissionRate > 1m ? d.CommissionRate / 100m : d.CommissionRate));
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[DealController.GetCommissionEarnedThisMonth] Error: {ex.Message}");
                return 0m;
            }
        }

        public List<CRMS_Peguit.winforms.Models.ViewModels.AdminCommissionTrendPointDto> GetCommissionTrendLast6Months(int months = 6)
        {
            try
            {
                var result = new List<CRMS_Peguit.winforms.Models.ViewModels.AdminCommissionTrendPointDto>();
                var now = DateTime.UtcNow;
                var startMonth = new DateTime(now.Year, now.Month, 1).AddMonths(-(months - 1));

                var deals = _db.Deals
                    .AsNoTracking()
                    .Where(d => d.Stage.ToLower() == "closed" && ((d.ContractSignedDate ?? d.CreatedAt) >= startMonth))
                    .ToList();

                for (int i = 0; i < months; i++)
                {
                    var targetMonth = startMonth.AddMonths(i);
                    var monthDeals = deals.Where(d =>
                    {
                        var dt = d.ContractSignedDate ?? d.CreatedAt;
                        return dt.Year == targetMonth.Year && dt.Month == targetMonth.Month;
                    }).ToList();

                    decimal comm = monthDeals.Sum(d => d.Value * (d.CommissionRate > 1m ? d.CommissionRate / 100m : d.CommissionRate));

                    result.Add(new CRMS_Peguit.winforms.Models.ViewModels.AdminCommissionTrendPointDto
                    {
                        MonthLabel = targetMonth.ToString("MMM"),
                        CommissionAmount = (double)comm
                    });
                }

                return result;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[DealController.GetCommissionTrendLast6Months] Error: {ex.Message}");
                return new List<CRMS_Peguit.winforms.Models.ViewModels.AdminCommissionTrendPointDto>();
            }
        }

        public void Dispose()
        {
            _db.Dispose();
        }
    }

    public class DealFinancingResult
    {
        public decimal DownPaymentAmount { get; set; }
        public decimal BalanceAmount { get; set; }
        public string DownPaymentDisplay { get; set; } = string.Empty;
        public string BalanceDisplay { get; set; } = string.Empty;
    }

    public class DealKpiResult
    {
        public int Total { get; set; }
        public int Offer { get; set; }
        public int Contract { get; set; }
        public int Closed { get; set; }
        public int Lost { get; set; }
        public decimal TotalVolume { get; set; }
        public decimal ClosedVolume { get; set; }
        public decimal ContractVolume { get; set; }
    }
}
