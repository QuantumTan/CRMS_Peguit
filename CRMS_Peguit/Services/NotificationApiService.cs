using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using CRMS_Peguit.domain.entities;

namespace CRMS_Peguit.winforms.Services
{
    public class NotificationApiService : BaseApiService
    {
        public NotificationApiService(string? baseUrl = null, HttpClient? httpClient = null)
            : base(baseUrl, httpClient)
        {
        }

        public List<Notification> GetMyNotifications(int userId, int take = 50) =>
            Get<List<Notification>>($"api/notifications?userId={userId}&take={take}") ?? new();

        public int GetUnreadCount(int userId) =>
            Get<int>($"api/notifications/unread-count?userId={userId}");

        public bool MarkAsRead(int notificationId) =>
            Post($"api/notifications/{notificationId}/read", new { });

        public int MarkAllAsRead(int userId)
        {
            var res = Post<object, dynamic>($"api/notifications/read-all?userId={userId}", new { });
            return 1;
        }

        public Dictionary<NotificationType, bool> GetPreferences(int userId) =>
            Get<Dictionary<NotificationType, bool>>($"api/notifications/preferences?userId={userId}") ?? new();

        public void UpdatePreferences(int userId, Dictionary<NotificationType, bool> preferences) =>
            Put($"api/notifications/preferences?userId={userId}", preferences);

        public bool IsTypeEnabled(int userId, NotificationType type) =>
            Get<bool>($"api/notifications/preferences/type-enabled?userId={userId}&type={type}");

        public Notification? CreateNotification(
            int recipientUserId,
            NotificationType type,
            string title,
            string message,
            string? targetEntity = null,
            int? targetEntityId = null)
        {
            return Post<object, Notification>("api/notifications", new
            {
                recipientUserId,
                type,
                title,
                message,
                targetEntity,
                targetEntityId
            });
        }

        public Notification? CreateNotification(
            int tenantId,
            int recipientUserId,
            NotificationType type,
            string title,
            string message,
            string? targetEntity = null,
            int? targetEntityId = null) =>
            CreateNotification(recipientUserId, type, title, message, targetEntity, targetEntityId);

        public void NotifyManagers(
            NotificationType type,
            string title,
            string message,
            string? targetEntity = null,
            int? targetEntityId = null)
        {
            Post("api/notifications/notify-managers", new
            {
                type,
                title,
                message,
                targetEntity,
                targetEntityId
            });
        }

        public void NotifyAdmins(
            NotificationType type,
            string title,
            string message,
            string? targetEntity = null,
            int? targetEntityId = null)
        {
            Post("api/notifications/notify-admins", new
            {
                type,
                title,
                message,
                targetEntity,
                targetEntityId
            });
        }

        public int PruneOldNotifications(int userId, int daysOld = 90) =>
            Delete($"api/notifications/prune?userId={userId}&daysOld={daysOld}") ? 1 : 0;

        public void CheckFollowUpReminders(int userId) =>
            Post($"api/notifications/check-reminders?userId={userId}", new { });

        public void CheckSubscriptionAlerts(int tenantId) =>
            Post($"api/notifications/check-subscriptions?tenantId={tenantId}", new { });

        public void CheckBackupAlerts(int tenantId) =>
            Post($"api/notifications/check-backups?tenantId={tenantId}", new { });
    }
}
