using Microsoft.AspNetCore.Http;

namespace HotelBooking.Application.Interfaces
{
    public interface IVnPayService
    {
        string CreatePaymentUrl(int bookingId, decimal amount, string orderInfo, string returnUrl, string transactionRef, string ipAddress = "127.0.0.1");
        bool ValidateSignature(IQueryCollection query, out string? receivedHash);
    }
}
