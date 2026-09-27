using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using CRMS_Peguit.domain.entities;
using CRMS_Peguit.infrastructure.data;

namespace CRMS_Peguit.api.Controllers
{
    public record AddCampaignRequest(string Name, string? Channel = null, decimal? Budget = null);
    public record AddChannelRequest(string ChannelName);
    public record CampaignSummaryDto(
        int TotalLeads,
        string TopChannelText,
        List<string> Channels,
        Dictionary<string, int> ChannelCounts,
        List<Lead> FilteredLeads);

    [ApiController]
    [Route("api/[controller]")]
    public class CampaignsController : ControllerBase
    {
        private readonly RealEstateDbContext _db;

        private static readonly string[] StandardChannels = new[]
        {
            "Facebook Ad",
            "Referral",
            "Walk-in",
            "Website",
            "Property Portal",
            "Google Ads",
            "Billboard / Outdoor",
            "Open House / Event"
        };

        public CampaignsController(RealEstateDbContext db)
        {
            _db = db;
        }

        private (int UserId, string Role, int TenantId) CurrentUser =>
            ApiSecurityHelper.GetCurrentUserInfo(HttpContext);

        [HttpGet("sources")]
        public async Task<IActionResult> GetActiveCampaignSources()
        {
            var sources = new HashSet<string>(StandardChannels, StringComparer.OrdinalIgnoreCase);

            try
            {
                var dbCampaigns = await _db.Campaigns
                    .AsNoTracking()
                    .Where(c => c.IsActive && c.Status == "Active")
                    .Select(c => c.Name)
                    .ToListAsync();

                foreach (var c in dbCampaigns)
                {
                    if (!string.IsNullOrWhiteSpace(c))
                    {
                        sources.Add(c.Trim());
                    }
                }
            }
            catch { }

            return Ok(sources.OrderBy(s => s).ToList());
        }

        [HttpPost]
        public async Task<IActionResult> AddCampaign([FromBody] AddCampaignRequest req)
        {
            if (string.IsNullOrWhiteSpace(req.Name))
                return BadRequest("Campaign name is required.");

            string trimmedName = req.Name.Trim();
            var existing = await _db.Campaigns
                .FirstOrDefaultAsync(c => c.Name.ToLower() == trimmedName.ToLower());

            if (existing != null)
            {
                existing.IsActive = true;
                existing.Status = "Active";
                if (!string.IsNullOrWhiteSpace(req.Channel))
                    existing.Channel = req.Channel.Trim();
                if (req.Budget.HasValue)
                    existing.Budget = req.Budget.Value;
            }
            else
            {
                var user = CurrentUser;
                if (user.TenantId <= 0) return Unauthorized();

                _db.Campaigns.Add(new Campaign
                {
                    TenantId = user.TenantId,
                    Name = trimmedName,
                    Channel = string.IsNullOrWhiteSpace(req.Channel) ? "Direct" : req.Channel.Trim(),
                    Status = "Active",
                    Budget = req.Budget,
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow
                });
            }

            await _db.SaveChangesAsync();
            return Ok(new { success = true });
        }

        [HttpPost("channels")]
        public async Task<IActionResult> AddCustomChannel([FromBody] AddChannelRequest req)
        {
            return await AddCampaign(new AddCampaignRequest(req.ChannelName));
        }

        [HttpGet("summary")]
        public async Task<IActionResult> GetCampaignSummary([FromQuery] string selectedChannel = "All")
        {
            var user = CurrentUser;
            var leadsQuery = _db.Leads
                .Where(l => !l.IsDeleted);

            if (!ApiSecurityHelper.HasFullOversight(user.Role) && ApiSecurityHelper.IsAgent(user.Role) && user.UserId > 0)
            {
                leadsQuery = leadsQuery.Where(l =>
                    (l.AssignedAgentId.HasValue && l.AssignedAgentId.Value > 0)
                        ? l.AssignedAgentId.Value == user.UserId
                        : l.CreatedByUserId == user.UserId);
            }

            var leads = await leadsQuery.ToListAsync();

            var sources = new HashSet<string>(StandardChannels, StringComparer.OrdinalIgnoreCase);
            var dbCampaigns = await _db.Campaigns
                .AsNoTracking()
                .Where(c => c.IsActive && c.Status == "Active")
                .Select(c => c.Name)
                .ToListAsync();

            foreach (var c in dbCampaigns)
            {
                if (!string.IsNullOrWhiteSpace(c)) sources.Add(c.Trim());
            }

            foreach (var l in leads)
            {
                if (!string.IsNullOrWhiteSpace(l.Source)) sources.Add(l.Source.Trim());
            }

            var orderedChannels = sources.OrderBy(s => s).ToList();
            var channelCounts = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            foreach (var s in orderedChannels)
            {
                channelCounts[s] = leads.Count(l => string.Equals(l.Source?.Trim(), s, StringComparison.OrdinalIgnoreCase));
            }

            var topSource = leads
                .Where(l => !string.IsNullOrWhiteSpace(l.Source))
                .GroupBy(l => l.Source!.Trim(), StringComparer.OrdinalIgnoreCase)
                .OrderByDescending(g => g.Count())
                .FirstOrDefault();

            string topText = topSource != null
                ? $"Top Channel: {topSource.Key} ({topSource.Count()} leads)"
                : "Top Channel: None";

            var filtered = string.Equals(selectedChannel, "All", StringComparison.OrdinalIgnoreCase)
                ? leads
                : leads.Where(l => string.Equals(l.Source?.Trim(), selectedChannel, StringComparison.OrdinalIgnoreCase)).ToList();

            return Ok(new CampaignSummaryDto(
                leads.Count,
                topText,
                orderedChannels,
                channelCounts,
                filtered));
        }
    }
}
