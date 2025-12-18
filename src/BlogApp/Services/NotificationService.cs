using BlogApp.Data;
using BlogApp.Models;
using Microsoft.EntityFrameworkCore;

namespace BlogApp.Services;

public class NotificationService
{
    private readonly EmailService _emailService;
    private readonly IServiceProvider _serviceProvider;

    public NotificationService(EmailService emailService, IServiceProvider serviceProvider)
    {
        _emailService = emailService;
        _serviceProvider = serviceProvider;
    }

    // Ana bildirim gönderme metodu
    public async Task SendNotificationAsync(NotificationType notificationType, int userId, int? postId = null, Dictionary<string, string>? additionalData = null)
    {
        try
        {
            // Scoped DbContext oluştur
            using var scope = _serviceProvider.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();

            // Kullanıcıyı bul
            var user = await context.Users.FindAsync(userId);
            if (user == null)
            {
                Console.WriteLine($"Bildirim gönderilemedi: Kullanıcı bulunamadı (UserId: {userId})");
                return;
            }

            // Post bilgisi gerekliyse al
            BlogPost? post = null;
            if (postId.HasValue)
            {
                post = await context.BlogPosts
                    .Include(p => p.Category)
                    .FirstOrDefaultAsync(p => p.Id == postId.Value);
            }

            // Email içeriğini hazırla
            var emailContent = GetEmailContent(notificationType, user, post, additionalData);
            if (emailContent == null)
            {
                Console.WriteLine($"Bildirim gönderilemedi: Email içeriği oluşturulamadı (Type: {notificationType})");
                return;
            }

            // Email gönder
            var subject = GetEmailSubject(notificationType);
            await _emailService.SendEmailAsync(
                user.Email,
                $"{user.FirstName} {user.LastName}",
                subject,
                emailContent
            );

            Console.WriteLine($"Bildirim gönderildi: {notificationType} - Kullanıcı: {user.Email}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Bildirim gönderme hatası ({notificationType}): {ex.Message}");
            // Hata olsa bile admin işlemi devam etsin
        }
    }

    // Email konusu
    private string GetEmailSubject(NotificationType notificationType)
    {
        return notificationType switch
        {
            NotificationType.UserBanned => "Hesabınız Yasaklandı",
            NotificationType.UserSuspended => "Hesabınız Askıya Alındı",
            NotificationType.PostApproved => "Yazınız Onaylandı",
            NotificationType.PostUnpublished => "Yazınız Yayından Kaldırıldı",
            NotificationType.PostDeleted => "Yazınız Silindi",
            _ => "BlogApp Bildirimi"
        };
    }

    // Email içeriği hazırlama
    private string? GetEmailContent(NotificationType notificationType, User user, BlogPost? post, Dictionary<string, string>? additionalData)
    {
        var appPort = Environment.GetEnvironmentVariable("APP_PORT") ?? "5055";
        var baseUrl = $"http://localhost:{appPort}";

        return notificationType switch
        {
            NotificationType.UserBanned => GetUserBannedEmail(user),
            NotificationType.UserSuspended => GetUserSuspendedEmail(user),
            NotificationType.PostApproved => GetPostApprovedEmail(user, post, baseUrl),
            NotificationType.PostUnpublished => GetPostUnpublishedEmail(user, post, baseUrl),
            NotificationType.PostDeleted => GetPostDeletedEmail(user, post),
            _ => null
        };
    }

    // Kullanıcı yasaklandı email
    private string GetUserBannedEmail(User user)
    {
        return $@"
            <html>
            <body style='font-family: Arial, sans-serif; line-height: 1.6; color: #333;'>
                <div style='max-width: 600px; margin: 0 auto; padding: 20px;'>
                    <h2 style='color: #dc3545;'>Hesabınız Yasaklandı</h2>
                    <p>Merhaba {user.FirstName} {user.LastName},</p>
                    <p>Maalesef hesabınız yasaklanmıştır. Bu nedenle giriş yapamaz ve platformu kullanamazsınız.</p>
                    <p>Bu kararla ilgili sorularınız için lütfen yönetici ile iletişime geçin.</p>
                    <hr style='border: none; border-top: 1px solid #eee; margin: 20px 0;'>
                    <p style='color: #666; font-size: 12px;'>Bu otomatik bir e-postadır. Lütfen yanıtlamayın.</p>
                </div>
            </body>
            </html>";
    }

