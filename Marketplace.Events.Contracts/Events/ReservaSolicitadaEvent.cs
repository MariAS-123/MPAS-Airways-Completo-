using Marketplace.Events.Contracts.Payloads;

namespace Marketplace.Events.Contracts.Events;

/// <summary>
/// Gateway → MS Reservas. Dispara CreateAsync + cadena interna de factura/boleto.
/// Incluye datos de pago (cargo servicio) que hoy van en PATCH pagar.
/// </summary>
public record ReservaSolicitadaEvent : MarketplaceEvent
{
    public int IdCliente { get; init; }
    public int IdVuelo { get; init; }
    public decimal SubtotalReserva { get; init; }
    public decimal ValorIva { get; init; }
    public decimal TotalReserva { get; init; }
    public decimal CargoServicio { get; init; }
    public string? ContactoEmail { get; init; }
    public string? ContactoTelefono { get; init; }
    public string? Observaciones { get; init; }
    public string OrigenCanalReserva { get; init; } = "MARKETPLACE";
    public IReadOnlyList<ReservaDetallePayload> Detalles { get; init; } = [];
    /// <summary>
    /// Equipaje por pasajero (MANO/BODEGA). Lista vacía = sin registros en ventas.equipaje.
    /// </summary>
    public IReadOnlyList<EquipajePayload> Equipaje { get; init; } = [];
    public string? TokenPreReserva { get; init; }
    public string CreadoPorUsuario { get; init; } = "marketplace-gateway";
}
