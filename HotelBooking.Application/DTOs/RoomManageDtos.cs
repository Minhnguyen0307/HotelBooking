namespace HotelBooking.Application.DTOs
{
    public class RoomManageListItemDto
    {
        public int RoomId { get; set; }
        public string RoomNumber { get; set; } = string.Empty;
        public string TypeName { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public int? Floor { get; set; }
    }

    public class RoomUpsertDto
    {
        public int? RoomId { get; set; } 
        public string RoomNumber { get; set; } = string.Empty;
        public int RoomTypeId { get; set; }
        public int? Floor { get; set; }
        public string Status { get; set; } = "Available";
        public string? Description { get; set; }
        public string? Address { get; set; }
    }
}
