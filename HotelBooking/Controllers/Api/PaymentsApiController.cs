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

        var payments = await _paymentService.GetPaymentsByBookingIdAsync(bookingId);
        return Ok(new
        {
            booking = ToBooking(booking),
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

        if (string.IsNullOrWhiteSpace(request.CardNumber))
            return BadRequest(new { message = "Vui lòng nhập thông tin thẻ hoặc tài khoản hợp lệ." });

        var payments = await _paymentService.GetPaymentsByBookingIdAsync(request.BookingId);
        if (payments.Any(p => p.PaymentStatus == "Success"))
            return BadRequest(new { message = "Đơn đặt phòng này đã được thanh toán." });

        var discount = await _context.FolioItems
            .Where(f => f.BookingId == booking.BookingId && f.Category == "Discount")
            .SumAsync(f => f.UnitPrice * f.Quantity);
        var services = await _context.FolioItems
            .Where(f => f.BookingId == booking.BookingId && f.Category == "Service")
            .SumAsync(f => f.UnitPrice * f.Quantity);
        var payable = Math.Max(0, booking.TotalPrice - discount + services);
        var transactionId = "TXN-" + Guid.NewGuid().ToString("N")[..12].ToUpperInvariant();
        var payment = await _paymentService.CreatePaymentRecordAsync(
            request.BookingId,
            request.PaymentMethod,
            payable,
            transactionId,
            "Success");

        return Ok(new
        {
            message = "Thanh toán thành công.",
            bookingId = request.BookingId,
            status = "Success",
            transactionId = payment.TransactionId,
            paymentMethod = payment.PaymentMethod,
            amount = payment.Amount
        });
    }

    private async Task<Booking?> LoadOwnBooking(int bookingId)
    {
        return await _context.Bookings
            .Include(b => b.BookingRooms)
            .ThenInclude(br => br.Room)
            .ThenInclude(r => r.RoomType)
            .FirstOrDefaultAsync(b => b.BookingId == bookingId && b.CustomerId == ApiUser.Id(User));
    }

    private object ToBooking(Booking booking)
    {
        var room = booking.BookingRooms.Select(br => br.Room).FirstOrDefault();
        var nights = Math.Max(0, booking.CheckOutDate.DayNumber - booking.CheckInDate.DayNumber);
        var quote = room?.RoomType == null
            ? null
            : OccupancyPricing.Price(room.RoomType.TypeName, room.RoomType.MaxGuests, room.RoomType.BasePrice, booking.NumberOfGuests, nights);

        var lines = _context.FolioItems.Where(f => f.BookingId == booking.BookingId).ToList();
        var discount = lines.Where(f => f.Category == "Discount").Sum(f => f.UnitPrice * f.Quantity);
        var services = lines.Where(f => f.Category == "Service")
            .Select(f => new { name = f.Description, amount = f.UnitPrice * f.Quantity })
            .ToList();
        var servicesTotal = services.Sum(f => f.amount);
        return new
        {
            booking.BookingId,
            booking.Status,
            booking.TotalPrice,
            discount,
            services,
            servicesTotal,
            payable = Math.Max(0, booking.TotalPrice - discount + servicesTotal),
            booking.CheckInDate,
            booking.CheckOutDate,
            booking.NumberOfGuests,
            booking.GuestName,
            booking.GuestPhone,
            booking.GuestNote,
            booking.PaymentChoice,
            stayLabel = quote?.Plan.Label,
            roomCharge = quote?.RoomCharge,
            surcharge = quote?.Surcharge,
            summary = quote?.Summary,
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
        public string PaymentMethod { get; set; } = "Card";
        public string CardNumber { get; set; } = string.Empty;
    }
}
