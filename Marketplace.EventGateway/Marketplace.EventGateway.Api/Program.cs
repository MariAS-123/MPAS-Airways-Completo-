using Marketplace.EventGateway.Api.GraphQL;
using Marketplace.EventGateway.Api.Messaging.Consumers;
using Marketplace.EventGateway.Api.Messaging.Options;
using Marketplace.EventGateway.Api.Messaging.Publishing;
using Marketplace.EventGateway.Api.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Logging.ClearProviders();
builder.Logging.AddConsole();
builder.Logging.AddDebug();

builder.Services.Configure<RabbitMqOptions>(builder.Configuration.GetSection(RabbitMqOptions.SectionName));

var vuelosBaseUrl = builder.Configuration["Integrations:Vuelos:BaseUrl"]
    ?? throw new InvalidOperationException("Integrations:Vuelos:BaseUrl es obligatorio.");

var seguridadBaseUrl = builder.Configuration["Integrations:Seguridad:BaseUrl"]
    ?? throw new InvalidOperationException("Integrations:Seguridad:BaseUrl es obligatorio.");

builder.Services.AddHttpClient<VuelosBookingClient>(client =>
{
    client.BaseAddress = new Uri(vuelosBaseUrl);
    client.Timeout = TimeSpan.FromSeconds(60);
});

builder.Services.AddHttpClient<SeguridadAuthClient>(client =>
{
    client.BaseAddress = new Uri(seguridadBaseUrl);
    client.Timeout = TimeSpan.FromSeconds(60);
});

builder.Services.AddHttpContextAccessor();
builder.Services.AddSingleton<IUserTokenAccessor, HttpUserTokenAccessor>();
builder.Services.AddSingleton<SagaStateStore>();
builder.Services.AddSingleton<IMarketplaceEventPublisher, RabbitMqEventPublisher>();
builder.Services.AddScoped<MarketplaceMutationService>();

var rabbitEnabled = builder.Configuration.GetValue<bool>($"{RabbitMqOptions.SectionName}:Enabled");
if (rabbitEnabled)
{
    builder.Services.AddHostedService<GatewayEventsConsumer>();
}

builder.Services
    .AddGraphQLServer()
    .AddQueryType(d => d.Name("Query"))
    .AddMutationType(d => d.Name("Mutation"))
    .AddTypeExtension<MarketplaceQuery>()
    .AddTypeExtension<MarketplaceMutation>()
    .ModifyRequestOptions(options =>
    {
        options.IncludeExceptionDetails = builder.Environment.IsDevelopment();
    });

var app = builder.Build();

app.Logger.LogInformation(
    "Gateway Integrations → Vuelos={VuelosUrl} Seguridad={SeguridadUrl}",
    vuelosBaseUrl,
    seguridadBaseUrl);

app.MapGet("/", () => Results.Redirect("/graphql"));

app.MapGraphQL("/graphql");

app.Run();
