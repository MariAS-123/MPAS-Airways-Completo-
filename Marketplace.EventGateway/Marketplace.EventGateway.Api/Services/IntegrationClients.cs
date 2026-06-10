using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Marketplace.EventGateway.Api.GraphQL;

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

    /// <summary>
    /// Equivalente a Vue GET /vuelos?estado_vuelo=PROGRAMADO (listado sin filtros de ruta).
    /// </summary>
    public async Task<string?> ListarVuelosProgramadosAsync(
        int page = 1,
        int pageSize = 200,
        CancellationToken cancellationToken = default)
    {
        var query = BuildQuery(new Dictionary<string, string?>
        {
            ["estado_vuelo"] = "PROGRAMADO",
            ["page"] = page.ToString(),
            ["page_size"] = pageSize.ToString()
        });

        return await GetDataJsonAsync($"api/v1/vuelos{query}", cancellationToken);
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

/// <summary>
/// Catálogo de aeropuertos — equivalente a Vue GET /aeropuertos vía Middleware.
/// </summary>
public sealed class AeropuertosCatalogClient
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<AeropuertosCatalogClient> _logger;

    public AeropuertosCatalogClient(HttpClient httpClient, ILogger<AeropuertosCatalogClient> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    public async Task<string?> ListarActivosAsync(
        int page = 1,
        int pageSize = 200,
        CancellationToken cancellationToken = default)
    {
        var query = $"?page={page}&page_size={pageSize}&estado=ACTIVO";

        try
        {
            using var response = await _httpClient.GetAsync($"api/v1/aeropuertos{query}", cancellationToken);
            response.EnsureSuccessStatusCode();

            var payload = await response.Content.ReadFromJsonAsync<ApiEnvelope<JsonElement>>(cancellationToken);
            return payload?.Data.GetRawText();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error HTTP Aeropuertos GET catalogo");
            throw;
        }
    }

    private sealed class ApiEnvelope<T>
    {
        public bool Success { get; set; }
        public T? Data { get; set; }
    }
}

public sealed class ClientesPortalClient
{
    private readonly HttpClient _httpClient;
    private readonly IUserTokenAccessor _tokenAccessor;
    private readonly ILogger<ClientesPortalClient> _logger;

    public ClientesPortalClient(
        HttpClient httpClient,
        IUserTokenAccessor tokenAccessor,
        ILogger<ClientesPortalClient> logger)
    {
        _httpClient = httpClient;
        _tokenAccessor = tokenAccessor;
        _logger = logger;
    }

    /// <summary>
    /// Equivalente a Vue POST /auth/register-cliente (Middleware → MS Clientes portal/registro).
    /// </summary>
    public async Task<string?> RegistrarClienteAsync(
        RegisterClienteInputGql input,
        CancellationToken cancellationToken = default)
    {
        var body = new
        {
            tipoIdentificacion = input.TipoIdentificacion,
            numeroIdentificacion = input.NumeroIdentificacion,
            nombres = input.Nombres,
            apellidos = input.Apellidos,
            correo = input.Correo,
            telefono = input.Telefono,
            direccion = input.Direccion,
            idCiudadResidencia = input.IdCiudadResidencia,
            idPaisNacionalidad = input.IdPaisNacionalidad,
            fechaNacimiento = input.FechaNacimiento,
            genero = input.Genero,
            username = input.Username,
            password = input.Password,
        };

        try
        {
            using var response = await _httpClient.PostAsJsonAsync(
                "api/v1/clientes/portal/registro",
                body,
                cancellationToken);

            response.EnsureSuccessStatusCode();

            var payload = await response.Content.ReadFromJsonAsync<ApiEnvelope<JsonElement>>(cancellationToken);
            return payload?.Data.GetRawText();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error HTTP Clientes POST portal/registro usuario={Username}", input.Username);
            throw;
        }
    }

    /// <summary>
    /// Equivalente a Vue GET /clientes/portal/mi-perfil (rol CLIENTE, claim id_cliente en JWT).
    /// </summary>
    public async Task<string?> MiPerfilAsync(CancellationToken cancellationToken = default)
    {
        var token = _tokenAccessor.Token;
        if (string.IsNullOrWhiteSpace(token))
            throw new InvalidOperationException("Se requiere Authorization Bearer para mi-perfil.");

        using var request = new HttpRequestMessage(HttpMethod.Get, "api/v1/clientes/portal/mi-perfil");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

        try
        {
            using var response = await _httpClient.SendAsync(request, cancellationToken);
            response.EnsureSuccessStatusCode();

            var payload = await response.Content.ReadFromJsonAsync<ApiEnvelope<JsonElement>>(cancellationToken);
            return payload?.Data.GetRawText();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error HTTP Clientes GET mi-perfil");
            throw;
        }
    }

    private sealed class ApiEnvelope<T>
    {
        public bool Success { get; set; }
        public T? Data { get; set; }
    }
}

public sealed class GeografiaCatalogClient
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<GeografiaCatalogClient> _logger;

    public GeografiaCatalogClient(HttpClient httpClient, ILogger<GeografiaCatalogClient> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    public async Task<string?> ListarPaisesAsync(
        int page = 1,
        int pageSize = 200,
        CancellationToken cancellationToken = default)
    {
        var query = BuildQuery(new Dictionary<string, string?>
        {
            ["page"] = page.ToString(),
            ["page_size"] = pageSize.ToString(),
        });

        return await GetDataJsonAsync($"api/v1/paises{query}", cancellationToken);
    }

    public async Task<string?> ListarCiudadesAsync(
        int idPais,
        int page = 1,
        int pageSize = 200,
        CancellationToken cancellationToken = default)
    {
        var query = BuildQuery(new Dictionary<string, string?>
        {
            ["id_pais"] = idPais.ToString(),
            ["page"] = page.ToString(),
            ["page_size"] = pageSize.ToString(),
        });

        return await GetDataJsonAsync($"api/v1/ciudades{query}", cancellationToken);
    }

    private async Task<string?> GetDataJsonAsync(string relativeUrl, CancellationToken cancellationToken)
    {
        try
        {
            using var response = await _httpClient.GetAsync(relativeUrl, cancellationToken);
            response.EnsureSuccessStatusCode();

            var payload = await response.Content.ReadFromJsonAsync<ApiEnvelope<JsonElement>>(cancellationToken);
            return payload?.Data.GetRawText();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error HTTP Geografia GET {Url}", relativeUrl);
            throw;
        }
    }

    private static string BuildQuery(IReadOnlyDictionary<string, string?> values)
    {
        var parts = values
            .Where(pair => !string.IsNullOrWhiteSpace(pair.Value))
            .Select(pair => $"{Uri.EscapeDataString(pair.Key)}={Uri.EscapeDataString(pair.Value!)}")
            .ToArray();

        return parts.Length == 0 ? string.Empty : $"?{string.Join("&", parts)}";
    }

    private sealed class ApiEnvelope<T>
    {
        public bool Success { get; set; }
        public T? Data { get; set; }
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

/// <summary>
/// Orquestación REST de pasajeros (mismo camino que Vue) para actualizar la saga sin depender solo de RabbitMQ.
/// </summary>
public sealed class ClientesPasajerosClient
{
    private readonly HttpClient _httpClient;
    private readonly IUserTokenAccessor _tokenAccessor;
    private readonly ILogger<ClientesPasajerosClient> _logger;

    public ClientesPasajerosClient(
        HttpClient httpClient,
        IUserTokenAccessor tokenAccessor,
        ILogger<ClientesPasajerosClient> logger)
    {
        _httpClient = httpClient;
        _tokenAccessor = tokenAccessor;
        _logger = logger;
    }

    public async Task<IReadOnlyList<int>> RegistrarPasajerosAsync(
        RegistrarPasajerosInput input,
        CancellationToken cancellationToken = default)
    {
        var token = _tokenAccessor.Token
            ?? throw new InvalidOperationException("Se requiere Authorization Bearer para registrar pasajeros.");

        var ids = new List<int>();

        foreach (var pasajero in input.Pasajeros)
        {
            if (pasajero.IdPasajero is > 0)
            {
                ids.Add(pasajero.IdPasajero.Value);
                continue;
            }

            var body = new
            {
                idCliente = input.IdCliente,
                nombrePasajero = pasajero.NombrePasajero,
                apellidoPasajero = pasajero.ApellidoPasajero,
                tipoDocumentoPasajero = pasajero.TipoDocumentoPasajero,
                numeroDocumentoPasajero = pasajero.NumeroDocumentoPasajero,
                fechaNacimientoPasajero = pasajero.FechaNacimientoPasajero,
                idPaisNacionalidad = pasajero.IdPaisNacionalidad,
                emailContactoPasajero = pasajero.EmailContactoPasajero,
                telefonoContactoPasajero = pasajero.TelefonoContactoPasajero,
                generoPasajero = pasajero.GeneroPasajero,
                requiereAsistencia = pasajero.RequiereAsistencia,
                observacionesPasajero = pasajero.ObservacionesPasajero,
            };

            using var request = new HttpRequestMessage(HttpMethod.Post, "api/v1/pasajeros")
            {
                Content = JsonContent.Create(body),
            };
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

            using var response = await _httpClient.SendAsync(request, cancellationToken);
            var raw = await response.Content.ReadAsStringAsync(cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError(
                    "Clientes POST pasajeros falló status={Status} body={Body}",
                    (int)response.StatusCode,
                    raw);
                throw new InvalidOperationException(ClientesPasajerosClientHelpers.ExtractApiMessage(raw) ?? "No se pudo registrar el pasajero.");
            }

            using var document = JsonDocument.Parse(raw);
            var data = document.RootElement.GetProperty("data");
            var idPasajero = data.TryGetProperty("idPasajero", out var camel)
                ? camel.GetInt32()
                : data.GetProperty("id_pasajero").GetInt32();
            ids.Add(idPasajero);
        }

        return ids;
    }
}

/// <summary>
/// Crear y pagar reserva vía REST (equivalente al flujo Vue / handler ReservaSolicitada).
/// </summary>
public sealed class ReservasBookingClient
{
    private readonly HttpClient _httpClient;
    private readonly IUserTokenAccessor _tokenAccessor;
    private readonly ILogger<ReservasBookingClient> _logger;

    public ReservasBookingClient(
        HttpClient httpClient,
        IUserTokenAccessor tokenAccessor,
        ILogger<ReservasBookingClient> logger)
    {
        _httpClient = httpClient;
        _tokenAccessor = tokenAccessor;
        _logger = logger;
    }

    public async Task<(int IdReserva, string CodigoReserva)> CrearYPagarAsync(
        SolicitarReservaInput input,
        CancellationToken cancellationToken = default)
    {
        var token = _tokenAccessor.Token
            ?? throw new InvalidOperationException("Se requiere Authorization Bearer para solicitar la reserva.");

        // MS ReservasF espera snake_case (igual que Middleware → ReservasClient).
        var createBody = new
        {
            id_cliente = input.IdCliente,
            id_vuelo = input.IdVuelo,
            subtotal_reserva = input.SubtotalReserva,
            valor_iva = input.ValorIva,
            total_reserva = input.TotalReserva,
            origen_canal_reserva = string.IsNullOrWhiteSpace(input.OrigenCanalReserva) ? "APP" : input.OrigenCanalReserva,
            contacto_email = input.ContactoEmail,
            contacto_telefono = input.ContactoTelefono,
            observaciones = input.Observaciones,
            detalles = input.Detalles.Select(d => new
            {
                id_pasajero = d.IdPasajero,
                id_asiento = d.IdAsiento,
                subtotal_linea = d.SubtotalLinea,
                valor_iva_linea = d.ValorIvaLinea,
                total_linea = d.TotalLinea,
            }),
        };

        using var createRequest = new HttpRequestMessage(HttpMethod.Post, "api/v1/reservas")
        {
            Content = JsonContent.Create(createBody),
        };
        createRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

        using var createResponse = await _httpClient.SendAsync(createRequest, cancellationToken);
        var createRaw = await createResponse.Content.ReadAsStringAsync(cancellationToken);
        if (!createResponse.IsSuccessStatusCode)
        {
            _logger.LogError("Reservas POST falló status={Status} body={Body}", (int)createResponse.StatusCode, createRaw);
            throw new InvalidOperationException(ClientesPasajerosClientHelpers.ExtractApiMessage(createRaw) ?? "No se pudo crear la reserva.");
        }

        using var createDoc = JsonDocument.Parse(createRaw);
        var createData = createDoc.RootElement.GetProperty("data");
        var idReserva = createData.TryGetProperty("idReserva", out var idCamel)
            ? idCamel.GetInt32()
            : createData.GetProperty("id_reserva").GetInt32();

        var detalles = createData.GetProperty("detalles");
        var equipajePagar = new List<object>();
        foreach (var item in input.Equipaje)
        {
            JsonElement? detalle = null;
            foreach (var row in detalles.EnumerateArray())
            {
                var idPasajero = row.TryGetProperty("idPasajero", out var pCamel)
                    ? pCamel.GetInt32()
                    : row.GetProperty("id_pasajero").GetInt32();
                if (idPasajero == item.IdPasajero)
                {
                    detalle = row;
                    break;
                }
            }

            if (detalle is null)
                throw new InvalidOperationException($"No se encontró detalle para equipaje del pasajero {item.IdPasajero}.");

            var idDetalle = detalle.Value.TryGetProperty("idDetalle", out var dCamel)
                ? dCamel.GetInt32()
                : detalle.Value.GetProperty("id_detalle").GetInt32();

            equipajePagar.Add(new
            {
                id_detalle = idDetalle,
                tipo = item.Tipo,
                peso_kg = item.PesoKg,
                descripcion_equipaje = item.DescripcionEquipaje,
            });
        }

        var pagarBody = new
        {
            cargo_servicio = input.CargoServicio,
            equipaje = equipajePagar,
        };

        using var pagarRequest = new HttpRequestMessage(HttpMethod.Patch, $"api/v1/reservas/{idReserva}/pagar")
        {
            Content = JsonContent.Create(pagarBody),
        };
        pagarRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

        using var pagarResponse = await _httpClient.SendAsync(pagarRequest, cancellationToken);
        var pagarRaw = await pagarResponse.Content.ReadAsStringAsync(cancellationToken);
        if (!pagarResponse.IsSuccessStatusCode)
        {
            _logger.LogError("Reservas PATCH pagar falló status={Status} body={Body}", (int)pagarResponse.StatusCode, pagarRaw);
            throw new InvalidOperationException(ClientesPasajerosClientHelpers.ExtractApiMessage(pagarRaw) ?? "No se pudo pagar la reserva.");
        }

        using var pagarDoc = JsonDocument.Parse(pagarRaw);
        var pagarData = pagarDoc.RootElement.GetProperty("data").GetProperty("reserva");
        var codigoReserva = pagarData.TryGetProperty("codigoReserva", out var codeCamel)
            ? codeCamel.GetString()
            : pagarData.GetProperty("codigo_reserva").GetString();

        if (string.IsNullOrWhiteSpace(codigoReserva))
            throw new InvalidOperationException("La reserva se pagó pero no se recibió el código.");

        return (idReserva, codigoReserva);
    }
}

internal static class ClientesPasajerosClientHelpers
{
    public static string? ExtractApiMessage(string raw)
    {
        try
        {
            using var document = JsonDocument.Parse(raw);
            var root = document.RootElement;

            string? detail = null;
            if (root.TryGetProperty("errors", out var errors) && errors.ValueKind == JsonValueKind.Array)
            {
                var parts = errors.EnumerateArray()
                    .Select(item => item.GetString())
                    .Where(item => !string.IsNullOrWhiteSpace(item))
                    .ToArray();

                if (parts.Length > 0)
                    detail = string.Join(' ', parts);
            }

            var message = root.TryGetProperty("message", out var messageNode)
                ? messageNode.GetString()
                : null;

            if (!string.IsNullOrWhiteSpace(message) && !string.IsNullOrWhiteSpace(detail))
                return $"{message} {detail}";

            return message ?? detail;
        }
        catch
        {
            // ignore
        }

        return null;
    }
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
