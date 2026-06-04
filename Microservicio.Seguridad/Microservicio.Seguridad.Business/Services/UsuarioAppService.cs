using System.Security.Cryptography;
using System.Text;
using Microservicio.Seguridad.Business.DTOs.UsuarioApp;
using Microservicio.Seguridad.Business.Exceptions;
using Microservicio.Seguridad.Business.Interfaces;
using Microservicio.Seguridad.Business.Mappers;
using Microservicio.Seguridad.Business.Validators;
using Microservicio.Seguridad.DataAccess.Common;
using Microservicio.Seguridad.DataManagement.Interfaces;
using Microservicio.Seguridad.DataManagement.Models;

namespace Microservicio.Seguridad.Business.Services;

public class UsuarioAppService : IUsuarioAppService
{
    private readonly IUsuarioAppDataService _usuarioAppDataService;
    private readonly UsuarioAppValidator _validator;

    // IClienteDataService eliminado — IdCliente es referencia lógica al MS Clientes
    // su validación la hace el MS Clientes al registrar un cliente, no este MS
    public UsuarioAppService(IUsuarioAppDataService usuarioAppDataService)
    {
        _usuarioAppDataService = usuarioAppDataService;
        _validator = new UsuarioAppValidator();
    }

    public async Task<PagedResult<UsuarioAppResponseDto>> GetPagedAsync(UsuarioAppFilterDto filter)
    {
        _validator.ValidateFilter(filter);

        var filtro = UsuarioAppBusinessMapper.ToFiltroDataModel(filter);
        var result = await _usuarioAppDataService.GetPagedAsync(filtro);

        return PagedResult<UsuarioAppResponseDto>.Crear(
            UsuarioAppBusinessMapper.ToResponseDtoList(result.Items),
            result.TotalRegistros,
            result.PaginaActual,
            result.TamanoPagina);
    }

    public async Task<UsuarioAppResponseDto?> GetByIdAsync(int idUsuario)
    {
        if (idUsuario <= 0)
            throw new ValidationException("El id del usuario debe ser mayor que 0.");

        var data = await _usuarioAppDataService.GetByIdAsync(idUsuario);
        return data == null ? null : UsuarioAppBusinessMapper.ToResponseDto(data);
    }

    public async Task<UsuarioAppResponseDto> CreateAsync(UsuarioAppRequestDto request, string creadoPorUsuario)
    {
        if (string.IsNullOrWhiteSpace(creadoPorUsuario))
            throw new UnauthorizedBusinessException("No se pudo identificar el usuario creador.");

        _validator.ValidateRequest(request);

        var existeUsername = await _usuarioAppDataService.GetByUsernameAsync(request.Username);
        if (existeUsername != null)
            throw new BusinessException("Ya existe un usuario con el mismo username.");

        var passwordSalt = GenerateSalt();
        var passwordHash = GenerateHash(request.Password, passwordSalt);

        var dataModel = UsuarioAppBusinessMapper.ToDataModel(
            request, creadoPorUsuario, passwordHash, passwordSalt);

        var creado = await _usuarioAppDataService.CreateAsync(dataModel);
        return UsuarioAppBusinessMapper.ToResponseDto(creado);
    }

    public async Task<UsuarioAppResponseDto?> UpdateAsync(int idUsuario, UsuarioAppUpdateRequestDto request, string modificadoPorUsuario)
    {
        if (idUsuario <= 0)
            throw new ValidationException("El id del usuario debe ser mayor que 0.");

        if (string.IsNullOrWhiteSpace(modificadoPorUsuario))
            throw new UnauthorizedBusinessException("No se pudo identificar el usuario modificador.");

        _validator.ValidateUpdate(request);

        var actual = await _usuarioAppDataService.GetByIdAsync(idUsuario);
        if (actual == null)
            throw new NotFoundException("Usuario no encontrado.");

        byte[]? passwordHash = null;
        byte[]? passwordSalt = null;

        if (!string.IsNullOrWhiteSpace(request.Password))
        {
            passwordSalt = GenerateSalt();
            passwordHash = GenerateHash(request.Password, passwordSalt);
        }

        var dataModel = UsuarioAppBusinessMapper.ToDataModel(
            idUsuario, request, actual, modificadoPorUsuario, passwordHash, passwordSalt);

        var actualizado = await _usuarioAppDataService.UpdateAsync(dataModel);
        return actualizado == null ? null : UsuarioAppBusinessMapper.ToResponseDto(actualizado);
    }

    public async Task<bool> DeleteAsync(int idUsuario, string modificadoPorUsuario)
    {
        if (idUsuario <= 0)
            throw new ValidationException("El id del usuario debe ser mayor que 0.");

        if (string.IsNullOrWhiteSpace(modificadoPorUsuario))
            throw new UnauthorizedBusinessException("No se pudo identificar el usuario modificador.");

        var actual = await _usuarioAppDataService.GetByIdAsync(idUsuario);
        if (actual == null)
            throw new NotFoundException("Usuario no encontrado.");

        return await _usuarioAppDataService.DeleteAsync(idUsuario, modificadoPorUsuario);
    }

    private static byte[] GenerateSalt()
    {
        var salt = new byte[32];
        using var rng = RandomNumberGenerator.Create();
        rng.GetBytes(salt);
        return salt;
    }

    private static byte[] GenerateHash(string password, byte[] salt)
    {
        using var sha256 = SHA256.Create();
        var passwordBytes = Encoding.UTF8.GetBytes(password);
        var combined = new byte[passwordBytes.Length + salt.Length];
        Buffer.BlockCopy(passwordBytes, 0, combined, 0, passwordBytes.Length);
        Buffer.BlockCopy(salt, 0, combined, passwordBytes.Length, salt.Length);
        return sha256.ComputeHash(combined);
    }
}