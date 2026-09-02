using Domain.Entities;

namespace Domain.Tests
{
    public class PropietarioTests
    {
        [Fact]
        public void Propietario_Create_ShouldBeValid()
        {
            // Arrange & Act
            var propietario = new Propietario("Juan", "Pérez", "12345678", "1155551234", "juan@email.com", "Calle 123");

            // Assert
            Assert.Equal("Juan", propietario.Nombre);
            Assert.Equal("Pérez", propietario.Apellido);
            Assert.Equal("12345678", propietario.DNI);
            Assert.Equal("1155551234", propietario.Telefono);
            Assert.Equal("juan@email.com", propietario.Email);
            Assert.Equal("Calle 123", propietario.Direccion);
            Assert.True(propietario.Activo);
            Assert.NotNull(propietario.Id);
            Assert.True(propietario.IsValid);
        }

        [Fact]
        public void Propietario_NombreCompleto_ShouldConcatenateNombreYApellido()
        {
            // Arrange
            var propietario = new Propietario("María", "García", "87654321", "1155559999");

            // Act & Assert
            Assert.Equal("María García", propietario.NombreCompleto);
        }

        [Fact]
        public void Propietario_CreateWithEmptyNombre_ShouldBeInvalid()
        {
            // Arrange & Act
            var propietario = new Propietario("", "Pérez", "12345678", "1155551234");

            // Assert
            Assert.False(propietario.IsValid);
            Assert.Contains(propietario.GetErrors(), e => e.PropertyName == "Nombre");
        }

        [Fact]
        public void Propietario_CreateWithEmptyDNI_ShouldBeInvalid()
        {
            // Arrange & Act
            var propietario = new Propietario("Juan", "Pérez", "", "1155551234");

            // Assert
            Assert.False(propietario.IsValid);
            Assert.Contains(propietario.GetErrors(), e => e.PropertyName == "DNI");
        }

        [Fact]
        public void Propietario_CreateWithInvalidEmail_ShouldBeInvalid()
        {
            // Arrange & Act
            var propietario = new Propietario("Juan", "Pérez", "12345678", "1155551234", "email-invalido");

            // Assert
            Assert.False(propietario.IsValid);
            Assert.Contains(propietario.GetErrors(), e => e.PropertyName == "Email");
        }

        [Fact]
        public void Propietario_Actualizar_ShouldUpdateProperties()
        {
            // Arrange
            var propietario = new Propietario("Juan", "Pérez", "12345678", "1155551234");

            // Act
            propietario.Actualizar("Juan Carlos", "Pérez López", "1155559999", "nuevo@email.com", "Nueva Dirección 456");

            // Assert
            Assert.Equal("Juan Carlos", propietario.Nombre);
            Assert.Equal("Pérez López", propietario.Apellido);
            Assert.Equal("1155559999", propietario.Telefono);
            Assert.Equal("nuevo@email.com", propietario.Email);
            Assert.Equal("Nueva Dirección 456", propietario.Direccion);
        }

        [Fact]
        public void Propietario_Actualizar_ConDNI_ShouldUpdateDNI()
        {
            // Arrange
            var propietario = new Propietario("Juan", "Pérez", "12345678", "1155551234");

            // Act
            propietario.Actualizar("Juan", "Pérez", "99999999", "1155551234", "juan@test.com", "Calle 1");

            // Assert
            Assert.Equal("99999999", propietario.DNI);
        }

        [Fact]
        public void Propietario_Desactivar_ShouldSetActivoFalse()
        {
            // Arrange
            var propietario = new Propietario("Juan", "Pérez", "12345678", "1155551234");

            // Act
            propietario.Desactivar();

            // Assert
            Assert.False(propietario.Activo);
        }
    }
}
