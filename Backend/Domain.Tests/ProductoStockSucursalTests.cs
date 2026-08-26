using Domain.Entities;
using Xunit;

namespace Domain.Tests
{
    public class ProductoStockSucursalTests
    {
        [Fact]
        public void StockPorSucursal_CalculaStockIndependienteYAlertasCorrectamente()
        {
            // Escenario del usuario:
            // Producto A con StockMinimo = 20
            // Deposito 1 en Sucursal A con 25 items
            // Deposito 2 en Sucursal B con 12 items
            // Stock consolidado global = 37

            var producto = new Producto("Producto A", "Alimento balanceado", "7791234567890", 1, 1000m, 1500m, 37, 20);

            var depSucursalA = new Deposito("Depósito Principal Suc A", "Pasillo 1", 1);
            var depSucursalB = new Deposito("Depósito Principal Suc B", "Sector B", 2);

            var stockDepA = new ProductoDeposito(producto.Id, 1, 25, 20);
            stockDepA.AsignarDeposito(depSucursalA);
            var stockDepB = new ProductoDeposito(producto.Id, 2, 12, 20);
            stockDepB.AsignarDeposito(depSucursalB);

            var todosLosStocks = new List<ProductoDeposito> { stockDepA, stockDepB };

            // 1. Stock global consolidado
            int stockGlobal = todosLosStocks.Sum(s => s.StockActual);
            Assert.Equal(37, stockGlobal);

            // 2. Consulta aislada para Sucursal A (SucursalId = 1)
            var stocksSucursalA = todosLosStocks.Where(s => s.Deposito?.SucursalId == 1).ToList();
            int stockSucursalA = stocksSucursalA.Sum(s => s.StockActual);
            bool stockBajoSucursalA = stockSucursalA <= producto.StockMinimo;

            Assert.Equal(25, stockSucursalA);
            Assert.False(stockBajoSucursalA); // 25 > 20 -> No es stock bajo

            // 3. Consulta aislada para Sucursal B (SucursalId = 2)
            var stocksSucursalB = todosLosStocks.Where(s => s.Deposito?.SucursalId == 2).ToList();
            int stockSucursalB = stocksSucursalB.Sum(s => s.StockActual);
            bool stockBajoSucursalB = stockSucursalB <= producto.StockMinimo;

            Assert.Equal(12, stockSucursalB);
            Assert.True(stockBajoSucursalB); // 12 <= 20 -> Es stock bajo en Sucursal B
        }

        [Fact]
        public void StockPorSucursal_MovimientosPorDeposito_SincronizanTotales()
        {
            var producto = new Producto("Producto B", "Vacuna", "7799876543210", 2, 500m, 800m, 20, 10);
            var depA = new Deposito("Depósito Suc A", "Rack 1", 1);
            var stockDepA = new ProductoDeposito(producto.Id, 1, 15, 10);
            stockDepA.AsignarDeposito(depA);

            // Entrada en Sucursal A (+10)
            stockDepA.AgregarStock(10);
            Assert.Equal(25, stockDepA.StockActual);

            // Salida en Sucursal A (-5)
            bool descontado = stockDepA.DescontarStock(5);
            Assert.True(descontado);
            Assert.Equal(20, stockDepA.StockActual);

            // Salida con stock insuficiente falla
            bool descontadoExceso = stockDepA.DescontarStock(30);
            Assert.False(descontadoExceso);
            Assert.Equal(20, stockDepA.StockActual);
        }
    }
}
