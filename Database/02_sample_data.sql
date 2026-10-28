-- Optional sample data. Run once after 01_schema.sql.
USE MovieBookingDB;
GO

INSERT INTO Movies (Title, Genre, DurationMinutes, [Language])
VALUES ('Leo',            'Action',   164, 'Tamil'),
       ('Premalu',        'Romance',  156, 'Malayalam'),
       ('Interstellar',   'Sci-Fi',   169, 'English');

INSERT INTO Shows (MovieId, ShowDateTime, TotalSeats, TicketPrice)
VALUES (1, DATEADD(DAY, 1, CAST(CAST(SYSDATETIME() AS DATE) AS DATETIME2)) + CAST('18:30' AS DATETIME2), 20, 250.00),
       (2, DATEADD(DAY, 1, CAST(CAST(SYSDATETIME() AS DATE) AS DATETIME2)) + CAST('21:00' AS DATETIME2), 30, 200.00),
       (3, DATEADD(DAY, 2, CAST(CAST(SYSDATETIME() AS DATE) AS DATETIME2)) + CAST('19:00' AS DATETIME2), 25, 300.00);

INSERT INTO Bookings (ShowId, CustomerName, SeatNumber, BookingStatus, AmountPaid)
VALUES (1, 'Arun Kumar', 5, 'Booked',    250.00),
       (1, 'Priya Devi', 6, 'Booked',    250.00),
       (2, 'Kavin Raj',  1, 'Cancelled', 200.00);
GO
