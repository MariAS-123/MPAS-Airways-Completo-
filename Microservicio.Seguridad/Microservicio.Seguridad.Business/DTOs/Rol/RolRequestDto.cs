namespace Microservicio.Seguridad.Business.DTOs.Rol;

public class RolRequestDto
{
    public string NombreRol { get; set; } = null!;
    public string? DescripcionRol { get; set; }
}