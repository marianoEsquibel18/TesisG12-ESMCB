using BlazorFrontEnd.Extensions;
using BlazorFrontEnd.Models;
using System.Net.Http.Json;

namespace BlazorFrontEnd.Services
{
    public class WhatsAppService
    {
        private readonly HttpClient _httpClient;
        private const string BaseUrl = "api/v1/WhatsApp";

        public WhatsAppService(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }

        public async Task<TwilioStatusDto?> GetStatusAsync()
        {
            try
            {
                return await _httpClient.GetUnwrappedAsync<TwilioStatusDto>($"{BaseUrl}/status");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[WhatsAppService.GetStatusAsync] Error: {ex.Message}");
                return null;
            }
        }

        public async Task<WhatsAppResponseDto> SendWhatsAppAsync(string telefono, string mensaje)
        {
            try
            {
                var request = new SendWhatsAppRequest
                {
                    Telefono = telefono,
                    Mensaje = mensaje
                };

                var httpResponse = await _httpClient.PostAsJsonAsync($"{BaseUrl}/send", request);
                if (httpResponse.IsSuccessStatusCode)
                {
                    var apiResp = await httpResponse.Content.ReadFromJsonAsync<ApiResponse<WhatsAppResponseDto>>();
                    if (apiResp?.Data != null)
                    {
                        return apiResp.Data;
                    }
                    
                    return new WhatsAppResponseDto
                    {
                        Success = true,
                        Message = "Mensaje procesado correctamente por el servidor."
                    };
                }
                else
                {
                    var errorBody = await httpResponse.Content.ReadAsStringAsync();
                    return new WhatsAppResponseDto
                    {
                        Success = false,
                        Message = string.IsNullOrWhiteSpace(errorBody) 
                            ? $"Error HTTP {(int)httpResponse.StatusCode}" 
                            : errorBody
                    };
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[WhatsAppService.SendWhatsAppAsync] Error: {ex.Message}");
                return new WhatsAppResponseDto
                {
                    Success = false,
                    Message = $"Error de comunicación: {ex.Message}"
                };
            }
        }
    }
}
