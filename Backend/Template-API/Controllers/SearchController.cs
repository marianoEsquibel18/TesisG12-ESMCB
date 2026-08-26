using Application.DataTransferObjects;
using Application.Repositories;
using Core.Application;
using Microsoft.AspNetCore.Mvc;

namespace Controllers
{
    /// <summary>
    /// Controller de búsqueda global y filtros avanzados
    /// </summary>
    [ApiController]
    public class SearchController(
        IPacienteRepository pacienteRepo,
        IPropietarioRepository propietarioRepo,
        IProductoRepository productoRepo,
        IProductoDepositoRepository pdRepo,
        IVeterinarioRepository veterinarioRepo,
        ITurnoRepository turnoRepo,
        IVacunaRepository vacunaRepo,
        IServicioRepository servicioRepo) : BaseController
    {
        private readonly IPacienteRepository _pacienteRepo = pacienteRepo;
        private readonly IPropietarioRepository _propietarioRepo = propietarioRepo;
        private readonly IProductoRepository _productoRepo = productoRepo;
        private readonly IProductoDepositoRepository _pdRepo = pdRepo;
        private readonly IVeterinarioRepository _veterinarioRepo = veterinarioRepo;
        private readonly ITurnoRepository _turnoRepo = turnoRepo;
        private readonly IVacunaRepository _vacunaRepo = vacunaRepo;
        private readonly IServicioRepository _servicioRepo = servicioRepo;

        /// <summary>
        /// Búsqueda global - busca en pacientes, propietarios, productos y veterinarios
        /// </summary>
        [HttpGet("api/v1/Search")]
        public async Task<IActionResult> GlobalSearch([FromQuery] string q)
        {
            if (string.IsNullOrWhiteSpace(q) || q.Length < 2)
                return BadRequest("La búsqueda debe tener al menos 2 caracteres");

            var term = q.ToLower();

            // Buscar pacientes
            var pacientes = (await _pacienteRepo.FindAllAsync())
                .Where(p => p.Nombre.ToLower().Contains(term))
                .Take(10)
                .Select(p => new SearchResultDto
                {
                    Tipo = "Paciente",
                    Id = p.Id,
                    Titulo = p.Nombre,
                    Subtitulo = $"{p.Especie?.Nombre ?? ""} | Dueño: {p.Propietario?.Nombre ?? ""} {p.Propietario?.Apellido ?? ""}",
                    Url = $"/api/v1/Paciente/{p.Id}"
                });

            // Buscar propietarios
            var propietarios = (await _propietarioRepo.FindAllAsync())
                .Where(p => p.Nombre.ToLower().Contains(term) ||
                            p.Apellido.ToLower().Contains(term) ||
                            p.DNI.Contains(term))
                .Take(10)
                .Select(p => new SearchResultDto
                {
                    Tipo = "Propietario",
                    Id = p.Id,
                    Titulo = $"{p.Apellido}, {p.Nombre}",
                    Subtitulo = $"DNI: {p.DNI} | Tel: {p.Telefono}",
                    Url = $"/api/v1/Propietario/{p.Id}"
                });

            // Buscar productos
            var matchedProds = (await _productoRepo.FindAllAsync())
                .Where(p => p.Activo && (p.Nombre.ToLower().Contains(term) ||
                            (p.CodigoBarras ?? "").Contains(term)))
                .Take(10)
                .ToList();

            var productos = new List<SearchResultDto>();
            foreach (var p in matchedProds)
            {
                int stock = p.StockActual;
                if (!IsAdmin && UserSucursalId.HasValue)
                {
                    var pds = await _pdRepo.GetByProductoIdAsync(p.Id);
                    stock = pds.Where(s => s.Deposito?.SucursalId == UserSucursalId.Value).Sum(s => s.StockActual);
                }

                productos.Add(new SearchResultDto
                {
                    Tipo = "Producto",
                    Id = p.Id,
                    Titulo = p.Nombre,
                    Subtitulo = $"Stock: {stock} | ${p.PrecioVenta}",
                    Url = $"/api/v1/Producto/{p.Id}"
                });
            }

            // Buscar veterinarios
            var vetQuery = (await _veterinarioRepo.FindAllAsync()).AsEnumerable();
            if (!IsAdmin && UserSucursalId.HasValue)
            {
                vetQuery = vetQuery.Where(v => v.SucursalId == UserSucursalId.Value);
            }
            var veterinarios = vetQuery
                .Where(v => v.Nombre.ToLower().Contains(term) ||
                            v.Apellido.ToLower().Contains(term) ||
                            v.Matricula.Contains(term))
                .Take(5)
                .Select(v => new SearchResultDto
                {
                    Tipo = "Veterinario",
                    Id = v.Id.ToString(),
                    Titulo = $"Dr. {v.Apellido}, {v.Nombre}",
                    Subtitulo = $"Mat: {v.Matricula} | {v.Especialidad}",
                    Url = $"/api/v1/Veterinario/{v.Id}"
                });

            var results = pacientes.Concat(propietarios).Concat(productos).Concat(veterinarios).ToList();
            return Ok(new { Total = results.Count, Resultados = results });
        }

        // ═══════════════════════════════════════════
        // FILTROS AVANZADOS POR ENTIDAD
        // ═══════════════════════════════════════════

        /// <summary>
        /// Buscar pacientes con filtros
        /// </summary>
        [HttpGet("api/v1/Search/pacientes")]
        public async Task<IActionResult> SearchPacientes(
            [FromQuery] string nombre, [FromQuery] int? especieId, [FromQuery] int? razaId,
            [FromQuery] string sexo, [FromQuery] string propietarioId)
        {
            var query = (await _pacienteRepo.FindAllAsync()).AsQueryable();

            if (!string.IsNullOrWhiteSpace(nombre))
                query = query.Where(p => p.Nombre.ToLower().Contains(nombre.ToLower()));
            if (especieId.HasValue)
                query = query.Where(p => p.EspecieId == especieId.Value);
            if (razaId.HasValue)
                query = query.Where(p => p.RazaId == razaId.Value);
            if (!string.IsNullOrWhiteSpace(sexo))
                query = query.Where(p => p.Sexo == sexo);
            if (!string.IsNullOrWhiteSpace(propietarioId))
                query = query.Where(p => p.PropietarioId == propietarioId);

            var results = query.ToList().Select(p => new
            {
                p.Id, p.Nombre, p.Sexo, p.FechaNacimiento,
                EspecieNombre = p.Especie?.Nombre ?? "",
                RazaNombre = p.Raza?.Nombre ?? "",
                PropietarioNombre = p.Propietario != null ? $"{p.Propietario.Nombre} {p.Propietario.Apellido}" : ""
            }).Take(50).ToList();

            return Ok(results);
        }

        /// <summary>
        /// Buscar propietarios con filtros
        /// </summary>
        [HttpGet("api/v1/Search/propietarios")]
        public async Task<IActionResult> SearchPropietarios(
            [FromQuery] string q, [FromQuery] string dni, [FromQuery] string telefono, [FromQuery] string email)
        {
            var query = (await _propietarioRepo.FindAllAsync()).Where(p => p.Activo).AsQueryable();

            if (!string.IsNullOrWhiteSpace(q))
            {
                var term = q.ToLower();
                query = query.Where(p => p.Nombre.ToLower().Contains(term) ||
                                         p.Apellido.ToLower().Contains(term) ||
                                         p.DNI.Contains(term));
            }
            if (!string.IsNullOrWhiteSpace(dni))
                query = query.Where(p => p.DNI.Contains(dni));
            if (!string.IsNullOrWhiteSpace(telefono))
                query = query.Where(p => p.Telefono.Contains(telefono));
            if (!string.IsNullOrWhiteSpace(email))
                query = query.Where(p => (p.Email ?? "").ToLower().Contains(email.ToLower()));

            var results = query.ToList().Select(p => new
            {
                p.Id, p.Nombre, p.Apellido, p.DNI, p.Telefono, p.Email,
                NombreCompleto = $"{p.Apellido}, {p.Nombre}",
                CantidadMascotas = p.Mascotas?.Count ?? 0
            }).Take(50).ToList();

            return Ok(results);
        }

        /// <summary>
        /// Buscar productos con filtros
        /// </summary>
        [HttpGet("api/v1/Search/productos")]
        public async Task<IActionResult> SearchProductos(
            [FromQuery] string nombre, [FromQuery] int? categoriaId, [FromQuery] int? marcaId,
            [FromQuery] bool? stockBajo, [FromQuery] decimal? precioMin, [FromQuery] decimal? precioMax,
            [FromQuery] int? sucursalId = null)
        {
            var list = (await _productoRepo.FindAllAsync()).Where(p => p.Activo).ToList();
            int? targetSucursalId = (!IsAdmin && UserSucursalId.HasValue) ? UserSucursalId : (sucursalId.HasValue && sucursalId.Value > 0 ? sucursalId : null);

            var filtered = new List<object>();
            foreach (var p in list)
            {
                if (!string.IsNullOrWhiteSpace(nombre) && !p.Nombre.ToLower().Contains(nombre.ToLower()))
                    continue;
                if (categoriaId.HasValue && p.CategoriaId != categoriaId.Value)
                    continue;
                if (marcaId.HasValue && p.MarcaId != marcaId.Value)
                    continue;
                if (precioMin.HasValue && p.PrecioVenta < precioMin.Value)
                    continue;
                if (precioMax.HasValue && p.PrecioVenta > precioMax.Value)
                    continue;

                int currentStock = p.StockActual;
                if (targetSucursalId.HasValue)
                {
                    var pds = await _pdRepo.GetByProductoIdAsync(p.Id);
                    currentStock = pds.Where(s => s.Deposito?.SucursalId == targetSucursalId.Value).Sum(s => s.StockActual);
                }

                if (stockBajo == true && currentStock > p.StockMinimo)
                    continue;

                filtered.Add(new
                {
                    p.Id, p.Nombre, p.CodigoBarras, p.PrecioVenta,
                    StockActual = currentStock,
                    p.StockMinimo,
                    CategoriaNombre = p.Categoria != null ? p.Categoria.Nombre : "",
                    MarcaNombre = p.Marca != null ? p.Marca.Nombre : ""
                });
            }

            return Ok(filtered.Take(50).ToList());
        }

        /// <summary>
        /// Buscar turnos con filtros
        /// </summary>
        [HttpGet("api/v1/Search/turnos")]
        public async Task<IActionResult> SearchTurnos(
            [FromQuery] DateTime? desde, [FromQuery] DateTime? hasta,
            [FromQuery] string veterinarioId, [FromQuery] int? servicioId,
            [FromQuery] int? estado)
        {
            var d = desde ?? DateTime.Today;
            var h = hasta ?? DateTime.Today.AddDays(30);
            var query = (await _turnoRepo.FindAllAsync())
                .Where(t => t.FechaHora >= d && t.FechaHora <= h).AsQueryable();

            if (!IsAdmin && UserSucursalId.HasValue)
            {
                query = query.Where(t => t.SucursalId == UserSucursalId.Value);
            }

            if (!string.IsNullOrWhiteSpace(veterinarioId))
                query = query.Where(t => t.VeterinarioId == veterinarioId);
            if (servicioId.HasValue)
                query = query.Where(t => t.ServicioId == servicioId.Value);
            if (estado.HasValue)
                query = query.Where(t => (int)t.Estado == estado.Value);

            var results = query.ToList().Select(t => new
            {
                t.Id, t.FechaHora, t.DuracionMinutos,
                Estado = t.Estado.ToString(), t.Motivo,
                PacienteNombre = t.Paciente?.Nombre ?? "",
                VeterinarioNombre = t.Veterinario != null ? $"Dr. {t.Veterinario.Apellido}" : "",
                ServicioNombre = t.Servicio?.Nombre ?? ""
            }).Take(50).ToList();

            return Ok(results);
        }
    }

    public class SearchResultDto
    {
        public string Tipo { get; set; }
        public string Id { get; set; }
        public string Titulo { get; set; }
        public string Subtitulo { get; set; }
        public string Url { get; set; }
    }

    public class PacienteSearchDto
    {
        public string Id { get; set; }
        public string Nombre { get; set; }
        public string Sexo { get; set; }
        public DateTime? FechaNacimiento { get; set; }
        public string EspecieNombre { get; set; }
        public string RazaNombre { get; set; }
        public string PropietarioNombre { get; set; }
    }
}
