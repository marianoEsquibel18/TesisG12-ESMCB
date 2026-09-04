using Application.DataTransferObjects;
using Application.Repositories;
using Core.Application;
using Domain.Entities;
using Microsoft.AspNetCore.Mvc;

namespace Controllers
{
    /// <summary>
    /// Controller para Dashboard y Reportes agregados
    /// </summary>
    [ApiController]
    public class ReporteController(
        IPacienteRepository pacienteRepo,
        IPropietarioRepository propietarioRepo,
        ITurnoRepository turnoRepo,
        IRegistroVacunacionRepository vacunacionRepo,
        ITratamientoRepository tratamientoRepo,
        IProductoRepository productoRepo,
        IProductoDepositoRepository pdRepo,
        IVentaRepository ventaRepo,
        IDetalleVentaRepository detalleVentaRepo,
        IVeterinarioRepository veterinarioRepo,
        IServicioRepository servicioRepo,
        IEspecieRepository especieRepo,
        IMetodoPagoRepository metodoPagoRepo) : BaseController
    {
        // ═══════════════════════════════════════════
        // DASHBOARD
        // ═══════════════════════════════════════════

        /// <summary>
        /// Obtiene las estadísticas generales del dashboard
        /// </summary>
        [HttpGet("api/v1/[Controller]/dashboard")]
        public async Task<IActionResult> GetDashboard([FromQuery] int? sucursalId = null)
        {
            int? targetSucursalId = (sucursalId.HasValue && sucursalId.Value > 0) ? sucursalId : UserSucursalId;
            var hoy = DateTime.Today;
            var inicioMes = new DateTime(hoy.Year, hoy.Month, 1);
            var finMes = inicioMes.AddMonths(1);

            // Pacientes y propietarios
            var pacientes = await pacienteRepo.FindAllAsync();
            var propietarios = await propietarioRepo.FindAllAsync();

            // Turnos de hoy
            var turnosHoy = await turnoRepo.GetByFechaAsync(hoy);
            if (targetSucursalId.HasValue)
            {
                turnosHoy = turnosHoy.Where(t => t.SucursalId == targetSucursalId.Value).ToList();
            }
            var turnosPendientes = turnosHoy.Count(t =>
                t.Estado == EstadoTurno.Programado || t.Estado == EstadoTurno.Confirmado);

            // Vacunas pendientes
            var vacunasPendientes = await vacunacionRepo.GetVacunasPendientesAsync();

            // Stock bajo
            var stockBajoCount = 0;
            if (targetSucursalId.HasValue)
            {
                var allActiveProds = await productoRepo.GetActivosAsync();
                foreach (var p in allActiveProds)
                {
                    var pd = await pdRepo.GetByProductoIdAsync(p.Id);
                    var branchStock = pd.Where(s => s.Deposito?.SucursalId == targetSucursalId.Value).Sum(s => s.StockActual);
                    if (branchStock <= p.StockMinimo)
                    {
                        stockBajoCount++;
                    }
                }
            }
            else
            {
                var stockBajo = await productoRepo.GetStockBajoAsync();
                stockBajoCount = stockBajo.Count();
            }

            // Ventas hoy
            var ventasHoy = await ventaRepo.GetByFechaRangoAsync(hoy, hoy.AddDays(1));
            if (targetSucursalId.HasValue)
            {
                ventasHoy = ventasHoy.Where(v => v.SucursalId == targetSucursalId.Value).ToList();
            }
            var ventasConfirmadas = ventasHoy.Where(v => v.Estado == EstadoVenta.Confirmada).ToList();

            // Ventas del mes
            var ventasMes = await ventaRepo.GetByFechaRangoAsync(inicioMes, finMes);
            if (targetSucursalId.HasValue)
            {
                ventasMes = ventasMes.Where(v => v.SucursalId == targetSucursalId.Value).ToList();
            }
            var ventasMesConf = ventasMes.Where(v => v.Estado == EstadoVenta.Confirmada).ToList();

            // Tratamientos activos (usamos un paciente genérico - buscamos todos)
            var tratamientosActivos = 0;
            foreach (var p in pacientes.Take(100)) // Limitar para performance
            {
                var trats = await tratamientoRepo.GetActivosAsync(p.Id);
                tratamientosActivos += trats.Count();
            }

            return Ok(new DashboardDto
            {
                TotalPacientes = pacientes.Count(p => p.Activo),
                TotalPropietarios = propietarios.Count(),
                TurnosHoy = turnosHoy.Count(),
                TurnosPendientes = turnosPendientes,
                VacunasPendientes = vacunasPendientes.Count(),
                ProductosStockBajo = stockBajoCount,
                VentasHoy = ventasConfirmadas.Sum(v => v.Total),
                VentasHoyCount = ventasConfirmadas.Count,
                VentasMes = ventasMesConf.Sum(v => v.Total),
                VentasMesCount = ventasMesConf.Count,
                TratamientosActivos = tratamientosActivos
            });
        }

        // ═══════════════════════════════════════════
        // REPORTE DE VENTAS
        // ═══════════════════════════════════════════

        /// <summary>
        /// Reporte de ventas por rango de fechas
        /// </summary>
        [HttpGet("api/v1/[Controller]/ventas")]
        public async Task<IActionResult> GetReporteVentas(
            [FromQuery] DateTime? desde, [FromQuery] DateTime? hasta, [FromQuery] int? sucursalId = null)
        {
            int? targetSucursalId = (sucursalId.HasValue && sucursalId.Value > 0) ? sucursalId : UserSucursalId;
            var d = desde ?? DateTime.Today.AddDays(-30);
            var h = hasta ?? DateTime.Today.AddDays(1);

            var ventas = await ventaRepo.GetByFechaRangoAsync(d, h);
            if (targetSucursalId.HasValue)
            {
                ventas = ventas.Where(v => v.SucursalId == targetSucursalId.Value).ToList();
            }
            var confirmadas = ventas.Where(v => v.Estado == EstadoVenta.Confirmada).ToList();

            // Ventas por método de pago
            var metodosPago = await metodoPagoRepo.FindAllAsync();
            var ventasPorMetodo = confirmadas
                .GroupBy(v => v.MetodoPagoId)
                .Select(g => new VentaPorMetodoPagoDto
                {
                    MetodoPago = metodosPago.FirstOrDefault(m => m.Id == g.Key)?.Nombre ?? "Desconocido",
                    Cantidad = g.Count(),
                    Total = g.Sum(v => v.Total)
                }).ToList();

            // Ventas por día
            var ventasPorDia = confirmadas
                .GroupBy(v => v.Fecha.Date)
                .Select(g => new VentaPorDiaDto
                {
                    Fecha = g.Key,
                    Cantidad = g.Count(),
                    Total = g.Sum(v => v.Total)
                })
                .OrderBy(v => v.Fecha)
                .ToList();

            // Productos más vendidos
            var productosMasVendidos = new List<ProductoMasVendidoDto>();
            foreach (var venta in confirmadas)
            {
                var detalles = await detalleVentaRepo.GetByVentaIdAsync(venta.Id);
                foreach (var det in detalles)
                {
                    var existing = productosMasVendidos.FirstOrDefault(p => p.ProductoId == det.ProductoId);
                    if (existing != null)
                    {
                        existing.CantidadVendida += det.Cantidad;
                        existing.TotalVendido += det.Subtotal;
                    }
                    else
                    {
                        productosMasVendidos.Add(new ProductoMasVendidoDto
                        {
                            ProductoId = det.ProductoId,
                            ProductoNombre = det.Descripcion,
                            CantidadVendida = det.Cantidad,
                            TotalVendido = det.Subtotal
                        });
                    }
                }
            }

            return Ok(new ReporteVentasDto
            {
                Desde = d, Hasta = h,
                CantidadVentas = confirmadas.Count,
                TotalVentas = confirmadas.Sum(v => v.Total),
                PromedioVenta = confirmadas.Any() ? confirmadas.Average(v => v.Total) : 0,
                VentasPorMetodoPago = ventasPorMetodo,
                VentasPorDia = ventasPorDia,
                ProductosMasVendidos = productosMasVendidos.OrderByDescending(p => p.CantidadVendida).Take(10).ToList()
            });
        }

        // ═══════════════════════════════════════════
        // REPORTE DE STOCK
        // ═══════════════════════════════════════════

        /// <summary>
        /// Reporte del estado actual del stock
        /// </summary>
        [HttpGet("api/v1/[Controller]/stock")]
        public async Task<IActionResult> GetReporteStock([FromQuery] int? sucursalId = null)
        {
            int? targetSucursalId = (sucursalId.HasValue && sucursalId.Value > 0) ? sucursalId : UserSucursalId;
            var todos = await productoRepo.FindAllAsync();
            var activos = todos.Where(p => p.Activo).ToList();

            var stockBajo = new List<Producto>();
            var sinStock = new List<Producto>();
            decimal valorTotalStock = 0;
            var listStockBajo = new List<ProductoStockBajoDto>();
            var todosProductos = new List<ProductoStockItemDto>();

            if (targetSucursalId.HasValue)
            {
                foreach (var p in activos)
                {
                    var pd = await pdRepo.GetByProductoIdAsync(p.Id);
                    var branchStocks = pd.Where(s => s.Deposito?.SucursalId == targetSucursalId.Value).ToList();
                    var branchStockActual = branchStocks.Sum(s => s.StockActual);

                    if (branchStockActual <= p.StockMinimo)
                    {
                        stockBajo.Add(p);
                        listStockBajo.Add(new ProductoStockBajoDto
                        {
                            Id = p.Id,
                            Nombre = p.Nombre,
                            StockActual = branchStockActual,
                            StockMinimo = p.StockMinimo,
                            CategoriaNombre = p.Categoria?.Nombre ?? ""
                        });
                    }
                    if (branchStockActual == 0)
                    {
                        sinStock.Add(p);
                    }
                    valorTotalStock += branchStockActual * p.PrecioCompra;

                    todosProductos.Add(new ProductoStockItemDto
                    {
                        Id = p.Id,
                        Nombre = p.Nombre,
                        CodigoBarras = p.CodigoBarras ?? "",
                        CategoriaNombre = p.Categoria?.Nombre ?? "",
                        StockActual = branchStockActual,
                        StockMinimo = p.StockMinimo,
                        PrecioUnitario = p.PrecioCompra,
                        PrecioVenta = p.PrecioVenta,
                        ValorTotal = branchStockActual * p.PrecioCompra
                    });
                }
            }
            else
            {
                stockBajo = activos.Where(p => p.StockBajo).ToList();
                sinStock = activos.Where(p => p.StockActual == 0).ToList();
                valorTotalStock = activos.Sum(p => p.StockActual * p.PrecioCompra);
                listStockBajo = stockBajo.Select(p => new ProductoStockBajoDto
                {
                    Id = p.Id,
                    Nombre = p.Nombre,
                    StockActual = p.StockActual,
                    StockMinimo = p.StockMinimo,
                    CategoriaNombre = p.Categoria?.Nombre ?? ""
                }).OrderBy(p => p.StockActual).ToList();

                todosProductos = activos.Select(p => new ProductoStockItemDto
                {
                    Id = p.Id,
                    Nombre = p.Nombre,
                    CodigoBarras = p.CodigoBarras ?? "",
                    CategoriaNombre = p.Categoria?.Nombre ?? "",
                    StockActual = p.StockActual,
                    StockMinimo = p.StockMinimo,
                    PrecioUnitario = p.PrecioCompra,
                    PrecioVenta = p.PrecioVenta,
                    ValorTotal = p.StockActual * p.PrecioCompra
                }).OrderBy(p => p.Nombre).ToList();
            }

            return Ok(new ReporteStockDto
            {
                TotalProductos = todos.Count(),
                ProductosActivos = activos.Count,
                ProductosStockBajo = stockBajo.Count,
                ProductosSinStock = sinStock.Count,
                ValorTotalStock = valorTotalStock,
                ListaStockBajo = listStockBajo,
                ListaTodosProductos = todosProductos
            });
        }

        // ═══════════════════════════════════════════
        // REPORTE DE TURNOS
        // ═══════════════════════════════════════════

        /// <summary>
        /// Reporte de turnos por rango de fechas
        /// </summary>
        [HttpGet("api/v1/[Controller]/turnos")]
        public async Task<IActionResult> GetReporteTurnos(
            [FromQuery] DateTime? desde, [FromQuery] DateTime? hasta, [FromQuery] int? sucursalId = null)
        {
            int? targetSucursalId = (sucursalId.HasValue && sucursalId.Value > 0) ? sucursalId : UserSucursalId;
            var d = desde ?? DateTime.Today.AddDays(-30);
            var h = hasta ?? DateTime.Today.AddDays(1);

            // Obtener todos los turnos del período día a día
            var todosTurnos = new List<Turno>();
            for (var dia = d.Date; dia < h.Date; dia = dia.AddDays(1))
            {
                var turnosDia = await turnoRepo.GetByFechaAsync(dia);
                if (targetSucursalId.HasValue)
                {
                    turnosDia = turnosDia.Where(t => t.SucursalId == targetSucursalId.Value).ToList();
                }
                todosTurnos.AddRange(turnosDia);
            }

            var completados = todosTurnos.Count(t => t.Estado == EstadoTurno.Completado);
            var cancelados = todosTurnos.Count(t => t.Estado == EstadoTurno.Cancelado);
            var ausentes = todosTurnos.Count(t => t.Estado == EstadoTurno.Ausente);
            var total = todosTurnos.Count;

            // Turnos por veterinario
            var vets = await veterinarioRepo.FindAllAsync();
            var turnosPorVet = todosTurnos
                .GroupBy(t => t.VeterinarioId)
                .Select(g => new TurnosPorVeterinarioDto
                {
                    VeterinarioId = g.Key,
                    VeterinarioNombre = vets.FirstOrDefault(v => v.Id == g.Key)?.NombreCompleto ?? "Desconocido",
                    TotalTurnos = g.Count(),
                    Completados = g.Count(t => t.Estado == EstadoTurno.Completado),
                    Ausentes = g.Count(t => t.Estado == EstadoTurno.Ausente),
                    Cancelados = g.Count(t => t.Estado == EstadoTurno.Cancelado)
                })
                .OrderByDescending(v => v.TotalTurnos)
                .ToList();

            // Top de servicios basados en servicios registrados y cobrados en comercio (Ventas confirmadas)
            var ventas = await ventaRepo.GetByFechaRangoAsync(d, h);
            if (targetSucursalId.HasValue)
            {
                ventas = ventas.Where(v => v.SucursalId == targetSucursalId.Value).ToList();
            }
            var confirmadas = ventas.Where(v => v.Estado == EstadoVenta.Confirmada).ToList();

            var servicios = await servicioRepo.FindAllAsync();
            var serviciosMap = new Dictionary<string, TurnosPorServicioDto>(StringComparer.OrdinalIgnoreCase);

            foreach (var venta in confirmadas)
            {
                var detalles = await detalleVentaRepo.GetByVentaIdAsync(venta.Id);
                foreach (var det in detalles)
                {
                    var esServicio = string.IsNullOrEmpty(det.ProductoId);
                    if (!esServicio)
                    {
                        esServicio = servicios.Any(s => string.Equals(s.Nombre, det.Descripcion, StringComparison.OrdinalIgnoreCase));
                    }

                    if (esServicio)
                    {
                        var nombreServicio = det.Descripcion?.Trim() ?? "Servicio";
                        var matchingServicio = servicios.FirstOrDefault(s => string.Equals(s.Nombre, nombreServicio, StringComparison.OrdinalIgnoreCase));
                        var claveAgrupacion = matchingServicio?.Nombre ?? nombreServicio;

                        if (!serviciosMap.TryGetValue(claveAgrupacion, out var dto))
                        {
                            dto = new TurnosPorServicioDto
                            {
                                ServicioId = matchingServicio?.Id ?? 0,
                                ServicioNombre = claveAgrupacion,
                                CantidadTurnos = 0,
                                TotalVendido = 0
                            };
                            serviciosMap[claveAgrupacion] = dto;
                        }

                        dto.CantidadTurnos += det.Cantidad;
                        dto.TotalVendido += det.Subtotal;
                    }
                }
            }

            var turnosPorServicio = serviciosMap.Values
                .OrderByDescending(s => s.CantidadTurnos)
                .ThenByDescending(s => s.TotalVendido)
                .ToList();

            // Turnos por día
            var turnosPorDia = todosTurnos
                .GroupBy(t => t.FechaHora.Date)
                .Select(g => new TurnoPorDiaDto
                {
                    Fecha = g.Key,
                    Total = g.Count(),
                    Programados = g.Count(t => t.Estado == EstadoTurno.Programado || t.Estado == EstadoTurno.Confirmado || t.Estado == EstadoTurno.EnCurso),
                    Completados = g.Count(t => t.Estado == EstadoTurno.Completado),
                    Ausentes = g.Count(t => t.Estado == EstadoTurno.Ausente),
                    Cancelados = g.Count(t => t.Estado == EstadoTurno.Cancelado)
                })
                .OrderBy(t => t.Fecha)
                .ToList();

            return Ok(new ReporteTurnosDto
            {
                Desde = d, Hasta = h,
                TotalTurnos = total,
                Completados = completados,
                Cancelados = cancelados,
                Ausentes = ausentes,
                TasaCumplimiento = total > 0 ? (decimal)completados / total * 100 : 0,
                TurnosPorVeterinario = turnosPorVet,
                TurnosPorServicio = turnosPorServicio,
                TurnosPorDia = turnosPorDia
            });
        }

        // ═══════════════════════════════════════════
        // REPORTE CLÍNICO
        // ═══════════════════════════════════════════

        /// <summary>
        /// Reporte clínico: pacientes por especie, vacunas del mes, tratamientos activos
        /// </summary>
        [HttpGet("api/v1/[Controller]/clinico")]
        public async Task<IActionResult> GetReporteClinico()
        {
            var pacientes = await pacienteRepo.FindAllAsync();
            var especies = await especieRepo.FindAllAsync();

            // Pacientes por especie
            var pacientesPorEspecie = pacientes
                .Where(p => p.Activo)
                .GroupBy(p => p.EspecieId)
                .Select(g => new PacientesPorEspecieDto
                {
                    EspecieId = g.Key,
                    EspecieNombre = especies.FirstOrDefault(e => e.Id == g.Key)?.Nombre ?? "Sin especie",
                    Cantidad = g.Count()
                }).ToList();

            // Vacunas aplicadas este mes
            var inicioMes = new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1);
            var vacunasAplicadas = 0;
            foreach (var p in pacientes.Where(p => p.Activo).Take(100))
            {
                var registros = await vacunacionRepo.GetByPacienteIdAsync(p.Id);
                vacunasAplicadas += registros.Count(r => r.FechaAplicacion >= inicioMes);
            }

            // Tratamientos activos
            var tratamientosActivos = 0;
            foreach (var p in pacientes.Where(p => p.Activo).Take(100))
            {
                var trats = await tratamientoRepo.GetActivosAsync(p.Id);
                tratamientosActivos += trats.Count();
            }

            return Ok(new ReporteClinicDto
            {
                TotalPacientes = pacientes.Count(p => p.Activo),
                PacientesPorEspecie = pacientesPorEspecie,
                VacunasAplicadasMes = vacunasAplicadas,
                TratamientosActivosCount = tratamientosActivos
            });
        }

