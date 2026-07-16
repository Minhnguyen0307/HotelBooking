using System;
using System.Collections.Generic;

namespace HotelBooking.Infrastructure;

public partial class VwRoomOccupancy
{
    public int RoomId { get; set; }

    public string RoomNumber { get; set; } = null!;

    public int? TotalBookings { get; set; }

    public int? TotalNightsBooked { get; set; }
}
