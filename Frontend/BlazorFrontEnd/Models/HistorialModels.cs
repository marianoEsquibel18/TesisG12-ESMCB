using System.ComponentModel.DataAnnotations;

namespace BlazorFrontEnd.Models
{
    public class HistorialClinicoDto
    {
        public string Id { get; set; } = string.Empty;
        
        [Required(ErrorMessage = "Requiere seleccionar un paciente")]
        public string PacienteId { get; set; } = string.Empty;
        public string PacienteNombre { get; set; } = string.Empty;
        
        [Required(ErrorMessage = "La fecha es obligatoria")]
        public DateTime? Fecha { get; set; } = DateTime.Today;
        
        [Required(ErrorMessage = "Indique el motivo de la consulta")]
        public string Motivo { get; set; } = string.Empty;
        
        public string Sintomas { get; set; } = string.Empty;
        public string Diagnostico { get; set; } = string.Empty;
        public string Indicaciones { get; set; } = string.Empty;
        
        [Required(ErrorMessage = "Indique el Veterinario tratante")]
        public string Veterinario { get; set; } = string.Empty;
        
        public decimal? Peso { get; set; }
        public decimal? Temperatura { get; set; }
        public string Observaciones { get; set; } = string.Empty;
        public string? ArchivosAdjuntos { get; set; } = "[]";
    }

    public class TratamientoDto
    {
        public string Id { get; set; } = string.Empty;
        
        [Required]
        public string PacienteId { get; set; } = string.Empty;
        public string PacienteNombre { get; set; } = string.Empty;
        
        public DateTime Fecha { get; set; } = DateTime.Today;
        public string Diagnostico { get; set; } = string.Empty;
        
        [Required(ErrorMessage = "La descripción del tratamiento es requerida")]
        public string Descripcion { get; set; } = string.Empty;
        public string Medicacion { get; set; } = string.Empty;
        public string Veterinario { get; set; } = string.Empty;
        public string Observaciones { get; set; } = string.Empty;
        public bool Finalizado { get; set; }
        public string? ArchivosAdjuntos { get; set; } = "[]";
    }

    public class RegistroVacunacionDto
    {
        public string Id { get; set; } = string.Empty;
        
        [Required]
        public string PacienteId { get; set; } = string.Empty;
        public string PacienteNombre { get; set; } = string.Empty;
        
        public int VacunaId { get; set; }
        public string? ProductoId { get; set; }
        public int? DepositoId { get; set; }
        public string VacunaNombre { get; set; } = string.Empty;
        
        public DateTime FechaAplicacion { get; set; } = DateTime.Today;
        public DateTime? FechaProximaDosis { get; set; }
        public bool ProximaDosisVencida { get; set; }
        
        public string Veterinario { get; set; } = string.Empty;
        public string NroLote { get; set; } = string.Empty;
        public string Observaciones { get; set; } = string.Empty;
    }

    public class VacunaDto
    {
        public int Id { get; set; }
        
        [Required(ErrorMessage = "El nombre de la vacuna es requerido")]
        public string Nombre { get; set; } = string.Empty;
        public string Descripcion { get; set; } = string.Empty;
        public string Laboratorio { get; set; } = string.Empty;
        public int? IntervaloDosisDias { get; set; }
        public bool Activo { get; set; }
    }
}
