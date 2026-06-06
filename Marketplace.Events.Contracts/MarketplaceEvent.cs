namespace Marketplace.Events.Contracts;

/// <summary>
/// Metadatos comunes a todos los mensajes del marketplace (Reto 3).
/// </summary>
public abstract record MarketplaceEvent
{
    public Guid MessageId { get; init; } = Guid.NewGuid();

    /// <summary>
    /// Identificador de seguimiento del flujo completo del cliente (saga).
    /// </summary>
    public Guid CorrelationId { get; init; }

    public DateTime OccurredAtUtc { get; init; } = DateTime.UtcNow;

    /// <summary>
    /// Versión del contrato para evolucionar payloads sin romper consumidores.
    /// </summary>
    public string SchemaVersion { get; init; } = MarketplaceEventSchema.CurrentVersion;
}
