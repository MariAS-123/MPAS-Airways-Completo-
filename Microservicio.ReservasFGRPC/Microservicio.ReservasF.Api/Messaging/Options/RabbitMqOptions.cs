namespace Microservicio.ReservasF.Api.Messaging.Options;

public class RabbitMqOptions
{
    public const string SectionName = "RabbitMQ";

    public bool Enabled { get; set; }

    public string HostName { get; set; } = "localhost";

    public int Port { get; set; } = 5672;

    public string UserName { get; set; } = "guest";

    public string Password { get; set; } = "guest";

    public string VirtualHost { get; set; } = "vuelos-marketplace";

    public string ExchangeName { get; set; } = "vuelos.marketplace.events";

    public string ReservaSolicitadaQueue { get; set; } = "reservas.reserva-solicitada.queue";

    public ushort PrefetchCount { get; set; } = 1;
}
