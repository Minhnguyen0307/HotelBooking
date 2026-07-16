using System;
using System.Collections.Generic;

namespace HotelBooking.Infrastructure;

public partial class VwMonthlyRevenue
{
    public int? RevenueYear { get; set; }

    public int? RevenueMonth { get; set; }

    public decimal? TotalRevenue { get; set; }

    public int? TotalBookingsPaid { get; set; }
}
