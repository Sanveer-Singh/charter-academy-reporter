using Charter.Reporter.Application.Services.Dashboard;
using Charter.Reporter.Infrastructure.Data.MariaDb;
using Charter.Reporter.Shared.Config;
using Dapper;
using Microsoft.Extensions.Options;

namespace Charter.Reporter.Infrastructure.Services.Dashboard;

public class MariaDbDashboardService : IDashboardService
{
    private readonly IMariaDbConnectionFactory _factory;
    private readonly IOptionsMonitor<MariaDbSettings> _moodleOptions;
    private readonly IOptionsMonitor<MariaDbSettings> _wooOptions;
    private const string ReadOnlyStatement = "SET TRANSACTION READ ONLY;";
    private const string MoodleNamedOptions = "Moodle";
    private const string WooNamedOptions = "Woo";

    public MariaDbDashboardService(IMariaDbConnectionFactory factory, IOptionsMonitor<MariaDbSettings> moodleOptions, IOptionsMonitor<MariaDbSettings> wooOptions)
    {
        _factory = factory;
        _moodleOptions = moodleOptions;
        _wooOptions = wooOptions;
    }

    public async Task<DashboardSummary> GetBaselineSummaryAsync(DateTime fromUtc, DateTime toUtc, CancellationToken cancellationToken)
    {
        var salesTask = GetSalesTotalAsync(fromUtc, toUtc, cancellationToken);
        var enrollTask = GetEnrollmentCountAsync(fromUtc, toUtc, cancellationToken);
        var completeTask = GetCompletionCountAsync(fromUtc, toUtc, cancellationToken);
        await Task.WhenAll(salesTask, enrollTask, completeTask);
        return new DashboardSummary(salesTask.Result, enrollTask.Result, completeTask.Result);
    }

    public async Task<DashboardSummary> GetSummaryAsync(DateTime? fromUtc, DateTime? toUtc, long? courseCategoryId, CancellationToken cancellationToken)
    {
        var salesTask = GetSalesTotalOptionalAsync(fromUtc, toUtc, cancellationToken);
        var enrollTask = GetEnrollmentCountOptionalAsync(fromUtc, toUtc, courseCategoryId, cancellationToken);
        var completeTask = GetCompletionCountOptionalAsync(fromUtc, toUtc, courseCategoryId, cancellationToken);
        await Task.WhenAll(salesTask, enrollTask, completeTask);
        return new DashboardSummary(salesTask.Result, enrollTask.Result, completeTask.Result);
    }

    public async Task<IReadOnlyList<CourseCategory>> GetCourseCategoriesAsync(CancellationToken cancellationToken)
    {
        using var conn = _factory.CreateMoodleConnection();
        await conn.OpenAsync(cancellationToken);
        await conn.ExecuteAsync(new CommandDefinition(ReadOnlyStatement, cancellationToken: cancellationToken));
        using var tx = await conn.BeginTransactionAsync(cancellationToken);
        try
        {
            var prefix = NormalizePrefix(_moodleOptions.Get(MoodleNamedOptions).TablePrefix, string.Empty);
            var sql = $@"
SELECT c.id AS Id, c.name AS Name
FROM {prefix}course_categories c
WHERE c.visible = 1
ORDER BY c.name";
            var categories = (await conn.QueryAsync<CourseCategory>(new CommandDefinition(sql, transaction: tx, cancellationToken: cancellationToken))).ToList();
            await tx.CommitAsync(cancellationToken);
            return categories;
        }
        catch
        {
            await tx.RollbackAsync(cancellationToken);
            throw;
        }
    }

