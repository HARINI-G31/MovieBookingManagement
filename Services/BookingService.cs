
using MovieTicketBookingSystem.Models;
using MovieTicketBookingSystem.Repositories;

namespace MovieTicketBookingSystem.Services
{
    public class BookingService
    {
        private readonly BookingRepository bookingRepository =
            new BookingRepository();

        // BOOK TICKET

        public async Task<bool> BookTicketAsync(Booking booking)
        {
            if (booking.ShowId <= 0)
            {
                Console.WriteLine("Invalid Show ID.");
                return false;
            }

            if (string.IsNullOrWhiteSpace(booking.CustomerName))
            {
                Console.WriteLine("Customer name cannot be empty.");
                return false;
            }

            if (booking.SeatNumber <= 0)
            {
                Console.WriteLine("Invalid seat number.");
                return false;
            }

            return await bookingRepository.BookSeatAsync(booking);
        }

        // VIEW ALL BOOKINGS

        public async Task<List<Booking>> GetAllBookingsAsync()
        {
            return await bookingRepository.GetAllAsync();
        }

        // SEARCH BOOKING

        public async Task<Booking?> GetBookingByIdAsync(int bookingId)
        {
            if (bookingId <= 0)
            {
                Console.WriteLine("Invalid Booking ID.");
                return null;
            }

            return await bookingRepository.GetByIdAsync(bookingId);
        }

        // CANCEL BOOKING

        public async Task<bool> CancelBookingAsync(int bookingId)
        {
            if (bookingId <= 0)
            {
                Console.WriteLine("Invalid Booking ID.");
                return false;
            }

            return await bookingRepository.CancelBookingAsync(bookingId);
        }
    }
}
