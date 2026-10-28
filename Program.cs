
using System;
using MovieTicketBookingSystem.Models;
using MovieTicketBookingSystem.Services;

namespace MovieTicketBookingSystem
{
    public class Program
    {
        public static async Task Main(string[] args)
        {
            BookingService bookingService = new BookingService();

            bool running = true;

            while (running)
            {
                Console.WriteLine("\n===== MOVIE TICKET BOOKING =====");
                Console.WriteLine("1. Book Ticket");
                Console.WriteLine("2. View All Bookings");
                Console.WriteLine("3. Search Booking by ID");
                Console.WriteLine("4. Cancel Booking");
                Console.WriteLine("5. Exit");
                Console.Write("Enter your choice: ");

                string choice = Console.ReadLine() ?? "";

                try
                {
                    switch (choice)
                    {
                        case "1":
                            Console.Write("Enter Show ID: ");
                            int showId = int.Parse(Console.ReadLine() ?? "0");

                            Console.Write("Enter Customer Name: ");
                            string customerName = Console.ReadLine() ?? "";

                            Console.Write("Enter Seat Number: ");
                            int seatNumber = int.Parse(Console.ReadLine() ?? "0");

                            Booking booking = new Booking
                            {
                                ShowId = showId,
                                CustomerName = customerName,
                                SeatNumber = seatNumber
                            };

                            await bookingService.BookTicketAsync(booking);
                            break;

                        case "2":
                            List<Booking> bookings =
                                await bookingService.GetAllBookingsAsync();

                            Console.WriteLine("\n===== ALL BOOKINGS =====");

                            if (bookings.Count == 0)
                            {
                                Console.WriteLine("No bookings found.");
                            }

                            foreach (Booking b in bookings)
                            {
                                DisplayBooking(b);
                            }
                            break;

                        case "3":
                            Console.Write("Enter Booking ID: ");
                            int searchId = int.Parse(Console.ReadLine() ?? "0");

                            Booking? result =
                                await bookingService.GetBookingByIdAsync(searchId);

                            if (result != null)
                            {
                                DisplayBooking(result);
                            }
                            else
                            {
                                Console.WriteLine("Booking not found.");
                            }
                            break;

                        case "4":
                            Console.Write("Enter Booking ID to cancel: ");
                            int cancelId = int.Parse(Console.ReadLine() ?? "0");

                            await bookingService.CancelBookingAsync(cancelId);
                            break;

                        case "5":
                            running = false;
                            Console.WriteLine("Exiting application...");
                            break;

                        default:
                            Console.WriteLine("Invalid choice.");
                            break;
                    }
                }
                catch (FormatException)
                {
                    Console.WriteLine("Please enter a valid number.");
                }
                catch (Exception ex)
                {
                    Console.WriteLine("Error: " + ex.Message);
                }
            }
        }

        // DISPLAY BOOKING DETAILS

        public static void DisplayBooking(Booking booking)
        {
            Console.WriteLine("-----------------------------");
            Console.WriteLine($"Booking ID: {booking.BookingId}");
            Console.WriteLine($"Movie: {booking.MovieTitle}");
            Console.WriteLine($"Show ID: {booking.ShowId}");
            Console.WriteLine($"Customer: {booking.CustomerName}");
            Console.WriteLine($"Seat Number: {booking.SeatNumber}");
            Console.WriteLine($"Booking Date: {booking.BookingDate}");
            Console.WriteLine($"Status: {booking.BookingStatus}");
            Console.WriteLine($"Amount: Rs. {booking.AmountPaid:F2}");
            Console.WriteLine("-----------------------------");
        }
    }
}
