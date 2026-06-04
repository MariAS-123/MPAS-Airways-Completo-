using Microservicio.Clientes.Api.Extensions;
using Microservicio.Clientes.Api.Middleware;
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
builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.PropertyNameCaseInsensitive = true;
    });

// ============================================================
// API VERSIONING
// ============================================================
builder.Services.AddApiVersioningDocumentation();

// ============================================================
// JWT AUTHENTICATION — Solo validación, sin generación
// ============================================================
builder.Services.AddJwtAuthentication(builder.Configuration);

// ============================================================
// CORS
// ============================================================
builder.Services.AddCorsPolicy(builder.Configuration);

// ============================================================
// SWAGGER
// ============================================================
builder.Services.AddSwaggerDocumentation();

// ============================================================
// PROJECT SERVICES
// DbContext + Repositories + DataManagement + Business + Integrations
// ============================================================
builder.Services.AddProjectServices(builder.Configuration);

// ============================================================
// AUTHORIZATION
// ============================================================
builder.Services.AddAuthorization();

// ============================================================
// PUERTO 5005 — MS Clientes
// ============================================================
builder.WebHost.UseUrls("http://localhost:5005");

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