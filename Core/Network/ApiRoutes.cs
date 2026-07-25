namespace OptimusPrime.Core.Network; 

public static class ApiRoutes {

    // ==========================
    // Endpoint routes
    // ==========================

    // system
    public const string AppVersion = "/api/v1/system/app-version";
    public const string Application  = "/api/v1/system/get-application";

    // auth
    public const string Register = "/api/v1/auth/register";
    public const string Login = "/api/v1/auth/login";

    public const string ConfirmEmail = "/public/v1/auth/confirm-email";
    public const string ResendConfirmEmail = "/api/v1/auth/resend-confirm-email";

    public const string ForgotPassword = "/api/v1/auth/forgot-password";
    public const string ResetPassword = "/api/v1/auth/reset-password";

    public const string Refresh = "/api/v1/auth/refresh";

    // profile
    public const string Me = "/api/v1/profile/me";

    // instruction 
    public const string Instruction = "/api/v1/gui/instruction";
}
