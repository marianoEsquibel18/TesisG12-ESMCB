using System;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading.Tasks;
using BlazorFrontEnd.Extensions;
using BlazorFrontEnd.Models;

namespace BlazorFrontEnd.Services
{
    public class IaService
    {
        private readonly HttpClient _httpClient;
        private const string BaseUrl = "api/v1/Ia";
        private readonly JsonSerializerOptions _jsonOptions = new() { PropertyNameCaseInsensitive = true };

        public IaService(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }

        public async Task<IaStatusDto?> GetStatusAsync()
        {
            try
            {
                return await _httpClient.GetUnwrappedAsync<IaStatusDto>($"{BaseUrl}/status");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[IaService.GetStatusAsync] Error: {ex.Message}");
                return new IaStatusDto
                {
                    Configurado = false,
                    Proveedor = "Google Gemini",
                    Modelo = "gemini-1.5-flash",
                    Mensaje = "No se pudo consultar el estado del servicio."
                };
            }
        }

        public async Task<ChatbotResponseDto> EnviarMensajeChatAsync(ChatbotRequestDto request)
        {
            try
            {
                var response = await _httpClient.PostAsJsonAsync($"{BaseUrl}/chat", request);
                if (response.IsSuccessStatusCode)
                {
                    var apiResp = await response.Content.ReadFromJsonAsync<ApiResponse<ChatbotResponseDto>>(_jsonOptions);
                    if (apiResp?.Data != null)
                    {
                        return apiResp.Data;
                    }

                    return new ChatbotResponseDto
                    {
                        Exito = true,
                        TipoRespuesta = "texto",
                        Respuesta = "Mensaje procesado correctamente."
                    };
                }
                else
                {
                    var errorBody = await response.Content.ReadAsStringAsync();
                    return new ChatbotResponseDto
                    {
                        Exito = false,
                        ErrorMensaje = string.IsNullOrWhiteSpace(errorBody) ? "Error al procesar mensaje en el servidor." : errorBody,
                        Respuesta = "Ocurrio un problema al comunicarse con el servidor. Intenta nuevamente."
                    };
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[IaService.EnviarMensajeChatAsync] Error: {ex.Message}");
                return new ChatbotResponseDto
                {
                    Exito = false,
                    ErrorMensaje = ex.Message,
                    Respuesta = "No se pudo establecer conexion con el servidor del copiloto."
                };
            }
        }

        public async Task<ChatbotResponseDto> ConfirmarTurnoAsync(TurnoPropuestoDto turnoDto)
        {
            try
            {
                var response = await _httpClient.PostAsJsonAsync($"{BaseUrl}/confirmar-turno", turnoDto);
                if (response.IsSuccessStatusCode)
                {
                    var apiResp = await response.Content.ReadFromJsonAsync<ApiResponse<ChatbotResponseDto>>(_jsonOptions);
                    if (apiResp?.Data != null)
                    {
                        return apiResp.Data;
                    }

                    return new ChatbotResponseDto
                    {
                        Exito = true,
                        TipoRespuesta = "turno_confirmado",
                        Respuesta = "Turno confirmado exitosamente."
                    };
                }
                else
                {
                    var errorBody = await response.Content.ReadAsStringAsync();
                    return new ChatbotResponseDto
                    {
                        Exito = false,
                        ErrorMensaje = string.IsNullOrWhiteSpace(errorBody) ? "Error al confirmar turno." : errorBody,
                        Respuesta = "No fue posible confirmar el turno solicitado."
                    };
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[IaService.ConfirmarTurnoAsync] Error: {ex.Message}");
                return new ChatbotResponseDto
                {
                    Exito = false,
                    ErrorMensaje = ex.Message,
                    Respuesta = "Ocurrio un error de conexion al confirmar el turno."
                };
            }
        }

        public async Task<ResumenHistoriaClinicaDto?> GenerarResumenHistorialAsync(string pacienteId)
        {
            try
            {
                return await _httpClient.GetUnwrappedAsync<ResumenHistoriaClinicaDto>($"{BaseUrl}/resumen-historial/{pacienteId}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[IaService.GenerarResumenHistorialAsync] Error: {ex.Message}");
                return null;
            }
        }

        public async Task<ResumenReporteResponseDto?> GenerarResumenReporteAsync(ResumenReporteRequestDto request)
        {
            try
            {
                var response = await _httpClient.PostAsJsonAsync($"{BaseUrl}/resumen-reporte", request);
                if (response.IsSuccessStatusCode)
                {
                    var apiResp = await response.Content.ReadFromJsonAsync<ApiResponse<ResumenReporteResponseDto>>(_jsonOptions);
                    return apiResp?.Data;
                }
                else
                {
                    var errorBody = await response.Content.ReadAsStringAsync();
                    return new ResumenReporteResponseDto
                    {
                        Exito = false,
                        ErrorMensaje = string.IsNullOrWhiteSpace(errorBody) ? "Error al generar resumen del reporte." : errorBody,
                        ResumenEjecutivo = "No se pudo generar el análisis del reporte en este momento."
                    };
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[IaService.GenerarResumenReporteAsync] Error: {ex.Message}");
                return new ResumenReporteResponseDto
                {
                    Exito = false,
                    ErrorMensaje = ex.Message,
                    ResumenEjecutivo = "Error de conexión al generar el análisis del reporte."
                };
            }
        }
    }
}
