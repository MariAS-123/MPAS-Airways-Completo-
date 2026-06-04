namespace Microservicio.Aeropuertos.DataAccess.Common;

public class PagedResult<T>
{
    public IReadOnlyCollection<T> Items { get; }

    public int TotalRegistros { get; }

    public int PaginaActual { get; }

    public int TamanoPagina { get; }

    public int TotalPaginas =>
        TamanoPagina <= 0
            ? 0
            : (int)Math.Ceiling(
                (double)TotalRegistros / TamanoPagina);

    public bool TienePaginaAnterior =>
        PaginaActual > 1;

    public bool TienePaginaSiguiente =>
        PaginaActual < TotalPaginas;

    public PagedResult(
        IEnumerable<T> items,
        int totalRegistros,
        int paginaActual,
        int tamanoPagina)
    {
        ArgumentNullException.ThrowIfNull(items);

        if (paginaActual <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(paginaActual),
                "La página actual debe ser mayor a 0.");
        }

        if (tamanoPagina <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(tamanoPagina),
                "El tamaño de página debe ser mayor a 0.");
        }

        if (totalRegistros < 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(totalRegistros),
                "El total de registros no puede ser negativo.");
        }

        Items =
            items.ToList().AsReadOnly();

        TotalRegistros =
            totalRegistros;

        PaginaActual =
            paginaActual;

        TamanoPagina =
            tamanoPagina;
    }

    public static PagedResult<T> Crear(
        IEnumerable<T> items,
        int totalRegistros,
        int paginaActual,
        int tamanoPagina)
    {
        return new PagedResult<T>(
            items,
            totalRegistros,
            paginaActual,
            tamanoPagina);
    }
}