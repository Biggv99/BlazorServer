using FluentValidation;
using OptimusPrime.Features.Contracts.Commands;

namespace OptimusPrime.Features.Validators;

public class EmailValidator : AbstractValidator<EmailCommand> {
    public EmailValidator() {
        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("Email is required")
            .EmailAddress().WithMessage("Invalid email format");
    }
}

public class RegisterValidator : AbstractValidator<RegisterCommand> {
    public RegisterValidator() {
        RuleFor(x => x.Email)
         .NotEmpty().WithMessage("Email is required")
         .EmailAddress().WithMessage("Invalid email format");

        RuleFor(x => x.Password)
            .NotEmpty().WithMessage("Password is required")
            .MinimumLength(8).WithMessage("Password must be at least 8 characters long")
            .Matches(@"^(?=.*[A-Z])(?=.*\d)(?=.*[^a-zA-Z0-9]).+$").WithMessage("Password must contain at least one uppercase letter, one number, and one special character");

        RuleFor(x => x.EncryptedEPub).NotEmpty().WithMessage("EncryptedEPub is required");
        RuleFor(x => x.Nonce).NotEmpty().WithMessage("Nonce is required");
        RuleFor(x => x.Tag).NotEmpty().WithMessage("Tag is required");
        RuleFor(x => x.Salt).NotEmpty().WithMessage("Salt is required");
    }
}

public class LoginValidator : AbstractValidator<LoginCommand> {
    public LoginValidator() {

        RuleFor(x => x.Email)
        .NotEmpty().WithMessage("Email is required")
        .EmailAddress().WithMessage("Invalid email format");

        RuleFor(x => x.Password)
            .NotEmpty().WithMessage("Password is required");
    }
}

public class ConfirmEmailValidator : AbstractValidator<ConfirmEmailCommand> {
    public ConfirmEmailValidator() {
        RuleFor(x => x.Token)
            .NotEmpty().WithMessage("Token is required")
            .Length(64).WithMessage("Token must be exactly 64 characters long")
            .Matches("^[0-9a-fA-F]+$").WithMessage("Token must be a valid hexadecimal string");
    }
}

public class ResetPasswordValidator : AbstractValidator<ResetPasswordCommand> {
    public ResetPasswordValidator() {
        RuleFor(x => x.Password)
            .NotEmpty().WithMessage("Password is required")
            .MinimumLength(8).WithMessage("Password must be at least 8 characters long")
            .Matches(@"^(?=.*[A-Z])(?=.*\d)(?=.*[^a-zA-Z0-9]).+$").WithMessage("Password must contain at least one uppercase letter, one number, and one special character");

        RuleFor(x => x.ResetCode)
            .NotEmpty().WithMessage("Reset code is required")
            .Length(78).WithMessage("Reset code must be exactly 78 characters long")
            .Matches("^[0-9a-fA-F]{64}\\d{14}$").WithMessage("Reset code must contain a 64-char hex token followed by a 14-digit UTC timestamp");
    }
}

public class RefreshValidator : AbstractValidator<RefreshCommand> {
    public RefreshValidator() {
        RuleFor(x => x.RefreshToken)
            .NotEmpty().WithMessage("Refresh Token is required");
    }
}

public class ResetDeviceValidator : AbstractValidator<ResetDeviceCommand> {
    public ResetDeviceValidator() {
        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("Email is required")
            .EmailAddress().WithMessage("Invalid email format");

        RuleFor(x => x.OtpCode)
            .NotEmpty().WithMessage("OTP code is required")
            .Length(78).WithMessage("OTP code must be exactly 78 characters long")
            .Matches("^[0-9a-fA-F]{64}\\d{14}$").WithMessage("Reset code must contain a 64-char hex token followed by a 14-digit UTC timestamp");

        RuleFor(x => x.EncryptedEPub).NotEmpty().WithMessage("EncryptedEPub is required");
        RuleFor(x => x.Nonce).NotEmpty().WithMessage("Nonce is required");
        RuleFor(x => x.Tag).NotEmpty().WithMessage("Tag is required");
        RuleFor(x => x.Salt).NotEmpty().WithMessage("Salt is required");
    }
}