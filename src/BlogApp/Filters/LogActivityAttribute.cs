using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc;
using BlogApp.Services;
using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using BlogApp.Data;

namespace BlogApp.Filters;

public class LogActivityAttribute : Attribute, IAsyncActionFilter
{
    private string? _description;

    public LogActivityAttribute(string? description = null)
    {
        _description = description;
    }

    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        // Action parametrelerinden userId'yi almaya çalış (Login/Register için)
        int? userIdFromAction = GetUserIdFromActionParameters(context);

        // Action çalıştır
        var executedContext = await next();

        // Sadece başarılı istekleri logla (200-299 status codes)
        if (executedContext.Result is ObjectResult objectResult && objectResult.StatusCode >= 200 && objectResult.StatusCode < 300)
        {
            // Service'i al
            var activityLogService = executedContext.HttpContext.RequestServices.GetRequiredService<ActivityLogService>();

            // Controller ve Action adlarını al
            var controllerName = executedContext.RouteData.Values["controller"]?.ToString() ?? "";
            var actionName = executedContext.RouteData.Values["action"]?.ToString() ?? "";

            // Kullanıcı ID'sini al (öncelik sırası: Action parametreleri > JWT > Session)
            int? userId = userIdFromAction;
            
            // JWT'den userId al (action parametrelerinden bulunamadıysa)
            if (userId == null)
            {
                var userIdClaim = executedContext.HttpContext.User?.FindFirst(ClaimTypes.NameIdentifier);
                if (userIdClaim != null && int.TryParse(userIdClaim.Value, out var jwtUserId))
                {
                    userId = jwtUserId;
                }
            }
            
            // Session'dan admin ID al (JWT yoksa)
            if (userId == null)
            {
                var adminId = executedContext.HttpContext.Session.GetString("AdminId");
                if (!string.IsNullOrEmpty(adminId) && int.TryParse(adminId, out var sessionUserId))
                {
                    userId = sessionUserId;
                }
            }

            // Login/Register için email'den userId bul
            if (userId == null && (actionName == "Login" || actionName == "Register"))
            {
                userId = await GetUserIdFromEmail(context, executedContext);
            }

            // Description oluştur (verilmemişse otomatik)
            var description = _description ?? $"{actionName} işlemi gerçekleştirildi";

            // Log kaydet
            await activityLogService.LogAsync(
                action: actionName,
                controller: controllerName,
                description: description,
                userId: userId
            );
        }
    }

    private int? GetUserIdFromActionParameters(ActionExecutingContext context)
    {
        // Action parametrelerinde userId, UserId, dto.UserId gibi alanları ara
        foreach (var parameter in context.ActionArguments.Values)
        {
            if (parameter == null) continue;

            var type = parameter.GetType();
            
            // DTO'lardan userId al
            var userIdProperty = type.GetProperty("UserId");
            if (userIdProperty != null)
            {
                var userIdValue = userIdProperty.GetValue(parameter);
                if (userIdValue != null && int.TryParse(userIdValue.ToString(), out var userId))
                {
                    if (userId > 0) return userId;
                }
            }

            // Direkt userId parametresi
            if (type == typeof(int) && context.ActionArguments.ContainsKey("userId"))
            {
                var userIdValue = context.ActionArguments["userId"];
                if (userIdValue != null && int.TryParse(userIdValue.ToString(), out var userId))
                {
                    if (userId > 0) return userId;
                }
            }
        }

        return null;
    }

    private async Task<int?> GetUserIdFromEmail(ActionExecutingContext context, ActionExecutedContext executedContext)
    {
        try
        {
            // LoginDto veya RegisterDto'dan email al
            string? email = null;
            
            foreach (var parameter in context.ActionArguments.Values)
            {
                if (parameter == null) continue;

                var type = parameter.GetType();
                var emailProperty = type.GetProperty("Email");
                if (emailProperty != null)
                {
                    email = emailProperty.GetValue(parameter)?.ToString();
                    break;
                }
            }

            if (string.IsNullOrEmpty(email)) return null;

            // DbContext'ten user'ı bul
            var dbContext = executedContext.HttpContext.RequestServices.GetRequiredService<BlogApp.Data.AppDbContext>();
            var user = await dbContext.Users
                .Where(u => u.Email == email)
                .Select(u => new { u.Id })
                .FirstOrDefaultAsync();

            return user?.Id;
        }
        catch
        {
            return null;
        }
    }
}

