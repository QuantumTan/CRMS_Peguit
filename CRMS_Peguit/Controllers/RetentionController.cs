using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CRMS_Peguit.domain.entities;
using CRMS_Peguit.infrastructure.data;
using CRMS_Peguit.winforms.Auth;
using CRMS_Peguit.winforms.Models.Roles;
using CRMS_Peguit.winforms.Models.Services;
using CRMS_Peguit.winforms.Services;
using Microsoft.EntityFrameworkCore;

namespace CRMS_Peguit.winforms.Controllers
{
    public class RetentionSummaryDto
    {
        public int TotalTrackedCustomers { get; set; }
        public int ActiveQueueCount { get; set; }
        public int PendingApprovalsCount { get; set; }
        public int DispatchedThisMonthCount { get; set; }
        public Dictionary<string, int> SegmentDistribution { get; set; } = new(StringComparer.OrdinalIgnoreCase);
    }

    public class RetentionCustomerRow
    {
        public int CustomerId { get; set; }
        public string FullName { get; set; } = string.Empty;
        public string FirstName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Phone { get; set; } = string.Empty;
        public string CurrentSegment { get; set; } = string.Empty;
        public DateTime? LastClosedDate { get; set; }
        public DateTime? LastActivityDate { get; set; }
        public int? AssignedAgentId { get; set; }
        public string AssignedAgentName { get; set; } = "Unassigned";
        public DateTime? LastRetentionEmailSentAt { get; set; }
        public bool IsOnCooldown { get; set; }
        public int DaysUntilCooldownExpires { get; set; }
    }

    public class RetentionRequestRow
    {
        public int RequestId { get; set; }
        public int CustomerId { get; set; }
        public string CustomerName { get; set; } = string.Empty;
        public string CustomerEmail { get; set; } = string.Empty;
        public int SubmittedByUserId { get; set; }
        public string SubmitterName { get; set; } = string.Empty;
        public string SubmitterRole { get; set; } = string.Empty;
        public int? AssignedAgentId { get; set; }
        public string AssignedAgentName { get; set; } = "Unassigned";
        public string TargetSegment { get; set; } = string.Empty;
        public string ActionType { get; set; } = string.Empty;
        public string ProposedIncentive { get; set; } = string.Empty;
        public string ReasonCategory { get; set; } = string.Empty;
        public string RetentionDetails { get; set; } = string.Empty;
        public string Status { get; set; } = "Pending";
        public int? ReviewedByUserId { get; set; }
        public string? ReviewerName { get; set; }
        public DateTime? ReviewedAt { get; set; }
        public string? ReviewerRemarks { get; set; }
        public string? RejectionReason { get; set; }
        public bool AddedToCampaign { get; set; }
        public DateTime CreatedAt { get; set; }
        public bool CanCurrentUserApprove { get; set; }
        public string CannotApproveReason { get; set; } = string.Empty;
    }

    public class RetentionQueueRow
    {
        public int EmailLogId { get; set; }
        public int CustomerId { get; set; }
        public string CustomerName { get; set; } = string.Empty;
        public string RecipientEmail { get; set; } = string.Empty;
        public string Segment { get; set; } = string.Empty;
        public string Subject { get; set; } = string.Empty;
        public string Body { get; set; } = string.Empty;
        public string? IncentiveOffered { get; set; }
        public string Status { get; set; } = "Queued";
        public string GenerationSource { get; set; } = "AutomatedRequest";
        public int? RetentionRequestId { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? DispatchedAt { get; set; }
        public string? DispatchedByName { get; set; }
        public string? ErrorMessage { get; set; }
        public bool IsCooldownOverride { get; set; }
        public string? CooldownOverrideReason { get; set; }
    }

    public class RetentionController : IDisposable
    {
        private readonly RealEstateDbContext _db;
        private int TenantId => CurrentSession.TenantId;

        public RetentionController()
        {
            _db = LocalDb.CreateContext(TenantId);
        }

        private void AssertTenantAccess()
        {
            if (RbacService.IsSuperAdmin)
            {
                throw new UnauthorizedAccessException("Super Admin has zero access to tenant customer retention data.");
            }
        }

        #region Summary & KPI Metrics

