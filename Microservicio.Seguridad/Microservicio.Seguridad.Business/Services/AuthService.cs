using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using Microservicio.Seguridad.Business.DTOs.Auth;
using Microservicio.Seguridad.Business.DTOs.UsuarioApp;
using Microservicio.Seguridad.Business.DTOs.UsuarioRol;
using Microservicio.Seguridad.Business.Exceptions;
using Microservicio.Seguridad.Business.Interfaces;
using Microservicio.Seguridad.Business.Mappers;
using Microservicio.Seguridad.Business.Validators;
using Microservicio.Seguridad.DataManagement.Interfaces;

namespace Microservicio.Seguridad.Business.Services;

public class AuthService : IAuthService
{
    private readonly IUsuarioAppDataService _usuarioDataService;
    private readonly IUsuarioAppService _usuarioAppService;
    private readonly IUsuarioRolService _usuarioRolService;
    private readonly IRolDataService _rolDataService;
    private readonly IUnitOfWork _unitOfWork;
    private readonly AuthValidator _validator;
    private readonly IConfiguration _configuration;

    // IClienteService eliminado — el cliente lo crea el MS Clientes
    // Este MS solo crea el UsuarioApp y asigna el rol
    public AuthService(
        IUsuarioAppDataService usuarioDataService,
        IUsuarioAppService usuarioAppService,
        IUsuarioRolService usuarioRolService,
        IRolDataService rolDataService,
        IUnitOfWork unitOfWork,
        IConfiguration configuration)
    {
        _usuarioDataService = usuarioDataService;
        _usuarioAppService = usuarioAppService;
        _usuarioRolService = usuarioRolService;
        _rolDataService = rolDataService;
        _unitOfWork = unitOfWork;
        _validator = new AuthValidator();
        _configuration = configuration;
    }

    public async Task<LoginResponse> LoginAsync(LoginRequest request)
    {
        _validator.ValidateLogin(request);

        var user = await _usuarioDataService.GetByUsernameAsync(request.Username);

        if (user == null)
            throw new UnauthorizedBusinessException("Usuario o contraseña incorrectos.");

        if (!user.Activo || user.EsEliminado || user.EstadoUsuario != "ACT")
            throw new UnauthorizedBusinessException("El usuario no está activo.");

        if (!IsPasswordValid(request.Password, user.PasswordHash, user.PasswordSalt))
            throw new UnauthorizedBusinessException("Usuario o contraseña incorrectos.");

        var expirationMinutes = _configuration.GetValue<int>("JwtSettings:ExpirationMinutes");
        var expiracion = DateTime.UtcNow.AddMinutes(expirationMinutes);

        var token = GenerateJwtToken(user.Username, user.Roles, user.IdCliente, expiracion);

        return AuthBusinessMapper.ToLoginResponse(user, token, expiracion);
    }

    public async Task<RegisterClienteResponse> RegisterClienteAsync(RegisterClienteRequest request)
    {
        Console.WriteLine($"[DEBUG] RegisterCliente. Username={request.Username} IdCliente={request.IdCliente}");

        _validator.ValidateRegisterCliente(request);

        // El MS Clientes ya creó el cliente y llama a este endpoint
        // pasando el IdCliente en el request
        // Este MS solo crea el UsuarioApp y asigna el rol CLIENTE
        var rolCliente = await _rolDataService.GetByNombreAsync("CLIENTE");
        if (rolCliente == null || rolCliente.EsEliminado || !rolCliente.Activo || rolCliente.EstadoRol != "ACT")
            throw new BusinessException("No se encontró el rol CLIENTE activo.");

        return await _unitOfWork.ExecuteInTransactionAsync(async () =>
        {
            var usuario = await _usuarioAppService.CreateAsync(new UsuarioAppRequestDto
            {
                IdCliente = request.IdCliente, // será asignado por el MS Clientes tras recibir el IdUsuario
                Username = request.Username,
                Correo = request.Correo,
                Password = request.Password
            }, "SELF_REGISTER");

            await _usuarioRolService.CreateAsync(new UsuarioRolRequestDto
            {
                IdUsuario = usuario.IdUsuario,
                IdRol = rolCliente.IdRol
            }, "SELF_REGISTER");

            return new RegisterClienteResponse
            {
                IdCliente = request.IdCliente ?? 0,// el MS Clientes tiene el IdCliente real
                IdUsuario = usuario.IdUsuario,
                Username = usuario.Username,
                RolAsignado = "CLIENTE"
            };
        });
    }

    public Task LogoutAsync(string token, string ejecutadoPorUsuario)
    {
        if (string.IsNullOrWhiteSpace(token))
            throw new UnauthorizedBusinessException("No se recibió un token válido para cerrar sesión.");

        if (string.IsNullOrWhiteSpace(ejecutadoPorUsuario))
            throw new UnauthorizedBusinessException("No se pudo identificar al usuario autenticado.");

        return Task.CompletedTask;
    }

    private string GenerateJwtToken(string username, List<string> roles, int? idCliente, DateTime expiracion)
    {
        // Clave según documento de arquitectura: JwtSettings:Secret
        var secret = _configuration["JwtSettings:Secret"];
        var issuer = _configuration["JwtSettings:Issuer"];
        var audience = _configuration["JwtSettings:Audience"];

        if (string.IsNullOrWhiteSpace(secret))
            throw new BusinessException("No se encontró JwtSettings:Secret en la configuración.");

        if (string.IsNullOrWhiteSpace(issuer))
            throw new BusinessException("No se encontró JwtSettings:Issuer en la configuración.");

        if (string.IsNullOrWhiteSpace(audience))
            throw new BusinessException("No se encontró JwtSettings:Audience en la configuración.");

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secret));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var claims = new List<Claim>
        {
            new(ClaimTypes.Name, username),
            new("username", username)
        };

        if (idCliente.HasValue)
            claims.Add(new Claim("id_cliente", idCliente.Value.ToString()));

        foreach (var role in roles.Distinct())
            claims.Add(new Claim(ClaimTypes.Role, role));

        var token = new JwtSecurityToken(
            issuer: issuer,
            audience: audience,
            claims: claims,
            expires: expiracion,
            signingCredentials: credentials);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    private static bool IsPasswordValid(string password, string storedHash, string? storedSalt)
    {
        if (string.Equals(storedHash, password, StringComparison.Ordinal))
            return true;

        if (string.IsNullOrWhiteSpace(storedHash) || string.IsNullOrWhiteSpace(storedSalt))
            return false;

        try
        {
            var hashBytes = Convert.FromBase64String(storedHash);
            var saltBytes = Convert.FromBase64String(storedSalt);

            using var sha256 = SHA256.Create();
            var passwordBytes = Encoding.UTF8.GetBytes(password);
            var combined = new byte[passwordBytes.Length + saltBytes.Length];
            Buffer.BlockCopy(passwordBytes, 0, combined, 0, passwordBytes.Length);
            Buffer.BlockCopy(saltBytes, 0, combined, passwordBytes.Length, saltBytes.Length);
            var computed = sha256.ComputeHash(combined);

            return CryptographicOperations.FixedTimeEquals(computed, hashBytes);
        }
        catch (FormatException)
        {
            return false;
        }
    }
}