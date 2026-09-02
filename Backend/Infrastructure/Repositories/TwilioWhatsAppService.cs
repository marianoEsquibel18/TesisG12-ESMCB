using Application.DataTransferObjects;
using Application.Repositories;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace Infrastructure.Repositories
{
    public class TwilioWhatsAppService : ITwilioWhatsAppService
    {
        private readonly IConfiguration _configuration;
        private readonly IConfiguracionSistemaRepository _configRepo;
        private readonly ILogger<TwilioWhatsAppService> _logger;
        private static readonly HttpClient _httpClient = new() { Timeout = TimeSpan.FromSeconds(15) };

        public TwilioWhatsAppService(
            IConfiguration configuration,
            IConfiguracionSistemaRepository configRepo,
            ILogger<TwilioWhatsAppService> logger)
        {
            _configuration = configuration;
            _configRepo = configRepo;
            _logger = logger;
        }

        private async Task<TwilioSettings> GetSettingsAsync()
        {
            var settings = new TwilioSettings
            {
                AccountSid = _configuration["Twilio:AccountSid"] ?? string.Empty,
                AuthToken = _configuration["Twilio:AuthToken"] ?? string.Empty,
                FromPhoneNumber = _configuration["Twilio:FromPhoneNumber"] ?? "whatsapp:+14155238886",
                DefaultCountryCode = _configuration["Twilio:DefaultCountryCode"] ?? "+549",
                EnableTwilio = bool.TryParse(_configuration["Twilio:EnableTwilio"], out var enabled) ? enabled : true
            };

            // Intentar enriquecer o sobreescribir desde ConfiguracionSistema en BD si existen
            try
            {
                var dbSid = await _configRepo.GetByClaveAsync("twilio_account_sid");
                if (dbSid != null && !string.IsNullOrWhiteSpace(dbSid.Valor))
                    settings.AccountSid = dbSid.Valor.Trim();

                var dbToken = await _configRepo.GetByClaveAsync("twilio_auth_token");
                if (dbToken != null && !string.IsNullOrWhiteSpace(dbToken.Valor))
                    settings.AuthToken = dbToken.Valor.Trim();

                var dbFrom = await _configRepo.GetByClaveAsync("twilio_from_phone");
                if (dbFrom != null && !string.IsNullOrWhiteSpace(dbFrom.Valor))
                    settings.FromPhoneNumber = dbFrom.Valor.Trim();

                var dbCode = await _configRepo.GetByClaveAsync("twilio_default_country_code");
                if (dbCode != null && !string.IsNullOrWhiteSpace(dbCode.Valor))
                    settings.DefaultCountryCode = dbCode.Valor.Trim();
            }
            catch (Exception ex)
            {
                _logger.LogWarning($"No se pudieron consultar configuraciones de Twilio en BD: {ex.Message}");
            }

            return settings;
        }

        public async Task<TwilioStatusDto> GetStatusAsync()
        {
            var settings = await GetSettingsAsync();
            bool isConfigured = !string.IsNullOrWhiteSpace(settings.AccountSid) &&
                                !string.IsNullOrWhiteSpace(settings.AuthToken) &&
                                !settings.AccountSid.StartsWith("AC_TU_") &&
                                !settings.AccountSid.Contains("YOUR_TWILIO");

            return new TwilioStatusDto
            {
                IsConfigured = isConfigured,
                EnableTwilio = settings.EnableTwilio,
                FromPhoneNumber = settings.FromPhoneNumber,
                DefaultCountryCode = settings.DefaultCountryCode
            };
        }

        public async Task<WhatsAppResponseDto> SendWhatsAppAsync(string telefono, string mensaje)
        {
            if (string.IsNullOrWhiteSpace(telefono))
            {
                return new WhatsAppResponseDto
                {
                    Success = false,
                    Message = "El número de teléfono destinatario no puede estar vacío."
                };
            }

            if (string.IsNullOrWhiteSpace(mensaje))
            {
                return new WhatsAppResponseDto
                {
                    Success = false,
                    Message = "El mensaje a enviar no puede estar vacío."
                };
            }

            var settings = await GetSettingsAsync();

            if (!settings.EnableTwilio)
            {
                return new WhatsAppResponseDto
                {
                    Success = false,
                    Message = "El servicio de Twilio WhatsApp se encuentra deshabilitado en la configuración."
                };
            }

            if (string.IsNullOrWhiteSpace(settings.AccountSid) || string.IsNullOrWhiteSpace(settings.AuthToken) ||
                settings.AccountSid.StartsWith("AC_TU_") || settings.AccountSid.Contains("YOUR_TWILIO"))
            {
                return new WhatsAppResponseDto
                {
                    Success = false,
                    Message = "Twilio no está configurado con credenciales válidas (Account SID o Auth Token). Verifique appsettings.json o la configuración del sistema."
                };
            }

            string formattedTo = FormatWhatsAppRecipient(telefono, settings.DefaultCountryCode);
            string formattedFrom = settings.FromPhoneNumber.StartsWith("whatsapp:")
                ? settings.FromPhoneNumber
                : $"whatsapp:{settings.FromPhoneNumber}";

            try
            {
                var requestUrl = $"https://api.twilio.com/2010-04-01/Accounts/{settings.AccountSid}/Messages.json";

                var formParams = new Dictionary<string, string>
                {
                    { "From", formattedFrom },
                    { "To", formattedTo },
                    { "Body", mensaje }
                };

                using var request = new HttpRequestMessage(HttpMethod.Post, requestUrl);
                var authBytes = Encoding.ASCII.GetBytes($"{settings.AccountSid}:{settings.AuthToken}");
                request.Headers.Authorization = new AuthenticationHeaderValue("Basic", Convert.ToBase64String(authBytes));
                request.Content = new FormUrlEncodedContent(formParams);

                var response = await _httpClient.SendAsync(request);
                var responseContent = await response.Content.ReadAsStringAsync();

                if (response.IsSuccessStatusCode)
                {
                    using var doc = JsonDocument.Parse(responseContent);
                    var root = doc.RootElement;
                    string sid = root.TryGetProperty("sid", out var sidProp) ? sidProp.GetString() ?? "" : "";
                    string status = root.TryGetProperty("status", out var statusProp) ? statusProp.GetString() ?? "" : "";

                    _logger.LogInformation($"[Twilio WhatsApp] Mensaje enviado exitosamente a {formattedTo}. SID: {sid}, Estado: {status}");

                    return new WhatsAppResponseDto
                    {
                        Success = true,
                        Message = "Mensaje enviado exitosamente vía Twilio WhatsApp.",
                        MessageSid = sid,
                        FormattedPhone = formattedTo,
                        Status = status
                    };
                }
                else
                {
                    string errorMsg = "Error al enviar mensaje vía Twilio.";
                    try
                    {
                        using var doc = JsonDocument.Parse(responseContent);
                        if (doc.RootElement.TryGetProperty("message", out var msgProp))
                        {
                            errorMsg = msgProp.GetString() ?? errorMsg;
                        }
                    }
                    catch { }

                    _logger.LogWarning($"[Twilio WhatsApp ERROR] Código {(int)response.StatusCode} al enviar a {formattedTo}: {errorMsg}");

                    string userFriendlyMsg = $"Twilio respondió con error: {errorMsg} (Destino: {formattedTo.Replace("whatsapp:", "")})";
                    if (errorMsg.Contains("ContentSid", StringComparison.OrdinalIgnoreCase))
                    {
                        userFriendlyMsg += " - Asegúrese de que este número haya enviado el código de unión ('join ...') al Sandbox en las últimas 24hs.";
                    }

                    return new WhatsAppResponseDto
                    {
                        Success = false,
                        Message = userFriendlyMsg,
                        FormattedPhone = formattedTo,
                        Status = response.StatusCode.ToString()
                    };
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"[Twilio WhatsApp Exception] Error de red o ejecución al enviar mensaje a {formattedTo}");
                return new WhatsAppResponseDto
                {
                    Success = false,
                    Message = $"Excepción al comunicar con Twilio: {ex.Message}",
                    FormattedPhone = formattedTo
                };
            }
        }

        private static string FormatWhatsAppRecipient(string phone, string defaultCountryCode)
        {
            if (string.IsNullOrWhiteSpace(phone)) return string.Empty;

            var raw = phone.Trim();
            var digitsOnly = new string(raw.Where(char.IsDigit).ToArray());

            if (digitsOnly.StartsWith("00"))
            {
                digitsOnly = digitsOnly.Substring(2);
            }

            // Normalización específica para Argentina (prefijo internacional 54)
            // En WhatsApp internacional para Argentina SIEMPRE debe ser: +54 9 <area> <numero> (sin 0 ni 15)
            if (digitsOnly.StartsWith("54"))
            {
                var after54 = digitsOnly.Substring(2);
                if (!after54.StartsWith("9"))
                {
                    digitsOnly = "549" + after54;
                }
            }
            else
            {
                if (digitsOnly.StartsWith("0"))
                {
                    digitsOnly = digitsOnly.TrimStart('0');
                }

                string cleanDefault = new string((defaultCountryCode ?? "+549").Where(char.IsDigit).ToArray());
                if (string.IsNullOrEmpty(cleanDefault)) cleanDefault = "549";
                if (!cleanDefault.StartsWith("549") && cleanDefault == "54") cleanDefault = "549";

                digitsOnly = cleanDefault + digitsOnly;
            }

            // Eliminar el prefijo local "15" si quedó incluido (en Argentina un móvil tiene 13 dígitos totales con 549)
            if (digitsOnly.StartsWith("549") && digitsOnly.Length > 13)
            {
                for (int areaLen = 2; areaLen <= 4; areaLen++)
                {
                    int pos15 = 3 + areaLen;
                    if (digitsOnly.Length >= pos15 + 2 && digitsOnly.Substring(pos15, 2) == "15")
                    {
                        var candidate = digitsOnly.Substring(0, pos15) + digitsOnly.Substring(pos15 + 2);
                        if (candidate.Length == 13)
                        {
                            digitsOnly = candidate;
                            break;
                        }
                    }
                }
            }

            return $"whatsapp:+{digitsOnly}";
        }
    }
}
