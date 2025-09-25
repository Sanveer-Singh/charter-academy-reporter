using Charter.Reporter.Application.Services.Dashboard;

namespace Charter.Reporter.Infrastructure.Services.Dashboard;

public class DashboardServiceStub : IDashboardService
{
    // Phase 1 baseline: stub values, later replace with Dapper queries
    public Task<DashboardSummary> GetBaselineSummaryAsync(DateTime fromUtc, DateTime toUtc, CancellationToken cancellationToken)
    {
        var summary = new DashboardSummary(SalesTotal: 0m, EnrollmentCount: 0, CompletionCount: 0);
        return Task.FromResult(summary);
    }

    public Task<DashboardSummary> GetSummaryAsync(DateTime? fromUtc, DateTime? toUtc, long? courseCategoryId, CancellationToken cancellationToken)
    {
        var summary = new DashboardSummary(SalesTotal: 0m, EnrollmentCount: 0, CompletionCount: 0);
        return Task.FromResult(summary);
    }

    public Task<IReadOnlyList<CourseCategory>> GetCourseCategoriesAsync(CancellationToken cancellationToken)
    {
        IReadOnlyList<CourseCategory> empty = Array.Empty<CourseCategory>();
        return Task.FromResult(empty);
    }

    public Task<PagedResult<MoodleReportRow>> GetMoodleReportAsync(DateTime? fromUtc, DateTime? toUtc, long? courseCategoryId, string? search, string? sortColumn, bool sortDesc, bool perUser, int page, int pageSize, CancellationToken cancellationToken)
    {
        return Task.FromResult(new PagedResult<MoodleReportRow>(Array.Empty<MoodleReportRow>(), 0, page, pageSize));
    }
}


