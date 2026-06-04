using System.Text.Json.Serialization;

namespace Microservicio.Seguridad.Business.DTOs.Auth;

public class LoginRequest
{
    [JsonPropertyName("username")]
    public string Username { get; set; } = null!;

    [JsonPropertyName("password")]
    public string Password { get; set; } = null!;
}