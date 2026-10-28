
namespace MovieTicketBookingSystem.Models
{
    public class Booking
    {
        public int BookingId { get; set; }
        public int ShowId { get; set; }
        public string CustomerName { get; set; } = string.Empty;
        public int SeatNumber { get; set; }
        public DateTime BookingDate { get; set; }
        public string BookingStatus { get; set; } = "Booked";
        public decimal AmountPaid { get; set; }
        public string? MovieTitle { get; set; }
    }
}
