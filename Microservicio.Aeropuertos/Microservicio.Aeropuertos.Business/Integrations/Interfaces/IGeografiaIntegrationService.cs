using Microservicio.Aeropuertos.Business.Integrations;

namespace Microservicio.Aeropuertos.Business.Integrations.Interfaces;

public interface IGeografiaIntegrationService
{
    Task<PaisIntegrationDto?> GetPaisAsync(
        int idPais,
        CancellationToken cancellationToken = default);

    Task<CiudadIntegrationDto?> GetCiudadAsync(
        int idCiudad,
        CancellationToken cancellationToken = default);
}