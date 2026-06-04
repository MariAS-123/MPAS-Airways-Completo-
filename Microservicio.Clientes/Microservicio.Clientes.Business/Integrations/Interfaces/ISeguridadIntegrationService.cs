namespace Microservicio.Clientes.Business.Integrations.Interfaces;

public interface ISeguridadIntegrationService
{
    Task<UsuarioAppIntegrationResponseDto> CrearUsuarioAppAsync(
        UsuarioAppIntegrationRequestDto request,
        CancellationToken cancellationToken = default);
}