using Application.Repositories;
using Core.Infraestructure.Repositories.Sql;
using Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Repositories.Sql
{
    internal class RolRepository : BaseRepository<Rol>, IRolRepository
    {
        public RolRepository(StoreDbContext context) : base(context) { }
        public async Task<IEnumerable<Rol>> GetActivosAsync() =>
            await Repository.Where(r => r.Activo).ToListAsync();
        public async Task<Rol> GetByNombreAsync(string nombre) =>
            await Repository.FirstOrDefaultAsync(r => r.Nombre == nombre);
    }

    internal class UsuarioRepository : BaseRepository<Usuario>, IUsuarioRepository
    {
        public UsuarioRepository(StoreDbContext context) : base(context) { }
        public async Task<Usuario> GetByNombreUsuarioAsync(string nombreUsuario) =>
            await Repository.Include(u => u.Rol).Include(u => u.Sucursal).FirstOrDefaultAsync(u => u.NombreUsuario == nombreUsuario);
        public async Task<Usuario> GetByEmailAsync(string email) =>
            await Repository.Include(u => u.Rol).Include(u => u.Sucursal).FirstOrDefaultAsync(u => u.Email == email);
        public async Task<IEnumerable<Usuario>> GetActivosAsync() =>
            await Repository.Include(u => u.Rol).Include(u => u.Sucursal).Where(u => u.Activo).ToListAsync();
        public async Task<IEnumerable<Usuario>> GetByRolIdAsync(int rolId) =>
            await Repository.Include(u => u.Rol).Include(u => u.Sucursal).Where(u => u.RolId == rolId).ToListAsync();
        public async Task<IEnumerable<Usuario>> GetAllWithNavigationAsync() =>
            await Repository.Include(u => u.Rol).Include(u => u.Sucursal).ToListAsync();
    }
}
