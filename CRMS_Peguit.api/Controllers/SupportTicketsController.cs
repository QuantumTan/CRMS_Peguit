using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using CRMS_Peguit.domain.entities;
using CRMS_Peguit.domain.Common;
using CRMS_Peguit.infrastructure.data;

namespace CRMS_Peguit.api.Controllers
{
    public record SupportTicketKpiDto(int Total, int Open, int InProgress, int Resolved, int Overdue);
    public record UpdateTicketStatusRequest(string NewStatus, string? Note = null);
    public record ReopenTicketRequest(string Reason);
    public record AssignTicketRequest(int? AgentId, string? Notes = null);
    public record AddTicketCommentRequest(string CommentText, bool IsInternal = true);
    public record TicketAttentionDto(int TicketId, string TicketNumber, string CustomerName, string Category, string Priority, string Status, string OpenedAgoText);

    [ApiController]
    [Route("api/[controller]")]
    public class SupportTicketsController : ControllerBase
    {
        private readonly RealEstateDbContext _db;

        public SupportTicketsController(RealEstateDbContext db)
        {
            _db = db;
        }

        private (int UserId, string Role, int TenantId) CurrentUser =>
            ApiSecurityHelper.GetCurrentUserInfo(HttpContext);

        [HttpGet]
        public async Task<IActionResult> GetAll(
            [FromQuery] int? page = null,
            [FromQuery] int? pageSize = null,
            [FromQuery] string? search = null,
            [FromQuery] string? status = null)
        {
            var user = CurrentUser;
            var query = _db.SupportTickets
                .Include(t => t.Customer).ThenInclude(c => c!.Person)
                .Include(t => t.AssignedToUser).ThenInclude(u => u!.Person)
                .AsQueryable();

            // Ownership-Based Access Control:
            if (!ApiSecurityHelper.HasFullOversight(user.Role) && ApiSecurityHelper.IsAgent(user.Role) && user.UserId > 0)
            {
                query = query.Where(t =>
                    (t.AssignedToUserId.HasValue && t.AssignedToUserId.Value > 0)
                        ? t.AssignedToUserId.Value == user.UserId
                        : t.RaisedByUserId == user.UserId);
            }

            if (!string.IsNullOrWhiteSpace(status) && !status.Equals("All", StringComparison.OrdinalIgnoreCase))
            {
                if (status.Equals("Overdue", StringComparison.OrdinalIgnoreCase))
                {
                    var now = DateTime.UtcNow;
                    query = query.Where(t => t.Status != "Resolved" && t.DueDate.HasValue && t.DueDate.Value < now);
                }
                else
                {
                    query = query.Where(t => t.Status.ToLower() == status.ToLower());
                }
            }

            if (!string.IsNullOrWhiteSpace(search))
            {
                string s = search.Trim();
                query = query.Where(t =>
                    t.TicketNumber.Contains(s) ||
                    t.Category.Contains(s) ||
                    t.Description.Contains(s) ||
                    (t.Customer != null && (t.Customer.Person.FirstName.Contains(s) || t.Customer.Person.LastName.Contains(s) || (t.Customer.Person.Email != null && t.Customer.Person.Email.Contains(s)))));
            }

            if (page.HasValue || pageSize.HasValue)
            {
                int pageNum = Math.Max(1, page.GetValueOrDefault(1));
                int size = Math.Max(1, pageSize.GetValueOrDefault(25));

                int totalCount = await query.CountAsync();
                var pagedList = await query.OrderByDescending(t => t.CreatedAt)
                    .Skip((pageNum - 1) * size)
                    .Take(size)
                    .ToListAsync();

                return Ok(new PagedResult<SupportTicket>(pagedList, totalCount, pageNum, size));
            }

            var items = await query.OrderByDescending(t => t.CreatedAt).ToListAsync();
            return Ok(items);
        }

