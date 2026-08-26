namespace Application.DataTransferObjects
{
    public class ServicioDto
    {
        public int Id { get; set; }
        public string Nombre { get; set; }
        public string Descripcion { get; set; }
        public int DuracionMinutos { get; set; }
        public decimal Precio { get; set; }
        public string ProductosUtilizados { get; set; }
        public bool Activo { get; set; }
    }
}
