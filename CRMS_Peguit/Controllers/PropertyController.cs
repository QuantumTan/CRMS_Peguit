using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CRMS_Peguit.domain.Common;
using CRMS_Peguit.domain.entities;
using CRMS_Peguit.infrastructure.data;
using CRMS_Peguit.winforms.Auth;
using CRMS_Peguit.winforms.Models.Services;
using Microsoft.EntityFrameworkCore;

namespace CRMS_Peguit.winforms.Controllers
{
    public class PropertyController : IDisposable
    {
        private readonly RealEstateDbContext _db;

        private int TenantId => CurrentSession.TenantId;
        private readonly NotificationController _notifCtrl;

        public PropertyController()
        {
            _db = LocalDb.CreateContext(TenantId);
            _notifCtrl = new NotificationController(_db);
        }

        public List<Property> GetAll()
        {
            try
            {
                var query = _db.Properties.AsNoTracking();

                // R23 & R25 (revised): Visibility scoped to creator while Pending, assignee once assigned.
                // Manager/Admin retain full oversight (R26).
                if (!RbacService.HasFullOversight && RbacService.IsAgent)
                {
                    int currentUserId = CurrentSession.UserId;
                    query = query.Where(p =>
                        (p.ListedByAgentId.HasValue && p.ListedByAgentId.Value > 0)
                            ? p.ListedByAgentId.Value == currentUserId
                            : p.CreatedByUserId == currentUserId);
                }

                return query
                    .OrderByDescending(x => x.CreatedAt)
                    .ThenBy(x => x.Address)
                    .ToList();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[PropertyController.GetAll] Error: {ex.Message}");
                return new List<Property>();
            }
        }

        public async Task<PagedResult<Property>> GetPagedAsync(
            int pageNumber = 1,
            int pageSize = 25,
            string? search = null,
            string? filterStatus = null)
        {
            try
            {
                using var db = LocalDb.CreateContext(TenantId);
                var query = db.Properties
                    .Include(p => p.OwnerCustomer).ThenInclude(c => c!.Person)
                    .Include(p => p.ListedByAgent).ThenInclude(u => u!.Person)
                    .AsNoTracking();

                if (!RbacService.HasFullOversight && RbacService.IsAgent)
                {
                    int currentUserId = CurrentSession.UserId;
                    query = query.Where(p =>
                        (p.ListedByAgentId.HasValue && p.ListedByAgentId.Value > 0)
                            ? p.ListedByAgentId.Value == currentUserId
                            : p.CreatedByUserId == currentUserId);
                }

                if (!string.IsNullOrWhiteSpace(filterStatus) && !string.Equals(filterStatus, "All", StringComparison.OrdinalIgnoreCase))
                {
                    if (string.Equals(filterStatus, "Available", StringComparison.OrdinalIgnoreCase))
                    {
                        query = query.Where(p => p.Status.ToLower() == "available");
                    }
                    else if (string.Equals(filterStatus, "Pending", StringComparison.OrdinalIgnoreCase))
                    {
                        query = query.Where(p => p.Status.ToLower() == "pending" || p.Status.ToLower() == "reserved");
                    }
                    else if (string.Equals(filterStatus, "Sold", StringComparison.OrdinalIgnoreCase))
                    {
                        query = query.Where(p => p.Status.ToLower() == "sold" || p.Status.ToLower() == "inactive");
                    }
                }

                if (!string.IsNullOrWhiteSpace(search))
                {
                    string s = search.Trim();
                    query = query.Where(p =>
                        p.Address.Contains(s) ||
                        (p.PropertyType != null && p.PropertyType.Contains(s)) ||
                        (p.Status != null && p.Status.Contains(s)) ||
                        (p.OwnerCustomer != null && (p.OwnerCustomer.Person.FirstName.Contains(s) || p.OwnerCustomer.Person.LastName.Contains(s))) ||
                        (p.ListedByAgent != null && (p.ListedByAgent.Person.FirstName.Contains(s) || p.ListedByAgent.Person.LastName.Contains(s))));
                }

                int totalCount = await query.CountAsync();

                int validPage = Math.Max(1, pageNumber);
                int validPageSize = Math.Max(1, pageSize);

                var items = await query
                    .OrderByDescending(x => x.CreatedAt)
                    .ThenBy(x => x.Address)
                    .Skip((validPage - 1) * validPageSize)
                    .Take(validPageSize)
                    .ToListAsync();

                return new PagedResult<Property>(items, totalCount, validPage, validPageSize);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[PropertyController.GetPagedAsync] Error: {ex.Message}");
                return new PagedResult<Property>(new List<Property>(), 0, pageNumber, pageSize);
            }
        }

