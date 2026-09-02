namespace Application.DataTransferObjects
{
    public class TwilioSettings
    {
        public string AccountSid { get; set; } = string.Empty;
        public string AuthToken { get; set; } = string.Empty;
        public string FromPhoneNumber { get; set; } = "whatsapp:+14155238886";
        public string DefaultCountryCode { get; set; } = "+549";
        public bool EnableTwilio { get; set; } = true;
    }

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
