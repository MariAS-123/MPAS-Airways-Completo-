namespace Microservicio.Aeropuertos.Business.Integrations;

public class CiudadIntegrationDto
{
    public int IdCiudad { get; set; }

    public int IdPais { get; set; }

    public string Nombre { get; set; } = string.Empty;

    public string Estado { get; set; } = string.Empty;
}