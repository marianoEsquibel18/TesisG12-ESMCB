using BlazorFrontEnd.Models;
using System.Net.Http.Json;
using BlazorFrontEnd.Extensions;

namespace BlazorFrontEnd.Services
{
    public class TratamientoService
    {
        private readonly HttpClient _httpClient;
        private const string BaseUrl = "api/v1/Tratamiento";

        public TratamientoService(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }

        public async Task<List<TratamientoDto>?> GetByPacienteIdAsync(string pacienteId)
        {
            try
            {
                return await _httpClient.GetUnwrappedAsync<List<TratamientoDto>>($"{BaseUrl}/byPaciente/{pacienteId}");
            }
            catch { return new List<TratamientoDto>(); }
        }

        public async Task<TratamientoDto?> GetByIdAsync(string id)
        {
            return await _httpClient.GetUnwrappedAsync<TratamientoDto>($"{BaseUrl}/{id}");
        }

        public async Task<bool> CreateAsync(TratamientoDto modelo)
        {
            try
            {
                var request = new
                {
                    PacienteId = modelo.PacienteId,
                    Fecha = modelo.Fecha,
                    Diagnostico = modelo.Diagnostico,
                    Descripcion = modelo.Descripcion,
                    Veterinario = modelo.Veterinario,
                    Medicacion = modelo.Medicacion,
                    Observaciones = modelo.Observaciones,
                    ArchivosAdjuntos = modelo.ArchivosAdjuntos
                };
                var response = await _httpClient.PostAsJsonAsync(BaseUrl, request);
                if (!response.IsSuccessStatusCode)
                {
                    var err = await response.Content.ReadAsStringAsync();
                    Console.WriteLine($"[TRATAMIENTO CREATE ERROR] {response.StatusCode}: {err}");
                }
                return response.IsSuccessStatusCode;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[TRATAMIENTO CREATE EXCEPTION] {ex.Message}");
                return false;
            }
        }

        public async Task<bool> UpdateAsync(string id, TratamientoDto modelo)
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

        public async Task<bool> FinalizarAsync(string id)
        {
            try
            {
                var response = await _httpClient.PutAsync($"{BaseUrl}/{id}/finalizar", null);
                if (!response.IsSuccessStatusCode)
                {
                    var err = await response.Content.ReadAsStringAsync();
                    Console.WriteLine($"[TRATAMIENTO FINALIZAR ERROR] {response.StatusCode}: {err}");
                }
                return response.IsSuccessStatusCode;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[TRATAMIENTO FINALIZAR EXCEPTION] {ex.Message}");
                return false;
            }
        }
    }
}