        public async Task<PropertyCounts> GetPropertyCountsAsync()
        {
            try
            {
                using var db = LocalDb.CreateContext(TenantId);
                var query = db.Properties.AsNoTracking();

                if (!RbacService.HasFullOversight && RbacService.IsAgent)
                {
                    int currentUserId = CurrentSession.UserId;
                    query = query.Where(p =>
                        (p.ListedByAgentId.HasValue && p.ListedByAgentId.Value > 0)
                            ? p.ListedByAgentId.Value == currentUserId
                            : p.CreatedByUserId == currentUserId);
                }

                int total = await query.CountAsync();
                int available = await query.CountAsync(p => p.Status.ToLower() == "available");
                int pending = await query.CountAsync(p => p.Status.ToLower() == "pending" || p.Status.ToLower() == "reserved");
                int sold = await query.CountAsync(p => p.Status.ToLower() == "sold" || p.Status.ToLower() == "inactive");

                return new PropertyCounts
                {
                    Total = total,
                    Available = available,
                    Pending = pending,
                    Sold = sold
                };
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[PropertyController.GetPropertyCountsAsync] Error: {ex.Message}");
                return new PropertyCounts();
            }
        }

        public Property? GetById(int id)
        {
            var item = _db.Properties
                .AsNoTracking()
                .SingleOrDefault(x => x.PropertyId == id);

            if (item is null) return null;

            if (!RbacService.CanAgentViewRecord(item.ListedByAgentId, item.CreatedByUserId))
                return null;

            return item;
        }

        public Property Add(Property property)
        {
            property.CreatedAt = DateTime.UtcNow;
            property.CreatedByUserId = CurrentSession.UserId > 0 ? CurrentSession.UserId : 1;

            if (RbacService.CanAssignRecords)
            {
                if (property.ListedByAgentId <= 0)
                {
                    property.ListedByAgentId = null;
                }
            }
            else
            {
                // R23: Default state is Unassigned — never auto-assigned to creator.
                // R24: Only Manager or Admin may set ownership.
                ApplyAssignmentDefaults(property);
            }

            _db.Properties.Add(property);
            _db.SaveChanges();
            LogActivity("Property Created", null, null, $"Property listing '{property.Address}' was created.");
            return property;
        }

        public void Update(Property property)
        {
            var item = _db.Properties
                .SingleOrDefault(x => x.PropertyId == property.PropertyId);
            if (item is null) return;

            var oldStatus = item.Status;

            item.Address = property.Address;
            item.PropertyType = property.PropertyType;
            item.Price = property.Price;
            item.Status = property.Status;
            item.OwnerCustomerId = property.OwnerCustomerId;

            // R24: Only Manager or Admin may set or change ownership.
            if (RbacService.CanAssignRecords)
            {
                var oldAgentId = item.ListedByAgentId;
                var newAgentId = property.ListedByAgentId <= 0 ? null : property.ListedByAgentId;

                item.ListedByAgentId = newAgentId;
                item.AssignmentStatus = property.AssignmentStatus;
                item.AssignmentReviewedByUserId = property.AssignmentReviewedByUserId;
                item.AssignmentReviewedAt = property.AssignmentReviewedAt;
                item.AssignmentReviewNotes = property.AssignmentReviewNotes;

                if (oldAgentId != newAgentId)
                {
                    if (newAgentId.HasValue && newAgentId.Value > 0)
                    {
                        _notifCtrl.CreateNotification(TenantId, newAgentId.Value, NotificationType.PropertyAssigned, "Property Listing Assigned", $"You have been assigned to listing '{item.Address}'.", "Property", item.PropertyId);
                    }

                    LogActivity("Property Assignment Changed", null, null,
                        $"Property listing '{item.Address}' assignment changed from Agent #{oldAgentId?.ToString() ?? "Unassigned"} to Agent #{newAgentId?.ToString() ?? "Unassigned"} by User #{CurrentSession.UserId}.");
                }
            }

            _db.SaveChanges();

            if (!string.Equals(oldStatus, property.Status, StringComparison.OrdinalIgnoreCase) && item.ListedByAgentId.HasValue && item.ListedByAgentId.Value > 0)
            {
                _notifCtrl.CreateNotification(TenantId, item.ListedByAgentId.Value, NotificationType.PropertyStatusChanged, "Property Status Updated", $"Property '{item.Address}' status changed to '{property.Status}'.", "Property", item.PropertyId);
            }

            LogActivity("Property Updated", null, null, $"Property listing '{item.Address}' was updated.");
        }

        public void Delete(Property property)
        {
            var item = _db.Properties
                .SingleOrDefault(x => x.PropertyId == property.PropertyId);
            if (item is null) return;

            _db.Properties.Remove(item);
            _db.SaveChanges();
            LogActivity("Property Removed", null, null, $"Property listing '{item.Address}' was removed.");
        }

