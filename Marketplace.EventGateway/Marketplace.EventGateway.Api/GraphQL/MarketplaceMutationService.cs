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

    public MarketplaceMutationService(
        IMarketplaceEventPublisher publisher,
        SagaStateStore sagaStateStore,
        IUserTokenAccessor userTokenAccessor)
    {
        _publisher = publisher;
        _sagaStateStore = sagaStateStore;
        _userTokenAccessor = userTokenAccessor;
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

        await _publisher.PublishAsync(new PasajerosRegistradosEvent
        {
            MessageId = messageId,
            CorrelationId = correlationId,
            IdCliente = input.IdCliente,
            Pasajeros = input.Pasajeros.Select(p => new PasajeroPayload
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
            }).ToList(),
            AccessToken = token
        }, cancellationToken);

        _sagaStateStore.MarkAccepted(correlationId, "PASAJEROS_REGISTRADOS");

        return new MutationAcceptedGql
        {
            CorrelationId = correlationId,
            MessageId = messageId,
            Paso = "PASAJEROS_REGISTRADOS",
            Mensaje = "Pasajeros enviados a validación."
        };
    }

    public async Task<MutationAcceptedGql> SolicitarReservaAsync(
        SolicitarReservaInput input,
        CancellationToken cancellationToken = default)
    {
        var correlationId = input.CorrelationId ?? Guid.NewGuid();
        var messageId = Guid.NewGuid();
        var token = _userTokenAccessor.Token;

        await _publisher.PublishAsync(new ReservaSolicitadaEvent
        {
            MessageId = messageId,
            CorrelationId = correlationId,
            IdCliente = input.IdCliente,
            IdVuelo = input.IdVuelo,
            SubtotalReserva = input.SubtotalReserva,
            ValorIva = input.ValorIva,
            TotalReserva = input.TotalReserva,
            CargoServicio = input.CargoServicio,
            ContactoEmail = input.ContactoEmail,
            ContactoTelefono = input.ContactoTelefono,
            Observaciones = input.Observaciones,
            OrigenCanalReserva = string.IsNullOrWhiteSpace(input.OrigenCanalReserva) ? "APP" : input.OrigenCanalReserva,
            TokenPreReserva = input.TokenPreReserva,
            Detalles = input.Detalles.Select(d => new ReservaDetallePayload
            {
                IdPasajero = d.IdPasajero,
                IdAsiento = d.IdAsiento,
                SubtotalLinea = d.SubtotalLinea,
                ValorIvaLinea = d.ValorIvaLinea,
                TotalLinea = d.TotalLinea
            }).ToList(),
            AccessToken = token
        }, cancellationToken);

        _sagaStateStore.MarkAccepted(correlationId, "RESERVA_SOLICITADA");

        return new MutationAcceptedGql
        {
            CorrelationId = correlationId,
            MessageId = messageId,
            Paso = "RESERVA_SOLICITADA",
            Mensaje = "Reserva en proceso. Consulta estadoReserva con el correlationId."
        };
    }
}
