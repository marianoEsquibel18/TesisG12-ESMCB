using System.ComponentModel.DataAnnotations;

namespace BlazorFrontEnd.Models
{
    public class MetodoPagoDto
    {
        public int Id { get; set; }
        
        [Required(ErrorMessage = "El nombre del método de pago es requerido")]
        public string Nombre { get; set; } = string.Empty;
        public bool Activo { get; set; }
    }

    public class VentaDto
    {
        public string Id { get; set; } = string.Empty;
        public DateTime Fecha { get; set; }
        public string PropietarioId { get; set; } = string.Empty;
        public string PropietarioNombre { get; set; } = string.Empty;
        public int MetodoPagoId { get; set; }
        public string MetodoPagoNombre { get; set; } = string.Empty;
        public decimal Total { get; set; }
        public string Estado { get; set; } = string.Empty;
        public string Observaciones { get; set; } = string.Empty;
        public List<DetalleVentaDto> Detalles { get; set; } = new();
    }

    public class DetalleVentaDto
    {
        public string Id { get; set; } = string.Empty;
        public string ProductoId { get; set; } = string.Empty;
        public string ProductoNombre { get; set; } = string.Empty;
        public string Descripcion { get; set; } = string.Empty;
        public int Cantidad { get; set; }
        public decimal PrecioUnitario { get; set; }
        public decimal Subtotal { get; set; }
    }

    public class FacturaDto
    {
        public string Id { get; set; } = string.Empty;
        public string VentaId { get; set; } = string.Empty;
        public string Numero { get; set; } = string.Empty;
        public string TipoFactura { get; set; } = string.Empty;
        public DateTime FechaEmision { get; set; }
        public decimal SubTotal { get; set; }
        public decimal IVA { get; set; }
        public decimal Total { get; set; }
    }

    // Requests para creación adaptados del Backend
    public class CreateVentaRequest
    {
        public string PropietarioId { get; set; } = string.Empty;
        
        [Required(ErrorMessage = "Debe especificar un método de pago")]
        [Range(1, int.MaxValue, ErrorMessage = "Debe elegir un método de pago válido")]
        public int MetodoPagoId { get; set; }
        public string Observaciones { get; set; } = string.Empty;
        public List<CreateDetalleVentaRequest> Detalles { get; set; } = new();
    }

    public class CreateDetalleVentaRequest
    {
        public string? ProductoId { get; set; }
        public string Descripcion { get; set; } = string.Empty;
        public int Cantidad { get; set; }
        public decimal PrecioUnitario { get; set; }
        public int? DepositoId { get; set; }
        public bool EsServicio { get; set; }
    }

    public class FacturarRequest
    {
        [Required]
        public string Numero { get; set; } = string.Empty;
        [Required]
        public string TipoFactura { get; set; } = "C";
        public decimal PorcentajeIVA { get; set; } = 21;
    }
}
