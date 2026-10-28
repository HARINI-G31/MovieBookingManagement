using System.Data;
using Microsoft.Data.SqlClient;
using MovieTicketBookingSystem.Data;
using MovieTicketBookingSystem.Models;

namespace MovieTicketBookingSystem.Repositories;

public class ShowRepository
{
    public async Task<int> AddAsync(Show show)
    {
        const string sql = """
            INSERT INTO Shows (MovieId, ShowDateTime, TotalSeats, TicketPrice)
            VALUES (@MovieId, @ShowDateTime, @TotalSeats, @TicketPrice);
            SELECT CAST(SCOPE_IDENTITY() AS INT);
            """;

        var (connection, transaction) = await DbConnection.BeginAsync();
        await using (connection)
        await using (transaction)
        {
            try
            {
                await using SqlCommand command = new(sql, connection, transaction);
                command.Parameters.Add("@MovieId", SqlDbType.Int).Value = show.MovieId;
                command.Parameters.Add("@ShowDateTime", SqlDbType.DateTime2).Value = show.ShowDateTime;
                command.Parameters.Add("@TotalSeats", SqlDbType.Int).Value = show.TotalSeats;
                SqlParameter price = command.Parameters.Add("@TicketPrice", SqlDbType.Decimal);
                price.Precision = 10;
                price.Scale = 2;
                price.Value = show.TicketPrice;

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

    public Task<List<Show>> GetAllAsync() => QueryAsync(null);

    public Task<List<Show>> GetByMovieAsync(int movieId) => QueryAsync(movieId);

    public async Task<Show?> GetByIdAsync(int showId)
    {
        const string sql = """
            SELECT s.ShowId, s.MovieId, s.ShowDateTime, s.TotalSeats, s.TicketPrice, m.Title
            FROM Shows AS s
            INNER JOIN Movies AS m ON m.MovieId = s.MovieId
            WHERE s.ShowId = @ShowId;
            """;

        await using SqlConnection connection = DbConnection.Create();
        await using SqlCommand command = new(sql, connection);
        command.Parameters.Add("@ShowId", SqlDbType.Int).Value = showId;
        await connection.OpenAsync();
        await using SqlDataReader reader = await command.ExecuteReaderAsync();
        return await reader.ReadAsync() ? Map(reader) : null;
    }

    /// <summary>Seat numbers that currently have an active ("Booked") booking.</summary>
    public async Task<List<int>> GetBookedSeatsAsync(int showId)
    {
        const string sql = """
            SELECT SeatNumber FROM Bookings
            WHERE ShowId = @ShowId AND BookingStatus = 'Booked'
            ORDER BY SeatNumber;
            """;

        List<int> seats = [];
        await using SqlConnection connection = DbConnection.Create();
        await using SqlCommand command = new(sql, connection);
        command.Parameters.Add("@ShowId", SqlDbType.Int).Value = showId;
        await connection.OpenAsync();
        await using SqlDataReader reader = await command.ExecuteReaderAsync();
        while (await reader.ReadAsync())
            seats.Add(reader.GetInt32(0));
        return seats;
    }

    private static async Task<List<Show>> QueryAsync(int? movieId)
    {
        string sql = """
            SELECT s.ShowId, s.MovieId, s.ShowDateTime, s.TotalSeats, s.TicketPrice, m.Title
            FROM Shows AS s
            INNER JOIN Movies AS m ON m.MovieId = s.MovieId
            """;
        if (movieId.HasValue)
            sql += " WHERE s.MovieId = @MovieId";
        sql += " ORDER BY s.ShowDateTime;";

        List<Show> shows = [];
        await using SqlConnection connection = DbConnection.Create();
        await using SqlCommand command = new(sql, connection);
        if (movieId.HasValue)
            command.Parameters.Add("@MovieId", SqlDbType.Int).Value = movieId.Value;
        await connection.OpenAsync();
        await using SqlDataReader reader = await command.ExecuteReaderAsync();
        while (await reader.ReadAsync())
            shows.Add(Map(reader));
        return shows;
    }

    private static Show Map(SqlDataReader reader) => new()
    {
        ShowId       = reader.GetInt32(reader.GetOrdinal("ShowId")),
        MovieId      = reader.GetInt32(reader.GetOrdinal("MovieId")),
        ShowDateTime = reader.GetDateTime(reader.GetOrdinal("ShowDateTime")),
        TotalSeats   = reader.GetInt32(reader.GetOrdinal("TotalSeats")),
        TicketPrice  = reader.GetDecimal(reader.GetOrdinal("TicketPrice")),
        MovieTitle   = reader.GetString(reader.GetOrdinal("Title"))
    };
}