        public async Task<RetentionSummaryDto> GetSummaryAsync()
        {
            AssertTenantAccess();

            var customerQuery = _db.Customers.AsNoTracking().Where(c => !c.IsDeleted);

            // Ownership scoping for Agents
            if (!RbacService.HasFullOversight && RbacService.IsAgent)
            {
                int currentUserId = CurrentSession.UserId;
                customerQuery = customerQuery.Where(c =>
                    (c.AssignedAgentId.HasValue && c.AssignedAgentId.Value > 0)
                        ? c.AssignedAgentId.Value == currentUserId
                        : c.CreatedByUserId == currentUserId);
            }

            var customers = await customerQuery.Select(c => new { c.CustomerId, c.CurrentRetentionSegment }).ToListAsync();

            var summary = new RetentionSummaryDto
            {
                TotalTrackedCustomers = customers.Count
            };

            foreach (var seg in RetentionCalculationService.AllSegments)
            {
                summary.SegmentDistribution[seg] = 0;
            }

            foreach (var c in customers)
            {
                string seg = string.IsNullOrWhiteSpace(c.CurrentRetentionSegment)
                    ? RetentionCalculationService.SegmentProspective
                    : c.CurrentRetentionSegment;

                if (!summary.SegmentDistribution.ContainsKey(seg))
                    summary.SegmentDistribution[seg] = 0;

                summary.SegmentDistribution[seg]++;
            }

            // Approvals & Campaign Queue counts
            var reqQuery = _db.RetentionRequests.AsNoTracking().Where(r => r.Status == "Pending");
            var queueQuery = _db.RetentionEmailLogs.AsNoTracking().Where(l => l.Status == "Queued");

            if (!RbacService.HasFullOversight && RbacService.IsAgent)
            {
                int currentUserId = CurrentSession.UserId;
                reqQuery = reqQuery.Where(r => r.SubmittedByUserId == currentUserId || r.AssignedAgentId == currentUserId);
                queueQuery = queueQuery.Where(l => l.Customer.AssignedAgentId == currentUserId);
            }

            summary.PendingApprovalsCount = await reqQuery.CountAsync();
            summary.ActiveQueueCount = await queueQuery.CountAsync();

            var startOfMonth = new DateTime(DateTime.UtcNow.Year, DateTime.UtcNow.Month, 1, 0, 0, 0, DateTimeKind.Utc);
            var dispatchedQuery = _db.RetentionEmailLogs.AsNoTracking().Where(l => l.Status == "Dispatched" && l.DispatchedAt >= startOfMonth);

            if (!RbacService.HasFullOversight && RbacService.IsAgent)
            {
                dispatchedQuery = dispatchedQuery.Where(l => l.DispatchedByUserId == CurrentSession.UserId || l.Customer.AssignedAgentId == CurrentSession.UserId);
            }

            summary.DispatchedThisMonthCount = await dispatchedQuery.CountAsync();

            return summary;
        }

        #endregion

        #region Customer Roster & Segments

        public async Task<List<RetentionCustomerRow>> GetCustomersAsync(string? segmentFilter = null, string? search = null)
        {
            AssertTenantAccess();

            var query = _db.Customers
                .Include(c => c.AssignedAgent)
                .AsNoTracking()
                .Where(c => !c.IsDeleted);

            // Ownership scoping
            if (!RbacService.HasFullOversight && RbacService.IsAgent)
            {
                int currentUserId = CurrentSession.UserId;
                query = query.Where(c =>
                    (c.AssignedAgentId.HasValue && c.AssignedAgentId.Value > 0)
                        ? c.AssignedAgentId.Value == currentUserId
                        : c.CreatedByUserId == currentUserId);
            }

            if (!string.IsNullOrWhiteSpace(segmentFilter) && !segmentFilter.Equals("All", StringComparison.OrdinalIgnoreCase))
            {
                query = query.Where(c => c.CurrentRetentionSegment == segmentFilter);
            }

            if (!string.IsNullOrWhiteSpace(search))
            {
                string s = search.Trim().ToLowerInvariant();
                query = query.Where(c =>
                    (c.FirstName + " " + c.LastName).ToLower().Contains(s) ||
                    (c.Email != null && c.Email.ToLower().Contains(s)) ||
                    (c.Phone != null && c.Phone.Contains(s))
                );
            }

            var customers = await query
                .OrderBy(c => c.LastName)
                .ThenBy(c => c.FirstName)
                .ToListAsync();

            if (customers.Count == 0) return new List<RetentionCustomerRow>();

            var customerIds = customers.Select(c => c.CustomerId).ToList();

            // Load latest closed deals for these customers
            var deals = await _db.Deals
                .AsNoTracking()
                .Where(d => customerIds.Contains(d.CustomerId) && d.Stage.ToLower() == "closed")
                .Select(d => new { d.CustomerId, Date = d.ContractSignedDate ?? d.CreatedAt })
                .ToListAsync();

            var latestDeals = deals
                .GroupBy(d => d.CustomerId)
                .ToDictionary(g => g.Key, g => g.Max(x => x.Date));

            // Load latest activities
            var activities = await _db.Activities
                .AsNoTracking()
                .Where(a => a.RelatedCustomerId.HasValue && customerIds.Contains(a.RelatedCustomerId.Value))
                .Select(a => new { CustomerId = a.RelatedCustomerId!.Value, a.ActivityDate })
                .ToListAsync();

            var latestActivities = activities
                .GroupBy(a => a.CustomerId)
                .ToDictionary(g => g.Key, g => g.Max(x => x.ActivityDate));

            var now = DateTime.UtcNow;
            var result = new List<RetentionCustomerRow>();

            foreach (var c in customers)
            {
                latestDeals.TryGetValue(c.CustomerId, out var lastClosed);
                latestActivities.TryGetValue(c.CustomerId, out var lastAct);

                bool isOnCooldown = false;
                int daysRemaining = 0;

                if (c.LastRetentionEmailSentAt.HasValue)
                {
                    var daysSince = (now - c.LastRetentionEmailSentAt.Value).TotalDays;
                    if (daysSince < 30)
                    {
                        isOnCooldown = true;
                        daysRemaining = (int)Math.Ceiling(30 - daysSince);
                    }
                }

                string agentName = c.AssignedAgent?.FullName ?? "Unassigned";

                result.Add(new RetentionCustomerRow
                {
                    CustomerId = c.CustomerId,
                    FullName = c.FullName,
                    FirstName = c.FirstName,
                    Email = c.Email ?? string.Empty,
                    Phone = c.Phone ?? string.Empty,
                    CurrentSegment = string.IsNullOrWhiteSpace(c.CurrentRetentionSegment)
                        ? RetentionCalculationService.SegmentProspective
                        : c.CurrentRetentionSegment,
                    LastClosedDate = lastClosed == default ? null : lastClosed,
                    LastActivityDate = lastAct == default ? null : lastAct,
                    AssignedAgentId = c.AssignedAgentId,
                    AssignedAgentName = agentName,
                    LastRetentionEmailSentAt = c.LastRetentionEmailSentAt,
                    IsOnCooldown = isOnCooldown,
                    DaysUntilCooldownExpires = daysRemaining
                });
            }

            return result;
        }

