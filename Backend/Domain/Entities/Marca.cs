using Core.Domain.Entities;
using Domain.Validators;

namespace Domain.Entities
{
    public class Marca : DomainEntity<int, MarcaValidator>
    {
        public string Nombre { get; private set; }
        public string Descripcion { get; private set; }
        public bool Activo { get; private set; }

        protected Marca() { }

        public Marca(string nombre, string descripcion = "") : this()
        {
            Nombre = nombre;
            Descripcion = descripcion;
            Activo = true;
        }

        public void Actualizar(string nombre, string descripcion = "")
        {
            Nombre = nombre;
            Descripcion = descripcion;
        }
        public void Desactivar() => Activo = false;
        public void Activar() => Activo = true;
    }
}