        public void ApproveAssignment(Property property, string? notes = null)
        {
            var item = _db.Properties
                .SingleOrDefault(x => x.PropertyId == property.PropertyId);
            if (item is null) return;

            item.AssignmentStatus = "approved";
            item.AssignmentReviewedByUserId = CurrentSession.UserId;
            item.AssignmentReviewedAt = DateTime.UtcNow;
            item.AssignmentReviewNotes = string.IsNullOrWhiteSpace(notes) ? null : notes.Trim();
            _db.SaveChanges();
            LogActivity("Property Assignment Approved", null, null, $"Assignment for property listing '{item.Address}' was approved.");
        }

        public void AssignAgent(Property property, int? agentId, bool approve = true, string? notes = null)
        {
            var item = _db.Properties
                .SingleOrDefault(x => x.PropertyId == property.PropertyId);
            if (item is null) return;

            var oldAgentId = item.ListedByAgentId;
            var newAgentId = agentId <= 0 ? null : agentId;

            if (newAgentId.HasValue && !_db.Users.Any(u => u.UserId == newAgentId.Value))
            {
                newAgentId = null;
            }

            item.ListedByAgentId = newAgentId;
            item.AssignmentStatus = approve ? "approved" : "pending_review";
            item.AssignmentReviewedByUserId = CurrentSession.UserId;
            item.AssignmentReviewedAt = DateTime.UtcNow;
            item.AssignmentReviewNotes = string.IsNullOrWhiteSpace(notes) ? null : notes.Trim();

            _db.SaveChanges();

            if (oldAgentId != newAgentId)
            {
                LogActivity("Property Assignment Changed", null, null,
                    $"Property listing '{item.Address}' assigned to Agent #{newAgentId?.ToString() ?? "Unassigned"} by User #{CurrentSession.UserId}.");
            }
        }

        public List<Property> GetPendingReview()
        {
            return _db.Properties
                .AsNoTracking()
                .Where(p => p.AssignmentStatus == "pending_review" || p.ListedByAgentId == null)
                .OrderByDescending(p => p.CreatedAt)
                .ToList();
        }

        public List<CustomerPickerItem> GetOwnerCustomers()
        {
            var sellerTypes = new[] { "seller", "both" };

            return _db.Customers
                .AsNoTracking()
                .Where(c => sellerTypes.Contains(c.Type.ToLower()) && c.Status.ToLower() == "active")
                .OrderBy(c => c.Person.LastName)
                .ThenBy(c => c.Person.FirstName)
                .Select(c => new
                {
                    c.CustomerId,
                    c.Person.FirstName,
                    c.Person.MiddleName,
                    c.Person.LastName,
                    c.Person.Suffix,
                    c.Person.Email
                })
                .AsEnumerable()
                .Select(c => new CustomerPickerItem(
                    c.CustomerId,
                    BuildFullName(c.FirstName, c.MiddleName, c.LastName, c.Suffix),
                    c.Email))
                .ToList();
        }

        public List<AgentPickerItem> GetAgents()
        {
            try
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
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[PropertyController.GetAgents] Error: {ex.Message}");
                return new List<AgentPickerItem>();
            }
        }

        public string? GetOwnerName(int ownerCustomerId)
        {
            return _db.Customers
                .AsNoTracking()
                .Where(c => c.CustomerId == ownerCustomerId)
                .Select(c => new
                {
                    c.Person.FirstName,
                    c.Person.MiddleName,
                    c.Person.LastName,
                    c.Person.Suffix
                })
                .AsEnumerable()
                .Select(c => BuildFullName(c.FirstName, c.MiddleName, c.LastName, c.Suffix))
                .SingleOrDefault();
        }

        public string? GetListedAgentName(int? listedByAgentId)
        {
            if (listedByAgentId is null) return null;
            return _db.Users
                .AsNoTracking()
                .Where(u => u.UserId == listedByAgentId.Value)
                .AsEnumerable()
                .Select(u => u.FullName)
                .SingleOrDefault();
        }

        public void Dispose() => _db.Dispose();

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

        private static void ApplyAssignmentDefaults(Property property)
        {
            // R23. Default state is Unassigned — never auto-assigned to creator.
            property.ListedByAgentId = null;
            property.AssignmentStatus = "pending_review";
        }

        private static string BuildFullName(
            string firstName,
            string? middleName,
            string lastName,
            string? suffix)
        {
            return string.Join(" ",
                new[] { firstName, middleName, lastName, suffix }
                    .Where(value => !string.IsNullOrWhiteSpace(value)));
        }
    }

    public sealed record CustomerPickerItem(int CustomerId, string FullName, string? Email)
    {
        public override string ToString() => FullName;
    }

    public sealed record AgentPickerItem(int UserId, string FullName, string Email)
    {
        public override string ToString() => FullName;
    }

    public class PropertyCounts
    {
        public int Total { get; set; }
        public int Available { get; set; }
        public int Pending { get; set; }
        public int Sold { get; set; }
    }
}
