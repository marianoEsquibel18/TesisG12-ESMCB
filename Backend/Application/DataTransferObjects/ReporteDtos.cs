namespace Application.DataTransferObjects
{
    // ── Dashboard ──
    public class DashboardDto
    {
        public int TotalPacientes { get; set; }
        public int TotalPropietarios { get; set; }
        public int TurnosHoy { get; set; }
        public int TurnosPendientes { get; set; }
        public int VacunasPendientes { get; set; }
        public int ProductosStockBajo { get; set; }
        public decimal VentasHoy { get; set; }
        public int VentasHoyCount { get; set; }
        public decimal VentasMes { get; set; }
        public int VentasMesCount { get; set; }
        public int TratamientosActivos { get; set; }
    }

    // ── Reportes de Ventas ──
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
        public string MetodoPago { get; set; }
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
        public string ProductoId { get; set; }
        public string ProductoNombre { get; set; }
        public int CantidadVendida { get; set; }
        public decimal TotalVendido { get; set; }
    }

    // ── Reportes de Stock ──
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
        public string Id { get; set; }
        public string Nombre { get; set; }
        public int StockActual { get; set; }
        public int StockMinimo { get; set; }
        public string CategoriaNombre { get; set; }
    }

    public class ProductoStockItemDto
    {
        public string Id { get; set; }
        public string Nombre { get; set; }
        public string CodigoBarras { get; set; }
        public string CategoriaNombre { get; set; }
        public int StockActual { get; set; }
        public int StockMinimo { get; set; }
        public decimal PrecioUnitario { get; set; }
        public decimal PrecioVenta { get; set; }
        public decimal ValorTotal { get; set; }
    }

    // ── Reportes de Turnos ──
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
        public string VeterinarioId { get; set; }
        public string VeterinarioNombre { get; set; }
        public int TotalTurnos { get; set; }
        public int Completados { get; set; }
        public int Ausentes { get; set; }
        public int Cancelados { get; set; }
    }

    public class TurnosPorServicioDto
    {
        public int ServicioId { get; set; }
        public string ServicioNombre { get; set; }
        public int CantidadTurnos { get; set; }
    }

    // ── Reportes Clínicos ──
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
        public string EspecieNombre { get; set; }
        public int Cantidad { get; set; }
    }

    // ── Reporte Histórico de Tratamientos (R003) ──
    public class HistoricoTratamientoItemDto
    {
        public string Id { get; set; }
        public DateTime Fecha { get; set; }
        public string PacienteNombre { get; set; }
        public string PropietarioNombre { get; set; }
        public string Veterinario { get; set; }
        public string Diagnostico { get; set; }
        public string Descripcion { get; set; }
        public bool Finalizado { get; set; }
    }
}
