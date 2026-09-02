using Application.DataTransferObjects;
using Application.Repositories;
using Core.Application;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Controllers
{
    /// <summary>
    /// Controller para gestionar los Pacientes (mascotas)
    /// </summary>
    [ApiController]
    [Authorize]
    public class PacienteController(
        IPacienteRepository pacienteRepository,
        IPropietarioRepository propietarioRepository,
        IEspecieRepository especieRepository,
        IRazaRepository razaRepository,
        ITurnoRepository turnoRepository) : BaseController
    {
        private readonly IPacienteRepository _pacienteRepository = pacienteRepository 
            ?? throw new ArgumentNullException(nameof(pacienteRepository));
        private readonly IPropietarioRepository _propietarioRepository = propietarioRepository 
            ?? throw new ArgumentNullException(nameof(propietarioRepository));
        private readonly IEspecieRepository _especieRepository = especieRepository 
            ?? throw new ArgumentNullException(nameof(especieRepository));
        private readonly IRazaRepository _razaRepository = razaRepository 
            ?? throw new ArgumentNullException(nameof(razaRepository));
        private readonly ITurnoRepository _turnoRepository = turnoRepository 
            ?? throw new ArgumentNullException(nameof(turnoRepository));

        /// <summary>
        /// Obtiene todos los pacientes
        /// </summary>
        [HttpGet("api/v1/[Controller]")]
        [Authorize(Roles = "Admin,Gerente,Veterinario,Recepcionista")]
        public async Task<IActionResult> GetAll(bool soloActivos = true)
        {
            var entities = soloActivos 
                ? await _pacienteRepository.GetActivosAsync() 
                : await _pacienteRepository.FindAllAsync();

            var dtos = entities.Select(MapToDto).ToList();
            return Ok(dtos);
        }

        /// <summary>
        /// Busca pacientes por nombre
        /// </summary>
        [HttpGet("api/v1/[Controller]/search")]
        [Authorize(Roles = "Admin,Gerente,Veterinario,Recepcionista")]
        public async Task<IActionResult> Search([FromQuery] string nombre)
        {
            if (string.IsNullOrWhiteSpace(nombre)) 
                return BadRequest("Debe proporcionar un término de búsqueda");

            var entities = await _pacienteRepository.SearchByNombreAsync(nombre);
            var dtos = entities.Select(MapToDto).ToList();
            return Ok(dtos);
        }

        /// <summary>
        /// Obtiene pacientes por propietario
        /// </summary>
        [HttpGet("api/v1/[Controller]/byPropietario/{propietarioId}")]
        [Authorize(Roles = "Admin,Gerente,Veterinario,Recepcionista")]
        public async Task<IActionResult> GetByPropietario(string propietarioId)
        {
            if (string.IsNullOrWhiteSpace(propietarioId)) 
                return BadRequest("El ID del propietario es requerido");

            var entities = await _pacienteRepository.GetByPropietarioIdAsync(propietarioId);
            var dtos = entities.Select(MapToDto).ToList();
            return Ok(dtos);
        }

        /// <summary>
        /// Obtiene pacientes por especie
        /// </summary>
        [HttpGet("api/v1/[Controller]/byEspecie/{especieId}")]
        [Authorize(Roles = "Admin,Gerente,Veterinario,Recepcionista")]
        public async Task<IActionResult> GetByEspecie(int especieId)
        {
            if (especieId <= 0) return BadRequest("El ID de especie debe ser mayor a 0");

            var entities = await _pacienteRepository.GetByEspecieIdAsync(especieId);
            var dtos = entities.Select(MapToDto).ToList();
            return Ok(dtos);
        }

        /// <summary>
        /// Obtiene un paciente por su ID
        /// </summary>
        [HttpGet("api/v1/[Controller]/{id}")]
        [Authorize(Roles = "Admin,Gerente,Veterinario,Recepcionista")]
        public async Task<IActionResult> GetById(string id)
        {
            if (string.IsNullOrWhiteSpace(id)) return BadRequest("El ID es requerido");

            var entity = await _pacienteRepository.FindOneAsync(id);
            if (entity == null) return NotFound($"No se encontró el paciente con Id {id}");

            return Ok(MapToDto(entity));
        }

        /// <summary>
        /// Crea un nuevo paciente
        /// </summary>
        [HttpPost("api/v1/[Controller]")]
        [Authorize(Roles = "Admin,Veterinario,Recepcionista")]
        public async Task<IActionResult> Create([FromBody] CreatePacienteRequest request)
        {
            if (request is null) return BadRequest("El request no puede ser nulo");

            // Validar que exista el propietario
            var propietario = await _propietarioRepository.FindOneAsync(request.PropietarioId);
            if (propietario == null) 
                return BadRequest($"No existe el propietario con Id {request.PropietarioId}");

            // Validar que exista la especie
            var especie = await _especieRepository.FindOneAsync(request.EspecieId);
            if (especie == null) 
                return BadRequest($"No existe la especie con Id {request.EspecieId}");

            // Validar la raza si se proporciona
            if (request.RazaId.HasValue)
            {
                var raza = await _razaRepository.FindOneAsync(request.RazaId.Value);
                if (raza == null) 
                    return BadRequest($"No existe la raza con Id {request.RazaId}");
                if (raza.EspecieId != request.EspecieId)
                    return BadRequest("La raza no pertenece a la especie seleccionada");
            }

            var entity = new Domain.Entities.Paciente(
                request.Nombre,
                request.EspecieId,
                request.PropietarioId,
                request.Sexo,
                request.RazaId,
                request.FechaNacimiento,
                request.FotoUrl ?? "",
                request.Observaciones ?? "");

            if (!entity.IsValid)
                return BadRequest(entity.GetErrors().Select(e => e.ErrorMessage));

            var createdId = await _pacienteRepository.AddAsync(entity);
            return Created($"api/v1/Paciente/{createdId}", new { Id = createdId });
        }

        /// <summary>
        /// Actualiza un paciente existente
        /// </summary>
        [HttpPut("api/v1/[Controller]/{id}")]
        [Authorize(Roles = "Admin,Veterinario,Recepcionista")]
        public async Task<IActionResult> Update(string id, [FromBody] UpdatePacienteRequest request)
        {
            if (request is null) return BadRequest("El request no puede ser nulo");

            var entity = await _pacienteRepository.FindOneAsync(id);
            if (entity == null) return NotFound($"No se encontró el paciente con Id {id}");

            entity.Actualizar(request.Nombre, request.Sexo, request.FechaNacimiento, request.Observaciones ?? "");

            if (request.FotoUrl != null)
                entity.ActualizarFoto(request.FotoUrl);

            if (!string.IsNullOrWhiteSpace(request.PropietarioId))
            {
                var propietario = await _propietarioRepository.FindOneAsync(request.PropietarioId);
                if (propietario != null)
                {
                    entity.CambiarPropietario(request.PropietarioId);
                }
            }

            if (request.EspecieId > 0)
            {
                entity.CambiarEspecie(request.EspecieId, request.RazaId);
            }

            if (!entity.IsValid)
                return BadRequest(entity.GetErrors().Select(e => e.ErrorMessage));

            _pacienteRepository.Update(id, entity);
            return NoContent();
        }

        /// <summary>
        /// Cambia el propietario de un paciente
        /// </summary>
        [HttpPut("api/v1/[Controller]/{id}/cambiarPropietario")]
        [Authorize(Roles = "Admin,Veterinario,Recepcionista")]
        public async Task<IActionResult> CambiarPropietario(string id, [FromBody] string nuevoPropietarioId)
        {
            if (string.IsNullOrWhiteSpace(id)) return BadRequest("El ID es requerido");

            var entity = await _pacienteRepository.FindOneAsync(id);
            if (entity == null) return NotFound($"No se encontró el paciente con Id {id}");

            var propietario = await _propietarioRepository.FindOneAsync(nuevoPropietarioId);
            if (propietario == null) 
                return BadRequest($"No existe el propietario con Id {nuevoPropietarioId}");

            entity.CambiarPropietario(nuevoPropietarioId);
            _pacienteRepository.Update(id, entity);
            return NoContent();
        }

        /// <summary>
        /// Elimina (desactiva) un paciente y elimina sus turnos asociados
        /// </summary>
        [HttpDelete("api/v1/[Controller]/{id}")]
        [Authorize(Roles = "Admin,Veterinario,Recepcionista")]
        public async Task<IActionResult> Delete(string id)
        {
            if (string.IsNullOrWhiteSpace(id)) return BadRequest("El ID es requerido");

            var entity = await _pacienteRepository.FindOneAsync(id);
            if (entity == null) return NotFound($"No se encontró el paciente con Id {id}");

            entity.Desactivar();
            _pacienteRepository.Update(id, entity);

            try
            {
                var turnos = await _turnoRepository.GetByPacienteIdAsync(id);
                if (turnos != null)
                {
                    foreach (var t in turnos)
                    {
                        _turnoRepository.Remove(t.Id);
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[WARN] Error al eliminar turnos del paciente {id}: {ex.Message}");
            }

            return NoContent();
        }

        private static PacienteDto MapToDto(Domain.Entities.Paciente p) => new()
        {
            Id = p.Id,
            Nombre = p.Nombre,
            EspecieId = p.EspecieId,
            EspecieNombre = p.Especie?.Nombre ?? "",
            RazaId = p.RazaId,
            RazaNombre = p.Raza?.Nombre ?? "",
            Sexo = p.Sexo,
            FechaNacimiento = p.FechaNacimiento,
            EdadEnAnios = p.EdadEnAnios,
            PropietarioId = p.PropietarioId,
            PropietarioNombre = p.Propietario?.NombreCompleto ?? "",
            FotoUrl = p.FotoUrl,
            Observaciones = p.Observaciones,
            FechaRegistro = p.FechaRegistro,
            Activo = p.Activo,
            ContadorInasistencias = p.ContadorInasistencias
        };
    }

    public class CreatePacienteRequest
    {
        public string Nombre { get; set; }
        public int EspecieId { get; set; }
        public int? RazaId { get; set; }
        public string Sexo { get; set; }
        public DateTime? FechaNacimiento { get; set; }
        public string PropietarioId { get; set; }
        public string FotoUrl { get; set; }
        public string Observaciones { get; set; }
    }

    public class UpdatePacienteRequest
    {
        public string Id { get; set; }
        public string Nombre { get; set; }
        public string Sexo { get; set; }
        public DateTime? FechaNacimiento { get; set; }
        public string FotoUrl { get; set; }
        public string Observaciones { get; set; }
        public string PropietarioId { get; set; }
        public int EspecieId { get; set; }
        public int? RazaId { get; set; }
    }
}
