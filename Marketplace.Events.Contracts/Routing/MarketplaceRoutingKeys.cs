namespace Marketplace.Events.Contracts.Routing;

/// <summary>
/// Claves de enrutamiento jerárquicas (exchange tipo topic).
/// Patrón: marketplace.&lt;dominio&gt;.&lt;accion&gt;
/// </summary>
public static class MarketplaceRoutingKeys
{
    public const string VueloSeleccionado = "marketplace.vuelo.seleccionado";
    public const string AsientoPreReservado = "marketplace.asiento.pre-reservado";
    public const string PasajerosRegistrados = "marketplace.pasajeros.registrados";
    public const string PasajerosValidados = "marketplace.pasajeros.validados";
    public const string EquipajeAgregado = "marketplace.equipaje.agregado";
    public const string ReservaSolicitada = "marketplace.reserva.solicitada";
    public const string ReservaCreada = "marketplace.reserva.creada";
    public const string ReservaRechazada = "marketplace.reserva.rechazada";
    public const string FacturaGenerada = "marketplace.factura.generada";
    public const string BoletoEmitido = "marketplace.boleto.emitido";

    /// <summary>
    /// Alias usado en el diagrama del PDF para la cola que consume el Gateway al finalizar la saga.
    /// Mapea al mismo payload que <see cref="BoletoEmitido"/> o estado COMPLETADA en polling.
    /// </summary>
    public const string ReservaConfirmada = "marketplace.reserva.confirmada";
}
