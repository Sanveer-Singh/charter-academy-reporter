namespace Charter.Reporter.Application.Services.Dashboard;

public interface IDashboardService
{
    Task<DashboardSummary> GetBaselineSummaryAsync(DateTime fromUtc, DateTime toUtc, CancellationToken cancellationToken);
    Task<DashboardSummary> GetSummaryAsync(DateTime? fromUtc, DateTime? toUtc, long? courseCategoryId, CancellationToken cancellationToken);
    Task<IReadOnlyList<CourseCategory>> GetCourseCategoriesAsync(CancellationToken cancellationToken);
    Task<PagedResult<MoodleReportRow>> GetMoodleReportAsync(
        DateTime? fromUtc,
        DateTime? toUtc,
        long? courseCategoryId,
        string? search,
        string? sortColumn,
        bool sortDesc,
        int page,
        int pageSize,
        CancellationToken cancellationToken);
}

public record DashboardSummary(decimal SalesTotal, int EnrollmentCount, int CompletionCount);

public record CourseCategory(long Id, string Name);

public record PagedResult<T>(IReadOnlyList<T> Items, int TotalCount, int Page, int PageSize);

public class MoodleReportRow
{
    public int UserId { get; set; }
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string PpraNo { get; set; } = string.Empty;
    public string IdNo { get; set; } = string.Empty;
    public string Province { get; set; } = string.Empty;
    public string Agency { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string CourseName { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public DateTime EnrolmentDate { get; set; }
    public DateTime CompletionDate { get; set; }
    public DateTime FourthCompletionDate { get; set; }
}


