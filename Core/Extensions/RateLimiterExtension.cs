using Microsoft.AspNetCore.RateLimiting;
using OptimusPrime.Core.Network;
using System.Threading.RateLimiting;

namespace OptimusPrime.Core.Extensions;

public static class RateLimiterExtension {
    public static IServiceCollection AddAppRateLimiter(this IServiceCollection services) {

        // ==========================
        // Rate Limit
        // ==========================
        services.AddRateLimiter(options => {
            options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

            options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(context => {

                string clientIp = context.GetClientIpAddress();
                if(string.IsNullOrEmpty(clientIp)) {
                    clientIp = "unknown";
                }
 
                return RateLimitPartition.GetFixedWindowLimiter(
                    partitionKey: clientIp,

                    // general limit for all clients
                    factory: partition => new FixedWindowRateLimiterOptions {
                        PermitLimit = 100,
                        Window = TimeSpan.FromSeconds(10),
                        QueueLimit = 0
                    });
            });

            // specific limit for protected endpoint 
            options.AddFixedWindowLimiter(policyName: "Protected", limiterOptions => {
                limiterOptions.PermitLimit = 5;
                limiterOptions.Window = TimeSpan.FromSeconds(10);
                limiterOptions.QueueLimit = 0;
            });
        });

        return services;
    }
}