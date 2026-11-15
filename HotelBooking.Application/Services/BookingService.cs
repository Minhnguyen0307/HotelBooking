using HotelBooking.Application.DTOs;
using HotelBooking.Application.Interfaces;
using HotelBooking.Infrastructure;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using HotelBooking.Application.Settings;
using HotelBooking.Application.Hubs;
namespace HotelBooking.Application.Services

{
    public class BookingService : IBookingService
    {
        private readonly HotelBookingDbContext _context;
        private readonly IHubContext<RoomHub> _hubContext;
        private readonly HotelPolicies _policy;
        public BookingService(HotelBookingDbContext context, IHubContext<RoomHub> hubContext, IOptions<HotelPolicies> policyOptions)
        {
            _context = context;
            _hubContext = hubContext;
            _policy = policyOptions.Value;
        }
        private async Task<bool> IsRoomAvailableAsync(int roomId, DateOnly checkIn, DateOnly checkOut)
        {
            bool isOverlapping = await _context.BookingRooms
                .Where(br => br.RoomId == roomId)
                .Where(br => br.Booking.Status == "Pending"
                          || br.Booking.Status == "Confirmed"
                          || br.Booking.Status == "CheckedIn"
                          || br.Booking.Status == "CancelRequested")
                .AnyAsync(br => br.Booking.CheckInDate < checkOut && br.Booking.CheckOutDate > checkIn);

            return !isOverlapping;
        }

        public async Task<List<PromoCodeOptionDto>> GetAvailablePromoCodesAsync(int customerId)
        {
            var isNewCustomer = !await _context.Bookings.AnyAsync(booking => booking.CustomerId == customerId);
            var now = DateTime.UtcNow;
            return await _context.PromoCodes
                .Where(promo => promo.IsActive
                    && (!promo.ExpiresAt.HasValue || promo.ExpiresAt > now)
                    && (!promo.MaxUses.HasValue || promo.UsageCount < promo.MaxUses.Value)
                    && (!promo.IsNewCustomerOnly || isNewCustomer))
                .OrderByDescending(promo => promo.IsNewCustomerOnly)
                .ThenBy(promo => promo.DiscountAmount == 0 ? 1 : 0)
                .ThenByDescending(promo => promo.DiscountAmount)
                .Select(promo => new PromoCodeOptionDto
                {
                    Code = promo.Code,
                    DiscountAmount = promo.DiscountAmount,
                    DiscountPercent = promo.DiscountPercent,
                    MinimumOrderAmount = promo.MinimumOrderAmount,
                    ExpiresAt = promo.ExpiresAt,
                    IsNewCustomerOnly = promo.IsNewCustomerOnly
                })
                .ToListAsync();
        }

        public async Task<BookingPreviewResult> GetBookingPreviewAsync(int customerId, BookingCreateDto dto)
        {
            if (!NormalizeGuests(dto, out var guestError))
                return FailPreview(guestError!);

            var today = DateOnly.FromDateTime(DateTime.Today);
            if (dto.CheckInDate < today)
                return FailPreview("Ngày nhận phòng không được là ngày trong quá khứ.");

            var room = await _context.Rooms
                .Include(r => r.RoomType)
                .FirstOrDefaultAsync(r => r.RoomId == dto.RoomId);

            if (room == null)
                return FailPreview("Phòng không tồn tại.");
            if (room.Status == "Maintenance" || room.HousekeepingStatus == "Maintenance")
                return FailPreview("Phòng đang bảo trì, chưa nhận đặt.");

            int nights = dto.CheckOutDate.DayNumber - dto.CheckInDate.DayNumber;
            if (nights <= 0)
                return FailPreview("Ngày trả phòng phải sau ngày nhận phòng.");

            var quote = OccupancyPricing.Price(room.RoomType.TypeName, room.RoomType.MaxGuests, room.RoomType.BasePrice, dto.NumberOfGuests, nights);
            if (!quote.Success)
                return FailPreview(quote.ErrorMessage ?? quote.Plan.Rule);

            var addOnQuote = BookingAddOnPricing.Quote(dto.AddOns, nights, dto.NumberOfGuests);
            if (addOnQuote.Error != null) return FailPreview(addOnQuote.Error);

            var dateMarkup = CalculateDynamicDateMarkup(dto.CheckInDate, dto.CheckOutDate, room.RoomType.BasePrice, out var markupSummary);
            var subtotal = quote.TotalPrice + dateMarkup + addOnQuote.Lines.Sum(line => line.TotalPrice);
            var promo = await GetDiscountAsync(customerId, dto.PromoCode, subtotal);
            if (promo.Error != null) return FailPreview(promo.Error);

            return new BookingPreviewResult
            {
                Success = true,
                Preview = ToConfirm(room, dto, nights, quote, addOnQuote.Lines, promo.Amount, subtotal - promo.Amount, dateMarkup, markupSummary)
            };
        }

