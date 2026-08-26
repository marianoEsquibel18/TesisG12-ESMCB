using Application.DataTransferObjects;
using Application.Repositories;
using Core.Application;
using Domain.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Controllers
{
    [ApiController]
    [Authorize]
    public class MetodoPagoController(IMetodoPagoRepository repo) : BaseController
    {
        private readonly IMetodoPagoRepository _repo = repo ?? throw new ArgumentNullException(nameof(repo));

        [HttpGet("api/v1/[Controller]")]
        [Authorize(Roles = "Admin,Gerente,Veterinario,Recepcionista")]
        public async Task<IActionResult> GetAll(bool soloActivos = true)
        {
            var entities = soloActivos ? await _repo.GetActivosAsync() : await _repo.FindAllAsync();
            var dtos = entities.Select(m => new MetodoPagoDto { Id = m.Id, Nombre = m.Nombre, Activo = m.Activo }).ToList();
            return Ok(new QueryResult<MetodoPagoDto>(dtos, dtos.Count, 1, 10));
        }

        [HttpGet("api/v1/[Controller]/{id}")]
        [Authorize(Roles = "Admin,Gerente,Veterinario,Recepcionista")]
        public async Task<IActionResult> GetById(int id)
        {
            var e = await _repo.FindOneAsync(id);
            if (e == null) return NotFound();
            return Ok(new MetodoPagoDto { Id = e.Id, Nombre = e.Nombre, Activo = e.Activo });
        }

        [HttpPost("api/v1/[Controller]")]
        [Authorize(Roles = "Admin,Veterinario,Recepcionista")]
        public async Task<IActionResult> Create([FromBody] CreateMetodoPagoRequest r)
        {
            var entity = new Domain.Entities.MetodoPago(r.Nombre);
            if (!entity.IsValid) return BadRequest(entity.GetErrors().Select(e => e.ErrorMessage));
            var id = await _repo.AddAsync(entity);
            return Created($"api/v1/MetodoPago/{id}", new { Id = id });
        }

        [HttpPut("api/v1/[Controller]")]
        [Authorize(Roles = "Admin,Veterinario,Recepcionista")]
        public async Task<IActionResult> Update([FromBody] UpdateMetodoPagoRequest r)
        {
            var e = await _repo.FindOneAsync(r.Id);
            if (e == null) return NotFound();
            e.Actualizar(r.Nombre);
            _repo.Update(r.Id, e);
            return NoContent();
        }

        [HttpDelete("api/v1/[Controller]/{id}")]
        [Authorize(Roles = "Admin,Veterinario,Recepcionista")]
        public async Task<IActionResult> Delete(int id)
        {
            var e = await _repo.FindOneAsync(id);
            if (e == null) return NotFound();
            e.Desactivar(); _repo.Update(id, e);
            return NoContent();
        }
    }

    public class CreateMetodoPagoRequest { public string Nombre { get; set; } }
    public class UpdateMetodoPagoRequest { public int Id { get; set; } public string Nombre { get; set; } }

    /// <summary>
    /// Controller para gestionar Ventas con descuento automático de stock
    /// </summary>
    [ApiController]
    [Authorize]
    public class VentaController(
        IVentaRepository ventaRepo,
        IDetalleVentaRepository detalleRepo,
        IPropietarioRepository propietarioRepo,
        IMetodoPagoRepository metodoPagoRepo,
        IProductoRepository productoRepo,
        IMovimientoStockRepository movimientoRepo,
        IFacturaRepository facturaRepo,
        IProductoDepositoRepository productoDepositoRepo) : BaseController
    {
        private readonly IVentaRepository _ventaRepo = ventaRepo;
        private readonly IDetalleVentaRepository _detalleRepo = detalleRepo;
        private readonly IPropietarioRepository _propietarioRepo = propietarioRepo;
        private readonly IMetodoPagoRepository _metodoPagoRepo = metodoPagoRepo;
        private readonly IProductoRepository _productoRepo = productoRepo;
        private readonly IMovimientoStockRepository _movimientoRepo = movimientoRepo;
        private readonly IFacturaRepository _facturaRepo = facturaRepo;
        private readonly IProductoDepositoRepository _pdRepo = productoDepositoRepo;

        [HttpGet("api/v1/[Controller]")]
        [Authorize(Roles = "Admin,Gerente,Veterinario,Recepcionista")]
        public async Task<IActionResult> GetByFecha([FromQuery] DateTime? desde, [FromQuery] DateTime? hasta)
        {
            var d = desde ?? DateTime.Today;
            var h = hasta ?? DateTime.Today.AddDays(1);
            var entities = await _ventaRepo.GetByFechaRangoAsync(d, h);
            if (!IsAdmin && UserSucursalId.HasValue)
            {
                entities = entities.Where(v => v.SucursalId == UserSucursalId.Value).ToList();
            }
            return Ok(entities.Select(MapToDto).ToList());
        }

        [HttpGet("api/v1/[Controller]/{id}")]
        [Authorize(Roles = "Admin,Gerente,Veterinario,Recepcionista")]
        public async Task<IActionResult> GetById(string id)
        {
            var entity = await _ventaRepo.GetWithDetallesAsync(id);
            if (entity == null) return NotFound();
            if (!IsAdmin && UserSucursalId.HasValue && entity.SucursalId != UserSucursalId.Value)
            {
                return StatusCode(403, "No tiene permisos para ver esta venta de otra sucursal");
            }
            return Ok(MapToDto(entity));
        }

        [HttpGet("api/v1/[Controller]/byPropietario/{propietarioId}")]
        [Authorize(Roles = "Admin,Gerente,Veterinario,Recepcionista")]
        public async Task<IActionResult> GetByPropietario(string propietarioId)
        {
            var entities = await _ventaRepo.GetByPropietarioIdAsync(propietarioId);
            if (!IsAdmin && UserSucursalId.HasValue)
            {
                entities = entities.Where(v => v.SucursalId == UserSucursalId.Value).ToList();
            }
            return Ok(entities.Select(MapToDto).ToList());
        }

        [HttpPost("api/v1/[Controller]")]
        [Authorize(Roles = "Admin,Veterinario,Recepcionista")]
        public async Task<IActionResult> Create([FromBody] CreateVentaRequest request)
        {
            if (request is null) return BadRequest("El request no puede ser nulo");
            if (request.Detalles == null || !request.Detalles.Any())
                return BadRequest("La venta debe tener al menos un detalle");

            // Validar propietario (opcional)
            if (!string.IsNullOrWhiteSpace(request.PropietarioId))
            {
                var prop = await _propietarioRepo.FindOneAsync(request.PropietarioId);
                if (prop == null) return BadRequest($"No existe el propietario con Id {request.PropietarioId}");
            }

            // Validar método de pago
            var metodo = await _metodoPagoRepo.FindOneAsync(request.MetodoPagoId);
            if (metodo == null) return BadRequest($"No existe el método de pago con Id {request.MetodoPagoId}");

            // Crear la venta
            var propId = string.IsNullOrWhiteSpace(request.PropietarioId) ? null : request.PropietarioId;
            var venta = new Domain.Entities.Venta(
                propId, request.MetodoPagoId, request.Observaciones ?? "");

            // Determinar Sucursal
            int sucursalId = 1; // default fallback (Sucursal Central)
            if (UserSucursalId.HasValue)
            {
                sucursalId = UserSucursalId.Value;
            }
            else
            {
                var primerDetalle = request.Detalles.FirstOrDefault();
                if (primerDetalle != null && primerDetalle.DepositoId.HasValue && primerDetalle.DepositoId.Value > 0)
                {
                    var pd = await _pdRepo.GetByProductoYDepositoAsync(primerDetalle.ProductoId, primerDetalle.DepositoId.Value);
                    if (pd?.Deposito != null)
                    {
                        sucursalId = pd.Deposito.SucursalId;
                    }
                }
            }
            venta.AsignarSucursal(sucursalId);

            if (!venta.IsValid) return BadRequest(venta.GetErrors().Select(e => e.ErrorMessage));

            var ventaId = (await _ventaRepo.AddAsync(venta)).ToString();

            // Procesar cada detalle
            decimal totalVenta = 0;
            foreach (var det in request.Detalles)
            {
                if (!string.IsNullOrWhiteSpace(det.ProductoId))
                {
                    // Validar producto
                    var producto = await _productoRepo.FindOneAsync(det.ProductoId);
                    if (producto == null)
                        return BadRequest($"No existe el producto con Id {det.ProductoId}");

                    // Descontar stock por depósito si se especifica
                    if (det.DepositoId.HasValue && det.DepositoId.Value > 0)
                    {
                        var pd = await _pdRepo.GetByProductoYDepositoAsync(det.ProductoId, det.DepositoId.Value);
                        if (pd == null)
                            return BadRequest($"No hay stock del producto '{producto.Nombre}' en el depósito seleccionado");
                        
                        if (!IsAdmin && UserSucursalId.HasValue && pd.Deposito?.SucursalId != UserSucursalId.Value)
                        {
                            return BadRequest($"El depósito '{pd.Deposito?.Nombre}' no pertenece a su sucursal");
                        }

                        if (!pd.DescontarStock(det.Cantidad))
                            return BadRequest($"Stock insuficiente para '{producto.Nombre}' en el depósito. Disponible: {pd.StockActual}");
                        _pdRepo.Update(pd.Id, pd);

                        // Sincronizar stock total
                        var allStocks = await _pdRepo.GetByProductoIdAsync(det.ProductoId);
                        producto.SetStockDirecto(allStocks.Sum(s => s.StockActual));
                    }
                    else
                    {
                        // Fallback: descontar del stock global
                        if (!producto.DescontarStock(det.Cantidad))
                            return BadRequest($"Stock insuficiente para '{producto.Nombre}'. Disponible: {producto.StockActual}");
                    }

                    _productoRepo.Update(det.ProductoId, producto);

                    // Registrar movimiento de stock
                    var movimiento = new Domain.Entities.MovimientoStock(
                        det.ProductoId, TipoMovimiento.Salida, det.Cantidad, "Venta", ventaId.ToString());
                    await _movimientoRepo.AddAsync(movimiento);

                    // Crear detalle y guardar en DB (con depósito para poder revertir al anular)
                    var precioUnit = det.PrecioUnitario > 0 ? det.PrecioUnitario : producto.PrecioVenta;
                    var detalle = new Domain.Entities.DetalleVenta(
                        ventaId, det.ProductoId, det.Descripcion ?? producto.Nombre,
                        det.Cantidad, precioUnit, det.DepositoId);

                    await _detalleRepo.AddAsync(detalle);
                    totalVenta += detalle.Subtotal;
                }
                else
                {
                    // Es un servicio u otro concepto no inventariable
                    var precioUnit = det.PrecioUnitario;
                    var detalle = new Domain.Entities.DetalleVenta(
                        ventaId, null, det.Descripcion ?? "Servicio",
                        det.Cantidad, precioUnit, null);

                    await _detalleRepo.AddAsync(detalle);
                    totalVenta += detalle.Subtotal;
                }
            }

            // Actualizar total y confirmar
            venta.ActualizarTotal(totalVenta);
            venta.Confirmar();
            _ventaRepo.Update(ventaId, venta);

            return Created($"api/v1/Venta/{ventaId}", new { Id = ventaId, Total = venta.Total });
        }

        /// <summary>
        /// Anula una venta y revierte el stock
        /// </summary>
        [HttpPut("api/v1/[Controller]/{id}/anular")]
        [Authorize(Roles = "Admin,Veterinario,Recepcionista")]
        public async Task<IActionResult> Anular(string id, [FromBody] string motivo = "")
        {
            var venta = await _ventaRepo.GetWithDetallesAsync(id);
            if (venta == null) return NotFound();
            
            if (!IsAdmin && UserSucursalId.HasValue && venta.SucursalId != UserSucursalId.Value)
            {
                return StatusCode(403, "No tiene permisos para anular una venta de otra sucursal");
            }

            if (venta.Estado == EstadoVenta.Anulada) return BadRequest("La venta ya está anulada");

            // Revertir stock
            foreach (var detalle in venta.Detalles)
            {
                var producto = await _productoRepo.FindOneAsync(detalle.ProductoId);
                if (producto != null)
                {
                    // Si el detalle tiene depósito, devolver stock al depósito específico
                    if (detalle.DepositoId.HasValue && detalle.DepositoId.Value > 0)
                    {
                        var pd = await _pdRepo.GetByProductoYDepositoAsync(detalle.ProductoId, detalle.DepositoId.Value);
                        if (pd != null)
                        {
                            pd.AgregarStock(detalle.Cantidad);
                            _pdRepo.Update(pd.Id, pd);
                        }
                        else
                        {
                            // El registro de depósito fue eliminado, crearlo de nuevo
                            var newPd = new ProductoDeposito(detalle.ProductoId, detalle.DepositoId.Value, detalle.Cantidad, 0);
                            await _pdRepo.AddAsync(newPd);
                        }

                        // Sincronizar stock total
                        var allStocks = await _pdRepo.GetByProductoIdAsync(detalle.ProductoId);
                        producto.SetStockDirecto(allStocks.Sum(s => s.StockActual));
                    }
                    else
                    {
                        // Fallback: stock global
                        producto.AgregarStock(detalle.Cantidad);
                    }

                    _productoRepo.Update(detalle.ProductoId, producto);

                    var movimiento = new Domain.Entities.MovimientoStock(
                        detalle.ProductoId, TipoMovimiento.Devolucion, detalle.Cantidad,
                        $"Anulación venta {id}", "");
                    await _movimientoRepo.AddAsync(movimiento);
                }
            }

            venta.Anular(motivo);
            _ventaRepo.Update(id, venta);
            return NoContent();
        }

        /// <summary>
        /// Genera una factura para una venta
        /// </summary>
        [HttpPost("api/v1/[Controller]/{id}/facturar")]
        [Authorize(Roles = "Admin,Veterinario,Recepcionista")]
        public async Task<IActionResult> Facturar(string id, [FromBody] FacturarRequest request)
        {
            var venta = await _ventaRepo.GetWithDetallesAsync(id);
            if (venta == null) return NotFound();
            
            if (!IsAdmin && UserSucursalId.HasValue && venta.SucursalId != UserSucursalId.Value)
            {
                return StatusCode(403, "No tiene permisos para facturar una venta de otra sucursal");
            }

            if (venta.Estado == EstadoVenta.Anulada) return BadRequest("No se puede facturar una venta anulada");

            var existingFactura = await _facturaRepo.GetByVentaIdAsync(id);
            if (existingFactura != null) return BadRequest("Esta venta ya tiene una factura asociada");

            var iva = venta.Total * (request.PorcentajeIVA / 100m);
            var factura = new Domain.Entities.Factura(
                id, request.Numero, request.TipoFactura, venta.Total, iva);

            if (!factura.IsValid) return BadRequest(factura.GetErrors().Select(e => e.ErrorMessage));

            var facturaId = (await _facturaRepo.AddAsync(factura)).ToString();
            return Created($"api/v1/Factura/{facturaId}", new FacturaDto
            {
                Id = facturaId, VentaId = id, Numero = factura.Numero,
                TipoFactura = factura.TipoFactura, FechaEmision = factura.FechaEmision,
                SubTotal = factura.SubTotal, IVA = factura.IVA, Total = factura.Total
            });
        }

        /// <summary>
        /// Obtiene una factura por número
        /// </summary>
        [HttpGet("api/v1/Factura/byNumero/{numero}")]
        [Authorize(Roles = "Admin,Gerente,Veterinario,Recepcionista")]
        public async Task<IActionResult> GetFacturaByNumero(string numero)
        {
            var f = await _facturaRepo.GetByNumeroAsync(numero);
            if (f == null) return NotFound();

            if (!IsAdmin && UserSucursalId.HasValue)
            {
                var venta = await _ventaRepo.FindOneAsync(f.VentaId);
                if (venta == null || venta.SucursalId != UserSucursalId.Value)
                {
                    return StatusCode(403, "No tiene permisos para acceder a esta factura");
                }
            }

            return Ok(new FacturaDto
            {
                Id = f.Id, VentaId = f.VentaId, Numero = f.Numero,
                TipoFactura = f.TipoFactura, FechaEmision = f.FechaEmision,
                SubTotal = f.SubTotal, IVA = f.IVA, Total = f.Total
            });
        }

        private static VentaDto MapToDto(Domain.Entities.Venta v) => new()
        {
            Id = v.Id, Fecha = v.Fecha,
            PropietarioId = v.PropietarioId,
            PropietarioNombre = v.Propietario?.NombreCompleto ?? "",
            MetodoPagoId = v.MetodoPagoId,
            MetodoPagoNombre = v.MetodoPago?.Nombre ?? "",
            Total = v.Total, Estado = v.Estado.ToString(),
            Observaciones = v.Observaciones,
            SucursalId = v.SucursalId,
            SucursalNombre = v.Sucursal?.Nombre ?? "",
            Detalles = v.Detalles?.Select(d => new DetalleVentaDto
            {
                Id = d.Id, ProductoId = d.ProductoId,
                ProductoNombre = d.Producto?.Nombre ?? "",
                Descripcion = d.Descripcion, Cantidad = d.Cantidad,
                PrecioUnitario = d.PrecioUnitario, Subtotal = d.Subtotal
            }).ToList() ?? new()
        };
    }

    public class CreateVentaRequest
    {
        public string PropietarioId { get; set; }
        public int MetodoPagoId { get; set; }
        public string Observaciones { get; set; }
        public List<CreateDetalleVentaRequest> Detalles { get; set; }
    }

    public class CreateDetalleVentaRequest
    {
        public string? ProductoId { get; set; }
        public string Descripcion { get; set; }
        public int Cantidad { get; set; }
        public decimal PrecioUnitario { get; set; }
        public int? DepositoId { get; set; }
    }

    public class FacturarRequest
    {
        public string Numero { get; set; }
        public string TipoFactura { get; set; }
        public decimal PorcentajeIVA { get; set; } = 21;
    }
}
