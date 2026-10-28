using MovieTicketBookingSystem.Models;
using MovieTicketBookingSystem.Repositories;

namespace MovieTicketBookingSystem.Services;

public class BookingService
{
    private readonly BookingRepository _bookings;

    public BookingService(BookingRepository bookings) => _bookings = bookings;

    public Task<int> BookTicketAsync(int showId, string customerName, int seatNumber)
    {
        if (showId <= 0)
            throw new ArgumentException("Select a valid show.");
        if (string.IsNullOrWhiteSpace(customerName))
            throw new ArgumentException("Customer name is required.");

        // Seat range, availability and the insert are checked inside the repository
        // transaction so two customers cannot take the same seat.
        return _bookings.BookSeatAsync(showId, customerName.Trim(), seatNumber);
    }

    public Task<List<Booking>> GetAllBookingsAsync() => _bookings.GetAllAsync();

    public Task<Booking?> GetBookingAsync(int bookingId) => _bookings.GetByIdAsync(bookingId);

    public Task CancelBookingAsync(int bookingId)
    {
        if (bookingId <= 0)
            throw new ArgumentException("Enter a valid booking ID.");
        return _bookings.CancelAsync(bookingId);
    }

    public Task<(int Booked, int Cancelled, decimal Revenue)> GetSummaryAsync()
        => _bookings.GetSummaryAsync();

    public Task<List<(string Title, int Booked, int Cancelled, decimal Revenue)>> GetMovieWiseAsync()
        => _bookings.GetMovieWiseAsync();
}
