namespace HotelBooking.Application.DTOs
{
    public class BookingCreateDto
    {
        public int RoomId { get; set; }
        public DateOnly CheckInDate { get; set; } = DateOnly.FromDateTime(DateTime.Today);
        public DateOnly CheckOutDate { get; set; } = DateOnly.FromDateTime(DateTime.Today.AddDays(1));
        public int NumberOfGuests { get; set; } = 1;
        public int Adults { get; set; } = 1;
        public int Children { get; set; } = 0;
    }

    public class BookingConfirmDto
    {
        public int RoomId { get; set; }
        public string RoomNumber { get; set; } = string.Empty;
        public string TypeName { get; set; } = string.Empty;
        public DateOnly CheckInDate { get; set; }
        public DateOnly CheckOutDate { get; set; }
        public int NumberOfGuests { get; set; }
        public int Nights { get; set; }
        public decimal PricePerNight { get; set; }
        public decimal TotalPrice { get; set; }
        public int Adults { get; set; }
        public int Children { get; set; }
        public decimal ExtraGuestSurcharge { get; set; }
        public decimal WeekendSurcharge { get; set; }
        public string PriceBreakdown { get; set; } = string.Empty;
    }

    public class BookingListItemDto
    {
        public int BookingId { get; set; }
        public string RoomNumber { get; set; } = string.Empty;
        public string TypeName { get; set; } = string.Empty;
        public DateOnly CheckInDate { get; set; }
        public DateOnly CheckOutDate { get; set; }
        public string Status { get; set; } = string.Empty;
        public decimal TotalPrice { get; set; }
        public DateTime CreatedAt { get; set; }
        public string? CustomerName { get; set; }
        public string? CancelReason { get; set; }
        public string? CustomerPhone { get; set; }
        public int NumberOfGuests { get; set; }
    }

    public class BookingResult
    {
        public bool Success { get; set; }
        public string? ErrorMessage { get; set; }
        public int? BookingId { get; set; }
    }
}
