using Marketplace.Events.Contracts.Events;
using Microservicio.Vuelos.Api.Messaging.Options;
using Microservicio.Vuelos.Api.Messaging.Publishing;
using Microservicio.Vuelos.Api.Messaging.Services;
using Microservicio.Vuelos.Business.Interfaces;
using Microsoft.Extensions.Options;

namespace Microservicio.Vuelos.Api.Messaging.Handlers;

public class VueloSeleccionadoHandler
{
    private readonly IVueloService _vueloService;
    private readonly IAsientoService _asientoService;
    private readonly IPreReservaAsientoStore _preReservaStore;
    private readonly IMarketplaceEventPublisher _publisher;
    private readonly RabbitMqOptions _options;
    private readonly ILogger<VueloSeleccionadoHandler> _logger;

    public VueloSeleccionadoHandler(
        IVueloService vueloService,
        IAsientoService asientoService,
        IPreReservaAsientoStore preReservaStore,
        IMarketplaceEventPublisher publisher,
        IOptions<RabbitMqOptions> options,
        ILogger<VueloSeleccionadoHandler> logger)
    {
        _vueloService = vueloService;
        _asientoService = asientoService;
        _preReservaStore = preReservaStore;
        _publisher = publisher;
        _options = options.Value;
        _logger = logger;
    }

    public async Task<bool> HandleAsync(
        VueloSeleccionadoEvent message,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var vuelo = await _vueloService.GetByIdAsync(message.IdVuelo);
            if (vuelo is null)
            {
                _logger.LogWarning("VueloSeleccionado: vuelo {IdVuelo} no encontrado.", message.IdVuelo);
                return true;
            }

            var estado = vuelo.Estado.Trim().ToUpperInvariant();
            var estadoVuelo = vuelo.EstadoVuelo.Trim().ToUpperInvariant();
            if (estado != "ACTIVO" || estadoVuelo is not ("PROGRAMADO" or "DEMORADO"))
            {
                _logger.LogWarning(
                    "VueloSeleccionado: vuelo {IdVuelo} no disponible (estado={Estado}, estadoVuelo={EstadoVuelo}).",
                    message.IdVuelo,
                    vuelo.Estado,
                    vuelo.EstadoVuelo);
                return true;
            }

            var asiento = await _asientoService.GetByIdAsync(message.IdAsiento);
            if (asiento is null
                || asiento.IdVuelo != message.IdVuelo
                || !asiento.Disponible
                || asiento.Estado.Trim().ToUpperInvariant() != "ACTIVO")
            {
                _logger.LogWarning(
                    "VueloSeleccionado: asiento {IdAsiento} no disponible para vuelo {IdVuelo}.",
                    message.IdAsiento,
                    message.IdVuelo);
                return true;
            }

            var duracion = TimeSpan.FromMinutes(_options.PreReservaMinutos);
            if (!_preReservaStore.TryRegistrar(
                    message.IdVuelo,
                    message.IdAsiento,
                    message.IdCliente,
                    message.CorrelationId,
                    duracion,
                    out var preReserva))
            {
                _logger.LogWarning(
                    "VueloSeleccionado: asiento {IdAsiento} ya pre-reservado por otro flujo.",
                    message.IdAsiento);
                return true;
            }

            await _publisher.PublishAsync(new AsientoPreReservadoEvent
            {
                CorrelationId = message.CorrelationId,
                IdVuelo = message.IdVuelo,
                IdAsiento = message.IdAsiento,
                IdCliente = message.IdCliente,
                ExpiraEnUtc = preReserva.ExpiraEnUtc,
                TokenPreReserva = preReserva.TokenPreReserva
            }, cancellationToken);

            _logger.LogInformation(
                "Asiento pre-reservado vuelo={IdVuelo} asiento={IdAsiento} expira={ExpiraEnUtc}",
                message.IdVuelo,
                message.IdAsiento,
                preReserva.ExpiraEnUtc);

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error procesando VueloSeleccionado correlationId={CorrelationId}.", message.CorrelationId);
            return false;
        }
    }
}
