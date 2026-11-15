using System.Globalization;

namespace HotelBooking.Application.Services
{
    public static class OccupancyPricing
    {
        private static readonly CultureInfo Vietnam = CultureInfo.GetCultureInfo("vi-VN");

        public sealed record Plan(string Label, int IncludedGuests, int Capacity, decimal ExtraGuestFeePerNight)
        {
            public string Rule => ExtraGuestFeePerNight <= 0
                ? $"{Label}: giá đã gồm {IncludedGuests} khách, tối đa {Capacity} khách."
                : $"{Label}: giá đã gồm {IncludedGuests} khách. Mỗi khách thêm {Money(ExtraGuestFeePerNight)}/đêm, tối đa {Capacity} khách.";
        }

        public sealed class Quote
        {
            public bool Success { get; init; }
            public string? ErrorMessage { get; init; }
            public Plan Plan { get; init; } = new("Phòng", 1, 1, 0);
            public int ExtraGuests { get; init; }
            public decimal RoomCharge { get; init; }
            public decimal Surcharge { get; init; }
            public decimal PricePerNight { get; init; }
            public decimal TotalPrice { get; init; }
            public string Summary { get; init; } = string.Empty;
        }

        public static Plan For(string? typeName, int storedMaxGuests)
        {
            var name = (typeName ?? string.Empty).ToLowerInvariant();
            var stored = Math.Max(1, storedMaxGuests);

            if (name.Contains("suite") || name.Contains("gia đình") || name.Contains("family"))
                return new Plan("Phòng gia đình", 4, Math.Max(stored, 6), 250_000m);

            if (name.Contains("deluxe") || name.Contains("cặp đôi") || name.Contains("couple") || name.Contains("double"))
                return new Plan("Phòng cặp đôi", 2, Math.Max(stored, 4), 200_000m);

            if (name.Contains("standard") || name.Contains("phòng đơn") || name.Contains("single"))
                return new Plan("Phòng đơn", 1, Math.Max(stored, 2), 150_000m);

            return new Plan(string.IsNullOrWhiteSpace(typeName) ? "Phòng" : typeName, stored, stored, 0);
        }

        public static Quote Price(string? typeName, int storedMaxGuests, decimal basePrice, int guests, int nights)
        {
            var plan = For(typeName, storedMaxGuests);
            if (guests < 1)
                return Fail(plan, "Số khách phải từ 1 trở lên.");
            if (nights < 1)
                return Fail(plan, "Ngày trả phòng phải sau ngày nhận phòng.");
            if (guests > plan.Capacity)
                return Fail(plan, $"{plan.Label} nhận tối đa {plan.Capacity} khách. {plan.Rule}");

            var extra = Math.Max(0, guests - plan.IncludedGuests);
            var surchargePerNight = extra * plan.ExtraGuestFeePerNight;
            var roomCharge = basePrice * nights;
            var surcharge = surchargePerNight * nights;
            var summary = extra == 0
                ? $"{plan.Label}. Giá gồm {plan.IncludedGuests} khách, không phát sinh phụ thu."
                : $"{plan.Label}. Thêm {extra} khách ngoài {plan.IncludedGuests} khách trong giá: phụ thu {extra} × {Money(plan.ExtraGuestFeePerNight)} × {nights} đêm = {Money(surcharge)}.";

            return new Quote
            {
                Success = true,
                Plan = plan,
                ExtraGuests = extra,
                RoomCharge = roomCharge,
                Surcharge = surcharge,
                PricePerNight = basePrice + surchargePerNight,
                TotalPrice = roomCharge + surcharge,
                Summary = summary
            };
        }

        public static string Money(decimal value) => string.Format(Vietnam, "{0:N0} ₫", value);

        private static Quote Fail(Plan plan, string message) => new()
        {
            Success = false,
            Plan = plan,
            ErrorMessage = message,
            Summary = plan.Rule
        };
    }
}