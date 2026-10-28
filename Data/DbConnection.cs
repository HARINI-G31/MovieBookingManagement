
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;

namespace MovieTicketBookingSystem.Data;

public static class DbConnection
{
    private static readonly string ConnectionString = LoadConnectionString();

    private static string LoadConnectionString()
    {
        IConfigurationRoot config = new ConfigurationBuilder()
            .SetBasePath(AppContext.BaseDirectory)
            .AddJsonFile("appsettings.json", optional: false)
            .Build();

        return config.GetConnectionString("MovieBookingDB")
            ?? throw new InvalidOperationException(
                "Connection string 'MovieBookingDB' is missing from appsettings.json.");
    }

    public static SqlConnection Create() => new(ConnectionString);

    public static async Task<(SqlConnection Connection, SqlTransaction Transaction)> BeginAsync()
    {
        SqlConnection connection = Create();
        await connection.OpenAsync();

        SqlTransaction transaction =
            (SqlTransaction)await connection.BeginTransactionAsync();

        return (connection, transaction);
    }
}
