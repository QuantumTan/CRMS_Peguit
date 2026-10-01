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
        private int TenantId => CurrentSession.TenantId > 0 ? CurrentSession.TenantId : 1;

        public FollowUpController()
        {
            _db = LocalDb.CreateContext(TenantId);
        }

        /// <summary>
        /// Retrieves active (non-deleted) follow-ups.
        /// Frontline agents see only their own assigned follow-ups.
        /// Managers and Admins have full oversight to view all follow-ups across the tenant.
        /// Also automatically checks and updates overdue status for pending follow-ups whose due date has passed.
        /// </summary>
        public List<TaskReminder> GetAll()
        {
            try
            {
                using var db = LocalDb.CreateContext(TenantId);
                int currentUserId = CurrentSession.UserId;
                if (currentUserId <= 0) return new List<TaskReminder>();

                // First, find any pending reminders whose due date has passed and update to Overdue
                var now = DateTime.UtcNow;
                var pendingOverdueQuery = db.TaskReminders
                    .Where(r => !r.IsDeleted
                                && r.Status == "Pending"
                                && r.DueDate < now);

                if (!RbacService.HasFullOversight)
                {
                    pendingOverdueQuery = pendingOverdueQuery.Where(r => r.AssignedToUserId == currentUserId);
                }

                var pendingOverdue = pendingOverdueQuery.ToList();

                if (pendingOverdue.Count > 0)
                {
                    foreach (var item in pendingOverdue)
                    {
                        item.Status = "Overdue";
                        item.UpdatedAt = now;
                    }
                    db.SaveChanges();
                }

                // Query non-deleted follow-ups with related entities included
                var query = db.TaskReminders
                    .AsNoTracking()
                    .Include(r => r.RelatedCustomer)
                    .Include(r => r.RelatedLead)
                    .Where(r => !r.IsDeleted);

                if (!RbacService.HasFullOversight)
                {
                    query = query.Where(r => r.AssignedToUserId == currentUserId);
                }

                var list = query
                    .OrderBy(r => r.DueDate)
                    .ToList();

                if (list.Count > 0)
                {
                    _ = Task.Run(() => LocalDataCache.Instance.SaveTaskRemindersMirror(TenantId, list));
                }

                return list;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[FollowUpController.GetAll] Error: {ex.Message}");
                return LocalDataCache.Instance.GetCachedTaskReminders(TenantId, CurrentSession.UserId, RbacService.IsAgent);
            }
        }

        public CRMS_Peguit.domain.Common.PagedResult<TaskReminder> GetPaged(string status = "All", string? search = null, string priority = "All", int pageNumber = 1, int pageSize = 25)
        {
            var list = GetAll();

            if (!string.IsNullOrWhiteSpace(status) && !status.Equals("All", StringComparison.OrdinalIgnoreCase))
            {
                var todayLocal = DateTime.Today;
                var tomorrowLocal = todayLocal.AddDays(1);
                var now = DateTime.UtcNow;

                if (status.Equals("Overdue", StringComparison.OrdinalIgnoreCase))
                {
                    list = list.Where(r => r.Status != "Completed" && (r.Status == "Overdue" || r.DueDate < now)).ToList();
                }
                else if (status.Equals("Today", StringComparison.OrdinalIgnoreCase))
                {
                    list = list.Where(r => r.Status != "Completed" && r.DueDate.ToLocalTime().Date == todayLocal).ToList();
                }
                else if (status.Equals("Upcoming", StringComparison.OrdinalIgnoreCase))
                {
                    list = list.Where(r => r.Status != "Completed" && r.DueDate.ToLocalTime().Date >= tomorrowLocal).ToList();
                }
                else if (status.Equals("Completed", StringComparison.OrdinalIgnoreCase))
                {
                    list = list.Where(r => r.Status == "Completed").ToList();
                }
            }

            if (!string.IsNullOrWhiteSpace(priority) && !priority.Equals("All", StringComparison.OrdinalIgnoreCase))
            {
                list = list.Where(r => string.Equals(r.Priority, priority, StringComparison.OrdinalIgnoreCase)).ToList();
            }

            if (!string.IsNullOrWhiteSpace(search))
            {
                string q = search.Trim();
                list = list.Where(r =>
                    r.Title.Contains(q, StringComparison.OrdinalIgnoreCase) ||
                    (r.Notes != null && r.Notes.Contains(q, StringComparison.OrdinalIgnoreCase)) ||
                    (r.RelatedCustomer != null && r.RelatedCustomer.FullName.Contains(q, StringComparison.OrdinalIgnoreCase)) ||
                    (r.RelatedLead != null && r.RelatedLead.FullName.Contains(q, StringComparison.OrdinalIgnoreCase))
                ).ToList();
            }

            int total = list.Count;
            int page = Math.Max(1, pageNumber);
            int size = Math.Max(1, pageSize);
            var pagedItems = list.Skip((page - 1) * size).Take(size).ToList();
            return new CRMS_Peguit.domain.Common.PagedResult<TaskReminder>(pagedItems, total, page, size);
        }

        public TaskReminder? GetById(int id)
        {
            try
            {
                int currentUserId = CurrentSession.UserId;
                if (currentUserId <= 0) return null;

                var query = _db.TaskReminders
                    .AsNoTracking()
                    .Include(r => r.RelatedCustomer)
                    .Include(r => r.RelatedLead)
                    .Where(r => r.TaskReminderId == id && !r.IsDeleted);

                if (!RbacService.HasFullOversight)
                {
                    query = query.Where(r => r.AssignedToUserId == currentUserId);
                }

                return query.SingleOrDefault();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[FollowUpController.GetById] Error: {ex.Message}");
                return LocalDataCache.Instance.GetCachedTaskReminders(TenantId, CurrentSession.UserId, RbacService.IsAgent)
                    .FirstOrDefault(r => r.TaskReminderId == id);
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

            _db.TaskReminders.Add(reminder);
            _db.SaveChanges();
            try { LocalDataCache.Instance.SaveTaskRemindersMirror(TenantId, new[] { reminder }); } catch { }

            SyncService.Instance.EnqueueOfflineCreate("TaskReminder", reminder, TenantId, currentUserId, reminder.TaskReminderId.ToString());
            if (SyncService.Instance.IsOnline && !CurrentSession.IsOffline)
            {
                _ = Task.Run(() => SyncService.Instance.SyncAsync());
            }

            return reminder;
        }

        public void Update(TaskReminder reminder)
        {
            ValidateRelationshipExclusivity(reminder);

            int currentUserId = CurrentSession.UserId;

            var item = _db.TaskReminders
                .SingleOrDefault(r => r.TaskReminderId == reminder.TaskReminderId && (RbacService.HasFullOversight || r.AssignedToUserId == currentUserId) && !r.IsDeleted);

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
            try { LocalDataCache.Instance.SaveTaskRemindersMirror(TenantId, new[] { item }); } catch { }

            SyncService.Instance.EnqueueOfflineUpdate("TaskReminder", item.TaskReminderId, item, TenantId, currentUserId, item.UpdatedAt ?? item.CreatedAt);
            if (SyncService.Instance.IsOnline && !CurrentSession.IsOffline)
            {
                _ = Task.Run(() => SyncService.Instance.SyncAsync());
            }
        }

        /// <summary>
        /// Completes a Follow-Up and optionally logs a resulting Activity in the client timeline.
        /// </summary>
        public void MarkComplete(int id, bool logActivity = false, string? activityNotes = null)
        {
            int currentUserId = CurrentSession.UserId;
            var item = _db.TaskReminders
                .SingleOrDefault(r => r.TaskReminderId == id && (RbacService.HasFullOversight || r.AssignedToUserId == currentUserId) && !r.IsDeleted);

            if (item is null) return;
            if (string.Equals(item.Status, "Completed", StringComparison.OrdinalIgnoreCase)) return;

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
            try { LocalDataCache.Instance.SaveTaskRemindersMirror(TenantId, new[] { item }); } catch { }

            SyncService.Instance.EnqueueOfflineUpdate("TaskReminder", item.TaskReminderId, item, TenantId, currentUserId, item.UpdatedAt ?? item.CompletedAt ?? DateTime.UtcNow);
            if (SyncService.Instance.IsOnline && !CurrentSession.IsOffline)
            {
                _ = Task.Run(() => SyncService.Instance.SyncAsync());
            }
        }

        /// <summary>
        /// Snoozes a Follow-Up by pushing the DueDate forward by the specified interval
        /// and resetting status back to Pending.
        /// </summary>
        public void Snooze(int id, TimeSpan interval)
        {
            int currentUserId = CurrentSession.UserId;
            var item = _db.TaskReminders
                .SingleOrDefault(r => r.TaskReminderId == id && (RbacService.HasFullOversight || r.AssignedToUserId == currentUserId) && !r.IsDeleted);

            if (item is null) return;
            if (string.Equals(item.Status, "Completed", StringComparison.OrdinalIgnoreCase)) return;

            item.Snooze(interval);
            _db.SaveChanges();
            try { LocalDataCache.Instance.SaveTaskRemindersMirror(TenantId, new[] { item }); } catch { }

            SyncService.Instance.EnqueueOfflineUpdate("TaskReminder", item.TaskReminderId, item, TenantId, currentUserId, item.UpdatedAt ?? DateTime.UtcNow);
            if (SyncService.Instance.IsOnline && !CurrentSession.IsOffline)
            {
                _ = Task.Run(() => SyncService.Instance.SyncAsync());
            }
        }

        /// <summary>
        /// Reschedules a Follow-Up to a new date/time and resets status back to Pending.
        /// </summary>
        public void Reschedule(int id, DateTime newDueDate)
        {
            int currentUserId = CurrentSession.UserId;
            var item = _db.TaskReminders
                .SingleOrDefault(r => r.TaskReminderId == id && (RbacService.HasFullOversight || r.AssignedToUserId == currentUserId) && !r.IsDeleted);

            if (item is null) return;
            if (string.Equals(item.Status, "Completed", StringComparison.OrdinalIgnoreCase)) return;

            item.Reschedule(newDueDate);
            _db.SaveChanges();
            try { LocalDataCache.Instance.SaveTaskRemindersMirror(TenantId, new[] { item }); } catch { }

            SyncService.Instance.EnqueueOfflineUpdate("TaskReminder", item.TaskReminderId, item, TenantId, currentUserId, item.UpdatedAt ?? DateTime.UtcNow);
            if (SyncService.Instance.IsOnline && !CurrentSession.IsOffline)
            {
                _ = Task.Run(() => SyncService.Instance.SyncAsync());
            }
        }

        /// <summary>
        /// Soft deletes a Follow-Up. Hard deletes are strictly forbidden.
        /// </summary>
        public void SoftDelete(int id)
        {
            int currentUserId = CurrentSession.UserId;
            var item = _db.TaskReminders
                .SingleOrDefault(r => r.TaskReminderId == id && (RbacService.HasFullOversight || r.AssignedToUserId == currentUserId) && !r.IsDeleted);

            if (item is null) return;

            item.IsDeleted = true;
            item.DeletedAt = DateTime.UtcNow;
            _db.SaveChanges();
            try { LocalDataCache.Instance.SaveTaskRemindersMirror(TenantId, new[] { item }); } catch { }

            SyncService.Instance.EnqueueOfflineDelete("TaskReminder", item.TaskReminderId, TenantId, currentUserId);
            if (SyncService.Instance.IsOnline && !CurrentSession.IsOffline)
            {
                _ = Task.Run(() => SyncService.Instance.SyncAsync());
            }
        }

        public List<TaskReminder> GetArchived()
        {
            try
            {
                return _db.TaskReminders
                    .IgnoreQueryFilters()
                    .Include(t => t.AssignedToUser)
                        .ThenInclude(u => u.Role)
                    .AsNoTracking()
                    .Where(t => t.AssignedToUser != null && t.AssignedToUser.Role != null && t.AssignedToUser.Role.TenantId == TenantId && t.IsDeleted)
                    .OrderByDescending(t => t.DeletedAt ?? t.CreatedAt)
                    .ToList();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[FollowUpController.GetArchived] Error: {ex.Message}");
                return new List<TaskReminder>();
            }
        }

        public void Restore(int taskReminderId)
        {
            var item = _db.TaskReminders.IgnoreQueryFilters().SingleOrDefault(r => r.TaskReminderId == taskReminderId);
            if (item is null) return;

            item.IsDeleted = false;
            item.DeletedAt = null;
            _db.SaveChanges();

            try { LocalDataCache.Instance.SaveTaskRemindersMirror(TenantId, new[] { item }); } catch { }

            int currentUserId = CurrentSession.UserId > 0 ? CurrentSession.UserId : 1;
            SyncService.Instance.EnqueueOfflineUpdate("TaskReminder", item.TaskReminderId, item, TenantId, currentUserId, DateTime.UtcNow);
            if (SyncService.Instance.IsOnline && !CurrentSession.IsOffline)
            {
                _ = Task.Run(() => SyncService.Instance.SyncAsync());
            }
        }

        public void HardDelete(int taskReminderId)
        {
            var item = _db.TaskReminders.IgnoreQueryFilters().SingleOrDefault(r => r.TaskReminderId == taskReminderId);
            if (item is null) return;

            _db.TaskReminders.Remove(item);
            _db.SaveChanges();

            int currentUserId = CurrentSession.UserId > 0 ? CurrentSession.UserId : 1;
            SyncService.Instance.EnqueueOfflineDelete("TaskReminder", taskReminderId, TenantId, currentUserId);
            if (SyncService.Instance.IsOnline && !CurrentSession.IsOffline)
            {
                _ = Task.Run(() => SyncService.Instance.SyncAsync());
            }
        }

        /// <summary>
        /// Returns customers assigned to the currently authenticated Agent.
        /// Managers and Admins see all tenant customers.
        /// </summary>
        public List<Customer> GetAssignedCustomers(int? includeCustomerId = null)
        {
            int currentUserId = CurrentSession.UserId;
            if (currentUserId <= 0) return new List<Customer>();

            var query = _db.Customers
                .AsNoTracking()
                .Where(c => !c.IsDeleted || (includeCustomerId.HasValue && c.CustomerId == includeCustomerId.Value));

            if (!RbacService.HasFullOversight)
            {
                query = query.Where(c => c.AssignedAgentId == currentUserId || c.CreatedByUserId == currentUserId || (includeCustomerId.HasValue && c.CustomerId == includeCustomerId.Value));
            }

            return query
                .OrderBy(c => c.LastName)
                .ThenBy(c => c.FirstName)
                .ToList();
        }

        /// <summary>
        /// Returns leads assigned to the currently authenticated Agent.
        /// Managers and Admins see all tenant leads.
        /// </summary>
        public List<Lead> GetAssignedLeads(int? includeLeadId = null)
        {
            int currentUserId = CurrentSession.UserId;
            if (currentUserId <= 0) return new List<Lead>();

            var query = _db.Leads
                .AsNoTracking()
                .Where(l => !l.IsDeleted && (l.Stage != "lost" || (includeLeadId.HasValue && l.LeadId == includeLeadId.Value)));

            if (!RbacService.HasFullOversight)
            {
                query = query.Where(l => l.AssignedAgentId == currentUserId || l.CreatedByUserId == currentUserId || (includeLeadId.HasValue && l.LeadId == includeLeadId.Value));
            }

            return query
                .OrderBy(l => l.LastName)
                .ThenBy(l => l.FirstName)
                .ToList();
        }

        /// <summary>
        /// Calculates KPI counts for Overdue, Due Today, Upcoming, and Completed for the logged-in Agent or Tenant.
        /// </summary>
        public FollowUpKpiCounts GetKpiCounts()
        {
            int currentUserId = CurrentSession.UserId;
            if (currentUserId <= 0) return new FollowUpKpiCounts(0, 0, 0, 0, 0);

            var now = DateTime.UtcNow;
            var todayLocal = DateTime.Today;
            var tomorrowLocal = todayLocal.AddDays(1);

            var query = _db.TaskReminders
                .AsNoTracking()
                .Where(r => !r.IsDeleted);

            if (!RbacService.HasFullOversight)
            {
                query = query.Where(r => r.AssignedToUserId == currentUserId);
            }

            var items = query.ToList();

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
                    .Include(r => r.RelatedCustomer)
                    .Include(r => r.RelatedLead)
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
