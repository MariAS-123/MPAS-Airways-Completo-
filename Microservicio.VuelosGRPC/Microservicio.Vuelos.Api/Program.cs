using Microservicio.Vuelos.Api.Extensions;
using Microservicio.Vuelos.Api.GrpcServices;
using Microservicio.Vuelos.Api.Messaging;
using Microservicio.Vuelos.Api.Middleware;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Grpc.AspNetCore.Web;
/// <summary>
/// CAMBIOS MICROSERVICIO:
///   - Eliminado: using Microservicio.Vuelos.Api.Security
///   - Eliminado: builder.Services.AddSingleton&lt;ITokenBlacklistService, TokenBlacklistService&gt;()
///     TokenBlacklistService es EXCLUSIVO del MS Seguridad.
///     Este MS solo valida la firma del JWT — no gestiona logout ni blacklist.
/// </summary>
/// 
AppContext.SetSwitch("Npgsql.EnableLegacyTimestampBehavior", true);

var builder = WebApplication.CreateBuilder(args);

builder.WebHost.ConfigureKestrel(options =>
{
    options.ConfigureEndpointDefaults(listenOptions =>
    {
        listenOptions.Protocols = HttpProtocols.Http1AndHttp2;
    });
});

builder.Logging.ClearProviders();
builder.Logging.AddConsole();
builder.Logging.AddDebug();

// Controllers + gRPC
// Controllers + gRPC
builder.Services.AddControllers();
builder.Services.AddGrpc();
//builder.Services.AddGrpcWeb(); // ← agregar

// Versioning
builder.Services.AddApiVersioningDocumentation();

// JWT — solo validación de firma, sin blacklist
builder.Services.AddJwtAuthentication(builder.Configuration);

// CORS
builder.Services.AddCorsPolicy(builder.Configuration);

// Swagger
builder.Services.AddSwaggerDocumentation();

// DbContext + Repositories + DataServices + BusinessServices + HttpClients
builder.Services.AddProjectServices(builder.Configuration);

// Marketplace messaging (Reto 3) — capa adicional; REST/gRPC sin cambios
builder.Services.AddMarketplaceMessaging(builder.Configuration);

// Authorization
builder.Services.AddAuthorization();

var app = builder.Build();

app.MapGet("/", context =>
{
    context.Response.Redirect("/swagger");
    return Task.CompletedTask;
});

// Swagger
app.UseSwaggerDocumentation();

if (!app.Environment.IsDevelopment())
{
    app.UseHttpsRedirection();
}

// CORS
app.UseCorsPolicy();

// Authentication / Authorization
app.UseAuthentication();
app.UseAuthorization();

// Global exception handling
app.UseMiddleware<ExceptionHandlingMiddleware>();

// Controllers + gRPC (HTTP/2 requiere perfil Kestrel https)
// Controllers + gRPC
app.MapControllers();
app.UseGrpcWeb(); // ← agregar ANTES de MapGrpcService
app.MapGrpcService<VuelosGrpcService>().EnableGrpcWeb(); // ← agregar EnableGrpcWeb

app.Run();