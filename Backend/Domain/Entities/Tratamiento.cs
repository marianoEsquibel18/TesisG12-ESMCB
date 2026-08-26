using Core.Domain.Entities;
using Domain.Validators;

namespace Domain.Entities
{
    /// <summary>
    /// Representa un tratamiento médico registrado para un paciente
    /// </summary>
    public class Tratamiento : DomainEntity<string, TratamientoValidator>
    {
        public string PacienteId { get; private set; }
        public DateTime Fecha { get; private set; }
        public string Diagnostico { get; private set; }
        public string Descripcion { get; private set; }
        public string Medicacion { get; private set; }
        public string Veterinario { get; private set; }
        public string Observaciones { get; private set; }
        public bool Finalizado { get; private set; }
        public string? ArchivosAdjuntos { get; private set; } = "[]";

        // Navegación
        public virtual Paciente Paciente { get; private set; }

        protected Tratamiento() { }

        public Tratamiento(
            string pacienteId,
            DateTime fecha,
            string diagnostico,
            string descripcion,
            string veterinario,
            string medicacion = "",
            string observaciones = "",
            string? archivosAdjuntos = "[]") : this()
        {
            Id = Guid.NewGuid().ToString();
            PacienteId = pacienteId;
            Fecha = fecha;
            Diagnostico = diagnostico;
            Descripcion = descripcion;
            Medicacion = medicacion;
            Veterinario = veterinario;
            Observaciones = observaciones;
            Finalizado = false;
            ArchivosAdjuntos = archivosAdjuntos ?? "[]";
        }

        public void Actualizar(string diagnostico, string descripcion, string medicacion, string observaciones, string? archivosAdjuntos = null)
        {
            Diagnostico = diagnostico;
            Descripcion = descripcion;
            Medicacion = medicacion;
            Observaciones = observaciones;
            if (archivosAdjuntos != null)
            {
                ArchivosAdjuntos = archivosAdjuntos;
            }
        }

        public void Finalizar()
        {
            Finalizado = true;
        }

        public void Reabrir()
        {
            Finalizado = false;
        }
    }
}
