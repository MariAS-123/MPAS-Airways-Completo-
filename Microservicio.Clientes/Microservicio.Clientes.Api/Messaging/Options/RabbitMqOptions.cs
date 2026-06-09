using RabbitMQ.Client;

namespace Microservicio.Clientes.Api.Messaging.Options;

public class RabbitMqOptions
{
    public const string SectionName = "RabbitMQ";

    public bool Enabled { get; set; }

    public string HostName { get; set; } = "localhost";

    public int Port { get; set; } = 5672;

    public string UserName { get; set; } = "guest";

    public string Password { get; set; } = "guest";

    public string VirtualHost { get; set; } = "vuelos-marketplace";

    public bool UseSsl { get; set; }

    public string ExchangeName { get; set; } = "vuelos.marketplace.events";

    public string PasajerosRegistradosQueue { get; set; } = "reservas.pasajeros-registrados.queue";

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
