// PasajeroFiltroDataModel.cs
namespace Microservicio.Clientes.DataManagement.Models;

public class PasajeroFiltroDataModel
{
    public int? IdCliente { get; set; }
    public string? NombrePasajero { get; set; }
    public string? ApellidoPasajero { get; set; }
    public string? TipoDocumentoPasajero { get; set; }
    public string? NumeroDocumentoPasajero { get; set; }
    public string? Estado { get; set; }
    public bool? RequiereAsistencia { get; set; }
    public bool IncluirEliminados { get; set; } = false;
    public int PageNumber { get; set; } = 1;
    public int PageSize { get; set; } = 10;
}