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
        // New methods
        Task<List<RoomMatrixFloorDto>> GetRoomMatrixAsync();
        Task<(bool Success, string? Error)> MarkRoomCleanAsync(int roomId);
        Task<(bool Success, string? Error, int? BookingId)> CreateWalkInBookingAsync(WalkInBookingDto dto);
        Task<(bool Success, string? Error)> TransferRoomAsync(int bookingId, int newRoomId);
        Task<(bool Success, string? Error)> ExtendStayAsync(int bookingId, DateOnly newCheckOutDate);
        Task<InvoiceDto?> GetInvoiceAsync(int bookingId);
    }
}
