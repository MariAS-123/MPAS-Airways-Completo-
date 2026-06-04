using Microservicio.Seguridad.Business.DTOs.Auth;

namespace Microservicio.Seguridad.Business.Interfaces;

public interface IAuthService
{
    Task<LoginResponse> LoginAsync(LoginRequest request);
    Task<RegisterClienteResponse> RegisterClienteAsync(RegisterClienteRequest request);
    Task LogoutAsync(string token, string ejecutadoPorUsuario);
}