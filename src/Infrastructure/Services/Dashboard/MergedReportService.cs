using Charter.Reporter.Application.Services.Dashboard;
using Microsoft.Extensions.Logging;

namespace Charter.Reporter.Infrastructure.Services.Dashboard;

public class MergedReportService : IMergedReportService
{
    private readonly IDashboardService _moodleService;
    private readonly IWordPressReportService _wordPressService;
    private readonly ILogger<MergedReportService> _logger;

    public MergedReportService(
        IDashboardService moodleService,
        IWordPressReportService wordPressService,
        ILogger<MergedReportService> logger)
    {
        _moodleService = moodleService;
        _wordPressService = wordPressService;
        _logger = logger;
    }

    public async Task<PagedResult<MergedReportRow>> GetMergedReportAsync(
        DateTime? fromUtc,
        DateTime? toUtc,
        long? courseCategoryId,
        string? search,
        string? sortColumn,
        bool sortDesc,
        bool perUser,
        int page,
        int pageSize,
        CancellationToken cancellationToken)
    {
        try
        {
            // Get both reports with larger page sizes to ensure we capture all data for merging
            var moodleTask = _moodleService.GetMoodleReportAsync(fromUtc, toUtc, courseCategoryId, search, sortColumn, sortDesc, perUser, 1, 10000, cancellationToken);
            var wordPressTask = _wordPressService.GetWordPressReportAsync(fromUtc, toUtc, courseCategoryId, search, sortColumn, sortDesc, 1, 10000, false, cancellationToken);

            await Task.WhenAll(moodleTask, wordPressTask);

            var moodleData = moodleTask.Result.Items;
            var wordPressData = wordPressTask.Result.Items;

            _logger.LogInformation("Merging reports: {MoodleCount} Moodle records, {WordPressCount} WordPress records", 
                moodleData.Count, wordPressData.Count);

            // Create merged data by email
            var mergedData = MergeReportsByEmail(moodleData, wordPressData);

            if (perUser)
            {
                // Collapse to one row per email/user: choose the row whose CompletionDate equals FourthCompletionDate when available
                mergedData = mergedData
                    .GroupBy(r => (r.Email ?? string.Empty).ToLowerInvariant())
                    .Select(g =>
                    {
                        var rows = g.ToList();
                        var preferred = rows.FirstOrDefault(r => r.FourthCompletionDate != default && r.CompletionDate == r.FourthCompletionDate)
                                       ?? rows.OrderByDescending(r => r.FourthCompletionDate).FirstOrDefault()
                                       ?? rows.First();
                        return preferred;
                    })
                    .ToList();
            }

            // Apply client-side filtering, sorting, and pagination to merged data
            var filteredData = ApplyFiltersToMergedData(mergedData, search, sortColumn, sortDesc);

            // Apply pagination
            var offset = Math.Max(page - 1, 0) * Math.Max(pageSize, 1);
            var pagedData = filteredData.Skip(offset).Take(pageSize).ToList();

            return new PagedResult<MergedReportRow>(pagedData, filteredData.Count, page, pageSize);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error merging Moodle and WordPress reports");
            throw;
        }
    }

