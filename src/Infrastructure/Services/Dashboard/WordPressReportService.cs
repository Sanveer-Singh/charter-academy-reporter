using Charter.Reporter.Application.Services.Dashboard;
using Charter.Reporter.Infrastructure.Data.MariaDb;
using Charter.Reporter.Shared.Config;
using Dapper;
using Microsoft.Extensions.Options;

namespace Charter.Reporter.Infrastructure.Services.Dashboard;

/// <summary>
/// WordPress Report Service - CORRECTED for actual WordPress structure
/// </summary>
public class WordPressReportService : IWordPressReportService
{
    private readonly IMariaDbConnectionFactory _factory;
    private readonly IOptionsMonitor<MariaDbSettings> _wooOptions;
    private const string ReadOnlyStatement = "SET TRANSACTION READ ONLY;";
    private const string WooNamedOptions = "Woo";

    public WordPressReportService(IMariaDbConnectionFactory factory, IOptionsMonitor<MariaDbSettings> wooOptions)
    {
        _factory = factory;
        _wooOptions = wooOptions;
    }

    public async Task<IReadOnlyList<CourseCategory>> GetWordPressCategoriesAsync(CancellationToken cancellationToken)
    {
        using var conn = _factory.CreateWooConnection();
        await conn.OpenAsync(cancellationToken);
        await conn.ExecuteAsync(new CommandDefinition(ReadOnlyStatement, cancellationToken: cancellationToken));
        using var tx = await conn.BeginTransactionAsync(cancellationToken);
        try
        {
            var prefix = NormalizePrefix(_wooOptions.Get(WooNamedOptions).TablePrefix, "wpbh_");
            
            // First check if we have any data at all - return debug info as categories for now
            var debugSql = $@"
SELECT 
    1 AS Id,
    CONCAT('Debug: ', 
        (SELECT COUNT(*) FROM {prefix}users), ' users, ',
        (SELECT COUNT(*) FROM {prefix}posts WHERE post_type = 'shop_order'), ' orders'
    ) AS Name
UNION ALL
SELECT 
    2 AS Id,
    CONCAT('Billing fields: ', 
        (SELECT COUNT(DISTINCT meta_key) FROM {prefix}usermeta WHERE meta_key LIKE '%billing%'), ' found'
    ) AS Name
UNION ALL
SELECT 
    3 AS Id,
    CONCAT('PPRA/SAID fields: ',
        (SELECT COUNT(*) FROM {prefix}usermeta WHERE meta_key IN ('billing_ppra', 'billing_said')), ' records'
    ) AS Name";

            var categories = (await conn.QueryAsync<CourseCategory>(new CommandDefinition(debugSql, transaction: tx, cancellationToken: cancellationToken))).ToList();
            await tx.CommitAsync(cancellationToken);
            return categories;
        }
        catch (Exception ex)
        {
            await tx.RollbackAsync(cancellationToken);
            // Return error as category for debugging
            return new List<CourseCategory> 
            { 
                new CourseCategory(999, $"ERROR: {ex.Message}")
            };
        }
    }

    public async Task<PagedResult<WordPressReportRow>> GetWordPressReportAsync(
        DateTime? fromUtc,
        DateTime? toUtc,
        long? courseCategoryId,
        string? search,
        string? sortColumn,
        bool sortDesc,
        int page,
        int pageSize,
        bool showOnlyFourthCompletion,
        CancellationToken cancellationToken)
    {
        using var conn = _factory.CreateWooConnection();
        await conn.OpenAsync(cancellationToken);
        await conn.ExecuteAsync(new CommandDefinition(ReadOnlyStatement, cancellationToken: cancellationToken));
        using var tx = await conn.BeginTransactionAsync(cancellationToken);
        try
        {
            var prefix = NormalizePrefix(_wooOptions.Get(WooNamedOptions).TablePrefix, "wpbh_");

            // Map allowed sort columns to SQL expressions to prevent injection
            static string SortExpr(string? col)
            {
                return (col?.ToLowerInvariant()) switch
                {
                    "firstname" => "FirstName",
                    "lastname" => "LastName",
                    "email" => "Email",
                    "phonenumber" => "PhoneNumber",
                    "pprano" => "PpraNo",
                    "idno" => "IdNo",
                    "province" => "Province",
                    "agency" => "Agency",
                    "coursename" => "CourseName",
                    "category" => "Category",
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

            // ULTRA SIMPLE: Just get ANY WordPress users first to see if connection works
            var sql = $@"
SELECT 
    u.ID AS UserId,
    COALESCE(u.display_name, u.user_login, 'Unknown') AS FirstName,
    COALESCE(u.user_nicename, '') AS LastName,
    COALESCE(
        (SELECT meta_value FROM {prefix}usermeta WHERE user_id = u.ID AND meta_key = 'billing_ppra' LIMIT 1),
        'No PPRA'
    ) AS PpraNo,
    COALESCE(
        (SELECT meta_value FROM {prefix}usermeta WHERE user_id = u.ID AND meta_key = 'billing_said' LIMIT 1),
        'No SAID'
    ) AS IdNo,
    COALESCE(
        (SELECT meta_value FROM {prefix}usermeta WHERE user_id = u.ID AND meta_key = 'billing_state' LIMIT 1),
        'No Province'
    ) AS Province,
    COALESCE(
        (SELECT meta_value FROM {prefix}usermeta WHERE user_id = u.ID AND meta_key = 'billing_company' LIMIT 1),
        'No Agency'
    ) AS Agency,
    u.user_email AS Email,
    COALESCE(
        (SELECT meta_value FROM {prefix}usermeta WHERE user_id = u.ID AND meta_key = 'billing_phone' LIMIT 1),
        'No Phone'
    ) AS PhoneNumber,
    CONCAT('WordPress User #', u.ID) AS CourseName,
    'WordPress Data' AS Category,
    u.user_registered AS EnrolmentDate,
    u.user_registered AS CompletionDate,
    u.user_registered AS FourthCompletionDate
FROM {prefix}users u
WHERE u.ID > 0
  AND (
    @search IS NULL OR @search = '' OR (
        u.user_email LIKE @like OR 
        u.display_name LIKE @like OR
        u.user_login LIKE @like
    )
  )
ORDER BY u.ID ASC
LIMIT @limit OFFSET @offset";

            var like = string.IsNullOrWhiteSpace(search) ? null : $"%{search!.Trim()}%";
            var offset = Math.Max(page - 1, 0) * Math.Max(pageSize, 1);
            var maxLimit = pageSize > 200 ? 100000 : 200;
            var limit = Math.Clamp(pageSize, 1, maxLimit);

            var rows = (await conn.QueryAsync<WordPressReportRow>(new CommandDefinition(sql, new
            {
                search,
                like,
                limit,
                offset
            }, tx, cancellationToken: cancellationToken))).ToList();

            // Get total count - simple count of all users
            var countSql = $@"SELECT COUNT(*) FROM {prefix}users WHERE ID > 0";

            var total = await conn.ExecuteScalarAsync<int>(new CommandDefinition(countSql, transaction: tx, cancellationToken: cancellationToken));

            await tx.CommitAsync(cancellationToken);
            return new PagedResult<WordPressReportRow>(rows, total, Math.Max(page, 1), limit);
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
        if (!prefix.EndsWith('_'))
        {
            prefix += "_";
        }
        return prefix;
    }
}