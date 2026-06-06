namespace Microservicio.Vuelos.Api.Messaging.Options;

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

    public string VueloSeleccionadoQueue { get; set; } = "reservas.vuelo-seleccionado.queue";

    public string BoletoEmitidoQueue { get; set; } = "vuelos.boleto-emitido.queue";

    public int PreReservaMinutos { get; set; } = 15;

    public ushort PrefetchCount { get; set; } = 1;
}
