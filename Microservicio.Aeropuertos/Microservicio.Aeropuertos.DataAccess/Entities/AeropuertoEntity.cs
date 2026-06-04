namespace Microservicio.Aeropuertos.DataAccess.Entities;

public class AeropuertoEntity
{
    public int IdAeropuerto { get; set; }

    public string CodigoIata { get; set; } = null!;

    public string? CodigoIcao { get; set; }

    public string Nombre { get; set; } = null!;

    // Referencia lógica a MS Geografía (sin FK física)
    public int? IdCiudad { get; set; }

    // Referencia lógica a MS Geografía (sin FK física)
    public int IdPais { get; set; }

    public string? ZonaHoraria { get; set; }

    public decimal? Latitud { get; set; }

    public decimal? Longitud { get; set; }

    public string Estado { get; set; } = null!;

    public bool Eliminado { get; set; }

    public DateTime FechaRegistroUtc { get; set; }

    public string CreadoPorUsuario { get; set; } = null!;

    public string? ModificadoPorUsuario { get; set; }

    public DateTime? FechaModificacionUtc { get; set; }

    public string? ModificacionIp { get; set; }
}