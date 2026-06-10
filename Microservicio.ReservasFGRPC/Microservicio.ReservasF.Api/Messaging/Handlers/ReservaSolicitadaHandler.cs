using Marketplace.Events.Contracts.Events;
using Microservicio.ReservasF.Api.Messaging;
using Microservicio.ReservasF.Api.Messaging.Mapping;
using Microservicio.ReservasF.Api.Messaging.Publishing;
using Microservicio.ReservasF.Business.DTOs.Reserva;
using Microservicio.ReservasF.Business.Exceptions;
using Microservicio.ReservasF.Business.Interfaces;

namespace Microservicio.ReservasF.Api.Messaging.Handlers;

public class ReservaSolicitadaHandler
{
    private readonly IReservaService _reservaService;
    private readonly IMarketplaceEventPublisher _publisher;
    private readonly IMessagingAccessTokenAccessor _accessTokenAccessor;
    private readonly ILogger<ReservaSolicitadaHandler> _logger;

    public ReservaSolicitadaHandler(
        IReservaService reservaService,
        IMarketplaceEventPublisher publisher,
        IMessagingAccessTokenAccessor accessTokenAccessor,
        ILogger<ReservaSolicitadaHandler> logger)
    {
        _reservaService = reservaService;
        _publisher = publisher;
        _accessTokenAccessor = accessTokenAccessor;
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

        _accessTokenAccessor.Token = message.AccessToken;

        try
        {
            var request = ReservaSolicitadaMapper.ToRequestDto(message);
            var created = await _reservaService.CreateAsync(request, message.CreadoPorUsuario);

            var equipajePagar = ReservaEquipajeMapper.ToPagarEquipaje(message.Equipaje, created.Detalles);
            var (idCliente, rol) = MessagingJwtClaims.Parse(message.AccessToken, message.IdCliente);

            var pagado = await _reservaService.PagarAsync(
                created.IdReserva,
                new ReservaPagarRequestDto
                {
                    CargoServicio = message.CargoServicio,
                    Equipaje = equipajePagar
                },
                message.CreadoPorUsuario,
                idCliente,
                rol);

            if (pagado is null)
                throw new BusinessException("No se pudo completar el pago de la reserva.");

            await _publisher.PublishAsync(new ReservaCreadaEvent
            {
                CorrelationId = message.CorrelationId,
                IdReserva = pagado.Reserva.IdReserva,
                CodigoReserva = pagado.Reserva.CodigoReserva,
                EstadoReserva = pagado.Reserva.EstadoReserva,
                IdCliente = created.IdCliente,
                IdVuelo = created.IdVuelo,
                EquipajesRegistrados = pagado.Equipajes.Count
            }, cancellationToken);

            _logger.LogInformation(
                "Reserva creada y pagada vía mensaje id={IdReserva} codigo={CodigoReserva} equipajes={Equipajes}",
                pagado.Reserva.IdReserva,
                pagado.Reserva.CodigoReserva,
                pagado.Equipajes.Count);

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
        finally
        {
            _accessTokenAccessor.Token = null;
        }
    }

    private static (string CodigoError, string PasoFallido) MapException(Exception ex) =>
        ex switch
        {
            ValidationException => ("VALIDATION_ERROR", "CREAR_RESERVA"),
            NotFoundException => ("NOT_FOUND", "CREAR_RESERVA"),
            UnauthorizedBusinessException => ("UNAUTHORIZED", "CREAR_RESERVA"),
            BusinessException business when business.Message.Contains("pago", StringComparison.OrdinalIgnoreCase)
                => ("BUSINESS_RULE", "PAGAR_RESERVA"),
            BusinessException => ("BUSINESS_RULE", "CREAR_RESERVA"),
            _ => ("INTERNAL_ERROR", "CREAR_RESERVA")
        };
}
