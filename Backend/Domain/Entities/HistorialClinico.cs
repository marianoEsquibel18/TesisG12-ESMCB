using Core.Domain.Entities;
using Domain.Validators;

namespace Domain.Entities
{
    /// <summary>
    /// Representa una entrada en el historial clínico de un paciente.
    /// Cada entrada registra una consulta, control o evento médico.
    /// </summary>
    public class HistorialClinico : DomainEntity<string, HistorialClinicoValidator>
    {
        public string PacienteId { get; private set; }
        public DateTime Fecha { get; private set; }
        public string Motivo { get; private set; }
        public string Sintomas { get; private set; }
        public string Diagnostico { get; private set; }
        public string Indicaciones { get; private set; }
        public string Veterinario { get; private set; }
        public decimal? Peso { get; private set; }  // Peso en kg al momento de la consulta
        public decimal? Temperatura { get; private set; }  // Temperatura corporal
        public string Observaciones { get; private set; }
        public string? ArchivosAdjuntos { get; private set; } = "[]";

        // Navegación
        public virtual Paciente Paciente { get; private set; }

        protected HistorialClinico() { }

        public HistorialClinico(
            string pacienteId,
            DateTime fecha,
            string motivo,
            string veterinario,
            string sintomas = "",
            string diagnostico = "",
            string indicaciones = "",
            decimal? peso = null,
            decimal? temperatura = null,
            string observaciones = "",
            string? archivosAdjuntos = "[]") : this()
        {
            Id = Guid.NewGuid().ToString();
            PacienteId = pacienteId;
            Fecha = fecha;
            Motivo = motivo;
            Veterinario = veterinario;
            Sintomas = sintomas;
            Diagnostico = diagnostico;
            Indicaciones = indicaciones;
            Peso = peso;
            Temperatura = temperatura;
            Observaciones = observaciones;
            ArchivosAdjuntos = archivosAdjuntos ?? "[]";
        }

        public void Actualizar(string motivo, string sintomas, string diagnostico, string indicaciones, 
            decimal? peso, decimal? temperatura, string observaciones, string? archivosAdjuntos = null)
        {
            Motivo = motivo;
            Sintomas = sintomas;
            Diagnostico = diagnostico;
            Indicaciones = indicaciones;
            Peso = peso;
            Temperatura = temperatura;
            Observaciones = observaciones;
            if (archivosAdjuntos != null)
            {
                ArchivosAdjuntos = archivosAdjuntos;
            }
        }
    }
}
