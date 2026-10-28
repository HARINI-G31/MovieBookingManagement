using MovieTicketBookingSystem.Models;
using MovieTicketBookingSystem.Repositories;

namespace MovieTicketBookingSystem.Services;

public class ShowService
{
    private readonly ShowRepository _shows;
    private readonly MovieRepository _movies;

    public ShowService(ShowRepository shows, MovieRepository movies)
    {
        _shows = shows;
        _movies = movies;
    }

    public async Task<int> CreateShowAsync(Show show)
    {
        // Rule 1: a movie must exist before a show can be created.
        if (await _movies.GetByIdAsync(show.MovieId) is null)
            throw new InvalidOperationException($"Movie {show.MovieId} does not exist.");

        // Rule 2: a show cannot be scheduled in the past.
        if (show.ShowDateTime <= DateTime.Now)
            throw new ArgumentException("A show cannot be scheduled in the past.");

        // Rule 3: seats and price must be greater than zero.
        if (show.TotalSeats <= 0)
            throw new ArgumentOutOfRangeException(nameof(show.TotalSeats),
                "Total seats must be greater than zero.");
        if (show.TicketPrice <= 0)
            throw new ArgumentOutOfRangeException(nameof(show.TicketPrice),
                "Ticket price must be greater than zero.");

        return await _shows.AddAsync(show);
    }

    public Task<List<Show>> GetAllShowsAsync() => _shows.GetAllAsync();

    public Task<List<Show>> GetShowsByMovieAsync(int movieId) => _shows.GetByMovieAsync(movieId);

    public async Task<(Show Show, List<int> BookedSeats)> GetSeatMapAsync(int showId)
    {
        Show show = await _shows.GetByIdAsync(showId)
            ?? throw new InvalidOperationException($"Show {showId} was not found.");
        List<int> booked = await _shows.GetBookedSeatsAsync(showId);
        return (show, booked);
    }
}
