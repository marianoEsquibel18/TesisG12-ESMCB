# Documentación Técnica del Sistema de Gestión Veterinaria

> Última actualización: 2026-04-16
> Rutas base del proyecto:
> - **Backend**: `Base/Backend/` (Solución `HybridDDDArchitecture.sln`)
> - **Frontend**: `Base/Frontend/BlazorFrontEnd/`

---

## 1. Arquitectura General

El sistema se divide en dos aplicaciones independientes que se comunican vía REST API (JSON sobre HTTPS):

| Componente | Tecnología | Puerto por defecto |
|---|---|---|
| **API Backend** | ASP.NET Core 9.0 (Web API) | `https://localhost:7204` |
| **Frontend** | Blazor Server (InteractiveServer) | `http://localhost:5062` |
| **Base de Datos** | SQLite (`VeterinariaDB.sqlite`) | Archivo local en Template-API/ |

### 1.1 Relación Frontend ↔ Backend
- El Frontend registra un `HttpClient` con `BaseAddress = https://localhost:7204/`.
- Todas las llamadas HTTP pasan por un `JwtAuthorizationMessageHandler` (DelegatingHandler) que inyecta el token JWT en el header `Authorization: Bearer {token}`.
- El token se almacena en `localStorage` del navegador y se sincroniza a un `TokenStorageService` en memoria (porque los DelegatingHandlers de Blazor Server no pueden usar JS Interop).
- El `HttpClient` se registra como **Scoped** (un HttpClient por circuito Blazor), NO como Singleton.

---

## 2. Backend — Estructura de Capas

```
Backend/
├── Core/                        # Framework base (Domain-Driven Design)
│   ├── Core.Domain/             # DomainEntity<TId, TValidator> base class
│   └── Core.Application/       # QueryResult<T>, BaseController, IEventBus
├── Domain/                      # Entidades y validadores del negocio
│   ├── Entities/                # 26 archivos de entidades
│   └── Validators/              # 23 validadores FluentValidation
├── Application/                 # Capa de aplicación
│   ├── DataTransferObjects/     # 18 DTOs
│   ├── Repositories/            # 18 interfaces de repositorios
│   └── Registrations/           # DI registration
├── Infrastructure/              # Implementaciones concretas
│   └── Repositories/Sql/       # EF Core + SQLite (StoreDbContext)
└── Template-API/                # Proyecto API ejecutable
    ├── Controllers/             # 34 controladores
    ├── Middleware/               # Performance, GlobalException, RateLimit, Audit
    ├── Startup.cs              # Configuración de servicios y pipeline
    ├── appsettings.json        # Config (JWT key, ConnectionStrings)
    └── VeterinariaDB.sqlite    # Base de datos SQLite
```

### 2.1 Patrones de Diseño del Backend
- **DDD Táctico**: Entidades con private setters, validación via FluentValidation integrada en la base class.
- **Herencia de entidades**: `DomainEntity<TId, TValidator>` — el `TId` puede ser `string` (GUID) o `int` (autoincremental).
- **Validación**: Cada entidad tiene su `{Entity}Validator` en `Domain/Validators/`. La propiedad `IsValid` ejecuta el validador y `GetErrors()` devuelve la lista.
- **Repositorios**: Interfaz en `Application/Repositories/`, implementación en `Infrastructure/Repositories/Sql/`.
- **Sin CQRS**: Los controllers hacen operaciones directas contra los repositorios (no hay MediatR, ni Commands/Queries separados).

---

## 3. Entidades del Dominio

### 3.1 Tabla completa de entidades

