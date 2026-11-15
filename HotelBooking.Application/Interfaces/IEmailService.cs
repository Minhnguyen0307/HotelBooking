namespace HotelBooking.Application.Interfaces
{
    public interface IEmailService
    {
        Task SendPasswordResetEmailAsync(string toEmail, string resetLink);
        Task SendBookingReminderEmailAsync(string toEmail, string guestName, int bookingId, string roomNumber, string propertyName, string address, DateOnly checkInDate);
        Task SendCheckoutThankYouEmailAsync(string toEmail, string guestName, int bookingId, int roomId, string roomNumber);
    }
}
