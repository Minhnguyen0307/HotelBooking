namespace HotelBooking.Application.DTOs
{
    public class RoomSearchDto
    {
        public DateOnly CheckInDate { get; set; } = DateOnly.FromDateTime(DateTime.Today);
        public DateOnly CheckOutDate { get; set; } = DateOnly.FromDateTime(DateTime.Today.AddDays(1));
        public int? Guests { get; set; }
        public int? RoomTypeId { get; set; }
    }

    public class RoomListItemDto
    {
        public int RoomId { get; set; }
        public string RoomNumber { get; set; } = string.Empty;
        public string TypeName { get; set; } = string.Empty;
        public decimal BasePrice { get; set; }
        public int MaxGuests { get; set; }
        public string? PrimaryImageUrl { get; set; }
        public string? Address { get; set; }
    }

    public class RoomDetailDto
    {
        public int RoomId { get; set; }
        public string RoomNumber { get; set; } = string.Empty;
        public string TypeName { get; set; } = string.Empty;
        public string? Description { get; set; }
        public decimal BasePrice { get; set; }
        public int MaxGuests { get; set; }
        public List<string> ImageUrls { get; set; } = new();
        public List<string> Amenities { get; set; } = new();
        public string? Address { get; set; }
    }
}
