namespace Charter.ReporterApp.Domain.ValueObjects;

/// <summary>
/// Date range value object for filtering and reporting
/// </summary>
public record DateRange
{
    public DateTime StartDate { get; }
    public DateTime EndDate { get; }

    public DateRange(DateTime startDate, DateTime endDate)
    {
        if (startDate > endDate)
            throw new ArgumentException("Start date cannot be after end date");

        StartDate = startDate.Date;
        EndDate = endDate.Date.AddDays(1).AddTicks(-1); // End of day
    }

    public int Days => (EndDate.Date - StartDate.Date).Days + 1;

    public bool Contains(DateTime date) => date >= StartDate && date <= EndDate;

    public static DateRange LastNDays(int days)
    {
        var endDate = DateTime.Today;
        var startDate = endDate.AddDays(-days + 1);
        return new DateRange(startDate, endDate);
    }

    public static DateRange ThisMonth()
    {
        var today = DateTime.Today;
        var startDate = new DateTime(today.Year, today.Month, 1);
        var endDate = startDate.AddMonths(1).AddDays(-1);
        return new DateRange(startDate, endDate);
    }

    public static DateRange LastMonth()
    {
        var today = DateTime.Today;
        var startDate = new DateTime(today.Year, today.Month, 1).AddMonths(-1);
        var endDate = startDate.AddMonths(1).AddDays(-1);
        return new DateRange(startDate, endDate);
    }

    public static DateRange ThisYear()
    {
        var today = DateTime.Today;
        var startDate = new DateTime(today.Year, 1, 1);
        var endDate = new DateTime(today.Year, 12, 31);
        return new DateRange(startDate, endDate);
    }

    public override string ToString() => $"{StartDate:yyyy-MM-dd} to {EndDate:yyyy-MM-dd}";
}