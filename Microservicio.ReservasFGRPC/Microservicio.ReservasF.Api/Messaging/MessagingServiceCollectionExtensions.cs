using Microservicio.ReservasF.Api.Messaging.Consumers;
using Microservicio.ReservasF.Api.Messaging.Handlers;
using Microservicio.ReservasF.Api.Messaging.Options;
using Microservicio.ReservasF.Api.Messaging.Publishing;

namespace Microservicio.ReservasF.Api.Messaging;

public static class MessagingServiceCollectionExtensions
{
    public static IServiceCollection AddMarketplaceMessaging(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.Configure<RabbitMqOptions>(configuration.GetSection(RabbitMqOptions.SectionName));

        services.AddSingleton<IMarketplaceEventPublisher, RabbitMqEventPublisher>();
        services.AddScoped<ReservaSolicitadaHandler>();

        var enabled = configuration.GetValue<bool>($"{RabbitMqOptions.SectionName}:Enabled");
        if (enabled)
        {
            services.AddHostedService<ReservaSolicitadaConsumer>();
        }

        return services;
    }
}
