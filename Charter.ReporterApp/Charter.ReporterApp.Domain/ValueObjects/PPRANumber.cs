namespace Charter.ReporterApp.Domain.ValueObjects;

/// <summary>
/// PPRA number value object with validation
/// </summary>
public record PPRANumber
{
    public string Value { get; }

    public PPRANumber(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new ArgumentException("PPRA number cannot be null or empty", nameof(value));

        if (!IsValidFormat(value))
            throw new ArgumentException("Invalid PPRA number format", nameof(value));

        Value = value.Trim().ToUpperInvariant();
    }

    private static bool IsValidFormat(string value)
    {
        // PPRA number validation logic
        // Example: PPRA-YYYY-NNNNN format
        if (value.Length < 10) return false;
        
        var parts = value.Split('-');
        if (parts.Length != 3) return false;
        
        return parts[0].Equals("PPRA", StringComparison.OrdinalIgnoreCase) &&
               parts[1].Length == 4 && int.TryParse(parts[1], out _) &&
               parts[2].Length >= 3 && int.TryParse(parts[2], out _);
    }

    public static implicit operator string(PPRANumber ppraNumber) => ppraNumber.Value;
    public static implicit operator PPRANumber(string value) => new(value);

    public override string ToString() => Value;
}