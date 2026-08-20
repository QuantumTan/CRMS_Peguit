using System;

namespace NEXA.Model
{
    public enum TaskStatus
    {
        Pending,
        Completed,
        Overdue
    }

    
    // CHILD CLASS - TaskReminder (inherits BaseEntity)
    
    public class TaskReminder : BaseEntity
    {
        // PUBLIC
        public string Title;
        public DateTime DueDate;
        public int AssignedToUserId;

        // Either one of these may be set - what the task relates to
        public int? RelatedCustomerId;
        public int? RelatedLeadId;

        // PRIVATE - status only changes through controlled methods,
        // so "Overdue" can't accidentally be set by hand when it should be computed
        private TaskStatus _status;

        public TaskReminder(string title, DateTime dueDate, int assignedToUserId) : base()
        {
            Title = title;
            DueDate = dueDate;
            AssignedToUserId = assignedToUserId;
            _status = TaskStatus.Pending;
        }

        public TaskStatus Status
        {
            get { return _status; }
        }

        public void MarkComplete()
        {
            _status = TaskStatus.Completed;
            UpdateTimestamp();
        }

        // Called by a scheduled check (or on load) rather than set directly,
        // so overdue status always reflects the real due date
        public void RefreshOverdueStatus()
        {
            if (_status == TaskStatus.Pending && DueDate < DateTime.Now)
            {
                _status = TaskStatus.Overdue;
                UpdateTimestamp();
            }
        }

        public void Reschedule(DateTime newDueDate)
        {
            DueDate = newDueDate;
            if (_status == TaskStatus.Overdue)
            {
                _status = TaskStatus.Pending;
            }
            UpdateTimestamp();
        }
    }
}