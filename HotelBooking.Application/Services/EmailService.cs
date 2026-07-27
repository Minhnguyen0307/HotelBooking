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

        public async Task SendPasswordResetEmailAsync(string toEmail, string resetLink)
        {
            var subject = "Khôi phục mật khẩu - HBS Luxury Hotel";
            var body = $@"
                <div style='font-family: Arial, sans-serif; max-width: 500px; margin: auto;'>
                    <h2 style='color:#1a2b4c;'>Yêu cầu khôi phục mật khẩu</h2>
                    <p>Bạn (hoặc ai đó) đã yêu cầu đặt lại mật khẩu cho tài khoản HBS Luxury Hotel.</p>
                    <p>Nhấn vào nút bên dưới để đặt lại mật khẩu (liên kết có hiệu lực trong 24 giờ):</p>
                    <p style='text-align:center; margin: 24px 0;'>
                        <a href='{resetLink}' style='background:#1a2b4c; color:#fff; padding:12px 24px; text-decoration:none; border-radius:6px;'>
                            Đặt lại mật khẩu
                        </a>
                    </p>
                    <p>Nếu bạn không yêu cầu việc này, vui lòng bỏ qua email này.</p>
                </div>";

            using var message = new MailMessage
            {
                From = new MailAddress(_settings.SenderEmail, _settings.SenderName),
                Subject = subject,
                Body = body,
                IsBodyHtml = true
            };
            message.To.Add(toEmail);

            using var client = new SmtpClient(_settings.SmtpServer, _settings.SmtpPort)
            {
                Credentials = new NetworkCredential(_settings.SenderEmail, _settings.SenderPassword),
                EnableSsl = true
            };

            await client.SendMailAsync(message);
        }
    }
}
