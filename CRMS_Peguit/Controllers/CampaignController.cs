using System;
using System.Collections.Generic;
using System.Linq;
using CRMS_Peguit.domain.entities;
using CRMS_Peguit.infrastructure.data;
using CRMS_Peguit.winforms.Auth;
using CRMS_Peguit.winforms.Models.Services;
using CRMS_Peguit.winforms.Models.ViewModels;
using Microsoft.EntityFrameworkCore;

namespace CRMS_Peguit.winforms.Controllers
{
    public class CampaignController : IDisposable
    {
        private readonly RealEstateDbContext _db;
        private readonly LeadController _leadController;

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

        public CampaignController()
        {
            int tenantId = CurrentSession.TenantId > 0 ? CurrentSession.TenantId : 1;
            _db = LocalDb.CreateContext(tenantId);
            _leadController = new LeadController();
        }

        public CampaignController(LeadController leadController)
        {
            int tenantId = CurrentSession.TenantId > 0 ? CurrentSession.TenantId : 1;
            _db = LocalDb.CreateContext(tenantId);
            _leadController = leadController;
        }

        /// <summary>
        /// Returns all active campaign names and standard lead channels for dropdown selection.
        /// </summary>
        public List<string> GetActiveCampaignSources()
        {
            var sources = new HashSet<string>(StandardChannels, StringComparer.OrdinalIgnoreCase);

            try
            {
                var dbCampaigns = _db.Campaigns
                    .AsNoTracking()
                    .Where(c => c.IsActive && c.Status == "Active")
                    .Select(c => c.Name)
                    .ToList();

                foreach (var c in dbCampaigns)
                {
                    if (!string.IsNullOrWhiteSpace(c))
                    {
                        sources.Add(c.Trim());
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[CampaignController.GetActiveCampaignSources] Error: {ex.Message}");
            }

            return sources.OrderBy(s => s).ToList();
        }

        /// <summary>
        /// Persists a new marketing campaign or lead source channel into the database.
        /// </summary>
        public bool AddCampaign(string name, string? channel = null, decimal? budget = null)
        {
            if (string.IsNullOrWhiteSpace(name))
                return false;

            string trimmedName = name.Trim();

            try
            {
                var existing = _db.Campaigns
                    .FirstOrDefault(c => c.Name.ToLower() == trimmedName.ToLower());

                if (existing != null)
                {
                    existing.IsActive = true;
                    existing.Status = "Active";
                    if (!string.IsNullOrWhiteSpace(channel))
                        existing.Channel = channel.Trim();
                    if (budget.HasValue)
                        existing.Budget = budget.Value;
                }
                else
                {
                    _db.Campaigns.Add(new Campaign
                    {
                        TenantId = CurrentSession.TenantId > 0 ? CurrentSession.TenantId : 1,
                        Name = trimmedName,
                        Channel = string.IsNullOrWhiteSpace(channel) ? "Direct" : channel.Trim(),
                        Status = "Active",
                        Budget = budget,
                        IsActive = true,
                        CreatedAt = DateTime.UtcNow
                    });
                }

                _db.SaveChanges();
                return true;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[CampaignController.AddCampaign] Error: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Legacy overload for backward compatibility with CampaignsView.
        /// </summary>
        public void AddCustomChannel(string channelName)
        {
            AddCampaign(channelName);
        }

        public CampaignSummaryViewModel GetCampaignSummary(string selectedChannel = "All")
        {
            var leads = _leadController.GetAll();
            var sources = new HashSet<string>(GetActiveCampaignSources(), StringComparer.OrdinalIgnoreCase);

            foreach (var l in leads)
            {
                if (!string.IsNullOrWhiteSpace(l.Source))
                {
                    sources.Add(l.Source.Trim());
                }
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

            return new CampaignSummaryViewModel
            {
                TotalLeads = leads.Count,
                TopChannelText = topText,
                Channels = orderedChannels,
                ChannelCounts = channelCounts,
                FilteredLeads = filtered
            };
        }

        public void Dispose()
        {
            _db.Dispose();
            _leadController.Dispose();
        }
    }
}
