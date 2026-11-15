using HotelBooking.Application.Interfaces;
using HotelBooking.Application.Services;
using HotelBooking.Application.Settings;
using HotelBooking.Infrastructure;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace HotelBooking.Controllers.Api;

[ApiController]
[Authorize]
[Route("api/payments")]
public class PaymentsApiController : ControllerBase
{
    private readonly IPaymentService _paymentService;
    private readonly HotelBookingDbContext _context;
    private readonly VietQrSettings _vietQr;

    public PaymentsApiController(IPaymentService paymentService, HotelBookingDbContext context, IOptions<VietQrSettings> vietQr)
    {
        _paymentService = paymentService;
        _context = context;
        _vietQr = vietQr.Value;
    }

    [HttpGet("{bookingId:int}")]
    public async Task<IActionResult> Get(int bookingId)
    {
        var booking = await LoadOwnBooking(bookingId);
        if (booking == null) return NotFound(new { message = "Không tìm thấy đơn đặt phòng." });

        var amountDue = await GetAmountDueAsync(booking);
        if (booking.Status == "Pending" && amountDue > 0
            && !await _context.Payments.AnyAsync(p => p.BookingId == bookingId && p.PaymentStatus == "Pending"))
        {
            _context.Payments.Add(new Payment
            {
                BookingId = bookingId,
                Amount = amountDue,
                PaymentMethod = "VietQR",
                PaymentStatus = "Pending",
                CreatedAt = DateTime.UtcNow
            });
            await _context.SaveChangesAsync();
        }
        var payments = await _paymentService.GetPaymentsByBookingIdAsync(bookingId);
        return Ok(new
        {
            booking = ToBooking(booking, amountDue),
            alreadyPaid = payments.Any(p => p.PaymentStatus == "Success"),
            paymentQrUrl = booking.Status == "Pending" && amountDue > 0 ? CreateVietQrUrl(bookingId, amountDue) : null,
            bankConfigured = IsVietQrConfigured(),
            beneficiary = IsVietQrConfigured() ? new
            {
                bankId = _vietQr.BankId,
                accountNumber = _vietQr.AccountNumber,
                accountName = _vietQr.AccountName
            } : null,
            payments = payments.Select(p => new
            {
                p.PaymentId,
                p.Amount,
                p.PaymentMethod,
                p.PaymentStatus,
                p.TransactionId,
                p.PaymentDate
            })
        });
    }

    [HttpPost("process")]
    public async Task<IActionResult> Process(ProcessPaymentRequest request)
    {
        var bookingExists = await _context.Bookings.AnyAsync(b => b.BookingId == request.BookingId && b.CustomerId == ApiUser.Id(User));
        if (!bookingExists) return NotFound(new { message = "Không tìm thấy đơn đặt phòng." });
        return Conflict(new { message = "Thanh toán được xác nhận sau khi nhân viên đối chiếu giao dịch đã vào tài khoản." });
    }

    [HttpPost("{bookingId:int}/confirm-manual")]
    [Authorize(Roles = "Receptionist,Manager,Admin")]
    public async Task<IActionResult> ConfirmManual(int bookingId)
    {
        var booking = await _context.Bookings
            .Include(b => b.Payments)
            .FirstOrDefaultAsync(b => b.BookingId == bookingId);
        if (booking == null) return NotFound(new { message = "Không tìm thấy đơn đặt phòng." });
        if (booking.Status is not ("Pending" or "Cancelled")) return Conflict(new { message = "Đơn không còn chờ xác nhận thanh toán." });

        var payment = booking.Payments.FirstOrDefault(p => p.PaymentMethod == "VietQR" && p.PaymentStatus == "Pending");
        if (payment == null) return Conflict(new { message = "Không tìm thấy giao dịch VietQR đang chờ xác nhận." });

        payment.PaymentStatus = "Success";
        payment.PaymentDate = DateTime.UtcNow;
        payment.TransactionId = "MANUAL-" + Guid.NewGuid().ToString("N")[..12].ToUpperInvariant();
        if (booking.Status == "Cancelled")
        {
            _context.Refunds.Add(new Refund
            {
                PaymentId = payment.PaymentId,
                Amount = payment.Amount,
                Reason = booking.CancelReason ?? "Hoàn tiền sau khi đối chiếu khoản VietQR của đơn đã hủy.",
                Status = "Pending",
                RequestedAt = DateTime.UtcNow
            });
        }
        else
        {
            booking.Status = "Confirmed";
        }
        await _context.SaveChangesAsync();
        var message = booking.Status == "Cancelled"
            ? $"Đã đối chiếu khoản chuyển của đơn đã hủy #{bookingId}; yêu cầu hoàn {OccupancyPricing.Money(payment.Amount)} đã được ghi nhận."
            : $"Đã xác nhận thanh toán đơn #{bookingId}.";
        return Ok(new { message, bookingId, status = payment.PaymentStatus, amount = payment.Amount });
    }

