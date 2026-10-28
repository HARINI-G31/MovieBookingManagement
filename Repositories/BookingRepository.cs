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
