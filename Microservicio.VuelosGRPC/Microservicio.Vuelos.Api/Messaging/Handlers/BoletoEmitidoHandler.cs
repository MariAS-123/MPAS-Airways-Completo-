using Marketplace.Events.Contracts.Events;
using Microservicio.Vuelos.Api.Messaging.Services;

namespace Microservicio.Vuelos.Api.Messaging.Handlers;

public class BoletoEmitidoHandler
{
    private readonly AsientoDisponibilidadOperations _asientoOperations;
    private readonly ILogger<BoletoEmitidoHandler> _logger;

    public BoletoEmitidoHandler(
        AsientoDisponibilidadOperations asientoOperations,
        ILogger<BoletoEmitidoHandler> logger)
    {
        _asientoOperations = asientoOperations;
        _logger = logger;
    }

    public async Task<bool> HandleAsync(
        BoletoEmitidoEvent message,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var (success, resultMessage) = await _asientoOperations.BloquearAsync(
                message.IdVuelo,
                message.IdAsiento,
                "MARKETPLACE_BOLETO_EMITIDO",
                cancellationToken);

            if (!success)
            {
                _logger.LogWarning(
                    "BoletoEmitido: no se bloqueó asiento {IdAsiento} vuelo {IdVuelo}: {Message}",
                    message.IdAsiento,
                    message.IdVuelo,
                    resultMessage);
                return true;
            }

            _logger.LogInformation(
                "BoletoEmitido: asiento {IdAsiento} bloqueado definitivamente para reserva {IdReserva}. {Message}",
                message.IdAsiento,
                message.IdReserva,
                resultMessage);

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Error procesando BoletoEmitido correlationId={CorrelationId}.",
                message.CorrelationId);
            return false;
        }
    }
}
