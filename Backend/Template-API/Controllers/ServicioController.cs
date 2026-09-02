using Application.DataTransferObjects;
using Application.Repositories;
using Core.Application;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Controllers
{
    [ApiController]
    [Authorize]
    public class ServicioController(IServicioRepository servicioRepository) : BaseController
    {
        private readonly IServicioRepository _repository = servicioRepository
            ?? throw new ArgumentNullException(nameof(servicioRepository));

        [HttpGet("api/v1/[Controller]")]
        [Authorize(Roles = "Admin,Gerente,Veterinario,Recepcionista")]
        public async Task<IActionResult> GetAll(bool soloActivos = true)
        {
            var entities = soloActivos
                ? await _repository.GetActivosAsync()
                : await _repository.FindAllAsync();

            var dtos = entities.Select(s => new ServicioDto
            {
                Id = s.Id, Nombre = s.Nombre, Descripcion = s.Descripcion,
                DuracionMinutos = s.DuracionMinutos, Precio = s.Precio,
                ProductosUtilizados = s.ProductosUtilizados ?? "", Activo = s.Activo
            }).ToList();

            return Ok(new QueryResult<ServicioDto>(dtos, dtos.Count, 1, 10));
        }

        [HttpGet("api/v1/[Controller]/{id}")]
        [Authorize(Roles = "Admin,Gerente,Veterinario,Recepcionista")]
        public async Task<IActionResult> GetById(int id)
        {
            if (id <= 0) return BadRequest("El ID debe ser mayor a 0");
            var entity = await _repository.FindOneAsync(id);
            if (entity == null) return NotFound($"No se encontró el servicio con Id {id}");

            return Ok(new ServicioDto
            {
                Id = entity.Id, Nombre = entity.Nombre, Descripcion = entity.Descripcion,
                DuracionMinutos = entity.DuracionMinutos, Precio = entity.Precio,
                ProductosUtilizados = entity.ProductosUtilizados ?? "", Activo = entity.Activo
            });
        }

        [HttpPost("api/v1/[Controller]")]
        [Authorize(Roles = "Admin,Gerente,Veterinario,Recepcionista")]
        public async Task<IActionResult> Create([FromBody] CreateServicioRequest request)
        {
            if (request is null) return BadRequest("El request no puede ser nulo");

            var duracion = request.DuracionMinutos > 0 ? request.DuracionMinutos : 30;
            var entity = new Domain.Entities.Servicio(
                request.Nombre, request.Descripcion ?? "", duracion, request.Precio, request.ProductosUtilizados ?? "");

            if (!entity.IsValid)
                return BadRequest(entity.GetErrors().Select(e => e.ErrorMessage));

            var createdId = await _repository.AddAsync(entity);
            return Created($"api/v1/Servicio/{createdId}", new { Id = createdId });
        }

        [HttpPut("api/v1/[Controller]")]
        [Authorize(Roles = "Admin,Gerente,Veterinario,Recepcionista")]
        public async Task<IActionResult> Update([FromBody] UpdateServicioRequest request)
        {
            if (request is null) return BadRequest("El request no puede ser nulo");
            var entity = await _repository.FindOneAsync(request.Id);
            if (entity == null) return NotFound($"No se encontró el servicio con Id {request.Id}");

            var duracion = request.DuracionMinutos > 0 ? request.DuracionMinutos : (entity.DuracionMinutos > 0 ? entity.DuracionMinutos : 30);
            entity.Actualizar(request.Nombre, request.Descripcion ?? "", duracion, request.Precio, request.ProductosUtilizados ?? "");

            if (request.Activo.HasValue)
            {
                if (request.Activo.Value) entity.Activar();
                else entity.Desactivar();
            }

            if (!entity.IsValid)
                return BadRequest(entity.GetErrors().Select(e => e.ErrorMessage));

            _repository.Update(request.Id, entity);
            return NoContent();
        }

        [HttpDelete("api/v1/[Controller]/{id}")]
        [Authorize(Roles = "Admin,Gerente,Veterinario,Recepcionista")]
        public async Task<IActionResult> Delete(int id)
        {
            if (id <= 0) return BadRequest("El ID debe ser mayor a 0");
            var entity = await _repository.FindOneAsync(id);
            if (entity == null) return NotFound($"No se encontró el servicio con Id {id}");

            entity.Desactivar();
            _repository.Update(id, entity);
            return NoContent();
        }
    }

    public class CreateServicioRequest
    {
        public string Nombre { get; set; }
        public string Descripcion { get; set; }
        public int DuracionMinutos { get; set; }
        public decimal Precio { get; set; }
        public string ProductosUtilizados { get; set; }
    }

    public class UpdateServicioRequest
    {
        public int Id { get; set; }
        public string Nombre { get; set; }
        public string Descripcion { get; set; }
        public int DuracionMinutos { get; set; }
        public decimal Precio { get; set; }
        public string ProductosUtilizados { get; set; }
        public bool? Activo { get; set; }
    }
}
