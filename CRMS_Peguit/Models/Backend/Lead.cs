using System;

namespace NEXA.Model
{
    public enum LeadStage
    {
        New,
        Contacted,
        Qualified,
        Converted,
        Lost
    }

    // ============================================
    // CHILD CLASS - Lead (inherits BaseEntity)
    // ============================================
    public class Lead : BaseEntity
    {
        // PUBLIC
        public string Name;
        public string CompanyName;
        public string Source;          // free-text fallback label 
        public int? CampaignId;        // NEW - links to Campaign for grouped reporting
        public int AssignedToUserId;

        // PRIVATE - stage can only move through valid transitions
        private LeadStage _stage;
        private int? _convertedCustomerId;

        public Lead(string name, string companyName, string source) : base()
        {
            Name = name;
            CompanyName = companyName;
            Source = source;
            _stage = LeadStage.New;
        }

        public LeadStage Stage
        {
            get { return _stage; }
        }

        public int? ConvertedCustomerId
        {
            get { return _convertedCustomerId; }
        }

        // NEW - tag this lead with the campaign/source it came from -- added for revisions
        public void AssignCampaign(int campaignId)
        {
            CampaignId = campaignId;
            UpdateTimestamp();
        }

        // Enforces that a lead can't jump straight from New to Won/Converted, etc.
        public void AdvanceStage(LeadStage nextStage)
        {
            if (_stage == LeadStage.Converted || _stage == LeadStage.Lost)
            {
                throw new InvalidOperationException("Cannot change stage of a closed lead.");
            }

            _stage = nextStage;
            UpdateTimestamp();
        }

        // Converting is a specific, controlled action - not a generic setter
        public Customer ConvertToCustomer(int newCustomerId)
        {
            _stage = LeadStage.Converted;
            _convertedCustomerId = newCustomerId;
            UpdateTimestamp();

            Customer customer = new Customer(Name, CompanyName);
            return customer;
        }

        public void MarkLost()
        {
            _stage = LeadStage.Lost;
            UpdateTimestamp();
        }
    }
}