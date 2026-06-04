using Microservicio.Geografia.Api.Extensions;
using Microservicio.Geografia.Api.Middleware;
AppContext.SetSwitch("Npgsql.EnableLegacyTimestampBehavior", true);
var builder = WebApplication.CreateBuilder(args);

// ============================================================
// LOGGING
// ============================================================
builder.Logging.ClearProviders();

builder.Logging.AddConsole();

builder.Logging.AddDebug();

// ============================================================
// CONTROLLERS
// ============================================================
builder.Services.AddControllers();

// ============================================================
// API VERSIONING
// ============================================================
builder.Services.AddApiVersioningDocumentation();

// ============================================================
// JWT AUTHENTICATION
// SOLO VALIDACIÓN JWT
// ============================================================
builder.Services.AddJwtAuthentication(
    builder.Configuration);

// ============================================================
// CORS
// ============================================================
builder.Services.AddCorsPolicy(
    builder.Configuration);

// ============================================================
// SWAGGER
// ============================================================
builder.Services.AddSwaggerDocumentation();

// ============================================================
// PROJECT SERVICES
// DbContext + Repositories + Business
// ============================================================
builder.Services.AddProjectServices(
    builder.Configuration);

// ============================================================
// AUTHORIZATION
// ============================================================
builder.Services.AddAuthorization();

var app = builder.Build();

// ============================================================
// REDIRECT ROOT TO SWAGGER
// ============================================================
app.MapGet(
    "/",
    context =>
    {
        context.Response.Redirect("/swagger");

        return Task.CompletedTask;
    });

// ============================================================
// SWAGGER
// ============================================================
app.UseSwaggerDocumentation();

// ============================================================
// HTTPS
// ============================================================
if (!app.Environment.IsDevelopment())
{
    app.UseHttpsRedirection();
}

// ============================================================
// CORS
// ============================================================
app.UseCorsPolicy();

// ============================================================
// AUTHENTICATION / AUTHORIZATION
// ============================================================
app.UseAuthentication();

app.UseAuthorization();

// ============================================================
// GLOBAL EXCEPTION HANDLING
// ============================================================
app.UseMiddleware<ExceptionHandlingMiddleware>();

// ============================================================
// CONTROLLERS
// ============================================================
app.MapControllers();

app.Run();