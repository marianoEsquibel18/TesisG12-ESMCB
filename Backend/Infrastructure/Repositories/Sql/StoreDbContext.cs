using Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Repositories.Sql
{
    /// <summary>
    /// Contexto de almacenamiento en base de datos. Aca se definen los nombres de 
    /// las tablas, y los mapeos entre los objetos
    /// </summary>
    internal class StoreDbContext : DbContext
    {
        // Entidades del sistema veterinario
        public DbSet<Especie> Especies { get; set; }
        public DbSet<Raza> Razas { get; set; }
        public DbSet<Propietario> Propietarios { get; set; }
        public DbSet<Paciente> Pacientes { get; set; }

        // Entidades de historial clínico y vacunaciones
        public DbSet<Vacuna> Vacunas { get; set; }
        public DbSet<RegistroVacunacion> RegistrosVacunacion { get; set; }
        public DbSet<Tratamiento> Tratamientos { get; set; }
        public DbSet<HistorialClinico> HistorialesClinico { get; set; }

        // Entidades de gestión de turnos y horarios
        public DbSet<Servicio> Servicios { get; set; }
        public DbSet<Veterinario> Veterinarios { get; set; }
        public DbSet<Turno> Turnos { get; set; }
        public DbSet<TipoHorario> TiposHorarios { get; set; }
        public DbSet<Horario> Horarios { get; set; }

        // Entidades de gestión de stock
        public DbSet<Categoria> Categorias { get; set; }
        public DbSet<Marca> Marcas { get; set; }
        public DbSet<Proveedor> Proveedores { get; set; }
        public DbSet<Deposito> Depositos { get; set; }
        public DbSet<Producto> Productos { get; set; }
        public DbSet<ProductoDeposito> ProductoDepositos { get; set; }
        public DbSet<MovimientoStock> MovimientosStock { get; set; }

        // Entidades de ventas y facturación
        public DbSet<MetodoPago> MetodosPago { get; set; }
        public DbSet<Venta> Ventas { get; set; }
        public DbSet<DetalleVenta> DetallesVenta { get; set; }
        public DbSet<Factura> Facturas { get; set; }

        // Entidades de autenticación
        public DbSet<Rol> Roles { get; set; }
        public DbSet<Usuario> Usuarios { get; set; }
        public DbSet<Sucursal> Sucursales { get; set; }

        // Entidades de auditoría y notificaciones
        public DbSet<AuditLog> AuditLogs { get; set; }
        public DbSet<Notificacion> Notificaciones { get; set; }

        // Entidades de configuración
        public DbSet<ConfiguracionSistema> Configuraciones { get; set; }

        public StoreDbContext(DbContextOptions<StoreDbContext> options) : base(options)
        {
        }

        protected StoreDbContext()
        {
        }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            base.OnConfiguring(optionsBuilder);
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            // Configuración de Especie
            modelBuilder.Entity<Especie>(entity =>
            {
                entity.ToTable("Especies");
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Id).ValueGeneratedOnAdd();
                entity.Property(e => e.Nombre).IsRequired().HasMaxLength(50);
                entity.Property(e => e.Descripcion).HasMaxLength(200);
            });

            // Configuración de Raza
            modelBuilder.Entity<Raza>(entity =>
            {
                entity.ToTable("Razas");
                entity.HasKey(r => r.Id);
                entity.Property(r => r.Id).ValueGeneratedOnAdd();
                entity.Property(r => r.Nombre).IsRequired().HasMaxLength(100);
                entity.Property(r => r.Descripcion).HasMaxLength(200);

                entity.HasOne(r => r.Especie)
                    .WithMany(e => e.Razas)
                    .HasForeignKey(r => r.EspecieId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            // Configuración de Propietario
            modelBuilder.Entity<Propietario>(entity =>
            {
                entity.ToTable("Propietarios");
                entity.HasKey(p => p.Id);
                entity.Property(p => p.Nombre).IsRequired().HasMaxLength(100);
                entity.Property(p => p.Apellido).IsRequired().HasMaxLength(100);
                entity.Property(p => p.DNI).IsRequired().HasMaxLength(20);
                entity.Property(p => p.Telefono).IsRequired().HasMaxLength(20);
                entity.Property(p => p.Email).HasMaxLength(100);
                entity.Property(p => p.Direccion).HasMaxLength(200);

                entity.HasIndex(p => p.DNI).IsUnique();
            });

            // Configuración de Paciente
            modelBuilder.Entity<Paciente>(entity =>
            {
                entity.ToTable("Pacientes");
                entity.HasKey(p => p.Id);
                entity.Property(p => p.Nombre).IsRequired().HasMaxLength(100);
                entity.Property(p => p.Sexo).IsRequired().HasMaxLength(1);
                entity.Property(p => p.FotoUrl).HasMaxLength(2000000);
                entity.Property(p => p.Observaciones).HasMaxLength(1000);

                entity.HasOne(p => p.Especie)
                    .WithMany(e => e.Pacientes)
                    .HasForeignKey(p => p.EspecieId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(p => p.Raza)
                    .WithMany(r => r.Pacientes)
                    .HasForeignKey(p => p.RazaId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(p => p.Propietario)
                    .WithMany(pr => pr.Mascotas)
                    .HasForeignKey(p => p.PropietarioId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            // Configuración de Vacuna
            modelBuilder.Entity<Vacuna>(entity =>
            {
                entity.ToTable("Vacunas");
                entity.HasKey(v => v.Id);
                entity.Property(v => v.Id).ValueGeneratedOnAdd();
                entity.Property(v => v.Nombre).IsRequired().HasMaxLength(100);
                entity.Property(v => v.Descripcion).HasMaxLength(500);
                entity.Property(v => v.Laboratorio).HasMaxLength(100);
            });

            // Configuración de RegistroVacunacion
            modelBuilder.Entity<RegistroVacunacion>(entity =>
            {
                entity.ToTable("RegistrosVacunacion");
                entity.HasKey(r => r.Id);
                entity.Property(r => r.Veterinario).IsRequired().HasMaxLength(100);
                entity.Property(r => r.NroLote).HasMaxLength(50);
                entity.Property(r => r.Observaciones).HasMaxLength(500);

                entity.HasOne(r => r.Paciente)
                    .WithMany()
                    .HasForeignKey(r => r.PacienteId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(r => r.Vacuna)
                    .WithMany(v => v.Registros)
                    .HasForeignKey(r => r.VacunaId)
                    .IsRequired(false)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(r => r.Producto)
                    .WithMany()
                    .HasForeignKey(r => r.ProductoId)
                    .IsRequired(false)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasIndex(r => r.PacienteId);
                entity.HasIndex(r => r.VacunaId);
                entity.HasIndex(r => r.ProductoId);
            });

            // Configuración de Tratamiento
            modelBuilder.Entity<Tratamiento>(entity =>
            {
                entity.ToTable("Tratamientos");
                entity.HasKey(t => t.Id);
                entity.Property(t => t.Diagnostico).IsRequired().HasMaxLength(500);
                entity.Property(t => t.Descripcion).IsRequired().HasMaxLength(1000);
                entity.Property(t => t.Medicacion).HasMaxLength(500);
                entity.Property(t => t.Veterinario).IsRequired().HasMaxLength(100);
                entity.Property(t => t.Observaciones).HasMaxLength(1000);
                entity.Property(t => t.ArchivosAdjuntos);

                entity.HasOne(t => t.Paciente)
                    .WithMany()
                    .HasForeignKey(t => t.PacienteId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasIndex(t => t.PacienteId);
            });

            // Configuración de HistorialClinico
            modelBuilder.Entity<HistorialClinico>(entity =>
            {
                entity.ToTable("HistorialesClinico");
                entity.HasKey(h => h.Id);
                entity.Property(h => h.Motivo).IsRequired().HasMaxLength(200);
                entity.Property(h => h.Sintomas).HasMaxLength(1000);
                entity.Property(h => h.Diagnostico).HasMaxLength(500);
                entity.Property(h => h.Indicaciones).HasMaxLength(1000);
                entity.Property(h => h.Veterinario).IsRequired().HasMaxLength(100);
                entity.Property(h => h.Observaciones).HasMaxLength(1000);
                entity.Property(h => h.ArchivosAdjuntos);
                entity.Property(h => h.Peso).HasColumnType("decimal(10,2)");
                entity.Property(h => h.Temperatura).HasColumnType("decimal(4,1)");

                entity.HasOne(h => h.Paciente)
                    .WithMany()
                    .HasForeignKey(h => h.PacienteId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasIndex(h => h.PacienteId);
            });

            // Configuración de Servicio
            modelBuilder.Entity<Servicio>(entity =>
            {
                entity.ToTable("Servicios");
                entity.HasKey(s => s.Id);
                entity.Property(s => s.Id).ValueGeneratedOnAdd();
                entity.Property(s => s.Nombre).IsRequired().HasMaxLength(100);
                entity.Property(s => s.Descripcion).HasMaxLength(500);
                entity.Property(s => s.Precio).HasColumnType("decimal(10,2)");
                entity.Property(s => s.ProductosUtilizados).HasMaxLength(2000);
            });

            // Configuración de Veterinario
            modelBuilder.Entity<Veterinario>(entity =>
            {
                entity.ToTable("Veterinarios");
                entity.HasKey(v => v.Id);
                entity.Property(v => v.Nombre).IsRequired().HasMaxLength(50);
                entity.Property(v => v.Apellido).IsRequired().HasMaxLength(50);
                entity.Property(v => v.Matricula).IsRequired().HasMaxLength(20);
                entity.Property(v => v.Telefono).IsRequired().HasMaxLength(20);
                entity.Property(v => v.Email).HasMaxLength(100);
                entity.Property(v => v.Especialidad).HasMaxLength(100);

                entity.HasIndex(v => v.Matricula).IsUnique();

                entity.HasOne(v => v.Sucursal)
                    .WithMany()
                    .HasForeignKey(v => v.SucursalId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            // Configuración de TipoHorario
            modelBuilder.Entity<TipoHorario>(entity =>
            {
                entity.ToTable("TiposHorarios");
                entity.HasKey(t => t.Id);
                entity.Property(t => t.Nombre).IsRequired().HasMaxLength(50);
                entity.Property(t => t.Descripcion).HasMaxLength(200);

                entity.HasData(
                    new TipoHorario(1, "Normal", "Horario de atención regular"),
                    new TipoHorario(2, "Guardia", "Horario de guardia / emergencia")
                );
            });

            // Configuración de Horario
            modelBuilder.Entity<Horario>(entity =>
            {
                entity.ToTable("Horarios");
                entity.HasKey(h => h.Id);
                entity.Property(h => h.VeterinarioId).IsRequired();
                entity.Property(h => h.DiaSemana).IsRequired();
                entity.Property(h => h.HoraInicio).IsRequired();
                entity.Property(h => h.HoraFin).IsRequired();
                entity.Property(h => h.TipoHorarioId).IsRequired();

                entity.HasOne(h => h.Veterinario)
                    .WithMany(v => v.Horarios)
                    .HasForeignKey(h => h.VeterinarioId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(h => h.TipoHorario)
                    .WithMany(t => t.Horarios)
                    .HasForeignKey(h => h.TipoHorarioId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasIndex(h => h.VeterinarioId);
                entity.HasIndex(h => h.DiaSemana);
            });

            // Configuración de Turno
            modelBuilder.Entity<Turno>(entity =>
            {
                entity.ToTable("Turnos");
                entity.HasKey(t => t.Id);
                entity.Property(t => t.Motivo).HasMaxLength(200);
                entity.Property(t => t.Observaciones).HasMaxLength(500);
                entity.Property(t => t.ArchivosAdjuntos);

                entity.HasOne(t => t.Paciente)
                    .WithMany()
                    .HasForeignKey(t => t.PacienteId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(t => t.Veterinario)
                    .WithMany(v => v.Turnos)
                    .HasForeignKey(t => t.VeterinarioId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(t => t.Servicio)
                    .WithMany()
                    .HasForeignKey(t => t.ServicioId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(t => t.Sucursal)
                    .WithMany()
                    .HasForeignKey(t => t.SucursalId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasIndex(t => t.PacienteId);
                entity.HasIndex(t => t.VeterinarioId);
                entity.HasIndex(t => t.FechaHora);
            });

            // Configuración de Categoria
            modelBuilder.Entity<Categoria>(entity =>
            {
                entity.ToTable("Categorias");
                entity.HasKey(c => c.Id);
                entity.Property(c => c.Id).ValueGeneratedOnAdd();
                entity.Property(c => c.Nombre).IsRequired().HasMaxLength(100);
                entity.Property(c => c.Descripcion).HasMaxLength(300);
            });

            // Configuración de Marca
            modelBuilder.Entity<Marca>(entity =>
            {
                entity.ToTable("Marcas");
                entity.HasKey(m => m.Id);
                entity.Property(m => m.Id).ValueGeneratedOnAdd();
                entity.Property(m => m.Nombre).IsRequired().HasMaxLength(100);
            });

            // Configuración de Proveedor
            modelBuilder.Entity<Proveedor>(entity =>
            {
                entity.ToTable("Proveedores");
                entity.HasKey(p => p.Id);
                entity.Property(p => p.RazonSocial).IsRequired().HasMaxLength(150);
                entity.Property(p => p.CUIT).IsRequired().HasMaxLength(13);
                entity.Property(p => p.Telefono).IsRequired().HasMaxLength(20);
                entity.Property(p => p.Email).HasMaxLength(100);
                entity.Property(p => p.Direccion).HasMaxLength(200);
                entity.Property(p => p.Contacto).HasMaxLength(100);
                entity.HasIndex(p => p.CUIT).IsUnique();
            });

            // Configuración de Deposito
            modelBuilder.Entity<Deposito>(entity =>
            {
                entity.ToTable("Depositos");
                entity.HasKey(d => d.Id);
                entity.Property(d => d.Id).ValueGeneratedOnAdd();
                entity.Property(d => d.Nombre).IsRequired().HasMaxLength(100);
                entity.Property(d => d.Ubicacion).HasMaxLength(200);

                entity.HasOne(d => d.Sucursal)
                    .WithMany()
                    .HasForeignKey(d => d.SucursalId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            // Configuración de Producto
            modelBuilder.Entity<Producto>(entity =>
            {
                entity.ToTable("Productos");
                entity.HasKey(p => p.Id);
                entity.Property(p => p.Nombre).IsRequired().HasMaxLength(150);
                entity.Property(p => p.Descripcion).HasMaxLength(500);
                entity.Property(p => p.CodigoBarras).HasMaxLength(50);
                entity.Property(p => p.PrecioCompra).HasColumnType("decimal(10,2)");
                entity.Property(p => p.PrecioVenta).HasColumnType("decimal(10,2)");

                entity.HasOne(p => p.Categoria)
                    .WithMany()
                    .HasForeignKey(p => p.CategoriaId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(p => p.Marca)
                    .WithMany()
                    .HasForeignKey(p => p.MarcaId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(p => p.Proveedor)
                    .WithMany()
                    .HasForeignKey(p => p.ProveedorId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(p => p.Deposito)
                    .WithMany()
                    .HasForeignKey(p => p.DepositoId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasIndex(p => p.CategoriaId);
                entity.HasIndex(p => p.CodigoBarras);
            });

            // Configuración de ProductoDeposito (stock por depósito)
            modelBuilder.Entity<ProductoDeposito>(entity =>
            {
                entity.ToTable("ProductoDepositos");
                entity.HasKey(pd => pd.Id);

                entity.HasOne(pd => pd.Producto)
                    .WithMany(p => p.StocksDepositos)
                    .HasForeignKey(pd => pd.ProductoId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(pd => pd.Deposito)
                    .WithMany()
                    .HasForeignKey(pd => pd.DepositoId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasIndex(pd => new { pd.ProductoId, pd.DepositoId }).IsUnique();
            });

            // Configuración de MovimientoStock
            modelBuilder.Entity<MovimientoStock>(entity =>
            {
                entity.ToTable("MovimientosStock");
                entity.HasKey(m => m.Id);
                entity.Property(m => m.Motivo).HasMaxLength(200);
                entity.Property(m => m.Referencia).HasMaxLength(50);

                entity.HasOne(m => m.Producto)
                    .WithMany()
                    .HasForeignKey(m => m.ProductoId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasIndex(m => m.ProductoId);
                entity.HasIndex(m => m.Fecha);
            });

            // Configuración de MetodoPago
            modelBuilder.Entity<MetodoPago>(entity =>
            {
                entity.ToTable("MetodosPago");
                entity.HasKey(m => m.Id);
                entity.Property(m => m.Id).ValueGeneratedOnAdd();
                entity.Property(m => m.Nombre).IsRequired().HasMaxLength(50);
            });

            // Configuración de Venta
            modelBuilder.Entity<Venta>(entity =>
            {
                entity.ToTable("Ventas");
                entity.HasKey(v => v.Id);
                entity.Property(v => v.Total).HasColumnType("decimal(10,2)");
                entity.Property(v => v.Observaciones).HasMaxLength(500);

                entity.HasOne(v => v.Propietario)
                    .WithMany()
                    .HasForeignKey(v => v.PropietarioId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(v => v.MetodoPago)
                    .WithMany()
                    .HasForeignKey(v => v.MetodoPagoId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(v => v.Sucursal)
                    .WithMany()
                    .HasForeignKey(v => v.SucursalId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasIndex(v => v.PropietarioId);
                entity.HasIndex(v => v.Fecha);
            });

            // Configuración de DetalleVenta
            modelBuilder.Entity<DetalleVenta>(entity =>
            {
                entity.ToTable("DetallesVenta");
                entity.HasKey(d => d.Id);
                entity.Property(d => d.Descripcion).IsRequired().HasMaxLength(200);
                entity.Property(d => d.PrecioUnitario).HasColumnType("decimal(10,2)");

                entity.HasOne<Venta>()
                    .WithMany(v => v.Detalles)
                    .HasForeignKey(d => d.VentaId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(d => d.Producto)
                    .WithMany()
                    .HasForeignKey(d => d.ProductoId)
                    .IsRequired(false)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasIndex(d => d.VentaId);
            });

            // Configuración de Factura
            modelBuilder.Entity<Factura>(entity =>
            {
                entity.ToTable("Facturas");
                entity.HasKey(f => f.Id);
                entity.Property(f => f.Numero).IsRequired().HasMaxLength(20);
                entity.Property(f => f.TipoFactura).IsRequired().HasMaxLength(1);
                entity.Property(f => f.SubTotal).HasColumnType("decimal(10,2)");
                entity.Property(f => f.IVA).HasColumnType("decimal(10,2)");
                entity.Property(f => f.Total).HasColumnType("decimal(10,2)");

                entity.HasOne(f => f.Venta)
                    .WithMany()
                    .HasForeignKey(f => f.VentaId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasIndex(f => f.Numero).IsUnique();
                entity.HasIndex(f => f.VentaId);
            });

            // Configuración de Rol
            modelBuilder.Entity<Rol>(entity =>
            {
                entity.ToTable("Roles");
                entity.HasKey(r => r.Id);
                entity.Property(r => r.Id).ValueGeneratedOnAdd();
                entity.Property(r => r.Nombre).IsRequired().HasMaxLength(50);
                entity.Property(r => r.Descripcion).HasMaxLength(200);
                entity.HasIndex(r => r.Nombre).IsUnique();
            });

            // Configuración de Usuario
            modelBuilder.Entity<Usuario>(entity =>
            {
                entity.ToTable("Usuarios");
                entity.HasKey(u => u.Id);
                entity.Property(u => u.NombreUsuario).IsRequired().HasMaxLength(50);
                entity.Property(u => u.Email).IsRequired().HasMaxLength(100);
                entity.Property(u => u.NombreCompleto).IsRequired().HasMaxLength(150);
                entity.Property(u => u.PasswordHash).IsRequired();
                entity.Property(u => u.PasswordSalt).IsRequired();
                entity.Property(u => u.FotoUrl).HasMaxLength(500000); // base64 photos
                entity.Property(u => u.VeterinarioId).HasMaxLength(50);

                entity.HasOne(u => u.Rol)
                    .WithMany()
                    .HasForeignKey(u => u.RolId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(u => u.Sucursal)
                    .WithMany()
                    .HasForeignKey(u => u.SucursalId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasIndex(u => u.NombreUsuario).IsUnique();
                entity.HasIndex(u => u.Email).IsUnique();
            });

            // Configuración de AuditLog
            modelBuilder.Entity<AuditLog>(entity =>
            {
                entity.ToTable("AuditLogs");
                entity.HasKey(a => a.Id);
                entity.Property(a => a.Accion).IsRequired().HasMaxLength(10);
                entity.Property(a => a.Entidad).IsRequired().HasMaxLength(100);
                entity.Property(a => a.Descripcion).HasMaxLength(500);
                entity.Property(a => a.NombreUsuario).HasMaxLength(50);
                entity.Property(a => a.IpOrigen).HasMaxLength(45);
                entity.HasIndex(a => a.Fecha);
                entity.HasIndex(a => a.UsuarioId);
                entity.HasIndex(a => a.Entidad);
            });

            // Configuración de Notificacion
            modelBuilder.Entity<Notificacion>(entity =>
            {
                entity.ToTable("Notificaciones");
                entity.HasKey(n => n.Id);
                entity.Property(n => n.Titulo).IsRequired().HasMaxLength(150);
                entity.Property(n => n.Mensaje).IsRequired().HasMaxLength(500);
                entity.HasIndex(n => n.FechaCreacion);
                entity.HasIndex(n => n.Leida);
                entity.HasIndex(n => n.Tipo);
            });

            // Configuración de ConfiguracionSistema
            modelBuilder.Entity<ConfiguracionSistema>(entity =>
            {
                entity.ToTable("Configuraciones");
                entity.HasKey(c => c.Id);
                entity.Property(c => c.Clave).IsRequired().HasMaxLength(100);
                entity.Property(c => c.Valor).IsRequired();
                entity.Property(c => c.Descripcion).HasMaxLength(300);
                entity.Property(c => c.Grupo).IsRequired().HasMaxLength(50);
                entity.HasIndex(c => c.Clave).IsUnique();
                entity.HasIndex(c => c.Grupo);
            });

            // Configuración de Sucursal
            modelBuilder.Entity<Sucursal>(entity =>
            {
                entity.ToTable("Sucursales");
                entity.HasKey(s => s.Id);
                entity.Property(s => s.Id).ValueGeneratedOnAdd();
                entity.Property(s => s.Nombre).IsRequired().HasMaxLength(100);
                entity.Property(s => s.Direccion).IsRequired().HasMaxLength(200);
                entity.Property(s => s.Telefono).IsRequired().HasMaxLength(50);
                entity.Property(s => s.Email).HasMaxLength(100);
            });
        }
    }
}
