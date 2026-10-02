using OptimusPrime.Features.Contracts.Commands.Shared;

namespace OptimusPrime.Features.Contracts.Commands;

public class EmailCommand : BaseTrackedCommand {
    public string Email { get; set; } = string.Empty;
}

public class RegisterCommand : BaseTrackedCommand {
    public string Email { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string EncryptedEPub { get; set; } = string.Empty;
    public string Nonce { get; set; } = string.Empty;
    public string Tag { get; set; } = string.Empty;
    public string Salt { get; set; } = string.Empty;
}

public class LoginCommand : BaseTrackedCommand {
    public string Email { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
}

public class ConfirmEmailCommand: BaseTrackedCommand {
    public string Token { get; set; } = string.Empty;
}

public class ResetPasswordCommand : BaseTrackedCommand {
    public string Email { get; set; } = string.Empty;
    public string ResetCode { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
}

public class RefreshCommand : BaseTrackedCommand {
    public string RefreshToken { get; set; } = string.Empty;
}

public class ResetDeviceCommand : BaseTrackedCommand {
    public string Email { get; set; } = string.Empty;
    public string OtpCode { get; set; } = string.Empty;
    public string EncryptedEPub { get; set; } = string.Empty;
    public string Nonce { get; set; } = string.Empty;
    public string Tag { get; set; } = string.Empty;
    public string Salt { get; set; } = string.Empty;
}