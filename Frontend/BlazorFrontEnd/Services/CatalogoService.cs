using BlazorFrontEnd.Models;
using System.Net.Http.Json;
using BlazorFrontEnd.Extensions;

namespace BlazorFrontEnd.Services
{
    public class CatalogoService
    {
        private readonly HttpClient _httpClient;

        public CatalogoService(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }

        public async Task<List<EspecieDto>?> GetEspeciesActivasAsync()
        {
            try
            {
                var res = await _httpClient.GetUnwrappedAsync<PaginatedList<EspecieDto>>("api/v1/Especie?soloActivas=true");
                return res?.Items ?? new List<EspecieDto>();
            }
            catch
            {
                return new List<EspecieDto>();
            }
        }

        public async Task<List<EspecieDto>?> GetTodasEspeciesAsync(bool soloActivas = false)
        {
            try
            {
                var res = await _httpClient.GetUnwrappedAsync<PaginatedList<EspecieDto>>($"api/v1/Especie?soloActivas={soloActivas}");
                return res?.Items ?? new List<EspecieDto>();
            }
            catch
            {
                return new List<EspecieDto>();
            }
        }

        public async Task<EspecieDto?> GetEspecieByIdAsync(int id)
        {
            return await _httpClient.GetUnwrappedAsync<EspecieDto>($"api/v1/Especie/{id}");
        }

        public async Task<List<RazaDto>?> GetRazasPorEspecieAsync(int especieId)
        {
            try
            {
                var todas = await _httpClient.GetUnwrappedAsync<List<RazaDto>>($"api/v1/Raza/byEspecie/{especieId}");
                return todas?.Where(r => r.Activo).ToList() ?? new List<RazaDto>();
            }
            catch
            {
                return new List<RazaDto>();
            }
        }

        public async Task<List<RazaDto>?> GetAllRazasAsync()
        {
            try
            {
                var todas = await _httpClient.GetUnwrappedAsync<List<RazaDto>>("api/v1/Raza");
                return todas ?? new List<RazaDto>();
            }
            catch
            {
                return new List<RazaDto>();
            }
        }

        public async Task<RazaDto?> GetRazaByIdAsync(int id)
        {
            return await _httpClient.GetUnwrappedAsync<RazaDto>($"api/v1/Raza/{id}");
        }

        public async Task<bool> CreateEspecieAsync(EspecieDto especie)
        {
            var response = await _httpClient.PostAsJsonAsync("api/v1/Especie", especie);
            return response.IsSuccessStatusCode;
        }

        public async Task<bool> UpdateEspecieAsync(EspecieDto especie)
        {
            var response = await _httpClient.PutAsJsonAsync("api/v1/Especie", especie);
            return response.IsSuccessStatusCode;
        }

        public async Task<bool> DeleteEspecieAsync(int id)
        {
            var response = await _httpClient.DeleteAsync($"api/v1/Especie/{id}");
            return response.IsSuccessStatusCode;
        }

        public async Task<bool> CreateRazaAsync(RazaDto raza)
        {
            var response = await _httpClient.PostAsJsonAsync("api/v1/Raza", raza);
            return response.IsSuccessStatusCode;
        }

        public async Task<bool> UpdateRazaAsync(RazaDto raza)
        {
            var response = await _httpClient.PutAsJsonAsync("api/v1/Raza", raza);
            return response.IsSuccessStatusCode;
        }

        public async Task<bool> DeleteRazaAsync(int id)
        {
            var response = await _httpClient.DeleteAsync($"api/v1/Raza/{id}");
            return response.IsSuccessStatusCode;
        }
    }
}
