using System.ComponentModel.DataAnnotations;

namespace BlazorFrontEnd.Models
{
    public class TurnoDto
    {
        public string Id { get; set; } = string.Empty;
        
        [Required(ErrorMessage = "Debe seleccionar un paciente")]
        public string PacienteId { get; set; } = string.Empty;
        public string PacienteNombre { get; set; } = string.Empty;
        
        [Required(ErrorMessage = "Debe seleccionar un veterinario")]
        public string VeterinarioId { get; set; } = string.Empty;
        public string VeterinarioNombre { get; set; } = string.Empty;
        public string PropietarioNombre { get; set; } = string.Empty;
        
        [Required(ErrorMessage = "Debe seleccionar un servicio")]
        public int ServicioId { get; set; }
        public string ServicioNombre { get; set; } = string.Empty;
        
        [Required(ErrorMessage = "Debe ingresar una fecha para el turno")]
        public DateTime FechaHora { get; set; } = DateTime.Today.AddHours(9);
        public DateTime FechaHoraFin { get; set; }
        public int DuracionMinutos { get; set; }
        
        public string Estado { get; set; } = "Programado"; // Programado, Completado, Cancelado, Reprogramado
        
        [Required(ErrorMessage = "El motivo es requerido")]
        public string Motivo { get; set; } = string.Empty;
        public string Observaciones { get; set; } = string.Empty;
        public DateTime FechaCreacion { get; set; }
        public string? ArchivosAdjuntos { get; set; } = "[]";
    }

    public class TipoHorarioDto
    {
        public int Id { get; set; }
        public string Nombre { get; set; } = string.Empty;
        public string Descripcion { get; set; } = string.Empty;
        public bool Activo { get; set; }
    }

    public class HorarioDto
    {
        public string Id { get; set; } = string.Empty;
        public string VeterinarioId { get; set; } = string.Empty;
        public int DiaSemana { get; set; } // 1=Lunes..7=Domingo
        public string DiaSemanaNombre => DiaSemana switch
        {
            1 => "Lunes",
            2 => "Martes",
            3 => "Miércoles",
            4 => "Jueves",
            5 => "Viernes",
            6 => "Sábado",
            7 => "Domingo",
            _ => "Desconocido"
        };
        public string HoraInicio { get; set; } = "08:00";
        public string HoraFin { get; set; } = "16:00";
        public int TipoHorarioId { get; set; } = 1; // 1=Normal, 2=Guardia
        public string TipoHorarioNombre { get; set; } = "Normal";
        public bool Activo { get; set; } = true;
    }

    public class VeterinarioDto
    {
        public string Id { get; set; } = string.Empty;
        public string Nombre { get; set; } = string.Empty;
        public string Apellido { get; set; } = string.Empty;
        public string NombreCompleto { get; set; } = string.Empty;
        public string Matricula { get; set; } = string.Empty;
        public string Telefono { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Especialidad { get; set; } = string.Empty;
        public bool Activo { get; set; }
        public int? SucursalId { get; set; }
        public string? SucursalNombre { get; set; }
        public List<HorarioDto> Horarios { get; set; } = new();
        public string DisponibilidadActual { get; set; } = "No Disponible";
    }

    public class ServicioDto
    {
        public int Id { get; set; }
        
        [Required(ErrorMessage = "El nombre del servicio es requerido")]
        public string Nombre { get; set; } = string.Empty;
        
        public string Descripcion { get; set; } = string.Empty;
        public int DuracionMinutos { get; set; } = 30;
        public decimal Precio { get; set; }
        public string ProductosUtilizados { get; set; } = string.Empty;
        public bool Activo { get; set; } = true;
    }

    public class Adjunto
    {
        public string Nombre { get; set; } = string.Empty;
        public string Base64 { get; set; } = string.Empty;
        public string ContentType { get; set; } = string.Empty;
    }
}
