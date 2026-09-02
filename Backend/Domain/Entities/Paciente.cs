using Core.Domain.Entities;
using Domain.Validators;

namespace Domain.Entities
{
    /// <summary>
    /// Representa a una mascota/paciente de la clínica veterinaria
    /// </summary>
    public class Paciente : DomainEntity<string, PacienteValidator>
    {
        public string Nombre { get; private set; }
        public int EspecieId { get; private set; }
        public int? RazaId { get; private set; }
        public string Sexo { get; private set; }  // "M" = Macho, "H" = Hembra
        public DateTime? FechaNacimiento { get; private set; }
        public string PropietarioId { get; private set; }
        public string FotoUrl { get; private set; }
        public string Observaciones { get; private set; }
        public DateTime FechaRegistro { get; private set; }
        public bool Activo { get; private set; }
        public int ContadorInasistencias { get; private set; } = 0;

        // Navegación
        public virtual Especie Especie { get; private set; }
        public virtual Raza Raza { get; private set; }
        public virtual Propietario Propietario { get; private set; }

        protected Paciente()
        {
        }

        public Paciente(
            string nombre,
            int especieId,
            string propietarioId,
            string sexo,
            int? razaId = null,
            DateTime? fechaNacimiento = null,
            string fotoUrl = "",
            string observaciones = "") : this()
        {
            Id = Guid.NewGuid().ToString();
            Nombre = nombre;
            EspecieId = especieId;
            RazaId = razaId;
            Sexo = sexo;
            FechaNacimiento = fechaNacimiento;
            PropietarioId = propietarioId;
            FotoUrl = fotoUrl;
            Observaciones = observaciones;
            FechaRegistro = DateTime.Now;
            Activo = true;
        }

        public int? EdadEnAnios
        {
            get
            {
                if (!FechaNacimiento.HasValue) return null;
                var today = DateTime.Today;
                var age = today.Year - FechaNacimiento.Value.Year;
                if (FechaNacimiento.Value.Date > today.AddYears(-age)) age--;
                return age;
            }
        }

        public void Actualizar(string nombre, string sexo, DateTime? fechaNacimiento, string observaciones)
        {
            Nombre = nombre;
            Sexo = sexo;
            FechaNacimiento = fechaNacimiento;
            Observaciones = observaciones;
        }

        public void CambiarEspecie(int especieId, int? razaId = null)
        {
            EspecieId = especieId;
            RazaId = razaId;
        }

        public void CambiarRaza(int razaId)
        {
            RazaId = razaId;
        }

        public void CambiarPropietario(string propietarioId)
        {
            PropietarioId = propietarioId;
        }

        public void ActualizarFoto(string fotoUrl)
        {
            FotoUrl = fotoUrl;
        }

        public void Desactivar()
        {
            Activo = false;
        }

        public void Activar()
        {
            Activo = true;
        }

        public void IncrementarInasistencias()
        {
            ContadorInasistencias++;
        }

        public void DecrementarInasistencias()
        {
            if (ContadorInasistencias > 0)
            {
                ContadorInasistencias--;
            }
        }

        public void ResetearInasistencias()
        {
            ContadorInasistencias = 0;
        }

        public void EstablecerInasistencias(int cantidad)
        {
            ContadorInasistencias = Math.Max(0, cantidad);
        }
    }
}
