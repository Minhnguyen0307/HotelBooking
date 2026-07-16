using System;
using System.Collections.Generic;

namespace HotelBooking.Infrastructure;

public partial class Refund
{
    public int RefundId { get; set; }

    public int PaymentId { get; set; }

    public decimal Amount { get; set; }

    public string? Reason { get; set; }

    public string Status { get; set; } = null!;

    public DateTime RequestedAt { get; set; }

    public DateTime? ProcessedAt { get; set; }

    public virtual Payment Payment { get; set; } = null!;
}