| Entidad | TId | Tabla DB | Descripción |
|---|---|---|---|
| **Paciente** | `string` (GUID) | `Pacientes` | Mascota/paciente de la clínica |
| **Propietario** | `string` (GUID) | `Propietarios` | Dueño de mascotas (cliente) |
| **Especie** | `int` | `Especies` | Canino, Felino, Ave, Roedor, etc. |
| **Raza** | `int` | `Razas` | Golden Retriever, Siamés, etc. (FK → Especie) |
| **Veterinario** | `string` (GUID) | `Veterinarios` | Profesional con matrícula |
| **Servicio** | `int` | `Servicios` | Consulta, Cirugía, Limpieza dental, etc. |
| **Turno** | `string` (GUID) | `Turnos` | Cita programada |
| **HistorialClinico** | `string` (GUID) | `HistorialClinico` | Entrada de consulta médica |
| **Tratamiento** | `string` (GUID) | `Tratamientos` | Tratamiento médico abierto/finalizado |
| **Vacuna** | `int` | `Vacunas` | Catálogo de vacunas (Antirrábica, etc.) |
| **RegistroVacunacion** | `string` (GUID) | `RegistrosVacunacion` | Aplicación de vacuna a un paciente |
| **Producto** | `string` (GUID) | `Productos` | Artículo del inventario |
| **Categoria** | `int` | `Categorias` | Categoría de producto |
| **Marca** | `int` | `Marcas` | Marca de producto |
| **Proveedor** | `string` (GUID) | `Proveedores` | Proveedor comercial (RazonSocial, CUIT) |
| **Deposito** | `int` | `Depositos` | Depósito/almacén físico |
| **MovimientoStock** | `string` (GUID) | `MovimientosStock` | Entrada/Salida/Ajuste/Devolución |
| **Venta** | `string` (GUID) | `Ventas` | Venta con estado (Pendiente/Confirmada/Anulada) |
| **DetalleVenta** | `string` (GUID) | `DetallesVenta` | Línea de producto dentro de una venta |
| **MetodoPago** | `int` | `MetodosPago` | Efectivo, Tarjeta, Transferencia |
| **Factura** | `string` (GUID) | `Facturas` | Factura fiscal A/B/C asociada a venta |
| **Usuario** | `string` (GUID) | `Usuarios` | Cuenta del sistema (con hash SHA512) |
| **Rol** | `int` | `Roles` | Admin, Gerente, Veterinario, Recepcionista |
| **Sucursal** | `int` | `Sucursales` | Filial o sucursal física de la veterinaria |
| **ConfiguracionSistema** | `int` | `Configuraciones` | Pares clave/valor del sistema |

### 3.2 Relaciones clave (Foreign Keys)

```
Propietario 1──N Paciente (PropietarioId)
Especie     1──N Raza (EspecieId)
Especie     1──N Paciente (EspecieId)
Raza        1──N Paciente (RazaId, nullable)
Paciente    1──N HistorialClinico (PacienteId)
Paciente    1──N Tratamiento (PacienteId)
Paciente    1──N RegistroVacunacion (PacienteId)
Paciente    1──N Turno (PacienteId)
Veterinario 1──N Turno (VeterinarioId)
Servicio    1──N Turno (ServicioId)
Vacuna      1──N RegistroVacunacion (VacunaId)
Categoria   1──N Producto (CategoriaId)
Marca       1──N Producto (MarcaId, nullable)
Proveedor   1──N Producto (ProveedorId, nullable)
Deposito    1──N Producto (DepositoId, nullable)
Producto    1──N DetalleVenta (ProductoId)
Producto    1──N MovimientoStock (ProductoId)
Venta       1──N DetalleVenta (VentaId)
Propietario 1──N Venta (PropietarioId, nullable)
MetodoPago  1──N Venta (MetodoPagoId)
Venta       1──1 Factura (VentaId)
Rol         1──N Usuario (RolId)
Sucursal    1──N Venta (SucursalId)
Sucursal    1──N Veterinario (SucursalId)
Sucursal    1──N Usuario (SucursalId, nullable)
Sucursal    1──N Turno (SucursalId)
Sucursal    1──N Deposito (SucursalId)
```

### 3.3 Enums

| Enum | Valores |
|---|---|
| `EstadoVenta` | `Pendiente=0, Confirmada=1, Anulada=2` |
| `EstadoTurno` | `Programado=0, Confirmado=1, EnCurso=2, Completado=3, Cancelado=4, Ausente=5, Reprogramado=6` |
| `TipoMovimiento` | `Entrada=0, Salida=1, Ajuste=2, Devolucion=3` |

### 3.4 Propiedades calculadas

