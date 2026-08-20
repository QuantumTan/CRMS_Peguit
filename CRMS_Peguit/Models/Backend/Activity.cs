using System;

namespace NEXA.Model
{
    public enum ActivityType
    {
        Call,
        Email,
        Meeting
    }

    
    // CHILD CLASS - Activity (inherits BaseEntity)
    
    public class Activity : BaseEntity
    {
        // PUBLIC - low sensitivity, freely read/edited by staff who logged it
        public ActivityType Type;
        public string Notes;
        public DateTime Date;
        public int LoggedByUserId;

        // Either one of these will be set, depending on whether the
        // activity is tied to a Lead (pre-conversion) or a Customer (post-conversion)
        public int? CustomerId;
        public int? LeadId;

        public Activity(ActivityType type, string notes, int loggedByUserId) : base()
        {
            Type = type;
            Notes = notes;
            Date = DateTime.Now;
            LoggedByUserId = loggedByUserId;
        }

        public void LinkToCustomer(int customerId)
        {
            CustomerId = customerId;
            LeadId = null;
            UpdateTimestamp();
        }

        public void LinkToLead(int leadId)
        {
            LeadId = leadId;
            CustomerId = null;
            UpdateTimestamp();
        }

        public void UpdateNotes(string newNotes)
        {
            Notes = newNotes;
            UpdateTimestamp();
        }
    }
}