    public async Task<PagedResult<MoodleReportRow>> GetMoodleReportAsync(
        DateTime? fromUtc,
        DateTime? toUtc,
        long? courseCategoryId,
        string? search,
        string? sortColumn,
        bool sortDesc,
        int page,
        int pageSize,
        CancellationToken cancellationToken)
    {
        using var conn = _factory.CreateMoodleConnection();
        await conn.OpenAsync(cancellationToken);
        await conn.ExecuteAsync(new CommandDefinition(ReadOnlyStatement, cancellationToken: cancellationToken));
        using var tx = await conn.BeginTransactionAsync(cancellationToken);
        try
        {
            var prefix = NormalizePrefix(_moodleOptions.Get(MoodleNamedOptions).TablePrefix, string.Empty);

            // Map allowed sort columns to SQL expressions to prevent injection
            static string SortExpr(string? col)
            {
                return (col?.ToLowerInvariant()) switch
                {
                    "firstname" => "FirstName",
                    "lastname" => "LastName",
                    "email" => "Email",
                    "province" => "Province",
                    "agency" => "Agency",
                    "coursename" => "CourseName",
                    "category" => "Category",
                    // Use epoch columns for deterministic ordering
                    "enrolmentdate" => "EnrolmentEpoch",
                    "completiondate" => "CompletionEpoch",
                    "fourthcompletiondate" => "FourthCompletionEpoch",
                    _ => "LastName, FirstName, CompletionEpoch"
                };
            }

            var orderBy = SortExpr(sortColumn);
            var orderDir = sortDesc ? "DESC" : "ASC";

            var fromEpoch = fromUtc.HasValue ? new DateTimeOffset(fromUtc.Value).ToUnixTimeSeconds() : 0L;
            var toEpoch = toUtc.HasValue ? new DateTimeOffset(toUtc.Value).ToUnixTimeSeconds() : long.MaxValue;
            var noDateFilter = fromUtc == null || toUtc == null;

            var sql = $@"
WITH UserCompletionRank AS (
    SELECT userid, course, timecompleted,
           ROW_NUMBER() OVER(PARTITION BY userid ORDER BY timecompleted ASC) as completion_rank
    FROM {prefix}course_completions
    WHERE timecompleted IS NOT NULL
),
FourthCompletion AS (
    SELECT userid, timecompleted AS fourth_completion_time
    FROM UserCompletionRank
    WHERE completion_rank = 4
),
UserEnrolment AS (
    SELECT ue.userid, e.courseid, MIN(ue.timecreated) as timeenrolled
    FROM {prefix}user_enrolments ue
    JOIN {prefix}enrol e ON ue.enrolid = e.id
    GROUP BY ue.userid, e.courseid
),
EnrolmentWithFallback AS (
    SELECT 
        cc.userid,
        cc.course as courseid,
        COALESCE(en.timeenrolled, cc.timecompleted) as effective_enrolment_time
    FROM {prefix}course_completions cc
    LEFT JOIN UserEnrolment en ON cc.userid = en.userid AND cc.course = en.courseid
    WHERE cc.timecompleted IS NOT NULL
),
CustomFields AS (
    SELECT 
        uid.userid,
        MAX(CASE WHEN uif.shortname = 'ppranumber' THEN uid.data END) as ppra_no,
        MAX(CASE WHEN uif.shortname = 'said' THEN uid.data END) as id_no,
        MAX(CASE WHEN uif.shortname IN ('region_province', 'province', 'user_province', 'employerprovince', 'workprovince') THEN uid.data END) as province,
        MAX(CASE WHEN uif.shortname IN ('region_agency', 'agency_name', 'agency', 'agencyname', 'employeragency', 'workagency', 'agencycompany') THEN uid.data END) as agency
    FROM {prefix}user_info_data uid
    JOIN {prefix}user_info_field uif ON uid.fieldid = uif.id
    WHERE uif.shortname IN ('ppranumber', 'said', 'region_province', 'province', 'user_province', 'employerprovince', 'workprovince', 'region_agency', 'agency_name', 'agency', 'agencyname', 'employeragency', 'workagency', 'agencycompany')
    GROUP BY uid.userid
),
Base AS (
    SELECT
        u.id AS UserId, 
        u.firstname AS FirstName, 
        u.lastname AS LastName, 
        COALESCE(cf.ppra_no, '') AS PpraNo,
        COALESCE(cf.id_no, '') AS IdNo,
        COALESCE(NULLIF(cf.province, ''), '-') AS Province,
        COALESCE(NULLIF(cf.agency, ''), '-') AS Agency,
        u.email AS Email,
        c.fullname AS CourseName, 
        cat.name AS Category,
        ef.effective_enrolment_time AS EnrolmentEpoch,
        cc.timecompleted AS CompletionEpoch,
        fc.fourth_completion_time AS FourthCompletionEpoch
    FROM {prefix}user u
    JOIN {prefix}course_completions cc ON u.id = cc.userid
    JOIN {prefix}course c ON cc.course = c.id
    JOIN {prefix}course_categories cat ON c.category = cat.id
    JOIN FourthCompletion fc ON u.id = fc.userid
    JOIN EnrolmentWithFallback ef ON u.id = ef.userid AND c.id = ef.courseid
    LEFT JOIN CustomFields cf ON u.id = cf.userid
    WHERE cc.timecompleted IS NOT NULL
      AND (@noDate = 1 OR fc.fourth_completion_time BETWEEN @fromEpoch AND @toEpoch)
      AND (@categoryId IS NULL OR c.category = @categoryId)
      AND (
            @search IS NULL OR @search = '' OR (
                u.firstname LIKE @like OR u.lastname LIKE @like OR u.email LIKE @like OR c.fullname LIKE @like OR cat.name LIKE @like OR cf.ppra_no LIKE @like OR cf.id_no LIKE @like OR cf.province LIKE @like OR cf.agency LIKE @like
            )
      )
)
SELECT SQL_CALC_FOUND_ROWS
    UserId,
    FirstName,
    LastName,
    PpraNo,
    IdNo,
    Province,
    Agency,
    Email,
    CourseName,
    Category,
    FROM_UNIXTIME(EnrolmentEpoch) AS EnrolmentDate,
    FROM_UNIXTIME(CompletionEpoch) AS CompletionDate,
    FROM_UNIXTIME(FourthCompletionEpoch) AS FourthCompletionDate
FROM Base
ORDER BY {orderBy} {orderDir}
LIMIT @limit OFFSET @offset;
SELECT FOUND_ROWS();";

            var like = string.IsNullOrWhiteSpace(search) ? null : $"%{search!.Trim()}%";
            var offset = Math.Max(page - 1, 0) * Math.Max(pageSize, 1);
            // Allow larger page sizes for export scenarios (up to 100K), but limit regular queries to 200 for performance
            var maxLimit = pageSize > 200 ? 100000 : 200;
            var limit = Math.Clamp(pageSize, 1, maxLimit);

            using var multi = await conn.QueryMultipleAsync(new CommandDefinition(sql, new
            {
                noDate = noDateFilter ? 1 : 0,
                fromEpoch,
                toEpoch,
                categoryId = courseCategoryId,
                search,
                like,
                limit,
                offset
            }, tx, cancellationToken: cancellationToken));

            var rows = (await multi.ReadAsync<MoodleReportRow>()).ToList();
            var total = await multi.ReadFirstAsync<int>();

            await tx.CommitAsync(cancellationToken);
            return new PagedResult<MoodleReportRow>(rows, total, Math.Max(page, 1), limit);
        }
        catch
        {
            await tx.RollbackAsync(cancellationToken);
            throw;
        }
    }

