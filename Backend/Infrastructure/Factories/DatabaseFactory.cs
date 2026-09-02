using Application.Repositories;
using Domain.Others.Utils;
using Infrastructure.Constants;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using MongoDB.Bson.Serialization.Conventions;
using static Domain.Enums.Enums;

namespace Infrastructure.Factories
{
    internal static class DatabaseFactory
    {
        public static void CreateDataBase(this IServiceCollection services, string dbType, IConfiguration configuration)
        {
            switch (dbType.ToEnum<DatabaseType>())
            {
                case DatabaseType.MYSQL:
                case DatabaseType.MARIADB:
                case DatabaseType.SQLSERVER:
                    services.AddSqlServerRepositories(configuration);
                    break;
                case DatabaseType.SQLITE:
                    services.AddSqliteRepositories(configuration);
                    break;
                case DatabaseType.MONGODB:
                    services.AddMongoDbRepositories(configuration);
                    break;
                default:
                    throw new NotSupportedException(InfrastructureConstants.DATABASE_TYPE_NOT_SUPPORTED);
            }
        }

        private static IServiceCollection AddSqlServerRepositories(this IServiceCollection services, IConfiguration configuration)
        {
            services.AddDbContext<Repositories.Sql.StoreDbContext>(options =>
            {
                options.UseSqlServer(configuration.GetConnectionString("SqlConnection"));
            }, ServiceLifetime.Scoped);

            //Habilitar para trabajar con Migrations
            var context = services.BuildServiceProvider().GetRequiredService<Repositories.Sql.StoreDbContext>();
            context.Database.Migrate();

            RegisterSqlRepositories(services);

            return services;
        }

        private static IServiceCollection AddSqliteRepositories(this IServiceCollection services, IConfiguration configuration)
        {
            services.AddDbContext<Repositories.Sql.StoreDbContext>(options =>
            {
                options.UseSqlite(configuration.GetConnectionString("SqliteConnection"));
            }, ServiceLifetime.Scoped);

            // Crear base de datos si no existe
            var context = services.BuildServiceProvider().GetRequiredService<Repositories.Sql.StoreDbContext>();
            context.Database.EnsureCreated();

            // Validar y aplicar migraciones manuales sólo si la columna no existe
            EnsureColumnExists(context, "Pacientes", "ContadorInasistencias", "INTEGER NOT NULL DEFAULT 0");
            EnsureColumnExists(context, "Turnos", "ArchivosAdjuntos", "TEXT");
            EnsureColumnExists(context, "Tratamientos", "ArchivosAdjuntos", "TEXT");
            EnsureColumnExists(context, "HistorialesClinico", "ArchivosAdjuntos", "TEXT");
            EnsureColumnExists(context, "Servicios", "ProductosUtilizados", "TEXT");
            EnsureColumnExists(context, "RegistrosVacunacion", "ProductoId", "TEXT");
            EnsureColumnExists(context, "RegistrosVacunacion", "DepositoId", "INTEGER");
            EnsureColumnExists(context, "Marcas", "Descripcion", "TEXT");

            try
            {
                if (!context.Categorias.Any(c => c.Nombre.ToLower() == "vacunas" || c.Nombre.ToLower() == "vacuna"))
                {
                    context.Categorias.Add(new Domain.Entities.Categoria("Vacunas", "Vacunas y biológicos veterinarios"));
                    context.SaveChanges();
                }
            }
            catch { }

            RegisterSqlRepositories(services);

            return services;
        }

