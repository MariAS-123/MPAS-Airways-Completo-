namespace Microservicio.Clientes.Api.Models.Settings;

public class JwtSettings
{
    public const string SectionName = "JwtSettings";

    // ✅ Cambiado de SecretKey a Secret — coincide con appsettings y demás MS
    public string Secret { get; set; } = string.Empty;

    public string Issuer { get; set; } = string.Empty;

    public string Audience { get; set; } = string.Empty;

    public int ExpirationMinutes { get; set; }
}