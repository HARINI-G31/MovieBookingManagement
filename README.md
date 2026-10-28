# Movie Ticket Booking System

A C# / .NET 10 console application for managing movies, scheduling shows and booking cinema seats. It uses **SQL Server** with **ADO.NET** (`Microsoft.Data.SqlClient`), and is structured so the repositories can later be swapped for **Entity Framework Core**.

The structure and style follow the ADO.NET e-commerce learning project: models, repositories, services, parameterized queries, async data access and transactions.

## Features

| Module | What it does |
|---|---|
| Movie management | Add, view, update and delete movies |
| Show management | Schedule shows, list all shows, list shows by movie, view a seat map |
| Ticket booking | Book one seat per booking with seat validation |
| Cancellation | Cancel a booking; the seat becomes available again |
| Reports | Total revenue and movie-wise bookings |

## Requirements

- [.NET 10 SDK](https://dotnet.microsoft.com/download)
- SQL Server or SQL Server Express (default instance in the config is `.\SQLEXPRESS`)
- SSMS (or any tool that can run `.sql` files)

## Setup

1. **Create the database.** In SSMS, run the scripts in order:
   - `Database/01_schema.sql` — creates `MovieBookingDB` and the three tables
   - `Database/02_sample_data.sql` — optional sample movies, shows and bookings
2. **Check the connection string** in `appsettings.json`:
   ```json
   "MovieBookingDB": "Server=.\\SQLEXPRESS;Database=MovieBookingDB;Trusted_Connection=True;TrustServerCertificate=True;"
   ```
   Change `Server=` if your instance has a different name.
3. **Run the app:**
   ```bash
   dotnet restore
   dotnet run
   ```

## Database design

```
Movies (1) ─────< Shows (1) ─────< Bookings
```

| Table | Columns |
|---|---|
| `Movies` | MovieId, Title, Genre, DurationMinutes, Language |
| `Shows` | ShowId, MovieId (FK), ShowDateTime, TotalSeats, TicketPrice |
| `Bookings` | BookingId, ShowId (FK), CustomerName, SeatNumber, BookingDate, BookingStatus (`Booked`/`Cancelled`), AmountPaid |

One booking represents one seat. A **filtered unique index** on `Bookings (ShowId, SeatNumber) WHERE BookingStatus = 'Booked'` guarantees at the database level that a seat can have only one active booking, while cancelled rows are ignored, so a cancelled seat can be booked again.

## Transactions

Every write operation runs in its own `SqlTransaction` and is **committed at the end of the step**. If anything throws, the transaction is **rolled back** and nothing is saved.

| Operation | Steps inside the transaction |
|---|---|
| Add / update / delete movie | run the statement → `CommitAsync()` |
| Create show | insert the show → `CommitAsync()` |
| **Book ticket** | lock the show row (`UPDLOCK, HOLDLOCK`) → read capacity and price → validate seat range → check the seat has no active booking → insert booking → `CommitAsync()` |
| **Cancel booking** | lock the booking row → reject if already cancelled → set status to `Cancelled` → `CommitAsync()` |

Locking the show row makes concurrent booking attempts for the same show queue up, so two customers cannot take the same seat. The unique index is the final safety net.

## Business rules

1. A movie must exist before a show can be created.
2. A show cannot be scheduled in the past.
3. Total seats and ticket price must be greater than zero.
4. The same seat cannot have two active bookings for the same show.
5. A seat number must be between 1 and the show's total capacity.
6. A cancelled booking cannot be cancelled again.
7. Cancelling a booking makes its seat available again.
8. A movie that still has shows cannot be deleted.
9. Booking a seat uses a database transaction.

## Project structure

```
MovieTicketBookingSystem/
├── Program.cs              Console menu and user input
├── appsettings.json        Connection string
├── Models/                 Movie, Show, Booking
├── Repositories/           SQL + ADO.NET (MovieRepository, ShowRepository, BookingRepository)
├── Services/               Validation and business rules
├── Data/DbConnection.cs    Connection and transaction helper
└── Database/               01_schema.sql, 02_sample_data.sql
```

| Layer | Responsibility |
|---|---|
| `Program.cs` | Display menus, read input, call services |
| `Services` | Validate input and enforce business rules |
| `Repositories` | Own all SQL statements and transactions |
| `Data` | Create connections from `appsettings.json` |

## Menu

```
MOVIE MANAGEMENT      1 Add Movie    2 View All Movies   3 Update Movie       4 Delete Movie
SHOW MANAGEMENT       5 Create Show  6 View All Shows    7 View Shows by Movie 8 Check Available Seats
BOOKING MANAGEMENT    9 Book Ticket  10 View All Bookings 11 Search Booking by ID 12 Cancel Booking
REPORTS               13 View Total Revenue   14 View Movie-wise Bookings   15 Exit
```

## Development stages and git history

Each stage was committed separately:

| Stage | Commit message |
|---|---|
| 1 | Project scaffold, SQL schema and sample data |
| 2 | Models and ADO.NET connection helper |
| 3 | Movie CRUD (repository + service, transaction per write) |
| 4 | Show management, JOIN queries and seat map |
| 5 | Ticket booking with seat validation in a single transaction |
| 6 | Booking cancellation (transaction) and reporting queries |
| 7 | Console menu and README |

View them with `git log --oneline`.

## Next step: migrate to EF Core

Keep the same database and services; replace the repository implementations:

| ADO.NET | EF Core |
|---|---|
| `SqlConnection` | `DbContext` |
| `INSERT` with `SqlCommand` | `Add()` + `SaveChanges()` |
| `SqlDataReader` | `ToList()` |
| `SqlTransaction` | `BeginTransaction()` |
| SQL `JOIN` | Navigation properties |

## Ideas for enhancements

- Book several seats in one transaction
- Add a Customers table
- Show a seat map when booking
- Search movies by title or genre

### Booking Model

File: `Models/Booking.cs`

The Booking model represents one reserved seat for a movie show.

Properties:
- BookingId: Unique booking identifier
- ShowId: Foreign key referencing Shows
- CustomerName: Name of the customer
- SeatNumber: Selected seat
- BookingDate: Date and time of booking
- BookingStatus: Booked or Cancelled
- AmountPaid: Ticket price
- MovieTitle: Optional property used for display

Learning outcomes:
- C# classes and properties
- Nullable reference types
- Default property values
- Mapping C# models to SQL Server tables

Status: Completed

## Booking Management - ADO.NET

### Implemented Modules
- Booking Model
- Database Connection
- Booking Repository
- Booking Service
- Audit Service
- Booking Console Menu

### Features
1. Book a ticket for an existing show
2. Check seat availability
3. Prevent duplicate active seat bookings
4. Display all bookings
5. Search bookings by ID
6. Cancel existing bookings
7. Record booking and cancellation audit logs

### ADO.NET Concepts
- SqlConnection
- SqlCommand
- SqlDataReader
- ExecuteScalarAsync
- ExecuteReaderAsync
- ExecuteNonQueryAsync
- SQL Parameters
- SQL Transactions
- Commit and Rollback
- SQL JOIN

### Database Tables
- Movies (existing)
- Shows (existing)
- Bookings
- AuditLogs

### Next Stage
Movie and Show Management
```
