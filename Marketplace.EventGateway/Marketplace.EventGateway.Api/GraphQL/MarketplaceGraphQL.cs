using HotChocolate;
using HotChocolate.Types;
using Marketplace.EventGateway.Api.Services;
using Marketplace.Events.Contracts.Saga;

namespace Marketplace.EventGateway.Api.GraphQL;

[ExtendObjectType("Query")]
public class MarketplaceQuery
{
    public async Task<string?> Aeropuertos(
        [Service] VuelosBookingClient client,
        string? nombre,
        int? idPais,
        int limit = 10,
        CancellationToken cancellationToken = default) =>
        await client.BuscarAeropuertosAsync(nombre, idPais, limit, cancellationToken);

    public async Task<string?> BuscarVuelos(
        [Service] VuelosBookingClient client,
        string? origen,
        string? destino,
        DateOnly? fecha,
        int? idAeropuertoOrigen,
        int? idAeropuertoDestino,
        CancellationToken cancellationToken = default) =>
        await client.BuscarVuelosAsync(
            origen,
            destino,
            fecha,
            idAeropuertoOrigen,
            idAeropuertoDestino,
            cancellationToken);

    public async Task<string?> Vuelo(
        [Service] VuelosBookingClient client,
        int idVuelo,
        CancellationToken cancellationToken = default) =>
        await client.ObtenerVueloAsync(idVuelo, cancellationToken);

    public async Task<string?> AsientosVuelo(
        [Service] VuelosBookingClient client,
        int idVuelo,
        string? clase,
        bool? disponible,
        CancellationToken cancellationToken = default) =>
        await client.ObtenerAsientosAsync(idVuelo, clase, disponible, cancellationToken);

    public SagaEstadoGql? EstadoReserva(
        [Service] SagaStateStore sagaStateStore,
        Guid correlationId)
    {
        var entry = sagaStateStore.TryGet(correlationId);
        return entry is null ? null : SagaEstadoGql.FromEntry(entry);
    }
}

[ExtendObjectType("Mutation")]
public class MarketplaceMutation
{
    public async Task<LoginResultGql> Login(
        [Service] SeguridadAuthClient client,
        LoginInput input,
        CancellationToken cancellationToken = default) =>
        await client.LoginAsync(input.Username, input.Password, cancellationToken)
        ?? throw new GraphQLException("Login falló.");

    public Task<MutationAcceptedGql> SeleccionarVuelo(
        [Service] MarketplaceMutationService service,
        SeleccionarVueloInput input,
        CancellationToken cancellationToken = default) =>
        service.SeleccionarVueloAsync(input, cancellationToken);

    public Task<MutationAcceptedGql> RegistrarPasajeros(
        [Service] MarketplaceMutationService service,
        RegistrarPasajerosInput input,
        CancellationToken cancellationToken = default) =>
        service.RegistrarPasajerosAsync(input, cancellationToken);

    public Task<MutationAcceptedGql> SolicitarReserva(
        [Service] MarketplaceMutationService service,
        SolicitarReservaInput input,
        CancellationToken cancellationToken = default) =>
        service.SolicitarReservaAsync(input, cancellationToken);
}

public class LoginInput
{
    public string Username { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
}

public class SeleccionarVueloInput
{
    public Guid? CorrelationId { get; set; }
    public int IdCliente { get; set; }
    public int IdVuelo { get; set; }
    public int IdAsiento { get; set; }
}

public class PasajeroInputGql
{
    public int? IdPasajero { get; set; }
    public string NombrePasajero { get; set; } = string.Empty;
    public string ApellidoPasajero { get; set; } = string.Empty;
    public string TipoDocumentoPasajero { get; set; } = string.Empty;
    public string NumeroDocumentoPasajero { get; set; } = string.Empty;
    public DateTime? FechaNacimientoPasajero { get; set; }
    public int? IdPaisNacionalidad { get; set; }
    public string? EmailContactoPasajero { get; set; }
    public string? TelefonoContactoPasajero { get; set; }
    public string? GeneroPasajero { get; set; }

    [DefaultValue(false)]
    public bool RequiereAsistencia { get; set; }

    public string? ObservacionesPasajero { get; set; }
}

public class RegistrarPasajerosInput
{
    public Guid? CorrelationId { get; set; }
    public int IdCliente { get; set; }
    public List<PasajeroInputGql> Pasajeros { get; set; } = [];
}

public class ReservaDetalleInputGql
{
    public int IdPasajero { get; set; }
    public int IdAsiento { get; set; }
    public decimal SubtotalLinea { get; set; }
    public decimal ValorIvaLinea { get; set; }
    public decimal TotalLinea { get; set; }
}

public class SolicitarReservaInput
{
    public Guid? CorrelationId { get; set; }
    public int IdCliente { get; set; }
    public int IdVuelo { get; set; }
    public decimal SubtotalReserva { get; set; }
    public decimal ValorIva { get; set; }
    public decimal TotalReserva { get; set; }
    public decimal CargoServicio { get; set; }
    public string? ContactoEmail { get; set; }
    public string? ContactoTelefono { get; set; }
    public string? Observaciones { get; set; }
    public string? OrigenCanalReserva { get; set; }
    public string? TokenPreReserva { get; set; }
    public List<ReservaDetalleInputGql> Detalles { get; set; } = [];
}

public class MutationAcceptedGql
{
    public Guid CorrelationId { get; set; }
    public Guid MessageId { get; set; }
    public string Paso { get; set; } = string.Empty;
    public string Mensaje { get; set; } = string.Empty;
}

public class SagaEstadoGql
{
    public Guid CorrelationId { get; set; }
    public MarketplaceSagaStatus Estado { get; set; }
    public string? UltimoPaso { get; set; }
    public int? IdReserva { get; set; }
    public string? CodigoReserva { get; set; }
    public string? TokenPreReserva { get; set; }
    public IReadOnlyList<int> IdsPasajerosValidados { get; set; } = [];
    public string? MotivoRechazo { get; set; }
    public string? CodigoError { get; set; }
    public DateTime ActualizadoEnUtc { get; set; }

    public static SagaEstadoGql FromEntry(SagaStateEntry entry) => new()
    {
        CorrelationId = entry.CorrelationId,
        Estado = entry.Estado,
        UltimoPaso = entry.UltimoPaso,
        IdReserva = entry.IdReserva,
        CodigoReserva = entry.CodigoReserva,
        TokenPreReserva = entry.TokenPreReserva,
        IdsPasajerosValidados = entry.IdsPasajerosValidados,
        MotivoRechazo = entry.MotivoRechazo,
        CodigoError = entry.CodigoError,
        ActualizadoEnUtc = entry.ActualizadoEnUtc
    };
}
