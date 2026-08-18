using System.Net.Http.Json;
using WicStock.Web.Models.Dtos;

namespace WicStock.Web.Services
{
    public class DashboardService
    {
        private readonly HttpClient _http;

        public DashboardService(HttpClient http)
        {
            _http = http;
        }

        public async Task<AdminDashboardDto?> GetAdminDashboard()
        {
            return await _http.GetFromJsonAsync<AdminDashboardDto>("api/dashboard/admin");
        }

        public async Task<ManagerDashboardDto?> GetManagerDashboard()
        {
            return await _http.GetFromJsonAsync<ManagerDashboardDto>("api/dashboard/manager");
        }
    }
}