        public async Task<Customer?> GetCustomerByIdAsync(int customerId)
        {
            AssertTenantAccess();

            var cust = await _db.Customers
                .Include(c => c.AssignedAgent)
                .FirstOrDefaultAsync(c => c.CustomerId == customerId && !c.IsDeleted);

            if (cust == null) return null;

            if (!RbacService.HasFullOversight && RbacService.IsAgent)
            {
                if (cust.AssignedAgentId != CurrentSession.UserId && cust.CreatedByUserId != CurrentSession.UserId)
                {
                    return null; // Not allowed to access
                }
            }

            return cust;
        }

        #endregion

        #region Retention Requests & Approvals Workflow

        public async Task<RetentionRequest> SubmitRetentionRequestAsync(
            int customerId,
            string targetSegment,
            string actionType,
            string proposedIncentive,
            string reasonCategory,
            string retentionDetails)
        {
            AssertTenantAccess();

            var customer = await GetCustomerByIdAsync(customerId);
            if (customer == null)
            {
                throw new InvalidOperationException("Customer record not found or access denied.");
            }

            if (string.IsNullOrWhiteSpace(proposedIncentive))
            {
                throw new ArgumentException("A proposed service incentive is required.", nameof(proposedIncentive));
            }

            if (string.IsNullOrWhiteSpace(retentionDetails))
            {
                throw new ArgumentException("Retention details and justification are required.", nameof(retentionDetails));
            }

            bool hasPending = await _db.RetentionRequests.AnyAsync(r => r.CustomerId == customerId && r.Status == "Pending");
            if (hasPending)
            {
                throw new InvalidOperationException("A pending retention request already exists for this customer.");
            }

            var request = new RetentionRequest
            {
                TenantId = TenantId,
                CustomerId = customerId,
                SubmittedByUserId = CurrentSession.UserId,
                AssignedAgentId = customer.AssignedAgentId,
                TargetSegment = string.IsNullOrWhiteSpace(targetSegment) ? customer.CurrentRetentionSegment : targetSegment,
                ActionType = string.IsNullOrWhiteSpace(actionType) ? "Incentive Offer" : actionType,
                ProposedIncentive = proposedIncentive.Trim(),
                ReasonCategory = string.IsNullOrWhiteSpace(reasonCategory) ? "Improve Customer Retention" : reasonCategory,
                RetentionDetails = retentionDetails.Trim(),
                Status = "Pending",
                AddedToCampaign = false,
                CreatedAt = DateTime.UtcNow
            };

            _db.RetentionRequests.Add(request);
            await _db.SaveChangesAsync();

            // Log to RetentionAuditLog
            await LogRetentionAuditAsync(
                "RequestSubmitted",
                customerId,
                customer.FullName,
                $"RequestId: {request.RequestId} | Segment: {request.TargetSegment} | Incentive: {request.ProposedIncentive} | Reason: {request.ReasonCategory}");

            return request;
        }

