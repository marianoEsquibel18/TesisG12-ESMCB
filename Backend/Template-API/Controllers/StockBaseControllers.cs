using Application.DataTransferObjects;
using Application.Repositories;
using Core.Application;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Controllers
{
    [ApiController]
    public class CategoriaController(ICategoriaRepository repo) : BaseController
    {
        private readonly ICategoriaRepository _repo = repo ?? throw new ArgumentNullException(nameof(repo));

        [HttpGet("api/v1/[Controller]")]
        public async Task<IActionResult> GetAll(bool soloActivas = true)
        {
            var entities = soloActivas ? await _repo.GetActivasAsync() : await _repo.FindAllAsync();
            var dtos = entities.Select(c => new CategoriaDto { Id = c.Id, Nombre = c.Nombre, Descripcion = c.Descripcion, Activo = c.Activo }).ToList();
            return Ok(new QueryResult<CategoriaDto>(dtos, dtos.Count, 1, 10));
        }

        [HttpGet("api/v1/[Controller]/{id}")]
        public async Task<IActionResult> GetById(int id)
        {
            var e = await _repo.FindOneAsync(id);
            if (e == null) return NotFound();
            return Ok(new CategoriaDto { Id = e.Id, Nombre = e.Nombre, Descripcion = e.Descripcion, Activo = e.Activo });
        }

        [HttpPost("api/v1/[Controller]")]
        [Authorize(Roles = "Admin,Gerente,Recepcionista")]
        public async Task<IActionResult> Create([FromBody] CreateCategoriaRequest r)
        {
            var entity = new Domain.Entities.Categoria(r.Nombre, r.Descripcion ?? "");
            if (!entity.IsValid) return BadRequest(entity.GetErrors().Select(e => e.ErrorMessage));
            var id = await _repo.AddAsync(entity);
            return Created($"api/v1/Categoria/{id}", new { Id = id });
        }

        [HttpPut("api/v1/[Controller]")]
        [Authorize(Roles = "Admin,Gerente,Recepcionista")]
        public async Task<IActionResult> Update([FromBody] UpdateCategoriaRequest r)
        {
            var e = await _repo.FindOneAsync(r.Id);
            if (e == null) return NotFound();
            e.Actualizar(r.Nombre, r.Descripcion ?? "");
            if (r.Activo.HasValue)
            {
                if (r.Activo.Value) e.Activar();
                else e.Desactivar();
            }
            if (!e.IsValid) return BadRequest(e.GetErrors().Select(x => x.ErrorMessage));
            _repo.Update(r.Id, e);
            return NoContent();
        }

        [HttpDelete("api/v1/[Controller]/{id}")]
        [Authorize(Roles = "Admin,Gerente,Recepcionista")]
        public async Task<IActionResult> Delete(int id)
        {
            var e = await _repo.FindOneAsync(id);
            if (e == null) return NotFound();
            e.Desactivar(); _repo.Update(id, e);
            return NoContent();
        }
    }

    public class CreateCategoriaRequest { public string Nombre { get; set; } public string Descripcion { get; set; } }
    public class UpdateCategoriaRequest { public int Id { get; set; } public string Nombre { get; set; } public string Descripcion { get; set; } public bool? Activo { get; set; } }

    [ApiController]
    public class MarcaController(IMarcaRepository repo) : BaseController
    {
        private readonly IMarcaRepository _repo = repo ?? throw new ArgumentNullException(nameof(repo));

        [HttpGet("api/v1/[Controller]")]
        public async Task<IActionResult> GetAll(bool soloActivas = true)
        {
            var entities = soloActivas ? await _repo.GetActivasAsync() : await _repo.FindAllAsync();
            var dtos = entities.Select(m => new MarcaDto { Id = m.Id, Nombre = m.Nombre, Descripcion = m.Descripcion, Activo = m.Activo }).ToList();
            return Ok(new QueryResult<MarcaDto>(dtos, dtos.Count, 1, 10));
        }

        [HttpGet("api/v1/[Controller]/{id}")]
        public async Task<IActionResult> GetById(int id)
        {
            var e = await _repo.FindOneAsync(id);
            if (e == null) return NotFound();
            return Ok(new MarcaDto { Id = e.Id, Nombre = e.Nombre, Descripcion = e.Descripcion, Activo = e.Activo });
        }

        [HttpPost("api/v1/[Controller]")]
        [Authorize(Roles = "Admin,Gerente,Recepcionista")]
        public async Task<IActionResult> Create([FromBody] CreateMarcaRequest r)
        {
            var entity = new Domain.Entities.Marca(r.Nombre, r.Descripcion ?? "");
            if (!entity.IsValid) return BadRequest(entity.GetErrors().Select(e => e.ErrorMessage));
            var id = await _repo.AddAsync(entity);
            return Created($"api/v1/Marca/{id}", new { Id = id });
        }

        [HttpPut("api/v1/[Controller]")]
        [Authorize(Roles = "Admin,Gerente,Recepcionista")]
        public async Task<IActionResult> Update([FromBody] UpdateMarcaRequest r)
        {
            var e = await _repo.FindOneAsync(r.Id);
            if (e == null) return NotFound();
            e.Actualizar(r.Nombre, r.Descripcion ?? "");
            if (r.Activo.HasValue)
            {
                if (r.Activo.Value) e.Activar();
                else e.Desactivar();
            }
            if (!e.IsValid) return BadRequest(e.GetErrors().Select(x => x.ErrorMessage));
            _repo.Update(r.Id, e);
            return NoContent();
        }

        [HttpDelete("api/v1/[Controller]/{id}")]
        [Authorize(Roles = "Admin,Gerente,Recepcionista")]
        public async Task<IActionResult> Delete(int id)
        {
            var e = await _repo.FindOneAsync(id);
            if (e == null) return NotFound();
            e.Desactivar(); _repo.Update(id, e);
            return NoContent();
        }
    }

    public class CreateMarcaRequest { public string Nombre { get; set; } public string Descripcion { get; set; } }
    public class UpdateMarcaRequest { public int Id { get; set; } public string Nombre { get; set; } public string Descripcion { get; set; } public bool? Activo { get; set; } }

    [ApiController]
    public class ProveedorController(IProveedorRepository repo) : BaseController
    {
        private readonly IProveedorRepository _repo = repo ?? throw new ArgumentNullException(nameof(repo));

        [HttpGet("api/v1/[Controller]")]
        public async Task<IActionResult> GetAll(bool soloActivos = true)
        {
            var entities = soloActivos ? await _repo.GetActivosAsync() : await _repo.FindAllAsync();
            var dtos = entities.Select(MapToDto).ToList();
            return Ok(new QueryResult<ProveedorDto>(dtos, dtos.Count, 1, 10));
        }

        [HttpGet("api/v1/[Controller]/{id}")]
        public async Task<IActionResult> GetById(string id)
        {
            var e = await _repo.FindOneAsync(id);
            if (e == null) return NotFound();
            return Ok(MapToDto(e));
        }

        [HttpGet("api/v1/[Controller]/byCuit/{cuit}")]
        public async Task<IActionResult> GetByCuit(string cuit)
        {
            var e = await _repo.GetByCUITAsync(cuit);
            if (e == null) return NotFound();
            return Ok(MapToDto(e));
        }

        [HttpPost("api/v1/[Controller]")]
        [Authorize(Roles = "Admin,Gerente,Recepcionista")]
        public async Task<IActionResult> Create([FromBody] CreateProveedorRequest r)
        {
            var existing = await _repo.GetByCUITAsync(r.CUIT);
            if (existing != null) return BadRequest($"Ya existe un proveedor con CUIT {r.CUIT}");

            var entity = new Domain.Entities.Proveedor(r.RazonSocial, r.CUIT, r.Telefono,
                r.Email ?? "", r.Direccion ?? "", r.Contacto ?? "");
            if (!entity.IsValid) return BadRequest(entity.GetErrors().Select(e => e.ErrorMessage));
            var id = await _repo.AddAsync(entity);
            return Created($"api/v1/Proveedor/{id}", new { Id = id });
        }

        [HttpPut("api/v1/[Controller]")]
        [Authorize(Roles = "Admin,Gerente,Recepcionista")]
        public async Task<IActionResult> Update([FromBody] UpdateProveedorRequest r)
        {
            var e = await _repo.FindOneAsync(r.Id);
            if (e == null) return NotFound();
            e.Actualizar(r.RazonSocial, r.CUIT, r.Telefono, r.Email ?? "", r.Direccion ?? "", r.Contacto ?? "");
            if (r.Activo.HasValue)
            {
                if (r.Activo.Value) e.Activar();
                else e.Desactivar();
            }
            if (!e.IsValid) return BadRequest(e.GetErrors().Select(x => x.ErrorMessage));
            _repo.Update(r.Id, e);
            return NoContent();
        }

        [HttpDelete("api/v1/[Controller]/{id}")]
        [Authorize(Roles = "Admin,Gerente,Recepcionista")]
        public async Task<IActionResult> Delete(string id)
        {
            var e = await _repo.FindOneAsync(id);
            if (e == null) return NotFound();
            e.Desactivar(); _repo.Update(id, e);
            return NoContent();
        }

        private static ProveedorDto MapToDto(Domain.Entities.Proveedor p) => new()
        {
            Id = p.Id, RazonSocial = p.RazonSocial, CUIT = p.CUIT,
            Telefono = p.Telefono, Email = p.Email, Direccion = p.Direccion,
            Contacto = p.Contacto, Activo = p.Activo
        };
    }

    public class CreateProveedorRequest
    {
        public string RazonSocial { get; set; } public string CUIT { get; set; }
        public string Telefono { get; set; } public string Email { get; set; }
        public string Direccion { get; set; } public string Contacto { get; set; }
    }

    public class UpdateProveedorRequest
    {
        public string Id { get; set; } public string RazonSocial { get; set; }
        public string CUIT { get; set; }
        public string Telefono { get; set; } public string Email { get; set; }
        public string Direccion { get; set; } public string Contacto { get; set; }
        public bool? Activo { get; set; }
    }

    [ApiController]
    public class DepositoController(IDepositoRepository repo) : BaseController
    {
        private readonly IDepositoRepository _repo = repo ?? throw new ArgumentNullException(nameof(repo));

        [HttpGet("api/v1/[Controller]")]
        public async Task<IActionResult> GetAll(bool soloActivos = true)
        {
            var entities = soloActivos ? await _repo.GetActivosAsync() : await _repo.FindAllAsync();
            if (!IsAdmin && UserSucursalId.HasValue)
            {
                entities = entities.Where(d => d.SucursalId == UserSucursalId.Value).ToList();
            }
            var dtos = entities.Select(MapToDto).ToList();
            return Ok(new QueryResult<DepositoDto>(dtos, dtos.Count, 1, 10));
        }

        [HttpGet("api/v1/[Controller]/{id}")]
        public async Task<IActionResult> GetById(int id)
        {
            var e = await _repo.FindOneAsync(id);
            if (e == null) return NotFound();
            if (!IsAdmin && UserSucursalId.HasValue && e.SucursalId != UserSucursalId.Value)
            {
                return StatusCode(403, "No tiene permisos para ver un depósito de otra sucursal");
            }
            return Ok(MapToDto(e));
        }

        [HttpPost("api/v1/[Controller]")]
        [Authorize(Roles = "Admin,Gerente,Recepcionista")]
        public async Task<IActionResult> Create([FromBody] CreateDepositoRequest r)
        {
            int targetSucursalId = 1; // default to Sucursal Central
            if (UserSucursalId.HasValue)
            {
                targetSucursalId = UserSucursalId.Value;
            }
            else if (r.SucursalId.HasValue)
            {
                targetSucursalId = r.SucursalId.Value;
            }

            var entity = new Domain.Entities.Deposito(r.Nombre, r.Ubicacion ?? "", targetSucursalId);
            if (!entity.IsValid) return BadRequest(entity.GetErrors().Select(e => e.ErrorMessage));
            var id = await _repo.AddAsync(entity);
            return Created($"api/v1/Deposito/{id}", new { Id = id });
        }

        [HttpPut("api/v1/[Controller]")]
        [Authorize(Roles = "Admin,Gerente,Recepcionista")]
        public async Task<IActionResult> Update([FromBody] UpdateDepositoRequest r)
        {
            var e = await _repo.FindOneAsync(r.Id);
            if (e == null) return NotFound();

            if (!IsAdmin && UserSucursalId.HasValue && e.SucursalId != UserSucursalId.Value)
            {
                return StatusCode(403, "No tiene permisos para modificar un depósito de otra sucursal");
            }

            int targetSucursalId = e.SucursalId;
            if (!IsAdmin && UserSucursalId.HasValue)
            {
                targetSucursalId = UserSucursalId.Value;
            }
            else if (r.SucursalId.HasValue)
            {
                targetSucursalId = r.SucursalId.Value;
            }

            e.Actualizar(r.Nombre, r.Ubicacion ?? "");
            e.AsignarSucursal(targetSucursalId);
            if (r.Activo.HasValue)
            {
                if (r.Activo.Value) e.Activar();
                else e.Desactivar();
            }

            if (!e.IsValid) return BadRequest(e.GetErrors().Select(x => x.ErrorMessage));
            _repo.Update(r.Id, e);
            return NoContent();
        }

        [HttpDelete("api/v1/[Controller]/{id}")]
        [Authorize(Roles = "Admin,Gerente,Recepcionista")]
        public async Task<IActionResult> Delete(int id)
        {
            var e = await _repo.FindOneAsync(id);
            if (e == null) return NotFound();

            if (!IsAdmin && UserSucursalId.HasValue && e.SucursalId != UserSucursalId.Value)
            {
                return StatusCode(403, "No tiene permisos para modificar un depósito de otra sucursal");
            }

            e.Desactivar(); _repo.Update(id, e);
            return NoContent();
        }

        private static DepositoDto MapToDto(Domain.Entities.Deposito d) => new()
        {
            Id = d.Id,
            Nombre = d.Nombre,
            Ubicacion = d.Ubicacion,
            Activo = d.Activo,
            SucursalId = d.SucursalId,
            SucursalNombre = d.Sucursal?.Nombre ?? ""
        };
    }

    public class CreateDepositoRequest { public string Nombre { get; set; } public string Ubicacion { get; set; } public int? SucursalId { get; set; } }
    public class UpdateDepositoRequest { public int Id { get; set; } public string Nombre { get; set; } public string Ubicacion { get; set; } public int? SucursalId { get; set; } public bool? Activo { get; set; } }
}
