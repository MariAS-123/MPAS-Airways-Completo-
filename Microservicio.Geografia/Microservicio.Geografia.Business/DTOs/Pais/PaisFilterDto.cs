namespace Microservicio.Geografia.Business.DTOs.Pais;

public class PaisFilterDto
{
    public string? Nombre { get; set; }

    public string? CodigoIso2 { get; set; }

    public string? CodigoIso3 { get; set; }

    public string? Continente { get; set; }

    public string? Estado { get; set; }

    public int PaginaActual { get; set; } = 1;

    public int TamanoPagina { get; set; } = 20;
}