| Entidad | Propiedad | Cálculo |
|---|---|---|
| `Paciente` | `EdadEnAnios` | Calculada desde `FechaNacimiento` (puede ser null) |
| `Paciente.Sexo` | — | Se guarda como `"M"`, `"H"`, `"D"`. El frontend muestra "Macho"/"Hembra"/"Desconocido" |
| `Propietario` | `NombreCompleto` | `$"{Nombre} {Apellido}"` |
| `Veterinario` | `NombreCompleto` | `$"{Nombre} {Apellido}"` |
| `Turno` | `FechaHoraFin` | `FechaHora.AddMinutes(DuracionMinutos)` |
| `Producto` | `StockBajo` | `StockActual <= StockMinimo` |
| `DetalleVenta` | `Subtotal` | `Cantidad * PrecioUnitario` |

---

## 4. API Endpoints (Controllers)

Todos los endpoints usan prefijo `api/v1/`. La respuesta JSON sigue el patrón:
```json
{ "data": <payload>, "errors": [], "count": N, "pageIndex": 1, "pageSize": 10 }
```

### 4.1 Autenticación (`AuthController`)
| Método | Endpoint | Descripción |
|---|---|---|
| POST | `/api/v1/auth/login` | Login => devuelve JWT token |
| POST | `/api/v1/auth/register` | Crear usuario (requiere rol Admin) |
| GET | `/api/v1/auth/usuarios` | Lista de usuarios (requiere Auth) |
| GET | `/api/v1/auth/roles` | Lista de roles |
| PUT | `/api/v1/auth/usuarios/{id}` | Actualizar usuario |

**JWT Config** (appsettings.json):
- Key: `VeterinariaSecretKeyMuyLargaParaDesarrollo2024!@#$`
- Issuer: `VeterinariaAPI`
- Audience: `VeterinariaApp`
- Claims: `name` (username), `role` (nombre del rol)

**Usuario admin por defecto** (cargado por SeedController):
- Username: `admin` / Password: `Admin123!` / Rol: Admin

### 4.2 Pacientes (`PacienteController` + `PaginacionController`)
| Método | Endpoint | Descripción |
|---|---|---|
| GET | `/api/v1/Paciente` | Paginado con búsqueda (?page, ?pageSize, ?search) |
| GET | `/api/v1/Paciente/{id}` | Detalle con Includes (Especie, Raza, Propietario) |
| POST | `/api/v1/Paciente` | Crear paciente |
| PUT | `/api/v1/Paciente` | Actualizar paciente |
| DELETE | `/api/v1/Paciente/{id}` | Soft-delete (Desactivar) |

### 4.3 Propietarios (`PropietarioController`)
| Método | Endpoint | Descripción |
|---|---|---|
| GET | `/api/v1/Propietario` | Paginado con búsqueda (?search por DNI/Apellido) |
| GET | `/api/v1/Propietario/{id}` | Detalle con count de mascotas |
| GET | `/api/v1/Propietario/activos` | Lista de activos (para combos) |
| POST | `/api/v1/Propietario` | Crear |
| PUT | `/api/v1/Propietario` | Actualizar (incluye DNI) |
| DELETE | `/api/v1/Propietario/{id}` | Soft-delete |

### 4.4 Inventario

#### Productos (`ProductoController`)
| Método | Endpoint | Descripción |
|---|---|---|
| GET | `/api/v1/Producto` | Paginado (?search, ?categoriaId) |
| GET | `/api/v1/Producto/{id}` | Detalle con Includes |
| POST | `/api/v1/Producto` | Crear producto |
| PUT | `/api/v1/Producto` | Actualizar |
| DELETE | `/api/v1/Producto/{id}` | Soft-delete |
| PUT | `/api/v1/Producto/{id}/ajustarStock` | Ajuste manual de stock |

#### Catálogos (`StockBaseControllers`)
**Categorías**: `GET/POST/PUT/DELETE /api/v1/Categoria`
**Marcas**: `GET/POST/PUT/DELETE /api/v1/Marca`
**Proveedores**: `GET/POST/PUT/DELETE /api/v1/Proveedor`
**Depósitos**: `GET/POST/PUT/DELETE /api/v1/Deposito`

> **IMPORTANTE**: Los proveedores usan Id tipo `string` (GUID). El resto de catálogos usa `int` auto-incremental.