        public async Task<List<RetentionRequestRow>> GetRetentionRequestsAsync(
            string? statusFilter = null,
            DateTime? fromDate = null,
            DateTime? toDate = null)
        {
            AssertTenantAccess();

            var query = _db.RetentionRequests
                .Include(r => r.Customer)
                .Include(r => r.SubmittedByUser)
                    .ThenInclude(u => u.Role)
                .Include(r => r.ReviewedByUser)
                .Include(r => r.AssignedAgent)
                .AsNoTracking();

            // Agent ownership filter
            if (!RbacService.HasFullOversight && RbacService.IsAgent)
            {
                int currentUserId = CurrentSession.UserId;
                query = query.Where(r => r.SubmittedByUserId == currentUserId || r.AssignedAgentId == currentUserId);
            }

            if (!string.IsNullOrWhiteSpace(statusFilter) && !statusFilter.Equals("All", StringComparison.OrdinalIgnoreCase))
            {
                query = query.Where(r => r.Status == statusFilter);
            }

            if (fromDate.HasValue)
            {
                query = query.Where(r => r.CreatedAt >= fromDate.Value);
            }

            if (toDate.HasValue)
            {
                var endOfDay = toDate.Value.Date.AddDays(1).AddTicks(-1);
                query = query.Where(r => r.CreatedAt <= endOfDay);
            }

            var list = await query.OrderByDescending(r => r.CreatedAt).ToListAsync();

            int currentUserIdSession = CurrentSession.UserId;
            var currentRole = CurrentSession.CurrentUser?.Role;

            var result = new List<RetentionRequestRow>();

            foreach (var r in list)
            {
                bool canApprove = false;
                string cannotReason = string.Empty;

                if (r.Status == "Pending")
                {
                    if (r.SubmittedByUserId == currentUserIdSession)
                    {
                        canApprove = false;
                        cannotReason = "Self-approval is forbidden. Another reviewer must approve this request.";
                    }
                    else if (currentRole == UserRole.SalesStaff)
                    {
                        canApprove = false;
                        cannotReason = "Agents do not have authorization to approve retention incentive requests.";
                    }
                    else if (currentRole == UserRole.Manager)
                    {
                        // Manager can approve agent submissions, but NOT submissions by other managers or admins
                        bool submitterIsAgent = r.SubmittedByUser?.Role?.RoleName?.Contains("Staff", StringComparison.OrdinalIgnoreCase) == true ||
                                                r.SubmittedByUser?.Role?.RoleName?.Contains("Agent", StringComparison.OrdinalIgnoreCase) == true;
                        if (submitterIsAgent)
                        {
                            canApprove = true;
                        }
                        else
                        {
                            canApprove = false;
                            cannotReason = "Manager submissions require executive Admin approval.";
                        }
                    }
                    else if (currentRole == UserRole.Admin)
                    {
                        // Admin can approve anything submitted by anyone else
                        canApprove = true;
                    }
                }
                else
                {
                    cannotReason = $"Request is already {r.Status}.";
                }

                result.Add(new RetentionRequestRow
                {
                    RequestId = r.RequestId,
                    CustomerId = r.CustomerId,
                    CustomerName = r.Customer?.FullName ?? "Unknown Client",
                    CustomerEmail = r.Customer?.Email ?? string.Empty,
                    SubmittedByUserId = r.SubmittedByUserId,
                    SubmitterName = r.SubmittedByUser?.FullName ?? "Unknown",
                    SubmitterRole = r.SubmittedByUser?.Role?.RoleName ?? "Staff",
                    AssignedAgentId = r.AssignedAgentId,
                    AssignedAgentName = r.AssignedAgent?.FullName ?? "Unassigned",
                    TargetSegment = r.TargetSegment,
                    ActionType = r.ActionType,
                    ProposedIncentive = r.ProposedIncentive,
                    ReasonCategory = r.ReasonCategory,
                    RetentionDetails = r.RetentionDetails,
                    Status = r.Status,
                    ReviewedByUserId = r.ReviewedByUserId,
                    ReviewerName = r.ReviewedByUser?.FullName,
                    ReviewedAt = r.ReviewedAt,
                    ReviewerRemarks = r.ReviewerRemarks,
                    RejectionReason = r.RejectionReason,
                    AddedToCampaign = r.AddedToCampaign,
                    CreatedAt = r.CreatedAt,
                    CanCurrentUserApprove = canApprove,
                    CannotApproveReason = cannotReason
                });
            }

            return result;
        }

        public async Task ApproveRetentionRequestAsync(int requestId, string reviewerRemarks)
        {
            AssertTenantAccess();

            var request = await _db.RetentionRequests
                .Include(r => r.Customer)
                .Include(r => r.SubmittedByUser)
                    .ThenInclude(u => u.Role)
                .Include(r => r.AssignedAgent)
                .FirstOrDefaultAsync(r => r.RequestId == requestId);

            if (request == null)
            {
                throw new InvalidOperationException("Retention request not found.");
            }

            if (request.Status != "Pending")
            {
                throw new InvalidOperationException($"Request #{requestId} has already been reviewed ({request.Status}).");
            }

            int currentUserId = CurrentSession.UserId;
            var currentRole = CurrentSession.CurrentUser?.Role;

            // Self-approval prevention
            if (request.SubmittedByUserId == currentUserId)
            {
                throw new InvalidOperationException("Self-Approval Prevention: You cannot approve a request you submitted.");
            }

            if (currentRole == UserRole.SalesStaff)
            {
                throw new UnauthorizedAccessException("Agents are not authorized to approve retention requests.");
            }

            if (currentRole == UserRole.Manager)
            {
                bool submitterIsAgent = request.SubmittedByUser?.Role?.RoleName?.Contains("Staff", StringComparison.OrdinalIgnoreCase) == true ||
                                        request.SubmittedByUser?.Role?.RoleName?.Contains("Agent", StringComparison.OrdinalIgnoreCase) == true;
                if (!submitterIsAgent)
                {
                    throw new UnauthorizedAccessException("Manager-submitted requests require Admin approval.");
                }
            }

            // Mark request approved
            request.Status = "Approved";
            request.ReviewedByUserId = currentUserId;
            request.ReviewedAt = DateTime.UtcNow;
            request.ReviewerRemarks = string.IsNullOrWhiteSpace(reviewerRemarks) ? "Approved per retention policy." : reviewerRemarks.Trim();

            // Idempotency: Auto-queue email campaign
            if (!request.AddedToCampaign)
            {
                request.AddedToCampaign = true;

                // Build email from template for this segment
                var template = !string.IsNullOrWhiteSpace(request.TargetSegment)
                    ? await _db.EmailTemplates
                        .AsNoTracking()
                        .FirstOrDefaultAsync(t => t.Category == "Retention" && t.Name != null && t.Name.Contains(request.TargetSegment))
                    : null;

                template ??= await _db.EmailTemplates.AsNoTracking().FirstOrDefaultAsync(t => t.Category == "Retention");

                string subject = template?.Subject ?? "Exclusive Client Privilege from NEXA Real Estate Advisory";
                string body = template?.Body ?? "Dear {{customer_name}},\n\nAs a valued client, we are pleased to offer you: {{proposed_incentive}}.\n\nWarm regards,\n{{agent_name}}\nNEXA Real Estate Advisory";

                string clientName = request.Customer?.FirstName ?? "Valued Client";
                string agentName = request.AssignedAgent?.FullName ?? CurrentSession.CurrentUser?.FullName ?? "NEXA Real Estate Advisory";

                subject = subject.Replace("{{customer_name}}", clientName)
                                 .Replace("{{proposed_incentive}}", request.ProposedIncentive);

                body = body.Replace("{{customer_name}}", clientName)
                           .Replace("{{proposed_incentive}}", request.ProposedIncentive)
                           .Replace("{{agent_name}}", agentName);

                var queueItem = new RetentionEmailLog
                {
                    TenantId = TenantId,
                    CustomerId = request.CustomerId,
                    RetentionRequestId = request.RequestId,
                    RecipientEmail = request.Customer?.Email ?? string.Empty,
                    RecipientName = request.Customer?.FullName ?? clientName,
                    Segment = request.TargetSegment,
                    Subject = subject,
                    Body = body,
                    IncentiveOffered = request.ProposedIncentive,
                    Status = "Queued",
                    GenerationSource = "AutomatedRequest",
                    CreatedAt = DateTime.UtcNow
                };

                _db.RetentionEmailLogs.Add(queueItem);
            }

            await _db.SaveChangesAsync();

            await LogRetentionAuditAsync(
                "RequestApproved",
                request.CustomerId,
                request.Customer?.FullName,
                $"RequestId: {requestId} | Incentive: {request.ProposedIncentive} | AddedToCampaign: True");
        }

