using Domain.Entities;

namespace Domain.Tests
{
    public class PacienteTests
    {
        [Fact]
        public void Paciente_Create_ShouldBeValid()
        {
            // Arrange & Act
            var paciente = new Paciente(
                nombre: "Firulais",
                especieId: 1,
                propietarioId: "owner-123",
                sexo: "M",
                razaId: 1,
                fechaNacimiento: new DateTime(2020, 5, 15)
            );

            // Assert
            Assert.Equal("Firulais", paciente.Nombre);
            Assert.Equal(1, paciente.EspecieId);
            Assert.Equal("owner-123", paciente.PropietarioId);
            Assert.Equal("M", paciente.Sexo);
            Assert.Equal(1, paciente.RazaId);
            Assert.True(paciente.Activo);
            Assert.NotNull(paciente.Id);
            Assert.True(paciente.IsValid);
        }

        [Fact]
        public void Paciente_CreateWithEmptyNombre_ShouldBeInvalid()
        {
            // Arrange & Act
            var paciente = new Paciente("", 1, "owner-123", "M");

            // Assert
            Assert.False(paciente.IsValid);
            Assert.Contains(paciente.GetErrors(), e => e.PropertyName == "Nombre");
        }

        [Fact]
        public void Paciente_CreateWithInvalidSexo_ShouldBeInvalid()
        {
            // Arrange & Act
            var paciente = new Paciente("Firulais", 1, "owner-123", "X");

            // Assert
            Assert.False(paciente.IsValid);
            Assert.Contains(paciente.GetErrors(), e => e.PropertyName == "Sexo");
        }

        [Fact]
        public void Paciente_CreateWithEmptyPropietarioId_ShouldBeInvalid()
        {
            // Arrange & Act
            var paciente = new Paciente("Firulais", 1, "", "M");

            // Assert
            Assert.False(paciente.IsValid);
            Assert.Contains(paciente.GetErrors(), e => e.PropertyName == "PropietarioId");
        }

        [Fact]
        public void Paciente_EdadEnAnios_ShouldCalculateCorrectly()
        {
            // Arrange
            var fechaNacimiento = DateTime.Today.AddYears(-3);
            var paciente = new Paciente("Max", 1, "owner-123", "M", fechaNacimiento: fechaNacimiento);

            // Act & Assert
            Assert.Equal(3, paciente.EdadEnAnios);
        }

        [Fact]
        public void Paciente_EdadEnAnios_ShouldReturnNullWhenNoFechaNacimiento()
        {
            // Arrange
            var paciente = new Paciente("Milo", 1, "owner-123", "M");

            // Act & Assert
            Assert.Null(paciente.EdadEnAnios);
        }

        [Fact]
        public void Paciente_CambiarPropietario_ShouldUpdatePropietarioId()
        {
            // Arrange
            var paciente = new Paciente("Rex", 1, "owner-123", "M");

            // Act
            paciente.CambiarPropietario("owner-456");

            // Assert
            Assert.Equal("owner-456", paciente.PropietarioId);
        }

        [Fact]
        public void Paciente_Actualizar_ShouldUpdateProperties()
        {
            // Arrange
            var paciente = new Paciente("Toby", 1, "owner-123", "M");

            // Act
            paciente.Actualizar("Tobias", "H", new DateTime(2019, 1, 1), "Paciente castrado");

            // Assert
            Assert.Equal("Tobias", paciente.Nombre);
            Assert.Equal("H", paciente.Sexo);
            Assert.Equal(new DateTime(2019, 1, 1), paciente.FechaNacimiento);
            Assert.Equal("Paciente castrado", paciente.Observaciones);
        }

        [Fact]
        public void Paciente_Desactivar_ShouldSetActivoFalse()
        {
            // Arrange
            var paciente = new Paciente("Luna", 2, "owner-789", "H");

            // Act
            paciente.Desactivar();

            // Assert
            Assert.False(paciente.Activo);
        }

        [Fact]
        public void Paciente_CambiarEspecie_ShouldUpdateEspecieAndRaza()
        {
            // Arrange
            var paciente = new Paciente("Michi", 1, "owner-123", "M", razaId: 5);

            // Act
            paciente.CambiarEspecie(2, 10);

            // Assert
            Assert.Equal(2, paciente.EspecieId);
            Assert.Equal(10, paciente.RazaId);
        }

        [Fact]
        public void Paciente_Inasistencias_ShouldIncrementAndDecrementCorrectly()
        {
            // Arrange
            var paciente = new Paciente("Michi", 1, "owner-123", "M");

            // Act & Assert initial
            Assert.Equal(0, paciente.ContadorInasistencias);

            // Act increment
            paciente.IncrementarInasistencias();
            paciente.IncrementarInasistencias();
            Assert.Equal(2, paciente.ContadorInasistencias);

            // Act decrement
            paciente.DecrementarInasistencias();
            Assert.Equal(1, paciente.ContadorInasistencias);

            // Act reset
            paciente.ResetearInasistencias();
            Assert.Equal(0, paciente.ContadorInasistencias);
        }
    }
}
