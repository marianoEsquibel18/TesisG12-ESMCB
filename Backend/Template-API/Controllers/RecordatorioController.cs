using Application.Repositories;
using Domain.Entities;
using Microsoft.AspNetCore.Mvc;

namespace Controllers
{
    /// <summary>
    /// Controller de recordatorios y alertas automáticas basados en reglas de negocio
    /// </summary>
    [ApiController]
    public class RecordatorioController(
        IPacienteRepository pacienteRepo,
        IProductoRepository productoRepo,
        ITurnoRepository turnoRepo,
        IRegistroVacunacionRepository vacunacionRepo,
        ITratamientoRepository tratamientoRepo,
        IVacunaRepository vacunaRepo) : BaseController
    {
        // ═══════════════════════════════
        //  DASHBOARD DE ALERTAS
        // ═══════════════════════════════

        /// <summary>
        /// Panel unificado de todas las alertas activas del sistema
        /// </summary>
        [HttpGet("api/v1/Recordatorio/dashboard")]
        public async Task<IActionResult> Dashboard()
        {
            var vacPendientes = await GetVacunacionesPendientesInternal(14);
            var turnosHoy = await GetTurnosDelDiaInternal();
            var stockBajo = await GetStockBajoInternal();
            var tratActivos = await GetTratamientosActivosInternal();
            var turnosVencidos = await GetTurnosVencidosInternal();

            var alertas = new List<AlertaDto>();

            // Alertas críticas (rojo)
            foreach (var s in stockBajo.Where(x => x.StockActual == 0))
                alertas.Add(new AlertaDto("CRITICA", "Stock", $"⛔ SIN STOCK: {s.Producto}", s.Detalle) { EntidadId = s.ProductoId });

            foreach (var t in turnosVencidos)
                alertas.Add(new AlertaDto("CRITICA", "Turno", $"⏰ Turno no atendido: {t.Paciente}", t.Detalle) { EntidadId = t.TurnoId });

            // Alertas importantes (naranja)
            foreach (var v in vacPendientes.Where(x => x.DiasRestantes <= 0))
                alertas.Add(new AlertaDto("IMPORTANTE", "Vacunación", $"💉 Vacuna vencida: {v.Paciente}", v.Detalle) { EntidadId = v.PacienteId });

            foreach (var s in stockBajo.Where(x => x.StockActual > 0))
                alertas.Add(new AlertaDto("IMPORTANTE", "Stock", $"📦 Stock bajo: {s.Producto}", s.Detalle) { EntidadId = s.ProductoId });

            // Alertas informativas (amarillo)
            foreach (var v in vacPendientes.Where(x => x.DiasRestantes > 0 && x.DiasRestantes <= 14))
                alertas.Add(new AlertaDto("INFO", "Vacunación", $"💉 Vacuna próxima: {v.Paciente}", v.Detalle) { EntidadId = v.PacienteId });

            return Ok(new
            {
                Timestamp = DateTime.Now,
                TotalAlertas = alertas.Count,
                Criticas = alertas.Count(a => a.Nivel == "CRITICA"),
                Importantes = alertas.Count(a => a.Nivel == "IMPORTANTE"),
                Informativas = alertas.Count(a => a.Nivel == "INFO"),
                TurnosHoy = turnosHoy.Count,
                TratamientosActivos = tratActivos.Count,
                Alertas = alertas.OrderBy(a => a.Nivel == "CRITICA" ? 0 : a.Nivel == "IMPORTANTE" ? 1 : 2).ToList()
            });
        }

        // ═══════════════════════════════
        //  VACUNACIONES PENDIENTES
        // ═══════════════════════════════

        /// <summary>
        /// Obtiene vacunaciones que vencen en los próximos N días
        /// </summary>
        [HttpGet("api/v1/Recordatorio/vacunas/pendientes")]
        public async Task<IActionResult> VacunasPendientes([FromQuery] int diasAntelacion = 30)
        {
            var resultado = await GetVacunacionesPendientesInternal(diasAntelacion);
            return Ok(new { Total = resultado.Count, DiasAntelacion = diasAntelacion, Items = resultado });
        }

        // ═══════════════════════════════
        //  TURNOS DEL DÍA
        // ═══════════════════════════════

        /// <summary>
        /// Obtiene los turnos programados para hoy con estado
        /// </summary>
        [HttpGet("api/v1/Recordatorio/turnos/hoy")]
        public async Task<IActionResult> TurnosHoy()
        {
            var resultado = await GetTurnosDelDiaInternal();
            return Ok(new
            {
                Fecha = DateTime.Today.ToString("dd/MM/yyyy"),
                Total = resultado.Count,
                Pendientes = resultado.Count(t => t.Estado == "Programado" || t.Estado == "Confirmado"),
                EnCurso = resultado.Count(t => t.Estado == "EnCurso"),
                Completados = resultado.Count(t => t.Estado == "Completado"),
                Items = resultado
            });
        }

        /// <summary>
        /// Obtiene turnos de mañana
        /// </summary>
        [HttpGet("api/v1/Recordatorio/turnos/manana")]
        public async Task<IActionResult> TurnosManana()
        {
            var manana = DateTime.Today.AddDays(1);
            var turnos = await turnoRepo.GetByFechaAsync(manana);
            var resultado = turnos
                .Where(t => t.Estado != EstadoTurno.Cancelado)
                .OrderBy(t => t.FechaHora)
                .Select(t => new
                {
                    Hora = t.FechaHora.ToString("HH:mm"),
                    Paciente = t.Paciente?.Nombre ?? "",
                    Propietario = t.Paciente?.Propietario != null
                        ? $"{t.Paciente.Propietario.Nombre} {t.Paciente.Propietario.Apellido}" : "",
                    Telefono = t.Paciente?.Propietario?.Telefono ?? "",
                    Veterinario = t.Veterinario?.NombreCompleto ?? "",
                    Servicio = t.Servicio?.Nombre ?? "",
                    t.Motivo
                }).ToList();

            return Ok(new { Fecha = manana.ToString("dd/MM/yyyy"), Total = resultado.Count, Items = resultado });
        }

        // ═══════════════════════════════
        //  TURNOS VENCIDOS (no atendidos)
        // ═══════════════════════════════

        /// <summary>
        /// Obtiene turnos pasados que siguen en estado Programado/Confirmado
        /// </summary>
        [HttpGet("api/v1/Recordatorio/turnos/vencidos")]
        public async Task<IActionResult> TurnosVencidos()
        {
            var resultado = await GetTurnosVencidosInternal();
            return Ok(new { Total = resultado.Count, Items = resultado });
        }

        // ═══════════════════════════════
        //  STOCK BAJO
        // ═══════════════════════════════

        /// <summary>
        /// Obtiene productos con stock igual o inferior al mínimo
        /// </summary>
        [HttpGet("api/v1/Recordatorio/stock/bajo")]
        public async Task<IActionResult> StockBajo()
        {
            var resultado = await GetStockBajoInternal();
            return Ok(new
            {
                Total = resultado.Count,
                SinStock = resultado.Count(x => x.StockActual == 0),
                Items = resultado
            });
        }

        // ═══════════════════════════════
        //  TRATAMIENTOS ACTIVOS
        // ═══════════════════════════════

        /// <summary>
        /// Obtiene tratamientos que aún no han sido finalizados
        /// </summary>
        [HttpGet("api/v1/Recordatorio/tratamientos/activos")]
        public async Task<IActionResult> TratamientosActivos()
        {
            var resultado = await GetTratamientosActivosInternal();
            return Ok(new { Total = resultado.Count, Items = resultado });
        }

        // ═══════════════════════════════
        //  CUMPLEAÑOS DE PACIENTES
        // ═══════════════════════════════

        /// <summary>
        /// Obtiene pacientes que cumplen años esta semana
        /// </summary>
        [HttpGet("api/v1/Recordatorio/cumpleanos")]
        public async Task<IActionResult> CumpleanosEstaSemana()
        {
            var pacientes = await pacienteRepo.FindAllAsync();
            var hoy = DateTime.Today;
            var finSemana = hoy.AddDays(7);

            var cumples = pacientes
                .Where(p => p.FechaNacimiento.HasValue)
                .Select(p =>
                {
                    var fn = p.FechaNacimiento!.Value;
                    var cumpleEsteAnio = new DateTime(hoy.Year, fn.Month, fn.Day);
                    if (cumpleEsteAnio < hoy) cumpleEsteAnio = cumpleEsteAnio.AddYears(1);
                    var edad = hoy.Year - fn.Year;
                    return new { p.Nombre, Especie = p.Especie?.Nombre ?? "", p.PropietarioId,
                        Propietario = p.Propietario != null ? $"{p.Propietario.Nombre} {p.Propietario.Apellido}" : "",
                        FechaCumple = cumpleEsteAnio, Edad = edad,
                        DiasParaCumple = (cumpleEsteAnio - hoy).Days };
                })
                .Where(x => x.DiasParaCumple >= 0 && x.DiasParaCumple <= 7)
                .OrderBy(x => x.DiasParaCumple)
                .ToList();

            return Ok(new { Semana = $"{hoy:dd/MM} al {finSemana:dd/MM}", Total = cumples.Count, Items = cumples });
        }

        // ═══════════════════════════════
        //  MÉTODOS INTERNOS
        // ═══════════════════════════════

        private async Task<List<VacunaPendienteDto>> GetVacunacionesPendientesInternal(int diasAntelacion)
        {
            var vacunaciones = await vacunacionRepo.FindAllAsync();
            var pacientes = await pacienteRepo.GetPacientesExpandidosAsync();
            var vacunas = await vacunaRepo.FindAllAsync();
            var hoy = DateTime.Today;

            return vacunaciones
                .Where(v => v.FechaProximaDosis.HasValue && v.FechaProximaDosis.Value <= hoy.AddDays(diasAntelacion))
                .Select(v =>
                {
                    var pac = pacientes.FirstOrDefault(p => p.Id == v.PacienteId);
                    var vac = vacunas.FirstOrDefault(va => va.Id == v.VacunaId);
                    var dias = (v.FechaProximaDosis!.Value - hoy).Days;
                    return new VacunaPendienteDto
                    {
                        PacienteId = v.PacienteId,
                        Paciente = pac?.Nombre ?? "",
                        Propietario = pac?.Propietario != null ? $"{pac.Propietario.Nombre} {pac.Propietario.Apellido}" : "",
                        Telefono = pac?.Propietario?.Telefono ?? "",
                        Vacuna = vac?.Nombre ?? "",
                        FechaProximaDosis = v.FechaProximaDosis!.Value,
                        DiasRestantes = dias,
                        Detalle = dias <= 0
                            ? $"VENCIDA hace {Math.Abs(dias)} días - {vac?.Nombre ?? ""}"
                            : $"Vence en {dias} días - {vac?.Nombre ?? ""}"
                    };
                })
                .OrderBy(x => x.DiasRestantes)
                .ToList();
        }

        private async Task<List<TurnoHoyDto>> GetTurnosDelDiaInternal()
        {
            var turnos = await turnoRepo.GetByFechaAsync(DateTime.Today);
            return turnos.OrderBy(t => t.FechaHora).Select(t => new TurnoHoyDto
            {
                Id = t.Id,
                Hora = t.FechaHora.ToString("HH:mm"),
                HoraFin = t.FechaHoraFin.ToString("HH:mm"),
                Paciente = t.Paciente?.Nombre ?? "",
                Propietario = t.Paciente?.Propietario != null
                    ? $"{t.Paciente.Propietario.Nombre} {t.Paciente.Propietario.Apellido}" : "",
                Veterinario = t.Veterinario?.NombreCompleto ?? "",
                Servicio = t.Servicio?.Nombre ?? "",
                Estado = t.Estado.ToString(),
                Motivo = t.Motivo,
                Detalle = $"{t.FechaHora:HH:mm} - {t.Paciente?.Nombre ?? ""} ({t.Servicio?.Nombre ?? ""})"
            }).ToList();
        }

        private async Task<List<StockBajoDto>> GetStockBajoInternal()
        {
            var productos = await productoRepo.FindAllAsync();
            return productos
                .Where(p => p.Activo && p.StockActual <= p.StockMinimo)
                .OrderBy(p => p.StockActual)
                .Select(p => new StockBajoDto
                {
                    ProductoId = p.Id,
                    Producto = p.Nombre,
                    StockActual = p.StockActual,
                    StockMinimo = p.StockMinimo,
                    Deficit = p.StockMinimo - p.StockActual,
                    Proveedor = p.Proveedor?.RazonSocial ?? "",
                    Detalle = p.StockActual == 0
                        ? $"SIN STOCK - {p.Nombre}"
                        : $"{p.StockActual}/{p.StockMinimo} - Faltan {p.StockMinimo - p.StockActual}"
                }).ToList();
        }

        private async Task<List<TratamientoActivoDto>> GetTratamientosActivosInternal()
        {
            var tratamientos = await tratamientoRepo.FindAllAsync();
            var pacientes = await pacienteRepo.FindAllAsync();
            return tratamientos
                .Where(t => !t.Finalizado)
                .Select(t =>
                {
                    var pac = pacientes.FirstOrDefault(p => p.Id == t.PacienteId);
                    return new TratamientoActivoDto
                    {
                        TratamientoId = t.Id,
                        Paciente = pac?.Nombre ?? "",
                        Diagnostico = t.Diagnostico,
                        Descripcion = t.Descripcion,
                        Medicacion = t.Medicacion,
                        Veterinario = t.Veterinario,
                        FechaInicio = t.Fecha,
                        DiasEnTratamiento = (DateTime.Today - t.Fecha).Days
                    };
                }).ToList();
        }

        private async Task<List<TurnoVencidoDto>> GetTurnosVencidosInternal()
        {
            var turnos = (await turnoRepo.FindAllAsync())
                .Where(t => t.FechaHora < DateTime.Now.AddHours(-2) &&
                    (t.Estado == EstadoTurno.Programado || t.Estado == EstadoTurno.Confirmado))
                .OrderBy(t => t.FechaHora)
                .Select(t => new TurnoVencidoDto
                {
                    TurnoId = t.Id,
                    FechaHora = t.FechaHora,
                    Paciente = t.Paciente?.Nombre ?? "",
                    Veterinario = t.Veterinario?.NombreCompleto ?? "",
                    Servicio = t.Servicio?.Nombre ?? "",
                    Estado = t.Estado.ToString(),
                    HorasVencido = (int)(DateTime.Now - t.FechaHora).TotalHours,
                    Detalle = $"{t.FechaHora:dd/MM HH:mm} - {t.Paciente?.Nombre ?? ""} (vencido hace {(int)(DateTime.Now - t.FechaHora).TotalHours}hs)"
                }).ToList();
            return turnos;
        }
    }

