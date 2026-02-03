using System.Net;
using System.Net.Mail;
using System.Threading.Tasks;
using LibraryStock.App.Models;
using Microsoft.Extensions.Configuration;

namespace LibraryStock.App.Clean.Services
{ 
    public interface IEmailService
    {
        // E-posta gönderme metodu "to" kime, "subject" konu, "body" içerik
        Task SendAsync(string to, string subject, string body, bool isHtml = false);
    }

    
    public class EmailService : IEmailService
    {
        // (appsettings.json’daki EmailSettings) okumak için
        private readonly IConfiguration _config;

        // Config’i ( uygulamanın ayarları) constructor üzerinden alıyoruz
        public EmailService(IConfiguration config)
        {
            _config = config;
        }

     
        public async Task SendAsync(string to, string subject, string body, bool isHtml = false)
        {
            // Ayar dosyasından değerleri çekiyoruz
            var smtpServer = _config["EmailSettings:SmtpServer"];      // Mail sunucusu
            var smtpPort = int.Parse(_config["EmailSettings:SmtpPort"] ?? "587"); // Port (varsayılan 587)
            var from = _config["EmailSettings:SenderEmail"];          
            var pass = _config["EmailSettings:SenderPassword"];       

            //  SMTP istemcisi oluştur
            using var client = new SmtpClient(smtpServer, smtpPort)
            {
                EnableSsl = true,                                     // Güvenli bağlantı
                Credentials = new NetworkCredential(from, pass)       // Kullanıcı adı & şifre
            };

            // Gönderilecek e-posta mesajı
            using var mail = new MailMessage
            {
                From = new MailAddress(from),    
                Subject = subject,               
                Body = body,                     
                IsBodyHtml = isHtml              // HTML mi düz metin mi?
            };

            mail.To.Add(to);

            await client.SendMailAsync(mail);
        }
    }
}