### 4.5 Turnos/Agenda (`TurnoController`)
| Método | Endpoint | Descripción |
|---|---|---|
| GET | `/api/v1/Turno` | Paginado (?search, ?veterinarioId) |
| GET | `/api/v1/Turno/{id}` | Detalle con Includes |
| GET | `/api/v1/Turno/rango` | Por rango de fechas (?desde, ?hasta) — sin paginación |
| POST | `/api/v1/Turno` | Crear turno (valida solapamiento) |
| PUT | `/api/v1/Turno` | Actualizar (Reprogramar + cambiar datos) |
| PUT | `/api/v1/Turno/{id}/estado` | Cambiar estado (body: string del estado) |
| DELETE | `/api/v1/Turno/{id}` | Cancelar |

**Estados**: Programado → Confirmado → EnCurso → Completado. También: Cancelado, Ausente, Reprogramado.

### 4.6 Ventas (`VentaController`)
| Método | Endpoint | Descripción |
|---|---|---|
| GET | `/api/v1/Venta` | Por rango fechas (?desde, ?hasta) |
| GET | `/api/v1/Venta/{id}` | Detalle con DetallesVenta |
| POST | `/api/v1/Venta` | **Crear venta** (descuenta stock automáticamente) |
| PUT | `/api/v1/Venta/{id}/anular` | Anular (revierte stock) |
| POST | `/api/v1/Venta/{id}/facturar` | Generar factura A/B/C |

**Flujo de venta POST**:
1. Valida PropietarioId (opcional, puede ser null = Consumidor Final)
2. Valida MetodoPagoId (requerido)
3. Crea entidad `Venta` con `PropietarioId` nullable
4. Para cada detalle: valida producto, descuenta stock, crea MovimientoStock(Salida), crea DetalleVenta
5. Recalcula total, confirma la venta

**MetodoPago**: `GET/POST/PUT/DELETE /api/v1/MetodoPago`

### 4.7 Historial Clínico (`HistorialClinicoController`)
| Método | Endpoint | Descripción |
|---|---|---|
| GET | `/api/v1/HistorialClinico/paciente/{pacienteId}` | Historial del paciente |
| POST | `/api/v1/HistorialClinico` | Crear entrada |
| PUT | `/api/v1/HistorialClinico` | Actualizar |
| DELETE | `/api/v1/HistorialClinico/{id}` | Eliminar |

### 4.8 Tratamientos (`TratamientoController`)
| Método | Endpoint | Descripción |
|---|---|---|
| GET | `/api/v1/Tratamiento/paciente/{pacienteId}` | Tratamientos del paciente |
| POST | `/api/v1/Tratamiento` | Crear |
| PUT | `/api/v1/Tratamiento` | Actualizar |
| PUT | `/api/v1/Tratamiento/{id}/finalizar` | Marcar como finalizado |

### 4.9 Vacunación (`VacunaController` + `RegistroVacunacionController`)
**Catálogo de Vacunas**: `GET/POST/PUT/DELETE /api/v1/Vacuna`
**Registros de Aplicación**: 
- `GET /api/v1/RegistroVacunacion/paciente/{pacienteId}` — historial del paciente
- `POST /api/v1/RegistroVacunacion` — registrar aplicación

### 4.10 Veterinarios (`VeterinarioController`)
| Método | Endpoint | Descripción |
|---|---|---|
| GET | `/api/v1/Veterinario` | Paginado (?soloActivos) |
| POST | `/api/v1/Veterinario` | Crear |
| PUT | `/api/v1/Veterinario` | Actualizar |
| DELETE | `/api/v1/Veterinario/{id}` | Soft-delete |

### 4.11 Sucursales (`SucursalController`)
| Método | Endpoint | Descripción |
|---|---|---|
| GET | `/api/v1/Sucursal` | Obtener todas las sucursales (?soloActivos) |
| GET | `/api/v1/Sucursal/{id}` | Obtener sucursal por ID |
| POST | `/api/v1/Sucursal` | Crear nueva sucursal (Admin) |
| PUT | `/api/v1/Sucursal` | Actualizar sucursal (Admin) |
| DELETE | `/api/v1/Sucursal/{id}` | Desactivar sucursal (Admin) |

### 4.12 Catálogos Clínicos
**Especies**: `GET/POST/PUT/DELETE /api/v1/Especie`
- `GET /api/v1/Especie` incluye count de pacientes por especie
- Las razas se filtran por especie: `GET /api/v1/Raza?especieId=X`

**Razas**: `GET/POST/PUT/DELETE /api/v1/Raza`
**Servicios**: `GET/POST/PUT/DELETE /api/v1/Servicio`

