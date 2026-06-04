using Microservicio.Seguridad.Business.DTOs.Internal;

namespace Microservicio.Seguridad.Business.Interfaces;

/// <summary>
/// Contrato del servicio interno de Seguridad.
/// Expone operaciones que solo el Bus de Integración u otros
/// microservicios internos autorizados pueden invocar.
/// No debe exponerse a usuarios finales ni aparecer en Swagger público.
/// </summary>
public interface IInternalSeguridadService
{
    /// <summary>
    /// Crea un usuario de aplicación con el IdCliente real ya vinculado.
    /// Reemplaza el flujo incompleto de register-cliente donde IdCliente = 0.
    /// Hashea la contraseña internamente, asigna rol CLIENTE
    /// y guarda la referencia lógica a MS Clientes en id_cliente.
    /// </summary>
    /// <param name="request">Datos mínimos para crear la cuenta de aplicación.</param>
    /// <returns>Datos del usuario creado para correlación en el Bus.</returns>
    Task<CreateUserForClientResponse> CreateUserForClientAsync(CreateUserForClientRequest request);
}