        private static BookingPreviewResult FailPreview(string message) =>
            new() { Success = false, ErrorMessage = message };

        private decimal CalculateDynamicDateMarkup(DateOnly checkIn, DateOnly checkOut, decimal basePrice, out string breakdown)
        {
            decimal markupTotal = 0;
            var holidays = new HashSet<string>(_policy.Holidays ?? new List<string>(), StringComparer.OrdinalIgnoreCase);
            var details = new List<string>();
            for (var date = checkIn; date < checkOut; date = date.AddDays(1))
            {
                var holidayKey = date.ToString("dd-MM");
                double percent = holidays.Contains(holidayKey)
                    ? _policy.HolidayMarkupPercent
                    : date.DayOfWeek is DayOfWeek.Friday or DayOfWeek.Saturday ? _policy.WeekendMarkupPercent : 0;
                if (percent <= 0) continue;
                var amount = Math.Round(basePrice * (decimal)(percent / 100.0), 0);
                markupTotal += amount;
                details.Add($"{date:dd/MM} +{percent}% (+{OccupancyPricing.Money(amount)})");
            }
            breakdown = string.Join("; ", details);
            return markupTotal;
        }

        private static BookingConfirmDto ToConfirm(Room room, BookingCreateDto dto, int nights, OccupancyPricing.Quote quote, List<BookingAddOnPricing.Line> addOns, decimal discount, decimal total, decimal dateMarkup, string markupSummary) =>
            new()
            {
                RoomId = room.RoomId,
                RoomNumber = room.RoomNumber,
                TypeName = room.RoomType.TypeName,
                CheckInDate = dto.CheckInDate,
                CheckOutDate = dto.CheckOutDate,
                NumberOfGuests = dto.NumberOfGuests,
                AdultCount = dto.AdultCount,
                ChildCount = dto.ChildCount,
                Nights = nights,
                PricePerNight = quote.PricePerNight,
                TotalPrice = total,
                StayLabel = quote.Plan.Label,
                IncludedGuests = quote.Plan.IncludedGuests,
                ExtraGuests = quote.ExtraGuests,
                ExtraGuestFee = quote.Plan.ExtraGuestFeePerNight,
                RoomCharge = quote.RoomCharge,
                Surcharge = quote.Surcharge + dateMarkup,
                AddOnsTotal = addOns.Sum(line => line.TotalPrice),
                DiscountTotal = discount,
                PromoCode = discount > 0 ? dto.PromoCode?.Trim().ToUpperInvariant() : null,
                AddOns = addOns.Select(line => new BookingAddOnLineDto
                {
                    Code = line.Code,
                    Name = line.Name,
                    Quantity = line.Quantity,
                    UnitPrice = line.UnitPrice,
                    TotalPrice = line.TotalPrice,
                    UnitLabel = line.UnitLabel
                }).ToList(),
                Summary = string.IsNullOrWhiteSpace(markupSummary)
                    ? quote.Summary
                    : $"{quote.Summary} · Phụ thu ngày lễ/cuối tuần: +{OccupancyPricing.Money(dateMarkup)} ({markupSummary})"
            };

