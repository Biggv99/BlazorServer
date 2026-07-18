using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using OptimusPrime.Features.Contracts.Models;
using OptimusPrime.Core.Settings;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;

namespace OptimusPrime.Core.Security;

public class JwtProvider(
    IOptions<JwtSettings> options,
    ILogger<HashProvider> logger ) {

    private readonly JwtSettings _settings = options.Value;
    private readonly ILogger<HashProvider> _logger = logger;

    // ==========================
    // JWT configuration
    // ==========================
    public JwtTokenModel GenerateToken(UserModel user) { 

        var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_settings.SecretKey));
        var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);

        var claims = new[] {
            new Claim(JwtRegisteredClaimNames.Sub, user.Email),
            new Claim(JwtRegisteredClaimNames.Email, user.Email),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            new Claim(ClaimTypes.Role, user.Ruolo.ToString())
        };

        var tokenDescriptor = new SecurityTokenDescriptor {
            Subject = new ClaimsIdentity(claims),
            Expires = DateTime.UtcNow.AddMinutes(_settings.AccessExpirationMinutes),
            Issuer = _settings.Issuer,
            Audience = _settings.Audience,
            SigningCredentials = credentials
        };

        var handler = new JwtSecurityTokenHandler();
        var token = handler.CreateToken(tokenDescriptor);
        string accessToken = handler.WriteToken(token);

        // generate refresh token
        var randomNumber = new byte[64];
        using var rng = RandomNumberGenerator.Create();
        rng.GetBytes(randomNumber);
        string refreshToken = Convert.ToBase64String(randomNumber);

        // generate jwt token
        return new JwtTokenModel {
            TokenType = "Bearer",
            AccessToken = accessToken,
            AccessExpiresIn = _settings.AccessExpirationMinutes * 60,
            RefreshExpiresIn = _settings.RefreshExpirationMinutes * 60,
            RefreshToken = refreshToken,
        };
    }

    // ==========================
    // Claim extraction
    // ==========================

    public string? ExtractEmail(string token) {
        var principal = GetPrincipalFromToken(token);
        if (principal == null) return null;

        var email = principal.FindFirst(ClaimTypes.Email)?.Value ?? principal.FindFirst(JwtRegisteredClaimNames.Sub)?.Value;
        if (string.IsNullOrEmpty(email)) {
            _logger.LogWarning("No email found in the token");
            return null;
        }

        return email;
    }

    public string? ExtractRole(string token) {
        var principal = GetPrincipalFromToken(token);
        if (principal == null) return null;

        var role = principal.FindFirst(ClaimTypes.Role)?.Value;
        if (string.IsNullOrEmpty(role)) {
            _logger.LogWarning("No role found in the token");
            return null;
        }

        return role;
    }

    // ==========================
    // Helpers
    // ==========================

    private ClaimsPrincipal? GetPrincipalFromToken(string token) {
        var tokenHandler = new JwtSecurityTokenHandler();
        var key = Encoding.UTF8.GetBytes(_settings.SecretKey);

        var validationParameters = new TokenValidationParameters {
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(key),
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidIssuer = _settings.Issuer,
            ValidAudience = _settings.Audience,
            ValidateLifetime = true,
            ClockSkew = TimeSpan.Zero,
        };

        try {
            var principal = tokenHandler.ValidateToken(token, validationParameters, out SecurityToken securityToken);

            // check that the token is a JWT and uses the correct algorithm
            if (securityToken is not JwtSecurityToken jwtSecurityToken ||
                !jwtSecurityToken.Header.Alg.Equals(SecurityAlgorithms.HmacSha256, StringComparison.InvariantCultureIgnoreCase)) {
                _logger.LogWarning("Invalid token or incorrect algorithm");
                return null;
            }

            return principal;

        } catch (Exception ex) {
            _logger.LogError("Unable to validate token: {Message}", ex.Message);
            return null;
        }
    }
}