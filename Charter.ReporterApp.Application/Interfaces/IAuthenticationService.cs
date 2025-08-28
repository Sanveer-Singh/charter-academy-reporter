using System.Threading.Tasks;
using Charter.ReporterApp.Application.DTOs;

namespace Charter.ReporterApp.Application.Interfaces
{
    public interface IAuthenticationService
    {
        Task<LoginResultDto> LoginAsync(LoginDto loginDto);
        Task<bool> LogoutAsync(string userId);
        Task<bool> ValidateUserAsync(string userId);
        Task<string> GenerateEmailVerificationTokenAsync(string email);
        Task<bool> VerifyEmailAsync(string email, string token);
    }
}