using Core.Domain.Entities;
using Domain.Validators;

namespace Domain.Entities
{
    /// <summary>
    /// Tabla intermedia que representa el stock de un producto en un depósito específico
    /// </summary>
    public class ProductoDeposito : DomainEntity<string, ProductoDepositoValidator>
    {
        public string ProductoId { get; private set; }
        public int DepositoId { get; private set; }
        public int StockActual { get; private set; }
        public int StockMinimo { get; private set; }

        // Navegación
        public virtual Producto Producto { get; private set; }
        public virtual Deposito Deposito { get; private set; }

        public bool StockBajo => StockActual <= StockMinimo;

        protected ProductoDeposito() { }

        public ProductoDeposito(string productoId, int depositoId, int stockActual = 0, int stockMinimo = 0) : this()
        {
            Id = Guid.NewGuid().ToString();
            ProductoId = productoId;
            DepositoId = depositoId;
            StockActual = stockActual;
            StockMinimo = stockMinimo;
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

        public void ActualizarStockMinimo(int stockMinimo)
        {
            StockMinimo = stockMinimo;
        }

        public void AsignarDeposito(Deposito deposito)
        {
            Deposito = deposito;
            if (deposito != null) DepositoId = deposito.Id;
        }
    }
}