        public async Task RejectRetentionRequestAsync(int requestId, string rejectionReason, string reviewerRemarks)
        {
            AssertTenantAccess();

            if (string.IsNullOrWhiteSpace(rejectionReason))
            {
                throw new ArgumentException("A specific reason for rejection is mandatory.", nameof(rejectionReason));
            }

            var request = await _db.RetentionRequests
                .Include(r => r.Customer)
                .FirstOrDefaultAsync(r => r.RequestId == requestId);

            if (request == null)
            {
                throw new InvalidOperationException("Retention request not found.");
            }

            if (request.Status != "Pending")
            {
                throw new InvalidOperationException($"Request #{requestId} is already {request.Status}.");
            }

            var currentRole = CurrentSession.CurrentUser?.Role;
            if (currentRole == UserRole.SalesStaff)
            {
                throw new UnauthorizedAccessException("Agents are not authorized to review retention requests.");
            }

            int currentUserId = CurrentSession.UserId;

            if (request.SubmittedByUserId == currentUserId)
            {
                throw new InvalidOperationException("Self-Approval Prevention: You cannot review your own request.");
            }

            request.Status = "Rejected";
            request.ReviewedByUserId = currentUserId;
            request.ReviewedAt = DateTime.UtcNow;
            request.RejectionReason = rejectionReason.Trim();
            request.ReviewerRemarks = string.IsNullOrWhiteSpace(reviewerRemarks) ? null : reviewerRemarks.Trim();

            await _db.SaveChangesAsync();

            await LogRetentionAuditAsync(
                "RequestRejected",
                request.CustomerId,
                request.Customer?.FullName,
                $"RequestId: {requestId} | RejectionReason: {request.RejectionReason}");
        }

        #endregion

        #region Campaign Queue & Dispatch

        public async Task<List<RetentionQueueRow>> GetCampaignQueueAsync(
            string? statusFilter = null,
            DateTime? fromDate = null,
            DateTime? toDate = null)
        {
            AssertTenantAccess();

            var query = _db.RetentionEmailLogs
                .Include(l => l.Customer)
                .Include(l => l.DispatchedByUser)
                .AsNoTracking();

            // Ownership scoping
            if (!RbacService.HasFullOversight && RbacService.IsAgent)
            {
                int currentUserId = CurrentSession.UserId;
                query = query.Where(l => l.Customer.AssignedAgentId == currentUserId || l.DispatchedByUserId == currentUserId);
            }

            if (!string.IsNullOrWhiteSpace(statusFilter) && !statusFilter.Equals("All", StringComparison.OrdinalIgnoreCase))
            {
                query = query.Where(l => l.Status == statusFilter);
            }

            if (fromDate.HasValue)
            {
                query = query.Where(l => l.CreatedAt >= fromDate.Value);
            }

            if (toDate.HasValue)
            {
                var endOfDay = toDate.Value.Date.AddDays(1).AddTicks(-1);
                query = query.Where(l => l.CreatedAt <= endOfDay);
            }

            var list = await query.OrderByDescending(l => l.CreatedAt).ToListAsync();

            return list.Select(l => new RetentionQueueRow
            {
                EmailLogId = l.EmailLogId,
                CustomerId = l.CustomerId,
                CustomerName = l.RecipientName,
                RecipientEmail = l.RecipientEmail,
                Segment = l.Segment,
                Subject = l.Subject,
                Body = l.Body,
                IncentiveOffered = l.IncentiveOffered,
                Status = l.Status,
                GenerationSource = l.GenerationSource,
                RetentionRequestId = l.RetentionRequestId,
                CreatedAt = l.CreatedAt,
                DispatchedAt = l.DispatchedAt,
                DispatchedByName = l.DispatchedByUser?.FullName,
                ErrorMessage = l.ErrorMessage,
                IsCooldownOverride = l.IsCooldownOverride,
                CooldownOverrideReason = l.CooldownOverrideReason
            }).ToList();
        }