        public async Task<BookingResult> CreateBookingAsync(int customerId, BookingCreateDto dto)
        {
            if (!NormalizeGuests(dto, out var guestError))
                return new BookingResult { Success = false, ErrorMessage = guestError };

            var today = DateOnly.FromDateTime(DateTime.Today);

            if (dto.CheckInDate < today)
                return new BookingResult { Success = false, ErrorMessage = "Ngày nhận phòng không được là ngày trong quá khứ." };

            if (dto.CheckOutDate <= dto.CheckInDate)
                return new BookingResult { Success = false, ErrorMessage = "Ngày trả phòng phải sau ngày nhận phòng." };

            var room = await _context.Rooms
                .Include(r => r.RoomType)
                .FirstOrDefaultAsync(r => r.RoomId == dto.RoomId);

            if (room == null)
                return new BookingResult { Success = false, ErrorMessage = "Phòng không tồn tại." };
            if (room.Status == "Maintenance" || room.HousekeepingStatus == "Maintenance")
                return new BookingResult { Success = false, ErrorMessage = "Phòng đang bảo trì, chưa nhận đặt." };

            int nights = dto.CheckOutDate.DayNumber - dto.CheckInDate.DayNumber;
            var quote = OccupancyPricing.Price(room.RoomType.TypeName, room.RoomType.MaxGuests, room.RoomType.BasePrice, dto.NumberOfGuests, nights);
            if (!quote.Success)
                return new BookingResult { Success = false, ErrorMessage = quote.ErrorMessage };

            var addOnQuote = BookingAddOnPricing.Quote(dto.AddOns, nights, dto.NumberOfGuests);
            if (addOnQuote.Error != null)
                return new BookingResult { Success = false, ErrorMessage = addOnQuote.Error };

            var dateMarkup = CalculateDynamicDateMarkup(dto.CheckInDate, dto.CheckOutDate, room.RoomType.BasePrice, out _);
            var roomTotal = quote.TotalPrice + dateMarkup;
            var subtotal = roomTotal + addOnQuote.Lines.Sum(line => line.TotalPrice);
            var promo = await GetDiscountAsync(customerId, dto.PromoCode, subtotal);
            if (promo.Error != null)
                return new BookingResult { Success = false, ErrorMessage = promo.Error };
            var amountDue = Math.Max(0, subtotal - promo.Amount);

            var guestName = (dto.GuestName ?? string.Empty).Trim();
            var guestPhone = new string((dto.GuestPhone ?? string.Empty).Where(char.IsDigit).ToArray());
            var guestNote = string.IsNullOrWhiteSpace(dto.GuestNote) ? null : dto.GuestNote.Trim();
            var payNow = !string.Equals(dto.PaymentChoice, "Cash", StringComparison.OrdinalIgnoreCase);

            if (guestName.Length < 2 || guestName.Length > 150)
                return new BookingResult { Success = false, ErrorMessage = "Vui lòng nhập tên người nhận phòng." };
            if (guestPhone.Length != 10 || !guestPhone.StartsWith('0'))
                return new BookingResult { Success = false, ErrorMessage = "Số điện thoại cần 10 số và bắt đầu bằng 0." };
            if (guestNote?.Length > 500)
                return new BookingResult { Success = false, ErrorMessage = "Lời nhắn tối đa 500 ký tự." };

            bool available = await IsRoomAvailableAsync(dto.RoomId, dto.CheckInDate, dto.CheckOutDate);
            if (!available)
                return new BookingResult { Success = false, ErrorMessage = "Rất tiếc, phòng vừa được người khác đặt trước. Vui lòng chọn phòng khác." };

            var booking = new Booking
            {
                CustomerId = customerId,
                CheckInDate = dto.CheckInDate,
                CheckOutDate = dto.CheckOutDate,
                NumberOfGuests = dto.NumberOfGuests,
                AdultCount = dto.AdultCount,
                ChildCount = dto.ChildCount,
                GuestName = guestName,
                GuestPhone = guestPhone,
                GuestNote = guestNote,
                PaymentChoice = payNow ? "Now" : "Cash",
                Status = payNow ? "Pending" : "Confirmed",
                TotalPrice = roomTotal,
                CreatedAt = DateTime.UtcNow
            };

            booking.BookingRooms.Add(new BookingRoom
            {
                RoomId = dto.RoomId,
                PricePerNight = quote.PricePerNight
            });

            if (!payNow)
            {
                booking.Payments.Add(new Payment
                {
                    Amount = amountDue,
                    PaymentMethod = "Cash",
                    PaymentStatus = "Pending",
                    TransactionId = null,
                    CreatedAt = DateTime.UtcNow
                });
            }

            foreach (var item in BookingAddOnPricing.ToFolioItems(addOnQuote.Lines, booking))
                booking.FolioItems.Add(item);

            if (promo.Amount > 0 && promo.PromoCode != null)
            {
                promo.PromoCode.UsageCount++;
                booking.FolioItems.Add(new FolioItem
                {
                    Booking = booking,
                    Category = "Discount",
                    Description = $"Mã {promo.PromoCode.Code}",
                    Quantity = 1,
                    UnitPrice = promo.Amount,
                    CreatedAt = DateTime.UtcNow
                });
            }

            _context.Bookings.Add(booking);
            await _context.SaveChangesAsync();
            await _hubContext.Clients.All.SendAsync("RoomBooked", new
            {
                RoomId = dto.RoomId,
                CheckInDate = dto.CheckInDate.ToString("yyyy-MM-dd"),
                CheckOutDate = dto.CheckOutDate.ToString("yyyy-MM-dd")
            });
            return new BookingResult { Success = true, BookingId = booking.BookingId, RequiresPayment = payNow };
        }

