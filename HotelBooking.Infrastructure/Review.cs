using System;
using System.Collections.Generic;

namespace HotelBooking.Infrastructure;

public partial class Review
{
    public int ReviewId { get; set; }

    public int RoomId { get; set; }

    public int UserId { get; set; }

    public int Rating { get; set; }

    public string? Comment { get; set; }

    public DateTime CreatedAt { get; set; }

    public string? StaffResponse { get; set; }

    public int? RespondedByUserId { get; set; }

    public DateTime? RespondedAt { get; set; }

    public virtual Room Room { get; set; } = null!;

    public virtual User User { get; set; } = null!;

    public virtual User? RespondedByUser { get; set; }

    public virtual ICollection<ReviewImage> ReviewImages { get; set; } = new List<ReviewImage>();
}

