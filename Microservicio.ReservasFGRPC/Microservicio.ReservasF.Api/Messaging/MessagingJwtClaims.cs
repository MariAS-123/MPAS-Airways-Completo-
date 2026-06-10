using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

namespace Microservicio.ReservasF.Api.Messaging;

internal static class MessagingJwtClaims
{
    public static (int? IdCliente, string Rol) Parse(string? accessToken, int idClienteMensaje)
    {
        if (string.IsNullOrWhiteSpace(accessToken))
            return (idClienteMensaje, "ADMINISTRADOR");

        var handler = new JwtSecurityTokenHandler();
        if (!handler.CanReadToken(accessToken))
            return (idClienteMensaje, "ADMINISTRADOR");

        var jwt = handler.ReadJwtToken(accessToken);
        var rol = jwt.Claims.FirstOrDefault(c =>
                c.Type == ClaimTypes.Role || c.Type.Equals("role", StringComparison.OrdinalIgnoreCase))
            ?.Value ?? "ADMINISTRADOR";

        var idClienteClaim = jwt.Claims.FirstOrDefault(c => c.Type == "id_cliente")?.Value;
        int? idCliente = int.TryParse(idClienteClaim, out var parsed) ? parsed : idClienteMensaje;

        return (idCliente, rol);
    }
}
