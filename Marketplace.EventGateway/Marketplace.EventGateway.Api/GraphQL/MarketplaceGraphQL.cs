using HotChocolate;
using HotChocolate.Types;
using Marketplace.EventGateway.Api.Services;
using Marketplace.Events.Contracts.Saga;

namespace Marketplace.EventGateway.Api.GraphQL;

[ExtendObjectType("Query")]
public class MarketplaceQuery
{
    /// <summary>
    /// Autocompletado booking (por nombre). Para dropdown completo como Vue, usar aeropuertosCatalogo.
    /// </summary>
    public async Task<string?> Aeropuertos(
        [Service] VuelosBookingClient client,
        string? nombre,
        int? idPais,
        int limit = 10,
        CancellationToken cancellationToken = default) =>
        await client.BuscarAeropuertosAsync(nombre, idPais, limit, cancellationToken);

    /// <summary>
    /// Catálogo activo — equivalente a Vue GET /aeropuertos (Middleware).
    /// </summary>
    public async Task<string?> AeropuertosCatalogo(
        [Service] AeropuertosCatalogClient client,
        int page = 1,
        int pageSize = 200,
        CancellationToken cancellationToken = default) =>
        await client.ListarActivosAsync(page, pageSize, cancellationToken);

    /// <summary>
    /// Vuelos programados sin filtro de ruta — equivalente a Vue GET /vuelos?estado_vuelo=PROGRAMADO.
    /// </summary>
    public async Task<string?> VuelosProgramados(
        [Service] VuelosBookingClient client,
        int page = 1,
        int pageSize = 200,
        CancellationToken cancellationToken = default) =>
        await client.ListarVuelosProgramadosAsync(page, pageSize, cancellationToken);

    public async Task<string?> BuscarVuelos(
        [Service] VuelosBookingClient client,
        string? origen,
        string? destino,
        /// <summary>Fecha ISO yyyy-MM-dd (mismo formato que REST booking).</summary>
        string? fecha,
        int? idAeropuertoOrigen,
        int? idAeropuertoDestino,
        CancellationToken cancellationToken = default)
    {
        DateOnly? fechaParsed = null;
        if (!string.IsNullOrWhiteSpace(fecha))
        {
            if (!DateOnly.TryParse(fecha, out var parsed))
                throw new GraphQLException("fecha debe tener formato yyyy-MM-dd (ej. 2026-06-15).");

            fechaParsed = parsed;
        }

        return await client.BuscarVuelosAsync(
            origen,
            destino,
            fechaParsed,
            idAeropuertoOrigen,
            idAeropuertoDestino,
            cancellationToken);
    }

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

    /// <summary>
    /// Perfil del cliente autenticado — equivalente a Vue GET /clientes/portal/mi-perfil.
    /// </summary>
    public async Task<string?> MiPerfilCliente(
        [Service] ClientesPortalClient client,
        CancellationToken cancellationToken = default) =>
        await client.MiPerfilAsync(cancellationToken);

    /// <summary>
    /// Catálogo de países — equivalente a Vue GET /paises.
    /// </summary>
    public async Task<string?> PaisesCatalogo(
        [Service] GeografiaCatalogClient client,
        int page = 1,
        int pageSize = 200,
        CancellationToken cancellationToken = default) =>
        await client.ListarPaisesAsync(page, pageSize, cancellationToken);

    /// <summary>
    /// Ciudades por país — equivalente a Vue GET /ciudades?id_pais=...
    /// </summary>
    public async Task<string?> CiudadesCatalogo(
        [Service] GeografiaCatalogClient client,
        int idPais,
        int page = 1,
        int pageSize = 200,
        CancellationToken cancellationToken = default) =>
        await client.ListarCiudadesAsync(idPais, page, pageSize, cancellationToken);
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

    /// <summary>
    /// Registro público de cliente — equivalente a Vue POST /auth/register-cliente.
    /// </summary>
    public async Task<string?> RegisterCliente(
        [Service] ClientesPortalClient client,
        RegisterClienteInputGql input,
        CancellationToken cancellationToken = default) =>
        await client.RegistrarClienteAsync(input, cancellationToken);

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

public class RegisterClienteInputGql
{
    public string TipoIdentificacion { get; set; } = string.Empty;
    public string NumeroIdentificacion { get; set; } = string.Empty;
    public string Nombres { get; set; } = string.Empty;
    public string? Apellidos { get; set; }
    public string Correo { get; set; } = string.Empty;
    public string Telefono { get; set; } = string.Empty;
    public string Direccion { get; set; } = string.Empty;
    public int IdCiudadResidencia { get; set; }
    public int IdPaisNacionalidad { get; set; }
    public DateTime? FechaNacimiento { get; set; }
    public string? Genero { get; set; }
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

public class EquipajeInputGql
{
    public int? IdPasajero { get; set; }
    public string Tipo { get; set; } = null!;
    public decimal PesoKg { get; set; }
    public string? DescripcionEquipaje { get; set; }
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
    /// <summary>Lista vacía = reserva emitida sin filas en ventas.equipaje.</summary>
    public List<EquipajeInputGql> Equipaje { get; set; } = [];
}

public class MutationAcceptedGql
{
    public Guid CorrelationId { get; set; }
    public Guid MessageId { get; set; }
    public string Paso { get; set; } = string.Empty;
    public string Mensaje { get; set; } = string.Empty;
    public IReadOnlyList<int>? IdsPasajerosValidados { get; set; }
    public int? IdReserva { get; set; }
    public string? CodigoReserva { get; set; }
    public string? TokenPreReserva { get; set; }
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
