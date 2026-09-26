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
    public class LeadController : IDisposable
    {
        private readonly RealEstateDbContext _db;
        private readonly NotificationController _notifCtrl;

        // FIXED: was hardcoded to 1 - now uses whoever is actually logged in.
        private int TenantId => CurrentSession.TenantId;

        public LeadController()
        {
            _db = LocalDb.CreateContext(TenantId);
            _notifCtrl = new NotificationController(_db);
        }

        public List<Lead> GetAll()
        {
            if (CurrentSession.IsOffline || !SyncService.Instance.IsOnline)
            {
                var cached = LocalDataCache.Instance.GetCachedLeads(TenantId, CurrentSession.UserId, RbacService.IsAgent);
                if (cached.Count > 0) return cached;
            }

            try
            {
                var query = _db.Leads.AsNoTracking();

                // R23 & R25 (revised): Visibility scoped to creator while Pending, assignee once assigned.
                // Manager/Admin retain full oversight (R26).
                if (!RbacService.HasFullOversight && RbacService.IsAgent)
                {
                    int currentUserId = CurrentSession.UserId;
                    query = query.Where(l =>
                        (l.AssignedAgentId.HasValue && l.AssignedAgentId.Value > 0)
                            ? l.AssignedAgentId.Value == currentUserId
                            : l.CreatedByUserId == currentUserId);
                }

                var list = query
                    .OrderBy(x => x.Person.LastName)
                    .ThenBy(x => x.Person.FirstName)
                    .ToList();

                if (list.Count > 0)
                {
                    _ = Task.Run(() => LocalDataCache.Instance.SaveLeadsMirror(TenantId, list));
                }

                return list;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[LeadController.GetAll] Error: {ex.Message}");
                return new List<Lead>();
            }
        }

        public async Task<PagedResult<Lead>> GetPagedAsync(
            int pageNumber = 1,
            int pageSize = 25,
            string? search = null,
            string? stage = null,
            string? sortColumn = null,
            bool isAscending = true)
        {
            if (CurrentSession.IsOffline || !SyncService.Instance.IsOnline)
            {
                var cached = LocalDataCache.Instance.GetCachedLeads(TenantId, CurrentSession.UserId, RbacService.IsAgent);
                if (cached.Count == 0)
                {
                    try
                    {
                        var localQuery = _db.Leads.Include(l => l.Person).AsNoTracking().Where(l => !l.IsDeleted);
                        if (!RbacService.HasFullOversight && RbacService.IsAgent)
                        {
                            int currentUserId = CurrentSession.UserId;
                            localQuery = localQuery.Where(l =>
                                (l.AssignedAgentId.HasValue && l.AssignedAgentId.Value > 0)
                                    ? l.AssignedAgentId.Value == currentUserId
                                    : l.CreatedByUserId == currentUserId);
                        }
                        var localList = await localQuery.ToListAsync();
                        if (localList.Count > 0)
                        {
                            cached = localList;
                            _ = Task.Run(() => LocalDataCache.Instance.SaveLeadsMirror(TenantId, localList));
                        }
                    }
                    catch { }
                }

                if (cached.Count > 0)
                {
                    if (!string.IsNullOrWhiteSpace(stage) && !string.Equals(stage, "All", StringComparison.OrdinalIgnoreCase))
                    {
                        cached = cached.Where(l => l.Stage.Equals(stage, StringComparison.OrdinalIgnoreCase)).ToList();
                    }
                    if (!string.IsNullOrWhiteSpace(search))
                    {
                        string s = search.Trim().ToLowerInvariant();
                        cached = cached.Where(l => (l.Person != null && (
                            l.Person.FirstName.ToLowerInvariant().Contains(s) ||
                            l.Person.LastName.ToLowerInvariant().Contains(s) ||
                            (l.Person.Email != null && l.Person.Email.ToLowerInvariant().Contains(s)) ||
                            (l.Person.Phone != null && l.Person.Phone.Contains(s)))) ||
                            (l.Source != null && l.Source.ToLowerInvariant().Contains(s))).ToList();
                    }
                    int total = cached.Count;
                    var paged = cached.Skip((pageNumber - 1) * pageSize).Take(pageSize).ToList();
                    return new PagedResult<Lead>(paged, total, pageNumber, pageSize);
                }
            }

            try
            {
                using var db = LocalDb.CreateContext(TenantId);
                var query = db.Leads
                    .Include(l => l.Person)
                    .AsNoTracking()
                    .Where(l => !l.IsDeleted);

                if (!RbacService.HasFullOversight && RbacService.IsAgent)
                {
                    int currentUserId = CurrentSession.UserId;
                    query = query.Where(l =>
                        (l.AssignedAgentId.HasValue && l.AssignedAgentId.Value > 0)
                            ? l.AssignedAgentId.Value == currentUserId
                            : l.CreatedByUserId == currentUserId);
                }

                if (!string.IsNullOrWhiteSpace(stage) && !string.Equals(stage, "All", StringComparison.OrdinalIgnoreCase))
                {
                    string s = stage.Trim().ToLower();
                    query = query.Where(l => l.Stage.ToLower() == s);
                }

                if (!string.IsNullOrWhiteSpace(search))
                {
                    string s = search.Trim();
                    query = query.Where(l =>
                        l.Person.FirstName.Contains(s) ||
                        (l.Person.MiddleName != null && l.Person.MiddleName.Contains(s)) ||
                        l.Person.LastName.Contains(s) ||
                        (l.Person.Suffix != null && l.Person.Suffix.Contains(s)) ||
                        (l.Person.Email != null && l.Person.Email.Contains(s)) ||
                        (l.Person.Phone != null && l.Person.Phone.Contains(s)));
                }

                int totalCount = await query.CountAsync();

                // Sort
                if (string.Equals(sortColumn, "Name", StringComparison.OrdinalIgnoreCase))
                {
                    query = isAscending
                        ? query.OrderBy(x => x.Person.LastName).ThenBy(x => x.Person.FirstName)
                        : query.OrderByDescending(x => x.Person.LastName).ThenByDescending(x => x.Person.FirstName);
                }
                else if (string.Equals(sortColumn, "Stage", StringComparison.OrdinalIgnoreCase))
                {
                    query = isAscending ? query.OrderBy(x => x.Stage) : query.OrderByDescending(x => x.Stage);
                }
                else if (string.Equals(sortColumn, "Source", StringComparison.OrdinalIgnoreCase))
                {
                    query = isAscending ? query.OrderBy(x => x.Source) : query.OrderByDescending(x => x.Source);
                }
                else if (string.Equals(sortColumn, "ExpectedValue", StringComparison.OrdinalIgnoreCase))
                {
                    query = isAscending ? query.OrderBy(x => x.ExpectedValue) : query.OrderByDescending(x => x.ExpectedValue);
                }
                else if (string.Equals(sortColumn, "Priority", StringComparison.OrdinalIgnoreCase))
                {
                    query = isAscending ? query.OrderBy(x => x.Priority) : query.OrderByDescending(x => x.Priority);
                }
                else
                {
                    query = isAscending ? query.OrderBy(x => x.CreatedAt) : query.OrderByDescending(x => x.CreatedAt);
                }

                int validPage = Math.Max(1, pageNumber);
                int validPageSize = Math.Max(1, pageSize);

                var items = await query
                    .Skip((validPage - 1) * validPageSize)
                    .Take(validPageSize)
                    .ToListAsync();

                return new PagedResult<Lead>(items, totalCount, validPage, validPageSize);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[LeadController.GetPagedAsync] Error: {ex.Message}");
                return new PagedResult<Lead>(new List<Lead>(), 0, pageNumber, pageSize);
            }
        }

        public async Task<LeadStageCounts> GetStageCountsAsync()
        {
            try
            {
                using var db = LocalDb.CreateContext(TenantId);
                var query = db.Leads.AsNoTracking().Where(l => !l.IsDeleted);

                if (!RbacService.HasFullOversight && RbacService.IsAgent)
                {
                    int currentUserId = CurrentSession.UserId;
                    query = query.Where(l =>
                        (l.AssignedAgentId.HasValue && l.AssignedAgentId.Value > 0)
                            ? l.AssignedAgentId.Value == currentUserId
                            : l.CreatedByUserId == currentUserId);
                }

                var groups = await query
                    .GroupBy(l => l.Stage)
                    .Select(g => new { Stage = g.Key, Count = g.Count() })
                    .ToListAsync();

                int total = groups.Sum(g => g.Count);
                int countNew = groups.FirstOrDefault(g => string.Equals(g.Stage, "new", StringComparison.OrdinalIgnoreCase))?.Count ?? 0;
                int countContacted = groups.FirstOrDefault(g => string.Equals(g.Stage, "contacted", StringComparison.OrdinalIgnoreCase))?.Count ?? 0;
                int countQualified = groups.FirstOrDefault(g => string.Equals(g.Stage, "qualified", StringComparison.OrdinalIgnoreCase))?.Count ?? 0;
                int countConverted = groups.FirstOrDefault(g => string.Equals(g.Stage, "converted", StringComparison.OrdinalIgnoreCase))?.Count ?? 0;

                return new LeadStageCounts
                {
                    Total = total,
                    New = countNew,
                    Contacted = countContacted,
                    Qualified = countQualified,
                    Converted = countConverted
                };
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[LeadController.GetStageCountsAsync] Error: {ex.Message}");
                return new LeadStageCounts();
            }
        }

        public Lead? GetById(int id)
        {
            if (CurrentSession.IsOffline || !SyncService.Instance.IsOnline)
            {
                return LocalDataCache.Instance.GetCachedLeads(TenantId, CurrentSession.UserId, RbacService.IsAgent)
                    .FirstOrDefault(l => l.LeadId == id);
            }

            var item = _db.Leads
                .AsNoTracking()
                .SingleOrDefault(x => x.LeadId == id);

            if (item is null) return null;

            if (!RbacService.CanAgentViewRecord(item.AssignedAgentId, item.CreatedByUserId))
                return null;

            return item;
        }

        public Lead Add(Lead lead)
        {
            if (lead.PersonId <= 0 && lead.Person == null)
            {
                lead.Person = new Person
                {
                    FirstName = lead.FirstName,
                    MiddleName = lead.MiddleName,
                    LastName = lead.LastName,
                    Suffix = lead.Suffix,
                    Email = lead.Email,
                    Phone = lead.Phone
                };
            }
            lead.CreatedAt = DateTime.UtcNow;
            lead.CreatedByUserId = CurrentSession.UserId > 0 ? CurrentSession.UserId : 1;
            lead.IsDeleted = false;
            lead.DeletedAt = null;

            if (RbacService.CanAssignRecords)
            {
                if (lead.AssignedAgentId <= 0)
                {
                    lead.AssignedAgentId = null;
                }
            }
            else
            {
                // R23: Default state is Unassigned — never auto-assigned to creator.
                // R24: Only Manager or Admin may set ownership.
                ApplyAssignmentDefaults(lead);
            }

            if (CurrentSession.IsOffline || !SyncService.Instance.IsOnline)
            {
                var qItem = SyncService.Instance.EnqueueOfflineCreate("Lead", lead, TenantId, CurrentSession.UserId);
                lead.LeadId = -qItem.QueueId;
                return lead;
            }

            _db.Leads.Add(lead);
            _db.SaveChanges();
            LogActivity("Lead Created", lead.LeadId, null, $"Lead '{lead.FullName}' was created.");

            if (lead.AssignedAgentId == null || lead.AssignedAgentId <= 0 || lead.AssignmentStatus == "pending_review")
            {
                _notifCtrl.NotifyManagers(TenantId, NotificationType.LeadUnassigned, "New Lead Pending Assignment", $"Lead '{lead.FullName}' was created and is pending review/assignment.", "Lead", lead.LeadId);
            }
            else if (lead.AssignedAgentId.HasValue && lead.AssignedAgentId.Value > 0)
            {
                _notifCtrl.CreateNotification(TenantId, lead.AssignedAgentId.Value, NotificationType.LeadAssigned, "Lead Assigned to You", $"You have been assigned Lead '{lead.FullName}'.", "Lead", lead.LeadId);
            }

            return lead;
        }

        public void Update(Lead lead)
        {
            if (CurrentSession.IsOffline || !SyncService.Instance.IsOnline)
            {
                SyncService.Instance.EnqueueOfflineUpdate("Lead", lead.LeadId, lead, TenantId, CurrentSession.UserId, lead.CreatedAt);
                return;
            }

            var item = _db.Leads
                .Include(l => l.Person)
                .SingleOrDefault(x => x.LeadId == lead.LeadId);
            if (item is null) return;

            item.FirstName = lead.FirstName;
            item.MiddleName = lead.MiddleName;
            item.LastName = lead.LastName;
            item.Suffix = lead.Suffix;
            item.Phone = lead.Phone;
            item.Email = lead.Email;
            item.Source = lead.Source;
            item.Stage = lead.Stage;
            item.Notes = lead.Notes;
            item.Priority = lead.Priority;
            item.ExpectedValue = lead.ExpectedValue;

            // R24: Only Manager or Admin may set or change ownership.
            if (RbacService.CanAssignRecords)
            {
                var oldAgentId = item.AssignedAgentId;
                var newAgentId = lead.AssignedAgentId <= 0 ? null : lead.AssignedAgentId;

                item.AssignedAgentId = newAgentId;
                item.AssignmentStatus = lead.AssignmentStatus;
                item.AssignmentReviewedByUserId = lead.AssignmentReviewedByUserId;
                item.AssignmentReviewedAt = lead.AssignmentReviewedAt;
                item.AssignmentReviewNotes = lead.AssignmentReviewNotes;

                if (oldAgentId != newAgentId)
                {
                    if (newAgentId.HasValue && newAgentId.Value > 0)
                    {
                        TransferOpenFollowUps(item.LeadId, newAgentId.Value);
                        _notifCtrl.CreateNotification(TenantId, newAgentId.Value, NotificationType.LeadAssigned, "Lead Assigned to You", $"You have been assigned Lead '{item.FullName}'.", "Lead", item.LeadId);
                    }

                    LogActivity("Lead Assignment Changed", item.LeadId, null,
                        $"Lead '{item.FullName}' assignment changed from Agent #{oldAgentId?.ToString() ?? "Unassigned"} to Agent #{newAgentId?.ToString() ?? "Unassigned"} by User #{CurrentSession.UserId}.");
                }
                else if (item.Stage != lead.Stage && item.AssignedAgentId.HasValue && item.AssignedAgentId.Value > 0)
                {
                    _notifCtrl.CreateNotification(TenantId, item.AssignedAgentId.Value, NotificationType.LeadStageChanged, "Lead Stage Updated", $"Lead '{item.FullName}' stage changed to {lead.Stage}.", "Lead", item.LeadId);
                }
            }
            else if (item.Stage != lead.Stage && item.AssignedAgentId.HasValue && item.AssignedAgentId.Value > 0)
            {
                _notifCtrl.CreateNotification(TenantId, item.AssignedAgentId.Value, NotificationType.LeadStageChanged, "Lead Stage Updated", $"Lead '{item.FullName}' stage changed to {lead.Stage}.", "Lead", item.LeadId);
            }

            _db.SaveChanges();

            LogActivity("Lead Updated", item.LeadId, null, $"Lead '{item.FullName}' was updated.");
        }

        public void SoftDelete(Lead lead)
        {
            var item = _db.Leads.SingleOrDefault(x => x.LeadId == lead.LeadId);
            if (item is null) return;

            item.IsDeleted = true;
            item.DeletedAt = DateTime.UtcNow;
            _db.SaveChanges();
            LogActivity("Lead Archived", item.LeadId, null, $"Lead '{item.FullName}' was archived.");
        }
        public void Restore(Lead lead)
        {
            var item = _db.Leads
                .IgnoreQueryFilters()
                .SingleOrDefault(x => x.LeadId == lead.LeadId);
            if (item is null) return;

            item.IsDeleted = false;
            item.DeletedAt = null;
            _db.SaveChanges();
            LogActivity("Lead Restored", item.LeadId, null, $"Lead '{item.FullName}' was restored from archive.");
        }

        public List<Lead> GetArchived()
        {
            try
            {
                return _db.Leads
                    .AsNoTracking()
                    .Include(l => l.Person)
                    .Where(l => l.IsDeleted)
                    .OrderByDescending(l => l.DeletedAt)
                    .ToList();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[LeadController.GetArchived] Error: {ex.Message}");
                return new List<Lead>();
            }
        }

        public Customer ConvertToCustomer(Lead lead)
        {
            var item = _db.Leads.SingleOrDefault(x => x.LeadId == lead.LeadId);
            if (item is null)
                throw new InvalidOperationException("Lead not found.");

            if (string.Equals(item.Stage, "converted", StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException("This lead has already been converted.");

            var customer = new Customer
            {
                PersonId = item.PersonId,
                CreatedByUserId = item.CreatedByUserId > 0 ? item.CreatedByUserId : (CurrentSession.UserId > 0 ? CurrentSession.UserId : 1),
                FirstName = item.FirstName,
                MiddleName = item.MiddleName,
                LastName = item.LastName,
                Suffix = item.Suffix,
                Email = item.Email,
                Phone = item.Phone,
                Type = "buyer",
                Status = "active",
                AssignedAgentId = item.AssignedAgentId,
                AssignmentStatus = item.AssignmentStatus,
                AssignmentReviewedByUserId = item.AssignmentReviewedByUserId,
                AssignmentReviewedAt = item.AssignmentReviewedAt,
                AssignmentReviewNotes = item.AssignmentReviewNotes,
                CreatedAt = DateTime.UtcNow,
                IsDeleted = false,
                DeletedAt = null
            };

            _db.Customers.Add(customer);
            _db.SaveChanges();

            item.Stage = "converted";
            item.ConvertedCustomerId = customer.CustomerId;
            _db.SaveChanges();
            LogActivity("Lead Converted", item.LeadId, customer.CustomerId, $"Lead '{item.FullName}' was converted to customer #{customer.CustomerId}.");

            return customer;
        }

        public void MarkLost(Lead lead)
        {
            var item = _db.Leads.SingleOrDefault(x => x.LeadId == lead.LeadId);
            if (item is null) return;

            item.Stage = "lost";
            _db.SaveChanges();
            LogActivity("Lead Lost", item.LeadId, null, $"Lead '{item.FullName}' was marked as lost.");
        }

        public void RestoreFromLost(Lead lead)
        {
            var item = _db.Leads.SingleOrDefault(x => x.LeadId == lead.LeadId);
            if (item is null) return;

            item.Stage = "contacted";
            _db.SaveChanges();
            LogActivity("Lead Restored", item.LeadId, null, $"Lead '{item.FullName}' was restored from lost.");
        }

        public void ApproveAssignment(Lead lead, string? notes = null)
        {
            var item = _db.Leads.SingleOrDefault(x => x.LeadId == lead.LeadId);
            if (item is null) return;

            item.AssignmentStatus = "approved";
            item.AssignmentReviewedByUserId = CurrentSession.UserId;
            item.AssignmentReviewedAt = DateTime.UtcNow;
            item.AssignmentReviewNotes = string.IsNullOrWhiteSpace(notes) ? null : notes.Trim();
            _db.SaveChanges();
            LogActivity("Lead Assignment Approved", item.LeadId, null, $"Assignment for '{item.FullName}' was approved.");

            if (item.AssignedAgentId.HasValue && item.AssignedAgentId.Value > 0)
            {
                _notifCtrl.CreateNotification(TenantId, item.AssignedAgentId.Value, NotificationType.LeadAssigned, "Lead Assignment Approved", $"Assignment for Lead '{item.FullName}' was approved.", "Lead", item.LeadId);
            }
        }

        public void AssignAgent(Lead lead, int? agentId, bool approve = true, string? notes = null)
        {
            var item = _db.Leads.SingleOrDefault(x => x.LeadId == lead.LeadId);
            if (item is null) return;

            var oldAgentId = item.AssignedAgentId;
            var newAgentId = agentId <= 0 ? null : agentId;

            if (newAgentId.HasValue && !_db.Users.Any(u => u.UserId == newAgentId.Value))
            {
                newAgentId = null;
            }

            item.AssignedAgentId = newAgentId;
            if (approve)
            {
                item.AssignmentStatus = "approved";
                item.AssignmentReviewedByUserId = CurrentSession.UserId;
                item.AssignmentReviewedAt = DateTime.UtcNow;
                item.AssignmentReviewNotes = string.IsNullOrWhiteSpace(notes) ? null : notes.Trim();
            }

            _db.SaveChanges();

            if (oldAgentId != newAgentId)
            {
                if (newAgentId.HasValue && newAgentId.Value > 0)
                {
                    TransferOpenFollowUps(item.LeadId, newAgentId.Value);
                    _db.SaveChanges();
                    _notifCtrl.CreateNotification(TenantId, newAgentId.Value, NotificationType.LeadAssigned, "Lead Assigned to You", $"You have been assigned Lead '{item.FullName}'.", "Lead", item.LeadId);
                }

                LogActivity("Lead Assignment Changed", item.LeadId, null,
                    $"Lead '{item.FullName}' assigned to Agent #{newAgentId?.ToString() ?? "Unassigned"} by User #{CurrentSession.UserId}.");
            }
        }

        private void TransferOpenFollowUps(int leadId, int newAgentId)
        {
            try
            {
                var openFollowUps = _db.TaskReminders
                    .Where(t => t.RelatedLeadId == leadId && !t.IsDeleted && t.Status != "Completed")
                    .ToList();

                foreach (var fu in openFollowUps)
                {
                    fu.AssignedToUserId = newAgentId;
                    fu.UpdatedAt = DateTime.UtcNow;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[LeadController.TransferOpenFollowUps] Error: {ex.Message}");
            }
        }

        public List<Lead> GetPendingReview()
        {
            return _db.Leads
                .AsNoTracking()
                .Where(l => l.AssignmentStatus == "pending_review" || l.AssignedAgentId == null)
                .OrderByDescending(l => l.CreatedAt)
                .ToList();
        }

        public List<AgentPickerItem> GetAgents()
        {
            var agentRoleIds = _db.Roles
                .AsNoTracking()
                .Where(r => r.RoleName.ToLower() == "agent")
                .Select(r => r.RoleId)
                .ToList();

            return _db.Users
                .AsNoTracking()
                .Where(u => agentRoleIds.Contains(u.RoleId) && u.Status.ToLower() != "inactive")
                .OrderBy(u => u.Person.LastName)
                .ThenBy(u => u.Person.FirstName)
                .AsEnumerable()
                .Select(u => new AgentPickerItem(u.UserId, u.FullName, u.Email))
                .ToList();
        }

        public void LogEmail(Lead lead, string subject)
        {
            LogActivity("Email", lead.LeadId, null, $"Email sent to '{lead.FullName}'. Subject: {subject}");
        }

        // ---- NEW: for the lead detail view ----

        private Dictionary<int, string>? _cachedAgentDict;

        public Dictionary<int, string> GetAgentDictionary()
        {
            if (_cachedAgentDict != null) return _cachedAgentDict;
            try
            {
                _cachedAgentDict = _db.Users
                    .AsNoTracking()
                    .Include(u => u.Person)
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
            if (assignedAgentId is null) return null;
            var dict = GetAgentDictionary();
            return dict.TryGetValue(assignedAgentId.Value, out var name) ? name : null;
        }

        public List<Activity> GetActivityHistory(int leadId)
        {
            return _db.Activities
                .AsNoTracking()
                .Where(a => a.RelatedLeadId == leadId)
                .OrderByDescending(a => a.ActivityDate)
                .ToList();
        }

        private void LogActivity(string type, int? leadId, int? customerId, string notes)
        {
            try
            {
                if (CurrentSession.UserId <= 0) return;

                int agentId = CurrentSession.UserId;
                if (!_db.Users.Any(u => u.UserId == agentId))
                {
                    var userByEmail = CurrentSession.CurrentUser != null && !string.IsNullOrEmpty(CurrentSession.CurrentUser.Email)
                        ? _db.Users.FirstOrDefault(u => u.Person != null && u.Person.Email != null && u.Person.Email.ToLower() == CurrentSession.CurrentUser.Email.ToLower())
                        : null;

                    if (userByEmail != null)
                    {
                        agentId = userByEmail.UserId;
                    }
                    else
                    {
                        var fallback = _db.Users.Select(u => u.UserId).FirstOrDefault();
                        if (fallback > 0)
                        {
                            agentId = fallback;
                        }
                        else
                        {
                            return;
                        }
                    }
                }

                _db.Activities.Add(new Activity
                {
                    Type = type,
                    RelatedLeadId = leadId,
                    RelatedCustomerId = customerId,
                    LoggedByAgentId = agentId,
                    Notes = notes,
                    ActivityDate = DateTime.UtcNow
                });

                // Auto-advance lead stage from 'new' to 'contacted' upon outbound activity/email
                if (leadId.HasValue && leadId.Value > 0)
                {
                    var lead = _db.Leads.FirstOrDefault(l => l.LeadId == leadId.Value);
                    if (lead != null && string.Equals(lead.Stage, "new", StringComparison.OrdinalIgnoreCase))
                    {
                        lead.Stage = "contacted";
                    }
                }

                _db.SaveChanges();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"LogActivity error ({type}): {ex.Message}");
            }
        }

        private static void ApplyAssignmentDefaults(Lead lead)
        {
            // R23. Default state is Unassigned — never auto-assigned to creator.
            lead.AssignedAgentId = null;
            lead.AssignmentStatus = "pending_review";
        }

        public static bool ValidateLeadInput(
            string firstName,
            string lastName,
            string? email,
            string? expectedValueText,
            out decimal? parsedExpectedValue,
            out string? errorMessage,
            out string? errorField)
        {
            return ValidateLeadInput(firstName, lastName, email, null, expectedValueText, out parsedExpectedValue, out errorMessage, out errorField);
        }

        public static bool ValidateLeadInput(
            string firstName,
            string lastName,
            string? email,
            string? phone,
            string? expectedValueText,
            out decimal? parsedExpectedValue,
            out string? errorMessage,
            out string? errorField)
        {
            parsedExpectedValue = null;
            errorField = null;

            if (!ValidationHelper.IsValidPersonName(firstName, "First name", out errorMessage))
            {
                errorField = "FirstName";
                return false;
            }

            if (!ValidationHelper.IsValidPersonName(lastName, "Last name", out errorMessage))
            {
                errorField = "LastName";
                return false;
            }

            if (!string.IsNullOrWhiteSpace(email) && !ContactEmailService.IsValidEmail(email.Trim()))
            {
                errorMessage = "Enter a valid email address.";
                errorField = "Email";
                return false;
            }

            if (!string.IsNullOrWhiteSpace(phone) && !ValidationHelper.IsValidPhoneNumber(phone, out errorMessage))
            {
                errorField = "Phone";
                return false;
            }

            if (!string.IsNullOrWhiteSpace(expectedValueText))
            {
                if (decimal.TryParse(expectedValueText.Replace(",", "").Trim(), out decimal parsedVal) && parsedVal >= 0)
                {
                    parsedExpectedValue = parsedVal;
                }
                else
                {
                    errorMessage = "Enter a valid expected value amount.";
                    errorField = "ExpectedValue";
                    return false;
                }
            }

            errorMessage = null;
            return true;
        }

        public int GetActiveLeadsCount(int? agentId = null)
        {
            try
            {
                var query = _db.Leads.AsNoTracking().Where(l => !l.IsDeleted && l.Stage.ToLower() != "converted" && l.Stage.ToLower() != "lost");
                if (agentId.HasValue && agentId.Value > 0)
                {
                    int uid = agentId.Value;
                    query = query.Where(l => (l.AssignedAgentId.HasValue && l.AssignedAgentId.Value > 0)
                        ? l.AssignedAgentId.Value == uid
                        : l.CreatedByUserId == uid);
                }
                else if (!RbacService.HasFullOversight && RbacService.IsAgent)
                {
                    int uid = CurrentSession.UserId;
                    query = query.Where(l => (l.AssignedAgentId.HasValue && l.AssignedAgentId.Value > 0)
                        ? l.AssignedAgentId.Value == uid
                        : l.CreatedByUserId == uid);
                }

                return query.Count();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[LeadController.GetActiveLeadsCount] Error: {ex.Message}");
                return 0;
            }
        }

        public double GetTeamConversionRate()
        {
            try
            {
                int total = _db.Leads.AsNoTracking().Count(l => !l.IsDeleted);
                if (total == 0) return 0.0;
                int converted = _db.Leads.AsNoTracking().Count(l => !l.IsDeleted && l.Stage.ToLower() == "converted");
                return Math.Round(((double)converted / total) * 100.0, 1);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[LeadController.GetTeamConversionRate] Error: {ex.Message}");
                return 0.0;
            }
        }

        public int GetPendingReviewCount()
        {
            try
            {
                return _db.Leads
                    .AsNoTracking()
                    .Count(l => !l.IsDeleted && (l.AssignmentStatus == "pending_review" || l.AssignedAgentId == null));
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[LeadController.GetPendingReviewCount] Error: {ex.Message}");
                return 0;
            }
        }

        public void Dispose() => _db.Dispose();
    }

    public class LeadStageCounts
    {
        public int Total { get; set; }
        public int New { get; set; }
        public int Contacted { get; set; }
        public int Qualified { get; set; }
        public int Converted { get; set; }
    }
}
