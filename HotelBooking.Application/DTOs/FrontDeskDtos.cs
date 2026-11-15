namespace HotelBooking.Application.DTOs
{
    public class RoomMatrixItemDto
    {
        public int RoomId { get; set; }
        public string RoomNumber { get; set; } = string.Empty;
        public int? Floor { get; set; }
        public string TypeName { get; set; } = string.Empty;
        public decimal BasePrice { get; set; }
        public string Status { get; set; } = string.Empty; // Available, Booked, Maintenance
        public int? ActiveBookingId { get; set; }
        public string? GuestName { get; set; }
        public string? GuestPhone { get; set; }
        public DateOnly? CheckInDate { get; set; }
        public DateOnly? CheckOutDate { get; set; }
    }

    public class RoomMatrixFloorDto
    {
        public int Floor { get; set; }
        public List<RoomMatrixItemDto> Rooms { get; set; } = new();
    }

    public class WalkInBookingDto
    {
        public int RoomId { get; set; }
        public string GuestName { get; set; } = string.Empty;
        public string GuestPhone { get; set; } = string.Empty;
        public string? GuestEmail { get; set; }
        public DateOnly CheckInDate { get; set; }
        public DateOnly CheckOutDate { get; set; }
        public int Adults { get; set; } = 1;
        public int Children { get; set; } = 0;
        public string PaymentMethod { get; set; } = "Cash";
    }

    public class TransferRoomDto
    {
        public int NewRoomId { get; set; }
    }

    public class ExtendStayDto
    {
        public DateOnly NewCheckOutDate { get; set; }
    }

    public class InvoiceDto
    {
        public int BookingId { get; set; }
        public string GuestName { get; set; } = string.Empty;
        public string? GuestPhone { get; set; }
        public string? GuestEmail { get; set; }
        public string RoomNumber { get; set; } = string.Empty;
        public string RoomTypeName { get; set; } = string.Empty;
        public DateOnly CheckInDate { get; set; }
        public DateOnly CheckOutDate { get; set; }
        public int Nights { get; set; }
        public int NumberOfGuests { get; set; }
        public decimal BaseRoomTotal { get; set; }
        public decimal ExtraGuestSurcharge { get; set; }
        public decimal WeekendSurcharge { get; set; }
        public decimal SubTotal { get; set; }
        public decimal VatAmount { get; set; }
        public decimal TotalAmount { get; set; }
        public string PaymentMethod { get; set; } = string.Empty;
        public string PaymentStatus { get; set; } = string.Empty;
        public string? TransactionId { get; set; }
        public DateTime? PaymentDate { get; set; }
        public DateTime InvoiceGeneratedAt { get; set; }
    }

}
