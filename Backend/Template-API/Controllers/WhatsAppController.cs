using Application.DataTransferObjects;
using Application.Repositories;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Controllers
{
    [ApiController]
    [Authorize]
    public class WhatsAppController : BaseController
    {
        private readonly ITwilioWhatsAppService _twilioService;
        private readonly ILogger<WhatsAppController> _logger;

        public WhatsAppController(ITwilioWhatsAppService twilioService, ILogger<WhatsAppController> logger)
        {
            _twilioService = twilioService;
            _logger = logger;
        }

        /// <summary>
        /// Obtiene el estado de configuración de Twilio WhatsApp
        /// </summary>
        [HttpGet("api/v1/WhatsApp/status")]
        [AllowAnonymous]
        public async Task<IActionResult> GetStatus()
        {
            var status = await _twilioService.GetStatusAsync();
            return Ok(status);
        }

        /// <summary>
        /// Envía un mensaje de WhatsApp directo a través de Twilio
        /// </summary>
        [HttpPost("api/v1/WhatsApp/send")]
        public async Task<IActionResult> SendMessage([FromBody] SendWhatsAppRequest request)
        {
            if (request == null || string.IsNullOrWhiteSpace(request.Telefono) || string.IsNullOrWhiteSpace(request.Mensaje))
            {
                return BadRequest("El número de teléfono y el mensaje son campos obligatorios.");
            }

            var result = await _twilioService.SendWhatsAppAsync(request.Telefono, request.Mensaje);
            return Ok(result);
        }

        /// <summary>
        /// Realiza una prueba de envío de WhatsApp (para administradores)
        /// </summary>
        [HttpPost("api/v1/WhatsApp/test")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> SendTestMessage([FromBody] SendWhatsAppRequest request)
        {
            if (request == null || string.IsNullOrWhiteSpace(request.Telefono))
            {
                return BadRequest("Debe indicar un número de teléfono para la prueba.");
            }

            string testMsg = string.IsNullOrWhiteSpace(request.Mensaje)
                ? "🐾 Mensaje de prueba desde Veterinaria Ñandubay (Twilio WhatsApp Integration activa)."
                : request.Mensaje;

            var result = await _twilioService.SendWhatsAppAsync(request.Telefono, testMsg);
            return Ok(result);
        }
    }
}
