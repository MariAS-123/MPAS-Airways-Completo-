using Microservicio.Seguridad.Business.DTOs.Internal;
using Microservicio.Seguridad.Business.Interfaces;
using Microservicio.Seguridad.DataManagement.Interfaces;
using Microservicio.Seguridad.DataManagement.Models;
using Microsoft.Extensions.Logging;
using System.Security.Cryptography;
using System.Text;

namespace Microservicio.Seguridad.Business.Services;

/// <summary>
/// Servicio interno de Seguridad invocado exclusivamente por el Bus de Integración.
/// Sin transacción explícita para compatibilidad con Supabase connection pooling.
/// La idempotencia protege contra duplicados en caso de reintento.
/// </summary>
public class InternalSeguridadService : IInternalSeguridadService
{
    private readonly IUsuarioAppDataService _usuarioDataService;
    private readonly IUsuarioRolDataService _usuarioRolDataService;
    private readonly IRolDataService _rolDataService;
    private readonly ILogger<InternalSeguridadService> _logger;

    private const string RolCliente = "CLIENTE";

    public InternalSeguridadService(
        IUsuarioAppDataService usuarioDataService,
        IUsuarioRolDataService usuarioRolDataService,
        IRolDataService rolDataService,
        ILogger<InternalSeguridadService> logger)
    {
        _usuarioDataService = usuarioDataService;
        _usuarioRolDataService = usuarioRolDataService;
        _rolDataService = rolDataService;
        _logger = logger;
    }

    public async Task<CreateUserForClientResponse> CreateUserForClientAsync(
        CreateUserForClientRequest request)
    {
        var correlationId = request.CorrelationId ?? Guid.NewGuid().ToString();

        _logger.LogInformation(
            "[Seguridad][Internal] CreateUserForClient iniciado. " +
            "IdCliente={IdCliente} Username={Username} CorrelationId={CorrelationId}",
            request.IdCliente, request.Username, correlationId);

        // ── 1. Idempotencia ──────────────────────────────────────────────────────
        var usuarioExistente = await _usuarioDataService
            .GetByUsernameAsync(request.Username);

        if (usuarioExistente is not null)
        {
            if (usuarioExistente.IdCliente == request.IdCliente)
            {
                _logger.LogWarning(
                    "[Seguridad][Internal] Reintento idempotente. " +
                    "Username={Username} CorrelationId={CorrelationId}",
                    request.Username, correlationId);

                return new CreateUserForClientResponse
                {
                    IdUsuario = usuarioExistente.IdUsuario,
                    UsuarioGuid = usuarioExistente.UsuarioGuid,
                    IdCliente = usuarioExistente.IdCliente!.Value,
                    Username = usuarioExistente.Username,
                    RolAsignado = RolCliente,
                    CorrelationId = correlationId
                };
            }

            throw new InvalidOperationException(
                $"El username '{request.Username}' ya está en uso por otro usuario.");
        }

        // ── 2. Verificar rol CLIENTE activo ──────────────────────────────────────
        var rol = await _rolDataService.GetByNombreAsync(RolCliente);
        if (rol is null || rol.EsEliminado || !rol.Activo || rol.EstadoRol != "ACT")
            throw new InvalidOperationException(
                $"El rol '{RolCliente}' no existe o no está activo.");

        // ── 3. Generar salt y hash ────────────────────────────────────────────────
        var salt = GenerateSalt();
        var hash = GenerateHash(request.Password, salt);

        // ── 4. Crear usuario ─────────────────────────────────────────────────────
        var nuevoUsuario = await _usuarioDataService.CreateAsync(new UsuarioAppDataModel
        {
            IdCliente = request.IdCliente,
            Username = request.Username,
            Correo = request.Correo,
            PasswordHash = hash,
            PasswordSalt = salt,
            CreadoPorUsuario = "BUS_INTEGRACION"
        });

        // ── 5. Asignar rol CLIENTE ───────────────────────────────────────────────
        await _usuarioRolDataService.CreateAsync(new UsuarioRolDataModel
        {
            IdUsuario = nuevoUsuario.IdUsuario,
            IdRol = rol.IdRol,
            CreadoPorUsuario = "BUS_INTEGRACION"
        });

        _logger.LogInformation(
            "[Seguridad][Internal] CreateUserForClient completado. " +
            "IdUsuario={IdUsuario} IdCliente={IdCliente} CorrelationId={CorrelationId}",
            nuevoUsuario.IdUsuario, request.IdCliente, correlationId);

        return new CreateUserForClientResponse
        {
            IdUsuario = nuevoUsuario.IdUsuario,
            UsuarioGuid = nuevoUsuario.UsuarioGuid,
            IdCliente = request.IdCliente,
            Username = nuevoUsuario.Username,
            RolAsignado = RolCliente,
            CorrelationId = correlationId
        };
    }

    // ── Hash idéntico al de AuthService ─────────────────────────────────────────

    private static string GenerateSalt()
    {
        var saltBytes = new byte[32];
        using var rng = RandomNumberGenerator.Create();
        rng.GetBytes(saltBytes);
        return Convert.ToBase64String(saltBytes);
    }

    private static string GenerateHash(string password, string salt)
    {
        var passwordBytes = Encoding.UTF8.GetBytes(password);
        var saltBytes = Convert.FromBase64String(salt);
        var combined = new byte[passwordBytes.Length + saltBytes.Length];
        Buffer.BlockCopy(passwordBytes, 0, combined, 0, passwordBytes.Length);
        Buffer.BlockCopy(saltBytes, 0, combined, passwordBytes.Length, saltBytes.Length);
        using var sha256 = SHA256.Create();
        return Convert.ToBase64String(sha256.ComputeHash(combined));
    }
}