namespace Microservicio.Geografia.DataManagement.Common;

public class DataPagedResult<T>
{
    public IReadOnlyCollection<T> Items { get; set; }
        = new List<T>();

    public int TotalRegistros { get; set; }

    public int PaginaActual { get; set; }

    public int TamanoPagina { get; set; }

    public int TotalPaginas =>
        TamanoPagina <= 0
            ? 0
            : (int)Math.Ceiling(
                (double)TotalRegistros / TamanoPagina);

    public bool TienePaginaAnterior =>
        PaginaActual > 1;

    public bool TienePaginaSiguiente =>
        PaginaActual < TotalPaginas;
}