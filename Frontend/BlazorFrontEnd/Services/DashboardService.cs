using BlazorFrontEnd.Models;
using BlazorFrontEnd.Extensions;

namespace BlazorFrontEnd.Services
{
    public class DashboardService
    {
        private readonly HttpClient _httpClient;

        public DashboardService(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }

        public async Task<KpiDashboardDto?> GetKpisAsync(int? sucursalId = null)
        {
            var url = "api/v1/Estadisticas/kpis";
            if (sucursalId.HasValue && sucursalId.Value > 0)
                url += $"?sucursalId={sucursalId.Value}";
            return await _httpClient.GetUnwrappedAsync<KpiDashboardDto>(url);
        }

        public async Task<DashboardAlertasResponse?> GetAlertasDashboardAsync(int? sucursalId = null)
        {
            var url = "api/v1/Recordatorio/dashboard";
            if (sucursalId.HasValue && sucursalId.Value > 0)
                url += $"?sucursalId={sucursalId.Value}";
            return await _httpClient.GetUnwrappedAsync<DashboardAlertasResponse>(url);
        }

        public async Task<List<VacunaPendienteDto>?> GetVacunasPendientesAsync(int diasAntelacion = 30, int? sucursalId = null)
        {
            var url = $"api/v1/Recordatorio/vacunas/pendientes?diasAntelacion={diasAntelacion}";
            if (sucursalId.HasValue && sucursalId.Value > 0)
                url += $"&sucursalId={sucursalId.Value}";
            var response = await _httpClient.GetUnwrappedAsync<VacunasPendientesResponse>(url);
            return response?.Items;
        }

        public async Task<List<IngresoDiarioDto>?> GetIngresosDiariosAsync(int dias = 7, int? sucursalId = null)
        {
            var desde = DateTime.Today.AddDays(-dias).ToString("yyyy-MM-dd");
            var hasta = DateTime.Today.ToString("yyyy-MM-dd");
            var url = $"api/v1/Estadisticas/ingresos/diario?desde={desde}&hasta={hasta}";
            if (sucursalId.HasValue && sucursalId.Value > 0)
                url += $"&sucursalId={sucursalId.Value}";
            return await _httpClient.GetUnwrappedAsync<List<IngresoDiarioDto>>(url);
        }

        public async Task<PacientesPorEspecieDashboardResponse?> GetPacientesPorEspecieAsync(DateTime? desde = null, DateTime? hasta = null, int? sucursalId = null)
        {
            var url = "api/v1/Estadisticas/pacientes/porEspecie";
            var query = new List<string>();
            if (desde.HasValue) query.Add($"desde={desde.Value:yyyy-MM-dd}");
            if (hasta.HasValue) query.Add($"hasta={hasta.Value:yyyy-MM-dd}");
            if (sucursalId.HasValue && sucursalId.Value > 0) query.Add($"sucursalId={sucursalId.Value}");
            if (query.Any()) url += "?" + string.Join("&", query);

            return await _httpClient.GetUnwrappedAsync<PacientesPorEspecieDashboardResponse>(url);
        }
    }
}
