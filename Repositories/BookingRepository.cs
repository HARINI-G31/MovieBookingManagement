
using System;
using System.Collections.Generic;
using System.Data;
using Microsoft.Data.SqlClient;
using MovieTicketBookingSystem.Models;
using MovieTicketBookingSystem.Data;
using MovieTicketBookingSystem.Audit;

namespace MovieTicketBookingSystem.Repositories
{
    public class BookingRepository
    {
        private readonly AuditService auditService = new AuditService();

        // 1. BOOK A SEAT

        public async Task<bool> BookSeatAsync(Booking booking)
        {
            using SqlConnection con = DbConnection.Create();
            await con.OpenAsync();

            using SqlTransaction transaction =
                con.BeginTransaction(IsolationLevel.Serializable);

            try
            {
                // Get show details
                string showQuery = @"SELECT TotalSeats, TicketPrice, ShowDateTime
                                     FROM Shows
                                     WHERE ShowId = @ShowId";

                using SqlCommand showCmd =
                    new SqlCommand(showQuery, con, transaction);

                showCmd.Parameters.AddWithValue("@ShowId", booking.ShowId);

                int totalSeats;
                decimal ticketPrice;
                DateTime showDate;

                using (SqlDataReader reader =
                    await showCmd.ExecuteReaderAsync())
                {
                    if (!await reader.ReadAsync())
                    {
                        Console.WriteLine("Show not found.");
                        return false;
                    }

                    totalSeats = reader.GetInt32(0);
                    ticketPrice = reader.GetDecimal(1);
                    showDate = reader.GetDateTime(2);
                }

                if (showDate <= DateTime.Now)
                {
                    Console.WriteLine("Cannot book a past show.");
                    return false;
                }

                if (booking.SeatNumber < 1 ||
                    booking.SeatNumber > totalSeats)
                {
                    Console.WriteLine("Invalid seat number.");
                    return false;
                }

                // Check whether seat is already booked
                string checkQuery = @"SELECT COUNT(*)
                                      FROM Bookings WITH (UPDLOCK, HOLDLOCK)
                                      WHERE ShowId = @ShowId
                                      AND SeatNumber = @SeatNumber
                                      AND BookingStatus = 'Booked'";

                using SqlCommand checkCmd =
                    new SqlCommand(checkQuery, con, transaction);

                checkCmd.Parameters.AddWithValue("@ShowId", booking.ShowId);
                checkCmd.Parameters.AddWithValue("@SeatNumber", booking.SeatNumber);

                int count = (int)(await checkCmd.ExecuteScalarAsync() ?? 0);

                if (count > 0)
                {
                    Console.WriteLine("Seat is already booked.");
                    return false;
                }

                // Insert booking
                string insertQuery = @"INSERT INTO Bookings
                    (ShowId, CustomerName, SeatNumber,
                     BookingStatus, AmountPaid)
                    OUTPUT INSERTED.BookingId
                    VALUES
                    (@ShowId, @CustomerName, @SeatNumber,
                     'Booked', @AmountPaid)";

                using SqlCommand insertCmd =
                    new SqlCommand(insertQuery, con, transaction);

                insertCmd.Parameters.AddWithValue("@ShowId", booking.ShowId);
                insertCmd.Parameters.AddWithValue("@CustomerName", booking.CustomerName);
                insertCmd.Parameters.AddWithValue("@SeatNumber", booking.SeatNumber);
                insertCmd.Parameters.AddWithValue("@AmountPaid", ticketPrice);

                int bookingId =
                    (int)(await insertCmd.ExecuteScalarAsync() ?? 0);

                // Save audit log
                await auditService.AddLogAsync(
                    bookingId,
                    "BOOKED",
                    $"Seat {booking.SeatNumber} booked for Show {booking.ShowId}",
                    con,
                    transaction);

                transaction.Commit();

                Console.WriteLine($"Booking successful! Booking ID: {bookingId}");
                Console.WriteLine($"Amount Paid: Rs. {ticketPrice:F2}");

                return true;
            }
            catch (Exception ex)
            {
                transaction.Rollback();
                Console.WriteLine("Booking failed: " + ex.Message);
                return false;
            }
        }

        // 2. VIEW ALL BOOKINGS

