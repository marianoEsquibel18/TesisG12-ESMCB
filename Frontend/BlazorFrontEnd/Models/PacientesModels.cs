using System.ComponentModel.DataAnnotations;

namespace BlazorFrontEnd.Models
{
    public class PropietarioDto
    {
        public string Id { get; set; } = string.Empty;
        
        [Required(ErrorMessage = "El nombre es requerido")]
        public string Nombre { get; set; } = string.Empty;
        
        [Required(ErrorMessage = "El apellido es requerido")]
        public string Apellido { get; set; } = string.Empty;
        
        public string NombreCompleto { get; set; } = string.Empty;
        
        [Required(ErrorMessage = "El DNI es requerido")]
        public string DNI { get; set; } = string.Empty;
        
        [Required(ErrorMessage = "El número de celular / teléfono es requerido")]
        public string Telefono { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Direccion { get; set; } = string.Empty;
        public DateTime FechaRegistro { get; set; }
        public bool Activo { get; set; }
        public int CantidadMascotas { get; set; }
    }

    public class PacienteDto
    {
        public string Id { get; set; } = string.Empty;
        
        [Required(ErrorMessage = "El nombre es requerido")]
        public string Nombre { get; set; } = string.Empty;
        
        [Required(ErrorMessage = "La especie debe ser seleccionada")]
        public int EspecieId { get; set; }
        public string EspecieNombre { get; set; } = string.Empty;
        
        [Required(ErrorMessage = "La raza debe ser seleccionada")]
        public int? RazaId { get; set; }
        public string RazaNombre { get; set; } = string.Empty;
        
        [Required(ErrorMessage = "Especifique el sexo")]
        public string Sexo { get; set; } = string.Empty;
        
        [Required(ErrorMessage = "Debe ingresar una fecha de nacimiento")]
        public DateTime? FechaNacimiento { get; set; }
        public int? EdadEnAnios { get; set; }
        
        [Required(ErrorMessage = "Debe asignar un propietario")]
        public string PropietarioId { get; set; } = string.Empty;
        public string PropietarioNombre { get; set; } = string.Empty;
        
        public string FotoUrl { get; set; } = string.Empty;
        public string Observaciones { get; set; } = string.Empty;
        public DateTime FechaRegistro { get; set; }
        public bool Activo { get; set; }
        public int ContadorInasistencias { get; set; }
    }

    public class EspecieDto
    {
        public int Id { get; set; }
        public string Nombre { get; set; } = string.Empty;
        public string Descripcion { get; set; } = string.Empty;
        public bool Activo { get; set; }
    }

    public class RazaDto
    {
        public int Id { get; set; }
        public string Nombre { get; set; } = string.Empty;
        public string Descripcion { get; set; } = string.Empty;
        public int EspecieId { get; set; }
        public string EspecieNombre { get; set; } = string.Empty;
        public bool Activo { get; set; }
    }
}
