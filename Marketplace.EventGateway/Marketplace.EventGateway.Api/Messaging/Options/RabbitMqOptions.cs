using RabbitMQ.Client;

namespace Marketplace.EventGateway.Api.Messaging.Options;

public class RabbitMqOptions
{
    public const string SectionName = "RabbitMQ";

    public bool Enabled { get; set; } = true;
    public string HostName { get; set; } = "localhost";
    public int Port { get; set; } = 5672;
    public string UserName { get; set; } = "marketplace";
    public string Password { get; set; } = "marketplace123";
    public string VirtualHost { get; set; } = "vuelos-marketplace";
    public bool UseSsl { get; set; }
    public string ExchangeName { get; set; } = "vuelos.marketplace.events";
    public string GatewayEventsQueue { get; set; } = "gateway.marketplace-events.queue";
    public ushort PrefetchCount { get; set; } = 1;

    public ConnectionFactory CreateConnectionFactory()
    {
        var factory = new ConnectionFactory
        {
            HostName = HostName,
            Port = Port,
            UserName = UserName,
            Password = Password,
            VirtualHost = VirtualHost
        };

        if (UseSsl)
        {
            factory.Ssl = new SslOption
            {
                Enabled = true,
                ServerName = HostName
            };
        }

        return factory;
    }
}
