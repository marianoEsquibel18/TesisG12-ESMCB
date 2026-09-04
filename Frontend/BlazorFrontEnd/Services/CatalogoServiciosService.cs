using BlazorFrontEnd.Models;
using System.Net.Http.Json;
using BlazorFrontEnd.Extensions;

namespace BlazorFrontEnd.Services
{
    public class CatalogoServiciosService
    {
        private readonly HttpClient _httpClient;

        public CatalogoServiciosService(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }

        public async Task<List<ServicioDto>?> GetServiciosActivosAsync()
        {
            try
            {
                var res = await _httpClient.GetUnwrappedAsync<PaginatedList<ServicioDto>>("api/v1/Servicio?soloActivos=true");
                if (res?.Items != null)
                {
                    return res.Items.Where(s => s.Activo).ToList();
                }
                var url = $"api/v1/Paginado/servicios?page=1&pageSize=1000";
                var resPag = await _httpClient.GetUnwrappedAsync<PaginatedList<ServicioDto>>(url);
                return resPag?.Items.Where(s => s.Activo).ToList() ?? new List<ServicioDto>();
            }
            catch
            {
                return new List<ServicioDto>();
            }
        }

        public async Task<PaginatedList<ServicioDto>?> GetServiciosAsync(int page = 1, int pageSize = 15, string searchTerm = "")
        {
            try
            {
                var url = $"api/v1/Paginado/servicios?page={page}&pageSize={pageSize}";
                var res = await _httpClient.GetUnwrappedAsync<PaginatedList<ServicioDto>>(url);
                if (res != null) return res;
                return await _httpClient.GetUnwrappedAsync<PaginatedList<ServicioDto>>("api/v1/Servicio?soloActivos=false");
            }
            catch
            {
                try
                {
                    return await _httpClient.GetUnwrappedAsync<PaginatedList<ServicioDto>>("api/v1/Servicio?soloActivos=false");
                }
                catch
                {
                    return null;
                }
            }
        }

        public async Task<ServicioDto?> GetByIdAsync(int id)
        {
            try
            {
                return await _httpClient.GetUnwrappedAsync<ServicioDto>($"api/v1/Servicio/{id}");
            }
            catch
            {
                return null;
            }
        }

        public async Task<(bool Success, string ErrorMessage)> CreateAsync(ServicioDto dto)
        {
            var response = await _httpClient.PostAsJsonAsync("api/v1/Servicio", dto);
            if (response.IsSuccessStatusCode) return (true, "");
            var err = await response.Content.ReadAsStringAsync();
            return (false, string.IsNullOrWhiteSpace(err) ? "Error al crear el servicio." : err);
        }

        public async Task<(bool Success, string ErrorMessage)> UpdateAsync(int id, ServicioDto dto)
        {
            var response = await _httpClient.PutAsJsonAsync("api/v1/Servicio", dto);
            if (response.IsSuccessStatusCode) return (true, "");
            var err = await response.Content.ReadAsStringAsync();
            return (false, string.IsNullOrWhiteSpace(err) ? "Error al actualizar el servicio." : err);
        }

        public async Task<(bool Success, string ErrorMessage)> DeleteAsync(int id)
        {
            var response = await _httpClient.DeleteAsync($"api/v1/Servicio/{id}");
            if (response.IsSuccessStatusCode) return (true, "");
            var err = await response.Content.ReadAsStringAsync();
            return (false, string.IsNullOrWhiteSpace(err) ? "Error al eliminar el servicio." : err);
        }
    }
}

