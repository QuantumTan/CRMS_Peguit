using Microsoft.AspNetCore.Http;
using CRMS_Peguit.infrastructure.data;

namespace CRMS_Peguit.api
{
    public class HttpTenantResolver : ITenantResolver
    {
        private readonly IHttpContextAccessor _httpContextAccessor;

        public HttpTenantResolver(IHttpContextAccessor httpContextAccessor)
        {
            _httpContextAccessor = httpContextAccessor;
        }

        public int GetTenantId()
        {
            var context = _httpContextAccessor.HttpContext;
            if (context?.User?.Identity?.IsAuthenticated == true)
            {
                var claim = context.User.FindFirst("tenantId")?.Value
                    ?? context.User.FindFirst("TenantId")?.Value;

                if (int.TryParse(claim, out var claimedTenantId) && claimedTenantId > 0)
                {
                    return claimedTenantId;
                }
            }

            return 0;
        }
    }
}