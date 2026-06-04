using Microservicio.Seguridad.Business.Interfaces;
using Microservicio.Seguridad.Business.Services;
using Microservicio.Seguridad.DataAccess.Context;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection.Extensions;
using System.Reflection;

namespace Microservicio.Seguridad.Api.Extensions;

public static class ServiceCollectionExtensions
{
    private const string ConnectionStringName = "MicroservicioVuelosDb";

    public static IServiceCollection AddProjectServices(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        RegisterDbContext(services, configuration);
        RegisterRepositoriesByConvention(services, "Microservicio.Seguridad.DataAccess");
        RegisterUnitOfWork(services, "Microservicio.Seguridad.DataManagement");
        RegisterServicesByConvention(services, "Microservicio.Seguridad.DataManagement");
        RegisterServicesByConvention(services, "Microservicio.Seguridad.Business");
        //services.AddScoped<IInternalSeguridadService, InternalSeguridadService>();
        return services;
    }

    private static void RegisterDbContext(IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString(ConnectionStringName);

        if (string.IsNullOrWhiteSpace(connectionString))
            throw new InvalidOperationException(
                $"La cadena de conexión '{ConnectionStringName}' no está configurada.");

        /*   services.AddDbContext<SistemaVuelosSeguridadDBContext>(options =>
           {
               options.UseNpgsql(connectionString, npgsqlOptions =>
               {
                   npgsqlOptions.EnableRetryOnFailure(
                       maxRetryCount: 5,
                       maxRetryDelay: TimeSpan.FromSeconds(10),
                       errorCodesToAdd: null);
               });
           });*/

        services.AddDbContext<SistemaVuelosSeguridadDBContext>(options =>
        {
            options.UseNpgsql(connectionString);
        });
    }

    private static void RegisterRepositoriesByConvention(IServiceCollection services, string assemblyName)
    {
        var assembly = LoadAssembly(assemblyName);

        var interfaces = assembly.GetTypes()
            .Where(t => t.IsInterface && t.IsPublic && t.Namespace is not null &&
                        t.Namespace.Contains(".Repositories.Interfaces", StringComparison.Ordinal))
            .ToList();

        var implementations = assembly.GetTypes()
            .Where(t => t.IsClass && !t.IsAbstract && t.IsPublic && t.Namespace is not null &&
                        t.Namespace.Contains(".Repositories", StringComparison.Ordinal) &&
                        !t.Namespace.Contains(".Interfaces", StringComparison.Ordinal))
            .ToList();

        RegisterByConvention(services, interfaces, implementations);
    }

    private static void RegisterUnitOfWork(IServiceCollection services, string assemblyName)
    {
        var assembly = LoadAssembly(assemblyName);

        var interfaceType = assembly.GetTypes()
            .FirstOrDefault(t => t.IsInterface && t.Name.Equals("IUnitOfWork", StringComparison.Ordinal));

        var implementationType = assembly.GetTypes()
            .FirstOrDefault(t => t.IsClass && !t.IsAbstract &&
                                 t.Name.Equals("UnitOfWork", StringComparison.Ordinal));

        if (interfaceType is null)
            throw new InvalidOperationException($"No se encontró IUnitOfWork en '{assemblyName}'.");

        if (implementationType is null)
            throw new InvalidOperationException($"No se encontró UnitOfWork en '{assemblyName}'.");

        services.TryAddScoped(interfaceType, implementationType);
    }

    private static void RegisterServicesByConvention(IServiceCollection services, string assemblyName)
    {
        var assembly = LoadAssembly(assemblyName);

        var interfaces = assembly.GetTypes()
            .Where(t => t.IsInterface && t.IsPublic && t.Namespace is not null &&
                        t.Namespace.Contains(".Interfaces", StringComparison.Ordinal) &&
                        !t.Name.Equals("IUnitOfWork", StringComparison.Ordinal))
            .ToList();

        var implementations = assembly.GetTypes()
            .Where(t => t.IsClass && !t.IsAbstract && t.IsPublic && t.Namespace is not null &&
                        t.Namespace.Contains(".Services", StringComparison.Ordinal))
            .ToList();

        RegisterByConvention(services, interfaces, implementations);
    }

    private static void RegisterByConvention(
        IServiceCollection services,
        List<Type> interfaces,
        List<Type> implementations)
    {
        foreach (var interfaceType in interfaces)
        {
            var expectedName = interfaceType.Name.StartsWith("I", StringComparison.Ordinal)
                ? interfaceType.Name[1..]
                : interfaceType.Name;

            var implementationType = implementations.FirstOrDefault(t =>
                t.Name.Equals(expectedName, StringComparison.Ordinal));

            if (implementationType is null) continue;

            services.TryAddScoped(interfaceType, implementationType);
        }
    }

    private static Assembly LoadAssembly(string assemblyName)
    {
        try
        {
            return Assembly.Load(assemblyName);
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException(
                $"No se pudo cargar el ensamblado '{assemblyName}'.", ex);
        }
    }
}