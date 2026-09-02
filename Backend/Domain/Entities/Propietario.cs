using Core.Domain.Entities;
using Domain.Validators;

namespace Domain.Entities
{
    /// <summary>
    /// Representa al dueño/propietario de una o más mascotas
    /// </summary>
    public class Propietario : DomainEntity<string, PropietarioValidator>
    {
        public string Nombre { get; private set; }
        public string Apellido { get; private set; }
        public string DNI { get; private set; }
        public string Telefono { get; private set; }
        public string Email { get; private set; }
        public string Direccion { get; private set; }
        public DateTime FechaRegistro { get; private set; }
        public bool Activo { get; private set; }

        // Navegación
        public virtual ICollection<Paciente> Mascotas { get; private set; }

        protected Propietario()
        {
            Mascotas = new List<Paciente>();
        }

        public Propietario(string nombre, string apellido, string dni, string telefono, string email = "", string direccion = "") : this()
        {
            Id = Guid.NewGuid().ToString();
            Nombre = nombre;
            Apellido = apellido;
            DNI = dni;
            Telefono = telefono;
            Email = email;
            Direccion = direccion;
            FechaRegistro = DateTime.Now;
            Activo = true;
        }

        public string NombreCompleto => $"{Nombre} {Apellido}";

        public void Actualizar(string nombre, string apellido, string telefono, string email, string direccion)
        {
            Nombre = nombre;
            Apellido = apellido;
            Telefono = telefono;
            Email = email;
            Direccion = direccion;
        }

        public void Actualizar(string nombre, string apellido, string dni, string telefono, string email, string direccion)
        {
            Nombre = nombre;
            Apellido = apellido;
            if (!string.IsNullOrWhiteSpace(dni))
            {
                DNI = dni;
            }
            Telefono = telefono;
            Email = email;
            Direccion = direccion;
        }

        public void ActualizarDNI(string dni)
        {
            DNI = dni;
        }

        public void Desactivar()
        {
            Activo = false;
        }

        public void Activar()
        {
            Activo = true;
        }
    }
}
