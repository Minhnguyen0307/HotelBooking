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

    public class ReportChartDto
    {
        public List<MonthlyRevenueDto> Revenue { get; set; } = new();
        public List<RoomTypeStatsDto> RoomTypeStats { get; set; } = new();
        public double OccupancyRatePercent { get; set; }
    }

    public class RoomTypeStatsDto
    {
        public string TypeName { get; set; } = string.Empty;
        public int TotalBookings { get; set; }
    }

    public class ExecutiveSummaryDto
    {
        public int TotalRooms { get; set; }
        public int AvailableRooms { get; set; }
        public int OccupiedRooms { get; set; }
        public int TotalBookings { get; set; }
        public int ActiveStays { get; set; }
        public int PendingCancels { get; set; }
        public decimal TodayRevenue { get; set; }
        public decimal MonthRevenue { get; set; }
        public decimal TotalRevenue { get; set; }
        public double OccupancyPercent { get; set; }
        public List<PropertyRevenueDto> PropertyRevenues { get; set; } = new();
        public List<PaymentMethodStatDto> PaymentMethodStats { get; set; } = new();
    }

    public class PropertyRevenueDto
    {
        public int PropertyId { get; set; }
        public string PropertyName { get; set; } = string.Empty;
        public decimal Revenue { get; set; }
        public int BookingCount { get; set; }
    }

    public class PaymentMethodStatDto
    {
        public string Method { get; set; } = string.Empty;
        public decimal TotalAmount { get; set; }
        public int Count { get; set; }
    }

    public class PropertyRevenueSummaryDto
    {
        public int PropertyId { get; set; }
        public string PropertyName { get; set; } = string.Empty;
        public string Area { get; set; } = string.Empty;
        public string Address { get; set; } = string.Empty;
        public int TotalRooms { get; set; }
        public decimal TotalRevenue { get; set; }
        public int TotalBookingsPaid { get; set; }
        public int TotalNightsBooked { get; set; }
        public decimal AverageRevenuePerBooking => TotalBookingsPaid > 0 ? Math.Round(TotalRevenue / TotalBookingsPaid, 0) : 0;
    }

    public class PropertyMonthlyRevenueDto
    {
        public int PropertyId { get; set; }
        public string PropertyName { get; set; } = string.Empty;
        public string Area { get; set; } = string.Empty;
        public int Year { get; set; }
        public int Month { get; set; }
        public decimal TotalRevenue { get; set; }
        public int TotalBookingsPaid { get; set; }
        public int TotalNightsBooked { get; set; }
    }

    public class PropertyOptionDto
    {
        public int PropertyId { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Area { get; set; } = string.Empty;
    }
}
