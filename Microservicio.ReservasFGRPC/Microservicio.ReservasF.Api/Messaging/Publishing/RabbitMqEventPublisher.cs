using Marketplace.Events.Contracts;
using Microservicio.ReservasF.Api.Messaging.Options;
using Microservicio.ReservasF.Api.Messaging.Serialization;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;

namespace Microservicio.ReservasF.Api.Messaging.Publishing;

public sealed class RabbitMqEventPublisher : IMarketplaceEventPublisher, IAsyncDisposable
{
    private readonly RabbitMqOptions _options;
    private readonly ILogger<RabbitMqEventPublisher> _logger;
    private readonly SemaphoreSlim _connectionLock = new(1, 1);
    private IConnection? _connection;
    private IChannel? _channel;

    public RabbitMqEventPublisher(
        IOptions<RabbitMqOptions> options,
        ILogger<RabbitMqEventPublisher> logger)
    {
        _options = options.Value;
        _logger = logger;
    }

    public async Task PublishAsync<TEvent>(TEvent marketplaceEvent, CancellationToken cancellationToken = default)
        where TEvent : MarketplaceEvent
    {
        if (!_options.Enabled)
        {
            _logger.LogDebug(
                "RabbitMQ deshabilitado. Evento {EventType} no publicado (MessageId={MessageId}).",
                typeof(TEvent).Name,
                marketplaceEvent.MessageId);
            return;
        }

        var routingKey = MarketplaceEventTypes.GetRoutingKey<TEvent>();
        var body = MarketplaceEventJson.Serialize(marketplaceEvent);

        await EnsureChannelAsync(cancellationToken);

        var properties = new BasicProperties
        {
            ContentType = "application/json",
            DeliveryMode = DeliveryModes.Persistent,
            MessageId = marketplaceEvent.MessageId.ToString(),
            CorrelationId = marketplaceEvent.CorrelationId.ToString(),
            Type = typeof(TEvent).Name,
            Timestamp = new AmqpTimestamp(DateTimeOffset.UtcNow.ToUnixTimeSeconds())
        };

        await _channel!.BasicPublishAsync(
            exchange: _options.ExchangeName,
            routingKey: routingKey,
            mandatory: false,
            basicProperties: properties,
            body: body,
            cancellationToken: cancellationToken);

        _logger.LogInformation(
            "Evento publicado: {EventType} routingKey={RoutingKey} correlationId={CorrelationId}",
            typeof(TEvent).Name,
            routingKey,
            marketplaceEvent.CorrelationId);
    }

    private async Task EnsureChannelAsync(CancellationToken cancellationToken)
    {
        if (_channel is { IsOpen: true })
            return;

        await _connectionLock.WaitAsync(cancellationToken);
        try
        {
            if (_channel is { IsOpen: true })
                return;

            if (_channel is not null)
            {
                await _channel.DisposeAsync();
                _channel = null;
            }

            if (_connection is not null)
            {
                await _connection.DisposeAsync();
                _connection = null;
            }

            var factory = new ConnectionFactory
            {
                HostName = _options.HostName,
                Port = _options.Port,
                UserName = _options.UserName,
                Password = _options.Password,
                VirtualHost = _options.VirtualHost
            };

            _connection = await factory.CreateConnectionAsync(cancellationToken);
            _channel = await _connection.CreateChannelAsync(cancellationToken: cancellationToken);

            await _channel.ExchangeDeclareAsync(
                exchange: _options.ExchangeName,
                type: ExchangeType.Topic,
                durable: true,
                autoDelete: false,
                cancellationToken: cancellationToken);
        }
        finally
        {
            _connectionLock.Release();
        }
    }

    public async ValueTask DisposeAsync()
    {
        if (_channel is not null)
            await _channel.DisposeAsync();

        if (_connection is not null)
            await _connection.DisposeAsync();

        _connectionLock.Dispose();
    }
}
