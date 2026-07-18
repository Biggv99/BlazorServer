using System.ComponentModel.DataAnnotations;

namespace OptimusPrime.Features.Contracts.Models;

public enum Role { Admin, Editor, Viewer }

public class UserModel {
    [Key]
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Email { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public RefreshTokenModel? RefreshToken { get; set; } = null;
    public Role Ruolo { get; set; } = Role.Viewer; // todo: change name
    public string PublicKey { get; set; } = string.Empty;
    public string DeviceId { get; set; } = string.Empty;
    public string EmailConfirmationToken { get; set; } = string.Empty;
    public bool IsEmailConfirmed { get; set; } = false;
    public string PasswordResetToken { get; set; } = string.Empty;
}