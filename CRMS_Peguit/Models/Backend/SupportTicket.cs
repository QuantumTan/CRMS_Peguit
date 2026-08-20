using System;

namespace NEXA.Model
{
    public enum TicketPriority
    {
        Low,
        Medium,
        High
    }

    public enum TicketStatus
    {
        Open,
        InProgress,
        Resolved
    }

    
    // CHILD CLASS - SupportTicket (inherits BaseEntity)
    
    public class SupportTicket : BaseEntity
    {
        // PUBLIC - low sensitivity
        public int CustomerId;
        public string Description;
        public TicketPriority Priority;

        // PRIVATE - controlled transitions, same reasoning as Lead/Deal stage
        private TicketStatus _status;
        private int _assignedToUserId;

        public SupportTicket(int customerId, string description, TicketPriority priority, int assignedToUserId) : base()
        {
            CustomerId = customerId;
            Description = description;
            Priority = priority;
            _assignedToUserId = assignedToUserId;
            _status = TicketStatus.Open;
        }

        public TicketStatus Status
        {
            get { return _status; }
        }

        public int AssignedToUserId
        {
            get { return _assignedToUserId; }
        }

        // Enforces a forward-only flow: Open -> InProgress -> Resolved
        public void MarkInProgress()
        {
            if (_status == TicketStatus.Resolved)
            {
                throw new InvalidOperationException("Cannot reopen a resolved ticket this way.");
            }
            _status = TicketStatus.InProgress;
            UpdateTimestamp();
        }

        public void MarkResolved()
        {
            _status = TicketStatus.Resolved;
            UpdateTimestamp();
        }

        // Manager-driven action: reassign the ticket to a different Sales Staff member
        public void ReassignTo(int newUserId)
        {
            _assignedToUserId = newUserId;
            UpdateTimestamp();
        }
    }
}