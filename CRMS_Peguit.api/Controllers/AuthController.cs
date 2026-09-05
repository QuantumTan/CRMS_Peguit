using CRMS_Peguit.domain.entities;
using CRMS_Peguit.infrastructure.data;
using CRMS_Peguit.infrastructure.Security;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace CRMS_Peguit.api.Controllers
{
    public record LoginRequest(string Email, string Password);

    public record LoginResponse(
        string Token,
        int UserId,
        string FullName,
        string Email,
        string RoleName
    );

    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly RealEstateDbContext _db;
        private readonly IConfiguration _config;

        public AuthController(RealEstateDbContext db, IConfiguration config)
        {
            _db = db;
            _config = config;
        }

        [HttpPost("login")]
        public async Task<ActionResult<LoginResponse>> Login([FromBody] LoginRequest request)
        {
            var user = await _db.Users
                .FirstOrDefaultAsync(u => u.Email == request.Email && u.Status == "Active");

            if (user is null || !PasswordHasher.Verify(request.Password, user.PasswordHash))
            {
                // Deliberately vague - never reveal whether the email or password was wrong
                return Unauthorized(new { message = "Invalid email or password." });
            }

            var role = await _db.Roles.FirstOrDefaultAsync(r => r.RoleId == user.RoleId);
            if (role is null)
            {
                return StatusCode(500, new { message = "User has no assigned role." });
            }

            var token = GenerateJwt(user, role.RoleName);

            return Ok(new LoginResponse(token, user.UserId, user.FullName, user.Email, role.RoleName));
        }

        private string GenerateJwt(User user, string roleName)
        {
            // appsettings.json needs:
            // "Jwt": { "Secret": "<a long random string>", "Issuer": "CRMS_Peguit", "ExpiryMinutes": "480" }
            var secret = _config["Jwt:Secret"]
                ?? throw new InvalidOperationException("Jwt:Secret is not configured.");
            var issuer = _config["Jwt:Issuer"] ?? "CRMS_Peguit";
            var expiryMinutes = int.Parse(_config["Jwt:ExpiryMinutes"] ?? "480");

            var claims = new[]
            {
                new Claim(ClaimTypes.NameIdentifier, user.UserId.ToString()),
                new Claim(ClaimTypes.Email, user.Email),
                new Claim(ClaimTypes.Role, roleName),
                new Claim("tenantId", user.TenantId.ToString()),
            };

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secret));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var jwt = new JwtSecurityToken(
                issuer: issuer,
                audience: issuer,
                claims: claims,
                expires: DateTime.UtcNow.AddMinutes(expiryMinutes),
                signingCredentials: creds
            );

            return new JwtSecurityTokenHandler().WriteToken(jwt);
        }
    }
}