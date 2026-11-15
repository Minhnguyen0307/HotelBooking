using HotelBooking.Application.DTOs;

namespace HotelBooking.Application.Interfaces
{
    public interface IBookingService
    {
        Task<BookingPreviewResult> GetBookingPreviewAsync(int customerId, BookingCreateDto dto);
        Task<List<PromoCodeOptionDto>> GetAvailablePromoCodesAsync(int customerId);
        Task<BookingResult> CreateBookingAsync(int customerId, BookingCreateDto dto);
        Task<List<BookingListItemDto>> GetCustomerBookingsAsync(int customerId);
        Task<BookingResult> CancelBookingAsync(int customerId, int bookingId, string? reason);
    }
}
