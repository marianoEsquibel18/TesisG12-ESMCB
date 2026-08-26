namespace Application.DataTransferObjects
{
    public class RegistroVacunacionDto
    {
        public string Id { get; set; }
        public string PacienteId { get; set; }
        public string PacienteNombre { get; set; }
        public int VacunaId { get; set; }
        public string ProductoId { get; set; }
        public int? DepositoId { get; set; }
        public string VacunaNombre { get; set; }
        public DateTime FechaAplicacion { get; set; }
        public DateTime? FechaProximaDosis { get; set; }
        public bool ProximaDosisVencida { get; set; }
        public string Veterinario { get; set; }
        public string NroLote { get; set; }
        public string Observaciones { get; set; }
    }
}
