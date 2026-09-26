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
    public class CustomerController : IDisposable
    {
        private readonly RealEstateDbContext _db;
        private readonly NotificationController _notifCtrl;

        // FIXED: was hardcoded to 1 - now uses whoever is actually logged in.
        private int TenantId => CurrentSession.TenantId;

        public CustomerController()
        {
            _db = LocalDb.CreateContext(TenantId);
            _notifCtrl = new NotificationController(_db);
        }

        public List<Customer> GetAll()
        {
            try
            {
                using var db = LocalDb.CreateContext(TenantId);
                var query = db.Customers.AsNoTracking();

                // R23 & R25 (revised): Visibility scoped to creator while Pending, assignee once assigned.
                // Manager/Admin retain full oversight (R26).
                if (!RbacService.HasFullOversight && RbacService.IsAgent)
                {
                    int currentUserId = CurrentSession.UserId;
                    query = query.Where(c =>
                        (c.AssignedAgentId.HasValue && c.AssignedAgentId.Value > 0)
                            ? c.AssignedAgentId.Value == currentUserId
                            : c.CreatedByUserId == currentUserId);
                }

                if (CurrentSession.CanAccessBranching && CurrentSession.ActiveBranchId.HasValue)
                {
                    int activeBranch = CurrentSession.ActiveBranchId.Value;
                    query = query.Where(c => (c.AssignedAgent != null && c.AssignedAgent.BranchId == activeBranch) ||
                                             (c.CreatedByUser != null && c.CreatedByUser.BranchId == activeBranch));
                }

                var list = query
                    .OrderBy(x => x.Person.LastName)
                    .ThenBy(x => x.Person.FirstName)
                    .ToList();

                if (list.Count > 0)
                {
                    _ = Task.Run(() => LocalDataCache.Instance.SaveCustomersMirror(TenantId, list));
                }

                return list;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[CustomerController.GetAll] Error: {ex.Message}");
                return new List<Customer>();
            }
        }

        public async Task<PagedResult<Customer>> GetPagedAsync(
            int pageNumber = 1,
            int pageSize = 25,
            string? search = null,
            string? filterStatus = null)
        {
            try
            {
                using var db = LocalDb.CreateContext(TenantId);
                var query = db.Customers
                    .Include(c => c.Person)
                    .Include(c => c.AssignedAgent)
                    .Include(c => c.CreatedByUser)
                    .AsNoTracking()
                    .Where(c => !c.IsDeleted);

                if (!RbacService.HasFullOversight && RbacService.IsAgent)
                {
                    int currentUserId = CurrentSession.UserId;
                    query = query.Where(c =>
                        (c.AssignedAgentId.HasValue && c.AssignedAgentId.Value > 0)
                            ? c.AssignedAgentId.Value == currentUserId
                            : c.CreatedByUserId == currentUserId);
                }

                if (CurrentSession.CanAccessBranching && CurrentSession.ActiveBranchId.HasValue)
                {
                    int activeBranch = CurrentSession.ActiveBranchId.Value;
                    query = query.Where(c => (c.AssignedAgent != null && c.AssignedAgent.BranchId == activeBranch) ||
                                             (c.CreatedByUser != null && c.CreatedByUser.BranchId == activeBranch));
                }

                if (!string.IsNullOrWhiteSpace(filterStatus) && !string.Equals(filterStatus, "All", StringComparison.OrdinalIgnoreCase))
                {
                    if (string.Equals(filterStatus, "Active", StringComparison.OrdinalIgnoreCase))
                    {
                        query = query.Where(c => c.Status.ToLower() == "active");
                    }
                    else if (string.Equals(filterStatus, "Inactive", StringComparison.OrdinalIgnoreCase))
                    {
                        query = query.Where(c => c.Status.ToLower() == "inactive");
                    }
                    else if (string.Equals(filterStatus, "Follow Up", StringComparison.OrdinalIgnoreCase))
                    {
                        query = query.Where(c => c.Status.ToLower() == "prospect" || c.AssignmentStatus.ToLower() == "pending");
                    }
                }

                if (!string.IsNullOrWhiteSpace(search))
                {
                    string s = search.Trim();
                    query = query.Where(c =>
                        c.Person.FirstName.Contains(s) ||
                        (c.Person.MiddleName != null && c.Person.MiddleName.Contains(s)) ||
                        c.Person.LastName.Contains(s) ||
                        (c.Person.Suffix != null && c.Person.Suffix.Contains(s)) ||
                        (c.Person.Email != null && c.Person.Email.Contains(s)) ||
                        (c.Person.Phone != null && c.Person.Phone.Contains(s)));
                }

                int totalCount = await query.CountAsync();

                int validPage = Math.Max(1, pageNumber);
                int validPageSize = Math.Max(1, pageSize);

                var items = await query
                    .OrderBy(x => x.Person.LastName)
                    .ThenBy(x => x.Person.FirstName)
                    .Skip((validPage - 1) * validPageSize)
                    .Take(validPageSize)
                    .ToListAsync();

                // Keep local SQLite mirror updated in background
                if (items.Count > 0)
                {
                    _ = Task.Run(() => LocalDataCache.Instance.SaveCustomersMirror(TenantId, items));
                }

                return new PagedResult<Customer>(items, totalCount, validPage, validPageSize);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[CustomerController.GetPagedAsync] LocalDb exception, falling back to cache: {ex.Message}");
                var cached = LocalDataCache.Instance.GetCachedCustomers(TenantId, CurrentSession.UserId, RbacService.IsAgent);

                if (!string.IsNullOrWhiteSpace(search))
                {
                    string s = search.Trim().ToLowerInvariant();
                    cached = cached.Where(c => (c.Person != null && (
                        c.Person.FirstName.ToLowerInvariant().Contains(s) ||
                        c.Person.LastName.ToLowerInvariant().Contains(s) ||
                        (c.Person.Email != null && c.Person.Email.ToLowerInvariant().Contains(s)) ||
                        (c.Person.Phone != null && c.Person.Phone.Contains(s)))) ||
                        c.Type.ToLowerInvariant().Contains(s)).ToList();
                }

                int total = cached.Count;
                var paged = cached.Skip((pageNumber - 1) * pageSize).Take(pageSize).ToList();
                return new PagedResult<Customer>(paged, total, pageNumber, pageSize);
            }
        }

        public Customer? GetById(int id)
        {
            try
            {
                var item = _db.Customers
                    .Include(c => c.Person)
                    .AsNoTracking()
                    .SingleOrDefault(x => x.CustomerId == id);

                if (item is null) return null;

                if (!RbacService.CanAgentViewRecord(item.AssignedAgentId, item.CreatedByUserId))
                    return null;

                return item;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[CustomerController.GetById] LocalDb query failed, falling back to cache: {ex.Message}");
                return LocalDataCache.Instance.GetCachedCustomers(TenantId, CurrentSession.UserId, RbacService.IsAgent)
                    .FirstOrDefault(c => c.CustomerId == id);
            }
        }

        public Customer Add(Customer customer)
        {
            if (customer.PersonId <= 0 && customer.Person == null)
            {
                customer.Person = new Person
                {
                    FirstName = customer.FirstName,
                    MiddleName = customer.MiddleName,
                    LastName = customer.LastName,
                    Suffix = customer.Suffix,
                    Email = customer.Email,
                    Phone = customer.Phone
                };
            }
            customer.CreatedAt = DateTime.UtcNow;
            customer.CreatedByUserId = CurrentSession.UserId > 0 ? CurrentSession.UserId : 1;
            customer.IsDeleted = false;
            customer.DeletedAt = null;

            if (RbacService.CanAssignRecords)
            {
                if (customer.AssignedAgentId <= 0)
                {
                    customer.AssignedAgentId = null;
                }
            }
            else
            {
                // R23: Default state is Unassigned — never auto-assigned to creator.
                // R24: Only Manager or Admin may set ownership.
                ApplyAssignmentDefaults(customer);
            }

            _db.Customers.Add(customer);
            _db.SaveChanges();
            try { LocalDataCache.Instance.SaveCustomersMirror(TenantId, new[] { customer }); } catch { }
            LogActivity("Customer Created", null, customer.CustomerId, $"Customer '{customer.FullName}' was created.");

            if (customer.AssignedAgentId == null || customer.AssignedAgentId <= 0 || customer.AssignmentStatus == "pending_review")
            {
                _notifCtrl.NotifyManagers(TenantId, NotificationType.CustomerUnassigned, "New Customer Pending Assignment", $"Customer '{customer.FullName}' was created and needs review/assignment.", "Customer", customer.CustomerId);
            }
            else if (customer.AssignedAgentId.HasValue && customer.AssignedAgentId.Value > 0)
            {
                _notifCtrl.CreateNotification(TenantId, customer.AssignedAgentId.Value, NotificationType.CustomerAssigned, "Customer Assigned to You", $"You have been assigned Customer '{customer.FullName}'.", "Customer", customer.CustomerId);
            }

            if (CurrentSession.IsOffline || !SyncService.Instance.IsOnline)
            {
                SyncService.Instance.EnqueueOfflineCreate("Customer", customer, TenantId, CurrentSession.UserId);
            }

            return customer;
        }

        public void Update(Customer customer)
        {
            if (CurrentSession.IsOffline || !SyncService.Instance.IsOnline)
            {
                SyncService.Instance.EnqueueOfflineUpdate("Customer", customer.CustomerId, customer, TenantId, CurrentSession.UserId, customer.CreatedAt);
            }

            var item = _db.Customers
                .Include(c => c.Person)
                .SingleOrDefault(x => x.CustomerId == customer.CustomerId);
            if (item is null) return;

            item.FirstName = customer.FirstName;
            item.MiddleName = customer.MiddleName;
            item.LastName = customer.LastName;
            item.Suffix = customer.Suffix;
            item.Phone = customer.Phone;
            item.Email = customer.Email;
            item.Type = customer.Type;
            item.Status = customer.Status;

            // R24: Only Manager or Admin may set or change ownership.
            if (RbacService.CanAssignRecords)
            {
                var oldAgentId = item.AssignedAgentId;
                var newAgentId = customer.AssignedAgentId <= 0 ? null : customer.AssignedAgentId;

                item.AssignedAgentId = newAgentId;
                item.AssignmentStatus = customer.AssignmentStatus;
                item.AssignmentReviewedByUserId = customer.AssignmentReviewedByUserId;
                item.AssignmentReviewedAt = customer.AssignmentReviewedAt;
                item.AssignmentReviewNotes = customer.AssignmentReviewNotes;

                if (oldAgentId != newAgentId)
                {
                    if (newAgentId.HasValue && newAgentId.Value > 0)
                    {
                        TransferOpenFollowUps(item.CustomerId, newAgentId.Value);
                        _notifCtrl.CreateNotification(TenantId, newAgentId.Value, NotificationType.CustomerAssigned, "Customer Assigned to You", $"You have been assigned Customer '{item.FullName}'.", "Customer", item.CustomerId);
                    }

                    LogActivity("Customer Assignment Changed", null, item.CustomerId,
                        $"Customer '{item.FullName}' assignment changed from Agent #{oldAgentId?.ToString() ?? "Unassigned"} to Agent #{newAgentId?.ToString() ?? "Unassigned"} by User #{CurrentSession.UserId}.");
                }
            }

            _db.SaveChanges();
            try { LocalDataCache.Instance.SaveCustomersMirror(TenantId, new[] { item }); } catch { }

            LogActivity("Customer Updated", null, item.CustomerId, $"Customer '{item.FullName}' was updated.");
        }

        public void SoftDelete(Customer customer)
        {
            var item = _db.Customers
                .SingleOrDefault(x => x.CustomerId == customer.CustomerId);
            if (item is null) return;

            item.IsDeleted = true;
            item.DeletedAt = DateTime.UtcNow;
            _db.SaveChanges();
            try { LocalDataCache.Instance.SaveCustomersMirror(TenantId, new[] { item }); } catch { }
            LogActivity("Customer Archived", null, item.CustomerId, $"Customer '{item.FullName}' was archived.");
        }

        public void Restore(Customer customer)
        {
            var item = _db.Customers
                .IgnoreQueryFilters()
                .SingleOrDefault(x => x.CustomerId == customer.CustomerId);
            if (item is null) return;

            item.IsDeleted = false;
            item.DeletedAt = null;
            _db.SaveChanges();
            try { LocalDataCache.Instance.SaveCustomersMirror(TenantId, new[] { item }); } catch { }
            LogActivity("Customer Restored", null, item.CustomerId, $"Customer '{item.FullName}' was restored from archive.");
        }

        public List<Customer> GetArchived()
        {
            try
            {
                return _db.Customers
                    .AsNoTracking()
                    .Include(c => c.Person)
                    .Where(c => c.IsDeleted)
                    .OrderByDescending(c => c.DeletedAt)
                    .ToList();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[CustomerController.GetArchived] Error: {ex.Message}");
                return new List<Customer>();
            }
        }

        public void Delete(Customer customer) => SoftDelete(customer);

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

        public List<Property> GetOwnedProperties(int customerId)
        {
            return _db.Properties
                .AsNoTracking()
                .Where(p => p.OwnerCustomerId == customerId)
                .OrderByDescending(p => p.CreatedAt)
                .ToList();
        }

        public List<Activity> GetActivityHistory(int customerId)
        {
            return _db.Activities
                .AsNoTracking()
                .Where(a => a.RelatedCustomerId == customerId)
                .OrderByDescending(a => a.ActivityDate)
                .ToList();
        }

        public void ApproveAssignment(Customer customer, string? notes = null)
        {
            var item = _db.Customers.SingleOrDefault(x => x.CustomerId == customer.CustomerId);
            if (item is null) return;

            item.AssignmentStatus = "approved";
            item.AssignmentReviewedByUserId = CurrentSession.UserId;
            item.AssignmentReviewedAt = DateTime.UtcNow;
            item.AssignmentReviewNotes = string.IsNullOrWhiteSpace(notes) ? null : notes.Trim();
            _db.SaveChanges();
            LogActivity("Customer Assignment Approved", null, item.CustomerId, $"Assignment for '{item.FullName}' was approved.");

            if (item.AssignedAgentId.HasValue && item.AssignedAgentId.Value > 0)
            {
                _notifCtrl.CreateNotification(TenantId, item.AssignedAgentId.Value, NotificationType.CustomerAssigned, "Customer Assignment Approved", $"Assignment for Customer '{item.FullName}' was approved.", "Customer", item.CustomerId);
            }
        }

        public void AssignAgent(Customer customer, int? agentId, bool approve = true, string? notes = null)
        {
            var item = _db.Customers.SingleOrDefault(x => x.CustomerId == customer.CustomerId);
            if (item is null) return;

            var oldAgentId = item.AssignedAgentId;
            var newAgentId = agentId <= 0 ? null : agentId;

            if (newAgentId.HasValue && !_db.Users.Any(u => u.UserId == newAgentId.Value))
            {
                newAgentId = null;
            }

            item.AssignedAgentId = newAgentId;
            item.AssignmentStatus = approve ? "approved" : "pending_review";
            item.AssignmentReviewedByUserId = CurrentSession.UserId;
            item.AssignmentReviewedAt = DateTime.UtcNow;
            item.AssignmentReviewNotes = string.IsNullOrWhiteSpace(notes) ? null : notes.Trim();

            _db.SaveChanges();

            if (oldAgentId != newAgentId)
            {
                if (newAgentId.HasValue && newAgentId.Value > 0)
                {
                    TransferOpenFollowUps(item.CustomerId, newAgentId.Value);
                    _db.SaveChanges();
                    _notifCtrl.CreateNotification(TenantId, newAgentId.Value, NotificationType.CustomerAssigned, "Customer Assigned to You", $"You have been assigned Customer '{item.FullName}'.", "Customer", item.CustomerId);
                }

                LogActivity("Customer Assignment Changed", null, item.CustomerId,
                    $"Customer '{item.FullName}' assigned to Agent #{newAgentId?.ToString() ?? "Unassigned"} by User #{CurrentSession.UserId}.");
            }
        }

        private void TransferOpenFollowUps(int customerId, int newAgentId)
        {
            try
            {
                var openFollowUps = _db.TaskReminders
                    .Where(t => t.RelatedCustomerId == customerId && !t.IsDeleted && t.Status != "Completed")
                    .ToList();

                foreach (var fu in openFollowUps)
                {
                    fu.AssignedToUserId = newAgentId;
                    fu.UpdatedAt = DateTime.UtcNow;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[CustomerController.TransferOpenFollowUps] Error: {ex.Message}");
            }
        }

        public List<Customer> GetPendingReview()
        {
            using var db = LocalDb.CreateContext(TenantId);
            return db.Customers
                .AsNoTracking()
                .Where(c => c.AssignmentStatus == "pending_review" || c.AssignedAgentId == null)
                .OrderByDescending(c => c.CreatedAt)
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

        // KPI counts - computed directly in SQL to prevent loading entire table into memory
        public async Task<CustomerKpiCounts> GetKpiCountsAsync()
        {
            try
            {
                using var db = LocalDb.CreateContext(TenantId);
                var query = db.Customers.AsNoTracking().Where(c => !c.IsDeleted);

                if (!RbacService.HasFullOversight && RbacService.IsAgent)
                {
                    int currentUserId = CurrentSession.UserId;
                    query = query.Where(c =>
                        (c.AssignedAgentId.HasValue && c.AssignedAgentId.Value > 0)
                            ? c.AssignedAgentId.Value == currentUserId
                            : c.CreatedByUserId == currentUserId);
                }

                var now = DateTime.UtcNow;
                int total = await query.CountAsync();
                int active = await query.CountAsync(c => c.Status.ToLower() == "active");
                int inactive = await query.CountAsync(c => c.Status.ToLower() == "inactive");
                int followUp = await query.CountAsync(c => c.Status.ToLower() == "prospect" || c.AssignmentStatus.ToLower() == "pending");
                int thisMonth = await query.CountAsync(c => c.CreatedAt.Year == now.Year && c.CreatedAt.Month == now.Month);

                return new CustomerKpiCounts
                {
                    Total = total,
                    Active = active,
                    Inactive = inactive,
                    FollowUp = followUp,
                    ThisMonth = thisMonth
                };
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[CustomerController.GetKpiCountsAsync] Error: {ex.Message}");
                return new CustomerKpiCounts();
            }
        }

        public CustomerKpiCounts GetKpiCounts()
        {
            try
            {
                return GetKpiCountsAsync().GetAwaiter().GetResult();
            }
            catch
            {
                return new CustomerKpiCounts();
            }
        }

        public void LogEmail(Customer customer, string subject)
        {
            LogActivity("Email", null, customer.CustomerId, $"Email sent to '{customer.FullName}'. Subject: {subject}");
        }

        public void LogCall(Customer customer, string notes)
        {
            LogActivity("Call", null, customer.CustomerId, $"Call logged for '{customer.FullName}': {notes}");
        }

        public void LogMeeting(Customer customer, string notes)
        {
            LogActivity("Meeting", null, customer.CustomerId, $"Meeting held with '{customer.FullName}': {notes}");
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
                _db.SaveChanges();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"LogActivity error ({type}): {ex.Message}");
            }
        }

        private static void ApplyAssignmentDefaults(Customer customer)
        {
            // R23. Default state is Unassigned — never auto-assigned to creator.
            customer.AssignedAgentId = null;
            customer.AssignmentStatus = "pending_review";
        }

        public static bool ValidateCustomerInput(string firstName, string lastName, string? email, out string? errorMessage)
        {
            return ValidateCustomerInput(firstName, lastName, email, null, out errorMessage);
        }

        public static bool ValidateCustomerInput(string firstName, string lastName, string? email, string? phone, out string? errorMessage)
        {
            if (!ValidationHelper.IsValidPersonName(firstName, "First name", out errorMessage))
            {
                return false;
            }

            if (!ValidationHelper.IsValidPersonName(lastName, "Last name", out errorMessage))
            {
                return false;
            }

            if (!string.IsNullOrWhiteSpace(email) && !ContactEmailService.IsValidEmail(email.Trim()))
            {
                errorMessage = "Enter a valid email address.";
                return false;
            }

            if (!string.IsNullOrWhiteSpace(phone) && !ValidationHelper.IsValidPhoneNumber(phone, out errorMessage))
            {
                return false;
            }

            errorMessage = null;
            return true;
        }

        public List<Activity> GetRecentActivitiesForAgent(int agentId, int maxCount = 5)
        {
            try
            {
                return _db.Activities
                    .AsNoTracking()
                    .Where(a => a.LoggedByAgentId == agentId)
                    .OrderByDescending(a => a.ActivityDate)
                    .Take(maxCount)
                    .ToList();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[CustomerController.GetRecentActivitiesForAgent] Error: {ex.Message}");
                return new List<Activity>();
            }
        }

        public int GetPendingReviewCount()
        {
            try
            {
                return _db.Customers
                    .AsNoTracking()
                    .Count(c => !c.IsDeleted && (c.AssignmentStatus == "pending_review" || c.AssignedAgentId == null));
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[CustomerController.GetPendingReviewCount] Error: {ex.Message}");
                return 0;
            }
        }

        public void Dispose() => _db.Dispose();
    }

    public class CustomerKpiCounts
    {
        public int Total { get; set; }
        public int Active { get; set; }
        public int Inactive { get; set; }
        public int FollowUp { get; set; }
        public int ThisMonth { get; set; }
    }
}
