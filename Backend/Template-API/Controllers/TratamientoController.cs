using Application.DataTransferObjects;
using Application.Repositories;
using Core.Application;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Controllers
{
    /// <summary>
    /// Controller para gestionar los Tratamientos médicos
    /// </summary>
    [ApiController]
    [Authorize(Roles = "Admin,Veterinario")]
    public class TratamientoController(
        ITratamientoRepository tratamientoRepository,
        IPacienteRepository pacienteRepository) : BaseController
    {
        private readonly ITratamientoRepository _tratamientoRepository = tratamientoRepository
            ?? throw new ArgumentNullException(nameof(tratamientoRepository));
        private readonly IPacienteRepository _pacienteRepository = pacienteRepository
            ?? throw new ArgumentNullException(nameof(pacienteRepository));

        /// <summary>
        /// Obtiene todos los tratamientos de un paciente
        /// </summary>
        [HttpGet("api/v1/[Controller]/byPaciente/{pacienteId}")]
        public async Task<IActionResult> GetByPaciente(string pacienteId)
        {
            if (string.IsNullOrWhiteSpace(pacienteId)) return BadRequest("El ID del paciente es requerido");

            var paciente = await _pacienteRepository.FindOneAsync(pacienteId);
            if (paciente == null) return NotFound($"No se encontró el paciente con Id {pacienteId}");

            var entities = await _tratamientoRepository.GetByPacienteIdAsync(pacienteId);
            var dtos = entities.Select(MapToDto).ToList();
            return Ok(dtos);
        }

        /// <summary>
        /// Obtiene tratamientos activos (no finalizados) de un paciente
        /// </summary>
        [HttpGet("api/v1/[Controller]/activos/{pacienteId}")]
        public async Task<IActionResult> GetActivos(string pacienteId)
        {
            if (string.IsNullOrWhiteSpace(pacienteId)) return BadRequest("El ID del paciente es requerido");

            var entities = await _tratamientoRepository.GetActivosAsync(pacienteId);
            var dtos = entities.Select(MapToDto).ToList();
            return Ok(dtos);
        }

        /// <summary>
        /// Obtiene un tratamiento por su ID
        /// </summary>
        [HttpGet("api/v1/[Controller]/{id}")]
        public async Task<IActionResult> GetById(string id)
        {
            if (string.IsNullOrWhiteSpace(id)) return BadRequest("El ID es requerido");

            var entity = await _tratamientoRepository.FindOneAsync(id);
            if (entity == null) return NotFound($"No se encontró el tratamiento con Id {id}");

            return Ok(MapToDto(entity));
        }

        /// <summary>
        /// Registra un nuevo tratamiento
        /// </summary>
        [HttpPost("api/v1/[Controller]")]
        public async Task<IActionResult> Create([FromBody] CreateTratamientoRequest request)
        {
            if (request is null) return BadRequest("El request no puede ser nulo");

            var paciente = await _pacienteRepository.FindOneAsync(request.PacienteId);
            if (paciente == null) return BadRequest($"No existe el paciente con Id {request.PacienteId}");

            var entity = new Domain.Entities.Tratamiento(
                request.PacienteId,
                request.Fecha,
                request.Diagnostico,
                request.Descripcion,
                request.Veterinario,
                request.Medicacion ?? "",
                request.Observaciones ?? "",
                archivosAdjuntos: request.ArchivosAdjuntos);

            if (!entity.IsValid)
                return BadRequest(entity.GetErrors().Select(e => e.ErrorMessage));

            var createdId = await _tratamientoRepository.AddAsync(entity);
            return Created($"api/v1/Tratamiento/{createdId}", new { Id = createdId });
        }

        /// <summary>
        /// Actualiza un tratamiento existente
        /// </summary>
        [HttpPut("api/v1/[Controller]")]
        public async Task<IActionResult> Update([FromBody] UpdateTratamientoRequest request)
        {
            if (request is null) return BadRequest("El request no puede ser nulo");

            var entity = await _tratamientoRepository.FindOneAsync(request.Id);
            if (entity == null) return NotFound($"No se encontró el tratamiento con Id {request.Id}");

            entity.Actualizar(request.Diagnostico, request.Descripcion, request.Medicacion ?? "", request.Observaciones ?? "", veterinario: request.Veterinario, archivosAdjuntos: request.ArchivosAdjuntos);

            if (!entity.IsValid)
                return BadRequest(entity.GetErrors().Select(e => e.ErrorMessage));

            _tratamientoRepository.Update(request.Id, entity);
            return NoContent();
        }

        /// <summary>
        /// Finaliza un tratamiento
        /// </summary>
        [HttpPut("api/v1/[Controller]/{id}/finalizar")]
        public async Task<IActionResult> Finalizar(string id)
        {
            if (string.IsNullOrWhiteSpace(id)) return BadRequest("El ID es requerido");

            var entity = await _tratamientoRepository.FindOneAsync(id);
            if (entity == null) return NotFound($"No se encontró el tratamiento con Id {id}");

            entity.Finalizar();
            _tratamientoRepository.Update(id, entity);
            return NoContent();
        }

        /// <summary>
        /// Elimina un tratamiento
        /// </summary>
        [HttpDelete("api/v1/[Controller]/{id}")]
        public async Task<IActionResult> Delete(string id)
        {
            if (string.IsNullOrWhiteSpace(id)) return BadRequest("El ID es requerido");
            var entity = await _tratamientoRepository.FindOneAsync(id);
            if (entity == null) return NotFound($"No se encontró el tratamiento con Id {id}");
            _tratamientoRepository.Remove(id);
            return NoContent();
        }

        private static TratamientoDto MapToDto(Domain.Entities.Tratamiento t) => new()
        {
            Id = t.Id,
            PacienteId = t.PacienteId,
            PacienteNombre = t.Paciente?.Nombre ?? "",
            Fecha = t.Fecha,
            Diagnostico = t.Diagnostico,
            Descripcion = t.Descripcion,
            Medicacion = t.Medicacion,
            Veterinario = t.Veterinario,
            Observaciones = t.Observaciones,
            Finalizado = t.Finalizado,
            ArchivosAdjuntos = t.ArchivosAdjuntos
        };
    }

    public class CreateTratamientoRequest
    {
        public string PacienteId { get; set; }
        public DateTime Fecha { get; set; }
        public string Diagnostico { get; set; }
        public string Descripcion { get; set; }
        public string Veterinario { get; set; }
        public string Medicacion { get; set; }
        public string Observaciones { get; set; }
        public string? ArchivosAdjuntos { get; set; }
    }

    public class UpdateTratamientoRequest
    {
        public string Id { get; set; }
        public string Diagnostico { get; set; }
        public string Descripcion { get; set; }
        public string? Veterinario { get; set; }
        public string Medicacion { get; set; }
        public string Observaciones { get; set; }
        public string? ArchivosAdjuntos { get; set; }
    }
}
