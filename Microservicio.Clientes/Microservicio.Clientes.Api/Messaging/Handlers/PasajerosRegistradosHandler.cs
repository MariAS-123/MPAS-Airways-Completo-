using Marketplace.Events.Contracts.Events;
using Microservicio.Clientes.Api.Messaging.Mapping;
using Microservicio.Clientes.Api.Messaging.Publishing;
using Microservicio.Clientes.Business.Interfaces;

namespace Microservicio.Clientes.Api.Messaging.Handlers;

public class PasajerosRegistradosHandler
{
    private const string MarketplaceUsuario = "marketplace-gateway";

    private readonly IPasajeroService _pasajeroService;
    private readonly IMarketplaceEventPublisher _publisher;
    private readonly ILogger<PasajerosRegistradosHandler> _logger;

    public PasajerosRegistradosHandler(
        IPasajeroService pasajeroService,
        IMarketplaceEventPublisher publisher,
        ILogger<PasajerosRegistradosHandler> logger)
    {
        _pasajeroService = pasajeroService;
        _publisher = publisher;
        _logger = logger;
    }

    public async Task<bool> HandleAsync(
        PasajerosRegistradosEvent message,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation(
            "Procesando PasajerosRegistrados correlationId={CorrelationId} pasajeros={Count}",
            message.CorrelationId,
            message.Pasajeros.Count);

        try
        {
            if (message.Pasajeros.Count == 0)
            {
                await PublishInvalidAsync(message, "Debe registrar al menos un pasajero.", cancellationToken);
                return true;
            }

            var idsValidados = new List<int>();

            foreach (var pasajero in message.Pasajeros)
            {
                if (pasajero.IdCliente != message.IdCliente)
                {
                    await PublishInvalidAsync(
                        message,
                        $"El pasajero no pertenece al cliente {message.IdCliente}.",
                        cancellationToken);
                    return true;
                }

                if (pasajero.IdPasajero is > 0)
                {
                    var existente = await _pasajeroService.GetByIdAsync(
                        pasajero.IdPasajero.Value,
                        message.IdCliente,
                        "CLIENTE");

                    if (existente is null)
                    {
                        await PublishInvalidAsync(
                            message,
                            $"Pasajero {pasajero.IdPasajero.Value} no encontrado.",
                            cancellationToken);
                        return true;
                    }

                    if (existente.Estado.Trim().ToUpperInvariant() != "ACTIVO")
                    {
                        await PublishInvalidAsync(
                            message,
                            $"Pasajero {pasajero.IdPasajero.Value} no está activo.",
                            cancellationToken);
                        return true;
                    }

                    idsValidados.Add(existente.IdPasajero);
                    continue;
                }

                var request = PasajeroPayloadMapper.ToRequestDto(pasajero, message.IdCliente);
                var creado = await _pasajeroService.CreateAsync(request, MarketplaceUsuario);
                idsValidados.Add(creado.IdPasajero);
            }

            await _publisher.PublishAsync(new PasajerosValidadosEvent
            {
                CorrelationId = message.CorrelationId,
                IdCliente = message.IdCliente,
                EsValido = true,
                IdsPasajerosValidados = idsValidados
            }, cancellationToken);

            _logger.LogInformation(
                "Pasajeros validados correlationId={CorrelationId} ids=[{Ids}]",
                message.CorrelationId,
                string.Join(",", idsValidados));

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(
                ex,
                "PasajerosRegistrados falló correlationId={CorrelationId}",
                message.CorrelationId);

            await PublishInvalidAsync(message, ex.Message, cancellationToken);
            return true;
        }
    }

    private async Task PublishInvalidAsync(
        PasajerosRegistradosEvent message,
        string motivo,
        CancellationToken cancellationToken)
    {
        await _publisher.PublishAsync(new PasajerosValidadosEvent
        {
            CorrelationId = message.CorrelationId,
            IdCliente = message.IdCliente,
            EsValido = false,
            MotivoRechazo = motivo
        }, cancellationToken);
    }
}