        [HttpGet("kpi-counts")]
        public async Task<IActionResult> GetKpiCounts()
        {
            var user = CurrentUser;
            var query = _db.SupportTickets.AsNoTracking().AsQueryable();

            if (!ApiSecurityHelper.HasFullOversight(user.Role) && ApiSecurityHelper.IsAgent(user.Role) && user.UserId > 0)
            {
                query = query.Where(t =>
                    (t.AssignedToUserId.HasValue && t.AssignedToUserId.Value > 0)
                        ? t.AssignedToUserId.Value == user.UserId
                        : t.RaisedByUserId == user.UserId);
            }

            var now = DateTime.UtcNow;
            int total = await query.CountAsync();
            int open = await query.CountAsync(t => t.Status.ToLower() == "open");
            int inProgress = await query.CountAsync(t => t.Status.ToLower() == "in progress" || t.Status.ToLower() == "in_progress");
            int resolved = await query.CountAsync(t => t.Status.ToLower() == "resolved");
            int overdue = await query.CountAsync(t => t.Status.ToLower() != "resolved" && t.DueDate.HasValue && t.DueDate.Value < now);

            return Ok(new SupportTicketKpiDto(total, open, inProgress, resolved, overdue));
        }

        [HttpGet("{id:int}")]
        public async Task<IActionResult> GetById(int id)
        {
            var user = CurrentUser;
            var item = await _db.SupportTickets
                .Include(t => t.Customer).ThenInclude(c => c!.Person)
                .Include(t => t.AssignedToUser).ThenInclude(u => u!.Person)
                .Include(t => t.Comments)
                .SingleOrDefaultAsync(t => t.TicketId == id);

            if (item is null) return NotFound();

            if (!ApiSecurityHelper.HasFullOversight(user.Role) && ApiSecurityHelper.IsAgent(user.Role) && user.UserId > 0)
            {
                bool allowed = (item.AssignedToUserId.HasValue && item.AssignedToUserId.Value > 0)
                    ? item.AssignedToUserId.Value == user.UserId
                    : item.RaisedByUserId == user.UserId;
                if (!allowed) return Forbid();
            }

            return Ok(item);
        }

        [HttpPost]
        public async Task<IActionResult> Create(SupportTicket ticket)
        {
            var user = CurrentUser;
            ticket.RaisedByUserId = user.UserId > 0 ? user.UserId : (ticket.RaisedByUserId > 0 ? ticket.RaisedByUserId : 1);
            ticket.CreatedAt = DateTime.UtcNow;
            ticket.AssignedToUserId = null; // Default unassigned
            if (string.IsNullOrWhiteSpace(ticket.Category)) ticket.Category = "Other";
            if (string.IsNullOrWhiteSpace(ticket.Priority)) ticket.Priority = "Medium";
            if (string.IsNullOrWhiteSpace(ticket.Status)) ticket.Status = "Open";

            ticket.DueDate = (ticket.Priority.ToLower()) switch
            {
                "high" or "urgent" => ticket.CreatedAt.AddHours(24),
                "medium" => ticket.CreatedAt.AddDays(3),
                "low" => ticket.CreatedAt.AddDays(7),
                _ => ticket.CreatedAt.AddDays(3)
            };

            ticket.TicketNumber = "TCK-TEMP";
            _db.SupportTickets.Add(ticket);
            await _db.SaveChangesAsync();

            ticket.TicketNumber = $"TCK-{ticket.TicketId:D5}";
            await _db.SaveChangesAsync();

            return CreatedAtAction(nameof(GetById), new { id = ticket.TicketId }, ticket);
        }

        [HttpPut("{id:int}")]
        public async Task<IActionResult> Update(int id, SupportTicket updated)
        {
            var user = CurrentUser;
            var item = await _db.SupportTickets.FindAsync(id);
            if (item is null) return NotFound();

            if (!ApiSecurityHelper.HasFullOversight(user.Role) && ApiSecurityHelper.IsAgent(user.Role) && user.UserId > 0)
            {
                bool allowed = (item.AssignedToUserId.HasValue && item.AssignedToUserId.Value > 0)
                    ? item.AssignedToUserId.Value == user.UserId
                    : item.RaisedByUserId == user.UserId;
                if (!allowed) return Forbid();
            }

            item.CustomerId = updated.CustomerId;
            item.Category = updated.Category;
            item.Description = updated.Description;
            item.Priority = updated.Priority;
            item.Status = updated.Status;
            item.DueDate = updated.DueDate;
            item.FirstRespondedAt = updated.FirstRespondedAt;
            item.ResolvedAt = updated.ResolvedAt;

            if (ApiSecurityHelper.CanAssignRecords(user.Role))
            {
                item.AssignedToUserId = updated.AssignedToUserId;
            }

            await _db.SaveChangesAsync();
            return Ok(item);
        }

