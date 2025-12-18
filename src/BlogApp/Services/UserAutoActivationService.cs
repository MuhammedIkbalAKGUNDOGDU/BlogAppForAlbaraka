using Microsoft.EntityFrameworkCore;
using BlogApp.Data;
using BlogApp.Models;

namespace BlogApp.Services;

public class UserAutoActivationService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly TimeSpan _checkInterval = TimeSpan.FromHours(1); // Her saat kontrol et
    private readonly int _suspensionDays = 5; // 5 gün sonra aktif et

    public UserAutoActivationService(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        Console.WriteLine("UserAutoActivationService başlatıldı - Suspended kullanıcılar 5 gün sonra otomatik aktif edilecek");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await CheckAndActivateSuspendedUsers();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"UserAutoActivationService hatası: {ex.Message}");
                Console.WriteLine($"Stack Trace: {ex.StackTrace}");
            }

            // Belirlenen süre kadar bekle
            await Task.Delay(_checkInterval, stoppingToken);
        }
    }

    private async Task CheckAndActivateSuspendedUsers()
    {
        // Her kontrol için yeni scope oluştur (DbContext için)
        using var scope = _serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        // 5 gün önce suspended olan kullanıcıları bul
        var cutoffDate = DateTime.UtcNow.AddDays(-_suspensionDays);

        var suspendedUsers = await context.Users
            .Where(u => u.Status == UserStatus.Suspended 
                     && u.SuspendedAt.HasValue 
                     && u.SuspendedAt.Value <= cutoffDate)
            .ToListAsync();

        if (suspendedUsers.Any())
        {
            Console.WriteLine($"{suspendedUsers.Count} suspended kullanıcı bulundu, aktif ediliyor...");

            foreach (var user in suspendedUsers)
            {
                user.Status = UserStatus.Active;
                user.IsActive = true;
                user.SuspendedAt = null; // Tarihi temizle

                Console.WriteLine($"Kullanıcı aktif edildi: {user.Email} (ID: {user.Id})");
            }

            await context.SaveChangesAsync();
            Console.WriteLine($"{suspendedUsers.Count} kullanıcı başarıyla aktif edildi");
        }
    }
}

