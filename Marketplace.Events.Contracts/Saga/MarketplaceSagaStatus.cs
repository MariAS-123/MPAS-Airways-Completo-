namespace Marketplace.Events.Contracts.Saga;

/// <summary>
/// Estados del flujo marketplace para polling GraphQL (Gateway). No es un mensaje RabbitMQ.
/// </summary>
public enum MarketplaceSagaStatus
{
    Aceptada = 0,
    VueloSeleccionado = 1,
    AsientoPreReservado = 2,
    PasajerosValidados = 3,
    EquipajeRegistrado = 4,
    ReservaEnProceso = 5,
    ReservaCreada = 6,
    FacturaGenerada = 7,
    BoletoEmitido = 8,
    Completada = 9,
    Rechazada = 99
}
