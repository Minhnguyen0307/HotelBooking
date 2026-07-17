using System;
using System.Security.Claims;
using System.Threading.Tasks;
using HotelBooking.Application.Interfaces;
using HotelBooking.Infrastructure;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HotelBooking.Controllers
{
    [Authorize]
    public class PaymentController : Controller
    {
        private readonly IPaymentService _paymentService;
        private readonly HotelBookingDbContext _context;

        public PaymentController(IPaymentService paymentService, HotelBookingDbContext context)
        {
            _paymentService = paymentService;
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

        [HttpPost]
        public async Task<IActionResult> ProcessPayment(int bookingId, string paymentMethod, string cardNumber)
        {
            var booking = await _context.Bookings
                .FirstOrDefaultAsync(b => b.BookingId == bookingId && b.CustomerId == CurrentUserId);

            if (booking == null) return NotFound();

            // Simulate card validation
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
