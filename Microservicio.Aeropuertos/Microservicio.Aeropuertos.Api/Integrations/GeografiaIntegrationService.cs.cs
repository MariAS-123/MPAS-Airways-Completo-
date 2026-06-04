using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

using Microservicio.Aeropuertos.Business.Integrations;
using Microservicio.Aeropuertos.Business.Integrations.Interfaces;

namespace Microservicio.Aeropuertos.Api.Integrations;

public class GeografiaIntegrationService
    : IGeografiaIntegrationService
{
    private readonly HttpClient _httpClient;

    private readonly ILogger<GeografiaIntegrationService> _logger;

    public GeografiaIntegrationService(
        HttpClient httpClient,
        ILogger<GeografiaIntegrationService> logger)
    {
        _httpClient = httpClient;

        _logger = logger;
    }

    public async Task<PaisIntegrationDto?> GetPaisAsync(
        int idPais,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var response =
                await _httpClient.GetAsync(
                    $"api/v1/paises/{idPais}",
                    cancellationToken);

            if (response.StatusCode == HttpStatusCode.NotFound)
            {
                return null;
            }

            response.EnsureSuccessStatusCode();

            var json =
                await response.Content.ReadAsStringAsync(
                    cancellationToken);

            using var document =
                JsonDocument.Parse(json);

            var data =
                document.RootElement
                    .GetProperty("data");

            return new PaisIntegrationDto
            {
                IdPais =
                    data.GetProperty("idPais").GetInt32(),

                Nombre =
                    data.GetProperty("nombre").GetString()
                    ?? string.Empty,

                Estado =
                    data.GetProperty("estado").GetString()
                    ?? string.Empty
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Error consultando país {IdPais} en MS Geografía.",
                idPais);

            throw;
        }
    }

    public async Task<CiudadIntegrationDto?> GetCiudadAsync(
        int idCiudad,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var response =
                await _httpClient.GetAsync(
                    $"api/v1/ciudades/{idCiudad}",
                    cancellationToken);

            if (response.StatusCode == HttpStatusCode.NotFound)
            {
                return null;
            }

            response.EnsureSuccessStatusCode();

            var json =
                await response.Content.ReadAsStringAsync(
                    cancellationToken);

            using var document =
                JsonDocument.Parse(json);

            var data =
                document.RootElement
                    .GetProperty("data");

            return new CiudadIntegrationDto
            {
                IdCiudad =
                    data.GetProperty("idCiudad").GetInt32(),

                IdPais =
                    data.GetProperty("idPais").GetInt32(),

                Nombre =
                    data.GetProperty("nombre").GetString()
                    ?? string.Empty,

                Estado =
                    data.GetProperty("estado").GetString()
                    ?? string.Empty
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Error consultando ciudad {IdCiudad} en MS Geografía.",
                idCiudad);

            throw;
        }
    }
}