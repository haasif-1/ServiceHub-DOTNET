using MySqlConnector;

namespace ServiceHub.Web.Data;

public sealed class MySqlConnectionFactory : IMySqlConnectionFactory
{
    private readonly string? _connectionString;

    public MySqlConnectionFactory(IConfiguration configuration)
    {
        _connectionString = configuration.GetConnectionString("DefaultConnection");
    }

    public MySqlConnection CreateConnection()
    {
        if (string.IsNullOrWhiteSpace(_connectionString))
        {
            throw new InvalidOperationException(
                "ConnectionStrings:DefaultConnection must be configured for provider services.");
        }

        return new MySqlConnection(_connectionString);
    }
}