    private List<MergedReportRow> MergeReportsByEmail(
        IReadOnlyList<MoodleReportRow> moodleData, 
        IReadOnlyList<WordPressReportRow> wordPressData)
    {
        var merged = new List<MergedReportRow>();

        // Group by email (case-insensitive)
        var moodleByEmail = moodleData
            .Where(m => !string.IsNullOrWhiteSpace(m.Email))
            .GroupBy(m => m.Email.ToLowerInvariant())
            .ToDictionary(g => g.Key, g => g.ToList());

        var wordPressByEmail = wordPressData
            .Where(w => !string.IsNullOrWhiteSpace(w.Email))
            .GroupBy(w => w.Email.ToLowerInvariant())
            .ToDictionary(g => g.Key, g => g.ToList());

        // Get all unique emails
        var allEmails = new HashSet<string>(moodleByEmail.Keys);
 

        foreach (var email in allEmails)
        {
            if (!moodleByEmail.TryGetValue(email, out var moodleRecords))
            {
                moodleRecords = new List<MoodleReportRow>();
            }
            if (!wordPressByEmail.TryGetValue(email, out var wordPressRecords))
            {
                wordPressRecords = new List<WordPressReportRow>();
            }

            if (moodleRecords.Any() && wordPressRecords.Any())
            {
                // BOTH: Merge data - Moodle as baseline, WordPress updates specific fields
                foreach (var moodleRecord in moodleRecords)
                {
                    var wpRecord = wordPressRecords.First(); // Use first WordPress record for user data
                    merged.Add(new MergedReportRow
                    {
                        UserId = moodleRecord.UserId,
                        FirstName = moodleRecord.FirstName,
                        LastName = moodleRecord.LastName,
                        Email = moodleRecord.Email,
                        
                        // WordPress takes precedence for these fields (per requirement)
                        PhoneNumber = !string.IsNullOrWhiteSpace(wpRecord.PhoneNumber) && wpRecord.PhoneNumber != "-" && wpRecord.PhoneNumber != "No Phone"
                            ? wpRecord.PhoneNumber : moodleRecord.PhoneNumber,
                        IdNo = !string.IsNullOrWhiteSpace(wpRecord.IdNo) && wpRecord.IdNo != "-" && wpRecord.IdNo != "No SAID"
                            ? wpRecord.IdNo : moodleRecord.IdNo,
                        PpraNo = !string.IsNullOrWhiteSpace(wpRecord.PpraNo) && wpRecord.PpraNo != "-" && wpRecord.PpraNo != "No PPRA"
                            ? wpRecord.PpraNo : moodleRecord.PpraNo,
                        Province = !string.IsNullOrWhiteSpace(wpRecord.Province) && wpRecord.Province != "-" && wpRecord.Province != "No Province"
                            ? wpRecord.Province : moodleRecord.Province,
                        Agency = !string.IsNullOrWhiteSpace(wpRecord.Agency) && wpRecord.Agency != "-" && wpRecord.Agency != "No Agency"
                            ? wpRecord.Agency : moodleRecord.Agency,
                        
                        // Moodle is source of truth for course data (per requirement)
                        CourseName = moodleRecord.CourseName,
                        Category = moodleRecord.Category,
                        EnrolmentDate = moodleRecord.EnrolmentDate,
                        CompletionDate = moodleRecord.CompletionDate,
                        FourthCompletionDate = moodleRecord.FourthCompletionDate,
                        
                        // No highlighting - data exists in both
                        HighlightRed = false,
                        HighlightBlue = false,
                        DataSource = "merged"
                    });
                }
            }
            else if (moodleRecords.Any())
            {
                // MOODLE ONLY: Highlight in red (in Moodle but not WordPress)
                foreach (var moodleRecord in moodleRecords)
                {
                    merged.Add(new MergedReportRow
                    {
                        UserId = moodleRecord.UserId,
                        FirstName = moodleRecord.FirstName,
                        LastName = moodleRecord.LastName,
                        PpraNo = moodleRecord.PpraNo,
                        IdNo = moodleRecord.IdNo,
                        Province = moodleRecord.Province,
                        Agency = moodleRecord.Agency,
                        Email = moodleRecord.Email,
                        PhoneNumber = moodleRecord.PhoneNumber,
                        CourseName = moodleRecord.CourseName,
                        Category = moodleRecord.Category,
                        EnrolmentDate = moodleRecord.EnrolmentDate,
                        CompletionDate = moodleRecord.CompletionDate,
                        FourthCompletionDate = moodleRecord.FourthCompletionDate,
                        HighlightRed = true,  // In Moodle but not WordPress
                        HighlightBlue = false,
                        DataSource = "moodle"
                    });
                }
            }
            else if (wordPressRecords.Any())
            {
                // WORDPRESS ONLY: Highlight in blue (in WordPress but not Moodle)
                foreach (var wpRecord in wordPressRecords)
                {
                    merged.Add(new MergedReportRow
                    {
                        UserId = wpRecord.UserId,
                        FirstName = wpRecord.FirstName,
                        LastName = wpRecord.LastName,
                        PpraNo = wpRecord.PpraNo,
                        IdNo = wpRecord.IdNo,
                        Province = wpRecord.Province,
                        Agency = wpRecord.Agency,
                        Email = wpRecord.Email,
                        PhoneNumber = wpRecord.PhoneNumber,
                        CourseName = wpRecord.CourseName,
                        Category = wpRecord.Category,
                        EnrolmentDate = wpRecord.EnrolmentDate,
                        CompletionDate = wpRecord.CompletionDate,
                        FourthCompletionDate = wpRecord.FourthCompletionDate,
                        HighlightRed = false,
                        HighlightBlue = true,  // In WordPress but not Moodle
                        DataSource = "wordpress"
                    });
                }
            }
        }

        return merged;
    }