### 4.13 Dashboard y Reportes
**Dashboard** (`EstadisticasController`):
- `GET /api/v1/Estadisticas/kpis` — KPIs principales (ingresos, turnos, pacientes, stock)
- `GET /api/v1/Estadisticas/alertas` — Alertas de stock bajo
- `GET /api/v1/Estadisticas/ingresos?dias=7` — Ingresos diarios para gráfico

**Reportes** (`ReporteController`):
- `GET /api/v1/Reporte/ventas` — Resumen comercial (30 días)
- `GET /api/v1/Reporte/stock` — Valorización de inventario
- `GET /api/v1/Reporte/turnos` — Rendimiento operativo
- `GET /api/v1/Reporte/clinico` — Censo poblacional
- `GET /api/v1/Reporte/tratamientos` — Historial de tratamientos (filtros por paciente, dueño y veterinario)

**Exportación CSV** (`ExportController`):
- `GET /api/v1/Export/ventas` — CSV de ventas
- `GET /api/v1/Export/productos` — Maestro de productos
- `GET /api/v1/Export/stockBajo` — Solo faltantes
- `GET /api/v1/Export/turnos` — Agenda completa
- `GET /api/v1/Export/pacientes` — Padrón de mascotas
- `GET /api/v1/Export/propietarios` — Padrón de dueños
- `GET /api/v1/Export/historial/{pacienteId}` — Historial individual

### 4.14 Otros Controllers
| Controller | Función |
|---|---|
| `SeedController` | `POST /api/v1/Seed/all` — Carga datos de prueba completos |
| `BackupController` | Backup/Restore de la base SQLite |
| `ConfiguracionController` | CRUD de configuraciones del sistema |
| `SearchController` | Búsqueda transversal |
| `IntegridadController` | Verificación de integridad de datos |
| `AuditController` | Auditoría de operaciones |
| `RecordatorioController` | Sistema de recordatorios/notificaciones |
| `SelfTestController` | Health checks |
| `DocumentacionController` | Documentación generada dinámicamente |

---

## 5. Frontend — Estructura

```
BlazorFrontEnd/
├── Auth/
│   ├── CustomAuthenticationStateProvider.cs  # Lee JWT, parsea claims
│   ├── JwtAuthorizationMessageHandler.cs    # DelegatingHandler → inyecta Bearer token
│   └── TokenStorageService.cs               # In-memory token store (Scoped)
├── Components/
│   ├── Layout/
│   │   ├── MainLayout.razor      # Layout principal (sidebar + topbar + content)
│   │   ├── MainLayout.razor.css  # CSS scoped del layout
│   │   ├── NavMenu.razor         # Menú de navegación lateral
│   │   └── NavMenu.razor.css     # CSS scoped del nav
│   └── Pages/
│       ├── Home.razor            # Dashboard (Centro de Comando)
│       ├── Login.razor           # Página de login
│       ├── Pacientes/            # Index.razor, Formulario.razor
│       ├── Propietarios/         # Index.razor, Formulario.razor
│       ├── Agenda/               # Index.razor (Lista/Diaria/Semanal/Mensual), Formulario.razor
│       ├── Historial/            # Index.razor (ficha del paciente)
│       ├── Inventario/           # Index.razor (por módulo: productos, categorías, marcas, proveedores, depósitos)
│       ├── Ventas/               # Index.razor (historial), PuntoDeVenta.razor (POS)
│       ├── Reportes/             # Index.razor (4 tabs: Finanzas, Stock, Turnos, Clínica)
│       ├── Veterinarios/         # Index.razor, Formulario.razor
│       └── Usuarios/             # Index.razor, Formulario.razor (solo Admin)
├── Models/                       # DTOs del frontend (NO comparten proyecto con backend)
│   ├── AgendaModels.cs           # TurnoDto, VeterinarioDto, ServicioDto
│   ├── AuthModels.cs             # LoginRequest, LoginResponse
│   ├── DashboardModels.cs        # KpiDashboardDto, DashboardAlertasResponse
│   ├── HistorialModels.cs        # HistorialClinicoDto, TratamientoDto, VacunacionDto
│   ├── InventarioModels.cs       # CategoriaDto, MarcaDto, ProveedorDto, DepositoDto
│   ├── PacientesModels.cs        # PacienteDto, PacienteCreateRequest, PropietarioDto
│   ├── PaginatedList.cs          # PaginatedList<T> wrapper genérico
│   ├── ReporteModels.cs          # ReporteVentasDto, ReporteStockDto, etc.
│   ├── UsuarioModels.cs          # UsuarioDto, RegisterRequest
│   └── VentaModels.cs            # VentaDto, CreateVentaRequest, etc.
├── Services/                     # Servicios HTTP (cada uno inyecta HttpClient)
│   ├── AuthService.cs            # Login/Register
│   ├── PacienteService.cs        # CRUD Pacientes
│   ├── PropietarioService.cs     # CRUD Propietarios
│   ├── TurnoService.cs           # CRUD Turnos + GetRango
│   ├── ProductoService.cs        # CRUD Productos
│   ├── VentaService.cs           # CRUD Ventas + POS
│   ├── VeterinarioService.cs     # CRUD Veterinarios
│   ├── CatalogoService.cs        # Especies + Razas
│   ├── CatalogoInventarioService.cs  # Categorías, Marcas, Proveedores, Depósitos
│   ├── CatalogoServiciosService.cs   # Servicios veterinarios
│   ├── HistorialClinicoService.cs    # Historial + CRUD
│   ├── TratamientoService.cs     # Tratamientos
│   ├── VacunacionService.cs      # Vacunas + Registros
│   ├── UsuarioService.cs         # Gestión de usuarios (Admin)
│   ├── DashboardService.cs       # KPIs + Alertas
│   ├── ReporteService.cs         # Los 4 reportes + descarga CSV
│   ├── MetodoPagoService.cs      # Métodos de pago
│   └── PersonalService.cs        # Veterinarios (alias)
├── wwwroot/
│   └── app.css                   # Tema global (paleta light pág. 209)
└── Program.cs                    # Registro de DI, HttpClient, servicios
```

