using Charter.ReporterApp.Domain.Entities;
using Charter.ReporterApp.Domain.Interfaces;
using Charter.ReporterApp.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Charter.ReporterApp.Infrastructure.Repositories;

/// <summary>
/// User repository implementation for data access
/// </summary>
public class UserRepository : IUserRepository
{
    private readonly AppDbContext _context;
    private readonly ILogger<UserRepository> _logger;

    public UserRepository(AppDbContext context, ILogger<UserRepository> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<User?> GetByIdAsync(string id)
    {
        try
        {
            return await _context.Users
                .Include(u => u.AuditLogs)
                .FirstOrDefaultAsync(u => u.Id == id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting user by ID: {UserId}", id);
            return null;
        }
    }

    public async Task<User?> GetByEmailAsync(string email)
    {
        try
        {
            return await _context.Users
                .FirstOrDefaultAsync(u => u.Email == email);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting user by email: {Email}", email);
            return null;
        }
    }

    public async Task<IEnumerable<User>> GetAllAsync()
    {
        try
        {
            return await _context.Users
                .OrderBy(u => u.FullName)
                .ToListAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting all users");
            return Enumerable.Empty<User>();
        }
    }

    public async Task<IEnumerable<User>> GetByRoleAsync(string role)
    {
        try
        {
            return await _context.Users
                .Join(_context.UserRoles, u => u.Id, ur => ur.UserId, (u, ur) => new { User = u, RoleId = ur.RoleId })
                .Join(_context.Roles, ur => ur.RoleId, r => r.Id, (ur, r) => new { ur.User, Role = r })
                .Where(ur => ur.Role.Name == role)
                .Select(ur => ur.User)
                .OrderBy(u => u.FullName)
                .ToListAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting users by role: {Role}", role);
            return Enumerable.Empty<User>();
        }
    }

    public async Task<bool> CreateAsync(User user)
    {
        try
        {
            _context.Users.Add(user);
            var result = await _context.SaveChangesAsync();
            return result > 0;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating user: {Email}", user.Email);
            return false;
        }
    }

    public async Task<bool> UpdateAsync(User user)
    {
        try
        {
            _context.Users.Update(user);
            var result = await _context.SaveChangesAsync();
            return result > 0;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating user: {UserId}", user.Id);
            return false;
        }
    }

    public async Task<bool> DeleteAsync(string id)
    {
        try
        {
            var user = await _context.Users.FindAsync(id);
            if (user != null)
            {
                _context.Users.Remove(user);
                var result = await _context.SaveChangesAsync();
                return result > 0;
            }
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting user: {UserId}", id);
            return false;
        }
    }

    public async Task<bool> ActivateAsync(string id, string activatedBy)
    {
        try
        {
            var user = await _context.Users.FindAsync(id);
            if (user != null)
            {
                user.IsActive = true;
                var result = await _context.SaveChangesAsync();
                return result > 0;
            }
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error activating user: {UserId}", id);
            return false;
        }
    }

    public async Task<bool> DeactivateAsync(string id, string deactivatedBy)
    {
        try
        {
            var user = await _context.Users.FindAsync(id);
            if (user != null)
            {
                user.IsActive = false;
                var result = await _context.SaveChangesAsync();
                return result > 0;
            }
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deactivating user: {UserId}", id);
            return false;
        }
    }

    // Registration request methods
    public async Task<IEnumerable<RegistrationRequest>> GetPendingRegistrationsAsync()
    {
        try
        {
            return await _context.RegistrationRequests
                .Where(r => r.Status == RegistrationStatus.Pending && r.EmailVerified)
                .OrderBy(r => r.CreatedAt)
                .ToListAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting pending registrations");
            return Enumerable.Empty<RegistrationRequest>();
        }
    }

    public async Task<RegistrationRequest?> GetRegistrationByIdAsync(Guid id)
    {
        try
        {
            return await _context.RegistrationRequests
                .FirstOrDefaultAsync(r => r.Id == id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting registration by ID: {RegistrationId}", id);
            return null;
        }
    }

    public async Task<RegistrationRequest?> GetRegistrationByEmailAsync(string email)
    {
        try
        {
            return await _context.RegistrationRequests
                .FirstOrDefaultAsync(r => r.Email == email);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting registration by email: {Email}", email);
            return null;
        }
    }

    public async Task<bool> CreateRegistrationRequestAsync(RegistrationRequest request)
    {
        try
        {
            _context.RegistrationRequests.Add(request);
            var result = await _context.SaveChangesAsync();
            return result > 0;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating registration request: {Email}", request.Email);
            return false;
        }
    }

    public async Task<bool> ApproveRegistrationAsync(Guid requestId, string approvedBy)
    {
        using var transaction = await _context.Database.BeginTransactionAsync();
        try
        {
            var request = await _context.RegistrationRequests
                .FirstOrDefaultAsync(r => r.Id == requestId);

            if (request == null || request.Status != RegistrationStatus.Pending)
                return false;

            // Update request status
            request.Status = RegistrationStatus.Approved;
            request.ApprovedBy = approvedBy;
            request.ApprovedAt = DateTime.UtcNow;

            var result = await _context.SaveChangesAsync();
            if (result > 0)
            {
                await transaction.CommitAsync();
                return true;
            }

            await transaction.RollbackAsync();
            return false;
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync();
            _logger.LogError(ex, "Error approving registration: {RequestId}", requestId);
            return false;
        }
    }

    public async Task<bool> RejectRegistrationAsync(Guid requestId, string rejectedBy, string reason)
    {
        try
        {
            var request = await _context.RegistrationRequests
                .FirstOrDefaultAsync(r => r.Id == requestId);

            if (request == null || request.Status != RegistrationStatus.Pending)
                return false;

            request.Status = RegistrationStatus.Rejected;
            request.RejectedBy = rejectedBy;
            request.RejectedAt = DateTime.UtcNow;
            request.RejectionReason = reason;

            var result = await _context.SaveChangesAsync();
            return result > 0;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error rejecting registration: {RequestId}", requestId);
            return false;
        }
    }

    public async Task<bool> UpdateRegistrationAsync(RegistrationRequest request)
    {
        try
        {
            _context.RegistrationRequests.Update(request);
            var result = await _context.SaveChangesAsync();
            return result > 0;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating registration: {RequestId}", request.Id);
            return false;
        }
    }

    public async Task<int> GetPendingRegistrationCountAsync()
    {
        try
        {
            return await _context.RegistrationRequests
                .CountAsync(r => r.Status == RegistrationStatus.Pending && r.EmailVerified);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting pending registration count");
            return 0;
        }
    }
}