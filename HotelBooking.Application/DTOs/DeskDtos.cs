namespace HotelBooking.Application.DTOs
{
    public class DeskRoomDto
    {
        public int RoomId { get; set; }
        public string RoomNumber { get; set; } = string.Empty;
        public string TypeName { get; set; } = string.Empty;
        public string PropertyName { get; set; } = string.Empty;
        public string StayLabel { get; set; } = string.Empty;
        public decimal BasePrice { get; set; }
        public string Status { get; set; } = string.Empty;
        public string HousekeepingStatus { get; set; } = "Clean";
    }

    public class DeskIssueDto
    {
        public int IssueId { get; set; }
        public int RoomId { get; set; }
        public string RoomNumber { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
    }

    public class DeskGuestDto
    {
        public int UserId { get; set; }
        public string FullName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string? PhoneNumber { get; set; }
        public int StayCount { get; set; }
        public List<BookingListItemDto> History { get; set; } = new();
    }

    public class WalkInDto
    {
        public string GuestName { get; set; } = string.Empty;
        public string GuestPhone { get; set; } = string.Empty;
        public string? Email { get; set; }
        public int RoomId { get; set; }
        public DateOnly CheckInDate { get; set; }
        public DateOnly CheckOutDate { get; set; }
        public int NumberOfGuests { get; set; } = 1;
        public string? GuestNote { get; set; }
        public string? PromoCode { get; set; }
        public decimal Deposit { get; set; }
    }

    public class StayUpdateDto
    {
        public string GuestName { get; set; } = string.Empty;
        public string GuestPhone { get; set; } = string.Empty;
        public string? GuestNote { get; set; }
        public DateOnly CheckInDate { get; set; }
        public DateOnly CheckOutDate { get; set; }
        public string? PromoCode { get; set; }
    }

    public class ChargeDto
    {
        public string Category { get; set; } = "Service";
        public string Description { get; set; } = string.Empty;
        public int Quantity { get; set; } = 1;
        public decimal UnitPrice { get; set; }
    }

    public class DepositDto
    {
        public decimal Amount { get; set; }
        public string Method { get; set; } = "Cash";
    }

    public class CheckInDto
    {
        public string GuestName { get; set; } = string.Empty;
        public string GuestPhone { get; set; } = string.Empty;
    }

    public class MinibarLineDto
    {
        public string Name { get; set; } = string.Empty;
        public int Quantity { get; set; }
        public decimal UnitPrice { get; set; }
    }

    public class CheckoutDto
    {
        public string Condition { get; set; } = "Dirty";
        public string? DamageNote { get; set; }
        public string PaymentMethod { get; set; } = "Cash";
        public List<MinibarLineDto> Minibar { get; set; } = new();
    }

    public class FolioLineDto
    {
        public string Category { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public int Quantity { get; set; }
        public decimal Amount { get; set; }
    }

    public class FolioDto
    {
        public int BookingId { get; set; }
        public string Status { get; set; } = string.Empty;
        public string GuestName { get; set; } = string.Empty;
        public string GuestPhone { get; set; } = string.Empty;
        public string? GuestNote { get; set; }
        public int RoomId { get; set; }
        public string RoomNumber { get; set; } = string.Empty;
        public string PropertyName { get; set; } = string.Empty;
        public DateOnly CheckInDate { get; set; }
        public DateOnly CheckOutDate { get; set; }
        public int NumberOfGuests { get; set; }
        public decimal RoomCharge { get; set; }
        public decimal Extras { get; set; }
        public decimal Discounts { get; set; }
        public decimal Paid { get; set; }
        public decimal Due { get; set; }
        public bool RoomPaid { get; set; }
        public string PaymentNote { get; set; } = string.Empty;
        public List<FolioLineDto> Lines { get; set; } = new();
    }

    public class ChangeRoomDto
    {
        public int RoomId { get; set; }
    }

    public class GuestUpdateDto
    {
        public string FullName { get; set; } = string.Empty;
        public string? PhoneNumber { get; set; }
        public string? Email { get; set; }
    }

    public class HousekeepingDto
    {
        public string Status { get; set; } = "Clean";
    }

    public class IssueDto
    {
        public string Description { get; set; } = string.Empty;
    }

    public class ServiceOfferDto
    {
        public string Name { get; set; } = string.Empty;
        public decimal Price { get; set; }
    }
}
