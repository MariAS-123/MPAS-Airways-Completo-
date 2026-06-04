namespace Microservicio.Geografia.DataManagement.Models;

public class CiudadFiltroDataModel
{
    public int? IdPais { get; set; }

    public string? Nombre { get; set; }

    public string? CodigoPostal { get; set; }

    public string? ZonaHoraria { get; set; }

    public string? Estado { get; set; }

    public bool IncluirEliminados { get; set; } = false;

    public int PaginaActual { get; set; } = 1;

    public int TamanoPagina { get; set; } = 10;
}