### 5.1 Registro de Servicios (Program.cs)

Todos los servicios se registran como **Scoped**. El HttpClient también es Scoped con un handler personalizado:

```csharp
builder.Services.AddScoped<HttpClient>(sp => {
    var tokenStorage = sp.GetRequiredService<TokenStorageService>();
    var handler = new JwtAuthorizationMessageHandler(tokenStorage) {
        InnerHandler = new HttpClientHandler {
            ServerCertificateCustomValidationCallback = (_, _, _, _) => true
        }
    };
    return new HttpClient(handler) {
        BaseAddress = new Uri("https://localhost:7204/")
    };
});
```

### 5.2 Rutas del Frontend

| Ruta | Componente | Descripción |
|---|---|---|
| `/login` | `Login.razor` | Login page (no usa MainLayout) |
| `/` | `Home.razor` | Dashboard principal |
| `/pacientes` | `Pacientes/Index.razor` | Lista de mascotas paginada |
| `/pacientes/nuevo` | `Pacientes/Formulario.razor` | Alta de mascota |
| `/pacientes/editar/{Id}` | `Pacientes/Formulario.razor` | Editar mascota |
| `/propietarios` | `Propietarios/Index.razor` | Lista de clientes paginada |
| `/propietarios/nuevo` | `Propietarios/Formulario.razor` | Alta de cliente |
| `/propietarios/editar/{Id}` | `Propietarios/Formulario.razor` | Editar cliente |
| `/agenda` | `Agenda/Index.razor` | Vista Lista/Diaria/Semanal/Mensual |
| `/agenda/nuevo` | `Agenda/Formulario.razor` | Agendar turno |
| `/agenda/editar/{Id}` | `Agenda/Formulario.razor` | Editar turno/dictamen |
| `/inventario/productos` | `Inventario/Index.razor` | Productos |
| `/inventario/categorias` | `Inventario/Categorias.razor` | Categorías CRUD |
| `/inventario/marcas` | `Inventario/Marcas.razor` | Marcas CRUD |
| `/inventario/proveedores` | `Inventario/Proveedores.razor` | Proveedores CRUD |
| `/inventario/proveedores/editar/{Id}` | `Inventario/EditarProveedor.razor` | Editar proveedor |
| `/inventario/depositos` | `Inventario/Depositos.razor` | Depósitos CRUD |
| `/ventas` | `Ventas/Index.razor` | Registro de ventas |
| `/ventas/pos` | `Ventas/PuntoDeVenta.razor` | Terminal POS |
| `/reportes` | `Reportes/Index.razor` | Centro de reportes (4 tabs) |
| `/veterinarios` | `Veterinarios/Index.razor` | Lista de veterinarios |
| `/veterinarios/nuevo` | `Veterinarios/Formulario.razor` | Alta veterinario |
| `/veterinarios/editar/{Id}` | `Veterinarios/Formulario.razor` | Editar veterinario |
| `/usuarios` | `Usuarios/Index.razor` | Admin: gestión de usuarios |
| `/usuarios/nuevo` | `Usuarios/Formulario.razor` | Admin: registrar usuario |

