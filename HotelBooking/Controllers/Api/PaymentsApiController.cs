using HotelBooking.Application.Interfaces;
using HotelBooking.Application.Services;
using HotelBooking.Infrastructure;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HotelBooking.Controllers.Api;

[ApiController]
[Authorize]
[Route("api/payments")]
public class PaymentsApiController : ControllerBase
{
    private readonly IPaymentService _paymentService;
    private readonly HotelBookingDbContext _context;

    public PaymentsApiController(IPaymentService paymentService, HotelBookingDbContext context)
    {
        _paymentService = paymentService;
        _context = context;
    }

    [HttpGet("{bookingId:int}")]
    public async Task<IActionResult> Get(int bookingId)
    {
        var booking = await LoadOwnBooking(bookingId);
        if (booking == null) return NotFound(new { message = "Không tìm thấy đơn đặt phòng." });

        var amountDue = await GetAmountDueAsync(booking);
        var payments = await _paymentService.GetPaymentsByBookingIdAsync(bookingId);
        return Ok(new
        {
            booking = ToBooking(booking, amountDue),
            alreadyPaid = payments.Any(p => p.PaymentStatus == "Success"),
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
        var booking = await _context.Bookings
            .FirstOrDefaultAsync(b => b.BookingId == request.BookingId && b.CustomerId == ApiUser.Id(User));
        if (booking == null) return NotFound(new { message = "Không tìm thấy đơn đặt phòng." });

        if (booking.Status != "Pending")
            return BadRequest(new { message = "Đơn đặt phòng này không ở trạng thái chờ thanh toán." });

        var payments = await _paymentService.GetPaymentsByBookingIdAsync(request.BookingId);
        if (payments.Any(p => p.PaymentStatus == "Success"))
            return BadRequest(new { message = "Đơn đặt phòng này đã được thanh toán." });

        var amountDue = await GetAmountDueAsync(booking);
        var transactionId = "MOCK-" + Guid.NewGuid().ToString("N")[..12].ToUpperInvariant();
        var payment = await _paymentService.CreatePaymentRecordAsync(
            request.BookingId,
            "VietQR Mock",
            amountDue,
            transactionId,
            "Success");
        booking.Status = "Confirmed";
        await _context.SaveChangesAsync();

        return Ok(new
        {
            message = "Thanh toán thành công.",
            bookingId = request.BookingId,
            status = "Success",
            transactionId = payment.TransactionId,
            paymentMethod = payment.PaymentMethod,
            amount = payment.Amount,
            simulated = true
        });
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
