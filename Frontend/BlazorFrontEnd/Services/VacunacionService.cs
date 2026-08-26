using BlazorFrontEnd.Models;
using System.Net.Http.Json;
using BlazorFrontEnd.Extensions;

namespace BlazorFrontEnd.Services
{
    public class VacunacionService
    {
        private readonly HttpClient _httpClient;
        private const string BaseUrl = "api/v1/RegistroVacunacion";

        public VacunacionService(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }

        public async Task<List<RegistroVacunacionDto>?> GetByPacienteIdAsync(string pacienteId)
        {
            try
            {
                // Typically you get everything and filter, or use an endpoint. Assuming an endpoint exists or we use the general one.
                // Depending on the backend mapping it might be at "api/v1/registros-vacunacion/paciente/{id}"
                return await _httpClient.GetUnwrappedAsync<List<RegistroVacunacionDto>>($"{BaseUrl}/byPaciente/{pacienteId}");
            }
            catch { return new List<RegistroVacunacionDto>(); }
        }

        public async Task<RegistroVacunacionDto?> GetByIdAsync(string id)
        {
            return await _httpClient.GetUnwrappedAsync<RegistroVacunacionDto>($"{BaseUrl}/{id}");
        }

        public async Task<bool> CreateAsync(RegistroVacunacionDto modelo)
        {
            try
            {
                var request = new
                {
                    PacienteId = modelo.PacienteId,
                    VacunaId = modelo.VacunaId,
                    ProductoId = modelo.ProductoId,
                    DepositoId = modelo.DepositoId,
                    FechaAplicacion = modelo.FechaAplicacion,
                    FechaProximaDosis = modelo.FechaProximaDosis,
                    Veterinario = modelo.Veterinario,
                    NroLote = modelo.NroLote,
                    Observaciones = modelo.Observaciones
                };
                var response = await _httpClient.PostAsJsonAsync(BaseUrl, request);
                if (!response.IsSuccessStatusCode)
                {
                    var err = await response.Content.ReadAsStringAsync();
                    Console.WriteLine($"[VACUNACION CREATE ERROR] {response.StatusCode}: {err}");
                }
                return response.IsSuccessStatusCode;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[VACUNACION CREATE EXCEPTION] {ex.Message}");
                return false;
            }
        }

        public async Task<bool> UpdateAsync(string id, RegistroVacunacionDto modelo)
        {
            modelo.Id = id;
            var response = await _httpClient.PutAsJsonAsync(BaseUrl, modelo);
            return response.IsSuccessStatusCode;
        }

        public async Task<bool> DeleteAsync(string id)
        {
            var response = await _httpClient.DeleteAsync($"{BaseUrl}/{id}");
            return response.IsSuccessStatusCode;
        }
        
        public async Task<List<VacunaDto>?> GetCatalogoVacunasAsync()
        {
            try
            {
                // Backend returns QueryResult<VacunaDto> wrapped in HttpMessageResult
                // GetUnwrappedAsync extracts Data → we get a QueryResult-like object
                // We need to get the Items from it
                var result = await _httpClient.GetUnwrappedAsync<QueryResultWrapper<VacunaDto>>("api/v1/Vacuna");
                return result?.Items?.ToList() ?? new List<VacunaDto>();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[VACUNA CATALOG ERROR] {ex.Message}");
                return new List<VacunaDto>();
            }
        }
    }

    // Helper class to deserialize QueryResult from backend
    public class QueryResultWrapper<T>
    {
        public List<T> Items { get; set; } = new();
        public long Count { get; set; }
        public int PageIndex { get; set; }
        public int PageSize { get; set; }
    }
}
