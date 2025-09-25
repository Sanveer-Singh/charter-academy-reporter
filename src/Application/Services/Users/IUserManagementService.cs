using Charter.Reporter.Shared;

namespace Charter.Reporter.Application.Services.Users;

public interface IUserManagementService
{
    Task<Result<UserDetailsVm>> GetUserDetailsAsync(string userId, CancellationToken cancellationToken = default);
    Task<Result<UserDetailsVm>> CreateUserAsync(UserCreateVm model, CancellationToken cancellationToken = default);
    Task<Result<UserDetailsVm>> UpdateUserAsync(UserEditVm model, CancellationToken cancellationToken = default);
    Task<Result<PasswordResetResultVm>> ResetPasswordAsync(string userId, CancellationToken cancellationToken = default);
    Task<Result<bool>> SetLockoutAsync(string userId, bool locked, CancellationToken cancellationToken = default);
    Task<Result<List<UserListVm>>> GetAllUsersAsync(CancellationToken cancellationToken = default);
    Task<Result<bool>> DeleteUserAsync(string userId, CancellationToken cancellationToken = default);
}