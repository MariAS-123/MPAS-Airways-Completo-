using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace Microservicio.Seguridad.Business.DTOs.Internal;

/// <summary>
/// Request que el Bus de Integración envía a MS Seguridad
/// para crear un usuario de aplicación ya vinculado a un cliente real.
/// Este endpoint reemplaza el flujo incompleto de register-cliente
/// donde IdCliente quedaba en null.
/// </summary>
public class CreateUserForClientRequest
{
    /// <summary>
    /// Id real del cliente creado en MS Clientes.
    /// Se guardará en seg.usuario_app.id_cliente como referencia lógica.
    /// </summary>
    [JsonPropertyName("id_cliente")]
    [Required]
    public int IdCliente { get; set; }

    /// <summary>
    /// Nombre de usuario único para la cuenta de aplicación.
    /// Máximo 50 caracteres, igual que el resto de usuarios del sistema.
    /// </summary>
    [JsonPropertyName("username")]
    [Required]
    [MaxLength(50)]
    public string Username { get; set; } = null!;

    /// <summary>
    /// Correo electrónico único del usuario.
    /// Máximo 120 caracteres.
    /// </summary>
    [JsonPropertyName("correo")]
    [Required]
    [EmailAddress]
    [MaxLength(120)]
    public string Correo { get; set; } = null!;

    /// <summary>
    /// Contraseña en texto plano. Seguridad genera el salt y el hash internamente.
    /// Nunca se almacena en texto plano.
    /// Mínimo 8 caracteres.
    /// </summary>
    [JsonPropertyName("password")]
    [Required]
    [MinLength(8)]
    public string Password { get; set; } = null!;

    /// <summary>
    /// CorrelationId del flujo de la saga que origina esta solicitud.
    /// Permite trazar el flujo completo entre Bus, Clientes y Seguridad.
    /// </summary>
    [JsonPropertyName("correlation_id")]
    public string? CorrelationId { get; set; }
}