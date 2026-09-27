using System;
using System.Collections.Generic;
using System.Linq;
using CRMS_Peguit.domain.entities;
using CRMS_Peguit.infrastructure.data;
using CRMS_Peguit.winforms.Auth;
using CRMS_Peguit.winforms.Models.Services;
using Microsoft.EntityFrameworkCore;

namespace CRMS_Peguit.winforms.Controllers
{
    public class NotificationController : IDisposable
    {
        private readonly RealEstateDbContext _db;
        private int TenantId => CurrentSession.TenantId;

        public NotificationController()
        {
            _db = LocalDb.CreateContext(TenantId);
        }

        public NotificationController(RealEstateDbContext db)
        {
            _db = db;
        }

        public void Dispose()
        {
            _db?.Dispose();
        }

        /// <summary>
        /// Retrieves notifications strictly for the specified user in the active tenant.
        /// Cross-user viewing is strictly forbidden.
        /// </summary>
        public List<Notification> GetMyNotifications(int userId, int take = 50)
        {
            if (userId <= 0) return new List<Notification>();

            try
            {
                return _db.Notifications
                    .AsNoTracking()
                    .Where(n => n.RecipientUserId == userId)
                    .OrderByDescending(n => n.CreatedAt)
                    .Take(take)
                    .ToList();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[NotificationController.GetMyNotifications] Error: {ex.Message}");
                return new List<Notification>();
            }
        }

        /// <summary>
        /// Returns the count of unread notifications for the specified user.
        /// </summary>
        public int GetUnreadCount(int userId)
        {
            if (userId <= 0) return 0;

            try
            {
                return _db.Notifications
                    .AsNoTracking()
                    .Count(n => n.RecipientUserId == userId && !n.IsRead);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[NotificationController.GetUnreadCount] Error: {ex.Message}");
                return 0;
            }
        }

