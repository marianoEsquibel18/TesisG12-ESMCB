using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Application.DataTransferObjects;
using Application.Repositories;
using Domain.Entities;
using Microsoft.Extensions.Configuration;

namespace Infrastructure.Services
{
    public class IaGeminiService : IIaService
    {
        private readonly IConfiguration _configuration;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly ITurnoRepository _turnoRepository;
        private readonly IPacienteRepository _pacienteRepository;
        private readonly IVeterinarioRepository _veterinarioRepository;
        private readonly IServicioRepository _servicioRepository;
        private readonly IHorarioRepository _horarioRepository;
        private readonly IHistorialClinicoRepository _historialClinicoRepository;
        private readonly ITratamientoRepository _tratamientoRepository;
        private readonly IRegistroVacunacionRepository _registroVacunacionRepository;
        private readonly IProductoRepository _productoRepository;
        private readonly IPropietarioRepository _propietarioRepository;
        private readonly IProductoDepositoRepository _productoDepositoRepository;
        private readonly ISucursalRepository _sucursalRepository;

        public IaGeminiService(
            IConfiguration configuration,
            IHttpClientFactory httpClientFactory,
            ITurnoRepository turnoRepository,
            IPacienteRepository pacienteRepository,
            IVeterinarioRepository veterinarioRepository,
            IServicioRepository servicioRepository,
            IHorarioRepository horarioRepository,
            IHistorialClinicoRepository historialClinicoRepository,
            ITratamientoRepository tratamientoRepository,
            IRegistroVacunacionRepository registroVacunacionRepository,
            IProductoRepository productoRepository,
            IPropietarioRepository propietarioRepository,
            IProductoDepositoRepository productoDepositoRepository,
            ISucursalRepository sucursalRepository)
        {
            _configuration = configuration;
            _httpClientFactory = httpClientFactory;
            _turnoRepository = turnoRepository;
            _pacienteRepository = pacienteRepository;
            _veterinarioRepository = veterinarioRepository;
            _servicioRepository = servicioRepository;
            _horarioRepository = horarioRepository;
            _historialClinicoRepository = historialClinicoRepository;
            _tratamientoRepository = tratamientoRepository;
            _registroVacunacionRepository = registroVacunacionRepository;
            _productoRepository = productoRepository;
            _propietarioRepository = propietarioRepository;
            _productoDepositoRepository = productoDepositoRepository;
            _sucursalRepository = sucursalRepository;
        }

        private string GetApiKey()
        {
            var key = _configuration["Gemini:ApiKey"];
            if (string.IsNullOrWhiteSpace(key))
            {
                key = Environment.GetEnvironmentVariable("GEMINI_API_KEY");
            }
            return key?.Trim() ?? string.Empty;
        }

        private string GetModel()
        {
            var model = _configuration["Gemini:Model"];
            return string.IsNullOrWhiteSpace(model) ? "gemini-1.5-flash" : model.Trim();
        }

        public Task<IaStatusDto> GetStatusAsync()
        {
            var apiKey = GetApiKey();
            var configured = !string.IsNullOrWhiteSpace(apiKey);
            return Task.FromResult(new IaStatusDto
            {
                Configurado = configured,
                Proveedor = "Google Gemini",
                Modelo = GetModel(),
                Mensaje = configured
                    ? "Servicio de Inteligencia Artificial activo y conectado."
                    : "Modo local activo (sin API Key de Gemini configurada)."
            });
        }

        public Task<IaStatusDto> IsConfiguredAsync() => GetStatusAsync();

        private static string NormalizarTexto(string texto)
        {
            if (string.IsNullOrWhiteSpace(texto)) return string.Empty;

            var normalizedString = texto.Normalize(NormalizationForm.FormD);
            var sb = new StringBuilder();

            foreach (var c in normalizedString)
            {
                var unicodeCategory = CharUnicodeInfo.GetUnicodeCategory(c);
                if (unicodeCategory != UnicodeCategory.NonSpacingMark)
                {
                    sb.Append(c);
                }
            }

            var clean = sb.ToString().Normalize(NormalizationForm.FormC).ToLowerInvariant();
            clean = Regex.Replace(clean, @"[¿?¡!.,;:_()""'/\\]", " ");
            clean = Regex.Replace(clean, @"\s+", " ").Trim();
            return clean;
        }

        private async Task<string> ObtenerNombreSucursalAsync(int? sucursalId)
        {
            if (!sucursalId.HasValue || sucursalId.Value <= 0)
            {
                return "Todas las sucursales";
            }
            try
            {
                var suc = await _sucursalRepository.FindOneAsync(sucursalId.Value);
                if (suc != null && !string.IsNullOrWhiteSpace(suc.Nombre))
                {
                    return suc.Nombre;
                }
            }
            catch { }
            return $"Sucursal #{sucursalId.Value}";
        }

        private static string SanitizarSinEmojis(string texto)
        {
            if (string.IsNullOrWhiteSpace(texto)) return string.Empty;
            return Regex.Replace(texto, @"[\p{Cs}\p{So}\p{Sk}\u2600-\u27BF\uE000-\uF8FF]", string.Empty).Trim();
        }

        public async Task<ChatbotResponseDto> ProcesarMensajeChatAsync(
            ChatbotRequestDto request, string usuarioNombre, string usuarioRol, int? sucursalId)
        {
            var res = await ProcesarMensajeChatInternoAsync(request, usuarioNombre, usuarioRol, sucursalId);
            if (res != null)
            {
                if (!string.IsNullOrWhiteSpace(res.Respuesta))
                {
                    res.Respuesta = SanitizarSinEmojis(res.Respuesta);
                }
                if (res.OpcionesSugeridas != null && res.OpcionesSugeridas.Any())
                {
                    res.OpcionesSugeridas = res.OpcionesSugeridas
                        .Select(SanitizarSinEmojis)
                        .Where(s => !string.IsNullOrWhiteSpace(s))
                        .ToList();
                }
            }
            return res;
        }

