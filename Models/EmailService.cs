using System.Net;
using System.Net.Mail;
using Microsoft.Extensions.Configuration;
using ResortBookingMVC.Interfaces;

namespace ResortBookingMVC.Models
{
    public class EmailService : IEmailService
    {
        private readonly IConfiguration _config;
        private readonly IWebHostEnvironment _env;

        public EmailService(IConfiguration config, IWebHostEnvironment env)
        {
            _config = config;
            _env = env;
        }

        // ── Gửi OTP ──
        public async Task SendOtpAsync(string toEmail, string toName, string otp)
        {
            var body = await LoadTemplate("otp-email.html");
            body = body
                .Replace("{{NAME}}", toName)
                .Replace("{{OTP}}", otp)
                .Replace("{{YEAR}}", DateTime.Now.Year.ToString());

            await Send(toEmail, toName,
                $"[HolidayBooking] Mã OTP đặt lại mật khẩu: {otp}", body);
        }

        // ── Gửi xác nhận đặt phòng ──
        public async Task SendBookingConfirmationAsync(
            string toEmail, string toName,
            string bookingCode, string resortName, string location,
            string checkIn, string checkOut, int nights, int rooms,
            string total, string deposit)
        {
            var body = await LoadTemplate("booking-confirmation.html");
            body = body
                .Replace("{{NAME}}", toName)
                .Replace("{{BOOKING_CODE}}", bookingCode)
                .Replace("{{RESORT_NAME}}", resortName)
                .Replace("{{LOCATION}}", location)
                .Replace("{{CHECK_IN}}", checkIn)
                .Replace("{{CHECK_OUT}}", checkOut)
                .Replace("{{NIGHTS}}", nights.ToString())
                .Replace("{{ROOMS}}", rooms.ToString())
                .Replace("{{TOTAL}}", total)
                .Replace("{{DEPOSIT}}", deposit)
                .Replace("{{YEAR}}", DateTime.Now.Year.ToString());

            await Send(toEmail, toName,
                $"[HolidayBooking] Xác nhận đặt phòng #{bookingCode}", body);
        }

        // ── Helper: đọc template ──
        private async Task<string> LoadTemplate(string fileName)
        {
            var path = Path.Combine(_env.WebRootPath, "email-templates", fileName);
            return await File.ReadAllTextAsync(path);
        }

        // ── Helper: gửi mail ──
        private async Task Send(string toEmail, string toName, string subject, string body)
        {
            var s = _config.GetSection("EmailSettings");
            var host = s["SmtpHost"]!;
            var port = int.Parse(s["SmtpPort"]!);
            var user = s["SmtpUser"]!;
            var pass = s["SmtpPassword"]!;
            var from = s["FromEmail"]!;
            var fromName = s["FromName"] ?? "HolidayBooking";

            using var smtp = new SmtpClient(host, port);
            smtp.EnableSsl = true;
            smtp.Credentials = new NetworkCredential(user, pass);

            var msg = new MailMessage
            {
                From = new MailAddress(from, fromName),
                Subject = subject,
                Body = body,
                IsBodyHtml = true
            };
            msg.To.Add(new MailAddress(toEmail, toName));

            await smtp.SendMailAsync(msg);
        }
    }
}