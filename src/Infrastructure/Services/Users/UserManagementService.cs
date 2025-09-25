using System.Security.Cryptography;
using Charter.Reporter.Application.Services.Users;
using Charter.Reporter.Infrastructure.Data;
using Charter.Reporter.Infrastructure.Data.Entities;
using Charter.Reporter.Infrastructure.Identity;
using Charter.Reporter.Shared;
using Charter.Reporter.Shared.Email;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Charter.Reporter.Infrastructure.Services.Users;

public class UserManagementService : IUserManagementService
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly UserRepository _userRepository;
    private readonly AppDbContext _dbContext;
    private readonly IEmailSender _emailSender;
    private readonly ILogger<UserManagementService> _logger;
    private const string PasswordChars = "ABCDEFGHJKLMNPQRSTUVWXYZabcdefghijkmnpqrstuvwxyz23456789!@#$%^&*";
    private const string UserNotFoundError = "User not found";

    public UserManagementService(
        UserManager<ApplicationUser> userManager,
        AppDbContext dbContext,
        IEmailSender emailSender,
        ILogger<UserManagementService> logger)
    {
        _userManager = userManager;
        _userRepository = new UserRepository(dbContext, userManager);
        _dbContext = dbContext;
        _emailSender = emailSender;
        _logger = logger;
    }

    public async Task<Result<UserDetailsVm>> GetUserDetailsAsync(string userId, CancellationToken cancellationToken = default)
    {
        try
        {
            var user = await _userRepository.GetByIdAsync(userId, cancellationToken);
            if (user == null)
                return Result<UserDetailsVm>.Failure(UserNotFoundError);

            var roles = await _userRepository.GetUserRolesAsync(userId, cancellationToken);
            var lockoutEnd = user.LockoutEnd?.UtcDateTime;
            
            var vm = new UserDetailsVm
            {
                Id = user.Id,
                Email = user.Email ?? string.Empty,
                FirstName = user.FirstName ?? string.Empty,
                LastName = user.LastName ?? string.Empty,
                Organization = user.Organization ?? string.Empty,
                IdNumber = user.IdNumber ?? string.Empty,
                Cell = user.Cell ?? string.Empty,
                Address = user.Address ?? string.Empty,
                Roles = roles,
                IsLockedOut = lockoutEnd.HasValue && lockoutEnd.Value > DateTime.UtcNow,
                LockoutEnd = lockoutEnd,
                EmailConfirmed = user.EmailConfirmed
            };

            return Result<UserDetailsVm>.Success(vm);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting user details for {UserId}", userId);
            return Result<UserDetailsVm>.Failure("An error occurred while retrieving user details");
        }
    }

    public async Task<Result<UserDetailsVm>> CreateUserAsync(UserCreateVm model, CancellationToken cancellationToken = default)
    {
        try
        {
            // Check if email already exists
            if (await _userRepository.EmailExistsAsync(model.Email, cancellationToken))
                return Result<UserDetailsVm>.Failure("A user with this email address already exists");

            var user = new ApplicationUser
            {
                UserName = model.Email,
                Email = model.Email,
                FirstName = model.FirstName,
                LastName = model.LastName,
                Organization = model.Organization,
                IdNumber = model.IdNumber,
                Cell = model.Cell,
                Address = model.Address,
                EmailConfirmed = true // Auto-confirm for admin-created users
            };

            // Generate temporary password
            var tempPassword = GenerateSecurePassword();
            
            var result = await _userManager.CreateAsync(user, tempPassword);
            if (!result.Succeeded)
            {
                var errors = string.Join(", ", result.Errors.Select(e => e.Description));
                return Result<UserDetailsVm>.Failure($"Failed to create user: {errors}");
            }

            // Assign role
            if (!string.IsNullOrEmpty(model.Role))
            {
                await _userManager.AddToRoleAsync(user, model.Role);
            }

            // Log the creation
            await LogAuditAsync("UserCreated", user.Id, $"User {user.Email} created by admin", cancellationToken);

            // Send welcome email with temporary password
            try
            {
                var emailHtml = $@"
                    <h2>Welcome to Charter Reporter</h2>
                    <p>Hello {user.FirstName},</p>
                    <p>An administrator has created an account for you on Charter Reporter.</p>
                    <p><strong>Your temporary password is:</strong> <code style='background: #f4f4f4; padding: 5px; font-size: 14px;'>{tempPassword}</code></p>
                    <p>Please log in and change your password immediately.</p>
                    <p>Best regards,<br/>Charter Reporter Team</p>
                ";
                await _emailSender.SendAsync(user.Email!, "Welcome to Charter Reporter - Account Created", emailHtml);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to send welcome email to {Email}", user.Email);
                // Don't fail the operation if email fails
            }

            var details = await GetUserDetailsAsync(user.Id, cancellationToken);
            if (details.IsSuccess && details.Value != null)
            {
                details.Value.TempPassword = tempPassword; // Include temp password in response
            }
            return details;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating user {Email}", model.Email);
            return Result<UserDetailsVm>.Failure("An error occurred while creating the user");
        }
    }

    public async Task<Result<UserDetailsVm>> UpdateUserAsync(UserEditVm model, CancellationToken cancellationToken = default)
    {
        try
        {
            var user = await _userManager.FindByIdAsync(model.Id);
            if (user == null)
                return Result<UserDetailsVm>.Failure(UserNotFoundError);

            // Store original values for audit
            var originalValues = $"Email: {user.Email}, Name: {user.FirstName} {user.LastName}, Org: {user.Organization}";

            // Update user properties
            user.FirstName = model.FirstName;
            user.LastName = model.LastName;
            user.Organization = model.Organization;
            user.IdNumber = model.IdNumber;
            user.Cell = model.Cell;
            user.Address = model.Address;

            // Update email if changed
            if (user.Email != model.Email)
            {
                var emailExists = await _userRepository.EmailExistsAsync(model.Email, cancellationToken);
                if (emailExists)
                    return Result<UserDetailsVm>.Failure("A user with this email address already exists");

                user.Email = model.Email;
                user.UserName = model.Email;
            }

            var result = await _userManager.UpdateAsync(user);
            if (!result.Succeeded)
            {
                var errors = string.Join(", ", result.Errors.Select(e => e.Description));
                return Result<UserDetailsVm>.Failure($"Failed to update user: {errors}");
            }

            // Update role if changed
            if (!string.IsNullOrEmpty(model.Role))
            {
                var currentRoles = await _userManager.GetRolesAsync(user);
                if (!currentRoles.Contains(model.Role))
                {
                    // Remove all current roles
                    if (currentRoles.Any())
                        await _userManager.RemoveFromRolesAsync(user, currentRoles);
                    
                    // Add new role
                    await _userManager.AddToRoleAsync(user, model.Role);
                }
            }

            // Log the update
            var newValues = $"Email: {user.Email}, Name: {user.FirstName} {user.LastName}, Org: {user.Organization}";
            await LogAuditAsync("UserUpdated", user.Id, $"Before: {originalValues} | After: {newValues}", cancellationToken);

            return await GetUserDetailsAsync(user.Id, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating user {UserId}", model.Id);
            return Result<UserDetailsVm>.Failure("An error occurred while updating the user");
        }
    }

    public async Task<Result<PasswordResetResultVm>> ResetPasswordAsync(string userId, CancellationToken cancellationToken = default)
    {
        try
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
                return Result<PasswordResetResultVm>.Failure(UserNotFoundError);

            // Generate new temporary password
            var tempPassword = GenerateSecurePassword();

            // Remove current password
            await _userManager.RemovePasswordAsync(user);
            
            // Set new password
            var result = await _userManager.AddPasswordAsync(user, tempPassword);
            if (!result.Succeeded)
            {
                var errors = string.Join(", ", result.Errors.Select(e => e.Description));
                return Result<PasswordResetResultVm>.Failure($"Failed to reset password: {errors}");
            }

            // Log the reset
            await LogAuditAsync("PasswordReset", user.Id, $"Password reset for user {user.Email}", cancellationToken);

            // Send email with new password
            try
            {
                var emailHtml = $@"
                    <h2>Password Reset</h2>
                    <p>Hello {user.FirstName},</p>
                    <p>An administrator has reset your password for Charter Reporter.</p>
                    <p><strong>Your new temporary password is:</strong> <code style='background: #f4f4f4; padding: 5px; font-size: 14px;'>{tempPassword}</code></p>
                    <p>Please log in and change your password immediately.</p>
                    <p>If you did not request this reset, please contact your administrator immediately.</p>
                    <p>Best regards,<br/>Charter Reporter Team</p>
                ";
                await _emailSender.SendAsync(user.Email!, "Charter Reporter - Password Reset", emailHtml);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to send password reset email to {Email}", user.Email);
            }

            return Result<PasswordResetResultVm>.Success(new PasswordResetResultVm
            {
                UserId = user.Id,
                Email = user.Email ?? string.Empty,
                TempPassword = tempPassword,
                UserName = $"{user.FirstName} {user.LastName}"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error resetting password for user {UserId}", userId);
            return Result<PasswordResetResultVm>.Failure("An error occurred while resetting the password");
        }
    }

    public async Task<Result<bool>> SetLockoutAsync(string userId, bool locked, CancellationToken cancellationToken = default)
    {
        try
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
                return Result<bool>.Failure(UserNotFoundError);

            if (locked)
            {
                // Lock out for 30 days
                await _userManager.SetLockoutEndDateAsync(user, DateTimeOffset.UtcNow.AddDays(30));
                await LogAuditAsync("UserLockedOut", user.Id, $"User {user.Email} locked out for 30 days", cancellationToken);
            }
            else
            {
                // Remove lockout
                await _userManager.SetLockoutEndDateAsync(user, null);
                await LogAuditAsync("UserUnlocked", user.Id, $"User {user.Email} unlocked", cancellationToken);
            }

            return Result<bool>.Success(true);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error setting lockout status for user {UserId}", userId);
            return Result<bool>.Failure("An error occurred while updating lockout status");
        }
    }

    public async Task<Result<List<UserListVm>>> GetAllUsersAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var users = await _userRepository.GetAllUsers()
                .OrderBy(u => u.LastName)
                .ThenBy(u => u.FirstName)
                .ToListAsync(cancellationToken);

            var userVms = new List<UserListVm>();
            
            foreach (var user in users)
            {
                var roles = await _userRepository.GetUserRolesAsync(user.Id, cancellationToken);
                userVms.Add(new UserListVm
                {
                    Id = user.Id,
                    Email = user.Email ?? string.Empty,
                    FullName = $"{user.FirstName} {user.LastName}",
                    Organization = user.Organization ?? string.Empty,
                    Role = roles.FirstOrDefault() ?? "User",
                    IsLockedOut = user.LockoutEnd.HasValue && user.LockoutEnd.Value > DateTimeOffset.UtcNow,
                    EmailConfirmed = user.EmailConfirmed
                });
            }

            return Result<List<UserListVm>>.Success(userVms);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving all users");
            return Result<List<UserListVm>>.Failure("An error occurred while retrieving users");
        }
    }

    public async Task<Result<bool>> DeleteUserAsync(string userId, CancellationToken cancellationToken = default)
    {
        try
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
                return Result<bool>.Failure(UserNotFoundError);

            var userEmail = user.Email;
            var result = await _userManager.DeleteAsync(user);
            if (!result.Succeeded)
            {
                var errors = string.Join(", ", result.Errors.Select(e => e.Description));
                return Result<bool>.Failure($"Failed to delete user: {errors}");
            }

            await LogAuditAsync("UserDeleted", userId, $"User {userEmail} deleted", cancellationToken);
            return Result<bool>.Success(true);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting user {UserId}", userId);
            return Result<bool>.Failure("An error occurred while deleting the user");
        }
    }

    private static string GenerateSecurePassword()
    {
        using var rng = RandomNumberGenerator.Create();
        var bytes = new byte[12];
        rng.GetBytes(bytes);
        
        var chars = new char[12];
        for (int i = 0; i < 12; i++)
        {
            chars[i] = PasswordChars[bytes[i] % PasswordChars.Length];
        }
        
        return new string(chars);
    }

    private async Task LogAuditAsync(string action, string entityId, string details, CancellationToken cancellationToken)
    {
        var auditLog = new AuditLog
        {
            Id = Guid.NewGuid(),
            EventType = $"{action} - User:{entityId}",
            PerformedByUserId = "System", // Would be replaced with current user ID in controller
            Details = details,
            CreatedUtc = DateTime.UtcNow
        };

        _dbContext.AuditLogs.Add(auditLog);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }
}
