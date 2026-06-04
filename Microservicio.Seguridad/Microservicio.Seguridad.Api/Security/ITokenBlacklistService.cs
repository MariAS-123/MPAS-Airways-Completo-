namespace Microservicio.Seguridad.Api.Security;

public interface ITokenBlacklistService
{
    void Blacklist(string token, DateTimeOffset expiresAtUtc);
    bool IsBlacklisted(string token);
}