        public async Task DispatchQueuedEmailAsync(
            int emailLogId,
            string editedSubject,
            string editedBody,
            bool overrideCooldown = false,
            string? overrideReason = null)
        {
            AssertTenantAccess();

            var log = await _db.RetentionEmailLogs
                .Include(l => l.Customer)
                .FirstOrDefaultAsync(l => l.EmailLogId == emailLogId);

            if (log == null)
            {
                throw new InvalidOperationException("Campaign queue entry not found.");
            }

            if (log.Status == "Dispatched")
            {
                throw new InvalidOperationException("This campaign email has already been dispatched.");
            }

            var customer = log.Customer;
            var now = DateTime.UtcNow;

            // Anti-fatigue cooldown check (30 days)
            if (customer.LastRetentionEmailSentAt.HasValue)
            {
                double daysSince = (now - customer.LastRetentionEmailSentAt.Value).TotalDays;
                if (daysSince < 30)
                {
                    if (!overrideCooldown)
                    {
                        int daysRemaining = (int)Math.Ceiling(30 - daysSince);
                        throw new InvalidOperationException(
                            $"Customer is currently in the 30-day anti-fatigue cooldown ({daysRemaining} days remaining). " +
                            $"A manual override with documented business reason is required to send.");
                    }

                    if (string.IsNullOrWhiteSpace(overrideReason))
                    {
                        throw new ArgumentException("A documented business rationale is mandatory when overriding the cooldown.", nameof(overrideReason));
                    }

                    log.IsCooldownOverride = true;
                    log.CooldownOverrideReason = overrideReason.Trim();

                    await LogRetentionAuditAsync(
                        "CooldownOverride",
                        customer.CustomerId,
                        customer.FullName,
                        $"EmailLogId: {log.EmailLogId} | DaysSinceLastEmail: {daysSince:F1} | Reason: {overrideReason.Trim()}");
                }
            }

            log.Subject = (editedSubject ?? log.Subject ?? string.Empty).Trim();
            log.Body = (editedBody ?? log.Body ?? string.Empty).Trim();

            // Dispatch via ContactEmailService
            var sendResult = await ContactEmailService.SendAsync(
                log.RecipientEmail,
                log.Subject,
                log.Body,
                isBodyHtml: true);

            if (sendResult.Success)
            {
                log.Status = "Dispatched";
                log.DispatchedAt = DateTime.UtcNow;
                log.DispatchedByUserId = CurrentSession.UserId;
                log.ErrorMessage = null;

                customer.LastRetentionEmailSentAt = DateTime.UtcNow;

                await _db.SaveChangesAsync();

                await LogRetentionAuditAsync(
                    "EmailDispatched",
                    customer.CustomerId,
                    customer.FullName,
                    $"EmailLogId: {log.EmailLogId} | Segment: {log.Segment} | Recipient: {log.RecipientEmail}");
            }
            else
            {
                log.Status = "Failed";
                log.ErrorMessage = sendResult.Message;
                await _db.SaveChangesAsync();

                await LogRetentionAuditAsync(
                    "EmailFailed",
                    customer.CustomerId,
                    customer.FullName,
                    $"EmailLogId: {log.EmailLogId} | Error: {sendResult.Message}");

                throw new InvalidOperationException($"Email dispatch failed: {sendResult.Message}");
            }
        }

