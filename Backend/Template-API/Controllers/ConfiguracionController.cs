using Application.DataTransferObjects;
using Application.Repositories;
using Core.Application;
using Domain.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Controllers
{
    [ApiController]
    [Authorize(Roles = "Admin")]
    public class ConfiguracionController(IConfiguracionSistemaRepository repo) : BaseController
    {
        private readonly IConfiguracionSistemaRepository _repo = repo;

        /// <summary>
        /// Obtener todas las configuraciones
        /// </summary>
        [HttpGet("api/v1/Configuracion")]
        public async Task<IActionResult> GetAll()
        {
            var entities = await _repo.FindAllAsync();
            return Ok(entities.OrderBy(c => c.Grupo).ThenBy(c => c.Clave).Select(MapToDto).ToList());
        }

        /// <summary>
        /// Obtener configuraciones por grupo
        /// </summary>
        [HttpGet("api/v1/Configuracion/grupo/{grupo}")]
        public async Task<IActionResult> GetByGrupo(string grupo)
        {
            var entities = await _repo.GetByGrupoAsync(grupo);
            return Ok(entities.Select(MapToDto).ToList());
        }

        /// <summary>
        /// Obtener una configuración por clave
        /// </summary>
        [HttpGet("api/v1/Configuracion/clave/{clave}")]
        public async Task<IActionResult> GetByClave(string clave)
        {
            var entity = await _repo.GetByClaveAsync(clave);
            if (entity == null) return NotFound($"No existe la configuración '{clave}'");
            return Ok(MapToDto(entity));
        }

        /// <summary>
        /// Actualizar el valor de una configuración
        /// </summary>
        [HttpPut("api/v1/Configuracion")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Update([FromBody] UpdateConfigRequest request)
        {
            var entity = await _repo.GetByClaveAsync(request.Clave);
            if (entity == null) return NotFound($"No existe la configuración '{request.Clave}'");
            entity.ActualizarValor(request.Valor);
            _repo.Update(entity.Id, entity);
            return Ok(MapToDto(entity));
        }

        /// <summary>
        /// Actualizar múltiples configuraciones a la vez
        /// </summary>
        [HttpPut("api/v1/Configuracion/batch")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> UpdateBatch([FromBody] List<UpdateConfigRequest> requests)
        {
            var results = new List<ConfiguracionSistemaDto>();
            foreach (var req in requests)
            {
                var entity = await _repo.GetByClaveAsync(req.Clave);
                if (entity == null) continue;
                entity.ActualizarValor(req.Valor);
                _repo.Update(entity.Id, entity);
                results.Add(MapToDto(entity));
            }
            return Ok(results);
        }

        /// <summary>
        /// Crear una nueva configuración (Admin)
        /// </summary>
        [HttpPost("api/v1/Configuracion")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Create([FromBody] CreateConfigRequest request)
        {
            var existing = await _repo.GetByClaveAsync(request.Clave);
            if (existing != null) return BadRequest($"Ya existe la configuración '{request.Clave}'");

            var tipoDato = Enum.TryParse<TipoDato>(request.TipoDato, true, out var t) ? t : TipoDato.String;
            var entity = new ConfiguracionSistema(request.Clave, request.Valor,
                request.Descripcion, request.Grupo, tipoDato);

            if (!entity.IsValid)
                return BadRequest(entity.GetErrors().Select(e => e.ErrorMessage));

            await _repo.AddAsync(entity);
            return Created($"api/v1/Configuracion/clave/{entity.Clave}", MapToDto(entity));
        }

        /// <summary>
        /// Eliminar una configuración (Admin)
        /// </summary>
        [HttpDelete("api/v1/Configuracion/{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Delete(string id)
        {
            var entity = await _repo.FindOneAsync(id);
            if (entity == null) return NotFound();
            _repo.Remove(id);
            return NoContent();
        }

        /// <summary>
        /// Inicializa las configuraciones por defecto del sistema
        /// </summary>
        [HttpPost("api/v1/Configuracion/seed")]
        public async Task<IActionResult> Seed()
        {
            var existing = await _repo.FindAllAsync();
            if (existing.Any()) return BadRequest("Ya existen configuraciones. Use PUT para actualizar.");

            var defaults = new List<ConfiguracionSistema>
            {
                // Clínica
                new("clinica_nombre", "Veterinaria San Martín", "Nombre de la clínica", "Clinica"),
                new("clinica_direccion", "Av. San Martín 1234", "Dirección de la clínica", "Clinica"),
                new("clinica_telefono", "011-4555-0001", "Teléfono principal", "Clinica"),
                new("clinica_email", "contacto@veterinaria.com", "Email de contacto", "Clinica"),
                new("clinica_cuit", "30-12345678-9", "CUIT de la clínica", "Clinica"),
                new("clinica_logo_url", "", "URL del logo", "Clinica"),

                // Facturación
                new("facturacion_iva", "21", "Porcentaje de IVA", "Facturacion", TipoDato.Numero),
                new("facturacion_moneda", "ARS", "Moneda por defecto", "Facturacion"),
                new("facturacion_prefijo", "FAC-", "Prefijo de factura", "Facturacion"),
                new("facturacion_condicion_iva", "Responsable Inscripto", "Condición ante IVA", "Facturacion"),

                // Turnos
                new("turnos_hora_inicio", "08:00", "Hora de apertura", "Turnos"),
                new("turnos_hora_fin", "20:00", "Hora de cierre", "Turnos"),
                new("turnos_duracion_default", "30", "Duración por defecto (minutos)", "Turnos", TipoDato.Numero),
                new("turnos_dias_habiles", "Lun,Mar,Mier,Jue,Vie,Sab", "Días hábiles", "Turnos"),

                // Stock
                new("stock_alerta_minimo", "5", "Cantidad mínima para alerta de stock", "Stock", TipoDato.Numero),
                new("stock_permitir_negativo", "false", "Permitir stock negativo", "Stock", TipoDato.Booleano),

                // Sistema
                new("sistema_version", "1.0.0", "Versión del sistema", "Sistema"),
                new("sistema_mantenimiento", "false", "Modo mantenimiento", "Sistema", TipoDato.Booleano),

                // Twilio WhatsApp
                new("twilio_account_sid", "", "Twilio Account SID para WhatsApp", "Twilio"),
                new("twilio_auth_token", "", "Twilio Auth Token para WhatsApp", "Twilio"),
                new("twilio_from_phone", "whatsapp:+14155238886", "Número emisor Twilio WhatsApp", "Twilio"),
                new("twilio_default_country_code", "+549", "Código de país por defecto (+549)", "Twilio"),
            };

            foreach (var config in defaults)
                await _repo.AddAsync(config);

            return Ok(new { ConfiguracionesCreadas = defaults.Count });
        }

        private static ConfiguracionSistemaDto MapToDto(ConfiguracionSistema c) => new()
        {
            Id = c.Id, Clave = c.Clave, Valor = c.Valor,
            Descripcion = c.Descripcion, Grupo = c.Grupo,
            TipoDato = c.TipoDato.ToString(), FechaModificacion = c.FechaModificacion
        };
    }

    public class UpdateConfigRequest
    {
        public string Clave { get; set; }
        public string Valor { get; set; }
    }

    public class CreateConfigRequest
    {
        public string Clave { get; set; }
        public string Valor { get; set; }
        public string Descripcion { get; set; }
        public string Grupo { get; set; }
        public string TipoDato { get; set; } = "String";
    }
}
