using Marketplace.Events.Contracts.Events;
using Marketplace.Events.Contracts.Routing;
using Microservicio.Clientes.Api.Messaging.Handlers;
using Microservicio.Clientes.Api.Messaging.Options;
using Microservicio.Clientes.Api.Messaging.Serialization;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace Microservicio.Clientes.Api.Messaging.Consumers;

public sealed class PasajerosRegistradosConsumer : BackgroundService
{
    private readonly RabbitMqOptions _options;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<PasajerosRegistradosConsumer> _logger;
    private IConnection? _connection;
    private IChannel? _channel;

    public PasajerosRegistradosConsumer(
        IOptions<RabbitMqOptions> options,
        IServiceScopeFactory scopeFactory,
        ILogger<PasajerosRegistradosConsumer> logger)
    {
        _options = options.Value;
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!_options.Enabled)
        {
            _logger.LogInformation("PasajerosRegistradosConsumer inactivo (RabbitMQ:Enabled=false).");
            return;
        }

        var factory = new ConnectionFactory
        {
            HostName = _options.HostName,
            Port = _options.Port,
            UserName = _options.UserName,
            Password = _options.Password,
            VirtualHost = _options.VirtualHost
        };

        _connection = await factory.CreateConnectionAsync(stoppingToken);
        _channel = await _connection.CreateChannelAsync(cancellationToken: stoppingToken);

        await _channel.ExchangeDeclareAsync(
            _options.ExchangeName,
            ExchangeType.Topic,
            durable: true,
            autoDelete: false,
            cancellationToken: stoppingToken);

        await _channel.QueueDeclareAsync(
            _options.PasajerosRegistradosQueue,
            durable: true,
            exclusive: false,
            autoDelete: false,
            cancellationToken: stoppingToken);

        await _channel.QueueBindAsync(
            _options.PasajerosRegistradosQueue,
            _options.ExchangeName,
            MarketplaceRoutingKeys.PasajerosRegistrados,
            cancellationToken: stoppingToken);

        await _channel.BasicQosAsync(0, _options.PrefetchCount, false, stoppingToken);

        var consumer = new AsyncEventingBasicConsumer(_channel);
        consumer.ReceivedAsync += async (_, delivery) =>
        {
            var success = false;
            try
            {
                var message = MarketplaceEventJson.Deserialize<PasajerosRegistradosEvent>(delivery.Body.Span);
                if (message is null)
                {
                    _logger.LogError("Mensaje PasajerosRegistrados inválido (JSON nulo).");
                    return;
                }

                await using var scope = _scopeFactory.CreateAsyncScope();
                var handler = scope.ServiceProvider.GetRequiredService<PasajerosRegistradosHandler>();
                success = await handler.HandleAsync(message, stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error inesperado en cola {Queue}.", _options.PasajerosRegistradosQueue);
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

        await _channel.BasicConsumeAsync(_options.PasajerosRegistradosQueue, false, consumer, stoppingToken);

        _logger.LogInformation(
            "PasajerosRegistradosConsumer escuchando cola {Queue}.",
            _options.PasajerosRegistradosQueue);

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
