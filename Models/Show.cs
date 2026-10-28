namespace MovieTicketBookingSystem.Models;

public class Show
{
    public int ShowId { get; set; }
    public int MovieId { get; set; }
    public DateTime ShowDateTime { get; set; }
    public int TotalSeats { get; set; }
    public decimal TicketPrice { get; set; }

    // Filled by JOIN queries for display only.
    public string? MovieTitle { get; set; }
}