### 5.3 Patrón de Servicios Frontend

Todos los servicios siguen el mismo patrón. Ejemplo (`PacienteService.cs`):

```csharp
public class PacienteService(HttpClient http)
{
    private readonly HttpClient _http = http;

    public async Task<PaginatedList<PacienteDto>?> GetAllAsync(int page = 1, int size = 10, string search = "")
    {
        var response = await _http.GetFromJsonAsync<QueryResult<PacienteDto>>(
            $"api/v1/Paciente?page={page}&pageSize={size}&search={search}");
        // Mapea a PaginatedList<T>
        ...
    }
}
```

La respuesta de la API viene envuelta en `QueryResult<T>` (backend) y el frontend la mapea a `PaginatedList<T>`.

---

## 6. Tema Visual (CSS)

Definido en `wwwroot/app.css`. Paleta según documentación página 209:

| Variable CSS | Valor | Uso |
|---|---|---|
| `--bg-main` | `#FAFAFA` | Fondo general de la app |
| `--bg-panel` | `#FFFFFF` | Fondo de tarjetas/cards |
| `--bg-topbar` | `#00A36C` | Sidebar verde |
| `--bg-input` | `#F1F1F1` | Fondo de inputs |
| `--text-primary` | `#333333` | Texto principal |
| `--text-secondary` | `#666666` | Texto secundario |
| `--accent-primary` | `#00A36C` | Verde veterinaria |
| `--accent-btn` | `#FFD700` | Amarillo - botón de acción principal |
| `--accent-danger` | `#FF6B6B` | Rojo para alertas |
| `--accent-warning` | `#FFA500` | Naranja preventivo |
| `--border-color` | `#CCCCCC` | Bordes de tablas/cards |

**Clases reutilizables**:
- `.glass-card` — Card blanca con padding, border y shadow
- `.btn-primary-glass` — Botón amarillo de acción principal
- `.app-container`, `.sidebar`, `.main-content`, `.topbar`, `.content-area` — Layout grid

**Compatibilidad backward**: Variables alias como `--border-glass`, `--bg-glass` se mantienen para no romper estilos inline legacy.

---

## 7. Autenticación — Flujo Completo

1. **Login**: `Login.razor` → `AuthService.LoginAsync(alias, password)` → `POST /api/v1/auth/login`
2. La API devuelve `{ token: "eyJ..." }`.
3. El frontend guarda el token en `localStorage["authToken"]` y en `TokenStorageService` (in-memory).
4. `CustomAuthenticationStateProvider` parsea el JWT y expone los claims (`name`, `role`).
5. En cada request HTTP → `JwtAuthorizationMessageHandler` lee token desde `TokenStorageService` y lo agrega como `Authorization: Bearer`.
6. **Sincronización**: `MainLayout.OnAfterRenderAsync(firstRender)` lee `localStorage` y sincroniza a `TokenStorageService` (porque el handler no puede usar JS Interop).
7. **Logout**: Limpia `localStorage`, limpia `TokenStorageService`, navega a `/login`.

**Roles**: `Admin`, `Gerente`, `Veterinario`, `Recepcionista`. Solo `Admin` ve el menú completo "Administrar Usuarios" (con permisos de creación/edición/desactivación). El rol `Gerente` tiene acceso de solo lectura al listado de usuarios y al log de auditoría (logs de operaciones).

---

## 8. Gotchas y Problemas Conocidos

