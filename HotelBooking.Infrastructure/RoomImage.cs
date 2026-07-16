using System;
using System.Collections.Generic;

namespace HotelBooking.Infrastructure;

public partial class RoomImage
{
    public int ImageId { get; set; }

    public int RoomId { get; set; }

    public string ImageUrl { get; set; } = null!;

    public bool IsPrimary { get; set; }

    public virtual Room Room { get; set; } = null!;
}
