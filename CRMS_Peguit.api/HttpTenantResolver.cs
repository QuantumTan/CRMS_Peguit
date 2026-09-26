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
                if (int.TryParse(claim, out var claimedTenantId) && claimedTenantId > 0)
                    return claimedTenantId;
            }

            // Extract from Bearer token if not unpacked by authentication middleware
            var authHeader = context.Request.Headers["Authorization"].ToString();
            if (!string.IsNullOrWhiteSpace(authHeader) && authHeader.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
            {
                var tokenStr = authHeader.Substring(7).Trim();
                try
                {
                    var handler = new System.IdentityModel.Tokens.Jwt.JwtSecurityTokenHandler();
                    var jwt = handler.ReadJwtToken(tokenStr);
                    var claim = jwt.Claims.FirstOrDefault(c => c.Type == "tenantId")?.Value;
                    if (int.TryParse(claim, out var claimedTenantId) && claimedTenantId > 0)
                        return claimedTenantId;
                }
                catch { }
            }

            // Fallback to headers (for initial unauthenticated/login discovery or client header)
            var header = context.Request.Headers["X-Tenant-Id"].ToString();
            if (string.IsNullOrWhiteSpace(header))
                header = context.Request.Headers["X-Company-Id"].ToString();
            return int.TryParse(header, out var tenantId) ? tenantId : 0;
        }
    }
}