using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using BlogApp.Data;
using BlogApp.Models;
using BlogApp.DTOs;
using BlogApp.Services;
using BlogApp.Filters;  

namespace BlogApp.Controllers
{
    [Route("api/admin")]
    [ApiController]
    public class AdminApiController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly RabbitMQService _rabbitMQService;  
        private readonly NotificationService _notificationService;  

        public AdminApiController(AppDbContext context, RabbitMQService rabbitMQService, NotificationService notificationService)
        {
            _context = context;
            _rabbitMQService = rabbitMQService;  
            _notificationService = notificationService;  
        }

        [HttpGet("draft-posts")]
        public async Task<IActionResult> GetDraftPosts()
        {
            if (!IsAdminLoggedIn())
            {
                return Unauthorized(new { message = "Admin yetkisi gereklidir." });
            }

            var posts = await _context.BlogPosts
                .Include(p => p.User)
                .Include(p => p.Category)
                .Include(p => p.Comments)
                .Include(p => p.Likes)
                .Where(p => p.IsDraft == true)
                .OrderByDescending(p => p.CreatedAt)
                .Select(p => new
                {
                    p.Id,
                    p.Title,
                    p.Content,
                    p.CreatedAt,
                    p.ViewCount,
                    User = new { p.User!.Id, p.User.FirstName, p.User.LastName, p.User.Email },
                    Category = new { p.Category!.Id, p.Category.Name },
                    CommentCount = p.Comments != null ? p.Comments.Count : 0,
                    LikeCount = p.Likes != null ? p.Likes.Count : 0
                })
                .ToListAsync();

            return Ok(posts);
        }

        // GET: api/admin/published-posts
        [HttpGet("published-posts")]
        public async Task<IActionResult> GetPublishedPosts()
        {
            if (!IsAdminLoggedIn())
            {
                return Unauthorized(new { message = "Admin yetkisi gereklidir." });
            }

            var posts = await _context.BlogPosts
                .Include(p => p.User)
                .Include(p => p.Category)
                .Include(p => p.Comments)
                .Include(p => p.Likes)
                .Where(p => p.IsDraft == false)
                .OrderByDescending(p => p.CreatedAt)
                .Select(p => new
                {
                    p.Id,
                    p.Title,
                    p.Content,
                    p.CreatedAt,
                    p.ViewCount,
                    User = new { p.User!.Id, p.User.FirstName, p.User.LastName, p.User.Email },
                    Category = new { p.Category!.Id, p.Category.Name },
                    CommentCount = p.Comments != null ? p.Comments.Count : 0,
                    LikeCount = p.Likes != null ? p.Likes.Count : 0
                })
                .ToListAsync();

            return Ok(posts);
        }

        // POST: api/admin/approve-post/{id}
        [HttpPost("approve-post/{id}")]
        [LogActivity("Blog yazısı onaylandı ve yayınlandı")]
        public async Task<IActionResult> ApprovePost(int id)
        {
            if (!IsAdminLoggedIn())
            {
                return Unauthorized(new { message = "Admin yetkisi gereklidir." });
            }

            var post = await _context.BlogPosts
                .Include(p => p.User)  // Yazar bilgisini de getir
                .FirstOrDefaultAsync(p => p.Id == id);
            
            if (post == null)
            {
                return NotFound(new { message = "Yazı bulunamadı." });
            }

            // Yazıyı onayla ve yayınla
            post.IsDraft = false;  // Draft'tan çıkar
            post.UpdatedAt = DateTime.UtcNow;  // Güncelleme tarihini ayarla

            await _context.SaveChangesAsync();  // Değişiklikleri kaydet

            // Yazarın takipçilerini bul
            var followers = await _context.Set<UserFollower>()
                .Where(uf => uf.FollowingId == post.UserId)  // Bu yazarı takip edenler
                .Select(uf => uf.FollowerId)  // Sadece takipçi ID'lerini al
                .ToListAsync();

            // Her takipçi için RabbitMQ'ya mesaj gönder
            foreach (var followerId in followers)
            {
                _rabbitMQService.PublishEmailMessage(post.Id, followerId);  // RabbitMQ'ya mesaj ekle
            }

            // Yazarına bildirim gönder
            await _notificationService.SendNotificationAsync(NotificationType.PostApproved, post.UserId, post.Id);

            return Ok(new { message = "Yazı onaylandı ve yayınlandı.", emailCount = followers.Count });
        }

        // POST: api/admin/unpublish-post/{id}
        [HttpPost("unpublish-post/{id}")]
        [LogActivity("Blog yazısı yayından kaldırıldı")]
        public async Task<IActionResult> UnpublishPost(int id)
        {
            if (!IsAdminLoggedIn())
            {
                return Unauthorized(new { message = "Admin yetkisi gereklidir." });
            }

            var post = await _context.BlogPosts.FindAsync(id);
            if (post == null)
            {
                return NotFound(new { message = "Yazı bulunamadı." });
            }

            var userId = post.UserId; // Silmeden önce kullanıcı ID'sini al

            post.IsDraft = true;
            post.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            // Yazarına bildirim gönder
            await _notificationService.SendNotificationAsync(NotificationType.PostUnpublished, userId, post.Id);

            return Ok(new { message = "Yazı draft'a çekildi." });
        }

