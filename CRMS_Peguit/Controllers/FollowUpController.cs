using System;
using System.Collections.Generic;
using System.Linq;
using CRMS_Peguit.domain.entities;
using CRMS_Peguit.infrastructure.data;
using CRMS_Peguit.winforms.Auth;
using CRMS_Peguit.winforms.Models.Services;
using CRMS_Peguit.winforms.Services.Offline;
using Microsoft.EntityFrameworkCore;

namespace CRMS_Peguit.winforms.Controllers
{
    public class FollowUpController : IDisposable
    {
        private readonly RealEstateDbContext _db;
        private int TenantId => CurrentSession.TenantId;

        public FollowUpController()
        {
            _db = LocalDb.CreateContext(TenantId);
        }

        /// <summary>
        /// Retrieves all active (non-deleted) follow-ups for the currently authenticated Agent.
        /// Manager/Admin cannot access this module, so results are strictly scoped to CurrentSession.UserId.
        /// Also automatically checks and updates overdue status for pending follow-ups whose due date has passed.
        /// </summary>
        public List<TaskReminder> GetAll()
        {
            if (CurrentSession.IsOffline || !SyncService.Instance.IsOnline)
            {
                return LocalDataCache.Instance.GetCachedTaskReminders(TenantId, CurrentSession.UserId, RbacService.IsAgent);
            }

            try
            {
                int currentUserId = CurrentSession.UserId;
                if (currentUserId <= 0) return new List<TaskReminder>();

                // First, find any pending reminders whose due date has passed and update to Overdue
                var now = DateTime.UtcNow;
                var pendingOverdue = _db.TaskReminders
                    .Where(r => r.AssignedToUserId == currentUserId
                                && !r.IsDeleted
                                && r.Status == "Pending"
                                && r.DueDate < now)
                    .ToList();

                if (pendingOverdue.Count > 0)
                {
                    foreach (var item in pendingOverdue)
                    {
                        item.Status = "Overdue";
                        item.UpdatedAt = now;
                    }
                    _db.SaveChanges();
                }

                // Query all non-deleted follow-ups for this agent with related entities included
                return _db.TaskReminders
                    .AsNoTracking()
                    .Include(r => r.RelatedCustomer)
                        .ThenInclude(c => c!.Person)
                    .Include(r => r.RelatedLead)
                        .ThenInclude(l => l!.Person)
                    .Where(r => r.AssignedToUserId == currentUserId && !r.IsDeleted)
                    .OrderBy(r => r.DueDate)
                    .ToList();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[FollowUpController.GetAll] Error: {ex.Message}");
                return new List<TaskReminder>();
            }
        }

