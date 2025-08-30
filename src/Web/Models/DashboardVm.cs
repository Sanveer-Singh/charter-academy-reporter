namespace Charter.Reporter.Web.Models;

using Charter.Reporter.Application.Services.Dashboard;

public class DashboardVm
{
	public decimal SalesTotal { get; set; }
	public int EnrollmentCount { get; set; }
	public int CompletionCount { get; set; }

	public DateTime? FromUtc { get; set; }
	public DateTime? ToUtc { get; set; }
	public string? SelectedPreset { get; set; }
	public int? SelectedCategoryId { get; set; }
	public IReadOnlyList<CourseCategory>? Categories { get; set; }
	public bool IsCharterAdmin { get; set; }
}
