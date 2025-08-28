using Charter.ReporterApp.Application.DTOs;

namespace Charter.ReporterApp.Application.Interfaces;

/// <summary>
/// Registration service interface for user registration workflow
/// </summary>
public interface IRegistrationService
{
    Task<OperationResult> RegisterUserAsync(RegisterUserDto model);
    Task<OperationResult> VerifyEmailAsync(string email, string token);
    Task<IEnumerable<RoleDto>> GetAvailableRolesAsync();
    Task<bool> IsEmailAlreadyRegisteredAsync(string email);
    Task<OperationResult> ResendVerificationEmailAsync(string email);
}

/// <summary>
/// Dashboard service interface
/// </summary>
public interface IDashboardService
{
    Task<DashboardViewModel> GetDashboardDataAsync(string userRole, DashboardFilterDto filter);
    Task<IEnumerable<CategoryDto>> GetCategoriesAsync();
    Task<IEnumerable<int>> GetAvailablePpraCyclesAsync();
}

/// <summary>
/// Reporting service interface
/// </summary>
public interface IReportingService
{
    Task<ReportViewModel> GenerateReportAsync(string userRole, ReportFilterDto filter);
    Task<byte[]> ExportCsvAsync(string userRole, ReportFilterDto filter);
    Task<byte[]> ExportXlsxAsync(string userRole, ReportFilterDto filter);
}

/// <summary>
/// Operation result model
/// </summary>
public class OperationResult
{
    public bool Succeeded { get; set; }
    public string? ErrorMessage { get; set; }
    public object? Data { get; set; }

    public static OperationResult Success(object? data = null)
        => new() { Succeeded = true, Data = data };

    public static OperationResult Failed(string errorMessage)
        => new() { Succeeded = false, ErrorMessage = errorMessage };
}