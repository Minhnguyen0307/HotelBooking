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

    public class RevenuePeriodDto
    {
        public string Period { get; set; } = "day";
        public DateOnly From { get; set; }
        public DateOnly To { get; set; }
        public decimal Total { get; set; }
        public int PaymentCount { get; set; }
        public List<RevenueBucketDto> Buckets { get; set; } = new();
    }

    public class RevenueBucketDto
    {
        public string Label { get; set; } = string.Empty;
        public decimal Amount { get; set; }
        public int Count { get; set; }
    }

    public class ConfirmedBillDto
    {
        public int BookingId { get; set; }
        public string GuestName { get; set; } = string.Empty;
        public string RoomNumber { get; set; } = string.Empty;
        public decimal Collected { get; set; }
        public string? ConfirmedBy { get; set; }
        public DateTime? ConfirmedAt { get; set; }
    }
}
