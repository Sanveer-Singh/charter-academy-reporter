namespace Charter.Reporter.Infrastructure.Data.Entities;

public class ExportLog
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string RequestedByUserId { get; set; } = string.Empty;
    public string FileName { get; set; } = string.Empty;
    public string FilterJson { get; set; } = string.Empty;
    public int RowCount { get; set; }
    public DateTime CreatedUtc { get; set; } = DateTime.UtcNow;
}


