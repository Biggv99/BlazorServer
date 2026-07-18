namespace OptimusPrime.Core.Settings;

public class JwtSettings {
    public string Issuer { get; set; } = string.Empty;
    public string Audience { get; set; } = string.Empty;
    public int AccessExpirationMinutes { get; set; }
    public int RefreshExpirationMinutes { get; set; }
    public string SecretKey { get; set; } = string.Empty;
}