using System.ComponentModel.DataAnnotations;

namespace Charter.ReporterApp.Application.DTOs;

/// <summary>
/// Dashboard filter DTO
/// </summary>
public class DashboardFilterDto
{
    public string DateRange { get; set; } = "30"; // Default to last 30 days
    public string? Category { get; set; }
    public int? PpraCycle { get; set; }
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }
}

/// <summary>
/// Report filter DTO
/// </summary>
public class ReportFilterDto
{
    [Required]
    public DateTime StartDate { get; set; } = DateTime.Today.AddMonths(-1);

    [Required]
    public DateTime EndDate { get; set; } = DateTime.Today;

    public string? Category { get; set; }
    public int? PpraCycle { get; set; }
    public string? ReportType { get; set; } = "enrollments";
    public string? ExportFormat { get; set; } = "csv";
}

/// <summary>
/// Dashboard view model
/// </summary>
public class DashboardViewModel
{
    public IEnumerable<MetricCardDto> Metrics { get; set; } = new List<MetricCardDto>();
    public IEnumerable<ChartDataDto> Charts { get; set; } = new List<ChartDataDto>();
    public FunnelDataDto? FunnelData { get; set; }
    public IEnumerable<CategoryDto> Categories { get; set; } = new List<CategoryDto>();
    public IEnumerable<int> AvailableCycles { get; set; } = new List<int>();
    public string UserRole { get; set; } = string.Empty;
    public DashboardFilterDto Filter { get; set; } = new();
}

/// <summary>
/// Report view model
/// </summary>
public class ReportViewModel
{
    public IEnumerable<ReportRowDto> Data { get; set; } = new List<ReportRowDto>();
    public ReportSummaryDto Summary { get; set; } = new();
    public ReportFilterDto Filter { get; set; } = new();
    public int TotalRecords { get; set; }
    public bool HasData => Data.Any();
}

/// <summary>
/// Metric card DTO
/// </summary>
public class MetricCardDto
{
    public string Label { get; set; } = string.Empty;
    public string Value { get; set; } = string.Empty;
    public string Icon { get; set; } = string.Empty;
    public string Color { get; set; } = string.Empty;
    public decimal? PercentageChange { get; set; }
    public string? ChangeDirection { get; set; }
}

/// <summary>
/// Chart data DTO
/// </summary>
public class ChartDataDto
{
    public string Type { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public IEnumerable<string> Labels { get; set; } = new List<string>();
    public IEnumerable<DataSeriesDto> Series { get; set; } = new List<DataSeriesDto>();
}

/// <summary>
/// Data series DTO
/// </summary>
public class DataSeriesDto
{
    public string Name { get; set; } = string.Empty;
    public IEnumerable<decimal> Data { get; set; } = new List<decimal>();
    public string Color { get; set; } = string.Empty;
}

/// <summary>
/// Funnel data DTO
/// </summary>
public class FunnelDataDto
{
    public int SiteVisits { get; set; }
    public int ProductViews { get; set; }
    public int CartAdds { get; set; }
    public int Purchases { get; set; }
    public int Enrollments { get; set; }
    public int Completions { get; set; }
    public decimal ProductViewRate { get; set; }
    public decimal CartAddRate { get; set; }
    public decimal PurchaseRate { get; set; }
    public decimal EnrollmentRate { get; set; }
    public decimal CompletionRate { get; set; }
}

/// <summary>
/// Category DTO
/// </summary>
public class CategoryDto
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public int CourseCount { get; set; }
}

/// <summary>
/// Report row DTO for generic report data
/// </summary>
public class ReportRowDto
{
    public DateTime Date { get; set; }
    public string Category { get; set; } = string.Empty;
    public string CourseName { get; set; } = string.Empty;
    public string UserName { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public decimal? Value { get; set; }
    public string? AdditionalData { get; set; }
}

/// <summary>
/// Report summary DTO
/// </summary>
public class ReportSummaryDto
{
    public int TotalRecords { get; set; }
    public decimal TotalValue { get; set; }
    public decimal AverageValue { get; set; }
    public string Period { get; set; } = string.Empty;
    public Dictionary<string, object> CustomMetrics { get; set; } = new();
}