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
                var url = $"api/v1/Paginado/servicios?page=1&pageSize=1000";
                var res = await _httpClient.GetUnwrappedAsync<PaginatedList<ServicioDto>>(url);
                return res?.Items.Where(s => s.Activo).ToList() ?? new List<ServicioDto>();
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
                return res;
            }
            catch
            {
                return null;
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

        public async Task<bool> CreateAsync(ServicioDto dto)
        {
            var response = await _httpClient.PostAsJsonAsync("api/v1/Servicio", dto);
            return response.IsSuccessStatusCode;
        }

        public async Task<bool> UpdateAsync(int id, ServicioDto dto)
        {
            var response = await _httpClient.PutAsJsonAsync("api/v1/Servicio", dto);
            return response.IsSuccessStatusCode;
        }

        public async Task<bool> DeleteAsync(int id)
        {
            var response = await _httpClient.DeleteAsync($"api/v1/Servicio/{id}");
            return response.IsSuccessStatusCode;
        }
    }
}