        private static bool NormalizeGuests(BookingCreateDto dto, out string? error)
        {
            error = null;
            if (dto.AdultCount < 0 || dto.ChildCount < 0)
            {
                error = "Số người lớn và trẻ em không hợp lệ.";
                return false;
            }
            if (dto.AdultCount == 0 && dto.ChildCount == 0)
                dto.AdultCount = dto.NumberOfGuests;
            if (dto.AdultCount < 1)
            {
                error = "Đặt phòng cần ít nhất một người lớn.";
                return false;
            }
            dto.NumberOfGuests = dto.AdultCount + dto.ChildCount;
            return true;
        }

        private async Task<(PromoCode? PromoCode, decimal Amount, string? Error)> GetDiscountAsync(int customerId, string? code, decimal subtotal)
        {
            if (string.IsNullOrWhiteSpace(code)) return (null, 0, null);
            var normalized = code.Trim().ToUpperInvariant();
            var promo = await _context.PromoCodes.FirstOrDefaultAsync(item => item.Code == normalized && item.IsActive);
            if (promo == null) return (null, 0, "Mã giảm giá không tồn tại hoặc đã bị tắt.");
            if (promo.IsNewCustomerOnly && await _context.Bookings.AnyAsync(booking => booking.CustomerId == customerId))
                return (null, 0, "Mã này chỉ áp dụng cho khách hàng đặt phòng lần đầu.");
            if (promo.ExpiresAt.HasValue && promo.ExpiresAt.Value <= DateTime.UtcNow)
                return (null, 0, "Mã giảm giá đã hết hạn.");
            if (promo.MaxUses.HasValue && promo.UsageCount >= promo.MaxUses.Value)
                return (null, 0, "Mã giảm giá đã hết lượt sử dụng.");
            if (subtotal < promo.MinimumOrderAmount)
                return (null, 0, $"Mã này áp dụng cho đơn từ {OccupancyPricing.Money(promo.MinimumOrderAmount)}.");

            var amount = promo.DiscountPercent > 0
                ? Math.Round(subtotal * promo.DiscountPercent / 100m, 0)
                : promo.DiscountAmount;
            return (promo, Math.Min(Math.Max(0, amount), subtotal), null);
        }

        public async Task<List<BookingListItemDto>> GetCustomerBookingsAsync(int customerId)
        {
            var today = DateOnly.FromDateTime(DateTime.Now);
            return await _context.Bookings
                .Where(b => b.CustomerId == customerId)
                .OrderByDescending(b => b.CreatedAt)
                .Select(b => new BookingListItemDto
                {
                    BookingId = b.BookingId,
                    RoomNumber = b.BookingRooms.Select(br => br.Room.RoomNumber).FirstOrDefault() ?? "",
                    TypeName = b.BookingRooms.Select(br => br.Room.RoomType.TypeName).FirstOrDefault() ?? "",
                    PropertyName = b.BookingRooms.Select(br => br.Room.Property.Name).FirstOrDefault() ?? "",
                    CheckInDate = b.CheckInDate,
                    CheckOutDate = b.CheckOutDate,
                    Status = b.Status,
                    TotalPrice = b.TotalPrice
                        + b.FolioItems.Where(item => item.Category == "Service").Sum(item => item.UnitPrice * item.Quantity)
                        - b.FolioItems.Where(item => item.Category == "Discount").Sum(item => item.UnitPrice * item.Quantity),
                    CreatedAt = b.CreatedAt,
                    AdultCount = b.AdultCount,
                    ChildCount = b.ChildCount,
                    GuestName = b.GuestName,
                    GuestPhone = b.GuestPhone,
                    GuestNote = b.GuestNote,
                    CancelReason = b.CancelReason,
                    PaymentChoice = b.PaymentChoice,
                    PaymentMethod = b.Payments
                        .OrderByDescending(payment => payment.CreatedAt)
                        .Select(payment => payment.PaymentMethod)
                        .FirstOrDefault() ?? b.PaymentChoice,
                    PaymentStatus = b.Payments.Any(payment => payment.PaymentStatus == "Refunded")
                        ? "Refunded"
                        : b.Payments.Any(payment => payment.PaymentStatus == "Success")
                            ? "Success"
                            : b.Payments.Any(payment => payment.PaymentStatus == "Pending")
                                ? "Pending"
                                : "Unpaid",
                    PaymentDate = b.Payments
                        .Where(payment => payment.PaymentDate != null)
                        .Max(payment => (DateTime?)payment.PaymentDate),
                    PaidAmount = b.Payments
                        .Where(payment => payment.PaymentStatus == "Success" || payment.PaymentStatus == "Refunded")
                        .Select(payment => (decimal?)payment.Amount)
                        .Sum() ?? 0m,
                    RefundStatus = b.Payments
                        .SelectMany(payment => payment.Refunds)
                        .OrderByDescending(refund => refund.RequestedAt)
                        .Select(refund => refund.Status)
                        .FirstOrDefault(),
                    CanCustomerCancel = b.CheckInDate > today
                        && (b.Status == "Pending" || b.Status == "Confirmed"),
                    RefundAmount = b.Payments
                        .Where(payment => payment.PaymentStatus == "Success")
                        .Select(payment => (decimal?)payment.Amount)
                        .Sum() ?? 0m
                })
                .ToListAsync();
        }

