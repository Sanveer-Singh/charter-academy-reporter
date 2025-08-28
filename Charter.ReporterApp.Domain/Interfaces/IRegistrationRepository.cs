using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Charter.ReporterApp.Domain.Entities;

namespace Charter.ReporterApp.Domain.Interfaces
{
    public interface IRegistrationRepository
    {
        Task<RegistrationRequest?> GetByIdAsync(Guid id);
        Task<RegistrationRequest?> GetByEmailAsync(string email);
        Task<IEnumerable<RegistrationRequest>> GetPendingAsync();
        Task<RegistrationRequest> CreateAsync(RegistrationRequest request);
        Task UpdateAsync(RegistrationRequest request);
        Task<bool> ApproveAsync(Guid requestId, string approvedBy);
        Task<bool> RejectAsync(Guid requestId, string rejectedBy, string reason);
        Task<bool> VerifyEmailAsync(string email, string token);
    }
}