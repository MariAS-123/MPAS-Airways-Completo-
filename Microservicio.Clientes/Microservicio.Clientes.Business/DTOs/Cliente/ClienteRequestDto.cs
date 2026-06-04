// ClienteRequestDto.cs
namespace Microservicio.Clientes.Business.DTOs.Cliente;

public class ClienteRequestDto
{
    public string TipoIdentificacion { get; set; } = null!;
    public string NumeroIdentificacion { get; set; } = null!;
    public string Nombres { get; set; } = null!;
    public string? Apellidos { get; set; }
    public string? RazonSocial { get; set; }
    public string Correo { get; set; } = null!;
    public string Telefono { get; set; } = null!;
    public string Direccion { get; set; } = null!;
    public int IdCiudadResidencia { get; set; }
    public int IdPaisNacionalidad { get; set; }
    public DateTime? FechaNacimiento { get; set; }
    public string? Genero { get; set; }

    // ✅ Agregados — necesarios para crear USUARIO_APP en MS Seguridad
    public string Username { get; set; } = null!;
    public string Password { get; set; } = null!;
}