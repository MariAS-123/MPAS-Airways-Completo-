namespace Marketplace.Events.Contracts.Payloads;

/// <summary>
/// Pasajero enviado desde el marketplace. Alineado a MS Clientes (crear o validar existente).
/// </summary>
public record PasajeroPayload
{
    public int? IdPasajero { get; init; }
    public int IdCliente { get; init; }
    public string NombrePasajero { get; init; } = null!;
    public string ApellidoPasajero { get; init; } = null!;
    public string TipoDocumentoPasajero { get; init; } = null!;
    public string NumeroDocumentoPasajero { get; init; } = null!;
    public DateTime? FechaNacimientoPasajero { get; init; }
    public int? IdPaisNacionalidad { get; init; }
    public string? EmailContactoPasajero { get; init; }
    public string? TelefonoContactoPasajero { get; init; }
    public string? GeneroPasajero { get; init; }
    public bool RequiereAsistencia { get; init; }
    public string? ObservacionesPasajero { get; init; }
}
