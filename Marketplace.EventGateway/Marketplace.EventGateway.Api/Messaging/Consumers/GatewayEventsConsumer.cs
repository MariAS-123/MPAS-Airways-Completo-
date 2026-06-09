using Marketplace.Events.Contracts.Events;
using Marketplace.Events.Contracts.Routing;
using Marketplace.Events.Contracts.Saga;
using Marketplace.EventGateway.Api.Messaging.Options;
using Marketplace.EventGateway.Api.Messaging.Serialization;
using Marketplace.EventGateway.Api.Services;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace Marketplace.EventGateway.Api.Messaging.Consumers;

public sealed class GatewayEventsConsumer : BackgroundService
{
    private static readonly string[] RoutingKeys =
    [
        MarketplaceRoutingKeys.AsientoPreReservado,
        MarketplaceRoutingKeys.PasajerosValidados,
        MarketplaceRoutingKeys.ReservaCreada,
        MarketplaceRoutingKeys.ReservaRechazada
    ];

    private readonly RabbitMqOptions _options;
    private readonly SagaStateStore _sagaStateStore;
    private readonly ILogger<GatewayEventsConsumer> _logger;
    private IConnection? _connection;
    private IChannel? _channel;

    public GatewayEventsConsumer(
        IOptions<RabbitMqOptions> options,
        SagaStateStore sagaStateStore,
        ILogger<GatewayEventsConsumer> logger)
    {
        _options = options.Value;
        _sagaStateStore = sagaStateStore;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!_options.Enabled)
        {
            _logger.LogInformation("GatewayEventsConsumer inactivo (RabbitMQ:Enabled=false).");
            return;
        }

        var factory = _options.CreateConnectionFactory();

        _connection = await factory.CreateConnectionAsync(stoppingToken);
        _channel = await _connection.CreateChannelAsync(cancellationToken: stoppingToken);

        await _channel.ExchangeDeclareAsync(
            _options.ExchangeName,
            ExchangeType.Topic,
            durable: true,
            autoDelete: false,
            cancellationToken: stoppingToken);

        await _channel.QueueDeclareAsync(
            _options.GatewayEventsQueue,
            durable: true,
            exclusive: false,
            autoDelete: false,
            cancellationToken: stoppingToken);

        foreach (var routingKey in RoutingKeys)
        {
            await _channel.QueueBindAsync(
                _options.GatewayEventsQueue,
                _options.ExchangeName,
                routingKey,
                cancellationToken: stoppingToken);
        }

        await _channel.BasicQosAsync(0, _options.PrefetchCount, false, stoppingToken);

        var consumer = new AsyncEventingBasicConsumer(_channel);
        consumer.ReceivedAsync += async (_, delivery) =>
        {
            try
            {
                ProcessMessage(delivery.BasicProperties.Type, delivery.Body.Span);
                if (_channel is { IsOpen: true })
                    await _channel.BasicAckAsync(delivery.DeliveryTag, false, stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error procesando evento en cola {Queue}.", _options.GatewayEventsQueue);
                if (_channel is { IsOpen: true })
                    await _channel.BasicNackAsync(delivery.DeliveryTag, false, false, stoppingToken);
            }
        };

        await _channel.BasicConsumeAsync(_options.GatewayEventsQueue, false, consumer, stoppingToken);

        _logger.LogInformation("GatewayEventsConsumer escuchando cola {Queue}.", _options.GatewayEventsQueue);

        await Task.Delay(Timeout.Infinite, stoppingToken);
    }

    private void ProcessMessage(string? eventType, ReadOnlySpan<byte> body)
    {
        switch (eventType)
        {
            case nameof(AsientoPreReservadoEvent):
            {
                var message = MarketplaceEventJson.Deserialize<AsientoPreReservadoEvent>(body);
                if (message is null) return;

                var entry = _sagaStateStore.GetOrCreate(message.CorrelationId);
                entry.Estado = MarketplaceSagaStatus.AsientoPreReservado;
                entry.TokenPreReserva = message.TokenPreReserva;
                entry.UltimoPaso = "ASIENTO_PRE_RESERVADO";
                entry.ActualizadoEnUtc = DateTime.UtcNow;

                _logger.LogInformation(
                    "Saga {CorrelationId} → AsientoPreReservado asiento={IdAsiento}",
                    message.CorrelationId,
                    message.IdAsiento);
                break;
            }
            case nameof(PasajerosValidadosEvent):
            {
                var message = MarketplaceEventJson.Deserialize<PasajerosValidadosEvent>(body);
                if (message is null) return;

                var entry = _sagaStateStore.GetOrCreate(message.CorrelationId);
                entry.IdsPasajerosValidados = message.IdsPasajerosValidados;
                entry.UltimoPaso = "PASAJEROS_VALIDADOS";
                entry.ActualizadoEnUtc = DateTime.UtcNow;

                if (message.EsValido)
                {
                    entry.Estado = MarketplaceSagaStatus.PasajerosValidados;
                }
                else
                {
                    entry.Estado = MarketplaceSagaStatus.Rechazada;
                    entry.MotivoRechazo = message.MotivoRechazo;
                    entry.CodigoError = "PASAJEROS_INVALIDOS";
                }

                _logger.LogInformation(
                    "Saga {CorrelationId} → PasajerosValidados valido={EsValido}",
                    message.CorrelationId,
                    message.EsValido);
                break;
            }
            case nameof(ReservaCreadaEvent):
            {
                var message = MarketplaceEventJson.Deserialize<ReservaCreadaEvent>(body);
                if (message is null) return;

                var entry = _sagaStateStore.GetOrCreate(message.CorrelationId);
                entry.Estado = MarketplaceSagaStatus.ReservaCreada;
                entry.IdReserva = message.IdReserva;
                entry.CodigoReserva = message.CodigoReserva;
                entry.UltimoPaso = "RESERVA_CREADA";
                entry.ActualizadoEnUtc = DateTime.UtcNow;

                _logger.LogInformation(
                    "Saga {CorrelationId} → ReservaCreada id={IdReserva} codigo={Codigo}",
                    message.CorrelationId,
                    message.IdReserva,
                    message.CodigoReserva);
                break;
            }
            case nameof(ReservaRechazadaEvent):
            {
                var message = MarketplaceEventJson.Deserialize<ReservaRechazadaEvent>(body);
                if (message is null) return;

                var entry = _sagaStateStore.GetOrCreate(message.CorrelationId);
                entry.Estado = MarketplaceSagaStatus.Rechazada;
                entry.MotivoRechazo = message.Mensaje;
                entry.CodigoError = message.CodigoError;
                entry.UltimoPaso = message.PasoFallido;
                entry.ActualizadoEnUtc = DateTime.UtcNow;

                _logger.LogWarning(
                    "Saga {CorrelationId} → ReservaRechazada: {Mensaje}",
                    message.CorrelationId,
                    message.Mensaje);
                break;
            }
            default:
                _logger.LogDebug("Evento ignorado por Gateway: {EventType}", eventType);
                break;
        }
    }

    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        if (_channel is not null)
            await _channel.CloseAsync(cancellationToken);

        if (_connection is not null)
            await _connection.CloseAsync(cancellationToken);

        await base.StopAsync(cancellationToken);
    }

    public override void Dispose()
    {
        _channel?.Dispose();
        _connection?.Dispose();
        base.Dispose();
    }
}
