using HotChocolate;
using Marketplace.Events.Contracts.Events;
using Marketplace.Events.Contracts.Payloads;
using Marketplace.EventGateway.Api.Messaging.Publishing;
using Marketplace.EventGateway.Api.Services;



namespace Marketplace.EventGateway.Api.GraphQL;



public sealed class MarketplaceMutationService

{

    private readonly IMarketplaceEventPublisher _publisher;

    private readonly SagaStateStore _sagaStateStore;

    private readonly IUserTokenAccessor _userTokenAccessor;

    private readonly ClientesPasajerosClient _clientesPasajerosClient;

    private readonly ReservasBookingClient _reservasBookingClient;

    private readonly ILogger<MarketplaceMutationService> _logger;



    public MarketplaceMutationService(

        IMarketplaceEventPublisher publisher,

        SagaStateStore sagaStateStore,

        IUserTokenAccessor userTokenAccessor,

        ClientesPasajerosClient clientesPasajerosClient,

        ReservasBookingClient reservasBookingClient,

        ILogger<MarketplaceMutationService> logger)

    {

        _publisher = publisher;

        _sagaStateStore = sagaStateStore;

        _userTokenAccessor = userTokenAccessor;

        _clientesPasajerosClient = clientesPasajerosClient;

        _reservasBookingClient = reservasBookingClient;

        _logger = logger;

    }



    public async Task<MutationAcceptedGql> SeleccionarVueloAsync(

        SeleccionarVueloInput input,

        CancellationToken cancellationToken = default)

    {

        var correlationId = input.CorrelationId ?? Guid.NewGuid();

        var messageId = Guid.NewGuid();

        var token = _userTokenAccessor.Token;



        await _publisher.PublishAsync(new VueloSeleccionadoEvent

        {

            MessageId = messageId,

            CorrelationId = correlationId,

            IdCliente = input.IdCliente,

            IdVuelo = input.IdVuelo,

            IdAsiento = input.IdAsiento,

            AccessToken = token

        }, cancellationToken);



        _sagaStateStore.MarkAccepted(correlationId, "VUELO_SELECCIONADO");



        return new MutationAcceptedGql

        {

            CorrelationId = correlationId,

            MessageId = messageId,

            Paso = "VUELO_SELECCIONADO",

            Mensaje = "Vuelo y asiento enviados a pre-reserva."

        };

    }



    public async Task<MutationAcceptedGql> RegistrarPasajerosAsync(

        RegistrarPasajerosInput input,

        CancellationToken cancellationToken = default)

    {

        var correlationId = input.CorrelationId ?? Guid.NewGuid();

        var messageId = Guid.NewGuid();

        var token = _userTokenAccessor.Token;



        var pasajerosPayload = input.Pasajeros.Select(p => new PasajeroPayload

        {

            IdPasajero = p.IdPasajero,

            IdCliente = input.IdCliente,

            NombrePasajero = p.NombrePasajero,

            ApellidoPasajero = p.ApellidoPasajero,

            TipoDocumentoPasajero = p.TipoDocumentoPasajero,

            NumeroDocumentoPasajero = p.NumeroDocumentoPasajero,

            FechaNacimientoPasajero = p.FechaNacimientoPasajero,

            IdPaisNacionalidad = p.IdPaisNacionalidad,

            EmailContactoPasajero = p.EmailContactoPasajero,

            TelefonoContactoPasajero = p.TelefonoContactoPasajero,

            GeneroPasajero = p.GeneroPasajero,

            RequiereAsistencia = p.RequiereAsistencia,

            ObservacionesPasajero = p.ObservacionesPasajero

        }).ToList();



        IReadOnlyList<int> idsValidados;

        try

        {

            idsValidados = await _clientesPasajerosClient.RegistrarPasajerosAsync(input, cancellationToken);

            _sagaStateStore.MarkPasajerosValidados(correlationId, idsValidados);



            _logger.LogInformation(

                "Pasajeros validados por REST correlationId={CorrelationId} ids=[{Ids}]",

                correlationId,

                string.Join(",", idsValidados));

        }

        catch (Exception ex)

        {

            _logger.LogError(ex, "Fallo REST registrar pasajeros correlationId={CorrelationId}", correlationId);

            _sagaStateStore.MarkRechazada(correlationId, ex.Message, "PASAJEROS_REST", "PASAJEROS_REGISTRADOS");

            throw new GraphQLException(ex.Message);

        }



        await _publisher.PublishAsync(new PasajerosRegistradosEvent

        {

            MessageId = messageId,

            CorrelationId = correlationId,

            IdCliente = input.IdCliente,

            Pasajeros = pasajerosPayload,

            AccessToken = token

        }, cancellationToken);



        return new MutationAcceptedGql

        {

            CorrelationId = correlationId,

            MessageId = messageId,

            Paso = "PASAJEROS_VALIDADOS",

            Mensaje = "Pasajeros registrados y validados.",

            IdsPasajerosValidados = idsValidados,

        };

    }



    public async Task<MutationAcceptedGql> SolicitarReservaAsync(

        SolicitarReservaInput input,

        CancellationToken cancellationToken = default)

    {

        var correlationId = input.CorrelationId ?? Guid.NewGuid();

        var messageId = Guid.NewGuid();

        var token = _userTokenAccessor.Token;



        try

        {

            var (idReserva, codigoReserva) = await _reservasBookingClient.CrearYPagarAsync(input, cancellationToken);

            _sagaStateStore.MarkReservaCreada(correlationId, idReserva, codigoReserva);



            _logger.LogInformation(

                "Reserva creada por REST correlationId={CorrelationId} id={IdReserva} codigo={Codigo}",

                correlationId,

                idReserva,

                codigoReserva);



            return new MutationAcceptedGql

            {

                CorrelationId = correlationId,

                MessageId = messageId,

                Paso = "RESERVA_CREADA",

                Mensaje = $"Reserva confirmada ({codigoReserva}).",

                IdReserva = idReserva,

                CodigoReserva = codigoReserva,

            };

        }

        catch (Exception ex)

        {

            _logger.LogWarning(ex, "REST crear/pagar reserva falló correlationId={CorrelationId}", correlationId);

            _sagaStateStore.MarkRechazada(correlationId, ex.Message, "RESERVA_REST", "RESERVA_SOLICITADA");

            throw new GraphQLException(ex.Message);

        }

    }

}


