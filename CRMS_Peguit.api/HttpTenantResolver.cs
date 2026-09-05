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

            var header = context.Request.Headers["X-Company-Id"].ToString();

            return int.TryParse(header, out var tenantId) ? tenantId : 0;
        }
    }
}