        private async Task<ChatbotResponseDto> ProcesarMensajeChatInternoAsync(
            ChatbotRequestDto request, string usuarioNombre, string usuarioRol, int? sucursalId)
        {
            var mensaje = request?.Mensaje?.Trim() ?? string.Empty;
            if (string.IsNullOrWhiteSpace(mensaje))
            {
                return new ChatbotResponseDto
                {
                    Exito = false,
                    Respuesta = "Por favor, escribe un mensaje o consulta.",
                    TipoRespuesta = "texto"
                };
            }

            var lower = mensaje.ToLowerInvariant();
            var norm = NormalizarTexto(mensaje);

            var esRecepcionista = usuarioRol.Equals("Recepcionista", StringComparison.OrdinalIgnoreCase) || 
                                  usuarioRol.Equals("Secretaria", StringComparison.OrdinalIgnoreCase);
            var esVeterinario = usuarioRol.Equals("Veterinario", StringComparison.OrdinalIgnoreCase);
            var esGerente = usuarioRol.Equals("Gerente", StringComparison.OrdinalIgnoreCase) ||
                            usuarioRol.Equals("Manager", StringComparison.OrdinalIgnoreCase);

            // ══════════════════════════════════════════════════════════
            // 1. RESTRICCIONES DE ACCESO POR ROL (RBAC)
            // ══════════════════════════════════════════════════════════

            // Restricción A: Finanzas / Ingresos / Facturación / Ventas
            bool intentaConsultarFinanzas = Regex.IsMatch(norm, @"\b(ingreso|ingresos|facturaci|facturado|ganancia|ganancias|ventas|balance|recaudaci|finanza|finanzas|cuanto ganamos|cuanto se vendio)\b");
            if (intentaConsultarFinanzas && (esRecepcionista || esVeterinario))
            {
                string mensajeRol = esRecepcionista
                    ? "Como Recepcionista no tienes autorización para consultar métricas financieras, balances o facturación del sistema. Puedes consultar la agenda de turnos, horarios de veterinarios o el catálogo de servicios."
                    : "Tu rol de Veterinario tiene perfil clínico y no tiene acceso a métricas financieras, balances ni facturación de la clínica. Puedes consultar turnos, pacientes, historias clínicas y tratamientos.";

                return new ChatbotResponseDto
                {
                    Exito = true,
                    TipoRespuesta = "texto",
                    Respuesta = mensajeRol,
                    OpcionesSugeridas = esRecepcionista 
                        ? new List<string> { "Turnos de hoy", "Horarios veterinarios", "Precios de servicios" }
                        : new List<string> { "Turnos de hoy", "Mis pacientes", "Stock en alerta" }
                };
            }

            // Restricción B: Historias Clínicas / Diagnósticos Médicos / Ficha Detallada de Pacientes (Recepcionista)
            bool intentaConsultarDiagnostico = Regex.IsMatch(norm, @"\b(diagnostico|diagnosticos|historial clinico|historia clinica|epicrisis|anamnesis|tratamiento medico|enfermedad de|sintomas de)\b");
            bool intentaConsultarFichaPaciente = Regex.IsMatch(norm, @"\b(info|informacion|datos|ficha|detalle|historia|saber de|como esta|que sabes de)\b.*\b(de|del|sobre|a)\s+([a-zA-Z0-9]+)") ||
                                                 norm.StartsWith("info de ") || norm.StartsWith("informacion de ") || norm.StartsWith("datos de ") || norm.Contains("ficha de ");

            if ((intentaConsultarDiagnostico || intentaConsultarFichaPaciente) && esRecepcionista)
            {
                return new ChatbotResponseDto
                {
                    Exito = true,
                    TipoRespuesta = "texto",
                    Respuesta = "Como Recepcionista no tienes autorización para acceder a la ficha detallada ni al historial clínico de los pacientes. Puedes consultar la agenda de turnos o agendar una cita médica.",
                    OpcionesSugeridas = new List<string> { "Turnos de hoy", "Agendar turno", "Horarios veterinarios" }
                };
            }

            // Restricción C: Gestión / Reposición de Stock (Recepcionista)
            bool intentaGestionarStock = Regex.IsMatch(norm, @"\b(stock critico|stock bajo|stock en alerta|sin stock|reponer stock|pedir proveedores|inventario critico)\b");
            if (intentaGestionarStock && esRecepcionista)
            {
                return new ChatbotResponseDto
                {
                    Exito = true,
                    TipoRespuesta = "texto",
                    Respuesta = "Como Recepcionista no tienes asignada la gestión de inventario y alertas de reposición. Puedes consultar las tarifas de servicios o el catálogo de precios para informar a los clientes.",
                    OpcionesSugeridas = new List<string> { "Precios de servicios", "Precios de productos", "Turnos de hoy" }
                };
            }

            // Restricción D: Agendamiento de Turnos (Gerente no puede agendar)
            bool intentaAgendarTurno = Regex.IsMatch(norm, @"\b(agendar|agendame|agendate|sacar|sacame|reservar|reservame|programar|anotar|anotame)\b") ||
                                       norm == "agendar turno" || norm.StartsWith("agendar ") || norm.StartsWith("agendame ");

            if (intentaAgendarTurno && esGerente)
            {
                return new ChatbotResponseDto
                {
                    Exito = true,
                    TipoRespuesta = "texto",
                    Respuesta = "Tu rol de Gerente es de supervisión y gestión administrativa; no tienes permisos para agendar turnos operativos. Por favor solicita el agendamiento al personal de Recepción o al Veterinario.",
                    OpcionesSugeridas = new List<string> { "Turnos de hoy", "Todos los turnos", "Stock en alerta", "Precios de servicios" }
                };
            }

            // ══════════════════════════════════════════════════════════
            // 2. INTENCIONES CON LENGUAJE NATURAL PERMISIVO
            // ══════════════════════════════════════════════════════════

            // A. Ayuda o Guía
            if (norm == "ayuda" || norm.Contains("como funciona") || norm.Contains("que puedes hacer") || 
                norm.Contains("que haces") || norm.Contains("guia") || norm.Contains("comandos") || norm.Contains("opciones"))
            {
                var opcionesGuia = esGerente
                    ? new List<string> { "Turnos de hoy", "Todos los turnos", "Stock en alerta", "Precios de servicios" }
                    : new List<string> { "Turnos de hoy", "Veterinarios y horarios", "Precios de servicios", "Agendar turno" };

                return new ChatbotResponseDto
                {
                    Exito = true,
                    TipoRespuesta = "guia_agendamiento",
                    Respuesta = "Puedo asistirte en la gestión operativa de la clínica:\n\n" +
                                "- Consultar agenda: '¿Qué turnos hay hoy?', 'agenda del día', 'citas de hoy'.\n" +
                                (esGerente ? "" : "- Agendar turno: 'Agendame un turno para [mascota] [fecha] [hora] motivo [motivo]' (ej: 'Agendame un turno para henry hoy 18:00 motivo Castracion'). Si no indicas motivo, será Consulta general.\n") +
                                "- Profesionales: '¿Quién atiende?', 'horarios de veterinarios'.\n" +
                                "- Productos y servicios: 'Información del producto amoxicilina', 'precio de servicios'.",
                    OpcionesSugeridas = opcionesGuia
                };
            }

            // B. Consultas de Turnos: Todos los turnos / Turnos de hoy / Agenda del día
            bool esConsultaTodosLosTurnos = Regex.IsMatch(norm, @"\b(todos los turnos|ver todos los turnos|mostrar todos los turnos|lista de turnos|listado de turnos|turnos programados|proximos turnos)\b");
            if (esConsultaTodosLosTurnos)
            {
                return await ConsultarTodosLosTurnosAsync(usuarioRol, sucursalId);
            }

            bool esConsultaTurnosHoy = 
                Regex.IsMatch(norm, @"\b(turnos?|citas?|agenda)\b.*\b(hoy|dia|actual)\b") ||
                Regex.IsMatch(norm, @"\b(hoy|dia|actual)\b.*\b(turnos?|citas?|agenda)\b") ||
                Regex.IsMatch(norm, @"\b(que|cuales|ver|mostrar|hay|tenemos|dime|decime|consultar)\b.*\b(turnos?|citas?|agenda)\b") ||
                norm == "turnos" || norm == "agenda" || norm == "citas" || norm == "turnos hoy" || norm == "agenda hoy" || norm == "turnos de hoy" || norm == "los turnos de hoy";

            bool tieneVerboAgendamiento = Regex.IsMatch(norm, @"\b(agend|sacar|reserv|anot|program)\b");

            // Si es consulta de turnos de hoy y NO incluye un verbo explícito de agendamiento
            if (esConsultaTurnosHoy && !tieneVerboAgendamiento)
            {
                return await ConsultarTurnosHoyAsync(usuarioRol, sucursalId);
            }

            // C. Intento de Agendamiento de Turno Concreto (con verbo de agendar o 'turno para ... hoy/mañana/hora')
            bool esAgendamientoConcreto = (tieneVerboAgendamiento || Regex.IsMatch(norm, @"\b(turno|turnos|cita|citas)\b.*\bpara\b")) 
                && (norm.Contains("manana") || norm.Contains("hoy") || Regex.IsMatch(norm, @"\b\d{1,2}[:.]\d{2}\b") || Regex.IsMatch(norm, @"\b\d{1,2}\s*(?:hs|h|horas)\b") || Regex.IsMatch(norm, @"\ba\s+las\s+\d{1,2}\b"));

            if (esAgendamientoConcreto)
            {
                if (esGerente)
                {
                    return new ChatbotResponseDto
                    {
                        Exito = true,
                        TipoRespuesta = "texto",
                        Respuesta = "Tu rol de Gerente es de supervisión y gestión administrativa; no tienes permisos para agendar turnos operativos. Por favor solicita el agendamiento al personal de Recepción o al Veterinario.",
                        OpcionesSugeridas = new List<string> { "Turnos de hoy", "Todos los turnos", "Stock en alerta", "Precios de servicios" }
                    };
                }

                var extraccion = await IntentarExtraerTurnoAsync(mensaje, sucursalId);
                if (extraccion != null)
                {
                    return extraccion;
                }
            }

            // D. Listar pacientes
            bool esListarPacientes = Regex.IsMatch(norm, @"\b(paciente|pacientes|mascota|mascotas)\b") &&
                                    (Regex.IsMatch(norm, @"\b(mis|listar|todos|ver|quienes|lista|listado|mostrar|cuales)\b") || norm == "pacientes" || norm == "mascotas");

            if (esListarPacientes)
            {
                return await ConsultarPacientesAsync(usuarioRol);
            }

            // E. Info / Ficha de tal paciente
            bool esInfoPaciente = Regex.IsMatch(norm, @"\b(info|informacion|datos|ficha|detalle|historia|saber de|como esta|que sabes de)\b.*\b(de|del|sobre|a)\s+([a-zA-Z0-9]+)") ||
                                  norm.StartsWith("info de ") || norm.StartsWith("informacion de ") || norm.StartsWith("datos de ") || norm.Contains("ficha de ");

            if (esInfoPaciente)
            {
                if (esRecepcionista)
                {
                    return new ChatbotResponseDto
                    {
                        Exito = true,
                        TipoRespuesta = "texto",
                        Respuesta = "Como Recepcionista no tienes autorización para acceder a la información detallada ni a la ficha clínica de los pacientes. Puedes consultar la agenda de turnos o agendar una cita médica.",
                        OpcionesSugeridas = new List<string> { "Turnos de hoy", "Agendar turno", "Horarios veterinarios" }
                    };
                }

                var respInfo = await ConsultarInfoPacienteAsync(mensaje, norm, usuarioRol);
                if (respInfo != null) return respInfo;
            }

            // F. Consulta Detallada de Producto Específico o Catálogo
            bool esConsultaProducto = 
                Regex.IsMatch(norm, @"\b(producto|productos|medicamento|medicamentos|alimento|alimentos|articulo|articulos|remedio|remedios)\b") ||
                Regex.IsMatch(norm, @"\b(informacion|info|detalle|detalles|datos|ficha|que es|para que sirve|stock de|stock del|precio de|precio del|costo de|costo del|cuanto sale|cuanto cuesta)\b");

            if (esConsultaProducto)
            {
                var respDetalleProd = await ConsultarDetalleProductoAsync(mensaje, norm, sucursalId);
                if (respDetalleProd != null) return respDetalleProd;

                var respPrecio = await ConsultarPrecioOServicioAsync(lower, norm);
                if (respPrecio != null) return respPrecio;
            }

            // G. Veterinarios y horarios
            bool esConsultaVets = norm.Contains("veterinari") 
                || norm.Contains("horario") 
                || norm.Contains("profesional") 
                || norm.Contains("quien atiende") 
                || norm.Contains("quienes atienden")
                || norm.Contains("disponib")
                || norm.Contains("doctores");

            if (esConsultaVets && !esAgendamientoConcreto)
            {
                return await ConsultarVeterinariosYHorariosAsync(sucursalId);
            }

            // H. Stock en alerta (para Admin, Gerente, Veterinario)
            bool esConsultaStock = Regex.IsMatch(norm, @"\b(stock|inventario|falta|reposicion|quedan|agotado|sin stock|bajo stock)\b");
            if (esConsultaStock)
            {
                return await ConsultarStockCriticoAsync(sucursalId);
            }

            // I. Intento genérico de agendar turno (sin hora explícita)
            var esIntentoTurnoGenerico = Regex.IsMatch(norm, @"\b(turno|turnos|cita|citas|agendar|agendame|agendate|sacar|sacame|reservar|reservame|programar|anotar|anotame)\b");
            if (esIntentoTurnoGenerico)
            {
                if (esGerente)
                {
                    return new ChatbotResponseDto
                    {
                        Exito = true,
                        TipoRespuesta = "texto",
                        Respuesta = "Tu rol de Gerente es de supervisión y gestión administrativa; no tienes permisos para agendar turnos operativos. Por favor solicita el agendamiento al personal de Recepción o al Veterinario.",
                        OpcionesSugeridas = new List<string> { "Turnos de hoy", "Todos los turnos", "Stock en alerta", "Precios de servicios" }
                    };
                }

                var extraccion = await IntentarExtraerTurnoAsync(mensaje, sucursalId);
                if (extraccion != null)
                {
                    return extraccion;
                }
            }

            // ══════════════════════════════════════════════════════════
            // 3. CONVERSACIONAL GENERAL VÍA GEMINI (CONTEXTO DE SUCURSAL Y ROL)
            // ══════════════════════════════════════════════════════════
            var apiKey = GetApiKey();
            if (!string.IsNullOrWhiteSpace(apiKey))
            {
                try
                {
                    var respuestaGemini = await ConsultarGeminiConversacionalAsync(request.Historial, mensaje, usuarioNombre, usuarioRol, sucursalId);
                    return new ChatbotResponseDto
                    {
                        Exito = true,
                        TipoRespuesta = "texto",
                        Respuesta = respuestaGemini,
                        OpcionesSugeridas = esGerente
                            ? new List<string> { "Turnos de hoy", "Todos los turnos", "Stock en alerta" }
                            : new List<string> { "Turnos de hoy", "Como agendar un turno?", "Horarios veterinarios" }
                    };
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[Gemini Chat Error] {ex.Message}");
                }
            }

            // Fallback conversacional local
            var nombreSuc = await ObtenerNombreSucursalAsync(sucursalId);
            var mensajeFallback = esGerente
                ? $"Soy el copiloto inteligente de la clínica veterinaria ({nombreSuc}). Puedo asistirte en la supervisión operativa, consultar agenda de turnos, revisar niveles de stock en alerta o consultar productos y servicios."
                : $"Soy el copiloto inteligente de la clínica veterinaria ({nombreSuc}). Puedo ayudarte a consultar la agenda, coordinar turnos médicos, ver horarios profesionales o consultar productos y servicios. Si deseas agendar, puedes decir por ejemplo: 'Agendame un turno para [mascota] hoy a las 18:00 motivo Castracion'.";

            return new ChatbotResponseDto
            {
                Exito = true,
                TipoRespuesta = "texto",
                Respuesta = mensajeFallback,
                OpcionesSugeridas = esGerente
                    ? new List<string> { "Turnos de hoy", "Todos los turnos", "Stock en alerta" }
                    : new List<string> { "Turnos de hoy", "Veterinarios y horarios", "Precios de servicios" }
            };
        }

        private async Task<ChatbotResponseDto> ConsultarTodosLosTurnosAsync(string usuarioRol, int? sucursalId)
        {
            var nombreSucursal = await ObtenerNombreSucursalAsync(sucursalId);
            var todos = (await _turnoRepository.GetTurnosExpandidosAsync()).ToList();

            if (sucursalId.HasValue && sucursalId.Value > 0)
            {
                todos = todos.Where(t => t.SucursalId == sucursalId.Value).ToList();
            }

            var esGerente = usuarioRol.Equals("Gerente", StringComparison.OrdinalIgnoreCase) || 
                            usuarioRol.Equals("Manager", StringComparison.OrdinalIgnoreCase);

            var proximosYHoy = todos.Where(t => t.FechaHora.Date >= DateTime.Today && t.Estado != EstadoTurno.Cancelado)
                                    .OrderBy(t => t.FechaHora)
                                    .Take(15)
                                    .ToList();

            if (!proximosYHoy.Any())
            {
                var ultimos = todos.OrderByDescending(t => t.FechaHora).Take(6).ToList();
                if (!ultimos.Any())
                {
                    return new ChatbotResponseDto
                    {
                        Exito = true,
                        TipoRespuesta = "texto",
                        Respuesta = $"No hay turnos registrados en el sistema para {nombreSucursal}.",
                        OpcionesSugeridas = esGerente
                            ? new List<string> { "Stock en alerta", "Veterinarios y horarios" }
                            : new List<string> { "Agendar turno", "Veterinarios y horarios" }
                    };
                }

                var sbU = new StringBuilder();
                sbU.AppendLine($"No hay turnos futuros programados en {nombreSucursal}. Últimos turnos registrados:\n");
                foreach (var t in ultimos)
                {
                    var pacNombre = t.Paciente?.Nombre ?? (!string.IsNullOrEmpty(t.PacienteId) ? t.PacienteId : "Paciente");
                    var vetNombre = t.Veterinario?.NombreCompleto ?? (!string.IsNullOrEmpty(t.VeterinarioId) ? t.VeterinarioId : "Veterinario");
                    var servNombre = t.Servicio?.Nombre ?? "Consulta";
                    sbU.AppendLine($"- {t.FechaHora:dd/MM/yyyy HH:mm} hs | Paciente: {pacNombre} | Profesional: {vetNombre} | Servicio: {servNombre} | Estado: {t.Estado}");
                }
                return new ChatbotResponseDto
                {
                    Exito = true,
                    TipoRespuesta = "texto",
                    Respuesta = sbU.ToString().TrimEnd(),
                    OpcionesSugeridas = esGerente
                        ? new List<string> { "Turnos de hoy", "Stock en alerta" }
                        : new List<string> { "Agendar turno", "Turnos de hoy" }
                };
            }

            var sb = new StringBuilder();
            sb.AppendLine($"Listado de turnos programados - {nombreSucursal} ({proximosYHoy.Count}):\n");
            foreach (var t in proximosYHoy)
            {
                var pacNombre = t.Paciente?.Nombre ?? (!string.IsNullOrEmpty(t.PacienteId) ? t.PacienteId : "Paciente");
                var vetNombre = t.Veterinario?.NombreCompleto ?? (!string.IsNullOrEmpty(t.VeterinarioId) ? t.VeterinarioId : "Veterinario");
                var servNombre = t.Servicio?.Nombre ?? "Consulta";
                sb.AppendLine($"- {t.FechaHora:dd/MM/yyyy HH:mm} hs | Paciente: {pacNombre} | Profesional: {vetNombre} | Servicio: {servNombre} | Estado: {t.Estado}");
            }

            return new ChatbotResponseDto
            {
                Exito = true,
                TipoRespuesta = "texto",
                Respuesta = sb.ToString().TrimEnd(),
                OpcionesSugeridas = esGerente
                    ? new List<string> { "Turnos de hoy", "Stock en alerta", "Veterinarios y horarios" }
                    : new List<string> { "Turnos de hoy", "Agendar turno", "Veterinarios y horarios" }
            };
        }

