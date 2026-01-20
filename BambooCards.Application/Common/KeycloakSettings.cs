namespace BambooCards.Application.Common
{
    public class KeycloakSettings
    {
        public string Authority { get; set; } = string.Empty;
        public string ClientId { get; set; } = string.Empty;
        public string ClientSecret { get; set; } = string.Empty;
        public bool RequireHttpsMetadata { get; set; } = true;
        public string Audience { get; set; } = string.Empty;
    }
}
