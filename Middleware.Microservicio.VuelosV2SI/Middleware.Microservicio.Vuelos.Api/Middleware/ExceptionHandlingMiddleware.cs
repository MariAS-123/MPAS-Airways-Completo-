using System.Text.Json;
using Middleware.Vuelos.Business.Exceptions;

namespace Middleware.Microservicio.Vuelos.Api.Middleware;

public class ExceptionHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionHandlingMiddleware> _logger;

    public ExceptionHandlingMiddleware(RequestDelegate next, ILogger<ExceptionHandlingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (UnauthorizedBusinessException ex)
        {
            await WriteErrorAsync(context, StatusCodes.Status401Unauthorized, ex.Message);
        }
        catch (BusinessException ex)
        {
            await WriteErrorAsync(context, StatusCodes.Status409Conflict, ex.Message);
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Operacion invalida. TraceId={TraceId}", context.TraceIdentifier);
            await WriteErrorAsync(context, StatusCodes.Status409Conflict, ex.Message);
        }
        catch (UnauthorizedAccessException ex)
        {
            await WriteErrorAsync(context, StatusCodes.Status401Unauthorized, ex.Message);
        }
    }

    private static async Task WriteErrorAsync(HttpContext context, int statusCode, string message)
    {
        if (context.Response.HasStarted)
            return;

        context.Response.Clear();
        context.Response.StatusCode = statusCode;
        context.Response.ContentType = "application/json";

        var payload = new
        {
            success = false,
            message,
        };

        await context.Response.WriteAsync(JsonSerializer.Serialize(payload));
    }
}