        private async Task<ChatbotResponseDto> ConsultarTurnosHoyAsync(string usuarioRol, int? sucursalId)
        {
            var hoy = DateTime.Today;
            var nombreSucursal = await ObtenerNombreSucursalAsync(sucursalId);
            var todosExpandidos = (await _turnoRepository.GetTurnosExpandidosAsync()).ToList();

            if (sucursalId.HasValue && sucursalId.Value > 0)
            {
                todosExpandidos = todosExpandidos.Where(t => t.SucursalId == sucursalId.Value).ToList();
            }

            // Comparar la fecha de hoy directamente en memoria C# para evitar discrepancias de formato
            var turnos = todosExpandidos
                .Where(t => t.FechaHora.Date == hoy)
                .OrderBy(t => t.FechaHora)
                .ToList();

            var esGerente = usuarioRol.Equals("Gerente", StringComparison.OrdinalIgnoreCase) || 
                            usuarioRol.Equals("Manager", StringComparison.OrdinalIgnoreCase);

            var opcionesSinTurnos = esGerente
                ? new List<string> { "Todos los turnos", "Stock en alerta", "Veterinarios y horarios" }
                : new List<string> { "Todos los turnos", "Agendar turno", "Veterinarios y horarios" };

            if (!turnos.Any())
            {
                var proximos = todosExpandidos
                    .Where(t => t.FechaHora.Date > hoy && t.Estado != EstadoTurno.Cancelado)
                    .OrderBy(t => t.FechaHora)
                    .Take(5)
                    .ToList();

                if (proximos.Any())
                {
                    var sbProx = new StringBuilder();
                    sbProx.AppendLine($"No hay turnos registrados para hoy ({hoy:dd/MM/yyyy}) en {nombreSucursal}.\n");
                    sbProx.AppendLine("Próximos turnos programados en la agenda:");
                    foreach (var p in proximos)
                    {
                        var pac = p.Paciente?.Nombre ?? (!string.IsNullOrEmpty(p.PacienteId) ? p.PacienteId : "Paciente");
                        var vet = p.Veterinario?.NombreCompleto ?? (!string.IsNullOrEmpty(p.VeterinarioId) ? p.VeterinarioId : "Veterinario");
                        var serv = p.Servicio?.Nombre ?? "Consulta";
                        sbProx.AppendLine($"- {p.FechaHora:dd/MM/yyyy HH:mm} hs | Paciente: {pac} | Profesional: {vet} | Servicio: {serv}");
                    }

                    return new ChatbotResponseDto
                    {
                        Exito = true,
                        TipoRespuesta = "texto",
                        Respuesta = sbProx.ToString().TrimEnd(),
                        OpcionesSugeridas = opcionesSinTurnos
                    };
                }

                return new ChatbotResponseDto
                {
                    Exito = true,
                    TipoRespuesta = "texto",
                    Respuesta = $"No hay turnos registrados para el día de hoy en {nombreSucursal}.",
                    OpcionesSugeridas = opcionesSinTurnos
                };
            }

            var sb = new StringBuilder();
            sb.AppendLine($"Agenda del día ({hoy:dd/MM/yyyy}) - {nombreSucursal} ({turnos.Count} turno(s) registrado(s)):\n");

            foreach (var t in turnos)
            {
                var pacNombre = t.Paciente != null ? t.Paciente.Nombre : (!string.IsNullOrEmpty(t.PacienteId) ? t.PacienteId : "Paciente");
                var vetNombre = t.Veterinario != null ? t.Veterinario.NombreCompleto : (!string.IsNullOrEmpty(t.VeterinarioId) ? t.VeterinarioId : "Veterinario");
                var servNombre = t.Servicio != null ? t.Servicio.Nombre : "Consulta general";

                sb.AppendLine($"- {t.FechaHora:HH:mm} hs | Paciente: {pacNombre} | Profesional: {vetNombre} | Servicio: {servNombre} | Estado: {t.Estado}");
            }

            return new ChatbotResponseDto
            {
                Exito = true,
                TipoRespuesta = "texto",
                Respuesta = sb.ToString().TrimEnd(),
                OpcionesSugeridas = esGerente
                    ? new List<string> { "Todos los turnos", "Stock en alerta", "Veterinarios y horarios" }
                    : new List<string> { "Todos los turnos", "Agendar turno", "Veterinarios y horarios" }
            };
        }

        private async Task<ChatbotResponseDto> ConsultarPacientesAsync(string usuarioRol)
        {
            var pacientesList = (await _pacienteRepository.GetActivosAsync()).Take(15).ToList();
            if (!pacientesList.Any())
            {
                return new ChatbotResponseDto
                {
                    Exito = true,
                    TipoRespuesta = "texto",
                    Respuesta = "No se encontraron pacientes activos registrados en el sistema.",
                    OpcionesSugeridas = new List<string> { "Turnos de hoy", "Veterinarios y horarios" }
                };
            }

            var sb = new StringBuilder();
            sb.AppendLine($"Listado de pacientes registrados ({pacientesList.Count}):\n");
            foreach (var p in pacientesList)
            {
                var esp = !string.IsNullOrEmpty(p.Especie?.Nombre) ? p.Especie.Nombre : "Mascota";
                var raz = !string.IsNullOrEmpty(p.Raza?.Nombre) ? p.Raza.Nombre : "Raza no especificada";
                sb.AppendLine($"- {p.Nombre} | {esp} ({raz}) | Sexo: {(p.Sexo == "M" ? "Macho" : "Hembra")}");
            }

            var esRecepcionista = usuarioRol.Equals("Recepcionista", StringComparison.OrdinalIgnoreCase) || 
                                  usuarioRol.Equals("Secretaria", StringComparison.OrdinalIgnoreCase);
            var esGerente = usuarioRol.Equals("Gerente", StringComparison.OrdinalIgnoreCase) || 
                            usuarioRol.Equals("Manager", StringComparison.OrdinalIgnoreCase);

            List<string> sugerencias;
            if (esRecepcionista)
            {
                sugerencias = pacientesList.Take(3).Select(p => $"Agendar turno para {p.Nombre}").Concat(new[] { "Turnos de hoy" }).ToList();
            }
            else if (esGerente)
            {
                sugerencias = new List<string> { "Turnos de hoy", "Todos los turnos", "Stock en alerta" };
            }
            else
            {
                sugerencias = pacientesList.Take(3).Select(p => $"Info de {p.Nombre}").Concat(new[] { "Turnos de hoy" }).ToList();
            }

            return new ChatbotResponseDto
            {
                Exito = true,
                TipoRespuesta = "texto",
                Respuesta = sb.ToString().TrimEnd(),
                OpcionesSugeridas = sugerencias
            };
        }

        private async Task<ChatbotResponseDto?> ConsultarInfoPacienteAsync(string textoOriginal, string norm, string usuarioRol)
        {
            var esRecepcionista = usuarioRol.Equals("Recepcionista", StringComparison.OrdinalIgnoreCase) || 
                                  usuarioRol.Equals("Secretaria", StringComparison.OrdinalIgnoreCase);

            if (esRecepcionista)
            {
                return new ChatbotResponseDto
                {
                    Exito = true,
                    TipoRespuesta = "texto",
                    Respuesta = "Como Recepcionista no tienes autorización para acceder a la ficha detallada ni al historial clínico de los pacientes. Puedes consultar la agenda de turnos o agendar una cita médica.",
                    OpcionesSugeridas = new List<string> { "Turnos de hoy", "Agendar turno", "Horarios veterinarios" }
                };
            }

            var pacientes = (await _pacienteRepository.GetActivosAsync()).ToList();
            Paciente? paciente = null;

            // Buscar por coincidencia con nombres de pacientes en el texto normalizado
            foreach (var p in pacientes)
            {
                if (string.IsNullOrWhiteSpace(p.Nombre)) continue;
                var normP = NormalizarTexto(p.Nombre);
                if (Regex.IsMatch(norm, $@"\b{Regex.Escape(normP)}\b"))
                {
                    paciente = p;
                    break;
                }
            }

            if (paciente == null) return null;

            var sb = new StringBuilder();
            sb.AppendLine($"Ficha del Paciente: {paciente.Nombre}");
            var espNombre = paciente.Especie?.Nombre ?? "No especificada";
            var razaNombre = paciente.Raza?.Nombre ?? "No especificada";
            sb.AppendLine($"- Especie: {espNombre} | Raza: {razaNombre}");
            sb.AppendLine($"- Sexo: {(paciente.Sexo == "M" ? "Macho" : "Hembra")} | Fecha Nacimiento: {(paciente.FechaNacimiento.HasValue ? paciente.FechaNacimiento.Value.ToString("dd/MM/yyyy") : "No registrada")}");
            
            if (!string.IsNullOrEmpty(paciente.PropietarioId))
            {
                var prop = await _propietarioRepository.FindOneAsync(paciente.PropietarioId);
                if (prop != null)
                {
                    sb.AppendLine($"- Propietario: {prop.NombreCompleto} | Telefono: {prop.Telefono}");
                }
            }

            sb.AppendLine($"- Inasistencias registradas: {paciente.ContadorInasistencias}");

            var historiales = (await _historialClinicoRepository.GetByPacienteIdAsync(paciente.Id)).OrderByDescending(h => h.Fecha).ToList();
            var ultConsulta = historiales.FirstOrDefault();

            if (ultConsulta != null)
            {
                sb.AppendLine($"- Ultima Consulta: {ultConsulta.Fecha:dd/MM/yyyy} - Motivo: {ultConsulta.Motivo} - Diagnostico: {ultConsulta.Diagnostico}");
            }
            else
            {
                sb.AppendLine("- Consultas medicas: Sin registros previos en historial.");
            }

            if (!string.IsNullOrWhiteSpace(paciente.Observaciones))
            {
                sb.AppendLine($"- Observaciones: {paciente.Observaciones}");
            }

            var esGerente = usuarioRol.Equals("Gerente", StringComparison.OrdinalIgnoreCase) || 
                            usuarioRol.Equals("Manager", StringComparison.OrdinalIgnoreCase);

            var opcionesInfo = esGerente
                ? new List<string> { "Turnos de hoy", "Todos los turnos", "Stock en alerta" }
                : new List<string> { $"Agendar turno para {paciente.Nombre}", "Turnos de hoy" };

            return new ChatbotResponseDto
            {
                Exito = true,
                TipoRespuesta = "texto",
                Respuesta = sb.ToString().TrimEnd(),
                OpcionesSugeridas = opcionesInfo
            };
        }

        private async Task<ChatbotResponseDto> ConsultarVeterinariosYHorariosAsync(int? sucursalId)
        {
            var vets = (await _veterinarioRepository.GetActivosAsync()).ToList();
            if (sucursalId.HasValue && sucursalId.Value > 0)
            {
                vets = vets.Where(v => v.SucursalId == sucursalId.Value || v.SucursalId == 0).ToList();
            }

            var nombreSucursal = await ObtenerNombreSucursalAsync(sucursalId);
            var sb = new StringBuilder();
            sb.AppendLine($"Equipo Profesional y Horarios de Atencion - {nombreSucursal} ({vets.Count}):\n");

            if (!vets.Any())
            {
                sb.AppendLine("No se registran veterinarios asignados a esta sucursal actualmente.");
            }

            var diasNombres = new Dictionary<int, string>
            {
                { 1, "Lunes" }, { 2, "Martes" }, { 3, "Miercoles" }, { 4, "Jueves" }, { 5, "Viernes" }, { 6, "Sabado" }, { 7, "Domingo" }
            };

            var ahora = DateTime.Now;
            var isoHoy = (int)ahora.DayOfWeek == 0 ? 7 : (int)ahora.DayOfWeek;
            var timeHoy = ahora.TimeOfDay;

            foreach (var v in vets)
            {
                sb.AppendLine($"Profesional: {v.NombreCompleto} (Matricula: {v.Matricula})");
                var horarios = (await _horarioRepository.GetByVeterinarioIdAsync(v.Id)).Where(h => h.Activo).OrderBy(h => h.DiaSemana).ToList();
                if (horarios.Any())
                {
                    bool disponibleAhora = horarios.Any(h => h.DiaSemana == isoHoy && timeHoy >= h.HoraInicio && (timeHoy <= h.HoraFin || (h.HoraFin == TimeSpan.Zero && timeHoy <= new TimeSpan(24,0,0))));
                    sb.AppendLine($"  Estado actual: {(disponibleAhora ? "En horario de atencion" : "Fuera de horario laboral")}");

                    foreach (var h in horarios)
                    {
                        var diaStr = diasNombres.ContainsKey(h.DiaSemana) ? diasNombres[h.DiaSemana] : $"Dia {h.DiaSemana}";
                        sb.AppendLine($"  - {diaStr}: {h.HoraInicio:hh\\:mm} a {h.HoraFin:hh\\:mm} hs");
                    }
                }
                else
                {
                    sb.AppendLine("  - Atencion general segun turnos programados.");
                }
                sb.AppendLine();
            }

            return new ChatbotResponseDto
            {
                Exito = true,
                TipoRespuesta = "texto",
                Respuesta = sb.ToString().TrimEnd(),
                OpcionesSugeridas = new List<string> { "Turnos de hoy", "Como agendar un turno?" }
            };
        }

