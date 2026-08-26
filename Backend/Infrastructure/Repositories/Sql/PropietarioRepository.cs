using Application.Repositories;
using Core.Infraestructure.Repositories.Sql;
using Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Repositories.Sql
{
    internal class PropietarioRepository : BaseRepository<Propietario>, IPropietarioRepository
    {
        public PropietarioRepository(StoreDbContext context) : base(context)
        {
        }

        public async Task<IEnumerable<Propietario>> GetActivosAsync()
        {
            return await Repository.Where(p => p.Activo).ToListAsync();
        }

        public async Task<Propietario> GetByDNIAsync(string dni)
        {
            return await Repository.FirstOrDefaultAsync(p => p.DNI == dni);
        }

        public async Task<IEnumerable<Propietario>> SearchByNombreAsync(string nombre)
        {
            var nombreLower = nombre.ToLower();
            return await Repository
                .Where(p => p.Nombre.ToLower().Contains(nombreLower) || 
                           p.Apellido.ToLower().Contains(nombreLower))
                .ToListAsync();
        }

        public async Task<IEnumerable<Propietario>> GetPropietariosConMascotasAsync()
        {
            return await Repository
                .Include(p => p.Mascotas)
                    .ThenInclude(m => m.Especie)
                .Include(p => p.Mascotas)
                    .ThenInclude(m => m.Raza)
                .ToListAsync();
        }
    }
}
