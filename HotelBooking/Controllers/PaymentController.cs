using System;
using System.Security.Claims;
using System.Threading.Tasks;
using HotelBooking.Application.Interfaces;
using HotelBooking.Infrastructure;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Linq;

namespace HotelBooking.Controllers
{
    [Authorize]
    public class PaymentController : Controller
    {
        private readonly IPaymentService _paymentService;
        private readonly IVnPayService _vnPayService;
        private readonly HotelBookingDbContext _context;

        public PaymentController(IPaymentService paymentService, IVnPayService vnPayService, HotelBookingDbContext context)
        {
            _paymentService = paymentService;
            _vnPayService = vnPayService;
            _context = context;
        }

        private int CurrentUserId =>
            int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

        [HttpGet]
        public async Task<IActionResult> Pay(int bookingId)
        {
            var booking = await _context.Bookings
                .Include(b => b.BookingRooms)
                .ThenInclude(br => br.Room)
                .FirstOrDefaultAsync(b => b.BookingId == bookingId && b.CustomerId == CurrentUserId);

            if (booking == null) return NotFound();

            if (booking.Status != "Pending")
            {
                TempData["ErrorMessage"] = "Đơn đặt phòng này không ở trạng thái chờ thanh toán.";
                return RedirectToAction("Index", "Booking");
            }

            var payments = await _paymentService.GetPaymentsByBookingIdAsync(bookingId);
            if (payments.Any(p => p.PaymentStatus == "Success"))
            {
                TempData["SuccessMessage"] = "Đơn đặt phòng này đã được thanh toán.";
                return RedirectToAction("Index", "Booking");
            }

            return View(booking);
        }

        [HttpGet]
        public async Task<IActionResult> MockGateway(int bookingId, string paymentMethod)
        {
            var booking = await _context.Bookings
                .Include(b => b.BookingRooms)
                .ThenInclude(br => br.Room)
                .FirstOrDefaultAsync(b => b.BookingId == bookingId && b.CustomerId == CurrentUserId);

            if (booking == null) return NotFound();

            ViewBag.PaymentMethod = paymentMethod;
            return View(booking);
        }

        [HttpGet]
        public async Task<IActionResult> CreateVnPayPayment(int bookingId)
        {
            var booking = await _context.Bookings
                .FirstOrDefaultAsync(b => b.BookingId == bookingId && b.CustomerId == CurrentUserId);

            if (booking == null) return NotFound();

            var transactionRef = $"{bookingId}{DateTime.UtcNow.AddHours(7):yyyyMMddHHmmss}";
            var returnUrl = "https://localhost:7023/Payment/VnPayReturn?bookingId=" + bookingId;
            var clientIp = Request.HttpContext.Connection.RemoteIpAddress?.ToString() ?? "127.0.0.1";
            Console.WriteLine($"VNPAY returnUrl generated: {returnUrl}");
            var paymentUrl = _vnPayService.CreatePaymentUrl(
                bookingId,
                booking.TotalPrice,
                $"Thanh toan don hang {booking.BookingId}",
                returnUrl,
                transactionRef,
                clientIp);

            return Redirect(paymentUrl);
        }

        [HttpPost]
        public async Task<IActionResult> ProcessPayment(int bookingId, string paymentMethod, string cardNumber)
        {
            var booking = await _context.Bookings
                .FirstOrDefaultAsync(b => b.BookingId == bookingId && b.CustomerId == CurrentUserId);

            if (booking == null) return NotFound();

            if (paymentMethod == "VNPAY")
            {
                return RedirectToAction(nameof(CreateVnPayPayment), new { bookingId });
            }

            if (string.IsNullOrWhiteSpace(cardNumber))
            {
                TempData["ErrorMessage"] = "Vui lòng nhập thông tin thẻ hoặc tài khoản hợp lệ.";
                return RedirectToAction("MockGateway", new { bookingId, paymentMethod });
            }

            var transactionId = "TXN-" + Guid.NewGuid().ToString("N").Substring(0, 12).ToUpper();
            var payment = await _paymentService.CreatePaymentRecordAsync(
                bookingId,
                paymentMethod,
                booking.TotalPrice,
                transactionId,
                "Success"
            );

            return RedirectToAction("Callback", new {
                bookingId,
                status = "Success",
                transactionId = payment.TransactionId,
                paymentMethod = payment.PaymentMethod
            });
        }

        [HttpGet]
        public async Task<IActionResult> VnPayReturn(int bookingId, string? vnp_ResponseCode, string? vnp_TransactionStatus, string? vnp_TxnRef, string? vnp_Amount, string? vnp_TransactionNo, string? vnp_SecureHash)
        {
            var booking = await _context.Bookings
                .FirstOrDefaultAsync(b => b.BookingId == bookingId && b.CustomerId == CurrentUserId);

            if (booking == null) return NotFound();

            var query = Request.Query;
            Console.WriteLine("VNPAY callback query string: " + Request.QueryString.Value);
            foreach (var item in query)
            {
                Console.WriteLine($"VNPAY callback param: {item.Key}={item.Value}");
            }
            var isValid = _vnPayService.ValidateSignature(query, out _);

            if (!isValid)
            {
                TempData["ErrorMessage"] = "Chữ ký VNPAY không hợp lệ.";
                return RedirectToAction(nameof(Callback), new { bookingId, status = "Failed", transactionId = vnp_TxnRef, paymentMethod = "VNPAY" });
            }

            if (vnp_ResponseCode == "00" && vnp_TransactionStatus == "00")
            {
                var transactionId = vnp_TransactionNo ?? vnp_TxnRef ?? $"TXN-{Guid.NewGuid():N}";
                await _paymentService.CreatePaymentRecordAsync(
                    bookingId,
                    "VNPAY",
                    booking.TotalPrice,
                    transactionId,
                    "Success"
                );

                return RedirectToAction(nameof(Callback), new {
                    bookingId,
                    status = "Success",
                    transactionId,
                    paymentMethod = "VNPAY"
                });
            }

            return RedirectToAction(nameof(Callback), new {
                bookingId,
                status = "Failed",
                transactionId = vnp_TxnRef,
                paymentMethod = "VNPAY"
            });
        }

        [HttpGet]
        public IActionResult Callback(int bookingId, string status, string transactionId, string paymentMethod)
        {
            ViewBag.BookingId = bookingId;
            ViewBag.Status = status;
            ViewBag.TransactionId = transactionId;
            ViewBag.PaymentMethod = paymentMethod;
            return View();
        }
    }
}
