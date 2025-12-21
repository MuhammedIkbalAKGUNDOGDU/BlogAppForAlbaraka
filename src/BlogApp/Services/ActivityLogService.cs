using Microsoft.EntityFrameworkCore;
using BlogApp.Data;
using BlogApp.Models;
using Microsoft.AspNetCore.Http;

namespace BlogApp.Services;

public class ActivityLogService
{
    private readonly AppDbContext _context;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public ActivityLogService(AppDbContext context, IHttpContextAccessor httpContextAccessor)
    {
        _context = context;
        _httpContextAccessor = httpContextAccessor;
    }

    public async Task LogAsync(
        string action,
        string controller,
        string description,
        int? userId = null)
    {
        try
        {
            var httpContext = _httpContextAccessor.HttpContext;
            if (httpContext == null) return;

            // IP adresini al
            var ipAddress = GetClientIPAddress(httpContext);

            // Request bilgilerini al
            var requestMethod = httpContext.Request.Method;
            var requestPath = httpContext.Request.Path.Value ?? "";

            // ActivityLog oluştur
            var activityLog = new ActivityLog
            {
                UserId = userId,
                Action = action,
                Controller = controller,
                RequestMethod = requestMethod,
                RequestPath = requestPath,
                IPAddress = ipAddress,
                Description = description,
                CreatedAt = DateTime.UtcNow
            };

            _context.ActivityLogs.Add(activityLog);
            await _context.SaveChangesAsync();
        }
        catch (Exception ex)
        {
            // Loglama hatası uygulamayı durdurmamalı
            Console.WriteLine($"ActivityLogService hatası: {ex.Message}");
        }
    }

    private string GetClientIPAddress(HttpContext httpContext)
    {
        // X-Forwarded-For header'ından IP al (proxy/load balancer arkasında)
        var forwardedFor = httpContext.Request.Headers["X-Forwarded-For"].FirstOrDefault();
        if (!string.IsNullOrEmpty(forwardedFor))
        {
            var ips = forwardedFor.Split(',');
            var ip = ips[0].Trim();
            // IPv6 localhost'u IPv4'e çevir
            if (ip == "::1" || ip == "::ffff:127.0.0.1")
                return "127.0.0.1";
            return ip;
        }

        // X-Real-IP header'ından IP al
        var realIP = httpContext.Request.Headers["X-Real-IP"].FirstOrDefault();
        if (!string.IsNullOrEmpty(realIP))
        {
            var ip = realIP;
            // IPv6 localhost'u IPv4'e çevir
            if (ip == "::1" || ip == "::ffff:127.0.0.1")
                return "127.0.0.1";
            return ip;
        }

        // Direkt connection IP'si
        var remoteIp = httpContext.Connection.RemoteIpAddress;
        if (remoteIp == null)
            return "Unknown";

        // IPv6 localhost'u IPv4'e çevir
        if (remoteIp.ToString() == "::1" || remoteIp.ToString() == "::ffff:127.0.0.1")
            return "127.0.0.1";

        // IPv6 mapped IPv4 adreslerini düzelt (::ffff:192.168.1.1 -> 192.168.1.1)
        if (remoteIp.IsIPv4MappedToIPv6)
        {
            return remoteIp.MapToIPv4().ToString();
        }

        return remoteIp.ToString();
    }
}

