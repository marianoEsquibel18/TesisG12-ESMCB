using Application.DataTransferObjects;
using Application.Repositories;
using Core.Application;
using Domain.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace Controllers
{
    [ApiController]
    public class AuthController(
        IUsuarioRepository usuarioRepo,
        IRolRepository rolRepo,
        IVeterinarioRepository veterinarioRepo,
        IAuditLogRepository auditRepo,
        IConfiguration config) : BaseController
    {
        private readonly IUsuarioRepository _usuarioRepo = usuarioRepo;
        private readonly IRolRepository _rolRepo = rolRepo;
        private readonly IVeterinarioRepository _veterinarioRepo = veterinarioRepo;
        private readonly IAuditLogRepository _auditRepo = auditRepo;
        private readonly IConfiguration _config = config;

        /// <summary>
        /// Login - devuelve JWT token
        /// </summary>
        [HttpPost("api/v1/auth/login")]
        public async Task<IActionResult> Login([FromBody] LoginRequest request)
        {
            var usuario = await _usuarioRepo.GetByNombreUsuarioAsync(request.NombreUsuario);
            if (usuario == null || !usuario.Activo)
                return Unauthorized("Usuario o contraseña incorrectos");

            if (!usuario.VerifyPassword(request.Password))
                return Unauthorized("Usuario o contraseña incorrectos");

            usuario.RegistrarLogin();
            _usuarioRepo.Update(usuario.Id, usuario);

            var token = GenerateJwtToken(usuario);
            var expiry = DateTime.UtcNow.AddHours(8);

            return Ok(new LoginResponseDto
            {
                Token = token,
                Expiracion = expiry,
                Usuario = MapToDto(usuario)
            });
        }

        /// <summary>
        /// Registrar nuevo usuario (Admin y Gerente)
        /// </summary>
        [HttpPost("api/v1/auth/register")]
        [Authorize(Roles = "Admin,Gerente")]
        public async Task<IActionResult> Register([FromBody] RegisterRequest request)
        {
            var rol = await _rolRepo.FindOneAsync(request.RolId);
            if (rol == null) return BadRequest($"No existe el rol con Id {request.RolId}");

            // Restricción para rol Gerente
            if (!IsAdmin)
            {
                if (rol.Nombre != "Veterinario" && rol.Nombre != "Recepcionista")
                {
                    return BadRequest("El rol Gerente solo tiene permisos para registrar usuarios con rol Veterinario o Recepcionista.");
                }

                if (UserSucursalId.HasValue)
                {
                    request.SucursalId = UserSucursalId.Value;
                }
            }

            var existingByUsername = await _usuarioRepo.GetByNombreUsuarioAsync(request.NombreUsuario);
            if (existingByUsername != null)
                return BadRequest($"Ya existe un usuario con el nombre '{request.NombreUsuario}'");

            var existingByEmail = await _usuarioRepo.GetByEmailAsync(request.Email);
            if (existingByEmail != null)
                return BadRequest($"Ya existe un usuario con el email '{request.Email}'");

            var usuario = new Usuario(request.NombreUsuario, request.Email,
                request.NombreCompleto, request.Password, request.RolId, request.SucursalId);

            if (!usuario.IsValid) return BadRequest(usuario.GetErrors().Select(e => e.ErrorMessage));

            if (rol.Nombre == "Veterinario")
            {
                var partes = (request.NombreCompleto ?? "").Trim().Split(' ', 2);
                var nombre = partes.Length > 0 ? partes[0] : request.NombreUsuario;
                var apellido = partes.Length > 1 ? partes[1] : "Veterinario";

                if (string.IsNullOrWhiteSpace(nombre)) nombre = request.NombreUsuario;
                if (string.IsNullOrWhiteSpace(apellido)) apellido = "Veterinario";

                if (nombre.Length > 50) nombre = nombre.Substring(0, 50);
                if (apellido.Length > 50) apellido = apellido.Substring(0, 50);

                var uniqueSuffix = Guid.NewGuid().ToString("N").Substring(0, 8).ToUpper();
                var matricula = $"MP-{uniqueSuffix}";
                var telefono = "0000000000";

                var nuevoVet = new Veterinario(
                    nombre,
                    apellido,
                    matricula,
                    telefono,
                    request.Email ?? "",
                    "General",
                    request.SucursalId ?? 0
                );

                var vetIdObj = await _veterinarioRepo.AddAsync(nuevoVet);
                var vetId = vetIdObj?.ToString() ?? nuevoVet.Id;

                usuario.SetVeterinarioId(vetId);
            }

            var id = await _usuarioRepo.AddAsync(usuario);
            return Created($"api/v1/auth/usuarios/{id}", MapToDto(usuario));
        }

        /// <summary>
        /// Seed inicial - crea roles por defecto y usuario admin (solo si no hay usuarios)
        /// </summary>
        [HttpPost("api/v1/auth/seed")]
        public async Task<IActionResult> Seed()
        {
            // Crear roles
            var roles = new[] { "Admin", "Veterinario", "Recepcionista", "Gerente" };
            foreach (var rolName in roles)
            {
                var existing = await _rolRepo.GetByNombreAsync(rolName);
                if (existing == null)
                    await _rolRepo.AddAsync(new Rol(rolName, $"Rol de {rolName}"));
            }

            var existingUsers = await _usuarioRepo.GetActivosAsync();
            if (!existingUsers.Any())
            {
                var adminRol = await _rolRepo.GetByNombreAsync("Admin");
                // Crear usuario admin
                var admin = new Usuario("admin", "admin@veterinaria.com",
                    "Administrador del Sistema", "Admin123!", adminRol.Id);
                await _usuarioRepo.AddAsync(admin);
                return Ok(new { Message = "Seed completado. Creados roles y usuario admin.", AdminUser = "admin", AdminPass = "Admin123!" });
            }

            return Ok(new { Message = "Roles verificados/creados. No se creó usuario admin porque ya existen usuarios en el sistema." });
        }

        // ═══════════════════════════════════════════
        // PERFIL DEL USUARIO AUTENTICADO
        // ═══════════════════════════════════════════

        /// <summary>
        /// Obtener perfil del usuario autenticado
        /// </summary>
        [HttpGet("api/v1/auth/me")]
        [Authorize]
        public async Task<IActionResult> GetProfile()
        {
            var username = User.FindFirst("name")?.Value;
            if (string.IsNullOrEmpty(username)) return Unauthorized();

            var usuario = await _usuarioRepo.GetByNombreUsuarioAsync(username);
            if (usuario == null) return NotFound();
            return Ok(MapToDto(usuario));
        }

        /// <summary>
        /// Actualizar perfil del usuario autenticado (nombre completo, email)
        /// </summary>
        [HttpPut("api/v1/auth/perfil")]
        [Authorize]
        public async Task<IActionResult> UpdateProfile([FromBody] UpdateProfileRequest request)
        {
            var userId = User.FindFirst("sub")?.Value;
            var usuario = await _usuarioRepo.FindOneAsync(userId);
            if (usuario == null) return NotFound();

            usuario.Actualizar(request.Email ?? usuario.Email, request.NombreCompleto ?? usuario.NombreCompleto);
            _usuarioRepo.Update(usuario.Id, usuario);
            return Ok(MapToDto(usuario));
        }

        /// <summary>
        /// Subir/actualizar foto de perfil (base64)
        /// </summary>
        [HttpPut("api/v1/auth/perfil/foto")]
        [Authorize]
        public async Task<IActionResult> UpdateProfilePhoto([FromBody] UpdatePhotoRequest request)
        {
            var userId = User.FindFirst("sub")?.Value;
            var usuario = await _usuarioRepo.FindOneAsync(userId);
            if (usuario == null) return NotFound();

            usuario.SetFotoUrl(request.FotoBase64);
            _usuarioRepo.Update(usuario.Id, usuario);
            return Ok(MapToDto(usuario));
        }

        /// <summary>
        /// Cambiar contraseña del usuario autenticado
        /// </summary>
        [HttpPut("api/v1/auth/cambiarPassword")]
        [Authorize]
        public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordRequest request)
        {
            var userId = User.FindFirst("sub")?.Value;
            var usuario = await _usuarioRepo.FindOneAsync(userId);
            if (usuario == null) return NotFound();

            if (!usuario.VerifyPassword(request.PasswordActual))
                return BadRequest("La contraseña actual es incorrecta");

            usuario.SetPassword(request.NuevaPassword);
            _usuarioRepo.Update(usuario.Id, usuario);
            return NoContent();
        }

        /// <summary>
        /// Obtener los audit logs del usuario autenticado
        /// </summary>
        [HttpGet("api/v1/auth/me/audit")]
        [Authorize]
        public async Task<IActionResult> GetMyAuditLogs([FromQuery] int cantidad = 30)
        {
            var userId = User.FindFirst("sub")?.Value;
            if (string.IsNullOrEmpty(userId)) return Unauthorized();

            var logs = await _auditRepo.GetByUsuarioIdAsync(userId);
            var result = logs.OrderByDescending(l => l.Fecha).Take(Math.Min(cantidad, 100))
                .Select(a => new AuditLogDto
                {
                    Id = a.Id, UsuarioId = a.UsuarioId, NombreUsuario = a.NombreUsuario,
                    Accion = a.Accion, Entidad = a.Entidad, EntidadId = a.EntidadId,
                    Descripcion = a.Descripcion, IpOrigen = a.IpOrigen,
                    Fecha = a.Fecha, StatusCode = a.StatusCode
                }).ToList();
            return Ok(result);
        }

        /// <summary>
        /// Obtener datos de veterinario vinculado al usuario autenticado
        /// </summary>
        [HttpGet("api/v1/auth/me/veterinario")]
        [Authorize]
        public async Task<IActionResult> GetMyVeterinario()
        {
            var userId = User.FindFirst("sub")?.Value;
            var usuario = await _usuarioRepo.FindOneAsync(userId);
            if (usuario == null) return NotFound();

            if (string.IsNullOrEmpty(usuario.VeterinarioId))
                return Ok((object)null);

            var vet = await _veterinarioRepo.FindOneAsync(usuario.VeterinarioId);
            if (vet == null) return Ok((object)null);

            return Ok(new VeterinarioPerfilDto
            {
                Id = vet.Id, Nombre = vet.Nombre, Apellido = vet.Apellido,
                Matricula = vet.Matricula, Telefono = vet.Telefono,
                Email = vet.Email, Especialidad = vet.Especialidad, Activo = vet.Activo
            });
        }

        /// <summary>
        /// Crear o actualizar registro de veterinario vinculado al usuario autenticado
        /// </summary>
        [HttpPut("api/v1/auth/me/veterinario")]
        [Authorize]
        public async Task<IActionResult> SaveMyVeterinario([FromBody] SaveVeterinarioRequest request)
        {
            var userId = User.FindFirst("sub")?.Value;
            var usuario = await _usuarioRepo.FindOneAsync(userId);
            if (usuario == null) return NotFound();

            // Check role is Veterinario
            var rolName = usuario.Rol?.Nombre;
            if (rolName == null && usuario.RolId > 0)
            {
                var rol = await _rolRepo.FindOneAsync(usuario.RolId);
                rolName = rol?.Nombre;
            }
            if (rolName != "Veterinario")
                return BadRequest("Solo los usuarios con rol Veterinario pueden completar estos datos");

            // Parse nombre completo into nombre/apellido
            var partes = (usuario.NombreCompleto ?? "").Split(' ', 2);
            var nombre = request.Nombre ?? (partes.Length > 0 ? partes[0] : "");
            var apellido = request.Apellido ?? (partes.Length > 1 ? partes[1] : "");

            if (!string.IsNullOrEmpty(usuario.VeterinarioId))
            {
                // Update existing
                var vet = await _veterinarioRepo.FindOneAsync(usuario.VeterinarioId);
                if (vet != null)
                {
                    vet.Actualizar(nombre, apellido, request.Telefono ?? "", request.Email ?? "", request.Especialidad ?? "");
                    
                    if (!vet.IsValid)
                        return BadRequest(vet.GetErrors().Select(e => e.ErrorMessage));

                    _veterinarioRepo.Update(vet.Id, vet);
                    return Ok(new { Message = "Datos de veterinario actualizados", VeterinarioId = vet.Id });
                }
            }

            // Create new
            var nuevoVet = new Veterinario(nombre, apellido, request.Matricula ?? "",
                request.Telefono ?? "", request.Email ?? "", request.Especialidad ?? "");

            if (!nuevoVet.IsValid)
                return BadRequest(nuevoVet.GetErrors().Select(e => e.ErrorMessage));

            var vetId = await _veterinarioRepo.AddAsync(nuevoVet);
            usuario.SetVeterinarioId(vetId.ToString());
            _usuarioRepo.Update(usuario.Id, usuario);

            return Ok(new { Message = "Registro de veterinario creado y vinculado", VeterinarioId = vetId });
        }

        // ═══════════════════════════════════════════
        // GESTIÓN DE USUARIOS (Admin, Gerente)
        // ═══════════════════════════════════════════

        [HttpGet("api/v1/auth/usuarios")]
        [Authorize(Roles = "Admin,Gerente")]
        public async Task<IActionResult> GetAllUsers([FromQuery] bool incluirInactivos = false)
        {
            var usuarios = incluirInactivos
                ? await _usuarioRepo.GetAllWithNavigationAsync()
                : await _usuarioRepo.GetActivosAsync();

            if (!IsAdmin && UserSucursalId.HasValue)
            {
                usuarios = usuarios.Where(u => u.SucursalId == UserSucursalId.Value);
            }

            return Ok(usuarios.Select(MapToDto).ToList());
        }

        [HttpGet("api/v1/auth/roles")]
        public async Task<IActionResult> GetRoles()
        {
            var roles = await _rolRepo.GetActivosAsync();
            return Ok(roles.Select(r => new RolDto
            {
                Id = r.Id, Nombre = r.Nombre, Descripcion = r.Descripcion, Activo = r.Activo
            }).ToList());
        }

        [HttpDelete("api/v1/auth/usuarios/{id}")]
        [Authorize(Roles = "Admin,Gerente")]
        public async Task<IActionResult> DeleteUser(string id)
        {
            var usuario = await _usuarioRepo.FindOneAsync(id);
            if (usuario == null) return NotFound();

            if (usuario.NombreUsuario.Equals("admin", StringComparison.OrdinalIgnoreCase))
            {
                return BadRequest("No se puede revocar el acceso al Super-Administrador del sistema.");
            }

            if (!IsAdmin)
            {
                var rolNombre = usuario.Rol?.Nombre;
                if (string.IsNullOrEmpty(rolNombre) && usuario.RolId > 0)
                {
                    var rol = await _rolRepo.FindOneAsync(usuario.RolId);
                    rolNombre = rol?.Nombre;
                }

                if (rolNombre != "Veterinario" && rolNombre != "Recepcionista")
                {
                    return BadRequest("El rol Gerente solo puede revocar el acceso a usuarios con rol Veterinario o Recepcionista.");
                }

                if (UserSucursalId.HasValue && usuario.SucursalId.HasValue && usuario.SucursalId.Value != UserSucursalId.Value)
                {
                    return BadRequest("No tiene permisos para revocar el acceso a un usuario de otra sucursal.");
                }
            }

            usuario.Desactivar();
            _usuarioRepo.Update(id, usuario);
            return NoContent();
        }

        [HttpPut("api/v1/auth/usuarios/{id}/restaurar")]
        [Authorize(Roles = "Admin,Gerente")]
        public async Task<IActionResult> RestoreUser(string id)
        {
            var usuario = await _usuarioRepo.FindOneAsync(id);
            if (usuario == null) return NotFound();

            if (!IsAdmin)
            {
                var rolNombre = usuario.Rol?.Nombre;
                if (string.IsNullOrEmpty(rolNombre) && usuario.RolId > 0)
                {
                    var rol = await _rolRepo.FindOneAsync(usuario.RolId);
                    rolNombre = rol?.Nombre;
                }

                if (rolNombre != "Veterinario" && rolNombre != "Recepcionista")
                {
                    return BadRequest("El rol Gerente solo puede restaurar usuarios con rol Veterinario o Recepcionista.");
                }

                if (UserSucursalId.HasValue && usuario.SucursalId.HasValue && usuario.SucursalId.Value != UserSucursalId.Value)
                {
                    return BadRequest("No tiene permisos para restaurar un usuario de otra sucursal.");
                }
            }

            usuario.Activar();
            _usuarioRepo.Update(id, usuario);
            return Ok(MapToDto(usuario));
        }

        // ═══════════════════════════════════════════
        // HELPERS
        // ═══════════════════════════════════════════

        private string GenerateJwtToken(Usuario usuario)
        {
            var key = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(_config["Jwt:Key"] ?? "VeterinariaSecretKeyMuyLargaParaDesarrollo2024!@#$"));

            var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var claimsList = new List<Claim>
            {
                new Claim("sub", usuario.Id),
                new Claim("name", usuario.NombreUsuario),
                new Claim("email", usuario.Email),
                new Claim("given_name", usuario.NombreCompleto),
                new Claim("role", usuario.Rol?.Nombre ?? "")
            };

            if (usuario.SucursalId.HasValue)
            {
                claimsList.Add(new Claim("sucursalId", usuario.SucursalId.Value.ToString()));
                if (usuario.Sucursal != null && !string.IsNullOrWhiteSpace(usuario.Sucursal.Nombre))
                {
                    claimsList.Add(new Claim("sucursalNombre", usuario.Sucursal.Nombre));
                }
            }

            var claims = claimsList.ToArray();

            var token = new JwtSecurityToken(
                issuer: _config["Jwt:Issuer"] ?? "VeterinariaAPI",
                audience: _config["Jwt:Audience"] ?? "VeterinariaApp",
                claims: claims,
                expires: DateTime.UtcNow.AddHours(8),
                signingCredentials: credentials);

            return new JwtSecurityTokenHandler().WriteToken(token);
        }

        private static UsuarioDto MapToDto(Usuario u) => new()
        {
            Id = u.Id, NombreUsuario = u.NombreUsuario, Email = u.Email,
            NombreCompleto = u.NombreCompleto, RolId = u.RolId,
            RolNombre = u.Rol?.Nombre ?? "", FotoUrl = u.FotoUrl,
            VeterinarioId = u.VeterinarioId,
            SucursalId = u.SucursalId,
            SucursalNombre = u.Sucursal?.Nombre ?? "",
            FechaCreacion = u.FechaCreacion,
            UltimoLogin = u.UltimoLogin, Activo = u.Activo
        };
    }

    public class LoginRequest
    {
        public string NombreUsuario { get; set; }
        public string Password { get; set; }
    }

    public class RegisterRequest
    {
        public string NombreUsuario { get; set; }
        public string Email { get; set; }
        public string NombreCompleto { get; set; }
        public string Password { get; set; }
        public int RolId { get; set; }
        public int? SucursalId { get; set; }
    }

    public class ChangePasswordRequest
    {
        public string PasswordActual { get; set; }
        public string NuevaPassword { get; set; }
    }

    public class UpdateProfileRequest
    {
        public string? NombreCompleto { get; set; }
        public string? Email { get; set; }
    }

    public class UpdatePhotoRequest
    {
        public string? FotoBase64 { get; set; }
    }

    public class SaveVeterinarioRequest
    {
        public string? Nombre { get; set; }
        public string? Apellido { get; set; }
        public string? Matricula { get; set; }
        public string? Telefono { get; set; }
        public string? Email { get; set; }
        public string? Especialidad { get; set; }
    }

    public class VeterinarioPerfilDto
    {
        public string Id { get; set; }
        public string Nombre { get; set; }
        public string Apellido { get; set; }
        public string Matricula { get; set; }
        public string Telefono { get; set; }
        public string Email { get; set; }
        public string Especialidad { get; set; }
        public bool Activo { get; set; }
    }
}
