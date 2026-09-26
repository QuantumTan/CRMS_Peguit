using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using CRMS_Peguit.domain.entities;
using CRMS_Peguit.infrastructure.data;

namespace CRMS_Peguit.api.Controllers
{
    public record CreateNotificationDto(
        int RecipientUserId,
        NotificationType Type,
        string Title,
        string Message,
        string? TargetEntity = null,
        int? TargetEntityId = null);

    public record BroadcastNotificationDto(
        NotificationType Type,
        string Title,
        string Message,
        string? TargetEntity = null,
        int? TargetEntityId = null);

    [ApiController]
    [Route("api/[controller]")]
    public class NotificationsController : ControllerBase
    {
        private readonly RealEstateDbContext _db;

        public NotificationsController(RealEstateDbContext db)
        {
            _db = db;
        }

        private (int UserId, string Role, int TenantId) CurrentUser =>
            ApiSecurityHelper.GetCurrentUserInfo(HttpContext);

        [HttpGet("my")]
        public async Task<IActionResult> GetMy([FromQuery] int? userId = null, [FromQuery] int take = 50)
        {
            int resolvedUserId = (userId.HasValue && userId.Value > 0) ? userId.Value : CurrentUser.UserId;
            if (resolvedUserId <= 0) return BadRequest("A valid userId is required.");

            var items = await _db.Notifications
                .AsNoTracking()
                .Where(n => n.RecipientUserId == resolvedUserId)
                .OrderByDescending(n => n.CreatedAt)
                .Take(take)
                .ToListAsync();

            return Ok(items);
        }

        [HttpGet("unread-count")]
        public async Task<IActionResult> GetUnreadCount([FromQuery] int? userId = null)
        {
            int resolvedUserId = (userId.HasValue && userId.Value > 0) ? userId.Value : CurrentUser.UserId;
            if (resolvedUserId <= 0) return BadRequest("A valid userId is required.");

            var count = await _db.Notifications
                .AsNoTracking()
                .CountAsync(n => n.RecipientUserId == resolvedUserId && !n.IsRead);

            return Ok(new { unreadCount = count });
        }

        [HttpPut("{id:int}/read")]
        public async Task<IActionResult> MarkAsRead(int id)
        {
            var item = await _db.Notifications.SingleOrDefaultAsync(n => n.NotificationId == id);
            if (item == null) return NotFound();

            if (!item.IsRead)
            {
                item.IsRead = true;
                item.ReadAt = DateTime.UtcNow;
                await _db.SaveChangesAsync();
            }

            return Ok(new { success = true });
        }

        [HttpPut("read-all")]
        public async Task<IActionResult> MarkAllAsRead([FromQuery] int? userId = null)
        {
            int resolvedUserId = (userId.HasValue && userId.Value > 0) ? userId.Value : CurrentUser.UserId;
            if (resolvedUserId <= 0) return BadRequest("A valid userId is required.");

            var unread = await _db.Notifications
                .Where(n => n.RecipientUserId == resolvedUserId && !n.IsRead)
                .ToListAsync();

            var now = DateTime.UtcNow;
            foreach (var item in unread)
            {
                item.IsRead = true;
                item.ReadAt = now;
            }

            await _db.SaveChangesAsync();
            return Ok(new { markedCount = unread.Count });
        }

        [HttpGet("preferences")]
        public async Task<IActionResult> GetPreferences([FromQuery] int? userId = null)
        {
            int resolvedUserId = (userId.HasValue && userId.Value > 0) ? userId.Value : CurrentUser.UserId;
            if (resolvedUserId <= 0) return BadRequest("A valid userId is required.");

            var rows = await _db.NotificationPreferences
                .AsNoTracking()
                .Where(p => p.UserId == resolvedUserId)
                .ToListAsync();

            var dict = new Dictionary<string, bool>();
            foreach (NotificationType t in Enum.GetValues<NotificationType>())
            {
                var match = rows.FirstOrDefault(r => r.Type == t);
                dict[t.ToString()] = match?.IsEnabled ?? true;
            }

            return Ok(dict);
        }

        [HttpPut("preferences")]
        public async Task<IActionResult> UpdatePreferences([FromBody] Dictionary<string, bool> preferences, [FromQuery] int? userId = null)
        {
            int resolvedUserId = (userId.HasValue && userId.Value > 0) ? userId.Value : CurrentUser.UserId;
            if (resolvedUserId <= 0) return BadRequest("A valid userId is required.");

            var existing = await _db.NotificationPreferences
                .Where(p => p.UserId == resolvedUserId)
                .ToListAsync();

            foreach (var kvp in preferences)
            {
                if (Enum.TryParse<NotificationType>(kvp.Key, out var typeEnum))
                {
                    var match = existing.FirstOrDefault(e => e.Type == typeEnum);
                    if (match != null)
                    {
                        match.IsEnabled = kvp.Value;
                    }
                    else
                    {
                        _db.NotificationPreferences.Add(new NotificationPreference
                        {
                            UserId = resolvedUserId,
                            Type = typeEnum,
                            IsEnabled = kvp.Value
                        });
                    }
                }
            }

            await _db.SaveChangesAsync();
            return Ok(new { success = true });
        }

