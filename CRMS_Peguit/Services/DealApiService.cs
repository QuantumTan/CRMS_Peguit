using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using CRMS_Peguit.domain.Common;
using CRMS_Peguit.domain.entities;

namespace CRMS_Peguit.winforms.Services
{
    public class DealKpiCounts
    {
        public int TotalDeals { get; set; }
        public decimal PipelineValue { get; set; }
        public int WonDeals { get; set; }
        public decimal WonValue { get; set; }
        public double WinRate { get; set; }
    }

    public class DealApiService : BaseApiService
    {
        public DealApiService(string? baseUrl = null, HttpClient? httpClient = null)
            : base(baseUrl, httpClient)
        {
        }

        public List<Deal> GetAll() =>
            Get<List<Deal>>("api/deals") ?? new();

        public async Task<PagedResult<Deal>> GetPagedAsync(
            int pageNumber = 1,
            int pageSize = 25,
            string? search = null,
            string? stage = null,
            int? agentId = null,
            string? sortColumn = null,
            bool sortAscending = false)
        {
            var uri = $"api/deals/paged?pageNumber={pageNumber}&pageSize={pageSize}";
            if (!string.IsNullOrWhiteSpace(search)) uri += $"&search={Uri.EscapeDataString(search)}";
            if (!string.IsNullOrWhiteSpace(stage)) uri += $"&stage={Uri.EscapeDataString(stage)}";
            if (agentId.HasValue) uri += $"&agentId={agentId.Value}";
            if (!string.IsNullOrWhiteSpace(sortColumn)) uri += $"&sortColumn={Uri.EscapeDataString(sortColumn)}&sortAscending={sortAscending}";

            return await GetAsync<PagedResult<Deal>>(uri) ?? new PagedResult<Deal>();
        }

        public PagedResult<Deal> GetPaged(
            int pageNumber = 1,
            int pageSize = 25,
            string? search = null,
            string? stage = null,
            int? agentId = null,
            string? sortColumn = null,
            bool sortAscending = false) =>
            GetPagedAsync(pageNumber, pageSize, search, stage, agentId, sortColumn, sortAscending).GetAwaiter().GetResult();

        public Deal? GetById(int id) =>
            Get<Deal>($"api/deals/{id}");

        public Deal Add(Deal deal)
        {
            var result = Post<Deal, Deal>("api/deals", deal);
            return result ?? deal;
        }

        public void Update(Deal deal) =>
            Put<Deal, Deal>($"api/deals/{deal.DealId}", deal);

        public void Delete(int id) =>
            Delete($"api/deals/{id}");

        public void Delete(Deal deal) =>
            Delete(deal.DealId);

        public async Task<DealKpiCounts> GetKpiCountsAsync() =>
            await GetAsync<DealKpiCounts>("api/deals/kpi") ?? new DealKpiCounts();

        public DealKpiCounts GetKpiCounts() =>
            GetKpiCountsAsync().GetAwaiter().GetResult();

        public async Task<DealKpiResult> GetDealKpisAsync() =>
            await GetAsync<DealKpiResult>("api/deals/kpi") ?? new DealKpiResult();

        public DealKpiResult GetDealKpis() =>
            GetDealKpisAsync().GetAwaiter().GetResult();

        public List<AgentPickerItem> GetAgents() =>
            Get<List<AgentPickerItem>>("api/deals/agents") ?? new();

        public Dictionary<int, string> GetAgentDictionary() =>
            GetAgents().ToDictionary(a => a.UserId, a => a.FullName);

        public Dictionary<int, string> GetAgentNames() =>
            GetAgentDictionary();

        public List<Customer> GetCustomers() =>
            Get<List<Customer>>("api/deals/customers") ?? new();

        public Dictionary<int, string> GetCustomerNames() =>
            GetCustomers().ToDictionary(c => c.CustomerId, c => c.FullName);

        public List<Property> GetProperties() =>
            Get<List<Property>>("api/deals/properties") ?? new();

        public Dictionary<int, string> GetPropertyAddresses() =>
            GetProperties().ToDictionary(p => p.PropertyId, p => p.Address);

        public void UpdateContingencyStatus(int dealId, int contingencyIndex, string status, string? notes = null) =>
            Put($"api/deals/{dealId}/contingency", new { ContingencyIndex = contingencyIndex, NewStatus = status, Notes = notes });

        public void UpdateContingencyStatus(int dealId, string status) =>
            UpdateContingencyStatus(dealId, 0, status, null);

        public List<KeyValuePair<int, string>> GetCustomerPickerList(int? existingId = null) =>
            GetCustomers().Select(c => new KeyValuePair<int, string>(c.CustomerId, c.FullName)).ToList();

        public List<KeyValuePair<int, string>> GetPropertyPickerList() =>
            GetProperties().Select(p => new KeyValuePair<int, string>(p.PropertyId, $"{p.Address} (₱{p.Price:N0})")).ToList();

        public List<KeyValuePair<int, string>> GetAgentPickerList() =>
            GetAgents().Select(a => new KeyValuePair<int, string>(a.UserId, a.FullName)).ToList();

        public int GetDealsClosedThisMonthCount() =>
            Get<int>("api/deals/closed-this-month/count");

        public int GetOpenDealsCount(int? agentId = null)
        {
            var uri = "api/deals/open-count";
            if (agentId.HasValue) uri += $"?agentId={agentId.Value}";
            return Get<int>(uri);
        }

        public decimal GetCommissionEarnedThisMonth() =>
            Get<decimal>("api/deals/commission-this-month");

        public List<AdminCommissionTrendPointDto> GetCommissionTrendLast6Months(int count = 6) =>
            Get<List<AdminCommissionTrendPointDto>>($"api/deals/commission-trend?count={count}") ?? new();

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
    }

    public class DealFinancingResult
    {
        public decimal DownPaymentAmount { get; set; }
        public decimal BalanceAmount { get; set; }
        public string DownPaymentDisplay { get; set; } = string.Empty;
        public string BalanceDisplay { get; set; } = string.Empty;
    }
}