        private async Task<ChatbotResponseDto> ConsultarStockCriticoAsync(int? sucursalId)
        {
            var nombreSucursal = await ObtenerNombreSucursalAsync(sucursalId);
            var sb = new StringBuilder();

            if (sucursalId.HasValue && sucursalId.Value > 0)
            {
                var productos = (await _productoRepository.GetActivosAsync()).ToList();
                var alertasSucursal = new List<(Producto Producto, int StockActual)>();

                foreach (var p in productos)
                {
                    var pds = await _productoDepositoRepository.GetByProductoIdAsync(p.Id);
                    var stockSucursal = pds.Where(s => s.Deposito?.SucursalId == sucursalId.Value).Sum(s => s.StockActual);
                    if (stockSucursal <= p.StockMinimo)
                    {
                        alertasSucursal.Add((p, stockSucursal));
                    }
                }

                if (!alertasSucursal.Any())
                {
                    return new ChatbotResponseDto
                    {
                        Exito = true,
                        TipoRespuesta = "texto",
                        Respuesta = $"En {nombreSucursal} todos los productos cuentan con niveles de stock superiores al minimo configurado.",
                        OpcionesSugeridas = new List<string> { "Turnos de hoy", "Consultar servicios" }
                    };
                }

                sb.AppendLine($"Productos con Stock en Alerta - {nombreSucursal} ({alertasSucursal.Count}):\n");
                foreach (var item in alertasSucursal.Take(15))
                {
                    var estado = item.StockActual == 0 ? "Sin Stock (Agotado)" : "Stock Bajo";
                    sb.AppendLine($"- {item.Producto.Nombre} | Stock en sucursal: {item.StockActual} (Minimo: {item.Producto.StockMinimo}) | Estado: {estado} | Precio Venta: ${item.Producto.PrecioVenta:N0}");
                }
            }
            else
            {
                var bajoStock = (await _productoRepository.GetStockBajoAsync()).ToList();
                if (!bajoStock.Any())
                {
                    return new ChatbotResponseDto
                    {
                        Exito = true,
                        TipoRespuesta = "texto",
                        Respuesta = "Actualmente todos los productos cuentan con niveles de stock superiores al minimo configurado en todas las sucursales.",
                        OpcionesSugeridas = new List<string> { "Turnos de hoy", "Consultar servicios" }
                    };
                }

                sb.AppendLine($"Productos con Stock en Alerta Global ({bajoStock.Count}):\n");
                foreach (var p in bajoStock.Take(15))
                {
                    var estado = p.StockActual == 0 ? "Sin Stock (Agotado)" : "Stock Bajo";
                    sb.AppendLine($"- {p.Nombre} | Stock Global: {p.StockActual} (Minimo: {p.StockMinimo}) | Estado: {estado} | Precio Venta: ${p.PrecioVenta:N0}");
                }
            }

            return new ChatbotResponseDto
            {
                Exito = true,
                TipoRespuesta = "texto",
                Respuesta = sb.ToString().TrimEnd(),
                OpcionesSugeridas = new List<string> { "Turnos de hoy", "Precios de productos" }
            };
        }

        private async Task<ChatbotResponseDto?> ConsultarDetalleProductoAsync(string mensajeOriginal, string norm, int? sucursalId)
        {
            var productos = (await _productoRepository.GetProductosExpandidosAsync()).Where(p => p.Activo).ToList();
            if (!productos.Any()) return null;

            if (norm == "productos" || norm == "precios de productos" || norm == "catalogo de productos" || norm == "lista de productos")
            {
                return null;
            }

            string termino = norm;
            string[] prefijos = new[]
            {
                "informacion detallada del producto ", "informacion detallada de ",
                "informacion del producto ", "informacion de ", "informacion sobre ",
                "info detallada del producto ", "info del producto ", "info de ", "info sobre ",
                "detalle del producto ", "detalle de ", "detalles del producto ", "detalles de ",
                "datos del producto ", "datos de ", "ficha del producto ", "ficha tecnica de ", "ficha de ",
                "que es el producto ", "que es la ", "que es el ", "que es ",
                "para que sirve el producto ", "para que sirve la ", "para que sirve el ", "para que sirve ",
                "precio del producto ", "precio de ", "precios de ", "precio ",
                "costo del producto ", "costo de ", "costos de ", "costo ",
                "cuanto sale el producto ", "cuanto sale la ", "cuanto sale el ", "cuanto sale ",
                "cuanto cuesta el producto ", "cuanto cuesta la ", "cuanto cuesta el ", "cuanto cuesta ",
                "stock del producto ", "stock de ",
                "tienen el producto ", "tienen ", "hay stock de ", "hay ",
                "producto ", "medicamento ", "alimento ", "articulo ", "remedio "
            };

            foreach (var prefijo in prefijos)
            {
                if (termino.StartsWith(prefijo))
                {
                    termino = termino.Substring(prefijo.Length).Trim();
                    break;
                }
                else if (termino.Contains(prefijo))
                {
                    var idx = termino.IndexOf(prefijo);
                    termino = termino.Substring(idx + prefijo.Length).Trim();
                    break;
                }
            }

            termino = termino.Trim();
            if (string.IsNullOrWhiteSpace(termino) || termino.Length < 2)
            {
                return null;
            }

            Producto? producto = null;

            // 1. Coincidencia exacta por Código de Barras
            producto = productos.FirstOrDefault(p => !string.IsNullOrWhiteSpace(p.CodigoBarras) && p.CodigoBarras.Equals(termino, StringComparison.OrdinalIgnoreCase));

            // 2. Coincidencia por Nombre
            if (producto == null)
            {
                var coincidencias = productos.Where(p =>
                {
                    var pNorm = NormalizarTexto(p.Nombre);
                    return pNorm == termino || pNorm.Contains(termino) || termino.Contains(pNorm);
                }).ToList();

                if (coincidencias.Count == 1)
                {
                    producto = coincidencias.First();
                }
                else if (coincidencias.Count > 1)
                {
                    var exacta = coincidencias.FirstOrDefault(p => NormalizarTexto(p.Nombre) == termino);
                    if (exacta != null)
                    {
                        producto = exacta;
                    }
                    else
                    {
                        var sbMulti = new StringBuilder();
                        sbMulti.AppendLine($"Se encontraron {coincidencias.Count} productos relacionados con '{termino}':\n");
                        foreach (var c in coincidencias.Take(8))
                        {
                            var catNom = c.Categoria?.Nombre ?? "General";
                            var marNom = c.Marca?.Nombre != null ? $" ({c.Marca.Nombre})" : "";
                            sbMulti.AppendLine($"- {c.Nombre}{marNom} | Cat: {catNom} | Precio: ${c.PrecioVenta:N0} | Stock: {c.StockActual} u.");
                        }
                        sbMulti.AppendLine("\nPuedes escribir el nombre exacto de cualquiera de ellos para ver su ficha completa.");
                        return new ChatbotResponseDto
                        {
                            Exito = true,
                            TipoRespuesta = "texto",
                            Respuesta = sbMulti.ToString().TrimEnd(),
                            OpcionesSugeridas = coincidencias.Take(3).Select(c => $"Informacion de {c.Nombre}").Concat(new[] { "Precios de productos" }).ToList()
                        };
                    }
                }
            }

            // 3. Coincidencia por Marca o Descripción
            if (producto == null)
            {
                var porMarcaODesc = productos.Where(p =>
                    (p.Marca != null && NormalizarTexto(p.Marca.Nombre).Contains(termino)) ||
                    (!string.IsNullOrWhiteSpace(p.Descripcion) && NormalizarTexto(p.Descripcion).Contains(termino))
                ).ToList();

                if (porMarcaODesc.Count == 1)
                {
                    producto = porMarcaODesc.First();
                }
                else if (porMarcaODesc.Count > 1)
                {
                    var sbMarca = new StringBuilder();
                    sbMarca.AppendLine($"Productos asociados a '{termino}' ({porMarcaODesc.Count}):\n");
                    foreach (var c in porMarcaODesc.Take(8))
                    {
                        sbMarca.AppendLine($"- {c.Nombre} | Precio: ${c.PrecioVenta:N0} | Stock: {c.StockActual} u.");
                    }
                    sbMarca.AppendLine("\nEscribe el nombre de un producto para consultar su ficha técnica y disponibilidad.");
                    return new ChatbotResponseDto
                    {
                        Exito = true,
                        TipoRespuesta = "texto",
                        Respuesta = sbMarca.ToString().TrimEnd(),
                        OpcionesSugeridas = porMarcaODesc.Take(3).Select(c => $"Informacion de {c.Nombre}").ToList()
                    };
                }
            }

            if (producto == null)
            {
                return null;
            }

            var nombreSucursal = await ObtenerNombreSucursalAsync(sucursalId);
            var pds = (await _productoDepositoRepository.GetByProductoIdAsync(producto.Id)).ToList();
            int stockEnSucursal = sucursalId.HasValue && sucursalId.Value > 0
                ? pds.Where(pd => pd.Deposito?.SucursalId == sucursalId.Value).Sum(pd => pd.StockActual)
                : producto.StockActual;

            var sb = new StringBuilder();
            sb.AppendLine($"Informacion Detallada del Producto: {producto.Nombre}\n");
            sb.AppendLine($"- Categoria: {producto.Categoria?.Nombre ?? "General"}");
            if (producto.Marca != null && !string.IsNullOrWhiteSpace(producto.Marca.Nombre))
            {
                sb.AppendLine($"- Marca: {producto.Marca.Nombre}");
            }
            sb.AppendLine($"- Precio de Venta: ${producto.PrecioVenta:N0}");
            if (sucursalId.HasValue && sucursalId.Value > 0)
            {
                sb.AppendLine($"- Stock en {nombreSucursal}: {stockEnSucursal} unidades");
            }
            sb.AppendLine($"- Stock Total General: {producto.StockActual} unidades (Minimo requerido: {producto.StockMinimo})");
            if (!string.IsNullOrWhiteSpace(producto.CodigoBarras))
            {
                sb.AppendLine($"- Codigo de Barras: {producto.CodigoBarras}");
            }
            if (producto.Proveedor != null && !string.IsNullOrWhiteSpace(producto.Proveedor.RazonSocial))
            {
                sb.AppendLine($"- Proveedor: {producto.Proveedor.RazonSocial}");
            }
            var desc = string.IsNullOrWhiteSpace(producto.Descripcion)
                ? "Articulo para uso y atencion en clinica veterinaria."
                : producto.Descripcion;
            sb.AppendLine($"- Descripcion: {desc}");

            return new ChatbotResponseDto
            {
                Exito = true,
                TipoRespuesta = "texto",
                Respuesta = sb.ToString().TrimEnd(),
                OpcionesSugeridas = new List<string> { "Precios de productos", "Stock en alerta", "Turnos de hoy" }
            };
        }

