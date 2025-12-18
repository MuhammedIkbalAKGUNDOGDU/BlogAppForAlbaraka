using MailKit.Net.Smtp;
using MimeKit;

namespace BlogApp.Services;

public class EmailService
{
    private readonly string _smtpHost;        // SMTP sunucu adresi (.env'den gelecek)
    private readonly int _smtpPort;           // SMTP port numarası (587 TLS için)
    private readonly string _smtpUsername;    // Gmail kullanıcı adı
    private readonly string _smtpPassword;    // Gmail App Password
    private readonly string _fromEmail;       // Gönderen email adresi
    private readonly string _fromName;        // Gönderen ismi

    public EmailService()
    {
        _smtpHost = Environment.GetEnvironmentVariable("SMTP_HOST") ?? "smtp.gmail.com";
        _smtpPort = int.Parse(Environment.GetEnvironmentVariable("SMTP_PORT") ?? "587");
        _smtpUsername = Environment.GetEnvironmentVariable("SMTP_USERNAME") ?? "";
        _smtpPassword = Environment.GetEnvironmentVariable("SMTP_PASSWORD") ?? "";
        _fromEmail = Environment.GetEnvironmentVariable("SMTP_FROM_EMAIL") ?? "";
        _fromName = Environment.GetEnvironmentVariable("SMTP_FROM_NAME") ?? "BlogApp";

        if (string.IsNullOrEmpty(_smtpUsername) || string.IsNullOrEmpty(_smtpPassword))
        {
            Console.WriteLine("UYARI: SMTP_USERNAME veya SMTP_PASSWORD .env dosyasında tanımlı değil!");
        }
        else
        {
            Console.WriteLine($"SMTP ayarları yüklendi: {_smtpHost}:{_smtpPort}");
        }
    }

    // Email gönderme 
    public async Task<bool> SendEmailAsync(string toEmail, string toName, string subject, string htmlBody)
    {
        try
        {
            Console.WriteLine($"Email gönderme denemesi başlatılıyor...");
            Console.WriteLine($"To: {toEmail}, From: {_fromEmail}");

            // SMTP ayarlarını kontrol et
            if (string.IsNullOrEmpty(_smtpUsername) || string.IsNullOrEmpty(_smtpPassword))
            {
                Console.WriteLine("HATA: SMTP_USERNAME veya SMTP_PASSWORD boş!");
                return false;
            }

            if (string.IsNullOrEmpty(_fromEmail))
            {
                Console.WriteLine("HATA: SMTP_FROM_EMAIL boş!");
                return false;
            }

            // MimeMessage oluştur (MailKit kullanarak)
            var email = new MimeMessage();
            email.From.Add(new MailboxAddress(_fromName, _fromEmail));  // Gönderen bilgisi
            email.To.Add(new MailboxAddress(toName, toEmail));           // Alıcı bilgisi
            email.Subject = subject;                                      // Email konusu
            email.Body = new TextPart("html")                            // HTML formatında body
            {
                Text = htmlBody
            };

            Console.WriteLine($"SMTP bağlantısı kuruluyor: {_smtpHost}:{_smtpPort}");

            // SMTP client oluştur ve bağlan
            using var client = new SmtpClient();
            await client.ConnectAsync(_smtpHost, _smtpPort, MailKit.Security.SecureSocketOptions.StartTls);  // TLS ile bağlan
            Console.WriteLine("SMTP bağlantısı başarılı");

            Console.WriteLine("SMTP kimlik doğrulaması yapılıyor...");
            Console.WriteLine($"Username: {_smtpUsername}, Password uzunluğu: {(_smtpPassword?.Length ?? 0)}");
            
            try
            {
                await client.AuthenticateAsync(_smtpUsername, _smtpPassword);  // Gmail ile kimlik doğrula
                Console.WriteLine("SMTP kimlik doğrulaması başarılı");
            }
            catch (Exception authEx)
            {
                Console.WriteLine($"═══════════════════════════════════════");
                Console.WriteLine($"SMTP KİMLİK DOĞRULAMA HATASI!");
                Console.WriteLine($"═══════════════════════════════════════");
                Console.WriteLine($"Hata: {authEx.Message}");
                Console.WriteLine($"Hata Tipi: {authEx.GetType().Name}");
                if (authEx.InnerException != null)
                {
                    Console.WriteLine($"Inner Exception: {authEx.InnerException.Message}");
                }
                Console.WriteLine($"Username: {_smtpUsername}");
                Console.WriteLine($"Password boş mu: {string.IsNullOrEmpty(_smtpPassword)}");
                Console.WriteLine($"Password uzunluğu: {(_smtpPassword?.Length ?? 0)}");
                Console.WriteLine($"═══════════════════════════════════════");
                throw;  // Hatayı yukarı fırlat
            }

            Console.WriteLine("Email gönderiliyor...");
            await client.SendAsync(email);                                 // Email'i gönder
            Console.WriteLine("Email başarıyla gönderildi");

            await client.DisconnectAsync(true);                            // Bağlantıyı kapat
            Console.WriteLine("SMTP bağlantısı kapatıldı");

            return true;  // Başarılı
        }
        catch (Exception ex)
        {
            // Hata durumunda detaylı log yaz
            Console.WriteLine($"═══════════════════════════════════════");
            Console.WriteLine($"EMAIL GÖNDERME HATASI!");
            Console.WriteLine($"═══════════════════════════════════════");
            Console.WriteLine($"Hata Mesajı: {ex.Message}");
            Console.WriteLine($"Hata Tipi: {ex.GetType().Name}");
            if (ex.InnerException != null)
            {
                Console.WriteLine($"Inner Exception: {ex.InnerException.Message}");
            }
            Console.WriteLine($"Stack Trace: {ex.StackTrace}");
            Console.WriteLine($"───────────────────────────────────────");
            Console.WriteLine($"SMTP Ayarları:");
            Console.WriteLine($"  Host: {_smtpHost}");
            Console.WriteLine($"  Port: {_smtpPort}");
            Console.WriteLine($"  Username: {_smtpUsername}");
            Console.WriteLine($"  Password: {(string.IsNullOrEmpty(_smtpPassword) ? "BOŞ!" : "***")}");
            Console.WriteLine($"  From Email: {_fromEmail}");
            Console.WriteLine($"  From Name: {_fromName}");
            Console.WriteLine($"───────────────────────────────────────");
            Console.WriteLine($"Email Bilgileri:");
            Console.WriteLine($"  To: {toEmail}");
            Console.WriteLine($"  To Name: {toName}");
            Console.WriteLine($"  Subject: {subject}");
            Console.WriteLine($"═══════════════════════════════════════");
            return false;
        }
    }
}