        // DELETE: api/admin/delete-post/{id}
        [HttpDelete("delete-post/{id}")]
        [LogActivity("Blog yazısı admin tarafından silindi")]
        public async Task<IActionResult> DeletePost(int id)
        {
            if (!IsAdminLoggedIn())
            {
                return Unauthorized(new { message = "Admin yetkisi gereklidir." });
            }

            var post = await _context.BlogPosts
                .Include(p => p.Category)
                .FirstOrDefaultAsync(p => p.Id == id);
            
            if (post == null)
            {
                return NotFound(new { message = "Yazı bulunamadı." });
            }

            var userId = post.UserId; // Silmeden önce kullanıcı ID'sini al
            var postId = post.Id; // Silmeden önce post ID'sini al

            _context.BlogPosts.Remove(post);
            await _context.SaveChangesAsync();

            // Yazarına bildirim gönder (post silindikten sonra ama bilgileri hala elimizde)
            await _notificationService.SendNotificationAsync(NotificationType.PostDeleted, userId, postId);

            return Ok(new { message = "Yazı başarıyla silindi." });
        }

        // GET: api/admin/users
        [HttpGet("users")]
        public async Task<IActionResult> GetUsers([FromQuery] string? search = null, [FromQuery] string? role = null, [FromQuery] int? status = null)
        {
            if (!IsAdminLoggedIn())
            {
                return Unauthorized(new { message = "Admin yetkisi gereklidir." });
            }

            var query = _context.Users.AsQueryable();

            // İsim veya email'e göre arama
            if (!string.IsNullOrWhiteSpace(search))
            {
                var searchTerm = search.Trim().ToLower();
                query = query.Where(u => 
                    u.FirstName.ToLower().Contains(searchTerm) ||
                    u.LastName.ToLower().Contains(searchTerm) ||
                    u.Email.ToLower().Contains(searchTerm) ||
                    (u.FirstName + " " + u.LastName).ToLower().Contains(searchTerm)
                );
            }

            // Role göre filtreleme
            if (!string.IsNullOrWhiteSpace(role) && (role == "admin" || role == "user"))
            {
                query = query.Where(u => u.Role == role);
            }

            // Status göre filtreleme
            if (status.HasValue && Enum.IsDefined(typeof(UserStatus), status.Value))
            {
                query = query.Where(u => u.Status == (UserStatus)status.Value);
            }

            var users = await query
                .Select(u => new
                {
                    u.Id,
                    u.FirstName,
                    u.LastName,
                    u.Email,
                    u.Role,
                    u.Status,
                    u.CreatedAt,
                    PostCount = u.Posts != null ? u.Posts.Count : 0
                })
                .OrderByDescending(u => u.CreatedAt)
                .ToListAsync();

            return Ok(users);
        }

        // PUT: api/admin/update-user-status
        [HttpPut("update-user-status")]
        [LogActivity("Kullanıcı durumu güncellendi")]
        public async Task<IActionResult> UpdateUserStatus([FromBody] UserStatusUpdateDto dto)
        {
            if (!IsAdminLoggedIn())
            {
                return Unauthorized(new { message = "Admin yetkisi gereklidir." });
            }

            var user = await _context.Users.FindAsync(dto.UserId);
            if (user == null)
            {
                return NotFound(new { message = "Kullanıcı bulunamadı." });
            }

            // Admin kendisini banlayamaz
            var adminId = HttpContext.Session.GetString("AdminId");
            if (adminId != null && int.Parse(adminId) == dto.UserId)
            {
                return BadRequest(new { message = "Kendi hesabınızın durumunu değiştiremezsiniz." });
            }

            user.Status = dto.Status;
            user.IsActive = dto.Status == UserStatus.Active; 

            // Suspended durumu için tarih kaydet, Active yapıldığında temizle
            if (dto.Status == UserStatus.Suspended)
            {
                user.SuspendedAt = DateTime.UtcNow;
            }
            else if (dto.Status == UserStatus.Active)
            {
                user.SuspendedAt = null; 
            }

            await _context.SaveChangesAsync();

            // Bildirim gönder
            if (dto.Status == UserStatus.Banned)
            {
                await _notificationService.SendNotificationAsync(NotificationType.UserBanned, user.Id);
            }
            else if (dto.Status == UserStatus.Suspended)
            {
                await _notificationService.SendNotificationAsync(NotificationType.UserSuspended, user.Id);
            }

            var statusText = dto.Status switch
            {
                UserStatus.Active => "Aktif",
                UserStatus.Suspended => "Askıya Alındı",
                UserStatus.Banned => "Yasaklandı",
                _ => "Bilinmeyen"
            };

            return Ok(new { message = $"Kullanıcı durumu '{statusText}' olarak güncellendi." });
        }

