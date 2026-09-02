using Application.DataTransferObjects;
using Application.Repositories;
using Core.Application;
using Domain.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Controllers
{
    /// <summary>
    /// Controller para gestionar Productos con control de stock por depósito
    /// </summary>
    [ApiController]
    public class ProductoController(
        IProductoRepository productoRepo,
        ICategoriaRepository categoriaRepo,
        IMovimientoStockRepository movimientoRepo,
        IProductoDepositoRepository productoDepositoRepo,
        IDepositoRepository depositoRepo,
        IMarcaRepository marcaRepo,
        IProveedorRepository proveedorRepo) : BaseController
    {
        private readonly IProductoRepository _productoRepo = productoRepo ?? throw new ArgumentNullException(nameof(productoRepo));
        private readonly ICategoriaRepository _categoriaRepo = categoriaRepo ?? throw new ArgumentNullException(nameof(categoriaRepo));
        private readonly IMovimientoStockRepository _movimientoRepo = movimientoRepo ?? throw new ArgumentNullException(nameof(movimientoRepo));
        private readonly IProductoDepositoRepository _pdRepo = productoDepositoRepo ?? throw new ArgumentNullException(nameof(productoDepositoRepo));
        private readonly IDepositoRepository _depositoRepo = depositoRepo ?? throw new ArgumentNullException(nameof(depositoRepo));
        private readonly IMarcaRepository _marcaRepo = marcaRepo ?? throw new ArgumentNullException(nameof(marcaRepo));
        private readonly IProveedorRepository _proveedorRepo = proveedorRepo ?? throw new ArgumentNullException(nameof(proveedorRepo));

        [HttpGet("api/v1/[Controller]")]
        public async Task<IActionResult> GetAll(bool soloActivos = true, [FromQuery] int? sucursalId = null)
        {
            var entities = soloActivos ? await _productoRepo.GetActivosAsync() : await _productoRepo.FindAllAsync();
            var dtos = new List<ProductoDto>();
            int? targetSucursalId = (!IsAdmin && UserSucursalId.HasValue) ? UserSucursalId : (sucursalId.HasValue && sucursalId.Value > 0 ? sucursalId : null);

            foreach (var p in entities)
            {
                var dto = MapToDto(p);
                // Cargar stocks por depósito
                var stocks = (await _pdRepo.GetByProductoIdAsync(p.Id)).ToList();
                
                if (targetSucursalId.HasValue)
                {
                    stocks = stocks.Where(s => s.Deposito?.SucursalId == targetSucursalId.Value).ToList();
                    dto.StockActual = stocks.Sum(s => s.StockActual);
                    dto.StockBajo = dto.StockActual <= dto.StockMinimo;
                }

                dto.StocksDepositos = stocks.Select(s => new ProductoDepositoStockDto
                {
                    Id = s.Id,
                    ProductoId = s.ProductoId,
                    DepositoId = s.DepositoId,
                    DepositoNombre = s.Deposito?.Nombre ?? "",
                    StockActual = s.StockActual,
                    StockMinimo = s.StockMinimo,
                    StockBajo = s.StockBajo
                }).ToList();
                dtos.Add(dto);
            }
            return Ok(new QueryResult<ProductoDto>(dtos, dtos.Count, 1, 10));
        }

        [HttpGet("api/v1/[Controller]/{id}")]
        public async Task<IActionResult> GetById(string id, [FromQuery] int? sucursalId = null)
        {
            var entity = await _productoRepo.FindOneAsync(id);
            if (entity == null) return NotFound();
            var dto = MapToDto(entity);
            var stocks = (await _pdRepo.GetByProductoIdAsync(id)).ToList();
            int? targetSucursalId = (!IsAdmin && UserSucursalId.HasValue) ? UserSucursalId : (sucursalId.HasValue && sucursalId.Value > 0 ? sucursalId : null);
            
            if (targetSucursalId.HasValue)
            {
                stocks = stocks.Where(s => s.Deposito?.SucursalId == targetSucursalId.Value).ToList();
                dto.StockActual = stocks.Sum(s => s.StockActual);
                dto.StockBajo = dto.StockActual <= dto.StockMinimo;
            }

            dto.StocksDepositos = stocks.Select(s => new ProductoDepositoStockDto
            {
                Id = s.Id, ProductoId = s.ProductoId, DepositoId = s.DepositoId,
                DepositoNombre = s.Deposito?.Nombre ?? "",
                StockActual = s.StockActual, StockMinimo = s.StockMinimo, StockBajo = s.StockBajo
            }).ToList();
            return Ok(dto);
        }

        [HttpGet("api/v1/[Controller]/search")]
        public async Task<IActionResult> Search([FromQuery] string nombre, [FromQuery] int? sucursalId = null)
        {
            if (string.IsNullOrWhiteSpace(nombre)) return BadRequest("Debe proporcionar un término de búsqueda");
            var entities = await _productoRepo.SearchByNombreAsync(nombre);
            var dtos = new List<ProductoDto>();
            int? targetSucursalId = (!IsAdmin && UserSucursalId.HasValue) ? UserSucursalId : (sucursalId.HasValue && sucursalId.Value > 0 ? sucursalId : null);

            foreach (var p in entities)
            {
                var dto = MapToDto(p);
                var stocks = (await _pdRepo.GetByProductoIdAsync(p.Id)).ToList();
                if (targetSucursalId.HasValue)
                {
                    stocks = stocks.Where(s => s.Deposito?.SucursalId == targetSucursalId.Value).ToList();
                    dto.StockActual = stocks.Sum(s => s.StockActual);
                    dto.StockBajo = dto.StockActual <= dto.StockMinimo;
                }
                dto.StocksDepositos = stocks.Select(s => new ProductoDepositoStockDto
                {
                    Id = s.Id, ProductoId = s.ProductoId, DepositoId = s.DepositoId,
                    DepositoNombre = s.Deposito?.Nombre ?? "",
                    StockActual = s.StockActual, StockMinimo = s.StockMinimo, StockBajo = s.StockBajo
                }).ToList();
                dtos.Add(dto);
            }
            return Ok(dtos);
        }

        [HttpGet("api/v1/[Controller]/byCategoria/{categoriaId}")]
        public async Task<IActionResult> GetByCategoria(int categoriaId, [FromQuery] int? sucursalId = null)
        {
            var entities = await _productoRepo.GetByCategoriaIdAsync(categoriaId);
            var dtos = new List<ProductoDto>();
            int? targetSucursalId = (!IsAdmin && UserSucursalId.HasValue) ? UserSucursalId : (sucursalId.HasValue && sucursalId.Value > 0 ? sucursalId : null);

            foreach (var p in entities)
            {
                var dto = MapToDto(p);
                var stocks = (await _pdRepo.GetByProductoIdAsync(p.Id)).ToList();
                if (targetSucursalId.HasValue)
                {
                    stocks = stocks.Where(s => s.Deposito?.SucursalId == targetSucursalId.Value).ToList();
                    dto.StockActual = stocks.Sum(s => s.StockActual);
                    dto.StockBajo = dto.StockActual <= dto.StockMinimo;
                }
                dto.StocksDepositos = stocks.Select(s => new ProductoDepositoStockDto
                {
                    Id = s.Id, ProductoId = s.ProductoId, DepositoId = s.DepositoId,
                    DepositoNombre = s.Deposito?.Nombre ?? "",
                    StockActual = s.StockActual, StockMinimo = s.StockMinimo, StockBajo = s.StockBajo
                }).ToList();
                dtos.Add(dto);
            }
            return Ok(dtos);
        }

        [HttpGet("api/v1/[Controller]/byCodigoBarras/{codigo}")]
        public async Task<IActionResult> GetByCodigoBarras(string codigo, [FromQuery] int? sucursalId = null)
        {
            var entity = await _productoRepo.GetByCodigoBarrasAsync(codigo);
            if (entity == null) return NotFound();
            var dto = MapToDto(entity);
            var stocks = (await _pdRepo.GetByProductoIdAsync(entity.Id)).ToList();
            int? targetSucursalId = (!IsAdmin && UserSucursalId.HasValue) ? UserSucursalId : (sucursalId.HasValue && sucursalId.Value > 0 ? sucursalId : null);

            if (targetSucursalId.HasValue)
            {
                stocks = stocks.Where(s => s.Deposito?.SucursalId == targetSucursalId.Value).ToList();
                dto.StockActual = stocks.Sum(s => s.StockActual);
                dto.StockBajo = dto.StockActual <= dto.StockMinimo;
            }
            dto.StocksDepositos = stocks.Select(s => new ProductoDepositoStockDto
            {
                Id = s.Id, ProductoId = s.ProductoId, DepositoId = s.DepositoId,
                DepositoNombre = s.Deposito?.Nombre ?? "",
                StockActual = s.StockActual, StockMinimo = s.StockMinimo, StockBajo = s.StockBajo
            }).ToList();
            return Ok(dto);
        }

        /// <summary>
        /// Obtiene productos con stock bajo (stock actual <= stock mínimo)
        /// </summary>
        [HttpGet("api/v1/[Controller]/stockBajo")]
        public async Task<IActionResult> GetStockBajo([FromQuery] int? sucursalId = null)
        {
            int? targetSucursalId = (!IsAdmin && UserSucursalId.HasValue) ? UserSucursalId : (sucursalId.HasValue && sucursalId.Value > 0 ? sucursalId : null);
            var dtos = new List<ProductoDto>();

            if (targetSucursalId.HasValue)
            {
                var entities = await _productoRepo.GetActivosAsync();
                foreach (var p in entities)
                {
                    var stocks = (await _pdRepo.GetByProductoIdAsync(p.Id))
                        .Where(s => s.Deposito?.SucursalId == targetSucursalId.Value).ToList();
                    var branchStock = stocks.Sum(s => s.StockActual);

                    if (branchStock <= p.StockMinimo)
                    {
                        var dto = MapToDto(p);
                        dto.StockActual = branchStock;
                        dto.StockBajo = true;
                        dto.StocksDepositos = stocks.Select(s => new ProductoDepositoStockDto
                        {
                            Id = s.Id, ProductoId = s.ProductoId, DepositoId = s.DepositoId,
                            DepositoNombre = s.Deposito?.Nombre ?? "",
                            StockActual = s.StockActual, StockMinimo = s.StockMinimo, StockBajo = s.StockBajo
                        }).ToList();
                        dtos.Add(dto);
                    }
                }
            }
            else
            {
                var entities = await _productoRepo.GetStockBajoAsync();
                foreach (var p in entities)
                {
                    var dto = MapToDto(p);
                    var stocks = (await _pdRepo.GetByProductoIdAsync(p.Id)).ToList();
                    dto.StocksDepositos = stocks.Select(s => new ProductoDepositoStockDto
                    {
                        Id = s.Id, ProductoId = s.ProductoId, DepositoId = s.DepositoId,
                        DepositoNombre = s.Deposito?.Nombre ?? "",
                        StockActual = s.StockActual, StockMinimo = s.StockMinimo, StockBajo = s.StockBajo
                    }).ToList();
                    dtos.Add(dto);
                }
            }
            return Ok(dtos);
        }

        /// <summary>
        /// Obtiene el stock desglosado por depósito de un producto
        /// </summary>
        [HttpGet("api/v1/[Controller]/{id}/stockDepositos")]
        public async Task<IActionResult> GetStockDepositos(string id, [FromQuery] int? sucursalId = null)
        {
            var stocks = (await _pdRepo.GetByProductoIdAsync(id)).ToList();
            int? targetSucursalId = (!IsAdmin && UserSucursalId.HasValue) ? UserSucursalId : (sucursalId.HasValue && sucursalId.Value > 0 ? sucursalId : null);

            if (targetSucursalId.HasValue)
            {
                stocks = stocks.Where(s => s.Deposito?.SucursalId == targetSucursalId.Value).ToList();
            }
            var dtos = stocks.Select(s => new ProductoDepositoStockDto
            {
                Id = s.Id, ProductoId = s.ProductoId, DepositoId = s.DepositoId,
                DepositoNombre = s.Deposito?.Nombre ?? "",
                StockActual = s.StockActual, StockMinimo = s.StockMinimo, StockBajo = s.StockBajo
            }).ToList();
            return Ok(dtos);
        }

        [HttpPost("api/v1/[Controller]")]
        [Authorize(Roles = "Admin,Gerente,Recepcionista")]
        public async Task<IActionResult> Create([FromBody] CreateProductoRequest r)
        {
            if (r is null) return BadRequest("El request no puede ser nulo");

            if (!string.IsNullOrWhiteSpace(r.CodigoBarras))
            {
                var existingBarcode = await _productoRepo.GetByCodigoBarrasAsync(r.CodigoBarras.Trim());
                if (existingBarcode != null)
                    return BadRequest($"Ya existe un producto con el código de barras '{r.CodigoBarras}'.");
            }

            var categoria = await _categoriaRepo.FindOneAsync(r.CategoriaId);
            if (categoria == null) return BadRequest($"No existe la categoría con Id {r.CategoriaId}");

            int? selectedDepositoId = r.DepositoId;
            if (selectedDepositoId.HasValue && selectedDepositoId.Value > 0)
            {
                var dep = await _depositoRepo.FindOneAsync(selectedDepositoId.Value);
                if (dep == null) return BadRequest($"No existe el depósito con Id {selectedDepositoId.Value}");
                if (!IsAdmin && UserSucursalId.HasValue && dep.SucursalId != UserSucursalId.Value)
                {
                    return StatusCode(403, "No tiene permisos para asignar un depósito de otra sucursal");
                }
            }
            else if (!IsAdmin && UserSucursalId.HasValue)
            {
                var sucursalDeps = (await _depositoRepo.GetActivosAsync())
                    .Where(d => d.SucursalId == UserSucursalId.Value).ToList();
                if (sucursalDeps.Any())
                {
                    selectedDepositoId = sucursalDeps.First().Id;
                }
            }

            var entity = new Domain.Entities.Producto(
                r.Nombre, r.Descripcion ?? "", r.CodigoBarras?.Trim() ?? "",
                r.CategoriaId, r.PrecioCompra, r.PrecioVenta,
                r.StockActual, r.StockMinimo,
                r.MarcaId, r.ProveedorId, selectedDepositoId);

            if (!entity.IsValid) return BadRequest(entity.GetErrors().Select(e => e.ErrorMessage));
            var id = (string)await _productoRepo.AddAsync(entity);

            // Si se especificó depósito, crear el registro de stock por depósito
            if (selectedDepositoId.HasValue && selectedDepositoId.Value > 0)
            {
                var pd = new ProductoDeposito(id, selectedDepositoId.Value, r.StockActual, r.StockMinimo);
                await _pdRepo.AddAsync(pd);
            }

            return Created($"api/v1/Producto/{id}", new { Id = id });
        }

        [HttpPut("api/v1/[Controller]")]
        [Authorize(Roles = "Admin,Gerente,Recepcionista")]
        public async Task<IActionResult> Update([FromBody] UpdateProductoRequest r)
        {
            var entity = await _productoRepo.FindOneAsync(r.Id);
            if (entity == null) return NotFound();

            if (!string.IsNullOrWhiteSpace(r.CodigoBarras))
            {
                var existingBarcode = await _productoRepo.GetByCodigoBarrasAsync(r.CodigoBarras.Trim());
                if (existingBarcode != null && existingBarcode.Id != r.Id)
                    return BadRequest($"Ya existe otro producto con el código de barras '{r.CodigoBarras}'.");
            }

            if (r.CategoriaId > 0)
            {
                var cat = await _categoriaRepo.FindOneAsync(r.CategoriaId);
                if (cat == null) return BadRequest($"No existe la categoría con Id {r.CategoriaId}");
            }
            if (r.MarcaId.HasValue && r.MarcaId.Value > 0)
            {
                var marca = await _marcaRepo.FindOneAsync(r.MarcaId.Value);
                if (marca == null) return BadRequest($"No existe la marca con Id {r.MarcaId.Value}");
            }
            if (!string.IsNullOrWhiteSpace(r.ProveedorId))
            {
                var prov = await _proveedorRepo.FindOneAsync(r.ProveedorId);
                if (prov == null) return BadRequest($"No existe el proveedor con Id {r.ProveedorId}");
            }
            if (r.DepositoId.HasValue && r.DepositoId.Value > 0)
            {
                var dep = await _depositoRepo.FindOneAsync(r.DepositoId.Value);
                if (dep == null) return BadRequest($"No existe el depósito con Id {r.DepositoId.Value}");
            }

            entity.Actualizar(r.Nombre, r.Descripcion ?? "", r.PrecioCompra, r.PrecioVenta, r.StockMinimo,
                r.CategoriaId > 0 ? r.CategoriaId : null,
                r.MarcaId,
                r.ProveedorId,
                r.DepositoId,
                r.CodigoBarras?.Trim());

            if (!entity.IsValid) return BadRequest(entity.GetErrors().Select(e => e.ErrorMessage));
            _productoRepo.Update(r.Id, entity);
            return NoContent();
        }

        /// <summary>
        /// Registra entrada de stock en un depósito específico
        /// </summary>
        [HttpPost("api/v1/[Controller]/{id}/entrada")]
        [Authorize(Roles = "Admin,Gerente,Recepcionista")]
        public async Task<IActionResult> EntradaStock(string id, [FromBody] MovimientoStockRequest r)
        {
            var entity = await _productoRepo.FindOneAsync(id);
            if (entity == null) return NotFound($"No se encontró el producto con Id {id}");
            if (r.Cantidad <= 0) return BadRequest("La cantidad debe ser mayor a 0");

            int? targetDepId = r.DepositoId;
            if ((!targetDepId.HasValue || targetDepId.Value <= 0) && !IsAdmin && UserSucursalId.HasValue)
            {
                var sucursalDeps = (await _depositoRepo.GetActivosAsync())
                    .Where(d => d.SucursalId == UserSucursalId.Value).ToList();
                if (sucursalDeps.Any())
                {
                    targetDepId = sucursalDeps.First().Id;
                }
            }

            if (targetDepId.HasValue && targetDepId.Value > 0)
            {
                var dep = await _depositoRepo.FindOneAsync(targetDepId.Value);
                if (dep == null) return BadRequest($"No existe el depósito con Id {targetDepId.Value}");
                if (!IsAdmin && UserSucursalId.HasValue && dep.SucursalId != UserSucursalId.Value)
                {
                    return StatusCode(403, "No tiene permisos para modificar el stock de un depósito de otra sucursal");
                }

                // Stock por depósito
                var pd = await _pdRepo.GetByProductoYDepositoAsync(id, targetDepId.Value);
                if (pd == null)
                {
                    // Crear registro de stock para este depósito
                    pd = new ProductoDeposito(id, targetDepId.Value, r.Cantidad, entity.StockMinimo);
                    await _pdRepo.AddAsync(pd);
                }
                else
                {
                    pd.AgregarStock(r.Cantidad);
                    _pdRepo.Update(pd.Id, pd);
                }

                // Sincronizar stock total del producto
                var allStocks = await _pdRepo.GetByProductoIdAsync(id);
                var total = allStocks.Sum(s => s.StockActual);
                entity.SetStockDirecto(total);
            }
            else
            {
                // Fallback: stock global (sin depósito)
                entity.AgregarStock(r.Cantidad);
            }

            _productoRepo.Update(id, entity);

            var movimiento = new Domain.Entities.MovimientoStock(
                id, TipoMovimiento.Entrada, r.Cantidad, r.Motivo ?? "Entrada de stock", r.Referencia ?? "");
            await _movimientoRepo.AddAsync(movimiento);

            int returnStock = entity.StockActual;
            if (!IsAdmin && UserSucursalId.HasValue)
            {
                var branchStocks = (await _pdRepo.GetByProductoIdAsync(id))
                    .Where(s => s.Deposito?.SucursalId == UserSucursalId.Value);
                returnStock = branchStocks.Sum(s => s.StockActual);
            }

            return Ok(new { StockActual = returnStock });
        }

        /// <summary>
        /// Registra salida de stock de un depósito específico
        /// </summary>
        [HttpPost("api/v1/[Controller]/{id}/salida")]
        [Authorize(Roles = "Admin,Gerente,Recepcionista")]
        public async Task<IActionResult> SalidaStock(string id, [FromBody] MovimientoStockRequest r)
        {
            var entity = await _productoRepo.FindOneAsync(id);
            if (entity == null) return NotFound($"No se encontró el producto con Id {id}");
            if (r.Cantidad <= 0) return BadRequest("La cantidad debe ser mayor a 0");

            int? targetDepId = r.DepositoId;
            if ((!targetDepId.HasValue || targetDepId.Value <= 0) && !IsAdmin && UserSucursalId.HasValue)
            {
                var sucursalDeps = (await _depositoRepo.GetActivosAsync())
                    .Where(d => d.SucursalId == UserSucursalId.Value).ToList();
                if (sucursalDeps.Any())
                {
                    targetDepId = sucursalDeps.First().Id;
                }
            }

            if (targetDepId.HasValue && targetDepId.Value > 0)
            {
                var dep = await _depositoRepo.FindOneAsync(targetDepId.Value);
                if (dep == null) return BadRequest($"No existe el depósito con Id {targetDepId.Value}");
                if (!IsAdmin && UserSucursalId.HasValue && dep.SucursalId != UserSucursalId.Value)
                {
                    return StatusCode(403, "No tiene permisos para descontar stock de un depósito de otra sucursal");
                }

                // Stock por depósito
                var pd = await _pdRepo.GetByProductoYDepositoAsync(id, targetDepId.Value);
                if (pd == null)
                    return BadRequest("No hay stock registrado para este producto en el depósito seleccionado");

                if (!pd.DescontarStock(r.Cantidad))
                    return BadRequest($"Stock insuficiente en el depósito. Stock actual: {pd.StockActual}, cantidad solicitada: {r.Cantidad}");

                _pdRepo.Update(pd.Id, pd);

                // Sincronizar stock total del producto
                var allStocks = await _pdRepo.GetByProductoIdAsync(id);
                var total = allStocks.Sum(s => s.StockActual);
                entity.SetStockDirecto(total);
            }
            else
            {
                // Fallback: stock global (sin depósito)
                if (!entity.DescontarStock(r.Cantidad))
                    return BadRequest($"Stock insuficiente. Stock actual: {entity.StockActual}, cantidad solicitada: {r.Cantidad}");
            }

            _productoRepo.Update(id, entity);

            var movimiento = new Domain.Entities.MovimientoStock(
                id, TipoMovimiento.Salida, r.Cantidad, r.Motivo ?? "Salida de stock", r.Referencia ?? "");
            await _movimientoRepo.AddAsync(movimiento);

            int returnStock = entity.StockActual;
            if (!IsAdmin && UserSucursalId.HasValue)
            {
                var branchStocks = (await _pdRepo.GetByProductoIdAsync(id))
                    .Where(s => s.Deposito?.SucursalId == UserSucursalId.Value);
                returnStock = branchStocks.Sum(s => s.StockActual);
            }

            return Ok(new { StockActual = returnStock });
        }

        /// <summary>
        /// Obtiene el historial de movimientos de un producto
        /// </summary>
        [HttpGet("api/v1/[Controller]/{id}/movimientos")]
        public async Task<IActionResult> GetMovimientos(string id)
        {
            var movimientos = await _movimientoRepo.GetByProductoIdAsync(id);
            var dtos = movimientos.Select(m => new MovimientoStockDto
            {
                Id = m.Id, ProductoId = m.ProductoId,
                ProductoNombre = m.Producto?.Nombre ?? "",
                Tipo = m.Tipo.ToString(), Cantidad = m.Cantidad,
                Fecha = m.Fecha, Motivo = m.Motivo, Referencia = m.Referencia
            }).ToList();
            return Ok(dtos);
        }

        [HttpDelete("api/v1/[Controller]/{id}")]
        [Authorize(Roles = "Admin,Gerente,Recepcionista")]
        public async Task<IActionResult> Delete(string id)
        {
            var entity = await _productoRepo.FindOneAsync(id);
            if (entity == null) return NotFound();
            entity.Desactivar();
            _productoRepo.Update(id, entity);
            return NoContent();
        }

        private static ProductoDto MapToDto(Domain.Entities.Producto p) => new()
        {
            Id = p.Id, Nombre = p.Nombre, Descripcion = p.Descripcion,
            CodigoBarras = p.CodigoBarras, CategoriaId = p.CategoriaId,
            CategoriaNombre = p.Categoria?.Nombre ?? "",
            MarcaId = p.MarcaId, MarcaNombre = p.Marca?.Nombre ?? "",
            ProveedorId = p.ProveedorId, ProveedorNombre = p.Proveedor?.RazonSocial ?? "",
            DepositoId = p.DepositoId, DepositoNombre = p.Deposito?.Nombre ?? "",
            PrecioCompra = p.PrecioCompra, PrecioVenta = p.PrecioVenta,
            StockActual = p.StockActual, StockMinimo = p.StockMinimo,
            StockBajo = p.StockBajo, Activo = p.Activo
        };
    }

    public class CreateProductoRequest
    {
        public string Nombre { get; set; } public string Descripcion { get; set; }
        public string CodigoBarras { get; set; } public int CategoriaId { get; set; }
        public int? MarcaId { get; set; } public string ProveedorId { get; set; }
        public int? DepositoId { get; set; } public decimal PrecioCompra { get; set; }
        public decimal PrecioVenta { get; set; } public int StockActual { get; set; }
        public int StockMinimo { get; set; }
    }

    public class UpdateProductoRequest
    {
        public string Id { get; set; }
        public string Nombre { get; set; }
        public string Descripcion { get; set; }
        public string CodigoBarras { get; set; }
        public int CategoriaId { get; set; }
        public int? MarcaId { get; set; }
        public string ProveedorId { get; set; }
        public int? DepositoId { get; set; }
        public decimal PrecioCompra { get; set; }
        public decimal PrecioVenta { get; set; }
        public int StockMinimo { get; set; }
    }

    public class MovimientoStockRequest
    {
        public int Cantidad { get; set; }
        public string Motivo { get; set; }
        public string Referencia { get; set; }
        public int? DepositoId { get; set; }
    }
}
