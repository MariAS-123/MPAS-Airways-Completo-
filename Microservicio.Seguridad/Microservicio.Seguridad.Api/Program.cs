using Microservicio.Seguridad.Api.Extensions;
using Microservicio.Seguridad.Api.Middleware;
using Microservicio.Seguridad.Api.Security;

AppContext.SetSwitch("Npgsql.EnableLegacyTimestampBehavior", true);

var builder = WebApplication.CreateBuilder(args);

builder.Logging.ClearProviders();
builder.Logging.AddConsole();
builder.Logging.AddDebug();

// Controllers
builder.Services.AddControllers();
builder.Services.AddSingleton<ITokenBlacklistService, TokenBlacklistService>();

// Versioning
builder.Services.AddApiVersioningDocumentation();

// JWT Authentication
builder.Services.AddJwtAuthentication(builder.Configuration);

// CORS
builder.Services.AddCorsPolicy(builder.Configuration);

// Swagger
builder.Services.AddSwaggerDocumentation();

// Project services (DbContext + DataManagement + Business)
builder.Services.AddProjectServices(builder.Configuration);

// Authorization
builder.Services.AddAuthorization();

var app = builder.Build();

app.MapGet("/", context =>
{
    context.Response.Redirect("/swagger");
    return Task.CompletedTask;
});

app.UseSwaggerDocumentation();

if (!app.Environment.IsDevelopment())
    app.UseHttpsRedirection();

app.UseCorsPolicy();
app.UseAuthentication();
app.UseAuthorization();
app.UseMiddleware<ExceptionHandlingMiddleware>();
app.MapControllers();

app.Run();