        private static void RegisterSqlRepositories(IServiceCollection services)
        {
            // Veterinary system repositories
            services.AddTransient<ISucursalRepository, Repositories.Sql.SucursalRepository>();
            services.AddTransient<IEspecieRepository, Repositories.Sql.EspecieRepository>();
            services.AddTransient<IRazaRepository, Repositories.Sql.RazaRepository>();
            services.AddTransient<IPropietarioRepository, Repositories.Sql.PropietarioRepository>();
            services.AddTransient<IPacienteRepository, Repositories.Sql.PacienteRepository>();

            // Phase 4 - Historial Clínico y Vacunaciones
            services.AddTransient<IVacunaRepository, Repositories.Sql.VacunaRepository>();
            services.AddTransient<IRegistroVacunacionRepository, Repositories.Sql.RegistroVacunacionRepository>();
            services.AddTransient<ITratamientoRepository, Repositories.Sql.TratamientoRepository>();
            services.AddTransient<IHistorialClinicoRepository, Repositories.Sql.HistorialClinicoRepository>();

            // Phase 5 - Gestión de Turnos y Horarios
            services.AddTransient<IServicioRepository, Repositories.Sql.ServicioRepository>();
            services.AddTransient<IVeterinarioRepository, Repositories.Sql.VeterinarioRepository>();
            services.AddTransient<ITurnoRepository, Repositories.Sql.TurnoRepository>();
            services.AddTransient<ITipoHorarioRepository, Repositories.Sql.TipoHorarioRepository>();
            services.AddTransient<IHorarioRepository, Repositories.Sql.HorarioRepository>();

            // Phase 6 - Gestión de Stock
            services.AddTransient<ICategoriaRepository, Repositories.Sql.CategoriaRepository>();
            services.AddTransient<IMarcaRepository, Repositories.Sql.MarcaRepository>();
            services.AddTransient<IProveedorRepository, Repositories.Sql.ProveedorRepository>();
            services.AddTransient<IDepositoRepository, Repositories.Sql.DepositoRepository>();
            services.AddTransient<IProductoRepository, Repositories.Sql.ProductoRepository>();
            services.AddTransient<IMovimientoStockRepository, Repositories.Sql.MovimientoStockRepository>();
            services.AddTransient<IProductoDepositoRepository, Repositories.Sql.ProductoDepositoRepository>();

            // Phase 7 - Ventas y Facturación
            services.AddTransient<IMetodoPagoRepository, Repositories.Sql.MetodoPagoRepository>();
            services.AddTransient<IVentaRepository, Repositories.Sql.VentaRepository>();
            services.AddTransient<IDetalleVentaRepository, Repositories.Sql.DetalleVentaRepository>();
            services.AddTransient<IFacturaRepository, Repositories.Sql.FacturaRepository>();

            // Phase 9 - Autenticación
            services.AddTransient<IRolRepository, Repositories.Sql.RolRepository>();
            services.AddTransient<IUsuarioRepository, Repositories.Sql.UsuarioRepository>();

            // Phase 10 - Auditoría y Notificaciones
            services.AddTransient<IAuditLogRepository, Repositories.Sql.AuditLogRepository>();
            services.AddTransient<INotificacionRepository, Repositories.Sql.NotificacionRepository>();

            // Phase 12 - Configuración del Sistema
            services.AddTransient<IConfiguracionSistemaRepository, Repositories.Sql.ConfiguracionSistemaRepository>();
        }

        private static IServiceCollection AddMongoDbRepositories(this IServiceCollection services, IConfiguration configuration)
        {
            ConventionRegistry.Register("Camel Case", new ConventionPack { new CamelCaseElementNameConvention() }, _ => true);

            Repositories.Mongo.StoreDbContext db = new(configuration.GetConnectionString("MongoConnection") ?? throw new NullReferenceException());
            services.AddSingleton(typeof(Repositories.Mongo.StoreDbContext), db);

            return services;
        }

        private static void EnsureColumnExists(Microsoft.EntityFrameworkCore.DbContext context, string tableName, string columnName, string columnDefinition)
        {
            try
            {
                var connection = context.Database.GetDbConnection();
                if (connection.State != System.Data.ConnectionState.Open)
                {
                    connection.Open();
                }

                using var command = connection.CreateCommand();
                command.CommandText = $"PRAGMA table_info({tableName});";
                using var reader = command.ExecuteReader();
                bool exists = false;
                while (reader.Read())
                {
                    var name = reader["name"]?.ToString();
                    if (string.Equals(name, columnName, StringComparison.OrdinalIgnoreCase))
                    {
                        exists = true;
                        break;
                    }
                }
                reader.Close();

                if (!exists)
                {
                    using var alterCommand = connection.CreateCommand();
                    alterCommand.CommandText = $"ALTER TABLE {tableName} ADD COLUMN {columnName} {columnDefinition};";
                    alterCommand.ExecuteNonQuery();
                }
            }
            catch { /* Ignore error on column check */ }
        }
    }
}

