namespace Application.DataTransferObjects
{
    public class HistorialClinicoDto
    {
        public string Id { get; set; }
        public string PacienteId { get; set; }
        public string PacienteNombre { get; set; }
        public DateTime Fecha { get; set; }
        public string Motivo { get; set; }
        public string Sintomas { get; set; }
        public string Diagnostico { get; set; }
        public string Indicaciones { get; set; }
        public string Veterinario { get; set; }
        public decimal? Peso { get; set; }
        public decimal? Temperatura { get; set; }
        public string Observaciones { get; set; }
        public string? ArchivosAdjuntos { get; set; } = "[]";
    }
}
