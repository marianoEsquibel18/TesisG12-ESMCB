using System;
using System.Collections.Generic;

namespace BlazorFrontEnd.Models
{
    public class IaStatusDto
    {
        public bool Configurado { get; set; }
        public string Proveedor { get; set; } = "Google Gemini";
        public string Modelo { get; set; } = "gemini-1.5-flash";
        public string Mensaje { get; set; } = string.Empty;
    }

    public class ChatMensajeDto
    {
        public string Role { get; set; } = "user"; // "user" o "model"
        public string Content { get; set; } = string.Empty;
        public DateTime Timestamp { get; set; } = DateTime.Now;
        public TurnoPropuestoDto? TurnoPropuesto { get; set; }
    }

    public class TurnoPropuestoDto
    {
        public string? TurnoId { get; set; }
        public string PacienteId { get; set; } = string.Empty;
        public string PacienteNombre { get; set; } = string.Empty;
        public string VeterinarioId { get; set; } = string.Empty;
        public string VeterinarioNombre { get; set; } = string.Empty;
        public int? ServicioId { get; set; }
        public string ServicioNombre { get; set; } = string.Empty;
        public int? SucursalId { get; set; }
        public DateTime FechaHora { get; set; }
        public int DuracionMinutos { get; set; } = 30;
        public string Motivo { get; set; } = string.Empty;
        public string Observaciones { get; set; } = string.Empty;
        public bool ListoParaConfirmar { get; set; }
        public string? MensajeValidacion { get; set; }
    }

    public class ChatbotRequestDto
    {
        public string Mensaje { get; set; } = string.Empty;
        public List<ChatMensajeDto> Historial { get; set; } = new();
        public int? SucursalId { get; set; }
        public string? UsuarioRol { get; set; }
    }

    public class ChatbotResponseDto
    {
        public bool Exito { get; set; } = true;
        public string Respuesta { get; set; } = string.Empty;
        public string TipoRespuesta { get; set; } = "texto"; // "texto", "propuesta_turno", "turno_confirmado", "guia_agendamiento"
        public TurnoPropuestoDto? TurnoPropuesto { get; set; }
        public List<string> OpcionesSugeridas { get; set; } = new();
        public string? ErrorMensaje { get; set; }
    }

    public class ResumenHistoriaClinicaDto
    {
        public string PacienteId { get; set; } = string.Empty;
        public string PacienteNombre { get; set; } = string.Empty;
        public string InformacionBasica { get; set; } = string.Empty;
        public string UltimaConsulta { get; set; } = string.Empty;
        public string TratamientosYVacunas { get; set; } = string.Empty;
        public string AlertasYRecomendaciones { get; set; } = string.Empty;
        public string ResumenCompletoMarkdown { get; set; } = string.Empty;
        public DateTime FechaGeneracion { get; set; } = DateTime.Now;
        public bool GeneradoPorIa { get; set; } = true;
        public string ModeloUtilizado { get; set; } = "gemini-1.5-flash";
    }

    public class ResumenReporteRequestDto
    {
        public string TipoReporte { get; set; } = string.Empty; // "FINANZAS", "STOCK", "TURNOS", "CLINICA"
        public string DatosJson { get; set; } = string.Empty;
    }

    public class ResumenReporteResponseDto
    {
        public bool Exito { get; set; } = true;
        public string TipoReporte { get; set; } = string.Empty;
        public string Titulo { get; set; } = string.Empty;
        public string ResumenEjecutivo { get; set; } = string.Empty;
        public List<string> PuntosClave { get; set; } = new();
        public List<string> Recomendaciones { get; set; } = new();
        public string TextoParaVoz { get; set; } = string.Empty;
        public string ModeloUtilizado { get; set; } = "gemini-1.5-flash";
        public string? ErrorMensaje { get; set; }
    }
}
