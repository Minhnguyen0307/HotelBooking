using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using HotelBooking.Application.Interfaces;
using HotelBooking.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace HotelBooking.Application.Services
{
    public class HotelBookingAutomationWorker : BackgroundService
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogger<HotelBookingAutomationWorker> _logger;

        public HotelBookingAutomationWorker(IServiceScopeFactory scopeFactory, ILogger<HotelBookingAutomationWorker> logger)
        {
            _scopeFactory = scopeFactory;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("HotelBookingAutomationWorker đã khởi động.");

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    await RunAutomationsInternalAsync(stoppingToken);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Lỗi xảy ra trong quá trình chạy background automation.");
                }

                // Chạy định kỳ mỗi 1 tiếng
                await Task.Delay(TimeSpan.FromHours(1), stoppingToken);
            }
        }

        public async Task<(int RemindersSent, int ThankYouSent)> TriggerManualRunAsync()
        {
            return await RunAutomationsInternalAsync(CancellationToken.None);
        }

        private async Task<(int RemindersSent, int ThankYouSent)> RunAutomationsInternalAsync(CancellationToken cancellationToken)
        {
            using var scope = _scopeFactory.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<HotelBookingDbContext>();
            var emailService = scope.ServiceProvider.GetRequiredService<IEmailService>();

            var today = DateOnly.FromDateTime(DateTime.Today);
            var tomorrow = today.AddDays(1);
            int remindersSent = 0;
            int thankYouSent = 0;

            // 1. Gửi email nhắc nhở trước ngày nhận phòng 1 ngày
            var upcomingBookings = await db.Bookings
                .Include(b => b.Customer)
                .Include(b => b.BookingRooms).ThenInclude(br => br.Room).ThenInclude(r => r.Property)
                .Where(b => b.CheckInDate == tomorrow && (b.Status == "Confirmed" || b.Status == "Pending"))
                .ToListAsync(cancellationToken);

            foreach (var b in upcomingBookings)
            {
                var email = b.Customer?.Email;
                if (!string.IsNullOrWhiteSpace(email))
                {
                    try
                    {
                        var br = b.BookingRooms.FirstOrDefault();
                        var roomNumber = br?.Room?.RoomNumber ?? "–";
                        var propName = br?.Room?.Property?.Name ?? "HBS Luxury Hotel Đà Nẵng";
                        var address = br?.Room?.Property?.Address ?? "Đà Nẵng";
                        var guestName = b.GuestName ?? b.Customer?.FullName ?? "Quý khách";

                        await emailService.SendBookingReminderEmailAsync(email, guestName, b.BookingId, roomNumber, propName, address, b.CheckInDate);
                        remindersSent++;
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning("Không thể gửi email nhắc nhở đơn #{BookingId}: {Message}", b.BookingId, ex.Message);
                    }
                }
            }

            // 2. Gửi email cảm ơn và mời đánh giá sau khi Check-out
            var checkedOutBookings = await db.Bookings
                .Include(b => b.Customer)
                .Include(b => b.BookingRooms).ThenInclude(br => br.Room)
                .Where(b => b.CheckOutDate == today && b.Status == "CheckedOut")
                .ToListAsync(cancellationToken);

            foreach (var b in checkedOutBookings)
            {
                var email = b.Customer?.Email;
                if (!string.IsNullOrWhiteSpace(email))
                {
                    try
                    {
                        var br = b.BookingRooms.FirstOrDefault();
                        var roomId = br?.RoomId ?? 1;
                        var roomNumber = br?.Room?.RoomNumber ?? "–";
                        var guestName = b.GuestName ?? b.Customer?.FullName ?? "Quý khách";

                        await emailService.SendCheckoutThankYouEmailAsync(email, guestName, b.BookingId, roomId, roomNumber);
                        thankYouSent++;
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning("Không thể gửi email cảm ơn đơn #{BookingId}: {Message}", b.BookingId, ex.Message);
                    }
                }
            }

            _logger.LogInformation("Đã quét automation: {RemindersSent} nhắc nhở, {ThankYouSent} thư cảm ơn.", remindersSent, thankYouSent);
            return (remindersSent, thankYouSent);
        }
    }
}
