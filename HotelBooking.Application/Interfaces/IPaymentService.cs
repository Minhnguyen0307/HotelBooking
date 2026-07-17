using System.Collections.Generic;
using System.Threading.Tasks;
using HotelBooking.Infrastructure;

namespace HotelBooking.Application.Interfaces
{
    public interface IPaymentService
    {
        Task<Payment> CreatePaymentRecordAsync(int bookingId, string paymentMethod, decimal amount, string transactionId, string status);
        Task<List<Payment>> GetPaymentsByBookingIdAsync(int bookingId);
        Task<Refund?> CreateRefundRecordAsync(int paymentId, decimal amount, string reason);
        Task<List<Refund>> GetRefundsByPaymentIdAsync(int paymentId);
    }
}
