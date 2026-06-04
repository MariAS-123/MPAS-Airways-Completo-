using System.Collections.Generic;

namespace Microservicio.Geografia.DataAccess.Entities
{
    public class PaisEntity
    {
        public int IdPais { get; set; }

        public string CodigoIso2 { get; set; } = null!;

        public string? CodigoIso3 { get; set; }

        public string Nombre { get; set; } = null!;

        public string? Continente { get; set; }

        public string Estado { get; set; } = "ACTIVO";

        public bool Eliminado { get; set; }

        // Navigation properties internas SOLO del mismo MS
        public virtual ICollection<CiudadEntity> Ciudades { get; set; }
            = new HashSet<CiudadEntity>();
    }
}