        // ═══════════════════════════════════════════
        // REPORTE HISTÓRICO DE TRATAMIENTOS (R003)
        // ═══════════════════════════════════════════

        /// <summary>
        /// Histórico de tratamientos realizados, filtrable por paciente, dueño, veterinario y período
        /// </summary>
        [HttpGet("api/v1/[Controller]/tratamientos")]
        public async Task<IActionResult> GetHistoricoTratamientos(
            [FromQuery] DateTime? desde,
            [FromQuery] DateTime? hasta,
            [FromQuery] string? pacienteNombre,
            [FromQuery] string? propietarioNombre,
            [FromQuery] string? veterinarioNombre)
        {
            var todos = await tratamientoRepo.GetAllWithIncludesAsync();
            var lista = todos.AsEnumerable();

            // Filtro por período
            if (desde.HasValue)
                lista = lista.Where(t => t.Fecha.Date >= desde.Value.Date);
            if (hasta.HasValue)
                lista = lista.Where(t => t.Fecha.Date <= hasta.Value.Date);

            // Filtro por nombre de paciente
            if (!string.IsNullOrWhiteSpace(pacienteNombre))
                lista = lista.Where(t =>
                    t.Paciente != null &&
                    t.Paciente.Nombre.Contains(pacienteNombre, StringComparison.OrdinalIgnoreCase));

            // Filtro por nombre de propietario/dueño
            if (!string.IsNullOrWhiteSpace(propietarioNombre))
                lista = lista.Where(t =>
                    t.Paciente?.Propietario != null &&
                    t.Paciente.Propietario.NombreCompleto.Contains(propietarioNombre, StringComparison.OrdinalIgnoreCase));

            // Filtro por veterinario
            if (!string.IsNullOrWhiteSpace(veterinarioNombre))
                lista = lista.Where(t =>
                    t.Veterinario != null &&
                    t.Veterinario.Contains(veterinarioNombre, StringComparison.OrdinalIgnoreCase));

            var resultado = lista.Select(t => new HistoricoTratamientoItemDto
            {
                Id = t.Id,
                Fecha = t.Fecha,
                PacienteNombre = t.Paciente?.Nombre ?? "",
                PropietarioNombre = t.Paciente?.Propietario?.NombreCompleto ?? "",
                Veterinario = t.Veterinario ?? "",
                Diagnostico = t.Diagnostico ?? "",
                Descripcion = t.Descripcion ?? "",
                Finalizado = t.Finalizado
            }).ToList();

            return Ok(resultado);
        }
    }
}
