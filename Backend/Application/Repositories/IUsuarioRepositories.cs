using Core.Application.Repositories;
using Domain.Entities;

namespace Application.Repositories
{
    public interface IRolRepository : IRepository<Rol>
    {
        Task<IEnumerable<Rol>> GetActivosAsync();
        Task<Rol> GetByNombreAsync(string nombre);
    }

    public interface IUsuarioRepository : IRepository<Usuario>
    {
        Task<Usuario> GetByNombreUsuarioAsync(string nombreUsuario);
        Task<Usuario> GetByEmailAsync(string email);
        Task<IEnumerable<Usuario>> GetActivosAsync();
        Task<IEnumerable<Usuario>> GetByRolIdAsync(int rolId);
        Task<IEnumerable<Usuario>> GetAllWithNavigationAsync();
    }
}
