using CRMS_Peguit.domain.entities;
using CRMS_Peguit.infrastructure.data;
using CRMS_Peguit.infrastructure.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace CRMS_Peguit.api.Controllers
{
    public record LoginRequest(
        string Email,
        string Password
    );

    public record LoginResponse(
        string Token,
        int UserId,
        int TenantId,
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

        public AuthController(
            RealEstateDbContext db,
            IConfiguration config)
        {
            _db = db;
            _config = config;
        }

        // ==============================================
        // CONNECTIVITY PING
        // ==============================================

        [AllowAnonymous]
        [HttpGet("ping")]
        [HttpHead("ping")]
        public IActionResult Ping()
        {
            return Ok(new { status = "online", timestamp = DateTime.UtcNow });
        }

        // ==============================================
        // LOGIN
        // ==============================================

        [AllowAnonymous]
        [HttpPost("login")]
        public async Task<ActionResult<LoginResponse>> Login(
            [FromBody] LoginRequest request)
        {
            if (request is null)
            {
                return BadRequest(new
                {
                    message = "Login request is required."
                });
            }

            if (string.IsNullOrWhiteSpace(request.Email) ||
                string.IsNullOrWhiteSpace(request.Password))
            {
                return Unauthorized(new
                {
                    message = "Invalid email or password."
                });
            }

            // ----------------------------------------------
            // FIND USER ACROSS ALL TENANTS
            // ----------------------------------------------

            var user =
                await _db.Users
                    .IgnoreQueryFilters()
                    .Include(u => u.Role)
                    .FirstOrDefaultAsync(
                        u =>
                            u.Email == request.Email.Trim()
                            &&
                            (u.Status == "Active" || u.Status == "active")
                    );

            // ----------------------------------------------
            // VERIFY PASSWORD
            // ----------------------------------------------

            bool isPasswordValid = false;
            try
            {
                isPasswordValid = user != null && PasswordHasher.Verify(request.Password, user.PasswordHash);
            }
            catch
            {
                isPasswordValid = false;
            }

            if (user is null || !isPasswordValid)
            {
                // Deliberately vague.
                // Do not reveal whether the email exists.
                return Unauthorized(new
                {
                    message =
                        "Invalid email or password."
                });
            }

            // Transparently upgrade legacy plain text hashes to BCrypt on successful login
            if (!user.PasswordHash.Trim().StartsWith("$2"))
            {
                try
                {
                    user.PasswordHash = PasswordHasher.Hash(request.Password);
                    await _db.SaveChangesAsync();
                }
                catch
                {
                    // Non-critical hash upgrade
                }
            }

            // ----------------------------------------------
            // GET ROLE ACROSS ALL TENANTS
            // ----------------------------------------------

            var role = user.Role ??
                await _db.Roles
                    .IgnoreQueryFilters()
                    .FirstOrDefaultAsync(
                        r =>
                            r.RoleId == user.RoleId
                    );

            if (role is null)
            {
                return StatusCode(
                    StatusCodes.Status500InternalServerError,
                    new
                    {
                        message =
                            "User has no assigned role."
                    }
                );
            }

            // ----------------------------------------------
            // GENERATE JWT
            // ----------------------------------------------

            var token =
                GenerateJwt(
                    user,
                    role.RoleName,
                    role.TenantId
                );

            // ----------------------------------------------
            // RETURN LOGIN RESPONSE
            // ----------------------------------------------

            return Ok(
                new LoginResponse(
                    Token: token,
                    UserId: user.UserId,
                    TenantId: role.TenantId,
                    FullName: user.FullName,
                    Email: user.Email,
                    RoleName: role.RoleName
                )
            );
        }

        // ==============================================
        // GENERATE JWT
        // ==============================================

        private string GenerateJwt(
            User user,
            string roleName,
            int tenantId)
        {
            var secret =
                _config["Jwt:Secret"]
                ?? throw new InvalidOperationException(
                    "Jwt:Secret is not configured."
                );

            var issuer =
                _config["Jwt:Issuer"]
                ?? "CRMS_Peguit";

            var expiryMinutes =
                int.Parse(
                    _config["Jwt:ExpiryMinutes"]
                    ?? "480"
                );

            var claims =
                new[]
                {
                    new Claim(
                        ClaimTypes.NameIdentifier,
                        user.UserId.ToString()
                    ),

                    new Claim(
                        ClaimTypes.Email,
                        user.Email
                    ),

                    new Claim(
                        ClaimTypes.Role,
                        roleName
                    ),

                    new Claim(
                        "tenantId",
                        tenantId.ToString()
                    )
                };

            var key =
                new SymmetricSecurityKey(
                    Encoding.UTF8.GetBytes(secret)
                );

            var credentials =
                new SigningCredentials(
                    key,
                    SecurityAlgorithms.HmacSha256
                );

            var jwt =
                new JwtSecurityToken(
                    issuer: issuer,
                    audience: issuer,
                    claims: claims,
                    expires:
                        DateTime.UtcNow.AddMinutes(
                            expiryMinutes
                        ),
                    signingCredentials: credentials
                );

            return new JwtSecurityTokenHandler()
                .WriteToken(jwt);
        }
    }
}