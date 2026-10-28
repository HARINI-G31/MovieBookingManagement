using System.Data;
using Microsoft.Data.SqlClient;
using MovieTicketBookingSystem.Data;
using MovieTicketBookingSystem.Models;

namespace MovieTicketBookingSystem.Repositories;

public class BookingRepository
{
    /// <summary>
    /// Books one seat. All steps run in ONE transaction:
    ///   1. lock the show row and read capacity + price
    ///   2. validate the seat number
    ///   3. make sure the seat has no active booking
    ///   4. insert the booking
    /// then COMMIT. Any failure rolls everything back.
    /// </summary>
    public async Task<int> BookSeatAsync(int showId, string customerName, int seatNumber)
    {
        var (connection, transaction) = await DbConnection.BeginAsync();
        await using (connection)
        await using (transaction)
        {
            try
            {
                // Step 1: lock the show row so concurrent bookings for this show queue up.
                int totalSeats;
                decimal price;
                DateTime showTime;
                const string showSql = """
                    SELECT TotalSeats, TicketPrice, ShowDateTime
                    FROM Shows WITH (UPDLOCK, HOLDLOCK, ROWLOCK)
                    WHERE ShowId = @ShowId;
                    """;
                await using (SqlCommand command = new(showSql, connection, transaction))
                {
                    command.Parameters.Add("@ShowId", SqlDbType.Int).Value = showId;
                    await using SqlDataReader reader = await command.ExecuteReaderAsync();
                    if (!await reader.ReadAsync())
                        throw new InvalidOperationException($"Show {showId} was not found.");
                    totalSeats = reader.GetInt32(0);
                    price      = reader.GetDecimal(1);
                    showTime   = reader.GetDateTime(2);
                }

                if (showTime <= DateTime.Now)
                    throw new InvalidOperationException("This show has already started or finished.");

                // Step 2: rule 5 - seat must be within capacity.
                if (seatNumber < 1 || seatNumber > totalSeats)
                    throw new ArgumentOutOfRangeException(nameof(seatNumber),
                        $"Seat number must be between 1 and {totalSeats}.");

                // Step 3: rule 4 - no two active bookings for the same seat.
                const string checkSql = """
                    SELECT COUNT(*) FROM Bookings
                    WHERE ShowId = @ShowId AND SeatNumber = @Seat AND BookingStatus = 'Booked';
                    """;
                await using (SqlCommand command = new(checkSql, connection, transaction))
                {
                    command.Parameters.Add("@ShowId", SqlDbType.Int).Value = showId;
                    command.Parameters.Add("@Seat", SqlDbType.Int).Value = seatNumber;
                    if (Convert.ToInt32(await command.ExecuteScalarAsync()) > 0)
                        throw new InvalidOperationException($"Seat {seatNumber} is already booked.");
                }

                // Step 4: insert the booking, copying the price at booking time.
                const string insertSql = """
                    INSERT INTO Bookings (ShowId, CustomerName, SeatNumber, BookingDate, BookingStatus, AmountPaid)
                    VALUES (@ShowId, @Customer, @Seat, SYSDATETIME(), 'Booked', @Amount);
                    SELECT CAST(SCOPE_IDENTITY() AS INT);
                    """;
                int bookingId;
                await using (SqlCommand command = new(insertSql, connection, transaction))
                {
                    command.Parameters.Add("@ShowId", SqlDbType.Int).Value = showId;
                    command.Parameters.Add("@Customer", SqlDbType.VarChar, 100).Value = customerName;
                    command.Parameters.Add("@Seat", SqlDbType.Int).Value = seatNumber;
                    SqlParameter amount = command.Parameters.Add("@Amount", SqlDbType.Decimal);
                    amount.Precision = 10;
                    amount.Scale = 2;
                    amount.Value = price;
                    bookingId = Convert.ToInt32(await command.ExecuteScalarAsync());
                }

                await transaction.CommitAsync();   // commit the whole booking
                return bookingId;
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
        }
    }

    public async Task<List<Booking>> GetAllAsync()
    {
        const string sql = """
            SELECT b.BookingId, b.ShowId, b.CustomerName, b.SeatNumber, b.BookingDate,
                   b.BookingStatus, b.AmountPaid, m.Title
            FROM Bookings AS b
            INNER JOIN Shows  AS s ON s.ShowId  = b.ShowId
            INNER JOIN Movies AS m ON m.MovieId = s.MovieId
            ORDER BY b.BookingId;
            """;

        List<Booking> bookings = [];
        await using SqlConnection connection = DbConnection.Create();
        await using SqlCommand command = new(sql, connection);
        await connection.OpenAsync();
        await using SqlDataReader reader = await command.ExecuteReaderAsync();
        while (await reader.ReadAsync())
            bookings.Add(Map(reader));
        return bookings;
    }

    public async Task<Booking?> GetByIdAsync(int bookingId)
    {
        const string sql = """
            SELECT b.BookingId, b.ShowId, b.CustomerName, b.SeatNumber, b.BookingDate,
                   b.BookingStatus, b.AmountPaid, m.Title
            FROM Bookings AS b
            INNER JOIN Shows  AS s ON s.ShowId  = b.ShowId
            INNER JOIN Movies AS m ON m.MovieId = s.MovieId
            WHERE b.BookingId = @BookingId;
            """;

        await using SqlConnection connection = DbConnection.Create();
        await using SqlCommand command = new(sql, connection);
        command.Parameters.Add("@BookingId", SqlDbType.Int).Value = bookingId;
        await connection.OpenAsync();
        await using SqlDataReader reader = await command.ExecuteReaderAsync();
        return await reader.ReadAsync() ? Map(reader) : null;
    }

    /// <summary>
    /// Cancels a booking in one transaction: lock the row, reject if already cancelled,
    /// flip the status, COMMIT. The filtered unique index only covers 'Booked' rows, so
    /// the seat becomes available again immediately.
    /// </summary>
    public async Task CancelAsync(int bookingId)
    {
        var (connection, transaction) = await DbConnection.BeginAsync();
        await using (connection)
        await using (transaction)
        {
            try
            {
                string status;
                const string readSql = """
                    SELECT BookingStatus FROM Bookings WITH (UPDLOCK, ROWLOCK)
                    WHERE BookingId = @BookingId;
                    """;
                await using (SqlCommand command = new(readSql, connection, transaction))
                {
                    command.Parameters.Add("@BookingId", SqlDbType.Int).Value = bookingId;
                    object? result = await command.ExecuteScalarAsync();
                    if (result is null)
                        throw new InvalidOperationException($"Booking {bookingId} was not found.");
                    status = (string)result;
                }

                // Rule 6: a cancelled booking cannot be cancelled again.
                if (status == "Cancelled")
                    throw new InvalidOperationException("This booking is already cancelled.");

                const string updateSql = """
                    UPDATE Bookings SET BookingStatus = 'Cancelled'
                    WHERE BookingId = @BookingId;
                    """;
                await using (SqlCommand command = new(updateSql, connection, transaction))
                {
                    command.Parameters.Add("@BookingId", SqlDbType.Int).Value = bookingId;
                    await command.ExecuteNonQueryAsync();
                }

                await transaction.CommitAsync();   // commit the cancellation
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
        }
    }

    // ---------- Reports ----------

    public async Task<(int Booked, int Cancelled, decimal Revenue)> GetSummaryAsync()
    {
        const string sql = """
            SELECT
                COALESCE(SUM(CASE WHEN BookingStatus = 'Booked'    THEN 1 ELSE 0 END), 0),
                COALESCE(SUM(CASE WHEN BookingStatus = 'Cancelled' THEN 1 ELSE 0 END), 0),
                COALESCE(SUM(CASE WHEN BookingStatus = 'Booked'    THEN AmountPaid ELSE 0 END), 0)
            FROM Bookings;
            """;

        await using SqlConnection connection = DbConnection.Create();
        await using SqlCommand command = new(sql, connection);
        await connection.OpenAsync();
        await using SqlDataReader reader = await command.ExecuteReaderAsync();
        await reader.ReadAsync();
        return (reader.GetInt32(0), reader.GetInt32(1), reader.GetDecimal(2));
    }

    public async Task<List<(string Title, int Booked, int Cancelled, decimal Revenue)>> GetMovieWiseAsync()
    {
        const string sql = """
            SELECT m.Title,
                   COALESCE(SUM(CASE WHEN b.BookingStatus = 'Booked'    THEN 1 ELSE 0 END), 0) AS Booked,
                   COALESCE(SUM(CASE WHEN b.BookingStatus = 'Cancelled' THEN 1 ELSE 0 END), 0) AS Cancelled,
                   COALESCE(SUM(CASE WHEN b.BookingStatus = 'Booked'    THEN b.AmountPaid ELSE 0 END), 0) AS Revenue
            FROM Movies AS m
            LEFT JOIN Shows    AS s ON s.MovieId = m.MovieId
            LEFT JOIN Bookings AS b ON b.ShowId  = s.ShowId
            GROUP BY m.MovieId, m.Title
            ORDER BY Revenue DESC, m.Title;
            """;

        List<(string, int, int, decimal)> rows = [];
        await using SqlConnection connection = DbConnection.Create();
        await using SqlCommand command = new(sql, connection);
        await connection.OpenAsync();
        await using SqlDataReader reader = await command.ExecuteReaderAsync();
        while (await reader.ReadAsync())
            rows.Add((reader.GetString(0), reader.GetInt32(1), reader.GetInt32(2), reader.GetDecimal(3)));
        return rows;
    }

    private static Booking Map(SqlDataReader reader) => new()
    {
        BookingId     = reader.GetInt32(reader.GetOrdinal("BookingId")),
        ShowId        = reader.GetInt32(reader.GetOrdinal("ShowId")),
        CustomerName  = reader.GetString(reader.GetOrdinal("CustomerName")),
        SeatNumber    = reader.GetInt32(reader.GetOrdinal("SeatNumber")),
        BookingDate   = reader.GetDateTime(reader.GetOrdinal("BookingDate")),
        BookingStatus = reader.GetString(reader.GetOrdinal("BookingStatus")),
        AmountPaid    = reader.GetDecimal(reader.GetOrdinal("AmountPaid")),
        MovieTitle    = reader.GetString(reader.GetOrdinal("Title"))
    };
}