        // POST: api/admin/create-admin
        [HttpPost("create-admin")]
        [LogActivity("Yeni admin kullanıcısı oluşturuldu")]
        public async Task<IActionResult> CreateAdmin([FromBody] AdminRegisterDto dto)
        {
            if (string.IsNullOrWhiteSpace(dto.Email) || string.IsNullOrWhiteSpace(dto.Password))
            {
                return BadRequest(new { message = "Email ve şifre gereklidir." });
            }

            if (string.IsNullOrWhiteSpace(dto.FirstName) || string.IsNullOrWhiteSpace(dto.LastName))
            {
                return BadRequest(new { message = "Ad ve soyad gereklidir." });
            }

            // Email daha önce kullanılmış mı kontrol et
            var exists = await _context.Users.AnyAsync(u => u.Email == dto.Email);
            if (exists)
            {
                return BadRequest(new { message = "Bu email adresi zaten kullanılıyor." });
            }

            // Şifreyi hashle
            var passwordHash = BCrypt.Net.BCrypt.HashPassword(dto.Password);

            // Yeni admin kullanıcı oluştur
            var admin = new User
            {
                FirstName = dto.FirstName,
                LastName = dto.LastName,
                Email = dto.Email,
                PasswordHash = passwordHash,
                Role = "admin",
                IsActive = true,
                Status = UserStatus.Active,
                CreatedAt = DateTime.UtcNow
            };

            _context.Users.Add(admin);
            await _context.SaveChangesAsync();

            return Ok(new { message = "Admin kullanıcısı başarıyla oluşturuldu.", adminId = admin.Id });
        }

        // GET: api/admin/stats
        [HttpGet("stats")]
        public async Task<IActionResult> GetStats()
        {
            if (!IsAdminLoggedIn())
            {
                return Unauthorized(new { message = "Admin yetkisi gereklidir." });
            }

            var stats = new
            {
                TotalUsers = await _context.Users.CountAsync(),
                ActiveUsers = await _context.Users.CountAsync(u => u.Status == UserStatus.Active),
                SuspendedUsers = await _context.Users.CountAsync(u => u.Status == UserStatus.Suspended),
                BannedUsers = await _context.Users.CountAsync(u => u.Status == UserStatus.Banned),
                TotalPosts = await _context.BlogPosts.CountAsync(),
                DraftPosts = await _context.BlogPosts.CountAsync(p => p.IsDraft == true),
                PublishedPosts = await _context.BlogPosts.CountAsync(p => p.IsDraft == false),
                TotalCategories = await _context.Categories.CountAsync()
            };

            return Ok(stats);
        }

        // GET: api/admin/activity-logs
        [HttpGet("activity-logs")]
        public async Task<IActionResult> GetActivityLogs(
            [FromQuery] int? userId = null,
            [FromQuery] string? action = null,
            [FromQuery] string? controller = null,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 50)
        {
            if (!IsAdminLoggedIn())
            {
                return Unauthorized(new { message = "Admin yetkisi gereklidir." });
            }

            var query = _context.ActivityLogs
                .Include(log => log.User)
                .AsQueryable();

            // Filtreleme
            if (userId.HasValue)
            {
                query = query.Where(log => log.UserId == userId.Value);
            }

            if (!string.IsNullOrEmpty(action))
            {
                query = query.Where(log => log.Action == action);
            }

            if (!string.IsNullOrEmpty(controller))
            {
                query = query.Where(log => log.Controller == controller);
            }

            // Toplam kayıt sayısı
            var totalCount = await query.CountAsync();

            // Sayfalama
            var logs = await query
                .OrderByDescending(log => log.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(log => new
                {
                    log.Id,
                    log.UserId,
                    UserName = log.User != null ? $"{log.User.FirstName} {log.User.LastName}" : "Anonim",
                    UserEmail = log.User != null ? log.User.Email : null,
                    log.Action,
                    log.Controller,
                    log.RequestMethod,
                    log.RequestPath,
                    log.IPAddress,
                    log.Description,
                    log.CreatedAt
                })
                .ToListAsync();

            return Ok(new
            {
                logs,
                totalCount,
                page,
                pageSize,
                totalPages = (int)Math.Ceiling(totalCount / (double)pageSize)
            });
        }

        // GET: api/admin/activity-logs/filters
        [HttpGet("activity-logs/filters")]
        public async Task<IActionResult> GetActivityLogFilters()
        {
            if (!IsAdminLoggedIn())
            {
                return Unauthorized(new { message = "Admin yetkisi gereklidir." });
            }

            // Tüm unique action'ları getir
            var actions = await _context.ActivityLogs
                .Select(log => log.Action)
                .Distinct()
                .OrderBy(a => a)
                .ToListAsync();

            // Tüm unique controller'ları getir
            var controllers = await _context.ActivityLogs
                .Select(log => log.Controller)
                .Distinct()
                .OrderBy(c => c)
                .ToListAsync();

            return Ok(new
            {
                actions,
                controllers
            });
        }

        private bool IsAdminLoggedIn()
        {
            return !string.IsNullOrEmpty(HttpContext.Session.GetString("AdminId"));
        }
    }
}
