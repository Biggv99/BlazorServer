using Microsoft.EntityFrameworkCore;
using OptimusPrime.Core.Network;
using OptimusPrime.Core.Utils;
using OptimusPrime.Features.Contracts.Models;
using OptimusPrime.Features.Interfaces;
using OptimusPrime.Infrastructure.Database;

namespace OptimusPrime.Infrastructure.Repository;

public class DatabaseAuthRepository(
    AppDbcontext appDBcontext,
    ILogger<DatabaseAuthRepository> logger) : IAuthRepository {

    private readonly AppDbcontext _appDBcontext = appDBcontext;
    private readonly ILogger<DatabaseAuthRepository> _logger = logger;

    // ====================================================
    // User Database Operations: PostgresSQL
    // ====================================================

    // verify if user exist
    public async Task<Result<bool>> ExistUser(string email) {
        try {
            bool userExist = await _appDBcontext.User.AnyAsync(u => u.Email == email);
            if(!userExist) {
                return Result<bool>.Failure("User not found", ErrorType.NotFound);
            }

            return Result<bool>.Success("User found");

        } catch(Exception ex) {
            _logger.LogError("Error during verifying user existence: {Message}", ex.Message);
            return Result<bool>.Failure("Error during verifying user existence", ErrorType.InternalError);
        }
    }

    // save user
    public async Task<Result<bool>> SaveUser(UserModel user) {
        try {
            bool userExist = await _appDBcontext.User.AnyAsync(u => u.Email == user.Email);
            if(userExist) {
                return Result<bool>.Failure("User already exists", ErrorType.Conflict);
            }

            await _appDBcontext.User.AddAsync(user);
            await _appDBcontext.SaveChangesAsync();

            return Result<bool>.Success("User saved successfully");

        } catch(Exception ex) {
            _logger.LogError("Error during saving user: {Message}", ex.Message);
            return Result<bool>.Failure("Error during saving user", ErrorType.InternalError);
        }
    }

    // confirm user email
    public async Task<Result<UserModel>> ConfirmEmail(string token) {
        try {
            var user = await _appDBcontext.User.FirstOrDefaultAsync(u => u.EmailConfirmationToken == token);
            if(user == null) {
                return Result<UserModel>.Failure("Invalid or expired confirmation email token", ErrorType.NotFound);
            }

            UserModel confirmedUser = new() {
                Id = user.Id,
                Email = user.Email,
            };

            if (!user.IsEmailConfirmed) {
                user.IsEmailConfirmed = true;
                await _appDBcontext.SaveChangesAsync();
                return Result<UserModel>.Success(confirmedUser, "Email confirmed");

            } else { 
                return Result<UserModel>.Failure("Email already confirmed", ErrorType.Conflict); 
            }

        } catch(Exception ex) {
            _logger.LogError("Error during email confirmation: {Message}", ex.Message);
            return Result<UserModel>.Failure("Error during email confirmation", ErrorType.InternalError);
        }
    }

    // retrieve email confirmation token 
    public async Task<Result<UserModel>> GetEmailConfirmToken(string email) {
        try {
            var user = await _appDBcontext.User.FirstOrDefaultAsync(u => u.Email == email);
            if(user == null) {
                return Result<UserModel>.Failure("User not found", ErrorType.NotFound);
            }

            if (user.IsEmailConfirmed) {
                return Result<UserModel>.Failure("User already confirmed", ErrorType.NotFound);
            }

            UserModel confirmedUser = new() {
                Id = user.Id,
                Email = user.Email,
                EmailConfirmationToken = user.EmailConfirmationToken
            };

            return Result<UserModel>.Success(confirmedUser, "Email confirmation token retrieved successfully");

        } catch (Exception ex) {
            _logger.LogError("Error during retrieving email confirmation token: {Message}", ex.Message);
            return Result<UserModel>.Failure("Error during retrieving email confirmation token", ErrorType.InternalError);
        }
    }

    // save forgot password token
    public async Task<Result<UserModel>> SaveForgotPasswordToken(string email, string token) {
        try {
            var user = await _appDBcontext.User.FirstOrDefaultAsync(u => u.Email == email);
            if (user == null) {
                return Result<UserModel>.Failure("User not found", ErrorType.NotFound);
            }

            user.PasswordResetToken = token;
            await _appDBcontext.SaveChangesAsync();

            UserModel confirmedUser = new() {
                Id = user.Id,
                Email = user.Email,
            };

            return Result<UserModel>.Success(confirmedUser,  "Forgot password token saved successfully");

        } catch (Exception ex) {
            _logger.LogError("Error during retrieving forgot password token: {Message}", ex.Message);
            return Result<UserModel>.Failure("Error during retrieving forgot password token", ErrorType.InternalError);
        }
    }

    // retrieve forgot password token
    public async Task<Result<UserModel>> GetForgotPasswordToken(string email) {
        try {
            var user = await _appDBcontext.User.FirstOrDefaultAsync(u => u.Email == email);
            if (user == null) {
                return Result<UserModel>.Failure("User not found", ErrorType.NotFound);
            }

            UserModel confirmedUser = new() {
                Id = user.Id,
                Email = user.Email,
                PasswordResetToken = user.PasswordResetToken
            };

            return Result<UserModel>.Success(confirmedUser, "Forgot password token retrieved successfully");

        } catch (Exception ex) {
            _logger.LogError("Error during retrieving forgot password token: {Message}", ex.Message);
            return Result<UserModel>.Failure("Error during retrieving forgot password token", ErrorType.InternalError);
        }
    }

    // change user password by email
    public async Task<Result<bool>> ChangePassword(string email, string newPassword) {

        try {
            var user = await _appDBcontext.User.FirstOrDefaultAsync(u => u.Email == email);
            if (user == null) {
                return Result<bool>.Failure("User not found", ErrorType.NotFound);
            }

            user.Password = newPassword;
            user.PasswordResetToken = string.Empty;
            await _appDBcontext.SaveChangesAsync();

            return Result<bool>.Success("Password changed successfully");

        } catch (Exception ex) {
            _logger.LogError("Error during changing password: {Message}", ex.Message);
            return Result<bool>.Failure("Error during changing password", ErrorType.InternalError);
        }
    }

    // retrieve sensitive user information
    public async Task<Result<UserModel>> GetUserSensitive(string email) {
        try {
            var user = await _appDBcontext.User
            .Include(u => u.RefreshToken)
            .Where(u => u.Email == email)
            .Select(u => new UserModel {
                Id = u.Id,
                Email = u.Email,
                Password = u.Password, 
                RefreshToken = u.RefreshToken,
                Ruolo = u.Ruolo,
                PublicKey = u.PublicKey,
                DeviceId = u.DeviceId,
                IsEmailConfirmed = u.IsEmailConfirmed,
            }).FirstOrDefaultAsync();

            if(user == null) {
                _logger.LogWarning("User not found");
                return Result<UserModel>.Failure("User not found", ErrorType.NotFound);
            }

            return Result<UserModel>.Success(user, "User retrieved successfully");  

        } catch (Exception ex) {
            _logger.LogError("Error during retrieving user: {Message}", ex.Message);
            return Result<UserModel>.Failure("Error during retrieving user", ErrorType.InternalError);
        }
    }


    // save refresh token
    public async Task<Result<bool>> SaveRefreshToken(string email, RefreshTokenModel token) {
        try {
            var user = await _appDBcontext.User.FirstOrDefaultAsync(u => u.Email == email);
            if (user == null) {
                _logger.LogWarning("User not found");
                return Result<bool>.Failure("User not found", ErrorType.NotFound);
            }

            // revoke the previous token if it exists
            user.RefreshToken?.IsRevoked = true;

            // associate the new token with the user
            token.UserId = user.Id;
            await _appDBcontext.Refresh.AddAsync(token);

            // update the reference to the user's refresh token
            user.RefreshToken = token;

            await _appDBcontext.SaveChangesAsync();

            return Result<bool>.Success("Token updated and saved successfully");

        } catch (Exception ex) {
            _logger.LogError("Error during saving refresh token: {Message}", ex.Message);
            return Result<bool>.Failure("Error during saving refresh token", ErrorType.InternalError);
        }
    }

    // delete revoked refresh tokens
    public async Task DeleteInvalidRefreshTokens() {
        try {
            var tokensToDelete = await _appDBcontext.Refresh
                .Where(rt => rt.IsRevoked || rt.ExpiresOn <= DateTime.UtcNow)
                .ToListAsync();

            if (tokensToDelete.Count > 0) {
                _appDBcontext.Refresh.RemoveRange(tokensToDelete);
                await _appDBcontext.SaveChangesAsync();

                _logger.LogInformation($"Deleted {tokensToDelete.Count} expired or revoked refresh tokens successfully");
            }

        } catch (Exception ex) {
            _logger.LogError("Error during deleting refresh tokens: {Message}", ex.Message);
        }
    }

    // delete all users
    public async Task<Result<bool>> DeleteAllUsers() {
        try {
            var users = await _appDBcontext.User.ToListAsync();
            _appDBcontext.User.RemoveRange(users);
            await _appDBcontext.SaveChangesAsync();

            return Result<bool>.Success("All users deleted successfully");

        } catch(Exception ex) {
            _logger.LogError("Error during deleting all users: {Message}", ex.Message);
            return Result<bool>.Failure("Error during deleting all users", ErrorType.InternalError);
        }
    }

}