using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using HotelBooking.Application.Interfaces;
using HotelBooking.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace HotelBooking.Application.Services
{
    public class PaymentService : IPaymentService
    {
        private readonly HotelBookingDbContext _context;

        public PaymentService(HotelBookingDbContext context)
        {
            _context = context;
        }

        public async Task<Payment> CreatePaymentRecordAsync(int bookingId, string paymentMethod, decimal amount, string transactionId, string status)
        {
            var payment = new Payment
            {
                BookingId = bookingId,
                PaymentMethod = paymentMethod,
                Amount = amount,
                TransactionId = transactionId,
                PaymentStatus = status,
                PaymentDate = status == "Success" ? DateTime.UtcNow : null,
                CreatedAt = DateTime.UtcNow
            };

            _context.Payments.Add(payment);

            if (status == "Success")
            {
                var booking = await _context.Bookings.FindAsync(bookingId);
                if (booking != null)
                {
                    booking.Status = "Confirmed";
                }
            }

            await _context.SaveChangesAsync();
            return payment;
        }

        public async Task<List<Payment>> GetPaymentsByBookingIdAsync(int bookingId)
        {
            return await _context.Payments
                .Where(p => p.BookingId == bookingId)
                .ToListAsync();
        }

        public async Task<Refund?> CreateRefundRecordAsync(int paymentId, decimal amount, string reason)
        {
            var payment = await _context.Payments.FindAsync(paymentId);
            if (payment == null) return null;

            var refund = new Refund
            {
                PaymentId = paymentId,
                Amount = amount,
                Reason = reason,
                Status = "Processed",
                RequestedAt = DateTime.UtcNow,
                ProcessedAt = DateTime.UtcNow
            };

            _context.Refunds.Add(refund);

            // Update payment status to indicate it was refunded/partially refunded
            payment.PaymentStatus = "Refunded";

            await _context.SaveChangesAsync();
            return refund;
        }

        public async Task<List<Refund>> GetRefundsByPaymentIdAsync(int paymentId)
        {
            return await _context.Refunds
                .Where(r => r.PaymentId == paymentId)
                .ToListAsync();
        }
    }
}
