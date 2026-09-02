using Domain.Entities;

namespace Application.DataTransferObjects
{
    public class CategoriaDto
    {
        public int Id { get; set; }
        public string Nombre { get; set; }
        public string Descripcion { get; set; }
        public bool Activo { get; set; }
    }

    public class MarcaDto
    {
        public int Id { get; set; }
        public string Nombre { get; set; }
        public string Descripcion { get; set; }
        public bool Activo { get; set; }
    }

    public class ProveedorDto
    {
        public string Id { get; set; }
        public string RazonSocial { get; set; }
        public string CUIT { get; set; }
        public string Telefono { get; set; }
        public string Email { get; set; }
        public string Direccion { get; set; }
        public string Contacto { get; set; }
        public bool Activo { get; set; }
    }

    public class DepositoDto
    {
        public int Id { get; set; }
        public string Nombre { get; set; }
        public string Ubicacion { get; set; }
        public bool Activo { get; set; }
        public int? SucursalId { get; set; }
        public string? SucursalNombre { get; set; }
    }

    public class ProductoDto
    {
        public string Id { get; set; }
        public string Nombre { get; set; }
        public string Descripcion { get; set; }
        public string CodigoBarras { get; set; }
        public int CategoriaId { get; set; }
        public string CategoriaNombre { get; set; }
        public int? MarcaId { get; set; }
        public string MarcaNombre { get; set; }
        public string ProveedorId { get; set; }
        public string ProveedorNombre { get; set; }
        public int? DepositoId { get; set; }
        public string DepositoNombre { get; set; }
        public decimal PrecioCompra { get; set; }
        public decimal PrecioVenta { get; set; }
        public int StockActual { get; set; }
        public int StockMinimo { get; set; }
        public bool StockBajo { get; set; }
        public bool Activo { get; set; }
        public List<ProductoDepositoStockDto> StocksDepositos { get; set; } = new();
    }

    public class ProductoDepositoStockDto
    {
        public string Id { get; set; }
        public string ProductoId { get; set; }
        public int DepositoId { get; set; }
        public string DepositoNombre { get; set; }
        public int StockActual { get; set; }
        public int StockMinimo { get; set; }
        public bool StockBajo { get; set; }
    }

    public class MovimientoStockDto
    {
        public string Id { get; set; }
        public string ProductoId { get; set; }
        public string ProductoNombre { get; set; }
        public string Tipo { get; set; }
        public int Cantidad { get; set; }
        public DateTime Fecha { get; set; }
        public string Motivo { get; set; }
        public string Referencia { get; set; }
    }
}
