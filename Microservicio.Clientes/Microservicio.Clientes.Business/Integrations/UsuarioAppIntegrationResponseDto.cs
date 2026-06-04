using System.Text.Json.Serialization;

namespace Microservicio.Clientes.Business.Integrations;

public class UsuarioAppIntegrationResponseDto
{
    [JsonPropertyName("idUsuario")]
    public int IdUsuario { get; set; }

    [JsonPropertyName("username")]
    public string Username { get; set; } = null!;

    [JsonPropertyName("rolAsignado")]
    public string RolAsignado { get; set; } = null!;
}