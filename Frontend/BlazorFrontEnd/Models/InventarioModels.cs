using System.ComponentModel.DataAnnotations;

namespace BlazorFrontEnd.Models
{
    public class ProductoDto
    {
        public string Id { get; set; } = string.Empty;
        
        [Required(ErrorMessage = "El nombre del producto es requerido")]
        public string Nombre { get; set; } = string.Empty;
        public string Descripcion { get; set; } = string.Empty;
        
        [Required(ErrorMessage = "El código de barras es requerido")]
        public string CodigoBarras { get; set; } = string.Empty;
        
        [Required(ErrorMessage = "Debe asignar una categoría")]
        public int CategoriaId { get; set; }
        public string CategoriaNombre { get; set; } = string.Empty;
        
        public int? MarcaId { get; set; }
        public string MarcaNombre { get; set; } = string.Empty;
        
        public string ProveedorId { get; set; } = string.Empty;
        public string ProveedorNombre { get; set; } = string.Empty;
        
        public int? DepositoId { get; set; }
        public string DepositoNombre { get; set; } = string.Empty;
        
        [Required]
        [Range(0, 9999999.99, ErrorMessage = "El precio de compra debe ser válido")]
        public decimal PrecioCompra { get; set; }
        
        [Required]
        [Range(0, 9999999.99, ErrorMessage = "El precio de venta debe ser válido")]
        public decimal PrecioVenta { get; set; }
        
        [Required]
        public int StockActual { get; set; }
        
        [Required]
        public int StockMinimo { get; set; }
        
        public bool StockBajo { get; set; }
        public bool Activo { get; set; }
        public List<ProductoDepositoStockDto> StocksDepositos { get; set; } = new();
    }

    public class CategoriaDto
    {
        public int Id { get; set; }
        public string Nombre { get; set; } = string.Empty;
        public string Descripcion { get; set; } = string.Empty;
        public bool Activo { get; set; }
    }

    public class MarcaDto
    {
        public int Id { get; set; }
        public string Nombre { get; set; } = string.Empty;
        public string Descripcion { get; set; } = string.Empty;
        public bool Activo { get; set; }
    }

    public class ProveedorDto
    {
        public string Id { get; set; } = string.Empty;
        public string RazonSocial { get; set; } = string.Empty;
        public string Cuit { get; set; } = string.Empty;
        public string Telefono { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Direccion { get; set; } = string.Empty;
        public string Contacto { get; set; } = string.Empty;
        public bool Activo { get; set; }
    }

    public class DepositoDto
    {
        public int Id { get; set; }
        public string Nombre { get; set; } = string.Empty;
        public string Ubicacion { get; set; } = string.Empty;
        public bool Activo { get; set; }
        public int? SucursalId { get; set; }
        public string? SucursalNombre { get; set; }
    }

    public class MovimientoStockRequest
    {
        [Required]
        [Range(1, 100000, ErrorMessage = "La cantidad debe ser mayor a 0")]
        public int Cantidad { get; set; }
        
        public string Motivo { get; set; } = string.Empty;
        public string Referencia { get; set; } = string.Empty;
        public int? DepositoId { get; set; }
    }

    public class ProductoDepositoStockDto
    {
        public string Id { get; set; } = string.Empty;
        public string ProductoId { get; set; } = string.Empty;
        public int DepositoId { get; set; }
        public string DepositoNombre { get; set; } = string.Empty;
        public int StockActual { get; set; }
        public int StockMinimo { get; set; }
        public bool StockBajo { get; set; }
    }
}
