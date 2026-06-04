using Microservicio.Seguridad.Business.DTOs.Auth;
using Microservicio.Seguridad.Business.Exceptions;

namespace Microservicio.Seguridad.Business.Validators;

public class AuthValidator
{
    public void ValidateLogin(LoginRequest request)
    {
        var errors = new List<string>();

        if (request == null)
        {
            errors.Add("La solicitud de login es obligatoria.");
            ThrowIfAny(errors);
            return;
        }

        if (string.IsNullOrWhiteSpace(request.Username))
            errors.Add("El username es obligatorio.");
        else if (request.Username.Length > 50)
            errors.Add("El username no puede exceder 50 caracteres.");

        if (string.IsNullOrWhiteSpace(request.Password))
            errors.Add("La contraseña es obligatoria.");
        else if (request.Password.Length > 100)
            errors.Add("La contraseña no puede exceder 100 caracteres.");

        ThrowIfAny(errors);
    }

    public void ValidateRegisterCliente(RegisterClienteRequest request)
    {
        var errors = new List<string>();

        if (request == null)
        {
            errors.Add("La solicitud de registro es obligatoria.");
            ThrowIfAny(errors, "Error de validación en registro.");
            return;
        }

        if (string.IsNullOrWhiteSpace(request.Username))
            errors.Add("El username es obligatorio.");

        if (string.IsNullOrWhiteSpace(request.Password))
            errors.Add("La contraseña es obligatoria.");
        else if (request.Password.Length < 8)
            errors.Add("La contraseña debe tener al menos 8 caracteres.");

        if (string.IsNullOrWhiteSpace(request.Correo))
            errors.Add("El correo es obligatorio.");

        ThrowIfAny(errors, "Error de validación en registro.");
    }

    private static void ThrowIfAny(List<string> errors, string message = "Error de validación en login.")
    {
        if (errors.Any())
            throw new ValidationException(message, errors);
    }
}