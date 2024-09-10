using System.Net;
using System.Net.Mail;

namespace WebAPI_LoginApp.Sevices
{
    public class EmailService
    {
        private readonly IConfiguration configuration;
        public EmailService(IConfiguration _configuration)
        {
            configuration = _configuration;
        }

        public async Task SendEmailAsyn(string toEmail, string subject, string body)
        {
            var smtpConfig = configuration.GetSection("Smtp");

            using (var client = new SmtpClient(smtpConfig["Host"], int.Parse(smtpConfig["Port"])))
            {
                client.Credentials = new NetworkCredential(smtpConfig["Username"], smtpConfig["Password"]);
                client.EnableSsl = bool.Parse(smtpConfig["EnableSSL"]);

                var mailMessage = new MailMessage
                {
                    From = new MailAddress(smtpConfig["Username"]),
                    Subject = subject,
                    Body = body,
                    IsBodyHtml = true
                };
                mailMessage.To.Add(toEmail);

                await client.SendMailAsync(mailMessage);
            }
        }
    }
}