    // ═══════════════════════════════
    //  DTOs
    // ═══════════════════════════════
    public class AlertaDto
    {
        public string Nivel { get; set; }
        public string Tipo { get; set; }
        public string Titulo { get; set; }
        public string Detalle { get; set; }
        public string EntidadId { get; set; } = string.Empty;
        public DateTime Timestamp { get; set; } = DateTime.Now;
        public AlertaDto(string nivel, string tipo, string titulo, string detalle)
        { Nivel = nivel; Tipo = tipo; Titulo = titulo; Detalle = detalle; }
    }

    public class VacunaPendienteDto
    {
        public string PacienteId { get; set; }
        public string Paciente { get; set; }
        public string Propietario { get; set; }
        public string Telefono { get; set; }
        public string Vacuna { get; set; }
        public DateTime FechaProximaDosis { get; set; }
        public int DiasRestantes { get; set; }
        public string Detalle { get; set; }
    }

    public class TurnoHoyDto
    {
        public string Id { get; set; }
        public string Hora { get; set; }
        public string HoraFin { get; set; }
        public string Paciente { get; set; }
        public string Propietario { get; set; }
        public string Veterinario { get; set; }
        public string Servicio { get; set; }
        public string Estado { get; set; }
        public string Motivo { get; set; }
        public string Detalle { get; set; }
    }

    public class StockBajoDto
    {
        public string ProductoId { get; set; }
        public string Producto { get; set; }
        public int StockActual { get; set; }
        public int StockMinimo { get; set; }
        public int Deficit { get; set; }
        public string Proveedor { get; set; }
        public string Detalle { get; set; }
    }

    public class TratamientoActivoDto
    {
        public string TratamientoId { get; set; }
        public string Paciente { get; set; }
        public string Diagnostico { get; set; }
        public string Descripcion { get; set; }
        public string Medicacion { get; set; }
        public string Veterinario { get; set; }
        public DateTime FechaInicio { get; set; }
        public int DiasEnTratamiento { get; set; }
    }

    public class TurnoVencidoDto
    {
        public string TurnoId { get; set; }
        public DateTime FechaHora { get; set; }
        public string Paciente { get; set; }
        public string Veterinario { get; set; }
        public string Servicio { get; set; }
        public string Estado { get; set; }
        public int HorasVencido { get; set; }
        public string Detalle { get; set; }
    }
}
