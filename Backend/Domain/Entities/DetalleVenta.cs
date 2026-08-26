using Core.Domain.Entities;
using Domain.Validators;

namespace Domain.Entities
{
    /// <summary>
    /// Detalle de un ítem dentro de una venta
    /// </summary>
    public class DetalleVenta : DomainEntity<string, DetalleVentaValidator>
    {
        public string VentaId { get; private set; }
        public string? ProductoId { get; private set; }
        public string Descripcion { get; private set; }
        public int Cantidad { get; private set; }
        public decimal PrecioUnitario { get; private set; }
        public decimal Subtotal => Cantidad * PrecioUnitario;

        /// <summary>
        /// Depósito del cual se descontó el stock (nullable para compatibilidad con ventas viejas o servicios)
        /// </summary>
        public int? DepositoId { get; private set; }

        // Navegación
        public virtual Producto? Producto { get; private set; }

        protected DetalleVenta() { }

        public DetalleVenta(string ventaId, string? productoId, string descripcion,
            int cantidad, decimal precioUnitario, int? depositoId = null) : this()
        {
            Id = Guid.NewGuid().ToString();
            VentaId = ventaId;
            ProductoId = productoId;
            Descripcion = descripcion;
            Cantidad = cantidad;
            PrecioUnitario = precioUnitario;
            DepositoId = depositoId;
        }
    }
}