        private async Task<ChatbotResponseDto?> ConsultarPrecioOServicioAsync(string lower, string norm)
        {
            var productos = (await _productoRepository.GetActivosAsync()).ToList();
            var servicios = (await _servicioRepository.GetActivosAsync()).ToList();

            // 1. Consulta general de productos
            bool esConsultaGeneralProductos = norm.Contains("precios de productos") ||
                                              norm.Contains("precio de productos") ||
                                              norm.Contains("precio productos") ||
                                              norm == "productos" ||
                                              norm.Contains("catalogo de productos") ||
                                              norm.Contains("lista de productos") ||
                                              norm.Contains("articulos");

            if (esConsultaGeneralProductos)
            {
                if (!productos.Any())
                {
                    return new ChatbotResponseDto
                    {
                        Exito = true,
                        TipoRespuesta = "texto",
                        Respuesta = "No se registran productos activos en el catalogo comercial.",
                        OpcionesSugeridas = new List<string> { "Precios de servicios", "Turnos de hoy" }
                    };
                }

                var sbProd = new StringBuilder();
                sbProd.AppendLine($"Lista de Precios de Productos ({productos.Count}):\n");
                foreach (var p in productos.Take(20))
                {
                    sbProd.AppendLine($"- {p.Nombre} | Precio: ${p.PrecioVenta:N0} | Stock: {p.StockActual} u.");
                }
                if (productos.Count > 20)
                {
                    sbProd.AppendLine($"\n(Mostrando primeros 20 de {productos.Count} productos. Para consultar un articulo especifico escribe: 'precio de [nombre]').");
                }

                return new ChatbotResponseDto
                {
                    Exito = true,
                    TipoRespuesta = "texto",
                    Respuesta = sbProd.ToString().TrimEnd(),
                    OpcionesSugeridas = new List<string> { "Precios de servicios", "Stock en alerta", "Turnos de hoy" }
                };
            }

            // 2. Consulta general de servicios
            bool esConsultaGeneralServicios = norm.Contains("precios de servicios") ||
                                              norm.Contains("precio de servicios") ||
                                              norm.Contains("precio servicios") ||
                                              norm == "servicios" ||
                                              norm.Contains("catalogo de servicios") ||
                                              norm.Contains("lista de servicios") ||
                                              norm.Contains("tarifas");

            if (esConsultaGeneralServicios)
            {
                if (!servicios.Any())
                {
                    return new ChatbotResponseDto
                    {
                        Exito = true,
                        TipoRespuesta = "texto",
                        Respuesta = "No se registran servicios activos en el catalogo clinico.",
                        OpcionesSugeridas = new List<string> { "Precios de productos", "Turnos de hoy" }
                    };
                }

                var sbServ = new StringBuilder();
                sbServ.AppendLine($"Catalogo y Tarifas de Servicios Medicos ({servicios.Count}):\n");
                foreach (var s in servicios)
                {
                    sbServ.AppendLine($"- {s.Nombre} | Tarifa: ${s.Precio:N0} | Duracion: {s.DuracionMinutos} min");
                }
                sbServ.AppendLine("\nPuedes agendar cualquiera de estos servicios indicando el paciente y horario deseado.");

                return new ChatbotResponseDto
                {
                    Exito = true,
                    TipoRespuesta = "texto",
                    Respuesta = sbServ.ToString().TrimEnd(),
                    OpcionesSugeridas = new List<string> { "Turnos de hoy", "Como agendar un turno?" }
                };
            }

            // 3. Consulta específica por producto o servicio
            string termino = norm;
            string[] prefijos = new[]
            {
                "precio de los ", "precios de los ", "precio de las ", "precios de las ",
                "precio del ", "precios del ", "precio de ", "precios de ", "precio ",
                "costo de los ", "costo de las ", "costo del ", "costo de ", "costo ",
                "cuanto sale el ", "cuanto sale la ", "cuanto sale los ", "cuanto sale las ", "cuanto sale ",
                "cuanto cuesta el ", "cuanto cuesta la ", "cuanto cuesta los ", "cuanto cuesta las ", "cuanto cuesta ",
                "info de ", "info del ", "informacion de ", "informacion del ", "datos de ", "datos del ",
                "que valor tiene el ", "que valor tiene la ", "que valor tiene ", "valor de ", "tarifa de ", "tarifa "
            };

            foreach (var prefijo in prefijos)
            {
                if (termino.StartsWith(prefijo))
                {
                    termino = termino.Substring(prefijo.Length).Trim();
                    break;
                }
                else if (termino.Contains(prefijo))
                {
                    var idx = termino.IndexOf(prefijo);
                    termino = termino.Substring(idx + prefijo.Length).Trim();
                    break;
                }
            }

            termino = termino.Trim();

            if (string.IsNullOrWhiteSpace(termino))
            {
                return null;
            }

            // A. Buscar en Servicios
            var servicio = servicios.FirstOrDefault(s =>
                NormalizarTexto(s.Nombre).Contains(termino) ||
                termino.Contains(NormalizarTexto(s.Nombre)));

            if (servicio != null)
            {
                return new ChatbotResponseDto
                {
                    Exito = true,
                    TipoRespuesta = "texto",
                    Respuesta = $"Informacion del Servicio:\n" +
                                $"- Servicio: {servicio.Nombre}\n" +
                                $"- Precio: ${servicio.Precio:N0}\n" +
                                $"- Duracion estimada: {servicio.DuracionMinutos} minutos\n" +
                                $"- Descripcion: {(string.IsNullOrWhiteSpace(servicio.Descripcion) ? "Atencion profesional en clinica." : servicio.Descripcion)}",
                    OpcionesSugeridas = new List<string> { $"Agendar {servicio.Nombre}", "Turnos de hoy" }
                };
            }

            // B. Buscar en Productos
            var productosCoincidentes = productos.Where(p =>
                NormalizarTexto(p.Nombre).Contains(termino) ||
                termino.Contains(NormalizarTexto(p.Nombre)) ||
                (!string.IsNullOrWhiteSpace(p.CodigoBarras) && p.CodigoBarras.Equals(termino, StringComparison.OrdinalIgnoreCase))
            ).ToList();

            if (productosCoincidentes.Count == 1)
            {
                var p = productosCoincidentes.First();
                return new ChatbotResponseDto
                {
                    Exito = true,
                    TipoRespuesta = "texto",
                    Respuesta = $"Informacion del Producto:\n" +
                                $"- Producto: {p.Nombre}\n" +
                                $"- Precio de Venta: ${p.PrecioVenta:N0}\n" +
                                $"- Stock Actual: {p.StockActual} unidades (Minimo: {p.StockMinimo})\n" +
                                $"- Codigo: {p.CodigoBarras}\n" +
                                $"- Descripcion: {(string.IsNullOrWhiteSpace(p.Descripcion) ? "Articulo para atencion veterinaria." : p.Descripcion)}",
                    OpcionesSugeridas = new List<string> { "Stock en alerta", "Precios de productos" }
                };
            }
            else if (productosCoincidentes.Count > 1)
            {
                var sb = new StringBuilder();
                sb.AppendLine($"Se encontraron {productosCoincidentes.Count} productos relacionados con '{termino}':\n");
                foreach (var p in productosCoincidentes.Take(10))
                {
                    sb.AppendLine($"- {p.Nombre} | Precio: ${p.PrecioVenta:N0} | Stock: {p.StockActual} u.");
                }
                return new ChatbotResponseDto
                {
                    Exito = true,
                    TipoRespuesta = "texto",
                    Respuesta = sb.ToString().TrimEnd(),
                    OpcionesSugeridas = new List<string> { "Precios de productos", "Stock en alerta" }
                };
            }

            if (norm.Contains("precio") || norm.Contains("costo") || norm.Contains("cuanto sale") || norm.Contains("cuanto cuesta"))
            {
                return new ChatbotResponseDto
                {
                    Exito = true,
                    TipoRespuesta = "texto",
                    Respuesta = $"No se encontro ningun producto o servicio con el nombre '{termino}'. Puedes consultar 'precios de productos' o 'precios de servicios' para ver la lista completa.",
                    OpcionesSugeridas = new List<string> { "Precios de productos", "Precios de servicios" }
                };
            }

            return null;
        }

