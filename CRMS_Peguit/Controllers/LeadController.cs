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

        // FIXED: was hardcoded to 1 - now uses whoever is actually logged in (safely defaults to 1).
        private int TenantId => CurrentSession.TenantId > 0 ? CurrentSession.TenantId : 1;

        public LeadController()
        {
            _db = LocalDb.CreateContext(TenantId);
            _notifCtrl = new NotificationController(_db);
        }

        public List<Lead> GetAll()
        {
            try
            {
                using var db = LocalDb.CreateContext(TenantId);
                var query = db.Leads.AsNoTracking();

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

                if (CurrentSession.CanAccessBranching && CurrentSession.ActiveBranchId.HasValue)
                {
                    query = query.Where(l => l.BranchId == CurrentSession.ActiveBranchId.Value);
                }

                var list = query
                    .OrderBy(x => x.LastName)
                    .ThenBy(x => x.FirstName)
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
            try
            {
                using var db = LocalDb.CreateContext(TenantId);
                var query = db.Leads
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

                if (CurrentSession.CanAccessBranching && CurrentSession.ActiveBranchId.HasValue)
                {
                    query = query.Where(l => l.BranchId == CurrentSession.ActiveBranchId.Value);
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
                        l.FirstName.Contains(s) ||
                        (l.MiddleName != null && l.MiddleName.Contains(s)) ||
                        l.LastName.Contains(s) ||
                        (l.Suffix != null && l.Suffix.Contains(s)) ||
                        (l.Email != null && l.Email.Contains(s)) ||
                        (l.Phone != null && l.Phone.Contains(s)));
                }

                int totalCount = await query.CountAsync();

                // Sort
                if (string.Equals(sortColumn, "Name", StringComparison.OrdinalIgnoreCase))
                {
                    query = isAscending
                        ? query.OrderBy(x => x.LastName).ThenBy(x => x.FirstName)
                        : query.OrderByDescending(x => x.LastName).ThenByDescending(x => x.FirstName);
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

                if (items.Count > 0)
                {
                    _ = Task.Run(() => LocalDataCache.Instance.SaveLeadsMirror(TenantId, items));
                }

                return new PagedResult<Lead>(items, totalCount, validPage, validPageSize);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[LeadController.GetPagedAsync] LocalDb failed, falling back to cache: {ex.Message}");
                var cached = LocalDataCache.Instance.GetCachedLeads(TenantId, CurrentSession.UserId, RbacService.IsAgent);
                if (CurrentSession.CanAccessBranching && CurrentSession.ActiveBranchId.HasValue)
                {
                    cached = cached.Where(l => l.BranchId == CurrentSession.ActiveBranchId.Value).ToList();
                }
                if (!string.IsNullOrWhiteSpace(stage) && !string.Equals(stage, "All", StringComparison.OrdinalIgnoreCase))
                {
                    cached = cached.Where(l => l.Stage.Equals(stage, StringComparison.OrdinalIgnoreCase)).ToList();
                }
                if (!string.IsNullOrWhiteSpace(search))
                {
                    string s = search.Trim().ToLowerInvariant();
                    cached = cached.Where(l =>
                        l.FirstName.ToLowerInvariant().Contains(s) ||
                        l.LastName.ToLowerInvariant().Contains(s) ||
                        (l.Email != null && l.Email.ToLowerInvariant().Contains(s)) ||
                        (l.Phone != null && l.Phone.Contains(s)) ||
                        (l.Source != null && l.Source.ToLowerInvariant().Contains(s))).ToList();
                }
                int total = cached.Count;
                var paged = cached.Skip((pageNumber - 1) * pageSize).Take(pageSize).ToList();
                return new PagedResult<Lead>(paged, total, pageNumber, pageSize);
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

                if (CurrentSession.CanAccessBranching && CurrentSession.ActiveBranchId.HasValue)
                {
                    query = query.Where(l => l.BranchId == CurrentSession.ActiveBranchId.Value);
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
            try
            {
                var item = _db.Leads
                    .AsNoTracking()
                    .SingleOrDefault(x => x.LeadId == id);

                if (item is null) return null;

                if (!RbacService.CanAgentViewRecord(item.AssignedAgentId, item.CreatedByUserId))
                    return null;

                return item;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[LeadController.GetById] LocalDb failed, falling back to cache: {ex.Message}");
                return LocalDataCache.Instance.GetCachedLeads(TenantId, CurrentSession.UserId, RbacService.IsAgent)
                    .FirstOrDefault(l => l.LeadId == id);
            }
        }

        public Lead Add(Lead lead)
        {
            if (!ValidationHelper.IsValidEmail(lead.Email, out var emailError))
            {
                throw new ArgumentException(emailError ?? "Email is required.");
            }
            lead.CreatedAt = DateTime.UtcNow;
            lead.CreatedByUserId = CurrentSession.UserId > 0 ? CurrentSession.UserId : 1;
            lead.IsDeleted = false;
            lead.DeletedAt = null;

            if (CurrentSession.CanAccessBranching && !lead.BranchId.HasValue && CurrentSession.ActiveBranchId.HasValue)
            {
                lead.BranchId = CurrentSession.ActiveBranchId.Value;
            }

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

            _db.Leads.Add(lead);
            _db.SaveChanges();
            try { LocalDataCache.Instance.SaveLeadsMirror(TenantId, new[] { lead }); } catch { }
            LogActivity("Lead Created", lead.LeadId, null, $"Lead '{lead.FullName}' was created.");

            if (lead.AssignedAgentId == null || lead.AssignedAgentId <= 0 || lead.AssignmentStatus == "pending_review")
            {
                _notifCtrl.NotifyManagers(TenantId, NotificationType.LeadUnassigned, "New Lead Pending Assignment", $"Lead '{lead.FullName}' was created and is pending review/assignment.", "Lead", lead.LeadId);
            }
            else if (lead.AssignedAgentId.HasValue && lead.AssignedAgentId.Value > 0)
            {
                _notifCtrl.CreateNotification(TenantId, lead.AssignedAgentId.Value, NotificationType.LeadAssigned, "Lead Assigned to You", $"You have been assigned Lead '{lead.FullName}'.", "Lead", lead.LeadId);
            }

            SyncService.Instance.EnqueueOfflineCreate("Lead", lead, TenantId, CurrentSession.UserId, lead.LeadId.ToString());
            if (SyncService.Instance.IsOnline && !CurrentSession.IsOffline)
            {
                _ = Task.Run(() => SyncService.Instance.SyncAsync());
            }

            return lead;
        }

        public void Update(Lead lead)
        {
            if (!ValidationHelper.IsValidEmail(lead.Email, out var emailError))
            {
                throw new ArgumentException(emailError ?? "Email is required.");
            }

            var item = _db.Leads
                .SingleOrDefault(x => x.LeadId == lead.LeadId);
            if (item is null) return;

            var oldStage = item.Stage;

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
                else if (oldStage != lead.Stage && item.AssignedAgentId.HasValue && item.AssignedAgentId.Value > 0)
                {
                    _notifCtrl.CreateNotification(TenantId, item.AssignedAgentId.Value, NotificationType.LeadStageChanged, "Lead Stage Updated", $"Lead '{item.FullName}' stage changed to {lead.Stage}.", "Lead", item.LeadId);
                }
            }
            else if (oldStage != lead.Stage && item.AssignedAgentId.HasValue && item.AssignedAgentId.Value > 0)
            {
                _notifCtrl.CreateNotification(TenantId, item.AssignedAgentId.Value, NotificationType.LeadStageChanged, "Lead Stage Updated", $"Lead '{item.FullName}' stage changed to {lead.Stage}.", "Lead", item.LeadId);
            }

            _db.SaveChanges();
            try { LocalDataCache.Instance.SaveLeadsMirror(TenantId, new[] { item }); } catch { }

            LogActivity("Lead Updated", item.LeadId, null, $"Lead '{item.FullName}' was updated.");

            SyncService.Instance.EnqueueOfflineUpdate("Lead", item.LeadId, item, TenantId, CurrentSession.UserId, DateTime.UtcNow);
            if (SyncService.Instance.IsOnline && !CurrentSession.IsOffline)
            {
                _ = Task.Run(() => SyncService.Instance.SyncAsync());
            }
        }

        public void SoftDelete(Lead lead)
        {
            var item = _db.Leads.SingleOrDefault(x => x.LeadId == lead.LeadId);
            if (item is null) return;

            item.IsDeleted = true;
            item.DeletedAt = DateTime.UtcNow;
            _db.SaveChanges();
            try { LocalDataCache.Instance.SaveLeadsMirror(TenantId, new[] { item }); } catch { }
            LogActivity("Lead Archived", item.LeadId, null, $"Lead '{item.FullName}' was archived.");

            SyncService.Instance.EnqueueOfflineDelete("Lead", item.LeadId, TenantId, CurrentSession.UserId);
            if (SyncService.Instance.IsOnline && !CurrentSession.IsOffline)
            {
                _ = Task.Run(() => SyncService.Instance.SyncAsync());
            }
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
            try { LocalDataCache.Instance.SaveLeadsMirror(TenantId, new[] { item }); } catch { }
            LogActivity("Lead Restored", item.LeadId, null, $"Lead '{item.FullName}' was restored from archive.");

            SyncService.Instance.EnqueueOfflineUpdate("Lead", item.LeadId, item, TenantId, CurrentSession.UserId, DateTime.UtcNow);
            if (SyncService.Instance.IsOnline && !CurrentSession.IsOffline)
            {
                _ = Task.Run(() => SyncService.Instance.SyncAsync());
            }
        }

        public List<Lead> GetArchived()
        {
            try
            {
                return _db.Leads
                    .IgnoreQueryFilters()
                    .Include(l => l.CreatedByUser)
                        .ThenInclude(u => u.Role)
                    .AsNoTracking()
                    .Where(l => l.CreatedByUser != null && l.CreatedByUser.Role != null && l.CreatedByUser.Role.TenantId == TenantId && l.IsDeleted)
                    .OrderByDescending(l => l.DeletedAt ?? l.CreatedAt)
                    .ToList();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[LeadController.GetArchived] Error: {ex.Message}");
                return new List<Lead>();
            }
        }

        public void HardDelete(int leadId)
        {
            var item = _db.Leads
                .IgnoreQueryFilters()
                .SingleOrDefault(x => x.LeadId == leadId);
            if (item is null) return;

            // Clean up related activities or task reminders if any
            var acts = _db.Activities.IgnoreQueryFilters().Where(a => a.RelatedLeadId == leadId).ToList();
            foreach (var a in acts) a.RelatedLeadId = null;

            var reminders = _db.TaskReminders.IgnoreQueryFilters().Where(r => r.RelatedLeadId == leadId).ToList();
            foreach (var r in reminders) r.RelatedLeadId = null;

            _db.Leads.Remove(item);
            _db.SaveChanges();

            LogActivity("Lead Permanently Deleted", null, null, $"Lead '{item.FullName}' was permanently deleted from recycle bin.");
            SyncService.Instance.EnqueueOfflineDelete("Lead", leadId, TenantId, CurrentSession.UserId);
            if (SyncService.Instance.IsOnline && !CurrentSession.IsOffline)
            {
                _ = Task.Run(() => SyncService.Instance.SyncAsync());
            }
        }

        public Customer ConvertToCustomer(Lead lead)
        {
            var item = _db.Leads.SingleOrDefault(x => x.LeadId == lead.LeadId);
            if (item is null)
                throw new InvalidOperationException("Lead not found.");

            if (string.Equals(item.Stage, "converted", StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException("This lead has already been converted.");

            if (!item.AssignedAgentId.HasValue)
            {
                if (item.CreatedByUserId > 0)
                {
                    item.AssignedAgentId = item.CreatedByUserId;
                }
                else if (CurrentSession.UserId > 0)
                {
                    item.AssignedAgentId = CurrentSession.UserId;
                }
            }

            var customer = new Customer
            {
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
                AssignmentStatus = "approved",
                AssignmentReviewedByUserId = item.AssignmentReviewedByUserId ?? (CurrentSession.UserId > 0 ? CurrentSession.UserId : 1),
                AssignmentReviewedAt = item.AssignmentReviewedAt ?? DateTime.UtcNow,
                AssignmentReviewNotes = item.AssignmentReviewNotes ?? "Approved upon conversion from lead.",
                CreatedAt = DateTime.UtcNow,
                IsDeleted = false,
                DeletedAt = null
            };

            var strategy = _db.Database.CreateExecutionStrategy();
            strategy.Execute(() =>
            {
                using var tx = _db.Database.BeginTransaction();
                try
                {
                    _db.Customers.Add(customer);
                    _db.SaveChanges();

                    item.Stage = "converted";
                    item.AssignmentStatus = "approved";
                    item.ConvertedCustomerId = customer.CustomerId;
                    if (!ReferenceEquals(lead, item))
                    {
                        lead.Stage = item.Stage;
                        lead.AssignmentStatus = item.AssignmentStatus;
                        lead.ConvertedCustomerId = item.ConvertedCustomerId;
                    }
                    _db.SaveChanges();
                    tx.Commit();
                }
                catch
                {
                    tx.Rollback();
                    throw;
                }
            });

            LogActivity("Lead Converted", item.LeadId, customer.CustomerId, $"Lead '{item.FullName}' was converted to customer #{customer.CustomerId}.");

            SyncService.Instance.EnqueueOfflineCreate("Customer", customer, TenantId, CurrentSession.UserId, customer.CustomerId.ToString());
            SyncService.Instance.EnqueueOfflineUpdate("Lead", item.LeadId, item, TenantId, CurrentSession.UserId, DateTime.UtcNow);
            if (SyncService.Instance.IsOnline && !CurrentSession.IsOffline)
            {
                _ = Task.Run(() => SyncService.Instance.SyncAsync());
            }

            return customer;
        }

        public void MarkLost(Lead lead)
        {
            var item = _db.Leads.SingleOrDefault(x => x.LeadId == lead.LeadId);
            if (item is null) return;

            item.Stage = "lost";
            _db.SaveChanges();
            LogActivity("Lead Lost", item.LeadId, null, $"Lead '{item.FullName}' was marked as lost.");

            SyncService.Instance.EnqueueOfflineUpdate("Lead", item.LeadId, item, TenantId, CurrentSession.UserId, DateTime.UtcNow);
            if (SyncService.Instance.IsOnline && !CurrentSession.IsOffline)
            {
                _ = Task.Run(() => SyncService.Instance.SyncAsync());
            }
        }

        public void RestoreFromLost(Lead lead)
        {
            var item = _db.Leads.SingleOrDefault(x => x.LeadId == lead.LeadId);
            if (item is null) return;

            item.Stage = "contacted";
            _db.SaveChanges();
            LogActivity("Lead Restored", item.LeadId, null, $"Lead '{item.FullName}' was restored from lost.");

            SyncService.Instance.EnqueueOfflineUpdate("Lead", item.LeadId, item, TenantId, CurrentSession.UserId, DateTime.UtcNow);
            if (SyncService.Instance.IsOnline && !CurrentSession.IsOffline)
            {
                _ = Task.Run(() => SyncService.Instance.SyncAsync());
            }
        }

        public void ApproveAssignment(Lead lead, string? notes = null)
        {
            var item = _db.Leads.SingleOrDefault(x => x.LeadId == lead.LeadId);
            if (item is null) return;

            // If not assigned to an agent, assign to the staff member who passed/created it
            if (!item.AssignedAgentId.HasValue && item.CreatedByUserId > 0)
            {
                item.AssignedAgentId = item.CreatedByUserId;
            }

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

            SyncService.Instance.EnqueueOfflineUpdate("Lead", item.LeadId, item, TenantId, CurrentSession.UserId, item.AssignmentReviewedAt);
            if (SyncService.Instance.IsOnline && !CurrentSession.IsOffline)
            {
                _ = Task.Run(() => SyncService.Instance.SyncAsync());
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

            SyncService.Instance.EnqueueOfflineUpdate("Lead", item.LeadId, item, TenantId, CurrentSession.UserId, DateTime.UtcNow);
            if (SyncService.Instance.IsOnline && !CurrentSession.IsOffline)
            {
                _ = Task.Run(() => SyncService.Instance.SyncAsync());
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
            using var db = LocalDb.CreateContext(TenantId);
            return db.Leads
                .AsNoTracking()
                .Where(l => !l.IsDeleted && l.Stage != "converted" && l.Stage != "lost" && (l.AssignmentStatus == "pending_review" || l.AssignedAgentId == null))
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
                .OrderBy(u => u.LastName)
                .ThenBy(u => u.FirstName)
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
                        ? _db.Users.FirstOrDefault(u => u.Email != null && u.Email.ToLower() == CurrentSession.CurrentUser.Email.ToLower())
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

            if (!ValidationHelper.IsValidEmail(email, out errorMessage))
            {
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

                if (CurrentSession.CanAccessBranching && CurrentSession.ActiveBranchId.HasValue)
                {
                    query = query.Where(l => l.BranchId == CurrentSession.ActiveBranchId.Value);
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
                var query = _db.Leads.AsNoTracking().Where(l => !l.IsDeleted);
                if (CurrentSession.CanAccessBranching && CurrentSession.ActiveBranchId.HasValue)
                {
                    query = query.Where(l => l.BranchId == CurrentSession.ActiveBranchId.Value);
                }

                int total = query.Count();
                if (total == 0) return 0.0;
                int converted = query.Count(l => l.Stage.ToLower() == "converted");
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
                var query = _db.Leads
                    .AsNoTracking()
                    .Where(l => !l.IsDeleted && l.Stage != "converted" && l.Stage != "lost" && (l.AssignmentStatus == "pending_review" || l.AssignedAgentId == null));

                if (CurrentSession.CanAccessBranching && CurrentSession.ActiveBranchId.HasValue)
                {
                    query = query.Where(l => l.BranchId == CurrentSession.ActiveBranchId.Value);
                }

                return query.Count();
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
