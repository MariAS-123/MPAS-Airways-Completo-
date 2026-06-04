using Microservicio.Aeropuertos.Api.Model.Settings;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;

namespace Microservicio.Aeropuertos.Api.Extensions;

public static class AuthenticationExtensions
{
    public static IServiceCollection AddJwtAuthentication(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.Configure<JwtSettings>(
            configuration.GetSection(
                JwtSettings.SectionName));

        var jwtSettings =
            configuration
                .GetSection(
                    JwtSettings.SectionName)
                .Get<JwtSettings>();

        if (jwtSettings is null)
        {
            throw new InvalidOperationException(
                "La configuración JwtSettings no existe o es inválida.");
        }

        if (string.IsNullOrWhiteSpace(jwtSettings.SecretKey))
        {
            throw new InvalidOperationException(
                "JwtSettings.SecretKey es obligatoria.");
        }

        if (string.IsNullOrWhiteSpace(jwtSettings.Issuer))
        {
            throw new InvalidOperationException(
                "JwtSettings.Issuer es obligatoria.");
        }

        if (string.IsNullOrWhiteSpace(jwtSettings.Audience))
        {
            throw new InvalidOperationException(
                "JwtSettings.Audience es obligatoria.");
        }

        var key =
            Encoding.UTF8.GetBytes(
                jwtSettings.SecretKey);

        services
            .AddAuthentication(options =>
            {
                options.DefaultAuthenticateScheme =
                    JwtBearerDefaults.AuthenticationScheme;

                options.DefaultChallengeScheme =
                    JwtBearerDefaults.AuthenticationScheme;

                options.DefaultScheme =
                    JwtBearerDefaults.AuthenticationScheme;
            })
            .AddJwtBearer(options =>
            {
                options.RequireHttpsMetadata =
                    false;

                options.SaveToken =
                    true;

                options.MapInboundClaims =
                    false;

                options.TokenValidationParameters =
                    new TokenValidationParameters
                    {
                        ValidateIssuerSigningKey = true,

                        IssuerSigningKey =
                            new SymmetricSecurityKey(key),

                        ValidateIssuer = true,

                        ValidIssuer =
                            jwtSettings.Issuer,

                        ValidateAudience = true,

                        ValidAudience =
                            jwtSettings.Audience,

                        ValidateLifetime = true,

                        ClockSkew =
                            TimeSpan.Zero,

                        RoleClaimType =
                            System.Security.Claims.ClaimTypes.Role,

                        NameClaimType =
                            System.Security.Claims.ClaimTypes.Name
                    };
            });

        return services;
    }
}