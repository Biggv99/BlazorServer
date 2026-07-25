using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace OptimusPrime.Features.Contracts.Models;

public enum Privileges { Admin, Editor, Viewer }

public class UserModel {
    [Key]
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Email { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public RefreshTokenModel? RefreshToken { get; set; } = null;
    public Privileges Role { get; set; } = Privileges.Viewer;
    public string EmailConfirmationToken { get; set; } = string.Empty;
    public bool IsEmailConfirmed { get; set; } = false;
    public string PasswordResetToken { get; set; } = string.Empty;

    [NotMapped]
    public UserDeviceModel? CurrentDevice { get; set; } = null;
}