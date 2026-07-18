using System.ComponentModel.DataAnnotations;

namespace OptimusPrime.Features.Contracts.Models;

public class RefreshTokenModel {
    [Key]
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid UserId { get; set; }

    public string RefreshToken { get; set; } = string.Empty;
    public DateTime CreatedOn { get; set; } = DateTime.UtcNow;
    public DateTime ExpiresOn { get; set; } = DateTime.UtcNow.AddDays(7);
    public bool IsRevoked { get; set; } = false;
}