    private static string NormalizePrefix(string? configuredPrefix, string fallback)
    {
        var prefix = string.IsNullOrWhiteSpace(configuredPrefix) ? fallback : configuredPrefix!;
        // Ensure trailing underscore to form valid table names, e.g. "wpbh_" + "posts"
        if (!prefix.EndsWith('_'))
        {
            prefix += "_";
        }
        return prefix;
    }

    private async Task<decimal> GetSalesTotalAsync(DateTime fromUtc, DateTime toUtc, CancellationToken ct)
    {
        using var conn = _factory.CreateWooConnection();
        await conn.OpenAsync(ct);
        // Ensure read-only BEFORE starting a transaction
        await conn.ExecuteAsync(new CommandDefinition(ReadOnlyStatement, cancellationToken: ct));
        using var tx = await conn.BeginTransactionAsync(ct);
        try
        {
            var prefix = NormalizePrefix(_wooOptions.Get(WooNamedOptions).TablePrefix, "wp_");
            var sql = $@"
SELECT COALESCE(SUM(CAST(pm.meta_value AS DECIMAL(18,2))),0)
FROM {prefix}posts p
JOIN {prefix}postmeta pm ON pm.post_id = p.ID AND pm.meta_key = '_order_total'
WHERE p.post_type = 'shop_order'
  AND p.post_status IN ('wc-completed')
  AND p.post_date BETWEEN @fromLocal AND @toLocal";
            // Woo stores local time in post_date
            var total = await conn.ExecuteScalarAsync<decimal>(new CommandDefinition(sql, new { fromLocal = fromUtc.ToLocalTime(), toLocal = toUtc.ToLocalTime() }, tx, cancellationToken: ct));
            await tx.CommitAsync(ct);
            return total;
        }
        catch
        {
            await tx.RollbackAsync(ct);
            throw;
        }
    }

