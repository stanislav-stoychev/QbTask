using System.Data.Common;
using Backend.Infrastructure.Configuration;
using Backend.Infrastructure.Interfaces;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Options;

namespace Backend.Infrastructure.Services;

public class SqliteDbManager(
    IOptionsMonitor<DatabaseConfiguration> config
) : IDbManager
{
    public DbConnection GetConnection()
    {
        var connection = new SqliteConnection(config.CurrentValue.ConnectionString);
        connection.Open();
            
        return connection;
    }
}