        [HttpPut("{id:int}/status")]
        public async Task<IActionResult> UpdateStatus(int id, [FromBody] UpdateTicketStatusRequest req)
        {
            var item = await _db.SupportTickets.FindAsync(id);
            if (item is null) return NotFound();

            item.Status = req.NewStatus;
            if (string.Equals(req.NewStatus, "Resolved", StringComparison.OrdinalIgnoreCase))
            {
                item.ResolvedAt = DateTime.UtcNow;
            }
            if (item.FirstRespondedAt == null)
            {
                item.FirstRespondedAt = DateTime.UtcNow;
            }

            if (!string.IsNullOrWhiteSpace(req.Note))
            {
                _db.TicketComments.Add(new TicketComment
                {
                    TicketId = id,
                    AuthorUserId = CurrentUser.UserId > 0 ? CurrentUser.UserId : 1,
                    CommentText = $"[Status Changed to {req.NewStatus}] {req.Note}",
                    IsInternal = true,
                    CreatedAt = DateTime.UtcNow
                });
            }

            await _db.SaveChangesAsync();
            return Ok(item);
        }

        [HttpPost("{id:int}/reopen")]
        public async Task<IActionResult> Reopen(int id, [FromBody] ReopenTicketRequest req)
        {
            var item = await _db.SupportTickets.FindAsync(id);
            if (item is null) return NotFound();

            item.Status = "Open";
            item.ResolvedAt = null;

            _db.TicketComments.Add(new TicketComment
            {
                TicketId = id,
                AuthorUserId = CurrentUser.UserId > 0 ? CurrentUser.UserId : 1,
                CommentText = $"[Ticket Reopened] {req.Reason}",
                IsInternal = true,
                CreatedAt = DateTime.UtcNow
            });

            await _db.SaveChangesAsync();
            return Ok(item);
        }

        [HttpPost("{id:int}/assign")]
        public async Task<IActionResult> AssignTo(int id, [FromBody] AssignTicketRequest req)
        {
            var user = CurrentUser;
            if (!ApiSecurityHelper.CanAssignRecords(user.Role))
                return StatusCode(StatusCodes.Status403Forbidden, "Only Manager or Admin may assign tickets.");

            var item = await _db.SupportTickets.FindAsync(id);
            if (item is null) return NotFound();

            var newAgentId = req.AgentId <= 0 ? null : req.AgentId;
            item.AssignedToUserId = newAgentId;

            if (!string.IsNullOrWhiteSpace(req.Notes))
            {
                _db.TicketComments.Add(new TicketComment
                {
                    TicketId = id,
                    AuthorUserId = user.UserId > 0 ? user.UserId : 1,
                    CommentText = $"[Assignment Note] {req.Notes}",
                    IsInternal = true,
                    CreatedAt = DateTime.UtcNow
                });
            }

            await _db.SaveChangesAsync();
            return Ok(item);
        }

        [HttpGet("{id:int}/comments")]
        public async Task<IActionResult> GetComments(int id)
        {
            var comments = await _db.TicketComments
                .Include(c => c.AuthorUser).ThenInclude(u => u!.Person)
                .Where(c => c.TicketId == id)
                .OrderBy(c => c.CreatedAt)
                .ToListAsync();
            return Ok(comments);
        }

        [HttpPost("{id:int}/comments")]
        public async Task<IActionResult> AddComment(int id, [FromBody] AddTicketCommentRequest req)
        {
            var user = CurrentUser;
            var ticket = await _db.SupportTickets.FindAsync(id);
            if (ticket is null) return NotFound();

            if (ticket.FirstRespondedAt == null)
            {
                ticket.FirstRespondedAt = DateTime.UtcNow;
            }

            var comment = new TicketComment
            {
                TicketId = id,
                AuthorUserId = user.UserId > 0 ? user.UserId : 1,
                CommentText = req.CommentText,
                IsInternal = req.IsInternal,
                CreatedAt = DateTime.UtcNow
            };

            _db.TicketComments.Add(comment);
            await _db.SaveChangesAsync();

            return Ok(comment);
        }