    private async Task<decimal> GetSalesTotalOptionalAsync(DateTime? fromUtc, DateTime? toUtc, CancellationToken ct)
    {
        using var conn = _factory.CreateWooConnection();
        await conn.OpenAsync(ct);
        await conn.ExecuteAsync(new CommandDefinition(ReadOnlyStatement, cancellationToken: ct));
        using var tx = await conn.BeginTransactionAsync(ct);
        try
        {
            var noFilter = fromUtc == null || toUtc == null;
            var prefix = NormalizePrefix(_wooOptions.Get(WooNamedOptions).TablePrefix, "wp_");
            var sql = $@"
SELECT COALESCE(SUM(CAST(pm.meta_value AS DECIMAL(18,2))),0)
FROM {prefix}posts p
JOIN {prefix}postmeta pm ON pm.post_id = p.ID AND pm.meta_key = '_order_total'
WHERE p.post_type = 'shop_order'
  AND p.post_status IN ('wc-completed')
  AND (@noFilter = 1 OR p.post_date BETWEEN @fromLocal AND @toLocal)";
            var fromLocal = (fromUtc ?? new DateTime(2000, 1, 1, 0, 0, 0, DateTimeKind.Utc)).ToLocalTime();
            var toLocal = (toUtc ?? DateTime.UtcNow).ToLocalTime();
            var total = await conn.ExecuteScalarAsync<decimal>(new CommandDefinition(sql, new { noFilter = noFilter ? 1 : 0, fromLocal, toLocal }, tx, cancellationToken: ct));
            await tx.CommitAsync(ct);
            return total;
        }
        catch
        {
            await tx.RollbackAsync(ct);
            throw;
        }
    }

    private async Task<int> GetEnrollmentCountAsync(DateTime fromUtc, DateTime toUtc, CancellationToken ct)
    {
        using var conn = _factory.CreateMoodleConnection();
        await conn.OpenAsync(ct);
        await conn.ExecuteAsync(new CommandDefinition(ReadOnlyStatement, cancellationToken: ct));
        using var tx = await conn.BeginTransactionAsync(ct);
        try
        {
            var prefix = NormalizePrefix(_moodleOptions.Get(MoodleNamedOptions).TablePrefix, string.Empty);
            var sql = $@"
SELECT COUNT(ue.id)
FROM {prefix}user_enrolments ue
JOIN {prefix}enrol e ON e.id = ue.enrolid
WHERE ue.timecreated BETWEEN @fromEpoch AND @toEpoch";
            var fromEpoch = new DateTimeOffset(fromUtc).ToUnixTimeSeconds();
            var toEpoch = new DateTimeOffset(toUtc).ToUnixTimeSeconds();
            var count = await conn.ExecuteScalarAsync<int>(new CommandDefinition(sql, new { fromEpoch, toEpoch }, tx, cancellationToken: ct));
            await tx.CommitAsync(ct);
            return count;
        }
        catch
        {
            await tx.RollbackAsync(ct);
            throw;
        }
    }

