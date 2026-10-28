using MovieTicketBookingSystem.Models;
using MovieTicketBookingSystem.Repositories;
using MovieTicketBookingSystem.Services;

MovieRepository movieRepo = new();
ShowRepository showRepo = new();
BookingRepository bookingRepo = new();

MovieService movieService = new(movieRepo);
ShowService showService = new(showRepo, movieRepo);
BookingService bookingService = new(bookingRepo);

while (true)
{
    PrintMenu();
    Console.Write("Enter your choice: ");
    string? choice = Console.ReadLine()?.Trim();

    if (choice == "15")
    {
        Console.WriteLine("Goodbye!");
        break;
    }

    try
    {
        switch (choice)
        {
            case "1":  await AddMovie(); break;
            case "2":  await ViewAllMovies(); break;
            case "3":  await UpdateMovie(); break;
            case "4":  await DeleteMovie(); break;
            case "5":  await CreateShow(); break;
            case "6":  await ViewAllShows(); break;
            case "7":  await ViewShowsByMovie(); break;
            case "8":  await CheckAvailableSeats(); break;
            case "9":  await BookTicket(); break;
            case "10": await ViewAllBookings(); break;
            case "11": await SearchBooking(); break;
            case "12": await CancelBooking(); break;
            case "13": await ViewTotalRevenue(); break;
            case "14": await ViewMovieWiseBookings(); break;
            default:   Console.WriteLine("Invalid choice. Enter a number from 1 to 15."); break;
        }
    }
    catch (Exception ex)
    {
        // Validation errors and rolled-back transactions land here; nothing was saved.
        Console.WriteLine($"\nError: {ex.Message}");
    }

    Console.WriteLine("\nPress Enter to continue...");
    Console.ReadLine();
}

// ---------------- Menu ----------------

static void PrintMenu()
{
    Console.Clear();
    Console.WriteLine("""
        MOVIE TICKET BOOKING SYSTEM

        ================================
                MOVIE MANAGEMENT
        ================================
        1. Add Movie
        2. View All Movies
        3. Update Movie
        4. Delete Movie

        ================================
                 SHOW MANAGEMENT
        ================================
        5. Create Show
        6. View All Shows
        7. View Shows by Movie
        8. Check Available Seats

        ================================
                BOOKING MANAGEMENT
        ================================
        9. Book Ticket
        10. View All Bookings
        11. Search Booking by ID
        12. Cancel Booking

        ================================
                      REPORTS
        ================================
        13. View Total Revenue
        14. View Movie-wise Bookings
        15. Exit
        """);
}

// ---------------- Input helpers ----------------

static string ReadText(string prompt)
{
    Console.Write(prompt);
    return Console.ReadLine()?.Trim() ?? string.Empty;
}

static int ReadInt(string prompt)
{
    Console.Write(prompt);
    if (!int.TryParse(Console.ReadLine(), out int value))
        throw new FormatException("Please enter a whole number.");
    return value;
}

static decimal ReadDecimal(string prompt)
{
    Console.Write(prompt);
    if (!decimal.TryParse(Console.ReadLine(), out decimal value))
        throw new FormatException("Please enter a valid amount.");
    return value;
}

static DateTime ReadDateTime(string prompt)
{
    Console.Write(prompt);
    if (!DateTime.TryParse(Console.ReadLine(), out DateTime value))
        throw new FormatException("Please enter a valid date and time, e.g. 2026-11-05 18:30.");
    return value;
}

static void PrintMovies(List<Movie> movies)
{
    if (movies.Count == 0) { Console.WriteLine("No movies found."); return; }
    Console.WriteLine($"{"ID",-5}{"Title",-30}{"Genre",-15}{"Mins",-7}Language");
    foreach (Movie m in movies)
        Console.WriteLine($"{m.MovieId,-5}{m.Title,-30}{m.Genre,-15}{m.DurationMinutes,-7}{m.Language}");
}

static void PrintShows(List<Show> shows)
{
    if (shows.Count == 0) { Console.WriteLine("No shows found."); return; }
    Console.WriteLine($"{"ID",-5}{"Movie",-28}{"Date & Time",-20}{"Seats",-7}Price");
    foreach (Show s in shows)
        Console.WriteLine($"{s.ShowId,-5}{s.MovieTitle,-28}{s.ShowDateTime:yyyy-MM-dd HH:mm,-20}{s.TotalSeats,-7}{s.TicketPrice:0.00}");
}

static void PrintBookings(List<Booking> bookings)
{
    if (bookings.Count == 0) { Console.WriteLine("No bookings found."); return; }
    Console.WriteLine($"{"ID",-5}{"Movie",-24}{"Customer",-20}{"Seat",-6}{"Status",-11}Amount");
    foreach (Booking b in bookings)
        Console.WriteLine($"{b.BookingId,-5}{b.MovieTitle,-24}{b.CustomerName,-20}{b.SeatNumber,-6}{b.BookingStatus,-11}{b.AmountPaid:0.00}");
}

// ---------------- Movie management ----------------

async Task AddMovie()
{
    Movie movie = new()
    {
        Title = ReadText("Title: "),
        Genre = ReadText("Genre: "),
        DurationMinutes = ReadInt("Duration (minutes): "),
        Language = ReadText("Language: ")
    };
    int id = await movieService.AddMovieAsync(movie);
    Console.WriteLine($"Movie saved and committed. MovieId = {id}");
}

