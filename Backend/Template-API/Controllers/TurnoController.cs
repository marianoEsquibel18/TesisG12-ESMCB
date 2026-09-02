using Application.DataTransferObjects;
using Application.Repositories;
using Core.Application;
using Domain.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Controllers
{
    /// <summary>
    /// Controller para gestionar Turnos con validación de disponibilidad
    /// </summary>
    [ApiController]
    [Authorize]
    public class TurnoController(
        ITurnoRepository turnoRepository,
        IPacienteRepository pacienteRepository,
        IVeterinarioRepository veterinarioRepository,
        IServicioRepository servicioRepository,
        IHistorialClinicoRepository historialClinicoRepository,
        IHorarioRepository horarioRepository) : BaseController
    {
        private readonly ITurnoRepository _turnoRepository = turnoRepository
            ?? throw new ArgumentNullException(nameof(turnoRepository));
        private readonly IPacienteRepository _pacienteRepository = pacienteRepository
            ?? throw new ArgumentNullException(nameof(pacienteRepository));
        private readonly IVeterinarioRepository _veterinarioRepository = veterinarioRepository
            ?? throw new ArgumentNullException(nameof(veterinarioRepository));
        private readonly IServicioRepository _servicioRepository = servicioRepository
            ?? throw new ArgumentNullException(nameof(servicioRepository));
        private readonly IHistorialClinicoRepository _historialClinicoRepository = historialClinicoRepository
            ?? throw new ArgumentNullException(nameof(historialClinicoRepository));
        private readonly IHorarioRepository _horarioRepository = horarioRepository
            ?? throw new ArgumentNullException(nameof(horarioRepository));

        /// <summary>
        /// Obtiene la agenda de un día (todos los turnos)
        /// </summary>
        [HttpGet("api/v1/[Controller]/agenda")]
        [Authorize(Roles = "Admin,Gerente,Veterinario,Recepcionista")]
        public async Task<IActionResult> GetAgenda([FromQuery] DateTime? fecha)
        {
            var dia = fecha ?? DateTime.Today;
            var entities = await _turnoRepository.GetByFechaAsync(dia);
            if (!IsAdmin && UserSucursalId.HasValue)
            {
                entities = entities.Where(t => t.SucursalId == UserSucursalId.Value).ToList();
            }
            var dtos = entities.Select(MapToDto).ToList();
            return Ok(dtos);
        }

        /// <summary>
        /// Obtiene turnos programados en un rango de fechas
        /// </summary>
        [HttpGet("api/v1/[Controller]/programados")]
        [Authorize(Roles = "Admin,Gerente,Veterinario,Recepcionista")]
        public async Task<IActionResult> GetProgramados([FromQuery] DateTime desde, [FromQuery] DateTime hasta, [FromQuery] string? veterinarioId = null, [FromQuery] string? searchTerm = null)
        {
            if (hasta <= desde) return BadRequest("La fecha 'hasta' debe ser posterior a 'desde'");

            var entities = await _turnoRepository.GetProgramadosAsync(desde, hasta);
            if (!IsAdmin && UserSucursalId.HasValue)
            {
                entities = entities.Where(t => t.SucursalId == UserSucursalId.Value).ToList();
            }
            
            if (!string.IsNullOrWhiteSpace(veterinarioId))
            {
                entities = entities.Where(t => t.VeterinarioId == veterinarioId).ToList();
            }
            if (!string.IsNullOrWhiteSpace(searchTerm))
            {
                var searchUpper = searchTerm.ToUpper();
                entities = entities.Where(t => 
                    (t.Paciente != null && t.Paciente.Nombre.ToUpper().Contains(searchUpper)) ||
                    (t.Motivo != null && t.Motivo.ToUpper().Contains(searchUpper))
                ).ToList();
            }
            
            var dtos = entities.Select(MapToDto).ToList();
            return Ok(dtos);
        }

        /// <summary>
        /// Obtiene turnos de un veterinario
        /// </summary>
        [HttpGet("api/v1/[Controller]/byVeterinario/{veterinarioId}")]
        [Authorize(Roles = "Admin,Gerente,Veterinario,Recepcionista")]
        public async Task<IActionResult> GetByVeterinario(string veterinarioId, [FromQuery] DateTime? desde, [FromQuery] DateTime? hasta)
        {
            if (string.IsNullOrWhiteSpace(veterinarioId)) return BadRequest("El ID del veterinario es requerido");

            var entities = await _turnoRepository.GetByVeterinarioIdAsync(veterinarioId, desde, hasta);
            if (!IsAdmin && UserSucursalId.HasValue)
            {
                entities = entities.Where(t => t.SucursalId == UserSucursalId.Value).ToList();
            }
            var dtos = entities.Select(MapToDto).ToList();
            return Ok(dtos);
        }

        /// <summary>
        /// Obtiene turnos de un paciente
        /// </summary>
        [HttpGet("api/v1/[Controller]/byPaciente/{pacienteId}")]
        [Authorize(Roles = "Admin,Gerente,Veterinario,Recepcionista")]
        public async Task<IActionResult> GetByPaciente(string pacienteId)
        {
            if (string.IsNullOrWhiteSpace(pacienteId)) return BadRequest("El ID del paciente es requerido");

            var entities = await _turnoRepository.GetByPacienteIdAsync(pacienteId);
            if (!IsAdmin && UserSucursalId.HasValue)
            {
                entities = entities.Where(t => t.SucursalId == UserSucursalId.Value).ToList();
            }
            var dtos = entities.Select(MapToDto).ToList();
            return Ok(dtos);
        }

        /// <summary>
        /// Obtiene un turno por su ID
        /// </summary>
        [HttpGet("api/v1/[Controller]/{id}")]
        [Authorize(Roles = "Admin,Gerente,Veterinario,Recepcionista")]
        public async Task<IActionResult> GetById(string id)
        {
            if (string.IsNullOrWhiteSpace(id)) return BadRequest("El ID es requerido");
            var entity = await _turnoRepository.FindOneAsync(id);
            if (entity == null) return NotFound($"No se encontró el turno con Id {id}");
            if (!IsAdmin && UserSucursalId.HasValue && entity.SucursalId != UserSucursalId.Value)
            {
                return StatusCode(403, "No tiene permisos para ver este turno de otra sucursal");
            }
            return Ok(MapToDto(entity));
        }

        [HttpPost("api/v1/[Controller]")]
        [Authorize(Roles = "Admin,Veterinario,Recepcionista")]
        public async Task<IActionResult> Create([FromBody] CreateTurnoRequest request)
        {
            if (request is null) return BadRequest("El request no puede ser nulo");

            // Validar paciente
            var paciente = await _pacienteRepository.FindOneAsync(request.PacienteId);
            if (paciente == null) return BadRequest($"No existe el paciente con Id {request.PacienteId}");

            // Validar veterinario
            var veterinario = await _veterinarioRepository.FindOneAsync(request.VeterinarioId);
            if (veterinario == null) return BadRequest($"No existe el veterinario con Id {request.VeterinarioId}");

            if (!IsAdmin && UserSucursalId.HasValue && veterinario.SucursalId != UserSucursalId.Value)
            {
                return BadRequest("No puede agendar turnos para veterinarios de otra sucursal");
            }

            // Validar servicio
            var servicio = await _servicioRepository.FindOneAsync(request.ServicioId);
            if (servicio == null) return BadRequest($"No existe el servicio con Id {request.ServicioId}");

            // Duración: usar la del servicio si no se especifica
            var duracion = request.DuracionMinutos > 0 ? request.DuracionMinutos : servicio.DuracionMinutos;

            // Validar disponibilidad horaria del veterinario (días y horas de trabajo)
            var horariosVet = (await _horarioRepository.GetByVeterinarioIdAsync(request.VeterinarioId)).Where(h => h.Activo).ToList();
            var isoDay = (int)request.FechaHora.DayOfWeek == 0 ? 7 : (int)request.FechaHora.DayOfWeek;
            var horaInicioTurno = request.FechaHora.TimeOfDay;
            var horaFinTurno = request.FechaHora.AddMinutes(duracion).TimeOfDay;

            bool estaEnHorario = horariosVet.Any(h =>
                h.DiaSemana == isoDay &&
                horaInicioTurno >= h.HoraInicio &&
                (horaFinTurno <= h.HoraFin || (h.HoraFin == TimeSpan.Zero && horaFinTurno <= new TimeSpan(24, 0, 0)))
            );

            if (!estaEnHorario)
            {
                return BadRequest($"{veterinario.NombreCompleto} no se encuentra disponible en el horario seleccionado");
            }

            // Validar superposición con turnos del veterinario
            var turnosVet = await _turnoRepository.GetByVeterinarioIdAsync(
                request.VeterinarioId, request.FechaHora.Date, request.FechaHora.Date.AddDays(1));

            var turnosActivos = turnosVet.Where(t =>
                t.Estado != EstadoTurno.Cancelado && t.Estado != EstadoTurno.Ausente);

            foreach (var turnoExistente in turnosActivos)
            {
                if (turnoExistente.SeSuperponeCon(request.FechaHora, duracion))
                {
                    return BadRequest($"El veterinario ya tiene un turno entre " +
                        $"{turnoExistente.FechaHora:HH:mm} y {turnoExistente.FechaHoraFin:HH:mm}");
                }
            }

            // Validar superposición con turnos del paciente
            var turnosPaciente = await _turnoRepository.GetByPacienteIdAsync(request.PacienteId);
            var turnosPacienteActivos = turnosPaciente.Where(t =>
                t.Estado != EstadoTurno.Cancelado && t.Estado != EstadoTurno.Ausente);

            foreach (var turnoExistente in turnosPacienteActivos)
            {
                if (turnoExistente.SeSuperponeCon(request.FechaHora, duracion))
                {
                    return BadRequest("Ya existe un turno para este paciente en el horario seleccionado");
                }
            }

            if (request.FechaHora < DateTime.Now.AddMinutes(30))
            {
                return BadRequest("Los turnos deben agendarse con al menos 30 minutos de anticipación");
            }

            var entity = new Domain.Entities.Turno(
                request.PacienteId, request.VeterinarioId, request.ServicioId,
                request.FechaHora, duracion, request.Motivo ?? "", request.Observaciones ?? "",
                archivosAdjuntos: request.ArchivosAdjuntos);

            entity.AsignarSucursal(veterinario.SucursalId);

            if (!entity.IsValid)
                return BadRequest(entity.GetErrors().Select(e => e.ErrorMessage));

            var createdId = await _turnoRepository.AddAsync(entity);
            return Created($"api/v1/Turno/{createdId}", new { Id = createdId });
        }

        /// <summary>
        /// Actualiza un turno existente con validaciones completas
        /// </summary>
        [HttpPut("api/v1/[Controller]/{id}")]
        [Authorize(Roles = "Admin,Veterinario,Recepcionista")]
        public async Task<IActionResult> Update(string id, [FromBody] CreateTurnoRequest request)
        {
            if (request is null) return BadRequest("El request no puede ser nulo");

            var entity = await _turnoRepository.FindOneAsync(id);
            if (entity == null) return NotFound($"No se encontró el turno con Id {id}");

            if (!IsAdmin && UserSucursalId.HasValue && entity.SucursalId != UserSucursalId.Value)
            {
                return StatusCode(403, "No tiene permisos para modificar este turno de otra sucursal");
            }

            // Validar paciente
            var paciente = await _pacienteRepository.FindOneAsync(request.PacienteId);
            if (paciente == null) return BadRequest($"No existe el paciente con Id {request.PacienteId}");

            // Validar veterinario
            var veterinario = await _veterinarioRepository.FindOneAsync(request.VeterinarioId);
            if (veterinario == null) return BadRequest($"No existe el veterinario con Id {request.VeterinarioId}");

            if (!IsAdmin && UserSucursalId.HasValue && veterinario.SucursalId != UserSucursalId.Value)
            {
                return BadRequest("No puede agendar turnos para veterinarios de otra sucursal");
            }

            // Validar servicio
            var servicio = await _servicioRepository.FindOneAsync(request.ServicioId);
            if (servicio == null) return BadRequest($"No existe el servicio con Id {request.ServicioId}");

            // Duración: usar la del servicio si no se especifica
            var duracion = request.DuracionMinutos > 0 ? request.DuracionMinutos : servicio.DuracionMinutos;

            bool fechaCambiada = entity.FechaHora != request.FechaHora;
            bool vetCambiado = entity.VeterinarioId != request.VeterinarioId;
            bool duracionCambiada = duracion != entity.DuracionMinutos;

            if (fechaCambiada || vetCambiado || duracionCambiada)
            {
                // Validar disponibilidad horaria del veterinario
                var horariosVet = (await _horarioRepository.GetByVeterinarioIdAsync(request.VeterinarioId)).Where(h => h.Activo).ToList();
                var isoDay = (int)request.FechaHora.DayOfWeek == 0 ? 7 : (int)request.FechaHora.DayOfWeek;
                var horaInicioTurno = request.FechaHora.TimeOfDay;
                var horaFinTurno = request.FechaHora.AddMinutes(duracion).TimeOfDay;

                bool estaEnHorario = horariosVet.Any(h =>
                    h.DiaSemana == isoDay &&
                    horaInicioTurno >= h.HoraInicio &&
                    (horaFinTurno <= h.HoraFin || (h.HoraFin == TimeSpan.Zero && horaFinTurno <= new TimeSpan(24, 0, 0)))
                );

                if (!estaEnHorario)
                {
                    return BadRequest($"{veterinario.NombreCompleto} no se encuentra disponible en el horario seleccionado");
                }

                // Validar superposición con otros turnos activos del veterinario
                var turnosVet = await _turnoRepository.GetByVeterinarioIdAsync(
                    request.VeterinarioId, request.FechaHora.Date, request.FechaHora.Date.AddDays(1));

                var turnosActivos = turnosVet.Where(t =>
                    t.Id != id && t.Estado != EstadoTurno.Cancelado && t.Estado != EstadoTurno.Ausente);

                foreach (var turnoExistente in turnosActivos)
                {
                    if (turnoExistente.SeSuperponeCon(request.FechaHora, duracion))
                    {
                        return BadRequest($"El veterinario ya tiene un turno entre " +
                            $"{turnoExistente.FechaHora:HH:mm} y {turnoExistente.FechaHoraFin:HH:mm}");
                    }
                }

                // Validar superposición con turnos del paciente
                var turnosPaciente = await _turnoRepository.GetByPacienteIdAsync(request.PacienteId);
                var turnosPacienteActivos = turnosPaciente.Where(t =>
                    t.Id != id && t.Estado != EstadoTurno.Cancelado && t.Estado != EstadoTurno.Ausente);

                foreach (var turnoExistente in turnosPacienteActivos)
                {
                    if (turnoExistente.SeSuperponeCon(request.FechaHora, duracion))
                    {
                        return BadRequest("Ya existe un turno para este paciente en el horario seleccionado");
                    }
                }
            }

            var estadoAnterior = entity.Estado;
            entity.Actualizar(request.PacienteId, request.VeterinarioId, request.ServicioId, request.FechaHora, duracion, request.Motivo ?? "", request.Observaciones ?? "", archivosAdjuntos: request.ArchivosAdjuntos);
            if (!string.IsNullOrWhiteSpace(request.Estado) && Enum.TryParse<EstadoTurno>(request.Estado, true, out var nuevoEstado))
            {
                if (nuevoEstado == EstadoTurno.Completado)
                {
                    entity.Completar(request.Observaciones ?? "");
                }
                else if (nuevoEstado == EstadoTurno.Cancelado)
                {
                    entity.Cancelar(request.Observaciones ?? "");
                }
                else if (nuevoEstado == EstadoTurno.Ausente)
                {
                    entity.Ausente();
                }
                else if (fechaCambiada)
                {
                    entity.CambiarEstado(EstadoTurno.Reprogramado);
                }
                else
                {
                    entity.CambiarEstado(nuevoEstado);
                }
            }
            else if (fechaCambiada)
            {
                entity.CambiarEstado(EstadoTurno.Reprogramado);
            }
            entity.AsignarSucursal(veterinario.SucursalId);
            _turnoRepository.Update(id, entity);

            // Sincronizar contador de inasistencias del paciente
            if (!string.IsNullOrEmpty(entity.PacienteId))
            {
                if (estadoAnterior != EstadoTurno.Ausente && entity.Estado == EstadoTurno.Ausente)
                {
                    var pac = await _pacienteRepository.FindOneAsync(entity.PacienteId);
                    if (pac != null)
                    {
                        pac.IncrementarInasistencias();
                        _pacienteRepository.Update(pac.Id, pac);
                    }
                }
                else if (estadoAnterior == EstadoTurno.Ausente && entity.Estado != EstadoTurno.Ausente)
                {
                    var pac = await _pacienteRepository.FindOneAsync(entity.PacienteId);
                    if (pac != null)
                    {
                        pac.DecrementarInasistencias();
                        _pacienteRepository.Update(pac.Id, pac);
                    }
                }
            }

            return NoContent();
        }

        /// <summary>
        /// Reprogramar un turno con validación de disponibilidad horaria
        /// </summary>
        [HttpPut("api/v1/[Controller]/{id}/reprogramar")]
        [Authorize(Roles = "Admin,Veterinario,Recepcionista")]
        public async Task<IActionResult> Reprogramar(string id, [FromBody] ReprogramarTurnoRequest request)
        {
            if (request is null) return BadRequest("El request no puede ser nulo");

            var entity = await _turnoRepository.FindOneAsync(id);
            if (entity == null) return NotFound($"No se encontró el turno con Id {id}");

            if (!IsAdmin && UserSucursalId.HasValue && entity.SucursalId != UserSucursalId.Value)
            {
                return StatusCode(403, "No tiene permisos para reprogramar un turno de otra sucursal");
            }

            if (entity.Estado == EstadoTurno.Completado || entity.Estado == EstadoTurno.Cancelado)
                return BadRequest("No se puede reprogramar un turno completado o cancelado");

            var duracion = request.DuracionMinutos > 0 ? request.DuracionMinutos : entity.DuracionMinutos;

            // Validar disponibilidad horaria del veterinario
            var vet = await _veterinarioRepository.FindOneAsync(entity.VeterinarioId);
            var vetNombre = vet?.NombreCompleto ?? "El veterinario";
            var horariosVet = (await _horarioRepository.GetByVeterinarioIdAsync(entity.VeterinarioId)).Where(h => h.Activo).ToList();
            var isoDay = (int)request.NuevaFechaHora.DayOfWeek == 0 ? 7 : (int)request.NuevaFechaHora.DayOfWeek;
            var horaInicioTurno = request.NuevaFechaHora.TimeOfDay;
            var horaFinTurno = request.NuevaFechaHora.AddMinutes(duracion).TimeOfDay;

            bool estaEnHorario = horariosVet.Any(h =>
                h.DiaSemana == isoDay &&
                horaInicioTurno >= h.HoraInicio &&
                (horaFinTurno <= h.HoraFin || (h.HoraFin == TimeSpan.Zero && horaFinTurno <= new TimeSpan(24, 0, 0)))
            );

            if (!estaEnHorario)
            {
                return BadRequest($"{vetNombre} no se encuentra disponible en el horario seleccionado");
            }

            // Validar superposición
            var turnosVet = await _turnoRepository.GetByVeterinarioIdAsync(
                entity.VeterinarioId, request.NuevaFechaHora.Date, request.NuevaFechaHora.Date.AddDays(1));

            var turnosActivos = turnosVet.Where(t =>
                t.Id != id && t.Estado != EstadoTurno.Cancelado && t.Estado != EstadoTurno.Ausente);

            foreach (var turnoExistente in turnosActivos)
            {
                if (turnoExistente.SeSuperponeCon(request.NuevaFechaHora, duracion))
                {
                    return BadRequest($"El veterinario ya tiene un turno entre " +
                        $"{turnoExistente.FechaHora:HH:mm} y {turnoExistente.FechaHoraFin:HH:mm}");
                }
            }

            // Validar superposición con turnos del paciente
            var turnosPaciente = await _turnoRepository.GetByPacienteIdAsync(entity.PacienteId);
            var turnosPacienteActivos = turnosPaciente.Where(t =>
                t.Id != id && t.Estado != EstadoTurno.Cancelado && t.Estado != EstadoTurno.Ausente);

            foreach (var turnoExistente in turnosPacienteActivos)
            {
                if (turnoExistente.SeSuperponeCon(request.NuevaFechaHora, duracion))
                {
                    return BadRequest("Ya existe un turno para este paciente en el horario seleccionado");
                }
            }

            entity.Reprogramar(request.NuevaFechaHora, duracion);
            _turnoRepository.Update(id, entity);
            return NoContent();
        }

        /// <summary>
        /// Confirmar turno
        /// </summary>
        [HttpPut("api/v1/[Controller]/{id}/confirmar")]
        [Authorize(Roles = "Admin,Veterinario,Recepcionista")]
        public async Task<IActionResult> Confirmar(string id)
        {
            var entity = await _turnoRepository.FindOneAsync(id);
            if (entity == null) return NotFound($"No se encontró el turno con Id {id}");

            if (!IsAdmin && UserSucursalId.HasValue && entity.SucursalId != UserSucursalId.Value)
            {
                return StatusCode(403, "No tiene permisos para modificar un turno de otra sucursal");
            }

            entity.Confirmar();
            _turnoRepository.Update(id, entity);
            return NoContent();
        }

        /// <summary>
        /// Marcar turno en curso
        /// </summary>
        [HttpPut("api/v1/[Controller]/{id}/encurso")]
        [Authorize(Roles = "Admin,Veterinario,Recepcionista")]
        public async Task<IActionResult> EnCurso(string id)
        {
            var entity = await _turnoRepository.FindOneAsync(id);
            if (entity == null) return NotFound($"No se encontró el turno con Id {id}");

            if (!IsAdmin && UserSucursalId.HasValue && entity.SucursalId != UserSucursalId.Value)
            {
                return StatusCode(403, "No tiene permisos para modificar un turno de otra sucursal");
            }

            entity.EnCurso();
            _turnoRepository.Update(id, entity);
            return NoContent();
        }

        /// <summary>
        /// Completar turno y registrar automáticamente una consulta en el historial clínico
        /// </summary>
        [HttpPut("api/v1/[Controller]/{id}/completar")]
        [Authorize(Roles = "Admin,Veterinario,Recepcionista")]
        public async Task<IActionResult> Completar(string id, [FromBody] string observaciones = "")
        {
            var entity = await _turnoRepository.FindOneAsync(id);
            if (entity == null) return NotFound($"No se encontró el turno con Id {id}");

            if (!IsAdmin && UserSucursalId.HasValue && entity.SucursalId != UserSucursalId.Value)
            {
                return StatusCode(403, "No tiene permisos para modificar un turno de otra sucursal");
            }

            entity.Completar(observaciones);
            _turnoRepository.Update(id, entity);

            // Cargar las relaciones para obtener nombres del veterinario y servicio
            var turnoExpandido = await _turnoRepository.GetByIdWithIncludesAsync(id);

            // Auto-crear entrada en historial clínico del paciente
            try
            {
                var vet = turnoExpandido?.Veterinario;
                var svc = turnoExpandido?.Servicio;
                var veterinarioNombre = vet?.NombreCompleto ?? "No especificado";
                var servicioNombre = svc?.Nombre ?? "";
                var motivo = !string.IsNullOrWhiteSpace(entity.Motivo) 
                    ? entity.Motivo 
                    : $"Consulta - {servicioNombre}".Trim(' ', '-');

                var historial = new Domain.Entities.HistorialClinico(
                    pacienteId: entity.PacienteId,
                    fecha: DateTime.Now,
                    motivo: motivo,
                    veterinario: veterinarioNombre,
                    observaciones: !string.IsNullOrWhiteSpace(observaciones) 
                        ? observaciones 
                        : $"Generado automáticamente al completar turno del {entity.FechaHora:dd/MM/yyyy HH:mm}. Servicio: {servicioNombre}",
                    archivosAdjuntos: entity.ArchivosAdjuntos);

                if (historial.IsValid)
                {
                    await _historialClinicoRepository.AddAsync(historial);
                }
            }
            catch (Exception ex)
            {
                // Log pero no fallar — el turno ya fue completado
                Console.WriteLine($"[WARN] Error al auto-crear historial clínico desde turno {id}: {ex.Message}");
            }

            return NoContent();
        }

        /// <summary>
        /// Cancelar turno
        /// </summary>
        [HttpPut("api/v1/[Controller]/{id}/cancelar")]
        [Authorize(Roles = "Admin,Veterinario,Recepcionista")]
        public async Task<IActionResult> Cancelar(string id, [FromBody] string motivo = "")
        {
            var entity = await _turnoRepository.FindOneAsync(id);
            if (entity == null) return NotFound($"No se encontró el turno con Id {id}");

            if (!IsAdmin && UserSucursalId.HasValue && entity.SucursalId != UserSucursalId.Value)
            {
                return StatusCode(403, "No tiene permisos para modificar un turno de otra sucursal");
            }

            entity.Cancelar(motivo);
            _turnoRepository.Update(id, entity);
            return NoContent();
        }

        /// <summary>
        /// Marcar como ausente
        /// </summary>
        [HttpPut("api/v1/[Controller]/{id}/ausente")]
        [Authorize(Roles = "Admin,Veterinario,Recepcionista")]
        public async Task<IActionResult> Ausente(string id)
        {
            var entity = await _turnoRepository.FindOneAsync(id);
            if (entity == null) return NotFound($"No se encontró el turno con Id {id}");

            if (!IsAdmin && UserSucursalId.HasValue && entity.SucursalId != UserSucursalId.Value)
            {
                return StatusCode(403, "No tiene permisos para modificar un turno de otra sucursal");
            }

            var estadoAnterior = entity.Estado;
            entity.Ausente();
            _turnoRepository.Update(id, entity);

            if (estadoAnterior != EstadoTurno.Ausente && !string.IsNullOrEmpty(entity.PacienteId))
            {
                var pac = await _pacienteRepository.FindOneAsync(entity.PacienteId);
                if (pac != null)
                {
                    pac.IncrementarInasistencias();
                    _pacienteRepository.Update(pac.Id, pac);
                }
            }

            return NoContent();
        }

        private static TurnoDto MapToDto(Domain.Entities.Turno t) => new()
        {
            Id = t.Id,
            PacienteId = t.PacienteId,
            PacienteNombre = t.Paciente?.Nombre ?? "",
            PropietarioNombre = t.Paciente?.Propietario?.Nombre != null ? $"{t.Paciente.Propietario.Nombre} {t.Paciente.Propietario.Apellido}".Trim() : "",
            VeterinarioId = t.VeterinarioId,
            VeterinarioNombre = t.Veterinario?.NombreCompleto ?? "",
            ServicioId = t.ServicioId,
            ServicioNombre = t.Servicio?.Nombre ?? "",
            FechaHora = t.FechaHora,
            FechaHoraFin = t.FechaHoraFin,
            DuracionMinutos = t.DuracionMinutos,
            Estado = t.Estado.ToString(),
            Motivo = t.Motivo,
            Observaciones = t.Observaciones,
            SucursalId = t.SucursalId,
            SucursalNombre = t.Sucursal?.Nombre ?? "",
            FechaCreacion = t.FechaCreacion,
            ArchivosAdjuntos = t.ArchivosAdjuntos
        };
    }

    public class CreateTurnoRequest
    {
        public string PacienteId { get; set; }
        public string VeterinarioId { get; set; }
        public int ServicioId { get; set; }
        public DateTime FechaHora { get; set; }
        public int DuracionMinutos { get; set; }
        public string Motivo { get; set; }
        public string Observaciones { get; set; }
        public string? Estado { get; set; }
        public string? ArchivosAdjuntos { get; set; }
    }

    public class ReprogramarTurnoRequest
    {
        public DateTime NuevaFechaHora { get; set; }
        public int DuracionMinutos { get; set; }
    }
}
