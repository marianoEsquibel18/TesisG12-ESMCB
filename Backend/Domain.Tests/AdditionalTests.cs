using Domain.Entities;

namespace Domain.Tests
{
    public class VeterinarioTests
    {
        [Fact]
        public void Crear_Veterinario_DatosValidos()
        {
            var vet = new Veterinario("Juan", "Pérez", "MP-001", "1155001001");
            Assert.Equal("Juan", vet.Nombre);
            Assert.Equal("Pérez", vet.Apellido);
            Assert.Equal("MP-001", vet.Matricula);
            Assert.Equal("1155001001", vet.Telefono);
        }

        [Fact]
        public void Crear_Veterinario_ConEspecialidad()
        {
            var vet = new Veterinario("Ana", "López", "MP-002", "1155002002", "ana@vet.com", "Cirugía");
            Assert.Equal("Cirugía", vet.Especialidad);
            Assert.Equal("ana@vet.com", vet.Email);
        }

        [Fact]
        public void NombreCompleto_FormateoCorrecto()
        {
            var vet = new Veterinario("Carlos", "Gómez", "MP-003", "1155003003");
            Assert.Equal("Carlos Gómez", vet.NombreCompleto);
        }
    }

    public class ServicioTests
    {
        [Fact]
        public void Crear_Servicio_DatosValidos()
        {
            var srv = new Servicio("Consulta", "Revisión general", 30, 5000m);
            Assert.Equal("Consulta", srv.Nombre);
            Assert.Equal(30, srv.DuracionMinutos);
            Assert.Equal(5000m, srv.Precio);
        }

        [Fact]
        public void Precio_NoNegativo()
        {
            var srv = new Servicio("Test", "Test", 15, 0m);
            Assert.True(srv.Precio >= 0);
        }
    }

    public class TurnoTests
    {
        [Fact]
        public void Crear_Turno_EstadoInicial_Programado()
        {
            var turno = new Turno("pac1", "vet1", 1, DateTime.Today.AddDays(1).AddHours(10), 30);
            Assert.Equal(EstadoTurno.Programado, turno.Estado);
        }

        [Fact]
        public void Completar_Turno_CambiaEstado()
        {
            var turno = new Turno("pac1", "vet1", 1, DateTime.Today.AddDays(1).AddHours(10), 30);
            turno.Completar("Buen estado");
            Assert.Equal(EstadoTurno.Completado, turno.Estado);
        }

        [Fact]
        public void Cancelar_Turno_CambiaEstado()
        {
            var turno = new Turno("pac1", "vet1", 1, DateTime.Today.AddDays(1).AddHours(10), 30);
            turno.Cancelar("No disponible");
            Assert.Equal(EstadoTurno.Cancelado, turno.Estado);
        }

        [Fact]
        public void FechaHoraFin_CalculoCorrecta()
        {
            var fecha = DateTime.Today.AddDays(1).AddHours(10);
            var turno = new Turno("pac1", "vet1", 1, fecha, 45);
            Assert.Equal(fecha.AddMinutes(45), turno.FechaHoraFin);
        }
    }

    public class ProductoTests
    {
        [Fact]
        public void Crear_Producto_DatosValidos()
        {
            var prod = new Producto("TestProd", "Desc", "123456", 1, 100m, 200m, 50, 10);
            Assert.Equal("TestProd", prod.Nombre);
            Assert.Equal(100m, prod.PrecioCompra);
            Assert.Equal(200m, prod.PrecioVenta);
            Assert.Equal(50, prod.StockActual);
            Assert.Equal(10, prod.StockMinimo);
            Assert.True(prod.Activo);
        }

        [Fact]
        public void PrecioVenta_MayorQue_PrecioCompra()
        {
            var prod = new Producto("Test", "Test", "000", 1, 100m, 200m, 10, 5);
            Assert.True(prod.PrecioVenta >= prod.PrecioCompra);
        }

        [Fact]
        public void StockBajo_Cuando_StockActual_MenorQue_Minimo()
        {
            var prod = new Producto("Test", "Test", "000", 1, 100m, 200m, 3, 10);
            Assert.True(prod.StockActual <= prod.StockMinimo);
        }
    }

    public class HistorialClinicoTests
    {
        [Fact]
        public void Crear_HistorialClinico_DatosValidos()
        {
            var hc = new HistorialClinico("pac1", DateTime.Today, "Control", "Dr. Test");
            Assert.Equal("pac1", hc.PacienteId);
            Assert.Equal("Control", hc.Motivo);
            Assert.Equal("Dr. Test", hc.Veterinario);
        }

        [Fact]
        public void Crear_HistorialClinico_ConDatosCompletos()
        {
            var hc = new HistorialClinico("pac1", DateTime.Today, "Consulta", "Dr. Test",
                "Fiebre", "Gripe", "Reposo", 5.5m, 38.5m, "Sin novedad");
            Assert.Equal("Fiebre", hc.Sintomas);
            Assert.Equal("Gripe", hc.Diagnostico);
            Assert.Equal(5.5m, hc.Peso);
            Assert.Equal(38.5m, hc.Temperatura);
        }
    }

    public class TratamientoTests
    {
        [Fact]
        public void Crear_Tratamiento_NoFinalizado()
        {
            var t = new Tratamiento("pac1", DateTime.Today, "Infección", "Antibiótico", "Dr. Test");
            Assert.False(t.Finalizado);
        }

        [Fact]
        public void Finalizar_Tratamiento_CambiaEstado()
        {
            var t = new Tratamiento("pac1", DateTime.Today, "Infección", "Antibiótico", "Dr. Test");
            t.Finalizar();
            Assert.True(t.Finalizado);
        }

        [Fact]
        public void Actualizar_Tratamiento_ActualizaVeterinario()
        {
            var t = new Tratamiento("pac1", DateTime.Today, "Infección", "Antibiótico", "Dr. Test");
            t.Actualizar("Infección tratada", "Continuar reposo", "Amoxicilina", "Sin complicaciones", veterinario: "Dr. Nuevo");
            Assert.Equal("Dr. Nuevo", t.Veterinario);
            Assert.Equal("Infección tratada", t.Diagnostico);
        }
    }

    public class VentaTests
    {
        [Fact]
        public void Crear_Venta_EstadoInicial_Pendiente()
        {
            var v = new Venta("prop1", 1, "Compra test");
            Assert.Equal(EstadoVenta.Pendiente, v.Estado);
            Assert.Equal(0m, v.Total);
        }

        [Fact]
        public void Confirmar_Venta_CambiaEstado()
        {
            var v = new Venta("prop1", 1, "Test");
            v.ActualizarTotal(5000m);
            v.Confirmar();
            Assert.Equal(EstadoVenta.Confirmada, v.Estado);
            Assert.Equal(5000m, v.Total);
        }
    }
}
