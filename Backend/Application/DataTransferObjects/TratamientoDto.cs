namespace Application.DataTransferObjects
{
    public class TratamientoDto
    {
        public string Id { get; set; }
        public string PacienteId { get; set; }
        public string PacienteNombre { get; set; }
        public DateTime Fecha { get; set; }
        public string Diagnostico { get; set; }
        public string Descripcion { get; set; }
        public string Medicacion { get; set; }
        public string Veterinario { get; set; }
        public string Observaciones { get; set; }
        public bool Finalizado { get; set; }
        public string? ArchivosAdjuntos { get; set; } = "[]";
    }
}
