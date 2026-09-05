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
            if (context is null)
                return 0;

            // Once authenticated, the tenant comes from the JWT claim only.
            // Never trust the header here - otherwise a logged-in user could
            // switch the X-Company-Id header and read another tenant's data.
            if (context.User?.Identity?.IsAuthenticated == true)
            {
                var claim = context.User.FindFirst("tenantId")?.Value;
                return int.TryParse(claim, out var claimedTenantId) ? claimedTenantId : 0;
            }

            // Not authenticated yet - this only happens on the login endpoint
            // itself, which has no JWT to read a tenant from yet.
            var header = context.Request.Headers["X-Company-Id"].ToString();
            return int.TryParse(header, out var tenantId) ? tenantId : 0;
        }
    }
}