    private async Task<Booking?> LoadOwnBooking(int bookingId)
    {
        return await _context.Bookings
            .Include(b => b.BookingRooms)
            .ThenInclude(br => br.Room)
            .ThenInclude(r => r.RoomType)
            .Include(b => b.FolioItems)
            .FirstOrDefaultAsync(b => b.BookingId == bookingId && b.CustomerId == ApiUser.Id(User));
    }

    private static object ToBooking(Booking booking, decimal amountDue)
    {
        var room = booking.BookingRooms.Select(br => br.Room).FirstOrDefault();
        var nights = Math.Max(0, booking.CheckOutDate.DayNumber - booking.CheckInDate.DayNumber);
        var quote = room?.RoomType == null
            ? null
            : OccupancyPricing.Price(room.RoomType.TypeName, room.RoomType.MaxGuests, room.RoomType.BasePrice, booking.NumberOfGuests, nights);

        return new
        {
            booking.BookingId,
            booking.Status,
            booking.TotalPrice,
            amountDue,
            booking.CheckInDate,
            booking.CheckOutDate,
            booking.NumberOfGuests,
            booking.AdultCount,
            booking.ChildCount,
            booking.GuestName,
            booking.GuestPhone,
            booking.GuestNote,
            booking.PaymentChoice,
            stayLabel = quote?.Plan.Label,
            roomCharge = quote?.RoomCharge,
            surcharge = quote == null ? 0 : Math.Max(0, booking.TotalPrice - quote.RoomCharge),
            summary = quote?.Summary,
            addOns = booking.FolioItems.Where(item => item.Category == "Service").Select(item => new
            {
                item.Description,
                item.Quantity,
                item.UnitPrice,
                totalPrice = item.UnitPrice * item.Quantity
            }),
            discount = booking.FolioItems.Where(item => item.Category == "Discount").Sum(item => item.UnitPrice * item.Quantity),
            rooms = booking.BookingRooms.Select(br => new
            {
                br.Room.RoomNumber,
                br.PricePerNight
            })
        };
    }

    public class ProcessPaymentRequest
    {
        public int BookingId { get; set; }
    }

    private bool IsVietQrConfigured() =>
        !string.IsNullOrWhiteSpace(_vietQr.BankId)
        && !string.IsNullOrWhiteSpace(_vietQr.AccountNumber)
        && !string.IsNullOrWhiteSpace(_vietQr.AccountName);

    private string? CreateVietQrUrl(int bookingId, decimal amount)
    {
        if (!IsVietQrConfigured()) return null;
        var bank = Uri.EscapeDataString(_vietQr.BankId.Trim());
        var account = Uri.EscapeDataString(_vietQr.AccountNumber.Trim());
        var accountName = Uri.EscapeDataString(_vietQr.AccountName.Trim());
        var memo = Uri.EscapeDataString($"BOOKING_{bookingId}");
        return $"https://img.vietqr.io/image/{bank}-{account}-compact2.svg?amount={decimal.ToInt64(decimal.Round(amount, 0))}&addInfo={memo}&accountName={accountName}";
    }

    private async Task<decimal> GetAmountDueAsync(Booking booking)
    {
        var items = booking.FolioItems.Count > 0
            ? booking.FolioItems
            : await _context.FolioItems.Where(item => item.BookingId == booking.BookingId).ToListAsync();
        var services = items.Where(item => item.Category == "Service").Sum(item => item.UnitPrice * item.Quantity);
        var discounts = items.Where(item => item.Category == "Discount").Sum(item => item.UnitPrice * item.Quantity);
        return Math.Max(0, booking.TotalPrice + services - discounts);
    }
}
