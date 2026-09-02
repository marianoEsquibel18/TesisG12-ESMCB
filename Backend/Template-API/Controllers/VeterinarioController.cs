using Application.DataTransferObjects;
using Application.Repositories;
using Core.Application;
using Domain.Entities;
using Microsoft.AspNetCore.Mvc;

namespace Controllers
{
    [ApiController]
    public class VeterinarioController(
        IVeterinarioRepository veterinarioRepository,
        IHorarioRepository horarioRepository) : BaseController
    {
        private readonly IVeterinarioRepository _repository = veterinarioRepository
            ?? throw new ArgumentNullException(nameof(veterinarioRepository));
        private readonly IHorarioRepository _horarioRepository = horarioRepository
            ?? throw new ArgumentNullException(nameof(horarioRepository));

        [HttpGet("api/v1/[Controller]")]
        public async Task<IActionResult> GetAll(bool soloActivos = true)
        {
            var entities = soloActivos
                ? await _repository.GetActivosAsync()
                : await _repository.FindAllAsync();

            if (!IsAdmin && UserSucursalId.HasValue)
            {
                entities = entities.Where(v => v.SucursalId == UserSucursalId.Value).ToList();
            }

            var allHorarios = (await _horarioRepository.GetActivosAsync()).ToList();

            var dtos = entities.Select(v => MapToDto(v, allHorarios.Where(h => h.VeterinarioId == v.Id))).ToList();
            return Ok(new QueryResult<VeterinarioDto>(dtos, dtos.Count, 1, 10));
        }

        [HttpGet("api/v1/[Controller]/{id}")]
        public async Task<IActionResult> GetById(string id)
        {
            if (string.IsNullOrWhiteSpace(id)) return BadRequest("El ID es requerido");
            var entity = await _repository.FindOneAsync(id);
            if (entity == null) return NotFound($"No se encontró el veterinario con Id {id}");

            if (!IsAdmin && UserSucursalId.HasValue && entity.SucursalId != UserSucursalId.Value)
            {
                return StatusCode(403, "No tiene permisos para ver un veterinario de otra sucursal");
            }

            var horarios = await _horarioRepository.GetByVeterinarioIdAsync(id);
            return Ok(MapToDto(entity, horarios));
        }

        [HttpGet("api/v1/[Controller]/search")]
        public async Task<IActionResult> Search([FromQuery] string nombre)
        {
            if (string.IsNullOrWhiteSpace(nombre)) return BadRequest("Debe proporcionar un término de búsqueda");
            var entities = await _repository.SearchByNombreAsync(nombre);

            if (!IsAdmin && UserSucursalId.HasValue)
            {
                entities = entities.Where(v => v.SucursalId == UserSucursalId.Value).ToList();
            }

            var allHorarios = (await _horarioRepository.GetActivosAsync()).ToList();
            return Ok(entities.Select(v => MapToDto(v, allHorarios.Where(h => h.VeterinarioId == v.Id))).ToList());
        }

        [HttpGet("api/v1/[Controller]/byMatricula/{matricula}")]
        public async Task<IActionResult> GetByMatricula(string matricula)
        {
            var entity = await _repository.GetByMatriculaAsync(matricula);
            if (entity == null) return NotFound($"No se encontró veterinario con matrícula {matricula}");

            if (!IsAdmin && UserSucursalId.HasValue && entity.SucursalId != UserSucursalId.Value)
            {
                return StatusCode(403, "No tiene permisos para ver un veterinario de otra sucursal");
            }

            var horarios = await _horarioRepository.GetByVeterinarioIdAsync(entity.Id);
            return Ok(MapToDto(entity, horarios));
        }

        [HttpPost("api/v1/[Controller]")]
        public async Task<IActionResult> Create([FromBody] CreateVeterinarioRequest request)
        {
            if (request is null) return BadRequest("El request no puede ser nulo");

            var existing = await _repository.GetByMatriculaAsync(request.Matricula);
            if (existing != null) return BadRequest($"Ya existe un veterinario con la matrícula '{request.Matricula}'");

            int targetSucursalId = 1; // default fallback (Sucursal Central)
            if (UserSucursalId.HasValue)
            {
                targetSucursalId = UserSucursalId.Value;
            }
            else if (request.SucursalId.HasValue)
            {
                targetSucursalId = request.SucursalId.Value;
            }

            var entity = new Veterinario(
                request.Nombre, request.Apellido, request.Matricula,
                request.Telefono, request.Email ?? "", request.Especialidad ?? "",
                targetSucursalId);

            if (!entity.IsValid)
                return BadRequest(entity.GetErrors().Select(e => e.ErrorMessage));

            var horarioValidationError = ValidarHorariosLaborales(request.Horarios);
            if (horarioValidationError != null)
            {
                return BadRequest(new[] { horarioValidationError });
            }

            var createdIdObj = await _repository.AddAsync(entity);
            var createdId = createdIdObj?.ToString() ?? entity.Id;

            // Guardar horarios configurados
            if (request.Horarios != null && request.Horarios.Any())
            {
                foreach (var hReq in request.Horarios)
                {
                    if (TimeSpan.TryParse(hReq.HoraInicio, out var hInicio) &&
                        TimeSpan.TryParse(hReq.HoraFin, out var hFin))
                    {
                        var horario = new Horario(createdId, hReq.DiaSemana, hInicio, hFin, hReq.TipoHorarioId);
                        if (horario.IsValid)
                        {
                            await _horarioRepository.AddAsync(horario);
                        }
                    }
                }
            }

            return Created($"api/v1/Veterinario/{createdId}", new { Id = createdId });
        }

        [HttpPut("api/v1/[Controller]")]
        public async Task<IActionResult> Update([FromBody] UpdateVeterinarioRequest request)
        {
            if (request is null) return BadRequest("El request no puede ser nulo");
            var entity = await _repository.FindOneAsync(request.Id);
            if (entity == null) return NotFound($"No se encontró el veterinario con Id {request.Id}");

            if (!IsAdmin && UserSucursalId.HasValue && entity.SucursalId != UserSucursalId.Value)
            {
                return StatusCode(403, "No tiene permisos para modificar un profesional de otra sucursal");
            }

            int targetSucursalId = entity.SucursalId;
            if (!IsAdmin && UserSucursalId.HasValue)
            {
                targetSucursalId = UserSucursalId.Value;
            }
            else if (request.SucursalId.HasValue)
            {
                targetSucursalId = request.SucursalId.Value;
            }

            entity.Actualizar(request.Nombre, request.Apellido, request.Matricula, request.Telefono,
                request.Email ?? "", request.Especialidad ?? "");
            entity.AsignarSucursal(targetSucursalId);

            if (!entity.IsValid)
                return BadRequest(entity.GetErrors().Select(e => e.ErrorMessage));

            var horarioValidationError = ValidarHorariosLaborales(request.Horarios);
            if (horarioValidationError != null)
            {
                return BadRequest(new[] { horarioValidationError });
            }

            _repository.Update(request.Id, entity);

            // Actualizar horarios: borrar anteriores y agregar nuevos si se proveyeron
            if (request.Horarios != null)
            {
                await _horarioRepository.DeleteByVeterinarioIdAsync(request.Id);
                foreach (var hReq in request.Horarios)
                {
                    if (TimeSpan.TryParse(hReq.HoraInicio, out var hInicio) &&
                        TimeSpan.TryParse(hReq.HoraFin, out var hFin))
                    {
                        var horario = new Horario(request.Id, hReq.DiaSemana, hInicio, hFin, hReq.TipoHorarioId);
                        if (horario.IsValid)
                        {
                            await _horarioRepository.AddAsync(horario);
                        }
                    }
                }
            }

            return NoContent();
        }

        [HttpDelete("api/v1/[Controller]/{id}")]
        public async Task<IActionResult> Delete(string id)
        {
            if (string.IsNullOrWhiteSpace(id)) return BadRequest("El ID es requerido");
            var entity = await _repository.FindOneAsync(id);
            if (entity == null) return NotFound($"No se encontró el veterinario con Id {id}");

            if (!IsAdmin && UserSucursalId.HasValue && entity.SucursalId != UserSucursalId.Value)
            {
                return StatusCode(403, "No tiene permisos para modificar un profesional de otra sucursal");
            }

            entity.Desactivar();
            _repository.Update(id, entity);
            return NoContent();
        }

        private static VeterinarioDto MapToDto(Veterinario v, IEnumerable<Horario> horarios)
        {
            var horariosList = horarios.Select(h => new HorarioDto
            {
                Id = h.Id,
                VeterinarioId = h.VeterinarioId,
                DiaSemana = h.DiaSemana,
                HoraInicio = h.HoraInicio.ToString(@"hh\:mm"),
                HoraFin = h.HoraFin.ToString(@"hh\:mm"),
                TipoHorarioId = h.TipoHorarioId,
                TipoHorarioNombre = h.TipoHorario?.Nombre ?? (h.TipoHorarioId == 2 ? "Guardia" : "Normal"),
                Activo = h.Activo
            }).ToList();

            var disponibilidad = CalcularDisponibilidad(horariosList);

            return new VeterinarioDto
            {
                Id = v.Id,
                Nombre = v.Nombre,
                Apellido = v.Apellido,
                NombreCompleto = v.NombreCompleto,
                Matricula = v.Matricula,
                Telefono = v.Telefono,
                Email = v.Email,
                Especialidad = v.Especialidad,
                Activo = v.Activo,
                SucursalId = v.SucursalId,
                SucursalNombre = v.Sucursal?.Nombre ?? "",
                Horarios = horariosList,
                DisponibilidadActual = disponibilidad
            };
        }

        public static string CalcularDisponibilidad(List<HorarioDto> horarios)
        {
            if (horarios == null || !horarios.Any()) return "No Disponible";

            var now = DateTime.Now;
            int currentDayIso = now.DayOfWeek switch
            {
                DayOfWeek.Monday => 1,
                DayOfWeek.Tuesday => 2,
                DayOfWeek.Wednesday => 3,
                DayOfWeek.Thursday => 4,
                DayOfWeek.Friday => 5,
                DayOfWeek.Saturday => 6,
                DayOfWeek.Sunday => 7,
                _ => 1
            };

            var currentTime = now.TimeOfDay;

            foreach (var h in horarios.Where(x => x.Activo && x.DiaSemana == currentDayIso))
            {
                if (TimeSpan.TryParse(h.HoraInicio, out var start) && TimeSpan.TryParse(h.HoraFin, out var end))
                {
                    if (currentTime >= start && currentTime <= end)
                    {
                        if (h.TipoHorarioId == 2 || h.TipoHorarioNombre.Equals("Guardia", StringComparison.OrdinalIgnoreCase))
                        {
                            return "Guardia";
                        }
                        return "Disponible";
                    }
                }
            }

            return "No Disponible";
        }

        private string? ValidarHorariosLaborales(List<CreateHorarioRequest>? horarios)
        {
            if (horarios == null || !horarios.Any()) return null;

            // Validar que no haya días duplicados (tanto normal como guardia)
            var diasDuplicados = horarios
                .GroupBy(h => h.DiaSemana)
                .Where(g => g.Count() > 1)
                .Select(g => g.Key)
                .ToList();

            if (diasDuplicados.Any())
            {
                return "No se puede registrar más de un horario laboral o de guardia para el mismo día de la semana";
            }

            double totalHorasSemanales = 0;
            var horasPorDia = new Dictionary<int, double>();

            foreach (var h in horarios)
            {
                // Solo validar horarios normales (TipoHorarioId = 1)
                if (h.TipoHorarioId != 1) continue;

                if (TimeSpan.TryParse(h.HoraInicio, out var inicio) &&
                     TimeSpan.TryParse(h.HoraFin, out var fin))
                {
                    if (fin <= inicio) continue; // Rango inválido, ignorar

                    var diff = (fin - inicio).TotalHours;
                    totalHorasSemanales += diff;

                    if (!horasPorDia.ContainsKey(h.DiaSemana))
                    {
                        horasPorDia[h.DiaSemana] = 0;
                    }
                    horasPorDia[h.DiaSemana] += diff;
                }
            }

            // Validar límite diario
            foreach (var kv in horasPorDia)
            {
                if (kv.Value > 8.0)
                {
                    return "El profesional solo puede trabajar 8 horas al dia";
                }
            }

            // Validar límite semanal
            if (totalHorasSemanales > 48.0)
            {
                return "El profesional solo puede trabajar 48 horas semanales";
            }

            return null;
        }
    }

    public class CreateVeterinarioRequest
    {
        public string Nombre { get; set; } = string.Empty;
        public string Apellido { get; set; } = string.Empty;
        public string Matricula { get; set; } = string.Empty;
        public string Telefono { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Especialidad { get; set; } = string.Empty;
        public int? SucursalId { get; set; }
        public List<CreateHorarioRequest>? Horarios { get; set; }
    }

    public class UpdateVeterinarioRequest
    {
        public string Id { get; set; } = string.Empty;
        public string Nombre { get; set; } = string.Empty;
        public string Apellido { get; set; } = string.Empty;
        public string Matricula { get; set; } = string.Empty;
        public string Telefono { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Especialidad { get; set; } = string.Empty;
        public int? SucursalId { get; set; }
        public List<CreateHorarioRequest>? Horarios { get; set; }
    }
}
