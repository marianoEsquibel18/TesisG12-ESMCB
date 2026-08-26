namespace BlazorFrontEnd.Models
{
    // ════════════════════════════════════
    // REPORTE DE VENTAS
    // ════════════════════════════════════
    public class ReporteVentasDto
    {
        public DateTime Desde { get; set; }
        public DateTime Hasta { get; set; }
        public int CantidadVentas { get; set; }
        public decimal TotalVentas { get; set; }
        public decimal PromedioVenta { get; set; }
        public List<VentaPorMetodoPagoDto> VentasPorMetodoPago { get; set; } = new();
        public List<VentaPorDiaDto> VentasPorDia { get; set; } = new();
        public List<ProductoMasVendidoDto> ProductosMasVendidos { get; set; } = new();
    }

    public class VentaPorMetodoPagoDto
    {
        public string MetodoPago { get; set; } = string.Empty;
        public int Cantidad { get; set; }
        public decimal Total { get; set; }
    }

    public class VentaPorDiaDto
    {
        public DateTime Fecha { get; set; }
        public int Cantidad { get; set; }
        public decimal Total { get; set; }
    }

    public class ProductoMasVendidoDto
    {
        public string ProductoId { get; set; } = string.Empty;
        public string ProductoNombre { get; set; } = string.Empty;
        public int CantidadVendida { get; set; }
        public decimal TotalVendido { get; set; }
    }

    // ════════════════════════════════════
    // REPORTE DE STOCK
    // ════════════════════════════════════
    public class ReporteStockDto
    {
        public int TotalProductos { get; set; }
        public int ProductosActivos { get; set; }
        public int ProductosStockBajo { get; set; }
        public int ProductosSinStock { get; set; }
        public decimal ValorTotalStock { get; set; }
        public List<ProductoStockBajoDto> ListaStockBajo { get; set; } = new();
        public List<ProductoStockItemDto> ListaTodosProductos { get; set; } = new();
    }

    public class ProductoStockBajoDto
    {
        public string Id { get; set; } = string.Empty;
        public string Nombre { get; set; } = string.Empty;
        public int StockActual { get; set; }
        public int StockMinimo { get; set; }
        public string CategoriaNombre { get; set; } = string.Empty;
    }

    public class ProductoStockItemDto
    {
        public string Id { get; set; } = string.Empty;
        public string Nombre { get; set; } = string.Empty;
        public string CodigoBarras { get; set; } = string.Empty;
        public string CategoriaNombre { get; set; } = string.Empty;
        public int StockActual { get; set; }
        public int StockMinimo { get; set; }
        public decimal PrecioUnitario { get; set; }
        public decimal PrecioVenta { get; set; }
        public decimal ValorTotal { get; set; }
    }

    // ════════════════════════════════════
    // REPORTE DE TURNOS
    // ════════════════════════════════════
    public class ReporteTurnosDto
    {
        public DateTime Desde { get; set; }
        public DateTime Hasta { get; set; }
        public int TotalTurnos { get; set; }
        public int Completados { get; set; }
        public int Cancelados { get; set; }
        public int Ausentes { get; set; }
        public decimal TasaCumplimiento { get; set; }
        public List<TurnosPorVeterinarioDto> TurnosPorVeterinario { get; set; } = new();
        public List<TurnosPorServicioDto> TurnosPorServicio { get; set; } = new();
        public List<TurnoPorDiaDto> TurnosPorDia { get; set; } = new();
    }

    public class TurnoPorDiaDto
    {
        public DateTime Fecha { get; set; }
        public int Total { get; set; }
        public int Programados { get; set; }
        public int Completados { get; set; }
        public int Ausentes { get; set; }
        public int Cancelados { get; set; }
    }

    public class TurnosPorVeterinarioDto
    {
        public string VeterinarioId { get; set; } = string.Empty;
        public string VeterinarioNombre { get; set; } = string.Empty;
        public int TotalTurnos { get; set; }
        public int Completados { get; set; }
        public int Ausentes { get; set; }
        public int Cancelados { get; set; }
    }

    public class TurnosPorServicioDto
    {
        public int ServicioId { get; set; }
        public string ServicioNombre { get; set; } = string.Empty;
        public int CantidadTurnos { get; set; }
    }

    // ════════════════════════════════════
    // REPORTE CLÍNICO
    // ════════════════════════════════════
    public class ReporteClinicDto
    {
        public int TotalPacientes { get; set; }
        public List<PacientesPorEspecieDto> PacientesPorEspecie { get; set; } = new();
        public int VacunasAplicadasMes { get; set; }
        public int TratamientosActivosCount { get; set; }
    }

    public class PacientesPorEspecieDto
    {
        public int EspecieId { get; set; }
        public string EspecieNombre { get; set; } = string.Empty;
        public int Cantidad { get; set; }
    }

    // ════════════════════════════════════
    // REPORTE HISTÓRICO DE TRATAMIENTOS (R003)
    // ════════════════════════════════════
    public class HistoricoTratamientoItemDto
    {
        public string Id { get; set; } = string.Empty;
        public DateTime Fecha { get; set; }
        public string PacienteNombre { get; set; } = string.Empty;
        public string PropietarioNombre { get; set; } = string.Empty;
        public string Veterinario { get; set; } = string.Empty;
        public string Diagnostico { get; set; } = string.Empty;
        public string Descripcion { get; set; } = string.Empty;
        public bool Finalizado { get; set; }
    }
}
