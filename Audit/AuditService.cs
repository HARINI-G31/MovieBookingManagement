
using Microsoft.Data.SqlClient;

namespace MovieTicketBookingSystem.Audit
{
    public class AuditService
    {
        public async Task AddLogAsync(
            int bookingId,
            string action,
            string description,
            SqlConnection connection,
            SqlTransaction transaction)
        {
            string query = @"INSERT INTO AuditLogs
                             (BookingId, Action, Description)
                             VALUES
                             (@BookingId, @Action, @Description)";

            using SqlCommand cmd =
                new SqlCommand(query, connection, transaction);

            cmd.Parameters.AddWithValue("@BookingId", bookingId);
            cmd.Parameters.AddWithValue("@Action", action);
            cmd.Parameters.AddWithValue("@Description", description);

            await cmd.ExecuteNonQueryAsync();
        }
    }
}
