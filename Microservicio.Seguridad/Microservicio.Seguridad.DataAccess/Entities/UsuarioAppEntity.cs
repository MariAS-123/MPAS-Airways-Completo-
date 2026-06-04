using System;
using System.Collections.Generic;

namespace Microservicio.Seguridad.DataAccess.Entities
{
    public class UsuarioAppEntity
    {
        public int IdUsuario { get; set; }
        public Guid UsuarioGuid { get; set; }
        public int? IdCliente { get; set; }
        // Referencia lógica al MS Clientes — sin navigation property
        public string Username { get; set; } = null!;
        public string Correo { get; set; } = null!;
        public string PasswordHash { get; set; } = null!;
        public string PasswordSalt { get; set; } = null!;
        public DateTime? FechaUltimoLogin { get; set; }
        public string EstadoUsuario { get; set; } = null!;
        public bool EsEliminado { get; set; }
        public bool Activo { get; set; }
        public string CreadoPorUsuario { get; set; } = null!;
        public DateTime FechaRegistroUtc { get; set; }
        public string? ModificadoPorUsuario { get; set; }
        public DateTime? FechaModificacionUtc { get; set; }
        public string? ModificacionIp { get; set; }

        // Navigation properties
        public virtual ICollection<UsuarioRolEntity> UsuariosRoles { get; set; }
            = new HashSet<UsuarioRolEntity>();
    }
}