namespace HotelBooking.Application.DTOs
{
    public class RoomSearchDto
    {
        public DateOnly CheckInDate { get; set; } = DateOnly.FromDateTime(DateTime.Today);
        public DateOnly CheckOutDate { get; set; } = DateOnly.FromDateTime(DateTime.Today.AddDays(1));
        public int? Guests { get; set; }
        public int? RoomTypeId { get; set; }
        public int? PropertyId { get; set; }
    }

    public class PropertyDto
    {
        public int PropertyId { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Area { get; set; } = string.Empty;
        public string Address { get; set; } = string.Empty;
        public string? Description { get; set; }
        public int RoomCount { get; set; }
    }

    public class RoomListItemDto
    {
        public int RoomId { get; set; }
        public string RoomNumber { get; set; } = string.Empty;
        public string TypeName { get; set; } = string.Empty;
        public decimal BasePrice { get; set; }
        public int MaxGuests { get; set; }
        public string StayLabel { get; set; } = string.Empty;
        public int IncludedGuests { get; set; }
        public decimal ExtraGuestFee { get; set; }
        public string OccupancyRule { get; set; } = string.Empty;
        public string? PrimaryImageUrl { get; set; }
        public string? Address { get; set; }
        public int PropertyId { get; set; }
        public string PropertyName { get; set; } = string.Empty;
        public string Area { get; set; } = string.Empty;
        public bool IsAvailable { get; set; } = true;
    }

    public class RoomDetailDto
    {
        public int RoomId { get; set; }
        public string RoomNumber { get; set; } = string.Empty;
        public string TypeName { get; set; } = string.Empty;
        public string? Description { get; set; }
        public decimal BasePrice { get; set; }
        public int MaxGuests { get; set; }
        public string StayLabel { get; set; } = string.Empty;
        public int IncludedGuests { get; set; }
        public decimal ExtraGuestFee { get; set; }
        public string OccupancyRule { get; set; } = string.Empty;
        public List<string> ImageUrls { get; set; } = new();
        public List<string> Amenities { get; set; } = new();
        public string? Address { get; set; }
        public int PropertyId { get; set; }
        public string PropertyName { get; set; } = string.Empty;
        public string Area { get; set; } = string.Empty;
        public RoomReviewsSummaryDto ReviewsSummary { get; set; } = new();
    }
}