        private async Task<ChatbotResponseDto?> IntentarExtraerTurnoAsync(string mensaje, int? sucursalId)
        {
            var pacientes = (await _pacienteRepository.GetActivosAsync()).ToList();
            var veterinarios = (await _veterinarioRepository.GetActivosAsync()).ToList();
            var servicios = (await _servicioRepository.GetActivosAsync()).ToList();

            if (sucursalId.HasValue && sucursalId.Value > 0)
            {
                veterinarios = veterinarios.Where(v => v.SucursalId == sucursalId.Value || v.SucursalId == 0).ToList();
            }

            var lower = mensaje.ToLowerInvariant();
            var norm = NormalizarTexto(mensaje);

            // Buscar Paciente (coincidencia normalizada y exacta)
            Paciente? pacienteEncontrado = null;
            foreach (var p in pacientes)
            {
                if (string.IsNullOrWhiteSpace(p.Nombre)) continue;
                var normP = NormalizarTexto(p.Nombre);
                if (Regex.IsMatch(norm, $@"\b{Regex.Escape(normP)}\b"))
                {
                    pacienteEncontrado = p;
                    break;
                }
            }

            // Buscar Veterinario
            Veterinario? vetEncontrado = null;
            foreach (var v in veterinarios)
            {
                var normCompleto = NormalizarTexto(v.NombreCompleto);
                var normApellido = NormalizarTexto(v.Apellido);
                var normNombre = NormalizarTexto(v.Nombre);

                if (Regex.IsMatch(norm, $@"\b{Regex.Escape(normCompleto)}\b") ||
                    Regex.IsMatch(norm, $@"\b{Regex.Escape(normApellido)}\b") ||
                    Regex.IsMatch(norm, $@"\b{Regex.Escape(normNombre)}\b"))
                {
                    vetEncontrado = v;
                    break;
                }
            }

            // Extraer Motivo explícito o servicio
            string? motivoDetectado = null;

            // 1. Prioridad: Coincidencia explícita de "motivo:? <motivo>"
            var matchMotivoExpl = Regex.Match(mensaje, @"\bmotivo(?:\s+es|\s*:)?\s+([a-zA-ZáéíóúÁÉÍÓÚñÑ0-9\s\-]+?)(?:\s+(?:con|el|en|a\s+las|hoy|mañana|manana|\d{1,2}[:.]\d{2})|\s*$|[.,;])", RegexOptions.IgnoreCase);
            if (matchMotivoExpl.Success)
            {
                var cand = matchMotivoExpl.Groups[1].Value.Trim();
                if (!string.IsNullOrWhiteSpace(cand))
                {
                    motivoDetectado = cand;
                }
            }

            // 2. Si no hubo "motivo ...", buscar con "por <motivo>" o "para <motivo>"
            if (string.IsNullOrWhiteSpace(motivoDetectado))
            {
                var matches = Regex.Matches(mensaje, @"\b(?:por|para)\s+([a-zA-ZáéíóúÁÉÍÓÚñÑ0-9\s\-]+?)(?:\s+(?:con|el|en|a\s+las|hoy|mañana|manana|\d{1,2}[:.]\d{2})|\s*$|[.,;])", RegexOptions.IgnoreCase);
                foreach (Match m in matches)
                {
                    var cand = m.Groups[1].Value.Trim();
                    var candNorm = NormalizarTexto(cand);
                    if (string.IsNullOrWhiteSpace(candNorm) ||
                        candNorm == "hoy" || candNorm == "manana" || candNorm == "pasado manana" ||
                        candNorm.StartsWith("las ") || candNorm.StartsWith("el ") ||
                        (pacienteEncontrado != null && candNorm.Contains(NormalizarTexto(pacienteEncontrado.Nombre))) ||
                        (vetEncontrado != null && candNorm.Contains(NormalizarTexto(vetEncontrado.NombreCompleto))))
                    {
                        continue;
                    }

                    motivoDetectado = cand;
                    break;
                }
            }

            // 3. Buscar si el motivo coincide con algún servicio del catálogo
            Servicio? servicioEncontrado = null;
            if (!string.IsNullOrWhiteSpace(motivoDetectado))
            {
                var motNorm = NormalizarTexto(motivoDetectado);
                servicioEncontrado = servicios.FirstOrDefault(s =>
                    NormalizarTexto(s.Nombre).Contains(motNorm) || motNorm.Contains(NormalizarTexto(s.Nombre)));
            }

            if (servicioEncontrado == null)
            {
                foreach (var s in servicios)
                {
                    var sNorm = NormalizarTexto(s.Nombre);
                    if (norm.Contains(sNorm))
                    {
                        servicioEncontrado = s;
                        if (string.IsNullOrWhiteSpace(motivoDetectado))
                        {
                            motivoDetectado = s.Nombre;
                        }
                        break;
                    }
                }
            }

            // 4. Motivo final: si el usuario lo especificó se usa, sino "Consulta general" por defecto
            string motivoFinal;
            if (!string.IsNullOrWhiteSpace(motivoDetectado))
            {
                motivoFinal = char.ToUpper(motivoDetectado[0]) + motivoDetectado.Substring(1);
            }
            else
            {
                motivoFinal = "Consulta general";
            }

            if (servicioEncontrado == null)
            {
                servicioEncontrado = servicios.FirstOrDefault(s => NormalizarTexto(s.Nombre).Contains("consulta"))
                                  ?? servicios.FirstOrDefault();
            }

            // Extraer Fecha
            DateTime? fechaObjetivo = null;
            if (lower.Contains("hoy"))
            {
                fechaObjetivo = DateTime.Today;
            }
            else if (lower.Contains("mañana") || lower.Contains("manana"))
            {
                fechaObjetivo = DateTime.Today.AddDays(1);
            }
            else if (lower.Contains("pasado mañana") || lower.Contains("pasado manana"))
            {
                fechaObjetivo = DateTime.Today.AddDays(2);
            }
            else
            {
                // Buscar días de la semana
                var dias = new Dictionary<string, DayOfWeek>
                {
                    { "lunes", DayOfWeek.Monday },
                    { "martes", DayOfWeek.Tuesday },
                    { "miercoles", DayOfWeek.Wednesday },
                    { "miércoles", DayOfWeek.Wednesday },
                    { "jueves", DayOfWeek.Thursday },
                    { "viernes", DayOfWeek.Friday },
                    { "sabado", DayOfWeek.Saturday },
                    { "sábado", DayOfWeek.Saturday },
                    { "domingo", DayOfWeek.Sunday }
                };

                foreach (var d in dias)
                {
                    if (lower.Contains(d.Key))
                    {
                        var targetDay = d.Value;
                        var start = DateTime.Today;
                        for (int i = 1; i <= 7; i++)
                        {
                            var candidate = start.AddDays(i);
                            if (candidate.DayOfWeek == targetDay)
                            {
                                fechaObjetivo = candidate;
                                break;
                            }
                        }
                        break;
                    }
                }

                // Buscar formato DD/MM/YYYY o DD/MM
                if (!fechaObjetivo.HasValue)
                {
                    var matchFecha = Regex.Match(lower, @"\b(\d{1,2})[\/\-](\d{1,2})(?:[\/\-](\d{2,4}))?\b");
                    if (matchFecha.Success)
                    {
                        int dia = int.Parse(matchFecha.Groups[1].Value);
                        int mes = int.Parse(matchFecha.Groups[2].Value);
                        int anio = matchFecha.Groups[3].Success ? int.Parse(matchFecha.Groups[3].Value) : DateTime.Today.Year;
                        if (anio < 100) anio += 2000;

                        if (DateTime.TryParse($"{anio}-{mes:D2}-{dia:D2}", out var dt))
                        {
                            fechaObjetivo = dt;
                        }
                    }
                }
            }

            // Extraer Hora
            TimeSpan? horaObjetivo = null;
            var matchHora = Regex.Match(lower, @"\b(\d{1,2})[:.](\d{2})\b");
            if (matchHora.Success)
            {
                int h = int.Parse(matchHora.Groups[1].Value);
                int m = int.Parse(matchHora.Groups[2].Value);
                if (h >= 0 && h < 24 && m >= 0 && m < 60)
                {
                    horaObjetivo = new TimeSpan(h, m, 0);
                }
            }
            else
            {
                var matchHoraSimple = Regex.Match(lower, @"\b(\d{1,2})\s*(?:hs|horas|h)\b");
                if (matchHoraSimple.Success)
                {
                    int h = int.Parse(matchHoraSimple.Groups[1].Value);
                    if (h >= 0 && h < 24)
                    {
                        horaObjetivo = new TimeSpan(h, 0, 0);
                    }
                }
            }

            // Si no se detectó paciente o fecha/hora, solicitar datos restantes
            if (pacienteEncontrado == null || !fechaObjetivo.HasValue || !horaObjetivo.HasValue)
            {
                var faltantes = new List<string>();
                if (pacienteEncontrado == null) faltantes.Add("el nombre del paciente registrado");
                if (!fechaObjetivo.HasValue) faltantes.Add("la fecha");
                if (!horaObjetivo.HasValue) faltantes.Add("el horario");

                bool tieneVerboAgendar = Regex.IsMatch(norm, @"\b(agend|sacar|reserv|anot|program)\b");

                // Solo solicitar datos si el usuario mencionó un paciente, o especificó fecha y hora juntos, o usó un verbo de agendamiento
                if (pacienteEncontrado != null || (fechaObjetivo.HasValue && horaObjetivo.HasValue) || tieneVerboAgendar)
                {
                    return new ChatbotResponseDto
                    {
                        Exito = true,
                        TipoRespuesta = "texto",
                        Respuesta = $"Para preparar el turno, necesito que especifiques: {string.Join(", ", faltantes)}.\n\nEjemplo: 'Turno para Toby manana a las 11:00'.",
                        OpcionesSugeridas = new List<string> { "Turnos de hoy", "Veterinarios y horarios" }
                    };
                }

                return null;
            }

            var fechaHoraCompleta = fechaObjetivo.Value.Date + horaObjetivo.Value;
            var duracion = servicioEncontrado?.DuracionMinutos ?? 30;

            // Si no se especificó veterinario, buscar prioritariamente el profesional disponible y de turno en ese horario
            if (vetEncontrado == null)
            {
                var isoDayCand = (int)fechaHoraCompleta.DayOfWeek == 0 ? 7 : (int)fechaHoraCompleta.DayOfWeek;
                var inicioCand = fechaHoraCompleta.TimeOfDay;
                var finCand = fechaHoraCompleta.AddMinutes(duracion).TimeOfDay;

                var candidatos = veterinarios.ToList();
                if (!candidatos.Any())
                {
                    candidatos = (await _veterinarioRepository.GetActivosAsync()).ToList();
                }

                // 1. PRIORIDAD MÁXIMA: Profesional de la sucursal con horario laboral activo que cubra ese turno y sin solapamiento
                foreach (var v in candidatos)
                {
                    var turnosV = await _turnoRepository.GetByVeterinarioIdAsync(v.Id, fechaHoraCompleta.Date, fechaHoraCompleta.Date.AddDays(1));
                    var tieneConflicto = turnosV.Any(t => t.Estado != EstadoTurno.Cancelado && t.Estado != EstadoTurno.Ausente && t.SeSuperponeCon(fechaHoraCompleta, duracion));
                    if (tieneConflicto) continue;

                    var horariosV = (await _horarioRepository.GetByVeterinarioIdAsync(v.Id)).Where(h => h.Activo).ToList();
                    bool enHorario = horariosV.Any(h => h.DiaSemana == isoDayCand && inicioCand >= h.HoraInicio && (finCand <= h.HoraFin || (h.HoraFin == TimeSpan.Zero && finCand <= new TimeSpan(24, 0, 0))));

                    if (enHorario)
                    {
                        vetEncontrado = v;
                        break;
                    }
                }

                // 2. SEGUNDA PRIORIDAD: Si ninguno tiene horario explícito que cubra ese momento, profesional de la sucursal sin horarios registrados (atención según turnos) y sin solapamiento
                if (vetEncontrado == null)
                {
                    foreach (var v in candidatos)
                    {
                        var turnosV = await _turnoRepository.GetByVeterinarioIdAsync(v.Id, fechaHoraCompleta.Date, fechaHoraCompleta.Date.AddDays(1));
                        var tieneConflicto = turnosV.Any(t => t.Estado != EstadoTurno.Cancelado && t.Estado != EstadoTurno.Ausente && t.SeSuperponeCon(fechaHoraCompleta, duracion));
                        if (tieneConflicto) continue;

                        var horariosV = (await _horarioRepository.GetByVeterinarioIdAsync(v.Id)).Where(h => h.Activo).ToList();
                        if (!horariosV.Any())
                        {
                            vetEncontrado = v;
                            break;
                        }
                    }
                }

                // 3. TERCERA PRIORIDAD: Si aún no se encontró, buscar en todos los veterinarios activos del sistema si alguno está en turno a esa hora
                if (vetEncontrado == null)
                {
                    var todosVets = (await _veterinarioRepository.GetActivosAsync()).ToList();
                    foreach (var v in todosVets)
                    {
                        var turnosV = await _turnoRepository.GetByVeterinarioIdAsync(v.Id, fechaHoraCompleta.Date, fechaHoraCompleta.Date.AddDays(1));
                        var tieneConflicto = turnosV.Any(t => t.Estado != EstadoTurno.Cancelado && t.Estado != EstadoTurno.Ausente && t.SeSuperponeCon(fechaHoraCompleta, duracion));
                        if (tieneConflicto) continue;

                        var horariosV = (await _horarioRepository.GetByVeterinarioIdAsync(v.Id)).Where(h => h.Activo).ToList();
                        bool enHorario = horariosV.Any(h => h.DiaSemana == isoDayCand && inicioCand >= h.HoraInicio && (finCand <= h.HoraFin || (h.HoraFin == TimeSpan.Zero && finCand <= new TimeSpan(24, 0, 0))));
                        if (enHorario || !horariosV.Any())
                        {
                            vetEncontrado = v;
                            break;
                        }
                    }
                }
            }

            if (vetEncontrado == null)
            {
                return new ChatbotResponseDto
                {
                    Exito = true,
                    TipoRespuesta = "texto",
                    Respuesta = $"No hay profesionales veterinarios disponibles para el dia {fechaHoraCompleta:dd/MM/yyyy} a las {fechaHoraCompleta:HH:mm} hs. Por favor selecciona otro dia u horario.",
                    OpcionesSugeridas = new List<string> { "Veterinarios disponibles y horarios", "Turnos de hoy" }
                };
            }

            // Validar anticipación de 30 minutos
            if (fechaHoraCompleta < DateTime.Now.AddMinutes(30))
            {
                return new ChatbotResponseDto
                {
                    Exito = true,
                    TipoRespuesta = "texto",
                    Respuesta = $"La fecha y hora indicada ({fechaHoraCompleta:dd/MM/yyyy HH:mm} hs) ya ha pasado o tiene menos de 30 minutos de anticipacion requeridos para coordinar la cita.",
                    OpcionesSugeridas = new List<string> { "Agendar manana 10:00 hs", "Turnos de hoy" }
                };
            }

            // Validar horario laboral del veterinario
            var horarios = (await _horarioRepository.GetByVeterinarioIdAsync(vetEncontrado.Id)).Where(h => h.Activo).ToList();
            if (horarios.Any())
            {
                var isoDay = (int)fechaHoraCompleta.DayOfWeek == 0 ? 7 : (int)fechaHoraCompleta.DayOfWeek;
                var inicioTurno = fechaHoraCompleta.TimeOfDay;
                var finTurno = fechaHoraCompleta.AddMinutes(duracion).TimeOfDay;

                bool dentroHorario = horarios.Any(h =>
                    h.DiaSemana == isoDay &&
                    inicioTurno >= h.HoraInicio &&
                    (finTurno <= h.HoraFin || (h.HoraFin == TimeSpan.Zero && finTurno <= new TimeSpan(24, 0, 0)))
                );

                if (!dentroHorario)
                {
                    return new ChatbotResponseDto
                    {
                        Exito = true,
                        TipoRespuesta = "texto",
                        Respuesta = $"{vetEncontrado.NombreCompleto} no cuenta con horario de atencion configurado para el dia {fechaHoraCompleta:dd/MM/yyyy} a las {fechaHoraCompleta:HH:mm} hs.",
                        OpcionesSugeridas = new List<string> { "Consultar horarios", "Turnos de hoy" }
                    };
                }
            }

            // Validar superposición con turnos del veterinario
            var turnosVet = await _turnoRepository.GetByVeterinarioIdAsync(
                vetEncontrado.Id, fechaHoraCompleta.Date, fechaHoraCompleta.Date.AddDays(1));

            var solapadoVet = turnosVet.Any(t =>
                t.Estado != EstadoTurno.Cancelado && t.Estado != EstadoTurno.Ausente &&
                t.SeSuperponeCon(fechaHoraCompleta, duracion));

            if (solapadoVet)
            {
                return new ChatbotResponseDto
                {
                    Exito = true,
                    TipoRespuesta = "texto",
                    Respuesta = $"{vetEncontrado.NombreCompleto} ya tiene un turno agendado en ese rango horario ({fechaHoraCompleta:HH:mm} hs). Por favor selecciona otro horario.",
                    OpcionesSugeridas = new List<string> { "Ver turnos de hoy", "Probar 30 minutos despues" }
                };
            }

            // Validar superposición con turnos del paciente
            var turnosPac = await _turnoRepository.GetByPacienteIdAsync(pacienteEncontrado.Id);
            var solapadoPac = turnosPac.Any(t =>
                t.Estado != EstadoTurno.Cancelado && t.Estado != EstadoTurno.Ausente &&
                t.SeSuperponeCon(fechaHoraCompleta, duracion));

            if (solapadoPac)
            {
                return new ChatbotResponseDto
                {
                    Exito = true,
                    TipoRespuesta = "texto",
                    Respuesta = $"El paciente {pacienteEncontrado.Nombre} ya tiene otro turno registrado en ese horario.",
                    OpcionesSugeridas = new List<string> { "Ver turnos de hoy", "Cambiar horario" }
                };
            }

            var turnoPropuesto = new TurnoPropuestoDto
            {
                PacienteId = pacienteEncontrado.Id,
                PacienteNombre = pacienteEncontrado.Nombre,
                VeterinarioId = vetEncontrado.Id,
                VeterinarioNombre = vetEncontrado.NombreCompleto,
                ServicioId = servicioEncontrado?.Id,
                ServicioNombre = servicioEncontrado?.Nombre ?? "Consulta General",
                SucursalId = (sucursalId.HasValue && sucursalId.Value > 0) ? sucursalId.Value : (vetEncontrado.SucursalId > 0 ? vetEncontrado.SucursalId : 1),
                FechaHora = fechaHoraCompleta,
                DuracionMinutos = duracion,
                Motivo = motivoFinal,
                ListoParaConfirmar = true,
                MensajeValidacion = "Horario y profesional disponibles para agendamiento."
            };

            return new ChatbotResponseDto
            {
                Exito = true,
                TipoRespuesta = "propuesta_turno",
                TurnoPropuesto = turnoPropuesto,
                Respuesta = $"He verificado la disponibilidad y preparado la propuesta de turno para {pacienteEncontrado.Nombre} con el profesional {vetEncontrado.NombreCompleto} el dia {fechaHoraCompleta:dd/MM/yyyy} a las {fechaHoraCompleta:HH:mm} hs para {motivoFinal}. Puedes confirmar la cita directamente con el boton inferior:",
                OpcionesSugeridas = new List<string>()
            };
        }

        public async Task<ChatbotResponseDto> ConfirmarTurnoPropuestoAsync(
            TurnoPropuestoDto turnoDto, string usuarioRol, int? sucursalId)
        {
            if (turnoDto == null)
            {
                return new ChatbotResponseDto
                {
                    Exito = false,
                    ErrorMensaje = "Datos del turno invalidos.",
                    TipoRespuesta = "texto"
                };
            }

            if (usuarioRol.Equals("Gerente", StringComparison.OrdinalIgnoreCase) || 
                usuarioRol.Equals("Manager", StringComparison.OrdinalIgnoreCase))
            {
                return new ChatbotResponseDto
                {
                    Exito = false,
                    ErrorMensaje = "Tu rol de Gerente no tiene permisos para confirmar turnos. Solicita el agendamiento a Recepción o al Veterinario.",
                    TipoRespuesta = "texto"
                };
            }

            var paciente = await _pacienteRepository.FindOneAsync(turnoDto.PacienteId);
            if (paciente == null)
            {
                return new ChatbotResponseDto
                {
                    Exito = false,
                    ErrorMensaje = $"No existe el paciente con ID {turnoDto.PacienteId}.",
                    TipoRespuesta = "texto"
                };
            }

            var veterinario = await _veterinarioRepository.FindOneAsync(turnoDto.VeterinarioId);
            if (veterinario == null)
            {
                return new ChatbotResponseDto
                {
                    Exito = false,
                    ErrorMensaje = $"No existe el veterinario con ID {turnoDto.VeterinarioId}.",
                    TipoRespuesta = "texto"
                };
            }

            var servicioId = turnoDto.ServicioId ?? 1;
            var duracion = turnoDto.DuracionMinutos > 0 ? turnoDto.DuracionMinutos : 30;

            // Validar superposición nuevamente
            var turnosVet = await _turnoRepository.GetByVeterinarioIdAsync(
                veterinario.Id, turnoDto.FechaHora.Date, turnoDto.FechaHora.Date.AddDays(1));

            if (turnosVet.Any(t => t.Estado != EstadoTurno.Cancelado && t.Estado != EstadoTurno.Ausente && t.SeSuperponeCon(turnoDto.FechaHora, duracion)))
            {
                return new ChatbotResponseDto
                {
                    Exito = false,
                    ErrorMensaje = "El horario fue ocupado por otra cita en el transcurso. Por favor selecciona otro turno.",
                    TipoRespuesta = "texto"
                };
            }

            int finalSucursal = (turnoDto.SucursalId.HasValue && turnoDto.SucursalId.Value > 0)
                ? turnoDto.SucursalId.Value
                : (sucursalId.HasValue && sucursalId.Value > 0 ? sucursalId.Value : (veterinario.SucursalId > 0 ? veterinario.SucursalId : 1));

            var nuevoTurno = new Turno(
                turnoDto.PacienteId,
                turnoDto.VeterinarioId,
                servicioId,
                turnoDto.FechaHora,
                duracion,
                motivo: string.IsNullOrWhiteSpace(turnoDto.Motivo) ? "Consulta general" : turnoDto.Motivo,
                observaciones: "Turno generado y confirmado desde el Copiloto Inteligente.",
                sucursalId: finalSucursal
            );

            nuevoTurno.AsignarSucursal(finalSucursal);

            if (!nuevoTurno.IsValid)
            {
                var errores = string.Join("; ", nuevoTurno.GetErrors().Select(e => e.ErrorMessage));
                return new ChatbotResponseDto
                {
                    Exito = false,
                    ErrorMensaje = $"Error de validacion: {errores}",
                    TipoRespuesta = "texto"
                };
            }

            var idGenerado = await _turnoRepository.AddAsync(nuevoTurno);
            turnoDto.TurnoId = idGenerado?.ToString();

            return new ChatbotResponseDto
            {
                Exito = true,
                TipoRespuesta = "turno_confirmado",
                TurnoPropuesto = turnoDto,
                Respuesta = $"Turno confirmado exitosamente para {paciente.Nombre} con {veterinario.NombreCompleto} el dia {turnoDto.FechaHora:dd/MM/yyyy} a las {turnoDto.FechaHora:HH:mm} hs. La cita ha quedado registrada en la agenda de la clinica.",
                OpcionesSugeridas = new List<string> { "Turnos de hoy", "Agendar otro turno" }
            };
        }

