using Marketplace.Events.Contracts.Events;
using Marketplace.Events.Contracts.Routing;
using Microservicio.Vuelos.Api.Messaging.Handlers;
using Microservicio.Vuelos.Api.Messaging.Options;
using Microservicio.Vuelos.Api.Messaging.Serialization;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace Microservicio.Vuelos.Api.Messaging.Consumers;

public sealed class VueloSeleccionadoConsumer : BackgroundService
{
    private readonly RabbitMqOptions _options;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<VueloSeleccionadoConsumer> _logger;
    private IConnection? _connection;
    private IChannel? _channel;

    public VueloSeleccionadoConsumer(
        IOptions<RabbitMqOptions> options,
        IServiceScopeFactory scopeFactory,
        ILogger<VueloSeleccionadoConsumer> logger)
    {
        _options = options.Value;
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!_options.Enabled)
        {
            _logger.LogInformation("VueloSeleccionadoConsumer inactivo (RabbitMQ:Enabled=false).");
            return;
        }

        await StartConsumerAsync(
            _options.VueloSeleccionadoQueue,
            MarketplaceRoutingKeys.VueloSeleccionado,
            ProcessMessageAsync,
            stoppingToken);
    }

    private async Task StartConsumerAsync(
        string queueName,
        string routingKey,
        Func<IServiceProvider, ReadOnlyMemory<byte>, CancellationToken, Task<bool>> processAsync,
        CancellationToken stoppingToken)
    {
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
            queueName,
            durable: true,
            exclusive: false,
            autoDelete: false,
            cancellationToken: stoppingToken);

        await _channel.QueueBindAsync(
            queueName,
            _options.ExchangeName,
            routingKey,
            cancellationToken: stoppingToken);

        await _channel.BasicQosAsync(0, _options.PrefetchCount, false, stoppingToken);

        var consumer = new AsyncEventingBasicConsumer(_channel);
        consumer.ReceivedAsync += async (_, delivery) =>
        {
            var success = false;
            try
            {
                await using var scope = _scopeFactory.CreateAsyncScope();
                success = await processAsync(scope.ServiceProvider, delivery.Body, stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error inesperado en cola {Queue}.", queueName);
            }
            finally
            {
                if (_channel is { IsOpen: true })
                {
                    if (success)
                        await _channel.BasicAckAsync(delivery.DeliveryTag, false, stoppingToken);
                    else
                        await _channel.BasicNackAsync(delivery.DeliveryTag, false, false, stoppingToken);
                }
            }
        };

        await _channel.BasicConsumeAsync(queueName, false, consumer, stoppingToken);

        _logger.LogInformation("VueloSeleccionadoConsumer escuchando cola {Queue}.", queueName);
        await Task.Delay(Timeout.Infinite, stoppingToken);
    }

    private async Task<bool> ProcessMessageAsync(
        IServiceProvider services,
        ReadOnlyMemory<byte> body,
        CancellationToken cancellationToken)
    {
        var message = MarketplaceEventJson.Deserialize<VueloSeleccionadoEvent>(body.Span);
        if (message is null)
        {
            _logger.LogError("Mensaje VueloSeleccionado inválido (JSON nulo).");
            return false;
        }

        var handler = services.GetRequiredService<VueloSeleccionadoHandler>();
        return await handler.HandleAsync(message, cancellationToken);
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
