namespace Microservicio.Seguridad.Api.Models.Settings;

public class JwtSettings
{
    public const string SectionName = "JwtSettings";

    public string Secret { get; set; } = string.Empty;
    // Renombrado de SecretKey a Secret — coincide con appsettings y arquitectura

    public string Issuer { get; set; } = string.Empty;

    public string Audience { get; set; } = string.Empty;

    public int ExpirationMinutes { get; set; }
}