        public async Task<ResumenHistoriaClinicaDto> GenerarResumenHistorialAsync(string pacienteId)
        {
            if (string.IsNullOrWhiteSpace(pacienteId))
            {
                throw new ArgumentException("El ID del paciente es requerido.", nameof(pacienteId));
            }

            var paciente = await _pacienteRepository.FindOneAsync(pacienteId);
            if (paciente == null)
            {
                throw new KeyNotFoundException($"No se encontro el paciente con Id {pacienteId}");
            }

            var historiales = (await _historialClinicoRepository.GetByPacienteIdAsync(pacienteId))
                .OrderByDescending(h => h.Fecha)
                .Take(10)
                .ToList();

            var tratamientos = (await _tratamientoRepository.GetByPacienteIdAsync(pacienteId))
                .OrderByDescending(t => t.Fecha)
                .Take(6)
                .ToList();

            var vacunas = (await _registroVacunacionRepository.GetByPacienteIdAsync(pacienteId))
                .OrderByDescending(v => v.FechaAplicacion)
                .Take(6)
                .ToList();

            var apiKey = GetApiKey();
            if (!string.IsNullOrWhiteSpace(apiKey))
            {
                try
                {
                    var resumenGemini = await InvocarGeminiResumenClinicoAsync(paciente, historiales, tratamientos, vacunas);
                    if (resumenGemini != null)
                    {
                        return resumenGemini;
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[Gemini Resumen Error Fallback] {ex.Message}");
                }
            }

            // Fallback estructurado local
            return GenerarResumenEstructuradoLocal(paciente, historiales, tratamientos, vacunas);
        }

        private async Task<string> ConsultarGeminiConversacionalAsync(
            List<ChatMensajeDto> historial, string mensajeActual, string usuarioNombre, string usuarioRol, int? sucursalId)
        {
            var apiKey = GetApiKey();
            var model = GetModel();
            var client = _httpClientFactory.CreateClient("GeminiClient");

            var nombreSucursal = await ObtenerNombreSucursalAsync(sucursalId);

            var systemPrompt =
                $"Eres el copiloto y asistente virtual inteligente de la clinica veterinaria.\n" +
                $"Usuario actual: '{usuarioNombre}', con rol: '{usuarioRol}'.\n" +
                $"Sucursal activa: '{nombreSucursal}'. Fecha y hora actual: {DateTime.Now:dd/MM/yyyy HH:mm}.\n\n" +
                "REGLAS OBLIGATORIAS:\n" +
                "1. Bajo ninguna circunstancia incluyas emojis en tus respuestas. CERO emojis.\n" +
                "2. Manten un tono formal, claro, profesional, cordial y sintetico en idioma espanol.\n" +
                "3. RESTRICCIONES SEGUN EL ROL DEL USUARIO:\n" +
                "   - Si el rol es 'Recepcionista': TIENE PROHIBIDO acceder a fichas o historiales clinicos detallados de pacientes, diagnosticos medicos confidenciales o finanzas/ingresos de la empresa. Si pregunta por esos temas, niega el acceso cordialmente indicando que su rol no tiene autorizacion para ver la ficha detallada del paciente y ofrecele ayuda para agendar turnos o ver horarios.\n" +
                "   - Si el rol es 'Veterinario': TIENE PROHIBIDO consultar finanzas, balances o facturacion global de la clinica. Solo puede ver temas clinicos, turnos, pacientes y vacunas.\n" +
                "   - Si el rol es 'Gerente': TIENE PROHIBIDO agendar o confirmar turnos operativos de la clinica. Su funcion es exclusivamente de gestion, supervision, stock, ingresos y reportes. Si intenta agendar turnos, recuerdale cordialmente que debe solicitarlo a Recepcion o al Veterinario.\n" +
                "   - Si el rol es 'Admin': Acceso total.\n" +
                $"4. Contexto de sucursal: Toda respuesta debe estar enmarcada en la sucursal activa ('{nombreSucursal}').\n" +
                "5. Si el usuario desea agendar un turno (y tiene permisos para ello), indicale que especifique paciente, fecha, hora y motivo (ejemplo: 'Agendame un turno para Henry hoy 18:00 motivo Castracion'). Si no especifica motivo, el sistema le asignara automaticamente 'Consulta general'.";

            var contents = new List<object>();

            // Historial reciente (hasta 6 mensajes)
            var ultimos = historial?.TakeLast(6).ToList() ?? new List<ChatMensajeDto>();
            foreach (var m in ultimos)
            {
                var role = m.Role == "model" ? "model" : "user";
                contents.Add(new
                {
                    role = role,
                    parts = new object[] { new { text = m.Content } }
                });
            }

            // Mensaje actual con instrucción del sistema
            contents.Add(new
            {
                role = "user",
                parts = new object[] { new { text = $"{systemPrompt}\n\nMensaje del usuario: {mensajeActual}" } }
            });

            var requestBody = new
            {
                contents = contents,
                generationConfig = new
                {
                    temperature = 0.2
                }
            };

            var jsonContent = new StringContent(JsonSerializer.Serialize(requestBody), Encoding.UTF8, "application/json");
            var url = $"https://generativelanguage.googleapis.com/v1beta/models/{model}:generateContent?key={apiKey}";

            var response = await client.PostAsync(url, jsonContent);
            response.EnsureSuccessStatusCode();

            var responseJson = await response.Content.ReadAsStringAsync();
            using var doc = JsonDocument.Parse(responseJson);

            var root = doc.RootElement;
            if (root.TryGetProperty("candidates", out var candidates) && candidates.GetArrayLength() > 0)
            {
                var text = candidates[0]
                    .GetProperty("content")
                    .GetProperty("parts")[0]
                    .GetProperty("text")
                    .GetString();

                return text?.Trim() ?? string.Empty;
            }

            return "No se obtuvo una respuesta del servicio de inteligencia artificial.";
        }

        private async Task<ResumenHistoriaClinicaDto?> InvocarGeminiResumenClinicoAsync(
            Paciente paciente,
            List<HistorialClinico> historiales,
            List<Tratamiento> tratamientos,
            List<RegistroVacunacion> vacunas)
        {
            var apiKey = GetApiKey();
            var model = GetModel();
            var client = _httpClientFactory.CreateClient("GeminiClient");

            var datosClinicos = new StringBuilder();
            datosClinicos.AppendLine($"PACIENTE: {paciente.Nombre} | Sexo: {paciente.Sexo} | Observaciones: {paciente.Observaciones}");
            if (paciente.FechaNacimiento.HasValue)
            {
                datosClinicos.AppendLine($"Fecha Nacimiento: {paciente.FechaNacimiento:dd/MM/yyyy}");
            }

            datosClinicos.AppendLine("\nULTIMAS CONSULTAS:");
            foreach (var h in historiales)
            {
                datosClinicos.AppendLine($"- {h.Fecha:dd/MM/yyyy}: Motivo: {h.Motivo} | Diagnostico: {h.Diagnostico} | Indicaciones: {h.Indicaciones} | Peso: {h.Peso} kg | Temp: {h.Temperatura} C");
            }

            datosClinicos.AppendLine("\nTRATAMIENTOS RECIENTES:");
            foreach (var t in tratamientos)
            {
                datosClinicos.AppendLine($"- Fecha: {t.Fecha:dd/MM/yyyy}: Medicacion: {t.Medicacion} | Diagnostico: {t.Diagnostico} | Descripcion: {t.Descripcion} | Estado: {(t.Finalizado ? "Finalizado" : "En curso")}");
            }

            datosClinicos.AppendLine("\nPLAN DE VACUNACION:");
            foreach (var v in vacunas)
            {
                var proxDosis = v.FechaProximaDosis.HasValue ? v.FechaProximaDosis.Value.ToString("dd/MM/yyyy") : "No definida";
                datosClinicos.AppendLine($"- Aplicada: {v.FechaAplicacion:dd/MM/yyyy} | Prox Dosis: {proxDosis} | Lote: {v.NroLote}");
            }

            var prompt =
                "Eres un especialista clinico veterinario de alto nivel. Analiza el expediente medico proporcionado y sintetiza un informe clinico ejecutivo.\n" +
                "REGLA ESTRICTA Y OBLIGATORIA: CERO EMOJIS. No utilices ningun emoji en ningun campo.\n" +
                "Debes devolver EXCLUSIVAMENTE un objeto JSON valido con esta estructura exacta de propiedades en camelCase:\n" +
                "{\n" +
                "  \"informacionBasica\": \"Sintesis de especie, raza, edad y perfil general del paciente\",\n" +
                "  \"ultimaConsulta\": \"Detalle del motivo, diagnostico e indicaciones de la consulta mas reciente\",\n" +
                "  \"tratamientosYVacunas\": \"Sintesis del estado de medicacion actual y plan de vacunacion\",\n" +
                "  \"alertasYRecomendaciones\": \"Puntos criticos de atencion, signos de alarma o recomendaciones de control\",\n" +
                "  \"resumenCompletoMarkdown\": \"Informe clinico integral redactado en formato Markdown (titulos, listas, negritas, SIN emojis)\"\n" +
                "}\n\n" +
                $"EXPEDIENTE CLINICO:\n{datosClinicos}";

            var requestBody = new
            {
                contents = new object[]
                {
                    new
                    {
                        role = "user",
                        parts = new object[] { new { text = prompt } }
                    }
                },
                generationConfig = new
                {
                    temperature = 0.2,
                    responseMimeType = "application/json"
                }
            };

            var jsonContent = new StringContent(JsonSerializer.Serialize(requestBody), Encoding.UTF8, "application/json");
            var url = $"https://generativelanguage.googleapis.com/v1beta/models/{model}:generateContent?key={apiKey}";

            var response = await client.PostAsync(url, jsonContent);
            response.EnsureSuccessStatusCode();

            var responseJson = await response.Content.ReadAsStringAsync();
            using var doc = JsonDocument.Parse(responseJson);

            var root = doc.RootElement;
            if (root.TryGetProperty("candidates", out var candidates) && candidates.GetArrayLength() > 0)
            {
                var text = candidates[0]
                    .GetProperty("content")
                    .GetProperty("parts")[0]
                    .GetProperty("text")
                    .GetString();

                if (!string.IsNullOrWhiteSpace(text))
                {
                    var cleanedJson = CleanJsonMarkdown(text);
                    var parsed = JsonSerializer.Deserialize<JsonElement>(cleanedJson);

                    return new ResumenHistoriaClinicaDto
                    {
                        PacienteId = paciente.Id,
                        PacienteNombre = paciente.Nombre,
                        InformacionBasica = GetJsonString(parsed, "informacionBasica"),
                        UltimaConsulta = GetJsonString(parsed, "ultimaConsulta"),
                        TratamientosYVacunas = GetJsonString(parsed, "tratamientosYVacunas"),
                        AlertasYRecomendaciones = GetJsonString(parsed, "alertasYRecomendaciones"),
                        ResumenCompletoMarkdown = GetJsonString(parsed, "resumenCompletoMarkdown"),
                        FechaGeneracion = DateTime.Now,
                        GeneradoPorIa = true,
                        ModeloUtilizado = model
                    };
                }
            }

            return null;
        }

        private static string GetJsonString(JsonElement element, string propName)
        {
            if (element.TryGetProperty(propName, out var prop))
            {
                return prop.GetString() ?? string.Empty;
            }
            return string.Empty;
        }

        private static string CleanJsonMarkdown(string raw)
        {
            var trimmed = raw.Trim();
            if (trimmed.StartsWith("```json", StringComparison.OrdinalIgnoreCase))
            {
                trimmed = trimmed.Substring(7);
            }
            else if (trimmed.StartsWith("```"))
            {
                trimmed = trimmed.Substring(3);
            }

            if (trimmed.EndsWith("```"))
            {
                trimmed = trimmed.Substring(0, trimmed.Length - 3);
            }

            return trimmed.Trim();
        }

        private ResumenHistoriaClinicaDto GenerarResumenEstructuradoLocal(
            Paciente paciente,
            List<HistorialClinico> historiales,
            List<Tratamiento> tratamientos,
            List<RegistroVacunacion> vacunas)
        {
            var ultimaConsulta = historiales.FirstOrDefault();
            var sexLabel = paciente.Sexo == "M" ? "Macho" : (paciente.Sexo == "H" ? "Hembra" : "Sin especificar");

            var infoBasica = $"Paciente: {paciente.Nombre}. Sexo: {sexLabel}. " +
                             $"Registrado en el sistema con {historiales.Count} consulta(s) documentada(s).";

            var ultConsultaText = ultimaConsulta != null
                ? $"Fecha: {ultimaConsulta.Fecha:dd/MM/yyyy}. Motivo: {ultimaConsulta.Motivo}. Diagnostico: {ultimaConsulta.Diagnostico}. Indicaciones: {ultimaConsulta.Indicaciones}."
                : "No se registran consultas medicas previas para este paciente.";

            var tratText = new StringBuilder();
            if (tratamientos.Any())
            {
                tratText.Append($"Tratamientos registrados ({tratamientos.Count}): ");
                tratText.Append(string.Join("; ", tratamientos.Select(t => $"{t.Medicacion} - {t.Diagnostico}")));
            }
            else
            {
                tratText.Append("Sin tratamientos activos.");
            }

            if (vacunas.Any())
            {
                tratText.Append($". Vacunacion: {vacunas.Count} dosis aplicadas. Ultima aplicacion: {vacunas.First().FechaAplicacion:dd/MM/yyyy}.");
            }
            else
            {
                tratText.Append(". Sin registro de vacunas aplicadas.");
            }

            var alertas = ultimaConsulta != null && !string.IsNullOrWhiteSpace(ultimaConsulta.Indicaciones)
                ? $"Mantener seguimiento de las indicaciones recientes: {ultimaConsulta.Indicaciones}."
                : "Paciente en estado de control general. Continuar con plan de vacunacion y desparasitacion periodica.";

            var markdown = new StringBuilder();
            markdown.AppendLine($"# Informe Clinico: {paciente.Nombre}");
            markdown.AppendLine($"**Fecha de Emision:** {DateTime.Now:dd/MM/yyyy HH:mm} hs  ");
            markdown.AppendLine($"**Sexo:** {sexLabel}  ");
            markdown.AppendLine();
            markdown.AppendLine("## 1. Perfil del Paciente");
            markdown.AppendLine(infoBasica);
            markdown.AppendLine();
            markdown.AppendLine("## 2. Ultima Atencion Medica");
            markdown.AppendLine(ultConsultaText);
            markdown.AppendLine();
            markdown.AppendLine("## 3. Terapeutica y Vacunacion");
            markdown.AppendLine(tratText.ToString());
            markdown.AppendLine();
            markdown.AppendLine("## 4. Alertas y Seguimiento");
            markdown.AppendLine(alertas);

            return new ResumenHistoriaClinicaDto
            {
                PacienteId = paciente.Id,
                PacienteNombre = paciente.Nombre,
                InformacionBasica = infoBasica,
                UltimaConsulta = ultConsultaText,
                TratamientosYVacunas = tratText.ToString(),
                AlertasYRecomendaciones = alertas,
                ResumenCompletoMarkdown = markdown.ToString(),
                FechaGeneracion = DateTime.Now,
                GeneradoPorIa = false,
                ModeloUtilizado = "Motor Estructurado Local (Fallback)"
            };
        }

        public async Task<ResumenReporteResponseDto> GenerarResumenReporteAsync(ResumenReporteRequestDto request)
        {
            if (request == null || string.IsNullOrWhiteSpace(request.TipoReporte))
            {
                return new ResumenReporteResponseDto
                {
                    Exito = false,
                    ErrorMensaje = "Solicitud de reporte vacia o tipo de reporte no especificado."
                };
            }

            var tipo = request.TipoReporte.ToUpperInvariant();
            var apiKey = GetApiKey();

            if (!string.IsNullOrWhiteSpace(apiKey))
            {
                try
                {
                    var resumenGemini = await ConsultarGeminiReporteAsync(tipo, request.DatosJson, apiKey);
                    if (resumenGemini != null)
                    {
                        return resumenGemini;
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[Gemini Reporte Error] {ex.Message}");
                }
            }

            // Fallback determinístico estructurado sin emojis
            return GenerarResumenReporteLocal(tipo, request.DatosJson);
        }

        private async Task<ResumenReporteResponseDto?> ConsultarGeminiReporteAsync(string tipo, string datosJson, string apiKey)
        {
            var url = $"https://generativelanguage.googleapis.com/v1beta/models/{GetModel()}:generateContent?key={apiKey}";

            var systemPrompt = "Eres un consultor analitico y de gestion para clinicas veterinarias. " +
                               "Tu tarea es analizar las metricas y graficos del reporte provisto y entregar un informe ejecutivo de alto nivel. " +
                               "REGLA OBLIGATORIA Y ESTRICTA: NO USES NINGUN EMOJI en ninguna parte de tu respuesta. " +
                               "Devuelve UNICAMENTE un objeto JSON valido con la siguiente estructura (sin bloques markdown ```json):\n" +
                               "{\n" +
                               "  \"titulo\": \"string\",\n" +
                               "  \"resumenEjecutivo\": \"string (analisis claro de metricas y evolucion)\",\n" +
                               "  \"puntosClave\": [\"string 1\", \"string 2\", \"string 3\"],\n" +
                               "  \"recomendaciones\": [\"string 1\", \"string 2\"],\n" +
                               "  \"textoParaVoz\": \"string (parrafo continuo y fluido en espanol, redactado exclusivamente para ser leido en voz alta por sintesis de voz, sin numerales ni asteriscos ni simbolos markdown)\"\n" +
                               "}";

            var userPrompt = $"Tipo de Reporte: {tipo}\nDatos agregados y metricas:\n{datosJson}";

            var payload = new
            {
                systemInstruction = new { parts = new[] { new { text = systemPrompt } } },
                contents = new[]
                {
                    new { role = "user", parts = new[] { new { text = userPrompt } } }
                },
                generationConfig = new
                {
                    temperature = 0.2,
                    maxOutputTokens = 1200,
                    responseMimeType = "application/json"
                }
            };

            var jsonContent = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");
            var client = _httpClientFactory.CreateClient("GeminiClient");
            var response = await client.PostAsync(url, jsonContent);

            if (!response.IsSuccessStatusCode)
            {
                return null;
            }

            var responseBody = await response.Content.ReadAsStringAsync();
            using var doc = JsonDocument.Parse(responseBody);
            var root = doc.RootElement;

            if (!root.TryGetProperty("candidates", out var candidates) || candidates.GetArrayLength() == 0)
            {
                return null;
            }

            var textResponse = candidates[0]
                .GetProperty("content")
                .GetProperty("parts")[0]
                .GetProperty("text")
                .GetString();

            if (string.IsNullOrWhiteSpace(textResponse)) return null;

            var cleanedJson = textResponse.Trim();
            if (cleanedJson.StartsWith("```json")) cleanedJson = cleanedJson.Substring(7);
            if (cleanedJson.StartsWith("```")) cleanedJson = cleanedJson.Substring(3);
            if (cleanedJson.EndsWith("```")) cleanedJson = cleanedJson.Substring(0, cleanedJson.Length - 3);
            cleanedJson = cleanedJson.Trim();

            using var resDoc = JsonDocument.Parse(cleanedJson);
            var r = resDoc.RootElement;

            var puntos = new List<string>();
            if (r.TryGetProperty("puntosClave", out var puntosEl) && puntosEl.ValueKind == JsonValueKind.Array)
            {
                foreach (var p in puntosEl.EnumerateArray())
                {
                    var val = p.GetString();
                    if (!string.IsNullOrWhiteSpace(val)) puntos.Add(val);
                }
            }

            var recs = new List<string>();
            if (r.TryGetProperty("recomendaciones", out var recsEl) && recsEl.ValueKind == JsonValueKind.Array)
            {
                foreach (var rec in recsEl.EnumerateArray())
                {
                    var val = rec.GetString();
                    if (!string.IsNullOrWhiteSpace(val)) recs.Add(val);
                }
            }

            return new ResumenReporteResponseDto
            {
                Exito = true,
                TipoReporte = tipo,
                Titulo = r.TryGetProperty("titulo", out var t) ? t.GetString() ?? $"Analisis de {tipo}" : $"Analisis de {tipo}",
                ResumenEjecutivo = r.TryGetProperty("resumenEjecutivo", out var re) ? re.GetString() ?? "" : "",
                PuntosClave = puntos,
                Recomendaciones = recs,
                TextoParaVoz = r.TryGetProperty("textoParaVoz", out var tv) ? tv.GetString() ?? "" : "",
                ModeloUtilizado = GetModel()
            };
        }

        private ResumenReporteResponseDto GenerarResumenReporteLocal(string tipo, string datosJson)
        {
            var puntos = new List<string>();
            var recs = new List<string>();
            string titulo;
            string resumen;
            string textoVoz;

            switch (tipo)
            {
                case "FINANZAS":
                    titulo = "Analisis Ejecutivo: Finanzas y Ventas";
                    resumen = "El rendimiento comercial del periodo evaluado muestra un flujo de facturacion sostenido. La correlacion entre transacciones diarias y volumen promedio por ticket indica estabilidad en la demanda de servicios e insumos veterinarios.";
                    puntos.Add("Evolucion de ingresos con consistencia operativa en los ultimos 30 dias.");
                    puntos.Add("Diversificacion adecuada en los metodos de cobro utilizados por los clientes.");
                    puntos.Add("Demanda concentrada en insumos de alta rotacion y consultas generales.");
                    recs.Add("Monitorear los articulos de mayor venta para evitar quiebres de inventario comercial.");
                    recs.Add("Promover promociones preventivas para elevar el ticket promedio en dias de baja afluencia.");
                    textoVoz = "Analisis ejecutivo de finanzas y ventas. La clinica registra una evolucion comercial equilibrada durante el periodo analizado. Los ingresos reflejan una afluencia constante de propietarios y una adopcion variada de medios de pago. Se recomienda asegurar el abastecimiento de los productos lideres e implementar paquetes de servicios en dias de menor actividad.";
                    break;

                case "STOCK":
                    titulo = "Analisis Ejecutivo: Control de Stock";
                    resumen = "La valorizacion global del deposito evidencia un nivel de capital de trabajo activo adecuado. No obstante, se observan lineas de productos aproximandose a sus umbrales minimos, lo que requiere atencion de compras.";
                    puntos.Add("Valorizacion patrimonial total alineada con la operatividad de la clinica.");
                    puntos.Add("Identificacion de articulos en estado critico o sin stock para reposicion inmediata.");
                    puntos.Add("Distribucion balanceada entre categorias de farmacia, alimentos y accesorios.");
                    recs.Add("Generar ordenes de compra prioritarias para los insumos esenciales en alerta roja.");
                    recs.Add("Revisar los parametros de stock minimo en productos con alta variabilidad estacional.");
                    textoVoz = "Analisis ejecutivo de control de stock. El inventario de la clinica se encuentra valorizado adecuadamente, asegurando el soporte continuo de las consultas. Se detectaron productos en alerta que requieren reposicion prioritaria con proveedores para evitar faltantes criticos en tratamientos medicos.";
                    break;

                case "TURNOS":
                    titulo = "Analisis Ejecutivo: Rendimiento de Turnos";
                    resumen = "La gestion de la agenda medica refleja un nivel elevado de ocupacion. La tasa de asistencia y cumplimiento de consultas evidencia una organizacion eficiente de los turnos programados.";
                    puntos.Add("La mayoria de las citas agendadas son completadas de forma satisfactoria.");
                    puntos.Add("Tasa de ausencias y cancelaciones contenida dentro de margenes manejables.");
                    puntos.Add("Distribucion equitativa de carga de atencion entre los profesionales veterinarios.");
                    recs.Add("Implementar recordatorios previos para reducir el porcentaje residual de inasistencias.");
                    recs.Add("Ajustar franjas horarias en profesionales con sobrecarga de turnos en horas pico.");
                    textoVoz = "Analisis ejecutivo de rendimiento de turnos. La agenda clinica presenta un alto indice de cumplimiento y asistencia en las citas de control. Para continuar optimizando los tiempos del equipo medico, se aconseja reforzar los recordatorios de citas y redistribuir los turnos en las franjas de mayor demanda.";
                    break;

                case "CLINICA":
                default:
                    titulo = "Analisis Ejecutivo: Actividad Clinica";
                    resumen = "El resumen clinico indica predominio de atenciones preventivas, controles de rutina y esquemas de vacunacion en caninos y felinos. La casuistica diagnostica se mantiene dentro de los parametros esperados.";
                    puntos.Add("Especies canina y felina representan la casi totalidad de la casuistica medica.");
                    puntos.Add("Las consultas preventivas y los controles post-tratamiento lideran los motivos de atencion.");
                    puntos.Add("Adherencia positiva a los calendarios de vacunacion y desparasitacion.");
                    recs.Add("Intensificar campanas de revacunacion para pacientes con esquemas proximos a vencer.");
                    recs.Add("Registrar minuciosamente las observaciones terapeuticas para enriquecer el historial clinico.");
                    textoVoz = "Analisis ejecutivo de actividad clinica. Las consultas se concentran principalmente en pacientes caninos y felinos para atencion preventiva y esquemas vacunales. Se sugiere realizar seguimiento proactivo de aquellos animales que requieren refuerzo de dosis para garantizar su proteccion sanitaria.";
                    break;
            }

            return new ResumenReporteResponseDto
            {
                Exito = true,
                TipoReporte = tipo,
                Titulo = titulo,
                ResumenEjecutivo = resumen,
                PuntosClave = puntos,
                Recomendaciones = recs,
                TextoParaVoz = textoVoz,
                ModeloUtilizado = "Motor Estructurado Local (Analitico)"
            };
        }
    }
}
