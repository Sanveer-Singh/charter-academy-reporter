namespace Charter.Reporter.Application.Services.Dashboard;

public interface IWordPressReportService
{
    Task<PagedResult<WordPressReportRow>> GetWordPressReportAsync(
        DateTime? fromUtc,
        DateTime? toUtc,
        long? courseCategoryId,
        string? search,
        string? sortColumn,
        bool sortDesc,
        int page,
        int pageSize,
        bool showOnlyFourthCompletion,
        CancellationToken cancellationToken);
    
    Task<IReadOnlyList<CourseCategory>> GetWordPressCategoriesAsync(CancellationToken cancellationToken);
}

public class WordPressReportRow
{
    public int UserId { get; set; }
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string PpraNo { get; set; } = string.Empty;
    public string IdNo { get; set; } = string.Empty;
    public string Province { get; set; } = string.Empty;
    public string Agency { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string PhoneNumber { get; set; } = "-";
    public string CourseName { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public DateTime EnrolmentDate { get; set; }
    public DateTime CompletionDate { get; set; }
    public DateTime FourthCompletionDate { get; set; }
}

public class MergedReportRow
{
    public int UserId { get; set; }
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string PpraNo { get; set; } = string.Empty;
    public string IdNo { get; set; } = string.Empty;
    public string Province { get; set; } = string.Empty;
    public string Agency { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string PhoneNumber { get; set; } = "-";
    public string CourseName { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public DateTime EnrolmentDate { get; set; }
    public DateTime CompletionDate { get; set; }
    public DateTime FourthCompletionDate { get; set; }
    
    // Highlighting flags for data reconciliation
    public bool HighlightRed { get; set; } = false;    // In Moodle but not WordPress
    public bool HighlightBlue { get; set; } = false;   // In WordPress but not Moodle
    public string DataSource { get; set; } = "merged";  // "moodle", "wordpress", "merged"
}
