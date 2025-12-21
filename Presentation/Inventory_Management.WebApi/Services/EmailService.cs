using Inventory_Management.Application.Interfaces;
using System.Net;
using System.Net.Mail;

namespace Inventory_Management.WebApi.Services
{
    public class EmailService : IEmailService
    {
        private readonly IConfiguration _configuration;

        public EmailService(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public async Task SendEmailAsync(string to, string subject, string body)
        {
            try
            {
                var emailSettings = _configuration.GetSection("EmailSettings");

                var smtpHost = emailSettings["SmtpHost"];
                var senderEmail = emailSettings["SenderEmail"];
                var senderPassword = emailSettings["SenderPassword"];
                
                // Port parse işlemi güvenli hale getirildi
                if (!int.TryParse(emailSettings["SmtpPort"], out int smtpPort))
                {
                    smtpPort = 587; // Varsayılan port
                }

                // Ayarlar eksikse hata fırlatmayalım, sadece loglayalım.
                if (string.IsNullOrEmpty(smtpHost) || string.IsNullOrEmpty(senderEmail))
                {
                    Console.WriteLine($"[EmailService] Email settings are missing (Host: {smtpHost}, Email: {senderEmail}). Email sending skipped.");
                    return;
                }

                using (var client = new SmtpClient(smtpHost, smtpPort))
                {
                    client.EnableSsl = true;
                    client.Credentials = new NetworkCredential(senderEmail, senderPassword);

                    var mailMessage = new MailMessage
                    {
                        From = new MailAddress(senderEmail),
                        Subject = subject,
                        Body = body,
                        IsBodyHtml = true
                    };

                    mailMessage.To.Add(to);

                    await client.SendMailAsync(mailMessage);
                }
            }
            catch (Exception ex)
            {
                // Hata durumunda akışı bozmamak için hatayı yakalayıp logluyoruz.
                Console.WriteLine($"[EmailService] Failed to send email: {ex.Message}");
                // Geliştirme ortamında bu hatanın kullanıcıya yansımaması istendiği için throw yapmıyoruz.
            }
        }
    }
}
