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

    // save user and first device
    public async Task<Result<bool>> SaveUser(UserModel user, UserDeviceModel initialDevice) {
        try {
            bool userExist = await _appDBcontext.User.AnyAsync(u => u.Email == user.Email);
            if(userExist) {
                return Result<bool>.Failure("User already exists", ErrorType.Conflict);
            }

            await _appDBcontext.User.AddAsync(user);

            initialDevice.UserId = user.Id;
            await _appDBcontext.Devices.AddAsync(initialDevice);

            await _appDBcontext.SaveChangesAsync();

            return Result<bool>.Success("User and initial device saved successfully");

        } catch(Exception ex) {
            _logger.LogError("Error during saving user: {Message}", ex.Message);
            return Result<bool>.Failure("Error during saving user", ErrorType.InternalError);
        }
    }

    // save new device for existing user
    public async Task<Result<bool>> SaveNewDevice(UserModel user, UserDeviceModel newDevice) {
        try {
            newDevice.UserId = user.Id;
            await _appDBcontext.Devices.AddAsync(newDevice);
            await _appDBcontext.SaveChangesAsync();

            return Result<bool>.Success("New device added successfully");

        } catch(Exception ex) {
            _logger.LogError("Error adding new device: {Message}", ex.Message);
            return Result<bool>.Failure("Error adding device", ErrorType.InternalError);
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
    public async Task<Result<UserModel>> GetUserSensitive(string email, string deviceId) {
        try {
            var user = await _appDBcontext.User
                .Include(u => u.RefreshToken)
                .FirstOrDefaultAsync(u => u.Email == email);

            if(user == null) {
                _logger.LogWarning("User not found");
                return Result<UserModel>.Failure("User not found", ErrorType.NotFound);
            }

            var device = await _appDBcontext.Devices
                .FirstOrDefaultAsync(d => d.UserId == user.Id && d.DeviceId == deviceId);

            user.CurrentDevice = device;
            return Result<UserModel>.Success(user, "User retrieved successfully");

        } catch(Exception ex) {
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

    // save device sync token
    public async Task<Result<bool>> SaveDeviceSyncToken(string email, string otpCode) {
        try {
            var user = await _appDBcontext.User.FirstOrDefaultAsync(u => u.Email == email); 
            if(user == null) {
                _logger.LogWarning("User not found");
                return Result<bool>.Failure("User not found", ErrorType.NotFound);
            }

            user.DeviceSyncToken = otpCode;
            await _appDBcontext.SaveChangesAsync();

            return Result<bool>.Success("Device sync token saved successfully");

        } catch(Exception ex) {
            _logger.LogError("Error during saving device sync token: {Message}", ex.Message);
            return Result<bool>.Failure("Error during saving device sync token", ErrorType.InternalError);
        }
    }

    // save or update device for existing user
    public async Task<Result<bool>> UpdateDevice(UserDeviceModel deviceToSave) {
        try {
            var existingDevice = await _appDBcontext.Devices
                .FirstOrDefaultAsync(d => d.UserId == deviceToSave.UserId && d.DeviceId == deviceToSave.DeviceId);

            // update the existing device if it exists, otherwise add a new one
            if(existingDevice != null) {
                existingDevice.PublicKey = deviceToSave.PublicKey;
                existingDevice.DeviceName = deviceToSave.DeviceName;
                existingDevice.CreatedOn = DateTime.UtcNow; 
            } else {
                await _appDBcontext.Devices.AddAsync(deviceToSave);
            }

            await _appDBcontext.SaveChangesAsync();

            return Result<bool>.Success("Device updated successfully");

        } catch(Exception ex) {
            _logger.LogError("Error updating device: {Message}", ex.Message);
            return Result<bool>.Failure("Error updating device", ErrorType.InternalError);
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