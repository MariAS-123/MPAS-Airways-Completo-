using Microservicio.Seguridad.Business.DTOs.Auth;
using Microservicio.Seguridad.DataManagement.Models;

namespace Microservicio.Seguridad.Business.Mappers;

public static class AuthBusinessMapper
{
    public static LoginResponse ToLoginResponse(
        UsuarioAppDataModel user,
        string token,
        DateTime expiracion)
    {
        return new LoginResponse
        {
            Usuario = user.Username,
            Token = token,
            Expiracion = expiracion,
            Roles = user.Roles
        };
    }
}