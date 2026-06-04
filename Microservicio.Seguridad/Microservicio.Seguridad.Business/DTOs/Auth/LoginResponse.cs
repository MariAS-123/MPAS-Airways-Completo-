namespace Microservicio.Seguridad.Business.DTOs.Auth;

public class LoginResponse
{
    public string Token { get; set; } = null!;
    public string Usuario { get; set; } = null!;
    public DateTime Expiracion { get; set; }
    public List<string> Roles { get; set; } = new();
}