using HotelBooking.Application.DTOs;
using HotelBooking.Infrastructure;

namespace HotelBooking.Application.Services;

public static class BookingAddOnPricing
{
    private sealed record Definition(string Code, string Name, string Description, decimal UnitPrice, string UnitLabel, bool PerNight, int MaxQuantity);

    private static readonly Definition[] Catalog =
    {
        new("airport-transfer", "Đưa đón sân bay", "Một lượt đưa đón bằng xe riêng.", 350_000m, "lượt", false, 2),
        new("breakfast", "Buffet sáng", "Buffet cho mỗi khách trong mỗi đêm lưu trú.", 180_000m, "khách/đêm", true, 8),
        new("spa", "Spa thư giãn", "Một suất dịch vụ spa.", 450_000m, "suất", false, 8),
        new("extra-bed", "Giường phụ", "Giường phụ tính theo đêm.", 300_000m, "giường/đêm", true, 2)
    };

    public sealed record Line(string Code, string Name, int Quantity, int StoredQuantity, decimal UnitPrice, decimal TotalPrice, string UnitLabel, string Description);

    public static IReadOnlyList<BookingAddOnOptionDto> GetOptions() => Catalog.Select(item => new BookingAddOnOptionDto
    {
        Code = item.Code,
        Name = item.Name,
        Description = item.Description,
        UnitPrice = item.UnitPrice,
        UnitLabel = item.UnitLabel,
        PerNight = item.PerNight,
        MaxQuantity = item.MaxQuantity
    }).ToArray();

    public static (List<Line> Lines, string? Error) Quote(IEnumerable<BookingAddOnSelectionDto>? selections, int nights, int guestCount)
    {
        var lines = new List<Line>();
        var requested = (selections ?? Array.Empty<BookingAddOnSelectionDto>()).ToList();
        if (requested.GroupBy(item => item.Code, StringComparer.OrdinalIgnoreCase).Any(group => group.Count() > 1))
            return (lines, "Mỗi dịch vụ cộng thêm chỉ được gửi một lần.");

        foreach (var selection in requested.Where(s => s.Quantity > 0))
        {
            var definition = Catalog.FirstOrDefault(item => item.Code.Equals(selection.Code, StringComparison.OrdinalIgnoreCase));
            if (definition == null) return (lines, "Có dịch vụ cộng thêm không hợp lệ.");
            var maximum = definition.Code == "breakfast" ? Math.Min(definition.MaxQuantity, Math.Max(1, guestCount)) : definition.MaxQuantity;
            if (selection.Quantity < 1 || selection.Quantity > maximum)
                return (lines, $"Số lượng {definition.Name} tối đa là {maximum}.");

            var storedQuantity = selection.Quantity * (definition.PerNight ? nights : 1);
            lines.Add(new Line(
                definition.Code,
                definition.Name,
                selection.Quantity,
                storedQuantity,
                definition.UnitPrice,
                definition.UnitPrice * storedQuantity,
                definition.UnitLabel,
                definition.PerNight
                    ? $"{definition.Name} ({selection.Quantity} {definition.UnitLabel} × {nights} đêm)"
                    : $"{definition.Name} ({selection.Quantity} {definition.UnitLabel})"));
        }
        return (lines, null);
    }

    public static List<FolioItem> ToFolioItems(IEnumerable<Line> lines, Booking booking) => lines.Select(line => new FolioItem
    {
        Booking = booking,
        Category = "Service",
        Description = line.Description,
        Quantity = line.StoredQuantity,
        UnitPrice = line.UnitPrice,
        CreatedAt = DateTime.UtcNow
    }).ToList();
}
