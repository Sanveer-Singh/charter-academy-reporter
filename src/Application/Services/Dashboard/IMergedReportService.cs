using Charter.Reporter.Application.Services.Dashboard;

namespace Charter.Reporter.Application.Services.Dashboard;

public interface IMergedReportService
{
    Task<PagedResult<MergedReportRow>> GetMergedReportAsync(
        DateTime? fromUtc,
        DateTime? toUtc,
        long? courseCategoryId,
        string? search,
        string? sortColumn,
        bool sortDesc,
        bool perUser,
        int page,
        int pageSize,
        CancellationToken cancellationToken);
}
