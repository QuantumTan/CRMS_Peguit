using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CRMS_Peguit.domain.Common;
using CRMS_Peguit.domain.entities;
using CRMS_Peguit.infrastructure.data;
using CRMS_Peguit.winforms.Auth;
using CRMS_Peguit.winforms.Models.Services;
using CRMS_Peguit.winforms.Services;
using CRMS_Peguit.winforms.Services.Offline;
using Microsoft.EntityFrameworkCore;

namespace CRMS_Peguit.winforms.Controllers
{
    public class SupportTicketController : IDisposable
    {
        private readonly RealEstateDbContext _db;

        private int TenantId => CurrentSession.TenantId;
        private readonly NotificationController _notifCtrl;

        public SupportTicketController()
        {
            _db = LocalDb.CreateContext(TenantId);
            _notifCtrl = new NotificationController(_db);
        }

        // =========================================================================
        // QUERY: GetAll() with Ownership-based Access Control (RBAC + Row-Level Security)
        // =========================================================================
        public List<SupportTicket> GetAll()
        {
            try
            {
                using var db = LocalDb.CreateContext(TenantId);
                var query = db.SupportTickets
                    .Include(t => t.Customer)
                    .Include(t => t.RaisedByUser)
                    .Include(t => t.AssignedToUser)
                    .AsNoTracking();

                // Row-level ownership check:
                // Manager & Admin have full oversight across tenant.
                // Agent sees ONLY their own tickets:
                // - While Unassigned, visible ONLY to the Agent who logged it.
                // - Once assigned, visible ONLY to the assigned Agent.
                if (!RbacService.HasFullOversight && RbacService.IsAgent)
                {
                    int currentUserId = CurrentSession.UserId;
                    query = query.Where(t =>
                        (t.AssignedToUserId.HasValue && t.AssignedToUserId.Value > 0)
                            ? t.AssignedToUserId.Value == currentUserId
                            : t.RaisedByUserId == currentUserId);
                }

                if (CurrentSession.CanAccessBranching && CurrentSession.ActiveBranchId.HasValue)
                {
                    int bId = CurrentSession.ActiveBranchId.Value;
                    query = query.Where(t =>
                        (t.AssignedToUser != null && t.AssignedToUser.BranchId == bId) ||
                        (t.RaisedByUser != null && t.RaisedByUser.BranchId == bId) ||
                        (t.Customer != null && t.Customer.AssignedAgent != null && t.Customer.AssignedAgent.BranchId == bId));
                }

                return query
                    .OrderByDescending(t => t.CreatedAt)
                    .ToList();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[SupportTicketController.GetAll] Error: {ex.Message}");
                return new List<SupportTicket>();
            }
        }

