namespace BlazorFrontEnd.Models
{
    public class KpiDashboardDto
    {
        public int TotalPacientes { get; set; }
        public int TotalPropietarios { get; set; }
        public int TotalProductos { get; set; }
        public int ProductosStockBajo { get; set; }
        public int VentasMesActual { get; set; }
        public decimal IngresosMesActual { get; set; }
        public int TurnosHoy { get; set; }
        public int TurnosPendientesHoy { get; set; }
        public int TurnosCompletadosHoy { get; set; }
    }

    public class DashboardAlertasResponse
    {
        public DateTime Timestamp { get; set; }
        public int TotalAlertas { get; set; }
        public int Criticas { get; set; }
        public int Importantes { get; set; }
        public int Informativas { get; set; }
        public int TurnosHoy { get; set; }
        public int TratamientosActivos { get; set; }
        public List<AlertaDto> Alertas { get; set; } = new();
    }

    public class AlertaDto
    {
        public string Nivel { get; set; } = string.Empty; // CRITICA, IMPORTANTE, INFO
        public string Tipo { get; set; } = string.Empty;
        public string Titulo { get; set; } = string.Empty;
        public string Detalle { get; set; } = string.Empty;
        public string EntidadId { get; set; } = string.Empty;
        public DateTime Timestamp { get; set; }
    }

    public class IngresoDiarioDto
    {
        public string Fecha { get; set; } = string.Empty;
        public string FechaCorta { get; set; } = string.Empty;
        public int TotalVentas { get; set; }
        public decimal Ingresos { get; set; }
    }

    public class VacunaPendienteDto
    {
        public string PacienteId { get; set; } = string.Empty;
        public string Paciente { get; set; } = string.Empty;
        public string Propietario { get; set; } = string.Empty;
        public string Telefono { get; set; } = string.Empty;
        public string Vacuna { get; set; } = string.Empty;
        public DateTime FechaProximaDosis { get; set; }
        public int DiasRestantes { get; set; }
        public string Detalle { get; set; } = string.Empty;
    }

    public class VacunasPendientesResponse
    {
        public int Total { get; set; }
        public int DiasAntelacion { get; set; }
        public List<VacunaPendienteDto> Items { get; set; } = new();
    }

    public class DistribucionEspecieItemDto
    {
        public string Especie { get; set; } = string.Empty;
        public int Cantidad { get; set; }
    }

    public class PacientesPorEspecieDashboardResponse
    {
        public int Total { get; set; }
        public List<DistribucionEspecieItemDto> Distribucion { get; set; } = new();
    }
}
