using System;

namespace NEXA.Model
{
    public enum CampaignType
    {
        FacebookAd,
        Referral,
        WalkIn,
        Other
    }

    // ============================================
    // CHILD CLASS - Campaign (inherits BaseEntity)
    // ============================================
    public class Campaign : BaseEntity
    {
        // PUBLIC - low sensitivity, managed by Admin/Manager
        public string Name;
        public CampaignType Type;

        public Campaign(string name, CampaignType type) : base()
        {
            Name = name;
            Type = type;
        }

        public void Rename(string newName)
        {
            Name = newName;
            UpdateTimestamp();
        }
    }
}