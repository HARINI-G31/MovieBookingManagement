using System.Data;
using Microsoft.Data.SqlClient;
using MovieTicketBookingSystem.Data;
using MovieTicketBookingSystem.Models;

namespace MovieTicketBookingSystem.Repositories;

public class MovieRepository
{
    public async Task<int> AddAsync(Movie movie)
    {
        const string sql = """
            INSERT INTO Movies (Title, Genre, DurationMinutes, [Language])
            VALUES (@Title, @Genre, @Duration, @Language);
            SELECT CAST(SCOPE_IDENTITY() AS INT);
            """;

        var (connection, transaction) = await DbConnection.BeginAsync();
        await using (connection)
        await using (transaction)
        {
            try
            {
                await using SqlCommand command = new(sql, connection, transaction);
                command.Parameters.Add("@Title", SqlDbType.VarChar, 100).Value = movie.Title;
                command.Parameters.Add("@Genre", SqlDbType.VarChar, 50).Value = movie.Genre;
                command.Parameters.Add("@Duration", SqlDbType.Int).Value = movie.DurationMinutes;
                command.Parameters.Add("@Language", SqlDbType.VarChar, 30).Value = movie.Language;

                int id = Convert.ToInt32(await command.ExecuteScalarAsync());
                await transaction.CommitAsync();   // commit this step
                return id;
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
        }
    }

    public async Task<List<Movie>> GetAllAsync()
    {
        const string sql = """
            SELECT MovieId, Title, Genre, DurationMinutes, [Language]
            FROM Movies ORDER BY MovieId;
            """;

        List<Movie> movies = [];
        await using SqlConnection connection = DbConnection.Create();
        await using SqlCommand command = new(sql, connection);
        await connection.OpenAsync();
        await using SqlDataReader reader = await command.ExecuteReaderAsync();
        while (await reader.ReadAsync())
            movies.Add(Map(reader));
        return movies;
    }

    public async Task<Movie?> GetByIdAsync(int movieId)
    {
        const string sql = """
            SELECT MovieId, Title, Genre, DurationMinutes, [Language]
            FROM Movies WHERE MovieId = @MovieId;
            """;

        await using SqlConnection connection = DbConnection.Create();
        await using SqlCommand command = new(sql, connection);
        command.Parameters.Add("@MovieId", SqlDbType.Int).Value = movieId;
        await connection.OpenAsync();
        await using SqlDataReader reader = await command.ExecuteReaderAsync();
        return await reader.ReadAsync() ? Map(reader) : null;
    }

    public async Task<bool> UpdateAsync(Movie movie)
    {
        const string sql = """
            UPDATE Movies
            SET Title = @Title, Genre = @Genre,
                DurationMinutes = @Duration, [Language] = @Language
            WHERE MovieId = @MovieId;
            """;

        var (connection, transaction) = await DbConnection.BeginAsync();
        await using (connection)
        await using (transaction)
        {
            try
            {
                await using SqlCommand command = new(sql, connection, transaction);
                command.Parameters.Add("@MovieId", SqlDbType.Int).Value = movie.MovieId;
                command.Parameters.Add("@Title", SqlDbType.VarChar, 100).Value = movie.Title;
                command.Parameters.Add("@Genre", SqlDbType.VarChar, 50).Value = movie.Genre;
                command.Parameters.Add("@Duration", SqlDbType.Int).Value = movie.DurationMinutes;
                command.Parameters.Add("@Language", SqlDbType.VarChar, 30).Value = movie.Language;

                bool updated = await command.ExecuteNonQueryAsync() > 0;
                await transaction.CommitAsync();   // commit this step
                return updated;
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
        }
    }

    public async Task<bool> DeleteAsync(int movieId)
    {
        const string sql = "DELETE FROM Movies WHERE MovieId = @MovieId;";

        var (connection, transaction) = await DbConnection.BeginAsync();
        await using (connection)
        await using (transaction)
        {
            try
            {
                await using SqlCommand command = new(sql, connection, transaction);
                command.Parameters.Add("@MovieId", SqlDbType.Int).Value = movieId;

                bool deleted = await command.ExecuteNonQueryAsync() > 0;
                await transaction.CommitAsync();   // commit this step
                return deleted;
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
        }
    }

    public async Task<bool> HasShowsAsync(int movieId)
    {
        const string sql = "SELECT CASE WHEN EXISTS (SELECT 1 FROM Shows WHERE MovieId = @MovieId) THEN 1 ELSE 0 END;";

        await using SqlConnection connection = DbConnection.Create();
        await using SqlCommand command = new(sql, connection);
        command.Parameters.Add("@MovieId", SqlDbType.Int).Value = movieId;
        await connection.OpenAsync();
        return Convert.ToInt32(await command.ExecuteScalarAsync()) == 1;
    }

    private static Movie Map(SqlDataReader reader) => new()
    {
        MovieId         = reader.GetInt32(reader.GetOrdinal("MovieId")),
        Title           = reader.GetString(reader.GetOrdinal("Title")),
        Genre           = reader.GetString(reader.GetOrdinal("Genre")),
        DurationMinutes = reader.GetInt32(reader.GetOrdinal("DurationMinutes")),
        Language        = reader.GetString(reader.GetOrdinal("Language"))
    };
}
