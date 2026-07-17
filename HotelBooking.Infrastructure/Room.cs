using System;
using System.Collections.Generic;

namespace HotelBooking.Infrastructure;

public partial class Room
{
    public int RoomId { get; set; }

    public int RoomTypeId { get; set; }

    public string RoomNumber { get; set; } = null!;

    public int? Floor { get; set; }

    public string Status { get; set; } = null!;

    public string? Description { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public string? Address { get; set; }

    public virtual ICollection<BookingRoom> BookingRooms { get; set; } = new List<BookingRoom>();

    public virtual ICollection<RoomImage> RoomImages { get; set; } = new List<RoomImage>();

    public virtual RoomType RoomType { get; set; } = null!;

    public virtual ICollection<Amenity> Amenities { get; set; } = new List<Amenity>();
}
