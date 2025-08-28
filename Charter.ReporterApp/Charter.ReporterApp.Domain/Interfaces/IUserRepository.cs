using Charter.ReporterApp.Domain.Entities;

namespace Charter.ReporterApp.Domain.Interfaces;

/// <summary>
/// User repository interface for data access abstraction
/// </summary>
public interface IUserRepository
{
    Task<User?> GetByIdAsync(string id);
    Task<User?> GetByEmailAsync(string email);
    Task<IEnumerable<User>> GetAllAsync();
    Task<IEnumerable<User>> GetByRoleAsync(string role);
    Task<bool> CreateAsync(User user);
    Task<bool> UpdateAsync(User user);
    Task<bool> DeleteAsync(string id);
    Task<bool> ActivateAsync(string id, string activatedBy);
    Task<bool> DeactivateAsync(string id, string deactivatedBy);

    // Registration request methods
    Task<IEnumerable<RegistrationRequest>> GetPendingRegistrationsAsync();
    Task<RegistrationRequest?> GetRegistrationByIdAsync(Guid id);
    Task<RegistrationRequest?> GetRegistrationByEmailAsync(string email);
    Task<bool> CreateRegistrationRequestAsync(RegistrationRequest request);
    Task<bool> ApproveRegistrationAsync(Guid requestId, string approvedBy);
    Task<bool> RejectRegistrationAsync(Guid requestId, string rejectedBy, string reason);
    Task<bool> UpdateRegistrationAsync(RegistrationRequest request);
    Task<int> GetPendingRegistrationCountAsync();
}