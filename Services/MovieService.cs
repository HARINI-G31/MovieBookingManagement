using MovieTicketBookingSystem.Models;
using MovieTicketBookingSystem.Repositories;

namespace MovieTicketBookingSystem.Services;

public class MovieService
{
    private readonly MovieRepository _movies;

    public MovieService(MovieRepository movies) => _movies = movies;

    public Task<int> AddMovieAsync(Movie movie)
    {
        Validate(movie);
        return _movies.AddAsync(movie);
    }

    public Task<List<Movie>> GetAllMoviesAsync() => _movies.GetAllAsync();

    public Task<Movie?> GetMovieAsync(int movieId) => _movies.GetByIdAsync(movieId);

    public async Task UpdateMovieAsync(Movie movie)
    {
        Validate(movie);
        if (!await _movies.UpdateAsync(movie))
            throw new InvalidOperationException($"Movie {movie.MovieId} was not found.");
    }

    // Business rule 8: a movie that still has shows (and therefore possibly bookings)
    // cannot be deleted until those shows are handled.
    public async Task DeleteMovieAsync(int movieId)
    {
        if (await _movies.GetByIdAsync(movieId) is null)
            throw new InvalidOperationException($"Movie {movieId} was not found.");

        if (await _movies.HasShowsAsync(movieId))
            throw new InvalidOperationException(
                "This movie has scheduled shows. Remove its shows and bookings first.");

        await _movies.DeleteAsync(movieId);
    }

    private static void Validate(Movie movie)
    {
        if (string.IsNullOrWhiteSpace(movie.Title))
            throw new ArgumentException("Title is required.");
        if (string.IsNullOrWhiteSpace(movie.Genre))
            throw new ArgumentException("Genre is required.");
        if (string.IsNullOrWhiteSpace(movie.Language))
            throw new ArgumentException("Language is required.");
        if (movie.DurationMinutes <= 0)
            throw new ArgumentOutOfRangeException(nameof(movie.DurationMinutes),
                "Duration must be greater than zero.");
    }
}
