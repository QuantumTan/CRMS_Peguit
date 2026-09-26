using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using CRMS_Peguit.domain.Common;

namespace CRMS_Peguit.winforms.Services
{
    public class SuperAdminSubscriptionApiService : BaseApiService
    {
        public SuperAdminSubscriptionApiService(string? baseUrl = null, HttpClient? httpClient = null)
            : base(baseUrl, httpClient)
        {
        }

        public async Task<PlatformBiSummaryDto> GetPlatformBiSummaryAsync() =>
            await GetAsync<PlatformBiSummaryDto>("api/superadminsubscriptions/bi-summary") ?? new PlatformBiSummaryDto();

        public PlatformBiSummaryDto GetPlatformBiSummary() =>
            GetPlatformBiSummaryAsync().GetAwaiter().GetResult();

        public async Task<List<TenantSubscriptionDto>> GetAllSubscriptionsAsync() =>
            await GetAsync<List<TenantSubscriptionDto>>("api/superadminsubscriptions") ?? new();

        public List<TenantSubscriptionDto> GetAllSubscriptions() =>
            GetAllSubscriptionsAsync().GetAwaiter().GetResult();

        public async Task<bool> UpdateSubscriptionAsync(int subscriptionId, string newPlanName, string newStatus, decimal billingAmount, DateTime? endDate) =>
            await PutAsync($"api/superadminsubscriptions/{subscriptionId}", new UpdateSubscriptionRequest
            {
                PlanName = newPlanName,
                Status = newStatus,
                BillingAmount = billingAmount,
                EndDate = endDate
            });

        public async Task<bool> ChangeTenantTierAsync(int companyId, string newPlanName) =>
            await PostAsync($"api/superadminsubscriptions/tier/{companyId}", new ChangeTierRequest { PlanName = newPlanName });
    }
}