        public async Task<BookingResult> CancelBookingAsync(int customerId, int bookingId, string? reason)
        {
            var booking = await _context.Bookings
                .Include(b => b.BookingRooms)
                .ThenInclude(br => br.Room)
                .FirstOrDefaultAsync(b => b.BookingId == bookingId && b.CustomerId == customerId);

            if (booking == null)
                return new BookingResult { Success = false, ErrorMessage = "Không tìm thấy đơn đặt phòng." };

            var today = DateOnly.FromDateTime(DateTime.Now);
            if (booking.CheckInDate <= today)
                return new BookingResult { Success = false, ErrorMessage = "Đơn chỉ được hủy trước 00:00 ngày nhận phòng." };

            if (booking.Status is not ("Pending" or "Confirmed"))
                return new BookingResult { Success = false, ErrorMessage = $"Không thể hủy đơn ở trạng thái '{booking.Status}'." };

            var normalizedReason = reason?.Trim();
            if (string.IsNullOrWhiteSpace(normalizedReason))
                return new BookingResult { Success = false, ErrorMessage = "Vui lòng nhập lý do hủy đơn." };
            if (normalizedReason.Length > 300)
                return new BookingResult { Success = false, ErrorMessage = "Lý do hủy không được vượt quá 300 ký tự." };

            booking.Status = "Cancelled";
            booking.CancelledAt = DateTime.UtcNow;
            booking.CancelReason = normalizedReason;

            foreach (var bookingRoom in booking.BookingRooms)
            {
                if (bookingRoom.Room.Status != "Maintenance")
                    bookingRoom.Room.Status = "Available";
            }

            var successfulPayments = await _context.Payments
                .Where(payment => payment.BookingId == bookingId && payment.PaymentStatus == "Success")
                .ToListAsync();
            decimal refundAmount = 0;
            foreach (var payment in successfulPayments)
            {
                var hasOpenOrCompletedRefund = await _context.Refunds.AnyAsync(refund =>
                    refund.PaymentId == payment.PaymentId && refund.Status != "Rejected");
                if (hasOpenOrCompletedRefund)
                    continue;

                _context.Refunds.Add(new Refund
                {
                    PaymentId = payment.PaymentId,
                    Amount = payment.Amount,
                    Reason = normalizedReason,
                    Status = "Pending",
                    RequestedAt = DateTime.UtcNow
                });
                refundAmount += payment.Amount;
            }

            await _context.SaveChangesAsync();

            var message = refundAmount > 0
                ? $"Đã hủy đơn. Yêu cầu hoàn {OccupancyPricing.Money(refundAmount)} đã được ghi nhận; thời gian xử lý dự kiến 3–5 ngày làm việc (mô phỏng)."
                : "Đã hủy đơn. Đơn chưa có thanh toán thành công nên không phát sinh hoàn tiền.";
            return new BookingResult { Success = true, Message = message, RefundAmount = refundAmount };
        }
    }
}
