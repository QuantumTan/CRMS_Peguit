using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using CRMS_Peguit.domain.Common;
using CRMS_Peguit.domain.entities;

namespace CRMS_Peguit.winforms.Services
{
    public class FollowUpKpiCounts
    {
        public int Total { get; set; }
        public int Today { get; set; }
        public int Upcoming { get; set; }
        public int Overdue { get; set; }
        public int Completed { get; set; }
    }

    public class FollowUpApiService : BaseApiService
    {
        public FollowUpApiService(string? baseUrl = null, HttpClient? httpClient = null)
            : base(baseUrl, httpClient)
        {
        }

        public List<TaskReminder> GetAll() =>
            Get<List<TaskReminder>>("api/taskreminders") ?? new();

        public async Task<PagedResult<TaskReminder>> GetPagedAsync(
            int pageNumber = 1,
            int pageSize = 25,
            string? search = null,
            string? filterStatus = null,
            string? filterPriority = null,
            string? filterType = null,
            string? sortColumn = null,
            bool sortAscending = true)
        {
            var uri = $"api/taskreminders/paged?pageNumber={pageNumber}&pageSize={pageSize}";
            if (!string.IsNullOrWhiteSpace(search)) uri += $"&search={Uri.EscapeDataString(search)}";
            if (!string.IsNullOrWhiteSpace(filterStatus)) uri += $"&status={Uri.EscapeDataString(filterStatus)}";
            if (!string.IsNullOrWhiteSpace(filterPriority)) uri += $"&priority={Uri.EscapeDataString(filterPriority)}";
            if (!string.IsNullOrWhiteSpace(filterType)) uri += $"&type={Uri.EscapeDataString(filterType)}";
            if (!string.IsNullOrWhiteSpace(sortColumn)) uri += $"&sortColumn={Uri.EscapeDataString(sortColumn)}&sortAscending={sortAscending}";

            return await GetAsync<PagedResult<TaskReminder>>(uri) ?? new PagedResult<TaskReminder>();
        }

        public PagedResult<TaskReminder> GetPaged(
            int pageNumber = 1,
            int pageSize = 25,
            string? search = null,
            string? filterStatus = null,
            string? filterPriority = null,
            string? filterType = null,
            string? sortColumn = null,
            bool sortAscending = true) =>
            GetPagedAsync(pageNumber, pageSize, search, filterStatus, filterPriority, filterType, sortColumn, sortAscending).GetAwaiter().GetResult();

        public TaskReminder? GetById(int id) =>
            Get<TaskReminder>($"api/taskreminders/{id}");

        public TaskReminder Add(TaskReminder reminder)
        {
            var result = Post<TaskReminder, TaskReminder>("api/taskreminders", reminder);
            return result ?? reminder;
        }

        public void Update(TaskReminder reminder) =>
            Put<TaskReminder, TaskReminder>($"api/taskreminders/{reminder.TaskReminderId}", reminder);

        public void MarkComplete(int id, bool logActivity = false, string? activityNotes = null) =>
            Post($"api/taskreminders/{id}/complete", new { logActivity, activityNotes });

        public void Snooze(int id, TimeSpan interval) =>
            Post($"api/taskreminders/{id}/snooze", new { minutes = (int)interval.TotalMinutes });

        public void Reschedule(int id, DateTime newDueDate) =>
            Post($"api/taskreminders/{id}/reschedule", new { newDueDate });

        public void SoftDelete(int id) =>
            Delete($"api/taskreminders/{id}");

        public List<Customer> GetAssignedCustomers() =>
            Get<List<Customer>>("api/taskreminders/assigned-customers") ?? new();

        public List<Lead> GetAssignedLeads() =>
            Get<List<Lead>>("api/taskreminders/assigned-leads") ?? new();

        public FollowUpKpiCounts GetKpiCounts() =>
            Get<FollowUpKpiCounts>("api/taskreminders/kpis") ?? new FollowUpKpiCounts();

        public List<TaskReminder> GetFollowUpsDueToday(int maxCount = 5) =>
            Get<List<TaskReminder>>($"api/taskreminders/due-today?count={maxCount}") ?? new();

        public static bool ValidateInput(string title, int? customerId, int? leadId, out string? errorMessage)
        {
            if (string.IsNullOrWhiteSpace(title))
            {
                errorMessage = "Title is required.";
                return false;
            }

            if (!customerId.HasValue && !leadId.HasValue)
            {
                errorMessage = "Must be linked to either a customer or a lead.";
                return false;
            }

            errorMessage = null;
            return true;
        }
    }
}
