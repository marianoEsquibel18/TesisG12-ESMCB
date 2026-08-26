using Application.Repositories;
using Core.Application;
using Microsoft.AspNetCore.Mvc;
using System.Linq.Expressions;

namespace Controllers
{
    /// <summary>
    /// Controller con endpoints paginados para todas las entidades principales.
    /// Complementa los controllers existentes con paginación y ordenamiento estándar.
    /// </summary>
    [ApiController]
    public class PaginacionController(
        IPacienteRepository pacienteRepo,
        IPropietarioRepository propietarioRepo,
        IProductoRepository productoRepo,
        IProductoDepositoRepository productoDepositoRepo,
        ITurnoRepository turnoRepo,
        IVentaRepository ventaRepo,
        IHistorialClinicoRepository historialRepo,
        IVeterinarioRepository veterinarioRepo,
        IServicioRepository servicioRepo,
        IEspecieRepository especieRepo,
        IRazaRepository razaRepo,
        IHorarioRepository horarioRepo) : BaseController
    {
        /// <summary>
        /// Pacientes paginados con ordenamiento
        /// </summary>
        [HttpGet("api/v1/Paginado/pacientes")]
        public async Task<IActionResult> PacientesPaginados(
            [FromQuery] int page = 1, [FromQuery] int pageSize = 10,
            [FromQuery] string sortBy = "Nombre", [FromQuery] string sortDir = "asc",
            [FromQuery] string searchTerm = "")
        {
            var entities = (await pacienteRepo.GetPacientesExpandidosAsync()).AsEnumerable();
            if (!string.IsNullOrWhiteSpace(searchTerm))
            {
                var st = searchTerm.ToLower();
                entities = entities.Where(p => 
                    p.Nombre.ToLower().Contains(st) || 
                    (p.Propietario != null && (p.Propietario.Nombre.ToLower().Contains(st) || p.Propietario.Apellido.ToLower().Contains(st)))
                );
            }

            var dtos = entities.Select(p => new
            {
                p.Id, p.Nombre, p.Sexo, p.FechaNacimiento,
                EspecieId = p.EspecieId,
                EspecieNombre = p.Especie != null ? p.Especie.Nombre : "",
                RazaId = p.RazaId,
                RazaNombre = p.Raza != null ? p.Raza.Nombre : "",
                PropietarioId = p.PropietarioId,
                PropietarioNombre = p.Propietario != null ? $"{p.Propietario.Nombre} {p.Propietario.Apellido}" : "",
                EdadEnAnios = p.FechaNacimiento.HasValue ? (int?)Math.Floor((DateTime.Now - p.FechaNacimiento.Value).TotalDays / 365.25) : null,
                p.FotoUrl,
                p.Activo
            });
            return Ok(PaginacionHelper.Paginar(dtos, page, pageSize, sortBy, sortDir));
        }

        /// <summary>
        /// Propietarios paginados con ordenamiento
        /// </summary>
        [HttpGet("api/v1/Paginado/propietarios")]
        public async Task<IActionResult> PropietariosPaginados(
            [FromQuery] int page = 1, [FromQuery] int pageSize = 10,
            [FromQuery] string sortBy = "Apellido", [FromQuery] string sortDir = "asc",
            [FromQuery] string searchTerm = "")
        {
            var entities = (await propietarioRepo.GetPropietariosConMascotasAsync()).AsEnumerable();
            if (!string.IsNullOrWhiteSpace(searchTerm))
            {
                var st = searchTerm.ToLower();
                entities = entities.Where(p => 
                    p.Nombre.ToLower().Contains(st) || 
                    p.Apellido.ToLower().Contains(st) || 
                    p.DNI.Contains(st)
                );
            }

            var dtos = entities.Select(p => new
            {
                p.Id, p.Nombre, p.Apellido, p.DNI, p.Telefono, p.Email,
                p.Direccion, p.FechaRegistro, p.Activo,
                CantidadMascotas = p.Mascotas != null ? p.Mascotas.Count : 0,
                NombreCompleto = $"{p.Nombre} {p.Apellido}"
            });
            return Ok(PaginacionHelper.Paginar(dtos, page, pageSize, sortBy, sortDir));
        }

        /// <summary>
        /// Productos paginados con ordenamiento
        /// </summary>
        [HttpGet("api/v1/Paginado/productos")]
        public async Task<IActionResult> ProductosPaginados(
            [FromQuery] int page = 1, [FromQuery] int pageSize = 15,
            [FromQuery] string sortBy = "Nombre", [FromQuery] string sortDir = "asc",
            [FromQuery] bool soloActivos = true, [FromQuery] string searchTerm = "",
            [FromQuery] int? sucursalId = null)
        {
            var entities = (await productoRepo.GetProductosExpandidosAsync()).AsEnumerable();
            if (soloActivos) entities = entities.Where(p => p.Activo);

            if (!string.IsNullOrWhiteSpace(searchTerm))
            {
                var st = searchTerm.ToLower();
                entities = entities.Where(p =>
                    p.Nombre.ToLower().Contains(st) ||
                    (p.CodigoBarras != null && p.CodigoBarras.ToLower().Contains(st))
                );
            }

            var list = entities.ToList();
            var dtos = new List<object>();
            int? targetSucursalId = (!IsAdmin && UserSucursalId.HasValue) ? UserSucursalId : (sucursalId.HasValue && sucursalId.Value > 0 ? sucursalId : null);

            foreach (var p in list)
            {
                var allStocks = (await productoDepositoRepo.GetByProductoIdAsync(p.Id)).ToList();
                var filteredStocks = allStocks;
                if (targetSucursalId.HasValue)
                {
                    filteredStocks = filteredStocks.Where(s => s.Deposito?.SucursalId == targetSucursalId.Value).ToList();
                }

                int currentStock = targetSucursalId.HasValue ? filteredStocks.Sum(s => s.StockActual) : p.StockActual;
                bool stockBajo = currentStock <= p.StockMinimo;

                var stockDtos = filteredStocks.Select(s => new
                {
                    s.Id,
                    s.ProductoId,
                    s.DepositoId,
                    DepositoNombre = s.Deposito?.Nombre ?? "",
                    s.StockActual,
                    s.StockMinimo,
                    StockBajo = s.StockBajo
                }).ToList();

                dtos.Add(new
                {
                    p.Id, p.Nombre, p.Descripcion, p.CodigoBarras,
                    p.CategoriaId, CategoriaNombre = p.Categoria != null ? p.Categoria.Nombre : "",
                    p.MarcaId, MarcaNombre = p.Marca != null ? p.Marca.Nombre : "",
                    p.ProveedorId, ProveedorNombre = p.Proveedor != null ? p.Proveedor.RazonSocial : "",
                    p.DepositoId, DepositoNombre = p.Deposito != null ? p.Deposito.Nombre : "",
                    p.PrecioCompra, p.PrecioVenta,
                    StockActual = currentStock,
                    p.StockMinimo,
                    p.Activo,
                    StockBajo = stockBajo,
                    StocksDepositos = stockDtos
                });
            }
            return Ok(PaginacionHelper.Paginar(dtos, page, pageSize, sortBy, sortDir));
        }

        /// <summary>
        /// Turnos paginados con ordenamiento
        /// </summary>
        [HttpGet("api/v1/Paginado/turnos")]
        public async Task<IActionResult> TurnosPaginados(
            [FromQuery] int page = 1, [FromQuery] int pageSize = 10,
            [FromQuery] string sortBy = "FechaHora", [FromQuery] string sortDir = "desc",
            [FromQuery] DateTime? desde = null, [FromQuery] DateTime? hasta = null,
            [FromQuery] string? veterinarioId = null, [FromQuery] string? searchTerm = null)
        {
            var entities = (await turnoRepo.GetTurnosExpandidosAsync()).AsEnumerable();
            if (!IsAdmin && UserSucursalId.HasValue)
            {
                entities = entities.Where(t => t.SucursalId == UserSucursalId.Value);
            }
            if (desde.HasValue) entities = entities.Where(t => t.FechaHora >= desde.Value);
            if (hasta.HasValue) entities = entities.Where(t => t.FechaHora <= hasta.Value);
            
            if (!string.IsNullOrWhiteSpace(veterinarioId))
            {
                entities = entities.Where(t => t.VeterinarioId == veterinarioId);
            }
            if (!string.IsNullOrWhiteSpace(searchTerm))
            {
                var searchUpper = searchTerm.ToUpper();
                entities = entities.Where(t => 
                    (t.Paciente != null && t.Paciente.Nombre.ToUpper().Contains(searchUpper)) ||
                    (t.Motivo != null && t.Motivo.ToUpper().Contains(searchUpper))
                );
            }

            var dtos = entities.Select(t => new
            {
                t.Id, t.FechaHora, t.DuracionMinutos,
                Estado = t.Estado.ToString(), t.Motivo,
                t.PacienteId,
                PacienteNombre = t.Paciente != null ? t.Paciente.Nombre : "",
                PropietarioNombre = t.Paciente?.Propietario?.Nombre != null ? $"{t.Paciente.Propietario.Nombre} {t.Paciente.Propietario.Apellido}".Trim() : "",
                t.VeterinarioId,
                VeterinarioNombre = t.Veterinario != null ? $"Dr. {t.Veterinario.Apellido}" : "",
                t.ServicioId,
                ServicioNombre = t.Servicio != null ? t.Servicio.Nombre : ""
            });
            return Ok(PaginacionHelper.Paginar(dtos, page, pageSize, sortBy, sortDir));
        }

        /// <summary>
        /// Ventas paginadas con ordenamiento
        /// </summary>
        [HttpGet("api/v1/Paginado/ventas")]
        public async Task<IActionResult> VentasPaginadas(
            [FromQuery] int page = 1, [FromQuery] int pageSize = 10,
            [FromQuery] string sortBy = "Fecha", [FromQuery] string sortDir = "desc",
            [FromQuery] DateTime? desde = null, [FromQuery] DateTime? hasta = null)
        {
            var entities = (await ventaRepo.FindAllAsync()).AsEnumerable();
            if (!IsAdmin && UserSucursalId.HasValue)
            {
                entities = entities.Where(v => v.SucursalId == UserSucursalId.Value);
            }
            if (desde.HasValue) entities = entities.Where(v => v.Fecha >= desde.Value);
            if (hasta.HasValue) entities = entities.Where(v => v.Fecha <= hasta.Value);

            var dtos = entities.Select(v => new
            {
                v.Id, v.Fecha, v.Total, Estado = v.Estado.ToString(),
                Propietario = v.Propietario != null ? $"{v.Propietario.Nombre} {v.Propietario.Apellido}" : "",
                MetodoPago = v.MetodoPago != null ? v.MetodoPago.Nombre : "",
                Items = v.Detalles != null ? v.Detalles.Count : 0
            });
            return Ok(PaginacionHelper.Paginar(dtos, page, pageSize, sortBy, sortDir));
        }

        /// <summary>
        /// Historial clínico paginado
        /// </summary>
        [HttpGet("api/v1/Paginado/historial")]
        public async Task<IActionResult> HistorialPaginado(
            [FromQuery] int page = 1, [FromQuery] int pageSize = 10,
            [FromQuery] string sortBy = "Fecha", [FromQuery] string sortDir = "desc",
            [FromQuery] string pacienteId = null)
        {
            var entities = pacienteId != null
                ? await historialRepo.GetByPacienteIdAsync(pacienteId)
                : await historialRepo.FindAllAsync();

            var dtos = entities.Select(h => new
            {
                h.Id, h.PacienteId, h.Fecha, h.Motivo, h.Diagnostico,
                h.Veterinario, h.Peso, h.Temperatura
            });
            return Ok(PaginacionHelper.Paginar(dtos, page, pageSize, sortBy, sortDir));
        }

        /// <summary>
        /// Veterinarios paginados con horarios
        /// </summary>
        [HttpGet("api/v1/Paginado/veterinarios")]
        public async Task<IActionResult> VeterinariosPaginados(
            [FromQuery] int page = 1, [FromQuery] int pageSize = 10,
            [FromQuery] string sortBy = "Apellido", [FromQuery] string sortDir = "asc")
        {
            var entities = (await veterinarioRepo.FindAllAsync()).AsEnumerable();
            if (!IsAdmin && UserSucursalId.HasValue)
            {
                entities = entities.Where(v => v.SucursalId == UserSucursalId.Value);
            }
            var allHorarios = (await horarioRepo.GetActivosAsync()).ToList();

            var dtos = entities.Select(v => {
                var vHorarios = allHorarios.Where(h => h.VeterinarioId == v.Id).Select(h => new Application.DataTransferObjects.HorarioDto {
                    Id = h.Id,
                    VeterinarioId = h.VeterinarioId,
                    DiaSemana = h.DiaSemana,
                    HoraInicio = h.HoraInicio.ToString(@"hh\:mm"),
                    HoraFin = h.HoraFin.ToString(@"hh\:mm"),
                    TipoHorarioId = h.TipoHorarioId,
                    TipoHorarioNombre = h.TipoHorario?.Nombre ?? (h.TipoHorarioId == 2 ? "Guardia" : "Normal"),
                    Activo = h.Activo
                }).ToList();

                return new
                {
                    v.Id, v.Nombre, v.Apellido,
                    NombreCompleto = v.NombreCompleto, v.Matricula,
                    v.Especialidad, v.Telefono, v.Email, v.Activo,
                    Horarios = vHorarios
                };
            });
            return Ok(PaginacionHelper.Paginar(dtos, page, pageSize, sortBy, sortDir));
        }

        /// <summary>
        /// Especies paginadas
        /// </summary>
        [HttpGet("api/v1/Paginado/especies")]
        public async Task<IActionResult> EspeciesPaginadas(
            [FromQuery] int page = 1, [FromQuery] int pageSize = 10,
            [FromQuery] string sortBy = "Nombre", [FromQuery] string sortDir = "asc")
        {
            var entities = await especieRepo.FindAllAsync();
            var dtos = entities.Select(e => new { e.Id, e.Nombre, e.Descripcion, e.Activo });
            return Ok(PaginacionHelper.Paginar(dtos, page, pageSize, sortBy, sortDir));
        }

        /// <summary>
        /// Razas paginadas
        /// </summary>
        [HttpGet("api/v1/Paginado/razas")]
        public async Task<IActionResult> RazasPaginadas(
            [FromQuery] int page = 1, [FromQuery] int pageSize = 10,
            [FromQuery] string sortBy = "Nombre", [FromQuery] string sortDir = "asc")
        {
            var entities = await razaRepo.FindAllAsync();
            var dtos = entities.Select(r => new
            {
                r.Id, r.Nombre, r.Descripcion, r.EspecieId,
                Especie = r.Especie != null ? r.Especie.Nombre : "",
                r.Activo
            });
            return Ok(PaginacionHelper.Paginar(dtos, page, pageSize, sortBy, sortDir));
        }

        /// <summary>
        /// Servicios paginados
        /// </summary>
        [HttpGet("api/v1/Paginado/servicios")]
        public async Task<IActionResult> ServiciosPaginados(
            [FromQuery] int page = 1, [FromQuery] int pageSize = 10,
            [FromQuery] string sortBy = "Nombre", [FromQuery] string sortDir = "asc")
        {
            var entities = await servicioRepo.FindAllAsync();
            var dtos = entities.Select(s => new
            {
                s.Id, s.Nombre, s.Descripcion, s.Precio,
                s.DuracionMinutos, s.ProductosUtilizados, s.Activo
            });
            return Ok(PaginacionHelper.Paginar(dtos, page, pageSize, sortBy, sortDir));
        }
    }