        public TaskReminder? GetById(int id)
        {
            try
            {
                int currentUserId = CurrentSession.UserId;
                if (currentUserId <= 0) return null;

                return _db.TaskReminders
                    .AsNoTracking()
                    .Include(r => r.RelatedCustomer)
                        .ThenInclude(c => c!.Person)
                    .Include(r => r.RelatedLead)
                        .ThenInclude(l => l!.Person)
                    .SingleOrDefault(r => r.TaskReminderId == id && r.AssignedToUserId == currentUserId && !r.IsDeleted);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[FollowUpController.GetById] Error: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Adds a new Follow-Up. Enforces:
        /// 1. Exactly one relationship (Customer XOR Lead).
        /// 2. Assigned exclusively to CurrentSession.UserId (the creating Agent).
        /// 3. Initial status computed based on DueDate (Pending or Overdue).
        /// 4. Relationship must be assigned to current agent.
        /// </summary>
        public TaskReminder Add(TaskReminder reminder)
        {
            ValidateRelationshipExclusivity(reminder);

            int currentUserId = CurrentSession.UserId;
            if (currentUserId <= 0)
            {
                throw new InvalidOperationException("An active Agent session is required to create a Follow-Up.");
            }

            // Enforce Agent ownership
            reminder.AssignedToUserId = currentUserId;
            reminder.CreatedAt = DateTime.UtcNow;
            reminder.UpdatedAt = null;
            reminder.CompletedAt = null;
            reminder.IsDeleted = false;
            reminder.DeletedAt = null;

            // Automatically set status based on DueDate
            reminder.Status = reminder.DueDate < DateTime.UtcNow ? "Overdue" : "Pending";

            if (CurrentSession.IsOffline || !SyncService.Instance.IsOnline)
            {
                var qItem = SyncService.Instance.EnqueueOfflineCreate("TaskReminder", reminder, TenantId, currentUserId);
                reminder.TaskReminderId = -qItem.QueueId;
                return reminder;
            }

            _db.TaskReminders.Add(reminder);
            _db.SaveChanges();

            return reminder;
        }

        public void Update(TaskReminder reminder)
        {
            ValidateRelationshipExclusivity(reminder);

            int currentUserId = CurrentSession.UserId;

            if (CurrentSession.IsOffline || !SyncService.Instance.IsOnline)
            {
                SyncService.Instance.EnqueueOfflineUpdate("TaskReminder", reminder.TaskReminderId, reminder, TenantId, currentUserId, reminder.UpdatedAt ?? reminder.CreatedAt);
                return;
            }

            var item = _db.TaskReminders
                .SingleOrDefault(r => r.TaskReminderId == reminder.TaskReminderId && r.AssignedToUserId == currentUserId && !r.IsDeleted);

            if (item is null)
            {
                throw new InvalidOperationException("Follow-Up not found or access denied.");
            }

            item.Title = reminder.Title.Trim();
            item.DueDate = reminder.DueDate;
            item.Type = reminder.Type;
            item.Priority = reminder.Priority;
            item.Notes = reminder.Notes?.Trim();
            item.RelatedCustomerId = reminder.RelatedCustomerId;
            item.RelatedLeadId = reminder.RelatedLeadId;
            item.UpdatedAt = DateTime.UtcNow;

            // If not completed, re-evaluate status against updated due date
            if (item.Status != "Completed")
            {
                item.Status = item.DueDate < DateTime.UtcNow ? "Overdue" : "Pending";
            }

            _db.SaveChanges();
        }

        /// <summary>
        /// Completes a Follow-Up and optionally logs a resulting Activity in the client timeline.
        /// </summary>
        public void MarkComplete(int id, bool logActivity = false, string? activityNotes = null)
        {
            int currentUserId = CurrentSession.UserId;
            var item = _db.TaskReminders
                .SingleOrDefault(r => r.TaskReminderId == id && r.AssignedToUserId == currentUserId && !r.IsDeleted);

            if (item is null) return;

            item.MarkComplete();

            if (logActivity)
            {
                string noteText = !string.IsNullOrWhiteSpace(activityNotes)
                    ? activityNotes.Trim()
                    : (!string.IsNullOrWhiteSpace(item.Notes) ? item.Notes : $"Completed follow-up: {item.Title}");

                var activity = new Activity
                {
                    Type = item.Type,
                    RelatedCustomerId = item.RelatedCustomerId,
                    RelatedLeadId = item.RelatedLeadId,
                    LoggedByAgentId = currentUserId,
                    Notes = noteText,
                    ActivityDate = DateTime.UtcNow
                };
                _db.Activities.Add(activity);
            }

            // Auto-advance lead stage from 'new' to 'contacted' upon completing follow-up
            if (item.RelatedLeadId.HasValue && item.RelatedLeadId.Value > 0)
            {
                var lead = _db.Leads.FirstOrDefault(l => l.LeadId == item.RelatedLeadId.Value);
                if (lead != null && string.Equals(lead.Stage, "new", StringComparison.OrdinalIgnoreCase))
                {
                    lead.Stage = "contacted";
                }
            }

            _db.SaveChanges();
        }

        /// <summary>
        /// Snoozes a Follow-Up by pushing the DueDate forward by the specified interval
        /// and resetting status back to Pending.
        /// </summary>
        public void Snooze(int id, TimeSpan interval)
        {
            int currentUserId = CurrentSession.UserId;
            var item = _db.TaskReminders
                .SingleOrDefault(r => r.TaskReminderId == id && r.AssignedToUserId == currentUserId && !r.IsDeleted);

            if (item is null) return;

            item.Snooze(interval);
            _db.SaveChanges();
        }

        /// <summary>
        /// Reschedules a Follow-Up to a new date/time and resets status back to Pending.
        /// </summary>
        public void Reschedule(int id, DateTime newDueDate)
        {
            int currentUserId = CurrentSession.UserId;
            var item = _db.TaskReminders
                .SingleOrDefault(r => r.TaskReminderId == id && r.AssignedToUserId == currentUserId && !r.IsDeleted);

            if (item is null) return;

            item.Reschedule(newDueDate);
            _db.SaveChanges();
        }

        /// <summary>
        /// Soft deletes a Follow-Up. Hard deletes are strictly forbidden.
        /// </summary>
        public void SoftDelete(int id)
        {
            int currentUserId = CurrentSession.UserId;
            var item = _db.TaskReminders
                .SingleOrDefault(r => r.TaskReminderId == id && r.AssignedToUserId == currentUserId && !r.IsDeleted);

            if (item is null) return;

            item.IsDeleted = true;
            item.DeletedAt = DateTime.UtcNow;
            _db.SaveChanges();
        }

        /// <summary>
        /// Returns customers assigned to the currently authenticated Agent.
        /// </summary>
        public List<Customer> GetAssignedCustomers()
        {
            int currentUserId = CurrentSession.UserId;
            if (currentUserId <= 0) return new List<Customer>();

            return _db.Customers
                .AsNoTracking()
                .Include(c => c.Person)
                .Where(c => c.AssignedAgentId == currentUserId && !c.IsDeleted)
                .OrderBy(c => c.Person.LastName)
                .ThenBy(c => c.Person.FirstName)
                .ToList();
        }

        /// <summary>
        /// Returns leads assigned to the currently authenticated Agent.
        /// </summary>
        public List<Lead> GetAssignedLeads()
        {
            int currentUserId = CurrentSession.UserId;
            if (currentUserId <= 0) return new List<Lead>();

            return _db.Leads
                .AsNoTracking()
                .Include(l => l.Person)
                .Where(l => l.AssignedAgentId == currentUserId && !l.IsDeleted && l.Stage != "lost")
                .OrderBy(l => l.Person.LastName)
                .ThenBy(l => l.Person.FirstName)
                .ToList();
        }

        /// <summary>
        /// Calculates KPI counts for Overdue, Due Today, Upcoming, and Completed for the logged-in Agent.
        /// </summary>
        public FollowUpKpiCounts GetKpiCounts()
        {
            int currentUserId = CurrentSession.UserId;
            if (currentUserId <= 0) return new FollowUpKpiCounts(0, 0, 0, 0, 0);

            var now = DateTime.UtcNow;
            var todayLocal = DateTime.Today;
            var tomorrowLocal = todayLocal.AddDays(1);

            var items = _db.TaskReminders
                .AsNoTracking()
                .Where(r => r.AssignedToUserId == currentUserId && !r.IsDeleted)
                .ToList();

            int completed = items.Count(r => r.Status == "Completed");
            int overdue = items.Count(r => r.Status != "Completed" && (r.Status == "Overdue" || r.DueDate < now));
            int today = items.Count(r => r.Status != "Completed" && r.DueDate.ToLocalTime().Date == todayLocal);
            int upcoming = items.Count(r => r.Status != "Completed" && r.DueDate.ToLocalTime().Date >= tomorrowLocal);

            return new FollowUpKpiCounts(
                Total: items.Count,
                Overdue: overdue,
                Today: today,
                Upcoming: upcoming,
                Completed: completed
            );
        }

        public static bool ValidateInput(string title, int? customerId, int? leadId, out string? errorMessage)
        {
            if (string.IsNullOrWhiteSpace(title))
            {
                errorMessage = "Please provide a title for this follow-up.";
                return false;
            }

            bool hasCust = customerId.HasValue && customerId.Value > 0;
            bool hasLead = leadId.HasValue && leadId.Value > 0;

            if (!hasCust && !hasLead)
            {
                errorMessage = "A follow-up must be linked to either a Customer or a Lead.";
                return false;
            }

            if (hasCust && hasLead)
            {
                errorMessage = "A follow-up cannot be linked to both a Customer and a Lead simultaneously.";
                return false;
            }

            errorMessage = null;
            return true;
        }

        private static void ValidateRelationshipExclusivity(TaskReminder reminder)
        {
            bool hasCust = reminder.RelatedCustomerId.HasValue && reminder.RelatedCustomerId.Value > 0;
            bool hasLead = reminder.RelatedLeadId.HasValue && reminder.RelatedLeadId.Value > 0;

            if (!hasCust && !hasLead)
            {
                throw new InvalidOperationException("A follow-up must relate to exactly one of Customer or Lead.");
            }

            if (hasCust && hasLead)
            {
                throw new InvalidOperationException("A follow-up cannot relate to both Customer and Lead simultaneously.");
            }
        }

        public List<TaskReminder> GetFollowUpsDueToday(int maxCount = 5)
        {
            try
            {
                int currentUserId = CurrentSession.UserId;
                if (currentUserId <= 0) return new List<TaskReminder>();

                var todayLocal = DateTime.Today;

                return _db.TaskReminders
                    .AsNoTracking()
                    .Include(r => r.RelatedCustomer).ThenInclude(c => c!.Person)
                    .Include(r => r.RelatedLead).ThenInclude(l => l!.Person)
                    .Where(r => r.AssignedToUserId == currentUserId && !r.IsDeleted && r.Status != "Completed")
                    .AsEnumerable()
                    .Where(r => r.DueDate.ToLocalTime().Date == todayLocal || r.Status == "Overdue" || r.DueDate < DateTime.UtcNow)
                    .OrderBy(r => r.DueDate)
                    .Take(maxCount)
                    .ToList();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[FollowUpController.GetFollowUpsDueToday] Error: {ex.Message}");
                return new List<TaskReminder>();
            }
        }

        public void Dispose() => _db.Dispose();
    }

    public sealed record FollowUpKpiCounts(
        int Total,
        int Overdue,
        int Today,
        int Upcoming,
        int Completed
    );
}
