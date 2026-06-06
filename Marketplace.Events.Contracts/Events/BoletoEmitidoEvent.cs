namespace Marketplace.Events.Contracts.Events;

/// <summary>
/// MS Reservas → MS Vuelos / Gateway. Boleto emitido; Vuelos bloquea asiento definitivamente (EMI).
/// </summary>
public record BoletoEmitidoEvent : MarketplaceEvent
{
    public int IdBoleto { get; init; }
    public int IdReserva { get; init; }
    public string CodigoReserva { get; init; } = null!;
    public int IdCliente { get; init; }
    public int IdVuelo { get; init; }
    public int IdAsiento { get; init; }
    public int IdPasajero { get; init; }
    public string EstadoReserva { get; init; } = "EMI";
}
