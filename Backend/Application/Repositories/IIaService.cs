using System.Threading.Tasks;
using Application.DataTransferObjects;

namespace Application.Repositories
{
    public interface IIaService
    {
        Task<IaStatusDto> GetStatusAsync();
        Task<IaStatusDto> IsConfiguredAsync();
        Task<ChatbotResponseDto> ProcesarMensajeChatAsync(ChatbotRequestDto request, string usuarioNombre, string usuarioRol, int? sucursalId);
        Task<ChatbotResponseDto> ConfirmarTurnoPropuestoAsync(TurnoPropuestoDto turnoDto, string usuarioRol, int? sucursalId);
        Task<ResumenHistoriaClinicaDto> GenerarResumenHistorialAsync(string pacienteId);
        Task<ResumenReporteResponseDto> GenerarResumenReporteAsync(ResumenReporteRequestDto request);
    }
}
