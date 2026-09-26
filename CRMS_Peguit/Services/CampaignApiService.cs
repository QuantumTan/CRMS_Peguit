using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using CRMS_Peguit.domain.entities;
using CRMS_Peguit.winforms.Models.ViewModels;

namespace CRMS_Peguit.winforms.Services
{
    public class CampaignApiService : BaseApiService
    {
        private readonly LeadApiService _leadService;

        public CampaignApiService(string? baseUrl = null, HttpClient? httpClient = null)
            : base(baseUrl, httpClient)
        {
            _leadService = new LeadApiService(baseUrl, httpClient: Client);
        }

        public List<string> GetActiveCampaignSources() =>
            Get<List<string>>("api/campaigns/sources") ?? new();

        public bool AddCampaign(string name, string? channel = null, decimal? budget = null)
        {
            if (string.IsNullOrWhiteSpace(name)) return false;
            return Post("api/campaigns", new { name = name.Trim(), channel = channel?.Trim(), budget });
        }

        public void AddCustomChannel(string channelName) =>
            AddCampaign(channelName);

        public CampaignSummaryViewModel GetCampaignSummary(string selectedChannel = "All")
        {
            var leads = _leadService.GetAll();
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

        public override void Dispose()
        {
            base.Dispose();
            _leadService?.Dispose();
        }
    }
}
