using Microsoft.EntityFrameworkCore;
using Microservicio.Clientes.DataAccess.Context;
using Microservicio.Clientes.DataAccess.Common;

namespace Microservicio.Clientes.DataAccess.Queries
{
    public class ClienteQueryRepository
    {
        private readonly SistemaVuelosClientesDBContext _context;

        public ClienteQueryRepository(SistemaVuelosClientesDBContext context)
        {
            _context = context;
        }

        public class ClienteResumenDto
        {
            public int IdCliente { get; set; }
            public string Nombres { get; set; } = string.Empty;
            public string? Apellidos { get; set; }
            public string Correo { get; set; } = string.Empty;
            public string NumeroIdentificacion { get; set; } = string.Empty;
            public string Estado { get; set; } = string.Empty;
            public int TotalPasajeros { get; set; }
        }

        // Clientes con sus pasajeros asociados — todo en BDD_Clientes
        public async Task<ClienteResumenDto?> ObtenerClienteConPasajerosAsync(
            int idCliente, CancellationToken cancellationToken = default)
        {
            return await _context.Clientes
                .AsNoTracking()
                .Where(c => c.IdCliente == idCliente && !c.EsEliminado)
                .Select(c => new ClienteResumenDto
                {
                    IdCliente = c.IdCliente,
                    Nombres = c.Nombres,
                    Apellidos = c.Apellidos,
                    Correo = c.Correo,
                    NumeroIdentificacion = c.NumeroIdentificacion,
                    Estado = c.Estado,
                    // ✅ Pasajero sí vive en BDD_Clientes
                    TotalPasajeros = c.Pasajeros.Count(p => !p.EsEliminado)
                })
                .FirstOrDefaultAsync(cancellationToken);
        }

        // Búsqueda paginada de clientes con filtros
        public async Task<PagedResult<ClienteResumenDto>> BuscarClientesAsync(
            string? nombre, string? correo, string? numeroIdentificacion,
            string? estado, int pagina, int tamano,
            CancellationToken cancellationToken = default)
        {
            var query = _context.Clientes
                .AsNoTracking()
                .Where(c => !c.EsEliminado);

            if (!string.IsNullOrWhiteSpace(nombre))
                query = query.Where(c =>
                    c.Nombres.Contains(nombre) ||
                    (c.Apellidos != null && c.Apellidos.Contains(nombre)));

            if (!string.IsNullOrWhiteSpace(correo))
                query = query.Where(c => c.Correo.Contains(correo));

            if (!string.IsNullOrWhiteSpace(numeroIdentificacion))
                query = query.Where(c => c.NumeroIdentificacion.Contains(numeroIdentificacion));

            if (!string.IsNullOrWhiteSpace(estado))
                query = query.Where(c => c.Estado == estado);

            var total = await query.CountAsync(cancellationToken);

            var items = await query
                .OrderBy(c => c.Apellidos)
                .ThenBy(c => c.Nombres)
                .Skip((pagina - 1) * tamano)
                .Take(tamano)
                .Select(c => new ClienteResumenDto
                {
                    IdCliente = c.IdCliente,
                    Nombres = c.Nombres,
                    Apellidos = c.Apellidos,
                    Correo = c.Correo,
                    NumeroIdentificacion = c.NumeroIdentificacion,
                    Estado = c.Estado,
                    TotalPasajeros = c.Pasajeros.Count(p => !p.EsEliminado)
                })
                .ToListAsync(cancellationToken);

            return new PagedResult<ClienteResumenDto>(items, total, pagina, tamano);
        }
    }
}