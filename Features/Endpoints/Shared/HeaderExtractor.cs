using Microsoft.Extensions.Options;            
using OptimusPrime.Core.Network;
using OptimusPrime.Core.Settings;
using OptimusPrime.Core.Utils;
using OptimusPrime.Features.Contracts.Commands.Shared;

namespace OptimusPrime.Features.Endpoints.Shared;

public static class HeaderExtractor {

    public static Result<bool> ExtractHeaders(
        this BaseTrackedCommand command,
        HttpContext context,
        bool requireAccessToken = true,
        bool requireDeviceChallenge = true) {

        // ip address 
        command.IpAddress = context.GetClientIpAddress();

        // user agent
        string customUserAgent = context.Request.Headers["x-device-user-agent"].ToString();
        command.UserAgent = !string.IsNullOrWhiteSpace(customUserAgent)
            ? customUserAgent
            : context.GetClientUserAgent();

        // device id
        string deviceId = context.Request.Headers["x-device-id"].ToString();
        if (string.IsNullOrWhiteSpace(deviceId)) {
            return Result<bool>.Failure("Missing required data: device id", ErrorType.Unauthorized);
        }
        command.DeviceId = deviceId;

        // access token 
        if (requireAccessToken) {
            var authHeader = context.Request.Headers.Authorization.FirstOrDefault();
            if (!string.IsNullOrEmpty(authHeader) && authHeader.StartsWith("Bearer ")) {
                command.AccessToken = authHeader["Bearer ".Length..].Trim();
            } else {
                return Result<bool>.Failure("Missing required data: access token", ErrorType.Unauthorized);
            }
        }

        // device challenge
        if (requireDeviceChallenge) {
            string signature = context.Request.Headers["x-device-signature"].ToString();
            string timestampStr = context.Request.Headers["x-device-timestamp"].ToString();

            if (string.IsNullOrWhiteSpace(signature) || string.IsNullOrWhiteSpace(timestampStr) || !long.TryParse(timestampStr, out var ts)) {
                return Result<bool>.Failure("Missing required data: signature", ErrorType.Unauthorized);
            }

            command.DeviceSignature = signature;
            command.DeviceTimestamp = ts;
        }

        // check: security secret
        var securitySettings = context.RequestServices.GetRequiredService<IOptions<SecuritySettings>>().Value;
        var securitySecret = context.Request.Headers["x-security-secret"].ToString();
        if (string.IsNullOrWhiteSpace(securitySecret) || securitySecret != securitySettings.Secret) {
            return Result<bool>.Failure("Invalid security secret", ErrorType.Unauthorized);
        }

        return Result<bool>.Success(string.Empty);
    }
}