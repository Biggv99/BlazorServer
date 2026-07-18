namespace OptimusPrime.Features.Contracts.Models;

public class JwtTokenModel {
    public string TokenType { get; set; } = "Bearer";
    public string AccessToken { get; set; } = string.Empty;
    public string RefreshToken { get; set; } = string.Empty;
    public int AccessExpiresIn { get; set; } = 0;
    public int RefreshExpiresIn { get; set; } = 0;
}
