using Charter.ReporterApp.Domain.ValueObjects;

namespace Charter.ReporterApp.Domain.Interfaces;

/// <summary>
/// Report repository interface for reporting data access
/// </summary>
public interface IReportRepository
{
    Task<DashboardData> GetDashboardDataAsync(string userRole, DateRange dateRange, string? category = null, int? ppraCycle = null);
    Task<IEnumerable<EnrollmentData>> GetEnrollmentDataAsync(DateRange dateRange, string? category = null);
    Task<IEnumerable<CompletionData>> GetCompletionDataAsync(DateRange dateRange, string? category = null);
    Task<IEnumerable<SalesData>> GetSalesDataAsync(DateRange dateRange);
    Task<FunnelData> GetConversionFunnelAsync(DateRange dateRange);
    Task<IEnumerable<CategoryData>> GetCategoriesAsync();
    Task<IEnumerable<int>> GetAvailablePpraCyclesAsync();
    Task<byte[]> ExportToCsvAsync(string userRole, DateRange dateRange, string? category = null);
    Task<byte[]> ExportToXlsxAsync(string userRole, DateRange dateRange, string? category = null);
}

/// <summary>
/// Dashboard data model
/// </summary>
public class DashboardData
{
    public int TotalEnrollments { get; set; }
    public int TotalCompletions { get; set; }
    public decimal TotalSales { get; set; }
    public int ActiveUsers { get; set; }
    public decimal CompletionRate { get; set; }
    public IEnumerable<MetricCard> Metrics { get; set; } = new List<MetricCard>();
    public IEnumerable<ChartData> Charts { get; set; } = new List<ChartData>();
    public FunnelData? FunnelData { get; set; }
}

/// <summary>
/// Metric card model for dashboard
/// </summary>
public class MetricCard
{
    public string Label { get; set; } = string.Empty;
    public string Value { get; set; } = string.Empty;
    public string Icon { get; set; } = string.Empty;
    public string Color { get; set; } = string.Empty;
    public decimal? PercentageChange { get; set; }
}

/// <summary>
/// Chart data model
/// </summary>
public class ChartData
{
    public string Type { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public IEnumerable<string> Labels { get; set; } = new List<string>();
    public IEnumerable<DataSeries> Series { get; set; } = new List<DataSeries>();
}

/// <summary>
/// Data series for charts
/// </summary>
public class DataSeries
{
    public string Name { get; set; } = string.Empty;
    public IEnumerable<decimal> Data { get; set; } = new List<decimal>();
    public string Color { get; set; } = string.Empty;
}

/// <summary>
/// Enrollment data model
/// </summary>
public class EnrollmentData
{
    public DateTime Date { get; set; }
    public string CourseName { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public int Count { get; set; }
    public string UserName { get; set; } = string.Empty;
}

/// <summary>
/// Completion data model
/// </summary>
public class CompletionData
{
    public DateTime Date { get; set; }
    public string CourseName { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public string UserName { get; set; } = string.Empty;
    public decimal Grade { get; set; }
}

/// <summary>
/// Sales data model
/// </summary>
public class SalesData
{
    public DateTime Date { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public string CustomerName { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
}

/// <summary>
/// Funnel data model for conversion analysis
/// </summary>
public class FunnelData
{
    public int SiteVisits { get; set; }
    public int ProductViews { get; set; }
    public int CartAdds { get; set; }
    public int Purchases { get; set; }
    public int Enrollments { get; set; }
    public int Completions { get; set; }

    public decimal ProductViewRate => SiteVisits > 0 ? (decimal)ProductViews / SiteVisits * 100 : 0;
    public decimal CartAddRate => ProductViews > 0 ? (decimal)CartAdds / ProductViews * 100 : 0;
    public decimal PurchaseRate => CartAdds > 0 ? (decimal)Purchases / CartAdds * 100 : 0;
    public decimal EnrollmentRate => Purchases > 0 ? (decimal)Enrollments / Purchases * 100 : 0;
    public decimal CompletionRate => Enrollments > 0 ? (decimal)Completions / Enrollments * 100 : 0;
}

/// <summary>
/// Category data model
/// </summary>
public class CategoryData
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public int CourseCount { get; set; }
}