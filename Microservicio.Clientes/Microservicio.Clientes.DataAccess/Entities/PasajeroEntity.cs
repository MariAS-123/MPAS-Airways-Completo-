using System;
using System.Collections.Generic;

namespace Microservicio.Clientes.DataAccess.Entities
{
    public class PasajeroEntity
    {
        public int IdPasajero { get; set; }


        public int? IdCliente { get; set; }

        public string NombrePasajero { get; set; } = null!;

        public string ApellidoPasajero { get; set; } = null!;

        public string TipoDocumentoPasajero { get; set; } = null!;

        public string NumeroDocumentoPasajero { get; set; } = null!;

        public DateTime? FechaNacimientoPasajero { get; set; }

        // Se mantiene como int? simple — FK cruzada eliminada (País está en BDD_Geografia)
        public int? IdPaisNacionalidad { get; set; }

        public string? EmailContactoPasajero { get; set; }

        public string? TelefonoContactoPasajero { get; set; }

        public string? GeneroPasajero { get; set; }

        public bool RequiereAsistencia { get; set; }

        public string? ObservacionesPasajero { get; set; }

        public string Estado { get; set; } = null!;

        public bool EsEliminado { get; set; }

        public string CreadoPorUsuario { get; set; } = null!;

        public DateTime FechaRegistroUtc { get; set; }

        public string? ModificadoPorUsuario { get; set; }

        public DateTime? FechaModificacionUtc { get; set; }

        public string? ModificacionIp { get; set; }

        // ✅ Se mantiene — Cliente vive en esta misma BDD_Clientes
        public virtual ClienteEntity? Cliente { get; set; }
    }
}