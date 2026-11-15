using System.Net;
using System.Net.Mail;
using HotelBooking.Application.Interfaces;
using HotelBooking.Application.Settings;
using Microsoft.Extensions.Options;

namespace HotelBooking.Application.Services
{
    public class EmailService : IEmailService
    {
        private readonly EmailSettings _settings;

        public EmailService(IOptions<EmailSettings> settings)
        {
            _settings = settings.Value;
        }

        private SmtpClient CreateClient()
        {
            return new SmtpClient(_settings.SmtpServer, _settings.SmtpPort)
            {
                Credentials = new NetworkCredential(_settings.SenderEmail, _settings.SenderPassword),
                EnableSsl = true
            };
        }

        public async Task SendPasswordResetEmailAsync(string toEmail, string resetLink)
        {
            var subject = "Khôi phục mật khẩu - HBS Luxury Hotel";
            var body = $@"
                <div style='font-family: Arial, sans-serif; max-width: 500px; margin: auto; padding: 20px; border: 1px solid #e0ede9; border-radius: 8px;'>
                    <h2 style='color:#1f4d45;'>Yêu cầu khôi phục mật khẩu</h2>
                    <p>Bạn (hoặc ai đó) đã yêu cầu đặt lại mật khẩu cho tài khoản HBS Luxury Hotel.</p>
                    <p>Nhấn vào nút bên dưới để đặt lại mật khẩu (liên kết có hiệu lực trong 24 giờ):</p>
                    <p style='text-align:center; margin: 24px 0;'>
                        <a href='{resetLink}' style='background:#1f4d45; color:#fff; padding:12px 24px; text-decoration:none; border-radius:6px; font-weight: bold;'>
                            Đặt lại mật khẩu
                        </a>
                    </p>
                    <p style='font-size: 0.85em; color: #777;'>Nếu bạn không yêu cầu việc này, vui lòng bỏ qua email này.</p>
                </div>";

            using var message = new MailMessage
            {
                From = new MailAddress(_settings.SenderEmail, _settings.SenderName),
                Subject = subject,
                Body = body,
                IsBodyHtml = true
            };
            message.To.Add(toEmail);

            using var client = CreateClient();
            await client.SendMailAsync(message);
        }

        public async Task SendBookingReminderEmailAsync(string toEmail, string guestName, int bookingId, string roomNumber, string propertyName, string address, DateOnly checkInDate)
        {
            var subject = $"[Nhắc nhở nhận phòng] Kỳ nghỉ sắp tới tại {propertyName} - Đơn #{bookingId}";
            var body = $@"
                <div style='font-family: Arial, sans-serif; max-width: 600px; margin: auto; padding: 24px; border: 1px solid #1f4d45; border-radius: 10px;'>
                    <div style='text-align: center; border-bottom: 2px solid #1f4d45; padding-bottom: 12px; margin-bottom: 20px;'>
                        <h1 style='color:#1f4d45; margin:0;'>HBS Luxury Hotel</h1>
                        <p style='color:#666; margin:4px 0 0;'>Trải nghiệm nghỉ dưỡng đẳng cấp</p>
                    </div>
                    <p>Kính gửi quý khách <strong>{guestName}</strong>,</p>
                    <p>HBS Luxury Hotel xin nhắc nhở về kỳ nghỉ sắp tới của quý khách vào ngày mai: <strong>{checkInDate:dd/MM/yyyy}</strong>.</p>
                    <div style='background: #f7faf9; border-left: 4px solid #1f4d45; padding: 16px; margin: 20px 0;'>
                        <p style='margin: 4px 0;'><strong>Mã đơn đặt phòng:</strong> #{bookingId}</p>
                        <p style='margin: 4px 0;'><strong>Phòng số:</strong> {roomNumber}</p>
                        <p style='margin: 4px 0;'><strong>Cơ sở:</strong> {propertyName}</p>
                        <p style='margin: 4px 0;'><strong>Địa chỉ:</strong> {address}</p>
                        <p style='margin: 4px 0;'><strong>Giờ nhận phòng tiêu chuẩn:</strong> 14:00</p>
                    </div>
                    <p>Nếu quý khách có yêu cầu đặc biệt về giờ đến hoặc dịch vụ đón tiễn, vui lòng liên hệ tổng đài <strong>1900-8888</strong>.</p>
                    <p style='margin-top: 24px;'>Trân trọng,<br/><strong>Ban Quản lý HBS Luxury Hotel</strong></p>
                </div>";

            using var message = new MailMessage
            {
                From = new MailAddress(_settings.SenderEmail, _settings.SenderName),
                Subject = subject,
                Body = body,
                IsBodyHtml = true
            };
            message.To.Add(toEmail);

            using var client = CreateClient();
            await client.SendMailAsync(message);
        }

        public async Task SendCheckoutThankYouEmailAsync(string toEmail, string guestName, int bookingId, int roomId, string roomNumber)
        {
            var subject = $"Cảm ơn quý khách đã lưu trú tại HBS Luxury Hotel - Đơn #{bookingId}";
            var body = $@"
                <div style='font-family: Arial, sans-serif; max-width: 600px; margin: auto; padding: 24px; border: 1px solid #c4a574; border-radius: 10px;'>
                    <div style='text-align: center; border-bottom: 2px solid #c4a574; padding-bottom: 12px; margin-bottom: 20px;'>
                        <h1 style='color:#1f4d45; margin:0;'>HBS Luxury Hotel</h1>
                    </div>
                    <p>Kính gửi quý khách <strong>{guestName}</strong>,</p>
                    <p>Chân thành cảm ơn quý khách đã tin tưởng và lựa chọn lưu trú tại phòng <strong>{roomNumber}</strong> của HBS Luxury Hotel.</p>
                    <p>Chúng tôi rất mong quý khách đã có một kỳ nghỉ thoải mái và đáng nhớ. Ý kiến đóng góp của quý khách là động lực quý báu để khách sạn không ngừng nâng cao chất lượng dịch vụ.</p>
                    <p style='text-align: center; margin: 30px 0;'>
                        <a href='http://localhost:3000/rooms/{roomId}' style='background: #c4a574; color: #fff; padding: 12px 28px; text-decoration: none; border-radius: 6px; font-weight: bold;'>
                            ★ Đánh giá trải nghiệm của bạn
                        </a>
                    </p>
                    <p>Rất hân hạnh được đón tiếp quý khách trong những chuyến đi tiếp theo!</p>
                    <p style='margin-top: 24px;'>Trân trọng,<br/><strong>Đội ngũ Chăm sóc Khách hàng HBS Luxury Hotel</strong></p>
                </div>";

            using var message = new MailMessage
            {
                From = new MailAddress(_settings.SenderEmail, _settings.SenderName),
                Subject = subject,
                Body = body,
                IsBodyHtml = true
            };
            message.To.Add(toEmail);

            using var client = CreateClient();
            await client.SendMailAsync(message);
        }
    }
}
