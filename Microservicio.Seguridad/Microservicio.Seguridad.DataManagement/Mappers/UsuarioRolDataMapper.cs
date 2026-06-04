using Microservicio.Seguridad.DataAccess.Entities;
using Microservicio.Seguridad.DataManagement.Models;

namespace Microservicio.Seguridad.DataManagement.Mappers;

public static class UsuarioRolDataMapper
{
    public static UsuarioRolDataModel ToDataModel(UsuarioRolEntity e) => new()
    {
        IdUsuarioRol = e.IdUsuarioRol,
        IdUsuario = e.IdUsuario,
        IdRol = e.IdRol,
        EstadoUsuarioRol = e.EstadoUsuarioRol,
        EsEliminado = e.EsEliminado,
        Activo = e.Activo,
        CreadoPorUsuario = e.CreadoPorUsuario,
        FechaRegistroUtc = e.FechaRegistroUtc,
        ModificadoPorUsuario = e.ModificadoPorUsuario,
        FechaModificacionUtc = e.FechaModificacionUtc
        // RowVersion eliminado
    };

    public static UsuarioRolEntity ToEntity(UsuarioRolDataModel m) => new()
    {
        IdUsuarioRol = m.IdUsuarioRol,
        IdUsuario = m.IdUsuario,
        IdRol = m.IdRol,
        EstadoUsuarioRol = string.IsNullOrWhiteSpace(m.EstadoUsuarioRol)
            ? "ACT"                                    // corregido: era "ACTIVO", el campo es char(3)
            : m.EstadoUsuarioRol.Trim().ToUpperInvariant(),
        EsEliminado = m.EsEliminado,
        Activo = m.Activo,
        CreadoPorUsuario = m.CreadoPorUsuario.Trim(),
        FechaRegistroUtc = m.FechaRegistroUtc == default ? DateTime.UtcNow : m.FechaRegistroUtc,
        ModificadoPorUsuario = string.IsNullOrWhiteSpace(m.ModificadoPorUsuario) ? null : m.ModificadoPorUsuario.Trim(),
        FechaModificacionUtc = m.FechaModificacionUtc
        // RowVersion eliminado
    };

    public static void UpdateEntity(UsuarioRolEntity e, UsuarioRolDataModel m)
    {
        e.EstadoUsuarioRol = m.EstadoUsuarioRol.Trim().ToUpperInvariant();
        e.Activo = m.Activo;
        e.ModificadoPorUsuario = string.IsNullOrWhiteSpace(m.ModificadoPorUsuario) ? null : m.ModificadoPorUsuario.Trim();
        e.FechaModificacionUtc = DateTime.UtcNow;
    }
}