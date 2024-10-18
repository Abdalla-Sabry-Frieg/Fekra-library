using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.Extensions.Options;
using System.Net.Mail;
using System.Net;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Net.Http;

namespace MyShop.web.Models
{
    public class EmailSender : IEmailSender
    {
        private readonly IConfiguration _configuration;

        public EmailSender(IConfiguration configuration)
        {
            _configuration = configuration;
        }
        public async Task SendEmailAsync(string email, string subject, string htmlMessage)
        {
            var message = new MailMessage();
            message.From = new MailAddress(_configuration["SMTP:Username"]);
            message.Subject = subject;
            message.To.Add(email);
            

            message.Body = $"<html><body>{htmlMessage}</body></html>";
            message.IsBodyHtml = true;

            try
            {
                using (var smtpClint = new SmtpClient(_configuration["SMTP:Host"]))
                {
                    smtpClint.Port = 587;
                    smtpClint.EnableSsl = true;
                    smtpClint.Credentials = new NetworkCredential(_configuration["SMTP:Username"], _configuration["SMTP:Password"]);

                    await smtpClint.SendMailAsync(message);
                    Console.WriteLine("Email sent successfully.");

                }
            }
            catch (Exception ex)
            {

                Console.WriteLine($"Error sending email: {ex.Message}");
                throw;
            }

          //  await smtpClint.SendMailAsync(message);  // Use async version

        }
    }

   
}
