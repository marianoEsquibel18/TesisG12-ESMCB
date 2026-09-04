using Application.Repositories;
using Domain.Entities;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Configuration;

namespace Controllers
{
    /// <summary>
    /// Controller para poblar la base de datos con datos de ejemplo realistas.
    /// Cubre los ultimos 30 dias y los proximos 15 dias para ambas sucursales.
    /// </summary>
    [ApiController]
    public class SeedController(
        IEspecieRepository especieRepo,
        IRazaRepository razaRepo,
        IPropietarioRepository propietarioRepo,
        IPacienteRepository pacienteRepo,
        IVeterinarioRepository veterinarioRepo,
        IServicioRepository servicioRepo,
        IVacunaRepository vacunaRepo,
        ITurnoRepository turnoRepo,
        IHistorialClinicoRepository historialRepo,
        IRegistroVacunacionRepository vacunacionRepo,
        ITratamientoRepository tratamientoRepo,
        IProductoRepository productoRepo,
        IVentaRepository ventaRepo,
        IDetalleVentaRepository detalleVentaRepo,
        IMetodoPagoRepository metodoPagoRepo,
        ICategoriaRepository categoriaRepo,
        IMarcaRepository marcaRepo,
        IProveedorRepository proveedorRepo,
        IDepositoRepository depositoRepo,
        ISucursalRepository sucursalRepo,
        IUsuarioRepository usuarioRepo,
        IRolRepository rolRepo,
        IProductoDepositoRepository pdRepo,
        IHorarioRepository horarioRepo,
        IConfiguration configuration) : BaseController
    {
        private async Task ClearDatabaseAsync()
        {
            var connectionString = configuration.GetConnectionString("SqliteConnection");
            using (var connection = new Microsoft.Data.Sqlite.SqliteConnection(connectionString))
            {
                await connection.OpenAsync();

                using (var pragmaCmd = connection.CreateCommand())
                {
                    pragmaCmd.CommandText = "PRAGMA foreign_keys = OFF;";
                    await pragmaCmd.ExecuteNonQueryAsync();
                }

                using (var transaction = await connection.BeginTransactionAsync())
                {
                    using (var command = connection.CreateCommand())
                    {
                        command.Transaction = transaction as SqliteTransaction;
                        command.CommandText = @"
                            DELETE FROM Facturas;
                            DELETE FROM DetallesVenta;
                            DELETE FROM Ventas;
                            DELETE FROM MetodosPago;
                            DELETE FROM MovimientosStock;
                            DELETE FROM ProductoDepositos;
                            DELETE FROM Productos;
                            DELETE FROM Depositos;
                            DELETE FROM Proveedores;
                            DELETE FROM Marcas;
                            DELETE FROM Categorias;
                            DELETE FROM Horarios;
                            DELETE FROM Turnos;
                            DELETE FROM HistorialesClinico;
                            DELETE FROM Tratamientos;
                            DELETE FROM RegistrosVacunacion;
                            DELETE FROM Vacunas;
                            DELETE FROM Servicios;
                            DELETE FROM Veterinarios;
                            DELETE FROM Pacientes;
                            DELETE FROM Propietarios;
                            DELETE FROM Razas;
                            DELETE FROM Especies;
                            DELETE FROM Usuarios;
                            DELETE FROM Roles;
                            DELETE FROM Sucursales;
                            DELETE FROM AuditLogs;
                            DELETE FROM Notificaciones;
                            DELETE FROM Configuraciones;
                            DELETE FROM sqlite_sequence;
                        ";
                        await command.ExecuteNonQueryAsync();
                    }
                    await transaction.CommitAsync();
                }

                using (var pragmaOn = connection.CreateCommand())
                {
                    pragmaOn.CommandText = "PRAGMA foreign_keys = ON;";
                    await pragmaOn.ExecuteNonQueryAsync();
                }
            }
        }

        /// <summary>
        /// Pobla la base de datos con datos de ejemplo completos para 45 dias (-30 a +15) en ambas sucursales.
        /// </summary>
        [HttpPost("api/v1/Seed/completo")]
        [HttpPost("api/v1/Seed/all")]
        public async Task<IActionResult> SeedCompleto()
        {
            await ClearDatabaseAsync();

            var resumen = new Dictionary<string, int>();

            // ═══════════════════════
            // 0. SUCURSALES Y ROLES
            // ═══════════════════════
            var sucursalCentral = new Sucursal("Sucursal Central", "Av. Corrientes 1234, CABA", "011-4555-0101", "central@veterinaria.com");
            await sucursalRepo.AddAsync(sucursalCentral);

            var sucursalNorte = new Sucursal("Sucursal Norte", "Av. Cabildo 5678, CABA", "011-4555-0202", "norte@veterinaria.com");
            await sucursalRepo.AddAsync(sucursalNorte);

            resumen["Sucursales"] = 2;

            // Roles
            var adminRol = new Rol("Admin", "Rol de Admin");
            await rolRepo.AddAsync(adminRol);

            var vetRol = new Rol("Veterinario", "Rol de Veterinario");
            await rolRepo.AddAsync(vetRol);

            var recepRol = new Rol("Recepcionista", "Rol de Recepcionista");
            await rolRepo.AddAsync(recepRol);

            var gerenteRol = new Rol("Gerente", "Rol de Gerente");
            await rolRepo.AddAsync(gerenteRol);

            resumen["Roles"] = 4;

            // Usuarios
            var adminUser = new Usuario("admin", "admin@veterinaria.com", "Administrador Global", "Admin123!", adminRol.Id, null);
            await usuarioRepo.AddAsync(adminUser);

            var vet1User = new Usuario("vet1", "vet1@veterinaria.com", "Alejandro Gómez", "Vet123!", vetRol.Id, sucursalCentral.Id);
            await usuarioRepo.AddAsync(vet1User);

            var vet2User = new Usuario("vet2", "vet2@veterinaria.com", "Valentina Ruiz", "Vet123!", vetRol.Id, sucursalCentral.Id);
            await usuarioRepo.AddAsync(vet2User);

            var vet3User = new Usuario("vet3", "vet3@veterinaria.com", "Martín Díaz", "Vet123!", vetRol.Id, sucursalNorte.Id);
            await usuarioRepo.AddAsync(vet3User);

            var vet4User = new Usuario("vet4", "vet4@veterinaria.com", "Carolina Méndez", "Vet123!", vetRol.Id, sucursalNorte.Id);
            await usuarioRepo.AddAsync(vet4User);

            var recep1User = new Usuario("recep1", "recep1@veterinaria.com", "Recepcionista Central", "Recep123!", recepRol.Id, sucursalCentral.Id);
            await usuarioRepo.AddAsync(recep1User);

            var recep2User = new Usuario("recep2", "recep2@veterinaria.com", "Recepcionista Norte", "Recep123!", recepRol.Id, sucursalNorte.Id);
            await usuarioRepo.AddAsync(recep2User);

            var gerente1User = new Usuario("gerente1", "gerente1@veterinaria.com", "Gerente Central", "Gerente123!", gerenteRol.Id, sucursalCentral.Id);
            await usuarioRepo.AddAsync(gerente1User);

            var gerente2User = new Usuario("gerente2", "gerente2@veterinaria.com", "Gerente Norte", "Gerente123!", gerenteRol.Id, sucursalNorte.Id);
            await usuarioRepo.AddAsync(gerente2User);

            resumen["Usuarios"] = 9;

            // ═══════════════════════
            // 1. ESPECIES Y RAZAS
            // ═══════════════════════
            var canino = new Especie("Canino", "Perros domésticos");
            var felino = new Especie("Felino", "Gatos domésticos");
            var ave = new Especie("Ave", "Aves domésticas y exóticas");
            var roedor = new Especie("Roedor", "Roedores domésticos");
            await especieRepo.AddAsync(canino);
            await especieRepo.AddAsync(felino);
            await especieRepo.AddAsync(ave);
            await especieRepo.AddAsync(roedor);
            resumen["Especies"] = 4;

            var razas = new List<Raza>
            {
                new("Labrador Retriever", canino.Id, "Raza grande, amigable"),
                new("Pastor Alemán", canino.Id, "Raza grande, inteligente"),
                new("Golden Retriever", canino.Id, "Raza grande, cariñosa"),
                new("Bulldog Francés", canino.Id, "Raza pequeña, compañía"),
                new("Caniche", canino.Id, "Raza mediana, hipoalergénica"),
                new("Mestizo", canino.Id, "Sin raza definida"),
                new("Siamés", felino.Id, "Gato elegante, vocal"),
                new("Persa", felino.Id, "Gato de pelo largo"),
                new("Común Europeo", felino.Id, "Gato doméstico estándar"),
                new("Loro", ave.Id, "Ave parlante"),
                new("Canario", ave.Id, "Ave cantora"),
                new("Hámster", roedor.Id, "Roedor pequeño"),
                new("Cobayo", roedor.Id, "Cobayo, roedor mediano"),
            };
            foreach (var r in razas) await razaRepo.AddAsync(r);
            resumen["Razas"] = razas.Count;

            // ═══════════════════════
            // 2. PROPIETARIOS (16)
            // ═══════════════════════
            var props = new List<Propietario>
            {
                new("Juan", "Pérez", "30555111", "1155550001", "juan@email.com", "Av. Mitre 1234, CABA"),
                new("María", "García", "30555222", "1155550002", "maria@email.com", "Calle 9 de Julio 567, CABA"),
                new("Carlos", "López", "30555333", "1155550003", "carlos@email.com", "Belgrano 890, Vicente López"),
                new("Ana", "Martínez", "30555444", "1155550004", "ana@email.com", "San Martín 321, San Isidro"),
                new("Roberto", "Fernández", "30555555", "1155550005", "roberto@email.com", "Rivadavia 456, CABA"),
                new("Laura", "Rodríguez", "30555666", "1155550006", "laura@email.com", "Sarmiento 789, CABA"),
                new("Diego", "Sánchez", "30555777", "1155550007", "diego@email.com", "Moreno 1011, Vicente López"),
                new("Patricia", "Torres", "30555888", "1155550008", "patricia@email.com", "Lavalle 1213, San Isidro"),
                new("Gonzalo", "Romero", "30555999", "1155550009", "gonzalo@email.com", "Av. Santa Fe 2345, CABA"),
                new("Lucía", "Benítez", "30555101", "1155550010", "lucia@email.com", "Av. Maipú 1500, Vicente López"),
                new("Esteban", "Morales", "30555102", "1155550011", "esteban@email.com", "Av. Cabildo 3200, CABA"),
                new("Florencia", "Castro", "30555103", "1155550012", "flor@email.com", "Av. Centenario 850, San Isidro"),
                new("Matías", "Rossi", "30555104", "1155550013", "matias@email.com", "Juramento 2100, CABA"),
                new("Camila", "Vega", "30555105", "1155550014", "camila@email.com", "Av. Libertador 4500, CABA"),
                new("Javier", "Navarro", "30555106", "1155550015", "javier@email.com", "Alvear 600, San Isidro"),
                new("Sofía", "Herrera", "30555107", "1155550016", "sofia@email.com", "Corrientes 3800, CABA"),
            };
            foreach (var p in props) await propietarioRepo.AddAsync(p);
            resumen["Propietarios"] = props.Count;

            // ═══════════════════════
            // 3. PACIENTES (24)
            // ═══════════════════════
            var pacientes = new List<Paciente>
            {
                new("Rex", canino.Id, props[0].Id, "M", razas[0].Id, DateTime.Today.AddYears(-3)),
                new("Luna", canino.Id, props[0].Id, "H", razas[2].Id, DateTime.Today.AddYears(-2)),
                new("Max", canino.Id, props[1].Id, "M", razas[1].Id, DateTime.Today.AddYears(-5)),
                new("Mia", felino.Id, props[1].Id, "H", razas[6].Id, DateTime.Today.AddYears(-1)),
                new("Rocky", canino.Id, props[2].Id, "M", razas[3].Id, DateTime.Today.AddMonths(-8)),
                new("Nina", canino.Id, props[3].Id, "H", razas[4].Id, DateTime.Today.AddYears(-4)),
                new("Simón", felino.Id, props[4].Id, "M", razas[7].Id, DateTime.Today.AddYears(-2)),
                new("Coco", ave.Id, props[5].Id, "M", razas[9].Id, DateTime.Today.AddYears(-6)),
                new("Toby", canino.Id, props[6].Id, "M", razas[5].Id, DateTime.Today.AddYears(-7)),
                new("Mishi", felino.Id, props[7].Id, "H", razas[8].Id, DateTime.Today.AddMonths(-10)),
                new("Pelusa", roedor.Id, props[3].Id, "H", razas[11].Id, DateTime.Today.AddMonths(-4)),
                new("Firulais", canino.Id, props[5].Id, "M", razas[5].Id, DateTime.Today.AddYears(-1)),
                new("Thor", canino.Id, props[8].Id, "M", razas[2].Id, DateTime.Today.AddYears(-3)),
                new("Bella", felino.Id, props[9].Id, "H", razas[6].Id, DateTime.Today.AddYears(-2)),
                new("Milo", canino.Id, props[10].Id, "M", razas[4].Id, DateTime.Today.AddYears(-5)),
                new("Kiara", canino.Id, props[11].Id, "H", razas[0].Id, DateTime.Today.AddYears(-4)),
                new("Bruno", canino.Id, props[12].Id, "M", razas[3].Id, DateTime.Today.AddYears(-1)),
                new("Lola", felino.Id, props[13].Id, "H", razas[8].Id, DateTime.Today.AddYears(-3)),
                new("Tom", felino.Id, props[14].Id, "M", razas[7].Id, DateTime.Today.AddYears(-6)),
                new("Baco", canino.Id, props[15].Id, "M", razas[1].Id, DateTime.Today.AddYears(-2)),
                new("Oliver", canino.Id, props[8].Id, "M", razas[5].Id, DateTime.Today.AddYears(-4)),
                new("Nala", felino.Id, props[9].Id, "H", razas[6].Id, DateTime.Today.AddYears(-1)),
                new("Sam", canino.Id, props[10].Id, "M", razas[0].Id, DateTime.Today.AddYears(-6)),
                new("Mora", canino.Id, props[11].Id, "H", razas[4].Id, DateTime.Today.AddYears(-2)),
                new("Henry", canino.Id, props[0].Id, "M", razas[0].Id, DateTime.Today.AddYears(-2)),
            };
            foreach (var p in pacientes) await pacienteRepo.AddAsync(p);
            resumen["Pacientes"] = pacientes.Count;

            // ═══════════════════════
            // 4. VETERINARIOS (4: 2 en Central, 2 en Norte)
            // ═══════════════════════
            var vets = new List<Veterinario>
            {
                new("Alejandro", "Gómez", "MP-1001", "1140001001", "agomez@vet.com", "Clínica General", sucursalCentral.Id),
                new("Valentina", "Ruiz", "MP-1002", "1140001002", "vruiz@vet.com", "Cirugía", sucursalCentral.Id),
                new("Martín", "Díaz", "MP-1003", "1140001003", "mdiaz@vet.com", "Dermatología", sucursalNorte.Id),
                new("Carolina", "Méndez", "MP-1004", "1140001004", "cmendez@vet.com", "Clínica General y Felinos", sucursalNorte.Id),
            };
            foreach (var v in vets) await veterinarioRepo.AddAsync(v);
            resumen["Veterinarios"] = vets.Count;

            // Link users to vets
            vet1User.SetVeterinarioId(vets[0].Id);
            vet2User.SetVeterinarioId(vets[1].Id);
            vet3User.SetVeterinarioId(vets[2].Id);
            vet4User.SetVeterinarioId(vets[3].Id);
            usuarioRepo.Update(vet1User.Id, vet1User);
            usuarioRepo.Update(vet2User.Id, vet2User);
            usuarioRepo.Update(vet3User.Id, vet3User);
            usuarioRepo.Update(vet4User.Id, vet4User);

            // Horarios semanales para los 4 veterinarios (TipoHorarioId = 1: Normal)
            for (int dia = 1; dia <= 5; dia++)
            {
                // Dr. Gómez (Central - Mañana)
                await horarioRepo.AddAsync(new Horario(vets[0].Id, dia, new TimeSpan(8, 0, 0), new TimeSpan(14, 0, 0), 1));
                // Dra. Ruiz (Central - Tarde)
                await horarioRepo.AddAsync(new Horario(vets[1].Id, dia, new TimeSpan(13, 0, 0), new TimeSpan(19, 0, 0), 1));
                // Dr. Díaz (Norte - Mañana)
                await horarioRepo.AddAsync(new Horario(vets[2].Id, dia, new TimeSpan(8, 0, 0), new TimeSpan(14, 0, 0), 1));
                // Dra. Méndez (Norte - Tarde)
                await horarioRepo.AddAsync(new Horario(vets[3].Id, dia, new TimeSpan(13, 0, 0), new TimeSpan(19, 0, 0), 1));
            }

            // ═══════════════════════
            // 5. SERVICIOS
            // ═══════════════════════
            var servicios = new List<Servicio>
            {
                new("Consulta general", "Revisión clínica completa del paciente", 30, 5000m, ""),
                new("Vacunación", "Aplicación de vacuna e insumos descartables", 15, 3500m, "Jeringa descartable 5ml x 100"),
                new("Cirugía menor", "Cirugías ambulatorias y suturas", 60, 15000m, "Amoxicilina 250mg x 20 comp, Jeringa descartable 5ml x 100"),
                new("Castración", "Esterilización quirúrgica completa", 45, 12000m, "Meloxicam 1.5mg/ml x 10ml, Jeringa descartable 5ml x 100"),
                new("Limpieza dental", "Profilaxis dental por ultrasonido", 40, 8000m, ""),
                new("Análisis clínico", "Extracción y análisis de sangre", 20, 4500m, "Jeringa descartable 5ml x 100"),
                new("Ecografía", "Estudio diagnóstico por imágenes", 30, 6000m, ""),
                new("Desparasitación", "Tratamiento antiparasitario", 10, 2500m, "Collar antipulgas canino"),
            };
            foreach (var s in servicios) await servicioRepo.AddAsync(s);
            resumen["Servicios"] = servicios.Count;

            // ═══════════════════════
            // 6. VACUNAS
            // ═══════════════════════
            var vacunas = new List<Vacuna>
            {
                new("Antirrábica", "Vacuna antirrábica obligatoria", "Bayer", 365),
                new("Séxtuple Canina", "Moquillo, Parvo, Hepatitis, Adenovirus, Parainfluenza, Leptospira", "Holliday", 365),
                new("Triple Felina", "Panleucopenia, Rinotraqueitis, Calicivirus", "Holliday", 365),
                new("Puppy DP", "Protección temprana cachorros Distemper y Parvovirus", "Bayer", 365),
                new("Leucemia Felina", "Virus de Leucemia Felina (FeLV)", "Holliday", 365),
            };
            foreach (var v in vacunas) await vacunaRepo.AddAsync(v);
            resumen["Vacunas"] = vacunas.Count;

            // ═══════════════════════
            // 7. CATEGORÍAS, MARCAS, PROVEEDORES Y DEPÓSITOS
            // ═══════════════════════
            var cats = new List<Categoria>
            {
                new("Medicamentos"), new("Vacunas"), new("Alimentos"), new("Accesorios"), new("Higiene"),
            };
            foreach (var c in cats) await categoriaRepo.AddAsync(c);

            var marcas = new List<Marca>
            {
                new("Royal Canin"), new("Bayer"), new("Purina"), new("Holliday"),
            };
            foreach (var m in marcas) await marcaRepo.AddAsync(m);

            var provs = new List<Proveedor>
            {
                new("Distribuidora VetPlus", "30-70001111-9", "1140009001", "vetplus@dist.com", "Zona Norte", "Pedro"),
                new("Droguería Animal", "30-70002222-9", "1140009002", "drogueria@animal.com", "Zona Sur", "Ana"),
            };
            foreach (var p in provs) await proveedorRepo.AddAsync(p);

            var deps = new List<Deposito>
            {
                new("Depósito Central", "Sala de insumos general", sucursalCentral.Id),
                new("Refrigerados Central", "Heladera de biológicos Central", sucursalCentral.Id),
                new("Depósito Norte", "Sala de insumos general", sucursalNorte.Id),
                new("Refrigerados Norte", "Heladera de biológicos Norte", sucursalNorte.Id),
            };
            foreach (var d in deps) await depositoRepo.AddAsync(d);

            // Productos
            var productos = new List<Producto>
            {
                new("Amoxicilina 250mg x 20 comp", "Antibiótico amplio espectro", "7790001001",
                    cats[0].Id, 1200m, 2500m, 60, 10, marcas[1].Id, provs[0].Id, deps[0].Id),
                new("Meloxicam 1.5mg/ml x 10ml", "Antiinflamatorio no esteroideo", "7790001002",
                    cats[0].Id, 1800m, 3500m, 40, 5, marcas[3].Id, provs[0].Id, deps[1].Id),
                new("Probiótico veterinario x 10", "Suplemento para flora intestinal", "7790001003",
                    cats[0].Id, 900m, 1800m, 35, 10, marcas[3].Id, provs[0].Id, deps[1].Id),
                new("Vacuna Antirrábica Canina/Felina", "Inmunización contra la rabia", "7790005001",
                    cats[1].Id, 2200m, 4500m, 50, 10, marcas[1].Id, provs[1].Id, deps[1].Id),
                new("Vacuna Séxtuple Canina", "Moquillo, Parvo, Hepatitis, Adenovirus", "7790005002",
                    cats[1].Id, 3500m, 6800m, 45, 10, marcas[3].Id, provs[1].Id, deps[1].Id),
                new("Vacuna Triple Felina", "Panleucopenia, Rinotraqueitis y Calicivirus", "7790005003",
                    cats[1].Id, 3000m, 6200m, 30, 8, marcas[3].Id, provs[1].Id, deps[1].Id),
                new("Royal Canin Adult 15kg", "Alimento seco perro adulto", "7790002001",
                    cats[2].Id, 18000m, 28000m, 25, 5, marcas[0].Id, provs[0].Id, deps[0].Id),
                new("Purina Cat Chow 8kg", "Alimento seco gato adulto", "7790002002",
                    cats[2].Id, 8000m, 12500m, 20, 5, marcas[2].Id, provs[1].Id, deps[0].Id),
                new("Collar antipulgas canino", "Collar antiparasitario externo", "7790003001",
                    cats[3].Id, 2000m, 4500m, 30, 10, marcas[1].Id, provs[1].Id, deps[0].Id),
                new("Jeringa descartable 5ml x 100", "Jeringas uso veterinario", "7790003002",
                    cats[3].Id, 3000m, 5500m, 100, 20, marcas[1].Id, provs[1].Id, deps[0].Id),
                new("Shampoo dermatológico 250ml", "Shampoo medicado piel sensible", "7790004001",
                    cats[4].Id, 1500m, 3000m, 45, 10, marcas[3].Id, provs[0].Id, deps[0].Id),
            };

            foreach (var p in productos)
            {
                await productoRepo.AddAsync(p);
                if (p.DepositoId.HasValue)
                {
                    var pdDefault = new ProductoDeposito(p.Id, p.DepositoId.Value, p.StockActual, p.StockMinimo);
                    await pdRepo.AddAsync(pdDefault);

                    int norteDepId = p.DepositoId.Value == deps[0].Id ? deps[2].Id : deps[3].Id;
                    var pdNorte = new ProductoDeposito(p.Id, norteDepId, p.StockActual / 2, p.StockMinimo);
                    await pdRepo.AddAsync(pdNorte);

                    p.SincronizarStockTotal();
                    productoRepo.Update(p.Id, p);
                }
            }
            resumen["Productos"] = productos.Count;

            // Métodos de Pago (5)
            var metodos = new List<MetodoPago>
            {
                new("Efectivo"), new("Tarjeta de Débito"), new("Tarjeta de Crédito"),
                new("Transferencia"), new("Mercado Pago"),
            };
            foreach (var m in metodos) await metodoPagoRepo.AddAsync(m);
            resumen["MétodosPago"] = metodos.Count;

            // ═══════════════════════════════════════════════════════════════════
            // 8. GENERACIÓN MASIVA: 45 DÍAS DE TURNOS, HISTORIALES, VACUNAS, TRATAMIENTOS Y VENTAS
            // ═══════════════════════════════════════════════════════════════════
            var hoy = DateTime.Today;
            int totalTurnosCount = 0;
            int turnosCompletadosCount = 0;
            int turnosAusentesCount = 0;
            int turnosCanceladosCount = 0;
            int turnosProgramadosCount = 0;
            int turnosReprogramadosCount = 0;
            int historialesCount = 0;
            int vacunacionesCount = 0;
            int tratamientosCount = 0;
            int ventasCount = 0;

            int globalTurnoIndex = 0;
            int centralPastIndex = 0;
            int nortePastIndex = 0;

            var diagnosticosMuestra = new[]
            {
                ("Gastroenteritis leve", "Vómitos y deposiciones blandas", "Dieta blanda con arroz y pollo hervido 3 días + probiótico", 38.4m),
                ("Otitis externa bilateral", "Rascado frecuente de orejas y sacudidas", "Limpieza de conducto auditivo y gotas óticas c/12hs x 7 días", 38.6m),
                ("Dermatitis alérgica por pulgas", "Prurito intenso en zona lumbosacra", "Collar antiparasitario y baño medicado con shampoo dermatológico", 38.3m),
                ("Control de rutina y peso", "Sin signos clínicos adversos", "Plan nutricional balanceado y control en 6 meses", 38.2m),
                ("Esguince de carpo leve", "Claudicación en pata delantera derecha", "Reposo relativo por 5 días + antiinflamatorio Meloxicam", 38.5m),
                ("Gingivitis y sarro moderado", "Halitosis y encías enrojecidas", "Se programa profilaxis dental y enjuague antiséptico", 38.3m),
                ("Chequeo anual preventivo", "Paciente activo y vivaz", "Vacunación al día y control general excelente", 38.1m),
                ("Faringitis viral felina", "Estornudos y secreción nasal serosa", "Nebulizaciones y antibiótico preventivo Amoxicilina", 38.9m)
            };

            for (int diaOffset = -30; diaOffset <= 15; diaOffset++)
            {
                var fechaDia = hoy.AddDays(diaOffset);
                bool esPasado = diaOffset < 0;
                bool esHoy = diaOffset == 0;
                bool esFuturo = diaOffset > 0;

                // Entre 2 y 4 turnos por día: asegurando SIEMPRE turnos en Central (sucursal 1) y en Norte (sucursal 2)
                int turnosHoyCount = (diaOffset % 2 == 0) ? 3 : 4;
                if (esHoy) turnosHoyCount = 5;

                for (int slot = 0; slot < turnosHoyCount; slot++)
                {
                    globalTurnoIndex++;
                    var pac = pacientes[(globalTurnoIndex + slot) % pacientes.Count];
                    var srv = servicios[(globalTurnoIndex * 2 + slot) % servicios.Count];

                    // Alternar sucursal y veterinario
                    // Central: vets[0] o vets[1]
                    // Norte: vets[2] o vets[3]
                    bool asignarCentral = (slot % 2 == 0);
                    var vet = asignarCentral
                        ? ((slot / 2 % 2 == 0) ? vets[0] : vets[1])
                        : ((slot / 2 % 2 == 0) ? vets[2] : vets[3]);

                    int hora = 9 + (slot * 2) + ((globalTurnoIndex % 3) * 1);
                    if (hora > 18) hora = 18;
                    var fechaHoraTurno = fechaDia.AddHours(hora).AddMinutes((globalTurnoIndex % 2 == 0) ? 0 : 30);

                    var turno = new Turno(pac.Id, vet.Id, srv.Id, fechaHoraTurno, srv.DuracionMinutos,
                        $"Atención: {srv.Nombre}", "", vet.SucursalId);

                    totalTurnosCount++;

                    if (esPasado)
                    {
                        // Distribución realista en ambas sucursales de forma equitativa:
                        // ~12.5% Ausentes, ~12.5% Cancelados, ~12.5% Reprogramados, ~62.5% Completados
                        int localIndex = asignarCentral ? (++centralPastIndex) : (++nortePastIndex);
                        int estadoMod = localIndex % 8;
                        if (estadoMod == 1)
                        {
                            turno.Ausente();
                            pac.IncrementarInasistencias();
                            pacienteRepo.Update(pac.Id, pac);
                            turnosAusentesCount++;
                        }
                        else if (estadoMod == 3)
                        {
                            turno.Cancelar("Cancelado con aviso previo por el propietario");
                            turnosCanceladosCount++;
                        }
                        else if (estadoMod == 5)
                        {
                            turno.Reprogramar(fechaHoraTurno.AddDays(3), srv.DuracionMinutos);
                            turnosReprogramadosCount++;
                        }
                        else
                        {
                            // Completado
                            turno.Completar("Atención médica finalizada. Paciente en buen estado clínico.");
                            turnosCompletadosCount++;

                            // Registrar Historial Clínico o Vacunación o Tratamiento
                            var diag = diagnosticosMuestra[(globalTurnoIndex + slot) % diagnosticosMuestra.Length];
                            decimal pesoAleat = 12m + (globalTurnoIndex % 18) * 1.2m;

                            if (srv.Nombre == "Vacunación")
                            {
                                var vac = vacunas[globalTurnoIndex % vacunas.Count];
                                var regVac = new RegistroVacunacion(pac.Id, vac.Id, fechaHoraTurno,
                                    vet.NombreCompleto, $"LOTE-{fechaHoraTurno:yyyyMM}A", fechaHoraTurno.AddYears(1),
                                    "Aplicación sin reacciones adversas");
                                await vacunacionRepo.AddAsync(regVac);
                                vacunacionesCount++;
                            }
                            else
                            {
                                var hist = new HistorialClinico(pac.Id, fechaHoraTurno, srv.Nombre,
                                    vet.NombreCompleto, diag.Item2, diag.Item1, diag.Item3,
                                    pesoAleat, diag.Item4, "Control clínico satisfactorio");
                                await historialRepo.AddAsync(hist);
                                historialesCount++;

                                if (srv.Nombre == "Cirugía menor" || srv.Nombre == "Castración" || srv.Nombre == "Desparasitación" || (globalTurnoIndex % 5 == 0))
                                {
                                    var trat = new Tratamiento(pac.Id, fechaHoraTurno, diag.Item1,
                                        $"Protocolo terapéutico: {diag.Item3}", vet.NombreCompleto,
                                        "Medicación según pauta médica", "Paciente en seguimiento");

                                    // Si tiene más de 7 días, se marca como finalizado; si es reciente, queda activo
                                    if (diaOffset < -7)
                                    {
                                        trat.Finalizar();
                                    }
                                    await tratamientoRepo.AddAsync(trat);
                                    tratamientosCount++;
                                }
                            }

                            // Venta asociada al turno completado
                            int metodoPagoId = metodos[globalTurnoIndex % metodos.Count].Id;
                            var ventaTurno = new Venta(pac.PropietarioId, metodoPagoId,
                                $"Cobro por {srv.Nombre} - {pac.Nombre}", vet.SucursalId, fechaHoraTurno);
                            await ventaRepo.AddAsync(ventaTurno);

                            // Detalle del servicio
                            await detalleVentaRepo.AddAsync(new DetalleVenta(ventaTurno.Id, null, srv.Nombre, 1, srv.Precio));
                            decimal totalVenta = srv.Precio;

                            // 50% de las veces se agrega un producto farmacéutico/insumo
                            if (globalTurnoIndex % 2 == 0)
                            {
                                var prodExtra = productos[globalTurnoIndex % productos.Count];
                                await detalleVentaRepo.AddAsync(new DetalleVenta(ventaTurno.Id, prodExtra.Id, prodExtra.Nombre, 1, prodExtra.PrecioVenta));
                                totalVenta += prodExtra.PrecioVenta;
                            }

                            ventaTurno.ActualizarTotal(totalVenta);
                            ventaTurno.Confirmar();
                            ventaRepo.Update(ventaTurno.Id, ventaTurno);
                            ventasCount++;
                        }
                    }
                    else if (esHoy)
                    {
                        // Turnos del día de hoy
                        if (slot == 0)
                        {
                            turno.Completar("Consulta matutina completada con éxito.");
                            turnosCompletadosCount++;

                            var hist = new HistorialClinico(pac.Id, fechaHoraTurno, srv.Nombre,
                                vet.NombreCompleto, "Control de rutina", "Saludable", "Próximo control anual", 22.4m, 38.3m);
                            await historialRepo.AddAsync(hist);
                            historialesCount++;

                            var vHoy = new Venta(pac.PropietarioId, metodos[0].Id, $"Atención de hoy: {srv.Nombre}", vet.SucursalId, fechaHoraTurno);
                            await ventaRepo.AddAsync(vHoy);
                            await detalleVentaRepo.AddAsync(new DetalleVenta(vHoy.Id, null, srv.Nombre, 1, srv.Precio));
                            vHoy.ActualizarTotal(srv.Precio);
                            vHoy.Confirmar();
                            ventaRepo.Update(vHoy.Id, vHoy);
                            ventasCount++;
                        }
                        else if (slot == 1)
                        {
                            turno.EnCurso();
                            turnosProgramadosCount++;
                        }
                        else if (slot == 2)
                        {
                            turno.Confirmar();
                            turnosProgramadosCount++;
                        }
                        else
                        {
                            // Programado
                            turnosProgramadosCount++;
                        }
                    }
                    else
                    {
                        // Futuro (+1 a +15 días)
                        if (diaOffset <= 3)
                        {
                            turno.Confirmar();
                        }
                        turnosProgramadosCount++;
                    }

                    await turnoRepo.AddAsync(turno);
                }

                // Generar 1 venta de mostrador (Pet shop / Insumos) por día para enriquecer las finanzas
                if (esPasado || esHoy)
                {
                    int sucursalVenta = (diaOffset % 2 == 0) ? sucursalCentral.Id : sucursalNorte.Id;
                    int metodoPagoId = metodos[(Math.Abs(diaOffset) + 2) % metodos.Count].Id;
                    var propComprador = props[(Math.Abs(diaOffset)) % props.Count];
                    var fechaVentaMostrador = fechaDia.AddHours(11).AddMinutes(20);

                    var vMostrador = new Venta(propComprador.Id, metodoPagoId, "Venta mostrador / Pet Shop", sucursalVenta, fechaVentaMostrador);
                    await ventaRepo.AddAsync(vMostrador);

                    var prod1 = productos[(Math.Abs(diaOffset)) % productos.Count];
                    var prod2 = productos[(Math.Abs(diaOffset) + 3) % productos.Count];

                    await detalleVentaRepo.AddAsync(new DetalleVenta(vMostrador.Id, prod1.Id, prod1.Nombre, 1, prod1.PrecioVenta));
                    await detalleVentaRepo.AddAsync(new DetalleVenta(vMostrador.Id, prod2.Id, prod2.Nombre, 1, prod2.PrecioVenta));

                    vMostrador.ActualizarTotal(prod1.PrecioVenta + prod2.PrecioVenta);
                    vMostrador.Confirmar();
                    ventaRepo.Update(vMostrador.Id, vMostrador);
                    ventasCount++;
                }
            }

            resumen["TurnosTotal"] = totalTurnosCount;
            resumen["TurnosCompletados"] = turnosCompletadosCount;
            resumen["TurnosAusentes"] = turnosAusentesCount;
            resumen["TurnosCancelados"] = turnosCanceladosCount;
            resumen["TurnosReprogramados"] = turnosReprogramadosCount;
            resumen["TurnosProgramados"] = turnosProgramadosCount;
            resumen["HistorialesClinicos"] = historialesCount;
            resumen["Vacunaciones"] = vacunacionesCount;
            resumen["Tratamientos"] = tratamientosCount;
            resumen["VentasConfirmadas"] = ventasCount;

            return Ok(new
            {
                Mensaje = "Datos de muestra para los ultimos 30 dias y proximos 15 dias generados exitosamente en ambas sucursales.",
                RangoTemporal = $"{hoy.AddDays(-30):dd/MM/yyyy} a {hoy.AddDays(15):dd/MM/yyyy}",
                SucursalesPobladas = new[] { "Sucursal Central (Id 1)", "Sucursal Norte (Id 2)" },
                Resumen = resumen
            });
        }

        /// <summary>
        /// Info para reseteo
        /// </summary>
        [HttpDelete("api/v1/Seed/reset")]
        public async Task<IActionResult> Reset()
        {
            await ClearDatabaseAsync();
            return Ok(new
            {
                Mensaje = "Base de datos reseteada (todas las tablas han sido vaciadas exitosamente).",
                Ruta = "Template-API/VeterinariaDB.sqlite"
            });
        }
    }
}
