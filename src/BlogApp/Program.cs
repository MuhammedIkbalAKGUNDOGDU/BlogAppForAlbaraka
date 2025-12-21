using DotNetEnv;
using Microsoft.EntityFrameworkCore;
using BlogApp.Data;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using BlogApp.Services;  

var builder = WebApplication.CreateBuilder(args);

Env.Load();

var port = Environment.GetEnvironmentVariable("APP_PORT") ?? "5001";
builder.WebHost.UseUrls($"http://*:{port}");

var dbHost = Environment.GetEnvironmentVariable("DB_HOST");
var dbPort = Environment.GetEnvironmentVariable("DB_PORT");
var dbName = Environment.GetEnvironmentVariable("DB_NAME");
var dbUser = Environment.GetEnvironmentVariable("DB_USER");
var dbPass = Environment.GetEnvironmentVariable("DB_PASS");

var connectionString =
    $"Host={dbHost};Port={dbPort};Database={dbName};Username={dbUser};Password={dbPass}";

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(connectionString)
);


// JWT Authentication yapamadım ama olsun
builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.SaveToken = true;
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = false,         
        ValidateAudience = false,       
        ValidateIssuerSigningKey = true,
        IssuerSigningKey = new SymmetricSecurityKey(
            Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Key"])
        ),
        ClockSkew = TimeSpan.Zero
    };
});

builder.Services.AddAuthorization();

builder.Services.AddControllersWithViews();
builder.Services.AddHttpContextAccessor();

// Activity Log Service - Scoped olarak ekle (her request için yeni instance)
builder.Services.AddScoped<ActivityLogService>(); 

// Email Service - Singleton olarak ekle (tek instance, tüm uygulama boyunca yaşar)
builder.Services.AddSingleton<EmailService>();  

// RabbitMQ Service - Singleton olarak ekle (tek bağlantı, tüm uygulama boyunca yaşar)
builder.Services.AddSingleton<RabbitMQService>();  

// Notification Service - Singleton olarak ekle (admin işlem bildirimleri için)
builder.Services.AddSingleton<NotificationService>(); 

// Email Consumer Service - Background service olarak ekle (arka planda sürekli çalışır)
builder.Services.AddHostedService<EmailConsumerService>();

// User Auto Activation Service - Suspended kullanıcıları 5 gün sonra otomatik aktif eder
builder.Services.AddHostedService<UserAutoActivationService>();  

// jwt olmadı admin panel için session support
builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(30);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();

app.UseRouting();

app.UseSession();          // Session middleware

app.UseAuthentication(); 
app.UseAuthorization();   

app.MapStaticAssets();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}")
    .WithStaticAssets();

app.Run();
