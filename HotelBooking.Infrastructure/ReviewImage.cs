using System;
using System.Collections.Generic;

namespace HotelBooking.Infrastructure;

public partial class ReviewImage
{
    public int ReviewImageId { get; set; }

    public int ReviewId { get; set; }

    public string ImageUrl { get; set; } = null!;

    public DateTime CreatedAt { get; set; }

    public virtual Review Review { get; set; } = null!;
}

