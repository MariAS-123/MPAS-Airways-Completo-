// PasajeroRequestDto.cs
using System.Text.Json.Serialization;

namespace Microservicio.Clientes.Business.DTOs.Pasajero;

public class PasajeroRequestDto
{
    [JsonPropertyName("idCliente")]
    public int? IdCliente { get; set; }

    [JsonPropertyName("nombrePasajero")]
    public string NombrePasajero { get; set; } = null!;

    [JsonPropertyName("apellidoPasajero")]
    public string ApellidoPasajero { get; set; } = null!;

    [JsonPropertyName("tipoDocumentoPasajero")]
    public string TipoDocumentoPasajero { get; set; } = null!;

    [JsonPropertyName("numeroDocumentoPasajero")]
    public string NumeroDocumentoPasajero { get; set; } = null!;

    [JsonPropertyName("fechaNacimientoPasajero")]
    public DateTime? FechaNacimientoPasajero { get; set; }

    [JsonPropertyName("idPaisNacionalidad")]
    public int? IdPaisNacionalidad { get; set; }

    [JsonPropertyName("emailContactoPasajero")]
    public string? EmailContactoPasajero { get; set; }

    [JsonPropertyName("telefonoContactoPasajero")]
    public string? TelefonoContactoPasajero { get; set; }

    [JsonPropertyName("generoPasajero")]
    public string? GeneroPasajero { get; set; }

    [JsonPropertyName("requiereAsistencia")]
    public bool RequiereAsistencia { get; set; }

    [JsonPropertyName("observacionesPasajero")]
    public string? ObservacionesPasajero { get; set; }
}