        [HttpDelete("{id:int}")]
        public async Task<IActionResult> Delete(int id)
        {
            var user = CurrentUser;
            var item = await _db.SupportTickets.FindAsync(id);
            if (item is null) return NotFound();

            if (!ApiSecurityHelper.HasFullOversight(user.Role) && ApiSecurityHelper.IsAgent(user.Role) && user.UserId > 0)
            {
                bool allowed = (item.AssignedToUserId.HasValue && item.AssignedToUserId.Value > 0)
                    ? item.AssignedToUserId.Value == user.UserId
                    : item.RaisedByUserId == user.UserId;
                if (!allowed) return Forbid();
            }

            _db.SupportTickets.Remove(item);
            await _db.SaveChangesAsync();
            return NoContent();
        }

        [HttpGet("customers")]
        public async Task<IActionResult> GetCustomers()
        {
            var customers = await _db.Customers
                .AsNoTracking()
                .Include(c => c.Person)
                .Where(c => !c.IsDeleted)
                .OrderBy(c => c.Person.LastName)
                .ThenBy(c => c.Person.FirstName)
                .ToListAsync();
            return Ok(customers);
        }

        [HttpGet("agents")]
        public async Task<IActionResult> GetAgents()
        {
            var agentRoleIds = await _db.Roles
                .AsNoTracking()
                .Where(r => r.RoleName.ToLower() == "agent" || r.RoleName.ToLower() == "salesstaff")
                .Select(r => r.RoleId)
                .ToListAsync();

            var agents = await _db.Users
                .AsNoTracking()
                .Include(u => u.Person)
                .Where(u => agentRoleIds.Contains(u.RoleId) && u.Status.ToLower() != "inactive")
                .OrderBy(u => u.Person.LastName)
                .ThenBy(u => u.Person.FirstName)
                .Select(u => new AgentPickerDto(u.UserId, u.FullName, u.Email))
                .ToListAsync();

            return Ok(agents);
        }

        [HttpGet("agent-dict")]
        public async Task<IActionResult> GetAgentDictionary()
        {
            var dict = await _db.Users
                .AsNoTracking()
                .Include(u => u.Person)
                .ToDictionaryAsync(u => u.UserId, u => u.FullName);
            return Ok(dict);
        }

        [HttpGet("stats/open-count")]
        public async Task<IActionResult> GetOpenTicketsCount()
        {
            var user = CurrentUser;
            var query = _db.SupportTickets.AsNoTracking().Where(t => t.Status.ToLower() != "resolved");

            if (!ApiSecurityHelper.HasFullOversight(user.Role) && ApiSecurityHelper.IsAgent(user.Role) && user.UserId > 0)
            {
                query = query.Where(t =>
                    (t.AssignedToUserId.HasValue && t.AssignedToUserId.Value > 0)
                        ? t.AssignedToUserId.Value == user.UserId
                        : t.RaisedByUserId == user.UserId);
            }

            int count = await query.CountAsync();
            return Ok(new { count });
        }

        [HttpGet("attention")]
        public async Task<IActionResult> GetTicketsNeedingAttention([FromQuery] int maxCount = 3)
        {
            var now = DateTime.UtcNow;
            var list = await _db.SupportTickets
                .AsNoTracking()
                .Include(t => t.Customer).ThenInclude(c => c!.Person)
                .Where(t => t.Status.ToLower() != "resolved")
                .OrderBy(t => t.CreatedAt)
                .Take(maxCount)
                .ToListAsync();

            var dtos = list.Select(t =>
            {
                var span = now - t.CreatedAt;
                string ago = span.TotalHours < 1 ? $"{(int)span.TotalMinutes}m ago" :
                             span.TotalHours < 24 ? $"{(int)span.TotalHours}h ago" :
                             $"{(int)span.TotalDays}d ago";

                return new TicketAttentionDto(
                    t.TicketId,
                    t.TicketNumber,
                    t.Customer?.FullName ?? "Unknown",
                    t.Category,
                    t.Priority,
                    t.Status,
                    ago);
            }).ToList();

            return Ok(dtos);
        }
    }
}