using Application.DataTransferObjects;

namespace Application.Repositories
{
    public interface ITwilioWhatsAppService
    {
        Task<WhatsAppResponseDto> SendWhatsAppAsync(string telefono, string mensaje);
        Task<TwilioStatusDto> GetStatusAsync();
    }
}
