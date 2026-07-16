namespace HotelBooking.Application.DTOs
{
    public class MonthlyRevenueDto
    {
        public int Year { get; set; }
        public int Month { get; set; }
        public decimal TotalRevenue { get; set; }
        public int TotalBookingsPaid { get; set; }
    }

    public class RoomOccupancyDto
    {
        public string RoomNumber { get; set; } = string.Empty;
        public int TotalBookings { get; set; }
        public int? TotalNightsBooked { get; set; }
    }
}
