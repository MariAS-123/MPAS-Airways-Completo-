using Microservicio.Aeropuertos.Api.Extensions;
using Microservicio.Aeropuertos.Api.Middleware;
AppContext.SetSwitch("Npgsql.EnableLegacyTimestampBehavior", true);

var builder = WebApplication.CreateBuilder(args);

// Logging
builder.Logging.ClearProviders();

builder.Logging.AddConsole();

builder.Logging.AddDebug();

// Controllers
builder.Services.AddControllers();

// API Versioning
builder.Services.AddApiVersioningDocumentation();

// JWT Authentication
builder.Services.AddJwtAuthentication(
    builder.Configuration);

// CORS
builder.Services.AddCorsPolicy(
    builder.Configuration);

// Swagger
builder.Services.AddSwaggerDocumentation();

// Project Services
builder.Services.AddProjectServices(
    builder.Configuration);

// Authorization
builder.Services.AddAuthorization();

var app = builder.Build();

// Redirect raíz → swagger
app.MapGet("/", context =>
{
    context.Response.Redirect("/swagger");

    return Task.CompletedTask;
});

// Swagger
app.UseSwaggerDocumentation();

// Global Exception Middleware
app.UseMiddleware<ExceptionHandlingMiddleware>();

// HTTPS
if (!app.Environment.IsDevelopment())
{
    app.UseHttpsRedirection();
}

// CORS
app.UseCorsPolicy();

// Authentication
app.UseAuthentication();

// Authorization
app.UseAuthorization();

// Controllers
app.MapControllers();

app.Run();