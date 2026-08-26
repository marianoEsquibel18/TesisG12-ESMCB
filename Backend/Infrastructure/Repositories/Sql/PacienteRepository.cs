using Application.Repositories;
using Core.Infraestructure.Repositories.Sql;
using Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Repositories.Sql
{
    internal class PacienteRepository : BaseRepository<Paciente>, IPacienteRepository
    {
        public PacienteRepository(StoreDbContext context) : base(context)
        {
        }

        private IQueryable<Paciente> PacientesConNavegacion => Repository
            .Include(p => p.Especie)
            .Include(p => p.Raza)
            .Include(p => p.Propietario);

        public async Task<IEnumerable<Paciente>> GetPacientesExpandidosAsync()
        {
            return await PacientesConNavegacion.ToListAsync();
        }

        public async Task<IEnumerable<Paciente>> GetActivosAsync()
        {
            return await PacientesConNavegacion.Where(p => p.Activo).ToListAsync();
        }

        public async Task<IEnumerable<Paciente>> GetByEspecieIdAsync(int especieId)
        {
            return await PacientesConNavegacion.Where(p => p.EspecieId == especieId && p.Activo).ToListAsync();
        }

        public async Task<IEnumerable<Paciente>> GetByPropietarioIdAsync(string propietarioId)
        {
            return await PacientesConNavegacion.Where(p => p.PropietarioId == propietarioId && p.Activo).ToListAsync();
        }

        public async Task<IEnumerable<Paciente>> SearchByNombreAsync(string nombre)
        {
            var nombreLower = nombre.ToLower();
            return await PacientesConNavegacion
                .Where(p => p.Nombre.ToLower().Contains(nombreLower) && p.Activo)
                .ToListAsync();
        }

        public new async Task<List<Paciente>> FindAllAsync()
        {
            return await PacientesConNavegacion.ToListAsync();
        }

        public new async Task<Paciente?> FindOneAsync(params object[] keyValues)
        {
            if (keyValues.Length > 0 && keyValues[0] is string id)
            {
                return await PacientesConNavegacion.FirstOrDefaultAsync(p => p.Id == id);
            }
            return await base.FindOneAsync(keyValues);
        }

        public new Paciente? FindOne(params object[] keyValues)
        {
            if (keyValues.Length > 0 && keyValues[0] is string id)
            {
                return PacientesConNavegacion.FirstOrDefault(p => p.Id == id);
            }
            return base.FindOne(keyValues);
        }
    }
}
