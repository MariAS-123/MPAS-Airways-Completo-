using Marketplace.Events.Contracts.Events;
using Microservicio.ReservasF.Api.Messaging.Mapping;
using Microservicio.ReservasF.Api.Messaging.Publishing;
using Microservicio.ReservasF.Business.Exceptions;
using Microservicio.ReservasF.Business.Interfaces;

namespace Microservicio.ReservasF.Api.Messaging.Handlers;

public class ReservaSolicitadaHandler
{
    private readonly IReservaService _reservaService;
    private readonly IMarketplaceEventPublisher _publisher;
    private readonly ILogger<ReservaSolicitadaHandler> _logger;

    public ReservaSolicitadaHandler(
        IReservaService reservaService,
        IMarketplaceEventPublisher publisher,
        ILogger<ReservaSolicitadaHandler> logger)
    {
        _reservaService = reservaService;
        _publisher = publisher;
        _logger = logger;
    }

    public async Task<bool> HandleAsync(
        ReservaSolicitadaEvent message,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation(
            "Procesando ReservaSolicitada correlationId={CorrelationId} messageId={MessageId}",
            message.CorrelationId,
            message.MessageId);

        try
        {
            var request = ReservaSolicitadaMapper.ToRequestDto(message);
            var created = await _reservaService.CreateAsync(request, message.CreadoPorUsuario);

            await _publisher.PublishAsync(new ReservaCreadaEvent
            {
                CorrelationId = message.CorrelationId,
                IdReserva = created.IdReserva,
                CodigoReserva = created.CodigoReserva,
                EstadoReserva = created.EstadoReserva,
                IdCliente = created.IdCliente,
                IdVuelo = created.IdVuelo
            }, cancellationToken);

            _logger.LogInformation(
                "Reserva creada vía mensaje id={IdReserva} codigo={CodigoReserva}",
                created.IdReserva,
                created.CodigoReserva);

            return true;
        }
        catch (Exception ex)
        {
            var (codigo, paso) = MapException(ex);

            _logger.LogWarning(
                ex,
                "ReservaSolicitada rechazada correlationId={CorrelationId}: {Mensaje}",
                message.CorrelationId,
                ex.Message);

            await _publisher.PublishAsync(new ReservaRechazadaEvent
            {
                CorrelationId = message.CorrelationId,
                IdCliente = message.IdCliente,
                IdVuelo = message.IdVuelo,
                PasoFallido = paso,
                CodigoError = codigo,
                Mensaje = ex.Message
            }, cancellationToken);

            return true;
        }
    }

    private static (string CodigoError, string PasoFallido) MapException(Exception ex) =>
        ex switch
        {
            ValidationException => ("VALIDATION_ERROR", "CREAR_RESERVA"),
            NotFoundException => ("NOT_FOUND", "CREAR_RESERVA"),
            UnauthorizedBusinessException => ("UNAUTHORIZED", "CREAR_RESERVA"),
            BusinessException => ("BUSINESS_RULE", "CREAR_RESERVA"),
            _ => ("INTERNAL_ERROR", "CREAR_RESERVA")
        };
}
