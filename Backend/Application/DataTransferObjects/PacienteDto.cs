namespace Application.DataTransferObjects
{
    public class PacienteDto
    {
        public string Id { get; set; }
        public string Nombre { get; set; }
        public int EspecieId { get; set; }
        public string EspecieNombre { get; set; }
        public int? RazaId { get; set; }
        public string RazaNombre { get; set; }
        public string Sexo { get; set; }
        public DateTime? FechaNacimiento { get; set; }
        public int? EdadEnAnios { get; set; }
        public string PropietarioId { get; set; }
        public string PropietarioNombre { get; set; }
        public string FotoUrl { get; set; }
        public string Observaciones { get; set; }
        public DateTime FechaRegistro { get; set; }
        public bool Activo { get; set; }
        public int ContadorInasistencias { get; set; }
    }
}
