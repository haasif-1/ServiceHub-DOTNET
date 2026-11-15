using MySqlConnector;

namespace ServiceHub.Web.Data;

public interface IMySqlConnectionFactory
{
    MySqlConnection CreateConnection();
}
