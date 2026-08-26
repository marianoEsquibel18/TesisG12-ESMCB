using Application.Repositories;
using Core.Infraestructure.Repositories.Sql;
using Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Repositories.Sql
{
    internal class RegistroVacunacionRepository : BaseRepository<RegistroVacunacion>, IRegistroVacunacionRepository
    {
        public RegistroVacunacionRepository(StoreDbContext context) : base(context) { }

        public async Task<IEnumerable<RegistroVacunacion>> GetByPacienteIdAsync(string pacienteId)
        {
            return await Repository
                .Include(r => r.Vacuna)
                .Include(r => r.Producto)
                .Where(r => r.PacienteId == pacienteId)
                .OrderByDescending(r => r.FechaAplicacion)
                .ToListAsync();
        }

        public async Task<IEnumerable<RegistroVacunacion>> GetVacunasPendientesAsync()
        {
            return await Repository
                .Where(r => r.FechaProximaDosis.HasValue && r.FechaProximaDosis.Value <= DateTime.Now)
                .OrderBy(r => r.FechaProximaDosis)
                .ToListAsync();
        }

        public async Task<IEnumerable<RegistroVacunacion>> GetByVacunaIdAsync(int vacunaId)
        {
            return await Repository
                .Where(r => r.VacunaId == vacunaId)
                .OrderByDescending(r => r.FechaAplicacion)
                .ToListAsync();
        }
    }
}
