using System.Net;

namespace OptimusPrime.Core.Network;

public static class HttpContextExtensions {

    // ==========================
    // IP Address Recovery
    // ==========================
    public static string GetClientIpAddress(this HttpContext context) {

        // from Cloudflare header
        if(context.Request.Headers.TryGetValue("CF-Connecting-IP", out var cfIp) && !string.IsNullOrWhiteSpace(cfIp)) {
            return cfIp.ToString();
        }

        // from X-Forwarded-For header
        if(context.Request.Headers.TryGetValue("X-Forwarded-For", out var forwardedFor) && !string.IsNullOrWhiteSpace(forwardedFor)) {
            var ip = forwardedFor.ToString().Split(',')[0].Trim();
            if(!string.IsNullOrWhiteSpace(ip)) return ip;
        }

        var remoteIp = context.Connection.RemoteIpAddress;
        if(remoteIp == null) return string.Empty;
        if(IPAddress.IsLoopback(remoteIp)) return IPAddress.Loopback.ToString();
        if(remoteIp.IsIPv4MappedToIPv6) return remoteIp.MapToIPv4().ToString();
        return remoteIp.ToString();
    }

    // ====================================================
    // UserAgent Recovery: Browser and Operating System
    // ====================================================
    public static string GetClientUserAgent(this HttpContext context) {
        var userAgent = context.Request.Headers.UserAgent.ToString();
        var browser = GetBrowser(userAgent, out var version);
        var browserInfo = string.IsNullOrWhiteSpace(version) ? browser : $"{browser} {version}";
        var os = GetOs(userAgent);

        if(string.IsNullOrWhiteSpace(userAgent)) return string.Empty;
        if(string.IsNullOrWhiteSpace(browser) && string.IsNullOrWhiteSpace(os)) return userAgent;
        if(string.IsNullOrWhiteSpace(os)) return browserInfo;
        if(string.IsNullOrWhiteSpace(browserInfo)) return os;

        return $"{browserInfo} ({os})";
    }

    private static string GetBrowser(string userAgent, out string version) {
        if(TryGetVersion(userAgent, "Edg/", out version)) return "Edge";
        if(TryGetVersion(userAgent, "Chrome/", out version)) return "Chrome";
        if(TryGetVersion(userAgent, "Firefox/", out version)) return "Firefox";
        if(TryGetVersion(userAgent, "Version/", out version) && userAgent.Contains("Safari/", StringComparison.OrdinalIgnoreCase)) return "Safari";

        return string.Empty;
    }

    private static string GetOs(string userAgent) {
        if(userAgent.Contains("Windows NT 10.0", StringComparison.OrdinalIgnoreCase)) return "Windows 10/11";
        if(userAgent.Contains("Windows NT 11.0", StringComparison.OrdinalIgnoreCase)) return "Windows 10/11";
        if(userAgent.Contains("Android", StringComparison.OrdinalIgnoreCase)) return "Android";
        if(userAgent.Contains("iPhone", StringComparison.OrdinalIgnoreCase)) return "iOS";
        if(userAgent.Contains("iPad", StringComparison.OrdinalIgnoreCase)) return "iPadOS";
        if(userAgent.Contains("Mac OS X", StringComparison.OrdinalIgnoreCase)) return "macOS";
        if(userAgent.Contains("Linux", StringComparison.OrdinalIgnoreCase)) return "Linux";

        return string.Empty;
    }

    private static bool TryGetVersion(string userAgent, string token, out string version) {
        version = string.Empty;

        var index = userAgent.IndexOf(token, StringComparison.OrdinalIgnoreCase);
        if(index < 0) return false;

        index += token.Length;
        var end = index;
        while(end < userAgent.Length && (char.IsDigit(userAgent[end]) || userAgent[end] == '.')) {
            end++;
        }

        if(end == index) return false;

        version = userAgent[index..end];
        return true;
    }
}
