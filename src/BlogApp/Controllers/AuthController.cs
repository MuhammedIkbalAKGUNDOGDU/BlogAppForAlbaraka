using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using BlogApp.Data;
using BlogApp.Models;
using BlogApp.DTOs;
using BlogApp.Services;

namespace BlogApp.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AuthController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly IConfiguration _config;
        private readonly EmailService _emailService;

        public AuthController(AppDbContext context, IConfiguration config, EmailService emailService)
        {
            _context = context;
            _config = config;
            _emailService = emailService;
        }

        // -------------------------------------------------------
        // REGISTER
        // -------------------------------------------------------
        [HttpPost("register")]
        public async Task<IActionResult> Register(RegisterDto dto)
        {
            // Email daha önce kullanılmış mı kontrol et
            var exists = await _context.Users.AnyAsync(x => x.Email == dto.Email);
            if (exists)
                return BadRequest(new { message = "Email already registered" });

            // Şifreyi hashle
            var passwordHash = BCrypt.Net.BCrypt.HashPassword(dto.Password);

            // Yeni kullanıcı oluştur
            var user = new User
            {
                FirstName = dto.FirstName,
                LastName = dto.LastName,
                Email = dto.Email,
                PasswordHash = passwordHash,
                Role = "user",
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            };

            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            return Ok(new { message = "User created successfully" });
        }

        // -------------------------------------------------------
        // LOGIN
        // -------------------------------------------------------
        [HttpPost("login")]
        public async Task<IActionResult> Login(LoginDto dto)
        {
            var user = await _context.Users.FirstOrDefaultAsync(x => x.Email == dto.Email);
            if (user == null)
                return BadRequest(new { message = "Invalid email or password" });

            var validPassword = BCrypt.Net.BCrypt.Verify(dto.Password, user.PasswordHash);
            if (!validPassword)
                return BadRequest(new { message = "Invalid email or password" });

            // JWT Token üret
            var token = GenerateToken(user);

            return Ok(new
            {
                token,
                user = new
                {
                    user.Id,
                    user.FirstName,
                    user.LastName,
                    user.Email,
                    user.Role
                }
            });
        }

        // -------------------------------------------------------
        // JWT TOKEN ÜRETME
        // -------------------------------------------------------
        private string GenerateToken(User user)
        {
            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_config["Jwt:Key"]));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var claims = new[]
            {
                new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new Claim(ClaimTypes.Email, user.Email),
                new Claim(ClaimTypes.Name, user.FirstName + " " + user.LastName),
                new Claim(ClaimTypes.Role, user.Role)
            };

            var token = new JwtSecurityToken(
                issuer: _config["Jwt:Issuer"],
                audience: _config["Jwt:Audience"],
                claims: claims,
                expires: DateTime.UtcNow.AddDays(7),
                signingCredentials: creds
            );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }

        // -------------------------------------------------------
        // ŞİFRE SIFIRLAMA - FORGOT PASSWORD
        // -------------------------------------------------------
        [HttpPost("forgot-password")]
        public async Task<IActionResult> ForgotPassword([FromBody] ForgotPasswordDto dto)
        {
            if (string.IsNullOrWhiteSpace(dto.Email))
            {
                return BadRequest(new { message = "E-posta adresi gereklidir." });
            }

            // Kullanıcıyı bul
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == dto.Email.ToLower().Trim());
            if (user == null)
            {
                // Güvenlik için: Kullanıcı yoksa da başarılı mesajı döndür (email enumeration saldırısını önler)
                return Ok(new { message = "Eğer bu e-posta adresi kayıtlıysa, şifre sıfırlama bağlantısı gönderildi." });
            }

            // Eski token'ları geçersiz kıl (aynı email için)
            var oldTokens = await _context.PasswordResetTokens
                .Where(t => t.Email == user.Email && !t.IsUsed && t.ExpiresAt > DateTime.UtcNow)
                .ToListAsync();
            
            foreach (var oldToken in oldTokens)
            {
                oldToken.IsUsed = true;
            }

            // Yeni token oluştur (5 dakika geçerli)
            var token = Guid.NewGuid().ToString() + "-" + Guid.NewGuid().ToString();
            var resetToken = new PasswordResetToken
            {
                Email = user.Email,
                Token = token,
                ExpiresAt = DateTime.UtcNow.AddMinutes(5),
                IsUsed = false,
                CreatedAt = DateTime.UtcNow
            };

            _context.PasswordResetTokens.Add(resetToken);
            await _context.SaveChangesAsync();

            // Email gönder (EmailService kullanarak)
            var appPort = Environment.GetEnvironmentVariable("APP_PORT") ?? "5055";
            var baseUrl = $"http://localhost:{appPort}";
            var resetUrl = $"{baseUrl}/AuthView/ResetPassword?token={Uri.EscapeDataString(token)}";

            var emailSubject = "Şifre Sıfırlama Talebi";
            var emailBody = $@"
                <html>
                <body style='font-family: Arial, sans-serif; line-height: 1.6; color: #333;'>
                    <div style='max-width: 600px; margin: 0 auto; padding: 20px;'>
                        <h2 style='color: #0d6efd;'>Şifre Sıfırlama</h2>
                        <p>Merhaba {user.FirstName} {user.LastName},</p>
                        <p>Şifre sıfırlama talebinde bulundunuz. Aşağıdaki bağlantıya tıklayarak yeni şifrenizi belirleyebilirsiniz:</p>
                        <p style='text-align: center; margin: 30px 0;'>
                            <a href='{resetUrl}' style='background-color: #0d6efd; color: white; padding: 12px 24px; text-decoration: none; border-radius: 5px; display: inline-block;'>Şifremi Sıfırla</a>
                        </p>
                        <p>Veya aşağıdaki bağlantıyı tarayıcınıza yapıştırabilirsiniz:</p>
                        <p style='word-break: break-all; color: #666;'>{resetUrl}</p>
                        <p><strong>Önemli:</strong> Bu bağlantı 5 dakika süreyle geçerlidir. Süresi dolduktan sonra yeni bir şifre sıfırlama talebinde bulunmanız gerekecektir.</p>
                        <p>Eğer bu talebi siz yapmadıysanız, bu e-postayı görmezden gelebilirsiniz.</p>
                        <hr style='border: none; border-top: 1px solid #eee; margin: 20px 0;'>
                        <p style='color: #666; font-size: 12px;'>Bu otomatik bir e-postadır. Lütfen yanıtlamayın.</p>
                    </div>
                </body>
                </html>";

            try
            {
                await _emailService.SendEmailAsync(
                    user.Email,
                    $"{user.FirstName} {user.LastName}",
                    emailSubject,
                    emailBody
                );
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Şifre sıfırlama emaili gönderilemedi: {ex.Message}");
                // Email gönderilemese bile token oluşturuldu, kullanıcıya genel mesaj döndür
            }

            return Ok(new { message = "Eğer bu e-posta adresi kayıtlıysa, şifre sıfırlama bağlantısı gönderildi." });
        }

        // -------------------------------------------------------
        // ŞİFRE SIFIRLAMA - TOKEN DOĞRULAMA
        // -------------------------------------------------------
        [HttpGet("validate-reset-token")]
        public async Task<IActionResult> ValidateResetToken([FromQuery] string token)
        {
            if (string.IsNullOrWhiteSpace(token))
            {
                return BadRequest(new { message = "Token gereklidir." });
            }

            var resetToken = await _context.PasswordResetTokens
                .FirstOrDefaultAsync(t => t.Token == token && !t.IsUsed);

            if (resetToken == null)
            {
                return BadRequest(new { message = "Geçersiz token." });
            }

            if (resetToken.ExpiresAt < DateTime.UtcNow)
            {
                return BadRequest(new { message = "Token süresi dolmuş." });
            }

            return Ok(new { message = "Token geçerli.", email = resetToken.Email });
        }

        // -------------------------------------------------------
        // ŞİFRE SIFIRLAMA - RESET PASSWORD
        // -------------------------------------------------------
        [HttpPost("reset-password")]
        public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordDto dto)
        {
            if (string.IsNullOrWhiteSpace(dto.Token))
            {
                return BadRequest(new { message = "Token gereklidir." });
            }

            if (string.IsNullOrWhiteSpace(dto.NewPassword) || dto.NewPassword.Length < 6)
            {
                return BadRequest(new { message = "Şifre en az 6 karakter olmalıdır." });
            }

            // Token'ı bul ve doğrula
            var resetToken = await _context.PasswordResetTokens
                .FirstOrDefaultAsync(t => t.Token == dto.Token && !t.IsUsed);

            if (resetToken == null)
            {
                return BadRequest(new { message = "Geçersiz token." });
            }

            if (resetToken.ExpiresAt < DateTime.UtcNow)
            {
                return BadRequest(new { message = "Token süresi dolmuş. Lütfen yeni bir şifre sıfırlama talebinde bulunun." });
            }

            // Kullanıcıyı bul
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == resetToken.Email);
            if (user == null)
            {
                return BadRequest(new { message = "Kullanıcı bulunamadı." });
            }

            // Şifreyi güncelle
            user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.NewPassword);
            
            // Token'ı kullanıldı olarak işaretle
            resetToken.IsUsed = true;

            await _context.SaveChangesAsync();

            return Ok(new { message = "Şifreniz başarıyla güncellendi. Giriş sayfasına yönlendiriliyorsunuz..." });
        }
    }
}
