using System;
using System.Collections.Generic;
using System.Net.Http;
using CRMS_Peguit.domain.Common;
using CRMS_Peguit.domain.entities;

namespace CRMS_Peguit.winforms.Services
{
    public class BranchApiService : BaseApiService
    {
        public BranchApiService(string? baseUrl = null, HttpClient? httpClient = null)
            : base(baseUrl, httpClient)
        {
        }

        public List<Branch> GetAll() =>
            Get<List<Branch>>("api/branches") ?? new();

        public async System.Threading.Tasks.Task<List<BranchItemDto>> GetAllBranchesAsync() =>
            await GetAsync<List<BranchItemDto>>("api/branches") ?? new();

        public List<Branch> GetActiveBranches() =>
            Get<List<Branch>>("api/branches/active") ?? new();

        public Branch? GetById(int id) =>
            Get<Branch>($"api/branches/{id}");

        public Branch Add(Branch branch)
        {
            var res = Post<Branch, Branch>("api/branches", branch);
            return res ?? branch;
        }

        public void Update(Branch branch) =>
            Put($"api/branches/{branch.BranchId}", branch);

        public void ToggleStatus(int id) =>
            Post($"api/branches/{id}/toggle-status", new { });

        public async System.Threading.Tasks.Task<bool> SaveBranchAsync(Branch branch)
        {
            if (branch.BranchId > 0)
            {
                return await PutAsync($"api/branches/{branch.BranchId}", branch);
            }
            else
            {
                var res = await PostAsync<Branch, Branch>("api/branches", branch);
                return res != null;
            }
        }

        public async System.Threading.Tasks.Task<bool> ToggleBranchStatusAsync(int id) =>
            await PostAsync($"api/branches/{id}/toggle-status", new { });
    }
}
