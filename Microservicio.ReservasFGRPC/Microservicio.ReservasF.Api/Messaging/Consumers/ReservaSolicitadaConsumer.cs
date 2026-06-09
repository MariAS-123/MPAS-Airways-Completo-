using Marketplace.Events.Contracts.Events;
using Marketplace.Events.Contracts.Routing;
using Microservicio.ReservasF.Api.Messaging.Handlers;
using Microservicio.ReservasF.Api.Messaging.Options;
using Microservicio.ReservasF.Api.Messaging.Serialization;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace Microservicio.ReservasF.Api.Messaging.Consumers;

public sealed class ReservaSolicitadaConsumer : BackgroundService
{
    private readonly RabbitMqOptions _options;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<ReservaSolicitadaConsumer> _logger;
    private IConnection? _connection;
    private IChannel? _channel;

    public ReservaSolicitadaConsumer(
        IOptions<RabbitMqOptions> options,
        IServiceScopeFactory scopeFactory,
        ILogger<ReservaSolicitadaConsumer> logger)
    {
        _options = options.Value;
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!_options.Enabled)
        {
            _logger.LogInformation("ReservaSolicitadaConsumer inactivo (RabbitMQ:Enabled=false).");
            return;
        }

        var factory = _options.CreateConnectionFactory();

        _connection = await factory.CreateConnectionAsync(stoppingToken);
        _channel = await _connection.CreateChannelAsync(cancellationToken: stoppingToken);

        await _channel.ExchangeDeclareAsync(
            exchange: _options.ExchangeName,
            type: ExchangeType.Topic,
            durable: true,
            autoDelete: false,
            cancellationToken: stoppingToken);

        await _channel.QueueDeclareAsync(
            queue: _options.ReservaSolicitadaQueue,
            durable: true,
            exclusive: false,
            autoDelete: false,
            cancellationToken: stoppingToken);

        await _channel.QueueBindAsync(
            queue: _options.ReservaSolicitadaQueue,
            exchange: _options.ExchangeName,
            routingKey: MarketplaceRoutingKeys.ReservaSolicitada,
            cancellationToken: stoppingToken);

        await _channel.BasicQosAsync(
            prefetchSize: 0,
            prefetchCount: _options.PrefetchCount,
            global: false,
            cancellationToken: stoppingToken);

        var consumer = new AsyncEventingBasicConsumer(_channel);
        consumer.ReceivedAsync += async (_, delivery) =>
        {
            var success = false;

            try
            {
                var message = MarketplaceEventJson.Deserialize<ReservaSolicitadaEvent>(delivery.Body.Span);

                if (message is null)
                {
                    _logger.LogError("Mensaje ReservaSolicitada inválido (JSON nulo).");
                    return;
                }

                await using var scope = _scopeFactory.CreateAsyncScope();
                var handler = scope.ServiceProvider.GetRequiredService<ReservaSolicitadaHandler>();
                success = await handler.HandleAsync(message, stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error inesperado procesando cola {Queue}.", _options.ReservaSolicitadaQueue);
            }
            finally
            {
                if (_channel is { IsOpen: true })
                {
                    if (success)
                    {
                        await _channel.BasicAckAsync(delivery.DeliveryTag, multiple: false, stoppingToken);
                    }
                    else
                    {
                        await _channel.BasicNackAsync(
                            delivery.DeliveryTag,
                            multiple: false,
                            requeue: false,
                            cancellationToken: stoppingToken);
                    }
                }
            }
        };

        await _channel.BasicConsumeAsync(
            queue: _options.ReservaSolicitadaQueue,
            autoAck: false,
            consumer: consumer,
            cancellationToken: stoppingToken);

        _logger.LogInformation(
            "ReservaSolicitadaConsumer escuchando cola {Queue}.",
            _options.ReservaSolicitadaQueue);

        await Task.Delay(Timeout.Infinite, stoppingToken);
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
