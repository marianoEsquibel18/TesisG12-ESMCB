using Application.DataTransferObjects;
using Application.Repositories;
using Core.Application;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Controllers
{
    /// <summary>
    /// Controller para gestionar el Historial Clínico de los pacientes
    /// </summary>
    [ApiController]
    [Authorize(Roles = "Admin,Veterinario")]
    public class HistorialClinicoController(
        IHistorialClinicoRepository historialRepository,
        IPacienteRepository pacienteRepository) : BaseController
    {
        private readonly IHistorialClinicoRepository _historialRepository = historialRepository
            ?? throw new ArgumentNullException(nameof(historialRepository));
        private readonly IPacienteRepository _pacienteRepository = pacienteRepository
            ?? throw new ArgumentNullException(nameof(pacienteRepository));

        /// <summary>
        /// Obtiene el historial clínico completo de un paciente
        /// </summary>
        [HttpGet("api/v1/[Controller]/byPaciente/{pacienteId}")]
        public async Task<IActionResult> GetByPaciente(string pacienteId)
        {
            if (string.IsNullOrWhiteSpace(pacienteId)) return BadRequest("El ID del paciente es requerido");

            var paciente = await _pacienteRepository.FindOneAsync(pacienteId);
            if (paciente == null) return NotFound($"No se encontró el paciente con Id {pacienteId}");

            var entities = await _historialRepository.GetByPacienteIdAsync(pacienteId);
            var dtos = entities.Select(MapToDto).ToList();
            return Ok(dtos);
        }

        /// <summary>
        /// Obtiene una entrada del historial clínico por su ID
        /// </summary>
        [HttpGet("api/v1/[Controller]/{id}")]
        public async Task<IActionResult> GetById(string id)
        {
            if (string.IsNullOrWhiteSpace(id)) return BadRequest("El ID es requerido");

            var entity = await _historialRepository.FindOneAsync(id);
            if (entity == null) return NotFound($"No se encontró la entrada con Id {id}");

            return Ok(MapToDto(entity));
        }

        /// <summary>
        /// Registra una nueva entrada en el historial clínico
        /// </summary>
        [HttpPost("api/v1/[Controller]")]
        public async Task<IActionResult> Create([FromBody] CreateHistorialClinicoRequest request)
        {
            if (request is null) return BadRequest("El request no puede ser nulo");

            var paciente = await _pacienteRepository.FindOneAsync(request.PacienteId);
            if (paciente == null) return BadRequest($"No existe el paciente con Id {request.PacienteId}");

            var fechaConsulta = request.Fecha;
            if (fechaConsulta.TimeOfDay == TimeSpan.Zero)
            {
                if (fechaConsulta.Date == DateTime.Today)
                    fechaConsulta = DateTime.Now;
                else
                    fechaConsulta = fechaConsulta.Date.Add(DateTime.Now.TimeOfDay);
            }

            var entity = new Domain.Entities.HistorialClinico(
                request.PacienteId,
                fechaConsulta,
                request.Motivo,
                request.Veterinario,
                request.Sintomas ?? "",
                request.Diagnostico ?? "",
                request.Indicaciones ?? "",
                request.Peso,
                request.Temperatura,
                request.Observaciones ?? "",
                archivosAdjuntos: request.ArchivosAdjuntos);

            if (!entity.IsValid)
                return BadRequest(entity.GetErrors().Select(e => e.ErrorMessage));

            var createdId = await _historialRepository.AddAsync(entity);
            return Created($"api/v1/HistorialClinico/{createdId}", new { Id = createdId });
        }

        /// <summary>
        /// Actualiza una entrada del historial clínico
        /// </summary>
        [HttpPut("api/v1/[Controller]")]
        public async Task<IActionResult> Update([FromBody] UpdateHistorialClinicoRequest request)
        {
            if (request is null) return BadRequest("El request no puede ser nulo");

            var entity = await _historialRepository.FindOneAsync(request.Id);
            if (entity == null) return NotFound($"No se encontró la entrada con Id {request.Id}");

            entity.Actualizar(request.Motivo, request.Sintomas ?? "", request.Diagnostico ?? "",
                request.Indicaciones ?? "", request.Peso, request.Temperatura, request.Observaciones ?? "", archivosAdjuntos: request.ArchivosAdjuntos);

            if (!entity.IsValid)
                return BadRequest(entity.GetErrors().Select(e => e.ErrorMessage));

            _historialRepository.Update(request.Id, entity);
            return NoContent();
        }

        /// <summary>
        /// Elimina una entrada del historial clínico
        /// </summary>
        [HttpDelete("api/v1/[Controller]/{id}")]
        public async Task<IActionResult> Delete(string id)
        {
            if (string.IsNullOrWhiteSpace(id)) return BadRequest("El ID es requerido");
            var entity = await _historialRepository.FindOneAsync(id);
            if (entity == null) return NotFound($"No se encontró la entrada con Id {id}");
            _historialRepository.Remove(id);
            return NoContent();
        }

        private static HistorialClinicoDto MapToDto(Domain.Entities.HistorialClinico h) => new()
        {
            Id = h.Id,
            PacienteId = h.PacienteId,
            PacienteNombre = h.Paciente?.Nombre ?? "",
            Fecha = h.Fecha,
            Motivo = h.Motivo,
            Sintomas = h.Sintomas,
            Diagnostico = h.Diagnostico,
            Indicaciones = h.Indicaciones,
            Veterinario = h.Veterinario,
            Peso = h.Peso,
            Temperatura = h.Temperatura,
            Observaciones = h.Observaciones,
            ArchivosAdjuntos = h.ArchivosAdjuntos
        };
    }

    public class CreateHistorialClinicoRequest
    {
        public string PacienteId { get; set; }
        public DateTime Fecha { get; set; }
        public string Motivo { get; set; }
        public string Veterinario { get; set; }
        public string Sintomas { get; set; }
        public string Diagnostico { get; set; }
        public string Indicaciones { get; set; }
        public decimal? Peso { get; set; }
        public decimal? Temperatura { get; set; }
        public string Observaciones { get; set; }
        public string? ArchivosAdjuntos { get; set; }
    }

    public class UpdateHistorialClinicoRequest
    {
        public string Id { get; set; }
        public string Motivo { get; set; }
        public string Sintomas { get; set; }
        public string Diagnostico { get; set; }
        public string Indicaciones { get; set; }
        public decimal? Peso { get; set; }
        public decimal? Temperatura { get; set; }
        public string Observaciones { get; set; }
        public string? ArchivosAdjuntos { get; set; }
    }
}
