using BlazorFrontEnd.Models;
using System.Net.Http.Json;
using BlazorFrontEnd.Extensions;

namespace BlazorFrontEnd.Services
{
    public class TurnoService
    {
        private readonly HttpClient _httpClient;
        private const string BaseUrl = "api/v1/Turno";

        public TurnoService(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }

        public async Task<PaginatedList<TurnoDto>?> GetAllAsync(int page = 1, int pageSize = 10, string searchTerm = "", string? veterinarioId = null)
        {
            var url = $"api/v1/Paginado/turnos?page={page}&pageSize={pageSize}";
            if (!string.IsNullOrWhiteSpace(searchTerm)) url += $"&searchTerm={Uri.EscapeDataString(searchTerm)}";
            if (!string.IsNullOrWhiteSpace(veterinarioId)) url += $"&veterinarioId={Uri.EscapeDataString(veterinarioId)}";
            
            return await _httpClient.GetUnwrappedAsync<PaginatedList<TurnoDto>>(url);
        }

        // Endpoint for Calendar View
        public async Task<List<TurnoDto>?> GetRangoAsync(DateTime inicio, DateTime fin, string? searchTerm = null, string? veterinarioId = null)
        {
            var url = $"{BaseUrl}/programados?desde={inicio:s}&hasta={fin:s}";
            if (!string.IsNullOrWhiteSpace(searchTerm)) url += $"&searchTerm={Uri.EscapeDataString(searchTerm)}";
            if (!string.IsNullOrWhiteSpace(veterinarioId)) url += $"&veterinarioId={Uri.EscapeDataString(veterinarioId)}";
            
            return await _httpClient.GetUnwrappedAsync<List<TurnoDto>>(url);
        }

        public async Task<TurnoDto?> GetByIdAsync(string id)
        {
            return await _httpClient.GetUnwrappedAsync<TurnoDto>($"{BaseUrl}/{id}");
        }

        public async Task<(bool Success, string ErrorMessage)> CreateWithResultAsync(TurnoDto turno)
        {
            var response = await _httpClient.PostAsJsonAsync(BaseUrl, turno);
            if (response.IsSuccessStatusCode)
                return (true, string.Empty);

            var content = await response.Content.ReadAsStringAsync();
            if (!string.IsNullOrWhiteSpace(content) && content.StartsWith("\"") && content.EndsWith("\""))
                content = content.Trim('"');

            return (false, string.IsNullOrWhiteSpace(content) ? "Error al guardar el turno" : content);
        }

        public async Task<bool> CreateAsync(TurnoDto turno)
        {
            var (success, _) = await CreateWithResultAsync(turno);
            return success;
        }

        public async Task<(bool Success, string ErrorMessage)> UpdateWithResultAsync(string id, TurnoDto turno)
        {
            var response = await _httpClient.PutAsJsonAsync($"{BaseUrl}/{id}", new
            {
                PacienteId = turno.PacienteId,
                VeterinarioId = turno.VeterinarioId,
                ServicioId = turno.ServicioId,
                FechaHora = turno.FechaHora,
                DuracionMinutos = turno.DuracionMinutos,
                Motivo = turno.Motivo,
                Observaciones = turno.Observaciones,
                Estado = turno.Estado,
                ArchivosAdjuntos = turno.ArchivosAdjuntos
            });
            if (response.IsSuccessStatusCode)
                return (true, string.Empty);

            var content = await response.Content.ReadAsStringAsync();
            if (!string.IsNullOrWhiteSpace(content) && content.StartsWith("\"") && content.EndsWith("\""))
                content = content.Trim('"');

            return (false, string.IsNullOrWhiteSpace(content) ? "Error al actualizar el turno" : content);
        }

        public async Task<bool> UpdateAsync(string id, TurnoDto turno)
        {
            var (success, _) = await UpdateWithResultAsync(id, turno);
            return success;
        }

        public async Task<bool> DeleteAsync(string id)
        {
            var response = await _httpClient.DeleteAsync($"{BaseUrl}/{id}");
            return response.IsSuccessStatusCode;
        }

        public async Task<bool> ChangeStatusAsync(string id, string newStatus)
        {
            if (newStatus == "Cancelado") {
                var response = await _httpClient.PutAsJsonAsync($"{BaseUrl}/{id}/cancelar", "Cancelado desde el sistema");
                return response.IsSuccessStatusCode;
            }
            if (newStatus == "Completado") {
                var response = await _httpClient.PutAsJsonAsync($"{BaseUrl}/{id}/completar", "Completado");
                return response.IsSuccessStatusCode;
            }
            if (newStatus == "Ausente") {
                var response = await _httpClient.PutAsJsonAsync($"{BaseUrl}/{id}/ausente", new { });
                return response.IsSuccessStatusCode;
            }
            if (newStatus == "Confirmado") {
                var response = await _httpClient.PutAsJsonAsync($"{BaseUrl}/{id}/confirmar", new { });
                return response.IsSuccessStatusCode;
            }
            if (newStatus == "EnCurso") {
                var response = await _httpClient.PutAsJsonAsync($"{BaseUrl}/{id}/encurso", new { });
                return response.IsSuccessStatusCode;
            }

            var existing = await GetByIdAsync(id);
            if (existing != null)
            {
                existing.Estado = newStatus;
                var res = await UpdateWithResultAsync(id, existing);
                return res.Success;
            }
            return false;
        }

        public async Task<List<TurnoDto>?> GetByPacienteIdAsync(string pacienteId)
        {
            return await _httpClient.GetUnwrappedAsync<List<TurnoDto>>($"{BaseUrl}/byPaciente/{pacienteId}");
        }
    }
}

