namespace Charter.Reporter.Shared.Config;

public class MariaDbSettings
{
    public string Host { get; set; } = string.Empty;
    public int Port { get; set; } = 3306;
    public string Database { get; set; } = string.Empty;
    public string Username { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string? TablePrefix { get; set; }
}
