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
        public string GuestName { get; set; } = string.Empty;
        public string GuestPhone { get; set; } = string.Empty;
        public string? GuestNote { get; set; }
        public string PaymentChoice { get; set; } = "Now";
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
        public string StayLabel { get; set; } = string.Empty;
        public int IncludedGuests { get; set; }
        public int ExtraGuests { get; set; }
        public decimal ExtraGuestFee { get; set; }
        public decimal RoomCharge { get; set; }
        public decimal Surcharge { get; set; }
        public string Summary { get; set; } = string.Empty;
    }

    public class BookingPreviewResult
    {
        public bool Success { get; set; }
        public string? ErrorMessage { get; set; }
        public BookingConfirmDto? Preview { get; set; }
    }

    public class BookingListItemDto
    {
        public int BookingId { get; set; }
        public string RoomNumber { get; set; } = string.Empty;
        public string TypeName { get; set; } = string.Empty;
        public string PropertyName { get; set; } = string.Empty;
        public DateOnly CheckInDate { get; set; }
        public DateOnly CheckOutDate { get; set; }
        public string Status { get; set; } = string.Empty;
        public decimal TotalPrice { get; set; }
        public DateTime CreatedAt { get; set; }
        public string? CustomerName { get; set; }
        public string? CustomerPhone { get; set; }
        public int NumberOfGuests { get; set; }
        public string? CancelReason { get; set; }
        public string? GuestName { get; set; }
        public string? GuestPhone { get; set; }
        public string? GuestNote { get; set; }
        public string PaymentChoice { get; set; } = "Now";
    }

    public class BookingResult
    {
        public bool Success { get; set; }
        public string? ErrorMessage { get; set; }
        public int? BookingId { get; set; }
        public bool RequiresPayment { get; set; }
    }
}
