namespace HotelBooking.Infrastructure;

public class FolioItem
{
    public int FolioItemId { get; set; }

    public int BookingId { get; set; }

    public string Category { get; set; } = null!;

    public string Description { get; set; } = null!;

    public int Quantity { get; set; }

    public decimal UnitPrice { get; set; }

    public DateTime CreatedAt { get; set; }

    public virtual Booking Booking { get; set; } = null!;
}
