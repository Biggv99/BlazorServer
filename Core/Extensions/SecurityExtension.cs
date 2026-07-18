using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.IdentityModel.Tokens;
using OptimusPrime.Core.Security;
using OptimusPrime.Core.Security.Crypto;
using OptimusPrime.Core.Settings;
using OptimusPrime.Features.Contracts.Models;
using System.Net;
using System.Security.Claims;
using System.Text;

namespace OptimusPrime.Core.Extensions;

public static class SecurityExtension {
    public static IServiceCollection AddAppSecurity(this IServiceCollection services, IConfiguration configuration) {

        // ==========================
        // Crypto Providers
        // ==========================
        services.Configure<EkeSettings>(configuration.GetSection("Eke"));
        services.Configure<SecuritySettings>(configuration.GetSection("Security"));

        services.AddScoped<EcdhProvider>();
        services.AddScoped<EkeProvider>();
        services.AddScoped<ProviderService>();

        // ==========================
        // Hash
        // ==========================
        services.AddSingleton<HashProvider>();

        // ==========================
        // Forwarded Headers 
        // ==========================
        services.Configure<ForwardedHeadersOptions>(options => {
            options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;

            // known proxies
            var knownProxies = configuration
                .GetSection("ForwardedHeaders:KnownProxies")
                .Get<string[]>() ?? [];

            foreach (var proxy in knownProxies) {
                if (IPAddress.TryParse(proxy, out var ip)) {
                    options.KnownProxies.Add(ip);
                }
            }

            // known networks
            var knownNetworks = configuration
                .GetSection("ForwardedHeaders:KnownNetworks")
                .Get<string[]>() ?? [];

            foreach (var network in knownNetworks) {
                if (System.Net.IPNetwork.TryParse(network, out var ipNetwork)) {
                    options.KnownIPNetworks.Add(ipNetwork);
                }
            }
        });

        // ==========================
        // Jwt
        // ==========================
        services.AddScoped<JwtProvider>();
        services.Configure<JwtSettings>(configuration.GetSection("Jwt"));

        var jwtSettings = configuration.GetSection("Jwt").Get<JwtSettings>();

        if (jwtSettings == null ||
           string.IsNullOrWhiteSpace(jwtSettings.SecretKey) ||
           Encoding.UTF8.GetBytes(jwtSettings.SecretKey).Length < 32 ||
           string.IsNullOrWhiteSpace(jwtSettings.Issuer) ||
           string.IsNullOrWhiteSpace(jwtSettings.Audience)) {

            throw new Exception("Jwt SecretKey not found, empty or invalid");
        }

        var secretKeyBytes = Encoding.UTF8.GetBytes(jwtSettings.SecretKey);

        services.AddAuthentication(x => {
            x.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
            x.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
        })
        .AddJwtBearer(x => {
            x.RequireHttpsMetadata = true;
            x.SaveToken = true;
            x.TokenValidationParameters = new TokenValidationParameters {
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = new SymmetricSecurityKey(secretKeyBytes),
                ValidateIssuer = true,
                ValidateAudience = true,
                ValidIssuer = jwtSettings.Issuer,
                ValidAudience = jwtSettings.Audience,
                ValidateLifetime = true,
                ClockSkew = TimeSpan.Zero,
                RoleClaimType = ClaimTypes.Role
            };
        });

        services.AddAuthorizationBuilder().AddPolicy("AdminOnly", policy => policy.RequireRole(Role.Admin.ToString()));

        // ==========================
        // Redis: Data Protection 
        // ==========================
        var redisConnectionString = configuration.GetConnectionString("RedisConnection");

        if (!string.IsNullOrEmpty(redisConnectionString)) {
            var redis = StackExchange.Redis.ConnectionMultiplexer.Connect(redisConnectionString);

            services.AddDataProtection()
                .SetApplicationName("OptimusPrime")
                .PersistKeysToStackExchangeRedis(redis, "DataProtection-Keys");

        } else {
            services.AddDataProtection().SetApplicationName("OptimusPrime");
        }

        services.AddAntiforgery(options => {
            var cookieName = configuration["Antiforgery:CookieName"];
            if (!string.IsNullOrWhiteSpace(cookieName))
                options.Cookie.Name = cookieName;
        });

        return services;
    }
}