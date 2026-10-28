-- Stage 1: create database and the three tables.
-- Run in SSMS. Run CREATE DATABASE only once.
CREATE DATABASE MovieBookingDB;
GO
USE MovieBookingDB;
GO

CREATE TABLE Movies
(
    MovieId         INT IDENTITY(1,1) PRIMARY KEY,
    Title           VARCHAR(100) NOT NULL,
    Genre           VARCHAR(50)  NOT NULL,
    DurationMinutes INT          NOT NULL CHECK (DurationMinutes > 0),
    [Language]      VARCHAR(30)  NOT NULL
);

CREATE TABLE Shows
(
    ShowId       INT IDENTITY(1,1) PRIMARY KEY,
    MovieId      INT           NOT NULL,
    ShowDateTime DATETIME2     NOT NULL,
    TotalSeats   INT           NOT NULL CHECK (TotalSeats > 0),
    TicketPrice  DECIMAL(10,2) NOT NULL CHECK (TicketPrice > 0),
    CONSTRAINT FK_Shows_Movies FOREIGN KEY (MovieId) REFERENCES Movies(MovieId)
);

CREATE TABLE Bookings
(
    BookingId     INT IDENTITY(1,1) PRIMARY KEY,
    ShowId        INT           NOT NULL,
    CustomerName  VARCHAR(100)  NOT NULL,
    SeatNumber    INT           NOT NULL CHECK (SeatNumber > 0),
    BookingDate   DATETIME2     NOT NULL DEFAULT SYSDATETIME(),
    BookingStatus VARCHAR(20)   NOT NULL DEFAULT 'Booked',
    AmountPaid    DECIMAL(10,2) NOT NULL CHECK (AmountPaid >= 0),
    CONSTRAINT FK_Bookings_Shows FOREIGN KEY (ShowId) REFERENCES Shows(ShowId),
    CONSTRAINT CK_Bookings_Status CHECK (BookingStatus IN ('Booked', 'Cancelled'))
);

-- Business rule 4: the same seat cannot have two ACTIVE bookings for one show.
-- A filtered unique index is the database-level safety net behind the C# check.
CREATE UNIQUE INDEX UX_Bookings_ActiveSeat
    ON Bookings (ShowId, SeatNumber)
    WHERE BookingStatus = 'Booked';
GO
