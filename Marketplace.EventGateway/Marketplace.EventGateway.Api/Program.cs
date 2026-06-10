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

var aeropuertosBaseUrl = builder.Configuration["Integrations:Aeropuertos:BaseUrl"]
    ?? throw new InvalidOperationException("Integrations:Aeropuertos:BaseUrl es obligatorio.");

var clientesBaseUrl = builder.Configuration["Integrations:Clientes:BaseUrl"]
    ?? throw new InvalidOperationException("Integrations:Clientes:BaseUrl es obligatorio.");

var reservasBaseUrl = builder.Configuration["Integrations:Reservas:BaseUrl"]
    ?? throw new InvalidOperationException("Integrations:Reservas:BaseUrl es obligatorio.");

var geografiaBaseUrl = builder.Configuration["Integrations:Geografia:BaseUrl"]
    ?? throw new InvalidOperationException("Integrations:Geografia:BaseUrl es obligatorio.");

builder.Services.AddHttpClient<VuelosBookingClient>(client =>
{
    client.BaseAddress = new Uri(vuelosBaseUrl);
    client.Timeout = TimeSpan.FromSeconds(60);
});

builder.Services.AddHttpClient<AeropuertosCatalogClient>(client =>
{
    client.BaseAddress = new Uri(aeropuertosBaseUrl);
    client.Timeout = TimeSpan.FromSeconds(60);
});

builder.Services.AddHttpClient<SeguridadAuthClient>(client =>
{
    client.BaseAddress = new Uri(seguridadBaseUrl);
    client.Timeout = TimeSpan.FromSeconds(60);
});

builder.Services.AddHttpClient<ClientesPortalClient>(client =>
{
    client.BaseAddress = new Uri(clientesBaseUrl);
    client.Timeout = TimeSpan.FromSeconds(60);
});

builder.Services.AddHttpClient<ClientesPasajerosClient>(client =>
{
    client.BaseAddress = new Uri(clientesBaseUrl);
    client.Timeout = TimeSpan.FromSeconds(60);
});

builder.Services.AddHttpClient<ReservasBookingClient>(client =>
{
    client.BaseAddress = new Uri(reservasBaseUrl);
    client.Timeout = TimeSpan.FromSeconds(120);
});

builder.Services.AddHttpClient<GeografiaCatalogClient>(client =>
{
    client.BaseAddress = new Uri(geografiaBaseUrl);
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
    "Gateway Integrations → Vuelos={VuelosUrl} Aeropuertos={AeropuertosUrl} Clientes={ClientesUrl} Geografia={GeografiaUrl} Seguridad={SeguridadUrl}",
    vuelosBaseUrl,
    aeropuertosBaseUrl,
    clientesBaseUrl,
    geografiaBaseUrl,
    seguridadBaseUrl);

app.MapGet("/", () => Results.Redirect("/graphql"));

app.MapGraphQL("/graphql");

app.Run();