async Task ViewAllMovies() => PrintMovies(await movieService.GetAllMoviesAsync());

async Task UpdateMovie()
{
    int id = ReadInt("Movie ID to update: ");
    Movie movie = await movieService.GetMovieAsync(id)
        ?? throw new InvalidOperationException($"Movie {id} was not found.");

    Console.WriteLine("Press Enter to keep the current value.");
    string title = ReadText($"Title [{movie.Title}]: ");
    string genre = ReadText($"Genre [{movie.Genre}]: ");
    string duration = ReadText($"Duration [{movie.DurationMinutes}]: ");
    string language = ReadText($"Language [{movie.Language}]: ");

    if (title != "") movie.Title = title;
    if (genre != "") movie.Genre = genre;
    if (duration != "")
    {
        if (!int.TryParse(duration, out int mins))
            throw new FormatException("Duration must be a whole number.");
        movie.DurationMinutes = mins;
    }
    if (language != "") movie.Language = language;

    await movieService.UpdateMovieAsync(movie);
    Console.WriteLine("Movie updated and committed.");
}

async Task DeleteMovie()
{
    int id = ReadInt("Movie ID to delete: ");
    await movieService.DeleteMovieAsync(id);
    Console.WriteLine("Movie deleted and committed.");
}

// ---------------- Show management ----------------

async Task CreateShow()
{
    Show show = new()
    {
        MovieId = ReadInt("Movie ID: "),
        ShowDateTime = ReadDateTime("Show date & time (e.g. 2026-11-05 18:30): "),
        TotalSeats = ReadInt("Total seats: "),
        TicketPrice = ReadDecimal("Ticket price: ")
    };
    int id = await showService.CreateShowAsync(show);
    Console.WriteLine($"Show saved and committed. ShowId = {id}");
}

async Task ViewAllShows() => PrintShows(await showService.GetAllShowsAsync());

async Task ViewShowsByMovie()
{
    int movieId = ReadInt("Movie ID: ");
    PrintShows(await showService.GetShowsByMovieAsync(movieId));
}

async Task CheckAvailableSeats()
{
    int showId = ReadInt("Show ID: ");
    var (show, booked) = await showService.GetSeatMapAsync(showId);

    Console.WriteLine($"\n{show.MovieTitle} - {show.ShowDateTime:yyyy-MM-dd HH:mm}");
    Console.WriteLine($"Available: {show.TotalSeats - booked.Count} of {show.TotalSeats}\n");
    Console.WriteLine("            CINEMA SCREEN\n");

    HashSet<int> bookedSet = [.. booked];
    for (int seat = 1; seat <= show.TotalSeats; seat++)
    {
        Console.Write(bookedSet.Contains(seat) ? $"[{seat,2}X] " : $"[{seat,2} ] ");
        if (seat % 10 == 0) Console.WriteLine();
    }
    Console.WriteLine("\n\n[ n ] available   [nX] booked");
}

// ---------------- Booking management ----------------

async Task BookTicket()
{
    PrintMovies(await movieService.GetAllMoviesAsync());
    int movieId = ReadInt("\nMovie ID: ");

    List<Show> shows = await showService.GetShowsByMovieAsync(movieId);
    PrintShows(shows);
    if (shows.Count == 0) return;

    int showId = ReadInt("\nShow ID: ");
    var (show, booked) = await showService.GetSeatMapAsync(showId);
    Console.WriteLine($"Booked seats: {(booked.Count == 0 ? "none" : string.Join(", ", booked))}");

    int seat = ReadInt($"Seat number (1-{show.TotalSeats}): ");
    string customer = ReadText("Customer name: ");

    int bookingId = await bookingService.BookTicketAsync(showId, customer, seat);
    Console.WriteLine($"Booking confirmed and committed. BookingId = {bookingId}, amount = {show.TicketPrice:0.00}");
}

async Task ViewAllBookings() => PrintBookings(await bookingService.GetAllBookingsAsync());

async Task SearchBooking()
{
    int id = ReadInt("Booking ID: ");
    Booking? booking = await bookingService.GetBookingAsync(id);
    if (booking is null) { Console.WriteLine("Booking not found."); return; }
    PrintBookings([booking]);
}

async Task CancelBooking()
{
    int id = ReadInt("Booking ID to cancel: ");
    await bookingService.CancelBookingAsync(id);
    Console.WriteLine("Booking cancelled and committed. The seat is available again.");
}

// ---------------- Reports ----------------

async Task ViewTotalRevenue()
{
    var (booked, cancelled, revenue) = await bookingService.GetSummaryAsync();
    Console.WriteLine($"Active bookings    : {booked}");
    Console.WriteLine($"Cancelled bookings : {cancelled}");
    Console.WriteLine($"Total revenue      : {revenue:0.00}");
}

async Task ViewMovieWiseBookings()
{
    var rows = await bookingService.GetMovieWiseAsync();
    Console.WriteLine($"{"Movie",-30}{"Booked",-9}{"Cancelled",-11}Revenue");
    foreach (var r in rows)
        Console.WriteLine($"{r.Title,-30}{r.Booked,-9}{r.Cancelled,-11}{r.Revenue:0.00}");
}
