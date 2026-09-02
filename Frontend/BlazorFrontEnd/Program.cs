using BlazorFrontEnd.Components;
using Blazored.LocalStorage;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

// Add HttpClient to interact with Backend API (created per-scope so the auth handler
// shares the same TokenStorageService instance as the components in the circuit)
builder.Services.AddScoped<HttpClient>(sp =>
{
    var tokenStorage = sp.GetRequiredService<BlazorFrontEnd.Auth.TokenStorageService>();
    var handler = new BlazorFrontEnd.Auth.JwtAuthorizationMessageHandler(tokenStorage)
    {
        InnerHandler = new HttpClientHandler
        {
            ServerCertificateCustomValidationCallback = (_, _, _, _) => true
        }
    };
    var config = sp.GetRequiredService<IConfiguration>();
    var env = sp.GetRequiredService<IWebHostEnvironment>();
    var defaultUrl = env.IsProduction() ? "https://veterinarianandubay.qd.je/" : "https://localhost:7204/";
    var baseUrl = config["ApiBaseUrl"] ?? defaultUrl;
    return new HttpClient(handler)
    {
        BaseAddress = new Uri(baseUrl)
    };
});

// Add LocalStorage for auth tokens
builder.Services.AddBlazoredLocalStorage();

// Add Authentication and Authorization
builder.Services.AddAuthentication();
builder.Services.AddCascadingAuthenticationState();
builder.Services.AddAuthorizationCore();
builder.Services.AddSingleton<Microsoft.AspNetCore.Authorization.IAuthorizationMiddlewareResultHandler, BlazorFrontEnd.Auth.BlazorAuthorizationMiddlewareResultHandler>();
builder.Services.AddScoped<Microsoft.AspNetCore.Components.Authorization.AuthenticationStateProvider, BlazorFrontEnd.Auth.CustomAuthenticationStateProvider>();
builder.Services.AddScoped<BlazorFrontEnd.Auth.TokenStorageService>();
builder.Services.AddScoped<BlazorFrontEnd.Services.AuthService>();

// Módulo de Pacientes
builder.Services.AddScoped<BlazorFrontEnd.Services.PropietarioService>();
builder.Services.AddScoped<BlazorFrontEnd.Services.PacienteService>();
builder.Services.AddScoped<BlazorFrontEnd.Services.CatalogoService>();

// Módulo de Agenda
builder.Services.AddScoped<BlazorFrontEnd.Services.TurnoService>();
builder.Services.AddScoped<BlazorFrontEnd.Services.PersonalService>();
builder.Services.AddScoped<BlazorFrontEnd.Services.CatalogoServiciosService>();

// Módulo Historial Clínico
builder.Services.AddScoped<BlazorFrontEnd.Services.HistorialClinicoService>();
builder.Services.AddScoped<BlazorFrontEnd.Services.TratamientoService>();
builder.Services.AddScoped<BlazorFrontEnd.Services.VacunacionService>();
builder.Services.AddScoped<BlazorFrontEnd.Services.VacunaService>();

// Módulo Inventario
builder.Services.AddScoped<BlazorFrontEnd.Services.ProductoService>();
builder.Services.AddScoped<BlazorFrontEnd.Services.CatalogoInventarioService>();

// Módulo de Ventas
builder.Services.AddScoped<BlazorFrontEnd.Services.VentaService>();
builder.Services.AddScoped<BlazorFrontEnd.Services.MetodoPagoService>();

// Módulo de Usuarios y Personal
builder.Services.AddScoped<BlazorFrontEnd.Services.UsuarioService>();
builder.Services.AddScoped<BlazorFrontEnd.Services.VeterinarioService>();
builder.Services.AddScoped<BlazorFrontEnd.Services.HorarioService>();
builder.Services.AddScoped<BlazorFrontEnd.Services.SucursalService>();

// Dashboard y Reportes
builder.Services.AddScoped<BlazorFrontEnd.Services.DashboardService>();
builder.Services.AddScoped<BlazorFrontEnd.Services.ReporteService>();

// Integraciones y Comunicaciones
builder.Services.AddScoped<BlazorFrontEnd.Services.WhatsAppService>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();


app.UseAntiforgery();

app.MapStaticAssets();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();
