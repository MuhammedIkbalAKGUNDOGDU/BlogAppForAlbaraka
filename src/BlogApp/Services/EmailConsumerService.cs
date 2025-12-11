using Microsoft.EntityFrameworkCore;
using BlogApp.Data;
using BlogApp.Models;

namespace BlogApp.Services;

public class EmailConsumerService : BackgroundService
{
    private readonly RabbitMQService _rabbitMQService;  // RabbitMQ servisi
    private readonly EmailService _emailService;       // Email servisi
    private readonly IServiceProvider _serviceProvider; // DbContext için service provider

    public EmailConsumerService(
        RabbitMQService rabbitMQService,
        EmailService emailService,
        IServiceProvider serviceProvider)
    {
        _rabbitMQService = rabbitMQService;
        _emailService = emailService;
        _serviceProvider = serviceProvider;
    }

    // BackgroundService'in ana metodu - servis başladığında çalışır
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        // RabbitMQ'dan mesaj dinlemeye başla
        _rabbitMQService.StartConsuming(async (postId, userId) =>
        {
            // Her mesaj için yeni scope oluştur (DbContext için)
            using var scope = _serviceProvider.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();

            try
            {
                // Blog post'u bul
                var post = await context.BlogPosts
                    .Include(p => p.User)  // Yazar bilgisini de getir
                    .FirstOrDefaultAsync(p => p.Id == postId);

                if (post == null)
                {
                    Console.WriteLine($"Post bulunamadı: {postId}");
                    return false;
                }

                // Takipçi kullanıcıyı bul
                var follower = await context.Users.FindAsync(userId);
                if (follower == null)
                {
                    Console.WriteLine($"Kullanıcı bulunamadı: {userId}");
                    return false;
                }

                // .env'den port bilgisini al
                var appPort = Environment.GetEnvironmentVariable("APP_PORT") ?? "5055";
                var baseUrl = $"http://localhost:{appPort}";  // Base URL oluştur
                var postUrl = $"{baseUrl}/BlogPost/Details?id={post.Id}";  // Blog post detay URL'i

                // Email içeriği oluştur
                var subject = $"Yeni Blog Yazısı: {post.Title}";
                var htmlBody = $@"
                    <html>
                    <body style='font-family: Arial, sans-serif; padding: 20px; background-color: #f5f5f5;'>
                        <div style='max-width: 600px; margin: 0 auto; background-color: white; padding: 30px; border-radius: 10px; box-shadow: 0 2px 4px rgba(0,0,0,0.1);'>
                            <h2 style='color: #333;'>Merhaba {follower.FirstName}!</h2>
                            <p style='color: #666; line-height: 1.6;'>Takip ettiğiniz <strong>{post.User?.FirstName} {post.User?.LastName}</strong> yeni bir blog yazısı yayınladı:</p>
                            <div style='background-color: #f8f9fa; padding: 20px; border-radius: 5px; margin: 20px 0;'>
                                <h3 style='color: #007bff; margin-top: 0;'>{post.Title}</h3>
                                <p style='color: #555;'>{post.Content.Substring(0, Math.Min(200, post.Content.Length))}...</p>
                            </div>
                            <div style='text-align: center; margin: 30px 0;'>
                                <a href='{postUrl}' style='background-color: #007bff; color: white; padding: 12px 30px; text-decoration: none; border-radius: 5px; display: inline-block; font-weight: bold;'>Yazıyı Oku</a>
                            </div>
                            <hr style='border: none; border-top: 1px solid #eee; margin: 30px 0;'>
                            <p style='color: #999; font-size: 12px; text-align: center; margin: 0;'>Bu email BlogApp tarafından gönderilmiştir.</p>
                            <p style='color: #999; font-size: 11px; text-align: center; margin-top: 10px;'>
                                <a href='{postUrl}' style='color: #999;'>{postUrl}</a>
                            </p>
                        </div>
                    </body>
                    </html>";

                // Email gönder
                var emailSent = await _emailService.SendEmailAsync(
                    follower.Email,
                    $"{follower.FirstName} {follower.LastName}",
                    subject,
                    htmlBody
                );

                if (emailSent)
                {
                    // EmailQueue kaydı oluştur (log için)
                    var emailQueue = new EmailQueue
                    {
                        PostId = postId,
                        UserId = userId,
                        Status = "Sent",
                        SentAt = DateTime.UtcNow
                    };
                    context.EmailQueues.Add(emailQueue);
                    await context.SaveChangesAsync();

                    Console.WriteLine($"Email gönderildi: {follower.Email} - Post: {post.Title}");
                    return true;
                }
                else
                {
                    // Başarısız durumda detaylı log kaydet
                    var errorMsg = "Email gönderilemedi - EmailService false döndü";
                    Console.WriteLine($"Email gönderilemedi - PostId: {postId}, UserId: {userId}, Email: {follower.Email}");
                    
                    var emailQueue = new EmailQueue
                    {
                        PostId = postId,
                        UserId = userId,
                        Status = "Failed",
                        ErrorMessage = errorMsg
                    };
                    context.EmailQueues.Add(emailQueue);
                    await context.SaveChangesAsync();

                    return false;
                }
            }
            catch (Exception ex)
            {
                // Hata durumunda detaylı log kaydet
                Console.WriteLine($"Email gönderme hatası: {ex.Message}");
                Console.WriteLine($"Stack Trace: {ex.StackTrace}");
                Console.WriteLine($"Inner Exception: {ex.InnerException?.Message}");
                
                var emailQueue = new EmailQueue
                {
                    PostId = postId,
                    UserId = userId,
                    Status = "Failed",
                    ErrorMessage = $"{ex.Message} | StackTrace: {ex.StackTrace}"
                };
                context.EmailQueues.Add(emailQueue);
                await context.SaveChangesAsync();

                return false;
            }
        });

        // Servis çalışırken bekle (stoppingToken iptal edilene kadar)
        while (!stoppingToken.IsCancellationRequested)
        {
            await Task.Delay(1000, stoppingToken);  // 1 saniye bekle
        }
    }
}
