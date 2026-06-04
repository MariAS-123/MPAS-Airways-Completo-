namespace Microservicio.Aeropuertos.DataManagement.Models;

public class AeropuertoFiltroDataModel
{
    public string? CodigoIata { get; set; }

    public string? CodigoIcao { get; set; }

    public string? Nombre { get; set; }

    // Referencia lógica a MS Geografía
    public int? IdCiudad { get; set; }

    // Referencia lógica a MS Geografía
    public int? IdPais { get; set; }

    public string? ZonaHoraria { get; set; }

    public string? Estado { get; set; }

    public bool IncluirEliminados { get; set; } = false;

    public int PageNumber { get; set; } = 1;

    public int PageSize { get; set; } = 10;
}