        public async Task<PagedResult<SupportTicket>> GetPagedAsync(
            int pageNumber = 1,
            int pageSize = 25,
            string? search = null,
            string? filterStatus = null)
        {
            try
            {
                using var db = LocalDb.CreateContext(TenantId);
                var query = db.SupportTickets
                    .Include(t => t.Customer)
                    .Include(t => t.Customer).ThenInclude(c => c.AssignedAgent)
                    .Include(t => t.RaisedByUser)
                    .Include(t => t.AssignedToUser)
                    .AsNoTracking()
                    .Where(t => !t.IsDeleted);

                if (!RbacService.HasFullOversight && RbacService.IsAgent)
                {
                    int currentUserId = CurrentSession.UserId;
                    query = query.Where(t =>
                        (t.AssignedToUserId.HasValue && t.AssignedToUserId.Value > 0)
                            ? t.AssignedToUserId.Value == currentUserId
                            : t.RaisedByUserId == currentUserId);
                }

                if (CurrentSession.CanAccessBranching && CurrentSession.ActiveBranchId.HasValue)
                {
                    int bId = CurrentSession.ActiveBranchId.Value;
                    query = query.Where(t =>
                        (t.AssignedToUser != null && t.AssignedToUser.BranchId == bId) ||
                        (t.RaisedByUser != null && t.RaisedByUser.BranchId == bId) ||
                        (t.Customer != null && t.Customer.AssignedAgent != null && t.Customer.AssignedAgent.BranchId == bId));
                }

                if (!string.IsNullOrWhiteSpace(filterStatus) && !string.Equals(filterStatus, "All", StringComparison.OrdinalIgnoreCase))
                {
                    if (string.Equals(filterStatus, "Open", StringComparison.OrdinalIgnoreCase))
                    {
                        query = query.Where(t => t.Status.ToLower() == "open");
                    }
                    else if (string.Equals(filterStatus, "In Progress", StringComparison.OrdinalIgnoreCase))
                    {
                        query = query.Where(t => t.Status.ToLower() == "in progress");
                    }
                    else if (string.Equals(filterStatus, "Resolved", StringComparison.OrdinalIgnoreCase))
                    {
                        query = query.Where(t => t.Status.ToLower() == "resolved");
                    }
                    else if (string.Equals(filterStatus, "Overdue", StringComparison.OrdinalIgnoreCase))
                    {
                        var now = DateTime.UtcNow;
                        query = query.Where(t => t.Status.ToLower() != "resolved" && t.DueDate.HasValue && t.DueDate.Value < now);
                    }
                }

                if (!string.IsNullOrWhiteSpace(search))
                {
                    string s = search.Trim();
                    query = query.Where(t =>
                        t.TicketNumber.Contains(s) ||
                        (t.Description != null && t.Description.Contains(s)) ||
                        t.Category.Contains(s) ||
                        t.Status.Contains(s) ||
                        (t.Customer != null && (t.Customer.FirstName.Contains(s) || t.Customer.LastName.Contains(s))) ||
                        (t.AssignedToUser != null && (t.AssignedToUser.FirstName.Contains(s) || t.AssignedToUser.LastName.Contains(s))));
                }

                int totalCount = await query.CountAsync();

                int validPage = Math.Max(1, pageNumber);
                int validPageSize = Math.Max(1, pageSize);

                var items = await query
                    .OrderByDescending(t => t.CreatedAt)
                    .Skip((validPage - 1) * validPageSize)
                    .Take(validPageSize)
                    .ToListAsync();

                return new PagedResult<SupportTicket>(items, totalCount, validPage, validPageSize);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[SupportTicketController.GetPagedAsync] Error: {ex.Message}");
                return new PagedResult<SupportTicket>(new List<SupportTicket>(), 0, pageNumber, pageSize);
            }
        }

        public SupportTicket? GetById(int id)
        {
            var item = _db.SupportTickets
                .Include(t => t.Customer)
                .Include(t => t.RaisedByUser)
                .Include(t => t.AssignedToUser)
                .Include(t => t.Comments).ThenInclude(c => c.AuthorUser)
                .AsNoTracking()
                .SingleOrDefault(t => t.TicketId == id);

            if (item is null) return null;

            // Row-level security: ensure Agent can only view their own ticket
            if (!CanUserViewTicket(item))
                return null;

            return item;
        }

        public bool CanUserViewTicket(SupportTicket ticket)
        {
            if (RbacService.HasFullOversight)
                return true;

            if (!RbacService.IsAgent)
                return false;

            int currentUserId = CurrentSession.UserId;
            if (currentUserId <= 0) return false;

            if (ticket.AssignedToUserId.HasValue && ticket.AssignedToUserId.Value > 0)
            {
                return ticket.AssignedToUserId.Value == currentUserId;
            }

            return ticket.RaisedByUserId == currentUserId;
        }

        public bool CanUserEditTicket(SupportTicket ticket)
        {
            // Admin is read-only oversight: cannot edit ticket details or resolve
            if (RbacService.IsAdmin)
                return false;

            // Manager has full CRUD
            if (RbacService.IsManager || RbacService.IsSuperAdmin)
                return true;

            // Agent can update status on tickets they own
            return CanUserViewTicket(ticket);
        }

