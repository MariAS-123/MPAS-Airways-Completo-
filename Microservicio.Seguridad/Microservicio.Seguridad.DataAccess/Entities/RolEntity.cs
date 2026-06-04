using System;
using System.Collections.Generic;

namespace Microservicio.Seguridad.DataAccess.Entities
{
    public class RolEntity
    {
        public int IdRol { get; set; }
        public Guid RolGuid { get; set; }
        public string NombreRol { get; set; } = null!;
        public string? DescripcionRol { get; set; }
        public string EstadoRol { get; set; } = null!;
        public bool EsEliminado { get; set; }
        public bool Activo { get; set; }
        public string CreadoPorUsuario { get; set; } = null!;
        public DateTime FechaRegistroUtc { get; set; }
        public string? ModificadoPorUsuario { get; set; }
        public DateTime? FechaModificacionUtc { get; set; }

        // Navigation properties
        public virtual ICollection<UsuarioRolEntity> UsuariosRoles { get; set; }
            = new HashSet<UsuarioRolEntity>();
    }
}