    /// <summary>
    /// Helper estático para paginación y ordenamiento
    /// </summary>
    public static class PaginacionHelper
    {
        public static PaginatedResult<T> Paginar<T>(
            IEnumerable<T> source, int page = 1, int pageSize = 10,
            string sortBy = null, string sortDirection = "asc") where T : class
        {
            if (page < 1) page = 1;
            if (pageSize < 1) pageSize = 10;
            if (pageSize > 100) pageSize = 100;

            var list = source.ToList();
            var totalItems = list.Count;

            // Ordenamiento dinámico por propiedad
            if (!string.IsNullOrWhiteSpace(sortBy))
            {
                var property = typeof(T).GetProperties()
                    .FirstOrDefault(p => p.Name.Equals(sortBy, StringComparison.OrdinalIgnoreCase));
                if (property != null)
                {
                    list = sortDirection?.ToLower() == "desc"
                        ? list.OrderByDescending(x => property.GetValue(x)).ToList()
                        : list.OrderBy(x => property.GetValue(x)).ToList();
                }
            }

            var items = list.Skip((page - 1) * pageSize).Take(pageSize).ToList();

            return new PaginatedResult<T>
            {
                Items = items, Page = page, PageSize = pageSize,
                TotalItems = totalItems,
                TotalPages = (int)Math.Ceiling(totalItems / (double)pageSize),
                HasPrevious = page > 1,
                HasNext = page * pageSize < totalItems
            };
        }
    }

    public class PaginatedResult<T>
    {
        public List<T> Items { get; set; }
        public int Page { get; set; }
        public int PageSize { get; set; }
        public int TotalItems { get; set; }
        public int TotalPages { get; set; }
        public bool HasPrevious { get; set; }
        public bool HasNext { get; set; }
    }
}
