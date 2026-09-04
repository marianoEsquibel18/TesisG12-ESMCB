using Application.Repositories;
using Domain.Entities;
using Microsoft.AspNetCore.Mvc;

namespace Controllers
{
    /// <summary>
    /// Controller de estadísticas y métricas avanzadas para dashboard
    /// </summary>
    [ApiController]
    public class EstadisticasController(
        IPacienteRepository pacienteRepo,
        IPropietarioRepository propietarioRepo,
        IProductoRepository productoRepo,
        IProductoDepositoRepository pdRepo,
        IVentaRepository ventaRepo,
        ITurnoRepository turnoRepo,
        IHistorialClinicoRepository historialRepo,
        IServicioRepository servicioRepo,
        IVeterinarioRepository veterinarioRepo) : BaseController
    {
        // ═══════════════════════════════
        //  KPIs PRINCIPALES
        // ═══════════════════════════════

        /// <summary>
        /// Obtiene los KPIs principales del sistema
        /// </summary>
        [HttpGet("api/v1/Estadisticas/kpis")]
        public async Task<IActionResult> GetKPIs([FromQuery] int? sucursalId = null)
        {
            int? targetSucursalId = (sucursalId.HasValue && sucursalId.Value > 0) ? sucursalId : UserSucursalId;

            var pacientes = await pacienteRepo.FindAllAsync();
            var propietarios = await propietarioRepo.FindAllAsync();
            var productos = await productoRepo.FindAllAsync();
            var ventas = (await ventaRepo.FindAllAsync()).AsEnumerable();
            var turnos = (await turnoRepo.FindAllAsync()).AsEnumerable();

            if (targetSucursalId.HasValue)
            {
                ventas = ventas.Where(v => v.SucursalId == targetSucursalId.Value);
                turnos = turnos.Where(t => t.SucursalId == targetSucursalId.Value);
            }

            var hoy = DateTime.Today;
            var mesActual = ventas.Where(v => v.Fecha.Month == hoy.Month && v.Fecha.Year == hoy.Year
                && v.Estado != EstadoVenta.Anulada);
            var turnosHoy = turnos.Where(t => t.FechaHora.Date == hoy);

            int stockBajoCount = 0;
            if (targetSucursalId.HasValue)
            {
                foreach (var p in productos.Where(p => p.Activo))
                {
                    var pds = await pdRepo.GetByProductoIdAsync(p.Id);
                    var branchStock = pds.Where(s => s.Deposito?.SucursalId == targetSucursalId.Value).Sum(s => s.StockActual);
                    if (branchStock <= p.StockMinimo) stockBajoCount++;
                }
            }
            else
            {
                stockBajoCount = productos.Count(p => p.Activo && p.StockActual <= p.StockMinimo);
            }

            return Ok(new
            {
                TotalPacientes = pacientes.Count(p => p.Activo),
                TotalPropietarios = propietarios.Count,
                TotalProductos = productos.Count(p => p.Activo),
                ProductosStockBajo = stockBajoCount,
                VentasMesActual = mesActual.Count(),
                IngresosMesActual = mesActual.Sum(v => v.Total),
                TurnosHoy = turnosHoy.Count(),
                TurnosPendientesHoy = turnosHoy.Count(t => t.Estado == EstadoTurno.Programado || t.Estado == EstadoTurno.Confirmado),
                TurnosCompletadosHoy = turnosHoy.Count(t => t.Estado == EstadoTurno.Completado)
            });
        }

        // ═══════════════════════════════
        //  INGRESOS MENSUALES (TENDENCIA)
        // ═══════════════════════════════

        /// <summary>
        /// Obtiene los ingresos de los últimos N meses
        /// </summary>
        [HttpGet("api/v1/Estadisticas/ingresos/mensual")]
        public async Task<IActionResult> IngresosMensuales([FromQuery] int meses = 12, [FromQuery] int? sucursalId = null)
        {
            int? targetSucursalId = (sucursalId.HasValue && sucursalId.Value > 0) ? sucursalId : UserSucursalId;
            var ventas = (await ventaRepo.FindAllAsync()).AsEnumerable();
            if (targetSucursalId.HasValue)
            {
                ventas = ventas.Where(v => v.SucursalId == targetSucursalId.Value);
            }
            var desde = DateTime.Today.AddMonths(-meses + 1);
            desde = new DateTime(desde.Year, desde.Month, 1);

            var resultado = new List<object>();
            for (int i = 0; i < meses; i++)
            {
                var mes = desde.AddMonths(i);
                var ventasMes = ventas.Where(v => v.Fecha.Month == mes.Month && v.Fecha.Year == mes.Year
                    && v.Estado != EstadoVenta.Anulada);
                resultado.Add(new
                {
                    Periodo = mes.ToString("yyyy-MM"),
                    Mes = mes.ToString("MMMM yyyy"),
                    TotalVentas = ventasMes.Count(),
                    Ingresos = ventasMes.Sum(v => v.Total)
                });
            }

            return Ok(resultado);
        }

        /// <summary>
        /// Obtiene ingresos agrupados por día en un rango
        /// </summary>
        [HttpGet("api/v1/Estadisticas/ingresos/diario")]
        public async Task<IActionResult> IngresosDiarios(
            [FromQuery] DateTime? desde, [FromQuery] DateTime? hasta, [FromQuery] int? sucursalId = null)
        {
            int? targetSucursalId = (sucursalId.HasValue && sucursalId.Value > 0) ? sucursalId : UserSucursalId;
            var d = desde ?? DateTime.Today.AddDays(-30);
            var h = hasta ?? DateTime.Today;
            var ventas = (await ventaRepo.FindAllAsync()).AsEnumerable();
            if (targetSucursalId.HasValue)
            {
                ventas = ventas.Where(v => v.SucursalId == targetSucursalId.Value);
            }

            var resultado = new List<object>();
            for (var dia = d; dia <= h; dia = dia.AddDays(1))
            {
                var ventasDia = ventas.Where(v => v.Fecha.Date == dia.Date
                    && v.Estado != EstadoVenta.Anulada);
                resultado.Add(new
                {
                    Fecha = dia.ToString("yyyy-MM-dd"),
                    FechaCorta = dia.ToString("dd/MM"),
                    TotalVentas = ventasDia.Count(),
                    Ingresos = ventasDia.Sum(v => v.Total)
                });
            }

            return Ok(resultado);
        }

        // ═══════════════════════════════
        //  TOP CLIENTES
        // ═══════════════════════════════

        /// <summary>
        /// Obtiene los clientes con más gasto
        /// </summary>
        [HttpGet("api/v1/Estadisticas/topClientes")]
        public async Task<IActionResult> TopClientes([FromQuery] int top = 10, [FromQuery] int? sucursalId = null)
        {
            int? targetSucursalId = (sucursalId.HasValue && sucursalId.Value > 0) ? sucursalId : UserSucursalId;
            var ventas = (await ventaRepo.FindAllAsync()).AsEnumerable();
            if (targetSucursalId.HasValue)
            {
                ventas = ventas.Where(v => v.SucursalId == targetSucursalId.Value);
            }
            var propietarios = await propietarioRepo.FindAllAsync();

            var ranking = ventas
                .Where(v => v.Estado != EstadoVenta.Anulada)
                .GroupBy(v => v.PropietarioId)
                .Select(g =>
                {
                    var prop = propietarios.FirstOrDefault(p => p.Id == g.Key);
                    return new
                    {
                        PropietarioId = g.Key,
                        Nombre = prop != null ? $"{prop.Apellido}, {prop.Nombre}" : "Desconocido",
                        DNI = prop?.DNI ?? "",
                        CantidadCompras = g.Count(),
                        TotalGastado = g.Sum(v => v.Total)
                    };
                })
                .OrderByDescending(x => x.TotalGastado)
                .Take(top)
                .ToList();

            return Ok(ranking);
        }

        // ═══════════════════════════════
        //  DISTRIBUCIÓN POR ESPECIE/RAZA
        // ═══════════════════════════════

        /// <summary>
        /// Obtiene la distribución de visitas y turnos de pacientes por especie en un rango de fechas
        /// </summary>
        [HttpGet("api/v1/Estadisticas/pacientes/porEspecie")]
        public async Task<IActionResult> PacientesPorEspecie(
            [FromQuery] DateTime? desde, [FromQuery] DateTime? hasta, [FromQuery] int? sucursalId = null)
        {
            int? targetSucursalId = (sucursalId.HasValue && sucursalId.Value > 0) ? sucursalId : UserSucursalId;
            var turnos = (await turnoRepo.FindAllAsync()).AsEnumerable();
            var pacientes = await pacienteRepo.FindAllAsync();

            if (targetSucursalId.HasValue)
            {
                turnos = turnos.Where(t => t.SucursalId == targetSucursalId.Value);
            }

            if (desde.HasValue)
            {
                var d = desde.Value.Date;
                turnos = turnos.Where(t => t.FechaHora.Date >= d);
            }

            if (hasta.HasValue)
            {
                var h = hasta.Value.Date;
                turnos = turnos.Where(t => t.FechaHora.Date <= h);
            }

            // Considerar visitas válidas (excluir turnos cancelados)
            turnos = turnos.Where(t => t.Estado != EstadoTurno.Cancelado);

            var listTurnos = turnos.ToList();
            var pacienteDict = pacientes.ToDictionary(p => p.Id, p => p.Especie?.Nombre ?? "Sin especie");

            var distribucion = listTurnos
                .GroupBy(t => pacienteDict.TryGetValue(t.PacienteId, out var esp) ? esp : "Sin especie")
                .Select(g => new { Especie = g.Key, Cantidad = g.Count() })
                .OrderByDescending(x => x.Cantidad)
                .ToList();

            return Ok(new { Total = listTurnos.Count, Distribucion = distribucion });
        }

        /// <summary>
        /// Obtiene la distribución de pacientes por raza
        /// </summary>
        [HttpGet("api/v1/Estadisticas/pacientes/porRaza")]
        public async Task<IActionResult> PacientesPorRaza([FromQuery] int top = 15)
        {
            var pacientes = await pacienteRepo.FindAllAsync();

            var distribucion = pacientes
                .GroupBy(p => new { Especie = p.Especie?.Nombre ?? "", Raza = p.Raza?.Nombre ?? "Sin raza" })
                .Select(g => new { g.Key.Especie, g.Key.Raza, Cantidad = g.Count() })
                .OrderByDescending(x => x.Cantidad)
                .Take(top)
                .ToList();

            return Ok(distribucion);
        }

        // ═══════════════════════════════
        //  MÉTRICAS DE TURNOS
        // ═══════════════════════════════

        /// <summary>
        /// Obtiene métricas de turnos (asistencia, cancelaciones, ausentismo)
        /// </summary>
        [HttpGet("api/v1/Estadisticas/turnos/metricas")]
        public async Task<IActionResult> MetricasTurnos([FromQuery] int diasAtras = 30)
        {
            var desde = DateTime.Today.AddDays(-diasAtras);
            var turnos = (await turnoRepo.FindAllAsync())
                .Where(t => t.FechaHora >= desde).ToList();

            var total = turnos.Count;
            if (total == 0) return Ok(new { Mensaje = "No hay turnos en el período" });

            return Ok(new
            {
                Periodo = $"Últimos {diasAtras} días",
                TotalTurnos = total,
                Completados = turnos.Count(t => t.Estado == EstadoTurno.Completado),
                Cancelados = turnos.Count(t => t.Estado == EstadoTurno.Cancelado),
                Ausentes = turnos.Count(t => t.Estado == EstadoTurno.Ausente),
                Pendientes = turnos.Count(t => t.Estado == EstadoTurno.Programado || t.Estado == EstadoTurno.Confirmado),
                TasaCompletado = Math.Round((double)turnos.Count(t => t.Estado == EstadoTurno.Completado) / total * 100, 1),
                TasaCancelacion = Math.Round((double)turnos.Count(t => t.Estado == EstadoTurno.Cancelado) / total * 100, 1),
                TasaAusentismo = Math.Round((double)turnos.Count(t => t.Estado == EstadoTurno.Ausente) / total * 100, 1)
            });
        }

        /// <summary>
        /// Obtiene turnos por franja horaria (para ver horas pico)
        /// </summary>
        [HttpGet("api/v1/Estadisticas/turnos/porHora")]
        public async Task<IActionResult> TurnosPorHora([FromQuery] int diasAtras = 90)
        {
            var desde = DateTime.Today.AddDays(-diasAtras);
            var turnos = (await turnoRepo.FindAllAsync())
                .Where(t => t.FechaHora >= desde).ToList();

            var porHora = Enumerable.Range(7, 14) // 7AM to 20PM
                .Select(hora => new
                {
                    Hora = $"{hora:D2}:00",
                    Cantidad = turnos.Count(t => t.FechaHora.Hour == hora)
                }).ToList();

            return Ok(porHora);
        }

        /// <summary>
        /// Obtiene la cantidad de turnos completados por servicio
        /// </summary>
        [HttpGet("api/v1/Estadisticas/turnos/porServicio")]
        public async Task<IActionResult> TurnosPorServicio([FromQuery] int diasAtras = 90)
        {
            var desde = DateTime.Today.AddDays(-diasAtras);
            var turnos = (await turnoRepo.FindAllAsync())
                .Where(t => t.FechaHora >= desde && t.Estado == EstadoTurno.Completado).ToList();
            var servicios = await servicioRepo.FindAllAsync();

            var resultado = turnos
                .GroupBy(t => t.ServicioId)
                .Select(g => new
                {
                    ServicioId = g.Key,
                    Servicio = servicios.FirstOrDefault(s => s.Id == g.Key)?.Nombre ?? "Desconocido",
                    Cantidad = g.Count(),
                    Completados = g.Count()
                })
                .OrderByDescending(x => x.Cantidad)
                .ToList();

            return Ok(resultado);
        }

        // ═══════════════════════════════
        //  DIAGNÓSTICOS FRECUENTES
        // ═══════════════════════════════

        /// <summary>
        /// Obtiene los diagnósticos más frecuentes del historial clínico
        /// </summary>
        [HttpGet("api/v1/Estadisticas/diagnosticos")]
        public async Task<IActionResult> DiagnosticosFrecuentes([FromQuery] int top = 15)
        {
            var historial = await historialRepo.FindAllAsync();

            var diagnosticos = historial
                .Where(h => !string.IsNullOrWhiteSpace(h.Diagnostico))
                .GroupBy(h => h.Diagnostico.Trim())
                .Select(g => new { Diagnostico = g.Key, Cantidad = g.Count() })
                .OrderByDescending(x => x.Cantidad)
                .Take(top)
                .ToList();

            return Ok(diagnosticos);
        }

        // ═══════════════════════════════
        //  RENDIMIENTO POR VETERINARIO
        // ═══════════════════════════════

        /// <summary>
        /// Obtiene estadísticas de rendimiento por veterinario
        /// </summary>
        [HttpGet("api/v1/Estadisticas/veterinarios/rendimiento")]
        public async Task<IActionResult> RendimientoVeterinarios([FromQuery] int diasAtras = 30)
        {
            var desde = DateTime.Today.AddDays(-diasAtras);
            var turnos = (await turnoRepo.FindAllAsync())
                .Where(t => t.FechaHora >= desde).ToList();
            var veterinarios = await veterinarioRepo.FindAllAsync();

            var resultado = turnos
                .GroupBy(t => t.VeterinarioId)
                .Select(g =>
                {
                    var vet = veterinarios.FirstOrDefault(v => v.Id.ToString() == g.Key);
                    var total = g.Count();
                    var completados = g.Count(t => t.Estado == EstadoTurno.Completado);
                    return new
                    {
                        VeterinarioId = g.Key,
                        Nombre = vet != null ? $"Dr. {vet.Apellido}, {vet.Nombre}" : "Desconocido",
                        Especialidad = vet?.Especialidad ?? "",
                        TotalTurnos = total,
                        Completados = completados,
                        TasaCompletado = total > 0 ? Math.Round((double)completados / total * 100, 1) : 0
                    };
                })
                .OrderByDescending(x => x.TotalTurnos)
                .ToList();

            return Ok(resultado);
        }
    }
}
