namespace Microservicio.Aeropuertos.Business.Integrations;

public class PaisIntegrationDto
{
    public int IdPais { get; set; }

    public string Nombre { get; set; } = string.Empty;

    public string Estado { get; set; } = string.Empty;
}