namespace Microservicio.Geografia.Business.Exceptions;

public class NotFoundException : BusinessException
{
    public NotFoundException(string message)
        : base("RESOURCE_NOT_FOUND", message, 404)
    {
    }
}