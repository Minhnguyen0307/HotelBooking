namespace HotelBooking.Application.DTOs
{
    public class BookingCreateDto
    {
        public int RoomId { get; set; }
        public DateOnly CheckInDate { get; set; } = DateOnly.FromDateTime(DateTime.Today);
        public DateOnly CheckOutDate { get; set; } = DateOnly.FromDateTime(DateTime.Today.AddDays(1));
        public int NumberOfGuests { get; set; } = 1;
        public int AdultCount { get; set; }
        public int ChildCount { get; set; }
        public string GuestName { get; set; } = string.Empty;
        public string GuestPhone { get; set; } = string.Empty;
        public string? GuestNote { get; set; }
        public string PaymentChoice { get; set; } = "Now";
        public string? PromoCode { get; set; }
        public List<BookingAddOnSelectionDto> AddOns { get; set; } = new();
    }

    public class BookingAddOnSelectionDto
    {
        public string Code { get; set; } = string.Empty;
        public int Quantity { get; set; }
    }

    public class BookingAddOnOptionDto
    {
        public string Code { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public decimal UnitPrice { get; set; }
        public string UnitLabel { get; set; } = string.Empty;
        public bool PerNight { get; set; }
        public int MaxQuantity { get; set; }
    }

    public class PromoCodeOptionDto
    {
        public string Code { get; set; } = string.Empty;
        public decimal DiscountAmount { get; set; }
        public int DiscountPercent { get; set; }
        public decimal MinimumOrderAmount { get; set; }
        public DateTime? ExpiresAt { get; set; }
        public bool IsNewCustomerOnly { get; set; }
    }

    public class BookingConfirmDto
    {
        public int RoomId { get; set; }
        public string RoomNumber { get; set; } = string.Empty;
        public string TypeName { get; set; } = string.Empty;
        public DateOnly CheckInDate { get; set; }
        public DateOnly CheckOutDate { get; set; }
        public int NumberOfGuests { get; set; }
        public int AdultCount { get; set; }
        public int ChildCount { get; set; }
        public int Nights { get; set; }
        public decimal PricePerNight { get; set; }
        public decimal TotalPrice { get; set; }
        public string StayLabel { get; set; } = string.Empty;
        public int IncludedGuests { get; set; }
        public int ExtraGuests { get; set; }
        public decimal ExtraGuestFee { get; set; }
        public decimal RoomCharge { get; set; }
        public decimal Surcharge { get; set; }
        public decimal AddOnsTotal { get; set; }
        public decimal DiscountTotal { get; set; }
        public string? PromoCode { get; set; }
        public List<BookingAddOnLineDto> AddOns { get; set; } = new();
        public string Summary { get; set; } = string.Empty;
    }

    public class BookingAddOnLineDto
    {
        public string Code { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public int Quantity { get; set; }
        public decimal UnitPrice { get; set; }
        public decimal TotalPrice { get; set; }
        public string UnitLabel { get; set; } = string.Empty;
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
        public int AdultCount { get; set; }
        public int ChildCount { get; set; }
        public DateTime CreatedAt { get; set; }
        public string? CustomerName { get; set; }
        public string? CancelReason { get; set; }
        public string? GuestName { get; set; }
        public string? GuestPhone { get; set; }
        public string? GuestNote { get; set; }
        public string PaymentChoice { get; set; } = "Now";
        public string PaymentMethod { get; set; } = string.Empty;
        public string PaymentStatus { get; set; } = "Unpaid";
        public DateTime? PaymentDate { get; set; }
        public decimal PaidAmount { get; set; }
        public string? RefundStatus { get; set; }
        public bool CanCustomerCancel { get; set; }
        public decimal RefundAmount { get; set; }
    }

    public class BookingResult
    {
        public bool Success { get; set; }
        public string? ErrorMessage { get; set; }
        public string? Message { get; set; }
        public int? BookingId { get; set; }
        public bool RequiresPayment { get; set; }
        public decimal RefundAmount { get; set; }
    }
}
