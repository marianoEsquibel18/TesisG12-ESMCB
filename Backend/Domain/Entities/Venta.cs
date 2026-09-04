using Core.Domain.Entities;
using Domain.Validators;

namespace Domain.Entities
{
    /// <summary>
    /// Representa una venta realizada en la clínica
    /// </summary>
    public class Venta : DomainEntity<string, VentaValidator>
    {
        public DateTime Fecha { get; private set; }
        public string? PropietarioId { get; private set; }
        public int MetodoPagoId { get; private set; }
        public decimal Total { get; private set; }
        public EstadoVenta Estado { get; private set; }
        public string Observaciones { get; private set; }

        public int SucursalId { get; private set; }

        // Navegación
        public virtual Propietario Propietario { get; private set; }
        public virtual MetodoPago MetodoPago { get; private set; }
        public virtual Sucursal Sucursal { get; private set; }
        public virtual ICollection<DetalleVenta> Detalles { get; private set; }

        protected Venta()
        {
            Detalles = new List<DetalleVenta>();
        }

        public Venta(string? propietarioId, int metodoPagoId, string observaciones = "", int sucursalId = 0, DateTime? fecha = null) : this()
        {
            Id = Guid.NewGuid().ToString();
            Fecha = fecha ?? DateTime.Now;
            PropietarioId = propietarioId;
            MetodoPagoId = metodoPagoId;
            Total = 0;
            Estado = EstadoVenta.Pendiente;
            SucursalId = sucursalId;
            Observaciones = observaciones;
        }

        public void EstablecerFecha(DateTime fecha) => Fecha = fecha;

        public void AsignarSucursal(int sucursalId) => SucursalId = sucursalId;

        public void AgregarDetalle(DetalleVenta detalle)
        {
            Detalles.Add(detalle);
            RecalcularTotal();
        }

        public void RecalcularTotal()
        {
            Total = Detalles.Sum(d => d.Subtotal);
        }

        public void ActualizarTotal(decimal total)
        {
            Total = total;
        }

        public void Confirmar()
        {
            Estado = EstadoVenta.Confirmada;
        }

        public void Anular(string motivo = "")
        {
            Estado = EstadoVenta.Anulada;
            if (!string.IsNullOrWhiteSpace(motivo))
                Observaciones = $"Anulada: {motivo}";
        }
    }

    public enum EstadoVenta
    {
        Pendiente = 0,
        Confirmada = 1,
        Anulada = 2
    }
}
