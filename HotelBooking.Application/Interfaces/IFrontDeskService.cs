using HotelBooking.Application.DTOs;

namespace HotelBooking.Application.Interfaces
{
    public interface IFrontDeskService
    {
        Task<List<BookingListItemDto>> GetAllCheckedInAsync();
        Task<List<BookingListItemDto>> GetTodayArrivalsAsync();
        Task<List<BookingListItemDto>> GetTodayDeparturesAsync();
        Task<(bool Success, string? Error)> CheckInAsync(int bookingId);
        Task<(bool Success, string? Error)> CheckOutAsync(int bookingId);
        Task<List<BookingListItemDto>> GetCancelRequestsAsync();
        Task<(bool Success, string? Error)> ConfirmCancelAsync(int bookingId);
    }
}
