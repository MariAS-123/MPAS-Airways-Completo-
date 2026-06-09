namespace Microservicio.ReservasF.Api.Messaging;

/// <summary>
/// Token JWT propagado desde mensajes RabbitMQ cuando no hay HttpContext.
/// </summary>
public interface IMessagingAccessTokenAccessor
{
    string? Token { get; set; }
}
