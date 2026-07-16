using System;
using System.Collections.Generic;

namespace HotelBooking.Infrastructure;

public partial class BookingRoom
{
    public int BookingRoomId { get; set; }

    public int BookingId { get; set; }

    public int RoomId { get; set; }

    public decimal PricePerNight { get; set; }

    public virtual Booking Booking { get; set; } = null!;

    public virtual Room Room { get; set; } = null!;
}
