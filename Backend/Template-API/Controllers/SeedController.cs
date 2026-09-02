using Application.Repositories;
using Domain.Entities;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Configuration;

namespace Controllers
{
    /// <summary>
    /// Controller para poblar la base de datos con datos de ejemplo realistas.
    /// Ideal para demostraciones de la tesis.
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
        IConfiguration configuration) : BaseController
    {
        private async Task ClearDatabaseAsync()
        {
            var connectionString = configuration.GetConnectionString("SqliteConnection");
            using (var connection = new Microsoft.Data.Sqlite.SqliteConnection(connectionString))
            {
                await connection.OpenAsync();
                using (var transaction = await connection.BeginTransactionAsync())
                {
                    using (var command = connection.CreateCommand())
                    {
                        command.Transaction = transaction as SqliteTransaction;
                        command.CommandText = @"
                            PRAGMA foreign_keys = OFF;
                            DELETE FROM DetallesVenta;
                            DELETE FROM Ventas;
                            DELETE FROM Facturas;
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
                            DELETE FROM Veterinarios;
                            DELETE FROM Servicios;
                            DELETE FROM HistorialesClinico;
                            DELETE FROM Tratamientos;
                            DELETE FROM RegistrosVacunacion;
                            DELETE FROM Vacunas;
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
                            PRAGMA foreign_keys = ON;
                        ";
                        await command.ExecuteNonQueryAsync();
                    }
                    await transaction.CommitAsync();
                }
            }
        }

        /// <summary>
        /// Pobla la base de datos con datos de ejemplo completos e interrelacionados
        /// </summary>
        [HttpPost("api/v1/Seed/completo")]
        public async Task<IActionResult> SeedCompleto()
        {
            // Reset database data completely to ensure a clean, idempotent seed execution
            await ClearDatabaseAsync();

            var resumen = new Dictionary<string, int>();

            // ═══════════════════════
            // 0. SUCURSALES Y ROLES
            // ═══════════════════════
            var sucursalCentral = (await sucursalRepo.FindAllAsync()).FirstOrDefault(s => s.Nombre == "Sucursal Central");
            if (sucursalCentral == null)
            {
                sucursalCentral = new Sucursal("Sucursal Central", "Av. Corrientes 1234, CABA", "011-4555-0101", "central@veterinaria.com");
                await sucursalRepo.AddAsync(sucursalCentral);
            }
            var sucursalNorte = (await sucursalRepo.FindAllAsync()).FirstOrDefault(s => s.Nombre == "Sucursal Norte");
            if (sucursalNorte == null)
            {
                sucursalNorte = new Sucursal("Sucursal Norte", "Av. Cabildo 5678, CABA", "011-4555-0202", "norte@veterinaria.com");
                await sucursalRepo.AddAsync(sucursalNorte);
            }
            resumen["Sucursales"] = 2;

            // Roles
            var adminRol = await rolRepo.GetByNombreAsync("Admin");
            if (adminRol == null)
            {
                adminRol = new Rol("Admin", "Rol de Admin");
                await rolRepo.AddAsync(adminRol);
            }
            var vetRol = await rolRepo.GetByNombreAsync("Veterinario");
            if (vetRol == null)
            {
                vetRol = new Rol("Veterinario", "Rol de Veterinario");
                await rolRepo.AddAsync(vetRol);
            }
            var recepRol = await rolRepo.GetByNombreAsync("Recepcionista");
            if (recepRol == null)
            {
                recepRol = new Rol("Recepcionista", "Rol de Recepcionista");
                await rolRepo.AddAsync(recepRol);
            }
            var gerenteRol = await rolRepo.GetByNombreAsync("Gerente");
            if (gerenteRol == null)
            {
                gerenteRol = new Rol("Gerente", "Rol de Gerente");
                await rolRepo.AddAsync(gerenteRol);
            }
            resumen["Roles"] = 4;

            // Usuarios (Admin es global, otros asignados a sucursal)
            var adminUser = await usuarioRepo.GetByNombreUsuarioAsync("admin");
            if (adminUser == null)
            {
                adminUser = new Usuario("admin", "admin@veterinaria.com", "Administrador Global", "Admin123!", adminRol.Id, null);
                await usuarioRepo.AddAsync(adminUser);
            }

            var vet1User = await usuarioRepo.GetByNombreUsuarioAsync("vet1");
            if (vet1User == null)
            {
                vet1User = new Usuario("vet1", "vet1@veterinaria.com", "Alejandro Gómez", "Vet123!", vetRol.Id, sucursalCentral.Id);
                await usuarioRepo.AddAsync(vet1User);
            }
            var vet2User = await usuarioRepo.GetByNombreUsuarioAsync("vet2");
            if (vet2User == null)
            {
                vet2User = new Usuario("vet2", "vet2@veterinaria.com", "Valentina Ruiz", "Vet123!", vetRol.Id, sucursalCentral.Id);
                await usuarioRepo.AddAsync(vet2User);
            }
            var vet3User = await usuarioRepo.GetByNombreUsuarioAsync("vet3");
            if (vet3User == null)
            {
                vet3User = new Usuario("vet3", "vet3@veterinaria.com", "Martín Díaz", "Vet123!", vetRol.Id, sucursalNorte.Id);
                await usuarioRepo.AddAsync(vet3User);
            }

            var recep1User = await usuarioRepo.GetByNombreUsuarioAsync("recep1");
            if (recep1User == null)
            {
                recep1User = new Usuario("recep1", "recep1@veterinaria.com", "Recepcionista Central", "Recep123!", recepRol.Id, sucursalCentral.Id);
                await usuarioRepo.AddAsync(recep1User);
            }
            var recep2User = await usuarioRepo.GetByNombreUsuarioAsync("recep2");
            if (recep2User == null)
            {
                recep2User = new Usuario("recep2", "recep2@veterinaria.com", "Recepcionista Norte", "Recep123!", recepRol.Id, sucursalNorte.Id);
                await usuarioRepo.AddAsync(recep2User);
            }

            var gerente1User = await usuarioRepo.GetByNombreUsuarioAsync("gerente1");
            if (gerente1User == null)
            {
                gerente1User = new Usuario("gerente1", "gerente1@veterinaria.com", "Gerente Central", "Gerente123!", gerenteRol.Id, sucursalCentral.Id);
                await usuarioRepo.AddAsync(gerente1User);
            }
            var gerente2User = await usuarioRepo.GetByNombreUsuarioAsync("gerente2");
            if (gerente2User == null)
            {
                gerente2User = new Usuario("gerente2", "gerente2@veterinaria.com", "Gerente Norte", "Gerente123!", gerenteRol.Id, sucursalNorte.Id);
                await usuarioRepo.AddAsync(gerente2User);
            }
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

            // Raza(string nombre, int especieId, string descripcion = "")
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
            // 2. PROPIETARIOS
            // ═══════════════════════
            var props = new List<Propietario>
            {
                new("Juan", "Pérez", "30555111", "1155550001", "juan@email.com", "Av. Mitre 1234"),
                new("María", "García", "30555222", "1155550002", "maria@email.com", "Calle 9 de Julio 567"),
                new("Carlos", "López", "30555333", "1155550003", "carlos@email.com", "Belgrano 890"),
                new("Ana", "Martínez", "30555444", "1155550004", "ana@email.com", "San Martín 321"),
                new("Roberto", "Fernández", "30555555", "1155550005", "roberto@email.com", "Rivadavia 456"),
                new("Laura", "Rodríguez", "30555666", "1155550006", "laura@email.com", "Sarmiento 789"),
                new("Diego", "Sánchez", "30555777", "1155550007", "diego@email.com", "Moreno 1011"),
                new("Patricia", "Torres", "30555888", "1155550008", "patricia@email.com", "Lavalle 1213"),
            };
            foreach (var p in props) await propietarioRepo.AddAsync(p);
            resumen["Propietarios"] = props.Count;

            // ═══════════════════════
            // 3. PACIENTES
            // Paciente(nombre, especieId, propietarioId, sexo, razaId?, fechaNacimiento?)
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
            };
            foreach (var p in pacientes) await pacienteRepo.AddAsync(p);
            resumen["Pacientes"] = pacientes.Count;

            // ═══════════════════════
            // 4. VETERINARIOS
            // Veterinario(nombre, apellido, matricula, telefono, email?, especialidad?, sucursalId)
            // ═══════════════════════
            var vets = new List<Veterinario>
            {
                new("Alejandro", "Gómez", "MP-1001", "1140001001", "agomez@vet.com", "Clínica General", sucursalCentral.Id),
                new("Valentina", "Ruiz", "MP-1002", "1140001002", "vruiz@vet.com", "Cirugía", sucursalCentral.Id),
                new("Martín", "Díaz", "MP-1003", "1140001003", "mdiaz@vet.com", "Dermatología", sucursalNorte.Id),
            };
            foreach (var v in vets) await veterinarioRepo.AddAsync(v);
            resumen["Veterinarios"] = vets.Count;

            // Link users to vets
            vet1User.SetVeterinarioId(vets[0].Id);
            vet2User.SetVeterinarioId(vets[1].Id);
            vet3User.SetVeterinarioId(vets[2].Id);
            usuarioRepo.Update(vet1User.Id, vet1User);
            usuarioRepo.Update(vet2User.Id, vet2User);
            usuarioRepo.Update(vet3User.Id, vet3User);

            // ═══════════════════════
            // 5. SERVICIOS
            // Servicio(nombre, descripcion, duracionMinutos, precio, insumos)
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
            // Vacuna(nombre, descripcion?, laboratorio?, intervaloDosisDias?)
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
            // 7. TURNOS
            // Turno(pacienteId, veterinarioId, servicioId, fechaHora, duracionMinutos, motivo?, observaciones?, sucursalId)
            // ═══════════════════════
            var turnosList = new List<Turno>();
            for (int i = 1; i <= 5; i++)
            {
                var vet = vets[i % vets.Count];
                var t = new Turno(pacientes[i % pacientes.Count].Id, vet.Id,
                    servicios[i % servicios.Count].Id, DateTime.Today.AddDays(i).AddHours(9 + i),
                    30, "Control de rutina", "", vet.SucursalId);
                turnosList.Add(t);
            }
            for (int i = 1; i <= 8; i++)
            {
                var vet = vets[i % vets.Count];
                var pac = pacientes[i % pacientes.Count];
                var t = new Turno(pac.Id, vet.Id,
                    servicios[0].Id, DateTime.Today.AddDays(-i * 5).AddHours(10),
                    30, "Consulta de seguimiento", "", vet.SucursalId);
                if (i == 2 || i == 6)
                {
                    t.Ausente();
                    pac.IncrementarInasistencias();
                    pacienteRepo.Update(pac.Id, pac);
                }
                else
                {
                    t.Completar("Paciente en buen estado");
                }
                turnosList.Add(t);
            }
            foreach (var t in turnosList) await turnoRepo.AddAsync(t);
            resumen["Turnos"] = turnosList.Count;

            // ═══════════════════════
            // 8. HISTORIAL CLÍNICO
            // HistorialClinico(pacienteId, fecha, motivo, veterinario, sintomas?, diagnostico?, indicaciones?, peso?, temp?, obs?)
            // ═══════════════════════
            var historiales = new List<HistorialClinico>
            {
                new(pacientes[0].Id, DateTime.Today.AddMonths(-6), "Control anual", "Dr. Gómez",
                    "Buen estado general", "Saludable", "Control en 6 meses", 28.5m, 38.2m, "Peso ideal"),
                new(pacientes[0].Id, DateTime.Today.AddMonths(-2), "Diarrea", "Dr. Gómez",
                    "Deposiciones blandas, inapetencia", "Gastroenteritis leve", "Dieta blanda 3 días + probióticos", 27.8m, 38.5m, "Mejoría esperada en 48hs"),
                new(pacientes[2].Id, DateTime.Today.AddMonths(-3), "Cojera", "Dra. Ruiz",
                    "Cojera en pata trasera izq", "Esguince leve", "Reposo 1 semana + antiinflamatorio", 32m, 38.3m),
                new(pacientes[4].Id, DateTime.Today.AddMonths(-1), "Primera consulta", "Dr. Díaz",
                    "Cachorro saludable", "Sin patologías", "Plan de vacunación iniciado", 4.2m, 38.8m, "Cachorro en excelente estado"),
                new(pacientes[6].Id, DateTime.Today.AddMonths(-4), "Vómitos", "Dr. Gómez",
                    "Vómitos recurrentes, pelo opaco", "Bola de pelo", "Pasta de malta + cepillado diario", 4.5m, 38.6m, "Típico en persas"),
                new(pacientes[8].Id, DateTime.Today.AddDays(-10), "Herida", "Dra. Ruiz",
                    "Herida cortante en almohadilla", "Herida superficial", "Limpieza + antibiótico tópico", 18m, 38.4m),
            };
            foreach (var h in historiales) await historialRepo.AddAsync(h);
            resumen["HistorialesClínicos"] = historiales.Count;

            // ═══════════════════════
            // 9. VACUNACIONES
            // RegistroVacunacion(pacienteId, vacunaId, fechaAplicacion, veterinario, nroLote?, fechaProximaDosis?, obs?)
            // ═══════════════════════
            var vacunaciones = new List<RegistroVacunacion>
            {
                new(pacientes[0].Id, vacunas[0].Id, DateTime.Today.AddMonths(-6), "Dr. Gómez",
                    "Lote-2026A", DateTime.Today.AddMonths(6), "Sin reacciones"),
                new(pacientes[0].Id, vacunas[1].Id, DateTime.Today.AddMonths(-6), "Dr. Gómez",
                    "Lote-2026B", DateTime.Today.AddMonths(6)),
                new(pacientes[2].Id, vacunas[0].Id, DateTime.Today.AddMonths(-2), "Dra. Ruiz",
                    "Lote-2026C", DateTime.Today.AddMonths(10)),
                new(pacientes[3].Id, vacunas[2].Id, DateTime.Today.AddMonths(-4), "Dr. Díaz",
                    "Lote-2026D", DateTime.Today.AddMonths(8)),
                new(pacientes[4].Id, vacunas[1].Id, DateTime.Today.AddDays(-15), "Dr. Gómez",
                    "Lote-2026E", DateTime.Today.AddDays(-15).AddDays(365), "Primera dosis"),
            };
            foreach (var v in vacunaciones) await vacunacionRepo.AddAsync(v);
            resumen["Vacunaciones"] = vacunaciones.Count;

            // ═══════════════════════
            // 10. TRATAMIENTOS
            // Tratamiento(pacienteId, fecha, diagnostico, descripcion, veterinario, medicacion?, obs?)
            // ═══════════════════════
            var tratamientos = new List<Tratamiento>
            {
                new(pacientes[0].Id, DateTime.Today.AddMonths(-2), "Gastroenteritis", "Dieta blanda + probióticos",
                    "Dr. Gómez", "Probiótico veterinario 1 sobre/día x 5 días", "Recuperación completa"),
                new(pacientes[2].Id, DateTime.Today.AddMonths(-3), "Esguince", "Reposo + antiinflamatorio",
                    "Dra. Ruiz", "Meloxicam 0.1mg/kg x 5 días", "Mejoría al 3er día"),
                new(pacientes[8].Id, DateTime.Today.AddDays(-10), "Herida en almohadilla", "Curación + antibiótico",
                    "Dra. Ruiz", "Amoxicilina 20mg/kg c/12hs x 7 días"),
            };
            // Finalizar los dos primeros
            tratamientos[0].Finalizar();
            tratamientos[1].Finalizar();
            foreach (var t in tratamientos) await tratamientoRepo.AddAsync(t);
            resumen["Tratamientos"] = tratamientos.Count;

            // ═══════════════════════
            // 11. STOCK Y DEPÓSITOS
            // ═══════════════════════
            var cats = new List<Categoria>
            {
                new("Medicamentos"), new("Vacunas"), new("Alimentos"), new("Accesorios"), new("Higiene"),
            };
            foreach (var c in cats) await categoriaRepo.AddAsync(c);
            resumen["Categorías"] = cats.Count;

            var marcas = new List<Marca>
            {
                new("Royal Canin"), new("Bayer"), new("Purina"), new("Holliday"),
            };
            foreach (var m in marcas) await marcaRepo.AddAsync(m);
            resumen["Marcas"] = marcas.Count;

            var provs = new List<Proveedor>
            {
                new("Distribuidora VetPlus", "30-70001111-9", "1140009001", "vetplus@dist.com", "Zona Norte", "Pedro"),
                new("Droguería Animal", "30-70002222-9", "1140009002", "drogueria@animal.com", "Zona Sur", "Ana"),
            };
            foreach (var p in provs) await proveedorRepo.AddAsync(p);
            resumen["Proveedores"] = provs.Count;

            var deps = new List<Deposito>
            {
                new("Depósito Central", "Sala de atrás", sucursalCentral.Id),
                new("Refrigerados Central", "Heladera de medicamentos y biológicos", sucursalCentral.Id),
                new("Depósito Norte", "Sala de atrás", sucursalNorte.Id),
                new("Refrigerados Norte", "Heladera de medicamentos y biológicos", sucursalNorte.Id),
            };
            foreach (var d in deps) await depositoRepo.AddAsync(d);
            resumen["Depósitos"] = deps.Count;

            // Producto(nombre, descripcion, codigoBarras, categoriaId, precioCompra, precioVenta, stockActual, stockMinimo, marcaId?, proveedorId?, depositoId?)
            var productos = new List<Producto>
            {
                // Medicamentos (cats[0])
                new("Amoxicilina 250mg x 20 comp", "Antibiótico amplio espectro", "7790001001",
                    cats[0].Id, 1200m, 2500m, 50, 10, marcas[1].Id, provs[0].Id, deps[0].Id),
                new("Meloxicam 1.5mg/ml x 10ml", "Antiinflamatorio no esteroideo", "7790001002",
                    cats[0].Id, 1800m, 3500m, 30, 5, marcas[3].Id, provs[0].Id, deps[1].Id),
                new("Probiótico veterinario x 10", "Suplemento para flora intestinal", "7790001003",
                    cats[0].Id, 900m, 1800m, 30, 10, marcas[3].Id, provs[0].Id, deps[1].Id),

                // Vacunas en Inventario (cats[1])
                new("Vacuna Antirrábica Canina/Felina", "Inmunización contra el virus de la rabia", "7790005001",
                    cats[1].Id, 2200m, 4500m, 40, 10, marcas[1].Id, provs[1].Id, deps[1].Id),
                new("Vacuna Séxtuple Canina", "Moquillo, Parvo, Hepatitis, Adenovirus, Parainfluenza, Leptospira", "7790005002",
                    cats[1].Id, 3500m, 6800m, 35, 10, marcas[3].Id, provs[1].Id, deps[1].Id),
                new("Vacuna Triple Felina", "Panleucopenia, Rinotraqueitis y Calicivirus", "7790005003",
                    cats[1].Id, 3000m, 6200m, 25, 8, marcas[3].Id, provs[1].Id, deps[1].Id),
                new("Vacuna Puppy DP", "Prevención temprana de Distemper y Parvovirus", "7790005004",
                    cats[1].Id, 2800m, 5800m, 20, 5, marcas[1].Id, provs[0].Id, deps[1].Id),

                // Alimentos (cats[2])
                new("Royal Canin Adult 15kg", "Alimento seco para perro adulto", "7790002001",
                    cats[2].Id, 18000m, 28000m, 20, 5, marcas[0].Id, provs[0].Id, deps[0].Id),
                new("Purina Cat Chow 8kg", "Alimento seco para gato", "7790002002",
                    cats[2].Id, 8000m, 12500m, 15, 5, marcas[2].Id, provs[1].Id, deps[0].Id),

                // Accesorios e Insumos (cats[3])
                new("Collar antipulgas canino", "Collar antiparasitario externo", "7790003001",
                    cats[3].Id, 2000m, 4500m, 25, 10, marcas[1].Id, provs[1].Id, deps[0].Id),
                new("Jeringa descartable 5ml x 100", "Jeringas descartables uso veterinario", "7790003002",
                    cats[3].Id, 3000m, 5500m, 80, 20, marcas[1].Id, provs[1].Id, deps[0].Id),

                // Higiene (cats[4])
                new("Shampoo dermatológico 250ml", "Shampoo medicado piel sensible", "7790004001",
                    cats[4].Id, 1500m, 3000m, 40, 10, marcas[3].Id, provs[0].Id, deps[0].Id),
            };
            foreach (var p in productos)
            {
                await productoRepo.AddAsync(p);
                if (p.DepositoId.HasValue)
                {
                    // Create ProductoDeposito for the default deposit (Central)
                    var pdDefault = new ProductoDeposito(p.Id, p.DepositoId.Value, p.StockActual, p.StockMinimo);
                    await pdRepo.AddAsync(pdDefault);

                    // Also seed stock for the corresponding deposit in Sucursal Norte (deps[2] or deps[3])
                    int norteDepId = p.DepositoId.Value == deps[0].Id ? deps[2].Id : deps[3].Id;
                    var pdNorte = new ProductoDeposito(p.Id, norteDepId, p.StockActual / 2, p.StockMinimo);
                    await pdRepo.AddAsync(pdNorte);

                    p.SincronizarStockTotal();
                    productoRepo.Update(p.Id, p);
                }
            }
            resumen["Productos"] = productos.Count;

            // ═══════════════════════
            // 12. MÉTODOS DE PAGO Y VENTAS
            // ═══════════════════════
            var metodos = new List<MetodoPago>
            {
                new("Efectivo"), new("Tarjeta de Débito"), new("Tarjeta de Crédito"),
                new("Transferencia"), new("Mercado Pago"),
            };
            foreach (var m in metodos) await metodoPagoRepo.AddAsync(m);
            resumen["MétodosPago"] = metodos.Count;

            // DetalleVenta(ventaId, productoId, descripcion, cantidad, precioUnitario)
            var v1 = new Venta(props[0].Id, metodos[0].Id, "Consulta y Medicación", sucursalCentral.Id);
            await ventaRepo.AddAsync(v1);
            await detalleVentaRepo.AddAsync(new DetalleVenta(v1.Id, null, servicios[0].Nombre, 1, servicios[0].Precio)); // Consulta general
            await detalleVentaRepo.AddAsync(new DetalleVenta(v1.Id, productos[2].Id, productos[2].Nombre, 1, productos[2].PrecioVenta));
            await detalleVentaRepo.AddAsync(new DetalleVenta(v1.Id, productos[0].Id, productos[0].Nombre, 2, productos[0].PrecioVenta));
            v1.ActualizarTotal(servicios[0].Precio + 28000m + 2 * 2500m);
            v1.Confirmar();
            ventaRepo.Update(v1.Id, v1);

            var v2 = new Venta(props[1].Id, metodos[1].Id, "Vacunación y Accesorios", sucursalCentral.Id);
            await ventaRepo.AddAsync(v2);
            await detalleVentaRepo.AddAsync(new DetalleVenta(v2.Id, null, servicios[1].Nombre, 1, servicios[1].Precio)); // Vacunación
            await detalleVentaRepo.AddAsync(new DetalleVenta(v2.Id, productos[4].Id, productos[4].Nombre, 1, productos[4].PrecioVenta));
            await detalleVentaRepo.AddAsync(new DetalleVenta(v2.Id, productos[5].Id, productos[5].Nombre, 1, productos[5].PrecioVenta));
            v2.ActualizarTotal(servicios[1].Precio + 4500m + 3000m);
            v2.Confirmar();
            ventaRepo.Update(v2.Id, v2);

            var v3 = new Venta(props[2].Id, metodos[4].Id, "Alimento felino", sucursalNorte.Id);
            await ventaRepo.AddAsync(v3);
            await detalleVentaRepo.AddAsync(new DetalleVenta(v3.Id, productos[3].Id, productos[3].Nombre, 2, productos[3].PrecioVenta));
            v3.ActualizarTotal(2 * 12500m);
            v3.Confirmar();
            ventaRepo.Update(v3.Id, v3);

            var v4 = new Venta(props[0].Id, metodos[2].Id, "Cirugía Menor y Medicamento", sucursalCentral.Id);
            await ventaRepo.AddAsync(v4);
            await detalleVentaRepo.AddAsync(new DetalleVenta(v4.Id, null, servicios[2].Nombre, 1, servicios[2].Precio)); // Cirugía menor
            await detalleVentaRepo.AddAsync(new DetalleVenta(v4.Id, null, servicios[0].Nombre, 1, servicios[0].Precio)); // Consulta general
            v4.ActualizarTotal(servicios[2].Precio + servicios[0].Precio);
            v4.Confirmar();
            ventaRepo.Update(v4.Id, v4);

            resumen["Ventas"] = 4;
            resumen["MétodosPago"] = metodos.Count;

            return Ok(new
            {
                Mensaje = "✅ Datos de ejemplo creados exitosamente",
                Resumen = resumen,
                TotalRegistros = resumen.Values.Sum(),
                StockBajo = new[] { "Probiótico veterinario x 10 (3/10)", "Jeringa descartable 5ml x 100 (8/20)" }
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