### 8.1 Tipos de ID inconsistentes
- Entidades con `int` Id: Especie, Raza, Servicio, Categoria, Marca, Deposito, MetodoPago, Rol, Vacuna, Sucursal, ConfiguracionSistema.
- Entidades con `string` (GUID) Id: Paciente, Propietario, Veterinario, Turno, Venta, DetalleVenta, Producto, Proveedor, MovimientoStock, Factura, HistorialClinico, Tratamiento, RegistroVacunacion, Usuario.
- **Cuidado**: Los DTOs del frontend deben matchear exactamente. Si el backend devuelve un `int` y el DTO del frontend dice `string`, el deserializador JSON falla con error tipo `"The JSON value could not be converted to System.String"`.

### 8.2 Frontend DTOs separados del Backend
Los modelos en `Frontend/Models/` son **independientes** de los DTOs en `Backend/Application/DataTransferObjects/`. Deben mantenerse sincronizados manualmente. Si se agrega una propiedad en el backend, hay que agregarla también en el frontend.

### 8.3 Foreign Keys nullable
- `Venta.PropietarioId` es **nullable** (`string?`). Si se envía un string vacío `""` en lugar de `null`, SQLite falla con `FOREIGN KEY constraint failed`. Siempre pasar `null` para "Consumidor Final".
- `Paciente.RazaId` es `int?` (nullable). OK.
- `Producto.MarcaId`, `Producto.ProveedorId`, `Producto.DepositoId` son nullable.

### 8.4 CSS Scoped vs Global
- `MainLayout.razor.css` y `NavMenu.razor.css` son CSS scoped (compilados en `BlazorFrontEnd.styles.css`).
- Cambios en estos archivos **requieren rebuild** del frontend para tomar efecto. Un simple hot-reload no siempre los refleja.
- El archivo `app.css` global sí se actualiza con hot-reload.
- Si un estilo inline tiene `!important` o `rgba()` con valores hardcodeados, el `app.css` puede no poder overridearlo. Hay que editar el componente directamente.

### 8.5 Blazor Server y JS Interop
- Los `DelegatingHandler` de `HttpClient` corren fuera del circuito Blazor y **no pueden** usar JS Interop.
- La lectura de `localStorage` solo funciona en `OnAfterRenderAsync` (no en `OnInitializedAsync`).
- Por eso existe `TokenStorageService` como puente: el componente Blazor lee localStorage → lo guarda en el servicio → el handler lee del servicio.

### 8.6 Paginación
- El backend usa `QueryResult<T>` que envuelve la data con paginación.
- El frontend usa `PaginatedList<T>` que tiene: `Items`, `PageIndex`, `TotalPages`, `TotalCount`, `HasPreviousPage`, `HasNextPage`.

### 8.7 Soft-Delete
- La mayoría de entidades usan **soft-delete** (`Activo = false`). No se eliminan físicamente de la base de datos.
- Excepción: `HistorialClinico` y `RegistroVacunacion` sí se eliminan físicamente.

---

## 9. Comandos de Desarrollo

### Backend
```bash
cd Base/Backend/Template-API
dotnet run                    # Inicia la API en https://localhost:7204
dotnet watch run              # Con hot-reload
```
Swagger disponible en: `https://localhost:7204/swagger`

### Frontend
```bash
cd Base/Frontend/BlazorFrontEnd
dotnet run                    # Inicia en http://localhost:5062
dotnet watch run              # Con hot-reload
```

### Seed de datos de prueba
Con la API corriendo, hacer POST a: `https://localhost:7204/api/v1/Seed/all`
Esto carga: especies, razas, servicios, vacunas, propietarios, pacientes, veterinarios, productos, categorías, marcas, proveedores, depósitos, métodos de pago, turnos, ventas, usuario admin, y datos adicionales.

---

## 10. Dependencias NuGet Clave

### Backend
- `Microsoft.EntityFrameworkCore.Sqlite` — ORM SQLite
- `FluentValidation` — Validación de entidades
- `AutoMapper` — Mapeo (usado mínimamente)
- `Microsoft.AspNetCore.Authentication.JwtBearer` — Auth JWT
- `Swashbuckle.AspNetCore` — Swagger/OpenAPI

### Frontend
- `Blazored.LocalStorage` — Acceso a localStorage desde Blazor
- `Microsoft.AspNetCore.Components.Authorization` — AuthorizeView, AuthenticationStateProvider
