using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using CRMS_Peguit.domain.Common;
using CRMS_Peguit.domain.entities;
using CRMS_Peguit.infrastructure.data;
using CRMS_Peguit.infrastructure.Services;

namespace CRMS_Peguit.api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class BrandingController : ControllerBase
    {
        private readonly MasterCrmsDbContext _masterDb;
        private readonly ITenantDbContextFactory _tenantFactory;

        public BrandingController(MasterCrmsDbContext masterDb, ITenantDbContextFactory tenantFactory)
        {
            _masterDb = masterDb;
            _tenantFactory = tenantFactory;
        }

        private (int UserId, string Role, int TenantId) GetCaller()
        {
            return ApiSecurityHelper.GetCurrentUserInfo(User);
        }

        [HttpGet]
        public async Task<ActionResult<TenantBrandingDto>> GetBranding([FromQuery] int? tenantId)
        {
            var caller = GetCaller();
            int effectiveTenantId = caller.TenantId;
            if (caller.Role.Equals("SuperAdmin", StringComparison.OrdinalIgnoreCase) && tenantId.HasValue && tenantId.Value > 0)
            {
                effectiveTenantId = tenantId.Value;
            }

            if (effectiveTenantId <= 0) return Unauthorized();

            var company = await _masterDb.Companies
                .Include(c => c.Branding)
                .Include(c => c.Subscriptions)
                .AsNoTracking()
                .FirstOrDefaultAsync(c => c.CompanyId == effectiveTenantId);

            if (company == null) return NotFound(new { message = $"Tenant #{effectiveTenantId} not found." });

            var sub = company.Subscriptions.OrderByDescending(s => s.EndDate).FirstOrDefault();
            var tier = sub?.Tier ?? TenantTier.TenantA;
            var branding = company.Branding;

            string currentDisplayName = branding?.DisplayName ?? company.CompanyName;
            bool isDuplicate = await _masterDb.TenantBrandings
                .AnyAsync(b => b.CompanyId != effectiveTenantId && b.DisplayName.ToLower() == currentDisplayName.ToLower());

            return Ok(new TenantBrandingDto
            {
                CompanyId = company.CompanyId,
                CompanyName = company.CompanyName,
                DisplayName = currentDisplayName,
                HasCustomLogo = branding?.LogoImage != null && branding.LogoImage.Length > 0,
                LogoVersion = branding?.LogoVersion ?? 1,
                AccentColor = branding?.AccentColor,
                ContactEmail = branding?.ContactEmail,
                ContactPhone = branding?.ContactPhone,
                Address = branding?.Address,
                HidePoweredBy = branding?.HidePoweredBy ?? false,
                CanCustomizeAccent = FeatureGate.CanUseAccentColor(tier),
                CanHidePoweredBy = FeatureGate.CanHidePoweredBy(tier),
                IsDuplicateName = isDuplicate,
                UpdatedAt = branding?.UpdatedAt ?? company.CreatedAt
            });
        }

        [HttpGet("logo")]
        public async Task<IActionResult> GetLogo([FromQuery] int? tenantId)
        {
            var caller = GetCaller();
            int effectiveTenantId = tenantId.HasValue && caller.Role.Equals("SuperAdmin", StringComparison.OrdinalIgnoreCase)
                ? tenantId.Value
                : (caller.TenantId > 0 ? caller.TenantId : (tenantId ?? 1));

            var branding = await _masterDb.TenantBrandings
                .AsNoTracking()
                .FirstOrDefaultAsync(b => b.CompanyId == effectiveTenantId);

            if (branding?.LogoImage == null || branding.LogoImage.Length == 0)
            {
                return NotFound();
            }

            return File(branding.LogoImage, "image/png");
        }

        [HttpPut]
        public async Task<ActionResult<TenantBrandingDto>> UpdateBranding([FromBody] UpdateBrandingRequest req, [FromQuery] int? tenantId = null)
        {
            var caller = GetCaller();
            if (caller.TenantId <= 0 && !caller.Role.Equals("SuperAdmin", StringComparison.OrdinalIgnoreCase))
            {
                return Unauthorized();
            }

            // Only Tenant Admin or Super Admin may edit branding
            if (!caller.Role.Equals("Admin", StringComparison.OrdinalIgnoreCase) &&
                !caller.Role.Equals("SuperAdmin", StringComparison.OrdinalIgnoreCase))
            {
                return Forbid();
            }

            int targetTenantId = caller.Role.Equals("SuperAdmin", StringComparison.OrdinalIgnoreCase)
                ? (tenantId ?? caller.TenantId)
                : caller.TenantId;

            if (targetTenantId <= 0)
            {
                return BadRequest(new { message = "Valid tenant ID is required." });
            }

            var validator = new UpdateBrandingRequestValidator(allowReservedOverride: false);
            var validation = await validator.ValidateAsync(req);
            if (!validation.IsValid)
            {
                return BadRequest(new { message = validation.Errors.FirstOrDefault()?.ErrorMessage ?? "Validation failed." });
            }

            var company = await _masterDb.Companies
                .Include(c => c.Branding)
                .Include(c => c.Subscriptions)
                .FirstOrDefaultAsync(c => c.CompanyId == targetTenantId);

            if (company == null) return NotFound();

            var sub = company.Subscriptions.OrderByDescending(s => s.EndDate).FirstOrDefault();
            var tier = sub?.Tier ?? TenantTier.TenantA;

            // Server-side tier gating via FeatureGate
            if (!string.IsNullOrWhiteSpace(req.AccentColor) && !FeatureGate.CanUseAccentColor(tier))
            {
                return StatusCode(StatusCodes.Status403Forbidden, new
                {
                    message = "Custom accent colors require an Enterprise tier subscription. Please upgrade your plan."
                });
            }

            if (req.HidePoweredBy && !FeatureGate.CanHidePoweredBy(tier))
            {
                return StatusCode(StatusCodes.Status403Forbidden, new
                {
                    message = "White-label footer removal requires an Enterprise tier subscription. Please upgrade your plan."
                });
            }

            var branding = company.Branding;
            if (branding == null)
            {
                branding = new TenantBranding
                {
                    CompanyId = targetTenantId,
                    DisplayName = req.DisplayName.Trim(),
                    LogoVersion = 1,
                    UpdatedAt = DateTime.UtcNow
                };
                _masterDb.TenantBrandings.Add(branding);
            }

            branding.DisplayName = req.DisplayName.Trim();
            branding.AccentColor = FeatureGate.CanUseAccentColor(tier) ? req.AccentColor?.Trim() : null;
            branding.HidePoweredBy = FeatureGate.CanHidePoweredBy(tier) && req.HidePoweredBy;
            branding.ContactEmail = string.IsNullOrWhiteSpace(req.ContactEmail) ? null : req.ContactEmail.Trim();
            branding.ContactPhone = string.IsNullOrWhiteSpace(req.ContactPhone) ? null : req.ContactPhone.Trim();
            branding.Address = string.IsNullOrWhiteSpace(req.Address) ? null : req.Address.Trim();
            branding.UpdatedAt = DateTime.UtcNow;
            branding.UpdatedByUserId = caller.UserId;

            await _masterDb.SaveChangesAsync();

            // Log in tenant audit log
            try
            {
                await using var tenantDb = await _tenantFactory.CreateAsync(targetTenantId);
                tenantDb.RetentionAuditLogs.Add(new RetentionAuditLog
                {
                    PerformedByUserId = caller.UserId,
                    ActionType = "BrandingUpdated",
                    Detail = $"Tenant branding updated: DisplayName='{branding.DisplayName}', AccentColor='{branding.AccentColor ?? "None"}', HidePoweredBy={branding.HidePoweredBy}",
                    CreatedAt = DateTime.UtcNow
                });
                await tenantDb.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[BrandingController.UpdateBranding] Tenant audit log error: {ex.Message}");
            }

            return Ok(new TenantBrandingDto
            {
                CompanyId = company.CompanyId,
                CompanyName = company.CompanyName,
                DisplayName = branding.DisplayName,
                HasCustomLogo = branding.LogoImage != null && branding.LogoImage.Length > 0,
                LogoVersion = branding.LogoVersion,
                AccentColor = branding.AccentColor,
                ContactEmail = branding.ContactEmail,
                ContactPhone = branding.ContactPhone,
                Address = branding.Address,
                HidePoweredBy = branding.HidePoweredBy,
                CanCustomizeAccent = FeatureGate.CanUseAccentColor(tier),
                CanHidePoweredBy = FeatureGate.CanHidePoweredBy(tier),
                UpdatedAt = branding.UpdatedAt
            });
        }

        [HttpPost("logo")]
        public async Task<IActionResult> UploadLogo([FromQuery] int? tenantId = null)
        {
            var caller = GetCaller();
            if (caller.TenantId <= 0 && !caller.Role.Equals("SuperAdmin", StringComparison.OrdinalIgnoreCase))
            {
                return Unauthorized();
            }

            if (!caller.Role.Equals("Admin", StringComparison.OrdinalIgnoreCase) &&
                !caller.Role.Equals("SuperAdmin", StringComparison.OrdinalIgnoreCase))
            {
                return Forbid();
            }

            int targetTenantId = caller.Role.Equals("SuperAdmin", StringComparison.OrdinalIgnoreCase)
                ? (tenantId ?? caller.TenantId)
                : caller.TenantId;

            if (targetTenantId <= 0)
            {
                return BadRequest(new { message = "Valid tenant ID is required." });
            }

            byte[]? rawBytes = null;
            if (Request.HasFormContentType && Request.Form.Files.Count > 0)
            {
                var file = Request.Form.Files[0];
                if (file.Length > 5 * 1024 * 1024)
                {
                    return BadRequest(new { message = "Logo file exceeds the maximum allowed size of 5MB." });
                }

                using var ms = new MemoryStream();
                await file.CopyToAsync(ms);
                rawBytes = ms.ToArray();
            }
            else
            {
                try
                {
                    using var reader = new StreamReader(Request.Body);
                    var json = await reader.ReadToEndAsync();
                    if (!string.IsNullOrWhiteSpace(json))
                    {
                        var bodyReq = System.Text.Json.JsonSerializer.Deserialize<UploadLogoRequest>(json, new System.Text.Json.JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                        rawBytes = bodyReq?.LogoBytes;
                    }
                }
                catch
                {
                }
            }

            if (rawBytes == null || rawBytes.Length == 0)
            {
                return BadRequest(new { message = "No image file provided." });
            }

            if (rawBytes.Length > 5 * 1024 * 1024)
            {
                return BadRequest(new { message = "Logo file exceeds the maximum allowed size of 5MB." });
            }

            var (success, normalizedBytes, error) = LogoProcessor.ProcessAndNormalize(rawBytes);
            if (!success || normalizedBytes == null)
            {
                return BadRequest(new { message = error ?? "Failed to process logo image." });
            }

            var branding = await _masterDb.TenantBrandings.FirstOrDefaultAsync(b => b.CompanyId == targetTenantId);
            if (branding == null)
            {
                var company = await _masterDb.Companies.FindAsync(targetTenantId);
                branding = new TenantBranding
                {
                    CompanyId = targetTenantId,
                    DisplayName = company?.CompanyName ?? $"Tenant #{targetTenantId}",
                    LogoVersion = 1,
                    UpdatedAt = DateTime.UtcNow
                };
                _masterDb.TenantBrandings.Add(branding);
            }

            branding.LogoImage = normalizedBytes;
            branding.LogoVersion++;
            branding.UpdatedAt = DateTime.UtcNow;
            branding.UpdatedByUserId = caller.UserId;

            await _masterDb.SaveChangesAsync();

            // Log in tenant audit log
            try
            {
                await using var tenantDb = await _tenantFactory.CreateAsync(targetTenantId);
                tenantDb.RetentionAuditLogs.Add(new RetentionAuditLog
                {
                    PerformedByUserId = caller.UserId,
                    ActionType = "BrandingLogoUploaded",
                    Detail = $"Tenant brand logo updated to version {branding.LogoVersion}",
                    CreatedAt = DateTime.UtcNow
                });
                await tenantDb.SaveChangesAsync();
            }
            catch { }

            return Ok(new { success = true, logoVersion = branding.LogoVersion });
        }

        [HttpDelete("logo")]
        public async Task<IActionResult> RemoveLogo([FromQuery] int? tenantId = null)
        {
            var caller = GetCaller();
            if (caller.TenantId <= 0 && !caller.Role.Equals("SuperAdmin", StringComparison.OrdinalIgnoreCase))
            {
                return Unauthorized();
            }

            if (!caller.Role.Equals("Admin", StringComparison.OrdinalIgnoreCase) &&
                !caller.Role.Equals("SuperAdmin", StringComparison.OrdinalIgnoreCase))
            {
                return Forbid();
            }

            int targetTenantId = caller.Role.Equals("SuperAdmin", StringComparison.OrdinalIgnoreCase)
                ? (tenantId ?? caller.TenantId)
                : caller.TenantId;

            if (targetTenantId <= 0)
            {
                return BadRequest(new { message = "Valid tenant ID is required." });
            }

            var branding = await _masterDb.TenantBrandings.FirstOrDefaultAsync(b => b.CompanyId == targetTenantId);
            if (branding != null && branding.LogoImage != null)
            {
                branding.LogoImage = null;
                branding.LogoVersion++;
                branding.UpdatedAt = DateTime.UtcNow;
                branding.UpdatedByUserId = caller.UserId;
                await _masterDb.SaveChangesAsync();

                try
                {
                    await using var tenantDb = await _tenantFactory.CreateAsync(targetTenantId);
                    tenantDb.RetentionAuditLogs.Add(new RetentionAuditLog
                    {
                        PerformedByUserId = caller.UserId,
                        ActionType = "BrandingLogoRemoved",
                        Detail = $"Tenant brand logo removed; reset to default icon",
                        CreatedAt = DateTime.UtcNow
                    });
                    await tenantDb.SaveChangesAsync();
                }
                catch { }
            }

            return Ok(new { success = true });
        }

        [HttpPost("reset")]
        public async Task<IActionResult> ResetBranding([FromQuery] int? tenantId = null)
        {
            var caller = GetCaller();
            if (caller.TenantId <= 0 && !caller.Role.Equals("SuperAdmin", StringComparison.OrdinalIgnoreCase))
            {
                return Unauthorized();
            }

            if (!caller.Role.Equals("Admin", StringComparison.OrdinalIgnoreCase) &&
                !caller.Role.Equals("SuperAdmin", StringComparison.OrdinalIgnoreCase))
            {
                return Forbid();
            }

            int targetTenantId = caller.Role.Equals("SuperAdmin", StringComparison.OrdinalIgnoreCase)
                ? (tenantId ?? caller.TenantId)
                : caller.TenantId;

            if (targetTenantId <= 0)
            {
                return BadRequest(new { message = "Valid tenant ID is required." });
            }

            var company = await _masterDb.Companies
                .Include(c => c.Branding)
                .FirstOrDefaultAsync(c => c.CompanyId == targetTenantId);

            if (company == null) return NotFound();

            var branding = company.Branding;
            if (branding != null)
            {
                branding.DisplayName = company.CompanyName;
                branding.LogoImage = null;
                branding.LogoVersion++;
                branding.AccentColor = null;
                branding.HidePoweredBy = false;
                branding.ContactEmail = null;
                branding.ContactPhone = null;
                branding.Address = null;
                branding.UpdatedAt = DateTime.UtcNow;
                branding.UpdatedByUserId = caller.UserId;
            }
            else
            {
                branding = new TenantBranding
                {
                    CompanyId = targetTenantId,
                    DisplayName = company.CompanyName,
                    LogoVersion = 1,
                    UpdatedAt = DateTime.UtcNow,
                    UpdatedByUserId = caller.UserId
                };
                _masterDb.TenantBrandings.Add(branding);
            }

            await _masterDb.SaveChangesAsync();

            try
            {
                await using var tenantDb = await _tenantFactory.CreateAsync(targetTenantId);
                tenantDb.RetentionAuditLogs.Add(new RetentionAuditLog
                {
                    PerformedByUserId = caller.UserId,
                    ActionType = "BrandingReset",
                    Detail = $"Tenant branding reset to company name '{company.CompanyName}'",
                    CreatedAt = DateTime.UtcNow
                });
                await tenantDb.SaveChangesAsync();
            }
            catch { }

            return Ok(new { success = true });
        }
    }
}
