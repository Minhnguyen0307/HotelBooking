using System.Collections.Generic;

namespace HotelBooking.Infrastructure;

public partial class Property
{
    public int PropertyId { get; set; }

    public string Name { get; set; } = null!;

    public string Area { get; set; } = null!;

    public string Address { get; set; } = null!;

    public string? Description { get; set; }

    public virtual ICollection<Room> Rooms { get; set; } = new List<Room>();
}
