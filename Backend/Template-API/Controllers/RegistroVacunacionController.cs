using Application.DataTransferObjects;
using Application.Repositories;
using Core.Application;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Controllers
{
    /// <summary>
    /// Controller para gestionar los registros de vacunación
    /// </summary>
    [ApiController]
    [Authorize(Roles = "Admin,Veterinario")]
    public class RegistroVacunacionController(
        IRegistroVacunacionRepository registroRepository,
        IPacienteRepository pacienteRepository,
        IVacunaRepository vacunaRepository,
        IProductoRepository productoRepository,
        IProductoDepositoRepository pdRepository,
        IMovimientoStockRepository movimientoRepo) : BaseController
    {
        private readonly IRegistroVacunacionRepository _registroRepository = registroRepository
            ?? throw new ArgumentNullException(nameof(registroRepository));
        private readonly IPacienteRepository _pacienteRepository = pacienteRepository
            ?? throw new ArgumentNullException(nameof(pacienteRepository));
        private readonly IVacunaRepository _vacunaRepository = vacunaRepository
            ?? throw new ArgumentNullException(nameof(vacunaRepository));
        private readonly IProductoRepository _productoRepository = productoRepository
            ?? throw new ArgumentNullException(nameof(productoRepository));
        private readonly IProductoDepositoRepository _pdRepository = pdRepository
            ?? throw new ArgumentNullException(nameof(pdRepository));
        private readonly IMovimientoStockRepository _movimientoRepo = movimientoRepo
            ?? throw new ArgumentNullException(nameof(movimientoRepo));

        /// <summary>
        /// Obtiene los registros de vacunación de un paciente
        /// </summary>
        [HttpGet("api/v1/[Controller]/byPaciente/{pacienteId}")]
        public async Task<IActionResult> GetByPaciente(string pacienteId)
        {
            if (string.IsNullOrWhiteSpace(pacienteId)) return BadRequest("El ID del paciente es requerido");

            var paciente = await _pacienteRepository.FindOneAsync(pacienteId);
            if (paciente == null) return NotFound($"No se encontró el paciente con Id {pacienteId}");

            var registros = await _registroRepository.GetByPacienteIdAsync(pacienteId);
            var dtos = registros.Select(MapToDto).ToList();
            return Ok(dtos);
        }

        /// <summary>
        /// Obtiene vacunas con próxima dosis vencida (alertas)
        /// </summary>
        [HttpGet("api/v1/[Controller]/pendientes")]
        public async Task<IActionResult> GetPendientes()
        {
            var registros = await _registroRepository.GetVacunasPendientesAsync();
            var dtos = registros.Select(MapToDto).ToList();
            return Ok(dtos);
        }

        /// <summary>
        /// Obtiene un registro de vacunación por ID
        /// </summary>
        [HttpGet("api/v1/[Controller]/{id}")]
        public async Task<IActionResult> GetById(string id)
        {
            if (string.IsNullOrWhiteSpace(id)) return BadRequest("El ID es requerido");

            var entity = await _registroRepository.FindOneAsync(id);
            if (entity == null) return NotFound($"No se encontró el registro con Id {id}");

            return Ok(MapToDto(entity));
        }

        /// <summary>
        /// Registra una vacunación aplicada y descuenta 1 unidad del inventario
        /// </summary>
        [HttpPost("api/v1/[Controller]")]
        public async Task<IActionResult> Create([FromBody] CreateRegistroVacunacionRequest request)
        {
            if (request is null) return BadRequest("El request no puede ser nulo");

            var paciente = await _pacienteRepository.FindOneAsync(request.PacienteId);
            if (paciente == null) return BadRequest($"No existe el paciente con Id {request.PacienteId}");

            int? vacunaId = request.VacunaId > 0 ? request.VacunaId : null;
            string? productoId = !string.IsNullOrWhiteSpace(request.ProductoId) ? request.ProductoId : null;

            if (vacunaId == null && productoId == null)
                return BadRequest("Debe seleccionar una vacuna válida");

            // Si viene asociado a un producto del inventario, descontar stock y generar movimiento
            if (!string.IsNullOrWhiteSpace(productoId))
            {
                var producto = await _productoRepository.FindOneAsync(productoId);
                if (producto == null) return BadRequest($"No existe el producto con Id {productoId}");

                if (request.DepositoId.HasValue && request.DepositoId.Value > 0)
                {
                    var pd = await _pdRepository.GetByProductoYDepositoAsync(productoId, request.DepositoId.Value);
                    if (pd == null)
                        return BadRequest($"No hay stock de la vacuna '{producto.Nombre}' en el depósito seleccionado");

                    if (!pd.DescontarStock(1))
                        return BadRequest($"Stock insuficiente para la vacuna '{producto.Nombre}' en el depósito. Disponible: {pd.StockActual}");

                    _pdRepository.Update(pd.Id, pd);

                    var allStocks = await _pdRepository.GetByProductoIdAsync(productoId);
                    producto.SetStockDirecto(allStocks.Sum(s => s.StockActual));
                }
                else
                {
                    // Intentar descontar del primer depósito con stock o stock global
                    var stocksDepositos = await _pdRepository.GetByProductoIdAsync(productoId);
                    var primerDepConStock = stocksDepositos.FirstOrDefault(s => s.StockActual > 0);
                    if (primerDepConStock != null)
                    {
                        primerDepConStock.DescontarStock(1);
                        _pdRepository.Update(primerDepConStock.Id, primerDepConStock);
                        producto.SetStockDirecto(stocksDepositos.Sum(s => s.StockActual));
                    }
                    else
                    {
                        if (!producto.DescontarStock(1))
                            return BadRequest($"Stock insuficiente para la vacuna '{producto.Nombre}'. Disponible: {producto.StockActual}");
                    }
                }

                _productoRepository.Update(productoId, producto);

                // Movimiento de stock
                var mov = new Domain.Entities.MovimientoStock(
                    productoId, Domain.Entities.TipoMovimiento.Salida, 1, $"Vacunación aplicada a paciente {paciente.Nombre}");
                await _movimientoRepo.AddAsync(mov);
            }
            else if (vacunaId.HasValue)
            {
                var vacuna = await _vacunaRepository.FindOneAsync(vacunaId.Value);
                if (vacuna == null) return BadRequest($"No existe la vacuna con Id {vacunaId.Value}");
            }

            var entity = new Domain.Entities.RegistroVacunacion(
                request.PacienteId,
                vacunaId,
                request.FechaAplicacion,
                request.Veterinario,
                request.NroLote ?? "",
                request.FechaProximaDosis,
                request.Observaciones ?? "",
                productoId,
                request.DepositoId);

            if (!entity.IsValid)
                return BadRequest(entity.GetErrors().Select(e => e.ErrorMessage));

            var createdId = await _registroRepository.AddAsync(entity);
            return Created($"api/v1/RegistroVacunacion/{createdId}", new { Id = createdId });
        }

        /// <summary>
        /// Actualiza un registro de vacunación
        /// </summary>
        [HttpPut("api/v1/[Controller]")]
        public async Task<IActionResult> Update([FromBody] UpdateRegistroVacunacionRequest request)
        {
            if (request is null) return BadRequest("El request no puede ser nulo");

            var entity = await _registroRepository.FindOneAsync(request.Id);
            if (entity == null) return NotFound($"No se encontró el registro con Id {request.Id}");

            entity.Actualizar(request.FechaAplicacion, request.Veterinario, request.NroLote ?? "",
                request.FechaProximaDosis, request.Observaciones ?? "", request.ProductoId, request.DepositoId);

            if (!entity.IsValid)
                return BadRequest(entity.GetErrors().Select(e => e.ErrorMessage));

            _registroRepository.Update(request.Id, entity);
            return NoContent();
        }

        /// <summary>
        /// Elimina un registro de vacunación
        /// </summary>
        [HttpDelete("api/v1/[Controller]/{id}")]
        public async Task<IActionResult> Delete(string id)
        {
            if (string.IsNullOrWhiteSpace(id)) return BadRequest("El ID es requerido");
            var entity = await _registroRepository.FindOneAsync(id);
            if (entity == null) return NotFound($"No se encontró el registro con Id {id}");
            _registroRepository.Remove(id);
            return NoContent();
        }

        private static RegistroVacunacionDto MapToDto(Domain.Entities.RegistroVacunacion r) => new()
        {
            Id = r.Id,
            PacienteId = r.PacienteId,
            PacienteNombre = r.Paciente?.Nombre ?? "",
            VacunaId = r.VacunaId ?? 0,
            ProductoId = r.ProductoId ?? "",
            DepositoId = r.DepositoId,
            VacunaNombre = !string.IsNullOrEmpty(r.Producto?.Nombre) ? r.Producto.Nombre : (r.Vacuna?.Nombre ?? ""),
            FechaAplicacion = r.FechaAplicacion,
            FechaProximaDosis = r.FechaProximaDosis,
            ProximaDosisVencida = r.FechaProximaDosis.HasValue && r.FechaProximaDosis.Value <= DateTime.Now,
            Veterinario = r.Veterinario,
            NroLote = r.NroLote,
            Observaciones = r.Observaciones
        };
    }

    public class CreateRegistroVacunacionRequest
    {
        public string PacienteId { get; set; }
        public int VacunaId { get; set; }
        public string ProductoId { get; set; }
        public int? DepositoId { get; set; }
        public DateTime FechaAplicacion { get; set; }
        public DateTime? FechaProximaDosis { get; set; }
        public string Veterinario { get; set; }
        public string NroLote { get; set; }
        public string Observaciones { get; set; }
    }

    public class UpdateRegistroVacunacionRequest
    {
        public string Id { get; set; }
        public string ProductoId { get; set; }
        public int? DepositoId { get; set; }
        public DateTime FechaAplicacion { get; set; }
        public DateTime? FechaProximaDosis { get; set; }
        public string Veterinario { get; set; }
        public string NroLote { get; set; }
        public string Observaciones { get; set; }
    }
}
