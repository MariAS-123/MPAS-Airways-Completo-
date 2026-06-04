using System;
using System.Collections.Generic;

namespace Microservicio.Geografia.DataAccess.Entities
{
    public class CiudadEntity
    {
        public int IdCiudad { get; set; }

        public int IdPais { get; set; }

        public string Nombre { get; set; } = null!;

        public string? CodigoPostal { get; set; }

        public string? ZonaHoraria { get; set; }

        public decimal? Latitud { get; set; }

        public decimal? Longitud { get; set; }

        public string Estado { get; set; } = "ACTIVO";

        public bool Eliminado { get; set; }

        public DateTime FechaRegistroUtc { get; set; }

        public string CreadoPorUsuario { get; set; } = "SYSTEM";

        public string? ModificadoPorUsuario { get; set; }

        public DateTime? FechaModificacionUtc { get; set; }

        public string? ModificacionIp { get; set; }

        // Navegación interna válida
        public virtual PaisEntity Pais { get; set; } = null!;
    }
}