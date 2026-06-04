using Grpc.Net.Client.Web;
using Microservicio.ReservasF.Api.Integrations;
using Microservicio.ReservasF.Business.Integrations.Interfaces;
using Microservicio.ReservasF.DataAccess.Context;
using Microservicio.Vuelos.Grpc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection.Extensions;
using System.Net.Http;
using System.Net.Security;
using System.Reflection;

namespace Microservicio.ReservasF.Api.Extensions;

public static class ServiceCollectionExtensions
{
    private const string ConnectionStringName = "MicroservicioReservasFDb";

    public static IServiceCollection AddProjectServices(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        RegisterDbContext(services, configuration);

        RegisterRepositoriesByConvention(services, "Microservicio.ReservasF.DataAccess");
        RegisterUnitOfWork(services, "Microservicio.ReservasF.DataManagement");
        RegisterServicesByConvention(services, "Microservicio.ReservasF.DataManagement");
        RegisterServicesByConvention(services, "Microservicio.ReservasF.Business");

        RegisterIntegrations(services, configuration);
        return services;
    }

    private static void RegisterDbContext(
        IServiceCollection services,
        IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString(ConnectionStringName);

        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new InvalidOperationException(
                $"La cadena de conexión '{ConnectionStringName}' no está configurada.");
        }
        /*
        services.AddDbContext<SistemaReservasDbContext>(options =>
        {
            options.UseNpgsql(connectionString, npgsqlOptions =>
            {
                npgsqlOptions.EnableRetryOnFailure(
                    maxRetryCount: 5,
                    maxRetryDelay: TimeSpan.FromSeconds(10),
                    errorCodesToAdd: null);
            });
        });*/
        services.AddDbContext<SistemaReservasDbContext>(options =>
        {
            options.UseNpgsql(connectionString);
        });

    }

    private static void RegisterIntegrations(
        IServiceCollection services,
        IConfiguration configuration)
    {
        var clientesUrl = configuration["Integrations:Clientes:BaseUrl"]!;
        var vuelosGrpcUrl = configuration["Integrations:Vuelos:GrpcUrl"]!;

        services.AddHttpClient<IClienteIntegrationService, ClienteIntegrationService>(client =>
        {
            client.BaseAddress = new Uri(clientesUrl);
        });
        services.AddGrpcClient<VuelosGrpc.VuelosGrpcClient>(options =>
        {
            options.Address = new Uri(vuelosGrpcUrl);
        })
        .ConfigurePrimaryHttpMessageHandler(() => new GrpcWebHandler(new HttpClientHandler
        {
            ServerCertificateCustomValidationCallback =
                HttpClientHandler.DangerousAcceptAnyServerCertificateValidator
        })
        {
            GrpcWebMode = GrpcWebMode.GrpcWeb
        });

        services.AddScoped<IVueloIntegrationService, VuelosGrpcIntegrationService>();
    }

    private static void RegisterRepositoriesByConvention(
        IServiceCollection services,
        string assemblyName)
    {
        var assembly = LoadAssembly(assemblyName);

        var interfaces = assembly.GetTypes()
            .Where(t =>
                t.IsInterface &&
                t.IsPublic &&
                t.Namespace is not null &&
                t.Namespace.Contains(".Repositories.Interfaces", StringComparison.Ordinal))
            .ToList();

        var implementations = assembly.GetTypes()
            .Where(t =>
                t.IsClass &&
                !t.IsAbstract &&
                t.IsPublic &&
                t.Namespace is not null &&
                t.Namespace.Contains(".Repositories", StringComparison.Ordinal) &&
                !t.Namespace.Contains(".Interfaces", StringComparison.Ordinal))
            .ToList();

        RegisterByConvention(services, interfaces, implementations);
    }

    private static void RegisterUnitOfWork(
        IServiceCollection services,
        string assemblyName)
    {
        var assembly = LoadAssembly(assemblyName);

        var interfaceType = assembly.GetTypes()
            .FirstOrDefault(t =>
                t.IsInterface &&
                t.Name.Equals("IUnitOfWork", StringComparison.Ordinal));

        var implementationType = assembly.GetTypes()
            .FirstOrDefault(t =>
                t.IsClass &&
                !t.IsAbstract &&
                t.Name.Equals("UnitOfWork", StringComparison.Ordinal));

        if (interfaceType is null)
        {
            throw new InvalidOperationException(
                $"No se encontró la interfaz IUnitOfWork en el ensamblado '{assemblyName}'.");
        }

        if (implementationType is null)
        {
            throw new InvalidOperationException(
                $"No se encontró la clase UnitOfWork en el ensamblado '{assemblyName}'.");
        }

        services.TryAddScoped(interfaceType, implementationType);
    }

    private static void RegisterServicesByConvention(
        IServiceCollection services,
        string assemblyName)
    {
        var assembly = LoadAssembly(assemblyName);

        var interfaces = assembly.GetTypes()
            .Where(t =>
                t.IsInterface &&
                t.IsPublic &&
                t.Namespace is not null &&
                t.Namespace.Contains(".Interfaces", StringComparison.Ordinal) &&
                !t.Name.Equals("IUnitOfWork", StringComparison.Ordinal) &&
                !t.Name.Equals("IVueloIntegrationService", StringComparison.Ordinal)) // ← agregar esta línea
            .ToList();

        // ... resto igual

        var implementations = assembly.GetTypes()
            .Where(t =>
                t.IsClass &&
                !t.IsAbstract &&
                t.IsPublic &&
                t.Namespace is not null &&
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
            var expectedImplementationName = interfaceType.Name.StartsWith("I", StringComparison.Ordinal)
                ? interfaceType.Name[1..]
                : interfaceType.Name;

            var implementationType = implementations.FirstOrDefault(t =>
                t.Name.Equals(expectedImplementationName, StringComparison.Ordinal));

            if (implementationType is null)
            {
                continue;
            }

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
                $"No se pudo cargar el ensamblado '{assemblyName}'. Verifica que el proyecto esté referenciado correctamente.",
                ex);
        }
    }
}