using Microservicio.Clientes.Api.Messaging.Consumers;
using Microservicio.Clientes.Api.Messaging.Handlers;
using Microservicio.Clientes.Api.Messaging.Options;
using Microservicio.Clientes.Api.Messaging.Publishing;

namespace Microservicio.Clientes.Api.Messaging;

public static class MessagingServiceCollectionExtensions
{
    public static IServiceCollection AddMarketplaceMessaging(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.Configure<RabbitMqOptions>(configuration.GetSection(RabbitMqOptions.SectionName));

        services.AddSingleton<IMarketplaceEventPublisher, RabbitMqEventPublisher>();
        services.AddScoped<PasajerosRegistradosHandler>();

        var enabled = configuration.GetValue<bool>($"{RabbitMqOptions.SectionName}:Enabled");
        if (enabled)
        {
            services.AddHostedService<PasajerosRegistradosConsumer>();
        }

        return services;
    }
}