        // =========================================================================
        // COMMAND: Add Ticket
        // Starts Unassigned/Pending, visible only to creator Agent until assigned.
        // =========================================================================
        public SupportTicket Add(SupportTicket ticket)
        {
            if (ticket.CustomerId <= 0)
                throw new ArgumentException("A valid customer must be selected for the ticket.", nameof(ticket.CustomerId));

            if (string.IsNullOrWhiteSpace(ticket.Category))
                ticket.Category = "Other";

            if (string.IsNullOrWhiteSpace(ticket.Priority))
                ticket.Priority = "Medium";

            ticket.Status = "Open";
            ticket.CreatedAt = DateTime.UtcNow;
            ticket.RaisedByUserId = CurrentSession.UserId > 0 ? CurrentSession.UserId : 1;

            // R23: Default state is Unassigned / Pending
            // Only Manager or Admin can assign or reassign.
            ticket.AssignedToUserId = null;

            ticket.IsDeleted = false;
            ticket.DeletedAt = null;
            ticket.FirstRespondedAt = null;
            ticket.ResolvedAt = null;

            // SLA calculation:
            ticket.DueDate = CalculateSlaDueDate(ticket.Priority, ticket.CreatedAt);

            // Temporary ticket number; will refine with TicketId after identity generation if needed
            ticket.TicketNumber = "TCK-TEMP";

            _db.SupportTickets.Add(ticket);
            _db.SaveChanges();

            // Set formatted permanent human-readable reference: TCK-00042
            ticket.TicketNumber = $"TCK-{ticket.TicketId:D5}";
            _db.SaveChanges();

            SyncService.Instance.EnqueueOfflineCreate("SupportTicket", ticket, TenantId, CurrentSession.UserId, ticket.TicketId.ToString());
            if (SyncService.Instance.IsOnline && !CurrentSession.IsOffline)
            {
                _ = Task.Run(() => SyncService.Instance.SyncAsync());
            }

            // Add creation audit entry in comment thread
            string authorName = CurrentSession.CurrentUser?.FullName ?? $"User #{ticket.RaisedByUserId}";
            var createComment = new TicketComment
            {
                TicketId = ticket.TicketId,
                AuthorUserId = ticket.RaisedByUserId,
                CommentText = $"Ticket logged with {ticket.Priority} priority ({ticket.Category}) by {authorName}.",
                CommentType = "StatusChange",
                IsInternal = true,
                CreatedAt = DateTime.UtcNow
            };
            _db.TicketComments.Add(createComment);
            _db.SaveChanges();

            // Manager receives: A new Support Ticket is logged by an Agent (needs oversight/potential reassignment)
            _notifCtrl.NotifyManagers(
                TenantId,
                NotificationType.TicketCreated,
                "New Support Ticket Logged",
                $"Support Ticket '{ticket.TicketNumber}' was logged by {authorName} and requires attention.",
                "SupportTicket",
                ticket.TicketId);

            return ticket;
        }

