namespace Microservicio.Seguridad.Business.DTOs.Internal;

/// <summary>
/// Respuesta que MS Seguridad devuelve al Bus después de crear
/// el usuario de aplicación vinculado al cliente real.
/// </summary>
public class CreateUserForClientResponse
{
    /// <summary>
    /// Id interno del usuario creado en seg.usuario_app.
    /// El Bus puede usarlo para correlacionar eventos futuros.
    /// </summary>
    public int IdUsuario { get; set; }

    /// <summary>
    /// GUID estable del usuario. Preferir este sobre IdUsuario
    /// para referencias en eventos y comandos del bus.
    /// </summary>
    public Guid UsuarioGuid { get; set; }

    /// <summary>
    /// Id del cliente en MS Clientes que quedó vinculado en seg.usuario_app.id_cliente.
    /// Confirma que la referencia lógica se guardó correctamente.
    /// </summary>
    public int IdCliente { get; set; }

    /// <summary>
    /// Username creado. El Bus puede notificar a MS Clientes o Auditoría.
    /// </summary>
    public string Username { get; set; } = null!;

    /// <summary>
    /// Rol asignado automáticamente al usuario creado.
    /// Siempre será "CLIENTE" para este flujo.
    /// </summary>
    public string RolAsignado { get; set; } = null!;

    /// <summary>
    /// CorrelationId recibido, devuelto para que el Bus confirme la correlación.
    /// </summary>
    public string? CorrelationId { get; set; }
}