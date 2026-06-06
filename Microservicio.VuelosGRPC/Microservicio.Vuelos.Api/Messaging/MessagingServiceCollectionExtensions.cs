using Microservicio.Vuelos.Api.Messaging.Consumers;
using Microservicio.Vuelos.Api.Messaging.Handlers;
using Microservicio.Vuelos.Api.Messaging.Options;
using Microservicio.Vuelos.Api.Messaging.Publishing;
using Microservicio.Vuelos.Api.Messaging.Services;

namespace Microservicio.Vuelos.Api.Messaging;

public static class MessagingServiceCollectionExtensions
{
    public static IServiceCollection AddMarketplaceMessaging(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.Configure<RabbitMqOptions>(configuration.GetSection(RabbitMqOptions.SectionName));
        services.AddMemoryCache();

        services.AddSingleton<IMarketplaceEventPublisher, RabbitMqEventPublisher>();
        services.AddSingleton<IPreReservaAsientoStore, PreReservaAsientoStore>();
        services.AddScoped<AsientoDisponibilidadOperations>();
        services.AddScoped<VueloSeleccionadoHandler>();
        services.AddScoped<BoletoEmitidoHandler>();

        var enabled = configuration.GetValue<bool>($"{RabbitMqOptions.SectionName}:Enabled");
        if (enabled)
        {
            services.AddHostedService<VueloSeleccionadoConsumer>();
            services.AddHostedService<BoletoEmitidoConsumer>();
        }

        return services;
    }
}