        public async Task<int> GenerateAutomatedValuationCampaignsAsync(bool forceAll = false)
        {
            AssertTenantAccess();

            var settings = await _db.AutomatedEmailSettings.FirstOrDefaultAsync(s => s.TenantId == TenantId)
                ?? new AutomatedEmailSettings { TenantId = TenantId, IsEnabled = true };

            var query = _db.Deals
                .AsNoTracking()
                .Include(d => d.Customer)
                .Include(d => d.Property)
                .Include(d => d.Agent)
                .Where(d => d.Customer != null && !d.Customer.IsDeleted)
                .Where(d => d.Stage.ToLower() == "closed" || d.Stage.ToLower() == "closed-won" || d.Stage.ToLower() == "won");

            if (!RbacService.HasFullOversight && RbacService.IsAgent)
            {
                int currentUserId = CurrentSession.UserId;
                query = query.Where(d => d.AgentId == currentUserId);
            }

            if (settings.TargetAudience.Equals("Buyers", StringComparison.OrdinalIgnoreCase))
                query = query.Where(d => d.Customer != null && d.Customer.Type != null && d.Customer.Type.ToLower() == "buyer");
            else if (settings.TargetAudience.Equals("Sellers", StringComparison.OrdinalIgnoreCase))
                query = query.Where(d => d.Customer != null && d.Customer.Type != null && d.Customer.Type.ToLower() == "seller");

            var deals = await query
                .OrderByDescending(d => d.ContractSignedDate ?? d.CreatedAt)
                .ToListAsync();

            var latestDealsByCustomer = deals
                .GroupBy(d => d.CustomerId)
                .Select(g => g.First())
                .ToList();

            var now = DateTime.UtcNow;
            int enqueuedCount = 0;

            var activeQueuedCustomerIds = await _db.RetentionEmailLogs
                .Where(l => l.Status == "Queued")
                .Select(l => l.CustomerId)
                .ToListAsync();

            int effectiveFrequency = Math.Clamp(settings.FrequencyDays, 1, 365);

            foreach (var deal in latestDealsByCustomer)
            {
                var customer = deal.Customer;
                if (customer == null || string.IsNullOrWhiteSpace(customer.Email)) continue;

                if (activeQueuedCustomerIds.Contains(customer.CustomerId)) continue;

                if (!forceAll && customer.LastRetentionEmailSentAt.HasValue)
                {
                    double daysSince = (now - customer.LastRetentionEmailSentAt.Value).TotalDays;
                    if (daysSince < effectiveFrequency) continue;
                }

                var metrics = MarketUpdateBackgroundService.CalculateValuation(settings, deal);
                var (subject, body) = settings.EmailFormat == "Html"
                    ? MarketUpdateBackgroundService.FormatHtmlMessage(settings, deal, metrics)
                    : MarketUpdateBackgroundService.FormatPlainTextMessage(settings, deal, metrics);

                var log = new RetentionEmailLog
                {
                    TenantId = TenantId,
                    CustomerId = customer.CustomerId,
                    RecipientName = customer.FullName,
                    RecipientEmail = customer.Email,
                    Segment = string.IsNullOrWhiteSpace(customer.CurrentRetentionSegment) ? "Recent Client" : customer.CurrentRetentionSegment,
                    Subject = subject,
                    Body = body,
                    IncentiveOffered = $"{settings.AnnualAppreciationRatePercent:F1}% Benchmark Equity Update",
                    Status = "Queued",
                    GenerationSource = "AutomatedValuation",
                    CreatedAt = now
                };

                _db.RetentionEmailLogs.Add(log);
                activeQueuedCustomerIds.Add(customer.CustomerId);
                enqueuedCount++;
            }

            if (enqueuedCount > 0)
            {
                await _db.SaveChangesAsync();

                await LogRetentionAuditAsync(
                    "AutomatedCampaignsGenerated",
                    null,
                    "Automated Valuation Engine",
                    $"Generated {enqueuedCount} queued valuation campaigns based on benchmark {settings.AnnualAppreciationRatePercent}% / {settings.FrequencyDays}d cadence.");
            }

            return enqueuedCount;
        }

        public async Task<(int Dispatched, int Failed, int SkippedCooldown)> DispatchAllQueuedCampaignEmailsAsync(
            bool overrideCooldown = false,
            string? overrideReason = null)
        {
            AssertTenantAccess();

            var queuedLogs = await _db.RetentionEmailLogs
                .Include(l => l.Customer)
                .Where(l => l.Status == "Queued")
                .OrderBy(l => l.CreatedAt)
                .ToListAsync();

            int dispatched = 0;
            int failed = 0;
            int skippedCooldown = 0;

            foreach (var log in queuedLogs)
            {
                try
                {
                    if (log.Customer.LastRetentionEmailSentAt.HasValue)
                    {
                        double daysSince = (DateTime.UtcNow - log.Customer.LastRetentionEmailSentAt.Value).TotalDays;
                        if (daysSince < 30 && !overrideCooldown)
                        {
                            skippedCooldown++;
                            continue;
                        }
                    }

                    await DispatchQueuedEmailAsync(log.EmailLogId, log.Subject, log.Body, overrideCooldown, overrideReason);
                    dispatched++;
                }
                catch
                {
                    failed++;
                }
            }

            return (dispatched, failed, skippedCooldown);
        }