    // Kullanıcı askıya alındı email
    private string GetUserSuspendedEmail(User user)
    {
        return $@"
            <html>
            <body style='font-family: Arial, sans-serif; line-height: 1.6; color: #333;'>
                <div style='max-width: 600px; margin: 0 auto; padding: 20px;'>
                    <h2 style='color: #ffc107;'>Hesabınız Askıya Alındı</h2>
                    <p>Merhaba {user.FirstName} {user.LastName},</p>
                    <p>Hesabınız geçici olarak(5 gün süreyle) askıya alınmıştır. Bu nedenle şu anda giriş yapamaz ve platformu kullanamazsınız.</p>
                    <p>Bu durumla ilgili sorularınız için lütfen yönetici ile iletişime geçin.</p>
                    <hr style='border: none; border-top: 1px solid #eee; margin: 20px 0;'>
                    <p style='color: #666; font-size: 12px;'>Bu otomatik bir e-postadır. Lütfen yanıtlamayın.</p>
                </div>
            </body>
            </html>";
    }

    // Yazı onaylandı email
    private string GetPostApprovedEmail(User user, BlogPost? post, string baseUrl)
    {
        if (post == null) return string.Empty;

        var postUrl = $"{baseUrl}/BlogPost/Details?id={post.Id}";

        return $@"
            <html>
            <body style='font-family: Arial, sans-serif; line-height: 1.6; color: #333;'>
                <div style='max-width: 600px; margin: 0 auto; padding: 20px;'>
                    <h2 style='color: #28a745;'>Yazınız Onaylandı</h2>
                    <p>Merhaba {user.FirstName} {user.LastName},</p>
                    <p>Harika haber! <strong>""{post.Title}""</strong> başlıklı yazınız admin tarafından onaylandı ve yayınlandı.</p>
                    <p style='text-align: center; margin: 30px 0;'>
                        <a href='{postUrl}' style='background-color: #28a745; color: white; padding: 12px 24px; text-decoration: none; border-radius: 5px; display: inline-block;'>Yazıyı Görüntüle</a>
                    </p>
                    <p>Yazınızı okumak için yukarıdaki bağlantıya tıklayabilirsiniz.</p>
                    <hr style='border: none; border-top: 1px solid #eee; margin: 20px 0;'>
                    <p style='color: #666; font-size: 12px;'>Bu otomatik bir e-postadır. Lütfen yanıtlamayın.</p>
                </div>
            </body>
            </html>";
    }

    // Yazı yayından kaldırıldı email
    private string GetPostUnpublishedEmail(User user, BlogPost? post, string baseUrl)
    {
        if (post == null) return string.Empty;

        return $@"
            <html>
            <body style='font-family: Arial, sans-serif; line-height: 1.6; color: #333;'>
                <div style='max-width: 600px; margin: 0 auto; padding: 20px;'>
                    <h2 style='color: #ffc107;'>Yazınız Yayından Kaldırıldı</h2>
                    <p>Merhaba {user.FirstName} {user.LastName},</p>
                    <p><strong>""{post.Title}""</strong> başlıklı yazınız yayından kaldırılmıştır ve şu anda draft durumundadır.</p>
                    <p>Yazınızı tekrar düzenleyip admin onayına gönderebilirsiniz.</p>
                    <p>Bu durumla ilgili sorularınız için lütfen yönetici ile iletişime geçin.</p>
                    <hr style='border: none; border-top: 1px solid #eee; margin: 20px 0;'>
                    <p style='color: #666; font-size: 12px;'>Bu otomatik bir e-postadır. Lütfen yanıtlamayın.</p>
                </div>
            </body>
            </html>";
    }

    // Yazı silindi email
    private string GetPostDeletedEmail(User user, BlogPost? post)
    {
        if (post == null) return string.Empty;

        return $@"
            <html>
            <body style='font-family: Arial, sans-serif; line-height: 1.6; color: #333;'>
                <div style='max-width: 600px; margin: 0 auto; padding: 20px;'>
                    <h2 style='color: #dc3545;'>Yazınız Silindi</h2>
                    <p>Merhaba {user.FirstName} {user.LastName},</p>
                    <p><strong>""{post.Title}""</strong> başlıklı yazınız admin tarafından kalıcı olarak silinmiştir.</p>
                    <p>Bu işlem geri alınamaz. Yazınız artık platformda görünmemektedir.</p>
                    <p>Bu durumla ilgili sorularınız için lütfen yönetici ile iletişime geçin.</p>
                    <hr style='border: none; border-top: 1px solid #eee; margin: 20px 0;'>
                    <p style='color: #666; font-size: 12px;'>Bu otomatik bir e-postadır. Lütfen yanıtlamayın.</p>
                </div>
            </body>
            </html>";
    }
}




