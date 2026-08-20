using System;

namespace NEXA.Model
{
    public enum ShowingStatus
    {
        Scheduled,
        Completed,
        Cancelled
    }

    // ============================================
    // CHILD CLASS - Showing (inherits BaseEntity)
    // A property viewing appointment - distinct from a generic Activity
    // because it always involves a specific Property + a specific Buyer.
    // ============================================
    public class Showing : BaseEntity
    {
        // PUBLIC
        public int PropertyId;
        public int CustomerId;      // the buyer viewing the property
        public int AgentUserId;
        public DateTime ScheduledDate;
        public string FeedbackNotes;

        // PRIVATE - controlled status transitions
        private ShowingStatus _status;

        public Showing(int propertyId, int customerId, int agentUserId, DateTime scheduledDate) : base()
        {
            PropertyId = propertyId;
            CustomerId = customerId;
            AgentUserId = agentUserId;
            ScheduledDate = scheduledDate;
            _status = ShowingStatus.Scheduled;
        }

        public ShowingStatus Status
        {
            get { return _status; }
        }

        public void MarkCompleted(string feedbackNotes)
        {
            if (_status == ShowingStatus.Cancelled)
            {
                throw new InvalidOperationException("Cannot complete a cancelled showing.");
            }
            _status = ShowingStatus.Completed;
            FeedbackNotes = feedbackNotes;
            UpdateTimestamp();
        }

        public void Cancel()
        {
            _status = ShowingStatus.Cancelled;
            UpdateTimestamp();
        }

        public void Reschedule(DateTime newDate)
        {
            if (_status != ShowingStatus.Scheduled)
            {
                throw new InvalidOperationException("Only a scheduled showing can be rescheduled.");
            }
            ScheduledDate = newDate;
            UpdateTimestamp();
        }
    }
}