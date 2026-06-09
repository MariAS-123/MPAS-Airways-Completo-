using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;

namespace Marketplace.EventGateway.Api.Services;

public sealed class VuelosBookingClient
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<VuelosBookingClient> _logger;

    public VuelosBookingClient(HttpClient httpClient, ILogger<VuelosBookingClient> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    public async Task<string?> BuscarAeropuertosAsync(
        string? nombre,
        int? idPais,
        int limit,
        CancellationToken cancellationToken = default)
    {
        var query = BuildQuery(new Dictionary<string, string?>
        {
            ["nombre"] = nombre,
            ["idPais"] = idPais?.ToString(),
            ["limit"] = limit.ToString()
        });

        return await GetDataJsonAsync($"api/v1/booking/aeropuertos{query}", cancellationToken);
    }

    public async Task<string?> BuscarVuelosAsync(
        string? origen,
        string? destino,
        DateOnly? fecha,
        int? idAeropuertoOrigen,
        int? idAeropuertoDestino,
        CancellationToken cancellationToken = default)
    {
        var query = BuildQuery(new Dictionary<string, string?>
        {
            ["origen"] = origen,
            ["destino"] = destino,
            ["fecha"] = fecha?.ToString("yyyy-MM-dd"),
            ["idAeropuertoOrigen"] = idAeropuertoOrigen?.ToString(),
            ["idAeropuertoDestino"] = idAeropuertoDestino?.ToString()
        });

        return await GetDataJsonAsync($"api/v1/booking/vuelos/buscar{query}", cancellationToken);
    }

    public async Task<string?> ObtenerVueloAsync(int idVuelo, CancellationToken cancellationToken = default) =>
        await GetDataJsonAsync($"api/v1/booking/vuelos/{idVuelo}", cancellationToken);

    public async Task<string?> ObtenerAsientosAsync(
        int idVuelo,
        string? clase,
        bool? disponible,
        CancellationToken cancellationToken = default)
    {
        var query = BuildQuery(new Dictionary<string, string?>
        {
            ["clase"] = clase,
            ["disponible"] = disponible?.ToString()?.ToLowerInvariant()
        });

        return await GetDataJsonAsync($"api/v1/booking/vuelos/{idVuelo}/asientos{query}", cancellationToken);
    }

    private async Task<string?> GetDataJsonAsync(string relativeUrl, CancellationToken cancellationToken)
    {
        var requestUri = _httpClient.BaseAddress is null
            ? relativeUrl
            : new Uri(_httpClient.BaseAddress, relativeUrl).ToString();

        try
        {
            using var response = await _httpClient.GetAsync(relativeUrl, cancellationToken);

            if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                _logger.LogWarning("Vuelos GET 404 Not Found: {Url}", requestUri);
                return null;
            }

            response.EnsureSuccessStatusCode();

            var payload = await response.Content.ReadFromJsonAsync<ApiEnvelope<JsonElement>>(cancellationToken);
            return payload?.Data.GetRawText();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error HTTP Vuelos GET {Url}", requestUri);
            throw;
        }
    }

    private static string BuildQuery(IReadOnlyDictionary<string, string?> parameters)
    {
        var parts = parameters
            .Where(x => !string.IsNullOrWhiteSpace(x.Value))
            .Select(x => $"{Uri.EscapeDataString(x.Key)}={Uri.EscapeDataString(x.Value!)}")
            .ToArray();

        return parts.Length == 0 ? string.Empty : "?" + string.Join("&", parts);
    }

    private sealed class ApiEnvelope<T>
    {
        public bool Success { get; set; }
        public T? Data { get; set; }
        public string Message { get; set; } = string.Empty;
    }
}

public sealed class SeguridadAuthClient
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<SeguridadAuthClient> _logger;

    public SeguridadAuthClient(HttpClient httpClient, ILogger<SeguridadAuthClient> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    public async Task<LoginResultGql?> LoginAsync(
        string username,
        string password,
        CancellationToken cancellationToken = default)
    {
        try
        {
            using var response = await _httpClient.PostAsJsonAsync(
                "api/v1/auth/login",
                new { username, password },
                cancellationToken);

            response.EnsureSuccessStatusCode();

            var envelope = await response.Content.ReadFromJsonAsync<ApiEnvelope<LoginResultGql>>(cancellationToken);
            return envelope?.Data;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error login Seguridad usuario={Username}", username);
            throw;
        }
    }

    private sealed class ApiEnvelope<T>
    {
        public bool Success { get; set; }
        public T? Data { get; set; }
    }
}

public sealed class LoginResultGql
{
    public string Token { get; set; } = string.Empty;
    public string Usuario { get; set; } = string.Empty;
    public DateTime Expiracion { get; set; }
    public List<string> Roles { get; set; } = [];
}

public interface IUserTokenAccessor
{
    string? Token { get; }
}

public sealed class HttpUserTokenAccessor(IHttpContextAccessor httpContextAccessor) : IUserTokenAccessor
{
    public string? Token
    {
        get
        {
            var authorization = httpContextAccessor.HttpContext?.Request.Headers.Authorization.ToString();
            if (string.IsNullOrWhiteSpace(authorization))
                return null;

            return authorization.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase)
                ? authorization["Bearer ".Length..].Trim()
                : authorization.Trim();
        }
    }
}
