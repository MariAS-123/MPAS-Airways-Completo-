using Microservicio.Clientes.Business.Exceptions;
using Microservicio.Clientes.Business.Integrations;
using Microservicio.Clientes.Business.Integrations.Interfaces;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Microservicio.Clientes.Api.Integrations;

public class SeguridadIntegrationService : ISeguridadIntegrationService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<SeguridadIntegrationService> _logger;

    public SeguridadIntegrationService(
        IHttpClientFactory httpClientFactory,
        ILogger<SeguridadIntegrationService> logger)
    {
        _httpClient = httpClientFactory.CreateClient("seguridad");
        _logger = logger;
    }

    public async Task<UsuarioAppIntegrationResponseDto> CrearUsuarioAppAsync(
        UsuarioAppIntegrationRequestDto request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var response = await _httpClient.PostAsJsonAsync(
                "/api/v1/auth/register-cliente",
                request,
                cancellationToken);

            if (response.IsSuccessStatusCode)
            {
                var content = await response.Content.ReadFromJsonAsync<ApiResponseWrapper<UsuarioAppIntegrationResponseDto>>(
                    cancellationToken: cancellationToken);

                if (content?.Data is null)
                    throw new BusinessException("MS Seguridad retornó una respuesta vacía al crear el usuario.");

                return content.Data;
            }

            // Leer el mensaje de error del MS Seguridad
            var errorContent = await response.Content.ReadAsStringAsync(cancellationToken);
            _logger.LogError("MS Seguridad retornó {StatusCode}: {Error}",
                (int)response.StatusCode, errorContent);

            throw new BusinessException(
                $"No se pudo crear el usuario en MS Seguridad. " +
                $"Estado: {(int)response.StatusCode}. Detalle: {errorContent}");
        }
        catch (BusinessException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error de conexión al intentar crear usuario en MS Seguridad");
            throw new BusinessException(
                "No se pudo conectar con MS Seguridad. Verifique que el servicio esté disponible.");
        }
    }

    // Wrapper para deserializar la respuesta estándar ApiResponse<T> del MS Seguridad
    private class ApiResponseWrapper<T>
    {
        [JsonPropertyName("data")]
        public T? Data { get; set; }

        [JsonPropertyName("message")]
        public string? Message { get; set; }
    }
}