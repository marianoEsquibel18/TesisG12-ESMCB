using Core.Domain.Entities;
using Domain.Validators;

namespace Domain.Entities
{
    /// <summary>
    /// Representa un servicio ofrecido por la clínica (Consulta, Cirugía, Limpieza dental, etc.)
    /// </summary>
    public class Servicio : DomainEntity<int, ServicioValidator>
    {
        public string Nombre { get; private set; }
        public string Descripcion { get; private set; }
        public int DuracionMinutos { get; private set; }
        public decimal Precio { get; private set; }
        public string ProductosUtilizados { get; private set; }
        public bool Activo { get; private set; }

        protected Servicio() { }

        public Servicio(string nombre, string descripcion, int duracionMinutos, decimal precio, string productosUtilizados = "") : this()
        {
            Nombre = nombre;
            Descripcion = descripcion ?? "";
            DuracionMinutos = duracionMinutos > 0 ? duracionMinutos : 30;
            Precio = precio;
            ProductosUtilizados = productosUtilizados ?? "";
            Activo = true;
        }

        public void Actualizar(string nombre, string descripcion, int duracionMinutos, decimal precio, string productosUtilizados = "")
        {
            Nombre = nombre;
            Descripcion = descripcion ?? "";
            DuracionMinutos = duracionMinutos > 0 ? duracionMinutos : 30;
            Precio = precio;
            ProductosUtilizados = productosUtilizados ?? "";
        }

        public void Desactivar() => Activo = false;
        public void Activar() => Activo = true;
    }
}
