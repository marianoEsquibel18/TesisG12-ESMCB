namespace BlazorFrontEnd.Models
{
    public class SendWhatsAppRequest
    {
        public string Telefono { get; set; } = string.Empty;
        public string Mensaje { get; set; } = string.Empty;
    }

    public class WhatsAppResponseDto
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public string? MessageSid { get; set; }
        public string? FormattedPhone { get; set; }
        public string? Status { get; set; }
    }

    public class TwilioStatusDto
    {
        public bool IsConfigured { get; set; }
        public bool EnableTwilio { get; set; }
        public string FromPhoneNumber { get; set; } = string.Empty;
        public string DefaultCountryCode { get; set; } = string.Empty;
    }
}
