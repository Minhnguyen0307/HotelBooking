namespace HotelBooking.Infrastructure;

public class PromoCode
{
    public int PromoCodeId { get; set; }

    public string Code { get; set; } = null!;

    public decimal DiscountAmount { get; set; }

    public int DiscountPercent { get; set; }

    public bool IsActive { get; set; }

    public bool IsNewCustomerOnly { get; set; }

    public decimal MinimumOrderAmount { get; set; }

    public int? MaxUses { get; set; }

    public int UsageCount { get; set; }

    public DateTime? ExpiresAt { get; set; }
}