        public async Task SendManualRetentionEmailAsync(
            int customerId,
            string subject,
            string body,
            string? incentiveOffered = null,
            bool overrideCooldown = false,
            string? overrideReason = null)
        {
            AssertTenantAccess();

            var customer = await GetCustomerByIdAsync(customerId);
            if (customer == null)
            {
                throw new InvalidOperationException("Customer not found or access denied.");
            }

            if (string.IsNullOrWhiteSpace(customer.Email))
            {
                throw new InvalidOperationException("Customer has no registered email address.");
            }

            var now = DateTime.UtcNow;
            bool isOverride = false;

            if (customer.LastRetentionEmailSentAt.HasValue)
            {
                double daysSince = (now - customer.LastRetentionEmailSentAt.Value).TotalDays;
                if (daysSince < 30)
                {
                    if (!overrideCooldown)
                    {
                        int daysRemaining = (int)Math.Ceiling(30 - daysSince);
                        throw new InvalidOperationException(
                            $"Customer is currently in the 30-day anti-fatigue cooldown ({daysRemaining} days remaining). " +
                            $"A documented override rationale is required.");
                    }

                    if (string.IsNullOrWhiteSpace(overrideReason))
                    {
                        throw new ArgumentException("A documented business rationale is mandatory when overriding the cooldown.", nameof(overrideReason));
                    }

                    isOverride = true;
                }
            }

            var sendResult = await ContactEmailService.SendAsync(
                customer.Email,
                subject.Trim(),
                body.Trim(),
                isBodyHtml: true);

            var emailLog = new RetentionEmailLog
            {
                TenantId = TenantId,
                CustomerId = customer.CustomerId,
                RecipientEmail = customer.Email,
                RecipientName = customer.FullName,
                Segment = customer.CurrentRetentionSegment,
                Subject = subject.Trim(),
                Body = body.Trim(),
                IncentiveOffered = incentiveOffered,
                Status = sendResult.Success ? "Dispatched" : "Failed",
                GenerationSource = "ManualSend",
                DispatchedByUserId = CurrentSession.UserId,
                DispatchedAt = sendResult.Success ? DateTime.UtcNow : null,
                ErrorMessage = sendResult.Success ? null : sendResult.Message,
                IsCooldownOverride = isOverride,
                CooldownOverrideReason = overrideReason?.Trim(),
                CreatedAt = DateTime.UtcNow
            };

            _db.RetentionEmailLogs.Add(emailLog);

            if (sendResult.Success)
            {
                customer.LastRetentionEmailSentAt = DateTime.UtcNow;
            }

            await _db.SaveChangesAsync();

            if (isOverride)
            {
                await LogRetentionAuditAsync(
                    "CooldownOverride",
                    customer.CustomerId,
                    customer.FullName,
                    $"ManualSend | Reason: {overrideReason?.Trim()}");
            }

            await LogRetentionAuditAsync(
                sendResult.Success ? "EmailDispatched" : "EmailFailed",
                customer.CustomerId,
                customer.FullName,
                $"ManualSend | Recipient: {customer.Email} | Status: {emailLog.Status}");

            if (!sendResult.Success)
            {
                throw new InvalidOperationException($"Email dispatch failed: {sendResult.Message}");
            }
        }

        #endregion

        #region Audit Logging & Export

        public async Task<List<RetentionAuditLog>> GetAuditLogsAsync(
            DateTime? fromDate = null,
            DateTime? toDate = null,
            string? actionFilter = null)
        {
            AssertTenantAccess();

            var query = _db.RetentionAuditLogs.AsNoTracking();

            if (!string.IsNullOrWhiteSpace(actionFilter) && !actionFilter.Equals("All", StringComparison.OrdinalIgnoreCase))
            {
                query = query.Where(a => a.ActionType == actionFilter);
            }

            if (fromDate.HasValue)
            {
                query = query.Where(a => a.CreatedAt >= fromDate.Value);
            }

            if (toDate.HasValue)
            {
                var endOfDay = toDate.Value.Date.AddDays(1).AddTicks(-1);
                query = query.Where(a => a.CreatedAt <= endOfDay);
            }

            return await query.OrderByDescending(a => a.CreatedAt).Take(200).ToListAsync();
        }

        private async Task LogRetentionAuditAsync(
            string actionType,
            int? customerId,
            string? customerName,
            string detail)
        {
            try
            {
                var audit = new RetentionAuditLog
                {
                    TenantId = TenantId,
                    PerformedByUserId = CurrentSession.UserId,
                    PerformedByName = CurrentSession.CurrentUser?.FullName ?? "System",
                    UserRole = CurrentSession.CurrentUser?.Role.ToString() ?? "Agent",
                    ActionType = actionType,
                    TargetCustomerId = customerId,
                    TargetCustomerName = customerName,
                    Detail = detail,
                    CreatedAt = DateTime.UtcNow
                };

                _db.RetentionAuditLogs.Add(audit);
                await _db.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[RetentionController] Audit logging notice: {ex.Message}");
            }
        }

        public void ExportRetentionToPdf(List<RetentionCustomerRow> customers, string filePath)
        {
            AssertTenantAccess();

            string[] headers = new[] { "ID", "Customer Name", "Email", "Phone", "Segment", "Last Closed", "Last Activity", "Assigned Agent", "Cooldown" };
            var rows = customers.Select(c => new string[]
            {
                c.CustomerId.ToString(),
                c.FullName,
                c.Email ?? "—",
                c.Phone ?? "—",
                c.CurrentSegment,
                c.LastClosedDate?.ToString("yyyy-MM-dd") ?? "N/A",
                c.LastActivityDate?.ToString("yyyy-MM-dd") ?? "N/A",
                c.AssignedAgentName ?? "Unassigned",
                c.IsOnCooldown ? $"YES ({c.DaysUntilCooldownExpires}d)" : "NO"
            }).ToList();

            var kpis = new List<(string Title, string Value, string ColorHex)>
            {
                ("Total Clients", customers.Count.ToString(), "#25679C"),
                ("VIP Champions", customers.Count(x => string.Equals(x.CurrentSegment, "VIP Champion", StringComparison.OrdinalIgnoreCase)).ToString(), "#8B5CF6"),
                ("At Risk", customers.Count(x => string.Equals(x.CurrentSegment, "At Risk", StringComparison.OrdinalIgnoreCase)).ToString(), "#DC2626"),
                ("On Cooldown", customers.Count(x => x.IsOnCooldown).ToString(), "#D97706")
            };

            CRMS_Peguit.winforms.Services.PdfExportHelper.ExportTable("Client Retention & VIP Lifecycle Roster", headers, rows, filePath, kpis: kpis);
        }

        #endregion

        public void Dispose()
        {
            _db.Dispose();
        }
    }
}
