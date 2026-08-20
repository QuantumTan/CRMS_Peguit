using System;

namespace NEXA.Model
{
    public enum CustomerStatus
    {
        Active,
        Inactive,
        Prospect
    }

    // Real estate needs to distinguish who this person is in a transaction
    public enum CustomerType
    {
        Buyer,
        Seller,
        Both
    }

    // ============================================
    // CHILD CLASS - Customer (inherits BaseEntity)
    // ============================================
    public class Customer : BaseEntity
    {
        // PUBLIC - everyday CRM fields
        public string Name;
        public string CompanyName;
        public string Phone;
        public string Email;
        public string Notes;
        public CustomerStatus Status;
        public DateTime? LastContactedDate;

        // Real estate-specific: which side of a transaction they're on,
        // plus buyer preferences used for matching against Property listings
        public CustomerType Type;
        public decimal? Budget;
        public string PreferredLocation;
        public PropertyType? PreferredPropertyType;

        // PRIVATE - only changed through a controlled method
        private int _assignedToUserId;

        public Customer(string name, string companyName) : base()
        {
            Name = name;
            CompanyName = companyName;
            Status = CustomerStatus.Prospect;
            Type = CustomerType.Buyer;
        }

        public int AssignedToUserId
        {
            get { return _assignedToUserId; }
        }

        // Reassignment goes through here (not a public setter) so that
        // in future you can add validation/logging - e.g. only Manager/Admin can call it
        public void ReassignTo(int newUserId)
        {
            _assignedToUserId = newUserId;
            UpdateTimestamp();
        }

        public void LogContact(DateTime contactDate)
        {
            LastContactedDate = contactDate;
            UpdateTimestamp();
        }

        public void UpdateBasicInfo(string name, string companyName, string phone, string email)
        {
            Name = name;
            CompanyName = companyName;
            Phone = phone;
            Email = email;
            UpdateTimestamp();
        }

        // Buyer-specific: capture what they're looking for, used to match against Property listings
        public void SetBuyerPreferences(decimal budget, string preferredLocation, PropertyType preferredPropertyType)
        {
            Budget = budget;
            PreferredLocation = preferredLocation;
            PreferredPropertyType = preferredPropertyType;
            UpdateTimestamp();
        }
    }
}