using Core.Domain.Entities;
using Domain.Validators;

namespace Domain.Entities
{
    public class Producto : DomainEntity<string, ProductoValidator>
    {
        public string Nombre { get; private set; }
        public string Descripcion { get; private set; }
        public string CodigoBarras { get; private set; }
        public int CategoriaId { get; private set; }
        public int? MarcaId { get; private set; }
        public string ProveedorId { get; private set; }
        public int? DepositoId { get; private set; }
        public decimal PrecioCompra { get; private set; }
        public decimal PrecioVenta { get; private set; }
        public int StockActual { get; private set; }
        public int StockMinimo { get; private set; }
        public bool Activo { get; private set; }

        // Navegación
        public virtual Categoria Categoria { get; private set; }
        public virtual Marca Marca { get; private set; }
        public virtual Proveedor Proveedor { get; private set; }
        public virtual Deposito Deposito { get; private set; }
        public virtual ICollection<ProductoDeposito> StocksDepositos { get; private set; } = new List<ProductoDeposito>();

        public bool StockBajo => StockActual <= StockMinimo;

        protected Producto() { }

        public Producto(string nombre, string descripcion, string codigoBarras,
            int categoriaId, decimal precioCompra, decimal precioVenta,
            int stockActual, int stockMinimo,
            int? marcaId = null, string proveedorId = null, int? depositoId = null) : this()
        {
            Id = Guid.NewGuid().ToString();
            Nombre = nombre;
            Descripcion = descripcion;
            CodigoBarras = codigoBarras;
            CategoriaId = categoriaId;
            MarcaId = marcaId;
            ProveedorId = proveedorId;
            DepositoId = depositoId;
            PrecioCompra = precioCompra;
            PrecioVenta = precioVenta;
            StockActual = stockActual;
            StockMinimo = stockMinimo;
            Activo = true;
        }

        public void Actualizar(string nombre, string descripcion, decimal precioCompra,
            decimal precioVenta, int stockMinimo, int? categoriaId = null, int? marcaId = null,
            string? proveedorId = null, int? depositoId = null, string? codigoBarras = null)
        {
            Nombre = nombre;
            Descripcion = descripcion;
            PrecioCompra = precioCompra;
            PrecioVenta = precioVenta;
            StockMinimo = stockMinimo;
            if (categoriaId.HasValue && categoriaId.Value > 0) CategoriaId = categoriaId.Value;
            MarcaId = marcaId;
            ProveedorId = !string.IsNullOrWhiteSpace(proveedorId) ? proveedorId : null;
            DepositoId = depositoId;
            if (codigoBarras != null) CodigoBarras = codigoBarras;
        }

        public void AgregarStock(int cantidad)
        {
            if (cantidad <= 0) return;
            StockActual += cantidad;
        }

        public bool DescontarStock(int cantidad)
        {
            if (cantidad <= 0 || StockActual < cantidad) return false;
            StockActual -= cantidad;
            return true;
        }

        public void ActualizarPrecios(decimal precioCompra, decimal precioVenta)
        {
            PrecioCompra = precioCompra;
            PrecioVenta = precioVenta;
        }

        public void Desactivar() => Activo = false;
        public void Activar() => Activo = true;

        /// <summary>
        /// Recalcula StockActual sumando el stock de todos los depósitos vinculados
        /// </summary>
        public void SincronizarStockTotal()
        {
            if (StocksDepositos != null && StocksDepositos.Any())
                StockActual = StocksDepositos.Sum(sd => sd.StockActual);
        }

        public void SetStockDirecto(int stock)
        {
            StockActual = stock;
        }
    }
}
