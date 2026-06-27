namespace ResortBookingMVC.Interfaces
{
    public interface IEmailService
    {
        Task SendOtpAsync(string toEmail, string toName, string otp);
        Task SendBookingConfirmationAsync(string toEmail, string toName,
            string bookingCode, string resortName, string location,
            string checkIn, string checkOut, int nights, int rooms,
            string total, string deposit);
    }
}