        public async Task<List<Booking>> GetAllAsync()
        {
            List<Booking> bookings = new List<Booking>();

            string query = @"SELECT b.BookingId, b.ShowId,
                                    b.CustomerName, b.SeatNumber,
                                    b.BookingDate, b.BookingStatus,
                                    b.AmountPaid, m.Title
                             FROM Bookings b
                             INNER JOIN Shows s ON b.ShowId = s.ShowId
                             INNER JOIN Movies m ON s.MovieId = m.MovieId
                             ORDER BY b.BookingId";

            using SqlConnection con = DbConnection.Create();
            await con.OpenAsync();

            using SqlCommand cmd = new SqlCommand(query, con);
            using SqlDataReader reader = await cmd.ExecuteReaderAsync();

            while (await reader.ReadAsync())
            {
                bookings.Add(MapBooking(reader));
            }

            return bookings;
        }

        // 3. SEARCH BOOKING BY ID

        public async Task<Booking?> GetByIdAsync(int bookingId)
        {
            string query = @"SELECT b.BookingId, b.ShowId,
                                    b.CustomerName, b.SeatNumber,
                                    b.BookingDate, b.BookingStatus,
                                    b.AmountPaid, m.Title
                             FROM Bookings b
                             INNER JOIN Shows s ON b.ShowId = s.ShowId
                             INNER JOIN Movies m ON s.MovieId = m.MovieId
                             WHERE b.BookingId = @BookingId";

            using SqlConnection con = DbConnection.Create();
            await con.OpenAsync();

            using SqlCommand cmd = new SqlCommand(query, con);
            cmd.Parameters.AddWithValue("@BookingId", bookingId);

            using SqlDataReader reader = await cmd.ExecuteReaderAsync();

            if (await reader.ReadAsync())
            {
                return MapBooking(reader);
            }

            return null;
        }

        // 4. CANCEL BOOKING

        public async Task<bool> CancelBookingAsync(int bookingId)
        {
            using SqlConnection con = DbConnection.Create();
            await con.OpenAsync();

            using SqlTransaction transaction =
                con.BeginTransaction(IsolationLevel.Serializable);

            try
            {
                string selectQuery = @"SELECT ShowId, SeatNumber, BookingStatus
                                       FROM Bookings WITH (UPDLOCK, HOLDLOCK)
                                       WHERE BookingId = @BookingId";

                using SqlCommand selectCmd =
                    new SqlCommand(selectQuery, con, transaction);

                selectCmd.Parameters.AddWithValue("@BookingId", bookingId);

                int showId;
                int seatNumber;
                string status;

                using (SqlDataReader reader =
                    await selectCmd.ExecuteReaderAsync())
                {
                    if (!await reader.ReadAsync())
                    {
                        Console.WriteLine("Booking not found.");
                        return false;
                    }

                    showId = reader.GetInt32(0);
                    seatNumber = reader.GetInt32(1);
                    status = reader.GetString(2);
                }

                if (status == "Cancelled")
                {
                    Console.WriteLine("Booking is already cancelled.");
                    return false;
                }

                string updateQuery = @"UPDATE Bookings
                                       SET BookingStatus = 'Cancelled'
                                       WHERE BookingId = @BookingId";

                using SqlCommand updateCmd =
                    new SqlCommand(updateQuery, con, transaction);

                updateCmd.Parameters.AddWithValue("@BookingId", bookingId);

                await updateCmd.ExecuteNonQueryAsync();

                await auditService.AddLogAsync(
                    bookingId,
                    "CANCELLED",
                    $"Seat {seatNumber} cancelled for Show {showId}",
                    con,
                    transaction);

                transaction.Commit();

                Console.WriteLine("Booking cancelled successfully.");
                return true;
            }
            catch (Exception ex)
            {
                transaction.Rollback();
                Console.WriteLine("Cancellation failed: " + ex.Message);
                return false;
            }
        }

        // 5. CONVERT DATABASE ROW INTO BOOKING OBJECT

        private Booking MapBooking(SqlDataReader reader)
        {
            return new Booking
            {
                BookingId = reader.GetInt32(0),
                ShowId = reader.GetInt32(1),
                CustomerName = reader.GetString(2),
                SeatNumber = reader.GetInt32(3),
                BookingDate = reader.GetDateTime(4),
                BookingStatus = reader.GetString(5),
                AmountPaid = reader.GetDecimal(6),
                MovieTitle = reader.GetString(7)
            };
        }
    }
}
