using System;
using System.Net.Http;
using System.Threading.Tasks;
using CRMS_Peguit.domain.Common;
using CRMS_Peguit.winforms.Models.ViewModels;

namespace CRMS_Peguit.winforms.Services
{
    public class DashboardApiService : BaseApiService
    {
        public DashboardApiService(string? baseUrl = null, HttpClient? httpClient = null)
            : base(baseUrl, httpClient)
        {
        }

        public AgentDashboardDto GetAgentSnapshot(int userId) =>
            Get<AgentDashboardDto>($"api/dashboard/agent?userId={userId}") ?? new AgentDashboardDto();

        public ManagerDashboardDto GetManagerSnapshot() =>
            Get<ManagerDashboardDto>("api/dashboard/manager") ?? new ManagerDashboardDto();

        public AdminDashboardDto GetAdminSnapshot() =>
            Get<AdminDashboardDto>("api/dashboard/admin") ?? new AdminDashboardDto();

        public SuperAdminDashboardDto GetSuperAdminSnapshot() =>
            Get<SuperAdminDashboardDto>("api/dashboard/superadmin") ?? new SuperAdminDashboardDto();
    }
}
