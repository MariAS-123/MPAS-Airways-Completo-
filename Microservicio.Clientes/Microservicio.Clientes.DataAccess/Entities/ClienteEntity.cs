using System;
using System.Collections.Generic;

namespace Microservicio.Clientes.DataAccess.Entities
{
    public class ClienteEntity
    {
        public int IdCliente { get; set; }

        public Guid ClienteGuid { get; set; }

        public string TipoIdentificacion { get; set; } = null!;

        public string NumeroIdentificacion { get; set; } = null!;

        public string Nombres { get; set; } = null!;

        public string? Apellidos { get; set; }

        public string? RazonSocial { get; set; }

        public string Correo { get; set; } = null!;

        public string Telefono { get; set; } = null!;

        public string Direccion { get; set; } = null!;

        // Se mantiene como int simple — FK cruzada eliminada (Ciudad está en BDD_Geografia)
        public int IdCiudadResidencia { get; set; }

        // Se mantiene como int simple — FK cruzada eliminada (País está en BDD_Geografia)
        public int IdPaisNacionalidad { get; set; }

        public DateTime? FechaNacimiento { get; set; }

        public string? Genero { get; set; }

        public string Estado { get; set; } = null!;

        public bool EsEliminado { get; set; }

        public string CreadoPorUsuario { get; set; } = null!;

        public DateTime FechaRegistroUtc { get; set; }

        public string? ModificadoPorUsuario { get; set; }

        public DateTime? FechaModificacionUtc { get; set; }

        public string? ModificacionIp { get; set; }

        public string ServicioOrigen { get; set; } = null!;

        public DateTime? FechaInhabilitacionUtc { get; set; }

        public string? MotivoInhabilitacion { get; set; }

        // ✅ Se mantiene — Pasajero vive en esta misma BDD_Clientes
        public virtual ICollection<PasajeroEntity> Pasajeros { get; set; }
            = new HashSet<PasajeroEntity>();
    }
}