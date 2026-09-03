using System;
using System.Security.Claims;
using System.Threading.Tasks;
using Application.DataTransferObjects;
using Application.Repositories;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Controllers
{
    [ApiController]
    public class IaController : BaseController
    {
        private readonly IIaService _iaService;

        public IaController(IIaService iaService)
        {
            _iaService = iaService ?? throw new ArgumentNullException(nameof(iaService));
        }

        private string GetEffectiveUserRole()
        {
            if (Request.Headers.TryGetValue("X-Impersonate-Role", out var rVal) && !string.IsNullOrWhiteSpace(rVal))
            {
                return rVal.ToString();
            }

            var claimRole = User.FindFirst("role")?.Value ?? User.FindFirst(ClaimTypes.Role)?.Value;
            if (!string.IsNullOrWhiteSpace(claimRole))
            {
                return claimRole;
            }

            if (User.IsInRole("Admin")) return "Admin";
            if (User.IsInRole("Veterinario")) return "Veterinario";
            if (User.IsInRole("Gerente")) return "Gerente";
            if (User.IsInRole("Recepcionista")) return "Recepcionista";

            return "Usuario";
        }

        /// <summary>
        /// Obtiene el estado y configuración de la integración con Google Gemini
        /// </summary>
        [HttpGet("api/v1/[controller]/status")]
        [AllowAnonymous]
        public async Task<IActionResult> GetStatus()
        {
            var status = await _iaService.GetStatusAsync();
            return Ok(status);
        }

        /// <summary>
        /// Procesa mensajes conversacionales y solicitudes de turno del Copiloto Flotante
        /// </summary>
        [HttpPost("api/v1/[controller]/chat")]
        [Authorize]
        public async Task<IActionResult> Chat([FromBody] ChatbotRequestDto request)
        {
            if (request == null || string.IsNullOrWhiteSpace(request.Mensaje))
            {
                return BadRequest("El mensaje es requerido.");
            }

            var nombre = User.Identity?.Name ?? "Usuario";
            var rol = GetEffectiveUserRole();
            var sucursal = request.SucursalId ?? UserSucursalId;

            var respuesta = await _iaService.ProcesarMensajeChatAsync(request, nombre, rol, sucursal);
            return Ok(respuesta);
        }

        /// <summary>
        /// Confirma y persiste un turno propuesto previamente por el Copiloto
        /// </summary>
        [HttpPost("api/v1/[controller]/confirmar-turno")]
        [Authorize]
        public async Task<IActionResult> ConfirmarTurno([FromBody] TurnoPropuestoDto request)
        {
            if (request == null)
            {
                return BadRequest("Los datos del turno propuesto son requeridos.");
            }

            var rol = GetEffectiveUserRole();
            var sucursal = request.SucursalId ?? UserSucursalId;

            var resultado = await _iaService.ConfirmarTurnoPropuestoAsync(request, rol, sucursal);
            if (!resultado.Exito)
            {
                return BadRequest(resultado.ErrorMensaje ?? "No se pudo confirmar el turno.");
            }

            return Ok(resultado);
        }

        /// <summary>
        /// Genera el resumen clínico inteligente del historial médico de un paciente
        /// </summary>
        [HttpGet("api/v1/[controller]/resumen-historial/{pacienteId}")]
        [Authorize(Roles = "Admin,Veterinario,Gerente")]
        public async Task<IActionResult> GenerarResumenHistorial(string pacienteId)
        {
            if (string.IsNullOrWhiteSpace(pacienteId))
            {
                return BadRequest("El ID del paciente es requerido.");
            }

            try
            {
                var resumen = await _iaService.GenerarResumenHistorialAsync(pacienteId);
                return Ok(resumen);
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(ex.Message);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Error al generar resumen clínico: {ex.Message}");
            }
        }

        /// <summary>
        /// Genera el resumen ejecutivo con IA de métricas y gráficos para un subreporte específico
        /// </summary>
        [HttpPost("api/v1/[controller]/resumen-reporte")]
        [Authorize(Roles = "Admin,Veterinario,Gerente")]
        public async Task<IActionResult> GenerarResumenReporte([FromBody] ResumenReporteRequestDto request)
        {
            if (request == null || string.IsNullOrWhiteSpace(request.TipoReporte))
            {
                return BadRequest("El tipo de reporte es requerido.");
            }

            try
            {
                var resumen = await _iaService.GenerarResumenReporteAsync(request);
                return Ok(resumen);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Error al generar resumen de reporte: {ex.Message}");
            }
        }
    }
}
