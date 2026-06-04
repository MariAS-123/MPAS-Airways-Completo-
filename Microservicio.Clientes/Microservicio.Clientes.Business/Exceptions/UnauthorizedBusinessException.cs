// UnauthorizedBusinessException.cs
namespace Microservicio.Clientes.Business.Exceptions;

public class UnauthorizedBusinessException : BusinessException
{
    public UnauthorizedBusinessException(string message)
        : base("UNAUTHORIZED", message, 401)
    {
    }
}