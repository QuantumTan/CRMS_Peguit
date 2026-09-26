using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using CRMS_Peguit.domain.Common;

namespace CRMS_Peguit.winforms.Services
{
    public class SuperAdminApiService : BaseApiService
    {
        public SuperAdminApiService(string? baseUrl = null, HttpClient? httpClient = null)
            : base(baseUrl, httpClient)
        {
        }

        public async Task<PlatformSnapshotDto> GetPlatformSnapshotAsync() =>
            await GetAsync<PlatformSnapshotDto>("api/superadmin/snapshot") ?? new PlatformSnapshotDto();

        public PlatformSnapshotDto GetPlatformSnapshot() =>
            GetPlatformSnapshotAsync().GetAwaiter().GetResult();

        public async Task<List<AdminDto>> GetAdministratorsAsync() =>
            await GetAsync<List<AdminDto>>("api/superadmin/administrators") ?? new();

        public List<AdminDto> GetAdministrators() =>
            GetAdministratorsAsync().GetAwaiter().GetResult();

        public async Task<(bool Success, string Error)> CreateAdminAsync(
            int tenantId, string firstName, string lastName, string email, string password, string roleName)
        {
            var req = new CreateAdminRequest
            {
                TenantId = tenantId,
                FirstName = firstName,
                LastName = lastName,
                Email = email,
                Password = password,
                RoleName = roleName
            };
            var success = await PostAsync("api/superadmin/administrators", req);
            return (success, success ? string.Empty : "Failed to create administrator");
        }

        public async Task<bool> DeactivateAdminAsync(int userId, int tenantId) =>
            await PostAsync($"api/superadmin/administrators/{userId}/deactivate?tenantId={tenantId}", new { });

        public async Task<bool> ActivateAdminAsync(int userId, int tenantId) =>
            await PostAsync($"api/superadmin/administrators/{userId}/activate?tenantId={tenantId}", new { });

        public async Task<CompanyDetailDto?> GetCompanyDetailAsync(int companyId) =>
            await GetAsync<CompanyDetailDto>($"api/superadmin/companies/{companyId}");

        public async Task<List<CompanyLookupDto>> GetCompaniesAsync() =>
            await GetAsync<List<CompanyLookupDto>>("api/superadmin/companies") ?? new();

        public async Task<List<SystemSettingDto>> GetSystemSettingsAsync() =>
            await GetAsync<List<SystemSettingDto>>("api/superadmin/settings") ?? new();

        public async Task<bool> UpdateSystemSettingAsync(int settingId, string newValue, int updatedByUserId) =>
            await PutAsync($"api/superadmin/settings/{settingId}", new UpdateSettingRequest { Value = newValue });

        public async Task<List<BackupLogDto>> GetBackupHistoryAsync() =>
            await GetAsync<List<BackupLogDto>>("api/superadmin/backups") ?? new();

        public async Task<(bool Success, BackupLogDto? Log)> RunBackupAsync(int performedByUserId)
        {
            var log = await PostAsync<object, BackupLogDto>("api/superadmin/backups/run", new { });
            return (log != null, log);
        }

        public async Task<BackupLogDto?> GetBackupForRestoreAsync(int backupId) =>
            await GetAsync<BackupLogDto>($"api/superadmin/backups/{backupId}");

        public async Task<bool> RestoreBackupAsync(int backupId, int performedByUserId) =>
            await PostAsync($"api/superadmin/backups/{backupId}/restore", new { });
    }
}
