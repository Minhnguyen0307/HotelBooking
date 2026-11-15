namespace HotelBooking.Infrastructure;

public class HotelService
{
    public int HotelServiceId { get; set; }

    public string ServiceCode { get; set; } = null!;

    public string Name { get; set; } = null!;

    public decimal Price { get; set; }

    public string UnitLabel { get; set; } = null!;

    public bool ForBooking { get; set; }

    public bool ForDesk { get; set; }
}
