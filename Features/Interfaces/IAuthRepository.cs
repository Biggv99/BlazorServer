using OptimusPrime.Core.Utils;
using OptimusPrime.Features.Contracts.Models;

namespace OptimusPrime.Features.Interfaces; 

public interface IAuthRepository {
    Task<Result<bool>> ExistUser(string email);
    Task<Result<bool>> SaveUser(UserModel user);

    Task<Result<UserModel>> ConfirmEmail(string token);
    Task<Result<UserModel>> GetEmailConfirmToken(string email);

    Task<Result<UserModel>> SaveForgotPasswordToken(string email, string token);
    Task<Result<UserModel>> GetForgotPasswordToken(string email);
    Task<Result<bool>> ChangePassword(string email, string newPassword);

    Task<Result<UserModel>> GetUserSensitive(string email);

    Task<Result<bool>> SaveRefreshToken(string email, RefreshTokenModel token);

    Task<Result<bool>> DeleteAllUsers();
    Task DeleteInvalidRefreshTokens();
}   
