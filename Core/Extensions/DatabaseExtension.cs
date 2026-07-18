using FirebaseAdmin;
using Google.Apis.Auth.OAuth2;
using Microsoft.EntityFrameworkCore;
using OptimusPrime.Core.Settings;
using OptimusPrime.Infrastructure.Database;
using StackExchange.Redis;

namespace OptimusPrime.Core.Extensions;

public static class DatabaseExtension {
    public static IServiceCollection AddAppDatabase(this IServiceCollection services, IConfiguration configuration) {

        // ==========================
        // Firebase
        // ==========================
        services.Configure<FirebaseSettings>(configuration.GetSection("Firebase"));

        var firebaseKeyPath = Path.Combine(Directory.GetCurrentDirectory(), "firebase-adminsdk.json");
        if (File.Exists(firebaseKeyPath)) {
            var credential = CredentialFactory.FromFile<ServiceAccountCredential>(firebaseKeyPath);

            FirebaseApp.Create(new AppOptions() {
                Credential = credential.ToGoogleCredential()
            });
        }

        // ==========================
        // PostgreSQL
        // ==========================
        string postgresConnetionString = configuration.GetConnectionString("PostgresConnection") ?? throw new Exception("Postgres connection string not found");
        services.AddDbContext<AppDbcontext>(options => {
            options.UseNpgsql(postgresConnetionString);
        });

        // ==========================
        // Redis
        // ==========================
        string redisConnectionString = configuration.GetConnectionString("RedisConnection") ?? throw new Exception("Redis connection string not found");
        services.AddStackExchangeRedisCache(options => {
            options.Configuration = redisConnectionString;
            options.InstanceName = "OptimusPrime_";
        });
        services.AddSingleton<IConnectionMultiplexer>(_ => ConnectionMultiplexer.Connect(redisConnectionString));

        return services;
    }
}