        [HttpPost]
        public async Task<IActionResult> CreateNotification([FromBody] CreateNotificationDto dto)
        {
            // Debounce check: duplicate within 2 minutes
            var cutoff = DateTime.UtcNow.AddMinutes(-2);
            bool duplicate = await _db.Notifications.AnyAsync(n =>
                n.RecipientUserId == dto.RecipientUserId &&
                n.Type == dto.Type &&
                n.Title == dto.Title &&
                n.CreatedAt > cutoff);

            if (duplicate) return Ok(new { message = "Debounced duplicate", created = false });

            // Preference check
            var pref = await _db.NotificationPreferences
                .AsNoTracking()
                .FirstOrDefaultAsync(p => p.UserId == dto.RecipientUserId && p.Type == dto.Type);
            if (pref != null && !pref.IsEnabled)
                return Ok(new { message = "Suppressed by preference", created = false });

            var notif = new Notification
            {
                RecipientUserId = dto.RecipientUserId,
                Type = dto.Type,
                Title = dto.Title,
                Message = dto.Message,
                RelatedEntityType = dto.TargetEntity,
                RelatedEntityId = dto.TargetEntityId,
                IsRead = false,
                CreatedAt = DateTime.UtcNow
            };

            _db.Notifications.Add(notif);
            await _db.SaveChangesAsync();
            return Ok(notif);
        }

        [HttpPost("notify-managers")]
        public async Task<IActionResult> NotifyManagers([FromBody] BroadcastNotificationDto dto)
        {
            var managerUsers = await _db.Users
                .Include(u => u.Role)
                .Where(u => u.Role != null && (u.Role.RoleName.ToLower() == "manager" || u.Role.RoleName.ToLower() == "admin") && u.Status.ToLower() != "inactive")
                .ToListAsync();

            int sent = 0;
            foreach (var m in managerUsers)
            {
                var n = new Notification
                {
                    RecipientUserId = m.UserId,
                    Type = dto.Type,
                    Title = dto.Title,
                    Message = dto.Message,
                    RelatedEntityType = dto.TargetEntity,
                    RelatedEntityId = dto.TargetEntityId,
                    IsRead = false,
                    CreatedAt = DateTime.UtcNow
                };
                _db.Notifications.Add(n);
                sent++;
            }

            await _db.SaveChangesAsync();
            return Ok(new { sentCount = sent });
        }

        [HttpPost("notify-admins")]
        public async Task<IActionResult> NotifyAdmins([FromBody] BroadcastNotificationDto dto)
        {
            var adminUsers = await _db.Users
                .Include(u => u.Role)
                .Where(u => u.Role != null && u.Role.RoleName.ToLower() == "admin" && u.Status.ToLower() != "inactive")
                .ToListAsync();

            int sent = 0;
            foreach (var a in adminUsers)
            {
                var n = new Notification
                {
                    RecipientUserId = a.UserId,
                    Type = dto.Type,
                    Title = dto.Title,
                    Message = dto.Message,
                    RelatedEntityType = dto.TargetEntity,
                    RelatedEntityId = dto.TargetEntityId,
                    IsRead = false,
                    CreatedAt = DateTime.UtcNow
                };
                _db.Notifications.Add(n);
                sent++;
            }

            await _db.SaveChangesAsync();
            return Ok(new { sentCount = sent });
        }

        [HttpPost("prune")]
        public async Task<IActionResult> PruneOld([FromQuery] int? userId = null, [FromQuery] int daysOld = 90)
        {
            int resolvedUserId = (userId.HasValue && userId.Value > 0) ? userId.Value : CurrentUser.UserId;
            if (resolvedUserId <= 0) return BadRequest("A valid userId is required.");

            var cutoff = DateTime.UtcNow.AddDays(-daysOld);
            var oldNotifications = await _db.Notifications
                .Where(n => n.RecipientUserId == resolvedUserId && n.IsRead && n.CreatedAt < cutoff)
                .ToListAsync();

            _db.Notifications.RemoveRange(oldNotifications);
            await _db.SaveChangesAsync();

            return Ok(new { deletedCount = oldNotifications.Count });
        }
    }
}
