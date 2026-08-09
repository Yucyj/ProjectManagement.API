using Microsoft.Extensions.Configuration;
using System;
using System.Linq;
using System.Net;
using System.Net.Mail;
using System.Threading.Tasks;

namespace ProjectManagement.API.Services
{
    public class EmailService : IEmailService
    {
        private readonly IConfiguration _configuration;

        public EmailService(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public async Task SendEmailAsync(string email, string subject, string body)
        {
            var smtpServer = _configuration["EmailSettings:SmtpServer"] ?? "smtp.gmail.com";
            var portStr = _configuration["EmailSettings:Port"] ?? "587";
            int.TryParse(portStr, out int port);
            var senderEmail = _configuration["EmailSettings:SenderEmail"] ?? "your-gmail@gmail.com";
            var senderName = _configuration["EmailSettings:SenderName"] ?? "ProSync Security";
            var username = _configuration["EmailSettings:Username"] ?? "your-gmail@gmail.com";
            var password = _configuration["EmailSettings:Password"] ?? "your-app-password";

            // If SMTP credentials are default or not set, skip attempting to connect to avoid long timeouts
            if (username == "your-gmail@gmail.com" || string.IsNullOrEmpty(password) || password == "your-app-password")
            {
                Console.WriteLine($"[SMTP NOT CONFIG]: Skip sending email to {email}. Subject: {subject}");
                return;
            }

            try
            {
                using (var client = new SmtpClient(smtpServer, port))
                {
                    client.Credentials = new NetworkCredential(username, password);
                    client.EnableSsl = true;

                    var mailMessage = new MailMessage
                    {
                        From = new MailAddress(senderEmail, senderName),
                        Subject = subject,
                        Body = body,
                        IsBodyHtml = true
                    };

                    mailMessage.To.Add(email);

                    await client.SendMailAsync(mailMessage);
                    Console.WriteLine($"[EMAIL SENT]: Successfully sent email to {email}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[EMAIL SEND FAILURE]: Failed to send email to {email}. Error: {ex.Message}");
            }
        }
    }
}
