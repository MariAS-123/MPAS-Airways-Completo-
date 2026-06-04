using Microsoft.OpenApi.Models;

namespace Microservicio.Geografia.Api.Extensions;

public static class SwaggerExtensions
{
    public static IServiceCollection AddSwaggerDocumentation(
        this IServiceCollection services)
    {
        services.AddEndpointsApiExplorer();

        services.AddSwaggerGen(options =>
        {
            options.SwaggerDoc(
                "v1",
                new OpenApiInfo
                {
                    Title = "Microservicio.Geografia.Api",

                    Version = "v1",

                    Description =
                        "API REST del microservicio de geografía para gestión de países y ciudades."
                });

            var securityScheme =
                new OpenApiSecurityScheme
                {
                    Name = "Authorization",

                    Description =
                        "Ingrese el token JWT con el formato: Bearer {token}",

                    In = ParameterLocation.Header,

                    Type = SecuritySchemeType.Http,

                    Scheme = "bearer",

                    BearerFormat = "JWT",

                    Reference =
                        new OpenApiReference
                        {
                            Type =
                                ReferenceType.SecurityScheme,

                            Id = "Bearer"
                        }
                };

            options.AddSecurityDefinition(
                "Bearer",
                securityScheme);

            options.AddSecurityRequirement(
                new OpenApiSecurityRequirement
                {
                    {
                        securityScheme,
                        Array.Empty<string>()
                    }
                });
        });

        return services;
    }

    public static IApplicationBuilder UseSwaggerDocumentation(
        this IApplicationBuilder app)
    {
        app.UseSwagger();

        app.UseSwaggerUI(options =>
        {
            options.SwaggerEndpoint(
                "/swagger/v1/swagger.json",
                "Microservicio.Geografia.Api v1");

            options.RoutePrefix = "swagger";

            options.DisplayRequestDuration();
        });

        return app;
    }
}