        /// <summary>
        /// Marks an individual notification as read.
        /// </summary>
        public bool MarkAsRead(int notificationId)
        {
            try
            {
                var item = _db.Notifications.SingleOrDefault(n => n.NotificationId == notificationId);
                if (item == null) return false;

                if (!item.IsRead)
                {
                    item.IsRead = true;
                    item.ReadAt = DateTime.UtcNow;
                    _db.SaveChanges();
                }
                return true;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[NotificationController.MarkAsRead] Error: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Marks all unread notifications for a user as read.
        /// </summary>
        public int MarkAllAsRead(int userId)
        {
            if (userId <= 0) return 0;

            try
            {
                var unread = _db.Notifications
                    .Where(n => n.RecipientUserId == userId && !n.IsRead)
                    .ToList();

                if (unread.Count == 0) return 0;

                var now = DateTime.UtcNow;
                foreach (var item in unread)
                {
                    item.IsRead = true;
                    item.ReadAt = now;
                }
                _db.SaveChanges();
                return unread.Count;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[NotificationController.MarkAllAsRead] Error: {ex.Message}");
                return 0;
            }
        }

        /// <summary>
        /// Retrieves the per-user preferences dictionary. Defaults to true if no preference row exists.
        /// </summary>
        public Dictionary<NotificationType, bool> GetPreferences(int userId)
        {
            var dict = new Dictionary<NotificationType, bool>();
            foreach (NotificationType t in Enum.GetValues<NotificationType>())
            {
                dict[t] = true;
            }

            if (userId <= 0) return dict;

            try
            {
                var rows = _db.NotificationPreferences
                    .AsNoTracking()
                    .Where(p => p.UserId == userId)
                    .ToList();

                foreach (var row in rows)
                {
                    dict[row.Type] = row.IsEnabled;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[NotificationController.GetPreferences] Error: {ex.Message}");
            }

            return dict;
        }

        /// <summary>
        /// Updates the user's notification preferences.
        /// </summary>
        public void UpdatePreferences(int userId, Dictionary<NotificationType, bool> preferences)
        {
            if (userId <= 0 || preferences == null) return;

            try
            {
                var existing = _db.NotificationPreferences
                    .Where(p => p.UserId == userId)
                    .ToList();

                foreach (var kvp in preferences)
                {
                    var match = existing.FirstOrDefault(e => e.Type == kvp.Key);
                    if (match != null)
                    {
                        match.IsEnabled = kvp.Value;
                    }
                    else
                    {
                        _db.NotificationPreferences.Add(new NotificationPreference
                        {
                            TenantId = TenantId,
                            UserId = userId,
                            Type = kvp.Key,
                            IsEnabled = kvp.Value
                        });
                    }
                }
                _db.SaveChanges();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[NotificationController.UpdatePreferences] Error: {ex.Message}");
            }
        }

        public bool IsTypeEnabled(int userId, NotificationType type)
        {
            if (userId <= 0) return true;

            try
            {
                var pref = _db.NotificationPreferences
                    .AsNoTracking()
                    .FirstOrDefault(p => p.UserId == userId && p.Type == type);

                return pref == null || pref.IsEnabled;
            }
            catch
            {
                return true;
            }
        }

        /// <summary>
        /// Internal method called across controllers to create a notification.
        /// Respects per-user preferences and debounces duplicates within a 2-minute window.
        /// </summary>
        public Notification? CreateNotification(
            int tenantId,
            int recipientUserId,
            NotificationType type,
            string title,
            string message,
            string? relatedEntityType = null,
            int? relatedEntityId = null)
        {
            if (recipientUserId <= 0) return null;

            try
            {
                // 1. Preference check
                if (!IsTypeEnabled(recipientUserId, type))
                {
                    return null; // Suppressed by user preference
                }

                // 2. Debounce check: prevent duplicate notifications within 2 minutes for same entity & recipient
                var debounceWindow = DateTime.UtcNow.AddMinutes(-2);
                bool exists = _db.Notifications
                    .Any(n => n.RecipientUserId == recipientUserId
                              && n.Type == type
                              && n.RelatedEntityType == relatedEntityType
                              && n.RelatedEntityId == relatedEntityId
                              && n.CreatedAt >= debounceWindow);

                if (exists)
                {
                    return null; // Duplicate prevented
                }

                // 3. Persist notification
                var item = new Notification
                {
                    TenantId = tenantId > 0 ? tenantId : 1,
                    RecipientUserId = recipientUserId,
                    Type = type,
                    Title = title,
                    Message = message,
                    RelatedEntityType = relatedEntityType,
                    RelatedEntityId = relatedEntityId,
                    IsRead = false,
                    CreatedAt = DateTime.UtcNow
                };

                _db.Notifications.Add(item);
                _db.SaveChanges();
                return item;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[NotificationController.CreateNotification] Error: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Helper to notify all active Managers under the specified tenant.
        /// </summary>
        public void NotifyManagers(
            int tenantId,
            NotificationType type,
            string title,
            string message,
            string? relatedEntityType = null,
            int? relatedEntityId = null)
        {
            try
            {
                var managerIds = _db.Users
                    .AsNoTracking()
                    .Where(u => (u.Role.RoleName == "Manager" || u.Role.RoleName == "Admin") && u.Status == "active")
                    .Select(u => u.UserId)
                    .Distinct()
                    .ToList();

                foreach (var mgrId in managerIds)
                {
                    CreateNotification(tenantId, mgrId, type, title, message, relatedEntityType, relatedEntityId);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[NotificationController.NotifyManagers] Error: {ex.Message}");
            }
        }

        /// <summary>
        /// Helper to notify all active Admins and Super Admins.
        /// </summary>
        public void NotifyAdmins(
            int tenantId,
            NotificationType type,
            string title,
            string message,
            string? relatedEntityType = null,
            int? relatedEntityId = null)
        {
            try
            {
                var adminIds = _db.Users
                    .AsNoTracking()
                    .Where(u => (u.Role.RoleName == "Admin" || u.Role.RoleName == "SuperAdmin" || u.Role.RoleName == "Super Admin") && u.Status == "active")
                    .Select(u => u.UserId)
                    .Distinct()
                    .ToList();

                foreach (var adminId in adminIds)
                {
                    CreateNotification(tenantId, adminId, type, title, message, relatedEntityType, relatedEntityId);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[NotificationController.NotifyAdmins] Error: {ex.Message}");
            }
        }

        /// <summary>
        /// Prunes read notifications older than the given number of days (default 90).
        /// </summary>
        public int PruneOldNotifications(int userId, int daysOld = 90)
        {
            if (userId <= 0) return 0;

            try
            {
                var cutoff = DateTime.UtcNow.AddDays(-daysOld);
                var oldNotifications = _db.Notifications
                    .Where(n => n.RecipientUserId == userId && n.IsRead && n.CreatedAt < cutoff)
                    .ToList();

                if (oldNotifications.Count == 0) return 0;

                _db.Notifications.RemoveRange(oldNotifications);
                _db.SaveChanges();
                return oldNotifications.Count;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[NotificationController.PruneOldNotifications] Error: {ex.Message}");
                return 0;
            }
        }

        /// <summary>
        /// Checks follow-up reminders owned by the current user:
        /// - Due within 1 hour: FollowUpDueSoon
        /// - Due in past or Overdue: FollowUpOverdue
        /// Automatically debounced so agents aren't spammed repeatedly.
        /// </summary>
        public void CheckFollowUpReminders(int userId)
        {
            if (userId <= 0) return;

            try
            {
                var now = DateTime.UtcNow;
                var oneHourFromNow = now.AddHours(1);

                var activeFollowUps = _db.TaskReminders
                    .AsNoTracking()
                    .Where(r => r.AssignedToUserId == userId
                                && !r.IsDeleted
                                && r.Status != "Completed"
                                && r.Status != "Cancelled")
                    .ToList();

                foreach (var item in activeFollowUps)
                {
                    if (item.DueDate < now || item.Status == "Overdue")
                    {
                        CreateNotification(
                            TenantId,
                            userId,
                            NotificationType.FollowUpOverdue,
                            "Follow-Up Overdue",
                            $"Follow-up '{item.Title}' is overdue (due {item.DueDate.ToLocalTime():g}).",
                            "FollowUp",
                            item.TaskReminderId);
                    }
                    else if (item.DueDate <= oneHourFromNow)
                    {
                        var remaining = item.DueDate - now;
                        int mins = Math.Max(1, (int)remaining.TotalMinutes);
                        CreateNotification(
                            TenantId,
                            userId,
                            NotificationType.FollowUpDueSoon,
                            "Follow-Up Due Soon",
                            $"Follow-up '{item.Title}' is due in {mins} minute{(mins == 1 ? "" : "s")}.",
                            "FollowUp",
                            item.TaskReminderId);
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[NotificationController.CheckFollowUpReminders] Error: {ex.Message}");
            }
        }

        /// <summary>
        /// Checks subscription expiry status for Admin / Super Admin oversight.
        /// </summary>
        public void CheckSubscriptionAlerts(int tenantId)
        {
            try
            {
                // Check system setting or subscription record
                var expirySetting = _db.SystemSettings
                    .AsNoTracking()
                    .FirstOrDefault(s => s.SettingKey == "SubscriptionExpiry");

                if (expirySetting != null && DateTime.TryParse(expirySetting.SettingValue, out var expiryDate))
                {
                    var now = DateTime.UtcNow;
                    if (expiryDate <= now)
                    {
                        NotifyAdmins(
                            tenantId,
                            NotificationType.SubscriptionExpired,
                            "Subscription Expired",
                            $"Your NEXA CRM tenant license expired on {expiryDate.ToLocalTime():d}. Contact support to renew.",
                            "Subscription",
                            null);
                    }
                    else if (expiryDate <= now.AddDays(7))
                    {
                        int daysLeft = Math.Max(1, (int)(expiryDate - now).TotalDays);
                        NotifyAdmins(
                            tenantId,
                            NotificationType.SubscriptionExpiring,
                            "Subscription Expiring Soon",
                            $"Your NEXA CRM subscription expires in {daysLeft} day{(daysLeft == 1 ? "" : "s")} ({expiryDate.ToLocalTime():d}).",
                            "Subscription",
                            null);
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[NotificationController.CheckSubscriptionAlerts] Error: {ex.Message}");
            }
        }

        /// <summary>
        /// Checks recent BackupLogs: if any backup failed, notifies Admin and Super Admin.
        /// Never notifies on success.
        /// </summary>
        public void CheckBackupAlerts(int tenantId)
        {
            try
            {
                var recentFailedBackups = _db.BackupLogs
                    .AsNoTracking()
                    .Where(b => b.Status == "Failed" && b.BackupDate >= DateTime.UtcNow.AddDays(-1))
                    .OrderByDescending(b => b.BackupDate)
                    .Take(5)
                    .ToList();

                foreach (var b in recentFailedBackups)
                {
                    NotifyAdmins(
                        tenantId,
                        NotificationType.BackupFailed,
                        "Backup Failed",
                        $"Database backup attempt on {b.BackupDate.ToLocalTime():g} failed.",
                        "BackupLog",
                        b.BackupId);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[NotificationController.CheckBackupAlerts] Error: {ex.Message}");
            }
        }
    }
}
