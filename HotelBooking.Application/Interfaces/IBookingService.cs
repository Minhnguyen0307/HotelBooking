using HotelBooking.Application.DTOs;

namespace HotelBooking.Application.Interfaces
{
    public interface IBookingService
    {
        Task<BookingConfirmDto?> GetBookingPreviewAsync(BookingCreateDto dto);
        Task<BookingResult> CreateBookingAsync(int customerId, BookingCreateDto dto);
        Task<List<BookingListItemDto>> GetCustomerBookingsAsync(int customerId);
        Task<BookingResult> CancelBookingAsync(int customerId, int bookingId, string? reason);
    }
}