        // =========================================================================
        // COMMAND: Update Status (Open -> In Progress -> Resolved)
        // A Resolved ticket cannot be moved back through normal flow (requires Reopen).
        // =========================================================================
        public void UpdateStatus(int ticketId, string newStatus, string? note = null)
        {
            var ticket = _db.SupportTickets.SingleOrDefault(t => t.TicketId == ticketId);
            if (ticket is null)
                throw new KeyNotFoundException($"Support ticket #{ticketId} not found.");

            // Permission check:
            if (RbacService.IsAdmin)
                throw new UnauthorizedAccessException("Admin has read-only oversight and cannot change ticket status.");

            if (!CanUserEditTicket(ticket))
                throw new UnauthorizedAccessException("You are not authorized to update this support ticket.");

            string currentStatus = ticket.Status.Trim();
            string targetStatus = newStatus.Trim();

            if (string.Equals(currentStatus, targetStatus, StringComparison.OrdinalIgnoreCase))
                return;

            // Business Rule 1: A Resolved ticket cannot be moved back to In Progress or Open through normal update flow
            if (string.Equals(currentStatus, "Resolved", StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException("A Resolved ticket cannot be updated back to Open or In Progress through standard status updates. Use the Reopen action.");
            }

            var allowedTransitions = new Dictionary<string, string[]>(StringComparer.OrdinalIgnoreCase)
            {
                { "Open", new[] { "In Progress", "Resolved" } },
                { "In Progress", new[] { "Resolved" } }
            };

            if (allowedTransitions.TryGetValue(currentStatus, out var validTargets))
            {
                if (!validTargets.Any(t => string.Equals(t, targetStatus, StringComparison.OrdinalIgnoreCase)))
                {
                    throw new InvalidOperationException($"Invalid status transition from '{currentStatus}' to '{targetStatus}'.");
                }
            }

            ticket.Status = targetStatus;

            // When moving to In Progress, capture FirstRespondedAt if not yet set
            if (string.Equals(targetStatus, "In Progress", StringComparison.OrdinalIgnoreCase) && !ticket.FirstRespondedAt.HasValue)
            {
                ticket.FirstRespondedAt = DateTime.UtcNow;
            }

            // When moving to Resolved, set ResolvedAt
            if (string.Equals(targetStatus, "Resolved", StringComparison.OrdinalIgnoreCase))
            {
                ticket.ResolvedAt = DateTime.UtcNow;
            }

            int actorUserId = CurrentSession.UserId > 0 ? CurrentSession.UserId : 1;
            string actorName = CurrentSession.CurrentUser?.FullName ?? $"User #{actorUserId}";
            string noteSuffix = string.IsNullOrWhiteSpace(note) ? string.Empty : $": {note.Trim()}";

            var logEntry = new TicketComment
            {
                TicketId = ticket.TicketId,
                AuthorUserId = actorUserId,
                CommentText = $"Status changed from '{currentStatus}' to '{targetStatus}' by {actorName}{noteSuffix}",
                CommentType = "StatusChange",
                IsInternal = true,
                CreatedAt = DateTime.UtcNow
            };
            _db.TicketComments.Add(logEntry);

            _db.SaveChanges();

            SyncService.Instance.EnqueueOfflineUpdate("SupportTicket", ticket.TicketId, ticket, TenantId, actorUserId, DateTime.UtcNow);
            if (SyncService.Instance.IsOnline && !CurrentSession.IsOffline)
            {
                _ = Task.Run(() => SyncService.Instance.SyncAsync());
            }

            if (ticket.AssignedToUserId.HasValue && ticket.AssignedToUserId.Value > 0 && ticket.AssignedToUserId.Value != actorUserId)
            {
                _notifCtrl.CreateNotification(
                    TenantId,
                    ticket.AssignedToUserId.Value,
                    NotificationType.TicketStatusChanged,
                    "Ticket Status Updated",
                    $"Support Ticket '{ticket.TicketNumber}' status changed to '{targetStatus}'.",
                    "SupportTicket",
                    ticket.TicketId);
            }
            if (ticket.RaisedByUserId != actorUserId)
            {
                _notifCtrl.CreateNotification(
                    TenantId,
                    ticket.RaisedByUserId,
                    NotificationType.TicketStatusChanged,
                    "Ticket Status Updated",
                    $"Support Ticket '{ticket.TicketNumber}' status changed to '{targetStatus}'.",
                    "SupportTicket",
                    ticket.TicketId);
            }
        }

        // =========================================================================
        // COMMAND: Explicit Reopen action
        // =========================================================================
        public void Reopen(int ticketId, string reason)
        {
            var ticket = _db.SupportTickets.SingleOrDefault(t => t.TicketId == ticketId);
            if (ticket is null)
                throw new KeyNotFoundException($"Support ticket #{ticketId} not found.");

            if (RbacService.IsAdmin)
                throw new UnauthorizedAccessException("Admin has read-only oversight and cannot reopen tickets.");

            if (!CanUserEditTicket(ticket))
                throw new UnauthorizedAccessException("You are not authorized to reopen this support ticket.");

            if (!string.Equals(ticket.Status, "Resolved", StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException("Only Resolved tickets can be reopened.");

            ticket.Status = "Open";
            ticket.ResolvedAt = null;

            int actorUserId = CurrentSession.UserId > 0 ? CurrentSession.UserId : 1;
            string actorName = CurrentSession.CurrentUser?.FullName ?? $"User #{actorUserId}";

            var logEntry = new TicketComment
            {
                TicketId = ticket.TicketId,
                AuthorUserId = actorUserId,
                CommentText = $"Ticket reopened by {actorName}. Reason: {reason.Trim()}",
                CommentType = "Reopened",
                IsInternal = true,
                CreatedAt = DateTime.UtcNow
            };
            _db.TicketComments.Add(logEntry);

            _db.SaveChanges();

            SyncService.Instance.EnqueueOfflineUpdate("SupportTicket", ticket.TicketId, ticket, TenantId, actorUserId, DateTime.UtcNow);
            if (SyncService.Instance.IsOnline && !CurrentSession.IsOffline)
            {
                _ = Task.Run(() => SyncService.Instance.SyncAsync());
            }
        }

        // =========================================================================
        // COMMAND: AssignTo() (Manager/Admin ONLY, blocks/throws for Agent)
        // Reassignment does NOT rewrite history. Every assignment is logged.
        // =========================================================================
        public void AssignTo(int ticketId, int? newAgentId, string? notes = null)
        {
            // Business Rule 2: Only Manager or Admin can assign or reassign a ticket to an Agent
            if (!RbacService.CanAssignRecords)
            {
                throw new UnauthorizedAccessException("Agents are not permitted to assign or reassign support tickets. Only Managers and Admins may assign tickets.");
            }

            var ticket = _db.SupportTickets.SingleOrDefault(t => t.TicketId == ticketId);
            if (ticket is null)
                throw new KeyNotFoundException($"Support ticket #{ticketId} not found.");

            int? previousAgentId = ticket.AssignedToUserId;
            int? validNewAgentId = (newAgentId.HasValue && newAgentId.Value > 0) ? newAgentId.Value : null;

            if (previousAgentId == validNewAgentId)
                return;

            if (validNewAgentId.HasValue && !_db.Users.Any(u => u.UserId == validNewAgentId.Value))
            {
                throw new ArgumentException($"Agent ID #{validNewAgentId.Value} does not exist in the system.");
            }

            ticket.AssignedToUserId = validNewAgentId;

            // Accountability audit logging: who, from whom, to whom, when
            int actorUserId = CurrentSession.UserId > 0 ? CurrentSession.UserId : 1;
            string actorName = CurrentSession.CurrentUser?.FullName ?? $"User #{actorUserId}";
            string fromName = GetAssignedAgentName(previousAgentId) ?? "Unassigned";
            string toName = GetAssignedAgentName(validNewAgentId) ?? "Unassigned";
            string notesPart = string.IsNullOrWhiteSpace(notes) ? string.Empty : $" Note: {notes.Trim()}";

            var logEntry = new TicketComment
            {
                TicketId = ticket.TicketId,
                AuthorUserId = actorUserId,
                CommentText = $"Ticket reassigned from {fromName} to {toName} by {actorName}.{notesPart}",
                CommentType = "Assignment",
                IsInternal = true,
                CreatedAt = DateTime.UtcNow
            };
            _db.TicketComments.Add(logEntry);

            _db.SaveChanges();

            SyncService.Instance.EnqueueOfflineUpdate("SupportTicket", ticket.TicketId, ticket, TenantId, actorUserId, DateTime.UtcNow);
            if (SyncService.Instance.IsOnline && !CurrentSession.IsOffline)
            {
                _ = Task.Run(() => SyncService.Instance.SyncAsync());
            }

            // Agent receives: A Support Ticket is assigned or reassigned to them
            if (validNewAgentId.HasValue && validNewAgentId.Value > 0)
            {
                _notifCtrl.CreateNotification(
                    TenantId,
                    validNewAgentId.Value,
                    NotificationType.TicketAssigned,
                    "Support Ticket Assigned to You",
                    $"Support Ticket '{ticket.TicketNumber}' has been assigned to you by {actorName}.",
                    "SupportTicket",
                    ticket.TicketId);
            }
        }

        // =========================================================================
        // COMMENTS / REPLY THREAD
        // =========================================================================
        public List<TicketComment> GetComments(int ticketId)
        {
            return _db.TicketComments
                .Include(c => c.AuthorUser).ThenInclude(u => u.Role)
                .Where(c => c.TicketId == ticketId)
                .OrderBy(c => c.CreatedAt)
                .ToList();
        }

        public TicketComment AddComment(int ticketId, string commentText, bool isInternal = true)
        {
            if (string.IsNullOrWhiteSpace(commentText))
                throw new ArgumentException("Comment text cannot be empty.", nameof(commentText));

            var ticket = _db.SupportTickets.SingleOrDefault(t => t.TicketId == ticketId);
            if (ticket is null)
                throw new KeyNotFoundException($"Support ticket #{ticketId} not found.");

            if (!CanUserViewTicket(ticket))
                throw new UnauthorizedAccessException("You are not authorized to view or comment on this ticket.");

            // Setting FirstRespondedAt when someone replies if not set
            if (!ticket.FirstRespondedAt.HasValue)
            {
                ticket.FirstRespondedAt = DateTime.UtcNow;
            }

            int actorUserId = CurrentSession.UserId > 0 ? CurrentSession.UserId : 1;

            var comment = new TicketComment
            {
                TicketId = ticketId,
                AuthorUserId = actorUserId,
                CommentText = commentText.Trim(),
                CommentType = "Comment",
                IsInternal = isInternal,
                CreatedAt = DateTime.UtcNow
            };

            _db.TicketComments.Add(comment);
            _db.SaveChanges();

            return comment;
        }

        // =========================================================================
        // KPI COUNTS - computed directly in SQL to avoid loading all tickets into memory
        // =========================================================================
        public async Task<SupportTicketKpiCounts> GetKpiCountsAsync()
        {
            try
            {
                using var db = LocalDb.CreateContext(TenantId);
                var query = db.SupportTickets.AsNoTracking().Where(t => !t.IsDeleted);

                if (!RbacService.HasFullOversight && RbacService.IsAgent)
                {
                    int currentUserId = CurrentSession.UserId;
                    query = query.Where(t =>
                        (t.AssignedToUserId.HasValue && t.AssignedToUserId.Value > 0)
                            ? t.AssignedToUserId.Value == currentUserId
                            : t.RaisedByUserId == currentUserId);
                }

                var now = DateTime.UtcNow;
                int total = await query.CountAsync();
                int open = await query.CountAsync(t => t.Status.ToLower() == "open");
                int inProgress = await query.CountAsync(t => t.Status.ToLower() == "in progress");
                int resolved = await query.CountAsync(t => t.Status.ToLower() == "resolved");
                int overdue = await query.CountAsync(t => t.Status.ToLower() != "resolved" && t.DueDate.HasValue && t.DueDate.Value < now);

                return new SupportTicketKpiCounts
                {
                    Total = total,
                    Open = open,
                    InProgress = inProgress,
                    Resolved = resolved,
                    Overdue = overdue
                };
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[SupportTicketController.GetKpiCountsAsync] Error: {ex.Message}");
                return new SupportTicketKpiCounts();
            }
        }

        public SupportTicketKpiCounts GetKpiCounts()
        {
            try
            {
                return GetKpiCountsAsync().GetAwaiter().GetResult();
            }
            catch
            {
                return new SupportTicketKpiCounts();
            }
        }

        // =========================================================================
        // SOFT DELETE (Archive)
        // =========================================================================
        public void SoftDelete(int ticketId)
        {
            if (!RbacService.IsManager && !RbacService.IsSuperAdmin)
                throw new UnauthorizedAccessException("Only Managers can archive support tickets.");

            var ticket = _db.SupportTickets.SingleOrDefault(t => t.TicketId == ticketId);
            if (ticket is null) return;

            ticket.IsDeleted = true;
            ticket.DeletedAt = DateTime.UtcNow;

            int actorUserId = CurrentSession.UserId > 0 ? CurrentSession.UserId : 1;
            string actorName = CurrentSession.CurrentUser?.FullName ?? $"User #{actorUserId}";

            var logEntry = new TicketComment
            {
                TicketId = ticket.TicketId,
                AuthorUserId = actorUserId,
                CommentText = $"Ticket archived by {actorName}.",
                CommentType = "StatusChange",
                IsInternal = true,
                CreatedAt = DateTime.UtcNow
            };
            _db.TicketComments.Add(logEntry);

            _db.SaveChanges();

            SyncService.Instance.EnqueueOfflineDelete("SupportTicket", ticket.TicketId, TenantId, actorUserId);
            if (SyncService.Instance.IsOnline && !CurrentSession.IsOffline)
            {
                _ = Task.Run(() => SyncService.Instance.SyncAsync());
            }
        }

        // =========================================================================
        // HELPERS & LOOKUPS
        // =========================================================================
        public static DateTime CalculateSlaDueDate(string priority, DateTime referenceTime)
        {
            return (priority?.Trim().ToLowerInvariant()) switch
            {
                "high" or "urgent" => referenceTime.AddHours(24),
                "medium" => referenceTime.AddDays(3),
                "low" => referenceTime.AddDays(7),
                _ => referenceTime.AddDays(3)
            };
        }

        private Dictionary<int, string>? _cachedAgentDict;

        public Dictionary<int, string> GetAgentDictionary()
        {
            if (_cachedAgentDict != null) return _cachedAgentDict;
            try
            {
                using var db = LocalDb.CreateContext(TenantId);
                _cachedAgentDict = db.Users
                    .AsNoTracking()
                    .ToDictionary(u => u.UserId, u => u.FullName);
            }
            catch
            {
                _cachedAgentDict = new Dictionary<int, string>();
            }
            return _cachedAgentDict;
        }

        public string? GetAssignedAgentName(int? assignedAgentId)
        {
            if (!assignedAgentId.HasValue || assignedAgentId.Value <= 0) return null;
            var dict = GetAgentDictionary();
            return dict.TryGetValue(assignedAgentId.Value, out var name) ? name : null;
        }

        public List<Customer> GetCustomers()
        {
            return _db.Customers
                .AsNoTracking()
                .OrderBy(c => c.LastName)
                .ThenBy(c => c.FirstName)
                .ToList();
        }

        public List<AgentPickerItem> GetAgents()
        {
            var agentRoleIds = _db.Roles
                .AsNoTracking()
                .Where(r => r.RoleName.ToLower() == "agent" || r.RoleName.ToLower() == "sales staff")
                .Select(r => r.RoleId)
                .ToList();

            return _db.Users
                .AsNoTracking()
                .Where(u => agentRoleIds.Contains(u.RoleId) && u.Status.ToLower() != "inactive")
                .OrderBy(u => u.LastName)
                .ThenBy(u => u.FirstName)
                .AsEnumerable()
                .Select(u => new AgentPickerItem(u.UserId, u.FullName, u.Email))
                .ToList();
        }

        public int GetOpenTicketsCount()
        {
            try
            {
                var query = _db.SupportTickets.AsNoTracking().Where(t => !t.IsDeleted && t.Status != "Resolved" && t.Status != "Closed");
                if (!RbacService.HasFullOversight && RbacService.IsAgent)
                {
                    int currentUserId = CurrentSession.UserId;
                    query = query.Where(t =>
                        (t.AssignedToUserId.HasValue && t.AssignedToUserId.Value > 0)
                            ? t.AssignedToUserId.Value == currentUserId
                            : t.RaisedByUserId == currentUserId);
                }

                return query.Count();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[SupportTicketController.GetOpenTicketsCount] Error: {ex.Message}");
                return 0;
            }
        }

        public List<CRMS_Peguit.winforms.Models.ViewModels.AdminTicketAttentionItemDto> GetTicketsNeedingAttention(int maxCount = 3)
        {
            try
            {
                var rawTickets = _db.SupportTickets
                    .AsNoTracking()
                    .Include(t => t.Customer)
                    .Where(t => !t.IsDeleted && t.Status != "Resolved" && t.Status != "Closed")
                    .OrderBy(t => t.CreatedAt)
                    .Take(maxCount)
                    .ToList();

                var now = DateTime.UtcNow;
                return rawTickets.Select(t =>
                {
                    int daysAgo = Math.Max(0, (int)(now - t.CreatedAt).TotalDays);
                    string openedAgo = daysAgo == 0 ? "opened today" : (daysAgo == 1 ? "opened 1 day ago" : $"opened {daysAgo} days ago");

                    return new CRMS_Peguit.winforms.Models.ViewModels.AdminTicketAttentionItemDto
                    {
                        TicketId = t.TicketId,
                        TicketNumber = t.TicketNumber,
                        CustomerName = t.Customer?.FullName ?? "Client",
                        Category = t.Category,
                        Priority = t.Priority,
                        Status = t.Status,
                        OpenedAgoText = openedAgo
                    };
                }).ToList();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[SupportTicketController.GetTicketsNeedingAttention] Error: {ex.Message}");
                return new List<CRMS_Peguit.winforms.Models.ViewModels.AdminTicketAttentionItemDto>();
            }
        }

        public void Dispose()
        {
            _db.Dispose();
        }
    }

    public class SupportTicketKpiCounts
    {
        public int Total { get; set; }
        public int Open { get; set; }
        public int InProgress { get; set; }
        public int Resolved { get; set; }
        public int Overdue { get; set; }
    }
}
