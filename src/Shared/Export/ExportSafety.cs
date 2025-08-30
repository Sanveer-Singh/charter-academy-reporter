namespace Charter.Reporter.Shared.Export;

public interface IExportSafetyService
{
    ExportDecision Evaluate(string datasetKey, string role, int estimatedRows, IReadOnlyList<string> requestedColumns);
}

public record ExportDecision(bool Allowed, IReadOnlyList<string> AllowedColumns, string? Reason);

public class ExportSafetyService : IExportSafetyService
{
    private readonly ExportOptions _options;
    private static readonly Dictionary<string, string[]> AllowLists = new(StringComparer.OrdinalIgnoreCase)
    {
        { "baseline", new [] { "column1", "column2" } }
    };

    public ExportSafetyService(ExportOptions options)
    {
        _options = options;
    }

    public ExportDecision Evaluate(string datasetKey, string role, int estimatedRows, IReadOnlyList<string> requestedColumns)
    {
        if (estimatedRows > _options.RowCap)
        {
            return new ExportDecision(false, Array.Empty<string>(), $"Row cap exceeded: {estimatedRows} > {_options.RowCap}");
        }

        if (!AllowLists.TryGetValue(datasetKey, out var allowed))
        {
            return new ExportDecision(false, Array.Empty<string>(), "Unknown dataset");
        }

        var filtered = requestedColumns.Where(c => allowed.Contains(c, StringComparer.OrdinalIgnoreCase)).ToArray();
        return new ExportDecision(true, filtered, null);
    }
}


