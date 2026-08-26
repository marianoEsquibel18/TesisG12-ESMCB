using Domain.Entities;

namespace Application.DataTransferObjects
{
    public class TurnoDto
    {
        public string Id { get; set; }
        public string PacienteId { get; set; }
        public string PacienteNombre { get; set; }
        public string PropietarioNombre { get; set; }
        public string VeterinarioId { get; set; }
        public string VeterinarioNombre { get; set; }
        public int ServicioId { get; set; }
        public string ServicioNombre { get; set; }
        public DateTime FechaHora { get; set; }
        public DateTime FechaHoraFin { get; set; }
        public int DuracionMinutos { get; set; }
        public string Estado { get; set; }
        public string Motivo { get; set; }
        public string Observaciones { get; set; }
        public int? SucursalId { get; set; }
        public string? SucursalNombre { get; set; }
        public DateTime FechaCreacion { get; set; }
        public string? ArchivosAdjuntos { get; set; } = "[]";
    }
}
