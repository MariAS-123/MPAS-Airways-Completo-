namespace Microservicio.Aeropuertos.Business.DTOs.Aeropuerto;

public class AeropuertoUpdateRequestDto
{
    public string CodigoIata { get; set; } = null!;

    public string? CodigoIcao { get; set; }

    public string Nombre { get; set; } = null!;

    // Referencia lógica a MS Geografía
    public int? IdCiudad { get; set; }
    // Referencia lógica a MS Geografía
    public int IdPais { get; set; }

    public string? ZonaHoraria { get; set; }

    public decimal? Latitud { get; set; }

    public decimal? Longitud { get; set; }
}