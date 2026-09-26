using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using CRMS_Peguit.domain.Common;
using CRMS_Peguit.domain.entities;

namespace CRMS_Peguit.winforms.Services
{
    public class UserApiService : BaseApiService
    {
        public UserApiService(string? baseUrl = null, HttpClient? httpClient = null)
            : base(baseUrl, httpClient)
        {
        }

        public async Task<List<User>> GetAllAsync(bool includeInactive = false) =>
            await GetAsync<List<User>>($"api/users?includeInactive={includeInactive}") ?? new();

        public List<User> GetAll(bool includeInactive = false) =>
            GetAllAsync(includeInactive).GetAwaiter().GetResult();

        public async Task<User?> GetByIdAsync(int id) =>
            await GetAsync<User>($"api/users/{id}");

        public User? GetById(int id) =>
            GetByIdAsync(id).GetAwaiter().GetResult();

        public async Task<List<Role>> GetManagedRolesAsync() =>
            await GetAsync<List<Role>>("api/users/roles") ?? new();

        public List<Role> GetManagedRoles() =>
            GetManagedRolesAsync().GetAwaiter().GetResult();

        public async Task CreateAsync(User user, string plainTextPassword) =>
            await PostAsync("api/users", new { user, plainTextPassword });

        public void Create(User user, string plainTextPassword) =>
            CreateAsync(user, plainTextPassword).GetAwaiter().GetResult();

        public async Task UpdateAsync(User user) =>
            await PutAsync($"api/users/{user.UserId}", user);

        public void Update(User user) =>
            UpdateAsync(user).GetAwaiter().GetResult();

        public async Task DeactivateAsync(int id) =>
            await PostAsync($"api/users/{id}/deactivate", new { });

        public void Deactivate(int id) =>
            DeactivateAsync(id).GetAwaiter().GetResult();

        public async Task ReactivateAsync(int id) =>
            await PostAsync($"api/users/{id}/reactivate", new { });

        public void Reactivate(int id) =>
            ReactivateAsync(id).GetAwaiter().GetResult();

        public async Task<List<User>> GetDeactivatedAsync() =>
            await GetAsync<List<User>>("api/users/deactivated") ?? new();

        public List<User> GetDeactivated() =>
            GetDeactivatedAsync().GetAwaiter().GetResult();

        public async Task ChangePasswordAsync(int id, string newPassword) =>
            await PostAsync($"api/users/{id}/change-password", new { newPassword });

        public void ChangePassword(int id, string newPassword) =>
            ChangePasswordAsync(id, newPassword).GetAwaiter().GetResult();

        public int GetActiveUsersCount() =>
            Get<int>("api/users/active-count");

        public (int ManagersCount, int AgentsCount) GetActiveStaffCounts()
        {
            var res = Get<dynamic>("api/users/staff-counts");
            try
            {
                int managers = res?.managersCount != null ? (int)res.managersCount : 0;
                int agents = res?.agentsCount != null ? (int)res.agentsCount : 0;
                return (managers, agents);
            }
            catch
            {
                return (0, 0);
            }
        }

        public List<AdminTeamRosterItemDto> GetTeamRoster(int maxCount = 8) =>
            Get<List<AdminTeamRosterItemDto>>($"api/users/team-roster?count={maxCount}") ?? new();
    }
}
