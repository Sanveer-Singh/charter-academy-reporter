using Charter.Reporter.Shared.Config;
using Microsoft.Extensions.Options;
using MySqlConnector;

namespace Charter.Reporter.Infrastructure.Data.MariaDb;

public interface IMariaDbConnectionFactory
{
	MySqlConnection CreateMoodleConnection();
	MySqlConnection CreateWooConnection();
}

public class MariaDbConnectionFactory : IMariaDbConnectionFactory
{
	private readonly MariaDbSettings _moodle;
	private readonly MariaDbSettings _woo;

	public MariaDbConnectionFactory(IOptionsSnapshot<MariaDbSettings> options)
	{
		// Using named options via DI registration
		_moodle = options.Get("Moodle");
		_woo = options.Get("Woo");
	}

	public MySqlConnection CreateMoodleConnection() => new MySqlConnection(BuildConnectionString(_moodle));
	public MySqlConnection CreateWooConnection() => new MySqlConnection(BuildConnectionString(_woo));

	private static string BuildConnectionString(MariaDbSettings s)
	{
		var cs = new MySqlConnectionStringBuilder
		{
			Server = s.Host,
			Port = (uint)s.Port,
			Database = s.Database,
			UserID = s.Username,
			Password = s.Password,
			SslMode = MySqlSslMode.Preferred,
			AllowUserVariables = true,
			DefaultCommandTimeout = 60
		};
		return cs.ConnectionString;
	}
}
