using Microsoft.EntityFrameworkCore;
using Microservicio.Clientes.DataAccess.Context;
using Microservicio.Clientes.DataAccess.Common;

namespace Microservicio.Clientes.DataAccess.Queries
{
    public class PasajeroQueryRepository
    {
        private readonly SistemaVuelosClientesDBContext _context;

        public PasajeroQueryRepository(SistemaVuelosClientesDBContext context)
        {
            _context = context;
        }

        public class PasajeroResumenDto
        {
            public int IdPasajero { get; set; }
            public string NombreCompleto { get; set; } = string.Empty;
            public string TipoDocumento { get; set; } = string.Empty;
            public string NumeroDocumento { get; set; } = string.Empty;
            public bool RequiereAsistencia { get; set; }
            public string? Observaciones { get; set; }
        }

        // ✅ Se mantiene — solo usa Pasajeros de BDD_Clientes
        public async Task<List<PasajeroResumenDto>> ObtenerConAsistenciaEspecialAsync(
            CancellationToken cancellationToken = default)
        {
            return await _context.Pasajeros
                .AsNoTracking()
                .Where(p => p.RequiereAsistencia && !p.EsEliminado)
                .Select(p => new PasajeroResumenDto
                {
                    IdPasajero = p.IdPasajero,
                    NombreCompleto = p.NombrePasajero + " " + p.ApellidoPasajero,
                    TipoDocumento = p.TipoDocumentoPasajero,
                    NumeroDocumento = p.NumeroDocumentoPasajero,
                    RequiereAsistencia = p.RequiereAsistencia,
                    Observaciones = p.ObservacionesPasajero
                })
                .ToListAsync(cancellationToken);
        }

        // Búsqueda paginada de pasajeros con filtros
        public async Task<PagedResult<PasajeroResumenDto>> BuscarPasajerosAsync(
            string? nombre, string? numeroDocumento, int? idCliente,
            int pagina, int tamano,
            CancellationToken cancellationToken = default)
        {
            var query = _context.Pasajeros
                .AsNoTracking()
                .Where(p => !p.EsEliminado);

            if (!string.IsNullOrWhiteSpace(nombre))
                query = query.Where(p =>
                    p.NombrePasajero.Contains(nombre) ||
                    p.ApellidoPasajero.Contains(nombre));

            if (!string.IsNullOrWhiteSpace(numeroDocumento))
                query = query.Where(p => p.NumeroDocumentoPasajero.Contains(numeroDocumento));

            if (idCliente.HasValue)
                query = query.Where(p => p.IdCliente == idCliente.Value);

            var total = await query.CountAsync(cancellationToken);

            var items = await query
                .OrderBy(p => p.ApellidoPasajero)
                .ThenBy(p => p.NombrePasajero)
                .Skip((pagina - 1) * tamano)
                .Take(tamano)
                .Select(p => new PasajeroResumenDto
                {
                    IdPasajero = p.IdPasajero,
                    NombreCompleto = p.NombrePasajero + " " + p.ApellidoPasajero,
                    TipoDocumento = p.TipoDocumentoPasajero,
                    NumeroDocumento = p.NumeroDocumentoPasajero,
                    RequiereAsistencia = p.RequiereAsistencia,
                    Observaciones = p.ObservacionesPasajero
                })
                .ToListAsync(cancellationToken);

            return new PagedResult<PasajeroResumenDto>(items, total, pagina, tamano);
        }
    }
}