    private List<MergedReportRow> ApplyFiltersToMergedData(
        List<MergedReportRow> data,
        string? search,
        string? sortColumn,
        bool sortDesc)
    {
        var filtered = data.AsEnumerable();

        // Apply search filter
        if (!string.IsNullOrWhiteSpace(search))
        {
            var searchTerm = search.ToLowerInvariant();
            filtered = filtered.Where(r =>
                r.FirstName.ToLowerInvariant().Contains(searchTerm) ||
                r.LastName.ToLowerInvariant().Contains(searchTerm) ||
                r.Email.ToLowerInvariant().Contains(searchTerm) ||
                r.CourseName.ToLowerInvariant().Contains(searchTerm) ||
                r.Category.ToLowerInvariant().Contains(searchTerm) ||
                r.PpraNo.ToLowerInvariant().Contains(searchTerm) ||
                r.IdNo.ToLowerInvariant().Contains(searchTerm) ||
                r.Province.ToLowerInvariant().Contains(searchTerm) ||
                r.Agency.ToLowerInvariant().Contains(searchTerm));
        }

        // Apply sorting
        filtered = (sortColumn?.ToLowerInvariant()) switch
        {
            "firstname" => sortDesc ? filtered.OrderByDescending(r => r.FirstName) : filtered.OrderBy(r => r.FirstName),
            "lastname" => sortDesc ? filtered.OrderByDescending(r => r.LastName) : filtered.OrderBy(r => r.LastName),
            "email" => sortDesc ? filtered.OrderByDescending(r => r.Email) : filtered.OrderBy(r => r.Email),
            "phonenumber" => sortDesc ? filtered.OrderByDescending(r => r.PhoneNumber) : filtered.OrderBy(r => r.PhoneNumber),
            "pprano" => sortDesc ? filtered.OrderByDescending(r => r.PpraNo) : filtered.OrderBy(r => r.PpraNo),
            "idno" => sortDesc ? filtered.OrderByDescending(r => r.IdNo) : filtered.OrderBy(r => r.IdNo),
            "province" => sortDesc ? filtered.OrderByDescending(r => r.Province) : filtered.OrderBy(r => r.Province),
            "agency" => sortDesc ? filtered.OrderByDescending(r => r.Agency) : filtered.OrderBy(r => r.Agency),
            "coursename" => sortDesc ? filtered.OrderByDescending(r => r.CourseName) : filtered.OrderBy(r => r.CourseName),
            "category" => sortDesc ? filtered.OrderByDescending(r => r.Category) : filtered.OrderBy(r => r.Category),
            "enrolmentdate" => sortDesc ? filtered.OrderByDescending(r => r.EnrolmentDate) : filtered.OrderBy(r => r.EnrolmentDate),
            "completiondate" => sortDesc ? filtered.OrderByDescending(r => r.CompletionDate) : filtered.OrderBy(r => r.CompletionDate),
            "fourthcompletiondate" => sortDesc ? filtered.OrderByDescending(r => r.FourthCompletionDate) : filtered.OrderBy(r => r.FourthCompletionDate),
            _ => sortDesc ? filtered.OrderByDescending(r => r.LastName).ThenByDescending(r => r.FirstName) 
                         : filtered.OrderBy(r => r.LastName).ThenBy(r => r.FirstName)
        };

        return filtered.ToList();
    }
}