    private async Task<int> GetEnrollmentCountOptionalAsync(DateTime? fromUtc, DateTime? toUtc, long? categoryId, CancellationToken ct)
    {
        using var conn = _factory.CreateMoodleConnection();
        await conn.OpenAsync(ct);
        await conn.ExecuteAsync(new CommandDefinition(ReadOnlyStatement, cancellationToken: ct));
        using var tx = await conn.BeginTransactionAsync(ct);
        try
        {
            var noFilter = fromUtc == null || toUtc == null;
            var prefix = NormalizePrefix(_moodleOptions.Get(MoodleNamedOptions).TablePrefix, string.Empty);
            var sql = $@"
SELECT COUNT(ue.id)
FROM {prefix}user_enrolments ue
JOIN {prefix}enrol e ON e.id = ue.enrolid
JOIN {prefix}course c ON c.id = e.courseid
WHERE (@noFilter = 1 OR ue.timecreated BETWEEN @fromEpoch AND @toEpoch)
  AND (@categoryId IS NULL OR c.category = @categoryId)";
            var fromEpoch = fromUtc.HasValue ? new DateTimeOffset(fromUtc.Value).ToUnixTimeSeconds() : 0;
            var toEpoch = toUtc.HasValue ? new DateTimeOffset(toUtc.Value).ToUnixTimeSeconds() : int.MaxValue;
            var count = await conn.ExecuteScalarAsync<int>(new CommandDefinition(sql, new { noFilter = noFilter ? 1 : 0, fromEpoch, toEpoch, categoryId }, tx, cancellationToken: ct));
            await tx.CommitAsync(ct);
            return count;
        }
        catch
        {
            await tx.RollbackAsync(ct);
            throw;
        }
    }

    private async Task<int> GetCompletionCountAsync(DateTime fromUtc, DateTime toUtc, CancellationToken ct)
    {
        using var conn = _factory.CreateMoodleConnection();
        await conn.OpenAsync(ct);
        await conn.ExecuteAsync(new CommandDefinition("SET TRANSACTION READ ONLY;", cancellationToken: ct));
        using var tx = await conn.BeginTransactionAsync(ct);
        try
        {
            var prefix = NormalizePrefix(_moodleOptions.Get("Moodle").TablePrefix, string.Empty);
            var sql = $@"
SELECT COUNT(cc.id)
FROM {prefix}course_completions cc
WHERE cc.timecompleted IS NOT NULL
  AND cc.timecompleted BETWEEN @fromEpoch AND @toEpoch";
            var fromEpoch = new DateTimeOffset(fromUtc).ToUnixTimeSeconds();
            var toEpoch = new DateTimeOffset(toUtc).ToUnixTimeSeconds();
            var count = await conn.ExecuteScalarAsync<int>(new CommandDefinition(sql, new { fromEpoch, toEpoch }, tx, cancellationToken: ct));
            await tx.CommitAsync(ct);
            return count;
        }
        catch
        {
            await tx.RollbackAsync(ct);
            throw;
        }
    }

    private async Task<int> GetCompletionCountOptionalAsync(DateTime? fromUtc, DateTime? toUtc, long? categoryId, CancellationToken ct)
    {
        using var conn = _factory.CreateMoodleConnection();
        await conn.OpenAsync(ct);
        await conn.ExecuteAsync(new CommandDefinition("SET TRANSACTION READ ONLY;", cancellationToken: ct));
        using var tx = await conn.BeginTransactionAsync(ct);
        try
        {
            var noFilter = fromUtc == null || toUtc == null;
            var prefix = NormalizePrefix(_moodleOptions.Get("Moodle").TablePrefix, string.Empty);
            var sql = $@"
SELECT COUNT(cc.id)
FROM {prefix}course_completions cc
JOIN {prefix}course c ON c.id = cc.course
WHERE cc.timecompleted IS NOT NULL
  AND (@noFilter = 1 OR cc.timecompleted BETWEEN @fromEpoch AND @toEpoch)
  AND (@categoryId IS NULL OR c.category = @categoryId)";
            var fromEpoch = fromUtc.HasValue ? new DateTimeOffset(fromUtc.Value).ToUnixTimeSeconds() : 0;
            var toEpoch = toUtc.HasValue ? new DateTimeOffset(toUtc.Value).ToUnixTimeSeconds() : int.MaxValue;
            var count = await conn.ExecuteScalarAsync<int>(new CommandDefinition(sql, new { noFilter = noFilter ? 1 : 0, fromEpoch, toEpoch, categoryId }, tx, cancellationToken: ct));
            await tx.CommitAsync(ct);
            return count;
        }
        catch
        {
            await tx.RollbackAsync(ct);
            throw;
        }
    }
}


